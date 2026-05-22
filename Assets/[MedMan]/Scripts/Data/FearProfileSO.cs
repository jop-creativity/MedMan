using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using NaughtyAttributes;
using UnityEngine.Serialization;

namespace MedMan.Data
{
    /// <summary>
    /// Self-contained configuration package for a specific Fear + DreamLevel combination.
    /// All systems (Audio, Visual, Narrative, DreamGameplayManager) load their
    /// settings exclusively from this asset — no system searches for data elsewhere.
    ///
    /// Naming convention: FearProfile_[FearType]_[DreamLevel]
    /// Example: FearProfile_Fear_A_Level1
    /// </summary>
    [CreateAssetMenu(fileName = "FearProfile_Fear_A_Level1", menuName = "MedMan/Fear Profile SO")]
    public class FearProfileSO : ScriptableObject
    {
        #region Identity

        [BoxGroup("Identity")]
        [InfoBox("Set FearType and DreamLevel. StateID fields below are read-only and generated automatically.")]
        [SerializeField] private Core.FearType _fearType;

        [BoxGroup("Identity")]
        [SerializeField] private Core.DreamLevel _dreamLevel;

        /// <summary>
        /// Internal numeric state ID displayed in Inspector for reference.
        /// Format: "{GameState}_{FearType}_{DreamLevel}" as integers.
        /// Example: "3_1_2" = Dream(3) + Fear_A(1) + Level2(2)
        /// </summary>
        [BoxGroup("Identity")]
        [ReadOnly]
        [SerializeField] private string _stateID;

        /// <summary>
        /// Human-readable state ID displayed in Inspector for debugging.
        /// Read-only — derived from FearType and DreamLevel.
        /// </summary>
        [BoxGroup("Identity")]
        [ReadOnly]
        [SerializeField] private string _stateIDReadable;

        /// <summary>Internal numeric StateID used by all systems for fast lookup.</summary>
        public string StateID         => $"{(int)Core.GameState.Dream}_{(int)_fearType}_{(int)_dreamLevel}";

        /// <summary>Human-readable StateID for logs and debugging.</summary>
        public string StateIDReadable => $"{Core.GameState.Dream}_{_fearType}_{_dreamLevel}";

        /// <summary>Public accessor for the configured FearType.</summary>
        public Core.FearType FearType     => _fearType;

        /// <summary>Public accessor for the configured DreamLevel.</summary>
        public Core.DreamLevel DreamLevel => _dreamLevel;

        #endregion

        #region Level Loading

        [BoxGroup("Level Loading")]
        [InfoBox("Provide either a Scene Name or a Level Prefab — not both.")]
        [Scene]
        [SerializeField] private string _sceneName;

        [BoxGroup("Level Loading")]
        [SerializeField] private GameObject _levelPrefab;

        /// <summary>Name of the scene to load for this level. Leave empty if using a prefab.</summary>
        public string SceneName       => _sceneName;

        /// <summary>Level prefab to spawn. Leave empty if using a scene.</summary>
        public GameObject LevelPrefab => _levelPrefab;

        #endregion

        #region Visual

        [BoxGroup("Visual")]
        [SerializeField] private VolumeProfile _volumeProfile;

        [BoxGroup("Visual")]
        [Range(0f, 1f)]
        [SerializeField] private float _kuwaharaIntensity = 0.5f;

        [BoxGroup("Visual")]
        [SerializeField] private GameObject _ambientVFXPrefab;

        /// <summary>URP Volume Profile defining the post-processing state for this level.</summary>
        public VolumeProfile VolumeProfile => _volumeProfile;

        /// <summary>Intensity of the Kuwahara painterly shader effect. Range: 0–1.</summary>
        public float KuwaharaIntensity     => _kuwaharaIntensity;

        /// <summary>Ambient VFX prefab spawned on level start (e.g. dust particles, fog).</summary>
        public GameObject AmbientVFXPrefab => _ambientVFXPrefab;

        #endregion

        #region Audio

        [BoxGroup("Audio")]
        [SerializeField] private AudioMixerSnapshot _audioSnapshot;

        [BoxGroup("Audio")]
        [SerializeField] private AudioClip _musicClip;

        [BoxGroup("Audio")]
        [SerializeField] private AudioClip[] _ambientClips;

        /// <summary>Audio Mixer Snapshot to transition to on level start.</summary>
        public AudioMixerSnapshot AudioSnapshot => _audioSnapshot;

        /// <summary>Background music clip for this level.</summary>
        public AudioClip MusicClip             => _musicClip;

        /// <summary>Ambient audio clips looped during this level.</summary>
        public AudioClip[] AmbientClips        => _ambientClips;

        #endregion

        #region Gameplay

        [BoxGroup("Gameplay")]
        [SerializeField] private Core.PillEffectType _pillEffectType;

        [FormerlySerializedAs("_startPillsCount")]
        [BoxGroup("Gameplay")]
        [MinValue(0)]
        [SerializeField] private int _startPillsCount = 3;

        [BoxGroup("Gameplay")]
        [MinValue(0f)]
        [SerializeField] private float _pillDuration = 15f;

        /// <summary>
        /// The perceptual effect triggered when the player consumes a pill.
        /// Determines which fear-specific visual response FearXVFXController activates.
        /// </summary>
        public Core.PillEffectType PillEffectType => _pillEffectType;

        /// <summary>Number of pills the player starts with at the beginning of this level.</summary>
        public int StartPillsCount       => _startPillsCount;

        /// <summary>Duration in seconds of a single pill effect.</summary>
        public float PillDuration  => _pillDuration;

        #endregion

        #region Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep read-only Inspector fields in sync with actual computed values
            _stateID         = StateID;
            _stateIDReadable = StateIDReadable;

            if (_fearType == Core.FearType.None)
                Debug.LogWarning($"[FearProfileSO] {name}: FearType is None — StateID will not match any game state.");

            if (_dreamLevel == Core.DreamLevel.None)
                Debug.LogWarning($"[FearProfileSO] {name}: DreamLevel is None — StateID will not match any game state.");

            if (_pillEffectType == Core.PillEffectType.None)
                Debug.LogWarning($"[FearProfileSO] {name}: PillEffectType is None — pill consumption will have no visual effect.");

            if (!string.IsNullOrEmpty(_sceneName) && _levelPrefab != null)
                Debug.LogWarning($"[FearProfileSO] {name}: Both SceneName and LevelPrefab are set — only one should be used.");
        }
#endif

        #endregion
    }
}
