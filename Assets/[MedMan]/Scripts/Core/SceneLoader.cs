using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace MedMan.Core
{
    /// <summary>
    /// Async scene loader with DoTween fade transitions.
    /// Listens for OnLevelReadyToPlayEvent to reveal the level after all systems confirm readiness.
    ///
    /// Required scene setup:
    ///   - A GameObject named "FadeCanvas" tagged "FadeCanvas" with:
    ///     - Canvas (Render Mode: Screen Space Overlay)
    ///     - CanvasGroup component
    ///     - Child Image named "FadePanel" (black, full screen stretch)
    ///   SceneLoader will call DontDestroyOnLoad on it automatically.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Singleton
        // ─────────────────────────────────────────

        public static SceneLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SubscribeToEvents();
            Debug.Log("[SceneLoader] Initialized.");
        }

        private void Start()
        {
            InitializeFadeCanvas();
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        // ─────────────────────────────────────────
        // Fade Canvas
        // ─────────────────────────────────────────

        [Header("Fade Settings")]
        [SerializeField] private float _fadeDuration = 0.5f;

        private CanvasGroup _fadeCanvasGroup;
        private bool _isLoading;

        private void InitializeFadeCanvas()
        {
            GameObject fadeCanvas = GameObject.FindGameObjectWithTag("FadeCanvas");

            if (fadeCanvas == null)
            {
                Debug.LogError("[SceneLoader] FadeCanvas not found. Create a Canvas tagged 'FadeCanvas' with a CanvasGroup component.");
                return;
            }

            DontDestroyOnLoad(fadeCanvas);
            _fadeCanvasGroup = fadeCanvas.GetComponent<CanvasGroup>();

            if (_fadeCanvasGroup == null)
            {
                Debug.LogError("[SceneLoader] FadeCanvas is missing a CanvasGroup component.");
                return;
            }

            // Start fully transparent
            _fadeCanvasGroup.alpha = 0f;
        }

        // ─────────────────────────────────────────
        // Event subscriptions
        // ─────────────────────────────────────────

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<OnLevelReadyToPlayEvent>(HandleLevelReady);
            EventBus.Subscribe<OnGameStateChangedEvent>(HandleGameStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<OnLevelReadyToPlayEvent>(HandleLevelReady);
            EventBus.Unsubscribe<OnGameStateChangedEvent>(HandleGameStateChanged);
        }

        // ─────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────

        /// <summary>
        /// Loads a scene asynchronously with fade out → load → fade in transition.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneLoader] Load already in progress — ignoring request.");
                return;
            }

            LoadSceneAsync(sceneName).Forget();
        }

        // ─────────────────────────────────────────
        // Async load pipeline
        // ─────────────────────────────────────────

        private async UniTaskVoid LoadSceneAsync(string sceneName)
        {
            _isLoading = true;

            EventBus.Publish(new OnSceneLoadStartedEvent(sceneName));
            Debug.Log($"[SceneLoader] Loading scene: {sceneName}");

            // Fade out
            await FadeAsync(1f);

            // Load scene async — allow activation only when fully loaded
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            // Wait until scene is ready (progress reaches 0.9 = fully loaded, awaiting activation)
            await UniTask.WaitUntil(() => operation.progress >= 0.9f);

            // Activate scene
            operation.allowSceneActivation = true;

            // Wait one frame for scene to fully initialize
            await UniTask.NextFrame();

            // Fade in
            await FadeAsync(0f);

            _isLoading = false;

            EventBus.Publish(new OnSceneLoadCompletedEvent(sceneName));
            Debug.Log($"[SceneLoader] Scene loaded: {sceneName}");
        }

        // ─────────────────────────────────────────
        // Fade helpers
        // ─────────────────────────────────────────

        private UniTask FadeAsync(float targetAlpha)
        {
            if (_fadeCanvasGroup == null)
                return UniTask.CompletedTask;

            var tcs = new UniTaskCompletionSource();

            _fadeCanvasGroup
                .DOFade(targetAlpha, _fadeDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => tcs.TrySetResult());

            return tcs.Task;
        }

        /// <summary>
        /// Instantly fades to black without animation. Useful for hard cuts.
        /// </summary>
        public void FadeToBlackImmediate()
        {
            if (_fadeCanvasGroup != null)
                _fadeCanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Instantly clears the fade overlay. Useful on game start.
        /// </summary>
        public void ClearFadeImmediate()
        {
            if (_fadeCanvasGroup != null)
                _fadeCanvasGroup.alpha = 0f;
        }

        // ─────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────

        private void HandleGameStateChanged(OnGameStateChangedEvent e)
        {
            // TODO CS-02: map StateID to scene name and trigger load if needed
        }

        private void HandleLevelReady(OnLevelReadyToPlayEvent e)
        {
            // Level is ready — fade in to reveal it to the player
            if (_fadeCanvasGroup != null)
                FadeAsync(0f).Forget();

            Debug.Log($"[SceneLoader] Level ready — revealing: {e.StateID}");
        }
    }
}
