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
    public class IntelligenceDashboardController : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        
        private Button _tabSurveillance, _tabCovertOps, _tabThreatBoard, _tabCounterIntel;
        private VisualElement _panelSurveillance, _panelCovertOps, _panelThreatBoard, _panelCounterIntel;
        private List<VisualElement> _allPanels = new List<VisualElement>();

        private Label _headerBudget, _headerOperatives, _headerCoverage, _exposureRiskLabel;
        private VisualElement _regionalCoverageList, _intelGridList, _intelInterceptFeed;
        private VisualElement _covertOpsContainer, _recruitedAssetsList;
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
            if (_uiDoc != null) _root = _uiDoc.rootVisualElement;
            
            var dashboard = _root?.Q<VisualElement>("IntelligenceDashboard");
            if (dashboard != null)
            {
                InitializeElements(dashboard);
                dashboard.style.display = DisplayStyle.None;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshIntelligenceData;
            RefreshIntelligenceData();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshIntelligenceData;
        }

        private void InitializeElements(VisualElement root)
        {
            _tabSurveillance = root.Q<Button>("TabSurveillance");
            _tabCovertOps = root.Q<Button>("TabCovertOps");
            _tabThreatBoard = root.Q<Button>("TabThreatBoard");
            _tabCounterIntel = root.Q<Button>("TabCounterIntel");

            _panelSurveillance = root.Q<VisualElement>("PanelSurveillance");
            _panelCovertOps = root.Q<VisualElement>("PanelCovertOps");
            _panelThreatBoard = root.Q<VisualElement>("PanelThreatBoard");
            _panelCounterIntel = root.Q<VisualElement>("PanelCounterIntel");

            _allPanels.Clear();
            _allPanels.AddRange(new[] { _panelSurveillance, _panelCovertOps, _panelThreatBoard, _panelCounterIntel });

            _tabSurveillance.clicked += () => SwitchTab(0);
            _tabCovertOps.clicked += () => SwitchTab(1);
            _tabThreatBoard.clicked += () => SwitchTab(2);
            _tabCounterIntel.clicked += () => SwitchTab(3);

            _headerBudget     = root.Q<Label>("HeaderIntelBudget");
            _headerOperatives = root.Q<Label>("HeaderOperativeCount");
            _headerCoverage   = root.Q<Label>("GlobalCoverageScore");
            _exposureRiskLabel = root.Q<Label>("ExposureRiskLabel");

            _regionalCoverageList = root.Q<ScrollView>("RegionalCoverageList");
            _intelGridList        = root.Q<VisualElement>("IntelGridList");
            _intelInterceptFeed   = root.Q<ScrollView>("IntelInterceptFeed");
            _covertOpsContainer   = root.Q<VisualElement>("CovertOpsContainer");
            _recruitedAssetsList  = root.Q<VisualElement>("RecruitedAssetsList");
            _foreignThreatsList   = root.Q<VisualElement>("ForeignThreatsList");
            _insurgentThreatsList = root.Q<VisualElement>("InsurgentThreatsList");
            _internalThreatsList  = root.Q<VisualElement>("InternalThreatsList");
            _counterEspionageList = root.Q<VisualElement>("CounterEspionageList");

            _agencyIntegrityBar  = root.Q<ProgressBar>("AgencyIntegrityBar");
            _exposureRiskBar     = root.Q<ProgressBar>("ExposureRiskBar");
            _foreignAgentsLabel  = root.Q<Label>("ForeignAgentsLabel");
            _moleHuntLabel       = root.Q<Label>("MoleHuntLabel");
            _covertBudgetValueLabel = root.Q<Label>("CovertBudgetValueLabel");
            _exposureRiskBarLabel   = root.Q<Label>("ExposureRiskBarLabel");

            _covertBudgetSlider = root.Q<Slider>("CovertBudgetSlider");
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

            root.Q<Button>("BtnLaunchCovertOp")?.RegisterCallback<ClickEvent>(_ => LaunchSampleCovertOp());
            root.Q<Button>("BtnInternalAudit")?.RegisterCallback<ClickEvent>(_ => RunInternalAudit());
            
            var legacyLaunchBtn = root.Q<Button>("BtnLaunchSatellite");
            if (legacyLaunchBtn != null) legacyLaunchBtn.style.display = DisplayStyle.None;
            
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
                if (active) UIStateManager.FixScrollViews(_allPanels[i]);
            }

            var tabs = new[] { _tabSurveillance, _tabCovertOps, _tabThreatBoard, _tabCounterIntel };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                if (i == index) tabs[i].AddToClassList("active-tab");
                else tabs[i].RemoveFromClassList("active-tab");
            }
        }

        public void Show()
        {
            var panel = _root.Q<VisualElement>("IntelligenceDashboard");
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshIntelligenceData();
                UIStateManager.FixScrollViews(panel);
            }
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        public void RefreshIntelligenceData()
        {
            if (GameManager.Instance == null || GameManager.Instance.playerCountry == null) return;
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
                _headerBudget.text = $"{UiFormat.Money(country.intelligenceBudget, sym)} / Turn";
            }
            if (_headerOperatives != null) _headerOperatives.text = country.operativeCount.ToString();
            if (_headerCoverage  != null) _headerCoverage.text   = $"{country.globalIntelCoverage:F0}/100";

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
            if (_covertBudgetSlider != null) _covertBudgetSlider.SetValueWithoutNotify(country.covertBudget);
            if (_covertBudgetValueLabel != null) _covertBudgetValueLabel.text = $"${country.covertBudget:F1}B / Turn";

            if (_agencyIntegrityBar != null)
            {
                _agencyIntegrityBar.value = country.agencyIntegrity;
                _agencyIntegrityBar.title = $"{country.agencyIntegrity:F0}%";
            }

            if (_foreignAgentsLabel != null) _foreignAgentsLabel.text = $"FOREIGN AGENTS DETECTED: {country.foreignAgentsDetected}";
            int activeMoleHunts = Mathf.Max(0, country.foreignAgentsDetected - (country.agencyIntegrity > 70f ? 1 : 0));
            if (_moleHuntLabel != null) _moleHuntLabel.text = $"Active mole hunts: {activeMoleHunts}  |  Agency integrity: {country.agencyIntegrity:F0}%";

            DashboardLiveContent.PopulateFeed(_intelInterceptFeed, DashboardLiveContent.BuildIntelFeed(country, gm), 6);
            PopulateSurveillance(country, gm);
            PopulateIntelGrid(country, gm);
            PopulateCovertOps(country);
            PopulateRecruitedAssets(country);
            PopulateThreatBoard(country, gm);
            PopulateCounterEspionage(country);
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

        private void PopulateCovertOps(CountryDataSO country)
        {
            if (_covertOpsContainer == null) return;
            _covertOpsContainer.Clear();

            if (country.activeCovertOps.Count == 0)
            {
                var emptyContainer = new VisualElement();
                emptyContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
                emptyContainer.style.borderTopWidth = 1; emptyContainer.style.borderBottomWidth = 1; emptyContainer.style.borderLeftWidth = 1; emptyContainer.style.borderRightWidth = 1;
                emptyContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.4f, 0.5f); emptyContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.4f, 0.5f); emptyContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.4f, 0.5f); emptyContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
                emptyContainer.style.borderTopLeftRadius = 4; emptyContainer.style.borderTopRightRadius = 4; emptyContainer.style.borderBottomLeftRadius = 4; emptyContainer.style.borderBottomRightRadius = 4;
                emptyContainer.style.paddingTop = 15; emptyContainer.style.paddingBottom = 15;
                emptyContainer.style.alignItems = Align.Center; emptyContainer.style.marginTop = 10;
                emptyContainer.Add(new Label("NO ACTIVE COVERT OPERATIONS") { style = { color = new Color(0.4f, 0.4f, 0.5f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 2f } });
                emptyContainer.Add(new Label("Dispatch agents below to initiate field missions.") { style = { color = new Color(0.5f, 0.5f, 0.6f), fontSize = 9, marginTop = 4 } });
                _covertOpsContainer.Add(emptyContainer);
                return;
            }

            foreach (var op in country.activeCovertOps)
                AddCovertOpCard(op);
        }

        private void AddCovertOpCard(CovertOperation op)
        {
            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.flexDirection = FlexDirection.Row;
            card.style.justifyContent = Justify.SpaceBetween;
            card.style.marginBottom = 10;
            card.style.paddingTop = 15;
            card.style.paddingBottom = 15;
            card.style.paddingLeft = 15;
            card.style.paddingRight = 15;
            card.style.backgroundColor = new Color(0.05f, 0.04f, 0.09f, 0.98f);
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = new Color(0.55f, 0.22f, 0.72f, 0.5f);
            card.style.borderBottomColor = new Color(0.55f, 0.22f, 0.72f, 0.5f);
            card.style.borderLeftColor = new Color(0.55f, 0.22f, 0.72f, 0.5f);
            card.style.borderRightColor = new Color(0.55f, 0.22f, 0.72f, 0.5f);
            card.style.borderTopLeftRadius = 4; card.style.borderTopRightRadius = 4;
            card.style.borderBottomLeftRadius = 4; card.style.borderBottomRightRadius = 4;

            var info = new VisualElement();
            info.Add(new Label($"{op.opName} [{op.type}]") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            info.Add(new Label($"Target: {op.target} · {op.operativeName}") { style = { color = Color.grey, fontSize = 9, marginTop = 4 } });
            card.Add(info);

            var status = new VisualElement();
            status.Add(new Label(op.status.ToUpper()) { style = { color = new Color(186/255f, 104/255f, 200/255f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
            status.Add(new Label($"RISK: {op.riskLevel}") { style = { color = Color.grey, fontSize = 9, marginTop = 4 } });
            card.Add(status);
            _covertOpsContainer.Add(card);
        }

        private void PopulateRecruitedAssets(CountryDataSO country)
        {
            if (_recruitedAssetsList == null) return;
            _recruitedAssetsList.Clear();

            if (country.recruitedAssets == null || country.recruitedAssets.Count == 0)
            {
                var emptyContainer = new VisualElement();
                emptyContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
                emptyContainer.style.borderTopWidth = 1; emptyContainer.style.borderBottomWidth = 1; emptyContainer.style.borderLeftWidth = 1; emptyContainer.style.borderRightWidth = 1;
                emptyContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.4f, 0.5f); emptyContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.4f, 0.5f); emptyContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.4f, 0.5f); emptyContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
                emptyContainer.style.borderTopLeftRadius = 4; emptyContainer.style.borderTopRightRadius = 4; emptyContainer.style.borderBottomLeftRadius = 4; emptyContainer.style.borderBottomRightRadius = 4;
                emptyContainer.style.paddingTop = 15; emptyContainer.style.paddingBottom = 15;
                emptyContainer.style.alignItems = Align.Center; emptyContainer.style.marginTop = 10;
                emptyContainer.Add(new Label("NO ASSETS RECRUITED") { style = { color = new Color(0.4f, 0.4f, 0.5f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 2f } });
                emptyContainer.Add(new Label("Run 'Asset Recruitment' covert ops to plant moles.") { style = { color = new Color(0.5f, 0.5f, 0.6f), fontSize = 9, marginTop = 4 } });
                _recruitedAssetsList.Add(emptyContainer);
                return;
            }

            foreach (var asset in country.recruitedAssets)
            {
                var row = new VisualElement();
                row.AddToClassList("sub-panel");
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.paddingTop = 12; row.style.paddingBottom = 12;
                row.style.paddingLeft = 14; row.style.paddingRight = 14;
                row.style.marginBottom = 6;
                row.style.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.98f);
                row.style.borderTopWidth = 1; row.style.borderBottomWidth = 1;
                row.style.borderLeftWidth = 1; row.style.borderRightWidth = 1;
                row.style.borderTopColor = new Color(0.3f, 0.3f, 0.45f, 0.35f);
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.45f, 0.35f);
                row.style.borderLeftColor = new Color(0.3f, 0.3f, 0.45f, 0.35f);
                row.style.borderRightColor = new Color(0.3f, 0.3f, 0.45f, 0.35f);
                row.style.borderTopLeftRadius = 3; row.style.borderTopRightRadius = 3;
                row.style.borderBottomLeftRadius = 3; row.style.borderBottomRightRadius = 3;

                var info = new VisualElement();
                info.Add(new Label(asset.assetName) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                info.Add(new Label($"Inside: {asset.targetCountry}  ·  Intel Tier: {asset.intelLevelProvided}/5")
                    { style = { color = Color.grey, fontSize = 8, marginTop = 3 } });
                row.Add(info);

                var loyaltyBlock = new VisualElement { style = { alignItems = Align.FlexEnd } };
                loyaltyBlock.Add(new Label("LOYALTY") { style = { color = Color.grey, fontSize = 8 } });
                var loyaltyBar = new ProgressBar { value = asset.loyalty, highValue = 100, style = { width = 80, height = 5, marginTop = 4 } };
                loyaltyBlock.Add(loyaltyBar);
                row.Add(loyaltyBlock);

                _recruitedAssetsList.Add(row);
            }
        }

        private void PopulateProxyWarPanel(CountryDataSO country, GameManager gm)
        {
        }

        private void PopulateThreatBoard(CountryDataSO country, GameManager gm)
        {
            if (_foreignThreatsList == null || _insurgentThreatsList == null || _internalThreatsList == null) return;
            _foreignThreatsList.Clear();
            _insurgentThreatsList.Clear();
            _internalThreatsList.Clear();

            bool anyCampsDetected = false;
            foreach (var region in country.regions)
            {
                if (!region.insurgentCampDetected) continue;
                anyCampsDetected = true;
                _insurgentThreatsList.Add(BuildCampThreatCard(region, country));
            }

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

            if (region.campStrength > 70f && !strikeOrdered)
            {
                var warn = new Label("🔴 CAMP APPROACHING FULL STRENGTH — strike window closing");
                warn.style.color = new Color(1f, 0.35f, 0.35f);
                warn.style.fontSize = 8;
                warn.style.marginBottom = 8;
                card.Add(warn);
            }

            if (!strikeOrdered)
            {
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

                    float cost = 0.05f;
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

                    gm.playerBudget -= cost;
                    country.treasury = gm.playerBudget;
                    country.operativeCount--;
                    country.exposureRisk = Mathf.Min(100f, country.exposureRisk + 3f);

                    string effectDesc = "";
                    if (threat.category == "Foreign Interference")
                    {
                        country.globalIntelCoverage = Mathf.Min(100f, country.globalIntelCoverage + 10f);
                        country.security = Mathf.Min(100f, country.security + 8f);
                        effectDesc = $"Counter-intelligence net active. Intel Coverage +10%, Security +8%.";

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
                    else
                    {
                        country.agencyIntegrity = Mathf.Min(100f, country.agencyIntegrity + 15f);
                        country.corruptionIndex = Mathf.Max(0f, country.corruptionIndex - 5f);
                        effectDesc = $"Internal audit & purge active. Agency Integrity +15%, Corruption -5%.";
                    }

                    NotificationManager.Instance?.AddNotification($"ASSET DEPLOYED: {threat.threatName}. {effectDesc}", new Color(186/255f, 104/255f, 200/255f));
                    NewsSystem.Instance?.PublishNews($"INTELLIGENCE: Assets deployed to address {threat.threatName}.");
                    
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

        private void PopulateCounterEspionage(CountryDataSO country)
        {
            if (_counterEspionageList == null) return;
            _counterEspionageList.Clear();

            if (country.foreignAgentsDetected == 0)
            {
                var box = new VisualElement();
                box.style.paddingTop = 20; box.style.paddingBottom = 20;
                box.style.paddingLeft = 12; box.style.paddingRight = 12;
                box.style.marginTop = 8;
                box.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.5f);
                box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1; box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
                box.style.borderTopColor = new Color(0.2f, 0.2f, 0.3f, 0.3f); box.style.borderBottomColor = new Color(0.2f, 0.2f, 0.3f, 0.3f); box.style.borderLeftColor = new Color(0.2f, 0.2f, 0.3f, 0.3f); box.style.borderRightColor = new Color(0.2f, 0.2f, 0.3f, 0.3f);
                box.style.borderTopLeftRadius = 4; box.style.borderTopRightRadius = 4; box.style.borderBottomLeftRadius = 4; box.style.borderBottomRightRadius = 4;
                box.style.alignItems = Align.Center;
                box.Add(new Label("NO ACTIVE CASES") { style = { color = new Color(0.4f, 0.4f, 0.5f), fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 2f } });
                box.Add(new Label("Counter-espionage grid operating cleanly.") { style = { color = new Color(0.5f, 0.5f, 0.6f), fontSize = 9, marginTop = 4 } });
                _counterEspionageList.Add(box);
                return;
            }

            for (int i = 0; i < country.foreignAgentsDetected; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("sub-panel");
                row.style.paddingTop = 12;
                row.style.paddingBottom = 12;
                row.style.paddingLeft = 14;
                row.style.paddingRight = 14;
                row.style.marginBottom = 8;
                row.style.backgroundColor = new Color(0.08f, 0.04f, 0.05f, 0.98f);
                row.style.borderLeftWidth = 3;
                row.style.borderLeftColor = new Color(1f, 0.32f, 0.32f, 0.8f);
                row.style.borderTopWidth = 1; row.style.borderBottomWidth = 1; row.style.borderRightWidth = 1;
                row.style.borderTopColor = new Color(0.3f, 0.15f, 0.15f, 0.5f);
                row.style.borderBottomColor = new Color(0.3f, 0.15f, 0.15f, 0.5f);
                row.style.borderRightColor = new Color(0.3f, 0.15f, 0.15f, 0.5f);
                row.style.borderTopLeftRadius = 3; row.style.borderTopRightRadius = 3;
                row.style.borderBottomLeftRadius = 3; row.style.borderBottomRightRadius = 3;

                row.Add(new Label($"CASE FILE #{i + 1:000}") { style = { color = new Color(1f, 0.55f, 0.55f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                row.Add(new Label("Suspected foreign handler — surveillance authorized") { style = { color = Color.grey, fontSize = 9, marginTop = 4 } });

                var bar = new ProgressBar() { value = Random.Range(20f, 85f), highValue = 100, style = { height = 4, marginTop = 8 } };
                row.Add(bar);
                _counterEspionageList.Add(row);
            }
        }

        private void LaunchSampleCovertOp()
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;

            var rivals = gm.allCountries.Where(c => c != country && gm.WarSystem != null && gm.WarSystem.GetWarStage(c) != WarStage.Peace).ToList();
            if (rivals.Count == 0) rivals = gm.allCountries.Where(c => c != country).ToList();
            var targetCountry = rivals.Count > 0 ? rivals[Random.Range(0, rivals.Count)].countryName : "Unknown";

            string[] types = { "Sabotage", "Propaganda", "Asset Recruitment", "Assassination" };
            
            float opCost = 0.1f;
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
            float auditCost = 0.05f;
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

        private void LaunchSatellite()
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;
            if (country.satelliteCount + country.satellitesUnderConstruction >= 4) return;
            
            float cost = 0.5f;
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
    }
}
