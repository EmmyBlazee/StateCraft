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
        private void PopulateSupplyRoutes(CountryDataSO country)
        {
            // Re-route the old initialization call to the new layout generator
            RefreshLogisticsTab(country);
        }

        private void RefreshLogisticsTab(CountryDataSO country)
        {
            if (_panelLogistics == null) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            // 1. Supply Status & Daily Consumption
            var supplyContainer = _panelLogistics.Q<VisualElement>("SupplyStatusContainer");
            var consumptionContainer = _panelLogistics.Q<VisualElement>("ConsumptionContainer");
            if (supplyContainer != null && consumptionContainer != null)
            {
                supplyContainer.Clear();
                consumptionContainer.Clear();
                
                // Mocks to match screenshot
                AddSupplyRow(supplyContainer, consumptionContainer, "Fuel", country.reserveFuel, "-2,800 L");
                AddSupplyRow(supplyContainer, consumptionContainer, "Ammunition", country.reserveAmmo, "-340 Crates");
                AddSupplyRow(supplyContainer, consumptionContainer, "Food", country.reserveFood, "-120 Tons");
                AddSupplyRow(supplyContainer, consumptionContainer, "Medical Supplies", country.reserveMedical, "-15 Crates");
                AddSupplyRow(supplyContainer, consumptionContainer, "Spare Parts", 46f, "-85 Units");
                AddSupplyRow(supplyContainer, consumptionContainer, "Uniforms", 73f, "-25 Units");
            }

            // 2. Storage & Depots
            var depotsContainer = _panelLogistics.Q<VisualElement>("DepotsContainer");
            if (depotsContainer != null)
            {
                depotsContainer.Clear();
                AddDepotRow(depotsContainer, "Central Supply Depot (Abuja)", 82f);
                AddDepotRow(depotsContainer, "Northern Depot (Kano)", 67f);
                AddDepotRow(depotsContainer, "Eastern Depot (Enugu)", 91f);
                AddDepotRow(depotsContainer, "Western Depot (Ibadan)", 54f);
                AddDepotRow(depotsContainer, "Navy Logistics Center (Lagos)", 76f);
                AddDepotRow(depotsContainer, "Air Force Depot (Makurdi)", 45f);
            }

            // 3. Operational Readiness (Donuts)
            var readinessContainer = _panelLogistics.Q<VisualElement>("ReadinessContainer");
            if (readinessContainer != null)
            {
                readinessContainer.Clear();
                AddReadinessDonut(readinessContainer, "Army", 95f);
                AddReadinessDonut(readinessContainer, "Air Force", 78f);
                AddReadinessDonut(readinessContainer, "Navy", 92f);
                AddReadinessDonut(readinessContainer, "Rapid Deployment Force", 88f);
            }

            // 4. Active Convoys
            var convoysScroll = _panelLogistics.Q<ScrollView>("ConvoysScroll");
            if (convoysScroll != null)
            {
                convoysScroll.Clear();
                AddConvoyRow(convoysScroll, "Abuja → Lagos", "Fuel", "2 Days", "On Time");
                AddConvoyRow(convoysScroll, "Kano → Maiduguri", "Food Supplies", "4 Days", "On Time");
                AddConvoyRow(convoysScroll, "Port Harcourt → Calabar", "Ammunition", "3 Days", "Delayed");
                AddConvoyRow(convoysScroll, "Kaduna → Jos", "Medical Supplies", "1 Day", "On Time");
            }

            // 5. Procurement & Purchases
            var procurementScroll = _panelLogistics.Q<ScrollView>("ProcurementScroll");
            if (procurementScroll != null)
            {
                procurementScroll.Clear();
                AddProcurementRow(procurementScroll, "Fuel (Per 100,000 L)", "₦120M", "3 Days", "FUEL", 0.12f);
                AddProcurementRow(procurementScroll, "Ammunition (Per Crate)", "₦85M", "5 Days", "AMMO", 0.085f);
                AddProcurementRow(procurementScroll, "Medical Supplies (Per Crate)", "₦65M", "4 Days", "MEDICAL", 0.065f);
                AddProcurementRow(procurementScroll, "Spare Parts (Per Unit)", "₦40M", "6 Days", "SPARE", 0.040f);
                AddProcurementRow(procurementScroll, "Trucks (Per Unit)", "₦95M", "7 Days", "TRUCK", 0.095f);
                AddProcurementRow(procurementScroll, "Transport Aircraft (Per Unit)", "₦6.2B", "20 Days", "AIRCRAFT", 6.2f);
            }

            // 6. Emergency Actions
            var emergencyContainer = _panelLogistics.Q<VisualElement>("EmergencyActionsContainer");
            if (emergencyContainer != null)
            {
                emergencyContainer.Clear();
                AddEmergencyAction(emergencyContainer, "Resupply All Active Operations");
                AddEmergencyAction(emergencyContainer, "Prioritize Army");
                AddEmergencyAction(emergencyContainer, "Prioritize Air Force");
                AddEmergencyAction(emergencyContainer, "Release Strategic Reserve");
                AddEmergencyAction(emergencyContainer, "Suspend Training & Non-Essential Use");
                AddEmergencyAction(emergencyContainer, "Emergency Foreign Purchase");
            }

            // 7. Alerts
            var alertsContainer = _panelLogistics.Q<VisualElement>("AlertsContainer");
            if (alertsContainer != null)
            {
                alertsContainer.Clear();
                AddAlert(alertsContainer, "Fuel reserve\nbelow 20%", Color.red);
                AddAlert(alertsContainer, "Air Force spare parts\nrunning low", Color.yellow);
                AddAlert(alertsContainer, "Medical supplies\nsufficient for only 9 days", Color.yellow);
                AddAlert(alertsContainer, "Northern depot at\nmaximum capacity", Color.red);
                AddAlert(alertsContainer, "Ammunition production\nbelow consumption", Color.yellow);
                AddAlert(alertsContainer, "3 convoys\ncurrently delayed", Color.yellow);
            }

            // 8. Summary
            var summaryContainer = _panelLogistics.Q<VisualElement>("SummaryContainer");
            if (summaryContainer != null)
            {
                summaryContainer.Clear();
                AddSummaryStat(summaryContainer, "Overall Logistics Rating", "92%", "Excellent", new Color(0.1f, 0.8f, 0.3f), true);
                AddSummaryStat(summaryContainer, "Daily Operating Cost", "₦1.8B", "/ day", Color.white, false);
                AddSummaryStat(summaryContainer, "Supply Efficiency", "95%", "High", new Color(0.1f, 0.8f, 0.3f), false);
                AddSummaryStat(summaryContainer, "Storage Utilization", "74%", "Good", new Color(0.1f, 0.8f, 0.3f), false);
                AddSummaryStat(summaryContainer, "Transport Fleet", "163", "Vehicles", Color.white, false);
            }
        }

        // --- HELPER METHODS ---
        
        private void AddSupplyRow(VisualElement supplyParent, VisualElement consumptionParent, string name, float pct, string consumeRate)
        {
            // Status Row
            var sRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4, height = 20 } };
            sRow.Add(new Label(name) { style = { color = Color.white, fontSize = 10, width = 90 } });
            
            var barBg = new VisualElement() { style = { height = 4, backgroundColor = new Color(1,1,1,0.1f), flexGrow = 1, marginRight = 10 } };
            var barFill = new VisualElement() { style = { height = 4, backgroundColor = new Color(0.14f, 0.93f, 0.34f), width = Length.Percent(pct) } };
            if (pct < 60) barFill.style.backgroundColor = Color.yellow;
            if (pct < 30) barFill.style.backgroundColor = Color.red;
            barBg.Add(barFill);
            sRow.Add(barBg);
            
            sRow.Add(new Label($"{pct:F0}%") { style = { color = Color.white, fontSize = 10, width = 30, unityTextAlign = TextAnchor.MiddleRight } });
            
            var dot = new VisualElement() { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, marginLeft = 10, backgroundColor = barFill.style.backgroundColor } };
            sRow.Add(dot);
            supplyParent.Add(sRow);

            // Consumption Row
            var cRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, justifyContent = Justify.SpaceBetween, marginBottom = 4, height = 20 } };
            cRow.Add(new Label(name) { style = { color = Color.white, fontSize = 10 } });
            cRow.Add(new Label(consumeRate) { style = { color = Color.white, fontSize = 10, unityTextAlign = TextAnchor.MiddleRight } });
            consumptionParent.Add(cRow);
        }

        private void AddDepotRow(VisualElement parent, string name, float pct)
        {
            var row = new VisualElement() { style = { marginBottom = 6 } };
            var top = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            top.Add(new Label(name) { style = { color = Color.white, fontSize = 10 } });
            
            string statusStr = pct > 70 ? "Good" : (pct > 40 ? "Medium" : "Low");
            Color statusColor = pct > 70 ? new Color(0.14f, 0.93f, 0.34f) : (pct > 40 ? Color.yellow : Color.red);
            top.Add(new Label(statusStr) { style = { color = statusColor, fontSize = 10 } });
            row.Add(top);

            var bot = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4 } };
            var barBg = new VisualElement() { style = { height = 2, backgroundColor = new Color(1,1,1,0.1f), flexGrow = 1, marginRight = 10 } };
            var barFill = new VisualElement() { style = { height = 2, backgroundColor = statusColor, width = Length.Percent(pct) } };
            barBg.Add(barFill);
            bot.Add(barBg);
            bot.Add(new Label($"{pct:F0}%") { style = { color = Color.white, fontSize = 9, width = 30, unityTextAlign = TextAnchor.MiddleRight } });
            row.Add(bot);

            parent.Add(row);
        }

        private void AddReadinessDonut(VisualElement parent, string branch, float pct)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 10 } };
            row.Add(new Label(branch) { style = { color = Color.white, fontSize = 10, width = 100, whiteSpace = WhiteSpace.Normal } });
            
            var donutContainer = new VisualElement() { style = { width = 30, height = 30, marginRight = 15, justifyContent = Justify.Center, alignItems = Align.Center } };
            
            // Draw a pseudo-donut with a colored border and text inside
            donutContainer.style.borderTopWidth = 3; donutContainer.style.borderBottomWidth = 3; donutContainer.style.borderLeftWidth = 3; donutContainer.style.borderRightWidth = 3;
            donutContainer.style.borderTopLeftRadius = 15; donutContainer.style.borderTopRightRadius = 15; donutContainer.style.borderBottomLeftRadius = 15; donutContainer.style.borderBottomRightRadius = 15;
            
            Color col = pct > 80 ? new Color(0.14f, 0.93f, 0.34f) : (pct > 50 ? Color.yellow : Color.red);
            donutContainer.style.borderTopColor = col; donutContainer.style.borderBottomColor = col; donutContainer.style.borderLeftColor = col; donutContainer.style.borderRightColor = new Color(1,1,1,0.1f);
            
            donutContainer.Add(new Label($"{pct:F0}%") { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
            row.Add(donutContainer);

            string statusStr = pct > 80 ? "Excellent" : (pct > 50 ? "Good" : "Poor");
            row.Add(new Label(statusStr) { style = { color = col, fontSize = 10 } });

            parent.Add(row);
        }

        private void AddConvoyRow(VisualElement parent, string route, string cargo, string eta, string status)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8, paddingBottom = 8, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.05f) } };
            var left = new VisualElement();
            left.Add(new Label(route) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            left.Add(new Label(cargo) { style = { color = Color.grey, fontSize = 9, marginTop = 2 } });
            row.Add(left);

            var right = new VisualElement() { style = { alignItems = Align.FlexEnd } };
            right.Add(new Label($"ETA: {eta}") { style = { color = Color.white, fontSize = 10 } });
            right.Add(new Label(status) { style = { color = status == "Delayed" ? Color.red : new Color(0.14f, 0.93f, 0.34f), fontSize = 9, marginTop = 2 } });
            row.Add(right);

            parent.Add(row);
        }

        private void AddProcurementRow(VisualElement parent, string item, string costStr, string eta, string resourceKey, float costB)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8, paddingBottom = 8, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.05f) } };
            row.Add(new Label(item) { style = { color = Color.white, fontSize = 10, width = 140 } });
            row.Add(new Label(costStr) { style = { color = Color.white, fontSize = 10, width = 50, unityFontStyleAndWeight = FontStyle.Bold } });
            row.Add(new Label($"ETA: {eta}") { style = { color = Color.grey, fontSize = 9, width = 60 } });
            
            var btn = new Button(() => BuyLogistics(resourceKey, costB)) { text = "PURCHASE" };
            btn.style.width = 70; btn.style.height = 20; btn.style.fontSize = 8;
            btn.style.backgroundColor = new Color(0, 0.3f, 0.1f); btn.style.color = Color.white;
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1; btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(0, 0.5f, 0.2f); btn.style.borderBottomColor = new Color(0, 0.5f, 0.2f); btn.style.borderLeftColor = new Color(0, 0.5f, 0.2f); btn.style.borderRightColor = new Color(0, 0.5f, 0.2f);
            row.Add(btn);

            parent.Add(row);
        }

        private void AddEmergencyAction(VisualElement parent, string actionName)
        {
            var btn = new Button() { text = actionName };
            btn.style.height = 24; btn.style.fontSize = 9; btn.style.color = Color.white;
            btn.style.backgroundColor = new Color(1,1,1,0.05f);
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1; btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(1,1,1,0.1f); btn.style.borderBottomColor = new Color(1,1,1,0.1f); btn.style.borderLeftColor = new Color(1,1,1,0.1f); btn.style.borderRightColor = new Color(1,1,1,0.1f);
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            parent.Add(btn);
        }

        private void AddAlert(VisualElement parent, string msg, Color col)
        {
            var box = new VisualElement() { style = { flexGrow = 1, marginRight = 8, paddingTop = 4, paddingBottom = 4, paddingLeft = 4, paddingRight = 4, backgroundColor = new Color(col.r, col.g, col.b, 0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = col, borderBottomColor = col, borderLeftColor = col, borderRightColor = col } };
            box.Add(new Label(msg) { style = { color = Color.white, fontSize = 8, whiteSpace = WhiteSpace.Normal } });
            parent.Add(box);
        }

        private void AddSummaryStat(VisualElement parent, string title, string val, string sub, Color valCol, bool large)
        {
            var box = new VisualElement() { style = { alignItems = Align.Center } };
            box.Add(new Label(title) { style = { color = Color.grey, fontSize = 8, marginBottom = 4 } });
            
            var valRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexEnd } };
            valRow.Add(new Label(val) { style = { color = valCol, fontSize = large ? 16 : 12, unityFontStyleAndWeight = FontStyle.Bold } });
            if (!string.IsNullOrEmpty(sub))
            {
                valRow.Add(new Label(sub) { style = { color = large ? valCol : Color.grey, fontSize = 8, marginLeft = 4 } });
            }
            box.Add(valRow);
            parent.Add(box);
        }

        private void BuyLogistics(string resource, float costB)
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float localCost = costB * country.exchangeRate;
            float costM = localCost * 1000f;
            string notification = "";
            bool success = false;

            switch (resource)
            {
                case "FUEL":
                    if (country.invOilBarrels >= 500000)
                    {
                        country.invOilBarrels -= 500000;
                        country.reserveFuel = Mathf.Min(100f, country.reserveFuel + 25f);
                        notification = $"FUEL RESERVES: Supplied from National Oil Reserves (-500,000 Barrels). Cost: {sym}0M";
                        success = true;
                    }
                    else if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        country.reserveFuel = Mathf.Min(100f, country.reserveFuel + 25f);
                        notification = $"FUEL RESERVES: Secured foreign oil imports (+25 days of fuel). Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;

                case "AMMO":
                    if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        country.reserveAmmo = Mathf.Min(100f, country.reserveAmmo + 25f);
                        notification = $"MUNITIONS SUPPLY: Purchased strategic stockpile (+25% ammo reserves). Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;

                case "MEDICAL":
                    if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        country.reserveMedical = Mathf.Min(100f, country.reserveMedical + 25f);
                        notification = $"TRAUMA LOGISTICS: Dispatched battlefield clinics (+25% medical reserves). Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;

                case "FOOD":
                    if (country.invRiceTonnes >= 10000)
                    {
                        country.invRiceTonnes -= 10000;
                        country.reserveFood = Mathf.Min(100f, country.reserveFood + 25f);
                        notification = $"RATIONS LOGISTICS: Supplied from National Rice Stockpile (-10,000 Tonnes). Cost: {sym}0M";
                        success = true;
                    }
                    else if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        country.reserveFood = Mathf.Min(100f, country.reserveFood + 25f);
                        notification = $"RATIONS LOGISTICS: Secured foreign field supply routes (+25% food reserves). Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;
                case "SPARE":
                case "TRUCK":
                case "AIRCRAFT":
                    if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        notification = $"LOGISTICS PURCHASE: Order for {resource} placed. Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;
            }

            if (success)
            {
                NotificationManager.Instance?.AddNotification(notification, Color.green);
                RefreshLogisticsTab(country);
            }
            else
            {
                NotificationManager.Instance?.AddNotification($"INSUFFICIENT RESOURCES/FUNDS: Need {sym}{costM:N0}M or national stockpiles to secure logistics!", Color.red);
            }
        }

    }
}
