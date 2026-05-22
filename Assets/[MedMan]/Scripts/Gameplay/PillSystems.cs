using UnityEngine;
using UnityEngine.InputSystem;
using MedMan.Core;
using MedMan.Data;
using NaughtyAttributes;

namespace MedMan.Gameplay
{
    /// <summary>
    /// Manages the player's pill resource for a single dream level.
    /// Reads pill count and duration from FearProfileSO loaded at level start.
    /// Handles consume input, timed effect expiry, and publishes events via EventBus.
    /// Sits as a component under DreamGameplayManager.
    /// Enabled only during Dream game state — controlled by DreamGameplayManager.
    /// </summary>
    public class PillSystem : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Debug")]
        [ReadOnly]
        [SerializeField] private int _pillsRemaining;

        [BoxGroup("Debug")]
        [ReadOnly]
        [SerializeField] private float _effectTimer;

        [BoxGroup("Debug")]
        [ReadOnly]
        [SerializeField] private bool _isEffectActive;

        private InputAction _consumeAction;
        private float _pillDuration;
        private int _pillsConsumedTotal;

        // ─────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────

        /// <summary>Total pills consumed across this level — read by SaveSystem.</summary>
        public int PillsConsumed => _pillsConsumedTotal;

        /// <summary>Whether a pill effect is currently active.</summary>
        public bool IsEffectActive => _isEffectActive;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        /// <summary>
        /// Binds consume input action.
        /// </summary>
        private void Awake()
        {
            _consumeAction = InputSystem.actions.FindAction("Player/ConsumePill");
            _consumeAction?.Enable();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnLevelInitializedEvent>(HandleLevelInitialized);
            EventBus.Subscribe<OnPickupCollectedEvent>(HandlePickupCollected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnLevelInitializedEvent>(HandleLevelInitialized);
            EventBus.Unsubscribe<OnPickupCollectedEvent>(HandlePickupCollected);
        }

        private void OnDestroy()
        {
            _consumeAction?.Disable();
        }

        /// <summary>
        /// Checks consume input and ticks the active effect timer each frame.
        /// </summary>
        private void Update()
        {
            HandleConsumeInput();
            HandleEffectTimer();
        }

        // ─────────────────────────────────────────
        // Public methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Initializes pill count and duration from the provided FearProfileSO.
        /// Resets all state — call at the start of each dream level.
        /// </summary>
        public void Initialize(FearProfileSO profile)
        {
            _pillsRemaining    = profile.StartPillsCount;
            _pillDuration      = profile.PillDuration;
            _pillsConsumedTotal = 0;
            _effectTimer       = 0f;
            _isEffectActive    = false;

            Debug.Log($"[PillSystem] Initialized — pills: {_pillsRemaining}, duration: {_pillDuration}s");
        }

        /// <summary>
        /// Consumes one pill if available and effect is not already active.
        /// Publishes OnPillConsumedEvent or OnPillDepletedEvent.
        /// </summary>
        public void Consume()
        {
            if (_isEffectActive)
            {
                Debug.Log("[PillSystem] Pill effect already active — consume ignored.");
                return;
            }

            if (_pillsRemaining <= 0)
            {
                EventBus.Publish(new OnPillDepletedEvent());
                Debug.Log("[PillSystem] No pills remaining.");
                return;
            }

            _pillsRemaining--;
            _pillsConsumedTotal++;
            _effectTimer    = _pillDuration;
            _isEffectActive = true;

            EventBus.Publish(new OnPillConsumedEvent(_pillsRemaining));
            Debug.Log($"[PillSystem] Pill consumed — {_pillsRemaining} remaining, effect active for {_pillDuration}s");
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Reads consume input and calls Consume() when pressed.
        /// </summary>
        private void HandleConsumeInput()
        {
            if (_consumeAction?.WasPressedThisFrame() ?? false)
                Consume();
        }

        /// <summary>
        /// Ticks the effect timer while active.
        /// Publishes OnPillExpiredEvent when the timer reaches zero.
        /// </summary>
        private void HandleEffectTimer()
        {
            if (!_isEffectActive) return;

            _effectTimer -= Time.deltaTime;

            if (_effectTimer <= 0f)
            {
                _effectTimer    = 0f;
                _isEffectActive = false;

                EventBus.Publish(new OnPillExpiredEvent());
                Debug.Log("[PillSystem] Pill effect expired.");
            }
        }

        /// <summary>
        /// Loads FearProfileSO from the level initialized event and calls Initialize.
        /// </summary>
        private void HandleLevelInitialized(OnLevelInitializedEvent e)
        {
            if (e.Profile != null)
                Initialize(e.Profile);
        }
        
        /// <summary>
        /// Adds pills to the counter when a pill pickup is collected.
        /// </summary>
        private void HandlePickupCollected(OnPickupCollectedEvent e)
        {
            if (e.PickupType != PickupType.Pills) return;

            _pillsRemaining += e.Amount;
            Debug.Log($"[PillSystem] Pills collected: +{e.Amount} — total: {_pillsRemaining}");
        }
    }
}