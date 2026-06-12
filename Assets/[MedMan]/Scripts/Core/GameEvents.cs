using MedMan.Data;
using MedMan.Narrative;
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
        public readonly FearType      FearType;
        public readonly DreamLevel    DreamLevel;
        public readonly string        StateID;
        public readonly FearProfileSO Profile;

        public OnLevelInitializedEvent(FearType fearType, DreamLevel dreamLevel, string stateID, FearProfileSO profile)
        {
            FearType   = fearType;
            DreamLevel = dreamLevel;
            StateID    = stateID;
            Profile    = profile;
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
        public readonly int PillsRemaining;
        public OnPillConsumedEvent(int pillsRemaining) => PillsRemaining = pillsRemaining;
    }

    /// <summary>
    /// Published when an active pill effect wears off.
    /// FearAManager reacts by gradually restoring the base dream visual state.
    /// </summary>
    public readonly struct OnPillExpiredEvent { }

    /// <summary>
    /// Published when the player attempts to consume a pill but has none remaining.
    /// Used to trigger feedback — protagonist line, audio cue, or visual hint.
    /// </summary>
    public readonly struct OnPillDepletedEvent { }

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

    /// <summary>Published when the player picks up an object.</summary>
    public readonly struct OnPickupCollectedEvent
    {
        public readonly PickupType PickupType;
        public readonly int Amount;

        public OnPickupCollectedEvent(PickupType pickupType, int amount)
        {
            PickupType = pickupType;
            Amount     = amount;
        }
    }

    /// <summary>Published when the player presses the take input during a pickup interaction.</summary>
    public readonly struct OnTakeInputEvent { }

    /// <summary>Published to show or hide the take prompt UI.</summary>
    public readonly struct OnPickupPromptShownEvent
    {
        public readonly bool IsVisible;
        public OnPickupPromptShownEvent(bool isVisible) => IsVisible = isVisible;
    }

    #endregion

    #region Player Events

    /// <summary>
    /// Published when the player remains stationary for the idle threshold duration.
    /// IdleMonologueSystem reacts by triggering a location-specific idle monologue line.
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

    /// <summary>
    /// Published when the player resumes movement after an idle state.
    /// IdleMonologueSystem reacts by cancelling any pending idle coroutine.
    /// </summary>
    public readonly struct OnPlayerMovedEvent { }

    /// <summary>Published to enable or disable player movement control.</summary>
    public readonly struct OnPlayerControlChangedEvent
    {
        public readonly bool IsEnabled;
        public OnPlayerControlChangedEvent(bool isEnabled) => IsEnabled = isEnabled;
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

    #region Interaction Events

    /// <summary>Published when the player's raycast lands on or leaves an IInteractable.</summary>
    public readonly struct OnInteractableHoveredEvent
    {
        public readonly InteractionType Type;
        public OnInteractableHoveredEvent(InteractionType type) => Type = type;
    }

    /// <summary>Published when the player begins an interaction.</summary>
    public readonly struct OnInteractionStartedEvent
    {
        public readonly InteractionType Type;
        public OnInteractionStartedEvent(InteractionType type) => Type = type;
    }

    /// <summary>Published when an interaction ends — either by cancel input or force-end.</summary>
    public readonly struct OnInteractionEndedEvent
    {
        public readonly InteractionType Type;
        public OnInteractionEndedEvent(InteractionType type) => Type = type;
    }

    /// <summary>Published when the player begins an interaction requiring a camera view point.</summary>
    public readonly struct OnInteractionViewRequestedEvent
    {
        public readonly Transform ViewPoint;
        public OnInteractionViewRequestedEvent(Transform viewPoint) => ViewPoint = viewPoint;
    }

    /// <summary>Published when the player exits an interaction and the camera should return to the player.</summary>
    public readonly struct OnInteractionViewExitedEvent { }

    #endregion

    #region Narrative Events

    /// <summary>Published when a dialogue line begins displaying.</summary>
    public readonly struct OnDialogueLineStartedEvent
    {
        public readonly DialogueLineSO Line;
        public OnDialogueLineStartedEvent(DialogueLineSO line) => Line = line;
    }

    /// <summary>Published when a dialogue line finishes displaying.</summary>
    public readonly struct OnDialogueLineEndedEvent
    {
        public readonly DialogueLineSO Line;
        public OnDialogueLineEndedEvent(DialogueLineSO line) => Line = line;
    }

    /// <summary>Published when the entire dialogue sequence finishes.</summary>
    public readonly struct OnDialogueSequenceEndedEvent { }

    /// <summary>
    /// Published when the player enters a named narrative zone trigger.
    /// IdleMonologueSystem reacts by switching to the zone-specific line pool.
    /// </summary>
    public readonly struct OnNarrativeZoneEnteredEvent
    {
        /// <summary>Unique identifier of the zone. Must match a ZoneMonologue.zoneId in IdleMonologueSystem.</summary>
        public readonly string ZoneId;

        public OnNarrativeZoneEnteredEvent(string zoneId)
        {
            ZoneId = zoneId;
        }
    }

    /// <summary>
    /// Published when the player exits a narrative zone with no replacement zone active.
    /// IdleMonologueSystem reacts by clearing the current zone.
    /// </summary>
    public readonly struct OnNarrativeZoneExitedEvent
    {
        public readonly string ZoneId;

        public OnNarrativeZoneExitedEvent(string zoneId)
        {
            ZoneId = zoneId;
        }
    }

    /// <summary>
    /// Published when IdleMonologueSystem plays a location-specific idle line.
    /// DialogueSystem or AudioManager can react by displaying or vocalising the line.
    /// </summary>
    public readonly struct OnIdleMonologuePlayedEvent
    {
        /// <summary>The text content of the line that was played.</summary>
        public readonly string Line;

        /// <summary>The zone in which the line was triggered.</summary>
        public readonly string ZoneId;

        public OnIdleMonologuePlayedEvent(string line, string zoneId)
        {
            Line   = line;
            ZoneId = zoneId;
        }
    }
    
    /// <summary>Published when a NarrativeSequencer finishes all its steps.</summary>
    public readonly struct OnNarrativeSequenceEndedEvent
    {
        public readonly NarrativeTextController Controller;
        public OnNarrativeSequenceEndedEvent(NarrativeTextController controller) => Controller = controller;
    }

    #endregion
}
