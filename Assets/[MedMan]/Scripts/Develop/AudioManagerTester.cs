using UnityEngine;
using NaughtyAttributes;
using MedMan.Core;

#if UNITY_EDITOR

namespace MedMan.Audio
{
    /// <summary>
    /// Test component for AudioManager. Attach to any GameObject in the test scene.
    /// Use Inspector buttons to trigger audio actions manually.
    /// Lives in Editor folder — excluded from builds automatically.
    /// </summary>
    public class AudioManagerTester : MonoBehaviour
    {
        [BoxGroup("Test Clips")]
        [SerializeField] private AudioClip _testMusicClip;

        [BoxGroup("Test Clips")]
        [SerializeField] private AudioClip _testAmbientClip;

        [BoxGroup("Test Clips")]
        [SerializeField] private AudioClip _testSFXClip;

        [BoxGroup("Volume Test")]
        [Range(0f, 1f)]
        [SerializeField] private float _testMasterVolume = 1f;

        [BoxGroup("Volume Test")]
        [Range(0f, 1f)]
        [SerializeField] private float _testMusicVolume = 1f;

        [BoxGroup("Volume Test")]
        [Range(0f, 1f)]
        [SerializeField] private float _testAmbientVolume = 1f;

        [BoxGroup("Volume Test")]
        [Range(0f, 1f)]
        [SerializeField] private float _testSFXVolume = 1f;

        // ─────────────────────────────────────────
        // Snapshot Tests
        // ─────────────────────────────────────────

        [Button("Snapshot — Default")]
        private void TransitionToDefault()
            => FireStateChanged(GameState.None);

        [Button("Snapshot — MainMenu")]
        private void TransitionToMainMenu()
            => FireStateChanged(GameState.MainMenu);

        [Button("Snapshot — HotelRoom")]
        private void TransitionToHotelRoom()
            => FireStateChanged(GameState.HotelRoom);

        [Button("Snapshot — Dream")]
        private void TransitionToDream()
            => FireStateChanged(GameState.Dream);

        [Button("Snapshot — PillActive (simulate pill consumed)")]
        private void SimulatePillConsumed()
            => EventBus.Publish(new OnPillConsumedEvent(2));

        [Button("Snapshot — Dream (simulate pill expired)")]
        private void SimulatePillExpired()
            => EventBus.Publish(new OnPillExpiredEvent(2));

        // ─────────────────────────────────────────
        // Playback Tests
        // ─────────────────────────────────────────

        [Button("Play Music")]
        private void PlayMusic()
        {
            if (!CheckInstance()) return;
            AudioManager.Instance.PlayMusic(_testMusicClip);
        }

        [Button("Stop Music")]
        private void StopMusic()
        {
            if (!CheckInstance()) return;
            AudioManager.Instance.StopMusic();
        }

        [Button("Play Ambient")]
        private void PlayAmbient()
        {
            if (!CheckInstance()) return;
            AudioManager.Instance.PlayAmbient(_testAmbientClip);
        }

        [Button("Stop Ambient")]
        private void StopAmbient()
        {
            if (!CheckInstance()) return;
            AudioManager.Instance.StopAmbient();
        }

        [Button("Play SFX")]
        private void PlaySFX()
        {
            if (!CheckInstance()) return;
            AudioManager.Instance.PlaySFX(_testSFXClip);
        }

        // ─────────────────────────────────────────
        // Volume Tests
        // ─────────────────────────────────────────

        [Button("Apply Volume Settings")]
        private void ApplyVolumes()
        {
            if (!CheckInstance()) return;
            AudioManager.Instance.SetVolume("MasterVolume",  _testMasterVolume);
            AudioManager.Instance.SetVolume("MusicVolume",   _testMusicVolume);
            AudioManager.Instance.SetVolume("AmbientVolume", _testAmbientVolume);
            AudioManager.Instance.SetVolume("SFXVolume",     _testSFXVolume);
            Debug.Log("[AudioManagerTester] Volumes applied.");
        }

        // ─────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────

        private void FireStateChanged(GameState newState)
        {
            EventBus.Publish(new OnGameStateChangedEvent(
                GameState.None,
                newState,
                FearType.None,
                DreamLevel.None,
                $"{newState}_None_None"
            ));
            Debug.Log($"[AudioManagerTester] Fired OnGameStateChangedEvent({newState})");
        }

        private bool CheckInstance()
        {
            if (AudioManager.Instance != null) return true;
            Debug.LogError("[AudioManagerTester] AudioManager.Instance is null — is AudioManager in the scene?");
            return false;
        }
    }
}

#endif
