using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR

namespace MedMan.Core
{
    /// <summary>
    /// Test component for SceneLoader. Attach to any GameObject in the scene.
    /// Use Inspector buttons to trigger scene transitions manually.
    /// Remove from build before release.
    /// </summary>
    public class SceneLoaderTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private string _testSceneName = "Prototype_Corridor";

        [Header("Fade Override")]
        [SerializeField] private bool _testFadeOnly;

        // ─────────────────────────────────────────
        // Scene Load Tests
        // ─────────────────────────────────────────

        /// <summary>Triggers a full async scene load with fade transition.</summary>
        [Button("Load Test Scene")]
        private void LoadTestScene()
        {
            if (SceneLoader.Instance == null)
            {
                Debug.LogError("[SceneLoaderTester] SceneLoader.Instance is null — is SceneLoader in the scene?");
                return;
            }

            Debug.Log($"[SceneLoaderTester] Loading scene: {_testSceneName}");
            SceneLoader.Instance.LoadScene(_testSceneName);
        }

        // ─────────────────────────────────────────
        // Fade Tests
        // ─────────────────────────────────────────

        /// <summary>Instantly fades to black.</summary>
        [Button("Fade To Black (Immediate)")]
        private void FadeToBlack()
        {
            if (SceneLoader.Instance == null) { Debug.LogError("[SceneLoaderTester] SceneLoader.Instance is null."); return; }
            SceneLoader.Instance.FadeToBlackImmediate();
        }

        /// <summary>Instantly clears the fade overlay.</summary>
        [Button("Clear Fade (Immediate)")]
        private void ClearFade()
        {
            if (SceneLoader.Instance == null) { Debug.LogError("[SceneLoaderTester] SceneLoader.Instance is null."); return; }
            SceneLoader.Instance.ClearFadeImmediate();
        }

        // ─────────────────────────────────────────
        // Event Tests
        // ─────────────────────────────────────────

        /// <summary>Manually fires OnLevelReadyToPlayEvent to test fade-in reveal.</summary>
        [Button("Fire OnLevelReadyToPlayEvent")]
        private void FireLevelReadyEvent()
        {
            EventBus.Publish(new OnLevelReadyToPlayEvent("TEST_STATE"));
            Debug.Log("[SceneLoaderTester] Fired OnLevelReadyToPlayEvent(TEST_STATE)");
        }

        /// <summary>Manually fires OnGameStateChangedEvent to test state transition handling.</summary>
        [Button("Fire OnGameStateChangedEvent (HotelRoom)")]
        private void FireGameStateChangedEvent()
        {
            EventBus.Publish(new OnGameStateChangedEvent(
                GameState.None,
                GameState.HotelRoom,
                FearType.None,
                DreamLevel.None,
                "HotelRoom_None_None"
            ));
            Debug.Log("[SceneLoaderTester] Fired OnGameStateChangedEvent(HotelRoom_None_None)");
        }
    }
}

#endif
