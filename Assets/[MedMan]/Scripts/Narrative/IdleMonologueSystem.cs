using System.Collections;
using UnityEngine;
using TMPro;
using NaughtyAttributes;
using MedMan.Core;

namespace MedMan.Narrative
{
    /// <summary>
    /// Plays idle monologue lines when the player remains stationary.
    /// Lines are taken from a DialogueLineSO array in sequence, looping when exhausted.
    /// The line prefab is a child of the Player — position is set manually in the scene.
    /// Subscribes to OnPlayerIdleEvent and OnPlayerMovedEvent via EventBus.
    /// Attach to the Player GameObject.
    /// </summary>
    public class IdleMonologueSystem : MonoBehaviour
    {
        // ── Fields ───────────────────────────────────────────────────────────

        [BoxGroup("Content")]
        [Tooltip("Sequence of dialogue lines played while the player is idle. Loops when exhausted.")]
        [SerializeField] private DialogueLineSO[] _dialogueLines;

#if UNITY_EDITOR
        [ShowNativeProperty]
        private string PreviewLine0 => ResolvePreview(0);
        [ShowNativeProperty]
        private string PreviewLine1 => ResolvePreview(1);
        [ShowNativeProperty]
        private string PreviewLine2 => ResolvePreview(2);
        [ShowNativeProperty]
        private string PreviewLine3 => ResolvePreview(3);
        [ShowNativeProperty]
        private string PreviewLine4 => ResolvePreview(4);
        [ShowNativeProperty]
        private string PreviewLine5 => ResolvePreview(5);
        [ShowNativeProperty]
        private string PreviewLine6 => ResolvePreview(6);
        [ShowNativeProperty]
        private string PreviewLine7 => ResolvePreview(7);
        [ShowNativeProperty]
        private string PreviewLine8 => ResolvePreview(8);
        [ShowNativeProperty]
        private string PreviewLine9 => ResolvePreview(9);
#endif

        [BoxGroup("Spawn")]
        [Tooltip("Child of the Player — defines position and rotation of spawned idle text.")]
        [SerializeField] private Transform _spawnPoint;

        [BoxGroup("Spawn")]
        [Tooltip("Prefab with NarrativeTextLine component.")]
        [SerializeField] private NarrativeTextLine _linePrefab;

        [BoxGroup("Spawn")]
        [Tooltip("NarrativeTextController to inject into spawned lines.")]
        [SerializeField] private NarrativeTextController _narrativeController;

        [BoxGroup("Timing")]
        [Tooltip("Seconds between consecutive idle lines while the player remains stationary.")]
        [Range(1f, 60f)]
        [SerializeField] private float _cooldownBetweenLines = 10f;

        [BoxGroup("Debug")]
        [ReadOnly]
        [SerializeField] private int _currentLineIndex = 0;

        // ── Private ──────────────────────────────────────────────────────────

        private bool      _playerIdle;
        private Coroutine _idleCoroutine;
        private Coroutine _visibilityCoroutine;
        private NarrativeTextLine _activeLine;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayerIdleEvent>(OnPlayerIdle);
            EventBus.Subscribe<OnPlayerMovedEvent>(OnPlayerMoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayerIdleEvent>(OnPlayerIdle);
            EventBus.Unsubscribe<OnPlayerMovedEvent>(OnPlayerMoved);
        }

        // ── Private Methods ──────────────────────────────────────────────────

        private void OnPlayerIdle(OnPlayerIdleEvent e)
        {
            _playerIdle = true;

            if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
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

            HideActiveLine();
        }

        private IEnumerator IdleMonologueCor()
        {
            while (_playerIdle)
            {
                SpawnLine();
                yield return new WaitForSeconds(_cooldownBetweenLines);
            }
        }

        private void SpawnLine()
        {
            if (_dialogueLines == null || _dialogueLines.Length == 0) return;
            if (_linePrefab == null)
            {
                Debug.LogWarning("[IdleMonologueSystem] No Line Prefab assigned.", this);
                return;
            }
            if (_spawnPoint == null)
            {
                Debug.LogWarning("[IdleMonologueSystem] No Spawn Point assigned.", this);
                return;
            }

            HideActiveLine();

            DialogueLineSO line = _dialogueLines[_currentLineIndex];
            _currentLineIndex = (_currentLineIndex + 1) % _dialogueLines.Length;

            // Spawn at spawn point position/rotation
            _activeLine = Instantiate(_linePrefab, _spawnPoint.position, _spawnPoint.rotation);

            // Inject controller explicitly — avoids GetComponentInParent hierarchy issues
            if (_narrativeController != null)
                _activeLine.SetController(_narrativeController);

            // Detach from player so text stays in world space
            _activeLine.transform.SetParent(null);

            // Apply always-on-top rendering
            TMP_Text tmp = _activeLine.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Material mat = new Material(tmp.fontMaterial);
                mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                mat.renderQueue = 4000;
                tmp.fontMaterial = mat;
            }

            _activeLine.Setup(line.LocalizedText.GetLocalizedString());
            _activeLine.Show();

            // Add PlayerVisibilityChecker and start watching
            PlayerVisibilityChecker checker = _activeLine.gameObject.AddComponent<PlayerVisibilityChecker>();
            if (_visibilityCoroutine != null) StopCoroutine(_visibilityCoroutine);
            _visibilityCoroutine = StartCoroutine(WatchVisibilityCor(checker));

            EventBus.Publish(new OnIdleMonologuePlayedEvent(line.name, ""));
        }

        private IEnumerator WatchVisibilityCor(PlayerVisibilityChecker checker)
        {
            // Wait until the line has been visible at least once
            yield return new WaitUntil(() => checker == null || checker.IsVisibleToPlayer);

            // Then wait until player looks away
            yield return new WaitUntil(() => checker == null || !checker.IsVisibleToPlayer);

            HideActiveLine();
        }

        private void HideActiveLine()
        {
            if (_visibilityCoroutine != null)
            {
                StopCoroutine(_visibilityCoroutine);
                _visibilityCoroutine = null;
            }

            if (_activeLine != null)
            {
                _activeLine.Hide();
                _activeLine = null;
            }
        }

#if UNITY_EDITOR
        private string ResolvePreview(int index)
        {
            if (_dialogueLines == null || index >= _dialogueLines.Length || _dialogueLines[index] == null)
                return "-";

            try
            {
                var stringRef  = _dialogueLines[index].LocalizedText;
                var settings   = UnityEditor.Localization.LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null) return "(no LocalizationSettings)";

                var locale = settings.GetAvailableLocales()?.GetLocale(
                    new UnityEngine.Localization.LocaleIdentifier("en"));
                if (locale == null) return "(en not found)";

                var collection = UnityEditor.Localization.LocalizationEditorSettings
                    .GetStringTableCollection(stringRef.TableReference.TableCollectionNameGuid);
                if (collection == null) return "(table not found)";

                var table = collection.GetTable(locale.Identifier)
                    as UnityEngine.Localization.Tables.StringTable;
                var entry = table?.GetEntry(stringRef.TableEntryReference.KeyId);
                return entry?.LocalizedValue ?? "(missing key)";
            }
            catch { return "(preview unavailable)"; }
        }
#endif
    }
}
