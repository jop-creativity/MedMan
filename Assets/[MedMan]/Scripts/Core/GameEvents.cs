using UnityEngine;

namespace MedMan.Core
{
    #region Game State Events

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
        /// Always in format: "GameState_FearType_DreamLevel"
        /// Examples: "HotelRoom_None_None", "Dream_Fear_A_Level1", "Epilogue_None_None"
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

    #endregion

    #region Dream Level Events

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

    #endregion

    #region Loading Barrier Events

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

    #endregion

    #region Fear Selection Events

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

    #endregion

    #region Pill Events

    /// <summary>
    /// Published when the player consumes a pill.
    /// FearAManager reacts by triggering the fear-specific visual effect (e.g. light surge).
    /// SaveSystem records pills consumed.
    /// </summary>
    public readonly struct OnPillConsumedEvent
    {
        /// <summary>Number of pills remaining after this consumption.</summary>
        public readonly int PillsRemaining;

        public OnPillConsumedEvent(int pillsRemaining)
        {
            PillsRemaining = pillsRemaining;
        }
    }

    /// <summary>
    /// Published when an active pill effect wears off.
    /// FearAManager reacts by gradually restoring the base dream visual state.
    /// </summary>
    public readonly struct OnPillExpiredEvent
    {
        /// <summary>Number of pills remaining at expiry.</summary>
        public readonly int PillsRemaining;

        public OnPillExpiredEvent(int pillsRemaining)
        {
            PillsRemaining = pillsRemaining;
        }
    }

    #endregion

    #region Checkpoint Events

    /// <summary>
    /// Published when the player reaches an autosave checkpoint.
    /// SaveSystem reacts by writing current game state to disk.
    /// </summary>
    public readonly struct OnCheckpointReachedEvent
    {
        /// <summary>Unique identifier of the checkpoint within the current level.</summary>
        public readonly string CheckpointID;

        public OnCheckpointReachedEvent(string checkpointID)
        {
            CheckpointID = checkpointID;
        }
    }

    #endregion

    #region Skill Events

    /// <summary>
    /// Published when the player unlocks a traversal skill on a hard path.
    /// SaveSystem records the unlocked skill so it persists across levels.
    /// </summary>
    public readonly struct OnSkillUnlockedEvent
    {
        /// <summary>Identifier of the unlocked skill (e.g. "ObjectRotation", "Swimming").</summary>
        public readonly string SkillID;

        public OnSkillUnlockedEvent(string skillID)
        {
            SkillID = skillID;
        }
    }

    #endregion

    #region Player Events

    /// <summary>
    /// Published when the player remains stationary for the idle threshold duration.
    /// NarrativeManager reacts by triggering a location-specific idle monologue.
    /// </summary>
    public readonly struct OnPlayerIdleEvent
    {
        /// <summary>Duration in seconds the player has been idle.</summary>
        public readonly float IdleDuration;

        public OnPlayerIdleEvent(float idleDuration)
        {
            IdleDuration = idleDuration;
        }
    }

    #endregion

    #region Save / Load Events

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

    #endregion
    
    #region Scene Load Events
     
        /// <summary>
        /// Published by SceneLoader when an async scene load begins.
        /// Systems can react by pausing logic, hiding UI, etc.
        /// </summary>
        public readonly struct OnSceneLoadStartedEvent
        {
            /// <summary>Name of the scene being loaded.</summary>
            public readonly string SceneName;
     
            public OnSceneLoadStartedEvent(string sceneName)
            {
                SceneName = sceneName;
            }
        }
     
        /// <summary>
        /// Published by SceneLoader when a scene has fully loaded and the fade-in is complete.
        /// Systems can react by resuming logic, showing UI, etc.
        /// </summary>
        public readonly struct OnSceneLoadCompletedEvent
        {
            /// <summary>Name of the scene that finished loading.</summary>
            public readonly string SceneName;
     
            public OnSceneLoadCompletedEvent(string sceneName)
            {
                SceneName = sceneName;
            }
        }
     
        #endregion
        
    #region Camera Events

    /// <summary>
    /// Published when a narrative trigger requests camera assist toward a world-space target.
    /// CameraController gently guides view toward the target without hard-locking input.
    /// </summary>
    public readonly struct OnCameraAssistRequestedEvent
    {
        /// <summary>World-space position the camera should be guided toward.</summary>
        public readonly Vector3 TargetPosition;

        /// <summary>Strength of the assist. Range 0-1. Higher = faster guidance.</summary>
        public readonly float AssistStrength;

        public OnCameraAssistRequestedEvent(Vector3 targetPosition, float assistStrength)
        {
            TargetPosition = targetPosition;
            AssistStrength = assistStrength;
        }
    }

    /// <summary>
    /// Published when an interaction begins and camera rotation should be constrained.
    /// CameraController limits how far the player can look away from the interaction point.
    /// </summary>
    public readonly struct OnCameraLockRequestedEvent
    {
        /// <summary>Maximum horizontal rotation in degrees from current facing direction.</summary>
        public readonly float MaxHorizontalAngle;

        /// <summary>Maximum vertical rotation in degrees from current facing direction.</summary>
        public readonly float MaxVerticalAngle;

        public OnCameraLockRequestedEvent(float maxHorizontalAngle, float maxVerticalAngle)
        {
            MaxHorizontalAngle = maxHorizontalAngle;
            MaxVerticalAngle   = maxVerticalAngle;
        }
    }

    /// <summary>
    /// Published when an interaction ends and camera rotation constraints should be released.
    /// </summary>
    public readonly struct OnCameraReleaseRequestedEvent { }

    #endregion
}
