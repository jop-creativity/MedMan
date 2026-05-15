using System.Collections.Generic;

namespace MedMan.Data
{
    /// <summary>
    /// Container for all serializable game data.
    /// Serialized to JSON and written to disk by SaveSystem.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        // ─────────────────────────────────────────
        // Game State
        // ─────────────────────────────────────────

        public Core.GameState  CurrentGameState  = Core.GameState.None;
        public Core.FearType   SelectedFear      = Core.FearType.None;
        public Core.DreamLevel CurrentDreamLevel = Core.DreamLevel.None;

        // ─────────────────────────────────────────
        // Player Progress
        // ─────────────────────────────────────────

        /// <summary>Skills unlocked on hard paths. Persist across all levels.</summary>
        public List<Core.SkillID> UnlockedSkills = new List<Core.SkillID>();

        /// <summary>Path choice made at each dream level. Index 0 = Level1, 1 = Level2, 2 = Level3.</summary>
        public List<Core.PathChoice> PathChoices = new List<Core.PathChoice>();

        // ─────────────────────────────────────────
        // Pill State
        // ─────────────────────────────────────────

        public int   PillsConsumedTotal = 0;
        public int   PillsRemaining     = 0;
    }
}