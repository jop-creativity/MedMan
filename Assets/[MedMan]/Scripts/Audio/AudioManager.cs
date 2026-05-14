using UnityEngine;
using UnityEngine.Audio;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Audio
{
    /// <summary>
    /// Manages all audio in Med Man.
    /// Handles Audio Mixer Snapshot transitions, volume control,
    /// SFX playback, music and ambient audio.
    ///
    /// Required setup:
    ///   Assign MedMan_AudioMixer and all Snapshots in the Inspector.
    ///   AudioSource components are created automatically on Awake.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Singleton
        // ─────────────────────────────────────────

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            SubscribeToEvents();
            Debug.Log("[AudioManager] Initialized.");
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        // ─────────────────────────────────────────
        // Inspector references
        // ─────────────────────────────────────────

        [BoxGroup("Audio Mixer")]
        [Required]
        [SerializeField] private AudioMixer _audioMixer;

        [BoxGroup("Snapshots")]
        [Required]
        [SerializeField] private AudioMixerSnapshot _snapshotDefault;

        [BoxGroup("Snapshots")]
        [Required]
        [SerializeField] private AudioMixerSnapshot _snapshotMainMenu;

        [BoxGroup("Snapshots")]
        [Required]
        [SerializeField] private AudioMixerSnapshot _snapshotHotelRoom;

        [BoxGroup("Snapshots")]
        [Required]
        [SerializeField] private AudioMixerSnapshot _snapshotDream;

        [BoxGroup("Snapshots")]
        [Required]
        [SerializeField] private AudioMixerSnapshot _snapshotPillActive;

        [BoxGroup("Transition")]
        [SerializeField] private float _snapshotTransitionTime = 1.5f;

        // ─────────────────────────────────────────
        // Audio Sources
        // Created automatically — one per channel
        // ─────────────────────────────────────────

        private AudioSource _musicSource;
        private AudioSource _ambientSource;
        private AudioSource _sfxSource;

        private void InitializeAudioSources()
        {
            _musicSource  = CreateAudioSource("MusicSource",  "Music");
            _ambientSource = CreateAudioSource("AmbientSource", "Ambient");
            _sfxSource    = CreateAudioSource("SFXSource",    "SFX");
        }

        private AudioSource CreateAudioSource(string objectName, string mixerGroupName)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop        = false;

            if (_audioMixer != null)
            {
                AudioMixerGroup[] groups = _audioMixer.FindMatchingGroups(mixerGroupName);
                if (groups.Length > 0)
                    source.outputAudioMixerGroup = groups[0];
                else
                    Debug.LogWarning($"[AudioManager] Mixer group '{mixerGroupName}' not found.");
            }

            return source;
        }

        // ─────────────────────────────────────────
        // Event subscriptions
        // ─────────────────────────────────────────

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<OnGameStateChangedEvent>(HandleGameStateChanged);
            EventBus.Subscribe<OnLevelInitializedEvent>(HandleLevelInitialized);
            EventBus.Subscribe<OnPillConsumedEvent>(HandlePillConsumed);
            EventBus.Subscribe<OnPillExpiredEvent>(HandlePillExpired);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<OnGameStateChangedEvent>(HandleGameStateChanged);
            EventBus.Unsubscribe<OnLevelInitializedEvent>(HandleLevelInitialized);
            EventBus.Unsubscribe<OnPillConsumedEvent>(HandlePillConsumed);
            EventBus.Unsubscribe<OnPillExpiredEvent>(HandlePillExpired);
        }

        // ─────────────────────────────────────────
        // Snapshot transitions
        // ─────────────────────────────────────────

        /// <summary>
        /// Transitions to the Audio Mixer Snapshot matching the current game state.
        /// </summary>
        public void TransitionToSnapshot(AudioMixerSnapshot snapshot, float transitionTime = -1f)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[AudioManager] Snapshot is null — skipping transition.");
                return;
            }

            float duration = transitionTime < 0f ? _snapshotTransitionTime : transitionTime;
            snapshot.TransitionTo(duration);
            Debug.Log($"[AudioManager] Transitioning to snapshot: {snapshot.name} over {duration}s");
        }

        private AudioMixerSnapshot GetSnapshotForState(GameState state)
        {
            return state switch
            {
                GameState.MainMenu     => _snapshotMainMenu,
                GameState.HotelRoom    => _snapshotHotelRoom,
                GameState.DoctorsOffice => _snapshotHotelRoom,
                GameState.Dream        => _snapshotDream,
                GameState.Epilogue     => _snapshotHotelRoom,
                _                      => _snapshotDefault
            };
        }

        // ─────────────────────────────────────────
        // Volume control
        // ─────────────────────────────────────────

        /// <summary>
        /// Sets the volume of a mixer group using its exposed parameter name.
        /// Value range: 0–1 (converted internally to dB).
        /// </summary>
        public void SetVolume(string exposedParameter, float normalizedValue)
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);

            // Convert 0-1 range to dB (-80 to 0)
            float dB = normalizedValue > 0.0001f
                ? Mathf.Log10(normalizedValue) * 20f
                : -80f;

            _audioMixer.SetFloat(exposedParameter, dB);
        }

        /// <summary>Gets the current normalized volume (0-1) of a mixer group.</summary>
        public float GetVolume(string exposedParameter)
        {
            if (_audioMixer.GetFloat(exposedParameter, out float dB))
                return Mathf.Pow(10f, dB / 20f);

            return 1f;
        }

        // ─────────────────────────────────────────
        // Playback — Music
        // ─────────────────────────────────────────

        /// <summary>Plays a music clip. Stops any currently playing music.</summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;

            _musicSource.loop = loop;
            _musicSource.clip = clip;
            _musicSource.Play();
        }

        /// <summary>Stops music playback.</summary>
        public void StopMusic() => _musicSource.Stop();

        // ─────────────────────────────────────────
        // Playback — Ambient
        // ─────────────────────────────────────────

        /// <summary>Plays an ambient clip on loop.</summary>
        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null) return;

            _ambientSource.loop = true;
            _ambientSource.clip = clip;
            _ambientSource.Play();
        }

        /// <summary>Stops ambient playback.</summary>
        public void StopAmbient() => _ambientSource.Stop();

        // ─────────────────────────────────────────
        // Playback — SFX
        // ─────────────────────────────────────────

        /// <summary>
        /// Plays a one-shot SFX clip.
        /// Use for short sounds that don't need to be stopped mid-play.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, volumeScale);
        }

        // ─────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────

        private void HandleGameStateChanged(OnGameStateChangedEvent e)
        {
            AudioMixerSnapshot snapshot = GetSnapshotForState(e.NewState);
            TransitionToSnapshot(snapshot);
        }

        private void HandleLevelInitialized(OnLevelInitializedEvent e)
        {
            // TODO CS-03: load audio config from FearProfileSO for e.StateID
            // FearProfileSO profile = ... find by e.StateID
            // PlayMusic(profile.MusicClip);
            // PlayAmbient(profile.AmbientClips[0]);

            EventBus.Publish(new OnSystemReadyEvent("AudioManager"));
        }

        private void HandlePillConsumed(OnPillConsumedEvent e)
        {
            // Pill active — transition to brighter audio state
            TransitionToSnapshot(_snapshotPillActive, 0.5f);
        }

        private void HandlePillExpired(OnPillExpiredEvent e)
        {
            // Pill worn off — return to dream audio state
            TransitionToSnapshot(_snapshotDream, 2f);
        }
    }
}
