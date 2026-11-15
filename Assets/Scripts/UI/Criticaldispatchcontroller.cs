using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using StateCraft.Systems;
using StateCraft.Data;
using StateCraft.Core;

namespace StateCraft.UI
{
    public class CriticalDispatchController : MonoBehaviour
    {
        public static CriticalDispatchController Instance { get; private set; }

        private VisualElement _root;
        private VisualElement _modalOverlay;
        private bool _isShowing = false;
        private bool _overlayBuilt = false;

        private readonly Queue<CriticalDispatch> _queue = new Queue<CriticalDispatch>();

        public struct CriticalDispatch
        {
            public string sender;
            public string subject;
            public string body;
            public Color  accentColor;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

public void ShowDispatch(string sender, string subject, string body, Color accentColor)
        {
            _queue.Enqueue(new CriticalDispatch
            {
                sender      = sender,
                subject     = subject,
                body        = body,
                accentColor = accentColor
            });

            if (!_isShowing)
                StartCoroutine(ShowNextDelayed());
        }

        private IEnumerator ShowNextDelayed()
        {
            yield return null;
            if (!_overlayBuilt)
            {
                BuildModalOverlay();
            }
            ShowNext();
        }

private void BuildModalOverlay()
        {
            var hostGO = new GameObject("CriticalDispatchHost");
            var hostDoc = hostGO.AddComponent<UIDocument>();
            
            var existingDoc = FindObjectOfType<UIDocument>();
            if (existingDoc != null) hostDoc.panelSettings = existingDoc.panelSettings;
            
            hostDoc.sortingOrder = 100;
            _root = hostDoc.rootVisualElement;

            _modalOverlay = new VisualElement();
            _modalOverlay.name = "CriticalDispatchOverlay";
            _modalOverlay.style.position        = Position.Absolute;
            _modalOverlay.style.top             = 0; _modalOverlay.style.left   = 0;
            _modalOverlay.style.right           = 0; _modalOverlay.style.bottom = 0;
            _modalOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.78f);
            _modalOverlay.style.justifyContent  = Justify.Center;
            _modalOverlay.style.alignItems      = Align.Center;
            _modalOverlay.style.display         = DisplayStyle.None;
            _modalOverlay.pickingMode           = PickingMode.Position;
            _modalOverlay.style.flexDirection   = FlexDirection.Column;

            _root.Add(_modalOverlay);
            _overlayBuilt = true;
        }

        private void ShowNext()
        {
            if (_queue.Count == 0)
            {
                _isShowing = false;
                if (_modalOverlay != null)
                    _modalOverlay.style.display = DisplayStyle.None;
                return;
            }

            _isShowing = true;
            var dispatch = _queue.Dequeue();
            PopulateModal(dispatch);

            if (_modalOverlay != null)
            {
                _modalOverlay.style.display = DisplayStyle.Flex;
                _modalOverlay.BringToFront();
            }
        }

        private void PopulateModal(CriticalDispatch d)
        {
            if (_modalOverlay == null) return;
            _modalOverlay.Clear();

            var card = new VisualElement();
            card.style.width    = new StyleLength(new Length(55f, LengthUnit.Percent));
            card.style.maxWidth = 620;
            card.style.backgroundColor       = new Color(0.04f, 0.04f, 0.07f, 0.99f);
            card.style.borderTopWidth        = 2; card.style.borderBottomWidth = 2;
            card.style.borderLeftWidth       = 2; card.style.borderRightWidth  = 2;
            card.style.borderTopColor        = d.accentColor;
            card.style.borderBottomColor     = d.accentColor;
            card.style.borderLeftColor       = d.accentColor;
            card.style.borderRightColor      = d.accentColor;
            card.style.borderTopLeftRadius   = 6; card.style.borderTopRightRadius    = 6;
            card.style.borderBottomLeftRadius= 6; card.style.borderBottomRightRadius = 6;
            card.style.paddingTop    = 0;
            card.style.paddingBottom = 20;
            card.style.paddingLeft   = 0;
            card.style.paddingRight  = 0;
            card.pickingMode = PickingMode.Position;

            var stripe = new VisualElement();
            stripe.style.height          = 4;
            stripe.style.backgroundColor = d.accentColor;
            stripe.style.marginBottom    = 0;
            card.Add(stripe);

            var header = new VisualElement();
            header.style.flexDirection   = FlexDirection.Row;
            header.style.justifyContent  = Justify.SpaceBetween;
            header.style.alignItems      = Align.Center;
            header.style.paddingTop      = 14; header.style.paddingBottom = 10;
            header.style.paddingLeft     = 20; header.style.paddingRight  = 20;
            header.style.backgroundColor = new Color(d.accentColor.r * 0.15f,
                                                      d.accentColor.g * 0.15f,
                                                      d.accentColor.b * 0.15f, 0.6f);

            var urgentLabel = new Label("⚠  URGENT DISPATCH");
            urgentLabel.style.color                    = d.accentColor;
            urgentLabel.style.fontSize                 = 9;
            urgentLabel.style.unityFontStyleAndWeight  = FontStyle.Bold;
            urgentLabel.style.letterSpacing            = 2f;
            header.Add(urgentLabel);

            if (_queue.Count > 0)
            {
                var queueLabel = new Label($"+{_queue.Count} MORE PENDING");
                queueLabel.style.color    = new Color(0.6f, 0.6f, 0.7f);
                queueLabel.style.fontSize = 8;
                header.Add(queueLabel);
            }
            card.Add(header);

            var senderBox = new VisualElement();
            senderBox.style.paddingTop    = 14; senderBox.style.paddingBottom = 10;
            senderBox.style.paddingLeft   = 20; senderBox.style.paddingRight  = 20;
            senderBox.style.borderBottomWidth = 1;
            senderBox.style.borderBottomColor = new Color(0.2f, 0.2f, 0.3f, 0.6f);

            var fromLabel = new Label($"FROM:  {d.sender.ToUpper()}");
            fromLabel.style.color       = new Color(0.5f, 0.5f, 0.65f);
            fromLabel.style.fontSize    = 8;
            fromLabel.style.marginBottom = 4;
            senderBox.Add(fromLabel);

            var subjectLabel = new Label(d.subject);
            subjectLabel.style.color                   = Color.white;
            subjectLabel.style.fontSize                = 13;
            subjectLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            subjectLabel.style.whiteSpace              = WhiteSpace.Normal;
            senderBox.Add(subjectLabel);
            card.Add(senderBox);

            var bodyScroll = new ScrollView();
            bodyScroll.style.maxHeight   = 280;
            bodyScroll.style.paddingTop  = 14; bodyScroll.style.paddingBottom = 14;
            bodyScroll.style.paddingLeft = 20; bodyScroll.style.paddingRight  = 20;
            UIStateManager.FixScrollViews(bodyScroll);

            var bodyLabel = new Label(d.body);
            bodyLabel.style.color     = new Color(0.82f, 0.82f, 0.88f);
            bodyLabel.style.fontSize  = 10;
            bodyLabel.style.whiteSpace = WhiteSpace.Normal;
            bodyScroll.Add(bodyLabel);
            card.Add(bodyScroll);

            var footer = new VisualElement();
            footer.style.flexDirection  = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.paddingTop     = 12;
            footer.style.paddingLeft    = 20; footer.style.paddingRight = 20;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);

            var ackBtn = new Button(() =>
            {
                var player = GameManager.Instance?.playerCountry;
                if (player?.inboxMessages != null)
                {
                    var msg = player.inboxMessages.Find(m =>
                        m.subject == d.subject && m.sender == d.sender && !m.isRead);
                    if (msg != null) msg.isRead = true;
                }
                ShowNext();
            }) { text = _queue.Count > 0 ? $"ACKNOWLEDGED  →  NEXT ({_queue.Count})" : "ACKNOWLEDGED  ✓  DISMISS" };

            ackBtn.style.height      = 34;
            ackBtn.style.paddingLeft = 20; ackBtn.style.paddingRight = 20;
            ackBtn.style.fontSize    = 10;
            ackBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            ackBtn.style.backgroundColor = new Color(d.accentColor.r * 0.3f,
                                                      d.accentColor.g * 0.3f,
                                                      d.accentColor.b * 0.3f, 0.9f);
            ackBtn.style.borderTopWidth    = 1; ackBtn.style.borderBottomWidth = 1;
            ackBtn.style.borderLeftWidth   = 1; ackBtn.style.borderRightWidth  = 1;
            ackBtn.style.borderTopColor    = d.accentColor;
            ackBtn.style.borderBottomColor = d.accentColor;
            ackBtn.style.borderLeftColor   = d.accentColor;
            ackBtn.style.borderRightColor  = d.accentColor;
            ackBtn.style.borderTopLeftRadius    = 4; ackBtn.style.borderTopRightRadius    = 4;
            ackBtn.style.borderBottomLeftRadius = 4; ackBtn.style.borderBottomRightRadius = 4;
            ackBtn.style.color = Color.white;
            footer.Add(ackBtn);
            card.Add(footer);

            _modalOverlay.Add(card);
        }
    }
}
