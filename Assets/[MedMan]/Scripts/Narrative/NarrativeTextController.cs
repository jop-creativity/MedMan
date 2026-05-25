using System.Collections;
using UnityEngine;
using TMPro;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Narrative
{
    /// <summary>
    /// Displays a DialogueLineSO as animated world-space TMP text.
    /// Attach to a GameObject with a TextMeshPro component in the scene.
    /// All animation parameters are public and tweakable in the Inspector.
    /// Supports Edit Mode preview via [Button] — no Play Mode required for iteration.
    /// Notifies DialogueSystem when display is complete via EventBus.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshPro))]
    public class NarrativeTextController : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Content")]
        [Tooltip("Dialogue line to display. Used for both runtime playback and Editor preview.")]
        [SerializeField] private DialogueLineSO _previewLine;

        [BoxGroup("Animation")]
        [Tooltip("Seconds per character when typing out the text.")]
        [SerializeField] private float _charDelay = 0.04f;

        [BoxGroup("Animation")]
        [Tooltip("How long the text remains fully visible before fading out. Set to 0 to persist until manually cleared.")]
        [SerializeField] private float _displayDuration = 3f;

        [BoxGroup("Animation")]
        [Tooltip("Duration of the fade-in and fade-out in seconds.")]
        [SerializeField] private float _fadeDuration = 0.4f;

        [BoxGroup("Animation")]
        [Tooltip("If true, text persists until ClearText() is called. Overrides Display Duration.")]
        [SerializeField] private bool _persistUntilCleared;

        [BoxGroup("Style")]
        [SerializeField] private Color _textColor = Color.white;

        [BoxGroup("Style")]
        [SerializeField] private float _fontSize = 0.2f;
        
        [BoxGroup("Audio")]
        [Tooltip("Enable to play an audio clip alongside this text. Defaults to the clip set in the assigned DialogueLineSO.")]
        [SerializeField] private bool _hasAudio;

        [BoxGroup("Audio")]
        [ShowIf("_hasAudio")]
        [Tooltip("Audio clip to play with this text. Pre-filled from DialogueLineSO on enable — override freely per controller instance.")]
        [SerializeField] private AudioClip _audioClip;

        private TextMeshPro _tmp;
        private Coroutine _displayCoroutine;
        private bool _isDisplaying;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        private void Awake()
        {
            _tmp = GetComponent<TextMeshPro>();
            ApplyStyle();

            // Hide text on start — only show when triggered
            if (Application.isPlaying)
                SetAlpha(0f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnDialogueLineStartedEvent>(HandleLineStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnDialogueLineStartedEvent>(HandleLineStarted);
        }

        // ─────────────────────────────────────────
        // Public methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Displays the given dialogue line — fetches localized string and animates text.
        /// Publishes OnDialogueLineEndedEvent when complete.
        /// </summary>
        public void DisplayLine(DialogueLineSO line)
        {
            if (line == null) return;

            if (_displayCoroutine != null)
                StopCoroutine(_displayCoroutine);

            _displayCoroutine = StartCoroutine(DisplayRoutine(line));
        }

        /// <summary>
        /// Immediately clears the text, resets alpha and stops any playing preview audio.
        /// Use for scene transitions or forced interruptions.
        /// </summary>
        public void ClearText()
        {
            if (_displayCoroutine != null)
                StopCoroutine(_displayCoroutine);

            _tmp.text     = string.Empty;
            _isDisplaying = false;
            SetAlpha(0f);

#if UNITY_EDITOR
            if (_hasAudio && _audioClip != null)
            {
                var unityEditorAssembly = typeof(UnityEditor.AudioImporter).Assembly;
                var audioUtilClass      = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                var stopMethod          = audioUtilClass.GetMethod(
                    "StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
                );
                stopMethod?.Invoke(null, null);
            }
#endif
        }

        // ─────────────────────────────────────────
        // Editor preview
        // ─────────────────────────────────────────

        /// <summary>
        /// Previews the assigned _previewLine in both Edit Mode and Play Mode.
        /// Fetches the localized string synchronously for instant Editor feedback.
        /// </summary>
        [Button("Preview Text")]
        private void PreviewText()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                var stringRef   = _previewLine.LocalizedText;
                var activeLocale = UnityEditor.Localization.LocalizationEditorSettings
                    .ActiveLocalizationSettings.GetSelectedLocale();
                if (activeLocale == null)
                {
                    Debug.LogWarning("[NarrativeTextController] No active locale — set one in Window → Asset Management → Localization Scene Controls.");
                    return;
                }

                var collection = UnityEditor.Localization.LocalizationEditorSettings
                    .GetStringTableCollection(stringRef.TableReference.TableCollectionNameGuid);
                var table = collection?.GetTable(activeLocale.Identifier) 
                    as UnityEngine.Localization.Tables.StringTable;
                var entry = table?.GetEntry(stringRef.TableEntryReference.KeyId);

                string text    = entry?.LocalizedValue ?? "[Missing Key]";
                _tmp.text      = text;
                _tmp.color     = _textColor;
                Debug.Log($"[NarrativeTextController] Preview: {text}");
                
                // Preview audio alongside text in Edit Mode
                if (_hasAudio && _audioClip != null)
                {
                    var unityEditorAssembly = typeof(UnityEditor.AudioImporter).Assembly;
                    var audioUtilClass      = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                    var playMethod          = audioUtilClass.GetMethod(
                        "PlayPreviewClip",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                        null,
                        new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                        null
                    );
                    playMethod?.Invoke(null, new object[] { _audioClip, 0, false });
                }
#endif
            }
        }

        /// <summary>
        /// Clears the preview text and stops audio from the Inspector button.
        /// </summary>
        [Button("Clear Text")]
        private void ClearTextButton()
        {
            if (_tmp == null)
                _tmp = GetComponent<TextMeshPro>();

            ClearText();
            Debug.Log("[NarrativeTextController] Text cleared.");
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Coroutine that fetches localized text, types it out character by character,
        /// holds for display duration, then fades out.
        /// Notifies DialogueSystem when complete.
        /// </summary>
        private IEnumerator DisplayRoutine(DialogueLineSO line)
        {
            _isDisplaying = true;

            // Fetch localized string async
            string text = string.Empty;
            var op = line.LocalizedText.GetLocalizedStringAsync();
            yield return op;
            text = op.Result;

            // Play audio if enabled — uses Controller clip (may be overridden from SO default)
            if (_hasAudio && _audioClip != null)
                AudioSource.PlayClipAtPoint(_audioClip, transform.position, 1f);

            // Fade in
            yield return StartCoroutine(FadeRoutine(0f, 1f, _fadeDuration));

            // Typewriter
            _tmp.text = string.Empty;
            foreach (char c in text)
            {
                _tmp.text += c;
                yield return new WaitForSeconds(_charDelay);
            }

            // Hold
            if (!_persistUntilCleared && _displayDuration > 0f)
            {
                yield return new WaitForSeconds(_displayDuration);

                // Fade out
                yield return StartCoroutine(FadeRoutine(1f, 0f, _fadeDuration));
                _tmp.text = string.Empty;
            }

            _isDisplaying = false;

            // Notify DialogueSystem
            EventBus.Publish(new OnDialogueLineEndedEvent(line));
            Debug.Log($"[NarrativeTextController] Line ended: {line.name}");
        }

        /// <summary>
        /// Fades text alpha from startAlpha to endAlpha over duration seconds.
        /// </summary>
        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, endAlpha, elapsed / duration));
                yield return null;
            }
            SetAlpha(endAlpha);
        }

        /// <summary>
        /// Sets TMP vertex color alpha without affecting the color property directly.
        /// </summary>
        private void SetAlpha(float alpha)
        {
            if (_tmp == null) return;
            Color c  = _tmp.color;
            c.a      = alpha;
            _tmp.color = c;
        }

        /// <summary>
        /// Applies Inspector style settings to the TMP component.
        /// Called on Awake and before preview.
        /// </summary>
        private void ApplyStyle()
        {
            if (_tmp == null) return;
            _tmp.color    = _textColor;
            _tmp.fontSize = _fontSize;
        }

        /// <summary>
        /// Receives dialogue line started event and displays the line
        /// if this controller is the active one in the scene.
        /// </summary>
        private void HandleLineStarted(OnDialogueLineStartedEvent e)
        {
            DisplayLine(e.Line);
        }
        
        /// <summary>
        /// Pre-fills _audioClip from the assigned DialogueLineSO when _hasAudio is enabled
        /// and no clip has been manually assigned yet.
        /// Allows per-controller override without modifying the source SO.
        /// </summary>
        private void OnValidate()
        {
            if (_hasAudio && _audioClip == null && _previewLine != null && _previewLine.HasAudio)
                _audioClip = _previewLine.AudioClip;
        }
    }
}