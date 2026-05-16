using UnityEngine;
using UnityEngine.InputSystem;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Player
{
    /// <summary>
    /// First-person player controller. Implements IControllable — 
    /// control is enabled/disabled by GameplayManager.
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

        [BoxGroup("Idle Detection")]
        [SerializeField] private float _idleThreshold = 30f;
        
        [BoxGroup("Look")]
        [SerializeField] private float _mouseSensitivity = 2f;

        private Transform _cameraTransform;
        private CharacterController _characterController;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _idleTimer;
        private float _verticalRotation;
        private bool _isControlEnabled = true;
        private bool _idleEventFired;

        // ─────────────────────────────────────────
        // IControllable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public bool IsControlEnabled => _isControlEnabled;

        /// <inheritdoc/>
        public void EnableControl()
        {
            _isControlEnabled = true;
            Debug.Log("[PlayerController] Control enabled.");
        }

        /// <inheritdoc/>
        public void DisableControl()
        {
            _isControlEnabled = false;
            _moveInput = Vector2.zero;
            Debug.Log("[PlayerController] Control disabled.");
        }

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        /// <summary>
        /// Initializes CharacterController, camera transform reference and locks cursor.
        /// </summary>
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _cameraTransform = Camera.main.transform;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>
        /// Handles movement and look each frame when control is enabled.
        /// </summary>
        private void Update()
        {
            if (!_isControlEnabled) return;

            HandleMovement();
            HandleLook();
            HandleIdleDetection();
        }

        // ─────────────────────────────────────────
        // Movement
        // ─────────────────────────────────────────

        private void HandleMovement()
        {
            Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);
            move = transform.TransformDirection(move) * _walkSpeed;

            // Apply gravity
            move.y -= 9.81f * Time.deltaTime;

            _characterController.Move(move * Time.deltaTime);
        }
        
        /// <summary>
        /// Handles mouse look. Rotates player on Y axis (horizontal)
        /// and camera on X axis (vertical) with clamping to prevent neck-breaking.
        /// </summary>
        private void HandleLook()
        {
            _cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
            transform.Rotate(Vector3.up * _lookInput.x * _mouseSensitivity);
    
            _verticalRotation -= _lookInput.y * _mouseSensitivity;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -90f, 90f);
        }

        // ─────────────────────────────────────────
        // Idle detection
        // ─────────────────────────────────────────

        private void HandleIdleDetection()
        {
            if (_moveInput != Vector2.zero)
            {
                _idleTimer = 0f;
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

        // ─────────────────────────────────────────
        // Input System callbacks
        // ─────────────────────────────────────────

        /// <summary>
        /// Called by Unity Input System when Move action is performed or cancelled.
        /// Bind this in the PlayerInput component on the same GameObject.
        /// </summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }
        
        /// <summary>
        /// Called by Unity Input System when Look action is performed or cancelled.
        /// </summary>
        public void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }
    }
}