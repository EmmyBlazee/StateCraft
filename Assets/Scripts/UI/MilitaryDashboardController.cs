using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    [DefaultExecutionOrder(-10)]
    public partial class MilitaryDashboardController : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        private VisualElement _dashboardRoot;
        
        private Button _tabForces, _tabRecruit, _tabWeapons, _tabBases, _tabOps, _tabProjects, _tabLogistics, _tabBudget;
        private VisualElement _panelForces, _panelRecruit, _panelWeapons, _panelBases, _panelOps, _panelProjects, _panelLogistics, _panelBudget;
        private List<VisualElement> _allPanels = new List<VisualElement>();

private Label _headerCountryName;
        private Label _headerBudget, _headerReadiness, _threatLevelBadge, _headerKIA;
        private Label _headerFirepower, _headerTotalPersonnel, _headerBasesCount, _headerOperationsCount;

        private ScrollView _generalsContainer, _blockadesContainer;
        private Button _btnRecruitGeneral, _btnDeclareBlockade;

        private VisualElement _branchContainer, _regionPoliceList, _recruitmentGrid;
        private VisualElement _personnelGrid, _weaponsGrid, _basesGrid;
        private Label _milBriefing;
        private VisualElement _milLiveFeed;
        private ScrollView _threatList;
        private ScrollView _opsList;
        private ScrollView _feedList;
        private VisualElement _opTypesGrid;
        private VisualElement _readinessList;

        private VisualElement _theatreStatusList;
        private VisualElement _activeOpsList;
        private VisualElement _globalPowerList;
        private ScrollView _activeProjectsList, _availableProjectsList;
        private ScrollView _budgetLedgerList;

        private ProgressBar _wmdProgressBar;
        private Label _wmdStatusLabel, _wmdOperationalBadge;

        private ProgressBar _fuelBar, _ammoBar, _medicalBar, _foodBar, _elecBar;
        private VisualElement _supplyRouteList;
        private Label _natFuelStock, _natFoodStock, _natElecStock;

private Label _doctrineDesc;
        private Label _postureLabel;
        private VisualElement _postureIcon;
        private string _currentPosture = "DETERRENCE";

        private void Awake()
        {
            _uiDoc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            BindActiveDashboardRoot();
        }

        private void BindActiveDashboardRoot()
        {
            var activePanel = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Military");
            if (activePanel != null)
            {
                _dashboardRoot = activePanel;
                _root = activePanel;
            }
            else
            {
                if (_uiDoc == null) _uiDoc = GetComponent<UIDocument>();
                if (_uiDoc != null) _root = _uiDoc.rootVisualElement;
                _dashboardRoot = _root?.Q<VisualElement>("MilitaryDashboard");
            }

            if (_dashboardRoot != null)
            {
                InitializeElements(_dashboardRoot);
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
            {
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshMilitaryData;
            }
            BindActiveDashboardRoot();
            RefreshMilitaryData();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
            {
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshMilitaryData;
            }
        }

        private void InitializeElements(VisualElement root)
        {
            _tabForces = root.Q<Button>("TabForces");
            _tabRecruit = root.Q<Button>("TabRecruit");
            _tabWeapons = root.Q<Button>("TabWeapons");
            _tabBases = root.Q<Button>("TabBases");
            _tabOps = root.Q<Button>("TabOperations");
            _tabLogistics = root.Q<Button>("TabLogistics");
            _tabBudget = root.Q<Button>("TabBudget");

            _panelForces = root.Q<VisualElement>("PanelForces");
            _panelRecruit = root.Q<VisualElement>("PanelRecruit");
            _panelWeapons = root.Q<VisualElement>("PanelWeapons");
            _panelBases = root.Q<VisualElement>("PanelBases");
            _panelOps = root.Q<VisualElement>("PanelOperationsInstance") ?? root.Q<VisualElement>("PanelOperations");
            _panelProjects = root.Q<VisualElement>("PanelProjects");
            _panelLogistics = root.Q<VisualElement>("PanelLogistics");
            _panelBudget = root.Q<VisualElement>("PanelBudget");

            _allPanels.Clear();
            _allPanels.AddRange(new[] { _panelForces, _panelRecruit, _panelWeapons, _panelBases, _panelOps, _panelLogistics, _panelBudget });

            InitializeWeapons(root);
            InjectForcesImages(root);
            InitializeRecruitTab(root);

            _tabForces.clicked += () => SwitchTab(0);
            _tabRecruit.clicked += () => SwitchTab(1);
            _tabWeapons.clicked += () => SwitchTab(2);
            _tabBases.clicked += () => SwitchTab(3);
            _tabOps.clicked += () => SwitchTab(4);
            _tabLogistics.clicked += () => SwitchTab(5);
            _tabBudget.clicked += () => SwitchTab(6);

_headerCountryName = root.Q<Label>("CountryName");
            _headerFirepower = root.Q<Label>("HdrMilRank");
            _headerTotalPersonnel = root.Q<Label>("HdrPersonnel");
            _headerBudget = root.Q<Label>("HdrDefBudget") ?? root.Q<Label>("HeaderMilBudget");
            _headerBasesCount = root.Q<Label>("HdrBases");
            _headerOperationsCount = root.Q<Label>("HdrOperations");
            _headerReadiness = root.Q<Label>("HdrReadiness") ?? root.Q<Label>("HeaderReadiness");
            _threatLevelBadge = root.Q<Label>("ThreatLevelBadge");
            _headerKIA = root.Q<Label>("HeaderKIA");

            _generalsContainer = root.Q<ScrollView>("GeneralsContainer");
            _blockadesContainer = root.Q<ScrollView>("BlockadesContainer");

            _btnRecruitGeneral = root.Q<Button>("BtnRecruitGeneral");
            if (_btnRecruitGeneral != null) _btnRecruitGeneral.clicked += RecruitGeneral;

            _btnDeclareBlockade = root.Q<Button>("BtnDeclareBlockade");
            if (_btnDeclareBlockade != null) _btnDeclareBlockade.clicked += ShowBlockadeModal;

            _branchContainer = root.Q<VisualElement>("BranchContainer");
            _recruitmentGrid = root.Q<VisualElement>("RecruitmentGrid");
            _personnelGrid = root.Q<VisualElement>("PersonnelGrid");
            _weaponsGrid = root.Q<VisualElement>("WeaponsGrid");
            _basesGrid = root.Q<VisualElement>("BasesGrid");
            _regionPoliceList = root.Q<VisualElement>("RegionPoliceList");
            _supplyRouteList = root.Q<VisualElement>("SupplyRouteList");
            _milBriefing = root.Q<Label>("MilBriefing");
            _milLiveFeed = root.Q<ScrollView>("MilLiveFeed");
                        _threatList = root.Q<ScrollView>("threat-list");
            _opsList = root.Q<ScrollView>("ops-list");
            _feedList = root.Q<ScrollView>("feed-list");
            _opTypesGrid = root.Q<VisualElement>("op-types-grid");
            _readinessList = root.Q<VisualElement>("readiness-list");

            _theatreStatusList = root.Q<VisualElement>("theatre-list") ?? root.Q<VisualElement>("TheatreStatusList");
            _activeOpsList = root.Q<ScrollView>("ActiveOpsList");
            _globalPowerList = root.Q<ScrollView>("GlobalPowerList");
            _activeProjectsList = root.Q<ScrollView>("ActiveProjectsList");
            _availableProjectsList = root.Q<ScrollView>("AvailableProjectsList");
            _budgetLedgerList = root.Q<ScrollView>("BudgetLedgerList");

            _wmdProgressBar = root.Q<ProgressBar>("WMDProgressBar");
            _wmdStatusLabel = root.Q<Label>("WMDStatusLabel");
            _wmdOperationalBadge = root.Q<Label>("WMDOperationalBadge");

            _fuelBar = root.Q<ProgressBar>("FuelBar");
            _ammoBar = root.Q<ProgressBar>("AmmoBar");
            _medicalBar = root.Q<ProgressBar>("MedicalBar");
            _foodBar = root.Q<ProgressBar>("FoodBar");
            _elecBar = root.Q<ProgressBar>("ElecBar");
            
            _natFuelStock = root.Q<Label>("NatFuelStock");
            _natFoodStock = root.Q<Label>("NatFoodStock");
            _natElecStock = root.Q<Label>("NatElecStock");

            _supplyRouteList = root.Q<VisualElement>("SupplyRouteList");

            var btnBuyFuel = root.Q<Button>("BtnBuyFuel");
            if (btnBuyFuel != null) btnBuyFuel.clicked += () => BuyLogistics("FUEL", 0.05f);

            var btnBuyAmmo = root.Q<Button>("BtnBuyAmmo");
            if (btnBuyAmmo != null) btnBuyAmmo.clicked += () => BuyLogistics("AMMO", 0.03f);

            var btnBuyMedical = root.Q<Button>("BtnBuyMedical");
            if (btnBuyMedical != null) btnBuyMedical.clicked += () => BuyLogistics("MEDICAL", 0.02f);

            var btnBuyFood = root.Q<Button>("BtnBuyFood");
            if (btnBuyFood != null) btnBuyFood.clicked += () => BuyLogistics("FOOD", 0.02f);

_doctrineDesc = root.Q<Label>("PostureDesc");
            _postureLabel = root.Q<Label>("PostureLabel");
            _postureIcon = root.Q<VisualElement>("PostureIconBg");
            
            var btnChangePosture = root.Q<Button>("BtnChangePosture");
            if (btnChangePosture != null) btnChangePosture.clicked += CyclePosture;
            
            var oldDef = root.Q<Button>("BtnPostureDefensive");
            if (oldDef != null) oldDef.clicked += () => SetPosture("DEFENSIVE");
            var oldDet = root.Q<Button>("BtnPostureDeterrence");
            if (oldDet != null) oldDet.clicked += () => SetPosture("DETERRENCE");
            var oldOff = root.Q<Button>("BtnPostureOffensive");
            if (oldOff != null) oldOff.clicked += () => SetPosture("OFFENSIVE");
            var oldCoin = root.Q<Button>("BtnPostureCOIN");
            if (oldCoin != null) oldCoin.clicked += () => SetPosture("COIN FOCUS");

            var btnLaunchOp = root.Q<Button>("btn-launch-new");
            if (btnLaunchOp == null) btnLaunchOp = root.Q<Button>("BtnLaunchOp");
            if (btnLaunchOp != null)
            {
                btnLaunchOp.clicked += ShowLaunchOpModalNoArgs;
            }

            var btnOpsInbox = root.Q<Button>("btn-inbox");
            if (btnOpsInbox != null) btnOpsInbox.clicked += ShowInboxModal;

            var btnViewForces = root.Q<Button>("btn-view-forces");
            if (btnViewForces != null) btnViewForces.clicked += () => SwitchTab(0);

            var btnViewOpTypes = root.Q<Button>("btn-view-op-types");
            if (btnViewOpTypes != null) btnViewOpTypes.clicked += ShowLaunchOpModalNoArgs;

            var btnViewAllOps = root.Q<Button>("btn-view-all-ops");
            if (btnViewAllOps != null) btnViewAllOps.clicked += ShowLaunchOpModalNoArgs;

            var btnViewIntel = root.Q<Button>("btn-view-intel");
            if (btnViewIntel != null) btnViewIntel.clicked += () => {
                Hide();
                if (MainDashboardToolkitController.Instance != null)
                    MainDashboardToolkitController.Instance.ShowPanel("IntelligenceDashboard");
            };

            var btnViewRegions = root.Q<Button>("btn-view-regions");
            if (btnViewRegions != null) btnViewRegions.clicked += () => {
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.ShowAlert("RESTRICTED", "Regional strategic map is currently restricted.", true);
            };

            var btnViewFeed = root.Q<Button>("btn-view-feed");
            if (btnViewFeed != null) btnViewFeed.clicked += ShowInboxModal;

            var btnInbox = root.Q<Button>("BtnInbox");
            if (btnInbox != null) btnInbox.clicked += ShowInboxModal;

            root.Q<Button>("CloseMilitary").clicked += Hide;
        }

        public void SwitchTabByName(string name)
        {
            if (name == "TabForces") SwitchTab(0);
            else if (name == "TabRecruit") SwitchTab(1);
            else if (name == "TabWeapons" || name == "WTabArsenal") { SwitchTab(2); SwitchWeaponsTab(0); }
            else if (name == "TabBases") SwitchTab(3);
            else if (name == "TabOperations") SwitchTab(4);
            else if (name == "TabLogistics") SwitchTab(5);
            else if (name == "TabBudget") SwitchTab(6);
            else if (name == "WTabProduction") { SwitchTab(2); SwitchWeaponsTab(1); }
            else if (name == "WTabProcurement") { SwitchTab(2); SwitchWeaponsTab(2); }
        }

        public void SwitchTab(int index)
        {
            for (int i = 0; i < _allPanels.Count; i++)
            {
                if (_allPanels[i] != null)
                {
                    bool active = (i == index);
                    _allPanels[i].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                    _allPanels[i].pickingMode = active ? PickingMode.Position : PickingMode.Ignore;
                    if (active) UIStateManager.FixScrollViews(_allPanels[i]);
                }
            }

            var tabs = new[] { _tabForces, _tabRecruit, _tabWeapons, _tabBases, _tabOps, _tabLogistics, _tabBudget };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                if (i == index) tabs[i].AddToClassList("active-tab");
                else tabs[i].RemoveFromClassList("active-tab");
            }

            if (index == 2 && _panelWeapons != null)
            {
                EnsureWeaponsWired();
                if (_weaponsWired && GameManager.Instance?.playerCountry != null)
                {
                    PopulateWeapons(GameManager.Instance.playerCountry);
                    SwitchWeaponsTab(0);
                }
                else
                {
                    EventCallback<GeometryChangedEvent> onLayout = null;
                    onLayout = evt =>
                    {
                        _panelWeapons.UnregisterCallback<GeometryChangedEvent>(onLayout);
                        EnsureWeaponsWired();
                        if (GameManager.Instance?.playerCountry != null)
                        {
                            PopulateWeapons(GameManager.Instance.playerCountry);
                            SwitchWeaponsTab(0);
                        }
                    };
                    _panelWeapons.RegisterCallback<GeometryChangedEvent>(onLayout);
                }
            }

            if (index == 3 && _panelBases != null)
            {
                EnsureBasesWired();
                if (_basesWired && GameManager.Instance?.playerCountry != null)
                {
                    PopulateBases(GameManager.Instance.playerCountry);
                }
                else
                {
                    EventCallback<GeometryChangedEvent> onLayout = null;
                    onLayout = evt =>
                    {
                        _panelBases.UnregisterCallback<GeometryChangedEvent>(onLayout);
                        EnsureBasesWired();
                        if (GameManager.Instance?.playerCountry != null)
                        {
                            PopulateBases(GameManager.Instance.playerCountry);
                        }
                    };
                    _panelBases.RegisterCallback<GeometryChangedEvent>(onLayout);
                }
            }

            if (index == 4 && _panelOps != null)
            {
                if (_panelOps.parent != null)
                {
                    _panelOps.parent.style.display = DisplayStyle.Flex;
                    _panelOps.parent.style.height = Length.Percent(100);
                }
                if (GameManager.Instance?.playerCountry != null)
                {
                    PopulateBranchReadiness(GameManager.Instance.playerCountry);
                    CheckOperationAlerts(GameManager.Instance.playerCountry);
                    PopulateOperationsMasterList(GameManager.Instance.playerCountry, GameManager.Instance);
                    PopulateThreatList(GameManager.Instance.playerCountry);
                }
            }
        }

        public void Show()
        {
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Military") 
                              ?? _root?.Q<VisualElement>("MilitaryDashboard");
                if (_dashboardRoot != null) _root = _dashboardRoot;
            }

            var panel = _dashboardRoot;
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.FixScrollViews(panel);
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshMilitaryData();
            }
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        public void RefreshMilitaryData()
        {
            if (_dashboardRoot == null || _dashboardRoot.panel == null)
            {
                _dashboardRoot = MainDashboardToolkitController.Instance?.GetPanelVisualElement("Military") 
                              ?? _root?.Q<VisualElement>("MilitaryDashboard")
                              ?? _dashboardRoot;
                if (_dashboardRoot != null) _root = _dashboardRoot;
            }

            if (GameManager.Instance == null || GameManager.Instance.playerCountry == null) return;
            var gm = GameManager.Instance;
            var country = gm.playerCountry;
            
            if (_headerCountryName != null) _headerCountryName.text = country.countryName.ToUpper();
            
            var rctArmy = _root?.Q<Label>("RecruitLblArmyName");
            if (rctArmy != null) rctArmy.text = $"The {country.countryName} Army";
            var rctAF = _root?.Q<Label>("RecruitLblAirForceName");
            if (rctAF != null) rctAF.text = $"The {country.countryName} Air Force";
            var rctNavy = _root?.Q<Label>("RecruitLblNavyName");
            if (rctNavy != null) rctNavy.text = $"The {country.countryName} Navy";
            
            var fcsArmy = _root?.Q<Label>("ForcesLblArmyName");
            if (fcsArmy != null) fcsArmy.text = $"The {country.countryName} Army";
            var fcsAF = _root?.Q<Label>("ForcesLblAirForceName");
            if (fcsAF != null) fcsAF.text = $"The {country.countryName} Air Force";
            var fcsNavy = _root?.Q<Label>("ForcesLblNavyName");
            if (fcsNavy != null) fcsNavy.text = $"The {country.countryName} Navy";
            
            var basesSub = _root?.Q<Label>("BasesLblSubtitle");
            if (basesSub != null) basesSub.text = $"Build, manage and upgrade military bases across {country.countryName} and abroad.";

            int armyDeployed = 0;
            int rdfDeployed = 0;
            int afDeployed = 0;
            int navyDeployed = 0;
            
            int tanksDeployed = 0;
            int apcsDeployed = 0;
            int helisDeployed = 0;
            int uavsDeployed = 0;
            int fightersDeployed = 0;
            int bombersDeployed = 0;
            int frigatesDeployed = 0;
            int subsDeployed = 0;

            if (country.activeOperations != null)
            {
                foreach (var op in country.activeOperations)
                {
                    armyDeployed += op.deployedTroops;
                    rdfDeployed += op.deployedRDF;
                    afDeployed += op.deployedAirForce;
                    navyDeployed += op.deployedNavy;
                    
                    tanksDeployed += op.deployedTanks;
                    apcsDeployed += op.deployedAPCs;
                    helisDeployed += op.deployedHelis;
                    uavsDeployed += op.deployedUAVs;
                    fightersDeployed += op.deployedFighters;
                    bombersDeployed += op.deployedBombers;
                    frigatesDeployed += op.deployedFrigates;
                    subsDeployed += op.deployedSubmarines;
                }
            }

            country.armyStats.personnel = country.unitInfantry + armyDeployed;
            country.rdfStats.personnel = Mathf.Max(country.rdfStats.personnel, rdfDeployed);

            var ms = country.militaryState;
            if (ms != null)
            {
                int afPersonnel = ms.airForce.pilots + ms.airForce.groundCrew + ms.airForce.airControllers + afDeployed;
                int navyPersonnel = ms.navy.sailors + ms.navy.navalOfficers + ms.navy.marines + ms.navy.technicalCrew + navyDeployed;
                if (afPersonnel > 0)   country.airForceStats.personnel = afPersonnel;
                if (navyPersonnel > 0) country.navyStats.personnel     = navyPersonnel;
            }

            country.armyStats.equipment = country.unitTanks + country.unitAPCs + country.unitArtillery + tanksDeployed + apcsDeployed;
            country.airForceStats.equipment = country.unitFighters + country.unitBombers + country.unitAttackHelis + country.unitUAVs + fightersDeployed + bombersDeployed + helisDeployed + uavsDeployed;
            country.navyStats.equipment = country.unitFrigates + country.unitDestroyers + country.unitSubmarines + country.unitPatrolBoats + frigatesDeployed + subsDeployed;

            UnityEngine.Debug.Log($"[Auto-Sync Healer] {country.countryName}: " +
                $"Army={country.armyStats.personnel}(base={country.unitInfantry}+deployed={armyDeployed}), " +
                $"AF={country.airForceStats.personnel}, Navy={country.navyStats.personnel}, " +
                $"ActiveOps={country.activeOperations?.Count ?? 0}");

            var shortageBadge = _root?.Q<Label>("ShortageAlertBadge");
            bool isWeaponsShortage = country.unitRifles < country.armyStats.personnel;
            if (shortageBadge != null)
            {
                if (isWeaponsShortage)
                {
                    shortageBadge.style.display = DisplayStyle.Flex;
                    shortageBadge.text = $"WEAPONS SHORTAGE: NEED {country.armyStats.personnel:N0} RIFLES";
                }
                else
                {
                    shortageBadge.style.display = DisplayStyle.None;
                }
            }

            if (isWeaponsShortage)
            {
                NotificationManager.Instance?.AddNotification($"CRITICAL WEAPONS SHORTAGE: Insufficient Service Rifles to equip active forces ({country.unitRifles:N0} rifles for {country.armyStats.personnel:N0} army personnel). Operations running at 50% efficiency!", Color.red);
            }

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            var stage = gm.WarSystem != null ? gm.WarSystem.GetWarStage(country) : WarStage.Peace;
            if (_threatLevelBadge != null)
            {
                _threatLevelBadge.text = stage.ToString().ToUpper();
                var stageColor = DashboardLiveContent.WarStageColor(stage);
                _threatLevelBadge.style.color = stageColor;
                _threatLevelBadge.style.borderTopColor = stageColor;
                _threatLevelBadge.style.borderBottomColor = stageColor;
                _threatLevelBadge.style.borderLeftColor = stageColor;
                _threatLevelBadge.style.borderRightColor = stageColor;
            }

            var hdrRank = _headerFirepower ?? _dashboardRoot?.Q<Label>("HdrMilRank") ?? _root?.Q<Label>("HdrMilRank");
            if (hdrRank != null)
            {
                float fp = country.GetMilitaryFirepowerScore();
                hdrRank.text = fp >= 1000f ? $"{fp / 1000f:F1}k" : $"{fp:F0}";
                hdrRank.style.color = fp > 5000 ? new Color(0.14f, 0.93f, 0.34f) : fp > 1000 ? new Color(1f, 0.83f, 0.2f) : Color.white;
            }

            var hdrPers = _headerTotalPersonnel ?? _dashboardRoot?.Q<Label>("HdrPersonnel") ?? _root?.Q<Label>("HdrPersonnel");
            if (hdrPers != null)
            {
                int totalPers = country.armyStats.personnel + country.navyStats.personnel + country.airForceStats.personnel + country.rdfStats.personnel;
                hdrPers.text = totalPers >= 1000000 ? $"{totalPers / 1000000f:F2}M" : totalPers >= 1000 ? $"{totalPers / 1000f:F1}k" : $"{totalPers}";
            }

            if (_headerBudget != null) _headerBudget.text = $"Allocated: {UiFormat.Money(country.militaryBudgetUSD * country.exchangeRate, sym)}  |  Upkeep: {UiFormat.Money(country.CalculateMilitaryUpkeep() * country.exchangeRate, sym)} / Turn";

            var hdrBases = _headerBasesCount ?? _dashboardRoot?.Q<Label>("HdrBases") ?? _root?.Q<Label>("HdrBases");
            if (hdrBases != null)
            {
                int totalBases = (country.militaryBases != null ? country.militaryBases.Count : 0) + (country.defenseFactories != null ? country.defenseFactories.Count : 0);
                hdrBases.text = totalBases.ToString();
            }

            var hdrOps = _headerOperationsCount ?? _dashboardRoot?.Q<Label>("HdrOperations") ?? _root?.Q<Label>("HdrOperations");
            var hdrOpsSub = _dashboardRoot?.Q<Label>("HdrOperationsSub") ?? _root?.Q<Label>("HdrOperationsSub");
            if (hdrOps != null)
            {
                int activeOps = country.activeOperations?.Count ?? 0;
                hdrOps.text = activeOps.ToString();
                hdrOps.style.color = activeOps > 0 ? new Color(1f, 0.83f, 0.2f) : Color.white;
                if (hdrOpsSub != null)
                    hdrOpsSub.text = activeOps == 1 ? "Deployed" : "Deployed";
            }

            float avgReady = (country.armyStats.readiness + country.navyStats.readiness + country.airForceStats.readiness + country.rdfStats.readiness) / 4f;
            var hdrReady = _headerReadiness ?? _dashboardRoot?.Q<Label>("HdrReadiness") ?? _root?.Q<Label>("HdrReadiness");
            if (hdrReady != null)
            {
                hdrReady.text = $"{avgReady:F0}%";
                hdrReady.style.color = avgReady >= 80f ? new Color(0.14f, 0.93f, 0.34f) : avgReady >= 60f ? new Color(1f, 0.83f, 0.2f) : new Color(1f, 0.3f, 0.3f);
            }

            if (_headerKIA != null)
            {
                _headerKIA.text = $"{country.totalKIA:N0} KIA | {country.totalWounded:N0} WIA";
            }

            var btnInbox = _root?.Q<Button>("BtnInbox");
            if (btnInbox != null && country.inboxMessages != null)
            {
                btnInbox.text = $"COMMAND INBOX ({country.inboxMessages.Count})";
                if (country.inboxMessages.Count > 0)
                {
                    btnInbox.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.4f);
                    btnInbox.style.borderTopColor = new Color(255/255f, 82/255f, 82/255f);
                    btnInbox.style.borderBottomColor = new Color(255/255f, 82/255f, 82/255f);
                    btnInbox.style.borderLeftColor = new Color(255/255f, 82/255f, 82/255f);
                    btnInbox.style.borderRightColor = new Color(255/255f, 82/255f, 82/255f);
                }
                else
                {
                    btnInbox.style.backgroundColor = new Color(33/255f, 150/255f, 243/255f, 0.5f);
                    btnInbox.style.borderTopColor = new Color(33/255f, 150/255f, 243/255f, 0.8f);
                    btnInbox.style.borderBottomColor = new Color(33/255f, 150/255f, 243/255f, 0.8f);
                    btnInbox.style.borderLeftColor = new Color(33/255f, 150/255f, 243/255f, 0.8f);
                    btnInbox.style.borderRightColor = new Color(33/255f, 150/255f, 243/255f, 0.8f);
                }
            }

            if (_milBriefing != null)
                _milBriefing.text = BuildMilBriefing(country, gm, stage);
            DashboardLiveContent.PopulateFeed(_milLiveFeed, DashboardLiveContent.BuildMilitaryFeed(country, gm));

            PopulateBranches(country);
            PopulateGenerals(country);
            PopulateBlockades(country);
            PopulatePolice(country);
            PopulateRecruit(country);
            PopulateWeapons(country);
            PopulateBases(country);
            PopulateGlobalPower(country);
            PopulateWMD(country);
            PopulateBranchReadiness(country);
            CheckOperationAlerts(country);
            PopulateOperationsMasterList(country, gm);
            UpdateIntelAssessment(country);
            PopulateThreatList(country);
            PopulateTheatre(country, gm);
            PopulateProjects(country);
            UpdateDoctrineText(_currentPosture);
            UpdatePostureButtonStyles();
            PopulateBudget(country);

            if (_fuelBar != null) { _fuelBar.value = country.reserveFuel; _fuelBar.title = $"{country.reserveFuel:F0} days"; }
            if (_ammoBar != null) { _ammoBar.value = country.reserveAmmo; _ammoBar.title = $"{country.reserveAmmo:F0}%"; }
            if (_medicalBar != null) { _medicalBar.value = country.reserveMedical; _medicalBar.title = $"{country.reserveMedical:F0}%"; }
            if (_foodBar != null) { _foodBar.value = country.reserveFood; _foodBar.title = $"{country.reserveFood:F0}%"; }
            if (_elecBar != null) { _elecBar.value = country.electricityLevel; _elecBar.title = $"{country.electricityLevel:F0}%"; }

            if (_natFuelStock != null)
            {
                float fuelProducedK = (country.indMiningOil * 2000000f) / 1000f;
                string status = country.reserveFuel < 100f ? "(- Active Drain)" : (fuelProducedK > 0 ? $"(+{fuelProducedK:N0}k/turn)" : "(+0)");
                _natFuelStock.text = $"Nat. Stock: {country.invOilBarrels / 1000f:N0}k bbl {status}";
                _natFuelStock.style.color = country.reserveFuel < 100f ? new Color(255/255f, 152/255f, 0) : Color.grey;
            }

            if (_natFoodStock != null)
            {
                float foodProducedK = ((country.indAgriRice * 50000f) + (country.indAgriCassava * 80000f) + (country.indAgriCocoa * 20000f)) / 1000f;
                float foodConsumedK = (country.population * 0.001f) / 1000f;
                float netFoodK = foodProducedK - foodConsumedK;
                string prodStr = netFoodK >= 0 ? $"(+{netFoodK:N0}k T/turn)" : $"({netFoodK:N0}k T/turn)";
                string status = country.reserveFood < 100f ? "(- Active Drain)" : prodStr;
                _natFoodStock.text = $"Nat. Stock: {country.invRiceTonnes / 1000f:N0}k T {status}";
                _natFoodStock.style.color = country.reserveFood < 100f ? new Color(255/255f, 152/255f, 0) : Color.grey;
            }

            if (_natElecStock != null)
            {
                float militaryDraw = (country.baseArmyCount * 1.5f) + (country.baseNavyCount * 2.5f) + (country.baseAirForceCount * 4f);
                string status = militaryDraw > 0 ? $"(-{militaryDraw:F1} MW Draw)" : "(+0)";
                _natElecStock.text = $"Nat. Grid: {country.electricityLevel:F0}% {status}";
                _natElecStock.style.color = new Color(0, 210/255f, 255/255f);
            }

            PopulateSupplyRoutes(country);
        }

private float CalculateFrontPower(CountryDataSO country, string enemyName)
        {
            float power = 0f;
            if (country.activeOperations == null) return power;
            foreach (var op in country.activeOperations)
            {
                if (op.regionName != enemyName || op.isDecommissioning || op.turnsToDeploy > 0) continue;
                power += op.deployedTroops   * 1.0f;
                power += op.deployedTanks    * 12f;
                power += op.deployedAPCs     * 5f;
                power += op.deployedFighters * 20f;
                power += op.deployedBombers  * 15f;
                power += op.deployedUAVs     * 3f;
            }
            return power;
        }

private static List<MilitaryOperation> GenerateAmbientOperations(CountryDataSO c, GameManager gm)
        {
            var list = new List<MilitaryOperation>();
            foreach (var region in c.regions)
            {
                if (region.security >= 50f) continue;
                list.Add(new MilitaryOperation
                {
                    opName = $"Operation {region.regionName.ToUpper().Replace(" ", "")} SHIELD",
                    regionName = region.regionName,
                    type = "COIN",
                    status = region.security < 30f ? "Active" : "Stalled",
                    costPerMonth = 12f + (50f - region.security) * 0.3f
                });
            }
            if (gm.WarSystem != null && gm.WarSystem.GetWarStage(c) >= WarStage.Tension)
            {
                list.Add(new MilitaryOperation
                {
                    opName = "Operation BORDER WATCH",
                    regionName = c.capitalCity,
                    type = "Defensive",
                    status = "Active",
                    costPerMonth = 25f
                });
            }
            return list;
        }

public enum ThreatLevel  { High, Medium, Low }
        public enum OpType       { CounterTerror, Peacekeeping, BorderPatrol, AirRecon, AirStrike, Maritime, NavalBombardment, HvtCapture }
        public enum FeedColour   { Green, Yellow, Red }

        public class OpTypeEntry {
            public string Name, Description, RequiredForces, EstCost;
            public OpType Type;
            public OpTypeEntry(string n, string d, string rf, string ec, OpType t) { Name=n; Description=d; RequiredForces=rf; EstCost=ec; Type=t; }
        }

        static string ThreatIconClass(ThreatLevel l)
        {
            if (l == ThreatLevel.High) return "threat-row__icon--high";
            if (l == ThreatLevel.Medium) return "threat-row__icon--medium";
            return "threat-row__icon--low";
        }

        static string ThreatLevelClass(ThreatLevel l)
        {
            if (l == ThreatLevel.High) return "threat-row__level--high";
            if (l == ThreatLevel.Medium) return "threat-row__level--medium";
            return "threat-row__level--low";
        }

        static Color ThreatColour(ThreatLevel l)
        {
            if (l == ThreatLevel.High)   return new Color(0.96f, 0.26f, 0.21f);
            if (l == ThreatLevel.Medium) return new Color(1f,    0.92f, 0.23f);
            return new Color(0f,    0.90f, 0.46f);
        }

        static string ThreatGlyph(ThreatLevel l)
        {
            if (l == ThreatLevel.High)   return "!";
            if (l == ThreatLevel.Medium) return "~";
            return "✓";
        }

        static string OpTypeCardClass(OpType t)
        {
            if (t == OpType.CounterTerror) return "op-card--counter-terror";
            if (t == OpType.Peacekeeping)  return "op-card--peacekeeping";
            if (t == OpType.BorderPatrol)  return "op-card--border-patrol";
            if (t == OpType.AirRecon)      return "op-card--air-recon";
            if (t == OpType.Maritime)      return "op-card--maritime";
            if (t == OpType.HvtCapture)    return "op-card--hvt";
            return "";
        }

        static string OpTypeGlyph(OpType t)
        {
            if (t == OpType.CounterTerror) return "[CT] ";
            if (t == OpType.Peacekeeping)  return "[PK] ";
            if (t == OpType.BorderPatrol)  return "[BP] ";
            if (t == OpType.AirRecon)      return "[AR] ";
            if (t == OpType.Maritime)      return "[MR] ";
            if (t == OpType.HvtCapture)    return "[HV] ";
            return "[?] ";
        }

        static string OpTypeIconPath(OpType t)
        {
            if (t == OpType.CounterTerror) return "project://database/Assets/Images/MilitaryIcons/icon_target.png";
            if (t == OpType.Peacekeeping)  return "project://database/Assets/Images/MilitaryIcons/icon_defend.png";
            if (t == OpType.BorderPatrol)  return "project://database/Assets/Images/MilitaryIcons/icon_forces.png";
            if (t == OpType.AirRecon)      return "project://database/Assets/Images/MilitaryIcons/icon_eyes.png";
            if (t == OpType.Maritime)      return "project://database/Assets/Images/MilitaryIcons/icon_bases.png";
            if (t == OpType.HvtCapture)    return "project://database/Assets/Images/MilitaryIcons/icon_attack.png";
            return "project://database/Assets/Images/MilitaryIcons/icon_operations.png";
        }

        static Color OpTypeAccentColour(OpType t)
        {
            if (t == OpType.CounterTerror) return new Color(0.96f, 0.26f, 0.21f);
            if (t == OpType.Peacekeeping)  return new Color(0f,    0.66f, 1f);
            if (t == OpType.BorderPatrol)  return new Color(0f,    0.90f, 0.46f);
            if (t == OpType.AirRecon)      return new Color(0f,    0.66f, 1f);
            if (t == OpType.Maritime)      return new Color(0f,    0.74f, 0.83f);
            if (t == OpType.HvtCapture)    return new Color(0.67f, 0.28f, 0.74f);
            return Color.white;
        }

        static string ProgressFillClass(int progress)
        {
            if (progress >= 60) return "op-card__progress-fill--good";
            if (progress >= 30) return "op-card__progress-fill--medium";
            return "op-card__progress-fill--low";
        }

        static string FeedDotClass(FeedColour c)
        {
            if (c == FeedColour.Green)  return "feed-item__dot--green";
            if (c == FeedColour.Yellow) return "feed-item__dot--yellow";
            return "feed-item__dot--red";
        }

private Label AddImpactStat(VisualElement parent, string titleText, string initialVal)
        {
            var box = new VisualElement();
            box.style.flexDirection = FlexDirection.Column;
            box.style.width = Length.Percent(33);
            
            var title = new Label(titleText);
            title.style.color = Color.grey;
            title.style.fontSize = 8;
            box.Add(title);

            var val = new Label(initialVal);
            val.style.color = Color.white;
            val.style.unityFontStyleAndWeight = FontStyle.Bold;
            val.style.fontSize = 10;
            val.style.marginTop = 2;
            box.Add(val);
            
            parent.Add(box);
            return val;
        }

        private VisualElement MakeEffectTag(string text, Color color)
        {
            var el = new VisualElement();
            el.style.backgroundColor = new StyleColor(new Color(color.r, color.g, color.b, 0.2f));
            SetBorderColor(el.style, color);
            el.style.borderTopWidth = el.style.borderBottomWidth = el.style.borderLeftWidth = el.style.borderRightWidth = 1;
            el.style.borderTopLeftRadius = el.style.borderTopRightRadius = el.style.borderBottomLeftRadius = el.style.borderBottomRightRadius = 3;
            el.style.paddingLeft = el.style.paddingRight = 4;
            el.style.paddingTop = el.style.paddingBottom = 2;
            el.style.marginRight = 4;
            el.style.marginBottom = 4;

            var lbl = new Label(text);
            lbl.style.color = color;
            lbl.style.fontSize = 8;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            el.Add(lbl);
            return el;
        }
    }
}