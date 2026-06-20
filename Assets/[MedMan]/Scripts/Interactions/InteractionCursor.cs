using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Interaction
{
    /// <summary>
    /// Drives the in-world UI cursor anchored to screen center.
    /// Animates through 5 sprite frames when transitioning between interaction states.
    /// Subscribes to OnInteractableHoveredEvent via EventBus.
    /// Attach to the CursorImage GameObject on the HUD Canvas.
    /// </summary>
    public class InteractionCursor : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Cursor Sprites")]
        [InfoBox("5 frames per interaction type. Frame 0 = source state, Frame 4 = target state.")]
        [SerializeField] private Sprite[] _examineFrames  = new Sprite[5];

        [BoxGroup("Cursor Sprites")]
        [SerializeField] private Sprite[] _rotateFrames   = new Sprite[5];

        [BoxGroup("Cursor Sprites")]
        [SerializeField] private Sprite[] _useFrames      = new Sprite[5];
        
        [BoxGroup("Cursor Sprites")]
        [SerializeField] private Sprite[] _openCloseFrames      = new Sprite[5];
        
        [BoxGroup("Cursor Sprites")]
        [SerializeField] private Sprite[] _takeFrames     = new Sprite[5];

        [BoxGroup("Animation")]
        [SerializeField] private float _frameDuration = 0.05f;

        [BoxGroup("Animation")]
        [SerializeField] private float _fadeInDuration = 0.1f;

        private Sprite[] _lastFrames;
        private Image _image;
        private InteractionType _currentType = InteractionType.None;
        private Sequence _animSequence;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        /// <summary>
        /// Caches Image component, hides system cursor, subscribes to hover events.
        /// </summary>
        private void Awake()
        {
            // Hide system cursor — UI cursor takes over
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnInteractableHoveredEvent>(HandleHoverChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnInteractableHoveredEvent>(HandleHoverChanged);
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Triggered by EventBus when the hovered interaction type changes.
        /// Ignores event if type has not changed.
        /// </summary>
        private void HandleHoverChanged(OnInteractableHoveredEvent e)
        {
            Debug.Log($"[Cursor] HandleHoverChanged: {e.Type} (current: {_currentType})");

            if (e.Type == _currentType) return;

            _currentType = e.Type;
            PlayTransition(e.Type);
        }

        /// <summary>
        /// Plays the sprite transition for the given interaction type.
        /// For None, reverses the last played frame sequence to animate back to the default state.
        /// Stores the last non-None frame sequence for use on exit.
        /// </summary>
        private void PlayTransition(InteractionType type)
        {
            Sprite[] frames;

            if (type == InteractionType.None)
            {
                if (_lastFrames == null) return;
                // Reverse a copy — don't modify the original array
                frames = (Sprite[])_lastFrames.Clone();
                System.Array.Reverse(frames);
            }
            else
            {
                frames = GetFrames(type);
                if (frames == null || frames.Length == 0) return;
                _lastFrames = frames;
            }

            _animSequence?.Kill();
            _animSequence = DOTween.Sequence();

            for (int i = 0; i < frames.Length; i++)
            {
                Sprite frame = frames[i];
                if (frame == null) continue;

                int frameIndex = i;

                _animSequence
                    .AppendCallback(() =>
                    {
                        _image.sprite = frame;
                        _image.color = new Color(1f, 1f, 1f, 0.6f);
                       // Debug.Log($"[Cursor] Frame {frameIndex}: {frame.name}");
                    })
                    .Append(_image.DOFade(1f, _fadeInDuration))
                    .AppendInterval(_frameDuration);
            }
        }

        /// <summary>
        /// Returns the sprite array for the given interaction type.
        /// Add new cases here when extending InteractionType enum.
        /// </summary>
        private Sprite[] GetFrames(InteractionType type)
        {
            return type switch
            {
                InteractionType.Examine => _examineFrames,
                InteractionType.Rotate  => _rotateFrames,
                InteractionType.OpenClose     => _openCloseFrames,
                InteractionType.Take    => _takeFrames,
                _                       => null
            };
        }
    }
}