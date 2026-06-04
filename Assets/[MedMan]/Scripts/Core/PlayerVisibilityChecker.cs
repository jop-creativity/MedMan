using UnityEngine;

namespace MedMan.Core
{
    /// <summary>
    /// Utility component that detects whether the player's camera is currently looking at this object.
    /// Uses dot product for angle check and a raycast to verify line of sight is not occluded.
    /// Attach to any GameObject that needs visibility-aware behaviour (e.g. WorldSpaceText fade-out).
    /// </summary>
    public class PlayerVisibilityChecker : MonoBehaviour
    {
        // ── Fields ───────────────────────────────────────────────────────────

        [Header("Visibility Settings")]
        [SerializeField] private float _fieldOfViewAngle = 60f;
        [SerializeField] private float _maxDistance = 20f;
        [SerializeField] private LayerMask _occlusionMask = Physics.DefaultRaycastLayers;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = false;

        // ── Properties ───────────────────────────────────────────────────────

        /// <summary>True if the player camera is currently looking at this object with clear line of sight.</summary>
        public bool IsVisibleToPlayer { get; private set; }

        /// <summary>Normalized dot product of camera forward and direction to this object. 1 = directly ahead.</summary>
        public float DotProduct { get; private set; }

        // ── Private ──────────────────────────────────────────────────────────

        private Camera _mainCamera;
        private float _halfAngleCos;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _mainCamera = Camera.main;
            _halfAngleCos = Mathf.Cos(_fieldOfViewAngle * 0.5f * Mathf.Deg2Rad);
        }

        private void Update()
        {
            IsVisibleToPlayer = CheckVisibility();
        }

        // ── Public Methods ───────────────────────────────────────────────────

        /// <summary>
        /// Manually reconfigure the visibility checker at runtime.
        /// Useful when the same prefab is reused with different detection parameters.
        /// </summary>
        public void Configure(float fovAngle, float maxDist, LayerMask occlusionMask)
        {
            _fieldOfViewAngle = fovAngle;
            _maxDistance = maxDist;
            _occlusionMask = occlusionMask;
            _halfAngleCos = Mathf.Cos(_fieldOfViewAngle * 0.5f * Mathf.Deg2Rad);
        }

        // ── Private Methods ──────────────────────────────────────────────────

        /// <summary>
        /// Performs a two-stage visibility check:
        /// 1. Dot product — is the object within the camera's field of view angle?
        /// 2. Raycast — is the line of sight unoccluded?
        /// </summary>
        private bool CheckVisibility()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return false;
            }

            Vector3 directionToObject = transform.position - _mainCamera.transform.position;
            float distance = directionToObject.magnitude;

            // Early out — too far away
            if (distance > _maxDistance)
            {
                DotProduct = 0f;
                return false;
            }

            // Dot product check — is object within FOV cone?
            Vector3 directionNormalized = directionToObject / distance;
            DotProduct = Vector3.Dot(_mainCamera.transform.forward, directionNormalized);

            if (DotProduct < _halfAngleCos)
                return false;

            // Raycast check — is line of sight clear?
            if (Physics.Raycast(_mainCamera.transform.position, directionNormalized, out RaycastHit hit, distance, _occlusionMask))
            {
                // If the ray hits something other than this object's collider, sight is occluded
                if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                    return false;
            }

            return true;
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;

            Gizmos.color = IsVisibleToPlayer ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            if (Camera.main != null)
                Gizmos.DrawLine(Camera.main.transform.position, transform.position);
        }
    }
}
