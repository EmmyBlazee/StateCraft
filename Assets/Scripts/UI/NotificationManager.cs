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

        public struct NotificationData
        {
            public string title;
            public string message;
            public Color color;
            public bool isCritical;
            public DateTime time;
        }

        private UIDocument _uiDocument;
        private VisualElement _stack;

        private List<NotificationData> _history = new List<NotificationData>();
        private VisualElement _historyModal;
        private ScrollView _historyScrollView;
        private Button _btnCloseHistory;
        private Button _btnClearHistory;
        private Label _unreadBadge;
        private int _unreadCount = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            var doc = GetComponent<UIDocument>();

            if (doc != null)
            {
                doc.sortingOrder = 105;
                _stack = doc.rootVisualElement.Q<VisualElement>("NotificationStack");
                _historyModal = doc.rootVisualElement.Q<VisualElement>("NotificationHistoryModal");
                _historyScrollView = doc.rootVisualElement.Q<ScrollView>("HistoryScrollView");
                _btnCloseHistory = doc.rootVisualElement.Q<Button>("BtnCloseHistory");
                _btnClearHistory = doc.rootVisualElement.Q<Button>("BtnClearHistory");
                doc.rootVisualElement.pickingMode = PickingMode.Ignore;
                if (_unreadBadge == null) _unreadBadge = doc.rootVisualElement.Q<Label>("NotificationBadge");
            }

            if (_stack == null || _historyModal == null || _unreadBadge == null)
            {
                var allDocs = FindObjectsOfType<UIDocument>();

                foreach (var d in allDocs)
                {
                    if (d == null || d.rootVisualElement == null) continue;
                    
                    if (_stack == null) _stack = d.rootVisualElement.Q<VisualElement>("NotificationStack");
                    if (_unreadBadge == null) _unreadBadge = d.rootVisualElement.Q<Label>("NotificationBadge");
                    if (_historyModal == null)
                    {
                        var modal = d.rootVisualElement.Q<VisualElement>("NotificationHistoryModal");
                        if (modal != null)
                        {
                            _historyModal = modal;
                            _historyScrollView = d.rootVisualElement.Q<ScrollView>("HistoryScrollView");
                            _btnCloseHistory = d.rootVisualElement.Q<Button>("BtnCloseHistory");
                            _btnClearHistory = d.rootVisualElement.Q<Button>("BtnClearHistory");
                        }
                    }

                    if (_stack != null && _historyModal != null && _unreadBadge != null) break;
                }
            }

            if (_stack == null)
            {
                Debug.LogWarning("[UI] NotificationManager: Could not find 'NotificationStack' in any UIDocument.");
            }

            BindHistoryControls();
        }

        private void BindHistoryControls()
        {
            if (_btnCloseHistory != null)
            {
                _btnCloseHistory.clicked -= OnCloseClicked;
                _btnCloseHistory.clicked += OnCloseClicked;
            }
            if (_btnClearHistory != null)
            {
                _btnClearHistory.clicked -= ClearHistory;
                _btnClearHistory.clicked += ClearHistory;
            }
        }

        private void OnCloseClicked() => ToggleHistoryModal(false);

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

public void AddNotification(string message, Color color)
        {
            bool isCritical = color == Color.red;

            bool isWarBulletin = color == new Color(0.9f, 0.75f, 0.25f) || color == new Color(1f, 0.5f, 0f);
            float displaySeconds = isCritical ? 10f : isWarBulletin ? 8f : 5f;

            string title = isCritical
                ? "CRITICAL ALERT"
                : isWarBulletin
                ? "WAR REPORT"
                : "INTELLIGENCE REPORT";

            _history.Insert(0, new NotificationData
            {
                title = title,
                message = message,
                color = color,
                isCritical = isCritical,
                time = DateTime.Now
            });

            if (_history.Count > 100) _history.RemoveAt(_history.Count - 1);
            if (_historyModal != null && _historyModal.style.display == DisplayStyle.Flex)
            {
                RefreshHistoryModal();
            }
            else
            {
                _unreadCount++;
                UpdateBadge();
            }

            ShowAlert(title, message, isCritical, displaySeconds);

            OnNotificationAdded?.Invoke(message, color);
        }

        private void UpdateBadge()
        {
            if (_unreadBadge == null) return;
            if (_unreadCount <= 0)
            {
                _unreadBadge.style.display = DisplayStyle.None;
            }
            else
            {
                _unreadBadge.text = _unreadCount > 99 ? "99+" : _unreadCount.ToString();
                _unreadBadge.style.display = DisplayStyle.Flex;
            }
        }

        public void ShowAlert(
            string title,
            string message,
            bool isCritical = false,
            float displaySeconds = 5f)
        {
            if (_stack == null) return;

            var card = new VisualElement();

            card.style.backgroundColor = isCritical
                ? new Color(0.15f, 0.05f, 0.05f, 0.95f)
                : new Color(0.05f, 0.1f, 0.15f, 0.95f);

            card.style.borderLeftWidth = 4;

            card.style.borderLeftColor = isCritical
                ? Color.red
                : new Color(0f, 0.82f, 1f);

            card.style.marginBottom = 10;

            card.style.paddingTop = 15;
            card.style.paddingBottom = 15;
            card.style.paddingLeft = 15;
            card.style.paddingRight = 15;

            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderLeftWidth = 4;

            var borderColor = new Color(1f, 1f, 1f, 0.1f);

            card.style.borderTopColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderRightColor = borderColor;

            card.style.translate = new Translate(350, 0, 0);

            UIStateManager.ApplySafeTransition(
                card,
                new[] { "translate", "opacity" },
                0.4f
            );

var titleLabel = new Label(title.ToUpper());

            titleLabel.style.fontSize = 11;

            titleLabel.style.color = isCritical
                ? Color.red
                : new Color(0f, 0.82f, 1f);

            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            card.Add(titleLabel);

var msgLabel = new Label(message);

            msgLabel.style.fontSize = 10;
            msgLabel.style.color = Color.white;
            msgLabel.style.whiteSpace = WhiteSpace.Normal;

            card.Add(msgLabel);

_stack.Add(card);

            StartCoroutine(AnimateIn(card));

            StartCoroutine(DismissAfter(card, displaySeconds));

            if (isCritical && GameManager.Instance != null)
            {
                GameManager.Instance.isRealTime = false;

}
        }

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

public void ToggleHistoryModal(bool? state = null)
        {
            if (_historyModal == null)
            {
                var allDocs = FindObjectsOfType<UIDocument>();
                foreach (var d in allDocs)
                {
                    if (d?.rootVisualElement == null) continue;
                    var modal = d.rootVisualElement.Q<VisualElement>("NotificationHistoryModal");
                    if (modal != null)
                    {
                        _historyModal = modal;
                        _historyScrollView = d.rootVisualElement.Q<ScrollView>("HistoryScrollView");
                        _btnCloseHistory = d.rootVisualElement.Q<Button>("BtnCloseHistory");
                        _btnClearHistory = d.rootVisualElement.Q<Button>("BtnClearHistory");
                        BindHistoryControls();
                        break;
                    }
                }
            }

            if (_historyModal == null) return;

            bool isVisible = _historyModal.style.display == DisplayStyle.Flex;
            bool newState = state ?? !isVisible;

            if (newState)
            {
                _historyModal.style.display = DisplayStyle.Flex;
                RefreshHistoryModal();
                _unreadCount = 0;
                UpdateBadge();
            }
            else
            {
                _historyModal.style.display = DisplayStyle.None;
            }
        }

        private void ClearHistory()
        {
            _history.Clear();
            RefreshHistoryModal();
        }

        private void RefreshHistoryModal()
        {
            if (_historyScrollView == null) return;
            _historyScrollView.Clear();

            if (_history.Count == 0)
            {
                var lbl = new Label("NO RECENT NOTIFICATIONS");
                lbl.style.color = new Color(0f, 0.85f, 1f, 0.7f);
                lbl.style.fontSize = 11;
                lbl.style.marginTop = 20;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                _historyScrollView.Add(lbl);
                return;
            }

            foreach (var notif in _history)
            {
                var card = new VisualElement();
                card.style.width = Length.Percent(100);
                card.style.flexShrink = 0;
                
                card.style.backgroundColor = notif.isCritical
                    ? new Color(0.15f, 0.05f, 0.05f, 0.95f)
                    : new Color(0.05f, 0.1f, 0.15f, 0.95f);

                card.style.borderLeftWidth = 4;
                card.style.borderLeftColor = notif.color;
                card.style.marginBottom = 10;
                card.style.paddingTop = 15;
                card.style.paddingBottom = 15;
                card.style.paddingLeft = 15;
                card.style.paddingRight = 15;
                
                card.style.borderTopWidth = 1;
                card.style.borderBottomWidth = 1;
                card.style.borderRightWidth = 1;
                var borderColor = new Color(1f, 1f, 1f, 0.1f);
                card.style.borderTopColor = borderColor;
                card.style.borderBottomColor = borderColor;
                card.style.borderRightColor = borderColor;

                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.justifyContent = Justify.SpaceBetween;

                var titleLabel = new Label(notif.title.ToUpper());
                titleLabel.style.fontSize = 11;
                titleLabel.style.color = notif.color;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                var timeLabel = new Label(notif.time.ToString("HH:mm"));
                timeLabel.style.fontSize = 10;
                timeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);

                headerRow.Add(titleLabel);
                headerRow.Add(timeLabel);

                var msgLabel = new Label(notif.message);
                msgLabel.style.fontSize = 10;
                msgLabel.style.color = Color.white;
                msgLabel.style.whiteSpace = WhiteSpace.Normal;
                msgLabel.style.marginTop = 5;

                card.Add(headerRow);
                card.Add(msgLabel);
                
                _historyScrollView.Add(card);
            }
        }
    }
}