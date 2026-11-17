
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Data.Economy;
using StateCraft.Systems;

namespace StateCraft.UI
{
    public partial class EconomyDashboardController
    {

        private string _blueprintFilter = "ALL";

        private VisualElement _liveBlueprintGrid;

        private VisualElement _blueprintTabBar;

        private bool _constructionTabWired = false;

        private void SyncProjectsSidePanel()
        {
            if (_panelConstruction == null) return;
            var wrapper = _panelConstruction.Q<VisualElement>("ProjectsSideWrapper");
            if (wrapper == null) return;

            var projCard = _panelConstruction.Q<VisualElement>("SelectedProjectCard");
            var advCard  = _panelConstruction.Q<VisualElement>("ProjectsAdviserCard");

            bool showProj = projCard != null && projCard.style.display == DisplayStyle.Flex;
            bool showAdv  = advCard != null && advCard.style.display == DisplayStyle.Flex;

            wrapper.style.display = (showProj || showAdv) ? DisplayStyle.Flex : DisplayStyle.None;
        }

internal void InitConstructionTab()
        {
            if (_panelConstruction == null) return;

            _liveBlueprintGrid = _panelConstruction.Q<VisualElement>("LiveBlueprintGrid");

            if (_constructionTabWired) return;
            _constructionTabWired = true;

            _blueprintTabBar = _panelConstruction.Q<VisualElement>("BlueprintFilterTabs");
            if (_blueprintTabBar != null)
            {
                foreach (var tab in _blueprintTabBar.Children().OfType<Label>())
                {
                    var captured = tab;
                    captured.RegisterCallback<ClickEvent>(_ => SetBlueprintFilter(captured.text));
                }
            }

            var closeBtn = _panelConstruction.Q<Button>(className: "projects-close-btn");
            if (closeBtn != null)
            {
                closeBtn.clicked += () =>
                {
                    var sideCard = _panelConstruction.Q<VisualElement>("SelectedProjectCard");
                    if (sideCard != null) sideCard.style.display = DisplayStyle.None;
                    SyncProjectsSidePanel();
                };
            }
        }

private void RefreshProjectsTab(CountryDataSO country, string sym)
        {
            if (country == null || _panelConstruction == null) return;

            float monthlySpend   = 0f;
            int   pipelineCount  = 0;
            float gdpBoost       = 0f;
            int   jobsCreated    = 0;
            float totalMonthsSum = 0f;

            foreach (var p in country.activeProjects)
            {
                if (!IsActivePipelineStage(p.stage)) continue;
                pipelineCount++;
                if (p.totalMonths > 0) monthlySpend += p.totalCost / p.totalMonths;
                totalMonthsSum += p.monthsRemaining;
                bool isInfra = IsInfraType(p.projectType);
                gdpBoost    += isInfra ? 0.8f : 0.4f;
                jobsCreated += isInfra ? 20000 : 10000;
            }

            float avgCompletion = pipelineCount > 0 ? totalMonthsSum / pipelineCount : 0f;

            PanelSetLabel("ProjectsKPI_Count",         pipelineCount.ToString());
            PanelSetLabel("ProjectsKPI_MonthlyCost",   UiFormat.Money(monthlySpend, sym));
            PanelSetLabel("ProjectsKPI_GDPBoost",      $"+{gdpBoost:F1}%");
            PanelSetLabel("ProjectsKPI_Jobs",          $"{jobsCreated:N0}");
            PanelSetLabel("ProjectsKPI_AvgCompletion", $"{avgCompletion:F1} Months");
            PanelSetLabel("ProjectsActiveHeading",     $"ACTIVE PROJECTS ({pipelineCount})");

            var advCard = _panelConstruction?.Q<VisualElement>("ProjectsAdviserCard");
            if (_currentAdviserRec.HasValue)
            {
                PanelSetLabel("ProjectsAdviserBody", _currentAdviserRec.Value.Rationale);
                if (advCard != null) advCard.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (advCard != null) advCard.style.display = DisplayStyle.None;
            }
            SyncProjectsSidePanel();

            if (_constructionSummary != null)
            {
                _constructionSummary.Clear();
                _constructionSummary.Add(new Label(
                    $"QUEUE {pipelineCount}/5 · Burn {UiFormat.Money(monthlySpend, sym)}/mo · Treasury {UiFormat.Money(GameManager.Instance?.playerBudget ?? 0f, sym)}")
                    { style = { color = Color.white, fontSize = 10 } });
            }

            var grid = _panelConstruction.Q<ScrollView>("ProjectsActiveGrid");
            Debug.Log($"[Construction] ProjectsActiveGrid found: {grid != null} | activeProjects: {country.activeProjects?.Count ?? -1}");

            if (grid != null)
            {
                grid.Clear();
                int cardCount = 0;
                foreach (var p in country.activeProjects)
                {
                    Debug.Log($"[Construction] Project '{p.projectName}' stage={p.stage} active={IsActivePipelineStage(p.stage)}");
                    if (!IsActivePipelineStage(p.stage)) continue;
                    AddConstructionProjectCard(grid, country, p, sym);
                    cardCount++;
                }
                Debug.Log($"[Construction] Cards built: {cardCount}");

                if (cardCount == 0)
                {
                    grid.Add(new Label("No active sites — commission a blueprint below to begin construction.")
                    {
                        style = { color = Color.grey, alignSelf = Align.Center, marginTop = 20,
                                  fontSize = 10, whiteSpace = WhiteSpace.Normal }
                    });
                }
            }

            RebuildBlueprintGrid(country, sym);
        }

private void AddConstructionProjectCard(VisualElement container, ActiveProject p, string sym)
            => AddConstructionProjectCard(container, GameManager.Instance?.playerCountry, p, sym);

        private void AddConstructionProjectCard(VisualElement container, CountryDataSO country, ActiveProject p, string sym)
        {
            var db  = GameManager.Instance?.EconomyDatabase;
            var bp  = db?.GetBlueprint(p.projectName);

            float pct     = p.totalMonths > 0 ? (p.totalMonths - p.monthsRemaining) / (float)p.totalMonths * 100f : 0f;
            float monthly = p.totalMonths > 0 ? p.totalCost / p.totalMonths : p.totalCost;
            float spent   = p.totalCost - (monthly * p.monthsRemaining);
            bool  isInfra = IsInfraType(p.projectType);
            float gdpEst  = isInfra ? 0.8f : 0.4f;
            int   jobsEst = isInfra ? 20000 : 10000;
            string desc   = !string.IsNullOrEmpty(p.description) ? p.description
                            : (bp?.description ?? "National strategic project.");

            string stageBadge = p.stage switch
            {
                ProjectStage.Planned           => "PLANNED",
                ProjectStage.Funded            => "FUNDED",
                ProjectStage.UnderConstruction => "UNDER CONSTRUCTION",
                _                              => p.stage.ToString().ToUpper()
            };

            var card = new VisualElement();
            card.AddToClassList("project-card");

            var img = new VisualElement();
            img.AddToClassList("project-image");
            img.AddToClassList(GetImageClass(p.projectName, p.projectType));
            card.Add(img);

            card.Add(new Label(p.projectName)
                { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
            card.Add(new Label(p.projectType?.ToUpper() ?? "PROJECT")
                { style = { color = new Color(0f, 210f/255f, 1f), fontSize = 8, marginBottom = 8 } });

            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(new ProgressBar() { value = pct, highValue = 100, style = { flexGrow = 1, height = 4 } });
            row.Add(new Label($"{pct:F0}%") { style = { color = Color.white, fontSize = 9, marginLeft = 6 } });
            card.Add(row);

            card.Add(new Label(
                $"Total: {UiFormat.Money(p.totalCost, sym)}  ·  Spent: {UiFormat.Money(spent, sym)}\n" +
                $"Monthly: {UiFormat.Money(monthly, sym)}  ·  {p.monthsRemaining} months left")
                { style = { color = Color.grey, fontSize = 8, marginTop = 8, whiteSpace = WhiteSpace.Normal } });

            card.Add(new Label(stageBadge)
                { style = { color = new Color(255f/255f, 171f/255f, 64f/255f), fontSize = 7, marginTop = 4 } });

            var capturedP    = p;
            var capturedSym  = sym;
            var capturedDesc = desc;
            var capturedGdp  = gdpEst;
            var capturedJobs = jobsEst;
            var capturedBp   = bp;

            card.RegisterCallback<ClickEvent>(_ =>
            {
                var sideCard = _panelConstruction?.Q<VisualElement>("SelectedProjectCard");
                if (sideCard != null) sideCard.style.display = DisplayStyle.Flex;
                SyncProjectsSidePanel();

                PanelSetLabel("SideProject_Title",    capturedP.projectName.ToUpper());
                PanelSetLabel("SideProject_Overview", capturedDesc);

                if (_panelConstruction?.Q<VisualElement>("SideProject_Info") is VisualElement info)
                {
                    info.Clear();
                    AddInfoRow(info, "Category",            capturedP.projectType ?? "—");
                    if (!string.IsNullOrEmpty(capturedP.regionName))
                        AddInfoRow(info, "Location",        capturedP.regionName);
                    AddInfoRow(info, "Stage",               stageBadge);
                    AddInfoRow(info, "Expected Completion", $"{capturedP.monthsRemaining} Months");
                    AddInfoRow(info, "Total Cost",          UiFormat.Money(capturedP.totalCost, capturedSym));
                    AddInfoRow(info, "Monthly Burn",        UiFormat.Money(monthly, capturedSym) + "/mo");
                    AddInfoRow(info, "Progress",            $"{pct:F0}%");
                    if (capturedBp != null && !string.IsNullOrEmpty(capturedBp.targetId))
                        AddInfoRow(info, "Target Asset",    capturedBp.targetId);
                }

                if (_panelConstruction?.Q<VisualElement>("SideProject_Benefits") is VisualElement benefits)
                {
                    benefits.Clear();
                    AddBenefitRow(benefits, "GDP Growth",   $"+{capturedGdp:F1}%");
                    AddBenefitRow(benefits, "Jobs Created", $"+{capturedJobs:N0}");
                }
            });

            container.Add(card);
        }

private void SetBlueprintFilter(string filter)
        {
            _blueprintFilter = filter.ToUpperInvariant();

            if (_blueprintTabBar != null)
            {
                foreach (var tab in _blueprintTabBar.Children().OfType<Label>())
                {
                    if (tab.text.ToUpperInvariant() == _blueprintFilter)
                        tab.AddToClassList("active");
                    else
                        tab.RemoveFromClassList("active");
                }
            }

            var country = GameManager.Instance?.playerCountry;
            var sym = country != null && !string.IsNullOrEmpty(country.currencySymbol)
                      ? country.currencySymbol : "₦";
            RebuildBlueprintGrid(country, sym);
        }

        private void RebuildBlueprintGrid(CountryDataSO country, string sym)
        {
            _liveBlueprintGrid = _panelConstruction?.Q<VisualElement>("LiveBlueprintGrid");
            Debug.Log($"[Construction] RebuildBlueprintGrid | _liveBlueprintGrid: {_liveBlueprintGrid != null} | _panelConstruction: {_panelConstruction != null}");

            if (_liveBlueprintGrid == null) return;
            _liveBlueprintGrid.Clear();

            var db = GameManager.Instance?.EconomyDatabase;
            Debug.Log($"[Construction] EconomyDatabase: {db != null} | blueprints: {db?.blueprints?.Count ?? -1}");

            if (db == null || db.blueprints == null || db.blueprints.Count == 0)
            {
                _liveBlueprintGrid.Add(new Label("No blueprints loaded — add BlueprintDataSO assets to EconomyDatabase.")
                    { style = { color = Color.grey, fontSize = 9, whiteSpace = WhiteSpace.Normal } });
                return;
            }

            int  inPipeline = country?.activeProjects?.Count(p => IsActivePipelineStage(p.stage)) ?? 0;
            bool queueFull  = inPipeline >= 5;

            foreach (var bp in db.blueprints)
            {
                if (_blueprintFilter != "ALL" && !bp.type.ToUpperInvariant().Contains(_blueprintFilter))
                    continue;

                bool alreadyQueued = country?.activeProjects?
                    .Any(p => p.projectName == bp.blueprintName && IsActivePipelineStage(p.stage)) ?? false;

                float deposit   = bp.totalCostB * 0.1f;
                bool  canAfford = (GameManager.Instance?.playerBudget ?? 0f) >= deposit;
                bool  isInfra   = IsInfraType(bp.type);
                float bpGdp     = isInfra ? 0.8f : 0.4f;
                int   bpJobs    = isInfra ? 20000 : 10000;

                var card = new VisualElement();
                card.AddToClassList("blueprint-card");

                var bpImg = new VisualElement();
                bpImg.AddToClassList("blueprint-image");
                bpImg.AddToClassList(GetImageClass(bp.blueprintName, bp.type));
                card.Add(bpImg);

                card.Add(new Label(bp.blueprintName.ToUpper())
                    { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
                card.Add(new Label($"{TypeIcon(bp.type)} {bp.type.ToUpper()}")
                    { style = { color = TypeColor(bp.type), fontSize = 8, marginBottom = 4 } });
                card.Add(new Label(
                    $"Est. Cost        {UiFormat.Money(bp.totalCostB, sym)}\n" +
                    $"Deposit (10%)  {UiFormat.Money(deposit, sym)}\n" +
                    $"Est. Time        {bp.months} Months\n" +
                    $"GDP Boost       +{bpGdp:F1}%\n" +
                    $"Jobs Created    +{bpJobs:N0}")
                    { style = { color = Color.grey, fontSize = 8, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

                if (!string.IsNullOrEmpty(bp.description))
                    card.Add(new Label(bp.description)
                        { style = { color = new Color(0.6f, 0.6f, 0.6f), fontSize = 8, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

                bool hasUranium = bp.targetId != "PowerNuclear" || country == null || country.isImportingUranium || (country.resources != null && country.resources.Exists(r => r.type == MineralType.Uranium));

                if (!hasUranium)
                {
                    card.Add(new Label("⚠ Requires domestic uranium or import deal")
                        { style = { color = new Color(255f/255f, 171f/255f, 64f/255f), fontSize = 7, marginTop = 4 } });
                }

                if (alreadyQueued)
                    card.Add(new Label("✓ Already in pipeline")
                        { style = { color = new Color(0f, 230f/255f, 118f/255f), fontSize = 7, marginTop = 4 } });
                else if (!canAfford)
                    card.Add(new Label($"⚠ Need {UiFormat.Money(deposit, sym)} deposit")
                        { style = { color = new Color(255f/255f, 82f/255f, 82f/255f), fontSize = 7, marginTop = 4 } });
                else if (queueFull)
                    card.Add(new Label("⚠ Queue full (max 5 sites)")
                        { style = { color = new Color(255f/255f, 171f/255f, 64f/255f), fontSize = 7, marginTop = 4 } });

                var bpCopy  = bp;
                var commBtn = new Button(() => CommissionBlueprintFromCard(bpCopy)) { text = "COMMISSION" };
                commBtn.AddToClassList("project-detail-btn");
                commBtn.style.height    = 26;
                commBtn.style.fontSize  = 8;
                commBtn.style.marginTop = 8;
                commBtn.SetEnabled(!alreadyQueued && !queueFull && canAfford && hasUranium);
                card.Add(commBtn);

                _liveBlueprintGrid.Add(card);
            }

            if (_liveBlueprintGrid.childCount == 0)
                _liveBlueprintGrid.Add(new Label($"No blueprints match '{_blueprintFilter}'.")
                    { style = { color = Color.grey, fontSize = 9 } });

            if (_blueprintsVisible && country != null)
                PopulateBlueprintCatalog(country);
        }

private void CommissionBlueprintFromCard(BlueprintDataSO bp)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null || bp == null) return;

            string sym     = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float  deposit = bp.totalCostB * 0.1f;
            float  treasury = GameManager.Instance?.playerBudget ?? 0f;
            bool   isInfra  = IsInfraType(bp.type);
            float  estGdp   = isInfra ? 0.8f : 0.4f;
            int    estJobs  = isInfra ? 20000 : 10000;

            string impact =
                $"Deposit (10%): {UiFormat.Money(deposit, sym)}  ·  Treasury after: {UiFormat.Money(treasury - deposit, sym)}\n" +
                $"Duration: {bp.months} months  ·  Monthly: {UiFormat.Money(bp.totalCostB / Mathf.Max(1, bp.months), sym)}/mo\n" +
                $"On completion — GDP +{estGdp:F1}%  ·  Jobs +{estJobs:N0}";

            ShowEconomyModal(
                $"Commission: {bp.blueprintName}",
                $"{bp.description}\n\nConfirm commissioning {bp.blueprintName}?",
                impact,
                "COMMISSION",
                () =>
                {
                    if (EconomyLogicEvaluator.TryStartProject(country, bp, out string msg))
                        NotifyEconomy(msg, Color.green);
                    else
                        NotifyEconomy(msg, new Color(255f/255f, 82f/255f, 82f/255f));
                    RefreshDisplay();
                });
        }

        private void CommissionBlueprint(string blueprintName)
        {
            var country = GameManager.Instance?.playerCountry;
            var db      = GameManager.Instance?.EconomyDatabase;
            if (country == null || db == null || string.IsNullOrEmpty(blueprintName)) return;

            foreach (var bp in db.blueprints)
            {
                if (!BlueprintMatches(bp.blueprintName, blueprintName)) continue;
                CommissionBlueprintFromCard(bp);
                return;
            }

            ShowEconomyModal("Blueprint Unavailable", blueprintName,
                "No matching blueprint exists in the database.", "OK", null);
        }

private void ToggleBlueprints()
        {
            _blueprintsVisible = !_blueprintsVisible;
            ApplyBlueprintVisibility();
        }

        public void ShowBlueprintCatalog()
        {
            _blueprintsVisible = true;
            ApplyBlueprintVisibility();
            SwitchTab(4);
        }

        private void ApplyBlueprintVisibility()
        {
            var show = _blueprintsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_blueprintCatalog != null)      _blueprintCatalog.style.display      = show;
            if (_blueprintSectionTitle != null) _blueprintSectionTitle.style.display = show;
            if (_blueprintsVisible && GameManager.Instance?.playerCountry != null)
                PopulateBlueprintCatalog(GameManager.Instance.playerCountry);
        }

        private void PopulateBlueprintCatalog(CountryDataSO country)
        {
            if (_blueprintCatalog == null) return;
            _blueprintCatalog.Clear();

            var db  = GameManager.Instance?.EconomyDatabase;
            if (db == null) return;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            foreach (var bp in db.blueprints)
            {
                var card = new VisualElement();
                card.AddToClassList("sub-panel");
                card.style.width = Length.Percent(48); card.style.marginBottom = 10;
                card.style.paddingTop = 12; card.style.paddingBottom = 12;
                card.style.paddingLeft = 12; card.style.paddingRight = 12;

                card.Add(new Label(bp.blueprintName)
                    { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
                card.Add(new Label($"{bp.type} · {bp.months} mo · {UiFormat.Money(bp.totalCostB, sym)} total")
                    { style = { color = new Color(0f, 210f/255f, 1f), fontSize = 8, marginTop = 4 } });
                card.Add(new Label(bp.description)
                    { style = { color = Color.grey, fontSize = 8, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

                bool hasUranium = bp.targetId != "PowerNuclear" || country == null || country.isImportingUranium || (country.resources != null && country.resources.Exists(r => r.type == MineralType.Uranium));
                if (!hasUranium)
                    card.Add(new Label("Requires domestic uranium or import deal")
                        { style = { color = new Color(255f/255f, 171f/255f, 64f/255f), fontSize = 7, marginTop = 4 } });

                var bpCopy   = bp;
                var startBtn = new Button(() => CommissionBlueprintFromCard(bpCopy)) { text = "COMMISSION" };
                startBtn.AddToClassList("button-premium");
                startBtn.style.height = 24; startBtn.style.fontSize = 8; startBtn.style.marginTop = 8;
                startBtn.SetEnabled(hasUranium);
                card.Add(startBtn);

                _blueprintCatalog.Add(card);
            }
        }

        private void PopulateConstruction(CountryDataSO country, string sym)
        {
            if (_constructionSummary != null)
            {
                _constructionSummary.Clear();
                float monthlySpend = 0f; int pipelineCount = 0;
                foreach (var p in country.activeProjects)
                {
                    if (!IsActivePipelineStage(p.stage)) continue;
                    pipelineCount++;
                    if (p.totalMonths > 0) monthlySpend += p.totalCost / p.totalMonths;
                }
                _constructionSummary.Add(new Label(
                    $"QUEUE {pipelineCount}/5 · Burn {UiFormat.Money(monthlySpend, sym)}/mo · Treasury {UiFormat.Money(GameManager.Instance?.playerBudget ?? 0f, sym)}")
                    { style = { color = Color.white, fontSize = 10 } });
            }

            if (_constructionCardsContainer == null) return;
            _constructionCardsContainer.Clear();

            int displayed = 0;
            foreach (var proj in country.activeProjects)
            {
                if (!IsActivePipelineStage(proj.stage)) continue;
                AddConstructionProjectRow(country, proj, sym);
                displayed++;
            }

            if (displayed == 0)
                _constructionCardsContainer.Add(new Label("No active sites — open blueprints to commission a megaproject.")
                    { style = { color = Color.grey, alignSelf = Align.Center, marginTop = 20, fontSize = 10, whiteSpace = WhiteSpace.Normal } });
        }

        private void AddConstructionProjectRow(CountryDataSO country, ActiveProject proj, string sym)
        {
            var row = new VisualElement();
            row.AddToClassList("sub-panel");
            row.style.marginBottom = 8; row.style.paddingTop = 12; row.style.paddingBottom = 12;
            row.style.paddingLeft = 14; row.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            header.Add(new Label(proj.projectName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(proj.projectType?.ToUpper() ?? "") { style = { color = new Color(0f, 210f/255f, 1f), fontSize = 8 } });
            row.Add(header);

            row.Add(new Label($"{proj.regionName} · Target: {proj.targetID}") { style = { color = Color.grey, fontSize = 8, marginTop = 4 } });
            float pct = proj.totalMonths > 0 ? (proj.totalMonths - proj.monthsRemaining) / (float)proj.totalMonths * 100f : 0f;
            row.Add(new Label($"{pct:F0}% complete · {proj.monthsRemaining} mo left · {UiFormat.Money(proj.totalCost / Mathf.Max(1, proj.totalMonths), sym)}/mo")
                { style = { color = Color.grey, fontSize = 8, marginTop = 4 } });
            row.Add(new ProgressBar() { value = proj.totalMonths - proj.monthsRemaining, highValue = Mathf.Max(1, proj.totalMonths), style = { height = 6, marginTop = 8 } });

            var cancelBtn = new Button(() =>
            {
                country.activeProjects.Remove(proj);
                NotificationManager.Instance?.AddNotification($"Cancelled: {proj.projectName}", new Color(255f/255f, 171f/255f, 64f/255f));
                RefreshDisplay();
            }) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.height = 24; cancelBtn.style.fontSize = 8; cancelBtn.style.marginTop = 8;
            cancelBtn.style.backgroundColor = new Color(255f/255f, 82f/255f, 82f/255f, 0.15f);
            row.Add(cancelBtn);

            _constructionCardsContainer.Add(row);
        }

private static bool IsActivePipelineStage(ProjectStage stage)
            => stage == ProjectStage.Planned ||
               stage == ProjectStage.Funded  ||
               stage == ProjectStage.UnderConstruction;

        private static bool IsInfraType(string type)
            => type?.IndexOf("Infra", System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static string GetImageClass(string name, string type)
        {
            if (string.IsNullOrEmpty(name)) return GetImageClassForType(type);
            string n = name.ToLowerInvariant();

            if (n.Contains("highway") || n.Contains("road"))                      return "highway";
            if (n.Contains("rail"))                                              return "rail";
            if (n.Contains("port") || n.Contains("sea"))                         return "port";
            if (n.Contains("gas"))                                               return "gas";
            if (n.Contains("hydroelectric") || n.Contains("dam"))                return "dam";
            if (n.Contains("nuclear"))                                           return "nuclear";
            if (n.Contains("solar"))                                             return "solar";
            if (n.Contains("rice"))                                              return "rice";
            if (n.Contains("cocoa"))                                             return "cocoa";
            if (n.Contains("data") || n.Contains("centre") || n.Contains("datacenter")) return "datacenter";
            if (n.Contains("space") || n.Contains("satellite") || n.Contains("comms"))  return "comms";
            if (n.Contains("defence") || n.Contains("defense") || n.Contains("military")) return "defence";

            return GetImageClassForType(type);
        }

        private static string GetImageClassForType(string type)
        {
            if (string.IsNullOrEmpty(type)) return "highway";
            string t = type.ToLowerInvariant();
            if (t.Contains("energy") || t.Contains("power"))   return "power";
            if (t.Contains("defence") || t.Contains("defense") || t.Contains("military")) return "defence";
            if (t.Contains("space") || t.Contains("satellite") || t.Contains("comms"))    return "comms";
            if (t.Contains("agri") || t.Contains("farm") || t.Contains("irrigation"))     return "irrigation";
            if (t.Contains("port") || t.Contains("sea"))       return "port";
            return "highway";
        }

        private static string TypeIcon(string type)
        {
            if (string.IsNullOrEmpty(type)) return "⌁";
            string t = type.ToLowerInvariant();
            if (t.Contains("energy") || t.Contains("power"))   return "⌁";
            if (t.Contains("defence") || t.Contains("defense")) return "✚";
            if (t.Contains("space") || t.Contains("comms"))    return "❄";
            if (t.Contains("agri"))                             return "☘";
            if (t.Contains("port") || t.Contains("infra"))     return "⚓";
            return "▣";
        }

        private static Color TypeColor(string type)
        {
            if (string.IsNullOrEmpty(type)) return new Color(0f, 210f/255f, 1f);
            string t = type.ToLowerInvariant();
            if (t.Contains("energy") || t.Contains("power"))   return new Color(0f, 230f/255f, 118f/255f);
            if (t.Contains("defence") || t.Contains("defense")) return new Color(255f/255f, 82f/255f, 82f/255f);
            if (t.Contains("agri"))                             return new Color(0f, 230f/255f, 118f/255f);
            return new Color(0f, 210f/255f, 1f);
        }

        private void PanelSetLabel(string name, string text)
        {
            if (_panelConstruction?.Q<Label>(name) is Label lbl) lbl.text = text;
        }

        private static void AddInfoRow(VisualElement parent, string label, string value)
        {
            parent.Add(new Label($"{label,-32}{value}")
                { style = { whiteSpace = WhiteSpace.NoWrap, marginBottom = 5, fontSize = 9 } });
        }

        private static void AddBenefitRow(VisualElement parent, string label, string value)
        {
            var l = new Label($"{label,-32}{value}");
            l.AddToClassList("positive");
            l.style.marginBottom = 5;
            l.style.fontSize     = 9;
            parent.Add(l);
        }
    }
}
