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
        
        private const float Gravity = -9.81f;
        private const float MoveDeadzoneSqr = 0.01f; // (~0.1 input magnitude)²

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private Vector2 _moveInput;
        private float _idleTimer;
        private float _verticalVelocity;
        private bool _isControlEnabled = true;
        private bool _idleEventFired;
        private bool _isSprinting;

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
        /// Horizontal input is clamped to magnitude 1 (no diagonal speed boost).
        /// Gravity is accumulated into a separate vertical velocity and applied once —
        /// CharacterController does not apply gravity itself. A small downward bias
        /// while grounded keeps the controller pinned to the floor.
        /// </summary>
        private void HandleMovement()
        {
            float speed = _isSprinting ? _sprintSpeed : _walkSpeed;

            Vector3 horizontal = new Vector3(_moveInput.x, 0f, _moveInput.y);
            horizontal = Vector3.ClampMagnitude(horizontal, 1f);
            horizontal = transform.TransformDirection(horizontal) * speed;

            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -1f;
            else
                _verticalVelocity += Gravity * Time.deltaTime;

            Vector3 velocity = horizontal;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * Time.deltaTime);
        }

        /// <summary>
        /// Tracks how long the player has been stationary.
        /// Publishes OnPlayerIdleEvent once when idle threshold is reached.
        /// Timer and flag reset when the player moves again.
        /// </summary>
        private void HandleIdleDetection()
        {
            // Deadzone compare instead of exact zero — analog sticks rarely return Vector2.zero
            if (_moveInput.sqrMagnitude > MoveDeadzoneSqr)
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