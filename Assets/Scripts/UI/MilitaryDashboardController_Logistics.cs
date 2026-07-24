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

            // 1. Supply Reserves & Consumption
            var supplyContainer = _panelLogistics.Q<VisualElement>("SupplyStatusContainer");

            if (supplyContainer != null)
            {
                supplyContainer.Clear();
                AddSupplyRow(supplyContainer, "Fuel", country.reserveFuel, "-2,800 L");
                AddSupplyRow(supplyContainer, "Ammunition", country.reserveAmmo, "-340 Crates");
                AddSupplyRow(supplyContainer, "Food", country.reserveFood, "-120 Tons");
                AddSupplyRow(supplyContainer, "Medical Supplies", country.reserveMedical, "-15 Crates");
                AddSupplyRow(supplyContainer, "Spare Parts", 46f, "-85 Units");
                AddSupplyRow(supplyContainer, "Uniforms", 73f, "-25 Units");
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

            // 5. Supply Contracts
            var contractsScroll = _panelLogistics.Q<ScrollView>("ContractsScroll");
            var activeContractsScroll = _panelLogistics.Q<ScrollView>("ActiveContractsScroll");
            if (contractsScroll != null && activeContractsScroll != null)
            {
                contractsScroll.Clear();
                activeContractsScroll.Clear();

                // Populate Available Offers
                AddSupplyContractOffer(contractsScroll, "Fuel Resupply (6 Mo)", "FUEL", 20f, 0.05f, 6);
                AddSupplyContractOffer(contractsScroll, "Fuel Strategic Reserve (12 Mo)", "FUEL", 40f, 0.09f, 12);
                AddSupplyContractOffer(contractsScroll, "Ammo Production Run (6 Mo)", "AMMO", 15f, 0.04f, 6);
                AddSupplyContractOffer(contractsScroll, "NATO Standard Ammo (12 Mo)", "AMMO", 30f, 0.07f, 12);
                AddSupplyContractOffer(contractsScroll, "Medical Supplies (6 Mo)", "MEDICAL", 10f, 0.02f, 6);
                AddSupplyContractOffer(contractsScroll, "Rations Pipeline (6 Mo)", "FOOD", 25f, 0.03f, 6);

                // Populate Active Contracts
                if (country.activeSupplyContracts != null)
                {
                    foreach (var contract in country.activeSupplyContracts)
                    {
                        AddActiveSupplyContract(activeContractsScroll, contract);
                    }
                }
            }

            // (Emergency Actions removed as per request)

            // 7. Alerts
            var alertsContainer = _panelLogistics.Q<VisualElement>("AlertsContainer");
            if (alertsContainer != null)
            {
                alertsContainer.style.flexDirection = FlexDirection.Column;
                alertsContainer.Clear();
                int alertCount = 0;

                if (country.reserveFuel < 30f) {
                    AddAlert(alertsContainer, $"Fuel reserve\ncritical ({country.reserveFuel:F0}%)", Color.red);
                    alertCount++;
                }
                else if (country.reserveFuel < 60f) {
                    AddAlert(alertsContainer, "Fuel reserve\nrunning low", Color.yellow);
                    alertCount++;
                }

                if (country.reserveAmmo < 30f) {
                    AddAlert(alertsContainer, $"Ammo reserve\ncritical ({country.reserveAmmo:F0}%)", Color.red);
                    alertCount++;
                }

                if (country.reserveFood < 30f) {
                    AddAlert(alertsContainer, "Food rations\nrunning dangerously low", Color.red);
                    alertCount++;
                }

                if (country.reserveMedical < 30f) {
                    AddAlert(alertsContainer, "Medical supplies\ncritical", Color.red);
                    alertCount++;
                }

                if (country.activeSupplyContracts == null || country.activeSupplyContracts.Count == 0) {
                    AddAlert(alertsContainer, "No active\nsupply contracts", Color.yellow);
                    alertCount++;
                }

                if (alertCount == 0) {
                    AddAlert(alertsContainer, "All supply lines\nstable and secure", new Color(0.14f, 0.93f, 0.34f));
                    AddAlert(alertsContainer, "Convoy routes operating\nat optimal capacity", new Color(0.14f, 0.93f, 0.34f));
                    AddAlert(alertsContainer, "No insurgent activity\nalong supply corridors", new Color(0.14f, 0.93f, 0.34f));
                }
            }

            // 8. Summary
            var summaryContainer = _panelLogistics.Q<VisualElement>("SummaryContainer");
            if (summaryContainer != null)
            {
                summaryContainer.Clear();
                
                float avgReserve = (country.reserveFuel + country.reserveAmmo + country.reserveFood + country.reserveMedical) / 4f;
                string overallStatus = avgReserve >= 80f ? "Excellent" : (avgReserve >= 50f ? "Good" : "Poor");
                Color overallColor = avgReserve >= 80f ? new Color(0.1f, 0.8f, 0.3f) : (avgReserve >= 50f ? Color.yellow : Color.red);
                AddSummaryStat(summaryContainer, "Overall Logistics Rating", $"{avgReserve:F0}%", overallStatus, overallColor, true);
                
                float upkeep = country.CalculateMilitaryUpkeep();
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                float upkeepLocal = upkeep * country.exchangeRate;
                AddSummaryStat(summaryContainer, "Monthly Operating Cost", $"{sym}{upkeepLocal:F2}B", "/ month", Color.white, false);
                
                float efficiency = Mathf.Clamp(avgReserve + (country.activeSupplyContracts != null ? country.activeSupplyContracts.Count * 5f : 0f), 0, 100);
                string effStatus = efficiency >= 80f ? "High" : (efficiency >= 50f ? "Average" : "Low");
                Color effColor = efficiency >= 80f ? new Color(0.1f, 0.8f, 0.3f) : (efficiency >= 50f ? Color.yellow : Color.red);
                AddSummaryStat(summaryContainer, "Supply Efficiency", $"{efficiency:F0}%", effStatus, effColor, false);
                
                float storage = Mathf.Clamp(100f - avgReserve + 20f, 10f, 95f);
                string storageStatus = storage < 85f ? "Good" : "Full";
                Color storageColor = storage < 85f ? new Color(0.1f, 0.8f, 0.3f) : Color.yellow;
                AddSummaryStat(summaryContainer, "Storage Utilization", $"{storage:F0}%", storageStatus, storageColor, false);
                
                int transportVehicles = country.unitAPCs + 120; // Approximation for logistics vehicles
                AddSummaryStat(summaryContainer, "Transport Fleet", $"{transportVehicles}", "Vehicles", Color.white, false);
            }
        }

        // --- HELPER METHODS ---        
        private void AddSupplyRow(VisualElement supplyParent, string name, float pct, string consumeRate)
        {
            var sRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4, height = 20 } };
            sRow.Add(new Label(name) { style = { color = Color.white, fontSize = 10, width = 110 } });
            
            var barBg = new VisualElement() { style = { height = 4, backgroundColor = new Color(1,1,1,0.1f), flexGrow = 1, marginRight = 15 } };
            var barFill = new VisualElement() { style = { height = 4, backgroundColor = new Color(0.14f, 0.93f, 0.34f), width = Length.Percent(pct) } };
            if (pct < 60) barFill.style.backgroundColor = Color.yellow;
            if (pct < 30) barFill.style.backgroundColor = Color.red;
            barBg.Add(barFill);
            sRow.Add(barBg);
            
            sRow.Add(new Label($"{pct:F0}%") { style = { color = Color.white, fontSize = 10, width = 35, unityTextAlign = TextAnchor.MiddleRight, marginRight = 15 } });
            
            sRow.Add(new Label(consumeRate) { style = { color = Color.white, fontSize = 10, width = 80, unityTextAlign = TextAnchor.MiddleRight, unityFontStyleAndWeight = FontStyle.Bold } });
            
            supplyParent.Add(sRow);
        }

        private void AddDepotRow(VisualElement parent, string name, float pct)
        {
            var row = new VisualElement() { style = { marginBottom = 6, flexShrink = 0 } };
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

        private void AddSupplyContractOffer(VisualElement parent, string label, string resourceKey, float amountPerTurn, float costB, int durationMonths)
        {
            var country = GameManager.Instance?.playerCountry;
            string sym = country != null && !string.IsNullOrEmpty(country.currencySymbol) ? country.currencySymbol : "₦";
            float localCost = costB * (country != null ? country.exchangeRate : 1f);

            var row = new VisualElement() { style = { 
                flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8, 
                paddingTop = 8, paddingBottom = 8, paddingLeft = 8, paddingRight = 8, 
                backgroundColor = new Color(1,1,1,0.03f), 
                borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                borderLeftWidth = 2, borderLeftColor = new Color(0.3f, 0.6f, 1f, 0.6f) 
            } };
            
            var left = new VisualElement() { style = { flexGrow = 1 } };
            left.Add(new Label(label) { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
            left.Add(new Label($"+{amountPerTurn:F0} {resourceKey} / month") { style = { color = new Color(0.7f, 0.7f, 0.7f), fontSize = 9, marginTop = 2 } });
            row.Add(left);
            
            var costBadge = new VisualElement() { style = { 
                backgroundColor = new Color(1f, 0.3f, 0.3f, 0.15f), 
                paddingTop = 3, paddingBottom = 3, paddingLeft = 6, paddingRight = 6, 
                borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                marginRight = 10 
            } };
            costBadge.Add(new Label($"{StateCraft.UI.UiFormat.Money(localCost, sym)}/mo") { style = { color = new Color(1f, 0.4f, 0.4f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            row.Add(costBadge);
            
            var btn = new Button(() => SignSupplyContract(label, resourceKey, amountPerTurn, costB, durationMonths)) { text = "SIGN" };
            btn.style.width = 50; btn.style.height = 22; btn.style.fontSize = 10; btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f, 0.2f); btn.style.color = new Color(0.14f, 0.93f, 0.34f);
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1; btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(0.14f, 0.93f, 0.34f, 0.5f); btn.style.borderBottomColor = new Color(0.14f, 0.93f, 0.34f, 0.5f); btn.style.borderLeftColor = new Color(0.14f, 0.93f, 0.34f, 0.5f); btn.style.borderRightColor = new Color(0.14f, 0.93f, 0.34f, 0.5f);
            btn.style.borderTopLeftRadius = 4; btn.style.borderTopRightRadius = 4; btn.style.borderBottomLeftRadius = 4; btn.style.borderBottomRightRadius = 4;
            btn.RegisterCallback<MouseEnterEvent>(e => btn.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f, 0.4f));
            btn.RegisterCallback<MouseLeaveEvent>(e => btn.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f, 0.2f));
            row.Add(btn);

            parent.Add(row);
        }

        private void AddActiveSupplyContract(VisualElement parent, SupplyContract contract)
        {
            var country = GameManager.Instance?.playerCountry;
            string sym = country != null && !string.IsNullOrEmpty(country.currencySymbol) ? country.currencySymbol : "₦";
            float localCost = contract.costPerTurn * (country != null ? country.exchangeRate : 1f);

            var row = new VisualElement() { style = { 
                flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8, 
                paddingTop = 8, paddingBottom = 8, paddingLeft = 8, paddingRight = 8, 
                backgroundColor = new Color(0.14f, 0.93f, 0.34f, 0.05f), 
                borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                borderLeftWidth = 2, borderLeftColor = new Color(0.14f, 0.93f, 0.34f)
            } };
            
            var left = new VisualElement() { style = { flexGrow = 1 } };
            left.Add(new Label(contract.label) { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
            
            var statusRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4 } };
            var activeDot = new VisualElement() { style = { width = 6, height = 6, backgroundColor = new Color(0.14f, 0.93f, 0.34f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, marginRight = 6 } };
            statusRow.Add(activeDot);
            statusRow.Add(new Label($"{contract.turnsRemaining} months remaining") { style = { color = new Color(0.7f, 0.7f, 0.7f), fontSize = 9 } });
            left.Add(statusRow);
            row.Add(left);
            
            var costBadge = new VisualElement() { style = { 
                backgroundColor = new Color(1f, 0.3f, 0.3f, 0.15f), 
                paddingTop = 3, paddingBottom = 3, paddingLeft = 6, paddingRight = 6, 
                borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4
            } };
            costBadge.Add(new Label($"{StateCraft.UI.UiFormat.Money(localCost, sym)}/mo") { style = { color = new Color(1f, 0.4f, 0.4f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            row.Add(costBadge);
            
            parent.Add(row);
        }

        private void SignSupplyContract(string label, string resourceKey, float amountPerTurn, float costB, int durationMonths)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            var contract = new SupplyContract()
            {
                id = System.Guid.NewGuid().ToString(),
                label = label,
                resourceType = resourceKey,
                amountPerTurn = amountPerTurn,
                costPerTurn = costB,
                turnsTotal = durationMonths,
                turnsRemaining = durationMonths
            };

            country.activeSupplyContracts.Add(contract);
            NotificationManager.Instance?.AddNotification($"Signed Contract: {label}. Cost will be added to military upkeep.", Color.green);
            
            RefreshLogisticsTab(country);
        }



        private void AddAlert(VisualElement parent, string msg, Color col)
        {
            var box = new VisualElement() { style = { 
                flexDirection = FlexDirection.Row, alignItems = Align.Center,
                marginRight = 8, marginBottom = 8, paddingTop = 8, paddingBottom = 8, paddingLeft = 12, paddingRight = 12, 
                backgroundColor = new Color(col.r, col.g, col.b, 0.1f), 
                borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                borderLeftWidth = 3, borderLeftColor = col 
            } };
            
            var icon = new VisualElement() { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = col, marginRight = 8 } };
            box.Add(icon);

            // Strip newline for single-line display
            msg = msg.Replace("\n", " ");
            box.Add(new Label(msg) { style = { color = Color.white, fontSize = 10, whiteSpace = WhiteSpace.Normal, unityFontStyleAndWeight = FontStyle.Bold } });
            parent.Add(box);
        }

        private void AddSummaryStat(VisualElement parent, string title, string val, string sub, Color valCol, bool large)
        {
            var box = new VisualElement() { style = { 
                alignItems = Align.FlexStart, 
                backgroundColor = new Color(1, 1, 1, 0.03f),
                paddingTop = 6, paddingBottom = 6, paddingLeft = 12, paddingRight = 12,
                borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4
            } };
            box.Add(new Label(title) { style = { color = new Color(0.6f, 0.65f, 0.7f), fontSize = 9, marginBottom = 4, unityFontStyleAndWeight = FontStyle.Bold } });
            
            var valRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexEnd } };
            valRow.Add(new Label(val) { style = { color = valCol, fontSize = large ? 16 : 13, unityFontStyleAndWeight = FontStyle.Bold } });
            if (!string.IsNullOrEmpty(sub))
            {
                valRow.Add(new Label(sub) { style = { color = large ? valCol : new Color(0.5f, 0.5f, 0.5f), fontSize = 9, marginLeft = 4, paddingBottom = 2 } });
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
