using System.Collections.Generic;
using UnityEngine;
using MedMan.Core;
using NaughtyAttributes;

namespace MedMan.Narrative
{
    /// <summary>
    /// Queues and plays DialogueLineSO assets in sequence.
    /// Notifies NarrativeTextController via EventBus to display each line.
    /// Sits under NarrativeManager in the hierarchy.
    /// Does not handle display logic — that lives in NarrativeTextController.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("References")]
        [Tooltip("Controller that displays each queued line. Driven directly — no global broadcast.")]
        [SerializeField] private NarrativeTextController _narrativeTextController;
        
        [BoxGroup("Debug")]
        [ReadOnly]
        [SerializeField] private int _queueCount;

        [BoxGroup("Debug")]
        [ReadOnly]
        [SerializeField] private bool _isPlaying;

        private readonly Queue<DialogueLineSO> _queue = new Queue<DialogueLineSO>();

        // ─────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────

        /// <summary>Whether a dialogue sequence is currently playing.</summary>
        public bool IsPlaying => _isPlaying;

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        /// <summary>Subscribes to line ended event to advance the queue.</summary>
        private void OnEnable()
        {
            EventBus.Subscribe<OnDialogueLineEndedEvent>(HandleLineEnded);
        }

        /// <summary>Unsubscribes from line ended event.</summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnDialogueLineEndedEvent>(HandleLineEnded);
        }

        // ─────────────────────────────────────────
        // Public methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Enqueues a single dialogue line and starts playback if not already playing.
        /// </summary>
        public void EnqueueLine(DialogueLineSO line)
        {
            if (line == null) return;
            _queue.Enqueue(line);
            _queueCount = _queue.Count;
            Debug.Log($"[DialogueSystem] Enqueued: {line.name} — queue size: {_queue.Count}");

            if (!_isPlaying)
                PlayNext();
        }

        /// <summary>
        /// Enqueues multiple dialogue lines in order and starts playback if not already playing.
        /// </summary>
        public void EnqueueSequence(IEnumerable<DialogueLineSO> lines)
        {
            foreach (var line in lines)
            {
                if (line != null)
                    _queue.Enqueue(line);
            }

            _queueCount = _queue.Count;
            Debug.Log($"[DialogueSystem] Enqueued sequence — queue size: {_queue.Count}");

            if (!_isPlaying)
                PlayNext();
        }

        /// <summary>
        /// Clears the queue and stops playback immediately.
        /// Use when a scene transition or state change interrupts dialogue.
        /// </summary>
        public void ClearQueue()
        {
            _queue.Clear();
            _queueCount = 0;
            _isPlaying  = false;
            Debug.Log("[DialogueSystem] Queue cleared.");
        }

        /// <summary>
        /// Called by NarrativeTextController via EventBus when a line finishes displaying.
        /// Advances to the next line or ends the sequence.
        /// </summary>
        public void NotifyLineEnded(DialogueLineSO line)
        {
            EventBus.Publish(new OnDialogueLineEndedEvent(line));
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Dequeues and publishes the next line.
        /// Publishes OnDialogueSequenceEndedEvent when the queue is empty.
        /// </summary>
        private void PlayNext()
        {
            if (_queue.Count == 0)
            {
                _isPlaying  = false;
                _queueCount = 0;
                EventBus.Publish(new OnDialogueSequenceEndedEvent());
                Debug.Log("[DialogueSystem] Sequence ended.");
                return;
            }
            
            _isPlaying = true;
            DialogueLineSO next = _queue.Dequeue();
            _queueCount = _queue.Count;

            // Drive display directly on the assigned controller — no global broadcast.
            // The event below stays a pure notification for future audio/subtitle consumers.
            if (_narrativeTextController != null)
                _narrativeTextController.DisplayLine(next);
            else
                Debug.LogWarning("[DialogueSystem] No NarrativeTextController assigned — line will not display.", this);

            EventBus.Publish(new OnDialogueLineStartedEvent(next));
            Debug.Log($"[DialogueSystem] Playing: {next.name} — {_queue.Count} remaining");
        }

        /// <summary>
        /// Advances queue when a line finishes displaying.
        /// </summary>
        private void HandleLineEnded(OnDialogueLineEndedEvent e)
        {
            PlayNext();
        }
    }
}