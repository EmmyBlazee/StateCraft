using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Interfaces;
using StateCraft.Systems;

namespace StateCraft.UI
{
    public class EventDashboardController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;
        
        private VisualElement _eventDashboard;
        private Button _btnClose;

        private Label _tickerText;

        private ScrollView _alertsScroll;
        private ScrollView _troopsScroll;
        private ScrollView _suggestionsScroll;
        private ScrollView _historyScroll;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null) _root = _uiDocument.rootVisualElement;

            _eventDashboard = _root?.Q<VisualElement>("EventDashboard");
            if (_eventDashboard != null)
            {
                InitializeElements();
                _eventDashboard.style.display = DisplayStyle.None;
            }
        }

        private void InitializeElements()
        {
            _btnClose = _eventDashboard.Q<Button>("BtnCloseEventDashboard");
            if (_btnClose != null) _btnClose.clicked += Hide;

            _tickerText = _eventDashboard.Q<Label>("TickerText");
            _alertsScroll = _eventDashboard.Q<ScrollView>("ActiveAlertsScrollView");
            _troopsScroll = _eventDashboard.Q<ScrollView>("TroopTrackerScrollView");
            _suggestionsScroll = _eventDashboard.Q<ScrollView>("SuggestionsScrollView");
            _historyScroll = _eventDashboard.Q<ScrollView>("HistoryLogScrollView");

            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
            {
                GameManager.Instance.TurnSystem.OnTurnEnded += Refresh;
            }
        }

        public void Show()
        {
            if (_eventDashboard == null) return;
            _eventDashboard.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        private void Refresh()
        {
            if (_eventDashboard == null || _eventDashboard.style.display == DisplayStyle.None) return;

            var country = GameManager.Instance.playerCountry;
            if (country == null) return;

            PopulateTicker(country);
            PopulateAlerts(country);
            PopulateTroops(country);
            PopulateSuggestions(country);
            PopulateHistory();
        }

        private void PopulateTicker(CountryDataSO country)
        {
            if (_tickerText == null) return;
            
            List<string> alerts = new List<string>();
            if (GameManager.Instance.WarSystem.GetWarStage(country) == WarStage.Conflict)
                alerts.Add($"🔴 WAR DECLARED: {country.countryName} is actively engaging {country.warEnemyName}.");
            foreach(var r in country.regions)
            {
                if (r.insurgencyLevel > 30f)
                    alerts.Add($"⚠️ INSURGENCY: Militants active in {r.regionName} (Risk: {r.insurgencyLevel:N1}%).");
            }

            if (alerts.Count == 0)
            {
                _tickerText.text = "🟢 GLOBAL CONDITIONS STABLE 🟢 NO CRITICAL ALERTS";
                _tickerText.style.color = new Color(0, 1, 0.5f);
            }
            else
            {
                _tickerText.text = string.Join("   ||   ", alerts);
                _tickerText.style.color = new Color(1, 0.2f, 0.2f);
            }
        }

        private void PopulateAlerts(CountryDataSO country)
        {
            if (_alertsScroll == null) return;
            _alertsScroll.Clear();

            var warStage = GameManager.Instance.WarSystem.GetWarStage(country);
            if (warStage >= WarStage.Tension)
            {
                _alertsScroll.Add(CreateAlertCard("CRITICAL", $"Border tensions escalating with {country.warEnemyName}.", "alert-critical"));
            }

            foreach(var r in country.regions)
            {
                if (r.insurgencyLevel > 70f)
                    _alertsScroll.Add(CreateAlertCard("CRITICAL", $"Insurgent takeover imminent in {r.regionName}!", "alert-critical"));
                else if (r.insurgencyLevel > 40f)
                    _alertsScroll.Add(CreateAlertCard("HIGH", $"Heavy fighting reported in {r.regionName}.", "alert-high"));
            }

            if (country.treasury < 0)
                _alertsScroll.Add(CreateAlertCard("HIGH", "Treasury is NEGATIVE. Total default imminent.", "alert-high"));
            else if (country.debtServiceRatio < 50f)
                _alertsScroll.Add(CreateAlertCard("MEDIUM", "Debt service missing payments. Compounding interest.", "alert-medium"));

            _alertsScroll.Add(BuildElectionStatusCard(country));

            if (_alertsScroll.childCount == 0)
            {
                _alertsScroll.Add(CreateAlertCard("LOW", "No major threats detected.", "alert-low"));
            }
        }

        private VisualElement CreateAlertCard(string priority, string text, string cssClass)
        {
            var card = new VisualElement();
            card.AddToClassList("alert-card");
            card.AddToClassList(cssClass);
            
            var lblPriority = new Label(priority);
            lblPriority.style.unityFontStyleAndWeight = FontStyle.Bold;
            lblPriority.style.fontSize = 11;
            
            var lblText = new Label(text);
            lblText.style.whiteSpace = WhiteSpace.Normal;
            lblText.style.fontSize = 13;
            lblText.style.color = Color.white;

            card.Add(lblPriority);
            card.Add(lblText);
            return card;
        }

        private VisualElement BuildElectionStatusCard(CountryDataSO country)
        {
            var gm = GameManager.Instance;
            int currentTurn = gm?.TurnSystem?.CurrentTurn ?? 1;
            int electionEvery = 48;
            int monthsUntil = electionEvery - (currentTurn % electionEvery);
            if (monthsUntil == electionEvery) monthsUntil = 0;

            float stabilityScore = country.stability * 0.40f;
            float econScore      = country.income > country.spending ? 30f : 
                                   Mathf.Clamp(30f + ((country.income - country.spending) * 5f), 0f, 30f);
            float prestigeScore  = country.globalPrestige * 0.15f;
            float militaryScore  = 10f;
            if (country.warExhaustion > 50f) militaryScore -= 5f;
            if (country.totalKIA > 5000)     militaryScore -= 5f;
            float inflationPenalty = Mathf.Clamp(country.inflationRate * 0.15f, 0f, 10f);
            float approval = Mathf.Clamp(stabilityScore + econScore + prestigeScore + militaryScore - inflationPenalty, 0f, 100f);

            bool winning = approval >= 50f;
            Color accentColor = approval >= 65f ? new Color(0.2f, 0.85f, 0.4f) :
                                approval >= 50f ? new Color(0.95f, 0.78f, 0.1f) :
                                                  new Color(1f, 0.3f, 0.3f);

            var card = new VisualElement();
            card.style.marginBottom      = 10;
            card.style.paddingTop        = 14; card.style.paddingBottom = 14;
            card.style.paddingLeft       = 14; card.style.paddingRight  = 14;
            card.style.backgroundColor   = new Color(0.05f, 0.05f, 0.08f, 0.98f);
            card.style.borderLeftWidth   = 4;
            card.style.borderLeftColor   = accentColor;
            card.style.borderTopWidth    = 1; card.style.borderBottomWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor    = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            card.style.borderBottomColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            card.style.borderRightColor  = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            card.style.borderTopLeftRadius    = 4; card.style.borderTopRightRadius    = 4;
            card.style.borderBottomLeftRadius = 4; card.style.borderBottomRightRadius = 4;

            var headerRow = new VisualElement();
            headerRow.style.flexDirection  = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.marginBottom   = 10;

            headerRow.Add(new Label("🗳  PRESIDENTIAL ELECTION") {
                style = { color = Color.white, fontSize = 11,
                          unityFontStyleAndWeight = FontStyle.Bold }
            });

            string timeText = monthsUntil == 0 ? "THIS MONTH!" :
                              monthsUntil <= 12 ? $"IN {monthsUntil} MONTHS" :
                              $"IN {monthsUntil / 12}y {monthsUntil % 12}m";
            var timeLabel = new Label(timeText);
            timeLabel.style.color           = monthsUntil <= 6 ? new Color(1f, 0.5f, 0.2f) : new Color(0.6f, 0.6f, 0.7f);
            timeLabel.style.fontSize        = 9;
            timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(timeLabel);
            card.Add(headerRow);

            var barBg = new VisualElement();
            barBg.style.height          = 6;
            barBg.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            barBg.style.borderTopLeftRadius    = 3; barBg.style.borderTopRightRadius    = 3;
            barBg.style.borderBottomLeftRadius = 3; barBg.style.borderBottomRightRadius = 3;
            barBg.style.marginBottom    = 4;

            var barFill = new VisualElement();
            barFill.style.height          = 6;
            barFill.style.width           = Length.Percent(approval);
            barFill.style.backgroundColor = accentColor;
            barFill.style.borderTopLeftRadius    = 3; barFill.style.borderTopRightRadius    = 3;
            barFill.style.borderBottomLeftRadius = 3; barFill.style.borderBottomRightRadius = 3;
            barBg.Add(barFill);
            card.Add(barBg);

            var approvalRow = new VisualElement();
            approvalRow.style.flexDirection  = FlexDirection.Row;
            approvalRow.style.justifyContent = Justify.SpaceBetween;
            approvalRow.style.marginBottom   = 10;

            approvalRow.Add(new Label($"APPROVAL RATING: {approval:F0}%") {
                style = { color = accentColor, fontSize = 10,
                          unityFontStyleAndWeight = FontStyle.Bold }
            });

            string status = approval >= 70f ? "STRONG LEAD" :
                            approval >= 60f ? "COMFORTABLE" :
                            approval >= 50f ? "MARGINAL WIN" :
                            approval >= 40f ? "LOSING" : "CRITICAL — DEFEAT LIKELY";
            approvalRow.Add(new Label(status) {
                style = { color = accentColor, fontSize = 9 }
            });
            card.Add(approvalRow);

            var breakdown = new VisualElement();
            breakdown.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.8f);
            breakdown.style.paddingTop  = 8; breakdown.style.paddingBottom = 8;
            breakdown.style.paddingLeft = 10; breakdown.style.paddingRight = 10;
            breakdown.style.borderTopLeftRadius    = 3; breakdown.style.borderTopRightRadius    = 3;
            breakdown.style.borderBottomLeftRadius = 3; breakdown.style.borderBottomRightRadius = 3;
            breakdown.style.marginBottom = 8;

            void AddBreakdownRow(string label, float score, float max, bool isPositive = true)
            {
                var row = new VisualElement();
                row.style.flexDirection  = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.marginBottom   = 3;
                row.Add(new Label(label) { style = { color = new Color(0.55f, 0.55f, 0.65f), fontSize = 8 } });
                Color scoreCol = isPositive ? (score >= max * 0.6f ? new Color(0.3f, 0.85f, 0.45f) :
                                               score >= max * 0.3f ? new Color(1f, 0.78f, 0.2f) :
                                                                      new Color(1f, 0.4f, 0.4f))
                                            : new Color(1f, 0.4f, 0.4f);
                string prefix = isPositive ? "+" : "-";
                row.Add(new Label($"{prefix}{score:F0} pts") { style = { color = scoreCol, fontSize = 8,
                    unityFontStyleAndWeight = FontStyle.Bold } });
                breakdown.Add(row);
            }

            AddBreakdownRow($"Stability ({country.stability:F0}%)", stabilityScore, 40f);
            AddBreakdownRow($"Budget ({(country.income > country.spending ? "Surplus" : "Deficit")})", econScore, 30f);
            AddBreakdownRow($"Prestige ({country.globalPrestige:F0})", prestigeScore, 15f);
            AddBreakdownRow($"Military morale", militaryScore, 10f);
            AddBreakdownRow($"Inflation penalty ({country.inflationRate:F0}%)", inflationPenalty, 10f, false);
            card.Add(breakdown);

            string advice = winning
                ? "Keep stability high and avoid costly wars before election day."
                : country.stability < 40f ? "⚠ Raise stability urgently — it's your biggest vote driver."
                : country.inflationRate > 20f ? "⚠ Inflation is costing you votes. Tighten monetary policy."
                : "⚠ Improve budget balance and international standing to recover.";

            var adviceLabel = new Label(advice);
            adviceLabel.style.color     = winning ? new Color(0.5f, 0.7f, 0.5f) : new Color(1f, 0.7f, 0.3f);
            adviceLabel.style.fontSize  = 8;
            adviceLabel.style.whiteSpace = WhiteSpace.Normal;
            card.Add(adviceLabel);

            return card;
        }

        private void PopulateTroops(CountryDataSO country)
        {
            if (_troopsScroll == null) return;
            _troopsScroll.Clear();

            if (country.activeOperations == null || country.activeOperations.Count == 0)
            {
                var emptyBox = new VisualElement();
                emptyBox.style.alignItems = Align.Center;
                emptyBox.style.paddingTop = 30;
                emptyBox.style.paddingBottom = 30;
                var emptyIcon = new Label("🛡") { style = { fontSize = 32, marginBottom = 10 } };
                var emptyText = new Label("NO ACTIVE DEPLOYMENTS") { style = { color = new Color(0.4f, 0.4f, 0.5f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } };
                var emptySubText = new Label("All forces are stationed at home bases.") { style = { color = Color.gray, fontSize = 9 } };
                emptyBox.Add(emptyIcon);
                emptyBox.Add(emptyText);
                emptyBox.Add(emptySubText);
                _troopsScroll.Add(emptyBox);
                return;
            }

            int totalDeployedTroops = 0;
            foreach (var op in country.activeOperations) totalDeployedTroops += op.deployedTroops;
            var summaryRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 10, paddingLeft = 4, paddingRight = 4 } };
            summaryRow.Add(new Label($"⚔ {country.activeOperations.Count} ACTIVE OPERATION(S)") { style = { color = new Color(1f, 0.72f, 0.2f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            summaryRow.Add(new Label($"{totalDeployedTroops:N0} TOTAL PERSONNEL DEPLOYED") { style = { color = new Color(0.5f, 0.8f, 1f), fontSize = 9 } });
            _troopsScroll.Add(summaryRow);

            foreach (var op in country.activeOperations)
            {
                Color statusColor = op.status == "Active"   ? new Color(0.2f, 0.85f, 0.4f) :
                                    op.status == "Stalled"  ? new Color(1f, 0.78f, 0.2f) :
                                    op.status == "Failed"   ? new Color(1f, 0.3f, 0.3f) :
                                                              new Color(0.5f, 0.8f, 1f);

                var card = new VisualElement();
                card.style.marginBottom = 12;
                card.style.paddingTop = 14;
                card.style.paddingBottom = 14;
                card.style.paddingLeft = 14;
                card.style.paddingRight = 14;
                card.style.backgroundColor = new Color(0.05f, 0.08f, 0.13f, 0.97f);
                card.style.borderLeftWidth = 4;
                card.style.borderLeftColor = statusColor;
                card.style.borderTopWidth = 1;
                card.style.borderBottomWidth = 1;
                card.style.borderRightWidth = 1;
                card.style.borderTopColor = new Color(0.2f, 0.3f, 0.4f, 0.5f);
                card.style.borderBottomColor = new Color(0.2f, 0.3f, 0.4f, 0.5f);
                card.style.borderRightColor = new Color(0.2f, 0.3f, 0.4f, 0.5f);
                card.style.borderTopLeftRadius = 4;
                card.style.borderTopRightRadius = 4;
                card.style.borderBottomLeftRadius = 4;
                card.style.borderBottomRightRadius = 4;

                var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                headerRow.Add(new Label($"⚔ {op.opName.ToUpper()}") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                headerRow.Add(new Label(op.status?.ToUpper() ?? "ACTIVE") { style = { color = statusColor, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                card.Add(headerRow);

                var metaRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginBottom = 10 } };
                metaRow.Add(new Label($"📍 {op.regionName}") { style = { color = new Color(0.5f, 0.8f, 1f), fontSize = 9, marginRight = 16 } });
                if (!string.IsNullOrEmpty(op.type))
                    metaRow.Add(new Label($"🎯 {op.type}") { style = { color = new Color(0.7f, 0.7f, 0.8f), fontSize = 9 } });
                card.Add(metaRow);

                bool hasLand = op.deployedTroops > 0 || op.deployedTanks > 0 || op.deployedAPCs > 0 || op.deployedRDF > 0;
                if (hasLand)
                {
                    card.Add(new Label("LAND FORCES") { style = { color = Color.grey, fontSize = 8, letterSpacing = 2, marginBottom = 4 } });
                    var landGrid = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 8 } };
                    if (op.deployedTroops > 0)  landGrid.Add(MakeUnitChip("🪖", "Infantry",    op.deployedTroops,  new Color(0.3f, 0.8f, 0.4f)));
                    if (op.deployedRDF > 0)      landGrid.Add(MakeUnitChip("🔫", "RDF",         op.deployedRDF,     new Color(0.4f, 0.9f, 0.6f)));
                    if (op.deployedTanks > 0)    landGrid.Add(MakeUnitChip("🪖", "Tanks",       op.deployedTanks,   new Color(0.7f, 0.5f, 0.2f)));
                    if (op.deployedAPCs > 0)     landGrid.Add(MakeUnitChip("🛡", "APCs",        op.deployedAPCs,    new Color(0.6f, 0.6f, 0.3f)));
                    if (op.deployedUAVs > 0)     landGrid.Add(MakeUnitChip("👁", "UAVs",        op.deployedUAVs,    new Color(0.5f, 0.5f, 0.9f)));
                    card.Add(landGrid);
                }

                bool hasAir = op.deployedFighters > 0 || op.deployedHelis > 0 || op.deployedBombers > 0;
                if (hasAir)
                {
                    card.Add(new Label("AIR FORCES") { style = { color = Color.grey, fontSize = 8, letterSpacing = 2, marginBottom = 4 } });
                    var airGrid = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 8 } };
                    if (op.deployedFighters > 0) airGrid.Add(MakeUnitChip("✈",  "Fighters",    op.deployedFighters, new Color(0.3f, 0.6f, 1f)));
                    if (op.deployedBombers > 0)  airGrid.Add(MakeUnitChip("💣",  "Bombers",     op.deployedBombers,  new Color(1f, 0.5f, 0.3f)));
                    if (op.deployedHelis > 0)    airGrid.Add(MakeUnitChip("🚁",  "Helicopters", op.deployedHelis,    new Color(0.6f, 0.8f, 0.6f)));
                    card.Add(airGrid);
                }

                bool hasNaval = op.deployedFrigates > 0 || op.deployedSubmarines > 0;
                if (hasNaval)
                {
                    card.Add(new Label("NAVAL FORCES") { style = { color = Color.grey, fontSize = 8, letterSpacing = 2, marginBottom = 4 } });
                    var navalGrid = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 8 } };
                    if (op.deployedFrigates > 0)    navalGrid.Add(MakeUnitChip("⚓", "Frigates",    op.deployedFrigates,    new Color(0.2f, 0.6f, 0.9f)));
                    if (op.deployedSubmarines > 0)  navalGrid.Add(MakeUnitChip("🌊", "Submarines",  op.deployedSubmarines,  new Color(0.1f, 0.4f, 0.7f)));
                    card.Add(navalGrid);
                }

                if (op.costPerMonth > 0)
                {
                    string sym = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;
                    card.Add(new Label($"Monthly cost: {sym}{op.costPerMonth:F1}B") { style = { color = new Color(1f, 0.4f, 0.4f), fontSize = 8, marginTop = 2 } });
                }

                var capturedOp = op;
                var btnRecall = new Button(() => {
                    country.unitInfantry += capturedOp.deployedTroops;
                    country.unitTanks += capturedOp.deployedTanks;
                    country.unitAPCs += capturedOp.deployedAPCs;
                    country.unitFighters += capturedOp.deployedFighters;
                    country.unitBombers += capturedOp.deployedBombers;
                    country.unitAttackHelis += capturedOp.deployedHelis;
                    country.unitFrigates += capturedOp.deployedFrigates;
                    country.unitSubmarines += capturedOp.deployedSubmarines;
                    country.unitUAVs += capturedOp.deployedUAVs;
                    if (country.militaryState?.army != null) 
                        country.militaryState.army.rdfPersonnel += capturedOp.deployedRDF;

                    country.activeOperations.Remove(capturedOp);
                    Refresh();
                });
                btnRecall.text = "⬅  RECALL FORCES";
                btnRecall.AddToClassList("btn-recall");
                btnRecall.style.marginTop = 10;
                btnRecall.style.height = 28;
                btnRecall.style.fontSize = 9;
                btnRecall.style.backgroundColor = new Color(0.6f, 0.15f, 0.15f, 0.45f);
                card.Add(btnRecall);

                _troopsScroll.Add(card);
            }
        }

        private static VisualElement MakeUnitChip(string icon, string label, int count, Color accentColor)
        {
            var chip = new VisualElement();
            chip.style.backgroundColor = new Color(accentColor.r * 0.15f, accentColor.g * 0.15f, accentColor.b * 0.15f, 0.9f);
            chip.style.borderTopWidth = 1;
            chip.style.borderBottomWidth = 1;
            chip.style.borderLeftWidth = 1;
            chip.style.borderRightWidth = 1;
            chip.style.borderTopColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
            chip.style.borderBottomColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
            chip.style.borderLeftColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
            chip.style.borderRightColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
            chip.style.borderTopLeftRadius = 4;
            chip.style.borderTopRightRadius = 4;
            chip.style.borderBottomLeftRadius = 4;
            chip.style.borderBottomRightRadius = 4;
            chip.style.paddingTop = 5;
            chip.style.paddingBottom = 5;
            chip.style.paddingLeft = 8;
            chip.style.paddingRight = 8;
            chip.style.marginRight = 6;
            chip.style.marginBottom = 6;
            chip.style.alignItems = Align.Center;

            chip.Add(new Label($"{icon} {count:N0}") { style = { color = accentColor, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            chip.Add(new Label(label) { style = { color = new Color(0.6f, 0.6f, 0.7f), fontSize = 7 } });
            return chip;
        }

        private void PopulateSuggestions(CountryDataSO country)
        {
            if (_suggestionsScroll == null) return;
            _suggestionsScroll.Clear();

            var eventSys = GameManager.Instance?.EventSystem;
            if (eventSys != null)
            {
                var pendingEvents = eventSys.GetPendingEvents(country);
                if (pendingEvents != null && pendingEvents.Count > 0)
                {
                    var urgentHeader = new Label($"⚠ {pendingEvents.Count} DECISION(S) REQUIRE YOUR ATTENTION");
                    urgentHeader.style.color = new Color(1f, 0.4f, 1f);
                    urgentHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                    urgentHeader.style.fontSize = 10;
                    urgentHeader.style.marginBottom = 10;
                    _suggestionsScroll.Add(urgentHeader);

                    foreach (var ev in pendingEvents)
                    {
                        var card = CreateEventDecisionCard(country, ev, eventSys);
                        _suggestionsScroll.Add(card);
                    }

                    var divider = new VisualElement();
                    divider.style.height = 1;
                    divider.style.backgroundColor = new Color(1, 1, 1, 0.15f);
                    divider.style.marginTop = 10;
                    divider.style.marginBottom = 10;
                    _suggestionsScroll.Add(divider);
                }
            }

            foreach (var region in country.regions)
            {
                if (region.insurgencyLevel > 40f)
                {
                    var capturedRegion = region;
                    var card = CreateSuggestionCard("⚠️ MILITARY ADVISORY",
                        $"Mr. President, insurgent activity in {region.regionName} has reached critical levels ({region.insurgencyLevel:F0}%). Recommend immediate troop deployment to suppress the threat before regional GDP collapses.",
                        "⚔ DEPLOY TROOPS IMMEDIATELY", () =>
                        {
                            int troops = Mathf.Max(1000, country.unitInfantry / 10);
                            
                            if (country.unitInfantry < troops) troops = country.unitInfantry;
                            if (troops <= 0)
                            {
                                NotificationManager.Instance?.AddNotification("❌ NOT ENOUGH INFANTRY AVAILABLE TO DEPLOY", Color.red);
                                return;
                            }
                            
                            var op = new MilitaryOperation
                            {
                                opName        = $"Op. IRON SHIELD – {capturedRegion.regionName}",
                                regionName    = capturedRegion.regionName,
                                type          = "CounterInsurgency",
                                status        = "Active",
                                deployedTroops = troops,
                                costPerMonth  = 0.05f,
                            };
                            country.activeOperations.Add(op);
                            country.unitInfantry -= troops;

                            capturedRegion.insurgencyLevel = Mathf.Max(0f, capturedRegion.insurgencyLevel - 15f);
                            NotificationManager.Instance?.AddNotification(
                                $"⚔ {troops:N0} troops deployed to {capturedRegion.regionName}.", new Color(1f, 0.72f, 0.2f));
                            NewsSystem.Instance?.PublishNews(
                                $"MILITARY: {country.countryName.ToUpper()} launches counter-insurgency sweep in {capturedRegion.regionName}.");
                            Refresh();
                        });
                    _suggestionsScroll.Add(card);
                    break;
                }
            }

            if (country.treasury < 2f && country.debt > 0)
            {
                var card = CreateSuggestionCard("📈 ECONOMIC SUGGESTION",
                    "Treasury is dangerously low while carrying active debt. Without intervention, currency collapse is imminent.",
                    "RAISE TAXES (+3%)", () => {
                        country.taxRate = Mathf.Clamp(country.taxRate + 3f, 5f, 40f);
                        Refresh();
                    });
                _suggestionsScroll.Add(card);
            }

            var warStage = GameManager.Instance.WarSystem.GetWarStage(country);
            if (warStage == WarStage.Tension)
            {
                var card = CreateSuggestionCard("🌍 DIPLOMATIC SUGGESTION",
                    $"Tensions with {country.warEnemyName} are critically high. You must force a resolution before they declare war.",
                    "ISSUE ULTIMATUM", () => {
                        country.globalPrestige += 10f;
                        NewsSystem.Instance.PublishNews($"ULTIMATUM GIVEN: {country.countryName} formally warns {country.warEnemyName} to stand down immediately.");
                        GameManager.Instance.WarSystem.SetWarStage(country, WarStage.Conflict);
                        Refresh();
                    });
                _suggestionsScroll.Add(card);
            }

            if (_suggestionsScroll.childCount == 0)
            {
                var advisorBox = new VisualElement();
                advisorBox.style.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 0.95f);
                advisorBox.style.borderLeftWidth = 3;
                advisorBox.style.borderLeftColor = new Color(0.2f, 0.7f, 0.4f, 0.8f);
                advisorBox.style.borderTopWidth = 1;
                advisorBox.style.borderBottomWidth = 1;
                advisorBox.style.borderRightWidth = 1;
                advisorBox.style.borderTopColor = new Color(0.2f, 0.3f, 0.2f, 0.4f);
                advisorBox.style.borderBottomColor = new Color(0.2f, 0.3f, 0.2f, 0.4f);
                advisorBox.style.borderRightColor = new Color(0.2f, 0.3f, 0.2f, 0.4f);
                advisorBox.style.borderTopLeftRadius = 4;
                advisorBox.style.borderTopRightRadius = 4;
                advisorBox.style.borderBottomLeftRadius = 4;
                advisorBox.style.borderBottomRightRadius = 4;
                advisorBox.style.paddingTop = 14;
                advisorBox.style.paddingBottom = 14;
                advisorBox.style.paddingLeft = 14;
                advisorBox.style.paddingRight = 14;

                var advisorHeader = new Label("🎖  PRESIDENTIAL ADVISOR");
                advisorHeader.style.color = new Color(0.3f, 0.85f, 0.5f);
                advisorHeader.style.fontSize = 10;
                advisorHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                advisorHeader.style.marginBottom = 8;
                advisorBox.Add(advisorHeader);

                var advisorText = new Label("Mr. President, all strategic indicators are within acceptable parameters. No immediate intervention is required at this time. Continue monitoring regional developments.");
                advisorText.style.color = new Color(0.75f, 0.78f, 0.82f);
                advisorText.style.fontSize = 10;
                advisorText.style.whiteSpace = WhiteSpace.Normal;
                advisorBox.Add(advisorText);

                _suggestionsScroll.Add(advisorBox);
            }
        }

        private VisualElement CreateSuggestionCard(string title, string desc, string actionName, System.Action actionCallback)
        {
            var card = new VisualElement();
            card.AddToClassList("suggestion-card");

            var lblTitle = new Label(title);
            lblTitle.AddToClassList("suggestion-title");

            var lblDesc = new Label(desc);
            lblDesc.AddToClassList("suggestion-desc");

            var row = new VisualElement();
            row.AddToClassList("suggestion-action-row");

            var btnAction = new Button(actionCallback);
            btnAction.text = actionName;
            btnAction.AddToClassList("btn-action");

            var btnIgnore = new Button(() => { card.style.display = DisplayStyle.None; });
            btnIgnore.text = "IGNORE";
            btnIgnore.AddToClassList("btn-action");
            btnIgnore.AddToClassList("btn-ignore");

            row.Add(btnAction);
            row.Add(btnIgnore);

            card.Add(lblTitle);
            card.Add(lblDesc);
            card.Add(row);

            return card;
        }

        private VisualElement CreateEventDecisionCard(CountryDataSO country, GameEventSO ev, StateCraft.Interfaces.IEventSystem evSys)
        {
            var card = new VisualElement();
            card.AddToClassList("suggestion-card");
            card.AddToClassList("decision-card");

            var lblTitle = new Label($"AWAITING DECISION: {ev.eventTitle.ToUpper()}");
            lblTitle.AddToClassList("suggestion-title");
            lblTitle.style.color = new Color(0.9f, 0.5f, 0.9f);

            var lblDesc = new Label(ev.description);
            lblDesc.AddToClassList("suggestion-desc");
            lblDesc.style.marginBottom = 10;

            card.Add(lblTitle);
            card.Add(lblDesc);

            var choicesContainer = new VisualElement() { style = { flexDirection = FlexDirection.Column } };

            var btnA = new Button(() => { 
                evSys.ResolvePendingEvent(country, ev, true); 
                Refresh(); 
            });
            btnA.AddToClassList("btn-action");
            btnA.AddToClassList("btn-choice-a");
            btnA.style.marginBottom = 5;
            btnA.style.height = StyleKeyword.Auto;
            btnA.style.paddingTop = 8;
            btnA.style.paddingBottom = 8;
            btnA.style.whiteSpace = WhiteSpace.Normal;

            string aImpactText = BuildConsequenceText(ev.optionACost, ev.optionAStability, ev.optionAGDP, ev.optionAIntel, ev.optionASecurity);
            var lblAAction = new Label(ev.optionAText) { style = { unityFontStyleAndWeight = FontStyle.Bold, color = Color.white } };
            var lblAImpact = new Label(aImpactText) { style = { fontSize = 9, color = new Color(0.7f, 1f, 0.7f), marginTop = 2 } };
            btnA.Add(lblAAction);
            btnA.Add(lblAImpact);

            var btnB = new Button(() => { 
                evSys.ResolvePendingEvent(country, ev, false); 
                Refresh(); 
            });
            btnB.AddToClassList("btn-action");
            btnB.AddToClassList("btn-choice-b");
            btnB.style.marginBottom = 5;
            btnB.style.height = StyleKeyword.Auto;
            btnB.style.paddingTop = 8;
            btnB.style.paddingBottom = 8;
            btnB.style.whiteSpace = WhiteSpace.Normal;

            string bImpactText = BuildConsequenceText(ev.optionBCost, ev.optionBStability, ev.optionBGDP, ev.optionBIntel, ev.optionBSecurity);
            var lblBAction = new Label(ev.optionBText) { style = { unityFontStyleAndWeight = FontStyle.Bold, color = Color.white } };
            var lblBImpact = new Label(bImpactText) { style = { fontSize = 9, color = new Color(1f, 0.7f, 0.7f), marginTop = 2 } };
            btnB.Add(lblBAction);
            btnB.Add(lblBImpact);

            choicesContainer.Add(btnA);
            choicesContainer.Add(btnB);

            card.Add(choicesContainer);
            return card;
        }

        private static string BuildConsequenceText(float cost, float stab, float gdp, float intel, float sec)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (cost  != 0f) parts.Add($"${Mathf.Abs(cost):F1}B {(cost > 0 ? "gained" : "cost")}");
            if (stab  != 0f) parts.Add($"Stability {(stab  > 0 ? "+" : "")}{stab:F0}");
            if (gdp   != 0f) parts.Add($"GDP {(gdp   > 0 ? "+" : "")}{gdp * 100f:F1}%");
            if (intel != 0f) parts.Add($"Intel {(intel > 0 ? "+" : "")}{intel:F0}");
            if (sec   != 0f) parts.Add($"Security {(sec > 0 ? "+" : "")}{sec:F0}");
            return parts.Count > 0 ? string.Join("  ·  ", parts) : "No immediate effect";
        }

        private string _activeFilter = "ALL";

        private void PopulateHistory()
        {
            if (_historyScroll == null || NewsSystem.Instance == null) return;
            _historyScroll.Clear();

            string[] filters = { "ALL", "MILITARY", "DIPLOMACY", "ECONOMY", "INSURGENCY" };
            foreach (var f in filters)
            {
                var btn = _eventDashboard.Q<Button>($"Filter{f}");
                if (btn != null)
                {
                    btn.clicked += () => { SetFilter(f); };
                }
            }

            var history = new List<string>(NewsSystem.Instance.NewsHistory);
            for (int i = history.Count - 1; i >= 0; i--)
            {
                string entry = history[i];

                if (_activeFilter != "ALL")
                {
                    if (_activeFilter == "MILITARY" && !entry.Contains("WAR") && !entry.Contains("TROOP") && !entry.Contains("ARMY") && !entry.Contains("ATTACK")) continue;
                    if (_activeFilter == "DIPLOMACY" && !entry.Contains("DIPLOMACY") && !entry.Contains("TRADE") && !entry.Contains("TREATY")) continue;
                    if (_activeFilter == "ECONOMY" && !entry.Contains("GDP") && !entry.Contains("CRISIS") && !entry.Contains("TREASURY")) continue;
                    if (_activeFilter == "INSURGENCY" && !entry.Contains("INSURGENCY") && !entry.Contains("TERROR") && !entry.Contains("MILITANT")) continue;
                }

                var card = new VisualElement();
                card.style.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.9f);
                card.style.paddingLeft = 15;
                card.style.paddingRight = 15;
                card.style.paddingTop = 10;
                card.style.paddingBottom = 10;
                card.style.marginBottom = 2;

                var lbl = new Label(entry);
                lbl.style.color = Color.white;
                lbl.style.whiteSpace = WhiteSpace.Normal;
                lbl.style.fontSize = 12;

                card.Add(lbl);
                _historyScroll.Add(card);
            }
        }

        private void SetFilter(string filter)
        {
            _activeFilter = filter;
            
            string[] filters = { "ALL", "MILITARY", "DIPLOMACY", "ECONOMY", "INSURGENCY" };
            foreach (var f in filters)
            {
                var btn = _eventDashboard.Q<Button>($"Filter{f}");
                if (btn != null)
                {
                    if (f == _activeFilter) btn.AddToClassList("filter-active");
                    else btn.RemoveFromClassList("filter-active");
                }
            }
            
            PopulateHistory();
        }
    }
}