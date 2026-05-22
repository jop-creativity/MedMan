using UnityEngine;
using UnityEngine.InputSystem;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Player
{
    /// <summary>
    /// First-person camera controller. Implements IControllable and ILookControllable.
    /// Handles mouse look, camera assist toward narrative targets,
    /// and rotation lock during object interactions.
    /// Uses direct InputAction.ReadValue each frame — avoids Input System callback jitter.
    /// Attach to the Camera GameObject (child of Player).
    /// </summary>
    public class CameraController : MonoBehaviour, IControllable, ILookControllable
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Look Settings")]
        [SerializeField] private float _mouseSensitivity = 0.5f;

        [BoxGroup("Look Settings")]
        [Range(-90f, 0f)]
        [SerializeField] private float _verticalLookMin = -80f;

        [BoxGroup("Look Settings")]
        [Range(0f, 90f)]
        [SerializeField] private float _verticalLookMax = 80f;

        [BoxGroup("Camera Assist")]
        [SerializeField] private float _defaultAssistStrength = 0.3f;

        [BoxGroup("Interaction Lock")]
        [Range(0f, 180f)]
        [SerializeField] private float _lockMaxHorizontal = 45f;

        [BoxGroup("Interaction Lock")]
        [Range(0f, 90f)]
        [SerializeField] private float _lockMaxVertical = 30f;

        private const float _lookThreshold = 0.01f;

        private Transform _playerTransform;
        private InputAction _lookAction;
        private float _verticalRotation;

        private bool _isControlEnabled = true;
        private bool _isLocked;

        private bool _isAssisting;
        private Vector3 _assistTarget;
        private float _assistStrength;

        private bool _isRotationLocked;
        private float _lockHorizontal;
        private float _lockVertical;
        private Quaternion _lockBaseRotation;

        // ─────────────────────────────────────────
        // Properties — IControllable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public bool IsControlEnabled => _isControlEnabled;

        // ─────────────────────────────────────────
        // Properties — ILookControllable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public bool IsLocked => _isLocked;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        /// <summary>
        /// Caches player transform, locks cursor and binds Look action directly.
        /// Direct InputAction binding avoids Input System callback jitter.
        /// </summary>
        private void Awake()
        {
            _playerTransform = transform.parent;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            _lookAction = InputSystem.actions.FindAction("Player/Look");
            _lookAction?.Enable();
        }

        private void OnEnable()  => SubscribeToEvents();
        private void OnDisable() => UnsubscribeFromEvents();

        private void OnDestroy()
        {
            _lookAction?.Disable();
        }

        /// <summary>
        /// Reads look input and handles camera rotation after all Update calls complete.
        /// LateUpdate ensures smooth camera movement without jitter.
        /// </summary>
        private void LateUpdate()
        {
            if (!_isControlEnabled) return;

            HandleLook();

            if (_isAssisting)
                HandleCameraAssist();
        }

        // ─────────────────────────────────────────
        // Public methods — IControllable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public void EnableControl()
        {
            _isControlEnabled = true;
            Debug.Log("[CameraController] Control enabled.");
        }

        /// <inheritdoc/>
        /// <summary>
        /// Disables camera input.
        /// Look input is read directly each frame so no reset needed.
        /// </summary>
        public void DisableControl()
        {
            _isControlEnabled = false;
            Debug.Log("[CameraController] Control disabled.");
        }

        // ─────────────────────────────────────────
        // Public methods — ILookControllable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public void AssistToward(Vector3 targetPosition, float assistStrength)
        {
            _isAssisting    = true;
            _assistTarget   = targetPosition;
            _assistStrength = Mathf.Clamp01(assistStrength);
            Debug.Log($"[CameraController] Assist toward {targetPosition} strength: {assistStrength}");
        }

        /// <inheritdoc/>
        public void LockRotation(float maxHorizontalAngle, float maxVerticalAngle)
        {
            _isRotationLocked = true;
            _isLocked         = true;
            _lockHorizontal   = maxHorizontalAngle;
            _lockVertical     = maxVerticalAngle;
            _lockBaseRotation = transform.rotation;
            Debug.Log($"[CameraController] Rotation locked H:{maxHorizontalAngle} V:{maxVerticalAngle}");
        }

        /// <inheritdoc/>
        public void Releaselock()
        {
            _isAssisting      = false;
            _isRotationLocked = false;
            _isLocked         = false;
            Debug.Log("[CameraController] Lock released.");
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Reads look input directly each frame and applies rotation.
        /// Direct ReadValue avoids Input System callback accumulation jitter.
        /// </summary>
        private void HandleLook()
        {
            Vector2 lookInput = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

            if (lookInput.sqrMagnitude < _lookThreshold) return;

            float mouseX = lookInput.x * _mouseSensitivity;
            float mouseY = lookInput.y * _mouseSensitivity;

            _verticalRotation -= mouseY;
            _verticalRotation  = Mathf.Clamp(_verticalRotation, _verticalLookMin, _verticalLookMax);

            if (_isRotationLocked)
            {
                ApplyRotationLock(mouseX);
                return;
            }

            transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
            _playerTransform.Rotate(Vector3.up * mouseX);
        }

        /// <summary>
        /// Clamps player and camera rotation within lock angles relative to base rotation.
        /// </summary>
        private void ApplyRotationLock(float mouseX)
        {
            _playerTransform.Rotate(Vector3.up * mouseX);

            float horizontalDelta = Quaternion.Angle(
                new Quaternion(0, _lockBaseRotation.y, 0, _lockBaseRotation.w),
                new Quaternion(0, _playerTransform.rotation.y, 0, _playerTransform.rotation.w)
            );

            if (horizontalDelta > _lockHorizontal)
                _playerTransform.rotation = _lockBaseRotation;

            _verticalRotation = Mathf.Clamp(_verticalRotation, -_lockVertical, _lockVertical);
            transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
        }

        /// <summary>
        /// Gently rotates camera toward assist target each frame.
        /// Does not hard-lock — player input still works.
        /// </summary>
        private void HandleCameraAssist()
        {
            Vector3 directionToTarget = (_assistTarget - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _assistStrength * Time.deltaTime
            );
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<OnCameraAssistRequestedEvent>(HandleAssistRequested);
            EventBus.Subscribe<OnCameraLockRequestedEvent>(HandleLockRequested);
            EventBus.Subscribe<OnCameraReleaseRequestedEvent>(HandleReleaseRequested);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<OnCameraAssistRequestedEvent>(HandleAssistRequested);
            EventBus.Unsubscribe<OnCameraLockRequestedEvent>(HandleLockRequested);
            EventBus.Unsubscribe<OnCameraReleaseRequestedEvent>(HandleReleaseRequested);
        }

        private void HandleAssistRequested(OnCameraAssistRequestedEvent e)
            => AssistToward(e.TargetPosition, e.AssistStrength);

        private void HandleLockRequested(OnCameraLockRequestedEvent e)
            => LockRotation(e.MaxHorizontalAngle, e.MaxVerticalAngle);

        private void HandleReleaseRequested(OnCameraReleaseRequestedEvent e)
            => Releaselock();
    }
}
