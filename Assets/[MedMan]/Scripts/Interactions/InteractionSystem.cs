using UnityEngine;
using UnityEngine.InputSystem;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Interaction
{
    /// <summary>
    /// Casts a ray from the camera each frame to detect IInteractable objects.
    /// Manages hover state, triggers interactions, and handles cancel input.
    /// Publishes OnInteractionStartedEvent and OnInteractionEndedEvent via EventBus.
    /// Sits under GameplayManager in the hierarchy.
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Raycast")]
        [SerializeField] private Camera _camera;

        [BoxGroup("Raycast")]
        [SerializeField] private float _interactRange = 2.5f;

        [BoxGroup("Raycast")]
        [SerializeField] private LayerMask _interactableLayer;

        private InputAction _interactAction;
        private InputAction _cancelAction;

        private IInteractable _currentHovered;
        private IInteractable _currentActive;
        private bool _isInteracting;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        /// <summary>
        /// Binds interact and cancel input actions.
        /// </summary>
        private void Awake()
        {
            _interactAction = InputSystem.actions.FindAction("Player/Interact");
            _cancelAction   = InputSystem.actions.FindAction("Player/Cancel");
            _interactAction?.Enable();
            _cancelAction?.Enable();
        }

        private void OnDestroy()
        {
            _interactAction?.Disable();
            _cancelAction?.Disable();
        }

        /// <summary>
        /// Each frame: handles cancel input first, then raycast hover detection,
        /// then interact input. Cancel is checked before interact to avoid
        /// re-triggering on the same frame the interaction ends.
        /// </summary>
        private void Update()
        {
            HandleCancel();
            HandleRaycast();
            HandleInteract();
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Casts a ray from the camera center. Updates hover state when
        /// the target IInteractable changes. Clears hover when no target is found
        /// or the target is not currently interactable.
        /// Does not raycast while an interaction is active.
        /// </summary>
        private void HandleRaycast()
        {
            if (_isInteracting) return;

            Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, _interactRange, _interactableLayer))
            {
                IInteractable target = hit.collider.GetComponent<IInteractable>();

                if (target != null && target.IsInteractable)
                {
                    if (target != _currentHovered)
                    {
                        // Exiting previous hover
                        _currentHovered?.OnHoverExit();

                        _currentHovered = target;
                        _currentHovered.OnHoverEnter();

                        // Notify cursor of new interaction type
                        EventBus.Publish(new OnInteractableHoveredEvent(target.InteractionType));
                        Debug.Log($"[InteractionSystem] Hover enter: {hit.collider.name} ({target.InteractionType})");
                    }
                    return;
                }
            }

            // Nothing hit — clear hover
            ClearHover();
        }

        /// <summary>
        /// Triggers the interaction on the currently hovered IInteractable
        /// when the interact input is pressed.
        /// </summary>
        private void HandleInteract()
        {
            if (_currentHovered == null) return;
            if (!_interactAction.WasPressedThisFrame()) return;

            _currentActive = _currentHovered;
            _isInteracting = true;

            _currentActive.OnInteract();
            EventBus.Publish(new OnInteractionStartedEvent(_currentActive.InteractionType));
            Debug.Log($"[InteractionSystem] Interaction started: {_currentActive.InteractionType}");
        }

        /// <summary>
        /// Cancels the active interaction when cancel input is pressed (RMB or backward step).
        /// Always possible — no interaction can block cancel input.
        /// </summary>
        private void HandleCancel()
        {
            if (!_isInteracting) return;
            if (!_cancelAction.WasPressedThisFrame()) return;

            _currentActive?.OnInteractCancel();
            EventBus.Publish(new OnInteractionEndedEvent(_currentActive?.InteractionType ?? InteractionType.None));
            Debug.Log($"[InteractionSystem] Interaction cancelled.");

            _currentActive = null;
            _isInteracting = false;
        }

        /// <summary>
        /// Clears hover state and publishes None cursor type if a hover was active.
        /// </summary>
        private void ClearHover()
        {
            if (_currentHovered == null) return;

            _currentHovered.OnHoverExit();
            _currentHovered = null;

            EventBus.Publish(new OnInteractableHoveredEvent(InteractionType.None));
            Debug.Log("[InteractionSystem] Hover cleared.");
        }

        /// <summary>
        /// Externally ends the active interaction — called by game systems
        /// that take control away from the player (cutscene, state change).
        /// </summary>
        public void ForceEndInteraction()
        {
            if (!_isInteracting) return;

            _currentActive?.OnInteractCancel();
            EventBus.Publish(new OnInteractionEndedEvent(_currentActive?.InteractionType ?? InteractionType.None));

            _currentActive = null;
            _isInteracting = false;
            ClearHover();

            Debug.Log("[InteractionSystem] Interaction force-ended.");
        }
    }
}