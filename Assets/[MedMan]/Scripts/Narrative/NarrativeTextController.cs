using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Supports Text Accumulation mode — lines from the assigned DialogueLineSO are split by '|'
    /// and displayed one by one, each appended to the same TMP object with a configurable delay.
    /// Can be triggered by zone entry (OnTriggerEnter) or called directly by DialogueSystem.
    /// All animation parameters are tweakable in the Inspector.
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
            Accumulation   = 1 << 4,  // Lines split by '|' stacked one by one with delay
        }

        // ─────────────────────────────────────────
        // Properties — read by NarrativeTextLine
        // ─────────────────────────────────────────

        /// <summary>Full animation type flags — used by NarrativeTextLine to replicate behaviour.</summary>
        public TextAnimationType AnimationType        => _animationType;

        /// <summary>Duration used for fade-in and fade-out on spawned lines.</summary>
        public float AnimationFadeInDuration          => _animationFadeInDuration;

        /// <summary>Fade duration used by Typewriter and plain fade modes.</summary>
        public float FadeDuration                     => _fadeDuration;

        /// <summary>Typewriter char delay.</summary>
        public float CharDelay                        => _charDelay;

        // ─────────────────────────────────────────
        // ShowIf helpers — animation type flags
        // ─────────────────────────────────────────

        // NaughtyAttributes [ShowIf] does not support flags enum directly.
        // These private bool properties are used as condition names in [ShowIf].
        private bool IsTypewriter     => (_animationType & TextAnimationType.Typewriter)     != 0;
        private bool IsFallingLetters => (_animationType & TextAnimationType.FallingLetters) != 0;
        private bool IsElectricShock  => (_animationType & TextAnimationType.ElectricShock)  != 0;
        private bool IsTremor         => (_animationType & TextAnimationType.Tremor)         != 0;
        private bool IsAccumulation   => (_animationType & TextAnimationType.Accumulation)   != 0;

        // ─────────────────────────────────────────
        // Fields — Content
        // ─────────────────────────────────────────

        [BoxGroup("Content")]
        [Tooltip("Dialogue line to display. Used for runtime playback and Editor preview.\n" +
                 "In Accumulation mode the localized string is split by '|' into separate lines.")]
        [SerializeField] private DialogueLineSO _dialogueLineSO;

#if UNITY_EDITOR
        [ShowNativeProperty]
        private string PreviewPL => ResolveLocalePreview("pl");

        [ShowNativeProperty]
        private string PreviewEN => ResolveLocalePreview("en");
#endif

        // ─────────────────────────────────────────
        // Fields — Zone Trigger
        // ─────────────────────────────────────────

        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, this controller activates when the player enters the attached trigger collider.")]
        [SerializeField] private bool _activateOnTriggerEnter = true;

        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, text clears when the player exits the trigger collider. " +
                 "Ignored in Accumulation mode — use Clear Only When Not Visible instead.")]
        [SerializeField] private bool _clearOnTriggerExit;

        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, trigger fires only once and disables itself after.")]
        [SerializeField] private bool _triggerOnce = true;

        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, text persists until ClearText() is called. Overrides Display Duration. " +
                 "Automatically active in Accumulation mode.")]
        [SerializeField] private bool _persistUntilCleared;

        [BoxGroup("Zone Trigger")]
        [Tooltip("If true, text clears only when the player is NOT looking at this object on trigger exit. " +
                 "Requires PlayerVisibilityChecker on the same GameObject.")]
        [SerializeField] private bool _clearOnlyWhenNotVisible = true;

        // ─────────────────────────────────────────
        // Fields — Animation Type
        // ─────────────────────────────────────────

        [BoxGroup("Animation Type")]
        [Tooltip("Select one or more animation effects to apply simultaneously.")]
        [SerializeField] private TextAnimationType _animationType = TextAnimationType.FallingLetters;

        [BoxGroup("Animation Type")]
        [Tooltip("Duration of the fade-in applied when any animation starts. 0 = instant.")]
        [Range(0f, 2f)]
        [SerializeField] private float _animationFadeInDuration = 0.4f;

        // ─────────────────────────────────────────
        // Fields — Typewriter
        // ─────────────────────────────────────────

        [BoxGroup("Typewriter")]
        [ShowIf("IsTypewriter")]
        [Tooltip("Seconds per character when typing out the text.")]
        [SerializeField] private float _charDelay = 0.04f;

        [BoxGroup("Typewriter")]
        [ShowIf("IsTypewriter")]
        [Tooltip("How long the text remains fully visible. 0 = persist until cleared.")]
        [SerializeField] private float _displayDuration = 3f;

        [BoxGroup("Typewriter")]
        [ShowIf("IsTypewriter")]
        [Tooltip("Duration of the fade-in and fade-out.")]
        [SerializeField] private float _fadeDuration = 0.4f;

        // ─────────────────────────────────────────
        // Fields — Falling Letters
        // ─────────────────────────────────────────

        [BoxGroup("Falling Letters")]
        [ShowIf("IsFallingLetters")]
        [Tooltip("Distance above final position each letter starts from.")]
        [SerializeField] private float _fallDistance = 30f;

        [BoxGroup("Falling Letters")]
        [ShowIf("IsFallingLetters")]
        [Tooltip("Time in seconds for each letter to fall into position.")]
        [SerializeField] private float _fallDuration = 0.3f;

        [BoxGroup("Falling Letters")]
        [ShowIf("IsFallingLetters")]
        [Tooltip("Delay between each letter starting to fall.")]
        [SerializeField] private float _fallStagger = 0.05f;

        // ─────────────────────────────────────────
        // Fields — Electric Shock
        // ─────────────────────────────────────────

        [BoxGroup("Electric Shock")]
        [ShowIf("IsElectricShock")]
        [Tooltip("Maximum random positional offset at shock peak.")]
        [SerializeField] private float _shockOffsetMax = 8f;

        [BoxGroup("Electric Shock")]
        [ShowIf("IsElectricShock")]
        [Tooltip("Number of shock jolt iterations before settling.")]
        [SerializeField] private int _shockIterations = 4;

        [BoxGroup("Electric Shock")]
        [ShowIf("IsElectricShock")]
        [Tooltip("Time between each shock jolt.")]
        [SerializeField] private float _shockInterval = 0.04f;

        [BoxGroup("Electric Shock")]
        [ShowIf("IsElectricShock")]
        [Tooltip("Time to settle back to zero after final jolt.")]
        [SerializeField] private float _shockSettleDuration = 0.1f;

        // ─────────────────────────────────────────
        // Fields — Tremor
        // ─────────────────────────────────────────

        [BoxGroup("Tremor")]
        [ShowIf("IsTremor")]
        [Tooltip("Maximum positional offset per vertex during tremor.")]
        [SerializeField] private float _tremorStrength = 1.5f;

        [BoxGroup("Tremor")]
        [ShowIf("IsTremor")]
        [Tooltip("Speed of tremor oscillation. Higher = faster shaking.")]
        [SerializeField] private float _tremorSpeed = 12f;

        [BoxGroup("Tremor")]
        [ShowIf("IsTremor")]
        [Tooltip("Per-letter phase offset for organic feel. Higher = more chaotic.")]
        [SerializeField] private float _tremorPhaseSpread = 2.5f;

        // ─────────────────────────────────────────
        // Fields — Text Accumulation
        // ─────────────────────────────────────────

        [BoxGroup("Text Accumulation")]
        [ShowIf("IsAccumulation")]
        [Tooltip("Delay in seconds between each accumulated line appearing.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _delayBetweenLines = 1.5f;

        [BoxGroup("Text Accumulation")]
        [ShowIf("IsAccumulation")]
        [Tooltip("Prefab with TextMeshPro + NarrativeTextLine components. Spawned for each line.")]
        [SerializeField] private NarrativeTextLine _linePrefab;

        [BoxGroup("Text Accumulation")]
        [ShowIf("IsAccumulation")]
        [Tooltip("Vertical offset in local space between each spawned line. Positive = upward.")]
        [SerializeField] private float _lineOffset = 0.3f;

        [BoxGroup("Text Accumulation")]
        [ShowIf("IsAccumulation")]
        [Tooltip("If true, destroys this entire GameObject after all lines have faded out. " +
                 "Use when this object exists solely to display text and is no longer needed.")]
        [SerializeField] private bool _destroyOnHide;

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
        [Tooltip("Audio clip to play. Pre-filled from DialogueLineSO if it carries one.")]
        [SerializeField] private AudioClip _audioClip;

        // ─────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────

        private TextMeshPro _tmp;
        private Coroutine   _displayCoroutine;
        private Coroutine   _tremorCoroutine;
        private Coroutine   _accumulationCoroutine;
        private bool        _isDisplaying;
        private bool        _triggered;
        private AudioSource _audioSource;

        // Accumulation state
        private List<string>            _parsedLines       = new List<string>();
        private List<NarrativeTextLine> _spawnedLines      = new List<NarrativeTextLine>();
        private bool                    _accumulationActive;
        private bool                    _accumulationComplete;
        private PlayerVisibilityChecker _visibilityChecker;

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

            _visibilityChecker = GetComponent<PlayerVisibilityChecker>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnDialogueLineStartedEvent>(HandleLineStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnDialogueLineStartedEvent>(HandleLineStarted);
        }

        private void OnValidate()
        {
            if (_hasAudio && _audioClip == null && _dialogueLineSO != null && _dialogueLineSO.HasAudio)
                _audioClip = _dialogueLineSO.AudioClip;
        }

        // ─────────────────────────────────────────
        // Zone Trigger
        // ─────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!_activateOnTriggerEnter) return;
            if (_triggerOnce && _triggered) return;
            if (!other.CompareTag("Player")) return;

            _triggered = true;

            if (IsAccumulation)
                StartAccumulation();
            else
                DisplayLine(_dialogueLineSO);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (IsAccumulation)
            {
                if (_clearOnlyWhenNotVisible && _visibilityChecker != null)
                    StartCoroutine(AccumulationClearWhenNotVisibleCor());
                else
                    ResetAccumulation();
                return;
            }

            if (!_clearOnTriggerExit) return;
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
        /// Immediately clears the text, resets alpha, stops all animations and any playing audio.
        /// </summary>
        public void ClearText()
        {
            if (_displayCoroutine      != null) StopCoroutine(_displayCoroutine);
            if (_tremorCoroutine       != null) StopCoroutine(_tremorCoroutine);
            if (_accumulationCoroutine != null) StopCoroutine(_accumulationCoroutine);

            _tmp.text             = string.Empty;
            _isDisplaying         = false;
            _accumulationActive   = false;
            _accumulationComplete = false;
            SetAlpha(0f);

            if (_hasAudio && _audioSource != null)
                _audioSource.Stop();

#if UNITY_EDITOR
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

        /// <summary>
        /// Starts the accumulation sequence using the assigned DialogueLineSO.
        /// Spawns a NarrativeTextLine prefab for each line, positioned with vertical offset.
        /// Called automatically on trigger enter when Accumulation flag is set.
        /// </summary>
        public void StartAccumulation()
        {
            if (_dialogueLineSO == null)
            {
                Debug.LogWarning("[NarrativeTextController] Accumulation started but no DialogueLineSO assigned.", this);
                return;
            }

            if (_linePrefab == null)
            {
                Debug.LogWarning("[NarrativeTextController] Accumulation started but no Line Prefab assigned.", this);
                return;
            }

            if (_accumulationCoroutine != null)
                StopCoroutine(_accumulationCoroutine);

            _spawnedLines.Clear();
            _accumulationActive   = true;
            _accumulationComplete = false;

            _accumulationCoroutine = StartCoroutine(AccumulationSequenceCor());
        }

        /// <summary>
        /// Hides all spawned lines (fade out + destroy) and resets accumulation state.
        /// If DestroyOnHide is enabled, also destroys this GameObject after all lines fade out.
        /// </summary>
        public void ResetAccumulation()
        {
            if (_accumulationCoroutine != null)
                StopCoroutine(_accumulationCoroutine);

            if (_tremorCoroutine != null)
            {
                StopCoroutine(_tremorCoroutine);
                _tremorCoroutine = null;
            }

            foreach (NarrativeTextLine line in _spawnedLines)
            {
                if (line != null)
                    line.Hide();
            }
            _spawnedLines.Clear();

            _accumulationActive   = false;
            _accumulationComplete = false;
            _triggered            = false;

            if (_destroyOnHide)
                StartCoroutine(DestroyAfterFadeOutCor());
        }

        // ─────────────────────────────────────────
        // Editor preview
        // ─────────────────────────────────────────

        [Button("Preview Text (Play Mode only for animations)")]
        private void PreviewText()
        {
            ClearText();

            if (_dialogueLineSO == null)
            {
                Debug.LogWarning("[NarrativeTextController] No DialogueLineSO assigned.", this);
                return;
            }

            if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
            ApplyStyle();

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                string text = ResolveLocalePreview("en");
                _tmp.text  = text;
                _tmp.color = _textColor;

                if (_hasAudio && _audioClip != null)
                {
                    var assembly   = typeof(UnityEditor.AudioImporter).Assembly;
                    var audioUtil  = assembly.GetType("UnityEditor.AudioUtil");
                    var playMethod = audioUtil.GetMethod("PlayPreviewClip",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                        null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                    playMethod?.Invoke(null, new object[] { _audioClip, 0, false });
                }

                Debug.Log($"[NarrativeTextController] Preview (EN): {text}");
#endif
            }
            else
            {
                if (IsAccumulation)
                    StartAccumulation();
                else
                    DisplayLine(_dialogueLineSO);
            }
        }

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

        private IEnumerator DisplayRoutine(DialogueLineSO line)
        {
            _isDisplaying = true;

            string text = string.Empty;
            var op = line.LocalizedText.GetLocalizedStringAsync();
            yield return op;
            text = op.Result;

            if (_hasAudio && _audioClip != null)
            {
                _audioSource.clip   = _audioClip;
                _audioSource.volume = line.AudioVolume;
                _audioSource.Play();
            }

            if ((_animationType & TextAnimationType.FallingLetters) != 0)
            {
                _tmp.color = new Color(_textColor.r, _textColor.g, _textColor.b, 0f);
                _tmp.text  = text;
                _tmp.ForceMeshUpdate();
                yield return null;

                if ((_animationType & TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(ElectricShockRoutine(_tmp));

                if ((_animationType & TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(TremorRoutine(_tmp));

                yield return StartCoroutine(FallingLettersRoutine(_tmp));
            }
            else if ((_animationType & TextAnimationType.Typewriter) != 0)
            {
                yield return StartCoroutine(FadeRoutine(_tmp, 0f, 1f, _fadeDuration));

                if ((_animationType & TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(ElectricShockRoutine(_tmp));

                if ((_animationType & TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(TremorRoutine(_tmp));

                _tmp.text = string.Empty;
                foreach (char c in text)
                {
                    _tmp.text += c;
                    yield return new WaitForSeconds(_charDelay);
                }
            }
            else
            {
                yield return StartCoroutine(FadeRoutine(_tmp, 0f, 1f, _fadeDuration));
                _tmp.text = text;
                _tmp.ForceMeshUpdate();

                if ((_animationType & TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(ElectricShockRoutine(_tmp));

                if ((_animationType & TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(TremorRoutine(_tmp));
            }

            if (!_persistUntilCleared && _displayDuration > 0f)
            {
                yield return new WaitForSeconds(_displayDuration);

                if (_tremorCoroutine != null)
                {
                    StopCoroutine(_tremorCoroutine);
                    _tremorCoroutine = null;
                }

                yield return StartCoroutine(FadeRoutine(_tmp, 1f, 0f, _fadeDuration));
                _tmp.text = string.Empty;
            }

            _isDisplaying = false;
            EventBus.Publish(new OnDialogueLineEndedEvent(line));
        }

        // ─────────────────────────────────────────
        // Private methods — Text Accumulation
        // ─────────────────────────────────────────

        /// <summary>
        /// Fetches the localized string, splits by '|', then spawns a NarrativeTextLine
        /// prefab for each line. Each line is positioned above the previous by _lineOffset.
        /// </summary>
        private IEnumerator AccumulationSequenceCor()
        {
            // Fetch localized string at runtime
            var op = _dialogueLineSO.LocalizedText.GetLocalizedStringAsync();
            yield return op;
            string fullText = op.Result;

            // Parse into lines
            _parsedLines.Clear();
            foreach (string part in fullText.Split('|'))
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    _parsedLines.Add(trimmed);
            }

            // Spawn line by line
            for (int i = 0; i < _parsedLines.Count; i++)
            {
                // Position: each new line spawns at base position + offset * index
                Vector3 spawnPos = transform.position + transform.up * (_lineOffset * i);
                NarrativeTextLine lineObj = Instantiate(_linePrefab, spawnPos, transform.rotation, transform);

                lineObj.Setup(_parsedLines[i]);

                lineObj.Show();
                _spawnedLines.Add(lineObj);

                yield return new WaitForSeconds(_delayBetweenLines);
            }

            _accumulationComplete = true;
        }

        /// <summary>
        /// Waits until the player is not looking at this object, then resets accumulation.
        /// </summary>
        private IEnumerator AccumulationClearWhenNotVisibleCor()
        {
            while (_visibilityChecker != null && _visibilityChecker.IsVisibleToPlayer)
                yield return null;

            ResetAccumulation();
        }

        /// <summary>
        /// Waits for the fade out duration then destroys this GameObject.
        /// Used when DestroyOnHide is enabled.
        /// </summary>
        private IEnumerator DestroyAfterFadeOutCor()
        {
            yield return new WaitForSeconds(_animationFadeInDuration + 0.1f);
            Destroy(gameObject);
        }

        // ─────────────────────────────────────────
        // Private methods — Animations
        // ─────────────────────────────────────────

        // ─────────────────────────────────────────
        // Internal methods — Animations (shared with NarrativeTextLine)
        // ─────────────────────────────────────────

        /// <summary>Animates each letter falling from above into position on the given TMP.</summary>
        internal IEnumerator FallingLettersRoutine(TextMeshPro tmp)
        {
            tmp.ForceMeshUpdate();
            TMP_TextInfo textInfo = tmp.textInfo;
            int charCount = textInfo.characterCount;

            float[] timers    = new float[charCount];
            bool[]  animating = new bool[charCount];

            for (int i = 0; i < charCount; i++)
            {
                timers[i]    = -i * _fallStagger;
                animating[i] = true;
            }

            bool anyAnimating = true;

            Vector3[][] originalVertices = new Vector3[textInfo.meshInfo.Length][];
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
                originalVertices[m] = (Vector3[])textInfo.meshInfo[m].vertices.Clone();

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

                    float t      = Mathf.Clamp01(timers[i] / _fallDuration);
                    float ease   = 1f - Mathf.Pow(1f - t, 3f);
                    float offset = Mathf.Lerp(_fallDistance, 0f, ease);

                    int meshIndex = charInfo.materialReferenceIndex;
                    int vertIndex = charInfo.vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;

                    for (int v = 0; v < 4; v++)
                        verts[vertIndex + v].y = originalVertices[meshIndex][vertIndex + v].y + offset * tmp.fontSize * 0.01f;

                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
                    byte alpha = (byte)(Mathf.Clamp01(ease) * 255);
                    for (int v = 0; v < 4; v++)
                        colors[vertIndex + v].a = alpha;

                    if (t >= 1f) animating[i] = false;
                    else         anyAnimating  = true;
                }

                for (int m = 0; m < textInfo.meshInfo.Length; m++)
                    tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);

                yield return null;
            }
        }

        /// <summary>Snaps each letter into view with a rapid positional jolt on the given TMP.</summary>
        internal IEnumerator ElectricShockRoutine(TextMeshPro tmp)
        {
            tmp.ForceMeshUpdate();
            TMP_TextInfo textInfo = tmp.textInfo;
            int charCount = textInfo.characterCount;

            for (int iter = 0; iter < _shockIterations; iter++)
            {
                tmp.ForceMeshUpdate();
                textInfo = tmp.textInfo;

                for (int i = 0; i < charCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    float offsetX = UnityEngine.Random.Range(-_shockOffsetMax, _shockOffsetMax);
                    float offsetY = UnityEngine.Random.Range(-_shockOffsetMax, _shockOffsetMax);
                    int   mesh    = charInfo.materialReferenceIndex;
                    int   vert    = charInfo.vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[mesh].vertices;

                    for (int v = 0; v < 4; v++)
                    {
                        verts[vert + v].x += offsetX * tmp.fontSize * 0.001f;
                        verts[vert + v].y += offsetY * tmp.fontSize * 0.001f;
                    }
                }

                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                yield return new WaitForSeconds(_shockInterval);
            }

            float elapsed = 0f;
            while (elapsed < _shockSettleDuration)
            {
                elapsed += Time.deltaTime;
                tmp.ForceMeshUpdate();
                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                yield return null;
            }
        }

        /// <summary>Continuously shakes each letter on the given TMP.</summary>
        internal IEnumerator TremorRoutine(TextMeshPro tmp)
        {
            float elapsed = 0f;

            while (true)
            {
                float fadeProgress = _animationFadeInDuration > 0f
                    ? Mathf.Clamp01(elapsed / _animationFadeInDuration)
                    : 1f;

                elapsed += Time.deltaTime;

                tmp.ForceMeshUpdate();
                TMP_TextInfo textInfo = tmp.textInfo;

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    float phase = i * _tremorPhaseSpread;
                    float sinX  = Mathf.Sin(Time.time * _tremorSpeed + phase);
                    float cosY  = Mathf.Cos(Time.time * _tremorSpeed + phase * 1.3f);
                    float ox    = sinX * _tremorStrength * tmp.fontSize * 0.001f;
                    float oy    = cosY * _tremorStrength * tmp.fontSize * 0.001f;

                    int       mesh   = charInfo.materialReferenceIndex;
                    int       vert   = charInfo.vertexIndex;
                    Vector3[] verts  = textInfo.meshInfo[mesh].vertices;
                    Color32[] colors = textInfo.meshInfo[mesh].colors32;

                    for (int v = 0; v < 4; v++)
                    {
                        verts[vert + v].x  += ox;
                        verts[vert + v].y  += oy;
                        colors[vert + v].a  = (byte)(fadeProgress * 255);
                    }
                }

                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
                yield return null;
            }
        }

        /// <summary>Fades TMP alpha from startAlpha to endAlpha over duration seconds.</summary>
        internal IEnumerator FadeRoutine(TextMeshPro tmp, float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetTmpAlpha(tmp, Mathf.Lerp(startAlpha, endAlpha, elapsed / duration));
                yield return null;
            }
            SetTmpAlpha(tmp, endAlpha);
        }

        // ─────────────────────────────────────────
        // Private methods — Helpers
        // ─────────────────────────────────────────

        internal static void SetTmpAlpha(TextMeshPro tmp, float alpha)
        {
            if (tmp == null) return;
            Color c = tmp.color;
            c.a     = alpha;
            tmp.color = c;
        }

        private void SetAlpha(float alpha) => SetTmpAlpha(_tmp, alpha);

        private void ApplyStyle()
        {
            if (_tmp == null) return;
            _tmp.color             = _textColor;
            _tmp.fontSize          = _fontSize;
            _tmp.verticalAlignment = VerticalAlignmentOptions.Top;
        }

        private void HandleLineStarted(OnDialogueLineStartedEvent e)
        {
            DisplayLine(e.Line);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Resolves the localized string for the given locale code in Edit Mode.
        /// Used by ShowNativeProperty preview fields.
        /// </summary>
        private string ResolveLocalePreview(string localeCode)
        {
            if (_dialogueLineSO == null) return "(no DialogueLineSO)";

            try
            {
                var stringRef  = _dialogueLineSO.LocalizedText;
                var settings   = UnityEditor.Localization.LocalizationEditorSettings
                    .ActiveLocalizationSettings;
                if (settings == null) return "(no LocalizationSettings)";

                var locale = settings.GetAvailableLocales()?.GetLocale(
                    new UnityEngine.Localization.LocaleIdentifier(localeCode));
                if (locale == null) return $"(locale '{localeCode}' not found)";

                var collection = UnityEditor.Localization.LocalizationEditorSettings
                    .GetStringTableCollection(stringRef.TableReference.TableCollectionNameGuid);
                if (collection == null) return "(table not found)";

                var table = collection.GetTable(locale.Identifier)
                    as UnityEngine.Localization.Tables.StringTable;
                if (table == null) return $"(table for '{localeCode}' not found)";

                var entry = table.GetEntry(stringRef.TableEntryReference.KeyId);
                return entry?.LocalizedValue ?? "(missing key)";
            }
            catch
            {
                return "(preview unavailable)";
            }
        }
#endif
    }
}
