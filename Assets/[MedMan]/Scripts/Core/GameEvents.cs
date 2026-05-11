namespace MedMan.Core
{
    // ═══════════════════════════════════════════════════════════
    // GAME STATE EVENTS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Published when GameManager changes the game state.
    /// All systems (Audio, Visual, Narrative, etc.) react to this event
    /// and configure themselves according to the new StateID.
    /// </summary>
    public readonly struct OnGameStateChangedEvent
    {
        public readonly GameState  PreviousState;
        public readonly GameState  NewState;
        public readonly FearType   FearType;
        public readonly DreamLevel DreamLevel;

        /// <summary>
        /// Unique identifier of the current state.
        /// Outside Dream: "HotelRoom", "Epilogue", etc.
        /// Inside Dream: "Fear_A_Level1", "Fear_B_Level3", etc.
        /// Used as a key to look up FearProfileSO and configuration packages.
        /// </summary>
        public readonly string StateID;

        public OnGameStateChangedEvent(
            GameState previousState,
            GameState newState,
            FearType fearType,
            DreamLevel dreamLevel,
            string stateID)
        {
            PreviousState = previousState;
            NewState      = newState;
            FearType      = fearType;
            DreamLevel    = dreamLevel;
            StateID       = stateID;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DREAM LEVEL EVENTS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Published by GameManager when a Dream level is initializing.
    /// DreamGameplayManager receives this and loads the appropriate FearXManager.
    /// All systems configure themselves based on StateID.
    /// </summary>
    public readonly struct OnLevelInitializedEvent
    {
        public readonly FearType   FearType;
        public readonly DreamLevel DreamLevel;
        public readonly string     StateID;

        public OnLevelInitializedEvent(FearType fearType, DreamLevel dreamLevel, string stateID)
        {
            FearType   = fearType;
            DreamLevel = dreamLevel;
            StateID    = stateID;
        }
    }

    /// <summary>
    /// Published when all systems have confirmed readiness (loading barrier cleared).
    /// SceneLoader reveals the level to the player only after receiving this event.
    /// </summary>
    public readonly struct OnLevelReadyToPlayEvent
    {
        public readonly string StateID;

        public OnLevelReadyToPlayEvent(string stateID)
        {
            StateID = stateID;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // LOADING BARRIER EVENTS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Published by each system after it finishes configuring for a new level.
    /// GameManager counts confirmations — when all arrive, publishes OnLevelReadyToPlayEvent.
    /// SystemName must exactly match the name registered in the loading barrier.
    /// </summary>
    public readonly struct OnSystemReadyEvent
    {
        public readonly string SystemName;

        public OnSystemReadyEvent(string systemName)
        {
            SystemName = systemName;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // FEAR SELECTION EVENTS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Published when the player selects a fear at the doctor's office.
    /// GameManager updates CurrentFearType.
    /// SaveSystem records the choice.
    /// </summary>
    public readonly struct OnFearSelectedEvent
    {
        public readonly FearType SelectedFear;

        public OnFearSelectedEvent(FearType selectedFear)
        {
            SelectedFear = selectedFear;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // SAVE / LOAD EVENTS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Published when SaveSystem finishes writing to disk.</summary>
    public readonly struct OnGameSavedEvent
    {
        public readonly string SavedStateID;

        public OnGameSavedEvent(string savedStateID)
        {
            SavedStateID = savedStateID;
        }
    }

    /// <summary>Published when SaveSystem finishes reading from disk.</summary>
    public readonly struct OnGameLoadedEvent
    {
        public readonly string LoadedStateID;

        public OnGameLoadedEvent(string loadedStateID)
        {
            LoadedStateID = loadedStateID;
        }
    }
}
