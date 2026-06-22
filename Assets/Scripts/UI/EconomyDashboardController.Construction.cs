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
        private void CommissionBlueprint(string blueprintName)
        {
            var country = GameManager.Instance?.playerCountry;
            var db = GameManager.Instance?.EconomyDatabase;
            if (country == null || db == null || string.IsNullOrEmpty(blueprintName)) return;

            foreach (var bp in db.blueprints)
            {
                if (!BlueprintMatches(bp.blueprintName, blueprintName)) continue;

                if (StateCraft.Systems.EconomyLogicEvaluator.TryStartProject(country, bp, out string msg))
                {
                    NotifyEconomy(msg, Color.green);
                    RefreshDisplay();
                }
                else
                {
                    ShowEconomyModal("Commission Blocked", bp.blueprintName, msg, "OK", null);
                }
                return;
            }

            ShowEconomyModal("Blueprint Unavailable", blueprintName, "No matching blueprint exists in database yet.", "OK", null);
        }

        private void ToggleBlueprints()
        {
            _blueprintsVisible = !_blueprintsVisible;
            ApplyBlueprintVisibility();
        }

        private void ShowBlueprintCatalog()
        {
            _blueprintsVisible = true;
            ApplyBlueprintVisibility();
            SwitchTab(4);
        }

        private void ApplyBlueprintVisibility()
        {
            var show = _blueprintsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_blueprintCatalog != null) _blueprintCatalog.style.display = show;
            if (_blueprintSectionTitle != null) _blueprintSectionTitle.style.display = show;
            if (_blueprintsVisible && GameManager.Instance?.playerCountry != null)
                PopulateBlueprintCatalog(GameManager.Instance.playerCountry);
        }

        private void PopulateConstruction(CountryDataSO country, string sym)
        {
            if (_constructionSummary != null)
            {
                _constructionSummary.Clear();
                float monthlySpend = 0f;
                int pipelineCount = 0;
                foreach (var p in country.activeProjects)
                {
                    if (p.stage == ProjectStage.Planned || p.stage == ProjectStage.Funded || p.stage == ProjectStage.UnderConstruction)
                    {
                        pipelineCount++;
                        if (p.totalMonths > 0) monthlySpend += p.totalCost / p.totalMonths;
                    }
                }

                _constructionSummary.Add(new Label($"QUEUE {pipelineCount}/5 · Burn {UiFormat.Money(monthlySpend, sym)}/mo · Treasury {UiFormat.Money(GameManager.Instance.playerBudget, sym)}")
                {
                    style = { color = Color.white, fontSize = 10 }
                });
            }

            if (_constructionCardsContainer == null) return;
            _constructionCardsContainer.Clear();

            int displayedCount = 0;
            foreach (var proj in country.activeProjects)
            {
                if (proj.stage == ProjectStage.Planned || proj.stage == ProjectStage.Funded || proj.stage == ProjectStage.UnderConstruction)
                {
                    AddConstructionProjectRow(country, proj, sym);
                    displayedCount++;
                }
            }

            if (displayedCount == 0)
            {
                _constructionCardsContainer.Add(new Label("No active sites — open blueprints to commission a megaproject.")
                {
                    style = { color = Color.grey, alignSelf = Align.Center, marginTop = 20, fontSize = 10, whiteSpace = WhiteSpace.Normal }
                });
            }
        }

        private void AddConstructionProjectRow(CountryDataSO country, ActiveProject proj, string sym)
        {
            var row = new VisualElement();
            row.AddToClassList("sub-panel");
            row.style.marginBottom = 8;
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.paddingLeft = 14;
            row.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            header.Add(new Label(proj.projectName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(proj.projectType.ToUpper()) { style = { color = new Color(0, 210/255f, 1f), fontSize = 8 } });
            row.Add(header);

            row.Add(new Label($"{proj.regionName} · Target: {proj.targetID}") { style = { color = Color.grey, fontSize = 8, marginTop = 4 } });
            float pct = proj.totalMonths > 0 ? (proj.totalMonths - proj.monthsRemaining) / (float)proj.totalMonths * 100f : 0f;
            row.Add(new Label($"{pct:F0}% complete · {proj.monthsRemaining} mo left · {UiFormat.Money(proj.totalCost / Mathf.Max(1, proj.totalMonths), sym)}/mo")
            {
                style = { color = Color.grey, fontSize = 8, marginTop = 4 }
            });
            row.Add(new ProgressBar()
            {
                value = proj.totalMonths - proj.monthsRemaining,
                highValue = Mathf.Max(1, proj.totalMonths),
                style = { height = 6, marginTop = 8 }
            });

            var cancelBtn = new Button(() =>
            {
                country.activeProjects.Remove(proj);
                NotificationManager.Instance?.AddNotification($"Cancelled: {proj.projectName}", new Color(255/255f, 171/255f, 64/255f));
                RefreshDisplay();
            }) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.height = 24;
            cancelBtn.style.fontSize = 8;
            cancelBtn.style.marginTop = 8;
            cancelBtn.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.15f);
            row.Add(cancelBtn);

            _constructionCardsContainer.Add(row);
        }

        private void PopulateBlueprintCatalog(CountryDataSO country)
        {
            if (_blueprintCatalog == null) return;
            _blueprintCatalog.Clear();

            var db = GameManager.Instance?.EconomyDatabase;
            if (db == null) return;

            foreach (var bp in db.blueprints)
            {
                var card = new VisualElement();
                card.AddToClassList("sub-panel");
                card.style.width = Length.Percent(48);
                card.style.marginBottom = 10;
                card.style.paddingTop = 12;
                card.style.paddingBottom = 12;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;

                card.Add(new Label(bp.blueprintName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
                card.Add(new Label($"{bp.type} · {bp.months} mo · {bp.totalCostB:N0}B total") { style = { color = new Color(0, 210/255f, 1f), fontSize = 8, marginTop = 4 } });
                card.Add(new Label(bp.description) { style = { color = Color.grey, fontSize = 8, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

                if (bp.targetId == "PowerNuclear" && !country.isImportingUranium && !country.resources.Exists(r => r.type == MineralType.Uranium))
                    card.Add(new Label("Requires domestic uranium or import deal") { style = { color = new Color(255/255f, 171/255f, 64/255f), fontSize = 7, marginTop = 4 } });

                var bpCopy = bp;
                var startBtn = new Button(() =>
                {
                    if (StateCraft.Systems.EconomyLogicEvaluator.TryStartProject(country, bpCopy, out string msg))
                        NotificationManager.Instance?.AddNotification(msg, Color.green);
                    else
                        NotificationManager.Instance?.AddNotification(msg, Color.red);
                    RefreshDisplay();
                }) { text = "COMMISSION" };
                startBtn.AddToClassList("button-premium");
                startBtn.style.height = 24;
                startBtn.style.fontSize = 8;
                startBtn.style.marginTop = 8;
                card.Add(startBtn);

                _blueprintCatalog.Add(card);
            }
        }

        private void RefreshProjectsTab(CountryDataSO country, string sym)
        {
            var grid = _root?.Q<ScrollView>("ProjectsActiveGrid");
            if (grid == null) return;
            grid.Clear();
            
            float monthlySpend = 0f;
            int pipelineCount = 0;
            float totalMonths = 0f;
            float totalCost = 0f;
            int jobsCreated = 0;
            float gdpBoost = 0f;

            foreach (var p in country.activeProjects)
            {
                if (p.stage == ProjectStage.Planned || p.stage == ProjectStage.Funded || p.stage == ProjectStage.UnderConstruction)
                {
                    pipelineCount++;
                    if (p.totalMonths > 0) monthlySpend += p.totalCost / p.totalMonths;
                    totalMonths += p.monthsRemaining;
                    totalCost += p.totalCost;
                    if (p.projectType != null) {
                        jobsCreated += p.projectType.ToLower().Contains("infra") ? 20000 : 10000;
                        gdpBoost += p.projectType.ToLower().Contains("infra") ? 0.8f : 0.4f;
                    }

                    AddConstructionProjectCard(grid, country, p, sym);
                }
            }

            if (_root.Q<Label>("ProjectsKPI_Count") is Label lK1) lK1.text = pipelineCount.ToString();
            if (_root.Q<Label>("ProjectsKPI_MonthlyCost") is Label lK2) lK2.text = UiFormat.Money(monthlySpend, sym);
            if (_root.Q<Label>("ProjectsKPI_GDPBoost") is Label lK3) lK3.text = $"+{gdpBoost:F1}%";
            if (_root.Q<Label>("ProjectsKPI_Jobs") is Label lK4) lK4.text = $"{jobsCreated:N0}";
            if (_root.Q<Label>("ProjectsKPI_AvgCompletion") is Label lK5) lK5.text = $"{(pipelineCount>0?totalMonths/pipelineCount:0f):F1} Months";

            if (_root.Q<Label>("ProjectsActiveHeading") is Label lAH) lAH.text = $"ACTIVE PROJECTS ({pipelineCount})";

            if (_constructionSummary != null)
            {
                _constructionSummary.Clear();
                _constructionSummary.Add(new Label($"QUEUE {pipelineCount}/5 • Burn {UiFormat.Money(monthlySpend, sym)}/mo • Treasury {UiFormat.Money(GameManager.Instance?.playerBudget ?? 0, sym)}")
                {
                    style = { color = Color.white, fontSize = 10 }
                });
            }

            if (_currentAdviserRec != null) {
                if (_root.Q<Label>("ProjectsAdviserBody") is Label lAdv) lAdv.text = _currentAdviserRec.Value.Rationale;
            }
        }

        private void AddConstructionProjectCard(VisualElement container, ActiveProject p, string sym) {
            AddConstructionProjectCard(container, GameManager.Instance?.playerCountry, p, sym);
        }

        private void AddConstructionProjectCard(VisualElement container, CountryDataSO country, ActiveProject p, string sym)
        {
            var card = new VisualElement();
            card.AddToClassList("project-card");

            float pct = p.totalMonths > 0 ? (p.totalMonths - p.monthsRemaining) / (float)p.totalMonths * 100f : 0f;
            float monthlyVal = p.totalMonths > 0 ? p.totalCost / p.totalMonths : p.totalCost;
            float spent = p.totalCost - (monthlyVal * p.monthsRemaining);

            var img = new VisualElement();
            img.AddToClassList("project-image");
            if (p.projectType.ToString().Contains("Infra")) img.AddToClassList("port");
            card.Add(img);

            card.Add(new Label(p.projectName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
            card.Add(new Label($"{p.projectType.ToString().ToUpper()}") { style = { color = new Color(0, 210/255f, 1f), fontSize = 8, marginBottom = 8 } });

            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(new ProgressBar() { value = pct, highValue = 100, style = { flexGrow = 1, height = 4 } });
            row.Add(new Label($"{pct:F0}%") { style = { color = Color.white, fontSize = 9, marginLeft = 6 } });
            card.Add(row);

            card.Add(new Label($"Total Cost: {UiFormat.Money(p.totalCost, sym)}\nSpent: {UiFormat.Money(spent, sym)}\nMonthly: {UiFormat.Money(monthlyVal, sym)}\nLeft: {p.monthsRemaining} Months") { style = { color = Color.grey, fontSize = 8, marginTop = 8, whiteSpace = WhiteSpace.Normal } });

            
            card.RegisterCallback<ClickEvent>(evt => {
                if (_root.Q<Label>("SideProject_Title") is Label tLabel) tLabel.text = p.projectName.ToUpper();
                if (_root.Q<Label>("SideProject_Overview") is Label oLabel) oLabel.text = string.IsNullOrEmpty(p.description) ? "National strategic project driving growth across key economic sectors." : p.description;
                if (_root.Q<VisualElement>("SideProject_Info") is VisualElement infoView) {
                    infoView.Clear();
                    infoView.Add(new Label($"Category                         {p.projectType}") { style = { whiteSpace = WhiteSpace.NoWrap, marginBottom = 5 } });
                    if (!string.IsNullOrEmpty(p.regionName)) infoView.Add(new Label($"Location                         {p.regionName}") { style = { whiteSpace = WhiteSpace.NoWrap, marginBottom = 5 } });
                    infoView.Add(new Label($"Expected Completion              {p.monthsRemaining} Months") { style = { whiteSpace = WhiteSpace.NoWrap, marginBottom = 5 } });
                }
                if (_root.Q<VisualElement>("SideProject_Benefits") is VisualElement benefitsView) {
                    benefitsView.Clear();
                    float gBoost = (p.projectType != null && p.projectType.ToLower().Contains("infra")) ? 0.8f : 0.4f;
                    int jC = (p.projectType != null && p.projectType.ToLower().Contains("infra")) ? 20000 : 10000;
                    var lBoost = new Label($"GDP Growth                       +{gBoost:F1}%");
                    lBoost.AddToClassList("positive"); lBoost.style.marginBottom = 5;
                    benefitsView.Add(lBoost);
                    
                    var lJobs = new Label($"Jobs Created                     +{jC:N0}");
                    lJobs.AddToClassList("positive"); lJobs.style.marginBottom = 5;
                    benefitsView.Add(lJobs);
                }
            });

            container.Add(card);
        }

    }
}