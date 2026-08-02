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
            BindActiveDashboardRoot();
        }

        private void BindActiveDashboardRoot()
        {
            var activePanel = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Intel");
            if (activePanel != null)
            {
                _dashboardRoot = activePanel;
                _root = activePanel;
            }
            else
            {
                if (_uiDoc == null) _uiDoc = GetComponent<UIDocument>();
                if (_uiDoc != null) _root = _uiDoc.rootVisualElement;
                _dashboardRoot = _root?.Q<VisualElement>("IntelligenceDashboard");
            }

            if (_dashboardRoot != null)
            {
                InitializeElements(_dashboardRoot);
            }
        }


        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshIntelligenceData;
            BindActiveDashboardRoot();
            if (_dashboardRoot != null) RefreshIntelligenceData();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshIntelligenceData;
        }

        private void InitializeElements(VisualElement root)
        {
            root.Q<Button>("BtnLaunchReconMission")?.RegisterCallback<ClickEvent>(_ => LaunchReconMission());

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

            _headerBudget     = root.Q<Label>("HdrIntelBudgetTop") ?? root.Q<Label>("HeaderIntelBudget");
            _headerOperatives = root.Q<Label>("HdrOperatives")     ?? root.Q<Label>("HeaderOperativeCount");
            _headerCoverage   = root.Q<Label>("HdrIntelCoverage")   ?? root.Q<Label>("GlobalCoverageScore");
            _exposureRiskLabel = root.Q<Label>("HdrExposureRisk")   ?? root.Q<Label>("ExposureRiskLabel");

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

            // Asset Procurement
            root.Q<Button>("BtnBuySatellite")?.RegisterCallback<ClickEvent>(_ => LaunchSatellite());
            root.Q<Button>("BtnBuyDrones")?.RegisterCallback<ClickEvent>(_ => ProcureDrones());

            var pSatImg = root.Q<VisualElement>("ProcurementSatImage");
            if (pSatImg != null && System.IO.File.Exists("Assets/UI/Icons/spy_satellite.png"))
            {
                var tex = new UnityEngine.Texture2D(2, 2);
                tex.LoadImage(System.IO.File.ReadAllBytes("Assets/UI/Icons/spy_satellite.png"));
                pSatImg.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
            }

            var pDroneImg = root.Q<VisualElement>("ProcurementDroneImage");
            if (pDroneImg != null && System.IO.File.Exists("Assets/UI/Icons/recon_drone.png"))
            {
                var tex = new UnityEngine.Texture2D(2, 2);
                tex.LoadImage(System.IO.File.ReadAllBytes("Assets/UI/Icons/recon_drone.png"));
                pDroneImg.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
            }

            root.Q<Button>("CloseIntelligence").clicked += Hide;
        }

        public void SwitchTabByName(string name)
        {
            if (name == "TabOverview") SwitchTab(0);
            else if (name == "TabIntelNetwork") SwitchTab(1);
            else if (name == "TabRecon") SwitchTab(2);
            else if (name == "TabCovertOps") SwitchTab(3);
            else if (name == "TabDataAnalysis") SwitchTab(4);
            else if (name == "TabThreats") SwitchTab(5);
        }

        public void SwitchTab(int index)
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
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Intel") 
                              ?? _root?.Q<VisualElement>("IntelligenceDashboard");
            }

            var panel = _dashboardRoot;
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshIntelligenceData();
                UIStateManager.FixScrollViews(panel); // Fix scrollbars in builds after UI refresh
            }
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        public void RefreshIntelligenceData()
        {
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Intel") 
                              ?? _root?.Q<VisualElement>("IntelligenceDashboard") 
                              ?? _dashboardRoot;
            }

            var gm = GameManager.Instance;
            var country = gm?.playerCountry;
            if (country == null) return;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;


            if (gm.TurnSystem != null && gm.TurnSystem.CurrentTurn != _lastSeenTurn)
            {
                _lastSeenTurn = gm.TurnSystem.CurrentTurn;
                _resolvedThreatNames.Clear();
            }

            if (_dashboardRoot != null)
            {
                var satCost = _dashboardRoot.Q<Label>("LblSatCost");
                if (satCost != null) satCost.text = $"{sym}500M";

                var droneCost = _dashboardRoot.Q<Label>("LblDroneCost");
                if (droneCost != null) droneCost.text = $"{sym}150M";
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

            // ── Intelligence Top Bar KPIs ──────────────────────────────────────
            var hdrCoverage = _headerCoverage ?? _dashboardRoot?.Q<Label>("HdrIntelCoverage");
            var hdrCoverageRating = _dashboardRoot?.Q<Label>("HdrIntelCoverageRating");
            if (hdrCoverage != null)
            {
                hdrCoverage.text = $"{country.globalIntelCoverage:F0}/100";
                hdrCoverage.style.color = country.globalIntelCoverage >= 70 ? new Color(0.14f, 0.93f, 0.34f) : country.globalIntelCoverage >= 40 ? new Color(1f, 0.83f, 0.2f) : new Color(1f, 0.3f, 0.3f);
                if (hdrCoverageRating != null)
                    hdrCoverageRating.text = country.globalIntelCoverage >= 70 ? "High Coverage" : country.globalIntelCoverage >= 40 ? "Moderate" : "Low Coverage";
            }

            var hdrOps = _headerOperatives ?? _dashboardRoot?.Q<Label>("HdrOperatives");
            if (hdrOps != null) hdrOps.text = country.operativeCount.ToString();

            var hdrCovert = _dashboardRoot?.Q<Label>("HdrCovertOps");
            var hdrCovertSub = _dashboardRoot?.Q<Label>("HdrCovertOpsSub");
            if (hdrCovert != null)
            {
                int covertCount = country.activeCovertOps?.Count ?? 0;
                hdrCovert.text = covertCount.ToString();
                hdrCovert.style.color = covertCount > 0 ? new Color(1f, 0.83f, 0.2f) : Color.white;
                if (hdrCovertSub != null) hdrCovertSub.text = covertCount == 1 ? "Active Mission" : "Active Missions";
            }

            var hdrSat = _dashboardRoot?.Q<Label>("HdrSatellites");
            if (hdrSat != null)
            {
                hdrSat.text = $"{country.satelliteCount}/4";
                hdrSat.style.color = country.satelliteCount >= 4 ? new Color(0.14f, 0.93f, 0.34f) : Color.white;
            }

            var hdrExp = _exposureRiskLabel ?? _dashboardRoot?.Q<Label>("HdrExposureRisk");
            var hdrExpSub = _dashboardRoot?.Q<Label>("HdrExposureRiskSub");
            if (hdrExp != null)
            {
                hdrExp.text = $"{country.exposureRisk:F0}%";
                hdrExp.style.color = country.exposureRisk > 75f ? new Color(1f, 0.3f, 0.3f) : country.exposureRisk > 40f ? new Color(1f, 0.83f, 0.2f) : new Color(0.14f, 0.93f, 0.34f);
                if (hdrExpSub != null)
                    hdrExpSub.text = country.exposureRisk > 75f ? "⚠ CRITICAL" : country.exposureRisk > 40f ? "Elevated Risk" : "Network Safe";
            }

            var hdrBudgetTop = _headerBudget ?? _dashboardRoot?.Q<Label>("HdrIntelBudgetTop");
            if (hdrBudgetTop != null)
                hdrBudgetTop.text = UiFormat.Money(country.intelligenceBudget * country.exchangeRate, sym);
        }

        private void PopulateSurveillance(CountryDataSO country, GameManager gm)
        {
            Debug.Log($"[IntelligenceDashboard] Populating Surveillance Tab... Satellites: {country.satelliteCount}, Exposure Risk: {country.exposureRisk}");
            
            var panel = _dashboardRoot?.Q<VisualElement>("PanelSurveillance");
            if (panel != null)
            {
                int totalAssets = country.satelliteCount * 4 + 10;
                int onlineAssets = Mathf.RoundToInt(totalAssets * 0.75f);
                if (panel.Q<Label>("RS_TotalAssets") != null) panel.Q<Label>("RS_TotalAssets").text = totalAssets.ToString();
                if (panel.Q<Label>("RS_OnlineAssets") != null) panel.Q<Label>("RS_OnlineAssets").text = $"Online: {onlineAssets}";
                if (panel.Q<Label>("RS_AssetHeaderCounts") != null) panel.Q<Label>("RS_AssetHeaderCounts").text = $"{onlineAssets} Online / {totalAssets} Total";

                int totalMissions = country.activeCovertOps.Count;
                int highPriority = country.activeCovertOps.FindAll(x => x.riskLevel == "Extreme" || x.riskLevel == "High").Count;
                if (panel.Q<Label>("RS_TotalMissions") != null) panel.Q<Label>("RS_TotalMissions").text = totalMissions.ToString();
                if (panel.Q<Label>("RS_HighPriority") != null) panel.Q<Label>("RS_HighPriority").text = $"High Priority: {highPriority}";

                float collectionRate = Mathf.Clamp(60f + country.intelligenceLevel * 0.4f, 0, 100);
                if (panel.Q<Label>("RS_CollectionRate") != null) panel.Q<Label>("RS_CollectionRate").text = $"{collectionRate:F0}%";
                if (panel.Q<Label>("RS_CollectionStatus") != null) panel.Q<Label>("RS_CollectionStatus").text = collectionRate > 80 ? "Excellent" : (collectionRate > 60 ? "Good" : "Poor");

                float detectionRate = Mathf.Clamp(50f + country.globalIntelCoverage * 0.3f, 0, 100);
                if (panel.Q<Label>("RS_DetectionRate") != null) panel.Q<Label>("RS_DetectionRate").text = $"{detectionRate:F0}%";
                if (panel.Q<Label>("RS_DetectionStatus") != null) panel.Q<Label>("RS_DetectionStatus").text = detectionRate > 75 ? "Optimal" : (detectionRate > 50 ? "Moderate" : "Low");

                float fusionAccuracy = Mathf.Clamp(70f + country.intelligenceLevel * 0.2f, 0, 100);
                if (panel.Q<Label>("RS_FusionAccuracy") != null) panel.Q<Label>("RS_FusionAccuracy").text = $"{fusionAccuracy:F0}%";
                if (panel.Q<Label>("RS_FusionStatus") != null) panel.Q<Label>("RS_FusionStatus").text = fusionAccuracy > 80 ? "High" : "Average";

                var assetsList = panel.Q<VisualElement>("RS_AssetsList");
                if (assetsList != null)
                {
                    assetsList.Clear();
                    if (totalAssets <= 0)
                    {
                        AddPlaceholder(assetsList, "No surveillance assets deployed.");
                    }
                    else
                    {
                        // Synthesize a few key assets for the dashboard
                        int itemsToShow = Mathf.Min(totalAssets, 6); // Just show top 6
                        for (int i = 0; i < itemsToShow; i++)
                        {
                            bool isOnline = i < onlineAssets;
                            string type = (i % 3 == 0) ? "IMINT" : (i % 3 == 1) ? "SIGINT" : "UAV";
                            string assetName = type == "UAV" ? $"MQ-{(9 + i)} Reaper Sqn" : $"SAT-{(101 + i)} Orbital";
                            string coverage = type == "UAV" ? "Regional" : "Global";
                            
                            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8, paddingBottom = 4, borderBottomWidth = 1, borderBottomColor = new Color(1, 1, 1, 0.1f) } };
                            row.Add(new Label(assetName) { style = { flexGrow = 2, width = Length.Percent(30), color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                            row.Add(new Label(type) { style = { flexGrow = 1, width = Length.Percent(15), color = Color.gray, fontSize = 12 } });
                            row.Add(new Label(isOnline ? "ONLINE" : "OFFLINE") { style = { flexGrow = 1, width = Length.Percent(20), color = isOnline ? new Color(0.14f, 0.93f, 0.34f) : Color.gray, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                            row.Add(new Label(coverage) { style = { flexGrow = 1, width = Length.Percent(20), color = Color.gray, fontSize = 12 } });
                            row.Add(new Label(isOnline ? $"{70 + i * 4}%" : "0%") { style = { flexGrow = 1, width = Length.Percent(15), color = Color.white, fontSize = 12, unityTextAlign = TextAnchor.MiddleRight } });
                            assetsList.Add(row);
                        }
                    }
                }
                var missionsList = panel.Q<VisualElement>("RS_MissionsList");
                var missionsHeader = panel.Q<Label>("RS_MissionsHeaderTitle");
                if (missionsList != null)
                {
                    missionsList.Clear();
                    int opsCount = (country.activeCovertOps != null) ? country.activeCovertOps.Count : 0;
                    
                    if (missionsHeader != null)
                    {
                        missionsHeader.text = $"ACTIVE RECON MISSIONS ({opsCount})";
                    }

                    if (opsCount == 0)
                    {
                        AddPlaceholder(missionsList, "No active recon missions.");
                    }
                    else
                    {
                        foreach (var op in country.activeCovertOps)
                        {
                            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8, paddingBottom = 4, borderBottomWidth = 1, borderBottomColor = new Color(1, 1, 1, 0.1f) } };
                            row.Add(new Label(op.opName) { style = { flexGrow = 2, width = Length.Percent(25), color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, whiteSpace = WhiteSpace.Normal } });
                            row.Add(new Label(op.operativeName) { style = { flexGrow = 1, width = Length.Percent(15), color = Color.gray, fontSize = 12 } });
                            row.Add(new Label(op.type) { style = { flexGrow = 2, width = Length.Percent(20), color = new Color(0.14f, 0.93f, 0.34f), fontSize = 12 } });
                            row.Add(new Label(op.target) { style = { flexGrow = 1, width = Length.Percent(15), color = Color.gray, fontSize = 12 } });
                            row.Add(new Label(op.status.ToUpper()) { style = { flexGrow = 1, width = Length.Percent(15), color = op.status == "ACTIVE" ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f), fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                            row.Add(new Label($"Phase {op.currentPhase}/{op.totalPhases}") { style = { flexGrow = 1, width = Length.Percent(10), color = Color.white, fontSize = 12, unityTextAlign = TextAnchor.MiddleRight } });
                            missionsList.Add(row);
                        }
                    }
                }
                var sensorList = panel.Q<VisualElement>("RS_SensorCoverageList");
                if (sensorList != null)
                {
                    sensorList.Clear();
                    if (country.regions == null || country.regions.Count == 0) AddPlaceholder(sensorList, "Sensor network offline.");
                    else
                    {
                        for (int i = 0; i < country.regions.Count && i < 5; i++)
                        {
                            var reg = country.regions[i];
                            if (reg == null) continue;
                            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                            
                            var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 110 } };
                            rL.Add(new Label(reg.regionName ?? "Region") { style = { color = Color.gray, fontSize = 10, whiteSpace = WhiteSpace.Normal } });
                            row.Add(rL);
                            
                            var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = new Color(1, 1, 1, 0.1f), borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                            float cov = Mathf.Clamp(reg.security + country.intelligenceLevel * 0.2f, 0f, 100f);
                            barBg.Add(new VisualElement { style = { width = Length.Percent(cov), height = 4, backgroundColor = cov > 70f ? new Color(0.14f, 0.93f, 0.34f) : cov > 40f ? new Color(0.95f, 0.77f, 0.06f) : new Color(1f, 0.32f, 0.32f), borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                            row.Add(barBg);
                            
                            row.Add(new Label($"{cov:F0}%") { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, width = 30, unityTextAlign = TextAnchor.MiddleRight } });
                            sensorList.Add(row);
                        }
                    }
                }
                var donutList = panel.Q<VisualElement>("RS_DonutBreakdownList");
                if (donutList != null) {
                    donutList.Clear();
                    string[] colTypes = { "SIGINT", "HUMINT", "OSINT", "CYBER" };
                    float baseVal = 20f + (country.intelligenceLevel * 0.2f);
                    float[] colVals = { baseVal + 10f, baseVal, baseVal - 5f, baseVal - 5f };
                    Color[] colColors = { new Color(0.18f, 0.6f, 0.95f), new Color(0.95f, 0.77f, 0.06f), new Color(0.137f, 0.929f, 0.337f), new Color(0.73f, 0.41f, 0.78f) };
                    
                    for(int i = 0; i < 4; i++) {
                        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 4 } };
                        var labelCont = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                        labelCont.Add(new VisualElement { style = { width = 8, height = 8, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = colColors[i], marginRight = 6 } });
                        labelCont.Add(new Label(colTypes[i]) { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                        row.Add(labelCont);
                        row.Add(new Label($"{colVals[i]:F0}%") { style = { color = Color.gray, fontSize = 12 } });
                        donutList.Add(row);
                    }
                }
                
                var donutVal = panel.Q<Label>("RS_DonutTotalValue");
                string daSym2 = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;
                if (donutVal != null) donutVal.text = $"{daSym2}{(country.intelligenceBudget * 0.25f * country.exchangeRate):F1}B";

                // Ensure no placeholders exist for analytics list
                var aList = panel.Q<VisualElement>("RS_AnalyticsList");
                if (aList != null) {
                    aList.Clear();
                    AddPlaceholder(aList, "Gathering active intelligence data...");
                }
                var recentActivityList = panel.Q<VisualElement>("RS_RecentActivityContainer");
                if (recentActivityList != null)
                {
                    recentActivityList.Clear();
                    if (country.recentReconReports != null && country.recentReconReports.Count > 0)
                    {
                        foreach (var report in country.recentReconReports)
                        {
                            var reportLabel = new Label(report) { style = { color = new Color(0.8f, 0.8f, 0.8f), fontSize = 12, whiteSpace = WhiteSpace.Normal, marginBottom = 4, borderBottomWidth = 1, borderBottomColor = new Color(1, 1, 1, 0.1f), paddingBottom = 4 } };
                            recentActivityList.Add(reportLabel);
                        }
                    }
                    else
                    {
                        AddPlaceholder(recentActivityList, "No recent activity.");
                    }
                }
                
                var feedList = panel.Q<ScrollView>("RS_LiveFeedList");
                if (feedList != null) {
                    var feedLines = DashboardLiveContent.BuildIntelFeed(country, gm);
                    DashboardLiveContent.PopulateFeed(feedList, feedLines, 15);
                }

            }

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

        
        private void AddPlaceholder(VisualElement container, string text = "Data not available")
        {
            if (container == null) return;
            container.Clear();
            var label = new Label(text) { style = { color = Color.gray, unityTextAlign = TextAnchor.MiddleCenter, flexGrow = 1, marginTop = 20, fontSize = 14 } };
            container.Add(label);
        }

        private void PopulateIntelGrid(CountryDataSO country, GameManager gm)
        {
            Debug.Log($"[IntelligenceDashboard] Populating Intel Grid... Global Coverage: {country.globalIntelCoverage}");
            if (_intelGridList == null) return;
            _intelGridList.Clear();
            _intelGridList.style.flexDirection = FlexDirection.Row;
            _intelGridList.style.flexWrap = Wrap.Wrap;
            _intelGridList.style.justifyContent = Justify.SpaceBetween;

            var cells = new[]
            {
                ("SIGINT", country.signalInterceptCapability),
                ("HUMINT", country.intelligenceLevel),
                ("CYBER", country.cyberDefenseLevel),
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

                cell.Add(new Label(name) { style = { color = new Color(0.82f, 0.55f, 0.95f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 1.2f } });
                cell.Add(new ProgressBar() { value = val, highValue = 100, style = { height = 3, marginTop = 6 } });
                cell.Add(new Label($"{val:F0}% effective") { style = { color = new Color(0.55f, 0.55f, 0.65f), fontSize = 10, marginTop = 5 } });
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
                    style = { color = new Color(0.82f, 0.55f, 0.95f), fontSize = 12,
                              unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 1f }
                });

                int maxSats = 4;
                string satStatus = country.satelliteCount >= maxSats
                    ? "FULL COVERAGE"
                    : $"{country.satelliteCount}/{maxSats} ACTIVE";
                var satStatusLabel = new Label(satStatus);
                satStatusLabel.style.fontSize = 11;
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
                satDesc.style.fontSize = 10;
                satDesc.style.whiteSpace = WhiteSpace.Normal;
                satDesc.style.marginBottom = 10;
                satSection.Add(satDesc);

                if (country.satellitesUnderConstruction > 0)
                {
                    var constructionLbl = new Label($"🚀 {country.satellitesUnderConstruction} SATELLITE(S) IN BUILD QUEUE ({country.satelliteBuildMonthsRemaining} MO REMAINING)");
                    constructionLbl.style.color = new Color(0.95f, 0.77f, 0.06f);
                    constructionLbl.style.fontSize = 11;
                    constructionLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    constructionLbl.style.marginBottom = 8;
                    satSection.Add(constructionLbl);
                }

                if (country.satelliteCount + country.satellitesUnderConstruction < maxSats)
                {
                    var launchBtn = new Button(LaunchSatellite) { text = "🚀  LAUNCH SPY SATELLITE  ($0.5B)" };
                    launchBtn.AddToClassList("button-premium");
                    launchBtn.style.height = 30;
                    launchBtn.style.fontSize = 12;
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
            header.Add(new Label(region) { style = { color = new Color(0.92f, 0.92f, 0.95f), fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });

            var tagLabel = new Label(tag);
            tagLabel.style.color = new Color(0.82f, 0.55f, 0.95f);
            tagLabel.style.fontSize = 10;
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
            Debug.Log($"[IntelligenceDashboard] Populating Intel Network Tab... Active Ops: {country.activeCovertOps.Count}, Recruited Assets: {country.recruitedAssets.Count}");
            var panel = _dashboardRoot?.Q<VisualElement>("PanelCovertOps");
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
            
            panel.Q<Label>("IN_CovertBudget").text = $"${country.covertBudget:F1}B / month";
            panel.Q<Label>("IN_NetworkExposure").text = country.exposureRisk > 70 ? "HIGH" : (country.exposureRisk > 40 ? "MEDIUM" : "LOW");
            panel.Q<Label>("IN_NetworkExposure").style.color = country.exposureRisk > 70 ? new Color(1f, 0.3f, 0.3f) : (country.exposureRisk > 40 ? new Color(1f, 0.6f, 0.2f) : new Color(0.137f, 0.929f, 0.337f));

            // --- 2. Active Agents ---
            panel.Q<Label>("IN_ActiveAgentsTitle").text = $"ACTIVE AGENTS ({country.activeCovertOps.Count})";
            var agentsList = panel.Q<ScrollView>("IN_ActiveAgentsList");
            agentsList.Clear();
            if (country.activeCovertOps.Count == 0)
            {
                agentsList.Add(new Label("NO ACTIVE OPERATIONS") { style = { color = Color.grey, fontSize = 12, marginTop = 10, unityTextAlign = TextAnchor.MiddleCenter } });
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
            var realEvents = DashboardLiveContent.BuildIntelFeed(country, GameManager.Instance);
            if (realEvents == null || realEvents.Count == 0)
            {
                eventsList.Add(new Label("NO RECENT EVENTS") { style = { color = Color.grey, fontSize = 12, marginTop = 10, unityTextAlign = TextAnchor.MiddleCenter } });
            }
            else
            {
                int count = 0;
                foreach (var ev in realEvents)
                {
                    if (count >= 10) break;
                    var evRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
                    var iconLbl = new Label("\uf192") { style = { color = new Color(0.137f, 0.929f, 0.337f), marginRight = 8, fontSize = 10, marginTop = 2 } };
                    iconLbl.AddToClassList("fa-icon");
                    evRow.Add(iconLbl);
                    evRow.Add(new Label(ev.Message) { style = { color = new Color(0.8f, 0.8f, 0.8f), fontSize = 11, whiteSpace = WhiteSpace.Normal, flexShrink = 1 } });
                    eventsList.Add(evRow);
                    count++;
                }
            }

            // --- 4. Foreign Assets ---
            var assetsList = panel.Q<VisualElement>("IN_ForeignAssetsList");
            assetsList.Clear();
            if (country.recruitedAssets.Count == 0)
            {
                assetsList.Add(new Label("NO FOREIGN ASSETS") { style = { color = Color.grey, fontSize = 12, marginTop = 10, unityTextAlign = TextAnchor.MiddleCenter } });
            }
            else
            {
                foreach (var asset in country.recruitedAssets)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, paddingBottom = 8, paddingTop = 8, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.05f) } };
                    
                    var info = new VisualElement();
                    info.Add(new Label(asset.assetName) { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                    info.Add(new Label(asset.targetCountry) { style = { color = Color.grey, fontSize = 10, marginTop = 2 } });
                    row.Add(info);

                    var stats = new VisualElement { style = { flexDirection = FlexDirection.Row, width = 120, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
                    
                    var trustBar = new ProgressBar { value = asset.loyalty, highValue = 100 };
                    trustBar.style.width = 40; trustBar.style.height = 4;
                    stats.Add(trustBar);

                    var relContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    for (int i=1; i<=5; i++)
                    {
                        relContainer.Add(new Label("★") { style = { color = i <= asset.intelLevelProvided ? new Color(0.137f, 0.929f, 0.337f) : new Color(1,1,1,0.2f), fontSize = 10 } });
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
                header.Add(new Label(capNames[i]) { style = { color = Color.grey, fontSize = 11 } });
                header.Add(new Label($"{capVals[i]:F0}%") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
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
                row.Add(new Label(brkNames[i]) { style = { color = Color.grey, fontSize = 11, width = 100 } });
                row.Add(new Label($"{brkPcts[i]:F0}%") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                brkList.Add(row);
            }

            // --- 7. Recruit List ---
            var recruitList = panel.Q<ScrollView>("IN_RecruitList");
            recruitList.Clear();
            string[] recNames = { "Domestic Agent", "Foreign Asset", "Cyber Operative", "Double Agent" };
            float[] recCosts = { 15f, 45f, 30f, 80f }; // in Millions
            for (int i=0; i<recNames.Length; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.03f), paddingBottom = 6, paddingTop = 6, paddingLeft = 8, paddingRight = 8, marginBottom = 4, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
                row.Add(new Label(recNames[i]) { style = { color = Color.white, fontSize = 11 } });
                
                var right = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;
                float localCost = recCosts[i] * country.exchangeRate;
                
                right.Add(new Label($"{sym}{localCost:F0}M") { style = { color = new Color(0.137f, 0.929f, 0.337f), fontSize = 11, marginRight = 8 } });
                
                var hireBtn = new Button { text = "HIRE", style = { height = 18, fontSize = 10, backgroundColor = new Color(0.137f, 0.929f, 0.337f, 0.2f), color = new Color(0.137f, 0.929f, 0.337f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(0.137f, 0.929f, 0.337f, 0.5f), borderBottomColor = new Color(0.137f, 0.929f, 0.337f, 0.5f), borderLeftColor = new Color(0.137f, 0.929f, 0.337f, 0.5f), borderRightColor = new Color(0.137f, 0.929f, 0.337f, 0.5f) } };
                
                int index = i;
                hireBtn.clicked += () => 
                {
                    if (country.intelligenceFunds < localCost)
                    {
                        NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need {sym}{localCost:F0}M to hire {recNames[index]}.", Color.red);
                        return;
                    }
                    country.intelligenceFunds -= localCost;
                    
                    if (index == 0) country.operativeCount++;
                    else if (index == 1) country.recruitedAssets.Add(new ForeignAsset { assetName = "New Asset", targetCountry = "Unknown", loyalty = 50f, intelLevelProvided = 2 });
                    else if (index == 2) country.intelligenceLevel = Mathf.Min(100f, country.intelligenceLevel + 1f);
                    else if (index == 3) country.exposureRisk = Mathf.Max(0f, country.exposureRisk - 5f);
                    
                    NotificationManager.Instance?.AddNotification($"{recNames[index].ToUpper()} HIRED: Deducted {sym}{localCost:F0}M from Intelligence Funds.", new Color(0.137f, 0.929f, 0.337f));
                    RefreshIntelligenceData();
                };
                
                right.Add(hireBtn);
                row.Add(right);
                recruitList.Add(row);
            }

            // --- 8. Quick Actions ---
            var qaGrid = panel.Q<VisualElement>("IN_QuickActionsGrid");
            qaGrid.Clear();
            string[] qaNames = { "Dispatch Agent", "Fund Operation", "Secure Channel", "Increase Surv.", "Debrief Agents", "Counter Intel" };
            float[] qaCosts = { 5f, 12f, 3f, 8f, 2f, 10f }; // in Millions
            for (int i=0; i<qaNames.Length; i++)
            {
                var btn = new Button { text = qaNames[i] };
                btn.style.width = Length.Percent(48);
                btn.style.height = 24;
                btn.style.marginBottom = 4;
                btn.style.fontSize = 10;
                btn.style.backgroundColor = new Color(1,1,1,0.05f);
                btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1; btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
                btn.style.borderTopColor = new Color(1,1,1,0.1f); btn.style.borderBottomColor = new Color(1,1,1,0.1f); btn.style.borderLeftColor = new Color(1,1,1,0.1f); btn.style.borderRightColor = new Color(1,1,1,0.1f);
                btn.style.color = Color.white;
                
                int index = i;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;
                float localCost = qaCosts[i] * country.exchangeRate;
                
                btn.clicked += () => 
                {
                    if (country.intelligenceFunds < localCost)
                    {
                        NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need {sym}{localCost:F0}M for {qaNames[index]}.", Color.red);
                        return;
                    }
                    country.intelligenceFunds -= localCost;
                    
                    if (index == 5) country.exposureRisk = Mathf.Max(0f, country.exposureRisk - 2f);
                    
                    NotificationManager.Instance?.AddNotification($"{qaNames[index].ToUpper()} EXECUTED: Deducted {sym}{localCost:F0}M.", new Color(0.18f, 0.6f, 0.95f));
                    RefreshIntelligenceData();
                };
                
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
            var lbl = new Label("\uf21b") { style = { color = Color.white, fontSize = 18 } };
            lbl.AddToClassList("fa-icon");
            avatar.Add(lbl);
            card.Add(avatar);

            var mid = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
            header.Add(new Label(string.IsNullOrEmpty(op.operativeName) ? "UNKNOWN" : op.operativeName) { style = { color = Color.white, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } });
            header.Add(new Label(op.status.ToUpper()) { style = { color = new Color(0.18f, 0.6f, 0.95f), fontSize = 9, backgroundColor = new Color(0.18f, 0.6f, 0.95f, 0.2f), paddingLeft = 4, paddingRight = 4, paddingTop = 2, paddingBottom = 2, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
            mid.Add(header);

            var details = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            details.Add(new Label($"Loc: {op.target}") { style = { color = Color.grey, fontSize = 10, marginRight = 8 } });
            details.Add(new Label($"Asg: {op.type}") { style = { color = Color.grey, fontSize = 10 } });
            mid.Add(details);

            card.Add(mid);

            var right = new VisualElement { style = { width = 60, justifyContent = Justify.Center } };
            var progLbl = new Label($"Phase {op.currentPhase}/{op.totalPhases}") { style = { color = Color.white, fontSize = 10, marginBottom = 2, unityTextAlign = TextAnchor.MiddleRight } };
            right.Add(progLbl);
            
            float progPct = (float)op.currentPhase / op.totalPhases * 100f;
            var pb = new ProgressBar { value = progPct, highValue = 100, style = { height = 4 } };
            right.Add(pb);
            
            var expLbl = new Label($"Exp: {op.detectionRisk}%") { style = { color = op.detectionRisk > 50 ? new Color(1f, 0.3f, 0.3f) : new Color(0.137f, 0.929f, 0.337f), fontSize = 9, marginTop = 2, unityTextAlign = TextAnchor.MiddleRight } };
            right.Add(expLbl);

            card.Add(right);

            return card;
        }

        private void PopulateProxyWarPanel(CountryDataSO country, GameManager gm)
        {
            Debug.Log("[IntelligenceDashboard] Populating Proxy War Panel...");
            // Provides a shortcut button in the Threat Board to fund enemy insurgencies
        }

        private void PopulateThreatBoard(CountryDataSO country, GameManager gm)
        {
            Debug.Log("[IntelligenceDashboard] Populating Threat Board...");
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
                style = { color = new Color(1f, 0.45f, 0.45f), fontSize = 12,
                          unityFontStyleAndWeight = FontStyle.Bold }
            });

            var badge = new Label("CONFIRMED") {
                style = { color = new Color(1f, 0.3f, 0.3f), fontSize = 10,
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
                cell.Add(new Label(label) { style = { color = new Color(0.5f, 0.5f, 0.6f), fontSize = 10 } });
                cell.Add(new Label(value) { style = { color = valColor, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
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
            statusLabel.style.fontSize = 10;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            statusLabel.style.marginBottom = 10;
            card.Add(statusLabel);

            // ── Warning: camp growing ──
            if (region.campStrength > 70f && !strikeOrdered)
            {
                var warn = new Label("🔴 CAMP APPROACHING FULL STRENGTH — strike window closing");
                warn.style.color = new Color(1f, 0.35f, 0.35f);
                warn.style.fontSize = 10;
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
                }) { text = "\uf0fb  FLAG FOR AIRSTRIKE  \uf061  MILITARY TO EXECUTE" };
                flagBtn.AddToClassList("fa-icon");

                flagBtn.style.height = 32;
                flagBtn.style.fontSize = 11;
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
                }) { text = "\uf2b5  COUNTER-INSURGENCY OP  ($0.08B)" };
                cinBtn.AddToClassList("fa-icon");

                cinBtn.style.height = 26;
                cinBtn.style.fontSize = 10;
                cinBtn.style.backgroundColor = new Color(0.05f, 0.2f, 0.35f, 0.9f);
                cinBtn.style.borderTopLeftRadius = 4; cinBtn.style.borderTopRightRadius = 4;
                cinBtn.style.borderBottomLeftRadius = 4; cinBtn.style.borderBottomRightRadius = 4;
                card.Add(cinBtn);
            }
            else
            {
                // Strike already ordered — show reminder
                var reminder = new Label("\uf061 Go to MILITARY \uf061 OPERATIONS to execute the strike");
                reminder.AddToClassList("fa-icon");
                reminder.style.color = new Color(0.6f, 0.6f, 0.7f);
                reminder.style.fontSize = 10;
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
            var lbl = new Label(text) { style = { color = new Color(0.4f, 0.4f, 0.5f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Italic } };
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
            headerContainer.Add(new Label(threat.threatName) { style = { color = new Color(0.95f, 0.95f, 1f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12 } });
            
            var badge = new Label(threat.severity.ToUpper()) { style = { fontSize = 10 } };
            badge.style.color = threat.severity == "Critical" ? new Color(255/255f, 82/255f, 82/255f) : new Color(241/255f, 196/255f, 15/255f);
            badge.style.alignSelf = Align.FlexStart;
            headerContainer.Add(badge);
            item.Add(headerContainer);

            if (!string.IsNullOrEmpty(threat.description))
                item.Add(new Label(threat.description) { style = { color = Color.grey, fontSize = 10, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

            if (threat.category == "Insurgent Activity")
            {
                var warningBox = new VisualElement();
                warningBox.style.backgroundColor = new Color(0.8f, 0.1f, 0.1f, 0.3f);
                warningBox.style.marginTop = 10;
                warningBox.style.paddingTop = 6; warningBox.style.paddingBottom = 6;
                warningBox.style.paddingLeft = 8; warningBox.style.paddingRight = 8;
                warningBox.style.borderTopLeftRadius = 4; warningBox.style.borderTopRightRadius = 4;
                warningBox.style.borderBottomLeftRadius = 4; warningBox.style.borderBottomRightRadius = 4;
                var warningText = new Label("⚠ CRITICAL WARNING: INSURGENT CAMP LOCATED") { style = { color = new Color(1f, 0.4f, 0.4f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } };
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

                }) { text = "\uf072 DEPLOY AIR ASSET TO BOMB LOCATION" };
                actionBtn.AddToClassList("fa-icon");
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
                            }) { text = "\uf53d FUND PROXY ($0.1B)" };
                            proxyBtn.AddToClassList("fa-icon");
                            proxyBtn.AddToClassList("button-premium");
                            proxyBtn.style.marginTop = 6;
                            proxyBtn.style.height = 22;
                            proxyBtn.style.fontSize = 10;
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
            actionBtn.style.fontSize = 10;
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
            Debug.Log($"[IntelligenceDashboard] Populating Covert Operations Tab... Active Ops: {country.activeCovertOps.Count}");
            var panel = _dashboardRoot?.Q<VisualElement>("PanelCovertOperations");
            if (panel == null) return;

            // Colors
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);
            var cBlue = new Color(0.18f, 0.6f, 0.95f);
            var cPurple = new Color(0.73f, 0.41f, 0.78f);
            var cWhiteAlpha = new Color(1, 1, 1, 0.1f);

            int intelLvl = Mathf.Clamp((int)country.intelligenceLevel, 10, 100);
            float budget = country.covertBudget > 0 ? country.covertBudget : (country.intelligenceBudget * 0.15f); // Fallback to 15% of intel budget in billions
            
            // --- KPIs ---
            int inProgress = country.activeCovertOps.Count(x => x.status == "In Progress");
            int planning = country.activeCovertOps.Count(x => x.status == "Planning");
            int onHold = country.activeCovertOps.Count(x => x.status == "On Hold");
            int completed = country.activeCovertOps.Count(x => x.status == "Completed");
            int totalOps = inProgress + planning + onHold + completed;

            if (panel.Q<Label>("CO_ActiveOps") != null) panel.Q<Label>("CO_ActiveOps").text = inProgress.ToString();
            
            int highRiskCount = country.activeCovertOps.FindAll(x => (x.riskLevel == "High" || x.riskLevel == "Extreme") && x.status == "In Progress").Count;
            if (panel.Q<Label>("CO_HighRiskOps") != null) panel.Q<Label>("CO_HighRiskOps").text = $"{highRiskCount} High Risk";
            
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;
            
            if (panel.Q<Label>("CO_IntelFundsLabel") != null) panel.Q<Label>("CO_IntelFundsLabel").text = $"FUNDS: {UiFormat.Money((country.intelligenceFunds / 1000f) * country.exchangeRate, sym)}";

            // --- Budget Summary Strip ---
            float annualBudgetLocal = country.intelligenceBudget * country.exchangeRate; // intelligenceBudget is in Billions
            float availableFundsLocal = (country.intelligenceFunds / 1000f) * country.exchangeRate;
            float monthlyUpkeepM = (country.satelliteCount * 50f) + (country.droneCount * 10f) + (country.operativeCount * 5f);
            float monthlyUpkeepLocal = (monthlyUpkeepM / 1000f) * country.exchangeRate;

            var annualLbl = panel.Q<Label>("CO_AnnualBudgetLabel");
            if (annualLbl != null)
            {
                annualLbl.text = UiFormat.Money(annualBudgetLocal, sym);
                annualLbl.style.color = new Color(100/255f, 180/255f, 255/255f);
            }

            var availLbl = panel.Q<Label>("CO_AvailableFundsLabel");
            if (availLbl != null)
            {
                availLbl.text = UiFormat.Money(availableFundsLocal, sym);
                availLbl.style.color = availableFundsLocal >= monthlyUpkeepLocal
                    ? new Color(35/255f, 237/255f, 86/255f)  // green — healthy
                    : new Color(1f, 0.32f, 0.32f);            // red — depleted
            }

            var upkeepLbl = panel.Q<Label>("CO_MonthlyUpkeepLabel");
            if (upkeepLbl != null) upkeepLbl.text = UiFormat.Money(monthlyUpkeepLocal, sym);

            
            float successRate = Mathf.Clamp(40f + (country.intelligenceLevel * 0.4f) + (country.operativeCount * 2f), 10f, 95f);
            if (panel.Q<Label>("CO_SuccessRate") != null) panel.Q<Label>("CO_SuccessRate").text = $"{successRate:F0}%";
            if (panel.Q<Label>("CO_SuccessDelta") != null) panel.Q<Label>("CO_SuccessDelta").text = "Stable";
            
            float localBudget = budget * country.exchangeRate;
            if (panel.Q<Label>("CO_Budget") != null) panel.Q<Label>("CO_Budget").text = $"{UiFormat.Money(localBudget, sym)}";
            if (panel.Q<Label>("CO_BudgetRemaining") != null) panel.Q<Label>("CO_BudgetRemaining").text = $"{UiFormat.Money(localBudget * 0.28f, sym)} rem.";
            
            var riskLbl = panel.Q<Label>("CO_RiskLabel");
            if (riskLbl != null)
            {
                if (country.exposureRisk > 60) { riskLbl.text = "HIGH"; riskLbl.style.color = cRed; if (panel.Q<VisualElement>("CO_RiskDot") != null) panel.Q<VisualElement>("CO_RiskDot").style.backgroundColor = cRed; }
                else if (country.exposureRisk > 30) { riskLbl.text = "MEDIUM"; riskLbl.style.color = cOrange; if (panel.Q<VisualElement>("CO_RiskDot") != null) panel.Q<VisualElement>("CO_RiskDot").style.backgroundColor = cOrange; }
                else { riskLbl.text = "LOW"; riskLbl.style.color = cGreen; if (panel.Q<VisualElement>("CO_RiskDot") != null) panel.Q<VisualElement>("CO_RiskDot").style.backgroundColor = cGreen; }
            }

            // --- OPERATIONS BUDGET & UPKEEP (VERSION 1) ---
            var budgetList = panel.Q<ScrollView>("CO_BudgetRunningCostsList");
            if (budgetList != null)
            {
                budgetList.Clear();
                float satCost = country.satelliteCount * 50f;
                float droneCost = country.droneCount * 10f;
                float opCost = country.operativeCount * 5f;
                float totalCost = satCost + droneCost + opCost;

                string[] bNames = { "Orbital Surveillance", "Tactical Recon", "Field Operatives", "TOTAL UPKEEP" };
                float[] bVals = { satCost, droneCost, opCost, totalCost };
                Color[] bColors = { cBlue, cGreen, cOrange, Color.white };

                for(int i=0; i<4; i++)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 12, alignItems = Align.Center } };
                    
                    var leftBox = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                    leftBox.Add(new VisualElement { style = { width = 8, height = 8, backgroundColor = bColors[i], marginRight = 8, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } });
                    leftBox.Add(new Label(bNames[i]) { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = (i == 3 ? FontStyle.Bold : FontStyle.Normal) } });
                    row.Add(leftBox);
                    
                    float localVal = (bVals[i] / 1000f) * country.exchangeRate; // Convert Millions to Billions
                    row.Add(new Label(UiFormat.Money(localVal, sym)) { style = { color = bColors[i], fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                    budgetList.Add(row);
                }
            }

            // --- ACTIVE COVERT OPERATIONS ---
            var activeList = panel.Q<ScrollView>("CO_ActiveOpsList");
            activeList.Clear();
            var activeOps = country.activeCovertOps.FindAll(x => x.status == "In Progress");
            panel.Q<Label>("CO_ActiveTitle").text = $"ACTIVE COVERT OPERATIONS ({activeOps.Count})";

            foreach(var op in activeOps)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.03f), paddingBottom = 6, paddingTop = 6, paddingLeft = 8, paddingRight = 8, marginBottom = 4, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
                
                Color cCol = op.type == "Political" ? cPurple : op.type == "Economic" ? cOrange : op.type == "Military" ? cBlue : op.type == "Influence" ? cGreen : cRed;
                string cIcn = op.type == "Political" ? "\uf0c0" : op.type == "Economic" ? "\uf153" : op.type == "Military" ? "\uf132" : op.type == "Influence" ? "\uf0a1" : "\uf108";
                
                var nameCol = new VisualElement { style = { width = 140, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var iconBox = new VisualElement { style = { width = 24, height = 24, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(cCol.r, cCol.g, cCol.b, 0.1f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 8 } };
                iconBox.Add(new Label(cIcn) { style = { color = cCol, fontSize = 12 } }.WithClass("fa-icon"));
                nameCol.Add(iconBox);
                
                var nameText = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                nameText.Add(new Label(op.opName) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                nameText.Add(new Label(op.type) { style = { color = Color.grey, fontSize = 9 } });
                nameCol.Add(nameText);
                row.Add(nameCol);

                row.Add(new Label(op.target) { style = { color = Color.grey, fontSize = 10, flexGrow = 1, whiteSpace = WhiteSpace.Normal } });
                row.Add(new Label("Unknown") { style = { color = Color.white, fontSize = 10, width = 80 } }); // Add region later if in data
                
                var rCol = op.riskLevel == "Extreme" ? cRed : op.riskLevel == "High" ? cRed : (op.riskLevel == "Medium" ? cOrange : cGreen);
                row.Add(new Label(op.riskLevel.ToUpper()) { style = { color = rCol, fontSize = 9, width = 50 } });
                
                var sCol = op.status == "In Progress" ? cGreen : cBlue;
                row.Add(new Label(op.status.ToUpper()) { style = { color = sCol, fontSize = 9, width = 70 } });

                float progPct = op.totalPhases > 0 ? (op.currentPhase / (float)op.totalPhases) * 100f : 0f;
                var progCol = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 60 } };
                var pBarBg = new VisualElement { style = { flexGrow = 1, height = 3, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 1, borderTopRightRadius = 1, borderBottomLeftRadius = 1, borderBottomRightRadius = 1, marginRight = 6 } };
                pBarBg.Add(new VisualElement { style = { width = Length.Percent(progPct), height = 3, backgroundColor = sCol, borderTopLeftRadius = 1, borderTopRightRadius = 1, borderBottomLeftRadius = 1, borderBottomRightRadius = 1 } });
                progCol.Add(pBarBg);
                progCol.Add(new Label($"{Mathf.RoundToInt(progPct)}%") { style = { color = Color.white, fontSize = 9 } });
                row.Add(progCol);

                row.Add(new Label($"{op.monthsRemaining} MONTHS") { style = { color = Color.white, fontSize = 10, width = 60, unityTextAlign = TextAnchor.MiddleRight } });
                
                activeList.Add(row);
            }

            // --- OPERATION DETAILS ---
            var detailsContainer = panel.Q<VisualElement>("CO_OpDetailsContainer");
            detailsContainer.Clear();

            if (activeOps == null || activeOps.Count == 0)
            {
                detailsContainer.style.flexDirection = FlexDirection.Column;
                detailsContainer.style.justifyContent = Justify.Center;
                detailsContainer.style.alignItems = Align.Center;
                detailsContainer.Add(new Label("\uf059") { style = { color = cWhiteAlpha, fontSize = 26, marginBottom = 8 } }.WithClass("fa-icon"));
                detailsContainer.Add(new Label("NO OPERATION SELECTED") { style = { color = Color.grey, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 1f } });
            }
            else
            {
                var op = activeOps[0]; // Display the first one by default for V1
                detailsContainer.style.flexDirection = FlexDirection.Row;

                Color oCol = op.type == "Political" ? cPurple : op.type == "Economic" ? cOrange : op.type == "Military" ? cBlue : op.type == "Influence" ? cGreen : cRed;
                string oIcn = op.type == "Political" ? "\uf0c0" : op.type == "Economic" ? "\uf153" : op.type == "Military" ? "\uf132" : op.type == "Influence" ? "\uf0a1" : "\uf108";

                // Details Left
                var dLeft = new VisualElement { style = { width = Length.Percent(50), paddingRight = 12, borderRightWidth = 1, borderRightColor = cWhiteAlpha } };
                
                var dHeader = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 12 } };
                var dIcon = new VisualElement { style = { width = 36, height = 36, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(oCol.r, oCol.g, oCol.b, 0.1f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 12 } };
                dIcon.Add(new Label(oIcn) { style = { color = oCol, fontSize = 18 } }.WithClass("fa-icon"));
                dHeader.Add(dIcon);
                var dTitle = new VisualElement();
                dTitle.Add(new Label(op.opName) { style = { color = Color.white, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });
                dTitle.Add(new Label($"{op.type} Operation") { style = { color = Color.grey, fontSize = 10 } });
                dHeader.Add(dTitle);
                dLeft.Add(dHeader);

                dLeft.Add(new Label("DESCRIPTION") { style = { color = Color.grey, fontSize = 9, marginBottom = 2 } });
                dLeft.Add(new Label($"Targeting {op.target} with priority '{op.priority}'. Currently in phase {op.currentPhase} of {op.totalPhases}.") { style = { color = Color.white, fontSize = 10, whiteSpace = WhiteSpace.Normal, marginBottom = 12 } });

                var dGrid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
                dGrid.Add(CreateDetailStat("TARGET", op.target));
                dGrid.Add(CreateDetailStat("STATUS", op.status));
                dGrid.Add(CreateDetailStat("SPONSOR", "NIA Directorate"));
                dGrid.Add(CreateDetailStat("ASSIGNED CASE OFFICER", string.IsNullOrEmpty(op.operativeName) ? "Unassigned" : op.operativeName));
                dLeft.Add(dGrid);
                
                var dBtns = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 12 } };
                
                var btnView = new Button { text = "VIEW INTEL", style = { height = 24, fontSize = 10, flexGrow = 1, backgroundColor = new Color(0.18f, 0.6f, 0.95f, 0.1f), color = cBlue, borderTopColor = cBlue, borderBottomColor = cBlue, borderLeftColor = cBlue, borderRightColor = cBlue, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, marginRight = 6 } };
                btnView.clicked += () => NotificationManager.Instance?.AddNotification($"INTEL REPORT: {op.opName} is progressing as expected.", cBlue);
                dBtns.Add(btnView);
                
                var btnPause = new Button { text = "PAUSE OPERATION", style = { height = 24, fontSize = 10, flexGrow = 1, backgroundColor = new Color(0.95f, 0.77f, 0.06f, 0.1f), color = cOrange, borderTopColor = cOrange, borderBottomColor = cOrange, borderLeftColor = cOrange, borderRightColor = cOrange, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, marginRight = 6 } };
                btnPause.clicked += () => {
                    op.status = "On Hold";
                    NotificationManager.Instance?.AddNotification($"OPERATION PAUSED: Agents ordered to stand down.", cOrange);
                    RefreshIntelligenceData();
                };
                dBtns.Add(btnPause);
                
                var btnTerm = new Button { text = "TERMINATE", style = { height = 24, fontSize = 10, flexGrow = 1, backgroundColor = new Color(1f, 0.32f, 0.32f, 0.1f), color = cRed, borderTopColor = cRed, borderBottomColor = cRed, borderLeftColor = cRed, borderRightColor = cRed, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1 } };
                btnTerm.clicked += () => {
                    country.activeCovertOps.Remove(op);
                    country.operativeCount++;
                    NotificationManager.Instance?.AddNotification($"OPERATION TERMINATED: Asset retrieved safely.", cRed);
                    RefreshIntelligenceData();
                };
                dBtns.Add(btnTerm);
                
                dLeft.Add(dBtns);
                
                detailsContainer.Add(dLeft);

                // Details Right
                var dRight = new VisualElement { style = { width = Length.Percent(50), paddingLeft = 12, flexDirection = FlexDirection.Column } };
                dRight.Add(new Label("OBJECTIVES") { style = { color = Color.grey, fontSize = 10, marginBottom = 6 } });
                string[] objTxt = { "Initial reconnaissance", "Deploy assets", "Execute primary directive" };
                for(int i=0; i<3; i++)
                {
                    bool isDone = i < op.currentPhase;
                    var oRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4, flexShrink = 0 } };
                    oRow.Add(new Label(isDone ? "\uF058" : "\uF111") { style = { color = isDone ? cGreen : Color.grey, fontSize = 12, marginRight = 6 } }.WithClass("fa-icon"));
                    oRow.Add(new Label(objTxt[i]) { style = { color = isDone ? Color.white : Color.grey, fontSize = 10 } });
                    dRight.Add(oRow);
                }

                var dMetrics = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 12, justifyContent = Justify.SpaceBetween } };
                
                Color rCol = op.riskLevel == "Extreme" || op.riskLevel == "High" ? cRed : op.riskLevel == "Medium" ? cOrange : cGreen;
                var mRisk = new VisualElement { style = { width = Length.Percent(30) } };
                mRisk.Add(new Label("RISK LEVEL") { style = { color = Color.grey, fontSize = 9, marginBottom = 2 } });
                var mrRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                mrRow.Add(new Label("\uf071") { style = { color = rCol, fontSize = 14, marginRight = 4 } }.WithClass("fa-icon"));
                mrRow.Add(new Label(string.IsNullOrEmpty(op.riskLevel) ? "UNKNOWN" : op.riskLevel.ToUpper()) { style = { color = rCol, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });
                mRisk.Add(mrRow);
                dMetrics.Add(mRisk);
                
                var mDet = new VisualElement { style = { width = Length.Percent(30) } };
                mDet.Add(new Label("DETECTION RISK") { style = { color = Color.grey, fontSize = 9, marginBottom = 2 } });
                mDet.Add(new Label($"{Mathf.RoundToInt(op.detectionRisk)}%") { style = { color = Color.white, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });
                var dbBar1 = new VisualElement { style = { height = 3, backgroundColor = cWhiteAlpha, marginTop = 4 } };
                dbBar1.Add(new VisualElement { style = { width = Length.Percent(op.detectionRisk), height = 3, backgroundColor = cOrange } });
                mDet.Add(dbBar1);
                dMetrics.Add(mDet);

                var mSuc = new VisualElement { style = { width = Length.Percent(30) } };
                float sucChance = Mathf.Clamp(100f - op.detectionRisk, 0, 100f);
                mSuc.Add(new Label("SUCCESS CHANCE") { style = { color = Color.grey, fontSize = 9, marginBottom = 2 } });
                mSuc.Add(new Label($"{Mathf.RoundToInt(sucChance)}%") { style = { color = Color.white, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });
                var dbBar2 = new VisualElement { style = { height = 3, backgroundColor = cWhiteAlpha, marginTop = 4 } };
                dbBar2.Add(new VisualElement { style = { width = Length.Percent(sucChance), height = 3, backgroundColor = cGreen } });
                mSuc.Add(dbBar2);
                dMetrics.Add(mSuc);

                dRight.Add(dMetrics);
                detailsContainer.Add(dRight);
            }

            // --- UPCOMING OPERATIONS ---
            var upcomingList = panel.Q<ScrollView>("CO_UpcomingOpsList");
            upcomingList.Clear();
            var upcomingOps = country.activeCovertOps.FindAll(x => x.status == "Planning" || x.status == "On Hold");

            foreach(var op in upcomingOps)
            {
                Color uCol = op.type == "Political" ? cPurple : op.type == "Economic" ? cOrange : op.type == "Military" ? cBlue : op.type == "Influence" ? cGreen : cRed;
                string uIcn = op.type == "Political" ? "\uf0c0" : op.type == "Economic" ? "\uf153" : op.type == "Military" ? "\uf132" : op.type == "Influence" ? "\uf0a1" : "\uf108";
                int dots = op.riskLevel == "Extreme" ? 4 : op.riskLevel == "High" ? 3 : op.riskLevel == "Medium" ? 2 : 1;

                var card = new VisualElement { style = { flexDirection = FlexDirection.Row, backgroundColor = new Color(1,1,1,0.03f), paddingBottom = 8, paddingTop = 8, paddingLeft = 8, paddingRight = 8, marginBottom = 6, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, borderLeftWidth = 2, borderLeftColor = uCol } };
                
                var cIcon = new VisualElement { style = { width = 24, height = 24, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(uCol.r, uCol.g, uCol.b, 0.1f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 8 } };
                cIcon.Add(new Label(uIcn) { style = { color = uCol, fontSize = 12 } }.WithClass("fa-icon"));
                card.Add(cIcon);
                
                var cMid = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };
                cMid.Add(new Label(op.opName) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                cMid.Add(new Label(op.type) { style = { color = Color.grey, fontSize = 9 } });
                cMid.Add(new Label($"Target: {op.target}") { style = { color = Color.grey, fontSize = 9, marginTop = 2 } });
                cMid.Add(new Label($"Start In: {op.monthsRemaining} Months") { style = { color = Color.white, fontSize = 9, marginTop = 2 } });
                card.Add(cMid);
                
                var cRight = new VisualElement { style = { alignItems = Align.FlexEnd, justifyContent = Justify.SpaceBetween } };
                cRight.Add(new Label(op.riskLevel.ToUpper()) { style = { color = dots >= 3 ? cRed : cOrange, fontSize = 9, marginBottom = 12 } });
                var dotRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                for(int d=0; d<4; d++)
                {
                    dotRow.Add(new VisualElement { style = { width = 4, height = 4, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, backgroundColor = d < dots ? (dots >= 3 ? cRed : cOrange) : cWhiteAlpha, marginLeft = 2 } });
                }
                cRight.Add(dotRow);
                card.Add(cRight);
                
                upcomingList.Add(card);
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
                iconBox.Add(new Label("\uf023") { style = { color = raCol[i], fontSize = 10 } }.WithClass("fa-icon"));
                row.Add(iconBox);
                
                var rtRow = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                rtRow.Add(new Label(raTxt[i]) { style = { color = Color.grey, fontSize = 10, whiteSpace = WhiteSpace.Normal, width = Length.Percent(70) } });
                rtRow.Add(new Label(raTime[i]) { style = { color = Color.grey, fontSize = 9 } });
                row.Add(rtRow);
                actList.Add(row);
            }



            PopulateAssetRoster(country, panel);
        }

        private void PopulateAssetRoster(StateCraft.Data.CountryDataSO country, VisualElement panel)
        {
            var rosterList = panel.Q<ScrollView>("CO_AssetRosterList");
            if (rosterList == null) return;
            rosterList.Clear();

            if (country.intelHardware == null || country.intelHardware.Count == 0)
            {
                AddPlaceholder(rosterList, "No intelligence hardware deployed.");
                return;
            }

            UnityEngine.Texture2D satTex = new UnityEngine.Texture2D(2, 2);
            if (System.IO.File.Exists("Assets/UI/Icons/spy_satellite.png"))
                satTex.LoadImage(System.IO.File.ReadAllBytes("Assets/UI/Icons/spy_satellite.png"));

            UnityEngine.Texture2D droneTex = new UnityEngine.Texture2D(2, 2);
            if (System.IO.File.Exists("Assets/UI/Icons/recon_drone.png"))
                droneTex.LoadImage(System.IO.File.ReadAllBytes("Assets/UI/Icons/recon_drone.png"));

            foreach (var hw in country.intelHardware)
            {
                var card = new VisualElement { style = { flexDirection = FlexDirection.Row, backgroundColor = new UnityEngine.Color(1,1,1,0.05f), paddingBottom = 6, paddingTop = 6, paddingLeft = 6, paddingRight = 6, marginBottom = 6, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
                
                var img = new VisualElement { style = { width = 40, height = 40, marginRight = 10, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
                if (hw.type == "Satellite") img.style.backgroundImage = new StyleBackground(satTex);
                else if (hw.type == "Drone") img.style.backgroundImage = new StyleBackground(droneTex);
                card.Add(img);

                var infoCol = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };
                infoCol.Add(new Label(hw.assetName) { style = { color = UnityEngine.Color.white, fontSize = 13, unityFontStyleAndWeight = UnityEngine.FontStyle.Bold } });
                infoCol.Add(new Label(hw.type) { style = { color = UnityEngine.Color.grey, fontSize = 11 } });
                card.Add(infoCol);

                var statusCol = new VisualElement { style = { justifyContent = Justify.Center, alignItems = Align.FlexEnd } };
                if (hw.status == "Building")
                {
                    statusCol.Add(new Label("CONSTRUCTING") { style = { color = new UnityEngine.Color(0.95f, 0.77f, 0.06f), fontSize = 11, unityFontStyleAndWeight = UnityEngine.FontStyle.Bold } });
                    statusCol.Add(new Label($"{hw.monthsRemaining} mo remaining") { style = { color = UnityEngine.Color.grey, fontSize = 10 } });
                }
                else
                {
                    statusCol.Add(new Label("ACTIVE") { style = { color = new UnityEngine.Color(0.137f, 0.929f, 0.337f), fontSize = 11, unityFontStyleAndWeight = UnityEngine.FontStyle.Bold } });
                    statusCol.Add(new Label("Operational") { style = { color = UnityEngine.Color.grey, fontSize = 10 } });
                }
                card.Add(statusCol);

                rosterList.Add(card);
            }
        }

        private VisualElement CreateDetailStat(string label, string value)
        {
            var box = new VisualElement { style = { width = Length.Percent(50), marginBottom = 8, flexShrink = 0 } };
            box.Add(new Label(label) { style = { color = Color.grey, fontSize = 9, marginBottom = 2 } });
            box.Add(new Label(value) { style = { color = Color.white, fontSize = 10, whiteSpace = WhiteSpace.Normal } });
            return box;
        }


        private void LaunchReconMission()
        {
            var root = _uiDoc?.rootVisualElement;
            var drpPriority = root?.Q<UnityEngine.UIElements.DropdownField>("DrpReconPriority");
            string priority = drpPriority != null ? drpPriority.value : "Low";

            UnityEngine.Debug.Log($"Recon Mission Launch Button Clicked! Priority: {priority}");
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.playerBudget < 0.05f)
                {
                    NotificationManager.Instance?.AddNotification("INSUFFICIENT FUNDS: Need $0.05B for Recon Mission.", UnityEngine.Color.red);
                    return;
                }
                gm.playerBudget -= 0.05f;
            }
            
            int delayDays = priority == "Critical" ? 1 : priority == "High" ? 3 : priority == "Medium" ? 7 : 14;
            NotificationManager.Instance?.AddNotification($"RECON MISSION LAUNCHED ({priority} Priority): Aerial assets deployed to scan area. ETA: {delayDays} Days", new UnityEngine.Color(0.2f, 0.8f, 0.2f));

            var country = GameManager.Instance?.playerCountry;
            if (country != null)
            {
                if (country.activeCovertOps == null) country.activeCovertOps = new System.Collections.Generic.List<StateCraft.Data.CovertOperation>();
                var op = new StateCraft.Data.CovertOperation
                {
                    opName = $"Recon Sweep {(char)UnityEngine.Random.Range(65, 91)}{UnityEngine.Random.Range(10, 99)}",
                    type = "Aerial Recon",
                    target = "Eastern Border",
                    operativeName = "Drone Sqn 1",
                    riskLevel = priority,
                    status = "Scanning",
                    detectionRisk = 5f,
                    currentPhase = 1,
                    totalPhases = 1,
                    monthsRemaining = 1,
                    daysRemaining = delayDays,
                    priority = priority
                };
                country.activeCovertOps.Add(op);
            }
            
            RefreshIntelligenceData();
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

            string[] types = { "Military", "Influence", "Political", "Cyber", "Economic" };
            
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
                status = "In Progress",
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
            var panel = _dashboardRoot?.Q<VisualElement>("PanelOverview");
            Debug.Log($"[Overview] PanelOverview found: {panel != null}");
            if (panel == null) { Debug.LogError("[Overview] PanelOverview is NULL — check UXML name matches 'PanelOverview'"); return; }

            // Colors
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);

            // 1. OVERALL THREAT LEVEL
            var threatCard     = panel.Q<VisualElement>("OverviewThreatCard");
            var threatTitle    = panel.Q<Label>("OverviewThreatTitle");
            var threatIcon     = panel.Q<Label>("OverviewThreatIcon");
            var threatLevelLbl = panel.Q<Label>("OverviewThreatLevel");
            var threatSubLbl   = panel.Q<Label>("OverviewThreatSubtext");
            Debug.Log($"[Overview] Threat Level block — ThreatCard:{threatCard != null} | ThreatTitle:{threatTitle != null} | ThreatIcon:{threatIcon != null} | ThreatLevel:{threatLevelLbl != null} | ThreatSub:{threatSubLbl != null}");
            Debug.Log($"[Overview] ExposureRisk = {country.exposureRisk:F1}");

            if (threatLevelLbl != null)
            {
                if (country.exposureRisk > 60)
                {
                    threatLevelLbl.text = "CRITICAL";
                    threatLevelLbl.style.color = cRed;
                    if (threatSubLbl != null) { threatSubLbl.text = "Immediate Action Required"; threatSubLbl.style.color = new Color(cRed.r, cRed.g, cRed.b, 0.6f); }
                    if (threatIcon != null) { threatIcon.text = "\uf071"; threatIcon.style.color = cRed; }
                    if (threatTitle != null) threatTitle.style.color = new Color(cRed.r, cRed.g, cRed.b, 0.8f);
                    if (threatCard != null) threatCard.style.borderTopColor = threatCard.style.borderBottomColor = threatCard.style.borderLeftColor = threatCard.style.borderRightColor = new Color(cRed.r, cRed.g, cRed.b, 0.3f);
                }
                else if (country.exposureRisk > 30)
                {
                    threatLevelLbl.text = "ELEVATED";
                    threatLevelLbl.style.color = cOrange;
                    if (threatSubLbl != null) { threatSubLbl.text = "Monitor Active Threats"; threatSubLbl.style.color = new Color(cOrange.r, cOrange.g, cOrange.b, 0.6f); }
                    if (threatIcon != null) { threatIcon.text = "\uf071"; threatIcon.style.color = cOrange; }
                    if (threatTitle != null) threatTitle.style.color = new Color(cOrange.r, cOrange.g, cOrange.b, 0.8f);
                    if (threatCard != null) threatCard.style.borderTopColor = threatCard.style.borderBottomColor = threatCard.style.borderLeftColor = threatCard.style.borderRightColor = new Color(cOrange.r, cOrange.g, cOrange.b, 0.3f);
                }
                else
                {
                    threatLevelLbl.text = "STABLE";
                    threatLevelLbl.style.color = cGreen;
                    if (threatSubLbl != null) { threatSubLbl.text = "No Immediate Danger Detected"; threatSubLbl.style.color = new Color(cGreen.r, cGreen.g, cGreen.b, 0.6f); }
                    if (threatIcon != null) { threatIcon.text = "\uf058"; threatIcon.style.color = cGreen; }
                    if (threatTitle != null) threatTitle.style.color = new Color(cGreen.r, cGreen.g, cGreen.b, 0.8f);
                    if (threatCard != null) threatCard.style.borderTopColor = threatCard.style.borderBottomColor = threatCard.style.borderLeftColor = threatCard.style.borderRightColor = new Color(cGreen.r, cGreen.g, cGreen.b, 0.3f);
                }
            }
            else
            {
                Debug.LogWarning("[Overview] OverviewThreatLevel label is NULL — Threat Level block will be blank");
            }

            // 2. THREAT MATRIX
            var threatList = panel.Q<ScrollView>("OverviewThreatMatrixList");
            Debug.Log($"[Overview] OverviewThreatMatrixList found: {threatList != null}");
            if (threatList != null)
            {
                threatList.Clear();
                var threats = GameManager.Instance.ThreatAssessmentSystem?.GenerateThreats(country, GameManager.Instance);
                Debug.Log($"[Overview] GenerateThreats returned: {(threats == null ? "NULL" : threats.Count.ToString())} threats");
                if (threats != null && threats.Count > 0)
                {
                    foreach (var th in threats)
                        AddThreatMatrixItem(threatList, th.title, th.region, th.probability, th.impact, th.color);
                }
                else
                {
                    threatList.Add(new Label("No significant threats detected in the current operational theater.") { style = { fontSize = 12, color = Color.grey, whiteSpace = WhiteSpace.Normal, marginTop = 12 } });
                }
            }
            else
            {
                Debug.LogWarning("[Overview] OverviewThreatMatrixList is NULL — Threat Matrix column will be blank");
            }

            // 3. INTEL FEED
            var intelFeed = panel.Q<ScrollView>("OverviewIntelFeedList");
            Debug.Log($"[Overview] OverviewIntelFeedList found: {intelFeed != null}");
            if (intelFeed != null)
            {
                intelFeed.Clear();
                var feedLines = DashboardLiveContent.BuildIntelFeed(country, GameManager.Instance);
                Debug.Log($"[Overview] BuildIntelFeed returned {feedLines?.Count ?? 0} items");
                if (feedLines != null)
                {
                    foreach (var feed in feedLines)
                        AddIntelFeedItem(intelFeed, feed.Tag, feed.Message, feed.Color);
                }
            }
            else
            {
                Debug.LogWarning("[Overview] OverviewIntelFeedList is NULL — Intel Feed column will be blank");
            }

            // 4. BLINDSPOTS
            var blindspotList = panel.Q<ScrollView>("OverviewBlindspotsList");
            Debug.Log($"[Overview] OverviewBlindspotsList found: {blindspotList != null}");
            Debug.Log($"[Overview] Stats — Satellites:{country.satelliteCount} | Operatives:{country.operativeCount} | IntelLevel:{country.intelligenceLevel:F1} | Funds:{country.intelligenceFunds:F1}");
            if (blindspotList != null)
            {
                blindspotList.Clear();
                if (country.satelliteCount == 0) AddBlindspotItem(blindspotList, "ORBITAL COVERAGE", "No active spy satellites. Global troop movements cannot be tracked.", cRed);
                if (country.operativeCount < 5)  AddBlindspotItem(blindspotList, "HUMINT (HUMAN INTELLIGENCE)", "Insufficient field agents. High risk of missing localized radicalization or political plots.", cOrange);
                if (country.intelligenceLevel < 30) AddBlindspotItem(blindspotList, "CYBER NETWORK", "Weak digital surveillance. Vulnerable to foreign cyber intrusions and disinformation.", cRed);

                float totalUpkeep = (country.satelliteCount * 50f) + (country.droneCount * 10f) + (country.operativeCount * 5f);
                if (country.intelligenceFunds < totalUpkeep)
                    AddBlindspotItem(blindspotList, "CRITICAL SHORTAGE", "Intelligence funds are depleted. Operations are halting and Agency Integrity is dropping. Increase the Intelligence Budget immediately.", cRed);

                Debug.Log($"[Overview] Blindspots populated: {blindspotList.childCount} items");
                if (blindspotList.childCount == 0)
                    blindspotList.Add(new Label("Coverage optimal. No critical blindspots currently identified.") { style = { fontSize = 12, color = cGreen, whiteSpace = WhiteSpace.Normal, marginTop = 12 } });
            }
            else
            {
                Debug.LogWarning("[Overview] OverviewBlindspotsList is NULL — Blindspots column will be blank");
            }

            Debug.Log("[Overview] PopulateOverviewTab COMPLETE");
        }

        private void AddThreatMatrixItem(VisualElement container, string title, string region, float prob, string impact, Color color)
        {
            var card = new VisualElement { style = { backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderBottomColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderLeftColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderRightColor = new Color(0.2f, 0.25f, 0.35f, 0.5f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingBottom = 10, paddingTop = 10, paddingLeft = 10, paddingRight = 10, marginBottom = 8 } };
            card.Add(new Label(title) { style = { fontSize = 13, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            card.Add(new Label($"Target Region: {region}") { style = { fontSize = 11, color = Color.grey, marginBottom = 6 } });
            
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            row.Add(new Label($"Probability: {prob}%") { style = { fontSize = 10, color = color } });
            row.Add(new Label($"Impact: {impact}") { style = { fontSize = 10, color = color } });
            card.Add(row);
            
            container.Add(card);
        }

        private void AddIntelFeedItem(VisualElement container, string tag, string message, Color color)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.05f), paddingBottom = 8 } };
            var tagLbl = new Label(tag) { style = { width = 50, fontSize = 11, color = color, unityFontStyleAndWeight = FontStyle.Bold } };
            var msgLbl = new Label(message) { style = { flexGrow = 1, fontSize = 11, color = Color.white, whiteSpace = WhiteSpace.Normal } };
            row.Add(tagLbl);
            row.Add(msgLbl);
            container.Add(row);
        }

        private void AddBlindspotItem(VisualElement container, string title, string desc, Color color)
        {
            var card = new VisualElement { style = { flexDirection = FlexDirection.Row, backgroundColor = new Color(color.r, color.g, color.b, 0.05f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(color.r, color.g, color.b, 0.3f), borderBottomColor = new Color(color.r, color.g, color.b, 0.3f), borderLeftColor = new Color(color.r, color.g, color.b, 0.3f), borderRightColor = new Color(color.r, color.g, color.b, 0.3f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingBottom = 10, paddingTop = 10, paddingLeft = 10, paddingRight = 10, marginBottom = 8 } };
            
            var iconLabel = new Label("\uf06e") { style = { fontSize = 18, color = color, marginRight = 12 } };
            iconLabel.AddToClassList("fa-icon");
            card.Add(iconLabel);
            
            var mid = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
            mid.Add(new Label(title) { style = { fontSize = 12, color = color, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            mid.Add(new Label(desc) { style = { fontSize = 11, color = Color.grey, whiteSpace = WhiteSpace.Normal } });
            card.Add(mid);
            
            container.Add(card);
        }

        private void LaunchSatellite()
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;
            if (country.satelliteCount + country.satellitesUnderConstruction >= 4)
            {
                NotificationManager.Instance?.AddNotification($"MAX CONSTELLATION REACHED. You cannot maintain more than 4 satellites.", UnityEngine.Color.yellow);
                return;
            }
            
            float cost = 500f; // 500M
            if (country.intelligenceFunds < cost)
            {
                NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need $500M in Intelligence Funds to build spy satellite.", UnityEngine.Color.red);
                return;
            }

            country.intelligenceFunds -= cost;
            country.satellitesUnderConstruction++;

            string aName = $"Argus-{UnityEngine.Random.Range(10, 99)}";

            if (country.intelHardware == null) country.intelHardware = new System.Collections.Generic.List<StateCraft.Data.IntelligenceHardware>();
            country.intelHardware.Add(new StateCraft.Data.IntelligenceHardware {
                id = System.Guid.NewGuid().ToString(),
                assetName = aName,
                type = "Satellite",
                status = "Building",
                monthsTotal = 6,
                monthsRemaining = 6
            });
            
            NotificationManager.Instance?.AddNotification($"SPY SATELLITE '{aName}' COMMISSIONED. Construction will take 6 months.", new UnityEngine.Color(186/255f, 104/255f, 200/255f));
            
            RefreshIntelligenceData();
        }

        private void ProcureDrones()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;
            
            float cost = 150f; // 150M
            if (country.intelligenceFunds < cost)
            {
                NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need $150M in Intelligence Funds to procure Recon Drones.", UnityEngine.Color.red);
                return;
            }

            country.intelligenceFunds -= cost;
            country.dronesUnderConstruction++;

            string aName = $"Reaper-Sq-{UnityEngine.Random.Range(1, 9)}";

            if (country.intelHardware == null) country.intelHardware = new System.Collections.Generic.List<StateCraft.Data.IntelligenceHardware>();
            country.intelHardware.Add(new StateCraft.Data.IntelligenceHardware {
                id = System.Guid.NewGuid().ToString(),
                assetName = aName,
                type = "Drone",
                status = "Building",
                monthsTotal = 2,
                monthsRemaining = 2
            });
            
            NotificationManager.Instance?.AddNotification($"RECON DRONES '{aName}' PROCURED. Delivery and setup will take 2 months.", new UnityEngine.Color(35/255f, 237/255f, 86/255f));
            
            RefreshIntelligenceData();
        }

        private void PopulateThreatAssessmentTab(CountryDataSO country)
        {
            Debug.Log($"[IntelligenceDashboard] Populating Threat Assessment Tab... Detected Threats: {country.detectedThreats.Count}");
            var panel = _dashboardRoot?.Q<VisualElement>("PanelThreatBoard");
            if (panel == null) return;

            var cRed = new Color(1f, 0.32f, 0.32f);
            var cOrange = new Color(0.95f, 0.77f, 0.06f);
            var cGreen = new Color(0.137f, 0.929f, 0.337f);
            var cPurple = new Color(0.73f, 0.41f, 0.78f);
            var cBlue = new Color(0.18f, 0.6f, 0.95f);
            var cWhiteAlpha = new Color(1, 1, 1, 0.1f);

            var gm = StateCraft.Core.GameManager.Instance;
            var threats = gm.ThreatAssessmentSystem.GenerateThreats(country, gm);

            // --- KPIs ---
            panel.Q<Label>("TA_ActiveThreats").text = threats.Count.ToString();
            int criticals = 0;
            foreach (var t in threats) if (t.impact == "Critical") criticals++;
            panel.Q<Label>("TA_CriticalThreats").text = criticals.ToString();
            
            int concernCount = 0;
            if (gm.allCountries != null)
            {
                foreach(var c in gm.allCountries) {
                    if (c != country && gm.WarSystem != null && gm.WarSystem.GetWarStage(c) >= WarStage.Tension)
                        concernCount++;
                }
            }
            panel.Q<Label>("TA_CountriesConcern").text = concernCount.ToString();

            // Dynamic Trends for KPIs
            bool isHighThreat = threats.Count >= 3 || criticals > 0;
            bool isMediumThreat = threats.Count > 0 && !isHighThreat;
            
            var overallLvl = panel.Q<Label>("TA_OverallLevel");
            var overallTrend = panel.Q<Label>("TA_OverallTrend");
            if (overallLvl != null) {
                overallLvl.text = isHighThreat ? "HIGH" : (isMediumThreat ? "ELEVATED" : "LOW");
                overallLvl.style.color = isHighThreat ? cRed : (isMediumThreat ? cOrange : cGreen);
            }
            if (overallTrend != null) {
                overallTrend.text = isHighThreat ? "↗ vs last month" : (isMediumThreat ? "→ stable" : "↘ vs last month");
                overallTrend.style.color = isHighThreat ? cRed : (isMediumThreat ? Color.gray : cGreen);
            }

            var activeTrend = panel.Q<Label>("TA_ActiveTrend");
            if (activeTrend != null) {
                activeTrend.text = threats.Count > 0 ? $"↗ {Mathf.Max(1, threats.Count/2)} new" : "No change";
                activeTrend.style.color = threats.Count > 0 ? cRed : Color.gray;
            }

            var critTrend = panel.Q<Label>("TA_CriticalTrend");
            if (critTrend != null) {
                critTrend.text = criticals > 0 ? $"↗ {criticals} new" : "No change";
                critTrend.style.color = criticals > 0 ? cRed : Color.gray;
            }

            var concernTrend = panel.Q<Label>("TA_CountriesTrend");
            if (concernTrend != null) {
                concernTrend.text = concernCount > 0 ? $"↗ {Mathf.Max(1, concernCount/3)} new" : "No change";
                concernTrend.style.color = concernCount > 0 ? cRed : Color.gray;
            }

            var trend30 = panel.Q<Label>("TA_Trend30");
            if (trend30 != null) {
                trend30.text = isHighThreat ? "RISING" : (isMediumThreat ? "STABLE" : "DECLINING");
                trend30.style.color = isHighThreat ? cRed : (isMediumThreat ? cOrange : cGreen);
            }

            // --- THREAT MATRIX ---
            var matrixContainer = panel.Q<VisualElement>("TA_MatrixGrid")?.parent;
            var matrix = panel.Q<VisualElement>("TA_MatrixGrid");
            if (matrix != null)
            {
                // Clear and rebuild the whole container to add axis labels
                matrix.Clear();
                Color[] mColors = { cGreen, cGreen, cOrange, cOrange,
                                    cGreen, cOrange, cOrange, cRed,
                                    cOrange, cOrange, cRed, cRed,
                                    cOrange, cRed, cRed, cRed };
                string[] colHeaders = { "LOW", "MED", "HIGH", "CRIT" };
                string[] rowHeaders = { "RARE", "LIKELY", "PROBABLE", "CERTAIN" };

                // Column axis header (Probability →)
                var colAxisRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2, marginLeft = 55 } };
                colAxisRow.Add(new Label("← IMPACT →") { style = { color = new Color(1,1,1,0.35f), fontSize = 9, flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter } });
                matrix.Add(colAxisRow);

                // Column header labels
                var colLabelRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4, marginLeft = 55 } };
                foreach (var h in colHeaders)
                    colLabelRow.Add(new Label(h) { style = { color = new Color(1,1,1,0.4f), fontSize = 9, width = Length.Percent(23), marginRight = Length.Percent(2), unityTextAlign = TextAnchor.MiddleCenter } });
                matrix.Add(colLabelRow);

                // Grid rows with row axis label
                for (int row = 0; row < 4; row++)
                {
                    var rowEl = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5, alignItems = Align.Center } };
                    rowEl.Add(new Label(rowHeaders[row]) { style = { color = new Color(1,1,1,0.4f), fontSize = 8, width = 50, unityTextAlign = TextAnchor.MiddleRight, marginRight = 5 } });
                    for (int col = 0; col < 4; col++)
                    {
                        int i = row * 4 + col;
                        var box = new VisualElement { style = { width = Length.Percent(23), height = 22, marginRight = col < 3 ? Length.Percent(2) : new Length(0), backgroundColor = mColors[i], borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, alignItems = Align.Center, justifyContent = Justify.Center } };
                        box.Add(new Label($"{i+1}") { style = { color = new Color(1,1,1,0.6f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                        rowEl.Add(box);
                    }
                    matrix.Add(rowEl);
                }

                // Row axis label (Probability ↑)
                var rowAxisLbl = new Label("← PROBABILITY →") { style = { color = new Color(1,1,1,0.35f), fontSize = 9, marginTop = 2, unityTextAlign = TextAnchor.MiddleCenter } };
                matrix.Add(rowAxisLbl);
            }

            // --- TOP THREATS ---
            var tList = panel.Q<ScrollView>("TA_TopThreatsList");
            tList.Clear();
            for(int i=0; i<threats.Count; i++)
            {
                var t = threats[i];
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingBottom = 6, paddingTop = 6, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                row.Add(new Label((i+1).ToString()) { style = { color = Color.grey, fontSize = 10, width = 16 } });
                row.Add(new Label(t.title) { style = { color = Color.white, fontSize = 10, width = 124 } });
                row.Add(new Label(t.region) { style = { color = Color.grey, fontSize = 10, flexGrow = 1 } });
                
                // Impact dots
                var impCol = new VisualElement { style = { flexDirection = FlexDirection.Row, width = 60 } };
                for(int d=0; d<4; d++) impCol.Add(new VisualElement { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = (d < (t.impact == "Critical" ? 4 : t.impact == "High" ? 3 : 2)) ? (t.impact == "Critical" ? cRed : cOrange) : cWhiteAlpha, marginRight = 2 } });
                row.Add(impCol);
                
                // Likelihood dots
                var likCol = new VisualElement { style = { flexDirection = FlexDirection.Row, width = 60 } };
                for(int d=0; d<4; d++) likCol.Add(new VisualElement { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = (d < (int)(t.probability/25f)) ? (t.impact == "Critical" ? cRed : cOrange) : cWhiteAlpha, marginRight = 2 } });
                row.Add(likCol);
                
                // Trend
                row.Add(new Label(t.impact == "Medium" ? "->" : "^") { style = { color = t.impact == "Medium" ? cOrange : cRed, fontSize = 12, width = 40, unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold } });
                
                // Risk Level
                row.Add(new Label(t.impact.ToUpper()) { style = { color = t.impact == "Critical" ? cRed : cOrange, fontSize = 10, width = 60, unityTextAlign = TextAnchor.MiddleRight, unityFontStyleAndWeight = FontStyle.Bold } });
                tList.Add(row);
            }

            // --- CATEGORIES ---
            panel.Q<Label>("TA_CatDonut").text = threats.Count.ToString();
            var cList = panel.Q<VisualElement>("TA_CategoryList");
            cList.Clear();
            string[] cNames = { "Terrorism", "Political", "Economic", "Cyber" };
            Color[] cColors = { cRed, cOrange, cOrange, cPurple };
            
            int cTerrorism = threats.FindAll(t => t.title.Contains("Insurgency") || t.title.Contains("Terror")).Count;
            int cPolitical = threats.FindAll(t => t.title.Contains("Conflict") || t.title.Contains("Clash") || t.title.Contains("Build-up")).Count;
            int cEconomic = threats.FindAll(t => t.title.Contains("Crime") || t.title.Contains("Smuggling")).Count;
            int cCyber = threats.FindAll(t => t.title.Contains("Cyber") || t.title.Contains("Exposure")).Count;
            
            // Assign leftovers to Political
            int totalCategorized = cTerrorism + cPolitical + cEconomic + cCyber;
            if (threats.Count > totalCategorized) cPolitical += (threats.Count - totalCategorized);

            int GetPct(int val) => threats.Count > 0 ? (val * 100 / threats.Count) : 0;

            string[] cVals = { 
                $"{cTerrorism} ({GetPct(cTerrorism)}%)", 
                $"{cPolitical} ({GetPct(cPolitical)}%)", 
                $"{cEconomic} ({GetPct(cEconomic)}%)", 
                $"{cCyber} ({GetPct(cCyber)}%)" 
            };
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 4 } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new VisualElement { style = { width = 6, height = 6, backgroundColor = cColors[i], marginRight = 6 } });
                rL.Add(new Label(cNames[i]) { style = { color = Color.white, fontSize = 10 } });
                row.Add(rL);
                row.Add(new Label(cVals[i]) { style = { color = Color.grey, fontSize = 10 } });
                cList.Add(row);
            }

            // --- REGIONAL THREAT OVERVIEW ---
            var rList = panel.Q<VisualElement>("TA_RegionList");
            rList.Clear();
            if (country.regions != null && country.regions.Count > 0)
            {
                for(int i=0; i<country.regions.Count && i<6; i++)
                {
                    var reg = country.regions[i];
                    if (reg == null) continue;
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                    
                    var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 110 } };
                    rL.Add(new Label(reg.regionName ?? "Region") { style = { color = Color.grey, fontSize = 10, whiteSpace = WhiteSpace.Normal } });
                    row.Add(rL);
                    
                    var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                    float risk = Mathf.Clamp(100f - reg.security, 0f, 100f);
                    barBg.Add(new VisualElement { style = { width = Length.Percent(risk), height = 4, backgroundColor = risk > 70f ? cRed : risk > 40f ? cOrange : cGreen, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                    row.Add(barBg);
                    
                    string rLvl = risk > 70f ? "HIGH" : risk > 40f ? "MEDIUM" : "LOW";
                    row.Add(new Label(rLvl) { style = { color = risk > 70f ? cRed : risk > 40f ? cOrange : cGreen, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, width = 50, unityTextAlign = TextAnchor.MiddleRight } });
                    rList.Add(row);
                }
            }

              // --- THREAT LEVEL OVER TIME (TREND CHART) ---
              var chart = panel.Q<VisualElement>("TA_TrendChart");
              if (chart != null)
              {
                  chart.Clear();
                  int baseThreats = Mathf.Max(1, threats.Count);
                  for(int i=0; i<30; i++)
                  {
                      var barCont = new VisualElement { style = { height = Length.Percent(100), width = 12, justifyContent = Justify.FlexEnd, alignItems = Align.Center } };
                      
                      float noise = UnityEngine.Random.Range(0.5f, 1.5f);
                      float trendVal = baseThreats * noise * 12f; 
                      if (trendVal < 10f) trendVal = 10f;
                      if (trendVal > 95f) trendVal = 95f;

                      var bar = new VisualElement { style = { width = 8, height = Length.Percent(trendVal), backgroundColor = trendVal > 70f ? cRed : trendVal > 40f ? cOrange : cBlue, borderTopLeftRadius = 2, borderTopRightRadius = 2 } };
                      barCont.Add(bar);
                      
                      if (i % 6 == 0) barCont.Add(new Label($"{i+1} DAY") { style = { color = Color.grey, fontSize = 8, marginTop = 4 } });
                      else barCont.Add(new Label("") { style = { fontSize = 8, marginTop = 4 } });
                      
                      chart.Add(barCont);
                  }
              }
                
              // --- MITIGATION PROGRESS ---
              var mitList = panel.Q<VisualElement>("TA_MitigationList");
              if (mitList != null)
              {
                  mitList.Clear();
                  int activeOps = 0;
                  
                  if (country.activeOperations != null) {
                      foreach (var op in country.activeOperations) {
                          var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6, paddingBottom=4, borderBottomWidth=1, borderBottomColor=cWhiteAlpha } };
                          row.Add(new Label($"Mil Op: {op.opName}") { style = { color = Color.white, fontSize = 12, width=120 } });
                          row.Add(new Label(op.regionName) { style = { color = Color.grey, fontSize = 10, flexGrow=1 } });
                          row.Add(new Label($"{op.objectiveProgress:F0}%") { style = { color = cGreen, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                          mitList.Add(row);
                          activeOps++;
                      }
                  }
                  
                  if (country.activeCovertOps != null) {
                      foreach (var op in country.activeCovertOps) {
                          var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6, paddingBottom=4, borderBottomWidth=1, borderBottomColor=cWhiteAlpha } };
                          row.Add(new Label($"Intel: {op.opName}") { style = { color = Color.white, fontSize = 12, width=120 } });
                          row.Add(new Label(op.target) { style = { color = Color.grey, fontSize = 10, flexGrow=1 } });
                          float prog = op.totalPhases > 0 ? (float)op.currentPhase / op.totalPhases * 100f : 0f;
                          row.Add(new Label($"{prog:F0}%") { style = { color = cBlue, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                          mitList.Add(row);
                          activeOps++;
                      }
                  }
                  
                  if (activeOps == 0) {
                      AddPlaceholder(mitList, "No active mitigation operations.");
                  }
              }

              // --- RECOMMENDATIONS ---
              var recList = panel.Q<VisualElement>("TA_RecommendationsList");
              if (recList != null)
              {
                  recList.Clear();
                  if (threats.Count == 0) {
                      AddPlaceholder(recList, "No immediate recommendations. Maintain vigilance.");
                  } else {
                      foreach (var t in threats) {
                          string recText = "";
                          if (t.title.Contains("Conflict") || t.title.Contains("Build-up"))
                              recText = $"Deploy additional military forces near {t.region}.";
                          else if (t.title.Contains("Crime") || t.title.Contains("Insurgency"))
                              recText = $"Increase internal security budget in {t.region}.";
                          else if (t.title.Contains("Exposure") || t.title.Contains("Cyber"))
                              recText = "Initiate counter-intelligence sweeps to identify leaks.";
                          else 
                              recText = $"Monitor {t.region} closely for escalating tensions.";
                              
                          var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6, paddingBottom=4, borderBottomWidth=1, borderBottomColor=cWhiteAlpha } };
                          row.Add(new Label("\uf0a9") { style = { color = cOrange, fontSize = 12, marginRight = 8 } }.WithClass("fa-icon")); // arrow-right
                          row.Add(new Label(recText) { style = { color = Color.white, fontSize = 11, whiteSpace = WhiteSpace.Normal, flexGrow=1 } });
                          recList.Add(row);
                      }
                  }
              }

        }

        private void PopulateDataAnalysisTab(CountryDataSO country)
        {
            Debug.Log($"[IntelligenceDashboard] Populating Data Analysis Tab... Agency Integrity: {country.agencyIntegrity}, Corruption: {country.corruptionIndex}");
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
            int dataSources = Mathf.Clamp(10 + Mathf.RoundToInt(country.intelligenceLevel * 0.5f) + country.satelliteCount * 2, 10, 150);
            if (panel.Q<Label>("DA_DataSources") != null) panel.Q<Label>("DA_DataSources").text = dataSources.ToString();
            
            float dataPoints = Mathf.Clamp(0.5f + (country.intelligenceLevel * 0.05f) + (country.satelliteCount * 0.1f), 0.5f, 10f);
            if (panel.Q<Label>("DA_DataPoints") != null) panel.Q<Label>("DA_DataPoints").text = $"{dataPoints:F2}M";
            
            float accuracy = Mathf.Clamp(40f + (country.intelligenceLevel * 0.4f) + (100f - country.corruptionIndex) * 0.2f, 10f, 98f);
            if (panel.Q<Label>("DA_Accuracy") != null) panel.Q<Label>("DA_Accuracy").text = $"{accuracy:F0}%";
            
            int insights = Mathf.RoundToInt((accuracy / 100f) * dataSources * 0.4f);
            if (panel.Q<Label>("DA_Insights") != null) panel.Q<Label>("DA_Insights").text = insights.ToString();
            
            float budget = country.covertBudget;
            string daSym = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;
            float budgetLocal = budget * country.exchangeRate;
            if (panel.Q<Label>("DA_Budget") != null) panel.Q<Label>("DA_Budget").text = $"{UiFormat.Money(budgetLocal * 0.8f, daSym)} / month";
            if (panel.Q<Label>("DA_BudgetRem") != null) panel.Q<Label>("DA_BudgetRem").text = $"{UiFormat.Money(budgetLocal * 0.32f, daSym)} remaining";

            // --- DATA SOURCES OVERVIEW ---
            var srcList = panel.Q<VisualElement>("DA_SourcesList");
            srcList.Clear();
            string[] sNames = { "Human Intelligence (HUMINT)", "Signals Intelligence (SIGINT)", "Imagery Intelligence (IMINT)", "Open Source Intelligence (OSINT)", "Cyber Intelligence (CYBINT)", "Financial Intelligence (FININT)", "Technical Intelligence (TECHINT)" };
            int[] sVals = { 
                Mathf.Clamp(Mathf.RoundToInt(country.intelligenceLevel * 0.8f + country.operativeCount * 1.5f), 10, 100), 
                Mathf.Clamp(Mathf.RoundToInt(country.globalIntelCoverage * 0.7f + 20f), 10, 100), 
                Mathf.Clamp(Mathf.RoundToInt(country.satelliteCount * 15f + 10f), 10, 100), 
                Mathf.Clamp(Mathf.RoundToInt(country.intelligenceLevel * 0.9f), 10, 100), 
                Mathf.Clamp(Mathf.RoundToInt(country.intelligenceLevel * 0.6f + 20f), 10, 100), 
                Mathf.Clamp(Mathf.RoundToInt(100f - country.corruptionIndex), 10, 100), 
                Mathf.Clamp(Mathf.RoundToInt(country.globalIntelCoverage * 0.5f + country.intelligenceLevel * 0.3f), 10, 100) 
            };
            Color[] sCols = { cGreen, cBlue, cOrange, cTeal, cPurple, cRed, cOrange };
            for(int i=0; i<7; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
                row.Add(new Label(sNames[i]) { style = { color = Color.grey, fontSize = 10, width = 110, whiteSpace = WhiteSpace.Normal } });
                
                var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                barBg.Add(new VisualElement { style = { width = Length.Percent(sVals[i]), height = 4, backgroundColor = sCols[i], borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                row.Add(barBg);
                
                row.Add(new Label($"{sVals[i]}%") { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, width = 25, unityTextAlign = TextAnchor.MiddleRight } });
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
                
                if (i % 6 == 0) barCont.Add(new Label($"{i+1} JAN") { style = { color = Color.grey, fontSize = 8, marginTop = 4 } });
                else barCont.Add(new Label("") { style = { fontSize = 8, marginTop = 4 } });
                chart.Add(barCont);
            }

            // --- KEY INSIGHTS ---
            var inList = panel.Q<ScrollView>("DA_InsightsList");
            inList.Clear();
            string[] iNames = { "Rising Political Tensions", "Economic Pressure Building", "Cyber Threat Activity Spike", "Smuggling Network Expansion" };
            string[] iDesc = { "Political instability indicators have increased by 24% in the last 30 days.", "Inflation and unemployment data suggest potential economic unrest.", "Cyber attacks targeting government systems up by 32% this week.", "New routes identified along northern corridor and coastal areas." };
            string[] iPri = { "HIGH PRIORITY", "MEDIUM PRIORITY", "HIGH PRIORITY", "MEDIUM PRIORITY" };
            string[] iDates = { "16 JAN 2026", "16 JAN 2026", "16 JAN 2026", "14 JAN 2026" };
            string[] iIco = { "\uf062", "\uf155", "\uf51e", "\uf508" };
            Color[] iCols = { cRed, cOrange, cPurple, cGreen };
            for(int i=0; i<4; i++)
            {
                var card = new VisualElement { style = { backgroundColor = new Color(1,1,1,0.03f), paddingTop = 12, paddingBottom = 12, paddingLeft = 12, paddingRight = 12, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, marginBottom = 8, borderLeftWidth = 2, borderLeftColor = iCols[i] } };
                
                var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
                var iconBox = new VisualElement { style = { width = 24, height = 24, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = new Color(iCols[i].r, iCols[i].g, iCols[i].b, 0.1f), alignItems = Align.Center, justifyContent = Justify.Center, marginRight = 8 } };
                iconBox.Add(new Label(iIco[i]) { style = { color = iCols[i], fontSize = 12 } }.WithClass("fa-icon"));
                topRow.Add(iconBox);
                
                var tRight = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                tRight.Add(new Label(iNames[i]) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                tRight.Add(new Label(iDates[i]) { style = { color = Color.grey, fontSize = 9 } });
                topRow.Add(tRight);
                card.Add(topRow);
                
                card.Add(new Label(iDesc[i]) { style = { color = Color.grey, fontSize = 10, whiteSpace = WhiteSpace.Normal, marginBottom = 6 } });
                card.Add(new Label(iPri[i]) { style = { color = iPri[i] == "HIGH PRIORITY" ? cRed : cOrange, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                inList.Add(card);
            }

            // --- DATA QUALITY ---
            // --- DATA QUALITY ---
            float highQ = Mathf.Clamp(20f + (country.intelligenceLevel * 0.5f), 10f, 60f);
            float medQ = Mathf.Clamp(50f - (country.intelligenceLevel * 0.2f), 20f, 50f);
            float lowQ = Mathf.Clamp(20f - (country.intelligenceLevel * 0.2f), 5f, 20f);
            float unvQ = 100f - (highQ + medQ + lowQ);
            
            panel.Q<Label>("DA_QualityDonut").text = $"{(highQ + medQ):F0}%";
            var qList = panel.Q<VisualElement>("DA_QualityList");
            qList.Clear();
            string[] qNames = { "High Quality", "Medium Quality", "Low Quality", "Unverified" };
            Color[] qCols = { cGreen, cOrange, cRed, Color.grey };
            float[] qVals = { highQ, medQ, lowQ, unvQ };
            for(int i=0; i<4; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 4 } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                rL.Add(new VisualElement { style = { width = 6, height = 6, backgroundColor = qCols[i], marginRight = 6 } });
                rL.Add(new Label(qNames[i]) { style = { color = Color.white, fontSize = 10 } });
                row.Add(rL);
                row.Add(new Label($"{qVals[i]}%") { style = { color = Color.grey, fontSize = 10 } });
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
                row.Add(new Label(gNames[i]) { style = { color = Color.grey, fontSize = 10, width = 80 } });
                
                var rCol = gLvls[i] == "Very High Activity" ? cRed : (gLvls[i] == "High Activity" ? cRed : (gLvls[i] == "Medium Activity" ? cOrange : cGreen));
                row.Add(new Label(gLvls[i]) { style = { color = rCol, fontSize = 9, width = 70, unityFontStyleAndWeight = FontStyle.Bold } });
                
                var barBg = new VisualElement { style = { flexGrow = 1, height = 4, backgroundColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, marginRight = 8, marginLeft = 8 } };
                barBg.Add(new VisualElement { style = { width = Length.Percent(gVals[i]), height = 4, backgroundColor = rCol, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
                row.Add(barBg);
                
                row.Add(new Label($"{gVals[i]}%") { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, width = 25, unityTextAlign = TextAnchor.MiddleRight } });
                gList.Add(row);
            }

            // --- PATTERN DETECTION ---
            var pList = panel.Q<ScrollView>("DA_PatternList");
            pList.Clear();
            string[] pNames = { "Unusual Financial Transactions", "Arms Movement Pattern", "Communications Anomaly", "Social Media Sentiment Shift", "Supply Chain Disruption" };
            string[] pStat = { "DETECTED", "DETECTED", "DETECTED", "MONITORING", "MONITORING" };
            string[] pIco = { "\uf19c", "\uf132", "\uf09e", "\uf0ac", "\uf0d1" };
            for(int i=0; i<5; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, paddingBottom = 6, paddingTop = 6, borderBottomWidth = 1, borderBottomColor = cWhiteAlpha } };
                var rL = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1 } };
                rL.Add(new Label(pIco[i]) { style = { color = Color.white, fontSize = 12, marginRight = 8, width = 14 } }.WithClass("fa-icon"));
                rL.Add(new Label(pNames[i]) { style = { color = Color.white, fontSize = 10 } });
                row.Add(rL);
                
                var sCol = pStat[i] == "DETECTED" ? cRed : cOrange;
                row.Add(new Label(pStat[i]) { style = { color = sCol, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold, paddingTop = 4, paddingBottom = 4, paddingLeft = 4, paddingRight = 4, backgroundColor = new Color(sCol.r, sCol.g, sCol.b, 0.1f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(sCol.r, sCol.g, sCol.b, 0.3f), borderBottomColor = new Color(sCol.r, sCol.g, sCol.b, 0.3f), borderLeftColor = new Color(sCol.r, sCol.g, sCol.b, 0.3f), borderRightColor = new Color(sCol.r, sCol.g, sCol.b, 0.3f), borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } });
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
                iconBox.Add(new Label("\uf00c") { style = { color = cGreen, fontSize = 12 } }.WithClass("fa-icon"));
                row.Add(iconBox);
                
                var tRight = new VisualElement { style = { flexGrow = 1 } };
                tRight.Add(new Label(prNames[i]) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                tRight.Add(new Label(prDesc[i]) { style = { color = Color.grey, fontSize = 9 } });
                row.Add(tRight);
                
                row.Add(new Label("ACTIVE") { style = { color = cGreen, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
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
                row.Add(new Label(rTits[i]) { style = { color = Color.white, fontSize = 10, flexGrow = 1 } });
                row.Add(new Label(rCats[i]) { style = { color = Color.grey, fontSize = 9, width = 70 } });
                row.Add(new Label(rPris[i]) { style = { color = rPris[i] == "HIGH" ? cRed : cOrange, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold, width = 60 } });
                row.Add(new Label(rDats[i]) { style = { color = Color.grey, fontSize = 9, width = 60 } });
                row.Add(new Label("Completed") { style = { color = cGreen, fontSize = 9, width = 60, unityTextAlign = TextAnchor.MiddleRight } });
                repList.Add(row);
            }

            // --- ANALYTIC TOOLS ---
            var tGrid = panel.Q<VisualElement>("DA_ToolsGrid");
            tGrid.Clear();
            string[] tlNames = { "Data Correlation", "Risk Modeling", "Scenario Analysis", "Network Analysis", "Sentiment Analysis", "Predictive Analytics" };
            string[] tlDesc = { "Find relationships", "Predict threats", "What-if scenarios", "Entity connections", "Social monitoring", "Trend forecasting" };
            string[] tlIco = { "\uf0e8", "\uf071", "\uf126", "\uf247", "\uf118", "\uf201" };
            float[] tlCosts = { 10f, 15f, 25f, 12f, 8f, 30f }; // in Millions
            for(int i=0; i<6; i++)
            {
                var btn = new Button { style = { width = Length.Percent(48), height = 36, flexDirection = FlexDirection.Row, justifyContent = Justify.FlexStart, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.02f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = cWhiteAlpha, borderBottomColor = cWhiteAlpha, borderLeftColor = cWhiteAlpha, borderRightColor = cWhiteAlpha, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, marginBottom = 6, paddingLeft = 12 } };
                btn.Add(new Label(tlIco[i]) { style = { color = Color.white, fontSize = 16, marginRight = 10, width = 16 } }.WithClass("fa-icon"));
                var txts = new VisualElement();
                
                float actionCost = tlCosts[i] * country.exchangeRate;
                
                txts.Add(new Label(tlNames[i]) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                txts.Add(new Label($"{tlDesc[i]} ({daSym}{actionCost:F0}M)") { style = { color = Color.grey, fontSize = 9 } });
                btn.Add(txts);
                
                int index = i;
                btn.clicked += () => 
                {
                    if (country.intelligenceFunds < actionCost)
                    {
                        NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need {daSym}{actionCost:F0}M for {tlNames[index]}.", Color.red);
                        return;
                    }
                    country.intelligenceFunds -= actionCost;
                    
                    if (index == 0) country.agencyIntegrity = Mathf.Min(100f, country.agencyIntegrity + 1f);
                    
                    NotificationManager.Instance?.AddNotification($"ANALYSIS INITIATED: {tlNames[index].ToUpper()} running. Deducted {daSym}{actionCost:F0}M.", new Color(0.73f, 0.41f, 0.78f));
                    RefreshIntelligenceData();
                };
                
                tGrid.Add(btn);
            }

            // --- QUICK ACTIONS ---
            var qaList = panel.Q<VisualElement>("DA_QuickActions");
            qaList.Clear();
            string[] qTxts = { "REQUEST DEEP ANALYSIS", "GENERATE THREAT REPORT", "EXPORT INTELLIGENCE DATA", "SHARE WITH STAKEHOLDERS" };
            string[] qIcos = { "\uf002", "\uf15c", "\uf56e", "\uf1e0" };
            float[] qCosts = { 50f, 20f, 5f, 10f }; // in Millions
            for(int i=0; i<4; i++)
            {
                var btn = new Button { style = { height = 28, flexDirection = FlexDirection.Row, justifyContent = Justify.FlexStart, alignItems = Align.Center, backgroundColor = new Color(1,1,1,0.05f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = cWhiteAlpha, borderBottomColor = cWhiteAlpha, borderLeftColor = cWhiteAlpha, borderRightColor = cWhiteAlpha, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, paddingLeft = 12 } };
                btn.Add(new Label(qIcos[i]) { style = { color = cGreen, fontSize = 12, marginRight = 8, width = 16 } }.WithClass("fa-icon"));
                
                float actionCost = qCosts[i] * country.exchangeRate;
                
                btn.Add(new Label($"{qTxts[i]} ({daSym}{actionCost:F0}M)") { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                
                int index = i;
                btn.clicked += () => 
                {
                    if (country.intelligenceFunds < actionCost)
                    {
                        NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need {daSym}{actionCost:F0}M for {qTxts[index]}.", Color.red);
                        return;
                    }
                    country.intelligenceFunds -= actionCost;
                    
                    NotificationManager.Instance?.AddNotification($"ACTION EXECUTED: {qTxts[index].ToUpper()}. Deducted {daSym}{actionCost:F0}M.", new Color(0.18f, 0.6f, 0.95f));
                    RefreshIntelligenceData();
                };
                
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
