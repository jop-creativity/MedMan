using UnityEngine;
using MedMan.Core;
using MedMan.Interaction;
using NaughtyAttributes;

namespace MedMan.Interaction
{
    /// <summary>
    /// Attach to any GameObject that should be interactable.
    /// Implements IInteractable — detected by InteractionSystem via raycast.
    /// Publishes interaction view events via EventBus on interact and cancel.
    /// Requires a Collider on the same GameObject.
    /// </summary>
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Interaction")]
        [SerializeField] private InteractionType _interactionType = InteractionType.Examine;

        [BoxGroup("Interaction")]
        [SerializeField] private bool _isInteractable = true;

        [BoxGroup("Interaction")]
        [Tooltip("Empty GameObject positioned where the camera should move during interaction.")]
        [SerializeField] private Transform _interactionViewPoint;

        // ─────────────────────────────────────────
        // Properties — IInteractable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public InteractionType InteractionType => _interactionType;

        /// <inheritdoc/>
        public bool IsInteractable => _isInteractable;

        // ─────────────────────────────────────────
        // Public methods — IInteractable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public void OnHoverEnter()
        {
            Debug.Log($"[InteractableObject] Hover enter: {name}");
        }

        /// <inheritdoc/>
        public void OnHoverExit()
        {
            Debug.Log($"[InteractableObject] Hover exit: {name}");
        }

        /// <inheritdoc/>
        /// <summary>
        /// Publishes OnInteractionViewRequestedEvent if a view point is assigned.
        /// InteractionSystem and CameraController handle the rest.
        /// </summary>
        public void OnInteract()
        {
            Debug.Log($"[InteractableObject] Interact: {name} ({_interactionType})");

            if (_interactionViewPoint != null)
                EventBus.Publish(new OnInteractionViewRequestedEvent(_interactionViewPoint));
        }

        /// <inheritdoc/>
        /// <summary>
        /// Publishes OnInteractionViewExitedEvent to return camera to player.
        /// </summary>
        public void OnInteractCancel()
        {
            Debug.Log($"[InteractableObject] Interact cancel: {name}");
            EventBus.Publish(new OnInteractionViewExitedEvent());
        }
    }
}