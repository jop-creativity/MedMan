using UnityEngine;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Testing
{
    /// <summary>
    /// Test script for VolumeTransitionSystem.
    /// Attach to any GameObject in the test scene.
    /// Use the Inspector buttons to fire GameState change events in Play Mode.
    /// Remove before shipping.
    /// </summary>
    public class VolumeTransitionTest : MonoBehaviour
    {
        [Button("-> MainMenu")]
        private void ToMainMenu() => Publish(GameState.MainMenu);

        [Button("-> DoctorsOffice")]
        private void ToDoctorsOffice() => Publish(GameState.DoctorsOffice);

        [Button("-> HotelRoom")]
        private void ToHotelRoom() => Publish(GameState.HotelRoom);

        [Button("-> Dream")]
        private void ToDream() => Publish(GameState.Dream);

        [Button("-> Epilogue")]
        private void ToEpilogue() => Publish(GameState.Epilogue);

        [Button("Pill: Consume")]
        private void PillConsume() => EventBus.Publish(new OnPillConsumedEvent(3));

        [Button("Pill: Expire")]
        private void PillExpire() => EventBus.Publish(new OnPillExpiredEvent());

        private void Publish(GameState state)
        {
            string stateID = $"{state}_None_None";
            EventBus.Publish(new OnGameStateChangedEvent(GameState.MainMenu, state, FearType.None, DreamLevel.None, stateID));
            UnityEngine.Debug.Log($"[VolumeTransitionTest] Published: {stateID}");
        }
    }
}
