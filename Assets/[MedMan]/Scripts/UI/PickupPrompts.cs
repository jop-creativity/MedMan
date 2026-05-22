using UnityEngine;
using TMPro;
using MedMan.Core;

namespace MedMan.UI
{
    /// <summary>
    /// Shows and hides the take prompt text during pickup interactions.
    /// Subscribes to OnPickupPromptShownEvent via EventBus.
    /// Attach to a TextMeshProUGUI GameObject on the HUD Canvas.
    /// </summary>
    public class PickupPrompt : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI _promptText;
        [SerializeField] private string _promptMessage = "[E] Take";

        // ─────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────

        private void Awake()
        {
            _promptText.text = _promptMessage;
            _promptText.gameObject.SetActive(false);
        }

        /// <summary>Subscribes to pickup prompt visibility events.</summary>
        private void OnEnable()
        {
            EventBus.Subscribe<OnPickupPromptShownEvent>(HandlePromptShown);
        }

        /// <summary>Unsubscribes from pickup prompt visibility events.</summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPickupPromptShownEvent>(HandlePromptShown);
        }

        // ─────────────────────────────────────────
        // Private methods
        // ─────────────────────────────────────────

        /// <summary>
        /// Shows or hides the prompt text based on the event.
        /// </summary>
        private void HandlePromptShown(OnPickupPromptShownEvent e)
        {
            _promptText.gameObject.SetActive(e.IsVisible);
        }
    }
}