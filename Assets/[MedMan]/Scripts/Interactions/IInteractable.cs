using MedMan.Core;
using UnityEngine;

namespace MedMan.Interaction
{
    /// <summary>
    /// Defines the contract for all interactable objects in the game.
    /// Implement on any MonoBehaviour that the player can interact with.
    /// InteractionSystem detects implementors via raycast and drives the interaction lifecycle.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// The type of interaction this object supports.
        /// Used by InteractionCursor to display the correct cursor state.
        /// </summary>
        InteractionType InteractionType { get; }

        /// <summary>
        /// Whether this object can currently be interacted with.
        /// InteractionSystem checks this before triggering OnInteract.
        /// </summary>
        bool IsInteractable { get; }

        /// <summary>
        /// Called by InteractionSystem when the player's raycast first hits this object.
        /// Use for highlight effects, audio cues, or any hover feedback.
        /// </summary>
        void OnHoverEnter();

        /// <summary>
        /// Called by InteractionSystem when the player's raycast stops hitting this object.
        /// Use to clean up any hover state set in OnHoverEnter.
        /// </summary>
        void OnHoverExit();

        /// <summary>
        /// Called by InteractionSystem when the player presses the interact input.
        /// Contains the core interaction logic for this object.
        /// </summary>
        void OnInteract();

        /// <summary>
        /// Called by InteractionSystem when the player exits the interaction
        /// via cancel input (RMB or backward step).
        /// Use to clean up any active interaction state.
        /// </summary>
        void OnInteractCancel();
    }
}