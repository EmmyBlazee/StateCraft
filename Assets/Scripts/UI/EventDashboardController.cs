using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    [DefaultExecutionOrder(-10)]
    public class EventDashboardController : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        private VisualElement _dashboardRoot;
        
        private Label _kpiActiveEvents, _kpiCritical, _kpiHigh, _kpiMedium;
        private VisualElement _activeEventsList, _upcomingEventsList;

        private void Awake()
        {
            _uiDoc = GetComponent<UIDocument>();
            if (_uiDoc != null) _root = _uiDoc.rootVisualElement;
            
            var dashboard = _root?.Q<VisualElement>("EventDashboard");
            if (dashboard != null)
            {
                _dashboardRoot = dashboard;
                InitializeElements(dashboard);
                dashboard.style.display = DisplayStyle.None;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshDashboard;
            RefreshDashboard();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshDashboard;
        }

        private void InitializeElements(VisualElement root)
        {
            root.Q<Button>("BtnCloseEventDashboard")?.RegisterCallback<ClickEvent>(_ => Hide());

            _kpiActiveEvents = root.Q<Label>("KpiActiveEvents");
            _kpiCritical = root.Q<Label>("KpiCritical");
            _kpiHigh = root.Q<Label>("KpiHigh");
            _kpiMedium = root.Q<Label>("KpiMedium");

            _activeEventsList = root.Q<VisualElement>("ActiveEventsList");
            _upcomingEventsList = root.Q<VisualElement>("UpcomingEventsList");
        }

        public void Show()
        {
            var panel = _dashboardRoot;
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshDashboard();
                UIStateManager.FixScrollViews(panel);
            }
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        public void RefreshDashboard()
        {
            if (GameManager.Instance == null) return;
            var player = GameManager.Instance.playerCountry;
            var evSys = GameManager.Instance.EventSystem;
            
            if (player == null || evSys == null) return;

            var pendingEvents = evSys.GetPendingEvents(player);

            // Populate KPIs
            int critical = 0, high = 0, medium = 0;
            foreach(var ev in pendingEvents)
            {
                if (ev.severity == "Critical") critical++;
                else if (ev.severity == "High") high++;
                else medium++;
            }

            if (_kpiActiveEvents != null) _kpiActiveEvents.text = pendingEvents.Count.ToString();
            if (_kpiCritical != null) _kpiCritical.text = critical.ToString();
            if (_kpiHigh != null) _kpiHigh.text = high.ToString();
            if (_kpiMedium != null) _kpiMedium.text = medium.ToString();

            // Populate Event Cards
            if (_activeEventsList != null)
            {
                _activeEventsList.Clear();
                if (pendingEvents.Count == 0)
                {
                    _activeEventsList.Add(new Label("No active situations require your attention.") { style = { color = Color.grey, marginTop = 20, unityTextAlign = TextAnchor.MiddleCenter } });
                }
                else
                {
                    foreach (var ev in pendingEvents)
                    {
                        _activeEventsList.Add(CreateEventCard(player, ev, evSys));
                    }
                }
            }

            // Populate Upcoming
            PopulateUpcomingEvents();
        }

        private VisualElement CreateEventCard(CountryDataSO player, GameEventSO ev, IEventSystem evSys)
        {
            var card = new VisualElement() { style = { backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopColor = new Color(1,1,1,0.1f), borderBottomColor = new Color(1,1,1,0.1f), borderLeftColor = new Color(1,1,1,0.1f), borderRightColor = new Color(1,1,1,0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, paddingLeft = 16, paddingRight = 16, paddingTop = 16, paddingBottom = 16, marginBottom = 12 } };
            
            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8 } };
            header.Add(new Label(ev.eventTitle) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16 } });

            Color sevCol = new Color(241/255f, 196/255f, 15/255f); // Medium
            if (ev.severity == "Critical") sevCol = new Color(255/255f, 82/255f, 82/255f);
            else if (ev.severity == "High") sevCol = new Color(255/255f, 152/255f, 0/255f);

            var sevLabel = new Label($"Severity: {ev.severity.ToUpper()}") { style = { color = sevCol, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } };
            header.Add(sevLabel);
            card.Add(header);

            card.Add(new Label(ev.description) { style = { color = new Color(0.8f, 0.8f, 0.8f), fontSize = 12, whiteSpace = WhiteSpace.Normal, marginBottom = 16 } });

            card.Add(new Label($"Time Remaining: {ev.timeRemainingDays} Days") { style = { color = Color.grey, fontSize = 10, marginBottom = 12 } });

            var choicesContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.FlexStart } };

            if (ev.choices != null)
            {
                for (int i = 0; i < ev.choices.Count; i++)
                {
                    int index = i;
                    var choice = ev.choices[i];
                    
                    var btnWrap = new VisualElement() { style = { width = Length.Percent(48), marginRight = 8, marginBottom = 8 } };
                    
                    var btn = new Button() { text = choice.choiceText };
                    btn.AddToClassList("button-premium");
                    btn.style.height = 36;
                    btn.style.fontSize = 11;
                    btn.RegisterCallback<ClickEvent>(_ => {
                        evSys.ResolvePendingEvent(player, ev, index);
                        RefreshDashboard();
                    });

                    btnWrap.Add(btn);

                    // Optional: show impacts below button
                    var impactStr = new List<string>();
                    if (choice.costUSD > 0) impactStr.Add($"-${choice.costUSD}B");
                    if (choice.stabilityEffect != 0) impactStr.Add($"{(choice.stabilityEffect > 0 ? "+" : "")}{choice.stabilityEffect} Stab");
                    if (choice.gdpEffect != 0) impactStr.Add($"{(choice.gdpEffect > 0 ? "+" : "")}{choice.gdpEffect * 100f}% GDP");

                    if (impactStr.Count > 0)
                    {
                        btnWrap.Add(new Label(string.Join(" | ", impactStr)) { style = { color = Color.grey, fontSize = 9, marginTop = 4, unityTextAlign = TextAnchor.MiddleCenter } });
                    }

                    choicesContainer.Add(btnWrap);
                }
            }

            card.Add(choicesContainer);
            return card;
        }

        private void PopulateUpcomingEvents()
        {
            if (_upcomingEventsList == null) return;
            _upcomingEventsList.Clear();

            // Mock upcoming events
            AddUpcoming("UN Summit", 3);
            AddUpcoming("National Budget", 18);
            AddUpcoming("Presidential Election", 94);
            AddUpcoming("Military Exercise", 6);
            AddUpcoming("Harvest Season", 15);
        }

        private void AddUpcoming(string title, int days)
        {
            var card = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopColor = new Color(1,1,1,0.1f), borderBottomColor = new Color(1,1,1,0.1f), borderLeftColor = new Color(1,1,1,0.1f), borderRightColor = new Color(1,1,1,0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 8, marginBottom = 6 } };
            card.Add(new Label(title) { style = { color = Color.white, fontSize = 11 } });
            card.Add(new Label($"{days} Days") { style = { color = new Color(0, 0.9f, 0.46f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            _upcomingEventsList.Add(card);
        }
    }
}
