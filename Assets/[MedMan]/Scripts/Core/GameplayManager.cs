using UnityEngine;

namespace MedMan.Core
{
    /// <summary>
    /// Manager for universal gameplay mechanics — active throughout the entire game.
    /// Owns systems that operate in all sections:
    /// player movement, interactions, and choices/dialogue.
    ///
    /// Dream-specific mechanics (Pills, FearXManagers) belong to DreamGameplayManager.
    /// </summary>
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeToEvents();
            Debug.Log("[GameplayManager] Initialized.");
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        // ─────────────────────────────────────────
        // System references
        // Assign in Inspector or via GetComponent in Awake
        // ─────────────────────────────────────────

        // TODO CS-05: PlayerController
        // TODO CS-07: InteractionSystem
        // TODO: ChoiceManager

        // ─────────────────────────────────────────
        // Event subscriptions
        // ─────────────────────────────────────────

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<OnGameStateChangedEvent>(HandleGameStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<OnGameStateChangedEvent>(HandleGameStateChanged);
        }

        // ─────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────

        private void HandleGameStateChanged(OnGameStateChangedEvent e)
        {
            // Example: disable player movement during cutscenes
            // PlayerController?.SetMovementEnabled(e.NewState != GameState.DoctorsOffice);

            Debug.Log($"[GameplayManager] State changed → {e.StateID}");
        }
    }
}
