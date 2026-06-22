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

        private void PopulateIndustry(CountryDataSO country, string sym)
        {
            var db = GameManager.Instance?.EconomyDatabase;
            if (db == null) return;

            float totalRev = 0f;
            foreach (var ind in db.industries)
                totalRev += StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, ind);

            if (_industrySummary != null)
            {
                _industrySummary.Clear();
                _industrySummary.Add(new Label($"INDUSTRIAL OUTPUT · Est. {UiFormat.Money(totalRev, sym)}/mo across {db.industries.Count} sectors")
                {
                    style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
                });

                var barRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, height = 8 } };
                Color[] sectorColors =
                {
                    new Color(1f, 0.6f, 0.1f), new Color(0.6f, 0.6f, 0.65f), new Color(0.2f, 0.8f, 0.3f),
                    new Color(0.3f, 0.5f, 1f), new Color(0.7f, 0.4f, 1f)
                };
                for (int i = 0; i < db.industries.Count; i++)
                {
                    var ind = db.industries[i];
                    float share = totalRev > 0 ? StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, ind) / totalRev : 0.2f;
                    var seg = new VisualElement() { style = { flexGrow = share, backgroundColor = sectorColors[i % sectorColors.Length], height = 8 } };
                    barRow.Add(seg);
                }
                _industrySummary.Add(barRow);
                _industrySummary.Add(new Label($"Unemployment {country.unemploymentRate:F0}% · Corruption leakage {country.corruptionIndex:F0}%")
                {
                    style = { color = Color.grey, fontSize = 8, marginTop = 8 }
                });
            }

            if (_industryCardsContainer == null) return;
            _industryCardsContainer.Clear();
            foreach (var ind in db.industries)
                AddIndustryCard(country, ind, sym);
        }

        private void AddIndustryCard(CountryDataSO country, StateCraft.Data.Economy.IndustryDataSO ind, string sym)
        {
            int level = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryLevel(country, ind.id);
            bool bullish = StateCraft.Systems.EconomyLogicEvaluator.IsIndustryBullish(country, ind.id);
            float cost = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryUpgradeCost(country, ind);
            float revenue = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, ind);

            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.width = Length.Percent(48);
            card.style.marginBottom = 12;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            header.Add(new Label(ind.title) { style = { color = new Color(0, 210/255f, 1f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(bullish ? "▲ BULLISH" : "▼ PRESSURE") { style = { color = bullish ? new Color(0, 230/255f, 118/255f) : new Color(241/255f, 196/255f, 15/255f), fontSize = 8 } });
            card.Add(header);

            card.Add(new Label($"Tier {level}/10 · Workforce {(StateCraft.Systems.EconomyLogicEvaluator.GetIndustryWorkforce(country, ind.id) / 1000f):F1}k") { style = { color = Color.white, fontSize = 10, marginTop = 6 } });
            card.Add(new Label($"Revenue ≈ {UiFormat.Money(revenue, sym)}/mo") { style = { color = new Color(0, 230/255f, 118/255f), fontSize = 10, marginTop = 4 } });
            card.Add(new Label($"Synergy: {StateCraft.Systems.EconomyLogicEvaluator.GetIndustrySynergy(country, ind.id)}") { style = { color = Color.grey, fontSize = 8, marginTop = 6, whiteSpace = WhiteSpace.Normal } });

            card.Add(new ProgressBar() { value = level * 10f, highValue = 100, style = { height = 5, marginTop = 8 } });

            var btnRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 10, justifyContent = Justify.SpaceBetween } };
            var upgradeBtn = new Button(() =>
            {
                if (StateCraft.Systems.EconomyLogicEvaluator.TryUpgradeIndustry(country, ind, out string msg))
                    NotificationManager.Instance?.AddNotification(msg, Color.cyan);
                else
                    NotificationManager.Instance?.AddNotification(msg, Color.red);
                RefreshDisplay();
            }) { text = $"UPGRADE · {UiFormat.Money(cost, sym)}" };
            upgradeBtn.AddToClassList("button-premium");
            upgradeBtn.style.height = 26;
            upgradeBtn.style.fontSize = 8;
            upgradeBtn.style.flexGrow = 1;
            btnRow.Add(upgradeBtn);
            card.Add(btnRow);

            _industryCardsContainer.Add(card);
        }

        private void RefreshIndustryTab(CountryDataSO country, string sym)
        {
            if (_root == null) return;
            var db = GameManager.Instance?.EconomyDatabase;
            if (db == null) { Debug.LogWarning("[IndustryUI] RefreshIndustryTab — GameManager.EconomyDatabase is NULL. Assign an EconomyDatabaseSO asset to GameManager.EconomyDatabase for this tab to show real data."); return; }

            // Re-attempt any static refs that were null at startup (panel was display:none then)
            if (_lblIndOverviewOutput == null) _lblIndOverviewOutput = _root.Q<Label>("IndustryOverview_Output");
            if (_lblIndOverviewGDPPct == null) _lblIndOverviewGDPPct = _root.Q<Label>("IndustryOverview_GDPPct");
            if (_lblIndOverviewMeter  == null) _lblIndOverviewMeter  = _root.Q<Label>("IndustryOverview_Meter");
            if (_pbIndOverview        == null) _pbIndOverview        = _root.Q<ProgressBar>("IndustryOverview_Bar");
            if (_lblIndTopName        == null) _lblIndTopName        = _root.Q<Label>("IndustryTop_Name");
            if (_lblIndTopRevenue     == null) _lblIndTopRevenue     = _root.Q<Label>("IndustryTop_Revenue");
            if (_lblIndTopGrowth      == null) _lblIndTopGrowth      = _root.Q<Label>("IndustryTop_Growth");
            if (_lblIndWeakName       == null) _lblIndWeakName       = _root.Q<Label>("IndustryWeak_Name");
            if (_lblIndWeakRevenue    == null) _lblIndWeakRevenue    = _root.Q<Label>("IndustryWeak_Revenue");
            if (_lblIndAdviserName    == null) _lblIndAdviserName    = _root.Q<Label>("IndustryAdviserName");
            if (_lblIndAdviserRec     == null) _lblIndAdviserRec     = _root.Q<Label>("IndustryAdviserRec");
            if (_lblIndAdviserBody    == null) _lblIndAdviserBody    = _root.Q<Label>("IndustryAdviserBody");
            if (_industryAlertsContainer == null) _industryAlertsContainer = _root.Q<VisualElement>("IndustryAlertsContainer");

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
                if (refs.Tier     == null) refs.Tier     = _root.Q<Label>($"Industry{ind.id}_Tier");
                if (refs.Btn      == null) refs.Btn      = _root.Q<Button>($"Industry{ind.id}_Btn");
                if (refs.Stat1    == null) refs.Stat1    = _root.Q<Label>($"Industry{ind.id}_Stat1");
                if (refs.Stat2    == null) refs.Stat2    = _root.Q<Label>($"Industry{ind.id}_Stat2");
                if (refs.Stat3    == null) refs.Stat3    = _root.Q<Label>($"Industry{ind.id}_Stat3");
                if (refs.Stat4    == null) refs.Stat4    = _root.Q<Label>($"Industry{ind.id}_Stat4");
                if (refs.Growth   == null) refs.Growth   = _root.Q<Label>($"Industry{ind.id}_Growth");
                if (refs.Outlook  == null) refs.Outlook  = _root.Q<Label>($"Industry{ind.id}_Outlook");
                if (refs.Pressure == null) refs.Pressure = _root.Q<Label>($"Industry{ind.id}_Pressure");
                if (refs.Benefits == null) refs.Benefits = _root.Q<Label>($"Industry{ind.id}_Benefits");
                if (refs.Ring     == null) refs.Ring     = _root.Q<Label>($"Ring{ind.id}");

                if (refs.Tier != null) refs.Tier.text = $"Tier {level}/10  •  Workforce {workforce:F1}k";
                if (refs.Btn  != null) refs.Btn.text  = $"UPGRADE TO TIER {Mathf.Min(level + 1, 10)}\nCost: {UiFormat.Money(cost, sym)}";
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