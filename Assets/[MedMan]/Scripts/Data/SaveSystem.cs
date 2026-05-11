using UnityEngine;

namespace MedMan.Data
{
    /// <summary>
    /// Save and load system. Self-contained — knows what to save and where to fetch data from.
    /// GameManager calls only Save() and Load(). No parameters, no external dependencies.
    ///
    /// Data is fetched via properties that reach directly into source systems.
    /// Example: int PillsConsumed { get => PillSystem.Instance.PillsConsumed; }
    ///
    /// Each system exposes its own properties. SaveSystem reads them at save time.
    /// Implementation: CS-04.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SubscribeToEvents();
            Debug.Log("[SaveSystem] Initialized.");
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        private void SubscribeToEvents()
        {
            Core.EventBus.Subscribe<Core.OnGameStateChangedEvent>(HandleGameStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            Core.EventBus.Unsubscribe<Core.OnGameStateChangedEvent>(HandleGameStateChanged);
        }

        private void HandleGameStateChanged(Core.OnGameStateChangedEvent e)
        {
            // TODO CS-04: autosave on state change if appropriate
        }

        // ─────────────────────────────────────────
        // Data source properties
        // Each property reaches into the source system at save time.
        // SaveSystem holds no copies — single source of truth stays in each system.
        // ─────────────────────────────────────────

        // TODO CS-04: add properties pointing to source systems
        // Examples:
        // private int PillsConsumed       => PillSystem.Instance?.PillsConsumed ?? 0;
        // private FearType SelectedFear   => Core.GameManager.Instance?.CurrentFearType ?? Core.FearType.None;
        // private string CurrentStateID   => Core.GameManager.Instance?.CurrentStateID ?? string.Empty;

        // ─────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────

        /// <summary>
        /// Saves game state. SaveSystem collects data from its own properties.
        /// Implementation: CS-04.
        /// </summary>
        public void Save()
        {
            // TODO CS-04: collect data from properties, serialize to JSON, write to disk
            Debug.Log("[SaveSystem] Save() — TODO CS-04");
            Core.EventBus.Publish(new Core.OnGameSavedEvent(Core.GameManager.Instance?.CurrentStateID ?? string.Empty));
        }

        /// <summary>
        /// Reads save data and restores game state.
        /// Implementation: CS-04.
        /// </summary>
        public void Load()
        {
            // TODO CS-04: read JSON from disk, deserialize, restore state via GameManager
            Debug.Log("[SaveSystem] Load() — TODO CS-04");
            Core.EventBus.Publish(new Core.OnGameLoadedEvent(string.Empty));
        }
    }
}
