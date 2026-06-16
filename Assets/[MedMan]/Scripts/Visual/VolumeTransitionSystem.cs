using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using NaughtyAttributes;
using MedMan.Core;

namespace MedMan.Visual
{
    /// <summary>
    /// Manages smooth transitions between URP Volume Profiles based on game state changes.
    /// Subscribes to OnGameStateChangedEvent, OnPillConsumedEvent, and OnPillExpiredEvent via EventBus.
    /// Each game state maps to a Volume Profile defined in FearProfileSO or the default profile map.
    ///
    /// Uses DOTween to interpolate Volume weights — the active profile fades in while the previous fades out.
    /// A single Global Volume component is used; this system swaps and blends its profile weight.
    /// </summary>
    public class VolumeTransitionSystem : MonoBehaviour
    {
        // ── Inner Types ──────────────────────────────────────────────────────

        [System.Serializable]
        public class StateVolumeEntry
        {
            public MedMan.Core.GameState gameState;
            public VolumeProfile profile;
        }

        // ── Fields ───────────────────────────────────────────────────────────

        [Header("Volume References")]
        [SerializeField]
        [Tooltip("The Global Volume used for all post-processing transitions.")]
        private Volume _globalVolume;

        [SerializeField]
        [Tooltip("Secondary volume used as blend target during transitions. Weight is animated 0->1.")]
        private Volume _transitionVolume;

        [Header("Default State Profiles")]
        [SerializeField]
        [Tooltip("Map of GameState to Volume Profile. Used for non-dream states.")]
        private List<StateVolumeEntry> _stateProfiles = new List<StateVolumeEntry>();

        [Header("Transition Settings")]
        [SerializeField]
        [Range(0.1f, 5f)]
        private float _transitionDuration = 1.5f;

        [SerializeField]
        private Ease _transitionEase = Ease.InOutSine;

        [Header("Pill Effect Override")]
        [SerializeField]
        [Tooltip("Profile applied when a pill is consumed (brightness surge for Fear A).")]
        private VolumeProfile _pillActiveProfile;

        [SerializeField]
        [Tooltip("Profile applied as the pill effect wears off (agitated brushwork transition).")]
        private VolumeProfile _pillFadeOutProfile;

        [SerializeField]
        [Range(0.1f, 3f)]
        [Tooltip("How quickly the pill effect fades out after expiry.")]
        private float _pillFadeOutDuration = 2f;

        [Header("Debug")]
        [ReadOnly]
        [SerializeField] private string _currentProfileName = "None";
        [ReadOnly]
        [SerializeField] private string _targetProfileName = "None";

        // ── Private ──────────────────────────────────────────────────────────

        private Dictionary<MedMan.Core.GameState, VolumeProfile> _profileMap
            = new Dictionary<MedMan.Core.GameState, VolumeProfile>();

        private VolumeProfile _baseGameStateProfile;
        private Tween _currentTween;
        private bool _pillActive = false;

        // Tracks which Volume is currently "holding" the active profile
        // They swap roles on every transition
        private Volume _activeVolume;
        private Volume _inactiveVolume;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            BuildProfileMap();
            ValidateVolumes();
            SetUpVolumeStartValues();
        }

        private void OnEnable()
        {
            // Register with VisualManager — it owns visual-state routing and drives this mechanism.
            // Self-registration (not an Inspector ref) because VisualManager is a DontDestroyOnLoad
            // singleton while this lives per-scene with the Global Volume.
            VisualManager.Instance?.RegisterVolumeTransition(this);
        }

        private void OnDisable()
        {
            VisualManager.Instance?.UnregisterVolumeTransition(this);
        }

        private void OnDestroy()
        {
            _currentTween?.Kill();
        }

        // ── Public Methods ───────────────────────────────────────────────────

        /// <summary>
        /// Immediately transitions to the given profile without animation.
        /// Useful for scene initialization.
        /// </summary>
        public void SetProfileImmediate(VolumeProfile profile)
        {
            if (profile == null || _activeVolume == null) return;

            _currentTween?.Kill();
            _activeVolume.sharedProfile   = profile;
            _activeVolume.weight          = 1f;
            _inactiveVolume.weight        = 0f;
            _currentProfileName           = profile.name;
        }

        /// <summary>
        /// Smoothly transitions to the given profile over the configured duration.
        /// Alternates which Volume animates on each call:
        /// - Even calls: inactive volume fades IN (0->1) while active holds at 1
        /// - Odd calls: active volume fades OUT (1->0) while inactive holds the new profile at 1
        /// After each transition the roles swap.
        /// </summary>
        public void TransitionToProfile(VolumeProfile profile, float duration = -1f)
        {
            if (profile == null || _activeVolume == null || _inactiveVolume == null) return;

            float dur = duration < 0f ? _transitionDuration : duration;
            _targetProfileName = profile.name;

            _currentTween?.Kill();

            // Assign new profile to inactive volume and ensure active stays visible
            _inactiveVolume.sharedProfile = profile;
            _inactiveVolume.weight        = 0f;
            _activeVolume.weight          = 1f;

            // Animate inactive from 0 to 1
            Volume incomingVolume = _inactiveVolume;
            Volume outgoingVolume = _activeVolume;

            _currentTween = DOTween.To(
                () => incomingVolume.weight,
                x =>
                {
                    incomingVolume.weight = x;
                    outgoingVolume.weight = 1f - x;
                },
                1f,
                dur
            )
            .SetEase(_transitionEase)
            .OnComplete(() =>
            {
                incomingVolume.weight = 1f;
                outgoingVolume.weight = 0f;

                // Swap roles for next transition
                _activeVolume   = incomingVolume;
                _inactiveVolume = outgoingVolume;

                _currentProfileName = profile.name;
            });
        }
        
        /// <summary>
        /// Applies the Volume Profile mapped to the given game state.
        /// Ignored while a pill effect is active (pill profile takes priority).
        /// Called by VisualManager on state change.
        /// </summary>
        public void ApplyStateProfile(MedMan.Core.GameState state)
        {
            if (_pillActive) return;

            if (_profileMap.TryGetValue(state, out VolumeProfile profile))
            {
                _baseGameStateProfile = profile;
                TransitionToProfile(profile);
            }
        }

        /// <summary>Transitions to the pill-active profile (fast onset). Called by VisualManager.</summary>
        public void ApplyPillProfile()
        {
            if (_pillActiveProfile == null) return;

            _pillActive = true;
            TransitionToProfile(_pillActiveProfile, 0.3f);
        }

        /// <summary>
        /// Begins the pill fade-out, then returns to the base state profile. Called by VisualManager.
        /// </summary>
        public void ApplyPillExpiry()
        {
            _pillActive = false;

            if (_pillFadeOutProfile != null)
            {
                TransitionToProfile(_pillFadeOutProfile, 0.5f);
                StartCoroutine(ReturnToBaseAfterFadeOutCor());
            }
            else if (_baseGameStateProfile != null)
            {
                TransitionToProfile(_baseGameStateProfile, _pillFadeOutDuration);
            }
        }
        

        // ── Private Methods ──────────────────────────────────────────────────

        private void BuildProfileMap()
        {
            _profileMap.Clear();
            foreach (StateVolumeEntry entry in _stateProfiles)
            {
                if (entry.profile != null)
                    _profileMap[entry.gameState] = entry.profile;
            }
        }

        private void ValidateVolumes()
        {
            if (_globalVolume == null)
                Debug.LogError("[VolumeTransitionSystem] Global Volume not assigned.", this);

            if (_transitionVolume == null)
                Debug.LogError("[VolumeTransitionSystem] Transition Volume not assigned.", this);
        }

        private void SetUpVolumeStartValues()
        {
            _globalVolume.weight     = 1f;
            _transitionVolume.weight = 0f;

            // Global starts as active (holds current profile), transition starts as inactive
            _activeVolume   = _globalVolume;
            _inactiveVolume = _transitionVolume;
        }

        private IEnumerator ReturnToBaseAfterFadeOutCor()
        {
            yield return new WaitForSeconds(_pillFadeOutDuration);

            if (!_pillActive && _baseGameStateProfile != null)
                TransitionToProfile(_baseGameStateProfile, _transitionDuration);
        }
    }
}
