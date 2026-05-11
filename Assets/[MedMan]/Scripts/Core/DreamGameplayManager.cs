using UnityEngine;

namespace MedMan.Core
{
    /// <summary>
    /// Manager for Dream-specific gameplay mechanics.
    /// Active throughout the game but internal mechanics are enabled/disabled
    /// based on the current game state.
    ///
    /// Responsibilities:
    /// - Loading the appropriate FearXManager based on FearType
    /// - Managing PillSystem (active only during Dream)
    /// - Routing StateID to the active FearXManager which configures itself
    ///
    /// Adding a new fear = add a FearXManager and register it here.
    /// Zero changes to GameManager or any other part of the architecture.
    /// </summary>
    public class DreamGameplayManager : MonoBehaviour
    {
        public static DreamGameplayManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeToEvents();
            Debug.Log("[DreamGameplayManager] Initialized.");
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        // ─────────────────────────────────────────
        // System references
        // ─────────────────────────────────────────

        // TODO CS-10: PillSystem — assign in Inspector
        // TODO: FearAManager — assign in Inspector
        // TODO: FearBManager — assign in Inspector (future expansion)

        // ─────────────────────────────────────────
        // Event subscriptions
        // ─────────────────────────────────────────

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<OnLevelInitializedEvent>(HandleLevelInitialized);
            EventBus.Subscribe<OnGameStateChangedEvent>(HandleGameStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<OnLevelInitializedEvent>(HandleLevelInitialized);
            EventBus.Unsubscribe<OnGameStateChangedEvent>(HandleGameStateChanged);
        }

        // ─────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────

        private void HandleGameStateChanged(OnGameStateChangedEvent e)
        {
            bool isDream = e.NewState == GameState.Dream;

            // Enable / disable PillSystem based on state
            // TODO CS-10: PillSystem?.SetActive(isDream);

            if (!isDream)
                DeactivateCurrentFearManager();
        }

        private void HandleLevelInitialized(OnLevelInitializedEvent e)
        {
            ActivateFearManager(e.FearType, e.StateID);
        }

        // ─────────────────────────────────────────
        // Fear Manager routing
        // ─────────────────────────────────────────

        private void ActivateFearManager(FearType fearType, string stateID)
        {
            DeactivateCurrentFearManager();

            switch (fearType)
            {
                case FearType.Fear_A:
                    // TODO: FearAManager?.Activate(stateID);
                    Debug.Log($"[DreamGameplayManager] Activating FearAManager for: {stateID}");
                    break;

                case FearType.Fear_B:
                    // TODO: FearBManager?.Activate(stateID);
                    Debug.Log($"[DreamGameplayManager] Activating FearBManager for: {stateID}");
                    break;

                default:
                    Debug.LogWarning($"[DreamGameplayManager] No FearManager registered for: {fearType}");
                    break;
            }

            // Confirm readiness to GameManager loading barrier
            EventBus.Publish(new OnSystemReadyEvent("DreamGameplayManager"));
        }

        private void DeactivateCurrentFearManager()
        {
            // TODO: FearAManager?.Deactivate();
            // TODO: FearBManager?.Deactivate();
        }
    }
}
