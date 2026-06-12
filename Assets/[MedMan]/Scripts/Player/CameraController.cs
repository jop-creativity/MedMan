using DG.Tweening;
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
        
        [BoxGroup("Interaction Mode")]
        [SerializeField] private float _interactionTweenDuration = 0.4f;


        private const float _lookThreshold = 0.01f;
        private const float _lookMaxDelta     = 100f;
        private const float _lookMaxDeltaSqr  = _lookMaxDelta * _lookMaxDelta;

        private Vector3 _anchorLocalPosition;
        private Quaternion _anchorLocalRotation;
        private Vector3 _anchorWorldPosition;
        private Quaternion _anchorWorldRotation;
        private Transform _playerTransform;
        
        private InputAction _lookAction;
        private float _verticalRotation;

        private bool _isControlEnabled = true;
        private bool _isLocked;
        private bool _isInInteractionMode;
        private bool _isAssisting;
        private bool _isRotationLocked;
        private bool _isTweening;
        private bool _skipNextFrame;
        
        private float _assistStrength;
        private float _lockHorizontal;
        private float _lockVertical;
        private float _anchorVerticalRotation;
        
        private Quaternion _lockBaseRotation;
        private Vector3 _assistTarget;


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
            if (_isTweening) return;

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
            
            if (lookInput.sqrMagnitude < _lookThreshold || lookInput.sqrMagnitude > _lookMaxDeltaSqr) return;

            float mouseX = lookInput.x * _mouseSensitivity;
            float mouseY = lookInput.y * _mouseSensitivity;

            _verticalRotation -= mouseY;

            if (_isInInteractionMode)
            {
                // Clamp relative to view point's base pitch, not absolute zero
                float basePitch = _lockBaseRotation.eulerAngles.x;
                if (basePitch > 180f) basePitch -= 360f;

                _verticalRotation = Mathf.Clamp(_verticalRotation, 
                    basePitch - _lockMaxVertical, 
                    basePitch + _lockMaxVertical);

                float clampedX = Mathf.Clamp(transform.eulerAngles.y + mouseX,
                    _lockBaseRotation.eulerAngles.y - _lockMaxHorizontal,
                    _lockBaseRotation.eulerAngles.y + _lockMaxHorizontal);

                transform.rotation = Quaternion.Euler(_verticalRotation, clampedX, 0f);
                return;
            }

            _verticalRotation = Mathf.Clamp(_verticalRotation, _verticalLookMin, _verticalLookMax);

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
            EventBus.Subscribe<OnInteractionViewRequestedEvent>(HandleInteractionViewRequested);
            EventBus.Subscribe<OnInteractionViewExitedEvent>(HandleInteractionViewExited);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<OnCameraAssistRequestedEvent>(HandleAssistRequested);
            EventBus.Unsubscribe<OnCameraLockRequestedEvent>(HandleLockRequested);
            EventBus.Unsubscribe<OnCameraReleaseRequestedEvent>(HandleReleaseRequested);
            EventBus.Unsubscribe<OnInteractionViewRequestedEvent>(HandleInteractionViewRequested);
            EventBus.Unsubscribe<OnInteractionViewExitedEvent>(HandleInteractionViewExited);
        }

        private void HandleAssistRequested(OnCameraAssistRequestedEvent e)
        {
            if (_isInInteractionMode) return; // Interaction mode takes priority
            AssistToward(e.TargetPosition, e.AssistStrength);
        }

        private void HandleLockRequested(OnCameraLockRequestedEvent e)
            => LockRotation(e.MaxHorizontalAngle, e.MaxVerticalAngle);

        private void HandleReleaseRequested(OnCameraReleaseRequestedEvent e)
            => Releaselock();
        
        private void HandleInteractionViewRequested(OnInteractionViewRequestedEvent e)
    => EnterInteractionMode(e.ViewPoint);

        private void HandleInteractionViewExited(OnInteractionViewExitedEvent e)
            => ExitInteractionMode();

        /// <summary>
        /// Detaches camera from player, tweens it to the interaction view point,
        /// then locks rotation and blocks player movement.
        /// </summary>
        private void EnterInteractionMode(Transform viewPoint)
        {
            if (_isInInteractionMode) return;
            _isInInteractionMode = true;

            // Snapshot both local and world before detaching
            _anchorLocalPosition = transform.localPosition;
            _anchorLocalRotation = transform.localRotation;
            _anchorWorldPosition = transform.position;
            _anchorWorldRotation = transform.rotation;
            _anchorVerticalRotation = _verticalRotation;

            EventBus.Publish(new OnPlayerControlChangedEvent(false));

            // Detach BEFORE tween — prevents player transform from dragging camera
            transform.SetParent(null);

            _isTweening = true;

            transform.DOMove(viewPoint.position, _interactionTweenDuration).SetEase(Ease.InOutSine);
            transform.DORotateQuaternion(viewPoint.rotation, _interactionTweenDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    _isTweening = false;
                    // Sync vertical rotation to view point's actual pitch
                    _verticalRotation = viewPoint.eulerAngles.x;
                    if (_verticalRotation > 180f) _verticalRotation -= 360f;
                    LockRotation(_lockMaxHorizontal, _lockMaxVertical);
                    Debug.Log("[CameraController] Interaction mode entered.");
                });
        }

        /// <summary>
        /// Tweens camera back to player anchor position and rotation,
        /// re-attaches it, releases rotation lock and restores player movement.
        /// </summary>
        private void ExitInteractionMode()
        {
            if (!_isInInteractionMode) return;
            _isInInteractionMode = false;

            Releaselock();

            _isTweening = true;
            transform.DOMove(_anchorWorldPosition, _interactionTweenDuration).SetEase(Ease.InOutSine);
            transform.DORotateQuaternion(_anchorWorldRotation, _interactionTweenDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    _isTweening = false;
                    transform.SetParent(_playerTransform);
                    transform.localPosition = _anchorLocalPosition;
                    transform.localRotation = _anchorLocalRotation;
                    _verticalRotation = _anchorLocalRotation.eulerAngles.x;
                    if (_verticalRotation > 180f) _verticalRotation -= 360f;

                    _playerTransform.rotation = Quaternion.Euler(0f, _anchorWorldRotation.eulerAngles.y, 0f);

                    EventBus.Publish(new OnPlayerControlChangedEvent(true));
                    Debug.Log("[CameraController] Interaction mode exited.");
                });
        }
    }
}
