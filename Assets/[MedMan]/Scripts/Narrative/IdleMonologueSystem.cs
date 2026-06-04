using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using MedMan.Core;

namespace MedMan.Narrative
{
    /// <summary>
    /// Plays location-specific monologue lines when the player remains idle for a configured duration.
    /// Subscribes to the PlayerController idle event via EventBus.
    /// Lines are defined per scene zone — the system tracks which zone the player is currently in
    /// and selects from the appropriate pool, avoiding repetition until all lines are exhausted.
    /// </summary>
    public class IdleMonologueSystem : MonoBehaviour
    {
        // ── Inner Types ──────────────────────────────────────────────────────

        /// <summary>Associates a named zone with a pool of idle monologue lines.</summary>
        [System.Serializable]
        public class ZoneMonologue
        {
            [Tooltip("Unique identifier for this zone. Must match the zone name sent by the trigger.")]
            public string zoneId;

            [Tooltip("Pool of lines to choose from when the player idles in this zone.")]
            [ResizableTextArea]
            public List<string> lines = new List<string>();
        }

        // ── Fields ───────────────────────────────────────────────────────────

        [Header("Monologue Data")]
        [SerializeField]
        private List<ZoneMonologue> _zoneMonologues = new List<ZoneMonologue>();

        [Header("Timing")]
        [SerializeField]
        [Tooltip("Seconds of inactivity before idle monologue triggers.")]
        [Range(5f, 120f)]
        private float _idleThreshold = 30f;

        [SerializeField]
        [Tooltip("Minimum delay between consecutive idle lines in the same zone.")]
        [Range(5f, 60f)]
        private float _cooldownBetweenLines = 10f;

        [Header("Output")]
        [SerializeField]
        [Tooltip("Optional: TextMeshPro component to display the line as world-space text. " +
                 "If null, line is only sent via EventBus.")]
        private TMPro.TextMeshPro _outputText;

        [Header("Debug")]
        [ReadOnly]
        [SerializeField] private string _currentZoneId = "";
        [ReadOnly]
        [SerializeField] private float _idleTimer = 0f;

        // ── Private ──────────────────────────────────────────────────────────

        private Dictionary<string, ZoneMonologue> _zoneMap = new Dictionary<string, ZoneMonologue>();
        private Dictionary<string, List<int>> _usedIndices = new Dictionary<string, List<int>>();
        private float _lastLineTime = -999f;
        private bool _playerIdle = false;
        private Coroutine _idleCoroutine;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            BuildZoneMap();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayerIdleEvent>(OnPlayerIdle);
            EventBus.Subscribe<OnPlayerMovedEvent>(OnPlayerMoved);
            EventBus.Subscribe<OnNarrativeZoneEnteredEvent>(OnZoneEntered);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayerIdleEvent>(OnPlayerIdle);
            EventBus.Unsubscribe<OnPlayerMovedEvent>(OnPlayerMoved);
            EventBus.Unsubscribe<OnNarrativeZoneEnteredEvent>(OnZoneEntered);
        }

        // ── Public Methods ───────────────────────────────────────────────────

        /// <summary>
        /// Registers the current zone. Called by zone triggers when the player enters.
        /// </summary>
        public void SetCurrentZone(string zoneId)
        {
            _currentZoneId = zoneId;
        }

        /// <summary>
        /// Clears the current zone. Called when the player leaves a zone with no replacement.
        /// </summary>
        public void ClearCurrentZone()
        {
            _currentZoneId = "";
        }

        // ── Private Methods ──────────────────────────────────────────────────

        private void BuildZoneMap()
        {
            _zoneMap.Clear();
            foreach (ZoneMonologue zm in _zoneMonologues)
            {
                if (!string.IsNullOrEmpty(zm.zoneId))
                    _zoneMap[zm.zoneId] = zm;
            }
        }

        private void OnPlayerIdle(OnPlayerIdleEvent e)
        {
            _playerIdle = true;

            if (_idleCoroutine != null)
                StopCoroutine(_idleCoroutine);

            _idleCoroutine = StartCoroutine(IdleMonologueCor());
        }

        private void OnPlayerMoved(OnPlayerMovedEvent e)
        {
            _playerIdle = false;

            if (_idleCoroutine != null)
            {
                StopCoroutine(_idleCoroutine);
                _idleCoroutine = null;
            }
        }

        private void OnZoneEntered(OnNarrativeZoneEnteredEvent e)
        {
            SetCurrentZone(e.ZoneId);
        }

        /// <summary>
        /// Waits for the idle threshold, then plays a random unused line from the current zone's pool.
        /// Repeats until the player moves or the zone changes.
        /// </summary>
        private IEnumerator IdleMonologueCor()
        {
            yield return new WaitForSeconds(_idleThreshold);

            while (_playerIdle)
            {
                float timeSinceLast = Time.time - _lastLineTime;
                if (timeSinceLast < _cooldownBetweenLines)
                {
                    yield return new WaitForSeconds(_cooldownBetweenLines - timeSinceLast);
                    continue;
                }

                string line = GetNextLine(_currentZoneId);
                if (!string.IsNullOrEmpty(line))
                {
                    PlayLine(line);
                    _lastLineTime = Time.time;
                }

                yield return new WaitForSeconds(_cooldownBetweenLines);
            }
        }

        /// <summary>
        /// Returns the next unused line from the specified zone's pool.
        /// Cycles through all lines before repeating. Returns empty string if zone not found.
        /// </summary>
        private string GetNextLine(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return "";
            if (!_zoneMap.TryGetValue(zoneId, out ZoneMonologue zm)) return "";
            if (zm.lines == null || zm.lines.Count == 0) return "";

            if (!_usedIndices.ContainsKey(zoneId))
                _usedIndices[zoneId] = new List<int>();

            List<int> used = _usedIndices[zoneId];

            // All lines used — reset pool
            if (used.Count >= zm.lines.Count)
                used.Clear();

            // Build available indices
            List<int> available = new List<int>();
            for (int i = 0; i < zm.lines.Count; i++)
            {
                if (!used.Contains(i))
                    available.Add(i);
            }

            int chosen = available[Random.Range(0, available.Count)];
            used.Add(chosen);
            return zm.lines[chosen];
        }

        private void PlayLine(string line)
        {
            // Display as world-space text if output is assigned
            if (_outputText != null)
                _outputText.text = line;

            // Publish to EventBus so DialogueSystem or AudioManager can react
            EventBus.Publish(new OnIdleMonologuePlayedEvent(line, _currentZoneId));
        }
    }
}
