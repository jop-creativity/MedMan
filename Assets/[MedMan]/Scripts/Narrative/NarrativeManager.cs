using UnityEngine;

namespace MedMan.Narrative
{
    /// <summary>
    /// Narrative manager. Handles the dialogue system, world-space TMP text,
    /// idle monologues and text accumulation at narrative climaxes.
    /// Implementation: ND-01, ND-02, ND-03, ND-04.
    /// </summary>
    public class NarrativeManager : MonoBehaviour
    {
        public static NarrativeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeToEvents();
            Debug.Log("[NarrativeManager] Initialized.");
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
            // TODO ND-01: clear active dialogues on state change
        }

        private void HandleLevelInitialized(Core.OnLevelInitializedEvent e)
        {
            // TODO ND-05: load narrative sequences for e.StateID
            // Confirm readiness to GameManager loading barrier:
            Core.EventBus.Publish(new Core.OnSystemReadyEvent("NarrativeManager"));
        }

        /// <summary>
        /// Displays a line of dialogue as world-space text.
        /// Called by FearXNarrativeController.
        /// Implementation: ND-02.
        /// </summary>
        public void ShowWorldSpaceText(string text, Vector3 worldPosition)
        {
            // TODO ND-02
        }

        /// <summary>
        /// Queues a protagonist dialogue line.
        /// Implementation: ND-01.
        /// </summary>
        public void QueueDialogueLine(string text)
        {
            // TODO ND-01
        }
    }
}
