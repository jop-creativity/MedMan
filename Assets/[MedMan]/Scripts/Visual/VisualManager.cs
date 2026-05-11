using UnityEngine;

namespace MedMan.Visual
{
    /// <summary>
    /// Visual effects manager. Handles Volume Profiles, Kuwahara shader,
    /// screen shake and state-based visual transitions.
    /// Implementation: VS-01, VS-02, VS-03.
    /// </summary>
    public class VisualManager : MonoBehaviour
    {
        public static VisualManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeToEvents();
            Debug.Log("[VisualManager] Initialized.");
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        private void SubscribeToEvents()
        {
            Core.EventBus.Subscribe<Core.OnGameStateChangedEvent>(HandleGameStateChanged);
            Core.EventBus.Subscribe<Core.OnLevelInitializedEvent>(HandleLevelInitialized);
        }

        private void UnsubscribeFromEvents()
        {
            Core.EventBus.Unsubscribe<Core.OnGameStateChangedEvent>(HandleGameStateChanged);
            Core.EventBus.Unsubscribe<Core.OnLevelInitializedEvent>(HandleLevelInitialized);
        }

        private void HandleGameStateChanged(Core.OnGameStateChangedEvent e)
        {
            // TODO VS-02: transition Volume Profile for the new state
            // TODO VS-01: update Kuwahara Filter parameters
        }

        private void HandleLevelInitialized(Core.OnLevelInitializedEvent e)
        {
            // TODO VS-03: load visual configuration from FearProfileSO for e.StateID
            // Confirm readiness to GameManager loading barrier:
            Core.EventBus.Publish(new Core.OnSystemReadyEvent("VisualManager"));
        }

        /// <summary>
        /// Plays a visual effects package from a ScriptableObject.
        /// Called by FearXVFXController.
        /// Implementation: VS-03.
        /// </summary>
        public void PlayVisualPackage(ScriptableObject visualPackageSO)
        {
            // TODO VS-03: replace ScriptableObject with the target VisualPackageSO type
        }
    }
}
