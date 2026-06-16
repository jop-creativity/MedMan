using UnityEngine;

namespace MedMan.Visual
{
    /// <summary>
    /// Visual effects manager. Owns visual-state routing and delegates the actual
    /// post-processing work to registered subsystems (VolumeTransitionSystem, Kuwahara).
    /// Reacts to game-state and pill events and drives the registered VolumeTransitionSystem.
    /// Implementation: VS-01, VS-02, VS-03.
    /// </summary>
    public class VisualManager : MonoBehaviour
    {
        public static VisualManager Instance { get; private set; }

        // ─────────────────────────────────────────
        // Registered subsystems
        // ─────────────────────────────────────────

        private VolumeTransitionSystem _volumeTransition;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeToEvents();
            Debug.Log("[VisualManager] Initialized.");
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        // ─────────────────────────────────────────
        // Public API — subsystem registration
        // ─────────────────────────────────────────

        /// <summary>Registers the per-scene VolumeTransitionSystem that this manager drives.</summary>
        public void RegisterVolumeTransition(VolumeTransitionSystem system) => _volumeTransition = system;

        /// <summary>Clears the registration if it matches the current subsystem.</summary>
        public void UnregisterVolumeTransition(VolumeTransitionSystem system)
        {
            if (_volumeTransition == system) _volumeTransition = null;
        }

        // ─────────────────────────────────────────
        // Event subscriptions
        // ─────────────────────────────────────────

        private void SubscribeToEvents()
        {
            Core.EventBus.Subscribe<Core.OnGameStateChangedEvent>(HandleGameStateChanged);
            Core.EventBus.Subscribe<Core.OnLevelInitializedEvent>(HandleLevelInitialized);
            Core.EventBus.Subscribe<Core.OnPillConsumedEvent>(HandlePillConsumed);
            Core.EventBus.Subscribe<Core.OnPillExpiredEvent>(HandlePillExpired);
        }

        private void UnsubscribeFromEvents()
        {
            Core.EventBus.Unsubscribe<Core.OnGameStateChangedEvent>(HandleGameStateChanged);
            Core.EventBus.Unsubscribe<Core.OnLevelInitializedEvent>(HandleLevelInitialized);
            Core.EventBus.Unsubscribe<Core.OnPillConsumedEvent>(HandlePillConsumed);
            Core.EventBus.Unsubscribe<Core.OnPillExpiredEvent>(HandlePillExpired);
        }

        // ─────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────

        private void HandleGameStateChanged(Core.OnGameStateChangedEvent e)
        {
            _volumeTransition?.ApplyStateProfile(e.NewState);
            // TODO VS-01: update Kuwahara Filter parameters per state
        }

        private void HandlePillConsumed(Core.OnPillConsumedEvent e)
            => _volumeTransition?.ApplyPillProfile();

        private void HandlePillExpired(Core.OnPillExpiredEvent e)
            => _volumeTransition?.ApplyPillExpiry();

        private void HandleLevelInitialized(Core.OnLevelInitializedEvent e)
        {
            // TODO VS-03: load visual configuration from FearProfileSO for e.StateID
            // Confirm readiness to GameManager loading barrier:
            Core.EventBus.Publish(new Core.OnSystemReadyEvent("VisualManager"));
        }

        /// <summary>
        /// Plays a visual effects package from a ScriptableObject.
        /// Called by FearXVFXController. Implementation: VS-03.
        /// </summary>
        public void PlayVisualPackage(ScriptableObject visualPackageSO)
        {
            // TODO VS-03: replace ScriptableObject with the target VisualPackageSO type
        }
    }
}