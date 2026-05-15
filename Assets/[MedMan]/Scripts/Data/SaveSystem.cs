using System.Collections.Generic;
using UnityEngine;

namespace MedMan.Data
{
    /// <summary>
    /// Save and load system. Self-contained — knows what to save and where to fetch data from.
    /// GameManager calls only Save() and Load(). No parameters, no external dependencies.
    ///
    /// Data is collected via CollectSaveData() which assembles a SaveData object
    /// from all source systems at save time. Each system remains the single source of truth
    /// for its own data.
    /// Implementation: CS-04.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }
        
        /// <summary>
        /// Absolute path to the save file on disk.
        /// Uses Application.persistentDataPath — writable on all platforms.
        /// </summary>
        private static readonly string SaveFilePath =
            System.IO.Path.Combine(Application.persistentDataPath, "medman_save.json");

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

        /// <summary>
        /// Triggers autosave on every game state transition except None.
        /// </summary>
        private void HandleGameStateChanged(Core.OnGameStateChangedEvent e)
        {
            if (e.NewState != Core.GameState.None)
                Save();
        }

        // ─────────────────────────────────────────
        // Data collection
        // ─────────────────────────────────────────
        
        /// <summary>
        /// Collects current game state from all source systems into a single SaveData object.
        /// Each system remains the single source of truth for its own data.
        /// </summary>
        private SaveData CollectSaveData()
        {
            return new SaveData
            {
                CurrentGameState  = Core.GameManager.Instance?.CurrentGameState  ?? Core.GameState.None,
                SelectedFear      = Core.GameManager.Instance?.CurrentFearType   ?? Core.FearType.None,
                CurrentDreamLevel = Core.GameManager.Instance?.CurrentDreamLevel ?? Core.DreamLevel.None,
                PillsRemaining    = 0, // TODO CS-10: PillSystem.Instance?.PillsRemaining ?? 0
                PillsConsumedTotal = 0, // TODO CS-10: PillSystem.Instance?.PillsConsumedTotal ?? 0
                UnlockedSkills    = new List<Core.SkillID>(),    // TODO: SkillSystem
                PathChoices       = new List<Core.PathChoice>()  // TODO: ChoiceManager
            };
        }

        // ─────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────

        /// <summary>
        /// Collects current game state via CollectSaveData(), serializes to JSON
        /// and writes to disk at SaveFilePath.
        /// Implementation: CS-04.
        /// </summary>
        public void Save()
        {
            SaveData data = CollectSaveData();
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            System.IO.File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[SaveSystem] Game saved to: {SaveFilePath}");
            Core.EventBus.Publish(new Core.OnGameSavedEvent(Core.GameManager.Instance?.CurrentStateID ?? string.Empty));
        }

        /// <summary>
        /// Reads save data from disk and restores game state.
        /// Implementation: CS-04.
        /// </summary>
        public void Load()
        {
            if (!System.IO.File.Exists(SaveFilePath))
            {
                Debug.Log("[SaveSystem] No save file found — starting fresh.");
                return;
            }

            string json = System.IO.File.ReadAllText(SaveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // TODO CS-04: restore state via GameManager
            // Core.GameManager.Instance?.TransitionTo(data.CurrentGameState, data.SelectedFear, data.CurrentDreamLevel);

            Debug.Log($"[SaveSystem] Game loaded from: {SaveFilePath}");
            Core.EventBus.Publish(new Core.OnGameLoadedEvent(data.CurrentGameState.ToString()));
        }
    }
}
