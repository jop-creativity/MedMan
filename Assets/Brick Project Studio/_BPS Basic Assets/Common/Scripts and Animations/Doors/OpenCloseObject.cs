using System.Collections;
using UnityEngine;
using NaughtyAttributes;

namespace MedMan.Interaction
{
    /// <summary>
    /// Toggles a door's open/closed animation state.
    /// Called by InteractableObject.OnInteract() when InteractionType is Use.
    /// Distance/raycast checks are handled by InteractionSystem — this component
    /// only plays the animation and tracks state.
    /// </summary>
    public class OpenCloseObject : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Door")]
        [Required]
        [SerializeField] private Animator _animator;

        [BoxGroup("Door")]
        [Tooltip("Animator state name played when opening.")]
        [SerializeField] private string _openStateName = "Opening";

        [BoxGroup("Door")]
        [Tooltip("Animator state name played when closing.")]
        [SerializeField] private string _closeStateName = "Closing";

        [BoxGroup("Door")]
        [Tooltip("Time in seconds before the door can be toggled again.")]
        [SerializeField] private float _toggleCooldown = 0.5f;

        // ─────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────

        /// <summary>True if the door is currently open.</summary>
        public bool IsOpen { get; private set; }

        // ─────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────

        private bool _isToggling;

        // ─────────────────────────────────────────
        // Public methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Toggles the door between open and closed states.
        /// Ignored if a toggle is already in progress.
        /// </summary>
        public void Toggle()
        {
            if (_isToggling) return;
            StartCoroutine(ToggleCor());
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>Plays the appropriate animation and blocks further toggles during cooldown.</summary>
        private IEnumerator ToggleCor()
        {
            _isToggling = true;

            if (!IsOpen)
            {
                _animator.Play(_openStateName);
                IsOpen = true;
                Debug.Log($"[OpenCloseDoor] Opening: {name}");
            }
            else
            {
                _animator.Play(_closeStateName);
                IsOpen = false;
                Debug.Log($"[OpenCloseDoor] Closing: {name}");
            }

            yield return new WaitForSeconds(_toggleCooldown);
            _isToggling = false;
        }
    }
}