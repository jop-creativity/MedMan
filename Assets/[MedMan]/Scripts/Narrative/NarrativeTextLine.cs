using System.Collections;
using UnityEngine;
using TMPro;

namespace MedMan.Narrative
{
    /// <summary>
    /// Lightweight component placed on a spawned narrative text line prefab.
    /// Reads all animation settings from the parent NarrativeTextController and
    /// delegates animation execution back to it — zero code duplication.
    /// Spawned and destroyed by NarrativeTextController — never placed manually in a scene.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class NarrativeTextLine : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────

        private TextMeshPro             _tmp;
        private NarrativeTextController _controller;
        private Coroutine               _showCoroutine;
        private Coroutine               _tremorCoroutine;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        private void Awake()
        {
            _tmp        = GetComponent<TextMeshPro>();
            _controller = GetComponentInParent<NarrativeTextController>();

            if (_controller == null)
                Debug.LogWarning("[NarrativeTextLine] No NarrativeTextController found in parent.", this);

            NarrativeTextController.SetTmpAlpha(_tmp, 0f);
        }

        // ─────────────────────────────────────────
        // Public methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Explicitly sets the controller reference.
        /// Call this after Instantiate when GetComponentInParent cannot resolve it automatically.
        /// </summary>
        public void SetController(NarrativeTextController controller)
        {
            _controller = controller;
        }

        /// <summary>
        /// Sets the text content. Call before Show().
        /// </summary>
        public void Setup(string text)
        {
            _tmp.text = text;
        }

        /// <summary>
        /// Runs the same animation sequence as NarrativeTextController.DisplayRoutine
        /// but on this line's own TMP object. All parameters come from the parent controller.
        /// </summary>
        public void Show()
        {
            if (_showCoroutine != null) StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowCor());
        }

        /// <summary>
        /// Fades out this line and destroys the GameObject when complete.
        /// </summary>
        public void Hide()
        {
            if (_tremorCoroutine != null)
            {
                StopCoroutine(_tremorCoroutine);
                _tremorCoroutine = null;
            }

            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            StartCoroutine(HideCor());
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        private IEnumerator ShowCor()
        {
            if (_controller == null) yield break;

            var animType = _controller.AnimationType;

            if ((animType & NarrativeTextController.TextAnimationType.FallingLetters) != 0)
            {
                NarrativeTextController.SetTmpAlpha(_tmp, 0f);
                _tmp.ForceMeshUpdate();
                yield return null;

                if ((animType & NarrativeTextController.TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(_controller.ElectricShockRoutine(_tmp));

                if ((animType & NarrativeTextController.TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(_controller.TremorRoutine(_tmp));

                yield return StartCoroutine(_controller.FallingLettersRoutine(_tmp));
            }
            else if ((animType & NarrativeTextController.TextAnimationType.Typewriter) != 0)
            {
                yield return StartCoroutine(_controller.FadeRoutine(_tmp, 0f, 1f, _controller.FadeDuration));

                if ((animType & NarrativeTextController.TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(_controller.ElectricShockRoutine(_tmp));

                if ((animType & NarrativeTextController.TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(_controller.TremorRoutine(_tmp));

                string fullText = _tmp.text;
                _tmp.text = string.Empty;
                foreach (char c in fullText)
                {
                    _tmp.text += c;
                    yield return new WaitForSeconds(_controller.CharDelay);
                }
            }
            else
            {
                yield return StartCoroutine(_controller.FadeRoutine(_tmp, 0f, 1f, _controller.FadeDuration));
                _tmp.ForceMeshUpdate();

                if ((animType & NarrativeTextController.TextAnimationType.ElectricShock) != 0)
                    StartCoroutine(_controller.ElectricShockRoutine(_tmp));

                if ((animType & NarrativeTextController.TextAnimationType.Tremor) != 0)
                    _tremorCoroutine = StartCoroutine(_controller.TremorRoutine(_tmp));
            }
        }

        private IEnumerator HideCor()
        {
            float fadeDuration = _controller != null ? _controller.AnimationFadeInDuration : 0.4f;
            float startAlpha   = _tmp != null ? _tmp.color.a : 1f;

            yield return StartCoroutine(
                _controller != null
                    ? _controller.FadeRoutine(_tmp, startAlpha, 0f, fadeDuration)
                    : FallbackFadeCor(startAlpha, 0f, fadeDuration)
            );

            Destroy(gameObject);
        }

        /// <summary>Fallback fade used only if controller reference is lost.</summary>
        private IEnumerator FallbackFadeCor(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                NarrativeTextController.SetTmpAlpha(_tmp, Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
            NarrativeTextController.SetTmpAlpha(_tmp, to);
        }
    }
}
