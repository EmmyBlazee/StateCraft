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

            // Query from _panelIndustry (the ScrollView) rather than _root.
            // _root is the EconomyDashboard scoped element; when PanelIndustry is display:none
            // on the first RefreshDisplay call, Unity's Q<> traversal doesn't descend into it
            // reliably, so every query returns null. Querying from the panel itself works
            // regardless of its display state because the element is still in the tree.
            var panel = _panelIndustry ?? _root.Q<VisualElement>("PanelIndustry");
            if (panel == null) { Debug.LogWarning("[IndustryUI] PanelIndustry not found — aborting refresh."); return; }

            // Re-attempt any static refs that were null at startup (panel was display:none then)
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

            // Overview Card
            if (_lblIndOverviewOutput != null) _lblIndOverviewOutput.text = UiFormat.Money(totalRev, sym) + " / mo";
            float pctGDP = country.gdp > 0 ? (totalRev / (country.gdp * country.exchangeRate / 12f)) * 100f : 0f;
            if (_lblIndOverviewGDPPct != null) _lblIndOverviewGDPPct.text = $"Total Contribution to GDP: {pctGDP:F1}%";
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

                // Cache this row's widgets once; retry only the ones still null (panel may not
                // have existed in the tree yet the first time we looked).
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
                        // Capture ind.id for the closure — do NOT capture the loop variable
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

                if (refs.Tier != null) refs.Tier.text = $"Tier {level}/10  •  Workforce {workforce:F1}k";
                bool atMax = level >= 10;
                if (refs.Btn != null)
                {
                    refs.Btn.SetEnabled(!atMax);
                    refs.Btn.text = atMax
                        ? "MAX TIER REACHED"
                        : $"UPGRADE TO TIER {level + 1}\nCost: {UiFormat.Money(cost, sym)}";
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
                    refs.Pressure.text = bullish ? "▲ TRENDING WELL\nOptimal conditions." : "▼ PRESSURE: HIGH\nSuboptimal synergy.";
                    refs.Pressure.EnableInClassList("warning", !bullish);
                    refs.Pressure.EnableInClassList("negative", false);
                }

                if (refs.Benefits != null) refs.Benefits.text = $"Benefits: +1 Tier, increased workforce & capacity.";
                if (refs.Ring     != null) refs.Ring.text     = $"{contrib:F0}%\n{ind.title}";
            }

            // Adviser
            if (_currentAdviserRec != null) {
                if (_lblIndAdviserName != null) _lblIndAdviserName.text = AdviserEngine.GetEconomicAdviserName(country);
                if (_lblIndAdviserRec  != null) _lblIndAdviserRec.text  = _currentAdviserRec.Value.Category;
                if (_lblIndAdviserBody != null) _lblIndAdviserBody.text = _currentAdviserRec.Value.Rationale;
            }

            // Alerts
            var alertsCont = _industryAlertsContainer;
            if (alertsCont != null) {
                while (alertsCont.childCount > 1) alertsCont.RemoveAt(1);
                var alerts = BuildUIAlerts(country).FindAll(a => a.Severity != AlertSeverity.Info);
                // just pick top 3
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
                    var view = new Label("VIEW"); view.AddToClassList("market"); row.Add(view);
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