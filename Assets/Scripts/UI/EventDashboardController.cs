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
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Events")
                              ?? _root?.Q<VisualElement>("EventDashboard");
            }

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
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Events")
                              ?? _root?.Q<VisualElement>("EventDashboard")
                              ?? _dashboardRoot;
            }

            if (GameManager.Instance == null) return;
            var player = GameManager.Instance.playerCountry;
            var evSys = GameManager.Instance.EventSystem;
            
            if (player == null || evSys == null) return;

            var pendingEvents = evSys.GetPendingEvents(player);

            int critical = 0, high = 0, medium = 0;
            foreach(var pEv in pendingEvents)
            {
                if (pEv.ev.severity == "Critical") critical++;
                else if (pEv.ev.severity == "High") high++;
                else medium++;
            }

            bool budgetOverdue = (GameManager.Instance.TurnSystem.CurrentTurn - player.lastBudgetApprovalTurn) >= 3;
            if (budgetOverdue) critical++;
            
            bool hasElectionResult = !string.IsNullOrEmpty(player.latestElectionResult);
            if (hasElectionResult) high++;

            int totalActive = pendingEvents.Count + (budgetOverdue ? 1 : 0) + (hasElectionResult ? 1 : 0);
            if (_kpiActiveEvents != null) _kpiActiveEvents.text = totalActive.ToString();
            if (_kpiCritical != null) _kpiCritical.text = critical.ToString();
            if (_kpiHigh != null) _kpiHigh.text = high.ToString();
            if (_kpiMedium != null) _kpiMedium.text = medium.ToString();

            if (_activeEventsList != null)
            {
                _activeEventsList.Clear();
                if (pendingEvents.Count == 0 && !budgetOverdue && !hasElectionResult)
                {
                    _activeEventsList.Add(new Label("No active situations require your attention.") { style = { color = Color.grey, marginTop = 20, unityTextAlign = TextAnchor.MiddleCenter } });
                }
                else
                {
                    if (hasElectionResult)
                    {
                        _activeEventsList.Add(CreateElectionResultCard(player));
                    }

                    if (budgetOverdue)
                    {
                        _activeEventsList.Add(CreateBudgetWarningCard());
                    }

                    foreach (var pEv in pendingEvents)
                    {
                        _activeEventsList.Add(CreateEventCard(player, pEv, evSys));
                    }
                }
            }

            PopulateUpcomingEvents();
        }

        private VisualElement CreateBudgetWarningCard()
        {
            var card = new VisualElement();
            card.AddToClassList("premium-card");
            card.style.marginBottom = 16;
            card.style.paddingLeft = 20; card.style.paddingRight = 20; card.style.paddingTop = 20; card.style.paddingBottom = 20;
            card.style.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 0.95f);
            
            Color sevCol = new Color(255/255f, 82/255f, 82/255f);

            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = sevCol;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 12, alignItems = Align.Center } };
            header.Add(new Label("GOVERNMENT SHUTDOWN IMMINENT") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16 } });

            var sevBadge = new VisualElement() { style = { backgroundColor = new Color(sevCol.r, sevCol.g, sevCol.b, 0.15f), borderBottomColor = sevCol, borderTopColor = sevCol, borderLeftColor = sevCol, borderRightColor = sevCol, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4 } };
            sevBadge.Add(new Label("CRITICAL") { style = { color = sevCol, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(sevBadge);
            card.Add(header);

            card.Add(new Label("The national budget is severely overdue. Government services are beginning to halt, and public unrest is rising rapidly. You must pass a new budget to restore order.") { style = { color = new Color(0.7f, 0.75f, 0.8f), fontSize = 13, whiteSpace = WhiteSpace.Normal, marginBottom = 16 } });

            var timeWrap = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 16 } };
            timeWrap.Add(new Label("TIME REMAINING:") { style = { color = Color.grey, fontSize = 10, marginRight = 6 } });
            timeWrap.Add(new Label("OVERDUE") { style = { color = sevCol, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
            card.Add(timeWrap);

            var choicesContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.SpaceBetween } };
            
            var btnWrap = new VisualElement() { style = { width = new StyleLength(new Length(100, LengthUnit.Percent)), marginBottom = 8 } };
            var btn = new Button();
            btn.text = "1. OPEN ECONOMY DASHBOARD";
            btn.AddToClassList("button-premium");
            btn.style.height = 38;
            btn.style.fontSize = 12;
            btn.RegisterCallback<ClickEvent>(_ => {
                MainDashboardToolkitController.Instance?.ShowPanel("Economy");
            });

            btnWrap.Add(btn);

            var impactBox = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.Center, marginTop = 4 } };
            impactBox.Add(new Label("-5 STAB per month") { style = { color = sevCol, fontSize = 10, marginRight = 6, unityFontStyleAndWeight = FontStyle.Bold } });
            btnWrap.Add(impactBox);
            
            choicesContainer.Add(btnWrap);
            card.Add(choicesContainer);
            return card;
        }

        private VisualElement CreateElectionResultCard(CountryDataSO player)
        {
            var card = new VisualElement();
            card.AddToClassList("premium-card");
            card.style.marginBottom = 16;
            card.style.paddingLeft = 20; card.style.paddingRight = 20; card.style.paddingTop = 20; card.style.paddingBottom = 20;
            card.style.backgroundColor = new Color(0.12f, 0.08f, 0.16f, 0.95f);
            
            Color sevCol = new Color(0.7f, 0.3f, 1f);

            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = sevCol;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 12, alignItems = Align.Center } };
            header.Add(new Label("NATIONAL ELECTION RESULTS") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16 } });

            var sevBadge = new VisualElement() { style = { backgroundColor = new Color(sevCol.r, sevCol.g, sevCol.b, 0.15f), borderBottomColor = sevCol, borderTopColor = sevCol, borderLeftColor = sevCol, borderRightColor = sevCol, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4 } };
            sevBadge.Add(new Label("OFFICIAL") { style = { color = sevCol, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(sevBadge);
            card.Add(header);

            card.Add(new Label(player.latestElectionResult) { style = { color = new Color(0.8f, 0.85f, 0.9f), fontSize = 13, whiteSpace = WhiteSpace.Normal, marginBottom = 16 } });

            var choicesContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.FlexEnd } };
            
            var btnWrap = new VisualElement() { style = { width = new StyleLength(new Length(100, LengthUnit.Percent)), marginBottom = 8 } };
            var btn = new Button();
            bool isDefeat = player.latestElectionResult.Contains("DEFEAT");
            btn.text = isDefeat ? "1. ACKNOWLEDGE DEFEAT" : "1. ACKNOWLEDGE RESULT";
            btn.AddToClassList("button-premium");
            btn.style.height = 38;
            btn.style.fontSize = 12;
            btn.RegisterCallback<ClickEvent>(_ => {
                player.latestElectionResult = "";
                
                if (isDefeat)
                    Hide();
                else
                    RefreshDashboard();
            });

            btnWrap.Add(btn);
            choicesContainer.Add(btnWrap);
            card.Add(choicesContainer);
            
            return card;
        }

        private VisualElement CreateEventCard(CountryDataSO player, PendingEvent pendingEvent, IEventSystem evSys)
        {
            var ev = pendingEvent.ev;
            var card = new VisualElement();
            card.AddToClassList("premium-card");
            card.style.marginBottom = 16;
            card.style.paddingLeft = 20; card.style.paddingRight = 20; card.style.paddingTop = 20; card.style.paddingBottom = 20;
            card.style.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 0.95f);
            
            Color sevCol = new Color(241/255f, 196/255f, 15/255f);
            if (ev.severity == "Critical") sevCol = new Color(255/255f, 82/255f, 82/255f);
            else if (ev.severity == "High") sevCol = new Color(255/255f, 152/255f, 0/255f);

            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = sevCol;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 12, alignItems = Align.Center } };
            header.Add(new Label(ev.eventTitle) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16 } });

            var sevBadge = new VisualElement() { style = { backgroundColor = new Color(sevCol.r, sevCol.g, sevCol.b, 0.15f), borderBottomColor = sevCol, borderTopColor = sevCol, borderLeftColor = sevCol, borderRightColor = sevCol, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4 } };
            sevBadge.Add(new Label(ev.severity.ToUpper()) { style = { color = sevCol, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(sevBadge);
            card.Add(header);

            card.Add(new Label(ev.description) { style = { color = new Color(0.7f, 0.75f, 0.8f), fontSize = 13, whiteSpace = WhiteSpace.Normal, marginBottom = 16 } });

            var timeWrap = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 16 } };
            timeWrap.Add(new Label("TIME REMAINING:") { style = { color = Color.grey, fontSize = 10, marginRight = 6 } });
            timeWrap.Add(new Label($"{pendingEvent.daysRemaining} Days") { style = { color = new Color(0.9f, 0.6f, 0f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
            card.Add(timeWrap);

            var choicesContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.SpaceBetween } };

            if (ev.choices != null)
            {
                for (int i = 0; i < ev.choices.Count; i++)
                {
                    int index = i;
                    var choice = ev.choices[i];
                    
                    var btnWrap = new VisualElement() { style = { width = Length.Percent(48), marginBottom = 12 } };
                    
                    var btn = new Button() { text = choice.choiceText };
                    btn.AddToClassList("button-premium");
                    btn.style.height = 38;
                    btn.style.fontSize = 12;
                    btn.RegisterCallback<ClickEvent>(_ => {
                        evSys.ResolvePendingEvent(player, ev, index);
                        RefreshDashboard();
                    });

                    btnWrap.Add(btn);

                    var impactBox = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.Center, marginTop = 4 } };
                    
                    string sym = string.IsNullOrEmpty(player.currencySymbol) ? "₦" : player.currencySymbol;
                    
                    if (choice.costUSD > 0) 
                        impactBox.Add(new Label($"-{sym}{choice.costUSD}B") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 10, marginRight = 6, unityFontStyleAndWeight = FontStyle.Bold } });
                    if (choice.stabilityEffect != 0) 
                        impactBox.Add(new Label($"{(choice.stabilityEffect > 0 ? "+" : "")}{choice.stabilityEffect} STAB") { style = { color = choice.stabilityEffect > 0 ? new Color(0.14f, 0.93f, 0.34f) : new Color(255/255f, 82/255f, 82/255f), fontSize = 10, marginRight = 6, unityFontStyleAndWeight = FontStyle.Bold } });
                    if (choice.gdpEffect != 0) 
                        impactBox.Add(new Label($"{(choice.gdpEffect > 0 ? "+" : "")}{choice.gdpEffect * 100f}% GDP") { style = { color = choice.gdpEffect > 0 ? new Color(0.14f, 0.93f, 0.34f) : new Color(255/255f, 82/255f, 82/255f), fontSize = 10, marginRight = 6, unityFontStyleAndWeight = FontStyle.Bold } });

                    if (impactBox.childCount == 0)
                        impactBox.Add(new Label("No Immediate Impact") { style = { color = Color.grey, fontSize = 10 } });

                    btnWrap.Add(impactBox);
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

            var gm = GameManager.Instance;
            int monthsUntilElection = 48;

            if (gm != null && gm.TurnSystem != null)
            {
                int currentTurn = gm.TurnSystem.CurrentTurn;
                int electionEvery = 48;
                monthsUntilElection = electionEvery - (currentTurn % electionEvery);
                if (monthsUntilElection == electionEvery) monthsUntilElection = 0;
            }

            string electionTimeframe = monthsUntilElection == 0 ? "THIS MONTH" : 
                                       monthsUntilElection < 12 ? $"{monthsUntilElection} Months" : 
                                       $"{monthsUntilElection / 12}y {monthsUntilElection % 12}m";

            AddUpcoming("National Election", electionTimeframe, "Domestic");

            int monthsSinceBudget = 0;
            if (gm != null && gm.TurnSystem != null && gm.playerCountry != null)
            {
                monthsSinceBudget = gm.TurnSystem.CurrentTurn - gm.playerCountry.lastBudgetApprovalTurn;
            }
            
            if (monthsSinceBudget >= 3)
            {
                AddUpcoming("National Budget Review", "OVERDUE", "Economy", true);
            }
            else
            {
                int monthsLeft = 3 - monthsSinceBudget;
                AddUpcoming("National Budget Review", $"{monthsLeft} Months", "Economy");
            }

            AddUpcoming("UN General Assembly", "3 Months", "Global");
            AddUpcoming("Joint Military Exercise", "8 Months", "Military");
        }

        private void AddUpcoming(string title, string timeframe, string category, bool isWarning = false)
        {
            var card = new VisualElement();
            card.AddToClassList("premium-card");
            card.style.marginBottom = 8;
            card.style.paddingLeft = 12; card.style.paddingRight = 12; card.style.paddingTop = 10; card.style.paddingBottom = 10;
            card.style.backgroundColor = new Color(0.08f, 0.11f, 0.15f, 0.9f);
            card.style.borderLeftWidth = 2;
            
            Color catColor = isWarning ? new Color(255/255f, 82/255f, 82/255f) : new Color(0.1f, 0.5f, 0.9f);
            Color timeColor = isWarning ? new Color(255/255f, 82/255f, 82/255f) : new Color(0.9f, 0.6f, 0f);

            card.style.borderLeftColor = catColor;

            var topRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
            topRow.Add(new Label(category.ToUpper()) { style = { color = catColor, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
            topRow.Add(new Label(timeframe) { style = { color = timeColor, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            
            card.Add(topRow);
            card.Add(new Label(title) { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });

            _upcomingEventsList.Add(card);
        }
    }
}
