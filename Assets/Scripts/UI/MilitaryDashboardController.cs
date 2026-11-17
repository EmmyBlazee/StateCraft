using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    public class MilitaryDashboardController : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        
        private Button _tabForces, _tabRecruit, _tabWeapons, _tabBases, _tabOps, _tabProjects, _tabLogistics, _tabBudget;
        private VisualElement _panelForces, _panelRecruit, _panelWeapons, _panelBases, _panelOps, _panelProjects, _panelLogistics, _panelBudget;
        private List<VisualElement> _allPanels = new List<VisualElement>();

private Label _headerBudget, _headerReadiness, _threatLevelBadge, _headerKIA;

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
        private string _currentPosture = "DETERRENCE";

        private void Awake()
        {
            _uiDoc = GetComponent<UIDocument>();
            if (_uiDoc != null) _root = _uiDoc.rootVisualElement;
            
            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null)
            {
                InitializeElements(dashboard);
                dashboard.style.display = DisplayStyle.None;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
            {
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshMilitaryData;
            }
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
            _tabProjects = root.Q<Button>("TabProjects");
            _tabLogistics = root.Q<Button>("TabLogistics");
            _tabBudget = root.Q<Button>("TabBudget");

            _panelForces = root.Q<VisualElement>("PanelForces");
            _panelRecruit = root.Q<VisualElement>("PanelRecruit");
            _panelWeapons = root.Q<VisualElement>("PanelWeapons");
            _panelBases = root.Q<VisualElement>("PanelBases");
            _panelOps = root.Q<VisualElement>("PanelOperations");
            _panelProjects = root.Q<VisualElement>("PanelProjects");
            _panelLogistics = root.Q<VisualElement>("PanelLogistics");
            _panelBudget = root.Q<VisualElement>("PanelBudget");

            _allPanels.Clear();
            _allPanels.AddRange(new[] { _panelForces, _panelRecruit, _panelWeapons, _panelBases, _panelOps, _panelProjects, _panelLogistics, _panelBudget });

            _tabForces.clicked += () => SwitchTab(0);
            _tabRecruit.clicked += () => SwitchTab(1);
            _tabWeapons.clicked += () => SwitchTab(2);
            _tabBases.clicked += () => SwitchTab(3);
            _tabOps.clicked += () => SwitchTab(4);
            _tabProjects.clicked += () => SwitchTab(5);
            _tabLogistics.clicked += () => SwitchTab(6);
            _tabBudget.clicked += () => SwitchTab(7);

_headerBudget = root.Q<Label>("HeaderMilBudget");
            _headerReadiness = root.Q<Label>("HeaderReadiness");
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

            _theatreStatusList = root.Q<VisualElement>("TheatreStatusList");
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

_doctrineDesc = root.Q<Label>("DoctrineDesc");
            root.Q<Button>("BtnPostureDefensive").clicked += () => SetPosture("DEFENSIVE");
            root.Q<Button>("BtnPostureDeterrence").clicked += () => SetPosture("DETERRENCE");
            root.Q<Button>("BtnPostureOffensive").clicked += () => SetPosture("OFFENSIVE");
            root.Q<Button>("BtnPostureCOIN").clicked += () => SetPosture("COIN FOCUS");

            var btnLaunchOp = root.Q<Button>("btn-launch-new");
            if (btnLaunchOp == null) btnLaunchOp = root.Q<Button>("BtnLaunchOp");
            if (btnLaunchOp != null)
            {
                btnLaunchOp.clicked += ShowLaunchOpModal;
            }

            var btnViewForces = root.Q<Button>("btn-view-forces");
            if (btnViewForces != null) btnViewForces.clicked += () => SwitchTab(0);

            var btnViewOpTypes = root.Q<Button>("btn-view-op-types");
            if (btnViewOpTypes != null) btnViewOpTypes.clicked += ShowLaunchOpModal;

            var btnViewAllOps = root.Q<Button>("btn-view-all-ops");
            if (btnViewAllOps != null) btnViewAllOps.clicked += ShowLaunchOpModal;

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
            if (btnViewFeed != null) btnViewFeed.clicked += () => {
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.ShowAlert("ENCRYPTED CHANNEL", "Full terminal feed requires Level 4 clearance.", false);
            };

            var btnInbox = root.Q<Button>("BtnInbox");
            if (btnInbox != null) btnInbox.clicked += ShowInboxModal;

            root.Q<Button>("CloseMilitary").clicked += Hide;
        }

        private void SwitchTab(int index)
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

            var tabs = new[] { _tabForces, _tabRecruit, _tabWeapons, _tabBases, _tabOps, _tabProjects, _tabLogistics, _tabBudget };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                if (i == index) tabs[i].AddToClassList("active-tab");
                else tabs[i].RemoveFromClassList("active-tab");
            }
        }

        public void Show()
        {
            var panel = _root.Q<VisualElement>("MilitaryDashboard");
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
            if (GameManager.Instance == null || GameManager.Instance.playerCountry == null) return;
            var gm = GameManager.Instance;
            var country = gm.playerCountry;

            int totalDeployed = 0;
            if (country.activeOperations != null)
            {
                foreach (var op in country.activeOperations)
                    totalDeployed += op.deployedTroops;
            }
            int correctBarracksPool = country.armyStats.personnel - totalDeployed;
            if (country.unitInfantry < correctBarracksPool)
            {
                country.unitInfantry = correctBarracksPool;
            }

            country.armyStats.equipment = country.unitTanks + country.unitAPCs + country.unitArtillery;
            country.airForceStats.equipment = country.unitFighters + country.unitBombers + country.unitAttackHelis + country.unitUAVs;
            country.navyStats.equipment = country.unitFrigates + country.unitDestroyers + country.unitSubmarines + country.unitPatrolBoats;

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

            if (_headerBudget != null) _headerBudget.text = $"{UiFormat.Money(country.militaryBudgetUSD * country.exchangeRate, sym)} / Turn";
            if (_headerReadiness != null)
            {
                _headerReadiness.text = $"{country.militaryReadiness:F0}%";
                _headerReadiness.style.color = country.militaryReadiness >= 70f
                    ? new Color(0, 230/255f, 118/255f)
                    : new Color(241/255f, 196/255f, 15/255f);
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
            PopulateGlobalPower(country);
            PopulateWMD(country);
            PopulateThreatList(country);
            PopulateActiveOpsList(country);
            PopulateOperationsFeed(country, gm);
            PopulateOpTypesGrid(country);
            PopulateReadinessList(country);
            UpdateIntelAssessment(country);
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

        private void RecruitGeneral()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.playerCountry == null) return;
            var country = gm.playerCountry;
            if (country.treasury >= 250f)
            {
                country.treasury -= 250f;
                string[] firstNames = { "John", "Ahmed", "David", "Sani", "Ibrahim", "James", "Victor" };
                string[] lastNames = { "Okafor", "Adeyemi", "Bello", "Ojo", "Musa", "Dogo", "Umar" };
                string name = $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
                GeneralSpecialty spec = (GeneralSpecialty)Random.Range(0, 4);
                
                if (gm.WarSystem is Systems.WarSystem warSys)
                    warSys.PromoteGeneral(country, name, spec, Random.Range(1, 3));
                
                RefreshMilitaryData();
            }
            else
            {
                NotificationManager.Instance?.AddNotification("Insufficient Funds: Need ₦250M to promote General.", Color.red);
            }
        }

        private void ShowBlockadeModal()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.playerCountry == null) return;
            var target = gm.GetCountryByName(gm.playerCountry.warEnemyName);
            if (target != null && gm.WarSystem is Systems.WarSystem warSys)
            {
                warSys.DeclareBlockade(gm.playerCountry, target);
                RefreshMilitaryData();
            }
            else
            {
                 NotificationManager.Instance?.AddNotification("Must be at war with a target to declare a blockade.", Color.yellow);
            }
        }

        private void PopulateGenerals(CountryDataSO country)
        {
            if (_generalsContainer == null) return;
            _generalsContainer.Clear();

            if (country.assignedGenerals == null || country.assignedGenerals.Count == 0)
            {
                _generalsContainer.Add(new Label("No Active Generals in Chain of Command.") { style = { color = Color.grey, fontSize = 10, unityFontStyleAndWeight = FontStyle.Italic } });
                return;
            }

            foreach (var gen in country.assignedGenerals)
            {
                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6, paddingBottom = 4, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.1f) } };
                string stars = new string('★', gen.rank);
                
                var topRow = new VisualElement();
                topRow.Add(new Label($"{stars} Gen. {gen.generalName}") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                topRow.Add(new Label($"Specialty: {gen.specialty}") { style = { color = new Color(241/255f, 196/255f, 15/255f), fontSize = 9 } });

                var botRow = new VisualElement() { style = { alignItems = Align.FlexEnd } };
                botRow.Add(new Label($"Bonus: +{gen.effectivenessBonus:P0} DMG | +{gen.moraleBonus:P0} Morale") { style = { color = new Color(0, 230/255f, 118/255f), fontSize = 9 } });
                string status = gen.isDeployed ? $"Deployed to {gen.assignedOperation}" : "In Reserve";
                botRow.Add(new Label(status) { style = { color = gen.isDeployed ? Color.white : Color.grey, fontSize = 9, unityFontStyleAndWeight = FontStyle.Italic } });

                row.Add(topRow);
                row.Add(botRow);
                _generalsContainer.Add(row);
            }
        }

        private void PopulateBlockades(CountryDataSO country)
        {
            if (_blockadesContainer == null) return;
            _blockadesContainer.Clear();

            var gm = GameManager.Instance;
            if (gm == null) return;

            bool foundAny = false;
            foreach (var enemy in gm.allCountries)
            {
                if (enemy == country || enemy.activeBlockades == null) continue;
                foreach (var b in enemy.activeBlockades.FindAll(x => x.blockerCountry == country.countryName))
                {
                    foundAny = true;
                    var row = new VisualElement() { style = { marginBottom = 6, paddingTop = 4, paddingBottom = 4, paddingLeft = 4, paddingRight = 4, backgroundColor = new Color(33/255f, 150/255f, 243/255f, 0.2f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
                    row.Add(new Label($"BLOCKADING: {b.targetCountry}") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                    row.Add(new Label($"Ships Assigned: {b.frigatesAssigned} Frig / {b.destroyersAssigned} Des") { style = { color = new Color(200/255f, 200/255f, 200/255f), fontSize = 9 } });
                    row.Add(new Label($"Trade Suppressed: {b.tradeSuppressionPercent}%") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                    _blockadesContainer.Add(row);
                }
            }

            if (!foundAny)
            {
                _blockadesContainer.Add(new Label("No Active Blockades Ordered.") { style = { color = Color.grey, fontSize = 10, unityFontStyleAndWeight = FontStyle.Italic } });
            }
        }

        private void PopulateWMD(CountryDataSO country)
        {
            if (_wmdProgressBar == null || _wmdStatusLabel == null || _wmdOperationalBadge == null) return;

            _wmdProgressBar.value = country.wmdDevelopmentProgress;

            if (country.hasNuclearWeapon)
            {
                _wmdProgressBar.style.display = DisplayStyle.None;
                _wmdStatusLabel.style.display = DisplayStyle.None;
                _wmdOperationalBadge.style.display = DisplayStyle.Flex;
                _wmdOperationalBadge.text = "NUCLEAR CAPABILITY SECURED (MAXIMUM DETERRENCE ONSITE)";
            }
            else
            {
                _wmdProgressBar.style.display = DisplayStyle.Flex;
                _wmdStatusLabel.style.display = DisplayStyle.Flex;
                _wmdOperationalBadge.style.display = DisplayStyle.None;

                if (country.wmdDevelopmentProgress == 0)
                {
                    _wmdStatusLabel.text = "Program Offline — Requires Funding Directive";
                    _wmdStatusLabel.style.color = new Color(241/255f, 196/255f, 15/255f);
                }
                else
                {
                    _wmdStatusLabel.text = country.wmdProgramDiscovered 
                        ? $"Active Enrichment (Risk Critical) - {country.wmdDevelopmentProgress}%" 
                        : $"Covert Enrichment Active - {country.wmdDevelopmentProgress}%";
                    
                    _wmdStatusLabel.style.color = country.wmdProgramDiscovered 
                        ? new Color(255/255f, 82/255f, 82/255f) 
                        : new Color(0, 230/255f, 118/255f);
                }
            }
        }

        private static string BuildMilBriefing(CountryDataSO c, GameManager gm, WarStage stage)
        {
            if (c.unitRifles < c.armyStats.personnel)
                return $"CRITICAL WEAPONS SHORTAGE: Insufficient service rifles ({c.unitRifles:N0}) to fully equip active army ({c.armyStats.personnel:N0}). Tactical operations running at 50% efficiency! Procure rifles under the WEAPONS tab.";
            if (stage == WarStage.Conflict)
                return $"Nation at war. Readiness {c.militaryReadiness:F0}% — prioritize logistics and COIN taskings.";
            if (c.reserveAmmo < 50f || c.reserveFuel < 50f)
                return "Supply lines strained. Convoy escorts and reserve replenishment recommended.";
            if (c.regions.Exists(r => r.security < 40f))
                return "Internal security deteriorating in at least one theatre. Consider COIN posture.";
            return $"Peacetime posture holding. Joint readiness {c.militaryReadiness:F0}% across {c.regions.Count} regions.";
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

        private void PopulateTheatre(CountryDataSO country, GameManager gm)
        {
            if (_theatreStatusList == null) return;
            _theatreStatusList.Clear();

            if (country.regions.Count == 0)
            {
                _theatreStatusList.Add(new Label("No regional theatre data.") { style = { color = Color.grey } });
                return;
            }

            foreach (var region in country.regions)
            {
                string status = region.security >= 70f ? "SECURE" : (region.security >= 45f ? "CONTESTED" : "CRITICAL");
                Color c = region.security >= 70f ? new Color(0, 230/255f, 118/255f)
                    : (region.security >= 45f ? new Color(241/255f, 196/255f, 15/255f) : new Color(255/255f, 82/255f, 82/255f));

                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                row.Add(new Label(region.regionName) { style = { color = Color.white, fontSize = 11 } });
                row.Add(new Label($"{status} · Sec {region.security:F0}% · Stab {region.stability:F0}%") { style = { color = c, fontSize = 10 } });
                _theatreStatusList.Add(row);
            }
        }

        private void PopulateOperations(CountryDataSO country, GameManager gm)
        {
            if (_activeOpsList == null) return;
            _activeOpsList.Clear();

            if (gm?.WarSystem != null && gm.allCountries != null)
            {
                bool atWar = false;
                foreach (var other in gm.allCountries)
                {
                    if (other == country) continue;
                    if (gm.WarSystem.GetWarStage(other) != StateCraft.Interfaces.WarStage.Conflict) continue;
                    atWar = true;

                    var frontCard = new VisualElement();
                    frontCard.style.marginBottom      = 12;
                    frontCard.style.paddingTop        = 14; frontCard.style.paddingBottom = 14;
                    frontCard.style.paddingLeft       = 14; frontCard.style.paddingRight  = 14;
                    frontCard.style.backgroundColor   = new Color(0.12f, 0.04f, 0.04f, 0.98f);
                    frontCard.style.borderTopWidth    = 1; frontCard.style.borderBottomWidth = 1;
                    frontCard.style.borderLeftWidth   = 3; frontCard.style.borderRightWidth  = 1;
                    frontCard.style.borderLeftColor   = new Color(1f, 0.25f, 0.25f, 0.9f);
                    frontCard.style.borderTopColor    = new Color(0.4f, 0.1f, 0.1f, 0.6f);
                    frontCard.style.borderBottomColor = new Color(0.4f, 0.1f, 0.1f, 0.6f);
                    frontCard.style.borderRightColor  = new Color(0.4f, 0.1f, 0.1f, 0.6f);
                    frontCard.style.borderTopLeftRadius    = 4; frontCard.style.borderTopRightRadius    = 4;
                    frontCard.style.borderBottomLeftRadius = 4; frontCard.style.borderBottomRightRadius = 4;

                    var fHeader = new VisualElement();
                    fHeader.style.flexDirection  = FlexDirection.Row;
                    fHeader.style.justifyContent = Justify.SpaceBetween;
                    fHeader.style.marginBottom   = 8;

                    fHeader.Add(new Label($"⚔️  ACTIVE FRONT: {other.countryName.ToUpper()}") {
                        style = { color = new Color(1f, 0.45f, 0.45f), fontSize = 11,
                                  unityFontStyleAndWeight = FontStyle.Bold }
                    });

                    string warPhase = country.warTurnsActive <= 2 ? "MOBILISATION" :
                                      country.warTurnsActive <= 6 ? "OFFENSIVE" :
                                      country.warTurnsActive <= 18 ? "ATTRITION" : "GRINDING WAR";
                    var phaseLabel = new Label($"MONTH {country.warTurnsActive} — {warPhase}");
                    phaseLabel.style.color     = new Color(0.9f, 0.7f, 0.2f);
                    phaseLabel.style.fontSize  = 8;
                    phaseLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    fHeader.Add(phaseLabel);
                    frontCard.Add(fHeader);

                    float myPower    = CalculateFrontPower(country, other.countryName);
                    float enemyPower = CalculateFrontPower(other, country.countryName);
                    float totalPower = Mathf.Max(myPower + enemyPower, 1f);
                    float myPct      = myPower / totalPower;

                    var barLabel = new VisualElement();
                    barLabel.style.flexDirection  = FlexDirection.Row;
                    barLabel.style.justifyContent = Justify.SpaceBetween;
                    barLabel.style.marginBottom   = 3;
                    barLabel.Add(new Label($"OUR COMBAT POWER: {myPower:N0}") { style = { color = new Color(0.4f, 0.85f, 0.5f), fontSize = 8 } });
                    barLabel.Add(new Label($"ENEMY COMBAT POWER: {enemyPower:N0}") { style = { color = new Color(1f, 0.45f, 0.45f), fontSize = 8 } });
                    frontCard.Add(barLabel);

                    var barBg = new VisualElement();
                    barBg.style.height = 8; barBg.style.flexDirection = FlexDirection.Row;
                    barBg.style.borderTopLeftRadius    = 4; barBg.style.borderTopRightRadius    = 4;
                    barBg.style.borderBottomLeftRadius = 4; barBg.style.borderBottomRightRadius = 4;
                    barBg.style.marginBottom = 10; barBg.style.overflow = Overflow.Hidden;

                    var ourBar = new VisualElement();
                    ourBar.style.width           = Length.Percent(myPct * 100f);
                    ourBar.style.backgroundColor = new Color(0.2f, 0.75f, 0.35f, 0.9f);
                    barBg.Add(ourBar);

                    var enemyBar = new VisualElement();
                    enemyBar.style.width           = Length.Percent((1f - myPct) * 100f);
                    enemyBar.style.backgroundColor = new Color(0.85f, 0.22f, 0.22f, 0.9f);
                    barBg.Add(enemyBar);
                    frontCard.Add(barBg);

                    var statsRow = new VisualElement();
                    statsRow.style.flexDirection  = FlexDirection.Row;
                    statsRow.style.flexWrap       = Wrap.Wrap;
                    statsRow.style.marginBottom   = 8;

                    void AddStat(string label, string val, Color col)
                    {
                        var s = new VisualElement();
                        s.style.marginRight = 16; s.style.marginBottom = 4;
                        s.Add(new Label(label) { style = { color = new Color(0.5f, 0.5f, 0.6f), fontSize = 7 } });
                        s.Add(new Label(val)   { style = { color = col, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                        statsRow.Add(s);
                    }

                    AddStat("EXHAUSTION",     $"{country.warExhaustion:F0}%",
                        country.warExhaustion > 70f ? new Color(1f, 0.3f, 0.3f) :
                        country.warExhaustion > 40f ? new Color(1f, 0.75f, 0.2f) : Color.green);
                    AddStat("WILL TO FIGHT",  $"{country.warWillToFight:F0}%",
                        country.warWillToFight < 30f ? new Color(1f, 0.3f, 0.3f) : Color.white);
                    AddStat("TOTAL KIA",      $"{country.totalKIA:N0}",
                        country.totalKIA > 5000 ? new Color(1f, 0.4f, 0.4f) : Color.white);
                    AddStat("AMMO",           $"{country.reserveAmmo:F0}",
                        country.reserveAmmo < 30f ? new Color(1f, 0.4f, 0.4f) : Color.white);
                    AddStat("FUEL",           $"{country.reserveFuel:F0}",
                        country.reserveFuel < 20f ? new Color(1f, 0.4f, 0.4f) : Color.white);
                    frontCard.Add(statsRow);

                    var enemyRow = new VisualElement();
                    enemyRow.style.flexDirection  = FlexDirection.Row;
                    enemyRow.style.justifyContent = Justify.SpaceBetween;
                    enemyRow.style.marginBottom   = 8;
                    enemyRow.Add(new Label($"Enemy exhaustion: {other.warExhaustion:F0}%  |  Enemy will: {other.warWillToFight:F0}%  |  Enemy KIA: {other.totalKIA:N0}") {
                        style = { color = new Color(0.55f, 0.55f, 0.65f), fontSize = 8 }
                    });
                    frontCard.Add(enemyRow);

                    string momMsg;
                    Color  momCol;
                    if (myPower > enemyPower * 1.5f) { momMsg = "▲ WE HOLD THE INITIATIVE — press the advantage"; momCol = Color.green; }
                    else if (enemyPower > myPower * 1.5f) { momMsg = "▼ ENEMY HAS THE INITIATIVE — reinforce or pull back"; momCol = new Color(1f, 0.35f, 0.35f); }
                    else if (myPower > enemyPower) { momMsg = "▲ SLIGHT ADVANTAGE — keep deploying to consolidate"; momCol = new Color(0.4f, 0.85f, 0.5f); }
                    else if (enemyPower > myPower) { momMsg = "▼ SLIGHT DISADVANTAGE — deploy reinforcements"; momCol = new Color(1f, 0.65f, 0.2f); }
                    else { momMsg = "◼ STALEMATE — neither side advancing"; momCol = Color.yellow; }

                    frontCard.Add(new Label(momMsg) { style = { color = momCol, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });

                    _activeOpsList.Add(frontCard);
                }

                if (atWar && (country.reserveAmmo < 30f || country.reserveFuel < 20f))
                {
                    var supplyWarn = new VisualElement();
                    supplyWarn.style.backgroundColor = new Color(0.3f, 0.1f, 0f, 0.9f);
                    supplyWarn.style.paddingTop = 8; supplyWarn.style.paddingBottom = 8;
                    supplyWarn.style.paddingLeft = 12; supplyWarn.style.paddingRight = 12;
                    supplyWarn.style.marginBottom = 10;
                    supplyWarn.style.borderTopLeftRadius = 4; supplyWarn.style.borderTopRightRadius = 4;
                    supplyWarn.style.borderBottomLeftRadius = 4; supplyWarn.style.borderBottomRightRadius = 4;
                    string warns = "";
                    if (country.reserveAmmo < 30f) warns += $"⚠ AMMO {country.reserveAmmo:F0}  ";
                    if (country.reserveFuel < 20f) warns += $"⚠ FUEL {country.reserveFuel:F0}  ";
                    supplyWarn.Add(new Label($"SUPPLY CRITICAL: {warns.Trim()} — Resupply via Logistics tab") {
                        style = { color = new Color(1f, 0.6f, 0.2f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold }
                    });
                    _activeOpsList.Add(supplyWarn);
                }
            }

            var ops = new List<MilitaryOperation>(country.activeOperations);

            if (ops.Count == 0)
            {
                _activeOpsList.Add(new Label("No active operations — theatre quiet.") { style = { color = Color.grey, marginTop = 12 } });
                return;
            }

            foreach (var op in ops)
            {
                var card = new VisualElement();
                card.AddToClassList("sub-panel");
                card.style.paddingTop = 12;
                card.style.paddingBottom = 12;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;
                card.style.marginBottom = 8;

                bool isBorderDefense = op.type == "DEFEND BORDERS";
                if (isBorderDefense)
                {
                    card.style.borderLeftWidth = 3;
                    card.style.borderLeftColor = new Color(76/255f, 175/255f, 80/255f, 0.9f);
                }

                card.Add(new Label(op.opName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
                card.Add(new Label($"{op.type} · {op.regionName}") { style = { color = Color.grey, fontSize = 9, marginTop = 4 } });

                string forceDetails = $"Deployment: {op.deployedTroops:N0} Troops";
                if (op.deployedRDF > 0) forceDetails += $" · {op.deployedRDF} RDF";
                if (op.deployedTanks > 0) forceDetails += $" · {op.deployedTanks} Tanks";
                if (op.deployedAPCs > 0) forceDetails += $" · {op.deployedAPCs} APCs";
                if (op.deployedFighters > 0) forceDetails += $" · {op.deployedFighters} Jets";
                if (op.deployedBombers > 0) forceDetails += $" · {op.deployedBombers} Bombers";
                if (op.deployedUAVs > 0) forceDetails += $" · {op.deployedUAVs} UAVs";
                if (op.deployedHelis > 0) forceDetails += $" · {op.deployedHelis} Helis";
                if (op.deployedFrigates > 0) forceDetails += $" · {op.deployedFrigates} Frigates";
                if (op.deployedSubmarines > 0) forceDetails += $" · {op.deployedSubmarines} Subs";
                card.Add(new Label(forceDetails) { style = { color = new Color(200/255f, 200/255f, 200/255f), fontSize = 8, marginTop = 2 } });

                var footer = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 8 } };
                string displayStatus = op.turnsToDeploy > 0 ? $"DEPLOYING ({op.turnsToDeploy}m)" : op.status.ToUpper();
                Color statusColor = op.turnsToDeploy > 0 ? new Color(241/255f, 196/255f, 15/255f) : new Color(0, 210/255f, 255/255f);
                footer.Add(new Label(displayStatus) { style = { color = statusColor, fontSize = 9 } });
                
                float opCostB = (op.costPerMonth / 1000f) * country.exchangeRate;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                footer.Add(new Label($"{sym}{opCostB:F2}B/mo") { style = { color = Color.grey, fontSize = 8 } });
                footer.Add(new Label($"{op.deployedTroops:N0} INF") { style = { color = Color.grey, fontSize = 8 } });
                card.Add(footer);

                if (country.activeOperations.Contains(op))
                {
                    var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 8 } };

                    var reinforceBtn = new Button() { text = "REINFORCE" };
                    reinforceBtn.AddToClassList("button-premium");
                    reinforceBtn.style.height = 22;
                    reinforceBtn.style.fontSize = 8;
                    reinforceBtn.style.backgroundColor = new Color(76/255f, 175/255f, 80/255f, 0.4f);
                    reinforceBtn.style.width = Length.Percent(48);
                    
                    var captureOp = op;
                    reinforceBtn.clicked += () => ShowReinforceModal(captureOp);
                    actionRow.Add(reinforceBtn);

                    var terminateBtn = new Button() { text = "DECOMMISSION OP" };
                    terminateBtn.AddToClassList("button-premium");
                    terminateBtn.style.height = 22;
                    terminateBtn.style.fontSize = 8;
                    terminateBtn.style.width = Length.Percent(48);
                    
                    terminateBtn.clicked += () =>
                    {
                        country.unitInfantry += captureOp.deployedTroops;
                        country.rdfStats.personnel += captureOp.deployedRDF;
                        country.unitAttackHelis += captureOp.deployedHelis;
                        country.unitTanks += captureOp.deployedTanks;
                        country.unitAPCs += captureOp.deployedAPCs;
                        country.unitFighters += captureOp.deployedFighters;
                        country.unitBombers += captureOp.deployedBombers;
                        country.unitUAVs += captureOp.deployedUAVs;
                        country.unitFrigates += captureOp.deployedFrigates;
                        country.unitSubmarines += captureOp.deployedSubmarines;

                        string retDetails = $"{captureOp.deployedTroops:N0} Troops";
                        if (captureOp.deployedRDF > 0) retDetails += $", {captureOp.deployedRDF} RDF";
                        if (captureOp.deployedTanks > 0) retDetails += $", {captureOp.deployedTanks} Tanks";
                        if (captureOp.deployedAPCs > 0) retDetails += $", {captureOp.deployedAPCs} APCs";
                        if (captureOp.deployedFighters > 0) retDetails += $", {captureOp.deployedFighters} Jets";
                        if (captureOp.deployedBombers > 0) retDetails += $", {captureOp.deployedBombers} Bombers";
                        if (captureOp.deployedUAVs > 0) retDetails += $", {captureOp.deployedUAVs} UAVs";
                        if (captureOp.deployedHelis > 0) retDetails += $", {captureOp.deployedHelis} Helis";
                        if (captureOp.deployedFrigates > 0) retDetails += $", {captureOp.deployedFrigates} Ships";
                        if (captureOp.deployedSubmarines > 0) retDetails += $", {captureOp.deployedSubmarines} Subs";

                        country.activeOperations.Remove(captureOp);
                        NotificationManager.Instance?.AddNotification($"TERMINATED: {captureOp.opName} decommissioned. {retDetails} returned.", Color.yellow);
                        NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} decommissioned {captureOp.opName}. Deployed forces returned to barracks.");
                        RefreshMilitaryData();
                    };
                    actionRow.Add(terminateBtn);
                    card.Add(actionRow);
                }

                _activeOpsList.Add(card);
            }

            var warStage = gm.WarSystem?.GetWarStage(country) ?? WarStage.Peace;
            if (warStage == WarStage.Conflict)
            {
                var warActHeader = new Label("⚔ WARTIME QUICK ACTIONS") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 14, marginBottom = 6 } };
                _activeOpsList.Add(warActHeader);

                var borderCard = new VisualElement();
                borderCard.AddToClassList("sub-panel");
                borderCard.style.paddingTop = 14;
                borderCard.style.paddingBottom = 14;
                borderCard.style.paddingLeft = 14;
                borderCard.style.paddingRight = 14;
                borderCard.style.marginBottom = 10;
                borderCard.style.borderLeftWidth = 3;
                borderCard.style.borderLeftColor = new Color(76/255f, 175/255f, 80/255f, 0.9f);

                bool alreadyDefending = country.activeOperations.Exists(o => o.type == "DEFEND BORDERS");
                borderCard.Add(new Label("🛡 NATIONAL BORDER DEFENSE") { style = { color = new Color(76/255f, 175/255f, 80/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
                borderCard.Add(new Label(alreadyDefending
                    ? "Border defense already active. Reinforce it using the REINFORCE button above."
                    : "Deploy troops and armour to hold your borders. Border units reduce enemy breakthrough damage by 50%.")
                    { style = { color = Color.grey, fontSize = 8, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });

                if (!alreadyDefending)
                {
                    var deployBorderBtn = new Button(() =>
                    {
                        if (country.unitInfantry < 1000)
                        {
                            NotificationManager.Instance?.AddNotification("BORDER DEFENSE FAILED: Need at least 1,000 infantry available.", Color.red);
                            return;
                        }
                        int borderTroops = Mathf.Min(5000, country.unitInfantry / 4);
                        int borderTanks  = Mathf.Min(20,   country.unitTanks);
                        int borderAPCs   = Mathf.Min(30,   country.unitAPCs);
                        country.unitInfantry -= borderTroops;
                        country.unitTanks    -= borderTanks;
                        country.unitAPCs     -= borderAPCs;

                        var borderOp = new MilitaryOperation
                        {
                            opName         = $"Operation IRON WALL",
                            regionName     = country.countryName + " (Borders)",
                            type           = "DEFEND BORDERS",
                            status         = "Active",
                            costPerMonth   = borderTroops * 0.005f,
                            deployedTroops = borderTroops,
                            deployedTanks  = borderTanks,
                            deployedAPCs   = borderAPCs,
                            turnsToDeploy  = 2
                        };
                        country.activeOperations.Add(borderOp);

                        NotificationManager.Instance?.AddNotification(
                            $"BORDER DEFENSE DEPLOYED: {borderTroops:N0} troops, {borderTanks} tanks, {borderAPCs} APCs ordered to National Borders (arrives in 2 months).",
                            new Color(76/255f, 175/255f, 80/255f));
                        NewsSystem.Instance?.PublishNews(
                            $"MILITARY: {country.countryName} fortifies its borders with heavy armoured units.");
                        RefreshMilitaryData();

                    }) { text = "🛡 SEND TROOPS TO NATIONAL BORDERS" };
                    deployBorderBtn.AddToClassList("button-premium");
                    deployBorderBtn.style.height = 28;
                    deployBorderBtn.style.fontSize = 9;
                    deployBorderBtn.style.backgroundColor = new Color(76/255f, 175/255f, 80/255f, 0.3f);
                    borderCard.Add(deployBorderBtn);
                }
                _activeOpsList.Add(borderCard);

                var enemy = gm.GetCountryByName(country.warEnemyName)
                         ?? gm.allCountries?.Find(c => c != country && (gm.WarSystem?.GetWarStage(c) == WarStage.Conflict));
                if (enemy != null)
                {
                    var interceptCard = new VisualElement();
                    interceptCard.AddToClassList("sub-panel");
                    interceptCard.style.paddingTop = 14;
                    interceptCard.style.paddingBottom = 14;
                    interceptCard.style.paddingLeft = 14;
                    interceptCard.style.paddingRight = 14;
                    interceptCard.style.marginBottom = 10;
                    interceptCard.style.borderLeftWidth = 3;
                    interceptCard.style.borderLeftColor = new Color(33/255f, 150/255f, 243/255f, 0.9f);

                    interceptCard.Add(new Label("✈ LAUNCH AIR INTERCEPT") { style = { color = new Color(33/255f, 150/255f, 243/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
                    interceptCard.Add(new Label($"Scramble fighters to intercept {enemy.countryName} air assets and stop bombing raids. Costs 40 fuel.")
                        { style = { color = Color.grey, fontSize = 8, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });

                    var captureEnemy = enemy;
                    var interceptBtn = new Button(() =>
                    {
                        if (gm.WarSystem is Systems.WarSystem ws)
                        {
                            ws.LaunchAirIntercept(country, captureEnemy);
                            RefreshMilitaryData();
                        }
                    }) { text = "✈ INTERCEPT ENEMY AIRCRAFT" };
                    interceptBtn.AddToClassList("button-premium");
                    interceptBtn.style.height = 28;
                    interceptBtn.style.fontSize = 9;
                    interceptBtn.style.backgroundColor = new Color(33/255f, 150/255f, 243/255f, 0.3f);
                    interceptCard.Add(interceptBtn);
                    _activeOpsList.Add(interceptCard);
                }
            }
        }

        private void ShowInboxModal()
        {
            var country = GameManager.Instance.playerCountry;
            StateCraft.Systems.InboxSystem.Instance?.ClearRoutineBadge();
            var overlay = new VisualElement() { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0, 0, 0, 0.85f), justifyContent = Justify.Center, alignItems = Align.Center } };
            var modal = new VisualElement() { style = { width = 500, paddingBottom = 20, paddingTop = 20, paddingLeft = 20, paddingRight = 20, backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f), borderTopLeftRadius=8, borderTopRightRadius=8, borderBottomLeftRadius=8, borderBottomRightRadius=8 } };
            SetBorderColor(modal.style, new Color(0, 210/255f, 255/255f, 0.5f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 10 } };
            headerRow.Add(new Label("COMMAND INBOX") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold } });
            
            var closeBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "×" };
            closeBtn.style.width = 30; closeBtn.style.height = 30; closeBtn.style.fontSize = 18;
            closeBtn.AddToClassList("button-premium");
            headerRow.Add(closeBtn);
            modal.Add(headerRow);

            var scroll = new ScrollView { style = { maxHeight = 400, marginBottom = 10 } };
            modal.Add(scroll);

            if (country.inboxMessages == null || country.inboxMessages.Count == 0)
            {
                scroll.Add(new Label("Inbox is empty. No new intelligence.") { style = { color = Color.grey, marginTop = 20, unityTextAlign = TextAnchor.MiddleCenter } });
            }
            else
            {
                var messages = new List<InboxMessage>(country.inboxMessages);
                messages.Reverse();

                foreach (var msg in messages)
                {
                    var card = new VisualElement();
                    card.AddToClassList("sub-panel");
                    card.style.paddingTop = 10; card.style.paddingBottom = 10;
                    card.style.paddingLeft = 10; card.style.paddingRight = 10;
                    card.style.marginBottom = 5;

                    var topRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                    Color senderColor = msg.urgency == "Critical" ? new Color(255/255f, 82/255f, 82/255f) : (msg.urgency == "Important" ? new Color(241/255f, 196/255f, 15/255f) : new Color(0, 210/255f, 255/255f));
                    topRow.Add(new Label(msg.sender) { style = { color = senderColor, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                    topRow.Add(new Label($"Turn {msg.turnReceived}") { style = { color = Color.grey, fontSize = 8 } });
                    card.Add(topRow);

                    card.Add(new Label(msg.subject) { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 2 } });
                    
                    var bodyLabel = new Label(msg.body) { style = { color = new Color(200/255f, 200/255f, 200/255f), fontSize = 10, marginTop = 4 } };
                    bodyLabel.style.whiteSpace = WhiteSpace.Normal;
                    card.Add(bodyLabel);

                    scroll.Add(card);
                }
            }

            var clearBtn = new Button() { text = "CLEAR ALL MESSAGES" };
            clearBtn.AddToClassList("button-premium");
            clearBtn.style.height = 30;
            clearBtn.clicked += () =>
            {
                country.inboxMessages?.Clear();
                overlay.parent?.Remove(overlay);
                RefreshMilitaryData();
            };
            modal.Add(clearBtn);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null) dashboard.Add(overlay);
            else _root?.Add(overlay);
        }

        private void ShowReinforceModal(MilitaryOperation op)
        {
            var country = GameManager.Instance.playerCountry;
            
            var overlay = new VisualElement() { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0, 0, 0, 0.85f), justifyContent = Justify.Center, alignItems = Align.Center } };
            var modal = new VisualElement() { style = { width = 450, paddingBottom = 20, paddingTop = 20, paddingLeft = 20, paddingRight = 20, backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f), borderTopLeftRadius=8, borderTopRightRadius=8, borderBottomLeftRadius=8, borderBottomRightRadius=8 } };
            SetBorderColor(modal.style, new Color(0, 210/255f, 255/255f, 0.5f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            modal.Add(new Label($"REINFORCE: {op.opName}") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 15 } });
            
            var scroll = new ScrollView { style = { maxHeight = 300, marginBottom = 20 } };
            modal.Add(scroll);

            scroll.Add(new Label("AVAILABLE RESERVES TO DEPLOY") { style = { color = Color.grey, fontSize = 9, marginBottom = 10 } });

            SliderInt troopsSlider = null;
            if (country.unitInfantry > 0)
            {
                var troopsLabel = new Label($"INFANTRY (Deploying: 0 / {country.unitInfantry:N0} avail)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } };
                scroll.Add(troopsLabel);
                troopsSlider = new SliderInt(0, country.unitInfantry) { style = { marginBottom = 10 } };
                troopsSlider.RegisterValueChangedCallback(evt => troopsLabel.text = $"INFANTRY (Deploying: {evt.newValue:N0} / {country.unitInfantry:N0} avail)");
                scroll.Add(troopsSlider);
            }

            SliderInt tanksSlider = null;
            if (country.unitTanks > 0)
            {
                var tanksLabel = new Label($"MAIN BATTLE TANKS (Deploying: 0 / {country.unitTanks:N0} avail)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } };
                scroll.Add(tanksLabel);
                tanksSlider = new SliderInt(0, country.unitTanks) { style = { marginBottom = 10 } };
                tanksSlider.RegisterValueChangedCallback(evt => tanksLabel.text = $"MAIN BATTLE TANKS (Deploying: {evt.newValue:N0} / {country.unitTanks:N0} avail)");
                scroll.Add(tanksSlider);
            }

            SliderInt apcsSlider = null;
            if (country.unitAPCs > 0)
            {
                var apcsLabel = new Label($"ARMOURED PERSONNEL CARRIERS (Deploying: 0 / {country.unitAPCs:N0} avail)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } };
                scroll.Add(apcsLabel);
                apcsSlider = new SliderInt(0, country.unitAPCs) { style = { marginBottom = 10 } };
                apcsSlider.RegisterValueChangedCallback(evt => apcsLabel.text = $"ARMOURED PERSONNEL CARRIERS (Deploying: {evt.newValue:N0} / {country.unitAPCs:N0} avail)");
                scroll.Add(apcsSlider);
            }

            SliderInt uavsSlider = null;
            if (country.unitUAVs > 0)
            {
                var uavsLabel = new Label($"TACTICAL UAVs (Deploying: 0 / {country.unitUAVs:N0} avail)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } };
                scroll.Add(uavsLabel);
                uavsSlider = new SliderInt(0, country.unitUAVs) { style = { marginBottom = 10 } };
                uavsSlider.RegisterValueChangedCallback(evt => uavsLabel.text = $"TACTICAL UAVs (Deploying: {evt.newValue:N0} / {country.unitUAVs:N0} avail)");
                scroll.Add(uavsSlider);
            }

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.width = Length.Percent(48);
            cancelBtn.style.height = 30;
            actionRow.Add(cancelBtn);

            var confirmBtn = new Button() { text = "DEPLOY REINFORCEMENTS" };
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.backgroundColor = new Color(0, 210/255f, 255/255f, 0.4f);
            confirmBtn.style.width = Length.Percent(48);
            confirmBtn.style.height = 30;
            confirmBtn.clicked += () =>
            {
                int sendTroops = troopsSlider != null ? troopsSlider.value : 0;
                int sendTanks = tanksSlider != null ? tanksSlider.value : 0;
                int sendApcs = apcsSlider != null ? apcsSlider.value : 0;
                int sendUavs = uavsSlider != null ? uavsSlider.value : 0;

                int totalDeployed = sendTroops + sendTanks + sendApcs + sendUavs;
                if (totalDeployed <= 0)
                {
                    NotificationManager.Instance?.AddNotification("INVALID COMMAND: Select units to reinforce.", Color.red);
                    return;
                }

                op.deployedTroops += sendTroops;
                op.deployedTanks += sendTanks;
                op.deployedAPCs += sendApcs;
                op.deployedUAVs += sendUavs;

                country.unitInfantry = Mathf.Max(0, country.unitInfantry - sendTroops);
                country.unitTanks = Mathf.Max(0, country.unitTanks - sendTanks);
                country.unitAPCs = Mathf.Max(0, country.unitAPCs - sendApcs);
                country.unitUAVs = Mathf.Max(0, country.unitUAVs - sendUavs);

                float addedBoost = (sendTroops / 1000f) * 0.5f + sendTanks * 0.8f + sendApcs * 0.4f + sendUavs * 1.2f;
                op.securityBoostRate += addedBoost;

                overlay.parent?.Remove(overlay);
                NotificationManager.Instance?.AddNotification($"REINFORCEMENTS DEPLOYED: {totalDeployed:N0} structural units added to {op.opName}.", Color.cyan);
                RefreshMilitaryData();
            };
            actionRow.Add(confirmBtn);
            modal.Add(actionRow);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null) dashboard.Add(overlay);
            else _panelOps.Add(overlay);
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

        private int GetTotalInTraining(CountryDataSO country, string id)
        {
            if (country.trainingPipelines == null) return 0;
            int total = 0;
            foreach (var pipe in country.trainingPipelines)
            {
                if (pipe.id == id) total += pipe.amount;
            }
            return total;
        }

        private void PopulateRecruit(CountryDataSO country)
        {
            if (_personnelGrid == null || _weaponsGrid == null) return;
            _personnelGrid.Clear();
            _weaponsGrid.Clear();
            if (_basesGrid != null) _basesGrid.Clear();

            int infantryCapacity = 5000 + (country.baseArmyCount * 15000);
            int inTrainingInfantry = GetTotalInTraining(country, "INF");
            string infantryLabel = $"Infantry Battalion (Cap: {country.armyStats.personnel + inTrainingInfantry:N0}/{infantryCapacity:N0})";
            AddRecruitCard(infantryLabel, "INF", country.armyStats.personnel, 0.05f, 1000, _personnelGrid, true);

            int policeCapacity = 5000 + (country.baseArmyCount * 5000);
            int inTrainingPolice = GetTotalInTraining(country, "POLICE");
            string policeLabel = $"Gendarmerie Training (Cap: {country.nationalPoliceSize + inTrainingPolice:N0}/{policeCapacity:N0})";
            AddRecruitCard(policeLabel, "POLICE", country.nationalPoliceSize, 0.02f, 500, _personnelGrid, true);

            int navyCapacity = 1000 + (country.baseNavyCount * 3000);
            int inTrainingNavy = GetTotalInTraining(country, "NAVY_PERS");
            string navyLabel = $"Naval Crew Training (Cap: {country.navyStats.personnel + inTrainingNavy:N0}/{navyCapacity:N0})";
            AddRecruitCard(navyLabel, "NAVY_PERS", country.navyStats.personnel, 0.04f, 250, _personnelGrid, true);

            int airCapacity = 500 + (country.baseAirForceCount * 2000);
            int inTrainingAir = GetTotalInTraining(country, "AIR_PERS");
            string airLabel = $"Airforce Flight Academy (Cap: {country.airForceStats.personnel + inTrainingAir:N0}/{airCapacity:N0})";
            AddRecruitCard(airLabel, "AIR_PERS", country.airForceStats.personnel, 0.06f, 150, _personnelGrid, true);

            int rdfCapacity = 200 + (country.baseRDFCount * 1000);
            int inTrainingRDF = GetTotalInTraining(country, "RDF_PERS");
            string rdfLabel = $"Elite Spec-Ops RDF (Cap: {country.rdfStats.personnel + inTrainingRDF:N0}/{rdfCapacity:N0})";
            AddRecruitCard(rdfLabel, "RDF_PERS", country.rdfStats.personnel, 0.08f, 100, _personnelGrid, true);

            if (country.trainingPipelines != null && country.trainingPipelines.Count > 0)
            {
                var divider = new VisualElement();
                divider.style.width = Length.Percent(100);
                divider.style.height = 1;
                divider.style.backgroundColor = new Color(255/255f, 255/255f, 255/255f, 0.1f);
                divider.style.marginTop = 15;
                divider.style.marginBottom = 15;
                _personnelGrid.Add(divider);

                var pipelineHeader = new Label("ACTIVE TRAINING ACADEMIES");
                pipelineHeader.style.width = Length.Percent(100);
                pipelineHeader.style.color = new Color(255/255f, 235/255f, 59/255f);
                pipelineHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                pipelineHeader.style.fontSize = 11;
                pipelineHeader.style.marginBottom = 10;
                _personnelGrid.Add(pipelineHeader);

                foreach (var pipe in country.trainingPipelines)
                {
                    var card = new VisualElement();
                    card.AddToClassList("sub-panel");
                    card.style.width = Length.Percent(48);
                    card.style.marginBottom = 10;
                    card.style.paddingTop = 10;
                    card.style.paddingBottom = 10;
                    card.style.paddingLeft = 15;
                    card.style.paddingRight = 15;

                    var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                    row.Add(new Label($"{pipe.amount:N0} {pipe.unitLabel}") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
                    row.Add(new Label($"{pipe.monthsRemaining} Months Left") { style = { color = new Color(255/255f, 152/255f, 0), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 9 } });
                    card.Add(row);

                    float percentElapsed = ((6 - pipe.monthsRemaining) / 6f) * 100f;
                    var progress = new ProgressBar() { value = percentElapsed, highValue = 100, style = { height = 4, marginTop = 6 } };
                    card.Add(progress);

                    _personnelGrid.Add(card);
                }
            }

            AddWeaponCard("Tactical Service Rifles", "RIFLE", country.unitRifles, 0.01f, 1000, _weaponsGrid, country);
            AddWeaponCard("Artillery Battery", "ARTILLERY", country.unitArtillery, 0.10f, 15, _weaponsGrid, country);
            AddWeaponCard("Main Battle Tanks", "TNK", country.unitTanks, 0.25f, 10, _weaponsGrid, country);
            AddWeaponCard("APC Platoon", "APC", country.unitAPCs, 0.15f, 20, _weaponsGrid, country);
            AddWeaponCard("Tactical UAVs", "UAV", country.unitUAVs, 0.12f, 10, _weaponsGrid, country);
            AddWeaponCard("Multi-role Fighter", "FTR", country.unitFighters, 0.5f, 2, _weaponsGrid, country);
            AddWeaponCard("Strategic Bombers", "BOMBER", country.unitBombers, 0.65f, 2, _weaponsGrid, country);
            AddWeaponCard("Attack Helicopter", "HEL", country.unitAttackHelis, 0.3f, 4, _weaponsGrid, country);
            AddWeaponCard("Patrol Boats", "PATROL_BOAT", country.unitPatrolBoats, 0.18f, 5, _weaponsGrid, country);
            AddWeaponCard("Coastal Frigate", "FRG", country.unitFrigates, 1.2f, 1, _weaponsGrid, country);
            AddWeaponCard("Attack Submarine", "SUB", country.unitSubmarines, 2.5f, 1, _weaponsGrid, country);
            AddWeaponCard("Destroyers", "DESTROYER", country.unitDestroyers, 2.5f, 1, _weaponsGrid, country);
            
            if (_basesGrid != null)
            {
                AddRecruitCard("Military Base (Army)", "BASE_ARMY", country.baseArmyCount, 0.40f, 1, _basesGrid, false);
                AddRecruitCard("Naval Base (Navy)", "BASE_NAVY", country.baseNavyCount, 0.80f, 1, _basesGrid, false);
                AddRecruitCard("Strategic Airbase", "BASE_AIR", country.baseAirForceCount, 0.60f, 1, _basesGrid, false);
                AddRecruitCard("RDF Command Center", "BASE_RDF", country.baseRDFCount, 0.50f, 1, _basesGrid, false);
                AddRecruitCard("Defense Factory Complex", "BASE_DEF_FAC", country.baseDefenseFactoryCount, 0.70f, 1, _basesGrid, false);
            }
        }

        private void PopulateGlobalPower(CountryDataSO country)
        {
            if (_globalPowerList == null || GameManager.Instance == null) return;
            _globalPowerList.Clear();

            var gm = GameManager.Instance;
            if (gm.allCountries == null || gm.allCountries.Count == 0) return;

            var sortedCountries = new List<CountryDataSO>(gm.allCountries);
            sortedCountries.Sort((a, b) => b.GetMilitaryFirepowerScore().CompareTo(a.GetMilitaryFirepowerScore()));

            float maxScore = sortedCountries.Count > 0 ? sortedCountries[0].GetMilitaryFirepowerScore() : 1f;
            if (maxScore <= 0f) maxScore = 1f;

            for (int i = 0; i < sortedCountries.Count; i++)
            {
                var c = sortedCountries[i];
                float score = c.GetMilitaryFirepowerScore();

                var row = new VisualElement();
                row.AddToClassList("sub-panel");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.marginBottom = 6;
                row.style.paddingTop = 8;
                row.style.paddingBottom = 8;
                row.style.paddingLeft = 15;
                row.style.paddingRight = 15;

                var leftGroup = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                
                var rankLabel = new Label($"#{i + 1}") 
                { 
                    style = { 
                        color = i == 0 ? new Color(255/255f, 215/255f, 0) : Color.grey, 
                        unityFontStyleAndWeight = FontStyle.Bold, 
                        fontSize = 11, 
                        marginRight = 15,
                        width = 25
                    } 
                };
                leftGroup.Add(rankLabel);

                var nameLabel = new Label(c.countryName.ToUpper()) 
                { 
                    style = { 
                        color = c == country ? new Color(0, 210/255f, 255/255f) : Color.white, 
                        unityFontStyleAndWeight = c == country ? FontStyle.Bold : FontStyle.Normal,
                        fontSize = 10,
                        width = 120
                    } 
                };
                leftGroup.Add(nameLabel);
                row.Add(leftGroup);

                var barContainer = new VisualElement() { style = { flexGrow = 1, height = 8, backgroundColor = new Color(0, 0, 0, 0.4f), marginLeft = 20, marginRight = 20 } };
                barContainer.style.borderTopLeftRadius = 4;
                barContainer.style.borderTopRightRadius = 4;
                barContainer.style.borderBottomLeftRadius = 4;
                barContainer.style.borderBottomRightRadius = 4;

                float percent = (score / maxScore) * 100f;
                var fillBar = new VisualElement() 
                { 
                    style = { 
                        width = Length.Percent(percent), 
                        height = Length.Percent(100), 
                        backgroundColor = c == country ? new Color(0, 210/255f, 255/255f) : new Color(255/255f, 82/255f, 82/255f)
                    } 
                };
                fillBar.style.borderTopLeftRadius = 4;
                fillBar.style.borderTopRightRadius = 4;
                fillBar.style.borderBottomLeftRadius = 4;
                fillBar.style.borderBottomRightRadius = 4;

                barContainer.Add(fillBar);
                row.Add(barContainer);

                var rightGroup = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                
                var scoreLabel = new Label($"{score:N0} MPI") 
                { 
                    style = { 
                        color = Color.white, 
                        unityFontStyleAndWeight = FontStyle.Bold, 
                        fontSize = 10,
                        marginRight = 15,
                        width = 80,
                        unityTextAlign = TextAnchor.MiddleRight
                    } 
                };
                rightGroup.Add(scoreLabel);

                var badge = new Label();
                badge.style.fontSize = 8;
                badge.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.style.paddingTop = 2;
                badge.style.paddingBottom = 2;
                badge.style.paddingLeft = 6;
                badge.style.paddingRight = 6;
                badge.style.borderTopLeftRadius = 3;
                badge.style.borderTopRightRadius = 3;
                badge.style.borderBottomLeftRadius = 3;
                badge.style.borderBottomRightRadius = 3;
                badge.style.width = 110;
                badge.style.unityTextAlign = TextAnchor.MiddleCenter;

                if (c == country)
                {
                    badge.text = "YOUR NATION";
                    badge.style.color = new Color(0, 210/255f, 255/255f);
                    badge.style.backgroundColor = new Color(0, 210/255f, 255/255f, 0.1f);
                }
                else if (score > country.GetMilitaryFirepowerScore())
                {
                    badge.text = "SUPERIOR FORCE";
                    badge.style.color = new Color(255/255f, 82/255f, 82/255f);
                    badge.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.1f);
                }
                else
                {
                    badge.text = "MATCHED / INFERIOR";
                    badge.style.color = Color.grey;
                    badge.style.backgroundColor = new Color(128/255f, 128/255f, 128/255f, 0.1f);
                }
                rightGroup.Add(badge);
                row.Add(rightGroup);

                _globalPowerList.Add(row);
            }
        }

        private void PopulateProjects(CountryDataSO country)
        {
            if (_activeProjectsList == null || _availableProjectsList == null) return;
            _activeProjectsList.Clear();
            _availableProjectsList.Clear();

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            if (country.militaryProjects == null || country.militaryProjects.Count == 0)
            {
                var emptyLabel = new Label("No active projects. Commission a directive below.");
                emptyLabel.style.color = Color.grey;
                emptyLabel.style.fontSize = 10;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.marginTop = 20;
                _activeProjectsList.Add(emptyLabel);
            }
            else
            {
                foreach (var proj in country.militaryProjects)
                {
                    var card = new VisualElement();
                    card.AddToClassList("sub-panel");
                    card.style.marginBottom = 12;
                    card.style.paddingTop = 14;
                    card.style.paddingBottom = 14;
                    card.style.paddingLeft = 16;
                    card.style.paddingRight = 16;

                    Color branchColor = proj.branch switch
                    {
                        "Army"     => new Color(255/255f, 152/255f, 0),
                        "Navy"     => new Color(33/255f, 150/255f, 243/255f),
                        "AirForce" => new Color(156/255f, 39/255f, 176/255f),
                        "RDF"      => new Color(255/255f, 82/255f, 82/255f),
                        _          => Color.grey
                    };

                    var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8 } };
                    var nameLabel = new Label(proj.projectName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } };
                    var branchBadge = new Label(proj.branch.ToUpper()) { style = { color = branchColor, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } };
                    branchBadge.style.backgroundColor = new Color(branchColor.r, branchColor.g, branchColor.b, 0.15f);
                    branchBadge.style.paddingLeft = 6; branchBadge.style.paddingRight = 6;
                    branchBadge.style.paddingTop = 2; branchBadge.style.paddingBottom = 2;
                    branchBadge.style.borderTopLeftRadius = 3; branchBadge.style.borderTopRightRadius = 3;
                    branchBadge.style.borderBottomLeftRadius = 3; branchBadge.style.borderBottomRightRadius = 3;
                    headerRow.Add(nameLabel);
                    headerRow.Add(branchBadge);
                    card.Add(headerRow);

                    float elapsed = Mathf.Max(0, proj.totalMonths - proj.monthsRemaining);
                    float pct = proj.totalMonths > 0 ? (elapsed / proj.totalMonths) * 100f : 0f;
                    var barBg = new VisualElement() { style = { height = 6, backgroundColor = new Color(0, 0, 0, 0.4f), marginBottom = 6 } };
                    barBg.style.borderTopLeftRadius = 3; barBg.style.borderTopRightRadius = 3;
                    barBg.style.borderBottomLeftRadius = 3; barBg.style.borderBottomRightRadius = 3;
                    var fill = new VisualElement() { style = { width = Length.Percent(pct), height = Length.Percent(100), backgroundColor = branchColor } };
                    fill.style.borderTopLeftRadius = 3; fill.style.borderTopRightRadius = 3;
                    fill.style.borderBottomLeftRadius = 3; fill.style.borderBottomRightRadius = 3;
                    barBg.Add(fill);
                    card.Add(barBg);

                    var infoRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                    infoRow.Add(new Label($"⏱ {proj.monthsRemaining} months remaining") { style = { color = new Color(255/255f, 152/255f, 0), fontSize = 9 } });
                    infoRow.Add(new Label($"+{proj.strengthGain:F0}% Strength on completion") { style = { color = new Color(0, 230/255f, 118/255f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                    card.Add(infoRow);

                    _activeProjectsList.Add(card);
                }
            }

            var catalog = new System.Collections.Generic.List<(string name, string branch, float strengthGain, float costBillions, int months, string desc)>
            {
                ("Iron Fist Armour Programme",  "Army",     12f, 0.80f, 12, "Procure 50 MBTs & 100 APCs. +12% Army Strength."),
                ("Artillery Modernization",     "Army",     8f,  0.40f,  8, "Upgrade field artillery systems. +8% Army Strength."),
                ("Infantry Combat System",      "Army",     6f,  0.25f,  6, "Advanced body armour & small arms. +6% Army Strength."),
                ("Rapid Reaction Force Uplift", "Army",     5f,  0.30f,  6, "Pre-position stockpiles for fast deployment. +5% Army."),
                ("Blue-Water Fleet Expansion",  "Navy",     15f, 2.50f, 18, "Commission 2 Destroyers & 1 Frigate. +15% Navy Strength."),
                ("Submarine Stealth Pack",      "Navy",     10f, 1.80f, 14, "Acquire 2 SSK attack submarines. +10% Navy Strength."),
                ("Coastal Defence Network",     "Navy",     7f,  0.60f,  9, "Mine layers, patrol boats, radar. +7% Navy Strength."),
                ("Naval Air Integration",       "Navy",     8f,  1.00f, 12, "Carrier-capable aircraft & EW systems. +8% Navy Strength."),
                ("Generation-5 Fighter Block",  "AirForce", 18f, 3.50f, 24, "Procure 12 5th-gen multi-role jets. +18% Air Strength."),
                ("Strategic Bomber Fleet",      "AirForce", 14f, 2.80f, 20, "4 long-range strategic bombers. +14% Air Strength."),
                ("UAV Swarm Programme",         "AirForce", 9f,  0.70f, 10, "50 tactical & 10 MALE UAVs. +9% Air Strength."),
                ("Air Defence Umbrella",        "AirForce", 11f, 1.20f, 12, "S-400 class SAM network. +11% Air Strength."),
                ("Black Sabre Spec-Ops",        "RDF",      20f, 0.50f,  8, "Elite tier-1 operators & equipment. +20% RDF Readiness."),
                ("SIGINT & Cyber Warfare",      "RDF",      12f, 0.35f,  6, "Electronic warfare & signals intel. +12% RDF Readiness."),
                ("Hostage Rescue Unit",         "RDF",      8f,  0.20f,  4, "Dedicated HRU with transport helis. +8% RDF Readiness."),
            };

            var activeNames = new System.Collections.Generic.HashSet<string>();
            if (country.militaryProjects != null)
                foreach (var p in country.militaryProjects) activeNames.Add(p.projectName);

            foreach (var (name, branch, strengthGain, costBillions, months, desc) in catalog)
            {
                float localCost = costBillions * country.exchangeRate;
                bool isActive   = activeNames.Contains(name);
                bool canAfford  = GameManager.Instance != null && GameManager.Instance.playerBudget >= localCost;

                Color branchColor = branch switch
                {
                    "Army"     => new Color(255/255f, 152/255f, 0),
                    "Navy"     => new Color(33/255f, 150/255f, 243/255f),
                    "AirForce" => new Color(156/255f, 39/255f, 176/255f),
                    "RDF"      => new Color(255/255f, 82/255f, 82/255f),
                    _          => Color.grey
                };

                var card = new VisualElement();
                card.AddToClassList("sub-panel");
                card.style.marginBottom = 12;
                card.style.paddingTop = 14;
                card.style.paddingBottom = 14;
                card.style.paddingLeft = 16;
                card.style.paddingRight = 16;
                card.style.opacity = isActive ? 0.45f : 1.0f;

                var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                var nameLabel = new Label(name) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11, flexShrink = 1 } };
                var branchBadge = new Label(branch.ToUpper()) { style = { color = branchColor, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } };
                branchBadge.style.backgroundColor = new Color(branchColor.r, branchColor.g, branchColor.b, 0.15f);
                branchBadge.style.paddingLeft = 6; branchBadge.style.paddingRight = 6;
                branchBadge.style.paddingTop = 2; branchBadge.style.paddingBottom = 2;
                branchBadge.style.borderTopLeftRadius = 3; branchBadge.style.borderTopRightRadius = 3;
                branchBadge.style.borderBottomLeftRadius = 3; branchBadge.style.borderBottomRightRadius = 3;
                headerRow.Add(nameLabel);
                headerRow.Add(branchBadge);
                card.Add(headerRow);

                var descLabel = new Label(desc) { style = { color = Color.grey, fontSize = 9, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } };
                card.Add(descLabel);

                var statsRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 10 } };
                statsRow.Add(new Label($"Cost: {UiFormat.Money(localCost, sym)}") { style = { color = canAfford ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                statsRow.Add(new Label($"Duration: {months} months") { style = { color = Color.grey, fontSize = 9 } });
                statsRow.Add(new Label($"+{strengthGain:F0}% on completion") { style = { color = new Color(0, 210/255f, 255/255f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                card.Add(statsRow);

                var btn = new Button();
                btn.AddToClassList("button-premium");
                btn.style.height = 26;
                btn.style.fontSize = 9;

                if (isActive)
                {
                    btn.text = "ALREADY IN PROGRESS";
                    btn.SetEnabled(false);
                }
                else if (!canAfford)
                {
                    btn.text = $"INSUFFICIENT FUNDS — Need {UiFormat.Money(localCost, sym)}";
                    btn.SetEnabled(false);
                }
                else
                {
                    string capName = name;
                    string capBranch = branch;
                    float capStrength = strengthGain;
                    float capCost = costBillions;
                    int capMonths = months;
                    btn.text = $"COMMISSION PROJECT — {UiFormat.Money(localCost, sym)}";
                    btn.clicked += () =>
                    {
                        if (GameManager.Instance == null) return;
                        if (GameManager.Instance.playerBudget < localCost)
                        {
                            NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need {UiFormat.Money(localCost, sym)} to commission {capName}.", Color.red);
                            return;
                        }
                        GameManager.Instance.playerBudget -= localCost;
                        country.treasury = GameManager.Instance.playerBudget;

                        var proj = new MilitaryProject
                        {
                            projectName    = capName,
                            branch         = capBranch,
                            strengthGain   = capStrength,
                            costBillions   = capCost,
                            monthsRemaining = capMonths,
                            totalMonths    = capMonths
                        };
                        country.militaryProjects.Add(proj);

                        NotificationManager.Instance?.AddNotification($"PROJECT COMMISSIONED: {capName} — {capMonths} months to delivery. ({UiFormat.Money(localCost, sym)} allocated)", new Color(0, 210/255f, 255/255f));
                        NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} commissions strategic project: {capName}.");
                        RefreshMilitaryData();
                    };
                }
                card.Add(btn);
                _availableProjectsList.Add(card);
            }
        }

        private void AddRecruitCard(string name, string id, int current, float costB, int amountPerClick, VisualElement grid, bool isPersonnel)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float localCost = costB * country.exchangeRate;

            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.width = Length.Percent(48);
            card.style.marginBottom = 12;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 15;
            card.style.paddingRight = 15;

            var title = new Label(name);
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 11;
            card.Add(title);

            var infoRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 5, justifyContent = Justify.SpaceBetween } };
            infoRow.Add(new Label($"Current: {current:N0}") { style = { color = Color.grey, fontSize = 9 } });
            infoRow.Add(new Label($"Cost: {UiFormat.Money(localCost, sym)} / {amountPerClick}") { style = { color = new Color(0, 230/255f, 118/255f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
            card.Add(infoRow);

            var btn = new Button(() => ShowPurchaseModal(name, id, costB, amountPerClick, isPersonnel, false, 0)) 
            { 
                text = isPersonnel ? $"RECRUIT & TRAIN..." : $"CONSTRUCT..." 
            };
            btn.AddToClassList("button-premium");
            btn.style.marginTop = 10;
            btn.style.height = 25;
            btn.style.fontSize = 9;
            card.Add(btn);

            var btnDecommission = new Button(() => DecommissionUnit(id, amountPerClick, isPersonnel))
            {
                text = isPersonnel ? $"DISCHARGE -{amountPerClick:N0}" : $"DECOMMISSION -{amountPerClick}"
            };
            btnDecommission.AddToClassList("button-premium");
            btnDecommission.style.height = 20;
            btnDecommission.style.fontSize = 8;
            btnDecommission.style.marginTop = 5;
            btnDecommission.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.5f);
            card.Add(btnDecommission);

            grid.Add(card);
        }

        private void AddWeaponCard(string name, string id, int current, float costB, int amountPerClick, VisualElement grid, CountryDataSO country)
        {
            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.width = Length.Percent(48);
            card.style.marginBottom = 12;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 15;
            card.style.paddingRight = 15;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float localCost = costB * country.exchangeRate;

            var titleRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var title = new Label(name);
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 11;
            titleRow.Add(title);
            
            var countLabel = new Label($"Current: {current:N0}");
            countLabel.style.color = Color.grey;
            countLabel.style.fontSize = 9;
            titleRow.Add(countLabel);
            card.Add(titleRow);

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 10 } };
            
            var btnImport = new Button(() => ShowPurchaseModal(name, id, costB, amountPerClick, false, false, 0)) 
            { 
                text = $"IMPORT..." 
            };
            btnImport.AddToClassList("button-premium");
            btnImport.style.width = Length.Percent(48);
            btnImport.style.height = 25;
            btnImport.style.fontSize = 8;
            btnImport.style.color = Color.white;
            
            var importCostLabel = new Label($"Cost: {UiFormat.Money(localCost, sym)} / {amountPerClick}");
            importCostLabel.style.color = new Color(241/255f, 196/255f, 15/255f);
            importCostLabel.style.fontSize = 7;
            importCostLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            importCostLabel.style.marginTop = 2;
            
            var importCol = new VisualElement() { style = { width = Length.Percent(48) } };
            importCol.Add(btnImport);
            importCol.Add(importCostLabel);
            actionRow.Add(importCol);

            float prodCost = localCost * 0.5f;
            float prodCostB = costB * 0.5f;
            float mineralsCost = costB * 10000f;
            
            var btnProduce = new Button(() => ShowPurchaseModal(name, id, prodCostB, amountPerClick, false, true, mineralsCost)) 
            { 
                text = $"PRODUCE..." 
            };
            btnProduce.AddToClassList("button-premium");
            btnProduce.style.width = Length.Percent(48);
            btnProduce.style.height = 25;
            btnProduce.style.fontSize = 8;

            var prodCostLabel = new Label($"Cost: {UiFormat.Money(prodCost, sym)} / {amountPerClick}\nMin: {mineralsCost:N0} T");
            prodCostLabel.style.color = new Color(0, 230/255f, 118/255f);
            prodCostLabel.style.fontSize = 7;
            prodCostLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            prodCostLabel.style.marginTop = 2;

            if (country.baseDefenseFactoryCount <= 0)
            {
                btnProduce.SetEnabled(false);
                btnProduce.text = "LOCKED";
                prodCostLabel.text = "Requires Def. Factory";
                prodCostLabel.style.color = new Color(255/255f, 82/255f, 82/255f);
            }

            var produceCol = new VisualElement() { style = { width = Length.Percent(48) } };
            produceCol.Add(btnProduce);
            produceCol.Add(prodCostLabel);
            actionRow.Add(produceCol);

            card.Add(actionRow);

            var btnDecommission = new Button(() => DecommissionUnit(id, amountPerClick, false))
            {
                text = $"DECOMMISSION -{amountPerClick:N0}"
            };
            btnDecommission.AddToClassList("button-premium");
            btnDecommission.style.width = Length.Percent(100);
            btnDecommission.style.height = 20;
            btnDecommission.style.fontSize = 8;
            btnDecommission.style.marginTop = 10;
            btnDecommission.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.5f);
            card.Add(btnDecommission);

            grid.Add(card);
        }

        private void ShowPurchaseModal(string name, string id, float baseCostB, int batchSize, bool isPersonnel, bool isProduce, float baseMineralsCost)
        {
            var country = GameManager.Instance.playerCountry;
            var overlay = new VisualElement() { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0, 0, 0, 0.85f), justifyContent = Justify.Center, alignItems = Align.Center } };
            var modal = new VisualElement() { style = { width = 450, paddingBottom = 20, paddingTop = 20, paddingLeft = 20, paddingRight = 20, backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f), borderTopLeftRadius=8, borderTopRightRadius=8, borderBottomLeftRadius=8, borderBottomRightRadius=8 } };
            SetBorderColor(modal.style, new Color(0, 210/255f, 255/255f, 0.5f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            string actionType = isPersonnel ? "RECRUIT" : (isProduce ? "PRODUCE" : "IMPORT");
            modal.Add(new Label($"{actionType}: {name.ToUpper()}") { style = { color = Color.white, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 15 } });

            var sliderLabel = new Label();
            sliderLabel.style.color = Color.grey;
            sliderLabel.style.fontSize = 9;
            sliderLabel.style.marginBottom = 5;
            modal.Add(sliderLabel);

            int maxBatches = 100;
            
            if (isPersonnel)
            {
                int capacity = 0;
                int currentTotal = 0;
                int inTraining = GetTotalInTraining(country, id);

                switch (id)
                {
                    case "INF":
                        capacity = 5000 + (country.baseArmyCount * 15000);
                        currentTotal = country.armyStats.personnel + inTraining;
                        break;
                    case "POLICE":
                        capacity = 5000 + (country.baseArmyCount * 5000);
                        currentTotal = country.nationalPoliceSize + inTraining;
                        break;
                    case "NAVY_PERS":
                        capacity = 1000 + (country.baseNavyCount * 3000);
                        currentTotal = country.navyStats.personnel + inTraining;
                        break;
                    case "AIR_PERS":
                        capacity = 500 + (country.baseAirForceCount * 2000);
                        currentTotal = country.airForceStats.personnel + inTraining;
                        break;
                    case "RDF_PERS":
                        capacity = 200 + (country.baseRDFCount * 1000);
                        currentTotal = country.rdfStats.personnel + inTraining;
                        break;
                }
                
                int availableSpace = Mathf.Max(0, capacity - currentTotal);
                maxBatches = Mathf.Max(1, availableSpace / batchSize);
            }

            SliderInt amountSlider = new SliderInt(1, maxBatches) { style = { marginBottom = 15 } };
            
            var totalCostLabel = new Label();
            totalCostLabel.style.color = new Color(241/255f, 196/255f, 15/255f);
            totalCostLabel.style.fontSize = 11;
            totalCostLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            totalCostLabel.style.marginBottom = 15;
            modal.Add(totalCostLabel);

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;

            amountSlider.RegisterValueChangedCallback(evt => {
                int totalUnits = evt.newValue * batchSize;
                float totalCost = evt.newValue * baseCostB;
                float totalMinerals = evt.newValue * baseMineralsCost;
                
                sliderLabel.text = $"QUANTITY: {totalUnits:N0} units ({evt.newValue} batches of {batchSize})";
                
                string costText = $"Total Cost: {UiFormat.Money(totalCost * country.exchangeRate, sym)}";
                if (isProduce && totalMinerals > 0) costText += $"  |  Minerals: {totalMinerals:N0} T";
                totalCostLabel.text = costText;
            });
            amountSlider.value = 1;
            modal.Insert(2, amountSlider);

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.width = Length.Percent(48);
            cancelBtn.style.height = 30;
            actionRow.Add(cancelBtn);

            var confirmBtn = new Button() { text = "CONFIRM ORDER" };
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.backgroundColor = new Color(0, 210/255f, 255/255f, 0.4f);
            confirmBtn.style.width = Length.Percent(48);
            confirmBtn.style.height = 30;
            confirmBtn.clicked += () => {
                float localCost = baseCostB * country.exchangeRate * amountSlider.value;
                float mineralsCost = baseMineralsCost * amountSlider.value;
                int amount = batchSize * amountSlider.value;
                
                RecruitUnit(id, localCost, amount, isPersonnel, isProduce, mineralsCost);
                overlay.parent?.Remove(overlay);
            };
            actionRow.Add(confirmBtn);
            modal.Add(actionRow);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null) dashboard.Add(overlay);
            else _root?.Add(overlay);
        }

        private void RecruitUnit(string id, float cost, int amount, bool isPersonnel, bool isProduce, float mineralsCost)
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            if (isPersonnel)
            {
                int capacity = 0;
                int currentTotal = 0;
                int inTraining = GetTotalInTraining(country, id);
                string baseTypeNeeded = "";

                switch (id)
                {
                    case "INF":
                        capacity = 5000 + (country.baseArmyCount * 15000);
                        currentTotal = country.armyStats.personnel + inTraining;
                        baseTypeNeeded = "Military Base (Army)";
                        break;
                    case "POLICE":
                        capacity = 5000 + (country.baseArmyCount * 5000);
                        currentTotal = country.nationalPoliceSize + inTraining;
                        baseTypeNeeded = "Military Base (Army)";
                        break;
                    case "NAVY_PERS":
                        capacity = 1000 + (country.baseNavyCount * 3000);
                        currentTotal = country.navyStats.personnel + inTraining;
                        baseTypeNeeded = "Naval Base (Navy)";
                        break;
                    case "AIR_PERS":
                        capacity = 500 + (country.baseAirForceCount * 2000);
                        currentTotal = country.airForceStats.personnel + inTraining;
                        baseTypeNeeded = "Strategic Airbase";
                        break;
                    case "RDF_PERS":
                        capacity = 200 + (country.baseRDFCount * 1000);
                        currentTotal = country.rdfStats.personnel + inTraining;
                        baseTypeNeeded = "RDF Command Center";
                        break;
                }

                if (currentTotal + amount > capacity)
                {
                    NotificationManager.Instance?.AddNotification($"CAPACITY FULL: Build more {baseTypeNeeded}s to house these recruits! (Cap: {capacity:N0})", Color.red);
                    return;
                }
            }

            if (isProduce && country.invMineralsTonnes < mineralsCost)
            {
                NotificationManager.Instance?.AddNotification($"LACK OF RAW MATERIALS: Need {mineralsCost:N0} Tonnes of Minerals to produce locally!", Color.red);
                return;
            }

            if (gm.playerBudget >= cost)
            {
                if (isProduce)
                {
                    country.invMineralsTonnes -= mineralsCost;
                }
                
                gm.playerBudget -= cost;
                country.treasury = gm.playerBudget;
                
                string tag = isPersonnel ? "TRAINING" : (isProduce ? "MANUFACTURE" : "IMPORT");
                string unitLabel = "";

                switch (id)
                {
                    case "INF": unitLabel = "Ground Troops"; break;
                    case "POLICE": unitLabel = "Police Officers"; break;
                    case "NAVY_PERS": unitLabel = "Naval Crews"; break;
                    case "AIR_PERS": unitLabel = "Air Crews"; break;
                    case "RDF_PERS": unitLabel = "Spec-Ops Forces"; break;
                    
                    case "RIFLE": unitLabel = "Service Rifles"; break;
                    case "ARTILLERY": unitLabel = "Artillery Batteries"; break;
                    case "TNK": unitLabel = "Main Battle Tanks"; break;
                    case "APC": unitLabel = "APCs"; break;
                    case "UAV": unitLabel = "Tactical UAVs"; break;
                    case "FTR": unitLabel = "Fighter Jets"; break;
                    case "BOMBER": unitLabel = "Strategic Bombers"; break;
                    case "HEL": unitLabel = "Attack Helis"; break;
                    case "PATROL_BOAT": unitLabel = "Patrol Boats"; break;
                    case "FRG": unitLabel = "Coastal Frigates"; break;
                    case "SUB": unitLabel = "Attack Submarines"; break;
                    case "DESTROYER": unitLabel = "Destroyers"; break;
                    
                    case "BASE_ARMY": unitLabel = "Military Bases"; break;
                    case "BASE_NAVY": unitLabel = "Naval Bases"; break;
                    case "BASE_AIR": unitLabel = "Strategic Airbases"; break;
                    case "BASE_RDF": unitLabel = "RDF Command Centers"; break;
                    case "BASE_DEF_FAC": unitLabel = "Defense Factories"; break;
                }

                if (isPersonnel)
                {
                    country.trainingPipelines.Add(new RecruitmentPipeline(id, unitLabel, amount, true));
                    
                    country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 0.5f);
                    
                    NotificationManager.Instance?.AddNotification($"TRAINING STARTED: {amount:N0} {unitLabel} entered the 6-month training academy ({sym}{cost:N2}B allocated).", Color.yellow);
                    NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} entered {amount:N0} {unitLabel} recruits into the 6-month instruction program.");
                }
                else
                {
                    switch (id)
                    {
                        case "RIFLE": 
                            country.unitRifles += amount; 
                            country.armyStats.equipment += amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 0.3f);
                            break;
                        case "ARTILLERY": 
                            country.unitArtillery += amount; 
                            country.armyStats.equipment += amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 1.0f);
                            break;
                        case "TNK": 
                            country.unitTanks += amount; 
                            country.armyStats.equipment += amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 1.2f);
                            break;
                        case "APC": 
                            country.unitAPCs += amount; 
                            country.armyStats.equipment += amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 0.8f);
                            break;
                        case "UAV": 
                            country.unitUAVs += amount; 
                            country.airForceStats.equipment += amount;
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 0.6f);
                            break;
                        case "FTR": 
                            country.unitFighters += amount; 
                            country.airForceStats.equipment += amount;
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 2.0f);
                            break;
                        case "BOMBER": 
                            country.unitBombers += amount; 
                            country.airForceStats.equipment += amount;
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 3.0f);
                            break;
                        case "HEL": 
                            country.unitAttackHelis += amount; 
                            country.airForceStats.equipment += amount;
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 1.0f);
                            break;
                        case "PATROL_BOAT": 
                            country.unitPatrolBoats += amount; 
                            country.navyStats.equipment += amount;
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 0.5f);
                            break;
                        case "FRG": 
                            country.unitFrigates += amount; 
                            country.navyStats.equipment += amount;
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 3.0f);
                            break;
                        case "SUB": 
                            country.unitSubmarines += amount; 
                            country.navyStats.equipment += amount;
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 4.5f);
                            break;
                        case "DESTROYER": 
                            country.unitDestroyers += amount; 
                            country.navyStats.equipment += amount;
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 5.5f);
                            break;
                        case "BASE_ARMY":
                            country.baseArmyCount += amount;
                            country.armyStats.readiness = Mathf.Min(100f, country.armyStats.readiness + 5.0f);
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 1.5f);
                            break;
                        case "BASE_NAVY":
                            country.baseNavyCount += amount;
                            country.navyStats.readiness = Mathf.Min(100f, country.navyStats.readiness + 8.0f);
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 2.5f);
                            break;
                        case "BASE_AIR":
                            country.baseAirForceCount += amount;
                            country.airForceStats.readiness = Mathf.Min(100f, country.airForceStats.readiness + 6.0f);
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 2.0f);
                            break;
                        case "BASE_RDF":
                            country.baseRDFCount += amount;
                            country.rdfStats.readiness = Mathf.Min(100f, country.rdfStats.readiness + 10.0f);
                            country.rdfReadiness = Mathf.Min(100f, country.rdfReadiness + 10.0f);
                            break;
                        case "BASE_DEF_FAC":
                            country.baseDefenseFactoryCount += amount;
                            break;
                    }

                    country.militaryStrength = (country.armyStrength * 0.5f) + (country.navyStrength * 0.2f) + (country.airForceStrength * 0.3f);
                    country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 1.0f);

                    NotificationManager.Instance?.AddNotification($"{tag}: {amount:N0} {unitLabel} added (Cost: {UiFormat.Money(cost, sym)}).", Color.cyan);
                    NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} completed {tag.ToLower()} of {amount:N0} {unitLabel}.");
                }

                RefreshMilitaryData();
            }
            else
            {
                NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need {UiFormat.Money(cost, sym)} to execute this transaction.", Color.red);
            }
        }

        private void DecommissionUnit(string id, int amountToRemove, bool isPersonnel)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;
            
            int amountRemoved = 0;
            string unitLabel = "";

            switch (id)
            {
                case "INF": 
                    amountRemoved = Mathf.Min(country.armyStats.personnel, amountToRemove);
                    country.armyStats.personnel -= amountRemoved;
                    country.unitInfantry -= amountRemoved;
                    unitLabel = "Ground Troops";
                    break;
                case "POLICE": 
                    amountRemoved = Mathf.Min(country.nationalPoliceSize, amountToRemove);
                    country.nationalPoliceSize -= amountRemoved;
                    unitLabel = "Police Officers";
                    break;
                case "NAVY_PERS": 
                    amountRemoved = Mathf.Min(country.navyStats.personnel, amountToRemove);
                    country.navyStats.personnel -= amountRemoved;
                    unitLabel = "Naval Crews";
                    break;
                case "AIR_PERS": 
                    amountRemoved = Mathf.Min(country.airForceStats.personnel, amountToRemove);
                    country.airForceStats.personnel -= amountRemoved;
                    unitLabel = "Air Crews";
                    break;
                case "RDF_PERS": 
                    amountRemoved = Mathf.Min(country.rdfStats.personnel, amountToRemove);
                    country.rdfStats.personnel -= amountRemoved;
                    unitLabel = "Spec-Ops Forces";
                    break;
                
                case "RIFLE": 
                    amountRemoved = Mathf.Min(country.unitRifles, amountToRemove);
                    country.unitRifles -= amountRemoved; 
                    country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amountRemoved);
                    unitLabel = "Service Rifles"; 
                    break;
                case "ARTILLERY": 
                    amountRemoved = Mathf.Min(country.unitArtillery, amountToRemove);
                    country.unitArtillery -= amountRemoved; 
                    country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amountRemoved);
                    unitLabel = "Artillery Batteries"; 
                    break;
                case "TNK": 
                    amountRemoved = Mathf.Min(country.unitTanks, amountToRemove);
                    country.unitTanks -= amountRemoved; 
                    country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amountRemoved);
                    unitLabel = "Main Battle Tanks"; 
                    break;
                case "APC": 
                    amountRemoved = Mathf.Min(country.unitAPCs, amountToRemove);
                    country.unitAPCs -= amountRemoved; 
                    country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amountRemoved);
                    unitLabel = "APCs"; 
                    break;
                case "UAV": 
                    amountRemoved = Mathf.Min(country.unitUAVs, amountToRemove);
                    country.unitUAVs -= amountRemoved; 
                    country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amountRemoved);
                    unitLabel = "Tactical UAVs"; 
                    break;
                case "FTR": 
                    amountRemoved = Mathf.Min(country.unitFighters, amountToRemove);
                    country.unitFighters -= amountRemoved; 
                    country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amountRemoved);
                    unitLabel = "Fighter Jets"; 
                    break;
                case "BOMBER": 
                    amountRemoved = Mathf.Min(country.unitBombers, amountToRemove);
                    country.unitBombers -= amountRemoved; 
                    country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amountRemoved);
                    unitLabel = "Strategic Bombers"; 
                    break;
                case "HEL": 
                    amountRemoved = Mathf.Min(country.unitAttackHelis, amountToRemove);
                    country.unitAttackHelis -= amountRemoved; 
                    country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amountRemoved);
                    unitLabel = "Attack Helis"; 
                    break;
                case "PATROL_BOAT": 
                    amountRemoved = Mathf.Min(country.unitPatrolBoats, amountToRemove);
                    country.unitPatrolBoats -= amountRemoved; 
                    country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amountRemoved);
                    unitLabel = "Patrol Boats"; 
                    break;
                case "FRG": 
                    amountRemoved = Mathf.Min(country.unitFrigates, amountToRemove);
                    country.unitFrigates -= amountRemoved; 
                    country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amountRemoved);
                    unitLabel = "Coastal Frigates"; 
                    break;
                case "SUB": 
                    amountRemoved = Mathf.Min(country.unitSubmarines, amountToRemove);
                    country.unitSubmarines -= amountRemoved; 
                    country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amountRemoved);
                    unitLabel = "Attack Submarines"; 
                    break;
                case "DESTROYER": 
                    amountRemoved = Mathf.Min(country.unitDestroyers, amountToRemove);
                    country.unitDestroyers -= amountRemoved; 
                    country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amountRemoved);
                    unitLabel = "Destroyers"; 
                    break;
                case "BASE_ARMY":
                    amountRemoved = Mathf.Min(country.baseArmyCount, amountToRemove);
                    country.baseArmyCount -= amountRemoved;
                    unitLabel = "Military Bases";
                    break;
                case "BASE_NAVY":
                    amountRemoved = Mathf.Min(country.baseNavyCount, amountToRemove);
                    country.baseNavyCount -= amountRemoved;
                    unitLabel = "Naval Bases";
                    break;
                case "BASE_AIR":
                    amountRemoved = Mathf.Min(country.baseAirForceCount, amountToRemove);
                    country.baseAirForceCount -= amountRemoved;
                    unitLabel = "Strategic Airbases";
                    break;
                case "BASE_RDF":
                    amountRemoved = Mathf.Min(country.baseRDFCount, amountToRemove);
                    country.baseRDFCount -= amountRemoved;
                    unitLabel = "RDF Command Centers";
                    break;
                case "BASE_DEF_FAC":
                    amountRemoved = Mathf.Min(country.baseDefenseFactoryCount, amountToRemove);
                    country.baseDefenseFactoryCount -= amountRemoved;
                    unitLabel = "Defense Factories";
                    break;
            }

            if (amountRemoved > 0)
            {
                country.militaryStrength = (country.armyStrength * 0.5f) + (country.navyStrength * 0.2f) + (country.airForceStrength * 0.3f);
                
                NotificationManager.Instance?.AddNotification($"DECOMMISSIONED: {amountRemoved:N0} {unitLabel} removed from active service.", new Color(255/255f, 82/255f, 82/255f));
                NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} decommissioned {amountRemoved:N0} {unitLabel} to reduce military expenditures.");
                RefreshMilitaryData();
            }
            else
            {
                NotificationManager.Instance?.AddNotification($"NO UNITS: Cannot decommission {unitLabel} - none in active service.", Color.red);
            }
        }

        private void SetBorderWidth(IStyle style, float width)
        {
            style.borderTopWidth = width;
            style.borderBottomWidth = width;
            style.borderLeftWidth = width;
            style.borderRightWidth = width;
        }

        private void SetBorderColor(IStyle style, Color color)
        {
            style.borderTopColor = color;
            style.borderBottomColor = color;
            style.borderLeftColor = color;
            style.borderRightColor = color;
        }

        private void ShowLaunchOpModal()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            var deterioratedRegions = country.regions.FindAll(r => r.security < 70f);
            if (deterioratedRegions.Count == 0)
            {
                NotificationManager.Instance?.AddNotification("THEATRE CALM: No regions require operations (Security is stable above 70%).", Color.yellow);
                return;
            }

            var campRegions = country.regions.FindAll(r => r.insurgentCampDetected && !r.campStrikeOrdered);
            if (campRegions.Count > 0 && country.unitBombers < 1 && country.unitFighters < 5)
            {
                NotificationManager.Instance?.AddNotification(
                    $"⚠ CAMP DETECTED but no bombers or fighters available! Go to RECRUIT tab and train air assets first.",
                    new Color(1f, 0.5f, 0f));
            }

            var overlay = new VisualElement();
            overlay.name = "OpModalOverlay";
            overlay.style.position = Position.Absolute;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.backgroundColor = new Color(8/255f, 12/255f, 22/255f, 0.96f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            var modal = new VisualElement();
            modal.AddToClassList("sub-panel");
            modal.style.width = 440;
            modal.style.maxHeight = Length.Percent(85);
            modal.style.flexDirection = FlexDirection.Column;
            modal.style.paddingTop = 15;
            modal.style.paddingBottom = 15;
            modal.style.paddingLeft = 20;
            modal.style.paddingRight = 20;
            
            SetBorderWidth(modal.style, 1);
            SetBorderColor(modal.style, new Color(255/255f, 82/255f, 82/255f, 0.4f));
            overlay.Add(modal);

            var title = new Label("LAUNCH MILITARY OPERATION");
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 13;
            title.style.marginBottom = 15;
            modal.Add(title);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.marginBottom = 12;
            modal.Add(scroll);

            scroll.Add(new Label("OPERATION CODENAME") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
            var nameInput = new TextField() { value = $"OPERATION {deterioratedRegions[0].regionName.ToUpper().Replace(" ", "")} SHIELD" };
            nameInput.style.marginBottom = 12;
            nameInput.style.height = 32;
            scroll.Add(nameInput);

            var inputElement = nameInput.Q<VisualElement>(className: "unity-text-field__input") ?? nameInput.Q<VisualElement>("unity-text-input");
            if (inputElement != null)
            {
                inputElement.style.color = Color.white;
                inputElement.style.backgroundColor = new Color(0.02f, 0.02f, 0.02f, 0.95f);
                inputElement.style.fontSize = 14;
                inputElement.style.unityFontStyleAndWeight = FontStyle.Bold;
                inputElement.style.paddingLeft = 8;
                inputElement.style.paddingRight = 8;
                SetBorderWidth(inputElement.style, 1);
                SetBorderColor(inputElement.style, new Color(0, 210/255f, 255/255f, 0.3f));
            }

            scroll.Add(new Label("TARGET THEATRE (DOMESTIC REGIONS OR ENEMY FRONTLINES)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
            var regionSelectContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 12 } };
            
            List<string> availableTheaters = new List<string>();
            foreach (var r in deterioratedRegions) availableTheaters.Add(r.regionName);

            var gmWarSys = GameManager.Instance.WarSystem;
            if (gmWarSys != null && GameManager.Instance.allCountries != null)
            {
                foreach(var c in GameManager.Instance.allCountries)
                {
                    if (c != GameManager.Instance.playerCountry && gmWarSys.GetWarStage(c) == StateCraft.Interfaces.WarStage.Conflict)
                    {
                        availableTheaters.Add(c.countryName);
                    }
                }
            }

            var detectedCamps = country.regions.FindAll(r => r.insurgentCampDetected && !r.campStrikeOrdered);
            foreach (var campRegion in detectedCamps)
            {
                string campEntry = $"🎯 CAMP: {campRegion.regionName}";
                if (!availableTheaters.Contains(campEntry))
                    availableTheaters.Insert(0, campEntry);
            }

            if (availableTheaters.Count == 0) availableTheaters.Add("HOME DEFENSE");
            string selectedRegion = availableTheaters[0];

            var regionButtons = new List<Button>();
            foreach (var tLabel in availableTheaters)
            {
                var btn = new Button() { text = tLabel };
                btn.style.fontSize = 9;
                btn.style.marginRight = 5;
                btn.style.marginBottom = 5;
                btn.style.height = 25;

                if (tLabel == selectedRegion)
                {
                    btn.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.4f);
                    SetBorderColor(btn.style, new Color(255/255f, 82/255f, 82/255f));
                }
                else
                {
                    btn.style.backgroundColor = new Color(0, 0, 0, 0.3f);
                    SetBorderColor(btn.style, new Color(255/255f, 255/255f, 255/255f, 0.1f));
                }

                string currentRegName = tLabel;
                btn.clicked += () =>
                {
                    selectedRegion = currentRegName;
                    nameInput.value = $"OPERATION {currentRegName.ToUpper().Replace(" ", "")} SHIELD";
                    foreach (var otherBtn in regionButtons)
                    {
                        if (otherBtn.text.StartsWith(currentRegName))
                        {
                            otherBtn.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.4f);
                            SetBorderColor(otherBtn.style, new Color(255/255f, 82/255f, 82/255f));
                        }
                        else
                        {
                            otherBtn.style.backgroundColor = new Color(0, 0, 0, 0.3f);
                            SetBorderColor(otherBtn.style, new Color(255/255f, 255/255f, 255/255f, 0.1f));
                        }
                    }
                };

                regionButtons.Add(btn);
                regionSelectContainer.Add(btn);
            }
            scroll.Add(regionSelectContainer);

            scroll.Add(new Label("OPERATION OBJECTIVE") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } });
            var typeSelectContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginBottom = 15 } };
            string selectedType = "COIN";
            string[] opTypes = { "COIN", "SPECIAL OPS", "AERIAL STRIKE", "NAVAL BLOCKADE", "Offensive", "Defensive", "STRATEGIC BOMBARDMENT" };
            typeSelectContainer.style.flexWrap = Wrap.Wrap;

            System.Action UpdateSliderVisibility = null;

            var typeButtons = new List<Button>();
            foreach (var t in opTypes)
            {
                var btn = new Button() { text = t.ToUpper() };
                btn.style.fontSize = 8;
                btn.style.flexGrow = 1;
                btn.style.height = 25;
                btn.style.marginRight = 3;

                if (t == selectedType)
                {
                    btn.style.backgroundColor = new Color(0, 210/255f, 255/255f, 0.4f);
                    SetBorderColor(btn.style, new Color(0, 210/255f, 255/255f));
                }
                else
                {
                    btn.style.backgroundColor = new Color(0, 0, 0, 0.3f);
                    SetBorderColor(btn.style, new Color(255/255f, 255/255f, 255/255f, 0.1f));
                }

                string currentType = t;
                btn.clicked += () =>
                {
                    selectedType = currentType;
                    foreach (var otherBtn in typeButtons)
                    {
                        if (otherBtn.text == currentType.ToUpper())
                        {
                            otherBtn.style.backgroundColor = new Color(0, 210/255f, 255/255f, 0.4f);
                            SetBorderColor(otherBtn.style, new Color(0, 210/255f, 255/255f));
                        }
                        else
                        {
                            otherBtn.style.backgroundColor = new Color(0, 0, 0, 0.3f);
                            SetBorderColor(otherBtn.style, new Color(255/255f, 255/255f, 255/255f, 0.1f));
                        }
                    }
                    if (UpdateSliderVisibility != null) UpdateSliderVisibility();
                };

                typeButtons.Add(btn);
                typeSelectContainer.Add(btn);
            }
            scroll.Add(typeSelectContainer);

            var troopsLabel = new Label($"DEPLOY INFANTRY: 2,500 Troops (Available: {country.unitInfantry:N0})");
            troopsLabel.style.color = Color.grey;
            troopsLabel.style.fontSize = 9;
            troopsLabel.style.marginBottom = 4;
            scroll.Add(troopsLabel);

            var maxInfantry = Mathf.Max(1000, country.unitInfantry);
            var troopsSlider = new SliderInt(1000, Mathf.Min(maxInfantry, 10000)) { value = Mathf.Min(maxInfantry, 2500) };
            troopsSlider.style.marginBottom = 10;
            troopsSlider.lowValue = 1000;
            troopsSlider.highValue = Mathf.Min(maxInfantry, 10000);
            scroll.Add(troopsSlider);

            var tanksLabel = new Label($"DEPLOY TANKS: 0 Tanks (Available: {country.unitTanks:N0})");
            tanksLabel.style.color = Color.grey;
            tanksLabel.style.fontSize = 9;
            tanksLabel.style.marginBottom = 4;
            scroll.Add(tanksLabel);

            var maxTanks = Mathf.Max(0, country.unitTanks);
            var tanksSlider = new SliderInt(0, Mathf.Min(maxTanks, 50)) { value = 0 };
            tanksSlider.style.marginBottom = 10;
            tanksSlider.lowValue = 0;
            tanksSlider.highValue = Mathf.Min(maxTanks, 50);
            scroll.Add(tanksSlider);

            var apcsLabel = new Label($"DEPLOY APCs: 0 APCs (Available: {country.unitAPCs:N0})");
            apcsLabel.style.color = Color.grey;
            apcsLabel.style.fontSize = 9;
            apcsLabel.style.marginBottom = 4;
            scroll.Add(apcsLabel);

            var maxAPCs = Mathf.Max(0, country.unitAPCs);
            var apcsSlider = new SliderInt(0, Mathf.Min(maxAPCs, 100)) { value = 0 };
            apcsSlider.style.marginBottom = 10;
            apcsSlider.lowValue = 0;
            apcsSlider.highValue = Mathf.Min(maxAPCs, 100);
            scroll.Add(apcsSlider);

var fightersLabel = new Label($"DEPLOY FIGHTER JETS: 0 Jets (Available: {country.unitFighters:N0})");
            fightersLabel.style.color = Color.grey;
            fightersLabel.style.fontSize = 9;
            fightersLabel.style.marginBottom = 4;
            scroll.Add(fightersLabel);

            var maxFighters = Mathf.Max(0, country.unitFighters);
            var fightersSlider = new SliderInt(0, Mathf.Min(maxFighters, 10)) { value = 0 };
            fightersSlider.style.marginBottom = 10;
            fightersSlider.lowValue = 0;
            fightersSlider.highValue = Mathf.Min(maxFighters, 10);
            scroll.Add(fightersSlider);

            var helisLabel = new Label($"DEPLOY HELICOPTERS: 0 Helicopters (Available: {country.unitAttackHelis:N0})");
            helisLabel.style.color = Color.grey;
            helisLabel.style.fontSize = 9;
            helisLabel.style.marginBottom = 4;
            scroll.Add(helisLabel);

            var maxHelis = Mathf.Max(0, country.unitAttackHelis);
            var helisSlider = new SliderInt(0, Mathf.Min(maxHelis, 15)) { value = 0 };
            helisSlider.style.marginBottom = 10;
            helisSlider.lowValue = 0;
            helisSlider.highValue = Mathf.Min(maxHelis, 15);
            scroll.Add(helisSlider);

            var frigatesLabel = new Label($"DEPLOY FRIGATES: 0 Frigates (Available: {country.unitFrigates:N0})");
            frigatesLabel.style.color = Color.grey;
            frigatesLabel.style.fontSize = 9;
            frigatesLabel.style.marginBottom = 4;
            scroll.Add(frigatesLabel);

            var maxFrigates = Mathf.Max(0, country.unitFrigates);
            var frigatesSlider = new SliderInt(0, Mathf.Min(maxFrigates, 5)) { value = 0 };
            frigatesSlider.style.marginBottom = 10;
            frigatesSlider.lowValue = 0;
            frigatesSlider.highValue = Mathf.Min(maxFrigates, 5);
            scroll.Add(frigatesSlider);

            var submarinesLabel = new Label($"DEPLOY SUBMARINES: 0 Submarines (Available: {country.unitSubmarines:N0})");
            submarinesLabel.style.color = Color.grey;
            submarinesLabel.style.fontSize = 9;
            submarinesLabel.style.marginBottom = 4;
            scroll.Add(submarinesLabel);

            var maxSubmarines = Mathf.Max(0, country.unitSubmarines);
            var submarinesSlider = new SliderInt(0, Mathf.Min(maxSubmarines, 5)) { value = 0 };
            submarinesSlider.style.marginBottom = 10;
            submarinesSlider.lowValue = 0;
            submarinesSlider.highValue = Mathf.Min(maxSubmarines, 5);
            scroll.Add(submarinesSlider);

            var rdfLabel = new Label($"DEPLOY RDF SPEC-OPS: 0 RDF (Available: {country.rdfStats.personnel:N0})");
            rdfLabel.style.color = Color.grey;
            rdfLabel.style.fontSize = 9;
            rdfLabel.style.marginBottom = 4;
            scroll.Add(rdfLabel);

            var maxRDF = Mathf.Max(0, country.rdfStats.personnel);
            var rdfSlider = new SliderInt(0, Mathf.Min(maxRDF, 5000)) { value = 0 };
            rdfSlider.style.marginBottom = 10;
            rdfSlider.lowValue = 0;
            rdfSlider.highValue = Mathf.Min(maxRDF, 5000);
            scroll.Add(rdfSlider);

            var bombersLabel = new Label($"DEPLOY STRATEGIC BOMBERS: 0 Bombers (Available: {country.unitBombers:N0})");
            bombersLabel.style.color = Color.grey;
            bombersLabel.style.fontSize = 9;
            bombersLabel.style.marginBottom = 4;
            scroll.Add(bombersLabel);

            var maxBombers = Mathf.Max(0, country.unitBombers);
            var bombersSlider = new SliderInt(0, Mathf.Min(maxBombers, 20)) { value = 0 };
            bombersSlider.style.marginBottom = 10;
            bombersSlider.lowValue = 0;
            bombersSlider.highValue = Mathf.Min(maxBombers, 20);
            scroll.Add(bombersSlider);

            var uavsLabel = new Label($"DEPLOY TACTICAL UAVs: 0 UAVs (Available: {country.unitUAVs:N0})");
            uavsLabel.style.color = Color.grey;
            uavsLabel.style.fontSize = 9;
            uavsLabel.style.marginBottom = 4;
            scroll.Add(uavsLabel);

            var maxUAVs = Mathf.Max(0, country.unitUAVs);
            var uavsSlider = new SliderInt(0, Mathf.Min(maxUAVs, 100)) { value = 0 };
            uavsSlider.style.marginBottom = 10;
            uavsSlider.lowValue = 0;
            uavsSlider.highValue = Mathf.Min(maxUAVs, 100);
            scroll.Add(uavsSlider);

            var costLabel = new Label();
            costLabel.style.color = new Color(0, 230/255f, 118/255f);
            costLabel.style.fontSize = 9;
            costLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            costLabel.style.marginBottom = 15;
            scroll.Add(costLabel);

            UpdateSliderVisibility = () => 
            {
                bool showTroops = true, showTanks = true, showApcs = true, showFighters = true;
                bool showHelis = true, showFrigates = true, showSubs = true, showRdf = true;
                bool showBombers = true, showUavs = true;

                switch (selectedType)
                {
                    case "COIN":
                        showTroops=true; showApcs=true; showHelis=true; showUavs=true; showRdf=true;
                        showTanks=false; showFighters=false; showFrigates=false; showSubs=false; showBombers=false;
                        break;
                    case "SPECIAL OPS":
                        showHelis=true; showUavs=true; showSubs=true; showRdf=true;
                        showTroops=false; showApcs=false; showTanks=false; showFighters=false; showFrigates=false; showBombers=false;
                        break;
                    case "AERIAL STRIKE":
                        showUavs=true; showFighters=true; showBombers=true;
                        showTroops=false; showApcs=false; showHelis=false; showTanks=false; showFrigates=false; showSubs=false; showRdf=false;
                        break;
                    case "NAVAL BLOCKADE":
                        showUavs=true; showFighters=true; showFrigates=true; showSubs=true;
                        showTroops=false; showApcs=false; showHelis=false; showTanks=false; showRdf=false; showBombers=false;
                        break;
                    case "STRATEGIC BOMBARDMENT":
                        showUavs=true; showBombers=true;
                        showTroops=false; showApcs=false; showHelis=false; showTanks=false; showFighters=false; showFrigates=false; showSubs=false; showRdf=false;
                        break;
                    case "Offensive":
                    case "Defensive":
                        showTroops=true; showApcs=true; showHelis=true; showUavs=true;
                        showTanks=true; showFighters=true; showFrigates=true; showSubs=true; showRdf=true; showBombers=true;
                        break;
                }

                troopsLabel.style.display = showTroops ? DisplayStyle.Flex : DisplayStyle.None;
                troopsSlider.style.display = showTroops ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showTroops) troopsSlider.value = troopsSlider.lowValue;

                tanksLabel.style.display = showTanks ? DisplayStyle.Flex : DisplayStyle.None;
                tanksSlider.style.display = showTanks ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showTanks) tanksSlider.value = tanksSlider.lowValue;

                apcsLabel.style.display = showApcs ? DisplayStyle.Flex : DisplayStyle.None;
                apcsSlider.style.display = showApcs ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showApcs) apcsSlider.value = apcsSlider.lowValue;

                fightersLabel.style.display = showFighters ? DisplayStyle.Flex : DisplayStyle.None;
                fightersSlider.style.display = showFighters ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showFighters) fightersSlider.value = fightersSlider.lowValue;

                helisLabel.style.display = showHelis ? DisplayStyle.Flex : DisplayStyle.None;
                helisSlider.style.display = showHelis ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showHelis) helisSlider.value = helisSlider.lowValue;

                frigatesLabel.style.display = showFrigates ? DisplayStyle.Flex : DisplayStyle.None;
                frigatesSlider.style.display = showFrigates ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showFrigates) frigatesSlider.value = frigatesSlider.lowValue;

                submarinesLabel.style.display = showSubs ? DisplayStyle.Flex : DisplayStyle.None;
                submarinesSlider.style.display = showSubs ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showSubs) submarinesSlider.value = submarinesSlider.lowValue;

                rdfLabel.style.display = showRdf ? DisplayStyle.Flex : DisplayStyle.None;
                rdfSlider.style.display = showRdf ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showRdf) rdfSlider.value = rdfSlider.lowValue;

                bombersLabel.style.display = showBombers ? DisplayStyle.Flex : DisplayStyle.None;
                bombersSlider.style.display = showBombers ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showBombers) bombersSlider.value = bombersSlider.lowValue;

                uavsLabel.style.display = showUavs ? DisplayStyle.Flex : DisplayStyle.None;
                uavsSlider.style.display = showUavs ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showUavs) uavsSlider.value = uavsSlider.lowValue;
            };
            UpdateSliderVisibility();

            System.Action UpdateEstimation = () =>
            {
                int tVal = troopsSlider.value;
                int tankVal = tanksSlider.value;
                int apcVal = apcsSlider.value;
                int ftrVal = fightersSlider.value;
                int hVal = helisSlider.value;
                int frigVal = frigatesSlider.value;
                int subVal = submarinesSlider.value;
                int rdfVal = rdfSlider.value;
                int bombVal = bombersSlider.value;
                int uavVal = uavsSlider.value;
                
                troopsLabel.text = $"DEPLOY INFANTRY: {tVal:N0} Troops (Available: {country.unitInfantry:N0})";
                tanksLabel.text = $"DEPLOY TANKS: {tankVal:N0} Tanks (Available: {country.unitTanks:N0})";
                apcsLabel.text = $"DEPLOY APCs: {apcVal:N0} APCs (Available: {country.unitAPCs:N0})";
                fightersLabel.text = $"DEPLOY FIGHTER JETS: {ftrVal:N0} Jets (Available: {country.unitFighters:N0})";
                helisLabel.text = $"DEPLOY HELICOPTERS: {hVal:N0} Helicopters (Available: {country.unitAttackHelis:N0})";
                frigatesLabel.text = $"DEPLOY FRIGATES: {frigVal:N0} Frigates (Available: {country.unitFrigates:N0})";
                submarinesLabel.text = $"DEPLOY SUBMARINES: {subVal:N0} Submarines (Available: {country.unitSubmarines:N0})";
                rdfLabel.text = $"DEPLOY RDF SPEC-OPS: {rdfVal:N0} RDF (Available: {country.rdfStats.personnel:N0})";
                bombersLabel.text = $"DEPLOY STRATEGIC BOMBERS: {bombVal:N0} Bombers (Available: {country.unitBombers:N0})";
                uavsLabel.text = $"DEPLOY TACTICAL UAVs: {uavVal:N0} UAVs (Available: {country.unitUAVs:N0})";

float monthlyCost = 0.01f +
                                    (tVal / 1000f) * 0.01f +
                                    tankVal * 0.005f +
                                    apcVal * 0.002f +
                                    ftrVal * 0.015f +
                                    hVal * 0.008f +
                                    frigVal * 0.04f +
                                    subVal * 0.06f +
                                    (rdfVal / 1000f) * 0.02f +
                                    bombVal * 0.025f +
                                    uavVal * 0.005f;

                float securityBoost = 1.0f +
                                      (tVal / 1000f) * 0.5f +
                                      tankVal * 0.8f +
                                      apcVal * 0.4f +
                                      ftrVal * 2.0f +
                                      hVal * 1.0f +
                                      frigVal * 1.5f +
                                      subVal * 2.0f +
                                      (rdfVal / 1000f) * 3.5f +
                                      bombVal * 3.0f +
                                      uavVal * 1.2f;

                float baseChance = Mathf.Clamp(50f + securityBoost * 1.5f, 50f, 95f);
                int durationMonths = Mathf.Max(1, Mathf.RoundToInt(3f + (10f / Mathf.Max(1f, securityBoost * 0.3f))));

                float localCost = monthlyCost * country.exchangeRate;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

                var impactCostVal = scroll.Q<Label>("impactCostVal");
                if (impactCostVal != null) impactCostVal.text = $"{UiFormat.Money(localCost, sym)} / Month";
                
                var impactDurationVal = scroll.Q<Label>("impactDurationVal");
                if (impactDurationVal != null) impactDurationVal.text = $"~{durationMonths} Months";
                
                var impactChanceVal = scroll.Q<Label>("impactChanceVal");
                if (impactChanceVal != null)
                {
                    impactChanceVal.text = $"{baseChance:F0}%";
                    impactChanceVal.style.color = new StyleColor(baseChance >= 75f
                        ? new Color(0, 0.9f, 0.47f)
                        : baseChance >= 55f
                            ? new Color(1f, 0.76f, 0.03f)
                            : new Color(1f, 0.32f, 0.32f));
                }

                var impactEffectsRow = scroll.Q<VisualElement>("impactEffectsRow");
                if (impactEffectsRow != null)
                {
                    impactEffectsRow.Clear();
                    impactEffectsRow.Add(MakeEffectTag("+Security", new Color(0, 0.9f, 0.47f)));
                    impactEffectsRow.Add(MakeEffectTag($"-{UiFormat.Money(localCost, sym)}/turn", new Color(1f, 0.55f, 0f)));
                    if (tVal > 0 || rdfVal > 0) impactEffectsRow.Add(MakeEffectTag("+Mil.XP", new Color(0, 0.82f, 1f)));
                    if (bombVal > 0 || ftrVal > 0) impactEffectsRow.Add(MakeEffectTag("-Civilian Relations", new Color(1f, 0.32f, 0.32f)));
                }
            };

            var impactPanel = new VisualElement();
            impactPanel.style.backgroundColor = new StyleColor(new Color(0.02f, 0.08f, 0.14f, 0.95f));
            impactPanel.style.borderTopWidth = impactPanel.style.borderBottomWidth =
                impactPanel.style.borderLeftWidth = impactPanel.style.borderRightWidth = 1;
            SetBorderColor(impactPanel.style, new Color(0, 0.82f, 1f, 0.25f));
            impactPanel.style.borderTopLeftRadius = impactPanel.style.borderTopRightRadius = 
                impactPanel.style.borderBottomLeftRadius = impactPanel.style.borderBottomRightRadius = 4;
            impactPanel.style.paddingTop = impactPanel.style.paddingBottom = 8;
            impactPanel.style.paddingLeft = impactPanel.style.paddingRight = 10;
            impactPanel.style.marginTop = 10;
            impactPanel.style.marginBottom = 8;
            impactPanel.style.flexShrink = 0;

            var impactTitle = new Label("OPERATION IMPACT");
            impactTitle.style.color = new StyleColor(new Color(0, 0.82f, 1f));
            impactTitle.style.fontSize = 9;
            impactTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            impactTitle.style.letterSpacing = 1;
            impactTitle.style.marginBottom = 6;
            impactPanel.Add(impactTitle);

            var impactStatRow = new VisualElement();
            impactStatRow.style.flexDirection = FlexDirection.Row;
            impactStatRow.style.marginBottom = 6;

            Label impactCostValLbl   = AddImpactStat(impactStatRow, "COST",         "-");
            impactCostValLbl.name = "impactCostVal";
            Label impactDurationValLbl = AddImpactStat(impactStatRow, "DURATION",   "-");
            impactDurationValLbl.name = "impactDurationVal";
            Label impactChanceValLbl = AddImpactStat(impactStatRow, "SUCCESS",       "-");
            impactChanceValLbl.name = "impactChanceVal";
            impactPanel.Add(impactStatRow);

            var impactEffectsLabel = new Label("EFFECTS:");
            impactEffectsLabel.style.color = new StyleColor(Color.grey);
            impactEffectsLabel.style.fontSize = 8;
            impactEffectsLabel.style.marginBottom = 4;
            impactPanel.Add(impactEffectsLabel);

            var impactEffectsRowContainer = new VisualElement();
            impactEffectsRowContainer.name = "impactEffectsRow";
            impactEffectsRowContainer.style.flexDirection = FlexDirection.Row;
            impactEffectsRowContainer.style.flexWrap = Wrap.Wrap;
            impactPanel.Add(impactEffectsRowContainer);

            scroll.Add(impactPanel);

            troopsSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            tanksSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            apcsSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            fightersSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            helisSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            frigatesSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            submarinesSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            rdfSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            bombersSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            uavsSlider.RegisterValueChangedCallback(_ => UpdateEstimation());
            UpdateEstimation();

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            
            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.width = Length.Percent(48);
            cancelBtn.style.height = 30;
            actionRow.Add(cancelBtn);

            var confirmBtn = new Button() { text = "LAUNCH OP" };
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.width = Length.Percent(48);
            confirmBtn.style.height = 30;
            confirmBtn.clicked += () =>
            {
                int totalDeployed = troopsSlider.value + tanksSlider.value + apcsSlider.value + fightersSlider.value + helisSlider.value + frigatesSlider.value + submarinesSlider.value + rdfSlider.value + bombersSlider.value + uavsSlider.value;
                if (totalDeployed <= 0)
                {
                    NotificationManager.Instance?.AddNotification("INVALID DEPLOYMENT: You must deploy at least some troops or equipment to launch this operation!", Color.red);
                    return;
                }

                string finalName = string.IsNullOrEmpty(nameInput.value) ? $"OPERATION {selectedRegion.ToUpper()} SHIELD" : nameInput.value.ToUpper();
                DeployOperation(country, finalName, selectedRegion, selectedType, 
                    troopsSlider.value, tanksSlider.value, apcsSlider.value, fightersSlider.value, helisSlider.value, frigatesSlider.value, submarinesSlider.value, rdfSlider.value, bombersSlider.value, uavsSlider.value);
                overlay.parent?.Remove(overlay);
            };
            actionRow.Add(confirmBtn);

            modal.Add(actionRow);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null)
            {
                dashboard.Add(overlay);
            }
            else
            {
                _panelOps.Add(overlay);
            }
        }

        private void DeployOperation(CountryDataSO country, string opName, string regionName, string type, 
            int troops, int tanks, int apcs, int fighters, int helis, int frigates, int submarines,
            int rdf = 0, int bombers = 0, int uavs = 0)
        {
            float monthlyCost = 0.01f + 
                                (troops / 1000f) * 0.01f + 
                                tanks * 0.005f + 
                                apcs * 0.002f + 
                                fighters * 0.015f + 
                                helis * 0.008f + 
                                frigates * 0.04f + 
                                submarines * 0.06f +
                                (rdf / 1000f) * 0.02f +
                                bombers * 0.025f +
                                uavs * 0.005f;

            float securityBoostRate = 1.0f + 
                                      (troops / 1000f) * 0.5f + 
                                      tanks * 0.8f + 
                                      apcs * 0.4f + 
                                      fighters * 2.0f + 
                                      helis * 1.0f + 
                                      frigates * 1.5f + 
                                      submarines * 2.0f +
                                      (rdf / 1000f) * 3.5f +
                                      bombers * 3.0f +
                                      uavs * 1.2f;

            if (country.unitRifles < country.armyStats.personnel)
            {
                securityBoostRate *= 0.5f;
            }

            country.unitInfantry = Mathf.Max(0, country.unitInfantry - troops);
            country.unitTanks = Mathf.Max(0, country.unitTanks - tanks);
            country.unitAPCs = Mathf.Max(0, country.unitAPCs - apcs);
            country.unitFighters = Mathf.Max(0, country.unitFighters - fighters);
            country.unitAttackHelis = Mathf.Max(0, country.unitAttackHelis - helis);
            country.unitFrigates = Mathf.Max(0, country.unitFrigates - frigates);
            country.unitSubmarines = Mathf.Max(0, country.unitSubmarines - submarines);
            country.rdfStats.personnel = Mathf.Max(0, country.rdfStats.personnel - rdf);
            country.unitBombers = Mathf.Max(0, country.unitBombers - bombers);
            country.unitUAVs = Mathf.Max(0, country.unitUAVs - uavs);

            var op = new MilitaryOperation
            {
                opName = opName,
                regionName = regionName,
                type = type,
                status = "Active",
                costPerMonth = monthlyCost * 1000f,
                deployedTroops = troops,
                deployedHelis = helis,
                deployedTanks = tanks,
                deployedAPCs = apcs,
                deployedFighters = fighters,
                deployedFrigates = frigates,
                deployedSubmarines = submarines,
                deployedRDF = rdf,
                deployedBombers = bombers,
                deployedUAVs = uavs,
                securityBoostRate = securityBoostRate,
                turnsToDeploy = 1
            };

            country.activeOperations.Add(op);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.playerBudget -= monthlyCost;
                country.treasury = gm.playerBudget;
            }

            var targetReg = country.regions.Find(r => r.regionName == regionName);
            if (targetReg != null)
            {
                float rdfBonus = rdf > 0 ? (rdf / 1000f) * 8f : 
                                 (country.rdfStats.personnel > 0 && country.rdfReadiness >= 75f) ? 3.0f : 0f;
                float uavBonus = uavs > 0 ? uavs * 0.15f : 0f;
                float airBonus = bombers > 0 ? bombers * 0.5f : 0f;

                targetReg.security = Mathf.Min(100f, targetReg.security + 5f + (troops / 1000f) * 1.5f + rdfBonus + uavBonus + airBonus);

                if (rdfBonus > 0f && rdf > 0)
                    NotificationManager.Instance?.AddNotification($"RDF SPEC-OPS: {rdf} elite operators inserted into {regionName} (+{rdfBonus:F1}% Immediate Security).", Color.cyan);
                if (uavBonus > 0f)
                    NotificationManager.Instance?.AddNotification($"ISR ACTIVE: {uavs} UAVs providing 24/7 surveillance over {regionName} theatre.", new Color(0, 0.8f, 1f));
                if (airBonus > 0f)
                    NotificationManager.Instance?.AddNotification($"AIR STRIKE: {bombers} strategic bombers struck key targets in {regionName} (+{airBonus:F1}% Suppression).", new Color(1f, 0.4f, 0f));
            }

            string deploymentDetails = $"{troops:N0} troops";
            if (rdf > 0) deploymentDetails += $", {rdf} RDF Spec-Ops";
            if (tanks > 0) deploymentDetails += $", {tanks} tanks";
            if (apcs > 0) deploymentDetails += $", {apcs} APCs";
            if (fighters > 0) deploymentDetails += $", {fighters} jets";
            if (bombers > 0) deploymentDetails += $", {bombers} bombers";
            if (uavs > 0) deploymentDetails += $", {uavs} UAVs";
            if (helis > 0) deploymentDetails += $", {helis} helis";
            if (frigates > 0) deploymentDetails += $", {frigates} frigates";
            if (submarines > 0) deploymentDetails += $", {submarines} subs";

            NotificationManager.Instance?.AddNotification($"DEPLOYED: {opName} launched ({deploymentDetails}).", Color.red);
            string opType = type == "AERIAL STRIKE" ? "air assets" : type == "SPECIAL OPS" ? "elite forces" : type == "NAVAL BLOCKADE" ? "naval forces" : "combined forces";
            NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} launched {type} operation in {regionName} theatre deploying {opType} under {opName}.");

            RefreshMilitaryData();
        }

private void PopulateBranches(CountryDataSO country)
        {
            if (_branchContainer == null) return;
            _branchContainer.Clear();

            AddBranchPanel(country.armyStats);
            AddBranchPanel(country.navyStats);
            AddBranchPanel(country.airForceStats);
            AddBranchPanel(country.rdfStats);
        }

        private void AddBranchPanel(BranchStats stats)
        {
            var panel = new VisualElement();
            panel.AddToClassList("sub-panel");
            panel.style.width = Length.Percent(48);
            panel.style.marginBottom = 15;
            panel.style.paddingTop = 15;
            panel.style.paddingBottom = 15;
            panel.style.paddingLeft = 15;
            panel.style.paddingRight = 15;

            var titleLabel = new Label(stats.branchName.ToUpper());
            titleLabel.style.color = new Color(255/255f, 82/255f, 82/255f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            panel.Add(titleLabel);

            var statsRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            statsRow.Add(new Label($"Personnel: {stats.personnel:N0}") { style = { color = Color.white, fontSize = 10 } });
            statsRow.Add(new Label($"Equipment: {stats.equipment:N0}") { style = { color = Color.white, fontSize = 10 } });
            panel.Add(statsRow);

            int activeBases = 0;
            string baseLabel = "Bases";
            var country = GameManager.Instance?.playerCountry;
            if (country != null)
            {
                if (stats.branchName.Contains("Army") || stats.branchName.Contains("ARMY"))
                {
                    activeBases = country.baseArmyCount;
                    baseLabel = "Military Bases";
                }
                else if (stats.branchName.Contains("Navy") || stats.branchName.Contains("NAVY"))
                {
                    activeBases = country.baseNavyCount;
                    baseLabel = "Naval Bases";
                }
                else if (stats.branchName.Contains("Air Force") || stats.branchName.Contains("AIR FORCE"))
                {
                    activeBases = country.baseAirForceCount;
                    baseLabel = "Airbases";
                }
                else if (stats.branchName.Contains("Rapid") || stats.branchName.Contains("RDF") || stats.branchName.Contains("Deployable"))
                {
                    activeBases = country.baseRDFCount;
                    baseLabel = "RDF Command Bases";
                }
            }

            var baseRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 4 } };
            baseRow.Add(new Label($"{baseLabel}:") { style = { color = Color.grey, fontSize = 9 } });
            baseRow.Add(new Label($"{activeBases} Active") { style = { color = new Color(0, 210/255f, 1f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
            panel.Add(baseRow);

            panel.Add(new Label("READINESS") { style = { color = Color.grey, fontSize = 9, marginTop = 10 } });
            panel.Add(new ProgressBar() { value = stats.readiness, highValue = 100, style = { height = 4 } });

            panel.Add(new Label("MORALE") { style = { color = Color.grey, fontSize = 9, marginTop = 5 } });
            panel.Add(new ProgressBar() { value = stats.morale, highValue = 100, style = { height = 4 } });

            _branchContainer.Add(panel);
        }

        private void PopulatePolice(CountryDataSO country)
        {
            if (_regionPoliceList == null) return;
            _regionPoliceList.Clear();

            foreach (var region in country.regions)
            {
                var item = new VisualElement();
                item.AddToClassList("sub-panel");
                item.style.paddingTop = 10;
                item.style.paddingBottom = 10;
                item.style.paddingLeft = 12;
                item.style.paddingRight = 12;
                item.style.width = Length.Percent(32);
                item.style.marginRight = 5;
                item.style.marginBottom = 10;

                int localPolice = country.regions.Count > 0 ? Mathf.RoundToInt(country.nationalPoliceSize / (float)country.regions.Count) : 0;
                
                item.Add(new Label(region.regionName.ToUpper()) { style = { color = new Color(255/255f, 235/255f, 59/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                item.Add(new Label($"Local Police: {localPolice:N0} Officers") { style = { color = Color.white, fontSize = 9, marginTop = 4 } });
                item.Add(new Label($"Security: {region.security:F0}%") { style = { color = Color.grey, fontSize = 8, marginTop = 2 } });
                _regionPoliceList.Add(item);
            }
        }

        private void PopulateSupplyRoutes(CountryDataSO country)
        {
            if (_supplyRouteList == null) return;
            _supplyRouteList.Clear();

            if (country.regions.Count == 0)
            {
                AddSupplyRoute("Capital depot → forward bases", Mathf.Min(country.reserveFuel, country.reserveAmmo));
                return;
            }

            string capital = string.IsNullOrEmpty(country.capitalCity) ? country.countryName : country.capitalCity;
            for (int i = 0; i < country.regions.Count && i < 6; i++)
            {
                var region = country.regions[i];
                float health = (region.security * 0.6f) + (country.reserveFuel * 0.2f) + (country.reserveAmmo * 0.2f);
                health = Mathf.Clamp(health, 5f, 100f);
                AddSupplyRoute($"{capital} → {region.regionName}", health);
            }
        }

        private void AddSupplyRoute(string routeName, float health)
        {
            var row = new VisualElement();
            row.AddToClassList("sub-panel");
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.marginBottom = 10;
            row.style.width = Length.Percent(100);
            row.style.flexShrink = 0;

            row.Add(new Label(routeName) { style = { color = Color.white, fontSize = 10 } });
            var bar = new ProgressBar() { value = health, highValue = 100, style = { height = 4, marginTop = 6, width = Length.Percent(100) } };
            if (health < 50) bar.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.3f);
            row.Add(bar);

            _supplyRouteList.Add(row);
        }

        private void SetPosture(string posture)
        {
            _currentPosture = posture;
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            switch (posture)
            {
                case "DEFENSIVE":
                    country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 0.5f);
                    break;
                case "OFFENSIVE":
                    country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 1f);
                    break;
                case "COIN FOCUS":
                    foreach (var r in country.regions)
                        if (r.security < 60f) r.security = Mathf.Min(100f, r.security + 1f);
                    break;
            }

            UpdatePostureButtonStyles();
            UpdateDoctrineText(posture);
            NotificationManager.Instance?.AddNotification($"DOCTRINE: {posture} posture enacted.", new Color(0, 210/255f, 1f));
        }

        private void UpdatePostureButtonStyles()
        {
            var btnDef = _root.Q<Button>("BtnPostureDefensive");
            var btnDet = _root.Q<Button>("BtnPostureDeterrence");
            var btnOff = _root.Q<Button>("BtnPostureOffensive");
            var btnCoin = _root.Q<Button>("BtnPostureCOIN");

            if (btnDef == null || btnDet == null || btnOff == null || btnCoin == null) return;

            var buttons = new Dictionary<string, Button>
            {
                { "DEFENSIVE", btnDef },
                { "DETERRENCE", btnDet },
                { "OFFENSIVE", btnOff },
                { "COIN FOCUS", btnCoin }
            };

            foreach (var kvp in buttons)
            {
                if (kvp.Key == _currentPosture)
                {
                    kvp.Value.AddToClassList("posture-active");
                    kvp.Value.RemoveFromClassList("posture-inactive");
                }
                else
                {
                    kvp.Value.AddToClassList("posture-inactive");
                    kvp.Value.RemoveFromClassList("posture-active");
                }
            }
        }

        private void UpdateDoctrineText(string posture)
        {
            if (_doctrineDesc == null) return;
            switch (posture)
            {
                case "DEFENSIVE":
                    _doctrineDesc.text = "Territorial integrity focus. Readiness +5% in home regions; lower escalation risk.";
                    break;
                case "DETERRENCE":
                    _doctrineDesc.text = "Visible patrols and exercises. Neighbours less likely to escalate; +8% defence budget strain.";
                    break;
                case "OFFENSIVE":
                    _doctrineDesc.text = "Force projection and rapid strikes. RDF effectiveness up; diplomatic tension rises.";
                    break;
                case "COIN FOCUS":
                    _doctrineDesc.text = "Counter-insurgency prioritized. Internal security ops cheaper; heavy armour readiness −5%.";
                    break;
            }
        }

        private void DecommissionUnit(string id, float cost, int amount, bool isPersonnel)
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;

            float refund = cost * 0.40f;

            if (isPersonnel)
            {
                int standing = 0;
                switch (id)
                {
                    case "INF":
                        standing = country.armyStats.personnel;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active infantry!", Color.red); return; }
                        country.armyStats.personnel -= amount;
                        country.unitInfantry = Mathf.Max(0, country.unitInfantry - amount);
                        break;
                    case "POLICE":
                        standing = country.nationalPoliceSize;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active police forces!", Color.red); return; }
                        country.nationalPoliceSize -= amount;
                        break;
                    case "NAVY_PERS":
                        standing = country.navyStats.personnel;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active naval personnel!", Color.red); return; }
                        country.navyStats.personnel -= amount;
                        break;
                    case "AIR_PERS":
                        standing = country.airForceStats.personnel;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active air crew!", Color.red); return; }
                        country.airForceStats.personnel -= amount;
                        break;
                    case "RDF_PERS":
                        standing = country.rdfStats.personnel;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active spec-ops soldiers!", Color.red); return; }
                        country.rdfStats.personnel -= amount;
                        break;
                }

                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                gm.playerBudget += refund;
                country.treasury = gm.playerBudget;
                NotificationManager.Instance?.AddNotification($"DISBANDED: Dismissed {amount:N0} forces. Reclaimed {sym}{refund:N2}B for Treasury.", Color.cyan);
            }
            else
            {
                switch (id)
                {
                    case "RIFLE":
                        if (country.unitRifles < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds rifles in stock!", Color.red); return; }
                        country.unitRifles -= amount;
                        country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amount);
                        break;
                    case "ARTILLERY":
                        if (country.unitArtillery < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds artillery in stock!", Color.red); return; }
                        country.unitArtillery -= amount;
                        country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amount);
                        break;
                    case "TNK":
                        if (country.unitTanks < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds tanks in stock!", Color.red); return; }
                        country.unitTanks -= amount;
                        country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amount);
                        break;
                    case "APC":
                        if (country.unitAPCs < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds APCs in stock!", Color.red); return; }
                        country.unitAPCs -= amount;
                        country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amount);
                        break;
                    case "UAV":
                        if (country.unitUAVs < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds UAVs in stock!", Color.red); return; }
                        country.unitUAVs -= amount;
                        country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amount);
                        break;
                    case "FTR":
                        if (country.unitFighters < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds fighter jets in stock!", Color.red); return; }
                        country.unitFighters -= amount;
                        country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amount);
                        break;
                    case "BOMBER":
                        if (country.unitBombers < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds bombers in stock!", Color.red); return; }
                        country.unitBombers -= amount;
                        country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amount);
                        break;
                    case "HEL":
                        if (country.unitAttackHelis < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds attack helicopters in stock!", Color.red); return; }
                        country.unitAttackHelis -= amount;
                        country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amount);
                        break;
                    case "PATROL_BOAT":
                        if (country.unitPatrolBoats < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds patrol boats in stock!", Color.red); return; }
                        country.unitPatrolBoats -= amount;
                        country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amount);
                        break;
                    case "FRG":
                        if (country.unitFrigates < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds frigates in stock!", Color.red); return; }
                        country.unitFrigates -= amount;
                        country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amount);
                        break;
                    case "SUB":
                        if (country.unitSubmarines < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds submarines in stock!", Color.red); return; }
                        country.unitSubmarines -= amount;
                        country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amount);
                        break;
                    case "DESTROYER":
                        if (country.unitDestroyers < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds destroyers in stock!", Color.red); return; }
                        country.unitDestroyers -= amount;
                        country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amount);
                        break;
                }

                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                gm.playerBudget += refund;
                country.treasury = gm.playerBudget;
                NotificationManager.Instance?.AddNotification($"DECOMMISSIONED: Scrapped equipment. Reclaimed {sym}{refund:N2}B for Treasury.", Color.cyan);
            }

            RefreshMilitaryData();
        }

        private void BuyLogistics(string resource, float costB)
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float localCost = costB * country.exchangeRate;
            float costM = localCost * 1000f;
            string notification = "";
            bool success = false;

            switch (resource)
            {
                case "FUEL":
                    if (country.invOilBarrels >= 500000)
                    {
                        country.invOilBarrels -= 500000;
                        country.reserveFuel = Mathf.Min(100f, country.reserveFuel + 25f);
                        notification = $"FUEL RESERVES: Supplied from National Oil Reserves (-500,000 Barrels). Cost: {sym}0M";
                        success = true;
                    }
                    else if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        country.reserveFuel = Mathf.Min(100f, country.reserveFuel + 25f);
                        notification = $"FUEL RESERVES: Secured foreign oil imports (+25 days of fuel). Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;

                case "AMMO":
                    if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        country.reserveAmmo = Mathf.Min(100f, country.reserveAmmo + 25f);
                        notification = $"MUNITIONS SUPPLY: Purchased strategic stockpile (+25% ammo reserves). Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;

                case "MEDICAL":
                    if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        country.reserveMedical = Mathf.Min(100f, country.reserveMedical + 25f);
                        notification = $"TRAUMA LOGISTICS: Dispatched battlefield clinics (+25% medical reserves). Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;

                case "FOOD":
                    if (country.invRiceTonnes >= 10000)
                    {
                        country.invRiceTonnes -= 10000;
                        country.reserveFood = Mathf.Min(100f, country.reserveFood + 25f);
                        notification = $"RATIONS LOGISTICS: Supplied from National Rice Stockpile (-10,000 Tonnes). Cost: {sym}0M";
                        success = true;
                    }
                    else if (gm.playerBudget >= localCost)
                    {
                        gm.playerBudget -= localCost;
                        country.treasury = gm.playerBudget;
                        country.reserveFood = Mathf.Min(100f, country.reserveFood + 25f);
                        notification = $"RATIONS LOGISTICS: Secured foreign field supply routes (+25% food reserves). Cost: {sym}{costM:N0}M";
                        success = true;
                    }
                    break;
            }

            if (success)
            {
                NotificationManager.Instance?.AddNotification(notification, Color.green);
                RefreshMilitaryData();
            }
            else
            {
                NotificationManager.Instance?.AddNotification($"INSUFFICIENT RESOURCES/FUNDS: Need {sym}{costM:N0}M or national stockpiles to secure logistics!", Color.red);
            }
        }
        private void PopulateBudget(CountryDataSO country)
        {
            if (_budgetLedgerList == null) return;
            _budgetLedgerList.Clear();

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float exc = country.exchangeRate;

            float armyUpkeepUSD = (country.unitInfantry * 0.0000020f) + (country.unitRifles * 0.0000002f) + (country.unitTanks * 0.000030f) + (country.unitAPCs * 0.000010f) + (country.unitArtillery * 0.000015f);
            float airUpkeepUSD = (country.airForceStats.personnel * 0.000006f) + (country.unitFighters * 0.000150f) + (country.unitBombers * 0.000280f) + (country.unitAttackHelis * 0.000080f) + (country.unitUAVs * 0.000020f);
            float navyUpkeepUSD = (country.navyStats.personnel * 0.000005f) + (country.unitFrigates * 0.000400f) + (country.unitDestroyers * 0.000800f) + (country.unitSubmarines * 0.001000f) + (country.unitPatrolBoats * 0.000040f);
            float rdfUpkeepUSD = (country.rdfStats.personnel * 0.000010f) + (country.rdfStats.equipment * 0.000050f);
            
            float basesUpkeepUSD = (country.baseArmyCount * 0.006f) + (country.baseNavyCount * 0.010f) + (country.baseAirForceCount * 0.010f) + (country.baseRDFCount * 0.015f);

            float actualArmy = armyUpkeepUSD * (country.armyStats.readiness / 100f);
            float actualAir = airUpkeepUSD * (country.airForceStats.readiness / 100f);
            float actualNavy = navyUpkeepUSD * (country.navyStats.readiness / 100f);
            float actualRdf = rdfUpkeepUSD * (country.rdfStats.readiness / 100f);

            float totalCalculated = actualArmy + actualAir + actualNavy + actualRdf + basesUpkeepUSD;
            float adminOverheadUSD = 0f;
            if (totalCalculated < 0.10f)
            {
                adminOverheadUSD = 0.10f - totalCalculated;
            }

            AddBudgetRow("Army (Infantry, Armor, Artillery & Rifles)", actualArmy * exc, new Color(255/255f, 152/255f, 0), sym);
            AddBudgetRow("Air Force (Personnel & Aircraft Maintenance)", actualAir * exc, new Color(156/255f, 39/255f, 176/255f), sym);
            AddBudgetRow("Navy (Personnel & Fleet Maintenance)", actualNavy * exc, new Color(33/255f, 150/255f, 243/255f), sym);
            AddBudgetRow("Rapid Deployable Force (Special Ops)", actualRdf * exc, new Color(255/255f, 82/255f, 82/255f), sym);
            AddBudgetRow("Strategic Bases Power & Upkeep", basesUpkeepUSD * exc, Color.grey, sym);
            if (adminOverheadUSD > 0f) AddBudgetRow("Minimum Command & Admin Overhead", adminOverheadUSD * exc, Color.white, sym);

            var separator = new VisualElement() { style = { height = 1, backgroundColor = new Color(1, 1, 1, 0.2f), marginTop = 10, marginBottom = 10 } };
            _budgetLedgerList.Add(separator);

            float totalBudget = Mathf.Max(0.10f, totalCalculated) * exc;
            var totalRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            totalRow.Add(new Label("TOTAL STANDING UPKEEP / TURN") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            totalRow.Add(new Label($"-{UiFormat.Money(totalBudget, sym)}") { style = { color = new Color(255/255f, 82/255f, 82/255f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12 } });
            _budgetLedgerList.Add(totalRow);
        }

        private void AddBudgetRow(string title, float billionsLocal, Color accentColor, string sym)
        {
            var row = new VisualElement();
            row.AddToClassList("sub-panel");
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.style.borderLeftWidth = 3;
            row.style.borderLeftColor = accentColor;

            row.Add(new Label(title) { style = { color = Color.white, fontSize = 10 } });
            row.Add(new Label($"-{UiFormat.Money(billionsLocal, sym)}") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });

            _budgetLedgerList.Add(row);
        }

private void PopulateThreatList(CountryDataSO country)
        {
            if (_threatList == null) return;
            _threatList.Clear();
            foreach (var r in country.regions)
            {
                var row = new VisualElement();
                row.AddToClassList("threat-row");

                string threatType = "Stable";
                ThreatLevel lvl = ThreatLevel.Low;

                if (r.security < 50f)
                {
                    threatType = "Terrorist Presence";
                    lvl = ThreatLevel.High;
                }
                else if (r.security < 75f)
                {
                    threatType = "Civil Unrest";
                    lvl = ThreatLevel.Medium;
                }

                var icon = new VisualElement();
                icon.AddToClassList("threat-row__icon");
                icon.AddToClassList(ThreatIconClass(lvl));
                var glyph = new Label(ThreatGlyph(lvl));
                glyph.AddToClassList("threat-row__icon-glyph");
                glyph.style.color = ThreatColour(lvl);
                icon.Add(glyph);
                row.Add(icon);

                var info = new VisualElement();
                info.AddToClassList("threat-row__info");
                var name = new Label(r.regionName);
                name.AddToClassList("threat-row__name");
                var sub = new Label(threatType);
                sub.AddToClassList("threat-row__sub");
                info.Add(name); info.Add(sub);
                row.Add(info);

                var badge = new VisualElement();
                badge.AddToClassList("threat-row__badge-col");
                var lvlLabel = new Label("THREAT LEVEL");
                lvlLabel.AddToClassList("threat-row__level-label");
                var lvlObj = new Label(lvl.ToString().ToUpper());
                lvlObj.AddToClassList("threat-row__level");
                lvlObj.AddToClassList(ThreatLevelClass(lvl));
                badge.Add(lvlLabel); badge.Add(lvlObj);
                row.Add(badge);

                _threatList.Add(row);
            }
        }

        private void UpdateIntelAssessment(CountryDataSO country)
        {
            if (country.regions.Count == 0) return;
            var r = country.regions[0];
            foreach (var reg in country.regions)
            {
                if (reg.security < r.security) r = reg;
            }

            var root = _root;
            if (root == null) return;

            string threatLevel = r.security < 50f ? "HIGH" : (r.security < 75f ? "MEDIUM" : "LOW");
            int minEnemies = Mathf.Max(500, (int)((100f - r.security) * 80));
            int maxEnemies = (int)(minEnemies * 1.4f);

            var lblRegion = root.Q<Label>("lbl-intel-region");
            if (lblRegion != null) lblRegion.text = r.regionName.ToUpper();
            
            var lblThreat = root.Q<Label>("lbl-threat-level");
            if (lblThreat != null) lblThreat.text = threatLevel;
            
            var lblStr = root.Q<Label>("lbl-enemy-strength");
            if (lblStr != null) lblStr.text = $"{minEnemies:N0} - {maxEnemies:N0}";
            
            var lblConf = root.Q<Label>("lbl-confidence");
            if (lblConf != null) lblConf.text = $"{r.stability:F0}%";
            
            var lblSrc = root.Q<Label>("lbl-intel-source");
            if (lblSrc != null) lblSrc.text = "Signals Intelligence";
            
            var lblUpd = root.Q<Label>("lbl-last-updated");
            if (lblUpd != null) lblUpd.text = "Live";
        }

        private void PopulateActiveOpsList(CountryDataSO country)
        {
            if (_opsList == null) return;
            _opsList.Clear();

            var btnViewAll = _root?.Q<Button>("btn-view-all-ops");
            int opCount = country.activeOperations != null ? country.activeOperations.Count : 0;
            if (btnViewAll != null)
                btnViewAll.text = opCount > 0 ? $"VIEW ALL OPERATIONS ({opCount}) >" : "VIEW ALL OPERATIONS >";

            if (country.activeOperations == null || country.activeOperations.Count == 0)
            {
                var empty = new VisualElement();
                empty.style.flexDirection = FlexDirection.Column;
                empty.style.alignItems = Align.Center;
                empty.style.justifyContent = Justify.Center;
                empty.style.flexGrow = 1;
                empty.style.paddingTop = 20;
                empty.style.paddingBottom = 20;
                var emptyIcon = new Label("—");
                emptyIcon.style.fontSize = 28;
                emptyIcon.style.color = new Color(0.27f, 0.50f, 0.60f);
                emptyIcon.style.marginBottom = 8;
                var emptyLbl = new Label("NO ACTIVE OPERATIONS");
                emptyLbl.style.fontSize = 11;
                emptyLbl.style.color = new StyleColor(new Color(0.42f, 0.50f, 0.60f, 1f));
                emptyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                emptyLbl.style.letterSpacing = 1;
                emptyLbl.style.marginTop = 10;
                var emptySub = new Label("Launch an operation to begin deployment.");
                emptySub.style.fontSize = 9;
                emptySub.style.color = new StyleColor(new Color(0.3f, 0.38f, 0.46f, 1f));
                emptySub.style.marginTop = 6;
                empty.Add(emptyIcon); empty.Add(emptyLbl); empty.Add(emptySub);
                _opsList.Add(empty);
                return;
            }

            foreach (var op in country.activeOperations)
            {
                OpType typeEnum = OpType.Peacekeeping;
                if (op.type.Contains("TERROR")) typeEnum = OpType.CounterTerror;
                else if (op.type.Contains("BORDER")) typeEnum = OpType.BorderPatrol;
                else if (op.type.Contains("RECON")) typeEnum = OpType.AirRecon;
                else if (op.type.Contains("MARITIME") || op.type.Contains("NAVAL")) typeEnum = OpType.Maritime;
                else if (op.type.Contains("HVT") || op.type.Contains("STRIKE")) typeEnum = OpType.HvtCapture;

                int progress = 100;
                if (op.turnsToDeploy > 0) progress = 10;
                else progress = 50;

                int successChance = 80;

                var card = new VisualElement();
                card.AddToClassList("op-card");
                card.AddToClassList(OpTypeCardClass(typeEnum));

                var iconEl = new VisualElement();
                iconEl.AddToClassList("op-card__icon");
                var iconGlyph = new Label(OpTypeGlyph(typeEnum));
                iconGlyph.AddToClassList("op-card__icon-glyph");
                iconEl.Add(iconGlyph);
                card.Add(iconEl);

                var info = new VisualElement();
                info.AddToClassList("op-card__info");
                var nameLabel = new Label(op.opName);
                nameLabel.AddToClassList("op-card__name");
                var meta = new Label($"Type: {op.type}    Region: {op.regionName}");
                meta.AddToClassList("op-card__meta");
                var obj = new Label($"Objective: Secure {op.regionName} and suppress hostilities.");
                obj.AddToClassList("op-card__objective");
                info.Add(nameLabel); info.Add(meta); info.Add(obj);
                card.Add(info);

                var progStat = new VisualElement();
                progStat.AddToClassList("op-card__stats");
                var progLbl = new Label("PROGRESS");
                progLbl.AddToClassList("op-card__stat-label");
                var progVal = new Label($"{progress}%");
                progVal.AddToClassList("op-card__stat-value");
                var barBg = new VisualElement();
                barBg.AddToClassList("op-card__progress-bar");
                var barFill = new VisualElement();
                barFill.AddToClassList("op-card__progress-fill");
                barFill.AddToClassList(ProgressFillClass(progress));
                barFill.style.width = Length.Percent(progress);
                barBg.Add(barFill);
                progStat.Add(progLbl); progStat.Add(progVal); progStat.Add(barBg);
                card.Add(progStat);

                var successStat = new VisualElement();
                successStat.AddToClassList("op-card__stats");
                var successLbl = new Label("SUCCESS\nCHANCE");
                successLbl.AddToClassList("op-card__stat-label");
                var successVal = new Label($"{successChance}%");
                successVal.AddToClassList("op-card__stat-value");
                successStat.Add(successLbl); successStat.Add(successVal);
                card.Add(successStat);

                var etaEl = new VisualElement();
                etaEl.AddToClassList("op-card__eta");
                var etaLbl = new Label("ETA");
                etaLbl.AddToClassList("op-card__eta-label");
                string etaStr = op.turnsToDeploy > 0 ? $"{op.turnsToDeploy} Mo" : "Deployed";
                var etaVal = new Label(etaStr);
                etaVal.AddToClassList("op-card__eta-value");
                etaEl.Add(etaLbl); etaEl.Add(etaVal);
                card.Add(etaEl);
                
                var viewBtn = new Button(() => {  });
                viewBtn.text = "ABORT";
                viewBtn.AddToClassList("op-card__view-btn");
                viewBtn.style.backgroundColor = new Color(244/255f, 67/255f, 54/255f, 0.4f);
                viewBtn.clicked += () => {
                        country.activeOperations.Remove(op);
                        country.unitInfantry += op.deployedTroops;
                        country.rdfStats.personnel += op.deployedRDF;
                        country.unitTanks += op.deployedTanks;
                        country.unitAPCs += op.deployedAPCs;
                        country.unitFighters += op.deployedFighters;
                        country.unitAttackHelis += op.deployedHelis;
                        RefreshMilitaryData();
                };
                card.Add(viewBtn);

                _opsList.Add(card);
            }
        }

        private void PopulateOperationsFeed(CountryDataSO country, GameManager gm)
        {
            if (_feedList == null) return;
            _feedList.Clear();
            var feedData = DashboardLiveContent.BuildMilitaryFeed(country, gm);
            if (feedData == null || feedData.Count == 0) return;

            foreach (var f in feedData)
            {
                var item = new VisualElement();
                item.AddToClassList("feed-item");

                FeedColour col = FeedColour.Green;
                if (f.Message.Contains("Critical") || f.Message.Contains("WAR") || f.Message.Contains("KIA") || f.Message.Contains("SHORTAGE")) col = FeedColour.Red;
                else if (f.Message.Contains("Alert") || f.Message.Contains("Warning")) col = FeedColour.Yellow;

                var dot = new VisualElement();
                dot.AddToClassList("feed-item__dot");
                dot.AddToClassList(FeedDotClass(col));
                item.Add(dot);

                var dateCol = new VisualElement();
                dateCol.AddToClassList("feed-item__date-col");
                var day  = new Label("LIVE");
                day.AddToClassList("feed-item__day");
                var date = new Label($"{gm.TurnSystem.CurrentDate}");
                date.AddToClassList("feed-item__date");
                dateCol.Add(day); dateCol.Add(date);
                item.Add(dateCol);

                var text = new Label($"[{f.Tag}] {f.Message}");
                text.AddToClassList("feed-item__text");
                item.Add(text);

                _feedList.Add(item);
            }
        }

        private void PopulateOpTypesGrid(CountryDataSO country)
        {
            var opTypesScroll = _root?.Q<ScrollView>("op-types-scroll");
            if (opTypesScroll == null) return;
            opTypesScroll.Clear();
            
            var opTypes = new List<OpTypeEntry>()
            {
                new OpTypeEntry("COUNTER TERROR", "Eliminate terrorist cells and safe havens.", "RDF, Air Support", "₦50B", OpType.CounterTerror),
                new OpTypeEntry("BORDER PATROL", "Secure borders and prevent infiltration.", "Infantry, Air Recon", "₦15B", OpType.BorderPatrol),
                new OpTypeEntry("PEACEKEEPING", "Maintain stability in unstable regions.", "Infantry, Logistics", "₦35B", OpType.Peacekeeping),
                new OpTypeEntry("AIR RECON", "Gather intelligence and monitor threats.", "Air Wing", "₦25B", OpType.AirRecon),
                new OpTypeEntry("MARITIME PATROL", "Protect territorial waters and trade.", "Navy, Air Support", "₦20B", OpType.Maritime),
                new OpTypeEntry("HVT CAPTURE", "Capture or neutralise high value targets.", "RDF, Intelligence", "₦40B", OpType.HvtCapture),
            };

            foreach (var t in opTypes)
            {
                var card = new VisualElement();
                card.AddToClassList("op-type-card");
                card.AddToClassList(OpTypeCardClass(t.Type));

                var header = new VisualElement();
                header.AddToClassList("op-type-card__header");
                var iconLabel = new Label(OpTypeGlyph(t.Type));
                iconLabel.AddToClassList("op-type-card__icon");
                iconLabel.style.color = OpTypeAccentColour(t.Type);
                var nameLabel = new Label(t.Name);
                nameLabel.AddToClassList("op-type-card__name");
                nameLabel.style.color = OpTypeAccentColour(t.Type);
                header.Add(iconLabel); header.Add(nameLabel);
                card.Add(header);

                var desc = new Label(t.Description);
                desc.AddToClassList("op-type-card__desc");
                card.Add(desc);

                var reqLabel = new Label("Required Forces");
                reqLabel.AddToClassList("op-type-card__req-label");
                card.Add(reqLabel);
                var reqVal = new Label(t.RequiredForces);
                reqVal.AddToClassList("op-type-card__req-val");
                card.Add(reqVal);

                var costLabel = new Label("Est. Cost");
                costLabel.AddToClassList("op-type-card__cost-label");
                card.Add(costLabel);
                var costVal = new Label(t.EstCost);
                costVal.AddToClassList("op-type-card__cost-val");
                card.Add(costVal);

                var selectBtn = new Button(() => { ShowLaunchOpModal(); });
                selectBtn.text = "SELECT";
                selectBtn.AddToClassList("op-type-card__select-btn");
                card.Add(selectBtn);

                opTypesScroll.Add(card);
            }
        }

        private void PopulateReadinessList(CountryDataSO country)
        {
            if (_readinessList == null) return;
            _readinessList.Clear();

            var branches = new List<System.Tuple<string, string, int>>()
            {
                new System.Tuple<string, string, int>("ARMY",      "A", Mathf.Clamp((int)country.armyStats.readiness,    0, 100)),
                new System.Tuple<string, string, int>("RDF",       "R", Mathf.Clamp((int)country.rdfStats.readiness,     0, 100)),
                new System.Tuple<string, string, int>("AIR FORCE", "F", Mathf.Clamp((int)country.airForceStats.readiness,0, 100)),
                new System.Tuple<string, string, int>("NAVY",      "N", Mathf.Clamp((int)country.navyStats.readiness,    0, 100))
            };

            foreach (var r in branches)
            {
                var row = new VisualElement();
                row.AddToClassList("readiness-row");

                var icon = new Label(r.Item2);
                icon.AddToClassList("readiness-row__icon");
                row.Add(icon);

                var name = new Label(r.Item1);
                name.AddToClassList("readiness-row__name");
                row.Add(name);

                var barBg = new VisualElement();
                barBg.AddToClassList("readiness-row__bar-bg");
                var barFill = new VisualElement();
                barFill.AddToClassList("readiness-row__bar-fill");
                barFill.style.width = Length.Percent(r.Item3);
                barBg.Add(barFill);
                row.Add(barBg);

                var pct = new Label($"{r.Item3}%");
                pct.AddToClassList("readiness-row__pct");
                row.Add(pct);

                _readinessList.Add(row);
            }
        }

        public enum ThreatLevel  { High, Medium, Low }
        public enum OpType       { CounterTerror, Peacekeeping, BorderPatrol, AirRecon, Maritime, HvtCapture }
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

