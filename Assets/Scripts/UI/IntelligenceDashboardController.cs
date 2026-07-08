using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    // Run before MainDashboardToolkitController (which has order 10) so our Start() fires first
    [DefaultExecutionOrder(-10)]
    public class IntelligenceDashboardController : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        private VisualElement _dashboardRoot;
        
        private Button _tabOverview, _tabIntelNetwork, _tabRecon, _tabCovertOps, _tabDataAnalysis, _tabThreats;
        private VisualElement _panelOverview, _panelSurveillance, _panelCovertOps, _panelThreatBoard, _panelCovertOperations, _panelDataAnalysis;
        private List<VisualElement> _allPanels = new List<VisualElement>();

        // Overview Containers
        private VisualElement _overviewIntelSummaryContainer, _overviewCoverageGrid, _overviewFooterContainer;
        private ScrollView _overviewActiveOpsList, _overviewRecentReportsList, _overviewThreatsList;

        private Label _headerBudget, _headerOperatives, _headerCoverage, _exposureRiskLabel;
        private VisualElement _regionalCoverageList, _intelGridList, _intelInterceptFeed;
        private VisualElement _recruitedAssetsList; // Kept to avoid breaking any other hidden references, but largely unused now
        private VisualElement _satellitePanelContainer;
        private VisualElement _foreignThreatsList, _insurgentThreatsList, _internalThreatsList;
        private ProgressBar _agencyIntegrityBar, _exposureRiskBar;
        private Label _foreignAgentsLabel, _moleHuntLabel, _covertBudgetValueLabel, _exposureRiskBarLabel;
        private VisualElement _counterEspionageList;
        private Slider _covertBudgetSlider;

        private int _lastSeenTurn = -1;
        private List<string> _resolvedThreatNames = new List<string>();

        private void Awake()
        {
            _uiDoc = GetComponent<UIDocument>();
            // Do NOT query rootVisualElement here — UIDocument may not have built
            // its visual tree yet, especially if the GameObject starts inactive.
        }

        private void Start()
        {
            if (_uiDoc == null) _uiDoc = GetComponent<UIDocument>();
            if (_uiDoc != null)
            {
                _root = _uiDoc.rootVisualElement;
            }

            var dashboard = _root?.Q<VisualElement>("IntelligenceDashboard");

            // Fallback: UIDocument can defer tree construction past Start().
            // Clone directly from the asset — the element will be injected into
            // MainDashboard's panel container so this reference is what we need.
            if (dashboard == null && _uiDoc?.visualTreeAsset != null)
            {
                var container = new VisualElement();
                _uiDoc.visualTreeAsset.CloneTree(container);
                _root = container;
                dashboard = container.Q<VisualElement>("IntelligenceDashboard");
            }

            if (dashboard != null)
            {
                _dashboardRoot = dashboard;
                InitializeElements(dashboard);
                dashboard.style.display = DisplayStyle.None;
            }
            else
            {
                UnityEngine.Debug.LogError("[IntelDashboard] 'IntelligenceDashboard' NOT FOUND even after CloneTree fallback! Check UXML file.");
            }
        }


        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshIntelligenceData;
            if (_dashboardRoot != null) RefreshIntelligenceData();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshIntelligenceData;
        }

        private void InitializeElements(VisualElement root)
        {
            _tabOverview = root.Q<Button>("TabOverview");
            _tabIntelNetwork = root.Q<Button>("TabIntelNetwork");
            _tabRecon = root.Q<Button>("TabRecon");
            _tabCovertOps = root.Q<Button>("TabCovertOps");
            _tabDataAnalysis = root.Q<Button>("TabDataAnalysis");
            _tabThreats = root.Q<Button>("TabThreats");

            _panelOverview = root.Q<VisualElement>("PanelOverview");
            _panelSurveillance = root.Q<VisualElement>("PanelSurveillance");
            _panelCovertOps = root.Q<VisualElement>("PanelCovertOps"); // Note: This is Intel Network UI
            _panelCovertOperations = root.Q<VisualElement>("PanelCovertOperations");
            _panelDataAnalysis = root.Q<VisualElement>("PanelDataAnalysis");
            _panelThreatBoard = root.Q<VisualElement>("PanelThreatBoard");

            _allPanels.Clear();
            _allPanels.AddRange(new[] { _panelOverview, _panelCovertOps, _panelSurveillance, _panelCovertOperations, _panelDataAnalysis, _panelThreatBoard });

            if (_tabOverview != null) _tabOverview.clicked += () => SwitchTab(0);
            if (_tabIntelNetwork != null) _tabIntelNetwork.clicked += () => SwitchTab(1);
            if (_tabRecon != null) _tabRecon.clicked += () => SwitchTab(2);
            if (_tabCovertOps != null) _tabCovertOps.clicked += () => SwitchTab(3);
            if (_tabDataAnalysis != null) _tabDataAnalysis.clicked += () => SwitchTab(4);
            if (_tabThreats != null) _tabThreats.clicked += () => SwitchTab(5);

            _overviewIntelSummaryContainer = root.Q<VisualElement>("OverviewIntelSummaryContainer");
            _overviewActiveOpsList = root.Q<ScrollView>("OverviewActiveOpsList");
            _overviewRecentReportsList = root.Q<ScrollView>("OverviewRecentReportsList");
            _overviewCoverageGrid = root.Q<VisualElement>("OverviewCoverageGrid");
            _overviewFooterContainer = root.Q<VisualElement>("OverviewFooterContainer");
            _overviewThreatsList = root.Q<ScrollView>("OverviewThreatsList");

            _headerBudget     = root.Q<Label>("HeaderIntelBudget");
            _headerOperatives = root.Q<Label>("HeaderOperativeCount");
            _headerCoverage   = root.Q<Label>("GlobalCoverageScore");
            _exposureRiskLabel = root.Q<Label>("ExposureRiskLabel");

            _regionalCoverageList = root.Q<ScrollView>("RegionalCoverageList");
            _intelGridList        = root.Q<VisualElement>("IntelGridList");
            _intelInterceptFeed   = root.Q<ScrollView>("IntelInterceptFeed");
            // Removed _covertOpsContainer
            _recruitedAssetsList  = root.Q<VisualElement>("RecruitedAssetsList");
            _foreignThreatsList   = root.Q<VisualElement>("ForeignThreatsList");
            _insurgentThreatsList = root.Q<VisualElement>("InsurgentThreatsList");
            _internalThreatsList  = root.Q<VisualElement>("InternalThreatsList");
            _counterEspionageList = root.Q<VisualElement>("CounterEspionageList");

            _agencyIntegrityBar  = root.Q<ProgressBar>("AgencyIntegrityBar");
            _exposureRiskBar     = root.Q<ProgressBar>("ExposureRiskBar");
            _foreignAgentsLabel  = root.Q<Label>("ForeignAgentsLabel");
            _moleHuntLabel       = root.Q<Label>("MoleHuntLabel");
            if (_covertBudgetSlider != null)
            {
                _covertBudgetSlider.RegisterValueChangedCallback(evt =>
                {
                    var c = GameManager.Instance?.playerCountry;
                    if (c == null) return;
                    c.covertBudget = evt.newValue;
                    if (_covertBudgetValueLabel != null)
                        _covertBudgetValueLabel.text = $"${evt.newValue:F1}B / Turn";
                });
            }

            root.Q<Button>("BtnInternalAudit")?.RegisterCallback<ClickEvent>(_ => RunInternalAudit());
            
            // Hide the legacy UXML button since we build a customized one
            var legacyLaunchBtn = root.Q<Button>("BtnLaunchSatellite");
            if (legacyLaunchBtn != null) legacyLaunchBtn.style.display = DisplayStyle.None;
            
            // Create a dedicated container for the satellite section
            _satellitePanelContainer = new VisualElement();
            if (_intelGridList != null && _intelGridList.parent != null)
            {
                _intelGridList.parent.Insert(_intelGridList.parent.IndexOf(_intelGridList) + 1, _satellitePanelContainer);
            }

            root.Q<Button>("CloseIntelligence").clicked += Hide;
        }

        private void SwitchTab(int index)
        {
            for (int i = 0; i < _allPanels.Count; i++)
            {
                bool active = (i == index);
                _allPanels[i].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                _allPanels[i].pickingMode = active ? PickingMode.Position : PickingMode.Ignore;
                // Fix scrollviews in active panel so scrollbars work in builds
                if (active) UIStateManager.FixScrollViews(_allPanels[i]);
            }

            var tabs = new[] { _tabOverview, _tabIntelNetwork, _tabRecon, _tabCovertOps, _tabDataAnalysis, _tabThreats };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                if (i == index) tabs[i].AddToClassList("active-tab");
                else tabs[i].RemoveFromClassList("active-tab");
            }
        }

        public void Show()
        {
            var panel = _dashboardRoot;
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshIntelligenceData();
                UIStateManager.FixScrollViews(panel); // Fix scrollbars in builds after UI refresh
            }
            else
            {
            }
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        public void RefreshIntelligenceData()
        {
            var gm = GameManager.Instance;
            var country = gm.playerCountry;
            
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            if (gm.TurnSystem != null && gm.TurnSystem.CurrentTurn != _lastSeenTurn)
            {
                _lastSeenTurn = gm.TurnSystem.CurrentTurn;
                _resolvedThreatNames.Clear();
            }

            if (_headerBudget != null)
            {
                // intelligenceBudget is stored as billions directly
                _headerBudget.text = $"{UiFormat.Money(country.intelligenceBudget, sym)} / Turn";
            }
            if (_headerOperatives != null) _headerOperatives.text = country.operativeCount.ToString();
            if (_headerCoverage  != null) _headerCoverage.text   = $"{country.globalIntelCoverage:F0}/100";

            // Exposure Risk display
            if (_exposureRiskLabel != null)
            {
                _exposureRiskLabel.text = $"{country.exposureRisk:F0}%";
                _exposureRiskLabel.style.color = country.exposureRisk > 75f
                    ? new Color(1f, 0.32f, 0.32f)
                    : country.exposureRisk > 40f
                        ? new Color(1f, 0.67f, 0.25f)
                        : new Color(0f, 0.82f, 0.39f);
            }
            if (_exposureRiskBar != null) _exposureRiskBar.value = country.exposureRisk;
            if (_exposureRiskBarLabel != null)
            {
                if (country.exposureRisk > 75f) { _exposureRiskBarLabel.text = "⚠ CRITICAL — STAND DOWN"; _exposureRiskBarLabel.style.color = new Color(1f, 0.32f, 0.32f); }
                else if (country.exposureRisk > 40f) { _exposureRiskBarLabel.text = "ELEVATED RISK"; _exposureRiskBarLabel.style.color = new Color(1f, 0.67f, 0.25f); }
                else { _exposureRiskBarLabel.text = "LOW RISK"; _exposureRiskBarLabel.style.color = new Color(0f, 0.82f, 0.39f); }
            }
            // Sync slider to current budget
            if (_covertBudgetSlider != null) _covertBudgetSlider.SetValueWithoutNotify(country.covertBudget);
            if (_covertBudgetValueLabel != null) _covertBudgetValueLabel.text = $"${country.covertBudget:F1}B / Turn";

            if (_agencyIntegrityBar != null)
            {
                _agencyIntegrityBar.value = country.agencyIntegrity;
                _agencyIntegrityBar.title = $"{country.agencyIntegrity:F0}%";
            }

            if (_foreignAgentsLabel != null) _foreignAgentsLabel.text = $"FOREIGN AGENTS DETECTED: {country.foreignAgentsDetected}";
            // Active mole hunts = agents detected that haven't been neutralised yet
            int activeMoleHunts = Mathf.Max(0, country.foreignAgentsDetected - (country.agencyIntegrity > 70f ? 1 : 0));
            if (_moleHuntLabel != null) _moleHuntLabel.text = $"Active mole hunts: {activeMoleHunts}  |  Agency integrity: {country.agencyIntegrity:F0}%";

            DashboardLiveContent.PopulateFeed(_intelInterceptFeed, DashboardLiveContent.BuildIntelFeed(country, gm), 6);
            PopulateSurveillance(country, gm);
            PopulateIntelGrid(country, gm);
            PopulateIntelNetworkTab(country);
            PopulateThreatAssessmentTab(country);
            PopulateCovertOperationsTab(country);
            PopulateDataAnalysisTab(country);
            PopulateOverviewTab(country);
        }

        private void PopulateSurveillance(CountryDataSO country, GameManager gm)
        {
            if (_regionalCoverageList == null) return;
            _regionalCoverageList.Clear();

            if (country.regions.Count > 0)
            {
                foreach (var region in country.regions)
                {
                    float coverage = Mathf.Clamp(country.globalIntelCoverage * 0.4f + region.security * 0.5f + country.intelligenceLevel * 0.1f, 5f, 100f);
                    AddCoverageItem(region.regionName, coverage, region.security < 45f ? "INSURGENT RISK" : "MONITORING");
                }
            }
            else
            {
                AddCoverageItem("Domestic theatre", country.globalIntelCoverage, "HOMELAND");
                AddCoverageItem("Neighbouring states", country.globalIntelCoverage * 0.6f, "HUMINT");
            }

            if (gm.allCountries != null)
            {
                int added = 0;
                foreach (var other in gm.allCountries)
                {
                    if (other == country || added >= 4) break;
                    if (gm.WarSystem != null && gm.WarSystem.GetWarStage(other) >= WarStage.Tension)
                    {
                        AddCoverageItem($"Tasking: {other.countryName}", country.globalIntelCoverage * 0.5f, "PRIORITY");
                        added++;
                    }
                }
            }
        }

        private void PopulateIntelGrid(CountryDataSO country, GameManager gm)
        {
            if (_intelGridList == null) return;
            _intelGridList.Clear();
            _intelGridList.style.flexDirection = FlexDirection.Row;
            _intelGridList.style.flexWrap = Wrap.Wrap;
            _intelGridList.style.justifyContent = Justify.SpaceBetween;

            var cells = new[]
            {
                ("SIGINT", country.globalIntelCoverage),
                ("HUMINT", country.intelligenceLevel),
                ("CYBER", Mathf.Clamp(country.intelligenceLevel * 0.8f + country.covertBudget * 5f, 0f, 100f)),
                ("MARITIME", Mathf.Clamp(country.portLevel * 20f, 0f, 100f)),
                ("BORDER", country.security),
                ("IMINT (SAT)", Mathf.Clamp(country.satelliteCount * 25f, 0f, 100f))
            };

            foreach (var (name, val) in cells)
            {
                var cell = new VisualElement();
                cell.AddToClassList("sub-panel");
                cell.style.width = Length.Percent(48);
                cell.style.marginBottom = 8;
                cell.style.paddingTop = 12;
                cell.style.paddingBottom = 12;
                cell.style.paddingLeft = 12;
                cell.style.paddingRight = 12;
                cell.style.backgroundColor = new Color(0.06f, 0.06f, 0.10f, 0.97f);
                cell.style.borderTopWidth = 1; cell.style.borderBottomWidth = 1;
                cell.style.borderLeftWidth = 1; cell.style.borderRightWidth = 1;
                cell.style.borderTopColor = new Color(0.45f, 0.18f, 0.65f, 0.55f);
                cell.style.borderBottomColor = new Color(0.45f, 0.18f, 0.65f, 0.55f);
                cell.style.borderLeftColor = new Color(0.45f, 0.18f, 0.65f, 0.55f);
                cell.style.borderRightColor = new Color(0.45f, 0.18f, 0.65f, 0.55f);
                cell.style.borderTopLeftRadius = 4; cell.style.borderTopRightRadius = 4;
                cell.style.borderBottomLeftRadius = 4; cell.style.borderBottomRightRadius = 4;

                // Top accent stripe
                var stripe = new VisualElement();
                stripe.style.height = 2;
                stripe.style.backgroundColor = new Color(0.73f, 0.41f, 0.78f, 0.9f);
                stripe.style.marginBottom = 8;
                cell.Add(stripe);

                cell.Add(new Label(name) { style = { color = new Color(0.82f, 0.55f, 0.95f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 1.2f } });
                cell.Add(new ProgressBar() { value = val, highValue = 100, style = { height = 3, marginTop = 6 } });
                cell.Add(new Label($"{val:F0}% effective") { style = { color = new Color(0.55f, 0.55f, 0.65f), fontSize = 8, marginTop = 5 } });
                _intelGridList.Add(cell);
            }

            // ── Satellite Purchase Panel ──
            if (_satellitePanelContainer != null)
            {
                _satellitePanelContainer.Clear();
                
                var satSection = new VisualElement();
                satSection.style.flexDirection = FlexDirection.Column;
                satSection.style.width = Length.Percent(100);
                satSection.style.marginTop = 12;
                satSection.style.paddingTop = 14;
                satSection.style.paddingBottom = 14;
                satSection.style.paddingLeft = 14;
                satSection.style.paddingRight = 14;
                satSection.style.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 0.98f);
                satSection.style.borderTopWidth = 1; satSection.style.borderBottomWidth = 1;
                satSection.style.borderLeftWidth = 1; satSection.style.borderRightWidth = 1;
                satSection.style.borderTopColor = new Color(0.45f, 0.18f, 0.65f, 0.45f);
                satSection.style.borderBottomColor = new Color(0.45f, 0.18f, 0.65f, 0.45f);
                satSection.style.borderLeftColor = new Color(0.45f, 0.18f, 0.65f, 0.45f);
                satSection.style.borderRightColor = new Color(0.45f, 0.18f, 0.65f, 0.45f);
                satSection.style.borderTopLeftRadius = 4; satSection.style.borderTopRightRadius = 4;
                satSection.style.borderBottomLeftRadius = 4; satSection.style.borderBottomRightRadius = 4;

                var satHeader = new VisualElement();
                satHeader.style.flexDirection = FlexDirection.Row;
                satHeader.style.justifyContent = Justify.SpaceBetween;
                satHeader.style.marginBottom = 6;

                satHeader.Add(new Label("🛰  SATELLITE CONSTELLATION") {
                    style = { color = new Color(0.82f, 0.55f, 0.95f), fontSize = 10,
                              unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 1f }
                });

                int maxSats = 4;
                string satStatus = country.satelliteCount >= maxSats
                    ? "FULL COVERAGE"
                    : $"{country.satelliteCount}/{maxSats} ACTIVE";
                var satStatusLabel = new Label(satStatus);
                satStatusLabel.style.fontSize = 9;
                satStatusLabel.style.color = country.satelliteCount >= maxSats
                    ? new Color(0f, 0.82f, 0.39f)
                    : new Color(0.82f, 0.55f, 0.95f);
                satStatusLabel.style.backgroundColor = country.satelliteCount >= maxSats
                    ? new Color(0f, 0.4f, 0.2f, 0.3f)
                    : new Color(0.45f, 0.18f, 0.65f, 0.2f);
                satStatusLabel.style.paddingLeft = 7; satStatusLabel.style.paddingRight = 7;
                satStatusLabel.style.paddingTop = 2; satStatusLabel.style.paddingBottom = 2;
                satStatusLabel.style.borderTopLeftRadius = 10; satStatusLabel.style.borderTopRightRadius = 10;
                satStatusLabel.style.borderBottomLeftRadius = 10; satStatusLabel.style.borderBottomRightRadius = 10;
                satHeader.Add(satStatusLabel);
                satSection.Add(satHeader);

                var satDots = new VisualElement();
                satDots.style.flexDirection = FlexDirection.Row;
                satDots.style.marginBottom = 10;
                for (int s = 0; s < maxSats; s++)
                {
                    var dot = new VisualElement();
                    dot.style.width = 14; dot.style.height = 14;
                    dot.style.borderTopLeftRadius = 7; dot.style.borderTopRightRadius = 7;
                    dot.style.borderBottomLeftRadius = 7; dot.style.borderBottomRightRadius = 7;
                    dot.style.marginRight = 6;
                    dot.style.backgroundColor = s < country.satelliteCount
                        ? new Color(0.73f, 0.41f, 0.78f, 0.95f)
                        : new Color(0.2f, 0.2f, 0.25f, 0.8f);
                    satDots.Add(dot);
                }
                satSection.Add(satDots);

                float nextGain = country.satelliteCount == 0 ? 15f : country.satelliteCount == 1 ? 12f : country.satelliteCount == 2 ? 8f : 5f;
                var satDesc = new Label(country.satelliteCount + country.satellitesUnderConstruction >= maxSats
                    ? "Maximum constellation reached or queued. Full IMINT coverage assured."
                    : $"Next satellite: +{nextGain:F0}% IMINT coverage · Passive +1.5% intel/turn · Cost: $0.5B (Takes 6 Months)");
                satDesc.style.color = new Color(0.6f, 0.6f, 0.7f);
                satDesc.style.fontSize = 8;
                satDesc.style.whiteSpace = WhiteSpace.Normal;
                satDesc.style.marginBottom = 10;
                satSection.Add(satDesc);

                if (country.satellitesUnderConstruction > 0)
                {
                    var constructionLbl = new Label($"🚀 {country.satellitesUnderConstruction} SATELLITE(S) IN BUILD QUEUE ({country.satelliteBuildMonthsRemaining} MO REMAINING)");
                    constructionLbl.style.color = new Color(0.95f, 0.77f, 0.06f);
                    constructionLbl.style.fontSize = 9;
                    constructionLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    constructionLbl.style.marginBottom = 8;
                    satSection.Add(constructionLbl);
                }

                if (country.satelliteCount + country.satellitesUnderConstruction < maxSats)
                {
                    var launchBtn = new Button(LaunchSatellite) { text = "🚀  LAUNCH SPY SATELLITE  ($0.5B)" };
                    launchBtn.AddToClassList("button-premium");
                    launchBtn.style.height = 30;
                    launchBtn.style.fontSize = 10;
                    launchBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                    launchBtn.style.backgroundColor = new Color(0.3f, 0.1f, 0.5f, 0.9f);
                    launchBtn.style.borderTopWidth = 1; launchBtn.style.borderBottomWidth = 1;
                    launchBtn.style.borderLeftWidth = 1; launchBtn.style.borderRightWidth = 1;
                    launchBtn.style.borderTopColor = new Color(0.73f, 0.41f, 0.78f, 0.8f);
                    launchBtn.style.borderBottomColor = new Color(0.73f, 0.41f, 0.78f, 0.8f);
                    launchBtn.style.borderLeftColor = new Color(0.73f, 0.41f, 0.78f, 0.8f);
                    launchBtn.style.borderRightColor = new Color(0.73f, 0.41f, 0.78f, 0.8f);
                    launchBtn.style.borderTopLeftRadius = 4; launchBtn.style.borderTopRightRadius = 4;
                    launchBtn.style.borderBottomLeftRadius = 4; launchBtn.style.borderBottomRightRadius = 4;
                    satSection.Add(launchBtn);
                }
                
                _satellitePanelContainer.Add(satSection);
            }
        }

        private void AddCoverageItem(string region, float coverage, string tag)
        {
            var row = new VisualElement();
            row.AddToClassList("sub-panel");
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.paddingLeft = 14;
            row.style.paddingRight = 14;
            row.style.marginBottom = 8;
            row.style.backgroundColor = new Color(0.05f, 0.05f, 0.09f, 0.97f);
            row.style.borderLeftWidth = 3;
            row.style.borderLeftColor = new Color(0.73f, 0.41f, 0.78f, 0.85f);
            row.style.borderTopWidth = 1; row.style.borderBottomWidth = 1; row.style.borderRightWidth = 1;
            row.style.borderTopColor = new Color(0.25f, 0.25f, 0.35f, 0.4f);
            row.style.borderBottomColor = new Color(0.25f, 0.25f, 0.35f, 0.4f);
            row.style.borderRightColor = new Color(0.25f, 0.25f, 0.35f, 0.4f);
            row.style.borderTopLeftRadius = 3; row.style.borderTopRightRadius = 3;
            row.style.borderBottomLeftRadius = 3; row.style.borderBottomRightRadius = 3;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            header.Add(new Label(region) { style = { color = new Color(0.92f, 0.92f, 0.95f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });

            var tagLabel = new Label(tag);
            tagLabel.style.color = new Color(0.82f, 0.55f, 0.95f);
            tagLabel.style.fontSize = 8;
            tagLabel.style.backgroundColor = new Color(0.45f, 0.18f, 0.65f, 0.2f);
            tagLabel.style.paddingLeft = 6; tagLabel.style.paddingRight = 6;
            tagLabel.style.paddingTop = 2; tagLabel.style.paddingBottom = 2;
            tagLabel.style.borderTopLeftRadius = 10; tagLabel.style.borderTopRightRadius = 10;
            tagLabel.style.borderBottomLeftRadius = 10; tagLabel.style.borderBottomRightRadius = 10;
            header.Add(tagLabel);
            row.Add(header);
            row.Add(new ProgressBar() { value = coverage, highValue = 100, style = { height = 3, marginTop = 7 } });
            _regionalCoverageList.Add(row);
        }

        private void PopulateIntelNetworkTab(CountryDataSO country)
        {
            var panel = _root.Q<VisualElement>("PanelCovertOps");
            if (panel == null) return;

            // --- 1. KPI Top Strip ---
            int totalAgents = country.operativeCount;
            int totalInformants = Mathf.RoundToInt(country.intelligenceLevel * 1.5f);
            int activeMissions = country.activeCovertOps.Count;
            int criticalMissions = country.activeCovertOps.FindAll(x => x.riskLevel == "Extreme" || x.riskLevel == "High").Count;
            float successRate = 65f + (country.intelligenceLevel * 0.3f);
            
            panel.Q<Label>("IN_TotalAgents").text = totalAgents.ToString();
            panel.Q<Label>("IN_TotalInformants").text = totalInformants.ToString();
            panel.Q<Label>("IN_ActiveMissions").text = activeMissions.ToString();
            
            var critLbl = panel.Q<Label>("IN_CriticalMissions");
            critLbl.text = criticalMissions > 0 ? $"⚠ {criticalMissions} critical" : "No critical";
            critLbl.style.color = criticalMissions > 0 ? new Color(1f, 0.4f, 0.4f) : Color.grey;
            
            panel.Q<Label>("IN_SuccessRate").text = $"{successRate:F0}%";
            
            panel.Q<Label>("IN_CovertBudget").text = $"₦{country.covertBudget:F1}B / month";
            panel.Q<Label>("IN_NetworkExposure").text = country.exposureRisk > 70 ? "HIGH" : (country.exposureRisk > 40 ? "MEDIUM" : "LOW");
            panel.Q<Label>("IN_NetworkExposure").style.color = country.exposureRisk > 70 ? new Color(1f, 0.3f, 0.3f) : (country.exposureRisk > 40 ? new Color(1f, 0.6f, 0.2f) : new Color(0.137f, 0.929f, 0.337f));

            // --- 2. Active Agents ---
            panel.Q<Label>("IN_ActiveAgentsTitle").text = $"ACTIVE AGENTS ({country.activeCovertOps.Count})";
            var agentsList = panel.Q<ScrollView>("IN_ActiveAgentsList");
            agentsList.Clear();
            if (country.activeCovertOps.Count == 0)
            {
                agentsList.Add(new Label("NO ACTIVE OPERATIONS") { style = { color = Color.grey, fontSize = 10, marginTop = 10, unityTextAlign = TextAnchor.MiddleCenter } });
            }
            else
            {
                foreach (var op in country.activeCovertOps)
                {
                    agentsList.Add(CreateAgentCard(op));
                }
            }

            // --- 3. Recent Network Events ---
            var eventsList = panel.Q<ScrollView>("IN_RecentEventsList");
            eventsList.Clear();
            string[] sampleEvents = {
                "Asset successfully recruited in neighboring region.",
                "Agent RAVEN reported successful infiltration.",
                "Intercepted encrypted comms from Insurgent Camp.",
                "Counter-intel sweep neutralized foreign mole."
            };
            foreach (var ev in sampleEvents)
            {
                var evRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
                evRow.Add(new Label("◉") { style = { color = new Color(0.137f, 0.929f, 0.337f), marginRight = 8, fontSize = 8, marginTop = 2 } });
                evRow.Add(new Label(ev) { style = { color = new Color(0.8f, 0.8f, 0.8f), fontSize = 9, whiteSpace = WhiteSpace.Normal, flexShrink = 1 } });
                eventsList.Add(evRow);
            }

            // --- 4. Foreign Assets ---
            var assetsList = panel.Q<VisualElement>("IN_ForeignAssetsList");
            assetsList.Clear();
            if (country.recruitedAssets.Count == 0)
            {
                assetsList.Add(new Label("NO FOREIGN ASSETS") { style = { color = Color.grey, fontSize = 10, marginTop = 10, unityTextAlign = TextAnchor.MiddleCenter } });
            }
            else
            {
                foreach (var asset in country.recruitedAssets)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, paddingBottom = 8, paddingTop = 8, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.05f) } };
                    
                    var info = new VisualElement();
                    info.Add(new Label(asset.assetName) { style = { color = Color.white, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                    info.Add(new Label(asset.targetCountry) { style = { color = Color.grey, fontSize = 8, marginTop = 2 } });
                    row.Add(info);

                    var stats = new VisualElement { style = { flexDirection = FlexDirection.Row, width = 120, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
                    
                    var trustBar = new ProgressBar { value = asset.loyalty, highValue = 100 };
                    trustBar.style.width = 40; trustBar.style.height = 4;
                    stats.Add(trustBar);

                    var relContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    for (int i=1; i<=5; i++)
                    {
                        relContainer.Add(new Label("★") { style = { color = i <= asset.intelLevelProvided ? new Color(0.137f, 0.929f, 0.337f) : new Color(1,1,1,0.2f), fontSize = 8 } });
                    }
                    stats.Add(relContainer);
                    
                    row.Add(stats);
                    assetsList.Add(row);
                }
            }

            // --- 5. Network Capabilities ---
            var capList = panel.Q<VisualElement>("IN_NetworkCapabilitiesList");
            capList.Clear();
            string[] capNames = { "Border Surveillance", "Cyber Intelligence", "Economic Intelligence", "Military Intelligence" };
            float[] capVals = { 
                Mathf.Clamp(country.intelligenceLevel * 1.2f, 10, 100), 
                Mathf.Clamp(country.intelligenceLevel * 0.8f, 10, 100), 
                Mathf.Clamp(country.intelligenceLevel * 1.5f, 10, 100), 
                Mathf.Clamp(country.intelligenceLevel * 1.1f, 10, 100) 
            };
            
            for (int i=0; i<capNames.Length; i++)
            {
                var row = new VisualElement { style = { marginBottom = 6 } };
                var header = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
                header.Add(new Label(capNames[i]) { style = { color = Color.grey, fontSize = 9 } });
                header.Add(new Label($"{capVals[i]:F0}%") { style = { color = Color.white, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                row.Add(header);

                var pb = new ProgressBar { value = capVals[i], highValue = 100 };
                pb.style.height = 4;
                row.Add(pb);
                capList.Add(row);
            }

            // --- 6. Covert Budget Breakdown ---
            var brkList = panel.Q<VisualElement>("IN_BudgetBreakdownList");
            brkList.Clear();
            string[] brkNames = { "Agent Operations", "Surveillance & Recon", "Cyber Operations", "Informant Payments" };
            Color[] brkColors = { new Color(0.137f, 0.929f, 0.337f), new Color(0.18f, 0.6f, 0.95f), new Color(0.9f, 0.2f, 0.2f), new Color(0.95f, 0.7f, 0.15f) };
            float[] brkPcts = { 40f, 30f, 15f, 15f };
            
            for (int i=0; i<brkNames.Length; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
                row.Add(new VisualElement { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = brkColors[i], marginRight = 8 } });
                row.Add(new Label(brkNames[i]) { style = { color = Color.grey, fontSize = 9, width = 100 } });
                row.Add(new Label($"{brkPcts[i]:F0}%") { style = { color = Color.white, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                brkList.Add(row);
            }

            // --- 7. Recruit List ---
            var recruitList = panel.Q<ScrollView>("IN_RecruitList");
            recruitList.Clear();
            string[] recNames = { "Domestic Agent", "Foreign Asset", "Cyber Operative", "Double Agent" };
            string[] recCosts = { "₦15M", "₦45M", "₦30M", "₦80M" };
            for (int i=0; i<recNames.Length; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.03f), paddingBottom = 6, paddingTop = 6, paddingLeft = 8, paddingRight = 8, marginBottom = 4, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
                row.Add(new Label(recNames[i]) { style = { color = Color.white, fontSize = 9 } });
                
                var right = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                right.Add(new Label(recCosts[i]) { style = { color = new Color(0.137f, 0.929f, 0.337f), fontSize = 9, marginRight = 8 } });
                right.Add(new Button { text = "HIRE", style = { height = 18, fontSize = 8, backgroundColor = new Color(0.137f, 0.929f, 0.337f, 0.2f), color = new Color(0.137f, 0.929f, 0.337f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(0.137f, 0.929f, 0.337f, 0.5f), borderBottomColor = new Color(0.137f, 0.929f, 0.337f, 0.5f), borderLeftColor = new Color(0.137f, 0.929f, 0.337f, 0.5f), borderRightColor = new Color(0.137f, 0.929f, 0.337f, 0.5f) } });
                row.Add(right);
                recruitList.Add(row);
            }

            // --- 8. Quick Actions ---
            var qaGrid = panel.Q<VisualElement>("IN_QuickActionsGrid");
            qaGrid.Clear();
            string[] qaNames = { "Dispatch Agent", "Fund Operation", "Secure Channel", "Increase Surv.", "Debrief Agents", "Counter Intel" };
            foreach (var qa in qaNames)
            {
                var btn = new Button { text = qa };
                btn.style.width = Length.Percent(48);
                btn.style.height = 24;
                btn.style.marginBottom = 4;
                btn.style.fontSize = 8;
                btn.style.backgroundColor = new Color(1,1,1,0.05f);
                btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1; btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
                btn.style.borderTopColor = new Color(1,1,1,0.1f); btn.style.borderBottomColor = new Color(1,1,1,0.1f); btn.style.borderLeftColor = new Color(1,1,1,0.1f); btn.style.borderRightColor = new Color(1,1,1,0.1f);
                btn.style.color = Color.white;
                qaGrid.Add(btn);
            }
        }

        private VisualElement CreateAgentCard(CovertOperation op)
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.backgroundColor = new Color(1,1,1,0.03f);
            card.style.paddingTop = 8; card.style.paddingBottom = 8; card.style.paddingLeft = 8; card.style.paddingRight = 8;
            card.style.marginBottom = 8;
            card.style.borderTopLeftRadius = 4; card.style.borderTopRightRadius = 4; card.style.borderBottomLeftRadius = 4; card.style.borderBottomRightRadius = 4;
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1; card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = new Color(1,1,1,0.05f); card.style.borderBottomColor = new Color(1,1,1,0.05f); card.style.borderLeftColor = new Color(1,1,1,0.05f); card.style.borderRightColor = new Color(1,1,1,0.05f);

            var avatar = new VisualElement { style = { width = 36, height = 36, borderTopLeftRadius = 18, borderTopRightRadius = 18, borderBottomLeftRadius = 18, borderBottomRightRadius = 18, backgroundColor = new Color(1,1,1,0.1f), marginRight = 10, alignItems = Align.Center, justifyContent = Justify.Center } };
            var lbl = new Label("&#xf21b;") { style = { color = Color.white, fontSize = 16 } };
            lbl.AddToClassList("fa-icon");
            avatar.Add(lbl);
            card.Add(avatar);

            var mid = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
            header.Add(new Label(string.IsNullOrEmpty(op.operativeName) ? "UNKNOWN" : op.operativeName) { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } });
            header.Add(new Label(op.status.ToUpper()) { style = { color = new Color(0.18f, 0.6f, 0.95f), fontSize = 7, backgroundColor = new Color(0.18f, 0.6f, 0.95f, 0.2f), paddingLeft = 4, paddingRight = 4, paddingTop = 2, paddingBottom = 2, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
            mid.Add(header);

            var details = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            details.Add(new Label($"Loc: {op.target}") { style = { color = Color.grey, fontSize = 8, marginRight = 8 } });
            details.Add(new Label($"Asg: {op.type}") { style = { color = Color.grey, fontSize = 8 } });
            mid.Add(details);

            card.Add(mid);

            var right = new VisualElement { style = { width = 60, justifyContent = Justify.Center } };
            var progLbl = new Label($"Phase {op.currentPhase}/{op.totalPhases}") { style = { color = Color.white, fontSize = 8, marginBottom = 2, unityTextAlign = TextAnchor.MiddleRight } };
            right.Add(progLbl);
            
            float progPct = (float)op.currentPhase / op.totalPhases * 100f;
            var pb = new ProgressBar { value = progPct, highValue = 100, style = { height = 4 } };
            right.Add(pb);
            
            var expLbl = new Label($"Exp: {op.detectionRisk}%") { style = { color = op.detectionRisk > 50 ? new Color(1f, 0.3f, 0.3f) : new Color(0.137f, 0.929f, 0.337f), fontSize = 7, marginTop = 2, unityTextAlign = TextAnchor.MiddleRight } };
            right.Add(expLbl);

            card.Add(right);

            return card;
        }

        private void PopulateProxyWarPanel(CountryDataSO country, GameManager gm)
        {
            // Provides a shortcut button in the Threat Board to fund enemy insurgencies
        }

        private void PopulateThreatBoard(CountryDataSO country, GameManager gm)
        {
            if (_foreignThreatsList == null || _insurgentThreatsList == null || _internalThreatsList == null) return;
            _foreignThreatsList.Clear();
            _insurgentThreatsList.Clear();
            _internalThreatsList.Clear();

            // ── Real detected insurgent camps from RegionDataSO ──
            // These are discovered by IntelligenceSystem and shown here for Military to act on
            bool anyCampsDetected = false;
            foreach (var region in country.regions)
            {
                if (!region.insurgentCampDetected) continue;
                anyCampsDetected = true;
                _insurgentThreatsList.Add(BuildCampThreatCard(region, country));
            }

            // ── Standard IntelligenceThreat objects (foreign + internal) ──
            var threats = new List<IntelligenceThreat>(country.detectedThreats);
            if (threats.Count == 0)
                threats.AddRange(GenerateAmbientThreats(country, gm));

            threats.RemoveAll(t => _resolvedThreatNames.Contains(t.threatName));

            foreach (var threat in threats)
            {
                var item = BuildThreatItem(threat);
                if (threat.category == "Foreign Interference") _foreignThreatsList.Add(item);
                else if (threat.category == "Insurgent Activity") _insurgentThreatsList.Add(item);
                else _internalThreatsList.Add(item);
            }

            if (_foreignThreatsList.childCount == 0)
                _foreignThreatsList.Add(EmptyThreatLabel("No foreign interference detected."));
            if (_insurgentThreatsList.childCount == 0)
                _insurgentThreatsList.Add(EmptyThreatLabel(anyCampsDetected ? "" : "No insurgent camps detected. Continue surveillance."));
            if (_internalThreatsList.childCount == 0)
                _internalThreatsList.Add(EmptyThreatLabel("No internal threats flagged."));
        }

        /// <summary>
        /// Builds a rich camp card for a detected insurgent camp.
        /// Shows camp intel, threat level, and a "FLAG FOR AIRSTRIKE" button that
        /// marks the camp so Military → Operations knows to target it.
        /// </summary>
        private VisualElement BuildCampThreatCard(RegionDataSO region, CountryDataSO country)
        {
            var card = new VisualElement();
            card.style.marginBottom = 10;
            card.style.paddingTop = 14; card.style.paddingBottom = 14;
            card.style.paddingLeft = 14; card.style.paddingRight = 14;
            card.style.backgroundColor = new Color(0.10f, 0.04f, 0.04f, 0.98f);
            card.style.borderLeftWidth = 4;
            card.style.borderLeftColor = new Color(1f, 0.25f, 0.25f, 1f);
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = new Color(0.5f, 0.15f, 0.15f, 0.6f);
            card.style.borderBottomColor = new Color(0.5f, 0.15f, 0.15f, 0.6f);
            card.style.borderRightColor = new Color(0.5f, 0.15f, 0.15f, 0.6f);
            card.style.borderTopLeftRadius = 4; card.style.borderTopRightRadius = 4;
            card.style.borderBottomLeftRadius = 4; card.style.borderBottomRightRadius = 4;

            // ── Header row: name + CONFIRMED badge ──
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 6;

            header.Add(new Label($"🎯  {region.regionName.ToUpper()} — INSURGENT BASE") {
                style = { color = new Color(1f, 0.45f, 0.45f), fontSize = 10,
                          unityFontStyleAndWeight = FontStyle.Bold }
            });

            var badge = new Label("CONFIRMED") {
                style = { color = new Color(1f, 0.3f, 0.3f), fontSize = 8,
                          backgroundColor = new Color(0.6f, 0.1f, 0.1f, 0.4f),
                          paddingLeft = 6, paddingRight = 6, paddingTop = 2, paddingBottom = 2 }
            };
            badge.style.borderTopLeftRadius = 8; badge.style.borderTopRightRadius = 8;
            badge.style.borderBottomLeftRadius = 8; badge.style.borderBottomRightRadius = 8;
            header.Add(badge);
            card.Add(header);

            // ── Intel data grid ──
            string ideology = region.insurgencyIdeology != InsurgencyIdeology.None
                ? region.insurgencyIdeology.ToString() : "Unknown Faction";

            var dataGrid = new VisualElement();
            dataGrid.style.flexDirection = FlexDirection.Row;
            dataGrid.style.flexWrap = Wrap.Wrap;
            dataGrid.style.marginBottom = 8;

            void AddDataCell(string label, string value, Color valColor)
            {
                var cell = new VisualElement();
                cell.style.width = Length.Percent(48);
                cell.style.marginBottom = 6;
                cell.Add(new Label(label) { style = { color = new Color(0.5f, 0.5f, 0.6f), fontSize = 8 } });
                cell.Add(new Label(value) { style = { color = valColor, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                dataGrid.Add(cell);
            }

            AddDataCell("FACTION", ideology, new Color(1f, 0.6f, 0.6f));
            AddDataCell("CAMP STRENGTH", $"{region.campStrength:F0}%",
                region.campStrength > 70f ? new Color(1f, 0.3f, 0.3f) :
                region.campStrength > 40f ? new Color(1f, 0.7f, 0.2f) : new Color(0.4f, 1f, 0.4f));
            AddDataCell("INSURGENCY LEVEL", $"{region.insurgencyLevel:F0}%",
                region.insurgencyLevel > 60f ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.8f, 0.2f));
            AddDataCell("SUPPLY LINES", $"{region.insurgentSupplyLines:F0}%", new Color(0.7f, 0.7f, 0.8f));
            card.Add(dataGrid);

            // ── Status line ──
            bool strikeOrdered = region.campStrikeOrdered;
            string statusText = strikeOrdered
                ? "✅ AIRSTRIKE REQUESTED — Awaiting Military execution via OPERATIONS"
                : "⚠  Awaiting strike authorisation from Intelligence Command";
            var statusLabel = new Label(statusText);
            statusLabel.style.color = strikeOrdered ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.75f, 0.2f);
            statusLabel.style.fontSize = 8;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            statusLabel.style.marginBottom = 10;
            card.Add(statusLabel);

            // ── Warning: camp growing ──
            if (region.campStrength > 70f && !strikeOrdered)
            {
                var warn = new Label("🔴 CAMP APPROACHING FULL STRENGTH — strike window closing");
                warn.style.color = new Color(1f, 0.35f, 0.35f);
                warn.style.fontSize = 8;
                warn.style.marginBottom = 8;
                card.Add(warn);
            }

            // ── Action buttons ──
            if (!strikeOrdered)
            {
                // Primary: Flag for airstrike (Intel authorises → Military executes)
                var flagBtn = new Button(() =>
                {
                    region.campStrikeOrdered = true;

                    NotificationManager.Instance?.AddNotification(
                        $"✅ STRIKE AUTHORISED: {region.regionName} camp flagged for airstrike. " +
                        $"Open MILITARY → OPERATIONS and launch STRATEGIC BOMBARDMENT targeting '{region.regionName}'.",
                        new Color(1f, 0.7f, 0.2f));

                    InboxSystem.Instance?.SendMessage(country, "INTELLIGENCE COMMAND",
                        $"STRIKE ORDER: {region.regionName} Insurgent Base",
                        $"Intelligence Command has authorised an airstrike on the confirmed insurgent camp in {region.regionName}.\n\n" +
                        $"CAMP PROFILE:\n" +
                        $"• Faction: {ideology}\n" +
                        $"• Strength: {region.campStrength:F0}%\n" +
                        $"• Insurgency: {region.insurgencyLevel:F0}%\n\n" +
                        $"ACTION REQUIRED:\n" +
                        $"1. Open MILITARY tab\n" +
                        $"2. Click OPERATIONS\n" +
                        $"3. Select operation type: STRATEGIC BOMBARDMENT\n" +
                        $"4. Target theatre: {region.regionName}\n" +
                        $"5. Deploy bombers and confirm launch\n\n" +
                        $"The camp is flagged at the top of the available theatres list.",
                        "Critical");

                    NewsSystem.Instance?.PublishNews(
                        $"CLASSIFIED: {country.countryName} intelligence command authorises airstrike on {ideology} camp in {region.regionName}.");

                    RefreshIntelligenceData();
                }) { text = "🚀  FLAG FOR AIRSTRIKE  →  MILITARY TO EXECUTE" };

                flagBtn.style.height = 32;
                flagBtn.style.fontSize = 9;
                flagBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                flagBtn.style.backgroundColor = new Color(0.55f, 0.1f, 0.1f, 0.9f);
                flagBtn.style.borderTopWidth = 1; flagBtn.style.borderBottomWidth = 1;
                flagBtn.style.borderLeftWidth = 1; flagBtn.style.borderRightWidth = 1;
                flagBtn.style.borderTopColor = new Color(1f, 0.3f, 0.3f, 0.7f);
                flagBtn.style.borderBottomColor = new Color(1f, 0.3f, 0.3f, 0.7f);
                flagBtn.style.borderLeftColor = new Color(1f, 0.3f, 0.3f, 0.7f);
                flagBtn.style.borderRightColor = new Color(1f, 0.3f, 0.3f, 0.7f);
                flagBtn.style.borderTopLeftRadius = 4; flagBtn.style.borderTopRightRadius = 4;
                flagBtn.style.borderBottomLeftRadius = 4; flagBtn.style.borderBottomRightRadius = 4;
                flagBtn.style.marginBottom = 6;
                card.Add(flagBtn);

                // Secondary: Fund counter-insurgency instead (softer option)
                var cinBtn = new Button(() =>
                {
                    float cost = 0.08f;
                    var gm = GameManager.Instance;
                    if (gm == null || gm.playerBudget < cost)
                    {
                        NotificationManager.Instance?.AddNotification("INSUFFICIENT FUNDS: Need $0.08B for counter-insurgency operation.", Color.red);
                        return;
                    }
                    gm.playerBudget -= cost;
                    country.treasury = gm.playerBudget;
                    region.heartsAndMinds = Mathf.Min(100f, region.heartsAndMinds + 20f);
                    region.insurgentSupplyLines = Mathf.Max(0f, region.insurgentSupplyLines - 25f);
                    region.insurgencyLevel = Mathf.Max(0f, region.insurgencyLevel - 15f);

                    NotificationManager.Instance?.AddNotification(
                        $"COIN OP: Counter-insurgency assets deployed to {region.regionName}. " +
                        $"Hearts & Minds +20%, supply lines cut -25%, insurgency -15%. Cost: $0.08B.",
                        new Color(0.4f, 0.8f, 1f));
                    RefreshIntelligenceData();
                }) { text = "🤝  COUNTER-INSURGENCY OP  ($0.08B)" };

                cinBtn.style.height = 26;
                cinBtn.style.fontSize = 8;
                cinBtn.style.backgroundColor = new Color(0.05f, 0.2f, 0.35f, 0.9f);
                cinBtn.style.borderTopLeftRadius = 4; cinBtn.style.borderTopRightRadius = 4;
                cinBtn.style.borderBottomLeftRadius = 4; cinBtn.style.borderBottomRightRadius = 4;
                card.Add(cinBtn);
            }
            else
            {
                // Strike already ordered — show reminder
                var reminder = new Label("→ Go to MILITARY → OPERATIONS to execute the strike");
                reminder.style.color = new Color(0.6f, 0.6f, 0.7f);
                reminder.style.fontSize = 8;
                reminder.style.marginTop = 4;
                card.Add(reminder);
            }

            return card;
        }

        private static VisualElement EmptyThreatLabel(string text)
        {
            var box = new VisualElement();
            box.style.paddingTop = 10; box.style.paddingBottom = 10;
            box.style.paddingLeft = 12; box.style.paddingRight = 12;
            box.style.marginTop = 8;
            box.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.5f);
            box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1; box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0.2f, 0.2f, 0.3f, 0.3f); box.style.borderBottomColor = new Color(0.2f, 0.2f, 0.3f, 0.3f); box.style.borderLeftColor = new Color(0.2f, 0.2f, 0.3f, 0.3f); box.style.borderRightColor = new Color(0.2f, 0.2f, 0.3f, 0.3f);
            box.style.borderTopLeftRadius = 4; box.style.borderTopRightRadius = 4; box.style.borderBottomLeftRadius = 4; box.style.borderBottomRightRadius = 4;
            var lbl = new Label(text) { style = { color = new Color(0.4f, 0.4f, 0.5f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Italic } };
            box.Add(lbl);
            return box;
        }

        private VisualElement BuildThreatItem(IntelligenceThreat threat)
        {
            var item = new VisualElement();
            item.AddToClassList("sub-panel");
            item.style.paddingTop = 12;
            item.style.paddingBottom = 12;
            item.style.paddingLeft = 14;
            item.style.paddingRight = 14;
            item.style.marginBottom = 8;
            item.style.backgroundColor = new Color(0.05f, 0.04f, 0.08f, 0.98f);
            bool isCritical = threat.severity == "Critical";
            Color severityColor = isCritical ? new Color(1f, 0.32f, 0.32f, 0.9f) : new Color(0.95f, 0.77f, 0.06f, 0.9f);
            item.style.borderLeftWidth = 3;
            item.style.borderLeftColor = severityColor;
            item.style.borderTopWidth = 1; item.style.borderBottomWidth = 1; item.style.borderRightWidth = 1;
            item.style.borderTopColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            item.style.borderBottomColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            item.style.borderRightColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            item.style.borderTopLeftRadius = 3; item.style.borderTopRightRadius = 3;
            item.style.borderBottomLeftRadius = 3; item.style.borderBottomRightRadius = 3;

            var headerContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            headerContainer.Add(new Label(threat.threatName) { style = { color = new Color(0.95f, 0.95f, 1f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
            
            var badge = new Label(threat.severity.ToUpper()) { style = { fontSize = 8 } };
            badge.style.color = threat.severity == "Critical" ? new Color(255/255f, 82/255f, 82/255f) : new Color(241/255f, 196/255f, 15/255f);
            badge.style.alignSelf = Align.FlexStart;
            headerContainer.Add(badge);
            item.Add(headerContainer);

            if (!string.IsNullOrEmpty(threat.description))
                item.Add(new Label(threat.description) { style = { color = Color.grey, fontSize = 8, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

            if (threat.category == "Insurgent Activity")
            {
                var warningBox = new VisualElement();
                warningBox.style.backgroundColor = new Color(0.8f, 0.1f, 0.1f, 0.3f);
                warningBox.style.marginTop = 10;
                warningBox.style.paddingTop = 6; warningBox.style.paddingBottom = 6;
                warningBox.style.paddingLeft = 8; warningBox.style.paddingRight = 8;
                warningBox.style.borderTopLeftRadius = 4; warningBox.style.borderTopRightRadius = 4;
                warningBox.style.borderBottomLeftRadius = 4; warningBox.style.borderBottomRightRadius = 4;
                var warningText = new Label("⚠ CRITICAL WARNING: INSURGENT CAMP LOCATED") { style = { color = new Color(1f, 0.4f, 0.4f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 9 } };
                warningBox.Add(warningText);
                item.Add(warningBox);
            }

            Button actionBtn;
            if (threat.category == "Insurgent Activity")
            {
                actionBtn = new Button(() =>
                {
                    var country = GameManager.Instance?.playerCountry;
                    if (country == null) return;
                    
                    if (country.unitFighters + country.unitBombers < 5)
                    {
                        NotificationManager.Instance?.AddNotification("AIRSTRIKE FAILED: Require at least 5 Fighters/Bombers to deploy.", Color.red);
                        return;
                    }
                    if (country.reserveFuel < 50f || country.reserveAmmo < 50f)
                    {
                        NotificationManager.Instance?.AddNotification("AIRSTRIKE FAILED: Insufficient Fuel or Ammo.", Color.red);
                        return;
                    }
                    
                    country.reserveFuel -= 50f;
                    country.reserveAmmo -= 50f;
                    
                    var region = country.regions.Find(r => threat.threatName.Contains(r.regionName) || (threat.description != null && threat.description.Contains(r.regionName)));
                    if (region != null)
                    {
                        region.security = Mathf.Min(100f, region.security + 25f);
                        region.insurgencyLevel = Mathf.Max(0f, region.insurgencyLevel - 40f);
                    }
                    else
                    {
                        country.security = Mathf.Min(100f, country.security + 15f);
                    }
                    
                    _resolvedThreatNames.Add(threat.threatName);
                    country.detectedThreats.Remove(threat);
                    
                    NotificationManager.Instance?.AddNotification($"MISSION SUCCESS: Air assets successfully bombed insurgent locations in {threat.threatName}. Threat eliminated.", Color.green);
                    NewsSystem.Instance?.PublishNews($"AIR FORCE COMMAND: Successful tactical airstrike completely destroys insurgent stronghold.");
                    RefreshIntelligenceData();

                }) { text = "DEPLOY AIR ASSET TO BOMB LOCATION" };
                actionBtn.style.backgroundColor = new Color(0.6f, 0.1f, 0.1f, 0.8f);
            }
            else
            {
                actionBtn = new Button(() =>
                {
                    var country = GameManager.Instance?.playerCountry;
                    var gm = GameManager.Instance;
                    if (country == null || gm == null) return;

                    float cost = 0.05f; // $50M per asset deployment
                    if (gm.playerBudget < cost)
                    {
                        NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need $0.05B to deploy intelligence assets.", Color.red);
                        return;
                    }

                    if (country.operativeCount < 1)
                    {
                        NotificationManager.Instance?.AddNotification("NO OPERATIVES AVAILABLE: All agents are currently deployed.", Color.red);
                        return;
                    }

                    // Deduct cost and allocate operative
                    gm.playerBudget -= cost;
                    country.treasury = gm.playerBudget;
                    country.operativeCount--;
                    country.exposureRisk = Mathf.Min(100f, country.exposureRisk + 3f); // small exposure increase for deploying

                    string effectDesc = "";
                    if (threat.category == "Foreign Interference")
                    {
                        country.globalIntelCoverage = Mathf.Min(100f, country.globalIntelCoverage + 10f);
                        country.security = Mathf.Min(100f, country.security + 8f);
                        effectDesc = $"Counter-intelligence net active. Intel Coverage +10%, Security +8%.";

                        // Also surface a Proxy War retaliation option
                        var targetName = threat.threatName.Replace(" military buildup", "").Trim();
                        var targetCountry = gm?.allCountries?.Find(c => c.countryName == targetName);
                        if (targetCountry != null && gm?.DiplomacySystem is DiplomacySystem diplo)
                        {
                            var proxyBtn = new Button(() =>
                            {
                                diplo.FundProxyInsurgency(country, targetCountry, 0.1f);
                                RefreshIntelligenceData();
                            }) { text = "💰 FUND PROXY ($0.1B)" };
                            proxyBtn.AddToClassList("button-premium");
                            proxyBtn.style.marginTop = 6;
                            proxyBtn.style.height = 22;
                            proxyBtn.style.fontSize = 8;
                            item.Add(proxyBtn);
                        }
                    }
                    else // Internal Threats
                    {
                        country.agencyIntegrity = Mathf.Min(100f, country.agencyIntegrity + 15f);
                        country.corruptionIndex = Mathf.Max(0f, country.corruptionIndex - 5f);
                        effectDesc = $"Internal audit & purge active. Agency Integrity +15%, Corruption -5%.";
                    }

                    NotificationManager.Instance?.AddNotification($"ASSET DEPLOYED: {threat.threatName}. {effectDesc}", new Color(186/255f, 104/255f, 200/255f));
                    NewsSystem.Instance?.PublishNews($"INTELLIGENCE: Assets deployed to address {threat.threatName}.");
                    
                    // Add to resolved threat list and remove from country list
                    _resolvedThreatNames.Add(threat.threatName);
                    country.detectedThreats.Remove(threat);

                    RefreshIntelligenceData();
                }) { text = "DEPLOY ASSET" };
            }
            
            actionBtn.AddToClassList("button-premium");
            actionBtn.style.marginTop = 10;
            actionBtn.style.height = 20;
            actionBtn.style.fontSize = 8;
            item.Add(actionBtn);
            return item;
        }

        private static List<IntelligenceThreat> GenerateAmbientThreats(CountryDataSO c, GameManager gm)
        {
            var list = new List<IntelligenceThreat>();

            if (gm.allCountries != null)
            {
                foreach (var other in gm.allCountries)
                {
                    if (other == c) continue;
                    // Detect at Crisis normally, or at Tension if satellites are available
                    var requiredStage = c.satelliteCount > 0 ? WarStage.Tension : WarStage.Crisis;
                    if (gm.WarSystem != null && gm.WarSystem.GetWarStage(other) >= requiredStage)
                    {
                        list.Add(new IntelligenceThreat
                        {
                            threatName = $"{other.countryName} military buildup",
                            category = "Foreign Interference",
                            severity = "Major",
                            description = c.satelliteCount > 0 ? "IMINT reveals early force concentration near shared border." : "Sat imagery shows force concentration near shared border."
                        });
                        break;
                    }
                }
            }

            foreach (var region in c.regions)
            {
                // Detect insurgency earlier with good satellite coverage
                float threshold = c.satelliteCount >= 2 ? 55f : 40f;
                if (region.security < threshold)
                {
                    list.Add(new IntelligenceThreat
                    {
                        threatName = $"Insurgent activity — {region.regionName}",
                        category = "Insurgent Activity",
                        severity = region.security < 25f ? "Critical" : "Major",
                        description = c.satelliteCount >= 2 ? "High-res IMINT confirms insurgent camp locations and supply routes." : "Human assets report weapons movement and checkpoint attacks."
                    });
                }
            }

            if (c.corruptionIndex > 50f)
                list.Add(new IntelligenceThreat
                {
                    threatName = "Cabinet leak risk",
                    category = "Internal Threats",
                    severity = "Minor",
                    description = "Financial intelligence flags unexplained transfers near ministries."
                });

            return list;
        }

        private void PopulateCovertOperationsTab(CountryDataSO country)
        {
            var panel = _root.Q<VisualElement>("PanelCovertOperations");
            if (panel == null) return;

            // Colors
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);
            var cBlue = new Color(0.18f, 0.6f, 0.95f);
            var cPurple = new Color(0.73f, 0.41f, 0.78f);
            var cWhiteAlpha = new Color(1, 1, 1, 0.1f);

            int intelLvl = Mathf.Clamp((int)country.intelligenceLevel, 10, 100);
            float budget = country.covertBudget;

            // --- KPIs ---
            int totalOps = 12; // Static for mockup look
            int inProgress = 5;
            int planning = 3;
            int onHold = 2;
            int completed = 2;

            panel.Q<Label>("CO_ActiveOps").text = inProgress.ToString();
            panel.Q<Label>("CO_HighRiskOps").text = "2 High Risk";
            panel.Q<Label>("CO_SuccessRate").text = "68%";
            panel.Q<Label>("CO_SuccessDelta").text = "↗ 6% vs last month";
            panel.Q<Label>("CO_Budget").text = $"₦{Mathf.RoundToInt(budget)}M";
            panel.Q<Label>("CO_BudgetRemaining").text = $"₦{Mathf.RoundToInt(budget * 0.28f)}M remaining";
            
            var riskLbl = panel.Q<Label>("CO_RiskLabel");
            riskLbl.text = "MEDIUM";
            riskLbl.style.color = cOrange;
            panel.Q<VisualElement>("CO_RiskDot").style.backgroundColor = cOrange;

            // --- DONUT CHART (Mock text) ---
            panel.Q<Label>("CO_TotalOpsDonut").text = totalOps.ToString();
            
            var statusList = panel.Q<VisualElement>("CO_StatusList");
            statusList.Clear();
            string[] sNames = { "In Progress", "Planning", "On Hold", "Completed" };
            int[] sVals = { inProgress, planning, onHold, completed };
            Color[] sColors = { cGreen, cBlue, cOrange, cPurple };
            
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4, alignItems = Align.Center } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new VisualElement { style = { width = 6, height = 6, backgroundColor = sColors[i], marginRight = 6 } });
                rL.Add(new Label(sNames[i]) { style = { color = Color.white, fontSize = 8 } });
                row.Add(rL);
                row.Add(new Label($"{sVals[i]} ({Mathf.RoundToInt(sVals[i]*100f/totalOps)}%)") { style = { color = Color.grey, fontSize = 8 } });
                statusList.Add(row);
            }

            // --- CATEGORIES ---
            var catList = panel.Q<VisualElement>("CO_CategoryList");
            catList.Clear();
            string[] cNames = { "Political Operations", "Economic Operations", "Military Operations", "Influence Operations", "Cyber Operations" };
            string[] cIcons = { "&#xf0c0;", "&#xf153;", "&#xf132;", "&#xf0a1;", "&#xf108;" };
            int[] cVals = { 4, 3, 2, 2, 1 };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6, alignItems = Align.Center } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new Label(cIcons[i]) { style = { color = Color.grey, fontSize = 10, marginRight = 8, width = 14 } }.WithClass("fa-icon"));
                rL.Add(new Label(cNames[i]) { style = { color = Color.grey, fontSize = 8 } });
                row.Add(rL);
                row.Add(new Label(cVals[i].ToString()) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                catList.Add(row);
            }

            // --- RESOURCE ALLOCATION ---
            var resList = panel.Q<VisualElement>("CO_ResourceList");
            resList.Clear();
            string[] rNames = { "Field Agents", "Technical Assets", "Safe Houses", "Logistics", "Cyber Tools" };
            int[] rVals = { 65, 55, 40, 60, 70 };
            Color[] rColors = { cGreen, cBlue, cOrange, cPurple, new Color(0.2f, 0.7f, 0.7f) };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                row.Add(new Label(rNames[i]) { style = { color = Color.grey, fontSize = 8, width = 80 } });
                
                var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                barBg.Add(new VisualElement { style = { width = Length.Percent(rVals[i]), height = 4, backgroundColor = rColors[i], borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                row.Add(barBg);
                
                row.Add(new Label($"{rVals[i]}%") { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, width = 25, unityTextAlign = TextAnchor.MiddleRight } });
                resList.Add(row);
            }

            // --- ACTIVE COVERT OPERATIONS ---
            var activeList = panel.Q<ScrollView>("CO_ActiveOpsList");
            activeList.Clear();
            string[] aOps = { "OPERATION SILENCE", "OPERATION MARKET SHIFT", "OPERATION WHISPER", "OPERATION IRON SHIELD", "OPERATION DARKNET" };
            string[] aCats = { "Political", "Economic", "Influence", "Military", "Cyber" };
            string[] aObjs = { "Discredit opposition leadership", "Undermine rival trade network", "Spread pro-government propaganda", "Acquire arms shipment intelligence", "Infiltrate hostile communication network" };
            string[] aLocs = { "Northern Nigeria", "Lagos State", "South-East Region", "Chad Basin", "Gulf of Guinea" };
            string[] aRisks = { "HIGH", "MEDIUM", "LOW", "HIGH", "MEDIUM" };
            string[] aStats = { "IN PROGRESS", "IN PROGRESS", "IN PROGRESS", "PLANNING", "PLANNING" };
            int[] aProgs = { 72, 45, 60, 25, 15 };
            string[] aEtas = { "12 DAYS", "18 DAYS", "9 DAYS", "—", "—" };
            Color[] aColors = { cRed, cOrange, cPurple, cBlue, cGreen };
            
            panel.Q<Label>("CO_ActiveTitle").text = $"ACTIVE COVERT OPERATIONS ({inProgress})";

            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.03f), paddingBottom = 6, paddingTop = 6, paddingLeft = 8, paddingRight = 8, marginBottom = 4, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
                
                var nameCol = new VisualElement { style = { width = 140, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var iconBox = new VisualElement { style = { width = 24, height = 24, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(aColors[i].r, aColors[i].g, aColors[i].b, 0.1f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 8 } };
                iconBox.Add(new Label(cIcons[i]) { style = { color = aColors[i], fontSize = 10 } }.WithClass("fa-icon"));
                nameCol.Add(iconBox);
                
                var nameText = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                nameText.Add(new Label(aOps[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                nameText.Add(new Label(aCats[i]) { style = { color = Color.grey, fontSize = 7 } });
                nameCol.Add(nameText);
                row.Add(nameCol);

                row.Add(new Label(aObjs[i]) { style = { color = Color.grey, fontSize = 8, flexGrow = 1, whiteSpace = WhiteSpace.Normal } });
                row.Add(new Label(aLocs[i]) { style = { color = Color.white, fontSize = 8, width = 80 } });
                
                var rCol = aRisks[i] == "HIGH" ? cRed : (aRisks[i] == "MEDIUM" ? cOrange : cGreen);
                row.Add(new Label(aRisks[i]) { style = { color = rCol, fontSize = 7, width = 50 } });
                
                var sCol = aStats[i] == "IN PROGRESS" ? cGreen : cBlue;
                row.Add(new Label(aStats[i]) { style = { color = sCol, fontSize = 7, width = 70 } });

                var progCol = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 60 } };
                var pBarBg = new VisualElement { style = { flexGrow = 1, height = 3, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 1, borderTopRightRadius = 1, borderBottomLeftRadius = 1, borderBottomRightRadius = 1, marginRight = 6 } };
                pBarBg.Add(new VisualElement { style = { width = Length.Percent(aProgs[i]), height = 3, backgroundColor = sCol, borderTopLeftRadius = 1, borderTopRightRadius = 1, borderBottomLeftRadius = 1, borderBottomRightRadius = 1 } });
                progCol.Add(pBarBg);
                progCol.Add(new Label($"{aProgs[i]}%") { style = { color = Color.white, fontSize = 7 } });
                row.Add(progCol);

                row.Add(new Label(aEtas[i]) { style = { color = Color.white, fontSize = 8, width = 40, unityTextAlign = TextAnchor.MiddleRight } });
                
                activeList.Add(row);
            }

            // --- OPERATION DETAILS ---
            var detailsContainer = panel.Q<VisualElement>("CO_OpDetailsContainer");
            detailsContainer.Clear();
            detailsContainer.style.flexDirection = FlexDirection.Row;

            // Details Left
            var dLeft = new VisualElement { style = { width = Length.Percent(50), paddingRight = 12, borderRightWidth = 1, borderRightColor = cWhiteAlpha } };
            
            var dHeader = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 12 } };
            var dIcon = new VisualElement { style = { width = 36, height = 36, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(cRed.r, cRed.g, cRed.b, 0.1f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 12 } };
            dIcon.Add(new Label("&#xf0c0;") { style = { color = cRed, fontSize = 16 } }.WithClass("fa-icon"));
            dHeader.Add(dIcon);
            var dTitle = new VisualElement();
            dTitle.Add(new Label("OPERATION SILENCE") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
            dTitle.Add(new Label("Political Operation") { style = { color = Color.grey, fontSize = 8 } });
            dHeader.Add(dTitle);
            dLeft.Add(dHeader);

            dLeft.Add(new Label("DESCRIPTION") { style = { color = Color.grey, fontSize = 7, marginBottom = 2 } });
            dLeft.Add(new Label("Discredit the leader of a major opposition party through exposure of fabricated corruption evidence.") { style = { color = Color.white, fontSize = 8, whiteSpace = WhiteSpace.Normal, marginBottom = 12 } });

            var dGrid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            dGrid.Add(CreateDetailStat("TARGET", "Opposition Leadership"));
            dGrid.Add(CreateDetailStat("TARGET AREA", "Northern Nigeria"));
            dGrid.Add(CreateDetailStat("SPONSOR", "NIA Directorate"));
            dGrid.Add(CreateDetailStat("ASSIGNED CASE OFFICER", "Agent Atlas"));
            dLeft.Add(dGrid);
            
            var dBtns = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 12 } };
            dBtns.Add(new Button { text = "VIEW INTEL", style = { height = 24, fontSize = 8, flexGrow = 1, backgroundColor = new Color(0.18f, 0.6f, 0.95f, 0.1f), color = cBlue, borderTopColor = cBlue, borderBottomColor = cBlue, borderLeftColor = cBlue, borderRightColor = cBlue, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, marginRight = 6 } });
            dBtns.Add(new Button { text = "MODIFY PLAN", style = { height = 24, fontSize = 8, flexGrow = 1, backgroundColor = new Color(0.18f, 0.6f, 0.95f, 0.1f), color = cBlue, borderTopColor = cBlue, borderBottomColor = cBlue, borderLeftColor = cBlue, borderRightColor = cBlue, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, marginRight = 6 } });
            dBtns.Add(new Button { text = "PAUSE OPERATION", style = { height = 24, fontSize = 8, flexGrow = 1, backgroundColor = new Color(0.95f, 0.77f, 0.06f, 0.1f), color = cOrange, borderTopColor = cOrange, borderBottomColor = cOrange, borderLeftColor = cOrange, borderRightColor = cOrange, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, marginRight = 6 } });
            dBtns.Add(new Button { text = "TERMINATE", style = { height = 24, fontSize = 8, flexGrow = 1, backgroundColor = new Color(1f, 0.32f, 0.32f, 0.1f), color = cRed, borderTopColor = cRed, borderBottomColor = cRed, borderLeftColor = cRed, borderRightColor = cRed, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1 } });
            dLeft.Add(dBtns);
            
            detailsContainer.Add(dLeft);

            // Details Right
            var dRight = new VisualElement { style = { width = Length.Percent(50), paddingLeft = 12, flexDirection = FlexDirection.Column } };
            dRight.Add(new Label("OBJECTIVES") { style = { color = Color.grey, fontSize = 8, marginBottom = 6 } });
            string[] objTxt = { "Identify vulnerabilities", "Create disinformation package", "Leak to media", "Amplify through social channels", "Monitor public reaction" };
            bool[] objDone = { true, true, false, false, false };
            for(int i=0; i<5; i++)
            {
                var oRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
                oRow.Add(new Label(objDone[i] ? "&#xf058;" : "&#xf111;") { style = { color = objDone[i] ? cGreen : Color.grey, fontSize = 10, marginRight = 6 } }.WithClass("fa-icon"));
                oRow.Add(new Label(objTxt[i]) { style = { color = objDone[i] ? Color.white : Color.grey, fontSize = 8 } });
                dRight.Add(oRow);
            }

            var dMetrics = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 12, justifyContent = Justify.SpaceBetween } };
            
            var mRisk = new VisualElement { style = { width = Length.Percent(30) } };
            mRisk.Add(new Label("RISK LEVEL") { style = { color = Color.grey, fontSize = 7, marginBottom = 2 } });
            var mrRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            mrRow.Add(new Label("&#xf071;") { style = { color = cRed, fontSize = 12, marginRight = 4 } }.WithClass("fa-icon"));
            mrRow.Add(new Label("HIGH") { style = { color = cRed, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
            mRisk.Add(mrRow);
            dMetrics.Add(mRisk);
            
            var mDet = new VisualElement { style = { width = Length.Percent(30) } };
            mDet.Add(new Label("DETECTION RISK") { style = { color = Color.grey, fontSize = 7, marginBottom = 2 } });
            mDet.Add(new Label("68%") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
            var dbBar1 = new VisualElement { style = { height = 3, backgroundColor = cWhiteAlpha, marginTop = 4 } };
            dbBar1.Add(new VisualElement { style = { width = Length.Percent(68), height = 3, backgroundColor = cOrange } });
            mDet.Add(dbBar1);
            dMetrics.Add(mDet);

            var mSuc = new VisualElement { style = { width = Length.Percent(30) } };
            mSuc.Add(new Label("SUCCESS CHANCE") { style = { color = Color.grey, fontSize = 7, marginBottom = 2 } });
            mSuc.Add(new Label("72%") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
            var dbBar2 = new VisualElement { style = { height = 3, backgroundColor = cWhiteAlpha, marginTop = 4 } };
            dbBar2.Add(new VisualElement { style = { width = Length.Percent(72), height = 3, backgroundColor = cGreen } });
            mSuc.Add(dbBar2);
            dMetrics.Add(mSuc);

            dRight.Add(dMetrics);
            detailsContainer.Add(dRight);

            // --- UPCOMING OPERATIONS ---
            var upcomingList = panel.Q<ScrollView>("CO_UpcomingOpsList");
            upcomingList.Clear();
            string[] uOps = { "OPERATION PUPPET", "OPERATION FRACTURE", "OPERATION BLACK SEA" };
            string[] uCats = { "Influence", "Political", "Military" };
            string[] uTargs = { "Target: Media Outlets", "Target: Opposition Party", "Target: Smuggling Route" };
            string[] uStarts = { "Start In: 3 Days", "Start In: 5 Days", "Start In: 7 Days" };
            string[] uRisks = { "MEDIUM", "HIGH", "HIGH" };
            int[] uDots = { 3, 4, 4 };
            Color[] uColors = { cPurple, cRed, cBlue };

            for(int i=0; i<3; i++)
            {
                var card = new VisualElement { style = { flexDirection = FlexDirection.Row, backgroundColor = new Color(1,1,1,0.03f), paddingBottom = 8, paddingTop = 8, paddingLeft = 8, paddingRight = 8, marginBottom = 6, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, borderLeftWidth = 2, borderLeftColor = uColors[i] } };
                
                var cIcon = new VisualElement { style = { width = 24, height = 24, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(uColors[i].r, uColors[i].g, uColors[i].b, 0.1f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 8 } };
                cIcon.Add(new Label(uCats[i] == "Influence" ? "&#xf0a1;" : uCats[i] == "Political" ? "&#xf0c0;" : "&#xf132;") { style = { color = uColors[i], fontSize = 10 } }.WithClass("fa-icon"));
                card.Add(cIcon);
                
                var cMid = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };
                cMid.Add(new Label(uOps[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                cMid.Add(new Label(uCats[i]) { style = { color = Color.grey, fontSize = 7 } });
                cMid.Add(new Label(uTargs[i]) { style = { color = Color.grey, fontSize = 7, marginTop = 2 } });
                cMid.Add(new Label(uStarts[i]) { style = { color = Color.white, fontSize = 7, marginTop = 2 } });
                card.Add(cMid);
                
                var cRight = new VisualElement { style = { alignItems = Align.FlexEnd, justifyContent = Justify.SpaceBetween } };
                cRight.Add(new Label(uRisks[i]) { style = { color = uRisks[i] == "HIGH" ? cRed : cOrange, fontSize = 7, marginBottom = 12 } });
                var dotRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                for(int d=0; d<4; d++)
                {
                    dotRow.Add(new VisualElement { style = { width = 4, height = 4, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, backgroundColor = d < uDots[i] ? (uRisks[i] == "HIGH" ? cRed : cOrange) : cWhiteAlpha, marginLeft = 2 } });
                }
                cRight.Add(dotRow);
                card.Add(cRight);
                
                upcomingList.Add(card);
            }

            // --- SAFE HOUSES ---
            var safeList = panel.Q<ScrollView>("CO_SafeHousesList");
            safeList.Clear();
            string[] shNames = { "SAFE HOUSE ALPHA", "SAFE HOUSE BRAVO", "SAFE HOUSE CHARLIE", "SAFE HOUSE DELTA" };
            string[] shLocs = { "Lagos State", "Kano State", "Rivers State", "Abuja FCT" };
            int[] shAgents = { 2, 3, 1, 2 };
            bool[] shSec = { true, true, false, true };

            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingBottom = 6, paddingTop = 6, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                row.Add(new Label("&#xf015;") { style = { color = Color.white, fontSize = 12, marginRight = 10 } }.WithClass("fa-icon"));
                
                var sMid = new VisualElement { style = { flexGrow = 1 } };
                sMid.Add(new Label(shNames[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                var sSub = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                sSub.Add(new Label(shLocs[i]) { style = { color = Color.grey, fontSize = 7, width = 60 } });
                sSub.Add(new Label($"{shAgents[i]} Agents") { style = { color = Color.grey, fontSize = 7 } });
                sMid.Add(sSub);
                row.Add(sMid);
                
                row.Add(new Label(shSec[i] ? "SECURE" : "AT RISK") { style = { color = shSec[i] ? cGreen : cOrange, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                safeList.Add(row);
            }

            // --- RECENT ACTIVITY ---
            var actList = panel.Q<ScrollView>("CO_RecentActivityList");
            actList.Clear();
            string[] raTxt = { "Operation Market Shift: Economic sanctions on target network initiated.", "Operation Whisper: Propaganda package successfully distributed.", "Operation Iron Shield: Asset recruited in Chad Basin.", "Operation Darknet: Access to server node established.", "Operation Ghost cancelled due to high detection risk." };
            string[] raTime = { "16 JAN 2026, 08:45", "16 JAN 2026, 07:30", "15 JAN 2026, 22:10", "15 JAN 2026, 18:40", "15 JAN 2026, 14:05" };
            Color[] raCol = { cOrange, cPurple, cBlue, cGreen, cRed };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
                var iconBox = new VisualElement { style = { width = 16, height = 16, borderTopLeftRadius = 8, borderTopRightRadius = 8, borderBottomLeftRadius = 8, borderBottomRightRadius = 8, backgroundColor = new Color(raCol[i].r, raCol[i].g, raCol[i].b, 0.2f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 8 } };
                iconBox.Add(new Label("&#xf023;") { style = { color = raCol[i], fontSize = 8 } }.WithClass("fa-icon"));
                row.Add(iconBox);
                
                var rtRow = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                rtRow.Add(new Label(raTxt[i]) { style = { color = Color.grey, fontSize = 8, whiteSpace = WhiteSpace.Normal, width = Length.Percent(70) } });
                rtRow.Add(new Label(raTime[i]) { style = { color = Color.grey, fontSize = 7 } });
                row.Add(rtRow);
                actList.Add(row);
            }

            // --- RECENT OUTCOMES ---
            var outList = panel.Q<ScrollView>("CO_RecentOutcomesList");
            outList.Clear();
            string[] roOps = { "Operation Deep Net", "Operation White Noise", "Operation Broken Chain", "Operation Silent Echo" };
            string[] roCats = { "Cyber Operation", "Influence Operation", "Economic Operation", "Political Operation" };
            string[] roRes = { "SUCCESS", "SUCCESS", "PARTIAL", "FAILED" };
            string[] roDates = { "14 JAN 2026", "13 JAN 2026", "12 JAN 2026", "11 JAN 2026" };
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.03f), paddingBottom = 6, paddingTop = 6, paddingLeft = 8, paddingRight = 8, marginBottom = 4, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 120 } };
                rL.Add(new Label("&#xf21b;") { style = { color = Color.white, fontSize = 10, marginRight = 6 } }.WithClass("fa-icon"));
                rL.Add(new Label(roOps[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                row.Add(rL);
                
                row.Add(new Label(roCats[i]) { style = { color = Color.grey, fontSize = 7, width = 80 } });
                
                var resCol = roRes[i] == "SUCCESS" ? cGreen : (roRes[i] == "PARTIAL" ? cOrange : cRed);
                var rStRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 60 } };
                rStRow.Add(new Label(roRes[i] == "SUCCESS" ? "&#xf058;" : (roRes[i] == "PARTIAL" ? "&#xf071;" : "&#xf057;")) { style = { color = resCol, fontSize = 8, marginRight = 4 } }.WithClass("fa-icon"));
                rStRow.Add(new Label(roRes[i]) { style = { color = resCol, fontSize = 7, unityFontStyleAndWeight = FontStyle.Bold } });
                row.Add(rStRow);
                
                row.Add(new Label(roDates[i]) { style = { color = Color.grey, fontSize = 7 } });
                outList.Add(row);
            }

            // --- QUICK ACTIONS ---
            var qac = panel.Q<VisualElement>("CO_QuickActionsContainer");
            qac.Clear();
            string[] qaTxt = { "LAUNCH NEW OPERATION", "RECRUIT ASSET", "EXTRACT ASSET", "SECURE SAFE HOUSE", "DISINFORMATION CAMPAIGN", "CYBER INTRUSION" };
            string[] qaIco = { "&#xf067;", "&#xf21b;", "&#xf072;", "&#xf132;", "&#xf0a1;", "&#xf108;" };
            for(int i=0; i<6; i++)
            {
                var btn = new Button { style = { width = Length.Percent(48), height = 28, flexDirection = FlexDirection.Row, justifyContent = Justify.FlexStart, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.05f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = cWhiteAlpha, borderBottomColor = cWhiteAlpha, borderLeftColor = cWhiteAlpha, borderRightColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginBottom = 6, paddingLeft = 12 } };
                btn.Add(new Label(qaIco[i]) { style = { color = cGreen, fontSize = 10, marginRight = 8, width = 16 } }.WithClass("fa-icon"));
                btn.Add(new Label(qaTxt[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                qac.Add(btn);
            }
        }

        private VisualElement CreateDetailStat(string label, string value)
        {
            var box = new VisualElement { style = { width = Length.Percent(50), marginBottom = 8 } };
            box.Add(new Label(label) { style = { color = Color.grey, fontSize = 7, marginBottom = 2 } });
            box.Add(new Label(value) { style = { color = Color.white, fontSize = 8, whiteSpace = WhiteSpace.Normal } });
            return box;
        }

        private void LaunchSampleCovertOp()
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;

            // Pick a valid rival country to target
            var rivals = gm.allCountries.Where(c => c != country && gm.WarSystem != null && gm.WarSystem.GetWarStage(c) != WarStage.Peace).ToList();
            if (rivals.Count == 0) rivals = gm.allCountries.Where(c => c != country).ToList();
            var targetCountry = rivals.Count > 0 ? rivals[Random.Range(0, rivals.Count)].countryName : "Unknown";

            string[] types = { "Sabotage", "Propaganda", "Asset Recruitment", "Assassination" };
            
            float opCost = 0.1f; // $100M per op
            var gm3 = GameManager.Instance;
            if (gm3 != null && gm3.playerBudget < opCost)
            {
                NotificationManager.Instance?.AddNotification("INSUFFICIENT FUNDS: Need $0.1B to launch a covert operation.", Color.red);
                return;
            }
            if (country.operativeCount < 1)
            {
                NotificationManager.Instance?.AddNotification("NO OPERATIVES AVAILABLE: Recruit or wait for agents to return.", Color.red);
                return;
            }

            if (gm3 != null) { gm3.playerBudget -= opCost; country.treasury = gm3.playerBudget; }

            string opType = types[Random.Range(0, types.Length)];
            string riskLevel = targetCountry == "Unknown" ? "Low" : (country.globalIntelCoverage > 60f ? "Low" : country.globalIntelCoverage > 30f ? "Medium" : "High");
            float baseDetection = riskLevel == "High" ? 25f : riskLevel == "Medium" ? 15f : 8f;

            var op = new CovertOperation
            {
                opName = $"Operation {(char)Random.Range(65, 91)}{Random.Range(10, 99)}",
                type = opType,
                target = targetCountry,
                operativeName = $"Agent {Random.Range(100, 999)}",
                riskLevel = riskLevel,
                status = "Infiltration",
                detectionRisk = baseDetection,
                currentPhase = 1,
                totalPhases = 3,
                monthsRemaining = 3
            };
            
            country.activeCovertOps.Add(op);
            country.operativeCount--;
            country.exposureRisk = Mathf.Min(100f, country.exposureRisk + 5f);
            NotificationManager.Instance?.AddNotification($"OPERATION LAUNCHED ({riskLevel} risk, $0.1B): {op.opName} targeting {targetCountry}. Type: {opType}.", new Color(186/255f, 104/255f, 200/255f));
            RefreshIntelligenceData();
        }

        private void RunInternalAudit()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            var gm2 = GameManager.Instance;
            float auditCost = 0.05f; // $50M
            if (gm2 != null && gm2.playerBudget < auditCost)
            {
                NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need $0.05B to run internal audit.", Color.red);
                return;
            }
            if (gm2 != null) { gm2.playerBudget -= auditCost; country.treasury = gm2.playerBudget; }
            country.agencyIntegrity = Mathf.Min(100f, country.agencyIntegrity + 10f);
            country.foreignAgentsDetected = Mathf.Max(0, country.foreignAgentsDetected - 1);
            country.exposureRisk = Mathf.Max(0f, country.exposureRisk - 8f);
            NotificationManager.Instance?.AddNotification($"AUDIT COMPLETE: Agency integrity +10%, Exposure risk -8%, cost $0.05B.", new Color(0, 210/255f, 1f));
            NewsSystem.Instance?.PublishNews($"{country.countryName} intelligence service completes internal security audit.");
            RefreshIntelligenceData();
        }

        private void PopulateOverviewTab(CountryDataSO country)
        {
            if (_overviewIntelSummaryContainer == null) return;

            // 1. INTEL SUMMARY
            _overviewIntelSummaryContainer.Clear();
            AddOverviewSummaryRow("Intel Level", country.intelligenceLevel > 75 ? "HIGH" : country.intelligenceLevel > 40 ? "MEDIUM" : "LOW", country.intelligenceLevel > 75 ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f), FaIcons.BullsEye);
            AddOverviewSummaryRowWithBar("Overall Coverage", country.globalIntelCoverage, FaIcons.Globe);
            AddOverviewSummaryRow("Active Agents", country.operativeCount.ToString(), Color.white, FaIcons.UserSecret);
            AddOverviewSummaryRow("Satellite Status", country.satelliteCount > 0 ? "ONLINE" : "OFFLINE", country.satelliteCount > 0 ? new Color(0.14f, 0.93f, 0.34f) : Color.grey, FaIcons.Microchip);
            AddOverviewSummaryRowWithBar("Border Surveillance", country.security, FaIcons.Road, new Color(0.24f, 0.53f, 0.94f));
            AddOverviewSummaryRowWithBar("Cyber Security", Mathf.Clamp(country.intelligenceLevel * 0.9f, 0, 100), FaIcons.Microchip, new Color(0.73f, 0.41f, 0.78f));
            float sigintLevel = Mathf.Clamp(country.intelligenceLevel * 0.8f + country.satelliteCount * 10f, 0, 100);
            AddOverviewSummaryRow("Signal Intercepts", sigintLevel > 70 ? "HIGH" : "MODERATE", sigintLevel > 70 ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f), FaIcons.Bolt);
            
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            AddOverviewSummaryRow("Intel Budget", $"{UiFormat.Money(country.intelligenceBudget, sym)} / month", Color.white, FaIcons.Clock, true);

            // 2. ACTIVE OPERATIONS
            if (_overviewActiveOpsList != null)
            {
                _overviewActiveOpsList.Clear();
                // Add mock ops to match mockup
                AddOverviewActiveOp("Operation Eagle Eye", "Border Surveillance", "ACTIVE", new Color(0.14f, 0.93f, 0.34f), FaIcons.Bolt);
                AddOverviewActiveOp("Border Watch", "Northern Border", "ACTIVE", new Color(0.14f, 0.93f, 0.34f), FaIcons.Bolt);
                AddOverviewActiveOp("Gulf Shield", "Maritime Surveillance", "ACTIVE", new Color(0.14f, 0.93f, 0.34f), FaIcons.Shield);
                AddOverviewActiveOp("Cyber Shield", "National Cyber Defense", "ACTIVE", new Color(0.14f, 0.93f, 0.34f), FaIcons.Shield);
            }

            // 3. RECENT REPORTS
            if (_overviewRecentReportsList != null)
            {
                _overviewRecentReportsList.Clear();
                // Add mock reports
                AddOverviewRecentReport("Border Incursion Detected", "Northern Nigeria", "Armed militants observed crossing from Niger near Katsina region.", "08 Jan 2026\n07:45", "HIGH", new Color(1f, 0.2f, 0.2f), FaIcons.Shield, new Color(0.14f, 0.93f, 0.34f));
                AddOverviewRecentReport("Political Unrest", "Cameroon", "Growing public discontent due to economic hardship and political instability.", "08 Jan 2026\n06:20", "MEDIUM", new Color(1f, 0.67f, 0.25f), FaIcons.UserSecret, new Color(1f, 0.67f, 0.25f));
                AddOverviewRecentReport("Pirate Activity Increase", "Gulf of Guinea", "Increase in pirate attacks on commercial vessels near Delta region.", "08 Jan 2026\n05:10", "MEDIUM", new Color(1f, 0.67f, 0.25f), FaIcons.TriangleExclaim, new Color(0.24f, 0.53f, 0.94f));
                AddOverviewRecentReport("Military Build-up", "Niger", "Heavy military equipment movement observed near Diffa region.", "08 Jan 2026\n04:30", "HIGH", new Color(1f, 0.2f, 0.2f), FaIcons.CircleXMark, new Color(1f, 0.2f, 0.2f));
            }

            // 4. COVERAGE GRID
            if (_overviewCoverageGrid != null)
            {
                _overviewCoverageGrid.Clear();
                AddOverviewCoverageItem("BORDER AREAS", 81, "GOOD", FaIcons.Road, new Color(0.14f, 0.93f, 0.34f));
                AddOverviewCoverageItem("MARITIME ZONE", 66, "FAIR", FaIcons.TriangleExclaim, new Color(1f, 0.67f, 0.25f));
                AddOverviewCoverageItem("CYBER SPACE", 62, "FAIR", FaIcons.Microchip, new Color(0.73f, 0.41f, 0.78f));
                AddOverviewCoverageItem("POLITICAL", 74, "GOOD", FaIcons.Landmark, new Color(0.14f, 0.93f, 0.34f));
                AddOverviewCoverageItem("ECONOMIC", 58, "FAIR", FaIcons.ChartLine, new Color(1f, 0.67f, 0.25f));
                AddOverviewCoverageItem("MILITARY", 77, "GOOD", FaIcons.Shield, new Color(0.14f, 0.93f, 0.34f));
            }

            // 5. THREAT ALERTS
            if (_overviewThreatsList != null)
            {
                _overviewThreatsList.Clear();
                AddOverviewThreatAlert("Border Breach Risk", "Northern Border", 78, "High", FaIcons.Shield, new Color(1f, 0.2f, 0.2f));
                AddOverviewThreatAlert("Insurgency Risk", "Northern Region", 52, "Medium", FaIcons.Fire, new Color(1f, 0.67f, 0.25f));
                AddOverviewThreatAlert("Military Build-up", "Niger", 87, "High", FaIcons.CircleXMark, new Color(1f, 0.2f, 0.2f));
                AddOverviewThreatAlert("Pirate Activity", "Gulf of Guinea", 48, "Medium", FaIcons.TriangleExclaim, new Color(1f, 0.67f, 0.25f));
                AddOverviewThreatAlert("Economic Espionage", "Multiple Targets", 28, "Low", FaIcons.Microchip, new Color(0.24f, 0.53f, 0.94f));
            }

            // 6. FOOTER METRICS
            if (_overviewFooterContainer != null)
            {
                _overviewFooterContainer.Clear();
                AddOverviewFooterMetric("Last Intel Update", "08 Jan 2026, 09:05", FaIcons.Clock, Color.white);
                AddOverviewFooterMetric("Intel Accuracy", "87%", FaIcons.BullsEye, new Color(0.14f, 0.93f, 0.34f));
                AddOverviewFooterMetric("Intel Influence", "STRONG", FaIcons.UserSecret, new Color(0.14f, 0.93f, 0.34f));
                AddOverviewFooterMetric("Data Intercepts (24h)", "1,248", FaIcons.ChartLine, Color.white);
                AddOverviewFooterMetric("Active Assets", "23", FaIcons.Bolt, Color.white);
            }
        }

        private void AddOverviewSummaryRow(string label, string value, Color valColor, string icon, bool isBottom = false)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = isBottom ? 0 : 8 } };
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var iconLabel = new Label(icon) { style = { fontSize = 10, color = Color.grey, width = 16 } };
            iconLabel.AddToClassList("fa-icon");
            left.Add(iconLabel);
            left.Add(new Label(label) { style = { fontSize = 10, color = Color.grey } });
            row.Add(left);
            row.Add(new Label(value) { style = { fontSize = 10, color = valColor, unityFontStyleAndWeight = FontStyle.Bold } });
            _overviewIntelSummaryContainer.Add(row);
        }

        private void AddOverviewSummaryRowWithBar(string label, float value, string icon, Color? barColor = null)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var iconLabel = new Label(icon) { style = { fontSize = 10, color = Color.grey, width = 16 } };
            iconLabel.AddToClassList("fa-icon");
            left.Add(iconLabel);
            left.Add(new Label(label) { style = { fontSize = 10, color = Color.grey } });
            row.Add(left);
            
            var right = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 70, justifyContent = Justify.FlexEnd } };
            right.Add(new Label($"{value:F0}%") { style = { fontSize = 10, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } });
            var pBar = new ProgressBar { value = value, highValue = 100, style = { width = 35, height = 4 } };
            if (barColor.HasValue) pBar.style.backgroundColor = barColor.Value;
            right.Add(pBar);
            row.Add(right);
            
            _overviewIntelSummaryContainer.Add(row);
        }

        private void AddOverviewActiveOp(string opName, string region, string status, Color statusColor, string icon)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 12 } };
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            
            var iconBox = new VisualElement { style = { width = 24, height = 24, borderTopLeftRadius = 12, borderTopRightRadius = 12, borderBottomLeftRadius = 12, borderBottomRightRadius = 12, backgroundColor = new Color(0.1f, 0.15f, 0.25f), justifyContent = Justify.Center, alignItems = Align.Center, marginRight = 10 } };
            var iconLabel = new Label(icon) { style = { fontSize = 12, color = new Color(0.73f, 0.41f, 0.78f) } };
            iconLabel.AddToClassList("fa-icon");
            iconBox.Add(iconLabel);
            left.Add(iconBox);
            
            var textCol = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            textCol.Add(new Label(opName) { style = { fontSize = 10, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            textCol.Add(new Label(region) { style = { fontSize = 9, color = Color.grey } });
            left.Add(textCol);
            
            row.Add(left);
            row.Add(new Label(status) { style = { fontSize = 9, color = statusColor, unityFontStyleAndWeight = FontStyle.Bold } });
            
            _overviewActiveOpsList.Add(row);
        }

        private void AddOverviewRecentReport(string title, string region, string desc, string time, string severity, Color sevColor, string icon, Color iconColor)
        {
            var card = new VisualElement { style = { flexDirection = FlexDirection.Row, backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderBottomColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderLeftColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderRightColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingBottom = 10, paddingTop = 10, paddingLeft = 12, paddingRight = 12, marginBottom = 8 } };
            
            var iconBox = new VisualElement { style = { width = 36, height = 36, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(iconColor.r, iconColor.g, iconColor.b, 0.1f), justifyContent = Justify.Center, alignItems = Align.Center, marginRight = 12, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(iconColor.r, iconColor.g, iconColor.b, 0.3f), borderBottomColor = new Color(iconColor.r, iconColor.g, iconColor.b, 0.3f), borderLeftColor = new Color(iconColor.r, iconColor.g, iconColor.b, 0.3f), borderRightColor = new Color(iconColor.r, iconColor.g, iconColor.b, 0.3f) } };
            var iconLabel = new Label(icon) { style = { fontSize = 18, color = iconColor } };
            iconLabel.AddToClassList("fa-icon");
            iconBox.Add(iconLabel);
            card.Add(iconBox);
            
            var mid = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column, justifyContent = Justify.Center } };
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexEnd, marginBottom = 2 } };
            header.Add(new Label(title) { style = { fontSize = 11, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } });
            header.Add(new Label(region) { style = { fontSize = 9, color = Color.grey } });
            mid.Add(header);
            mid.Add(new Label(desc) { style = { fontSize = 9, color = new Color(0.6f, 0.65f, 0.7f), whiteSpace = WhiteSpace.Normal } });
            card.Add(mid);
            
            var right = new VisualElement { style = { flexDirection = FlexDirection.Column, alignItems = Align.FlexEnd, justifyContent = Justify.SpaceBetween, width = 60 } };
            right.Add(new Label(time) { style = { fontSize = 8, color = Color.grey, unityTextAlign = TextAnchor.UpperRight } });
            var badge = new Label(severity) { style = { fontSize = 8, color = sevColor, unityFontStyleAndWeight = FontStyle.Bold, backgroundColor = new Color(sevColor.r, sevColor.g, sevColor.b, 0.15f), paddingTop = 2, paddingBottom = 2, paddingLeft = 6, paddingRight = 6, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(sevColor.r, sevColor.g, sevColor.b, 0.3f), borderBottomColor = new Color(sevColor.r, sevColor.g, sevColor.b, 0.3f), borderLeftColor = new Color(sevColor.r, sevColor.g, sevColor.b, 0.3f), borderRightColor = new Color(sevColor.r, sevColor.g, sevColor.b, 0.3f) } };
            right.Add(badge);
            card.Add(right);
            
            _overviewRecentReportsList.Add(card);
        }

        private void AddOverviewCoverageItem(string label, float val, string status, string icon, Color color)
        {
            var col = new VisualElement { style = { flexDirection = FlexDirection.Column, alignItems = Align.Center, width = Length.Percent(15) } };
            col.Add(new Label(label) { style = { fontSize = 8, color = Color.grey, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 6, whiteSpace = WhiteSpace.Normal } });
            col.Add(new Label($"{val:F0}%") { style = { fontSize = 14, color = color, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            col.Add(new Label(status) { style = { fontSize = 9, color = color, marginBottom = 6 } });
            var pBar = new ProgressBar { value = val, highValue = 100, style = { width = 30, height = 3, marginBottom = 8 } };
            col.Add(pBar);
            var iconLabel = new Label(icon) { style = { fontSize = 16, color = Color.grey } };
            iconLabel.AddToClassList("fa-icon");
            col.Add(iconLabel);
            _overviewCoverageGrid.Add(col);
        }

        private void AddOverviewThreatAlert(string title, string region, float prob, string impact, string icon, Color color)
        {
            var card = new VisualElement { style = { flexDirection = FlexDirection.Row, backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderBottomColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderLeftColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderRightColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingBottom = 12, paddingTop = 12, paddingLeft = 12, paddingRight = 12, marginBottom = 8, alignItems = Align.Center } };
            
            var iconBox = new VisualElement { style = { width = 36, height = 36, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(color.r, color.g, color.b, 0.1f), justifyContent = Justify.Center, alignItems = Align.Center, marginRight = 12, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(color.r, color.g, color.b, 0.3f), borderBottomColor = new Color(color.r, color.g, color.b, 0.3f), borderLeftColor = new Color(color.r, color.g, color.b, 0.3f), borderRightColor = new Color(color.r, color.g, color.b, 0.3f) } };
            var iconLabel = new Label(icon) { style = { fontSize = 18, color = color } };
            iconLabel.AddToClassList("fa-icon");
            iconBox.Add(iconLabel);
            card.Add(iconBox);
            
            var mid = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column, justifyContent = Justify.Center } };
            mid.Add(new Label(title) { style = { fontSize = 11, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            mid.Add(new Label(region) { style = { fontSize = 9, color = Color.grey, marginBottom = 4 } });
            
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(new Label($"Probability: {prob}%") { style = { fontSize = 8, color = color, marginRight = 8 } });
            row.Add(new Label($"Impact: {impact}") { style = { fontSize = 8, color = color } });
            mid.Add(row);
            
            card.Add(mid);
            card.Add(new Label("›") { style = { fontSize = 16, color = Color.grey } });
            
            _overviewThreatsList.Add(card);
        }

        private void AddOverviewFooterMetric(string label, string val, string icon, Color color)
        {
            var el = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var iconLabel = new Label(icon) { style = { fontSize = 12, color = Color.grey, marginRight = 6 } };
            iconLabel.AddToClassList("fa-icon");
            el.Add(iconLabel);
            var col = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            col.Add(new Label(label) { style = { fontSize = 8, color = Color.grey } });
            col.Add(new Label(val) { style = { fontSize = 10, color = color, unityFontStyleAndWeight = FontStyle.Bold } });
            el.Add(col);
            _overviewFooterContainer.Add(el);
        }

        private void LaunchSatellite()
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;
            if (country.satelliteCount + country.satellitesUnderConstruction >= 4) return;
            
            float cost = 0.5f; // $500M
            if (gm.playerBudget < cost)
            {
                NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need $0.5B to build spy satellite.", Color.red);
                return;
            }

            gm.playerBudget -= cost;
            country.treasury = gm.playerBudget;
            country.satellitesUnderConstruction++;
            country.satelliteBuildMonthsRemaining += 6;
            
            NotificationManager.Instance?.AddNotification($"SPY SATELLITE COMMISSIONED. Construction will take {country.satelliteBuildMonthsRemaining} months.", new Color(186/255f, 104/255f, 200/255f));
            
            RefreshIntelligenceData();
        }

        private void PopulateThreatAssessmentTab(CountryDataSO country)
        {
            var panel = _dashboardRoot?.Q<VisualElement>("PanelThreatBoard");
            if (panel == null) return;

            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cPurple = new Color(0.73f, 0.41f, 0.78f);
            var cBlue = new Color(0.18f, 0.6f, 0.95f);
            var cWhiteAlpha = new Color(1, 1, 1, 0.1f);

            // --- KPIs ---
            panel.Q<Label>("TA_ActiveThreats").text = "18";
            panel.Q<Label>("TA_CriticalThreats").text = "4";
            panel.Q<Label>("TA_CountriesConcern").text = "7";

            // --- THREAT MATRIX ---
            var matrix = panel.Q<VisualElement>("TA_MatrixGrid");
            matrix.Clear();
            Color[] mColors = { cGreen, cGreen, cOrange, cOrange,
                                cGreen, cOrange, cOrange, cRed,
                                cOrange, cOrange, cRed, cRed,
                                cOrange, cRed, cRed, cRed };
            for(int i=0; i<16; i++)
            {
                var box = new VisualElement { style = { width = Length.Percent(23), height = 24, marginRight = Length.Percent(2), marginBottom = 6, backgroundColor = mColors[i], borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, alignItems = Align.Center, justifyContent = Justify.Center } };
                if (i == 3 || i == 7 || i == 11 || i == 15) box.style.marginRight = 0; // last column
                box.Add(new Label($"{i+1}") { style = { color = new Color(1,1,1,0.5f), fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                matrix.Add(box);
            }

            // --- TOP THREATS ---
            var tList = panel.Q<ScrollView>("TA_TopThreatsList");
            tList.Clear();
            string[] tNames = { "Insurgent Activity", "Political Instability", "Economic Collapse", "Cyber Warfare", "Ethno-Religious Conflict" };
            string[] tCats = { "Terrorism", "Political", "Economic", "Cyber", "Social" };
            string[] tRisks = { "CRITICAL", "HIGH", "HIGH", "HIGH", "MEDIUM" };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingBottom = 6, paddingTop = 6, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                row.Add(new Label((i+1).ToString()) { style = { color = Color.grey, fontSize = 8, width = 16 } });
                row.Add(new Label(tNames[i]) { style = { color = Color.white, fontSize = 8, width = 124 } });
                row.Add(new Label(tCats[i]) { style = { color = Color.grey, fontSize = 8, flexGrow = 1 } });
                
                // Impact dots
                var impCol = new VisualElement { style = { flexDirection = FlexDirection.Row, width = 60 } };
                for(int d=0; d<4; d++) impCol.Add(new VisualElement { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = (d < (5-i)) ? (tRisks[i] == "CRITICAL" ? cRed : cOrange) : cWhiteAlpha, marginRight = 2 } });
                row.Add(impCol);
                
                // Likelihood dots
                var likCol = new VisualElement { style = { flexDirection = FlexDirection.Row, width = 60 } };
                for(int d=0; d<4; d++) likCol.Add(new VisualElement { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = (d < (4-i)) ? (tRisks[i] == "CRITICAL" ? cRed : cOrange) : cWhiteAlpha, marginRight = 2 } });
                row.Add(likCol);
                
                // Trend
                row.Add(new Label(tRisks[i] == "MEDIUM" ? "→" : "↑") { style = { color = tRisks[i] == "MEDIUM" ? cOrange : cRed, fontSize = 10, width = 40, unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold } });
                
                // Risk Level
                row.Add(new Label(tRisks[i]) { style = { color = tRisks[i] == "CRITICAL" ? cRed : cOrange, fontSize = 8, width = 60, unityTextAlign = TextAnchor.MiddleRight, unityFontStyleAndWeight = FontStyle.Bold } });
                tList.Add(row);
            }

            // --- CATEGORIES ---
            panel.Q<Label>("TA_CatDonut").text = "18";
            var cList = panel.Q<VisualElement>("TA_CategoryList");
            cList.Clear();
            string[] cNames = { "Terrorism", "Political", "Economic", "Cyber" };
            Color[] cColors = { cRed, cOrange, cOrange, cPurple };
            string[] cVals = { "4 (22%)", "3 (17%)", "3 (17%)", "2 (11%)" };
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 4 } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new VisualElement { style = { width = 6, height = 6, backgroundColor = cColors[i], marginRight = 6 } });
                rL.Add(new Label(cNames[i]) { style = { color = Color.white, fontSize = 8 } });
                row.Add(rL);
                row.Add(new Label(cVals[i]) { style = { color = Color.grey, fontSize = 8 } });
                cList.Add(row);
            }

            // --- REGIONAL THREAT OVERVIEW ---
            var rList = panel.Q<VisualElement>("TA_RegionList");
            rList.Clear();
            string[] rNames = { "North-East Region", "North-West Region", "North-Central Region", "South-South Region", "South-East Region", "South-West Region" };
            string[] rLvls = { "VERY HIGH", "HIGH", "MEDIUM", "MEDIUM", "LOW", "LOW" };
            int[] rVals = { 85, 72, 48, 46, 28, 26 };
            for(int i=0; i<6; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                row.Add(new Label(rNames[i]) { style = { color = Color.grey, fontSize = 8, width = 90 } });
                
                var rCol = rLvls[i] == "VERY HIGH" ? cRed : (rLvls[i] == "HIGH" ? cOrange : (rLvls[i] == "MEDIUM" ? cOrange : cGreen));
                row.Add(new Label(rLvls[i]) { style = { color = rCol, fontSize = 7, width = 50, unityFontStyleAndWeight = FontStyle.Bold } });
                
                var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                barBg.Add(new VisualElement { style = { width = Length.Percent(rVals[i]), height = 4, backgroundColor = rCol, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                row.Add(barBg);
                
                row.Add(new Label($"{rVals[i]}%") { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, width = 25, unityTextAlign = TextAnchor.MiddleRight } });
                rList.Add(row);
            }

            // --- THREAT LEVEL OVER TIME (Bar Chart instead of Line Graph) ---
            var chart = panel.Q<VisualElement>("TA_TrendChart");
            chart.Clear();
            for(int i=0; i<20; i++)
            {
                var barCont = new VisualElement { style = { height = Length.Percent(100), width = 12, justifyContent = Justify.FlexEnd, alignItems = Align.Center } };
                float val = UnityEngine.Random.Range(30f, 90f);
                var bar = new VisualElement { style = { width = 6, height = Length.Percent(val), backgroundColor = val > 75 ? cRed : cOrange, borderTopLeftRadius = 2, borderTopRightRadius = 2 } };
                barCont.Add(bar);
                if (i % 5 == 0) barCont.Add(new Label($"{i+1} JAN") { style = { color = Color.grey, fontSize = 6, marginTop = 4 } });
                else barCont.Add(new Label("") { style = { fontSize = 6, marginTop = 4 } });
                chart.Add(barCont);
            }

            // --- COUNTRIES OF CONCERN ---
            var coList = panel.Q<ScrollView>("TA_CountriesList");
            coList.Clear();
            string[] coNames = { "Niger", "Cameroon", "Chad", "Libya", "Mali", "Benin", "Ghana" };
            string[] coLvls = { "HIGH", "MEDIUM", "MEDIUM", "HIGH", "MEDIUM", "LOW", "LOW" };
            string[] coReas = { "Insurgent Safe Haven", "Cross-border Arms Flow", "Militant Movement", "Arms Proliferation", "Terrorist Training Camps", "Smuggling Routes", "Cyber Threat Origin" };
            for(int i=0; i<7; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingBottom = 6, paddingTop = 6, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                row.Add(new Label(coNames[i]) { style = { color = Color.white, fontSize = 8, width = 70 } });
                var coCol = coLvls[i] == "HIGH" ? cRed : (coLvls[i] == "MEDIUM" ? cOrange : cGreen);
                row.Add(new Label(coLvls[i]) { style = { color = coCol, fontSize = 8, width = 80, unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold } });
                row.Add(new Label(coReas[i]) { style = { color = Color.grey, fontSize = 8, flexGrow = 1 } });
                row.Add(new Label(coLvls[i] == "LOW" ? "↓" : "↑") { style = { color = coLvls[i] == "LOW" ? cGreen : cRed, fontSize = 10, width = 30, unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold } });
                coList.Add(row);
            }

            // --- CRITICAL THREATS ---
            var crList = panel.Q<VisualElement>("TA_CriticalCards");
            crList.Clear();
            string[] crNames = { "Insurgent Activity", "Political Instability", "Economic Collapse", "Cyber Warfare" };
            string[] crCats = { "Terrorism", "Political", "Economic", "Cyber" };
            string[] crIcons = { "&#xf071;", "&#xf19c;", "&#xf201;", "&#xf108;" };
            string[] crDesc = { "Increase in attacks and militant recruitment in North-East region.", "Rising political tensions could lead to civil unrest in major cities.", "High inflation and debt levels pose risk to economic stability.", "Increasing attempts to disrupt critical government infrastructure." };
            for(int i=0; i<4; i++)
            {
                var card = new VisualElement { style = { width = Length.Percent(24), backgroundColor = new Color(1,1,1,0.03f), paddingTop = 12, paddingBottom = 12, paddingLeft = 12, paddingRight = 12, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, borderTopWidth = 2, borderTopColor = cRed } };
                
                var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
                topRow.Add(new Label(crIcons[i]) { style = { color = cRed, fontSize = 16, marginRight = 8 } }.WithClass("fa-icon"));
                var topT = new VisualElement();
                topT.Add(new Label(crNames[i]) { style = { color = Color.white, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                topT.Add(new Label(crCats[i]) { style = { color = Color.grey, fontSize = 7 } });
                topRow.Add(topT);
                card.Add(topRow);
                
                card.Add(new Label(crDesc[i]) { style = { color = Color.grey, fontSize = 8, whiteSpace = WhiteSpace.Normal, height = 40 } });
                card.Add(new Label("RISK LEVEL") { style = { color = Color.grey, fontSize = 7, marginTop = 8 } });
                card.Add(new Label("CRITICAL") { style = { color = cRed, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8 } });
                
                card.Add(new Button { text = "VIEW DETAILS", style = { height = 24, fontSize = 8, backgroundColor = new Color(1,1,1,0.05f), color = Color.white, borderTopColor = cWhiteAlpha, borderBottomColor = cWhiteAlpha, borderLeftColor = cWhiteAlpha, borderRightColor = cWhiteAlpha, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1 } });
                crList.Add(card);
            }

            // --- MITIGATION PROGRESS ---
            var mList = panel.Q<ScrollView>("TA_MitigationList");
            mList.Clear();
            string[] mNames = { "Counter Terrorism Operations", "Intelligence Sharing", "Cyber Defense Enhancement", "Border Security Reinforcement", "Economic Stabilization Measures", "Community Engagement Programs" };
            int[] mVals = { 72, 68, 55, 45, 38, 32 };
            Color[] mColors2 = { cGreen, cGreen, cBlue, cOrange, cOrange, cRed };
            for(int i=0; i<6; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                row.Add(new Label(mNames[i]) { style = { color = Color.grey, fontSize = 8, flexGrow = 1 } });
                
                var barBg = new VisualElement { style = { width = 100, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                barBg.Add(new VisualElement { style = { width = Length.Percent(mVals[i]), height = 4, backgroundColor = mColors2[i], borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                row.Add(barBg);
                
                row.Add(new Label($"{mVals[i]}%") { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, width = 25, unityTextAlign = TextAnchor.MiddleRight } });
                mList.Add(row);
            }

            // --- RECOMMENDATIONS ---
            var recList = panel.Q<ScrollView>("TA_RecommendationsList");
            recList.Clear();
            string[] recTxt = { "Increase surveillance in North-East region.", "Strengthen intelligence cooperation with Niger.", "Address economic vulnerabilities urgently.", "Enhance cybersecurity infrastructure.", "Monitor political developments closely." };
            bool[] recIsWarn = { false, false, true, false, true };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
                var icon = new Label(recIsWarn[i] ? "&#xf071;" : "&#xf058;") { style = { color = recIsWarn[i] ? cOrange : cGreen, fontSize = 10, marginRight = 8 } }.WithClass("fa-icon");
                row.Add(icon);
                row.Add(new Label(recTxt[i]) { style = { color = Color.white, fontSize = 8, whiteSpace = WhiteSpace.Normal, flexGrow = 1 } });
                recList.Add(row);
            }
        }

        private void PopulateDataAnalysisTab(CountryDataSO country)
        {
            var panel = _dashboardRoot?.Q<VisualElement>("PanelDataAnalysis");
            if (panel == null) return;

            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cPurple = new Color(0.73f, 0.41f, 0.78f);
            var cBlue = new Color(0.18f, 0.6f, 0.95f);
            var cTeal = new Color(0.2f, 0.7f, 0.7f);
            var cWhiteAlpha = new Color(1, 1, 1, 0.1f);

            int intelLvl = Mathf.Clamp((int)country.intelligenceLevel, 10, 100);

            // --- KPIs ---
            panel.Q<Label>("DA_DataSources").text = "87";
            panel.Q<Label>("DA_DataPoints").text = "2.48M";
            panel.Q<Label>("DA_Accuracy").text = "78%";
            panel.Q<Label>("DA_Insights").text = "34";
            float budget = country.covertBudget; // We can use covert budget for analysis budget mockup
            panel.Q<Label>("DA_Budget").text = $"₦{Mathf.RoundToInt(budget * 0.8f)}M / month";
            panel.Q<Label>("DA_BudgetRem").text = $"₦{Mathf.RoundToInt(budget * 0.8f * 0.4f)}M remaining";

            // --- DATA SOURCES OVERVIEW ---
            var srcList = panel.Q<VisualElement>("DA_SourcesList");
            srcList.Clear();
            string[] sNames = { "Human Intelligence (HUMINT)", "Signals Intelligence (SIGINT)", "Imagery Intelligence (IMINT)", "Open Source Intelligence (OSINT)", "Cyber Intelligence (CYBINT)", "Financial Intelligence (FININT)", "Technical Intelligence (TECHINT)" };
            int[] sVals = { 68, 72, 64, 81, 59, 67, 53 };
            Color[] sCols = { cGreen, cBlue, cOrange, cTeal, cPurple, cRed, cOrange };
            for(int i=0; i<7; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                row.Add(new Label(sNames[i]) { style = { color = Color.grey, fontSize = 8, width = 110, whiteSpace = WhiteSpace.Normal } });
                
                var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                barBg.Add(new VisualElement { style = { width = Length.Percent(sVals[i]), height = 4, backgroundColor = sCols[i], borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                row.Add(barBg);
                
                row.Add(new Label($"{sVals[i]}%") { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, width = 25, unityTextAlign = TextAnchor.MiddleRight } });
                srcList.Add(row);
            }

            // --- INTELLIGENCE TRENDS ---
            var chart = panel.Q<VisualElement>("DA_TrendChart");
            chart.Clear();
            for(int i=0; i<30; i++)
            {
                var barCont = new VisualElement { style = { height = Length.Percent(100), width = 12, justifyContent = Justify.FlexEnd, alignItems = Align.Center } };
                float v1 = UnityEngine.Random.Range(10f, 25f);
                float v2 = UnityEngine.Random.Range(10f, 25f);
                float v3 = UnityEngine.Random.Range(10f, 25f);
                float v4 = UnityEngine.Random.Range(10f, 25f);
                float v5 = UnityEngine.Random.Range(10f, 25f);
                
                var colStack = new VisualElement { style = { width = 6, flexDirection = FlexDirection.Column, justifyContent = Justify.FlexEnd } };
                colStack.Add(new VisualElement { style = { height = Length.Percent(v5), backgroundColor = cRed, borderTopLeftRadius = 2, borderTopRightRadius = 2 } });
                colStack.Add(new VisualElement { style = { height = Length.Percent(v4), backgroundColor = cOrange } });
                colStack.Add(new VisualElement { style = { height = Length.Percent(v3), backgroundColor = cBlue } });
                colStack.Add(new VisualElement { style = { height = Length.Percent(v2), backgroundColor = cPurple } });
                colStack.Add(new VisualElement { style = { height = Length.Percent(v1), backgroundColor = cGreen } });
                barCont.Add(colStack);
                
                if (i % 6 == 0) barCont.Add(new Label($"{i+1} JAN") { style = { color = Color.grey, fontSize = 6, marginTop = 4 } });
                else barCont.Add(new Label("") { style = { fontSize = 6, marginTop = 4 } });
                chart.Add(barCont);
            }

            // --- KEY INSIGHTS ---
            var inList = panel.Q<ScrollView>("DA_InsightsList");
            inList.Clear();
            string[] iNames = { "Rising Political Tensions", "Economic Pressure Building", "Cyber Threat Activity Spike", "Smuggling Network Expansion" };
            string[] iDesc = { "Political instability indicators have increased by 24% in the last 30 days.", "Inflation and unemployment data suggest potential economic unrest.", "Cyber attacks targeting government systems up by 32% this week.", "New routes identified along northern corridor and coastal areas." };
            string[] iPri = { "HIGH PRIORITY", "MEDIUM PRIORITY", "HIGH PRIORITY", "MEDIUM PRIORITY" };
            string[] iDates = { "16 JAN 2026", "16 JAN 2026", "16 JAN 2026", "14 JAN 2026" };
            string[] iIco = { "&#xf062;", "&#xf155;", "&#xf51e;", "&#xf508;" };
            Color[] iCols = { cRed, cOrange, cPurple, cGreen };
            for(int i=0; i<4; i++)
            {
                var card = new VisualElement { style = { backgroundColor = new Color(1,1,1,0.03f), paddingTop = 12, paddingBottom = 12, paddingLeft = 12, paddingRight = 12, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, marginBottom = 8, borderLeftWidth = 2, borderLeftColor = iCols[i] } };
                
                var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
                var iconBox = new VisualElement { style = { width = 24, height = 24, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(iCols[i].r, iCols[i].g, iCols[i].b, 0.1f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 8 } };
                iconBox.Add(new Label(iIco[i]) { style = { color = iCols[i], fontSize = 10 } }.WithClass("fa-icon"));
                topRow.Add(iconBox);
                
                var tRight = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                tRight.Add(new Label(iNames[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                tRight.Add(new Label(iDates[i]) { style = { color = Color.grey, fontSize = 7 } });
                topRow.Add(tRight);
                card.Add(topRow);
                
                card.Add(new Label(iDesc[i]) { style = { color = Color.grey, fontSize = 8, whiteSpace = WhiteSpace.Normal, marginBottom = 6 } });
                card.Add(new Label(iPri[i]) { style = { color = iPri[i] == "HIGH PRIORITY" ? cRed : cOrange, fontSize = 7, unityFontStyleAndWeight = FontStyle.Bold } });
                inList.Add(card);
            }

            // --- DATA QUALITY ---
            panel.Q<Label>("DA_QualityDonut").text = "76%";
            var qList = panel.Q<VisualElement>("DA_QualityList");
            qList.Clear();
            string[] qNames = { "High Quality", "Medium Quality", "Low Quality", "Unverified" };
            Color[] qCols = { cGreen, cOrange, cRed, Color.grey };
            int[] qVals = { 41, 35, 16, 8 };
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 4 } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new VisualElement { style = { width = 6, height = 6, backgroundColor = qCols[i], marginRight = 6 } });
                rL.Add(new Label(qNames[i]) { style = { color = Color.white, fontSize = 8 } });
                row.Add(rL);
                row.Add(new Label($"{qVals[i]}%") { style = { color = Color.grey, fontSize = 8 } });
                qList.Add(row);
            }

            // --- GEOGRAPHIC FOCUS ---
            var gList = panel.Q<VisualElement>("DA_GeographicList");
            gList.Clear();
            string[] gNames = { "Northern Region", "Coastal Corridor", "Central Region", "Southern Region", "Border Areas" };
            string[] gLvls = { "High Activity", "High Activity", "Medium Activity", "Medium Activity", "Very High Activity" };
            int[] gVals = { 78, 65, 52, 47, 85 };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                row.Add(new Label(gNames[i]) { style = { color = Color.grey, fontSize = 8, width = 80 } });
                
                var rCol = gLvls[i] == "Very High Activity" ? cRed : (gLvls[i] == "High Activity" ? cRed : (gLvls[i] == "Medium Activity" ? cOrange : cGreen));
                row.Add(new Label(gLvls[i]) { style = { color = rCol, fontSize = 7, width = 70, unityFontStyleAndWeight = FontStyle.Bold } });
                
                var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                barBg.Add(new VisualElement { style = { width = Length.Percent(gVals[i]), height = 4, backgroundColor = rCol, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                row.Add(barBg);
                
                row.Add(new Label($"{gVals[i]}%") { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, width = 25, unityTextAlign = TextAnchor.MiddleRight } });
                gList.Add(row);
            }

            // --- PATTERN DETECTION ---
            var pList = panel.Q<ScrollView>("DA_PatternList");
            pList.Clear();
            string[] pNames = { "Unusual Financial Transactions", "Arms Movement Pattern", "Communications Anomaly", "Social Media Sentiment Shift", "Supply Chain Disruption" };
            string[] pStat = { "DETECTED", "DETECTED", "DETECTED", "MONITORING", "MONITORING" };
            string[] pIco = { "&#xf19c;", "&#xf132;", "&#xf09e;", "&#xf0ac;", "&#xf0d1;" };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, paddingBottom = 6, paddingTop = 6, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };
                rL.Add(new Label(pIco[i]) { style = { color = Color.white, fontSize = 10, marginRight = 8, width = 14 } }.WithClass("fa-icon"));
                rL.Add(new Label(pNames[i]) { style = { color = Color.white, fontSize = 8 } });
                row.Add(rL);
                
                var sCol = pStat[i] == "DETECTED" ? cRed : cOrange;
                row.Add(new Label(pStat[i]) { style = { color = sCol, fontSize = 7, unityFontStyleAndWeight = FontStyle.Bold, paddingTop = 4, paddingBottom = 4, paddingLeft = 4, paddingRight = 4, backgroundColor = new Color(sCol.r, sCol.g, sCol.b, 0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(sCol.r, sCol.g, sCol.b, 0.3f), borderBottomColor = new Color(sCol.r, sCol.g, sCol.b, 0.3f), borderLeftColor = new Color(sCol.r, sCol.g, sCol.b, 0.3f), borderRightColor = new Color(sCol.r, sCol.g, sCol.b, 0.3f), borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                pList.Add(row);
            }

            // --- DATA PROCESSING ---
            var prList = panel.Q<ScrollView>("DA_ProcessingList");
            prList.Clear();
            string[] prNames = { "Data Collection", "Data Processing", "Pattern Recognition", "Report Generation" };
            string[] prDesc = { "Collecting from 87 sources", "Analyzing 2.48M data points", "Identifying patterns and anomalies", "Generating actionable insights" };
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 12 } };
                var iconBox = new VisualElement { style = { width = 24, height = 24, borderTopLeftRadius = 12, borderTopRightRadius = 12, borderBottomLeftRadius = 12, borderBottomRightRadius = 12, backgroundColor = new Color(cGreen.r, cGreen.g, cGreen.b, 0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = cGreen, borderBottomColor = cGreen, borderLeftColor = cGreen, borderRightColor = cGreen, alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 12 } };
                iconBox.Add(new Label("&#xf00c;") { style = { color = cGreen, fontSize = 10 } }.WithClass("fa-icon"));
                row.Add(iconBox);
                
                var tRight = new VisualElement { style = { flexGrow = 1 } };
                tRight.Add(new Label(prNames[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                tRight.Add(new Label(prDesc[i]) { style = { color = Color.grey, fontSize = 7 } });
                row.Add(tRight);
                
                row.Add(new Label("ACTIVE") { style = { color = cGreen, fontSize = 7, unityFontStyleAndWeight = FontStyle.Bold } });
                prList.Add(row);
            }

            // --- RECENT ANALYTIC REPORTS ---
            var repList = panel.Q<ScrollView>("DA_ReportsList");
            repList.Clear();
            string[] rTits = { "Northern Border Security Assessment", "Economic Instability Risk Analysis", "Cyber Threat Landscape Report", "Political Opposition Activity Review", "Maritime Security Intelligence Brief" };
            string[] rCats = { "Security", "Economic", "Cyber", "Political", "Security" };
            string[] rPris = { "HIGH", "MEDIUM", "HIGH", "HIGH", "MEDIUM" };
            string[] rDats = { "16 JAN 2026", "15 JAN 2026", "15 JAN 2026", "14 JAN 2026", "14 JAN 2026" };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingBottom = 6, paddingTop = 6, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                row.Add(new Label(rTits[i]) { style = { color = Color.white, fontSize = 8, flexGrow = 1 } });
                row.Add(new Label(rCats[i]) { style = { color = Color.grey, fontSize = 7, width = 70 } });
                row.Add(new Label(rPris[i]) { style = { color = rPris[i] == "HIGH" ? cRed : cOrange, fontSize = 7, unityFontStyleAndWeight = FontStyle.Bold, width = 60 } });
                row.Add(new Label(rDats[i]) { style = { color = Color.grey, fontSize = 7, width = 60 } });
                row.Add(new Label("Completed") { style = { color = cGreen, fontSize = 7, width = 60, unityTextAlign = TextAnchor.MiddleRight } });
                repList.Add(row);
            }

            // --- ANALYTIC TOOLS ---
            var tGrid = panel.Q<VisualElement>("DA_ToolsGrid");
            tGrid.Clear();
            string[] tlNames = { "Data Correlation", "Risk Modeling", "Scenario Analysis", "Network Analysis", "Sentiment Analysis", "Predictive Analytics" };
            string[] tlDesc = { "Find relationships", "Predict threats", "What-if scenarios", "Entity connections", "Social monitoring", "Trend forecasting" };
            string[] tlIco = { "&#xf0e8;", "&#xf071;", "&#xf126;", "&#xf247;", "&#xf118;", "&#xf201;" };
            for(int i=0; i<6; i++)
            {
                var btn = new Button { style = { width = Length.Percent(48), height = 36, flexDirection = FlexDirection.Row, justifyContent = Justify.FlexStart, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.02f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = cWhiteAlpha, borderBottomColor = cWhiteAlpha, borderLeftColor = cWhiteAlpha, borderRightColor = cWhiteAlpha, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, marginBottom = 6, paddingLeft = 12 } };
                btn.Add(new Label(tlIco[i]) { style = { color = Color.white, fontSize = 14, marginRight = 10, width = 16 } }.WithClass("fa-icon"));
                var txts = new VisualElement();
                txts.Add(new Label(tlNames[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                txts.Add(new Label(tlDesc[i]) { style = { color = Color.grey, fontSize = 7 } });
                btn.Add(txts);
                tGrid.Add(btn);
            }

            // --- QUICK ACTIONS ---
            var qaList = panel.Q<VisualElement>("DA_QuickActions");
            qaList.Clear();
            string[] qTxts = { "REQUEST DEEP ANALYSIS", "GENERATE THREAT REPORT", "EXPORT INTELLIGENCE DATA", "SHARE WITH STAKEHOLDERS" };
            string[] qIcos = { "&#xf002;", "&#xf15c;", "&#xf56e;", "&#xf1e0;" };
            for(int i=0; i<4; i++)
            {
                var btn = new Button { style = { height = 28, flexDirection = FlexDirection.Row, justifyContent = Justify.FlexStart, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.05f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = cWhiteAlpha, borderBottomColor = cWhiteAlpha, borderLeftColor = cWhiteAlpha, borderRightColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, paddingLeft = 12 } };
                btn.Add(new Label(qIcos[i]) { style = { color = cGreen, fontSize = 10, marginRight = 8, width = 16 } }.WithClass("fa-icon"));
                btn.Add(new Label(qTxts[i]) { style = { color = Color.white, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
                qaList.Add(btn);
            }
        }

}
}

public static class VEExtensionHelpers {
    public static T WithClass<T>(this T ve, string className) where T : UnityEngine.UIElements.VisualElement {
        ve.AddToClassList(className);
        return ve;
    }
}
