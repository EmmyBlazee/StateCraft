using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    public partial class MilitaryDashboardController
    {
        private void PopulateBudget(CountryDataSO country)
        {
            if (_panelBudget == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float exc = country.exchangeRate;

            // 1. Calculate Base Values
            float armyUpkeepLocal = ((country.unitInfantry * 0.000002f) + (country.unitTanks * 0.00003f) + (country.unitAPCs * 0.00001f) + (country.unitArtillery * 0.000015f)) * exc;
            float airUpkeepLocal = ((country.airForceStats.personnel * 0.000006f) + (country.unitFighters * 0.00015f) + (country.unitBombers * 0.00028f) + (country.unitAttackHelis * 0.00008f) + (country.unitUAVs * 0.00002f)) * exc;
            float navyUpkeepLocal = ((country.navyStats.personnel * 0.000005f) + (country.unitFrigates * 0.0004f) + (country.unitDestroyers * 0.0008f) + (country.unitSubmarines * 0.001f) + (country.unitPatrolBoats * 0.00004f)) * exc;
            float rdfUpkeepLocal = ((country.rdfStats.personnel * 0.00001f) + (country.rdfStats.equipment * 0.00005f)) * exc;

            float totalActual = armyUpkeepLocal + airUpkeepLocal + navyUpkeepLocal + rdfUpkeepLocal;
            
            // Use the exact allocation formula from the Economy Tab
            float totalBudget = Mathf.Max(1f, country.income * (country.budgetMilitaryShare / 100f) * exc);
            float available = totalBudget - totalActual;
            float pctUtil = totalActual / totalBudget;

            // -- TOP KPI ROW --
            var kpiTotal = _panelBudget.Q<VisualElement>("KpiTotal");
            if (kpiTotal != null)
            {
                kpiTotal.Clear();
                kpiTotal.Add(new Label("TOTAL DEFENCE BUDGET") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
                kpiTotal.Add(new Label($"{UiFormat.Money(totalBudget, sym)} / Turn") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
                kpiTotal.Add(new Label($"{UiFormat.MoneySigned(totalBudget * 0.02f, sym)} from last turn") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 9 } });
            }

            var kpiSpent = _panelBudget.Q<VisualElement>("KpiSpent");
            if (kpiSpent != null)
            {
                kpiSpent.Clear();
                kpiSpent.Add(new Label("SPENT THIS TURN") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
                kpiSpent.Add(new Label(UiFormat.Money(totalActual, sym)) { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
                kpiSpent.Add(new Label($"{(pctUtil * 100f):F1}% of budget") { style = { color = new Color(1f, 0.6f, 0f), fontSize = 9 } });
            }

            var kpiAvail = _panelBudget.Q<VisualElement>("KpiAvailable");
            if (kpiAvail != null)
            {
                kpiAvail.Clear();
                kpiAvail.Add(new Label("AVAILABLE") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
                kpiAvail.Add(new Label(UiFormat.Money(available, sym)) { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            }

            var kpiUtil = _panelBudget.Q<VisualElement>("KpiUtilization");
            if (kpiUtil != null)
            {
                kpiUtil.Clear();
                kpiUtil.Add(new Label("BUDGET UTILIZATION") { style = { color = Color.grey, fontSize = 9, marginBottom = 6 } });
                kpiUtil.Add(new Label($"{(pctUtil * 100f):F0}%") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });
                var barBg = new VisualElement() { style = { height = 4, backgroundColor = new Color(1,1,1,0.1f), width = Length.Percent(100) } };
                var barFill = new VisualElement() { style = { height = 4, backgroundColor = new Color(0.14f, 0.93f, 0.34f), width = Length.Percent(pctUtil * 100f) } };
                barBg.Add(barFill);
                kpiUtil.Add(barBg);
            }

            var kpiGDP = _panelBudget.Q<VisualElement>("KpiGDP");
            if (kpiGDP != null)
            {
                kpiGDP.Clear();
                kpiGDP.Add(new Label("DEFENCE BUDGET (% OF GDP)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
                kpiGDP.Add(new Label("2.1%") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
                kpiGDP.Add(new Label("Target: 2.0%") { style = { color = Color.grey, fontSize = 9 } });
            }

            // -- MIDDLE ROW: BRANCH ALLOCATION --
            void SetupBranchCard(string eleName, string title, float amt, Color col, string p1, float v1, string p2, float v2, string p3, float v3, string p4, float v4)
            {
                var card = _panelBudget.Q<VisualElement>(eleName);
                if (card == null) return;
                card.Clear();
                
                var top = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 10 } };
                top.Add(new VisualElement() { style = { width = 16, height = 16, backgroundColor = col, marginRight = 8, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } }); // mock icon
                top.Add(new Label(title) { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                card.Add(top);

                var amtRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.FlexEnd, marginBottom = 15 } };
                
                var amtBlock = new VisualElement();
                amtBlock.Add(new Label("Budget") { style = { color = Color.grey, fontSize = 8 } });
                amtBlock.Add(new Label(UiFormat.Money(amt, sym)) { style = { color = Color.white, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });
                amtRow.Add(amtBlock);
                
                float pctTotal = (amt / totalActual) * 100f;
                amtRow.Add(new Label($"{pctTotal:F1}%") { style = { color = Color.white, fontSize = 10 } });
                card.Add(amtRow);

                void AddSubBar(string lTitle, float lPct)
                {
                    var row = new VisualElement() { style = { marginBottom = 10 } };
                    var rTop = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
                    rTop.Add(new Label(lTitle) { style = { color = Color.white, fontSize = 9, paddingTop = 0, paddingBottom = 0, marginTop = 0, marginBottom = 0 } });
                    rTop.Add(new Label($"{lPct:F0}%") { style = { color = Color.white, fontSize = 9, paddingTop = 0, paddingBottom = 0, marginTop = 0, marginBottom = 0 } });
                    row.Add(rTop);

                    var bBg = new VisualElement() { style = { height = 3, backgroundColor = new Color(1,1,1,0.15f), borderTopLeftRadius = 1, borderTopRightRadius = 1, borderBottomLeftRadius = 1, borderBottomRightRadius = 1 } };
                    var bFill = new VisualElement() { style = { height = 3, backgroundColor = col, width = Length.Percent(lPct), borderTopLeftRadius = 1, borderTopRightRadius = 1, borderBottomLeftRadius = 1, borderBottomRightRadius = 1 } };
                    bBg.Add(bFill);
                    row.Add(bBg);
                    card.Add(row);
                }

                AddSubBar(p1, v1);
                AddSubBar(p2, v2);
                AddSubBar(p3, v3);
                AddSubBar(p4, v4);
            }

            SetupBranchCard("BranchArmy", "ARMY", armyUpkeepLocal, new Color(0.14f, 0.7f, 0.34f), "Personnel", 46, "Equipment", 25, "Operations", 15, "Maintenance", 14);
            SetupBranchCard("BranchAirForce", "AIR FORCE", airUpkeepLocal, new Color(0.1f, 0.5f, 0.9f), "Personnel", 24, "Aircraft Maintenance", 36, "Fuel", 24, "Operations", 16);
            SetupBranchCard("BranchNavy", "NAVY", navyUpkeepLocal, new Color(0.4f, 0.2f, 0.8f), "Fleet Maintenance", 35, "Fuel", 27, "Personnel", 22, "Operations", 16);
            SetupBranchCard("BranchRDF", "RDF", rdfUpkeepLocal, new Color(0.9f, 0.6f, 0f), "Rapid Response", 35, "Training", 25, "Deployment", 20, "Equipment", 20);

            // -- MIDDLE ROW: INDICATORS & HEALTH --
            var indicators = _panelBudget.Q<VisualElement>("IndicatorsContainer");
            if (indicators != null)
            {
                indicators.Clear();
                void AddInd(string t, string v, Color c)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                    r.Add(new Label(t) { style = { color = Color.grey, fontSize = 10 } });
                    r.Add(new Label(v) { style = { color = c, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                    indicators.Add(r);
                }
                AddInd("Revenue Growth (YoY)", "+12.4%", new Color(0.14f, 0.93f, 0.34f));
                AddInd("Expenditure Growth (YoY)", "+8.7%", new Color(0.14f, 0.93f, 0.34f));
                AddInd("Budget Deficit", "0.0% of GDP", Color.white);
                AddInd("Debt to GDP Ratio", "36.2%", new Color(0.9f, 0.6f, 0f));
                AddInd("Interest Payments", $"{UiFormat.Money(totalBudget * 0.015f, sym)} (7.0%)", Color.white);
                AddInd("Capital Expenditure", $"{UiFormat.Money(totalBudget * 0.05f, sym)} (23.5%)", Color.white);
            }

            var health = _panelBudget.Q<VisualElement>("BudgetHealthContainer");
            if (health != null)
            {
                health.Clear();
                var dContainer = new VisualElement() { style = { width = 80, height = 80, justifyContent = Justify.Center, alignItems = Align.Center, borderTopWidth = 6, borderBottomWidth = 6, borderLeftWidth = 6, borderRightWidth = 6, borderTopLeftRadius = 40, borderTopRightRadius = 40, borderBottomLeftRadius = 40, borderBottomRightRadius = 40, borderTopColor = new Color(0.14f, 0.93f, 0.34f), borderBottomColor = new Color(0.14f, 0.93f, 0.34f), borderLeftColor = new Color(0.14f, 0.93f, 0.34f), borderRightColor = new Color(1,1,1,0.1f), marginBottom = 15 } };
                dContainer.Add(new Label("78%") { style = { color = Color.white, fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = -2 } });
                dContainer.Add(new Label("GOOD") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 9 } });
                health.Add(dContainer);
                
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = Length.Percent(100), justifyContent = Justify.SpaceAround } };
                var r1 = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                r1.Add(new Label("Sustainable") { style = { color = Color.grey, fontSize = 8, marginRight = 4 } });
                r1.Add(new VisualElement() { style = { width = 6, height = 6, backgroundColor = new Color(0.14f, 0.93f, 0.34f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } });
                
                var r2 = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                r2.Add(new Label("Stable") { style = { color = Color.grey, fontSize = 8, marginRight = 4 } });
                r2.Add(new VisualElement() { style = { width = 6, height = 6, backgroundColor = new Color(0.14f, 0.93f, 0.34f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } });
                r.Add(r1); r.Add(r2);
                health.Add(r);
            }

            // -- BOTTOM ROW: EXPENSES LIST --
            var expensesList = _panelBudget.Q<VisualElement>("ExpensesScroll");
            if (expensesList != null)
            {
                expensesList.Clear();
                void AddExpense(string n, float amt, string trend, Color trendCol, float barPct)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 14 } };
                    var left = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                    left.Add(new VisualElement() { style = { width = 12, height = 12, backgroundColor = new Color(1,1,1,0.2f), marginRight = 10, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } }); // mock icon
                    left.Add(new Label(n) { style = { color = Color.white, fontSize = 10, width = 140, paddingTop = 0, paddingBottom = 0, marginTop = 0, marginBottom = 0 } });
                    r.Add(left);
                    
                    var right = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = 130, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
                    right.Add(new Label(UiFormat.Money(amt, sym)) { style = { color = Color.white, fontSize = 10, width = 50, unityFontStyleAndWeight = FontStyle.Bold } });
                    
                    float pct = (amt / totalActual) * 100f;
                    var pctBlock = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = 60, alignItems = Align.Center } };
                    pctBlock.Add(new Label($"{pct:F1}%") { style = { color = Color.white, fontSize = 9, width = 25, marginRight = 4 } });
                    pctBlock.Add(new VisualElement() { style = { height = 4, width = 25, backgroundColor = new Color(0.14f, 0.93f, 0.34f) } });
                    right.Add(pctBlock);
                    
                    right.Add(new Label(trend) { style = { color = trendCol, fontSize = 9, width = 30, unityTextAlign = TextAnchor.MiddleRight } });
                    r.Add(right);
                    expensesList.Add(r);
                }

                AddExpense("Personnel Salaries", totalActual * 0.34f, "↑ 2.1%", new Color(0.14f, 0.93f, 0.34f), 34);
                AddExpense("Base Maintenance", totalActual * 0.10f, "↑ 1.3%", new Color(0.14f, 0.93f, 0.34f), 10);
                AddExpense("Fuel Consumption", totalActual * 0.11f, "↓ -0.8%", new Color(255/255f, 82/255f, 82/255f), 11);
                AddExpense("Aircraft Maintenance", totalActual * 0.07f, "↑ 3.4%", new Color(0.14f, 0.93f, 0.34f), 7);
                AddExpense("Fleet Maintenance", totalActual * 0.07f, "↑ 1.7%", new Color(0.14f, 0.93f, 0.34f), 7);
                AddExpense("Training", totalActual * 0.05f, "↑ 0.6%", new Color(0.14f, 0.93f, 0.34f), 5);
                AddExpense("Research & Development", totalActual * 0.03f, "↑ 2.0%", new Color(0.14f, 0.93f, 0.34f), 3);
                AddExpense("Operations (Active)", totalActual * 0.19f, "↑ 5.3%", new Color(0.14f, 0.93f, 0.34f), 19);
                AddExpense("Reserve Fund", totalActual * 0.02f, "— 0.0%", Color.grey, 2);
            }
            var tExpFooter = _panelBudget.Q<VisualElement>("TotalExpensesFooter");
            if (tExpFooter != null)
            {
                tExpFooter.Clear();
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                r.Add(new Label("TOTAL STANDING EXPENSES") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                r.Add(new Label(UiFormat.Money(totalActual, sym)) { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                tExpFooter.Add(r);
            }

            // -- BOTTOM ROW: CENTER (OPS & FORECAST) --
            var opsScroll = _panelBudget.Q<VisualElement>("ActiveOpsScroll");
            if (opsScroll != null)
            {
                opsScroll.Clear();
                void AddOpCost(string n, string cost, string s, Color c)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8 } };
                    var left = new VisualElement();
                    left.Add(new Label(n) { style = { color = Color.white, fontSize = 10, marginBottom = 2 } });
                    left.Add(new Label($"{sym}{cost}M / day") { style = { color = Color.grey, fontSize = 9 } });
                    r.Add(left);
                    r.Add(new Label(s) { style = { color = c, fontSize = 9 } });
                    opsScroll.Add(r);
                }
                AddOpCost("Operation Lagos Shield", "420", "Active", new Color(0.14f, 0.93f, 0.34f));
                AddOpCost("Delta Sea Watch", "180", "Planning", new Color(0.1f, 0.5f, 0.9f));
                AddOpCost("Northern Corridor Security", "260", "Active", new Color(0.14f, 0.93f, 0.34f));
            }
            var tOpsFooter = _panelBudget.Q<VisualElement>("ActiveOpsFooter");
            if (tOpsFooter != null)
            {
                tOpsFooter.Clear();
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                r.Add(new Label("TOTAL DAILY COST") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                r.Add(new Label($"{sym}860M") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                tOpsFooter.Add(r);
            }

            var forecast = _panelBudget.Q<VisualElement>("ForecastContainer");
            if (forecast != null)
            {
                forecast.Clear();
                void AddFcst(string n, string v, Color c)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                    r.Add(new Label(n) { style = { color = Color.grey, fontSize = 10 } });
                    r.Add(new Label(v) { style = { color = c, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                    forecast.Add(r);
                }
                AddFcst("Expected Income", UiFormat.Money(totalBudget, sym), new Color(0.14f, 0.93f, 0.34f));
                AddFcst("Expected Spending", UiFormat.Money(totalActual * 1.1f, sym), new Color(255/255f, 82/255f, 82/255f));
                AddFcst("Projected Surplus", UiFormat.Money(totalBudget - (totalActual * 1.1f), sym), new Color(0.14f, 0.93f, 0.34f));
                AddFcst("Forecast Confidence", "High", new Color(0.14f, 0.93f, 0.34f));
            }

            // -- BOTTOM ROW: COMMITMENTS --
            var cmtScroll = _panelBudget.Q<VisualElement>("CommitmentsScroll");
            if (cmtScroll != null)
            {
                cmtScroll.Clear();
                void AddCmt(string n, string cost, string d)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 10 } };
                    var left = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                    left.Add(new VisualElement() { style = { width = 12, height = 12, backgroundColor = Color.white, marginRight = 8 } });
                    left.Add(new Label(n) { style = { color = Color.grey, fontSize = 9, width = 80 } });
                    r.Add(left);
                    r.Add(new Label($"{sym}{cost}") { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                    r.Add(new Label(d) { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 8, width = 60, unityTextAlign = TextAnchor.MiddleRight } });
                    cmtScroll.Add(r);
                }
                AddCmt("20 APCs", "4.2B", "15 Days");
                AddCmt("2 Patrol Boats", "8.7B", "48 Days");
                AddCmt("12 Attack Helicopters", "15B", "93 Days");
            }
            var tCmtFooter = _panelBudget.Q<VisualElement>("CommitmentsFooter");
            if (tCmtFooter != null)
            {
                tCmtFooter.Clear();
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                r.Add(new Label("TOTAL COMMITTED") { style = { color = new Color(0.9f, 0.6f, 0f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                r.Add(new Label($"{sym}27.9B") { style = { color = new Color(0.9f, 0.6f, 0f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                tCmtFooter.Add(r);
            }
        }
    }
}
