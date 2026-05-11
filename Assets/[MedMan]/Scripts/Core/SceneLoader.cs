using UnityEngine;

namespace MedMan.Core
{
    /// <summary>
    /// Scene loading manager. Handles async scene loading with transitions.
    /// Reveals the level to the player only after receiving OnLevelReadyToPlayEvent.
    /// Implementation: CS-02.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeToEvents();
            Debug.Log("[SceneLoader] Initialized.");
        }

        private void OnDestroy() => UnsubscribeFromEvents();

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

        private void HandleGameStateChanged(OnGameStateChangedEvent e)
        {
            // TODO CS-02: begin async scene loading for e.StateID
        }

        private void HandleLevelReady(OnLevelReadyToPlayEvent e)
        {
            // TODO CS-02: complete fade-in, reveal level to player
            Debug.Log($"[SceneLoader] Level ready to show: {e.StateID}");
        }

        /// <summary>
        /// Loads a scene asynchronously with transition.
        /// Implementation: CS-02.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            // TODO CS-02: UniTask async load with transition
        }
    }
}
