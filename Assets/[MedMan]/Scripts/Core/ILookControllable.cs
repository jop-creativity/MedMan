using UnityEngine;

namespace MedMan.Core
{
    /// <summary>
    /// Extends IControllable with camera-specific functionality.
    /// Implemented by CameraController.
    /// Allows external systems to assist camera toward a target
    /// and lock rotation during object interactions.
    /// Follows Interface Segregation — only CameraController implements this,
    /// PlayerController uses IControllable only.
    /// </summary>
    public interface ILookControllable : IControllable
    {
        /// <summary>
        /// Gently assists camera toward a world-space target position.
        /// Does not hard-lock player input — player can still look away.
        /// </summary>
        void AssistToward(Vector3 targetPosition, float assistStrength);

        /// <summary>
        /// Locks camera rotation within defined angle constraints.
        /// Used during object interactions to limit how far player can look away.
        /// </summary>
        void LockRotation(float maxHorizontalAngle, float maxVerticalAngle);

        /// <summary>
        /// Releases any active camera assist or rotation lock.
        /// </summary>
        void Releaselock();

        /// <summary>Whether camera is currently locked or assisted toward a target.</summary>
        bool IsLocked { get; }
    }
}
