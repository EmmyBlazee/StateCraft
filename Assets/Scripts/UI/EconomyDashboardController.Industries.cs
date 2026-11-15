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
        private void UpgradeIndustry(string industryId)
        {
            var country = GameManager.Instance?.playerCountry;
            var db = GameManager.Instance?.EconomyDatabase;
            if (country == null || db == null || string.IsNullOrEmpty(industryId)) return;

            foreach (var ind in db.industries)
            {
                if (ind.id != industryId) continue;

                if (StateCraft.Systems.EconomyLogicEvaluator.TryUpgradeIndustry(country, ind, out string msg))
                {
                    NotifyEconomy(msg, Color.green);
                    RefreshDisplay();
                }
                else
                {
                    ShowEconomyModal("Upgrade Blocked", ind.title, msg, "OK", null);
                }
                return;
            }
        }

        private void UpgradeIndustryById(string industryId)
        {
            UpgradeIndustry(industryId);
        }

        private void RefreshIndustryTab(CountryDataSO country, string sym)
        {
            if (_root == null) return;
            var db = GameManager.Instance?.EconomyDatabase;
            if (db == null) { Debug.LogWarning("[IndustryUI] RefreshIndustryTab — GameManager.EconomyDatabase is NULL. Assign an EconomyDatabaseSO asset to GameManager.EconomyDatabase for this tab to show real data."); return; }

            var panel = _panelIndustry ?? _root.Q<VisualElement>("PanelIndustry");
            if (panel == null) { Debug.LogWarning("[IndustryUI] PanelIndustry not found — aborting refresh."); return; }

            if (_lblIndOverviewOutput == null) _lblIndOverviewOutput = panel.Q<Label>("IndustryOverview_Output");
            if (_lblIndOverviewGDPPct == null) _lblIndOverviewGDPPct = panel.Q<Label>("IndustryOverview_GDPPct");
            if (_lblIndOverviewMeter  == null) _lblIndOverviewMeter  = panel.Q<Label>("IndustryOverview_Meter");
            if (_pbIndOverview        == null) _pbIndOverview        = panel.Q<ProgressBar>("IndustryOverview_Bar");
            if (_lblIndTopName        == null) _lblIndTopName        = panel.Q<Label>("IndustryTop_Name");
            if (_lblIndTopRevenue     == null) _lblIndTopRevenue     = panel.Q<Label>("IndustryTop_Revenue");
            if (_lblIndTopGrowth      == null) _lblIndTopGrowth      = panel.Q<Label>("IndustryTop_Growth");
            if (_lblIndWeakName       == null) _lblIndWeakName       = panel.Q<Label>("IndustryWeak_Name");
            if (_lblIndWeakRevenue    == null) _lblIndWeakRevenue    = panel.Q<Label>("IndustryWeak_Revenue");
            if (_lblIndAdviserName    == null) _lblIndAdviserName    = panel.Q<Label>("IndustryAdviserName");
            if (_lblIndAdviserRec     == null) _lblIndAdviserRec     = panel.Q<Label>("IndustryAdviserRec");
            if (_lblIndAdviserBody    == null) _lblIndAdviserBody    = panel.Q<Label>("IndustryAdviserBody");
            if (_industryAlertsContainer == null) _industryAlertsContainer = panel.Q<VisualElement>("IndustryAlertsContainer");

            float totalRev = 0f;
            StateCraft.Data.Economy.IndustryDataSO topInd = null;
            StateCraft.Data.Economy.IndustryDataSO weakInd = null;
            float topRev = -1f;
            float weakRev = float.MaxValue;

            foreach (var ind in db.industries)
            {
                float rev = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, ind);
                totalRev += rev;
                if (rev > topRev) { topRev = rev; topInd = ind; }
                if (rev < weakRev) { weakRev = rev; weakInd = ind; }
            }

            if (_lblIndOverviewOutput != null) _lblIndOverviewOutput.text = UiFormat.Money(totalRev, sym) + " / mo";
            float pctGDP = country.gdp > 0 ? (totalRev / (country.gdp * country.exchangeRate / 12f)) * 100f : 0f;
            if (_lblIndOverviewGDPPct != null) _lblIndOverviewGDPPct.text = $"Total Contribution to GDP: {pctGDP:F1}%";
            
            var powerLabel = panel.Q<Label>("IndustryOverview_Power");
            if (powerLabel != null)
            {
                float cap = GetTotalPowerCapacity(country);
                powerLabel.text = $"GRID CAPACITY: {cap:N0} MW ({country.electricityLevel:F0}% Coverage)";
            }

            if (_lblIndOverviewMeter  != null) _lblIndOverviewMeter.text  = $"{pctGDP:F1}% of GDP";
            if (_pbIndOverview        != null) _pbIndOverview.value       = pctGDP;

            if (topInd != null) {
                if (_lblIndTopName    != null) _lblIndTopName.text    = topInd.title;
                if (_lblIndTopRevenue != null) _lblIndTopRevenue.text = $"{UiFormat.Money(topRev, sym)} / mo";
                if (_lblIndTopGrowth  != null) _lblIndTopGrowth.text  = StateCraft.Systems.EconomyLogicEvaluator.IsIndustryBullish(country, topInd.id) ? "▲ Growing" : "▼ Slowing";
            }
            if (weakInd != null) {
                if (_lblIndWeakName    != null) _lblIndWeakName.text    = weakInd.title;
                if (_lblIndWeakRevenue != null) _lblIndWeakRevenue.text = $"{UiFormat.Money(weakRev, sym)} / mo";
            }

            foreach (var ind in db.industries)
            {
                int level = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryLevel(country, ind.id);
                float cost = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryUpgradeCost(country, ind);
                float revenue = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, ind);
                float contrib = totalRev > 0 ? (revenue / totalRev) * 100f : 0f;
                bool bullish = StateCraft.Systems.EconomyLogicEvaluator.IsIndustryBullish(country, ind.id);
                string synergy = StateCraft.Systems.EconomyLogicEvaluator.GetIndustrySynergy(country, ind.id);
                float workforce = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryWorkforce(country, ind.id) / 1000f;

                if (!_industryRowRefs.TryGetValue(ind.id, out var refs))
                {
                    refs = new IndustryRowRefs();
                    _industryRowRefs[ind.id] = refs;
                }
                if (refs.Tier     == null) refs.Tier     = panel.Q<Label>($"Industry{ind.id}_Tier");
                if (refs.Btn      == null)
                {
                    refs.Btn = panel.Q<Button>($"Industry{ind.id}_Btn");
                    if (refs.Btn != null)
                    {
                        string capturedId = ind.id;
                        refs.Btn.RegisterCallback<ClickEvent>(_ => UpgradeIndustry(capturedId));
                    }
                }
                if (refs.Stat1    == null) refs.Stat1    = panel.Q<Label>($"Industry{ind.id}_Stat1");
                if (refs.Stat2    == null) refs.Stat2    = panel.Q<Label>($"Industry{ind.id}_Stat2");
                if (refs.Stat3    == null) refs.Stat3    = panel.Q<Label>($"Industry{ind.id}_Stat3");
                if (refs.Stat4    == null) refs.Stat4    = panel.Q<Label>($"Industry{ind.id}_Stat4");
                if (refs.Growth   == null) refs.Growth   = panel.Q<Label>($"Industry{ind.id}_Growth");
                if (refs.Outlook  == null) refs.Outlook  = panel.Q<Label>($"Industry{ind.id}_Outlook");
                if (refs.Pressure == null) refs.Pressure = panel.Q<Label>($"Industry{ind.id}_Pressure");
                if (refs.Benefits == null) refs.Benefits = panel.Q<Label>($"Industry{ind.id}_Benefits");
                if (refs.Ring     == null) refs.Ring     = panel.Q<Label>($"Ring{ind.id}");
                
                if (refs.RingElem == null)
                {
                    var container = panel.Q<VisualElement>($"RingElem{ind.id}");
                    if (container != null)
                    {
                        refs.RingElem = new ProgressRingElement();
                        refs.RingElem.style.position = Position.Absolute;
                        refs.RingElem.style.left = 0;
                        refs.RingElem.style.right = 0;
                        refs.RingElem.style.top = 0;
                        refs.RingElem.style.bottom = 0;
                        container.Insert(0, refs.RingElem);
                    }
                }

                if (refs.Tier != null) refs.Tier.text = $"Tier {level}/10  •  Workforce {workforce:F1}k";
                bool atMax = level >= 10;
                bool hasResource = StateCraft.Systems.EconomyLogicEvaluator.HasRequiredResource(country, ind.id, out string missingRes);
                if (refs.Btn != null)
                {
                    refs.Btn.SetEnabled(!atMax && hasResource);
                    if (atMax)
                        refs.Btn.text = "MAX TIER REACHED";
                    else if (!hasResource)
                        refs.Btn.text = $"NO DEPOSITS\nNeed: {missingRes}";
                    else
                        refs.Btn.text = $"UPGRADE TO TIER {level + 1}\nCost: {UiFormat.Money(cost, sym)}";
                }
                if (refs.Stat1 != null) refs.Stat1.text = $"Contribution to GDP\n{contrib:F1}%";
                if (refs.Stat2 != null) refs.Stat2.text = $"Synergy\n{synergy}";
                if (refs.Stat3 != null) refs.Stat3.text = $"Efficiency\n{(level * 10f):F0}%";
                if (refs.Stat4 != null) refs.Stat4.text = $"Revenue\n{UiFormat.Money(revenue, sym)}";

                if (refs.Growth != null) {
                    refs.Growth.text = $"Growth (YoY)\n{(bullish ? "+" : "")}{(bullish ? 3 + level : -1 - level / 2f):F1}%";
                    refs.Growth.EnableInClassList("positive", bullish);
                    refs.Growth.EnableInClassList("negative", !bullish);
                }

                if (refs.Outlook != null) {
                    refs.Outlook.text = $"Outlook\n{(bullish ? "Strong" : "Weak")}";
                    refs.Outlook.EnableInClassList("positive", bullish);
                    refs.Outlook.EnableInClassList("negative", !bullish);
                    refs.Outlook.EnableInClassList("warning", false);
                }

                if (refs.Pressure != null) {
                    if (bullish)
                    {
                        refs.Pressure.text = "▲ TRENDING WELL\nOptimal conditions.";
                        refs.Pressure.EnableInClassList("warning", false);
                        refs.Pressure.EnableInClassList("negative", false);
                    }
                    else
                    {
                        var constraints = new List<string>();
                        if (!hasResource) constraints.Add($"Resource: {missingRes}");
                        if (ind.id == "Oil")
                        {
                            if (country.electricityLevel <= 50f) constraints.Add($"Power Grid ({country.electricityLevel:F0}%/50%)");
                            if (country.portLevel <= 1) constraints.Add($"Ports (Lv.{country.portLevel}/2)");
                        }
                        else if (ind.id == "Mining")
                        {
                            if (country.railLevel <= 2) constraints.Add($"Rail Network (Lv.{country.railLevel}/3)");
                            if (country.resourceSovereignty <= 60f) constraints.Add($"Sovereignty ({country.resourceSovereignty:F0}%/60%)");
                        }
                        else if (ind.id == "Agri")
                        {
                            if (country.foodSecurity <= 55f) constraints.Add($"Food Security ({country.foodSecurity:F0}%/55%)");
                            if (country.roadLevel <= 2) constraints.Add($"Road Network (Lv.{country.roadLevel}/3)");
                        }
                        else if (ind.id == "Mfg")
                        {
                            if (country.electricityLevel <= 55f) constraints.Add($"Power Grid ({country.electricityLevel:F0}%/55%)");
                            if (country.roadLevel <= 3) constraints.Add($"Road Network (Lv.{country.roadLevel}/4)");
                        }
                        else if (ind.id == "Tech")
                        {
                            if (country.telecomLevel <= 3) constraints.Add($"Telecom (Lv.{country.telecomLevel}/4)");
                            if (country.educationLevel <= 2) constraints.Add($"Education (Lv.{country.educationLevel}/3)");
                        }

                        if (constraints.Count > 0)
                        {
                            refs.Pressure.text = $"▼ CONSTRAINED BY:\n{string.Join(", ", constraints)}";
                        }
                        else
                        {
                            refs.Pressure.text = "▼ PRESSURE: HIGH\nSuboptimal synergy.";
                        }
                        refs.Pressure.EnableInClassList("warning", true);
                        refs.Pressure.EnableInClassList("negative", false);
                    }
                }

                if (refs.Benefits != null) refs.Benefits.text = $"Benefits: +1 Tier, increased workforce & capacity.";
                if (refs.Ring     != null) refs.Ring.text     = $"{contrib:F0}%\n{ind.title}";
                if (refs.RingElem != null) refs.RingElem.SetValue(contrib);
            }

            if (_currentAdviserRec != null) {
                if (_lblIndAdviserName != null) _lblIndAdviserName.text = AdviserEngine.GetEconomicAdviserName(country);
                if (_lblIndAdviserRec  != null) _lblIndAdviserRec.text  = _currentAdviserRec.Value.Category;
                if (_lblIndAdviserBody != null) _lblIndAdviserBody.text = _currentAdviserRec.Value.Rationale;
            }

            var alertsCont = _industryAlertsContainer;
            if (alertsCont != null) {
                while (alertsCont.childCount > 1) alertsCont.RemoveAt(1);
                var alerts = BuildUIAlerts(country).FindAll(a => a.Severity != AlertSeverity.Info);
                int ct = 0;
                foreach (var a in alerts) {
                    if (ct >= 3) break;
                    var row = new VisualElement(); row.AddToClassList("industry-alert-row");
                    var icon = new Label("⚠"); icon.AddToClassList("industry-alert-icon");
                    icon.AddToClassList(a.Severity == AlertSeverity.Critical ? "negative" : "warning");
                    row.Add(icon);
                    var info = new VisualElement();
                    var title = new Label(a.Category); title.AddToClassList("industry-card-title"); info.Add(title);
                    var copy = new Label(a.Message); copy.AddToClassList("industry-copy"); info.Add(copy);
                    row.Add(info);
                    alertsCont.Add(row);
                    ct++;
                }
                if (ct == 0) {
                    var empty = new Label("All industries operating nominally.");
                    empty.AddToClassList("industry-copy"); empty.style.marginTop = 10;
                    alertsCont.Add(empty);
                }
            }
        }

    }
}