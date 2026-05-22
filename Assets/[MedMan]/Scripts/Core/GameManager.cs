using System.Collections.Generic;
using UnityEngine;
using MedMan.Data;
using MedMan.Gameplay;
using NaughtyAttributes;

namespace MedMan.Core
{
    /// <summary>
    /// Central game conductor. Manages game state and state transitions only.
    /// Does not touch gameplay logic or any specific system directly —
    /// broadcasts information via EventBus, other systems react on their own.
    ///
    /// Manager hierarchy:
    ///   GameManager          — game state (this file)
    ///   AudioManager         — audio
    ///   SaveSystem           — save / load
    ///   SceneLoader          — scene loading
    ///   VisualManager        — visual effects, Volume Profiles, Kuwahara
    ///   NarrativeManager     — dialogue, world-space text, monologues
    ///   GameplayManager      — universal mechanics (movement, interactions, choices)
    ///   DreamGameplayManager — Dream-specific mechanics (Pills, FearXManagers)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Singleton
        // ─────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SubscribeToEvents();
            Debug.Log("[GameManager] Initialized.");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #if UNITY_EDITOR
        [BoxGroup("Debug")]
        [SerializeField] private FearProfileSO _debugFearProfile;

        private void Start()
        {
            if (_debugFearProfile != null)
            {
                CurrentFearProfile = _debugFearProfile;
                CurrentFearType    = _debugFearProfile.FearType;
                CurrentDreamLevel  = _debugFearProfile.DreamLevel;
        
                // Initialize PillSystem directly for debug purposes
                var pillSystem = FindObjectOfType<PillSystem>();
                pillSystem?.Initialize(_debugFearProfile);
        
                Debug.Log($"[GameManager] Debug profile loaded: {_debugFearProfile.StateIDReadable}");
            }
        }
        #endif

        // ─────────────────────────────────────────
        // Game state — read-only from outside
        // ─────────────────────────────────────────

        /// <summary>Current top-level game state.</summary>
        public GameState CurrentGameState { get; private set; } = GameState.None;

        /// <summary>Active fear type. None outside the Dream section.</summary>
        public FearType CurrentFearType { get; private set; } = FearType.None;

        /// <summary>Active dream level. None outside the Dream section.</summary>
        public DreamLevel CurrentDreamLevel { get; private set; } = DreamLevel.None;
        
        /// <summary>Active fear profile for the current dream level.</summary>
        public FearProfileSO CurrentFearProfile { get; private set; }

        /// <summary>
        /// Unique identifier of the current state.
        /// Always in format: "GameState_FearType_DreamLevel"
        /// Examples: "HotelRoom_None_None", "Dream_Fear_A_Level1", "Epilogue_None_None"
        /// Used as a key for FearProfileSO and configuration packages.
        /// </summary>
        public string CurrentStateID => $"{CurrentGameState}_{CurrentFearType}_{CurrentDreamLevel}";

        // ─────────────────────────────────────────
        // Event subscriptions
        // ─────────────────────────────────────────

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<OnSystemReadyEvent>(HandleSystemReady);
            EventBus.Subscribe<OnFearSelectedEvent>(HandleFearSelected);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<OnSystemReadyEvent>(HandleSystemReady);
            EventBus.Unsubscribe<OnFearSelectedEvent>(HandleFearSelected);
        }

        // ─────────────────────────────────────────
        // Public API — state transitions
        // ─────────────────────────────────────────

        /// <summary>
        /// Transitions to a new game state.
        /// For non-Dream states pass FearType.None and DreamLevel.None.
        /// Example: TransitionTo(GameState.HotelRoom, FearType.None, DreamLevel.None)
        /// Example: TransitionTo(GameState.Dream, FearType.Fear_A, DreamLevel.Level1)
        /// </summary>
        public void TransitionTo(GameState newState, FearType fearType, DreamLevel dreamLevel)
        {
            SetState(newState, fearType, dreamLevel);
        }

        /// <summary>Triggers a manual save. SaveSystem knows what to save.</summary>
        public void RequestSave() => SaveSystem.Instance?.Save();

        /// <summary>Triggers a load. SaveSystem knows what to restore.</summary>
        public void RequestLoad() => SaveSystem.Instance?.Load();

        // ─────────────────────────────────────────
        // Internal state transition logic
        // ─────────────────────────────────────────

        private void SetState(GameState newState, FearType fearType, DreamLevel dreamLevel)
        {
            GameState previousState = CurrentGameState;

            CurrentGameState  = newState;
            CurrentFearType   = fearType;
            CurrentDreamLevel = dreamLevel;

            Debug.Log($"[GameManager] State: {previousState} → {CurrentStateID}");

            // Notify all systems of the state change
            EventBus.Publish(new OnGameStateChangedEvent(
                previousState,
                CurrentGameState,
                CurrentFearType,
                CurrentDreamLevel,
                CurrentStateID
            ));

            // Dream requires a loading barrier — the level is revealed only after
            // all systems confirm readiness
            if (newState == GameState.Dream)
                StartLoadingBarrier();
            else
                EventBus.Publish(new OnLevelReadyToPlayEvent(CurrentStateID));

            // Autosave on every state change
            SaveSystem.Instance?.Save();
        }

        // ─────────────────────────────────────────
        // Loading Barrier
        // ─────────────────────────────────────────

        private readonly HashSet<string> _pendingSystems = new HashSet<string>();

        private void StartLoadingBarrier()
        {
            _pendingSystems.Clear();

            // Register systems that must confirm readiness before the level is revealed.
            // Each system must publish OnSystemReadyEvent with exactly this name.
            _pendingSystems.Add("AudioManager");
            _pendingSystems.Add("VisualManager");
            _pendingSystems.Add("NarrativeManager");
            _pendingSystems.Add("DreamGameplayManager");

            Debug.Log($"[GameManager] Loading barrier active for: {CurrentStateID}. Waiting for {_pendingSystems.Count} systems.");

            // Broadcast the initialization package — systems begin configuring themselves
            EventBus.Publish(new OnLevelInitializedEvent(
                CurrentFearType,
                CurrentDreamLevel,
                CurrentStateID,
                CurrentFearProfile
            ));
        }

        private void HandleSystemReady(OnSystemReadyEvent e)
        {
            if (!_pendingSystems.Contains(e.SystemName))
                return;

            _pendingSystems.Remove(e.SystemName);
            Debug.Log($"[GameManager] System ready: {e.SystemName}. Remaining: {_pendingSystems.Count}");

            if (_pendingSystems.Count == 0)
            {
                Debug.Log($"[GameManager] All systems ready. Revealing level: {CurrentStateID}");
                EventBus.Publish(new OnLevelReadyToPlayEvent(CurrentStateID));
            }
        }

        // ─────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────

        private void HandleFearSelected(OnFearSelectedEvent e)
        {
            CurrentFearType = e.SelectedFear;
            Debug.Log($"[GameManager] Fear selected: {CurrentFearType}");
        }
    }
}
