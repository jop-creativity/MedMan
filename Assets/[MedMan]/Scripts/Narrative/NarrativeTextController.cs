using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Narrative
{
    /// <summary>
    /// Displays a DialogueLineSO as animated world-space TMP text.
    /// Supports multiple simultaneous per-character animations via TextAnimationType flags.
    /// Can be triggered by zone entry (OnTriggerEnter) or called directly by DialogueSystem.
    /// All animation parameters are public and tweakable in the Inspector.
    /// Supports Edit Mode preview via [Button].
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshPro))]
    public class NarrativeTextController : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Enums
        // ─────────────────────────────────────────

        /// <summary>
        /// Flags enum — multiple animation types can be active simultaneously.
        /// Combine in Inspector to layer effects.
        /// </summary>
        [Flags]
        public enum TextAnimationType
        {
            None           = 0,
            Typewriter     = 1 << 0,  // Letters appear one by one
            FallingLetters = 1 << 1,  // Letters drop from above into position
            ElectricShock  = 1 << 2,  // Letters snap into view with a jolt
            Tremor         = 1 << 3,  // Continuous subtle shaking per letter
        }

        // ─────────────────────────────────────────
        // Fields — Content
        // ─────────────────────────────────────────

        [BoxGroup("Content")]
        [Tooltip("Dialogue line to display. Used for both runtime playback and Editor preview.")]
        [SerializeField] private DialogueLineSO _previewLine;

        // ─────────────────────────────────────────
        // Fields — Zone Trigger
        // ─────────────────────────────────────────

        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, this controller activates when the player enters the attached trigger collider.")]
        [SerializeField] private bool _activateOnTriggerEnter = true;

        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, text clears when the player exits the trigger collider.")]
        [SerializeField] private bool _clearOnTriggerExit;

        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, trigger fires only once and disables itself after.")]
        [SerializeField] private bool _triggerOnce = true;
        
        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, text persists until ClearText() is called. Overrides Display Duration.")]
        [SerializeField] private bool _persistUntilCleared;

        // ─────────────────────────────────────────
        // Fields — Animation Type
        // ─────────────────────────────────────────

        [BoxGroup("Animation Type")]
        [Tooltip("Select one or more animation effects to apply simultaneously.")]
        [SerializeField] private TextAnimationType _animationType = TextAnimationType.FallingLetters;

        // ─────────────────────────────────────────
        // Fields — Typewriter
        // ─────────────────────────────────────────

        [BoxGroup("Typewriter")]
        [Tooltip("Seconds per character when typing out the text.")]
        [SerializeField] private float _charDelay = 0.04f;

        [BoxGroup("Typewriter")]
        [Tooltip("How long the text remains fully visible. 0 = persist until cleared.")]
        [SerializeField] private float _displayDuration = 3f;

        [BoxGroup("Typewriter")]
        [Tooltip("Duration of the fade-in and fade-out.")]
        [SerializeField] private float _fadeDuration = 0.4f;

        // ─────────────────────────────────────────
        // Fields — Falling Letters
        // ─────────────────────────────────────────

        [BoxGroup("Falling Letters")]
        [Tooltip("Distance above final position each letter starts from.")]
        [SerializeField] private float _fallDistance = 30f;

        [BoxGroup("Falling Letters")]
        [Tooltip("Time in seconds for each letter to fall into position.")]
        [SerializeField] private float _fallDuration = 0.3f;

        [BoxGroup("Falling Letters")]
        [Tooltip("Delay between each letter starting to fall.")]
        [SerializeField] private float _fallStagger = 0.05f;

        // ─────────────────────────────────────────
        // Fields — Electric Shock
        // ─────────────────────────────────────────

        [BoxGroup("Electric Shock")]
        [Tooltip("Maximum random positional offset at shock peak.")]
        [SerializeField] private float _shockOffsetMax = 8f;

        [BoxGroup("Electric Shock")]
        [Tooltip("Number of shock jolt iterations before settling.")]
        [SerializeField] private int _shockIterations = 4;

        [BoxGroup("Electric Shock")]
        [Tooltip("Time between each shock jolt.")]
        [SerializeField] private float _shockInterval = 0.04f;

        [BoxGroup("Electric Shock")]
        [Tooltip("Time to settle back to zero after final jolt.")]
        [SerializeField] private float _shockSettleDuration = 0.1f;

        // ─────────────────────────────────────────
        // Fields — Tremor
        // ─────────────────────────────────────────

        [BoxGroup("Tremor")]
        [Tooltip("Maximum positional offset per vertex during tremor.")]
        [SerializeField] private float _tremorStrength = 1.5f;

        [BoxGroup("Tremor")]
        [Tooltip("Speed of tremor oscillation. Higher = faster shaking.")]
        [SerializeField] private float _tremorSpeed = 12f;

        [BoxGroup("Tremor")]
        [Tooltip("Per-letter phase offset for organic feel. Higher = more chaotic.")]
        [SerializeField] private float _tremorPhaseSpread = 2.5f;
        
        [BoxGroup("Tremor")]
        [Tooltip("Duration of tremor fade-in. 0 = instant.")]
        [SerializeField] private float _tremorFadeInDuration = 0.5f;

        // ─────────────────────────────────────────
        // Fields — Style
        // ─────────────────────────────────────────

        [BoxGroup("Style")]
        [SerializeField] private Color _textColor = Color.white;

        [BoxGroup("Style")]
        [SerializeField] private float _fontSize = 0.2f;

        // ─────────────────────────────────────────
        // Fields — Audio
        // ─────────────────────────────────────────

        [BoxGroup("Audio")]
        [Tooltip("Enable to play an audio clip alongside this text.")]
        [SerializeField] private bool _hasAudio;

        [BoxGroup("Audio")]
        [ShowIf("_hasAudio")]
        [Tooltip("Audio clip to play. Pre-filled from DialogueLineSO on enable.")]
        [SerializeField] private AudioClip _audioClip;

        // ─────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────

        private TextMeshPro _tmp;
        private Coroutine   _displayCoroutine;
        private Coroutine   _tremorCoroutine;
        private bool        _isDisplaying;
        private bool        _triggered;
        private AudioSource _audioSource;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        private void Awake()
        {
            _tmp = GetComponent<TextMeshPro>();
            ApplyStyle();

            if (Application.isPlaying)
                SetAlpha(0f);
            
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        /// <summary>Subscribes to dialogue line started event.</summary>
        private void OnEnable()
        {
            EventBus.Subscribe<OnDialogueLineStartedEvent>(HandleLineStarted);
        }

        /// <summary>Unsubscribes from dialogue line started event.</summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnDialogueLineStartedEvent>(HandleLineStarted);
        }

        private void OnValidate()
        {
            if (_hasAudio && _audioClip == null && _previewLine != null && _previewLine.HasAudio)
                _audioClip = _previewLine.AudioClip;
        }

        // ─────────────────────────────────────────
        // Zone Trigger
        // ─────────────────────────────────────────

        /// <summary>
        /// Activates text display when the player enters the trigger collider.
        /// Fires only once if _triggerOnce is enabled.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!_activateOnTriggerEnter) return;
            if (_triggerOnce && _triggered) return;
            if (!other.CompareTag("Player")) return;

            _triggered = true;
            DisplayLine(_previewLine);
        }

        /// <summary>
        /// Clears text when the player exits the trigger collider, if enabled.
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (!_clearOnTriggerExit) return;
            if (!other.CompareTag("Player")) return;

            ClearText();
        }

        // ─────────────────────────────────────────
        // Public methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Displays the given dialogue line with the configured animation type.
        /// Publishes OnDialogueLineEndedEvent when complete.
        /// </summary>
        public void DisplayLine(DialogueLineSO line)
        {
            if (line == null) return;

            if (_displayCoroutine != null) StopCoroutine(_displayCoroutine);
            if (_tremorCoroutine  != null) StopCoroutine(_tremorCoroutine);

            _displayCoroutine = StartCoroutine(DisplayRoutine(line));
        }

        /// <summary>
        /// Immediately clears the text, resets alpha, stops animations and any playing audio.
        /// </summary>
        public void ClearText()
        {
            if (_displayCoroutine != null) StopCoroutine(_displayCoroutine);
            if (_tremorCoroutine  != null) StopCoroutine(_tremorCoroutine);

            _tmp.text     = string.Empty;
            _isDisplaying = false;
            SetAlpha(0f);

            // Stop runtime audio
            if (_hasAudio && _audioSource != null)
                _audioSource.Stop();

#if UNITY_EDITOR
            // Stop editor preview audio
            if (_hasAudio && _audioClip != null)
            {
                var assembly   = typeof(UnityEditor.AudioImporter).Assembly;
                var audioUtil  = assembly.GetType("UnityEditor.AudioUtil");
                var stopMethod = audioUtil.GetMethod("StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                stopMethod?.Invoke(null, null);
            }
#endif
        }

        // ─────────────────────────────────────────
        // Editor preview
        // ─────────────────────────────────────────

        /// <summary>
        /// Previews the assigned dialogue line in both Edit Mode and Play Mode.
        /// </summary>
        [Button("Preview Text (Animated only on PlayMode)")]
        private void PreviewText()
        {
            //Always clear last setup before setting up new preview
            ClearText();
            
            if (_previewLine == null)
            {
                Debug.LogWarning("[NarrativeTextController] No preview line assigned.");
                return;
            }

            if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
            ApplyStyle();

            if (!Application.isPlaying)
            {
                #if UNITY_EDITOR
                var stringRef  = _previewLine.LocalizedText;
                var activeLocale = UnityEditor.Localization.LocalizationEditorSettings
                    .ActiveLocalizationSettings.GetSelectedLocale();
                if (activeLocale == null)
                {
                    Debug.LogWarning("[NarrativeTextController] No active locale set.");
                    return;
                }

                var collection = UnityEditor.Localization.LocalizationEditorSettings
                    .GetStringTableCollection(stringRef.TableReference.TableCollectionNameGuid);
                var table = collection?.GetTable(activeLocale.Identifier)
                    as UnityEngine.Localization.Tables.StringTable;
                var entry = table?.GetEntry(stringRef.TableEntryReference.KeyId);

                string text = entry?.LocalizedValue ?? "[Missing Key]";
                _tmp.text   = text;
                _tmp.color  = _textColor;

                if (_hasAudio && _audioClip != null)
                {
                    var assembly   = typeof(UnityEditor.AudioImporter).Assembly;
                    var audioUtil  = assembly.GetType("UnityEditor.AudioUtil");
                    var playMethod = audioUtil.GetMethod("PlayPreviewClip",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                        null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                    playMethod?.Invoke(null, new object[] { _audioClip, 0, false });
                }

                Debug.Log($"[NarrativeTextController] Preview: {text}");
                #endif
            }
            else
            {
                DisplayLine(_previewLine);
            }
        }

        /// <summary>Clears the preview text from the Inspector button.</summary>
        [Button("Clear Text")]
        private void ClearTextButton()
        {
            if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
            ClearText();
            Debug.Log("[NarrativeTextController] Text cleared.");
        }

        // ─────────────────────────────────────────
        // Private methods — Display
        // ─────────────────────────────────────────

        /// <summary>
        /// Fetches localized string, types it out, applies animations,
        /// holds for display duration, then fades out.
        /// </summary>
        private IEnumerator DisplayRoutine(DialogueLineSO line)
        {
            _isDisplaying = true;

            // Fetch localized string
            string text = string.Empty;
            var op = line.LocalizedText.GetLocalizedStringAsync();
            yield return op;
            text = op.Result;

            if (_hasAudio && _audioClip != null)
            {
                _audioSource.clip = _audioClip;
                _audioSource.Play();
            }

            // Reveal text — Typewriter or FallingLetters (mutually exclusive reveal methods)
            if ((_animationType & TextAnimationType.FallingLetters) != 0)
            {
                // Set full text but hide all letters initially via vertex alpha
                _tmp.color = new Color(_textColor.r, _textColor.g, _textColor.b, 0f);
                _tmp.text = text;
                _tmp.ForceMeshUpdate();
                yield return null; // Wait one frame for vertex data to apply

                if ((_animationType & TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(ElectricShockRoutine());

                if ((_animationType & TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(TremorRoutine());

                yield return StartCoroutine(FallingLettersRoutine());
            }
            else if ((_animationType & TextAnimationType.Typewriter) != 0)
            {
                yield return StartCoroutine(FadeRoutine(0f, 1f, _fadeDuration));
                
                // ElectricShock and Tremor start immediately alongside typewriter
                if ((_animationType & TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(ElectricShockRoutine());

                if ((_animationType & TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(TremorRoutine());

                _tmp.text = string.Empty;
                foreach (char c in text)
                {
                    _tmp.text += c;
                    yield return new WaitForSeconds(_charDelay);
                }
            }
            else
            {
                yield return StartCoroutine(FadeRoutine(0f, 1f, _fadeDuration));

                // No reveal effect — text appears instantly
                _tmp.text = text;
                _tmp.ForceMeshUpdate();

                if ((_animationType & TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(ElectricShockRoutine());

                if ((_animationType & TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(TremorRoutine());
            }

            // Hold
            if (!_persistUntilCleared && _displayDuration > 0f)
            {
                yield return new WaitForSeconds(_displayDuration);

                if (_tremorCoroutine != null)
                {
                    StopCoroutine(_tremorCoroutine);
                    _tremorCoroutine = null;
                }

                yield return StartCoroutine(FadeRoutine(1f, 0f, _fadeDuration));
                _tmp.text = string.Empty;
            }

            _isDisplaying = false;
            EventBus.Publish(new OnDialogueLineEndedEvent(line));
        }

        // ─────────────────────────────────────────
        // Private methods — Animations
        // ─────────────────────────────────────────

        /// <summary>
        /// Animates each letter falling from above into its final position.
        /// Letters are staggered by _fallStagger seconds for a cascade effect.
        /// </summary>
        private IEnumerator FallingLettersRoutine()
        {
            _tmp.ForceMeshUpdate();
            TMP_TextInfo textInfo = _tmp.textInfo;
            int charCount = textInfo.characterCount;

            float[] timers    = new float[charCount];
            bool[]  animating = new bool[charCount];

            for (int i = 0; i < charCount; i++)
            {
                timers[i]    = -i * _fallStagger;
                animating[i] = true;
            }

            bool anyAnimating = true;
            
            // Cache original vertex positions before animation loop
            Vector3[][] originalVertices = new Vector3[textInfo.meshInfo.Length][];
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                originalVertices[m] = (Vector3[])textInfo.meshInfo[m].vertices.Clone();
            }
            
            while (anyAnimating)
            {
                anyAnimating = false;

                for (int i = 0; i < charCount; i++)
                {
                    if (!animating[i]) continue;

                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) { animating[i] = false; continue; }

                    timers[i] += Time.deltaTime;

                    if (timers[i] < 0f) { anyAnimating = true; continue; }

                    float t = Mathf.Clamp01(timers[i] / _fallDuration);
                    // Ease out cubic
                    float ease   = 1f - Mathf.Pow(1f - t, 3f);
                    float offset = Mathf.Lerp(_fallDistance, 0f, ease);

                    int meshIndex  = charInfo.materialReferenceIndex;
                    int vertIndex  = charInfo.vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;

                    for (int v = 0; v < 4; v++)
                        verts[vertIndex + v].y = originalVertices[meshIndex][vertIndex + v].y + offset * _tmp.fontSize * 0.01f;
                    
                    // Fade in letter as it falls
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
                    byte alpha = (byte)(Mathf.Clamp01(ease) * 255);
                    for (int v = 0; v < 4; v++)
                        colors[vertIndex + v].a = alpha;

                    if (t >= 1f) animating[i] = false;
                    else         anyAnimating  = true;
                }

                for (int m = 0; m < textInfo.meshInfo.Length; m++)
                {
                    _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
                }

                yield return null;
            }
        }

        /// <summary>
        /// Snaps each letter into view with a rapid positional jolt,
        /// simulating an electric shock. Each letter jolts independently.
        /// </summary>
        private IEnumerator ElectricShockRoutine()
        {
            _tmp.ForceMeshUpdate();
            TMP_TextInfo textInfo = _tmp.textInfo;
            int charCount = textInfo.characterCount;

            for (int iter = 0; iter < _shockIterations; iter++)
            {
                _tmp.ForceMeshUpdate();
                textInfo = _tmp.textInfo;

                for (int i = 0; i < charCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    float  offsetX = UnityEngine.Random.Range(-_shockOffsetMax, _shockOffsetMax);
                    float  offsetY = UnityEngine.Random.Range(-_shockOffsetMax, _shockOffsetMax);
                    int    mesh    = charInfo.materialReferenceIndex;
                    int    vert    = charInfo.vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[mesh].vertices;

                    for (int v = 0; v < 4; v++)
                    {
                        verts[vert + v].x += offsetX * _tmp.fontSize * 0.001f;
                        verts[vert + v].y += offsetY * _tmp.fontSize * 0.001f;
                    }
                }

                _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                yield return new WaitForSeconds(_shockInterval);
            }

            // Settle — lerp back to zero over _shockSettleDuration
            float elapsed = 0f;
            while (elapsed < _shockSettleDuration)
            {
                elapsed += Time.deltaTime;
                _tmp.ForceMeshUpdate();
                _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                yield return null;
            }
        }

        /// <summary>
        /// Continuously shakes each letter with a per-character phase offset.
        /// Fades in via vertex alpha over _tremorFadeInDuration for a smooth onset.
        /// Runs until stopped externally.
        /// </summary>
        private IEnumerator TremorRoutine()
        {
            float elapsed = 0f;

            while (true)
            {
                float fadeProgress = _tremorFadeInDuration > 0f
                    ? Mathf.Clamp01(elapsed / _tremorFadeInDuration)
                    : 1f;

                elapsed += Time.deltaTime;

                _tmp.ForceMeshUpdate();
                TMP_TextInfo textInfo = _tmp.textInfo;

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    float  phase  = i * _tremorPhaseSpread;
                    float  sinX   = Mathf.Sin(Time.time * _tremorSpeed + phase);
                    float  cosY   = Mathf.Cos(Time.time * _tremorSpeed + phase * 1.3f);
                    float  ox     = sinX * _tremorStrength * _tmp.fontSize * 0.001f;
                    float  oy     = cosY * _tremorStrength * _tmp.fontSize * 0.001f;

                    int       mesh   = charInfo.materialReferenceIndex;
                    int       vert   = charInfo.vertexIndex;
                    Vector3[] verts  = textInfo.meshInfo[mesh].vertices;
                    Color32[] colors = textInfo.meshInfo[mesh].colors32;

                    for (int v = 0; v < 4; v++)
                    {
                        verts[vert + v].x += ox;
                        verts[vert + v].y += oy;
                        colors[vert + v].a = (byte)(fadeProgress * 255);
                    }
                }

                _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
                yield return null;
            }
        }

        // ─────────────────────────────────────────
        // Private methods — Helpers
        // ─────────────────────────────────────────

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

        /// <summary>Sets TMP vertex color alpha.</summary>
        private void SetAlpha(float alpha)
        {
            if (_tmp == null) return;
            Color c = _tmp.color;
            c.a     = alpha;
            _tmp.color = c;
        }

        /// <summary>Applies Inspector style settings to TMP.</summary>
        private void ApplyStyle()
        {
            if (_tmp == null) return;
            _tmp.color    = _textColor;
            _tmp.fontSize = _fontSize;
        }

        /// <summary>
        /// Receives dialogue line started event from DialogueSystem.
        /// </summary>
        private void HandleLineStarted(OnDialogueLineStartedEvent e)
        {
            DisplayLine(e.Line);
        }
    }
}