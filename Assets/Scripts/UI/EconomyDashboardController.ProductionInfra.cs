using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using System.Linq;

namespace StateCraft.UI
{
    public partial class EconomyDashboardController
    {
        private void UpdateTariffRate(float val)
        {
            if (GameManager.Instance?.playerCountry != null)
            {
                GameManager.Instance.playerCountry.importTariffRate = val;
                if (_labelTariffRate != null) _labelTariffRate.text = $"{val:F0}%";
            }
        }

        private void PopulateProduction(CountryDataSO country)
        {
            float foodNeed = country.population * 0.001f;
            float foodStock = country.invRiceTonnes + country.invCassavaTonnes;
            float foodMonths = foodNeed > 0 ? foodStock / foodNeed : 99f;

            if (_productionSummary != null)
            {
                _productionSummary.Clear();
                AddSummaryMetric(_productionSummary, "FOOD SECURITY", $"{country.foodSecurity:F0}%", StateCraft.Systems.EconomyLogicEvaluator.FoodSecurityColor(country));
                AddSummaryMetric(_productionSummary, "RESERVES", $"{foodMonths:F1} mo food", foodMonths >= 2f ? Color.green : Color.red);
                AddSummaryMetric(_productionSummary, "SOVEREIGNTY", $"{country.resourceSovereignty:F0}%", new Color(0, 210/255f, 1f));
                AddSummaryMetric(_productionSummary, "ENVIRONMENT", $"{country.environmentScore:F0}/100", Color.grey);
            }

            if (_productionCardsContainer == null) return;
            _productionCardsContainer.Clear();
            var db = GameManager.Instance?.EconomyDatabase;
            if (db != null)
            {
                foreach (var comm in db.commodities)
                    AddProductionCard(country, comm);
            }

            if (_resourceDepositsList != null)
            {
                _resourceDepositsList.Clear();
                if (country.resources.Count == 0)
                {
                    _resourceDepositsList.Add(new Label("No mapped geological surveys.") { style = { color = Color.grey, fontSize = 9 } });
                }
                else
                {
                    foreach (var res in country.resources)
                        AddDepositChip(res);
                }
            }
        }

        private void AddSummaryMetric(VisualElement row, string label, string value, Color valueColor)
        {
            var col = new VisualElement() { style = { alignItems = Align.Center, minWidth = 80 } };
            col.Add(new Label(label) { style = { fontSize = 8, color = Color.grey } });
            col.Add(new Label(value) { style = { fontSize = 12, color = valueColor, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 4 } });
            row.Add(col);
        }

        private void AddProductionCard(CountryDataSO country, StateCraft.Data.Economy.CommodityDataSO comm)
        {
            float stock = StateCraft.Systems.EconomyLogicEvaluator.GetCommodityStock(country, comm.id);
            float prod = StateCraft.Systems.EconomyLogicEvaluator.GetCommodityProduction(country, comm.id);
            float cons = StateCraft.Systems.EconomyLogicEvaluator.GetCommodityConsumption(country, comm.id);
            float net = prod - cons;
            string status = StateCraft.Systems.EconomyLogicEvaluator.StockpileStatus(stock, cons);
            Color statusColor = status.Contains("CRITICAL") || status == "LOW"
                ? new Color(255/255f, 82/255f, 82/255f)
                : (status.Contains("STRATEGIC") || status == "HEALTHY" ? new Color(0, 230/255f, 118/255f) : new Color(241/255f, 196/255f, 15/255f));

            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.width = Length.Percent(48);
            card.style.marginBottom = 12;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8 } };
            header.Add(new Label(comm.title) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(status) { style = { color = statusColor, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
            card.Add(header);

            card.Add(new Label(comm.role) { style = { color = Color.grey, fontSize = 8, marginBottom = 6 } });
            card.Add(new Label($"STOCKPILE: {stock:N0} {comm.unit}") { style = { color = new Color(0, 210/255f, 255/255f), fontSize = 11 } });

            var flowRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 8, justifyContent = Justify.SpaceBetween } };
            flowRow.Add(new Label($"+{prod:N0}/mo") { style = { color = new Color(0, 230/255f, 118/255f), fontSize = 9 } });
            flowRow.Add(new Label(cons > 0 ? $"-{cons:N0}/mo" : "—") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 9 } });
            card.Add(flowRow);

            card.Add(new Label($"NET {(net >= 0 ? "+" : "")}{net:N0} {comm.unit}/mo") { style = { color = Color.grey, fontSize = 8, marginTop = 4 } });

            float barVal = cons > 0 ? Mathf.Clamp(stock / (cons * 6f) * 100f, 0f, 100f) : Mathf.Min(stock > 0 ? 100f : 0f, 100f);
            card.Add(new ProgressBar() { value = barVal, highValue = 100, style = { height = 4, marginTop = 8 } });

            _productionCardsContainer.Add(card);
        }

        private void AddDepositChip(ResourceDeposit res)
        {
            var chip = new VisualElement();
            chip.AddToClassList("sub-panel");
            chip.style.width = Length.Percent(31);
            chip.style.marginBottom = 8;
            chip.style.paddingTop = 8;
            chip.style.paddingBottom = 8;
            chip.style.paddingLeft = 10;
            chip.style.paddingRight = 10;

            chip.Add(new Label(res.type.ToString().ToUpper()) { style = { color = Color.white, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
            chip.Add(new Label($"Abundance {res.abundance:F0}%") { style = { color = new Color(0, 210/255f, 1f), fontSize = 8, marginTop = 4 } });
            chip.Add(new ProgressBar() { value = res.abundance, highValue = 100, style = { height = 3, marginTop = 6 } });
            _resourceDepositsList.Add(chip);
        }

        private void PopulateInfra(CountryDataSO country)
        {
            float score = StateCraft.Systems.EconomyLogicEvaluator.NationalInfraScore(country);

            if (_infraSummary != null)
            {
                _infraSummary.Clear();
                var top = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
                top.Add(new Label($"NATIONAL INFRASTRUCTURE INDEX") { style = { color = Color.grey, fontSize = 9, letterSpacing = 2 } });
                top.Add(new Label($"{score:F0}/100") { style = { color = score >= 60f ? new Color(0, 230/255f, 118/255f) : new Color(241/255f, 196/255f, 15/255f), fontSize = 22, unityFontStyleAndWeight = FontStyle.Bold } });
                _infraSummary.Add(top);
                _infraSummary.Add(new ProgressBar() { value = score, highValue = 100, style = { height = 8, marginTop = 8, marginBottom = 8 } });

                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                AddSummaryMetric(row, "TRANSPORT", $"{(country.roadLevel + country.railLevel + country.portLevel) * 3.3f:F0}%", new Color(0, 210/255f, 1f));
                AddSummaryMetric(row, "GRID COVERAGE", $"{country.electricityLevel:F0}%", country.electricityLevel > 50f ? Color.green : Color.red);
                
                float cap = GetTotalPowerCapacity(country);
                float dem = GetTotalPowerDemand(country);
                float netPower = cap - dem;
                
                AddSummaryMetric(row, "GRID CAPACITY", $"{cap:N0} MW", new Color(255/255f, 202/255f, 40/255f));
                
                string netStr = netPower >= 0 ? $"+{netPower:F0} MW" : $"{netPower:F0} MW";
                AddSummaryMetric(row, "GRID BALANCE", netStr, netPower >= 0 ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f));
                
                AddSummaryMetric(row, "DIGITAL", $"{country.telecomLevel * 10f:F0}%", new Color(186/255f, 104/255f, 200/255f));
                _infraSummary.Add(row);
            }

            if (_infraSynergyText != null)
            {
                var synergies = new List<string>();
                if (country.portLevel > country.railLevel)
                    synergies.Add("Ports outpacing rail — consider Trans-National Rail.");
                if (country.electricityLevel < 50f && country.indManufacturing > 3)
                    synergies.Add("Factories constrained by weak grid.");
                if (country.roadLevel >= 5 && country.foodSecurity < 60f)
                    synergies.Add("Roads built but farms still isolated — invest agri.");
                if (country.telecomLevel >= 4 && country.indTechIT < 3)
                    synergies.Add("Digital backbone ready for tech park expansion.");
                if (country.hasNuclearPower)
                    synergies.Add("Nuclear baseload stabilizing industrial zones.");
                _infraSynergyText.text = synergies.Count > 0 ? string.Join(" ", synergies) : "All major networks operating within design parameters.";
            }

            if (_infraCardsContainer == null) return;
            _infraCardsContainer.Clear();

            var db = GameManager.Instance?.EconomyDatabase;
            if (db != null)
            {
                StateCraft.Data.Economy.InfraCategory lastCat = (StateCraft.Data.Economy.InfraCategory)(-1);
                foreach (var def in db.infrastructure)
                {
                    if (def.category != lastCat)
                    {
                        lastCat = def.category;
                        _infraCardsContainer.Add(new Label(lastCat.ToString().ToUpper())
                        {
                            style = { color = Color.grey, fontSize = 9, letterSpacing = 2, marginTop = 8, marginBottom = 6 }
                        });
                    }
                    AddInfraCard(country, def);
                }
            }

            AddPowerPlantBreakdown(country);
        }

        private void AddInfraCard(CountryDataSO country, StateCraft.Data.Economy.InfraDataSO def)
        {
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float rating = StateCraft.Systems.EconomyLogicEvaluator.GetInfraRating(country, def.id);
            float cost = StateCraft.Systems.EconomyLogicEvaluator.GetInfraInvestCost(country, def);
            string bottleneck = StateCraft.Systems.EconomyLogicEvaluator.GetInfraBottleneck(country, def.id);
            bool canInvest = StateCraft.Systems.EconomyLogicEvaluator.CanInvest(country, def.id);

            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.marginBottom = 10;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
            header.Add(new Label(def.title) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(def.iconTag) { style = { color = new Color(0, 210/255f, 1f), fontSize = 8 } });
            card.Add(header);

            card.Add(new Label(StateCraft.Systems.EconomyLogicEvaluator.GetInfraStatus(country, def.id)) { style = { color = new Color(0, 210/255f, 1f), fontSize = 10, marginTop = 6 } });
            card.Add(new Label(def.effectDescription) { style = { color = Color.grey, fontSize = 8, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

            if (bottleneck != "—")
                card.Add(new Label($"⚠ {bottleneck}") { style = { color = new Color(255/255f, 171/255f, 64/255f), fontSize = 8, marginTop = 6 } });

            card.Add(new ProgressBar() { value = rating, highValue = 100, style = { height = 6, marginTop = 8 } });

            var btnRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 10, justifyContent = Justify.SpaceBetween } };
            var investBtn = new Button(() =>
            {
                if (StateCraft.Systems.EconomyLogicEvaluator.TryInvestInfra(country, def, out string msg))
                    NotificationManager.Instance?.AddNotification(msg, Color.cyan);
                else
                    NotificationManager.Instance?.AddNotification(msg, Color.red);
                RefreshDisplay();
            })
            { text = canInvest ? $"INVEST · {UiFormat.Money(cost, sym)}" : "MAX LEVEL" };
            investBtn.AddToClassList("button-premium");
            investBtn.style.height = 26;
            investBtn.style.fontSize = 8;
            investBtn.SetEnabled(canInvest);
            btnRow.Add(investBtn);

            if (def.id == "Power")
            {
                var plantBtn = new Button(() => ShowBlueprintCatalog())
                { text = "BUILD PLANT" };
                plantBtn.AddToClassList("button-premium");
                plantBtn.style.height = 26;
                plantBtn.style.fontSize = 8;
                plantBtn.style.marginLeft = 8;
                btnRow.Add(plantBtn);
            }
            card.Add(btnRow);
            _infraCardsContainer.Add(card);
        }

        private void AddPowerPlantBreakdown(CountryDataSO c)
        {
            var panel = new VisualElement();
            panel.AddToClassList("sub-panel");
            panel.style.paddingTop = 12;
            panel.style.paddingBottom = 12;
            panel.style.paddingLeft = 14;
            panel.style.paddingRight = 14;
            panel.style.marginTop = 8;

            panel.Add(new Label("GENERATION MIX") { style = { color = Color.grey, fontSize = 9, letterSpacing = 2, marginBottom = 8 } });
            AddPlantRow(panel, "Gas", c.powerGasPlants, $"{StateCraft.Systems.PowerGridManager.GasPlantCapacity} MW each");
            AddPlantRow(panel, "Hydro", c.powerHydroPlants, $"{StateCraft.Systems.PowerGridManager.HydroPlantCapacity} MW each");
            AddPlantRow(panel, "Nuclear", c.powerNuclearPlants, c.hasNuclearPower ? $"{StateCraft.Systems.PowerGridManager.NuclearPlantCapacity} MW · online" : "Requires uranium");
            AddPlantRow(panel, "Solar", c.powerSolarPlants, $"{StateCraft.Systems.PowerGridManager.SolarPlantCapacity} MW each");
            AddPlantRow(panel, "Wind", c.powerWindPlants, $"{StateCraft.Systems.PowerGridManager.WindPlantCapacity} MW each");
            panel.Add(new Label($"Transmission efficiency {c.gridTransmissionEfficiency:P0} · Blackout risk {c.currentBlackoutRisk:P0}")
            {
                style = { color = Color.grey, fontSize = 8, marginTop = 8 }
            });
            _infraCardsContainer.Add(panel);
        }

        private static void AddPlantRow(VisualElement parent, string name, int count, string note)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
            row.Add(new Label($"{name}: {count}") { style = { color = Color.white, fontSize = 9 } });
            row.Add(new Label(note) { style = { color = Color.grey, fontSize = 8 } });
            parent.Add(row);
        }

        private float GetTotalPowerCapacity(CountryDataSO country)
        {
            if (country == null) return 0f;
            Debug.Log($"[PowerDebug] country: {country.countryName}, gas: {country.powerGasPlants}, hydro: {country.powerHydroPlants}, nuclear: {country.powerNuclearPlants}, solar: {country.powerSolarPlants}, wind: {country.powerWindPlants}");
            if (PowerGridManager.Instance != null)
            {
                float calculated = PowerGridManager.Instance.CalculateCapacity(country);
                Debug.Log($"[PowerDebug] PowerGridManager capacity: {calculated}");
                return calculated;
            }
            
            float fallback = country.powerGasPlants * 500f +
                   country.powerHydroPlants * 1200f +
                   country.powerNuclearPlants * 3000f +
                   country.powerSolarPlants * 200f +
                   country.powerWindPlants * 150f;
            Debug.Log($"[PowerDebug] fallback capacity: {fallback}");
            return fallback;
        }

        private float GetTotalPowerDemand(CountryDataSO country)
        {
            if (PowerGridManager.Instance != null)
                return PowerGridManager.Instance.CalculateDemand(country);
            
            float civilianDemand = (country.population / 1000000f) * 50f;
            float industrialDemand = (country.indAgriRice + country.indAgriCassava + country.indAgriCocoa + 
                                       country.indMiningLocal + country.indMiningOil + 
                                       country.indTechIT + country.indTechFintech) * 150f;
            float militaryDemand = 500f;
            return civilianDemand + industrialDemand + militaryDemand;
        }

    }
}