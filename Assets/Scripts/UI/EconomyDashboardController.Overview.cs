using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;
using System.Linq;

namespace StateCraft.UI
{
    public partial class EconomyDashboardController
    {
        private void ApproveFuelSubsidyPolicy()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            country.subsidyLevel   = Mathf.Max(0f, country.subsidyLevel   - 1f);
            country.publicApproval = Mathf.Max(0f, country.publicApproval - 1f);
            country.inflationRate += 0.001f;
            if (_sliderSubsidyLevel != null) _sliderSubsidyLevel.SetValueWithoutNotify(country.subsidyLevel);
            if (_labelSubsidyLevel  != null) _labelSubsidyLevel.text = $"{country.subsidyLevel:F0}%";

            NotifyEconomy("Fuel subsidy reduction approved.", Color.green);
            RefreshDisplay();
        }

        private void UpdateOverviewAdviserCard(CountryDataSO country)
        {
            if (country == null) return;

            var rec = GetCurrentAdviserRecommendation(country);
            bool hasRecommendation = rec.HasValue;

            if (!hasRecommendation)
            {
                if (_overviewAdviserName != null) _overviewAdviserName.text = AdviserEngine.GetEconomicAdviserName(country);
                if (_overviewAdviserTitle != null) _overviewAdviserTitle.text = "No active recommendation";
                if (_overviewAdviserBody != null) _overviewAdviserBody.text = "All indicators are stable. The adviser will return when conditions change.";
                if (_overviewAdviserBenefit1 != null) _overviewAdviserBenefit1.text = " ";
                if (_overviewAdviserBenefit2 != null) _overviewAdviserBenefit2.text = " ";
                if (_overviewAdviserBenefit3 != null) _overviewAdviserBenefit3.text = " ";
                if (_overviewAdviserRisk1 != null) _overviewAdviserRisk1.text = " ";
                if (_overviewAdviserRisk2 != null) _overviewAdviserRisk2.text = " ";
                if (_overviewAdviserRisk3 != null) _overviewAdviserRisk3.text = " ";
            }
            else
            {
                var current = rec.Value;
                if (_overviewAdviserName != null) _overviewAdviserName.text = AdviserEngine.GetEconomicAdviserName(country);
                if (_overviewAdviserTitle != null) _overviewAdviserTitle.text = current.Title;
                if (_overviewAdviserBody != null) _overviewAdviserBody.text = current.Rationale;

                string[] benefits = SplitLines(current.Benefits, 3);
                string[] risks = SplitLines(current.Risks, 3);
                if (_overviewAdviserBenefit1 != null) _overviewAdviserBenefit1.text = benefits[0];
                if (_overviewAdviserBenefit2 != null) _overviewAdviserBenefit2.text = benefits[1];
                if (_overviewAdviserBenefit3 != null) _overviewAdviserBenefit3.text = benefits[2];
                if (_overviewAdviserRisk1 != null) _overviewAdviserRisk1.text = risks[0];
                if (_overviewAdviserRisk2 != null) _overviewAdviserRisk2.text = risks[1];
                if (_overviewAdviserRisk3 != null) _overviewAdviserRisk3.text = risks[2];
            }

            DisplayStyle recommendationDisplay = hasRecommendation ? DisplayStyle.Flex : DisplayStyle.None;
            if (_overviewAdviserImpactGrid != null) _overviewAdviserImpactGrid.style.display = recommendationDisplay;
            if (_overviewAdviserActionRow != null) _overviewAdviserActionRow.style.display = recommendationDisplay;
            if (_overviewApproveButton != null) _overviewApproveButton.SetEnabled(hasRecommendation);
            if (_overviewDeclineButton != null) _overviewDeclineButton.SetEnabled(hasRecommendation);
        }

        private void PopulateLedger(CountryDataSO country, string sym, GameManager gm)
        {
            if (_incomeList == null || _expenseList == null) return;
            _incomeList.Clear();
            _expenseList.Clear();

            float totalIncome = 0f;
            float totalExpense = 0f;

            // All base estimates (GDP, income, debt, spending) are stored in Billions of USD.
            // We multiply by exchangeRate to get Billions of Local Currency, ready for AddLedgerRow.

            float monthlyGDP = country.gdp / 12f;
            float taxEstUSD = monthlyGDP * (country.taxRate / 100f) * 0.35f;
            float taxEstB = taxEstUSD * country.exchangeRate;
            
            float tariffEstUSD = monthlyGDP * 0.015f * (country.importTariffRate / 100f);
            float tariffEstB = tariffEstUSD * country.exchangeRate;
            
            // Extract approximate resource dividend from income formula, backing out tax and tariff
            float resourceEstUSD = Mathf.Max(0f, country.income - taxEstUSD - tariffEstUSD);
            float resourceEstB = resourceEstUSD * country.exchangeRate;

            totalIncome += taxEstB;
            totalIncome += resourceEstB;
            AddLedgerRow(_incomeList, "Tax Revenue", taxEstB, true, sym);
            AddLedgerRow(_incomeList, "Resource Dividends", resourceEstB, true, sym);
            
            if (country.importTariffRate > 0)
            {
                totalIncome += tariffEstB;
                AddLedgerRow(_incomeList, "Import Tariff Yield", tariffEstB, true, sym);
            }
            
            if (country.foreignAidReceived > 0f)
            {
                // foreignAidReceived is mostly stored in Billions in CountryDataSO as well
                float aidB = country.foreignAidReceived * country.exchangeRate;
                totalIncome += aidB;
                AddLedgerRow(_incomeList, "Foreign Aid", aidB, true, sym);
            }

            if (country.activeTradeAgreements != null)
            {
                // Group duplicates (e.g. multiple deals of the same resource to the same country)
                var activeDeals = country.activeTradeAgreements
                    .Where(a => a.status == TradeAgreementStatus.Active)
                    .GroupBy(a => new { a.isSeller, a.resource, a.partnerCountry });

                foreach (var group in activeDeals)
                {
                    float totalValueUSD = group.Sum(a => a.monthlyPaymentUSD);
                    float valueInBillions = (totalValueUSD * country.exchangeRate) / 1e9f;
                    
                    if (group.Key.isSeller)
                    {
                        totalIncome += valueInBillions;
                        AddLedgerRow(_incomeList, $"Agrmt: {group.Key.resource} (to {group.Key.partnerCountry})", valueInBillions, true, sym);
                    }
                    else
                    {
                        totalExpense += valueInBillions;
                        AddLedgerRow(_expenseList, $"Agrmt: {group.Key.resource} (from {group.Key.partnerCountry})", valueInBillions, false, sym);
                    }
                }
            }

            // Military budget is directly in Billions USD
            float militaryB = country.militaryBudgetUSD * country.exchangeRate;
            totalExpense += militaryB;
            AddLedgerRow(_expenseList, "Military Budget", militaryB, false, sym);

            if (country.activeOperations != null && country.activeOperations.Count > 0)
            {
                float totalOpCostUSD = 0f;
                foreach (var op in country.activeOperations)
                {
                    totalOpCostUSD += (op.costPerMonth > 0f) ? (op.costPerMonth / 1000f) : 0.05f;
                }
                float totalOpCostLocal = totalOpCostUSD * country.exchangeRate;
                totalExpense += totalOpCostLocal;
                AddLedgerRow(_expenseList, "Active Operations", totalOpCostLocal, false, sym);
            }

            // Welfare Subsidies: country.gdp is in Billions. 
            float subsidyCostUSD = (country.gdp / 12f) * (country.subsidyLevel / 100f);
            float subsidyCostB = subsidyCostUSD * country.exchangeRate;
            totalExpense += subsidyCostB;
            AddLedgerRow(_expenseList, "Welfare Subsidies", subsidyCostB, false, sym);

            if (country.isImportingUranium)
            {
                // Uranium imports is 0.05f Billions of USD.
                float uraniumCostB = 0.05f * country.exchangeRate;
                totalExpense += uraniumCostB;
                AddLedgerRow(_expenseList, "Uranium Imports", uraniumCostB, false, sym);
            }

            if (gm.DebtSystem != null && country.debt > 0f)
            {
                float interestUSD = gm.DebtSystem.CalculateInterest(country); // returns Billions
                float principalPaymentUSD = (country.debt * 0.01f) * (country.debtServiceRatio / 100f); // returns Billions
                float totalMonthlyUSD = interestUSD + principalPaymentUSD;
                
                float totalDebtCostB = totalMonthlyUSD * country.exchangeRate;
                totalExpense += totalDebtCostB;
                AddLedgerRow(_expenseList, "Central Bank Debt Servicing", totalDebtCostB, false, sym);
            }

            // Update Total Mirror Labels
            var root = _dashboardRoot;
            if (root != null)
            {
                var incMirror = root.Q<Label>("HeaderIncomeMirror");
                if (incMirror != null) incMirror.text = "+" + UiFormat.Money(totalIncome, sym);
                
                var expMirror = root.Q<Label>("HeaderExpenseMirror");
                if (expMirror != null) expMirror.text = "-" + UiFormat.Money(totalExpense, sym);
            }
        }

        private void AddLedgerRow(VisualElement container, string label, float value, bool isPositive, string sym)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 5;

            var name = new Label(label) { style = { color = Color.white, fontSize = 10 } };
            float signed = isPositive ? value : -value;
            var val = new Label(UiFormat.MoneySigned(signed, sym)) { style = { color = isPositive ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f), fontSize = 10 } };
            
            row.Add(name);
            row.Add(val);
            container.Add(row);
        }

        private void RefreshOverviewTab(CountryDataSO country, string sym, GameManager gm)
        {
            RefreshOverviewReports(country, gm);
            RefreshOverviewSectors(country, sym);
            RefreshOverviewProjects(country, sym);
            RefreshDebtFinancing(country, sym);
        }

        /// <summary>
        /// Replaces the four static Economic Reports rows with live FeedLines
        /// from BuildEconomyFeed. The panel header is preserved.
        /// Same CSS classes — design unchanged.
        /// </summary>
        private void RefreshOverviewReports(CountryDataSO country, GameManager gm)
        {
            if (_econReportsPanel == null) return;

            // Keep only the header (index 0), remove all report rows below it
            while (_econReportsPanel.childCount > 1)
                _econReportsPanel.RemoveAt(1);

            var feed = DashboardLiveContent.BuildEconomyFeed(country, gm);
            int max = Mathf.Min(feed.Count, 4);

            for (int i = 0; i < max; i++)
            {
                var line = feed[i];

                // Decide row class from Color (matches UXML classes: alert/good/warning/market)
                string rowClass = ColorToReportClass(line.Color);

                // Icon glyph by class — using Font Awesome 6 Solid codepoints
                string icon = rowClass == "alert"  ? FaIcons.Fire
                            : rowClass == "good"   ? FaIcons.ArrowTrendUp
                            : rowClass == "market" ? FaIcons.Globe
                            :                       FaIcons.Scroll;

                string iconColor = rowClass == "alert"  ? "fa-red"
                                 : rowClass == "good"   ? "fa-green"
                                 : rowClass == "market" ? "fa-cyan"
                                 :                       "fa-gold";

                // Category label class
                string catClass = rowClass == "alert" ? "negative" : (rowClass == "good" ? "positive" : (rowClass == "market" ? "market" : "warning"));

                // Category display name
                string catLabel = rowClass == "alert" ? "ECONOMIC ALERT"
                                : rowClass == "good"  ? "POSITIVE DEVELOPMENT"
                                : rowClass == "market"? "GLOBAL MARKET UPDATE"
                                :                       "POLICY UPDATE";

                string stamp = DashboardLiveContent.TurnStamp(gm);

                var row = new VisualElement();
                row.AddToClassList("econ-report");
                row.AddToClassList(rowClass);

                var iconLbl = new Label(icon);
                iconLbl.AddToClassList("econ-report-icon");
                iconLbl.AddToClassList("fa-icon");
                iconLbl.AddToClassList(iconColor);
                row.Add(iconLbl);

                var body = new VisualElement();
                body.AddToClassList("econ-report-body");

                var catLbl = new Label(catLabel);
                catLbl.AddToClassList(catClass);
                body.Add(catLbl);

                var titleLbl = new Label(line.Tag);
                titleLbl.AddToClassList("econ-report-title");
                body.Add(titleLbl);

                var copyLbl = new Label(line.Message);
                copyLbl.AddToClassList("econ-report-copy");
                body.Add(copyLbl);

                row.Add(body);

                var timeLbl = new Label(stamp);
                timeLbl.AddToClassList("econ-muted");
                row.Add(timeLbl);

                _econReportsPanel.Add(row);
            }
        }

        private static string ColorToReportClass(Color c)
        {
            // Green → good, Red → alert, Yellow/amber → warning, everything else → market
            if (c.r < 0.2f && c.g > 0.7f) return "good";
            if (c.r > 0.7f && c.g < 0.5f) return "alert";
            if (c.r > 0.7f && c.g > 0.6f) return "warning";
            return "market";
        }

        /// <summary>
        /// Replaces the four static sector cards with live data from Industries[].
        /// Same CSS classes — design unchanged.
        /// </summary>
        private void RefreshOverviewSectors(CountryDataSO country, string sym)
        {
            if (_econSectorsGrid == null) return;
            var db = GameManager.Instance?.EconomyDatabase;
            if (db == null) return;

            _econSectorsGrid.Clear();

            float totalRev = 0f;
            foreach (var ind in db.industries)
                totalRev += StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, ind);

            // The grid design supports exactly 4 cards; taking the top 4 by revenue prevents layout crushing
            var topSectors = new System.Collections.Generic.List<StateCraft.Data.Economy.IndustryDataSO>(db.industries);
            topSectors.Sort((a, b) => StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, b).CompareTo(StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, a)));
            int maxSectors = UnityEngine.Mathf.Min(topSectors.Count, 4);

            for (int i = 0; i < maxSectors; i++)
            {
                var ind = topSectors[i];
                float rev = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryRevenue(country, ind);
                float contrib = totalRev > 0 ? (rev / totalRev) * 100f : 0f;
                float workforce = StateCraft.Systems.EconomyLogicEvaluator.GetIndustryWorkforce(country, ind.id);
                bool bullish = StateCraft.Systems.EconomyLogicEvaluator.IsIndustryBullish(country, ind.id);
                string workforceStr = workforce >= 1_000_000f
                    ? $"{workforce / 1_000_000f:F1}M"
                    : workforce >= 1000f
                        ? $"{workforce / 1000f:F1}k"
                        : $"{workforce:F0}";

                // Map sorted sector back to original index for consistent icon assignment
                int origIdx = db.industries.IndexOf(ind);
                string faIcon = (origIdx >= 0 && origIdx < FaIcons.SectorIcons.Length)
                    ? FaIcons.SectorIcons[origIdx]
                    : FaIcons.BullsEye;
                string safeTitle = ind.title.Replace(" & MINERALS", "").Replace(" & FINTECH","").Replace(" & GAS", "").ToUpper();

                var card = new VisualElement();
                card.AddToClassList("econ-sector-card");
                // Right column cards (i=1,3) need no right margin — USS nth-child not supported in Unity
                if (i % 2 == 1) card.style.marginRight = 0;

                // Title row: FA icon + sector name
                var titleRow = new VisualElement();
                titleRow.style.flexDirection = FlexDirection.Row;
                titleRow.style.alignItems = Align.Center;
                titleRow.style.marginBottom = 6;

                var faLbl = new Label(faIcon);
                faLbl.AddToClassList("fa-icon");
                faLbl.AddToClassList("fa-cyan");
                faLbl.AddToClassList("fa-sm");
                faLbl.style.marginRight = 5;
                titleRow.Add(faLbl);

                var titleLbl = new Label(safeTitle);
                titleLbl.AddToClassList("econ-sector-title");
                titleLbl.style.marginBottom = 0; // already handled by titleRow marginBottom
                titleRow.Add(titleLbl);
                card.Add(titleRow);

                // Real growth calculation tied to the national GDP growth rate (YoY) + sector-specific modifiers
                float baseGrowthYoY = country.gdpGrowthRate * 12f * 100f;
                float realGrowth = baseGrowthYoY + (bullish ? 1.5f : -0.8f);
                var contribLbl = new Label($"Contribution  {contrib:F0}%");
                contribLbl.style.marginTop = 2;
                card.Add(contribLbl);

                var trendIcon = new Label(bullish ? FaIcons.TrendUp : FaIcons.TrendDown);
                trendIcon.AddToClassList("fa-icon");
                trendIcon.AddToClassList("fa-sm");
                trendIcon.AddToClassList(bullish ? "fa-green" : "fa-red");
                trendIcon.style.marginRight = 4;

                var growthRow = new VisualElement();
                growthRow.style.flexDirection = FlexDirection.Row;
                growthRow.style.alignItems = Align.Center;
                growthRow.Add(trendIcon);
                var growthLbl = new Label(bullish ? $"+{realGrowth:F1}%" : $"{realGrowth:F1}%");
                growthLbl.AddToClassList(bullish ? "positive" : "negative");
                growthLbl.style.fontSize = 11;
                growthRow.Add(growthLbl);
                card.Add(growthRow);

                var empLbl = new Label($"Emp: {workforceStr}");
                empLbl.AddToClassList("econ-muted");
                card.Add(empLbl);

                _econSectorsGrid.Add(card);
            }
        }

        /// <summary>
        /// Replaces the three static project rows with live activeProjects data.
        /// Falls back to hiding the section if no active projects exist.
        /// Same CSS classes — design unchanged.
        /// </summary>
        private void RefreshOverviewProjects(CountryDataSO country, string sym)
        {
            if (_overviewProjectsPanel == null) return;

            // Keep only the panel header (index 0)
            while (_overviewProjectsPanel.childCount > 1)
                _overviewProjectsPanel.RemoveAt(1);

            var active = new System.Collections.Generic.List<ActiveProject>();
            foreach (var p in country.activeProjects)
            {
                if (p.stage == ProjectStage.UnderConstruction ||
                    p.stage == ProjectStage.Funded ||
                    p.stage == ProjectStage.Planned)
                {
                    active.Add(p);
                    if (active.Count >= 3) break;
                }
            }

            if (active.Count == 0)
            {
                _overviewProjectsPanel.style.display = DisplayStyle.None;
                return;
            }
            _overviewProjectsPanel.style.display = DisplayStyle.Flex;

            foreach (var p in active)
            {
                float pct = p.totalMonths > 0
                    ? (p.totalMonths - p.monthsRemaining) / (float)p.totalMonths * 100f
                    : 0f;
                float monthlyCost = p.totalMonths > 0 ? p.totalCost / p.totalMonths : p.totalCost;

                // CSS class for the progress bar (match original "econ-progress" / "econ-progress warning-bar")
                string barClass = pct < 40f ? "econ-progress warning-bar" : "econ-progress";

                // Thumb class derived from project type string
                string thumbClass = p.projectType?.ToLower().Contains("tech") == true ? "tech"
                                  : p.projectType?.ToLower().Contains("rail") == true ? "rail"
                                  : "";

                var row = new VisualElement();
                row.AddToClassList("econ-project-row");

                var thumb = new VisualElement();
                thumb.AddToClassList("econ-project-thumb");
                if (!string.IsNullOrEmpty(thumbClass)) thumb.AddToClassList(thumbClass);
                row.Add(thumb);

                var info = new VisualElement();
                info.AddToClassList("econ-project-info");

                var nameLbl = new Label(p.projectName);
                nameLbl.AddToClassList("econ-report-title");
                info.Add(nameLbl);

                string typeDisplay = p.projectType;
                if (!string.IsNullOrEmpty(typeDisplay)) {
                    if (typeDisplay.ToLower().Contains("infra")) typeDisplay = "Infrastructure";
                    if (typeDisplay.ToLower().Contains("tech")) typeDisplay = "Technology";
                }

                var typeLbl = new Label(typeDisplay ?? "Project");
                typeLbl.AddToClassList("econ-muted");
                info.Add(typeLbl);

                var bar = new ProgressBar { value = pct, highValue = 100 };
                bar.AddToClassList("econ-progress");
                if (pct < 40f) bar.AddToClassList("warning-bar");
                info.Add(bar);

                row.Add(info);

                var costLbl = new Label($"{UiFormat.Money(monthlyCost, sym)}\n{p.monthsRemaining} mo left");
                costLbl.style.color = UnityEngine.Color.white;
                costLbl.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
                row.Add(costLbl);

                _overviewProjectsPanel.Add(row);
            }
        }

    }
}