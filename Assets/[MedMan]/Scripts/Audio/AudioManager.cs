using UnityEngine;

namespace MedMan.Audio
{
    /// <summary>
    /// Audio manager. Handles Audio Mixer Snapshots, SFX, music and ambient layers.
    /// Implementation: CS-03.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeToEvents();
            Debug.Log("[AudioManager] Initialized.");
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
            // TODO CS-03: transition Audio Mixer Snapshot for the new state
        }

        private void HandleLevelInitialized(Core.OnLevelInitializedEvent e)
        {
            // TODO CS-03: load audio configuration from FearProfileSO for e.StateID
            // Confirm readiness to GameManager loading barrier:
            Core.EventBus.Publish(new Core.OnSystemReadyEvent("AudioManager"));
        }
    }
}
