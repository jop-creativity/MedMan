using UnityEngine;
using MedMan.Core;
using MedMan.Interaction;
using MedMan.Narrative;
using NaughtyAttributes;
using SojaExiles;
using UnityEngine.Serialization;

namespace MedMan.Interaction
{
    /// <summary>
    /// Attach to any GameObject that should be interactable.
    /// Implements IInteractable — detected by InteractionSystem via raycast.
    /// Publishes interaction view events via EventBus on interact and cancel.
    /// When InteractionType is Take, handles pickup logic internally —
    /// no additional component required.
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
        
        [BoxGroup("Interaction")]
        [SerializeField] private bool _hasSequence;

        [BoxGroup("Interaction")]
        [ShowIf("_hasSequence")]
        [AllowNesting]
        [SerializeField] private NarrativeTextController _narrativeTextController;

        [BoxGroup("Pickup")]
        [ShowIf("_interactionType", InteractionType.Take)]
        [SerializeField] private PickupType _pickupType = PickupType.None;

        [BoxGroup("Pickup")]
        [ShowIf("_interactionType", InteractionType.Take)]
        [SerializeField] private int _amount = 1;
        
        [FormerlySerializedAs("_doorController")]
        [BoxGroup("Interaction")]
        [ShowIf("_interactionType", InteractionType.OpenClose)]
        [AllowNesting]
        [Tooltip("Door component to toggle when interacted with.")]
        [SerializeField] private OpenCloseObject objectController;

        
 

        // ─────────────────────────────────────────
        // Properties — IInteractable
        // ─────────────────────────────────────────

        /// <inheritdoc/>
        public InteractionType InteractionType => _interactionType;

        /// <inheritdoc/>
        public bool IsInteractable => _isInteractable;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        /// <summary>Subscribes to take input event when object is active.</summary>
        private void OnEnable()
        {
            EventBus.Subscribe<OnTakeInputEvent>(HandleTakeInput);
        }

        /// <summary>Unsubscribes from take input event when object is deactivated.</summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnTakeInputEvent>(HandleTakeInput);
        }

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
        /// Moves camera to view point. For Take type, also shows pickup prompt.
        /// </summary>
        public void OnInteract()
        {
            Debug.Log($"[InteractableObject] Interact: {name} ({_interactionType})");

            if (_interactionViewPoint != null)
                EventBus.Publish(new OnInteractionViewRequestedEvent(_interactionViewPoint));

            if (_interactionType == InteractionType.Take)
                EventBus.Publish(new OnPickupPromptShownEvent(true));
            
            if (_hasSequence && _narrativeTextController != null)
                _narrativeTextController.PlaySequence();
            
            if (_interactionType == InteractionType.OpenClose)
                objectController?.Toggle();
        }

        /// <inheritdoc/>
        /// <summary>
        /// Returns camera to player. For Take type, also hides pickup prompt.
        /// </summary>
        public void OnInteractCancel()
        {
            Debug.Log($"[InteractableObject] Interact cancel: {name}");

            EventBus.Publish(new OnInteractionViewExitedEvent());

            if (_interactionType == InteractionType.Take)
                EventBus.Publish(new OnPickupPromptShownEvent(false));
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Handles take input — collects the pickup, hides prompt,
        /// returns camera and destroys this object.
        /// Only fires if this object is of type Take and is still interactable.
        /// </summary>
        private void HandleTakeInput(OnTakeInputEvent e)
        {
            if (_interactionType != InteractionType.Take) return;
            if (!_isInteractable) return;

            _isInteractable = false;

            EventBus.Publish(new OnPickupCollectedEvent(_pickupType, _amount));
            EventBus.Publish(new OnInteractionViewExitedEvent());
            EventBus.Publish(new OnPickupPromptShownEvent(false));
            EventBus.Publish(new OnInteractionEndedEvent(InteractionType.Take));

            Debug.Log($"[InteractableObject] Collected: {_pickupType} x{_amount}");
            Destroy(gameObject);
        }
    }
}