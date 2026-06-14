using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using StateCraft.Core;

namespace StateCraft.UI
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        public event Action<string, Color> OnNotificationAdded;

        private UIDocument _uiDocument;
        private VisualElement _stack;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            var doc = GetComponent<UIDocument>();

            if (doc != null)
            {
                doc.sortingOrder = 105;
                _stack = doc.rootVisualElement.Q<VisualElement>("NotificationStack");
                doc.rootVisualElement.pickingMode = PickingMode.Ignore;
            }

            // FALLBACK: Global search if not found locally
            if (_stack == null)
            {
                var allDocs = FindObjectsOfType<UIDocument>();

                foreach (var d in allDocs)
                {
                    var found = d.rootVisualElement?.Q<VisualElement>("NotificationStack");

                    if (found != null)
                    {
                        _stack = found;
                        break;
                    }
                }
            }

            if (_stack == null)
            {
                Debug.LogWarning("[UI] NotificationManager: Could not find 'NotificationStack' in any UIDocument.");
            }
        }

        // =========================
        // EVENT BUS CONNECTION
        // =========================

        private void OnEnable()
        {
            EventBus.OnWorldEvent += HandleWorldEvent;
        }

        private void OnDisable()
        {
            EventBus.OnWorldEvent -= HandleWorldEvent;
        }

        private void HandleWorldEvent(WorldEvent worldEvent)
        {
            Color alertColor = GetSeverityColor(worldEvent.severity);

            AddNotification(
                worldEvent.title,
                alertColor
            );
        }

        private Color GetSeverityColor(AlertSeverity severity)
        {
            switch (severity)
            {
                case AlertSeverity.Critical:
                    return Color.red;

                case AlertSeverity.Important:
                    return new Color(1f, 0.6f, 0f);

                case AlertSeverity.Informational:
                    return Color.cyan;

                default:
                    return Color.white;
            }
        }

        // =========================
        // EXISTING NOTIFICATION LOGIC
        // =========================

        public void AddNotification(string message, Color color)
        {
            // Determine if critical based on color
            bool isCritical = color == Color.red;

            // War bulletins (gold/orange) stay longer so the player can actually read them
            bool isWarBulletin = color == new Color(0.9f, 0.75f, 0.25f) || color == new Color(1f, 0.5f, 0f);
            float displaySeconds = isCritical ? 10f : isWarBulletin ? 8f : 5f;

            string title = isCritical
                ? "CRITICAL ALERT"
                : isWarBulletin
                ? "WAR REPORT"
                : "INTELLIGENCE REPORT";

            ShowAlert(title, message, isCritical, displaySeconds);

            // Notify listeners (like NewsTickerUI)
            OnNotificationAdded?.Invoke(message, color);
        }

        public void ShowAlert(
            string title,
            string message,
            bool isCritical = false,
            float displaySeconds = 5f)
        {
            if (_stack == null) return;

            // Create Alert Card
            var card = new VisualElement();

            card.style.backgroundColor = isCritical
                ? new Color(0.15f, 0.05f, 0.05f, 0.95f)
                : new Color(0.05f, 0.1f, 0.15f, 0.95f);

            card.style.borderLeftWidth = 4;

            card.style.borderLeftColor = isCritical
                ? Color.red
                : new Color(0f, 0.82f, 1f);

            card.style.marginBottom = 10;

            // Padding
            card.style.paddingTop = 15;
            card.style.paddingBottom = 15;
            card.style.paddingLeft = 15;
            card.style.paddingRight = 15;

            // Borders
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderLeftWidth = 4;

            var borderColor = new Color(1f, 1f, 1f, 0.1f);

            card.style.borderTopColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderRightColor = borderColor;

            // Animation Start Position
            card.style.translate = new Translate(350, 0, 0);

            UIStateManager.ApplySafeTransition(
                card,
                new[] { "translate", "opacity" },
                0.4f
            );

            // =========================
            // TITLE
            // =========================

            var titleLabel = new Label(title.ToUpper());

            titleLabel.style.fontSize = 11;

            titleLabel.style.color = isCritical
                ? Color.red
                : new Color(0f, 0.82f, 1f);

            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            card.Add(titleLabel);

            // =========================
            // MESSAGE
            // =========================

            var msgLabel = new Label(message);

            msgLabel.style.fontSize = 10;
            msgLabel.style.color = Color.white;
            msgLabel.style.whiteSpace = WhiteSpace.Normal;

            card.Add(msgLabel);

            // =========================
            // ADD TO STACK
            // =========================

            _stack.Add(card);

            // Animate In
            StartCoroutine(AnimateIn(card));

            // Auto Dismiss
            StartCoroutine(DismissAfter(card, displaySeconds));

            // Pause Simulation On Critical
            if (isCritical && GameManager.Instance != null)
            {
                GameManager.Instance.isRealTime = false;

}
        }

        // =========================
        // ANIMATIONS
        // =========================

        private IEnumerator AnimateIn(VisualElement element)
        {
            yield return new WaitForSeconds(0.05f);

            element.style.translate = new Translate(0, 0, 0);
        }

        private IEnumerator DismissAfter(
            VisualElement element,
            float delay)
        {
            yield return new WaitForSeconds(delay);

            element.style.translate = new Translate(350, 0, 0);

            element.style.opacity = 0;

            yield return new WaitForSeconds(0.5f);

            _stack.Remove(element);
        }
    }
}