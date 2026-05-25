using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Audio;
using NaughtyAttributes;

namespace MedMan.Narrative
{
    /// <summary>
    /// Data container for a single dialogue line.
    /// Stores a localized string key and an optional audio clip.
    /// Animation and display parameters live in NarrativeTextController — not here.
    /// Naming convention: DialogueLine_[Scene]_[Index] e.g. DialogueLine_DoctorOffice_01
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueLine_New", menuName = "MedMan/Dialogue Line")]
    public class DialogueLineSO : ScriptableObject
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Content")]
        [Tooltip("Localized string reference — set up via Unity Localization String Table.")]
        [SerializeField] private LocalizedString _localizedText;

        [BoxGroup("Audio")]
        [Tooltip("Enable to attach a one-shot audio clip to this dialogue line.")]
        [SerializeField] private bool _hasAudio;

        [BoxGroup("Audio")]
        [ShowIf("_hasAudio")]
        [SerializeField] private AudioClip _audioClip;

        [BoxGroup("Audio")]
        [ShowIf("_hasAudio")]
        [Range(0f, 1f)]
        [SerializeField] private float _audioVolume = 1f;

        // ─────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────

        /// <summary>Localized string reference for this dialogue line.</summary>
        public LocalizedString LocalizedText => _localizedText;

        /// <summary>Whether this line has an accompanying audio clip.</summary>
        public bool HasAudio => _hasAudio;

        /// <summary>Optional one-shot audio clip played alongside the text.</summary>
        public AudioClip AudioClip => _audioClip;

        /// <summary>Volume for the optional audio clip. Range: 0-1.</summary>
        public float AudioVolume => _audioVolume;
    }
}