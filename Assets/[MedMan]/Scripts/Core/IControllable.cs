namespace MedMan.Core
{
    /// <summary>
    /// Defines a contract for any system that can have player control
    /// enabled or disabled by GameplayManager.
    /// Implemented by: PlayerController, CameraController.
    /// </summary>
    public interface IControllable
    {
        /// <summary>Enables player input and control.</summary>
        void EnableControl();

        /// <summary>Disables player input and control. Used during cutscenes and interactions.</summary>
        void DisableControl();

        /// <summary>Whether this controller is currently accepting input.</summary>
        bool IsControlEnabled { get; }
    }
}
