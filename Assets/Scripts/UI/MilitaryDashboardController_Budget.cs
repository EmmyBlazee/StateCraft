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

            float armyUpkeepLocal = ((country.unitInfantry * 0.0000020f) + (country.unitRifles * 0.0000002f) + (country.unitTanks * 0.000030f) + (country.unitAPCs * 0.000010f) + (country.unitArtillery * 0.000015f)) * exc;
            float airUpkeepLocal = ((country.airForceStats.personnel * 0.000006f) + (country.unitFighters * 0.000150f) + (country.unitBombers * 0.000280f) + (country.unitAttackHelis * 0.000080f) + (country.unitUAVs * 0.000020f)) * exc;
            float navyUpkeepLocal = ((country.navyStats.personnel * 0.000005f) + (country.unitFrigates * 0.000400f) + (country.unitDestroyers * 0.000800f) + (country.unitSubmarines * 0.001000f) + (country.unitPatrolBoats * 0.000040f)) * exc;
            float rdfUpkeepLocal = ((country.rdfStats.personnel * 0.000010f) + (country.rdfStats.equipment * 0.000050f)) * exc;
            
            float baseUpkeepLocal = (country.militaryBases != null ? country.militaryBases.Count * 0.005000f : 0f) * exc;
            float totalStandingActual = armyUpkeepLocal + airUpkeepLocal + navyUpkeepLocal + rdfUpkeepLocal + baseUpkeepLocal;

            float operationsCostLocal = 0f;
            if (country.activeOperations != null) {
                foreach (var op in country.activeOperations) operationsCostLocal += (op.costPerMonth / 1000f) * exc;
            }

            float procurementCostLocal = 0f;
            if (country.activeContracts != null) {
                foreach (var pc in country.activeContracts) procurementCostLocal += pc.monthlyCostB * exc;
            }

            float supplyContractsCostLocal = 0f;
            if (country.activeSupplyContracts != null) {
                foreach (var sc in country.activeSupplyContracts) supplyContractsCostLocal += sc.costPerTurn * exc;
            }

            float totalActual = totalStandingActual + operationsCostLocal + procurementCostLocal + supplyContractsCostLocal;
            
            float totalBudget = Mathf.Max(1f, country.income * (country.budgetMilitaryShare / 100f) * exc);
            float available = totalBudget - totalActual;
            float pctUtil = totalActual / totalBudget;

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
                Color availColor = available >= 0 ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.3f, 0.3f);
                string availStr = available >= 0 ? UiFormat.Money(available, sym) : UiFormat.MoneySigned(available, sym);
                kpiAvail.Add(new Label("AVAILABLE") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
                kpiAvail.Add(new Label(availStr) { style = { color = availColor, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            }

            var kpiUtil = _panelBudget.Q<VisualElement>("KpiUtilization");
            if (kpiUtil != null)
            {
                kpiUtil.Clear();
                kpiUtil.Add(new Label("BUDGET UTILIZATION") { style = { color = Color.grey, fontSize = 9, marginBottom = 6 } });
                
                Color utilColor = pctUtil > 1.0f ? new Color(1f, 0.3f, 0.3f) : new Color(0.14f, 0.93f, 0.34f);
                kpiUtil.Add(new Label($"{(pctUtil * 100f):F0}%") { style = { color = utilColor, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });
                
                var barBg = new VisualElement() { style = { height = 4, backgroundColor = new Color(1,1,1,0.1f), width = Length.Percent(100) } };
                var barFill = new VisualElement() { style = { height = 4, backgroundColor = utilColor, width = Length.Percent(Mathf.Min(pctUtil * 100f, 100f)) } };
                barBg.Add(barFill);
                kpiUtil.Add(barBg);
            }

            var kpiGDP = _panelBudget.Q<VisualElement>("KpiGDP");
            if (kpiGDP != null)
            {
                kpiGDP.Clear();
                float pctGdp = country.gdp > 0 ? ((country.income * (country.budgetMilitaryShare / 100f) * 12f) / country.gdp) * 100f : 0f;
                kpiGDP.Add(new Label("DEFENCE BUDGET (% OF GDP)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
                kpiGDP.Add(new Label($"{pctGdp:F1}%") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
                kpiGDP.Add(new Label("Target: 2.0%") { style = { color = Color.grey, fontSize = 9 } });
            }

            void SetupBranchCard(string eleName, string title, float amt, Color col, string p1, float v1, string p2, float v2, string p3, float v3, string p4, float v4)
            {
                var card = _panelBudget.Q<VisualElement>(eleName);
                if (card == null) return;
                card.Clear();
                
                var top = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 10 } };
                top.Add(new VisualElement() { style = { width = 16, height = 16, backgroundColor = col, marginRight = 8, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
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

                float healthScore = 100f;

                if (pctUtil > 1.0f)
                    healthScore -= Mathf.Min(60f, (pctUtil - 1.0f) * 100f);
                else if (pctUtil > 0.85f)
                    healthScore -= (pctUtil - 0.85f) * 100f;

                float pctGdpCalculated = country.gdp > 0 ? ((country.income * (country.budgetMilitaryShare / 100f) * 12f) / country.gdp) * 100f : 0f;
                if (pctGdpCalculated > 10f)
                {
                    healthScore -= Mathf.Min(35f, (pctGdpCalculated - 10f) * 1.5f);
                }

                healthScore = Mathf.Clamp(healthScore, 5f, 100f);

                string healthLabel;
                Color ringColor;
                if      (healthScore >= 80f) { healthLabel = "EXCELLENT"; ringColor = new Color(0.14f, 0.93f, 0.34f); }
                else if (healthScore >= 60f) { healthLabel = "GOOD";      ringColor = new Color(0.14f, 0.93f, 0.34f); }
                else if (healthScore >= 40f) { healthLabel = "FAIR";      ringColor = new Color(1f, 0.7f, 0f); }
                else                         { healthLabel = "CRITICAL";   ringColor = new Color(1f, 0.3f, 0.3f); }

                var dContainer = new VisualElement() { style = {
                    width = 80, height = 80, justifyContent = Justify.Center, alignItems = Align.Center,
                    borderTopWidth = 6, borderBottomWidth = 6, borderLeftWidth = 6, borderRightWidth = 6,
                    borderTopLeftRadius = 40, borderTopRightRadius = 40, borderBottomLeftRadius = 40, borderBottomRightRadius = 40,
                    borderTopColor = ringColor, borderBottomColor = ringColor,
                    borderLeftColor = ringColor, borderRightColor = new Color(1,1,1,0.1f),
                    marginBottom = 15
                }};
                dContainer.Add(new Label($"{healthScore:F0}%") { style = { color = Color.white, fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = -2 } });
                dContainer.Add(new Label(healthLabel) { style = { color = ringColor, fontSize = 9 } });
                health.Add(dContainer);
                
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = Length.Percent(100), justifyContent = Justify.SpaceAround } };
                var r1 = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                r1.Add(new Label("Sustainable") { style = { color = Color.grey, fontSize = 8, marginRight = 4 } });
                r1.Add(new VisualElement() { style = { width = 6, height = 6, backgroundColor = ringColor, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } });
                
                var r2 = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                r2.Add(new Label("Stable") { style = { color = Color.grey, fontSize = 8, marginRight = 4 } });
                r2.Add(new VisualElement() { style = { width = 6, height = 6, backgroundColor = ringColor, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } });
                r.Add(r1); r.Add(r2);
                health.Add(r);
            }

var expensesList = _panelBudget.Q<VisualElement>("ExpensesScroll");
            if (expensesList != null)
            {
                expensesList.Clear();
                void AddExpense(string n, float amt, string trend, Color trendCol, float barPct)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 14 } };
                    var left = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                    left.Add(new VisualElement() { style = { width = 12, height = 12, backgroundColor = new Color(1,1,1,0.2f), marginRight = 10, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                    left.Add(new Label(n) { style = { color = Color.white, fontSize = 10, width = 140, paddingTop = 0, paddingBottom = 0, marginTop = 0, marginBottom = 0 } });
                    r.Add(left);
                    
                    var right = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = 220, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
                    right.Add(new Label(UiFormat.Money(amt, sym)) { style = { color = Color.white, fontSize = 10, width = 60, unityFontStyleAndWeight = FontStyle.Bold } });
                    
                    float pct = (amt / totalActual) * 100f;
                    var pctBlock = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = 80, alignItems = Align.Center } };
                    pctBlock.Add(new Label($"{pct:F1}%") { style = { color = Color.white, fontSize = 9, width = 35, marginRight = 4, unityTextAlign = TextAnchor.MiddleRight } });
                    pctBlock.Add(new VisualElement() { style = { height = 4, width = 35, backgroundColor = new Color(0.14f, 0.93f, 0.34f) } });
                    right.Add(pctBlock);
                    
                    right.Add(new Label(trend) { style = { color = trendCol, fontSize = 9, width = 40, unityTextAlign = TextAnchor.MiddleRight } });
                    r.Add(right);
                    expensesList.Add(r);
                }

                float personnelCost = (country.unitInfantry * 0.0000020f + country.airForceStats.personnel * 0.000006f + country.navyStats.personnel * 0.000005f + country.rdfStats.personnel * 0.000010f) * exc;
                float equipmentCost = totalStandingActual - personnelCost;
                
                AddExpense("Personnel Salaries", personnelCost, "—", Color.grey, personnelCost / totalActual * 100f);
                AddExpense("Equipment & Maintenance", equipmentCost, "—", Color.grey, equipmentCost / totalActual * 100f);
                
                if (baseUpkeepLocal > 0) AddExpense("Base Maintenance", baseUpkeepLocal, "—", Color.grey, baseUpkeepLocal / totalActual * 100f);
                if (supplyContractsCostLocal > 0) AddExpense("Supply Contracts", supplyContractsCostLocal, "—", Color.grey, supplyContractsCostLocal / totalActual * 100f);
                if (operationsCostLocal > 0) AddExpense("Active Operations", operationsCostLocal, "—", Color.grey, operationsCostLocal / totalActual * 100f);
                if (procurementCostLocal > 0) AddExpense("Procurement", procurementCostLocal, "—", Color.grey, procurementCostLocal / totalActual * 100f);
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

            var opsScroll = _panelBudget.Q<VisualElement>("ActiveOpsScroll");
            if (opsScroll != null)
            {
                opsScroll.Clear();
                void AddOpCost(string n, string costStr, string s, Color c)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8 } };
                    var left = new VisualElement();
                    left.Add(new Label(n) { style = { color = Color.white, fontSize = 10, marginBottom = 2 } });
                    left.Add(new Label(costStr) { style = { color = Color.grey, fontSize = 9 } });
                    r.Add(left);
                    r.Add(new Label(s) { style = { color = c, fontSize = 9 } });
                    opsScroll.Add(r);
                }
                if (country.activeOperations != null && country.activeOperations.Count > 0)
                {
                    foreach (var op in country.activeOperations)
                    {
                        Color statusColor = op.status == "Active" ? new Color(0.14f, 0.93f, 0.34f) : new Color(0.1f, 0.5f, 0.9f);
                        AddOpCost(op.opName, UiFormat.Money((op.costPerMonth / 1000f) * exc, sym) + " / month", op.status, statusColor);
                    }
                }
                else
                {
                    opsScroll.Add(new Label("No Active Operations") { style = { color = Color.grey, fontSize = 10, marginTop = 10, unityTextAlign = TextAnchor.MiddleCenter } });
                }
            }
            var tOpsFooter = _panelBudget.Q<VisualElement>("ActiveOpsFooter");
            if (tOpsFooter != null)
            {
                tOpsFooter.Clear();
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                r.Add(new Label("TOTAL MONTHLY COST") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                r.Add(new Label($"{UiFormat.Money(operationsCostLocal, sym)}") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
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
                float projectedSurplus = totalBudget - totalActual;
                Color surplusColor = projectedSurplus >= 0 ? new Color(0.14f, 0.93f, 0.34f) : new Color(255/255f, 82/255f, 82/255f);
                
                AddFcst("Expected Income", UiFormat.Money(totalBudget, sym), Color.white);
                AddFcst("Expected Spending", UiFormat.Money(totalActual, sym), Color.white);
                AddFcst("Projected Surplus", UiFormat.MoneySigned(projectedSurplus, sym), surplusColor);
                AddFcst("Forecast Confidence", "High", new Color(0.14f, 0.93f, 0.34f));
            }

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
                if (country.activeContracts != null && country.activeContracts.Count > 0)
                {
                    foreach (var pc in country.activeContracts)
                    {
                        AddCmt($"{pc.quantity} {pc.unitLabel}", UiFormat.Money(pc.monthlyCostB * exc, sym), $"{pc.monthsRemaining} Mo");
                    }
                }
                else
                {
                    cmtScroll.Add(new Label("No Active Procurement") { style = { color = Color.grey, fontSize = 10, marginTop = 10, unityTextAlign = TextAnchor.MiddleCenter } });
                }
            }
            var tCmtFooter = _panelBudget.Q<VisualElement>("CommitmentsFooter");
            if (tCmtFooter != null)
            {
                tCmtFooter.Clear();
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                r.Add(new Label("TOTAL COMMITTED (MONTHLY)") { style = { color = new Color(0.9f, 0.6f, 0f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                r.Add(new Label($"{UiFormat.Money(procurementCostLocal, sym)}") { style = { color = new Color(0.9f, 0.6f, 0f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                tCmtFooter.Add(r);
            }
        }
    }
}
