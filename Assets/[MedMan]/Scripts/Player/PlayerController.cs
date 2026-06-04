using UnityEngine;
using UnityEngine.InputSystem;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Player
{
    /// <summary>
    /// First-person player controller. Implements IControllable —
    /// control is enabled/disabled by GameplayManager.
    /// Handles movement (walk + sprint) and idle detection only.
    /// Mouse look is handled by CameraController.
    /// Uses direct InputAction.ReadValue each frame — avoids Input System callback jitter.
    /// Publishes OnPlayerIdleEvent via EventBus when idle threshold is reached.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, IControllable
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Movement")]
        [SerializeField] private float _walkSpeed = 2.5f;

        [BoxGroup("Movement")]
        [SerializeField] private float _sprintSpeed = 5f;

        [BoxGroup("Idle Detection")]
        [SerializeField] private float _idleThreshold = 30f;

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private Vector2 _moveInput;
        private bool _isSprinting;
        private float _idleTimer;
        private bool _isControlEnabled = true;
        private bool _idleEventFired;

        // ─────────────────────────────────────────
        // Properties — IControllable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public bool IsControlEnabled => _isControlEnabled;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        /// <summary>
        /// Initializes CharacterController and binds Move and Sprint actions directly.
        /// Direct InputAction binding avoids Input System callback jitter.
        /// Cursor lock is handled by CameraController.
        /// </summary>
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _moveAction = InputSystem.actions.FindAction("Player/Move");
            _sprintAction = InputSystem.actions.FindAction("Player/Sprint");
            _moveAction?.Enable();
            _sprintAction?.Enable();
        }
        
        /// <summary>
        /// Subscribes to player control change events.
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayerControlChangedEvent>(HandleControlChanged);
        }
        
        /// <summary>
        /// Unsubscribes from player control change events.
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayerControlChangedEvent>(HandleControlChanged);
        }

        private void OnDestroy()
        {
            _moveAction?.Disable();
            _sprintAction?.Disable();
        }

        /// <summary>
        /// Reads move and sprint input, handles movement and idle detection each frame when control is enabled.
        /// </summary>
        private void Update()
        {
            if (!_isControlEnabled) return;

            _moveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            _isSprinting = _sprintAction?.IsPressed() ?? false;

            HandleMovement();
            HandleIdleDetection();
        }

        // ─────────────────────────────────────────
        // Public methods — IControllable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public void EnableControl()
        {
            _isControlEnabled = true;
            Debug.Log("[PlayerController] Control enabled.");
        }

        /// <inheritdoc/>
        /// <summary>
        /// Disables player input and resets move input to prevent
        /// continued movement after control is taken away.
        /// </summary>
        public void DisableControl()
        {
            _isControlEnabled = false;
            _moveInput = Vector2.zero;
            Debug.Log("[PlayerController] Control disabled.");
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Moves the player based on current input and sprint state.
        /// Clamps diagonal movement to magnitude 1.
        /// Applies gravity manually as CharacterController does not include it.
        /// </summary>
        private void HandleMovement()
        {
            float speed = _isSprinting ? _sprintSpeed : _walkSpeed;

            Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);
            move = Vector3.ClampMagnitude(move, 1f);
            move = transform.TransformDirection(move) * speed;
            move.y -= 9.81f * Time.deltaTime;

            _characterController.Move(move * Time.deltaTime);
        }

        /// <summary>
        /// Tracks how long the player has been stationary.
        /// Publishes OnPlayerIdleEvent once when idle threshold is reached.
        /// Timer and flag reset when the player moves again.
        /// </summary>
        private void HandleIdleDetection()
        {
            if (_moveInput != Vector2.zero)
            {
                if (_idleEventFired)
                    EventBus.Publish(new OnPlayerMovedEvent());

                _idleTimer      = 0f;
                _idleEventFired = false;
                return;
            }

            _idleTimer += Time.deltaTime;

            if (_idleTimer >= _idleThreshold && !_idleEventFired)
            {
                _idleEventFired = true;
                EventBus.Publish(new OnPlayerIdleEvent(_idleTimer));
                Debug.Log($"[PlayerController] Player idle for {_idleTimer:F1}s — event published.");
            }
        }
        
        /// <summary>
        /// Enables or disables player control in response to game systems
        /// that need to take movement away from the player (e.g. interaction mode, cutscenes).
        /// </summary>
        private void HandleControlChanged(OnPlayerControlChangedEvent e)
        {
            if (e.IsEnabled) EnableControl();
            else DisableControl();
        }
    }
}