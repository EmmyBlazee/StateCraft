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
    public partial class MilitaryDashboardController
    {
        // UXML Element References
        private Label _basesStatTotal;
        private Label _basesStatCapacity;
        private Label _basesStatOccupied;
        private Label _basesStatUpkeep;
        private ScrollView _basesListContainer;
        
        private Label _detailBaseName;
        private Label _detailBaseLocation;
        private Label _detailBaseStatus;
        private Label _detailBaseEstablished;
        private Label _detailBaseGarrison;
        private Label _detailBasePersonnel;
        private Label _detailBaseSize;
        private Label _detailBaseThreat;
        
        // Titles for Details
        private Label _titleBaseEstablished;
        private Label _titleBaseGarrison;
        private Label _titleBasePersonnel;
        private Label _titleBaseSize;
        private Label _titleBaseThreat;
        
        private VisualElement _baseInfoListContainer;
        private VisualElement _baseCapabilitiesContainer;
        private VisualElement _assignedUnitsContainer;
        private VisualElement _baseActivityContainer;
        
        private VisualElement _globalBasesOverviewContainer;
        private ScrollView _constructionQueueContainer;
        private ScrollView _upgradeQueueContainer;
        
        private VisualElement _baseDetailsPanel;
        private VisualElement _baseDetailsEmptyState;
        private VisualElement _baseBannerImage;
        private Label _detailBaseIcon;
        
        // Construction Modal
        private VisualElement _constructModalOverlay;
        private Button _btnCloseConstructModal;
        private Button _btnTypeBase;
        private Button _btnTypeFactory;
        private Button _btnTypeTraining;
        private DropdownField _dropdownConstructRegion;
        private DropdownField _dropdownConstructBranch;
        private TextField _inputConstructName;
        private Button _btnCancelConstruct;
        private Button _btnConfirmConstruct;
        private VisualElement _containerConstructBranch;
        
        // Construction Stats
        private Label _labelConstructCost;
        private Label _labelConstructCapacityTitle;
        private Label _labelConstructCapacity;
        private Label _labelConstructTime;
        
        private int _constructFacilityType = 0; // 0=Base, 1=Factory, 2=Training
        private List<RegionDataSO> _availableConstructRegions;
        
        // Upgrade Modal
        private VisualElement _upgradeModalOverlay;
        private Button _btnCloseUpgradeModal;
        private Button _btnCancelUpgrade;
        private Button _btnConfirmUpgrade;
        private Label _labelUpgradeTitle;
        private Label _labelUpgradeName;
        private Label _labelUpgradeDesc;
        private Label _labelUpgradeCost;
        private Label _labelUpgradeBenefitTitle;
        private Label _labelUpgradeBenefit;
        private Label _labelUpgradeTime;
        
        private bool _basesWired = false;

        private void EnsureBasesWired()
        {
            if (_basesWired) return;
            if (_panelBases == null) 
            {
                Debug.Log("[Bases] _panelBases is null!");
                return;
            }
            
            BindBasesElements(_panelBases);
            
            if (_basesListContainer != null) 
            {
                _basesWired = true;
                Debug.Log("[Bases] EnsureBasesWired success!");
            }
            else
            {
                Debug.Log("[Bases] BindBasesElements failed to find _basesListContainer!");
                DumpVisualTree(_panelBases, 0);
            }
        }

        private void DumpVisualTree(VisualElement element, int depth)
        {
            if (element == null) return;
            string indent = new string('-', depth * 2);
            Debug.Log($"[BasesTree] {indent} Name: {element.name}, Type: {element.GetType().Name}");
            foreach (var child in element.Children())
            {
                DumpVisualTree(child, depth + 1);
            }
        }

        private string _currentBranchFilter = "ALL";
        private Button _btnFilterAll, _btnFilterArmy, _btnFilterAirForce, _btnFilterNavy, _btnFilterJoint;
        
        private Button _btnDetailUpgrade;
        private MilitaryBase _currentSelectedBase;
        private DefenseFactory _currentSelectedFactory;

        private void BindBasesElements(VisualElement root)
        {
            _basesStatTotal = root.Q<Label>("BasesStatTotal");
            _basesStatCapacity = root.Q<Label>("BasesStatCapacity");
            _basesStatOccupied = root.Q<Label>("BasesStatOccupied");
            _basesStatUpkeep = root.Q<Label>("BasesStatUpkeep");
            
            _basesListContainer = root.Q<ScrollView>("BasesListContainer");
            
            _detailBaseName = root.Q<Label>("DetailBaseName");
            _detailBaseLocation = root.Q<Label>("DetailBaseLocation");
            _detailBaseStatus = root.Q<Label>("DetailBaseStatus");
            _detailBaseEstablished = root.Q<Label>("DetailBaseEstablished");
            _detailBaseGarrison = root.Q<Label>("DetailBaseGarrison");
            _detailBasePersonnel = root.Q<Label>("DetailBasePersonnel");
            _detailBaseSize = root.Q<Label>("DetailBaseSize");
            _detailBaseThreat = root.Q<Label>("DetailBaseThreat");
            
            _titleBaseEstablished = root.Q<Label>("TitleBaseEstablished");
            _titleBaseGarrison = root.Q<Label>("TitleBaseGarrison");
            _titleBasePersonnel = root.Q<Label>("TitleBasePersonnel");
            _titleBaseSize = root.Q<Label>("TitleBaseSize");
            _titleBaseThreat = root.Q<Label>("TitleBaseThreat");
            
            _baseInfoListContainer = root.Q<VisualElement>("BaseInfoListContainer");
            _baseCapabilitiesContainer = root.Q<VisualElement>("BaseCapabilitiesContainer");
            _assignedUnitsContainer = root.Q<VisualElement>("AssignedUnitsContainer");
            _baseActivityContainer = root.Q<VisualElement>("BaseActivityContainer");
            
            _globalBasesOverviewContainer = root.Q<VisualElement>("GlobalBasesOverviewContainer");
            _constructionQueueContainer = root.Q<ScrollView>("ConstructionQueueContainer");
            _upgradeQueueContainer = root.Q<ScrollView>("UpgradeQueueContainer");
            
            _baseDetailsPanel = root.Q<VisualElement>("BaseDetailsPanel");
            _baseDetailsEmptyState = root.Q<VisualElement>("BaseDetailsEmptyState");
            _baseBannerImage = root.Q<VisualElement>("BaseBannerImage");
            _detailBaseIcon = root.Q<Label>("DetailBaseIcon");
            
            _btnDetailUpgrade = root.Q<Button>("BtnDetailUpgrade");
            if (_btnDetailUpgrade != null) _btnDetailUpgrade.clicked += OnUpgradeClicked;

            _btnFilterAll = root.Q<Button>("BtnFilterAll");
            _btnFilterArmy = root.Q<Button>("BtnFilterArmy");
            _btnFilterAirForce = root.Q<Button>("BtnFilterAirForce");
            _btnFilterNavy = root.Q<Button>("BtnFilterNavy");
            _btnFilterJoint = root.Q<Button>("BtnFilterJoint");

            if (_btnFilterAll != null) _btnFilterAll.clicked += () => SetFilter("ALL");
            if (_btnFilterArmy != null) _btnFilterArmy.clicked += () => SetFilter("ARMY");
            if (_btnFilterAirForce != null) _btnFilterAirForce.clicked += () => SetFilter("AIR FORCE");
            if (_btnFilterNavy != null) _btnFilterNavy.clicked += () => SetFilter("NAVY");
            if (_btnFilterJoint != null) _btnFilterJoint.clicked += () => SetFilter("JOINT");

            var btnConstructNewBase = root.Q<Button>("BtnConstructNewBase");
            if (btnConstructNewBase != null)
            {
                btnConstructNewBase.clicked += OnConstructNewBaseClicked;
            }

            _constructModalOverlay = root.Q<VisualElement>("ConstructModalOverlay");
            _btnCloseConstructModal = root.Q<Button>("BtnCloseConstructModal");
            _btnTypeBase = root.Q<Button>("BtnTypeBase");
            _btnTypeFactory = root.Q<Button>("BtnTypeFactory");
            _btnTypeTraining = root.Q<Button>("BtnTypeTraining");
            _dropdownConstructRegion = root.Q<DropdownField>("DropdownConstructRegion");
            _dropdownConstructBranch = root.Q<DropdownField>("DropdownConstructBranch");
            if (_dropdownConstructBranch != null) _dropdownConstructBranch.RegisterValueChangedCallback(evt => UpdateConstructStatsUI());
            _containerConstructBranch = root.Q<VisualElement>("ContainerConstructBranch");
            _inputConstructName = root.Q<TextField>("InputConstructName");
            _btnCancelConstruct = root.Q<Button>("BtnCancelConstruct");
            _btnConfirmConstruct = root.Q<Button>("BtnConfirmConstruct");
            
            _labelConstructCost = root.Q<Label>("LabelConstructCost");
            _labelConstructCapacityTitle = root.Q<Label>("LabelConstructCapacityTitle");
            _labelConstructCapacity = root.Q<Label>("LabelConstructCapacity");
            _labelConstructTime = root.Q<Label>("LabelConstructTime");

            if (_constructModalOverlay == null) Debug.LogError("[Bases] CRITICAL: ConstructModalOverlay is null in UXML!");
            else Debug.Log("[Bases] ConstructModalOverlay found and bound successfully.");

            if (_btnCloseConstructModal != null) _btnCloseConstructModal.clicked += () => _constructModalOverlay.style.display = DisplayStyle.None;
            if (_btnCancelConstruct != null) _btnCancelConstruct.clicked += () => _constructModalOverlay.style.display = DisplayStyle.None;
            if (_btnConfirmConstruct != null) _btnConfirmConstruct.clicked += OnConfirmConstructClicked;
            
            if (_btnTypeBase != null) _btnTypeBase.clicked += () => SetConstructType(0);
            if (_btnTypeFactory != null) _btnTypeFactory.clicked += () => SetConstructType(1);
            if (_btnTypeTraining != null) _btnTypeTraining.clicked += () => SetConstructType(2);

            _upgradeModalOverlay = root.Q<VisualElement>("UpgradeModalOverlay");
            _btnCloseUpgradeModal = root.Q<Button>("BtnCloseUpgradeModal");
            _btnCancelUpgrade = root.Q<Button>("BtnCancelUpgrade");
            _btnConfirmUpgrade = root.Q<Button>("BtnConfirmUpgrade");
            _labelUpgradeTitle = root.Q<Label>("LabelUpgradeTitle");
            _labelUpgradeName = root.Q<Label>("LabelUpgradeName");
            _labelUpgradeDesc = root.Q<Label>("LabelUpgradeDesc");
            _labelUpgradeCost = root.Q<Label>("LabelUpgradeCost");
            _labelUpgradeBenefitTitle = root.Q<Label>("LabelUpgradeBenefitTitle");
            _labelUpgradeBenefit = root.Q<Label>("LabelUpgradeBenefit");
            _labelUpgradeTime = root.Q<Label>("LabelUpgradeTime");

            if (_btnCloseUpgradeModal != null) _btnCloseUpgradeModal.clicked += () => _upgradeModalOverlay.style.display = DisplayStyle.None;
            if (_btnCancelUpgrade != null) _btnCancelUpgrade.clicked += () => _upgradeModalOverlay.style.display = DisplayStyle.None;
            if (_btnConfirmUpgrade != null) _btnConfirmUpgrade.clicked += OnConfirmUpgradeClicked;
        }

        private void SetFilter(string filter)
        {
            _currentBranchFilter = filter;
            if (_btnFilterAll != null) { _btnFilterAll.RemoveFromClassList("active-tab"); _btnFilterAll.Query<Label>().Last().style.color = new Color(0.64f, 0.69f, 0.73f); }
            if (_btnFilterArmy != null) { _btnFilterArmy.RemoveFromClassList("active-tab"); _btnFilterArmy.Query<Label>().Last().style.color = new Color(0.64f, 0.69f, 0.73f); }
            if (_btnFilterAirForce != null) { _btnFilterAirForce.RemoveFromClassList("active-tab"); _btnFilterAirForce.Query<Label>().Last().style.color = new Color(0.64f, 0.69f, 0.73f); }
            if (_btnFilterNavy != null) { _btnFilterNavy.RemoveFromClassList("active-tab"); _btnFilterNavy.Query<Label>().Last().style.color = new Color(0.64f, 0.69f, 0.73f); }
            if (_btnFilterJoint != null) { _btnFilterJoint.RemoveFromClassList("active-tab"); _btnFilterJoint.Query<Label>().Last().style.color = new Color(0.64f, 0.69f, 0.73f); }

            Button activeBtn = filter == "ALL" ? _btnFilterAll :
                               filter == "ARMY" ? _btnFilterArmy :
                               filter == "AIR FORCE" ? _btnFilterAirForce :
                               filter == "NAVY" ? _btnFilterNavy : _btnFilterJoint;
            
            if (activeBtn != null)
            {
                activeBtn.AddToClassList("active-tab");
                activeBtn.Query<Label>().Last().style.color = new Color(0.14f, 0.93f, 0.34f);
            }

            var country = GameManager.Instance?.playerCountry;
            if (country != null) PopulateBases(country);
        }

        private void PopulateBases(CountryDataSO country)
        {
            Debug.Log("[Bases] PopulateBases called");
            if (!_basesWired) EnsureBasesWired();
            if (!_basesWired) 
            {
                Debug.Log("[Bases] Not wired, aborting populate.");
                return;
            }
            if (_basesListContainer == null) return;
            
            EnsureBasesData(country);

            try 
            {
            
            // 1. Top Stats
            int totalBases = country.militaryBases.Count + (country.defenseFactories?.Count ?? 0);
            int totalCapacity = 0;
            int totalOccupied = 0;
            float totalUpkeep = 0f;
            foreach (var b in country.militaryBases) {
                totalCapacity += b.capacity;
                totalOccupied += b.currentPersonnel;
                totalUpkeep += b.upkeepCost;
            }

            if (_basesStatTotal != null) _basesStatTotal.text = totalBases.ToString();
            if (_basesStatCapacity != null) _basesStatCapacity.text = totalCapacity.ToString("N0");
            if (_basesStatOccupied != null) 
            {
                int pct = totalCapacity > 0 ? Mathf.RoundToInt((totalOccupied / (float)totalCapacity) * 100f) : 0;
                _basesStatOccupied.text = $"{totalOccupied:N0} ({pct}%)";
            }
            if (_basesStatUpkeep != null) _basesStatUpkeep.text = $"₦{totalUpkeep:F1}B";

            // 2. Base List (Left Column)
            _basesListContainer.Clear();
            
            int cArmy = 0, cAir = 0, cNavy = 0, cJoint = 0, cAll = 0;
            MilitaryBase firstVisible = null;
            DefenseFactory firstFac = null;
            
            foreach (var b in country.militaryBases)
            {
                if (b.branch == "ARMY") cArmy++;
                else if (b.branch == "AIR") cAir++;
                else if (b.branch == "NAVY") cNavy++;
                else if (b.branch == "JOINT") cJoint++;
                cAll++;

                if (_currentBranchFilter != "ALL" && b.branch != (_currentBranchFilter == "AIR FORCE" ? "AIR" : _currentBranchFilter)) continue;

                if (firstVisible == null) firstVisible = b;
                RegionDataSO region = country.regions?.Find(r => r.regionName == b.regionName);
                string locationLabel = region != null ? $"{region.regionName}, {GameManager.Instance.playerCountry.countryName}" : b.regionName;
                string threatLevel   = GetThreatLevel(region);
                Color  sColor = b.status == "OPERATIONAL" ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f);
                string bIcon  = b.branch == "AIR" ? FaIcons.Plane : b.branch == "NAVY" ? FaIcons.Anchor : b.branch == "JOINT" ? FaIcons.Shield : FaIcons.HardHat;
                var baseRef = b;
                AddBaseListItem(b.baseName, b.branch, locationLabel, b.status, sColor, $"{b.currentPersonnel:N0} / {b.capacity:N0}", b.role, bIcon, false, () => ShowBaseDetails(baseRef, country));
            }

            if (country.defenseFactories != null)
            {
                foreach (var fac in country.defenseFactories)
                {
                    string fBranch = fac.produceType == "Naval Vessels" ? "NAVY" : 
                                     fac.produceType == "Aircraft" ? "AIR FORCE" : "ARMY";
                    
                    if (fBranch == "ARMY") cArmy++;
                    else if (fBranch == "AIR FORCE") cAir++;
                    else if (fBranch == "NAVY") cNavy++;
                    cAll++;

                    if (_currentBranchFilter != "ALL" && fBranch != _currentBranchFilter) continue;
                    
                    if (firstFac == null && firstVisible == null) firstFac = fac;

                    var factoryRef = fac;
                    string status = fac.level > 0 ? "OPERATIONAL" : "UNDER CONSTRUCTION";
                    Color statusColor = fac.level > 0 ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f);
                    AddBaseListItem(fac.factoryName, fBranch, "Defense Facility", status, statusColor, $"{fac.utilizationPct:F0}% Utilized", $"Level {fac.level} Factory", FaIcons.Wrench, false, () => ShowFactoryDetails(factoryRef, country));
                }
            }

            if (country.facilityConstructionOrders != null)
            {
                foreach (var order in country.facilityConstructionOrders)
                {
                    if (order.facilityType == 1) continue; // Skip factories since they might be handled separately or they don't map to bases easily here
                    
                    string branch = order.branch == "AIR" ? "AIR FORCE" : order.branch;
                    
                    if (branch == "ARMY") cArmy++;
                    else if (branch == "AIR FORCE") cAir++;
                    else if (branch == "NAVY") cNavy++;
                    else if (branch == "JOINT") cJoint++;
                    cAll++;

                    if (_currentBranchFilter != "ALL" && branch != _currentBranchFilter) continue;

                    string bIcon = order.branch == "AIR" ? FaIcons.Plane : order.branch == "NAVY" ? FaIcons.Anchor : order.branch == "JOINT" ? FaIcons.Shield : FaIcons.HardHat;
                    string locationLabel = $"{order.regionName}, {GameManager.Instance.playerCountry.countryName}";
                    string role = order.facilityType == 2 ? "Training Academy" : "Base under construction";

                    // For the click handler, we can pass a dummy MilitaryBase to ShowBaseDetails just so it renders something, or disable click.
                    // For now, let's create a temporary MilitaryBase object to display in details panel
                    var tempBase = new MilitaryBase(
                        order.id,
                        order.facilityName,
                        order.regionName,
                        order.branch,
                        order.capacity,
                        role
                    );
                    tempBase.status = "UNDER CONSTRUCTION";
                    tempBase.currentPersonnel = 0;
                    tempBase.upkeepCost = 0f;
                    
                    if (firstVisible == null) firstVisible = tempBase;

                    AddBaseListItem(order.facilityName, order.branch, locationLabel, "UNDER CONSTRUCTION", new Color(1f, 0.67f, 0.25f), $"0 / {order.capacity:N0}", role, bIcon, false, () => ShowBaseDetails(tempBase, country));
                }
            }

            if (_btnFilterAll != null) _btnFilterAll.Query<Label>().Last().text = $"ALL ({cAll})";
            if (_btnFilterArmy != null) _btnFilterArmy.Query<Label>().Last().text = $"ARMY ({cArmy})";
            if (_btnFilterAirForce != null) _btnFilterAirForce.Query<Label>().Last().text = $"AIR FORCE ({cAir})";
            if (_btnFilterNavy != null) _btnFilterNavy.Query<Label>().Last().text = $"NAVY ({cNavy})";
            if (_btnFilterJoint != null) _btnFilterJoint.Query<Label>().Last().text = $"JOINT ({cJoint})";

            // 3. Show first base details by default (auto-select first base)
            if (firstVisible != null)
            {
                ShowBaseDetails(firstVisible, country);
            }
            else if (firstFac != null)
            {
                ShowFactoryDetails(firstFac, country);
            }
            else
            {
                if (_baseDetailsPanel != null) _baseDetailsPanel.style.display = DisplayStyle.None;
                if (_baseDetailsEmptyState != null) _baseDetailsEmptyState.style.display = DisplayStyle.Flex;
            }

            // 4. Global Overview (live)
            int opCount    = country.militaryBases.FindAll(x => x.status == "OPERATIONAL").Count + (country.defenseFactories?.FindAll(f => f.level > 0).Count ?? 0);
            int constrCount = country.militaryBases.FindAll(x => x.status == "UNDER CONSTRUCTION").Count + (country.defenseFactories?.FindAll(f => f.level == 0).Count ?? 0);
            _globalBasesOverviewContainer.Clear();
            AddOverviewRow("Operational", opCount.ToString(), new Color(0.14f, 0.93f, 0.34f));
            AddOverviewRow("Under Construction", constrCount.ToString(), new Color(1f, 0.67f, 0.25f));
            AddOverviewRow("Inactive", "0", Color.grey);

            // 5. Construction & Upgrade queues
            PopulateQueues(country);
            
            Debug.Log("[Bases] PopulateBases finished successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bases] Exception during PopulateBases: {ex}");
            }
        }

        private void PopulateQueues(CountryDataSO country)
        {
            if (_constructionQueueContainer == null || _upgradeQueueContainer == null) return;
            
            _constructionQueueContainer.Clear();
            _upgradeQueueContainer.Clear();
            
            // Build combined construction list: new queue orders + legacy "UNDER CONSTRUCTION" bases
            bool hasConstructionItems = false;

            // New queue orders
            if (country.facilityConstructionOrders != null)
            {
                foreach (var order in country.facilityConstructionOrders)
                {
                    string icon = order.branch == "AIR" ? FaIcons.Plane : order.branch == "NAVY" ? FaIcons.Anchor : order.branch == "JOINT" ? FaIcons.Shield : FaIcons.HardHat;
                    if (order.facilityType == 1) icon = FaIcons.Wrench;
                    AddQueueItem(_constructionQueueContainer, order.facilityName, order.regionName, (int)order.progressPct, $"{order.monthsRemaining} Months", "BUILD", icon);
                    hasConstructionItems = true;
                }
            }

            // Legacy: bases added directly with UNDER CONSTRUCTION status (old save data)
            if (country.militaryBases != null)
            {
                foreach (var b in country.militaryBases)
                {
                    if (b.status != "UNDER CONSTRUCTION") continue;
                    string icon = b.branch == "AIR" ? FaIcons.Plane : b.branch == "NAVY" ? FaIcons.Anchor : b.branch == "JOINT" ? FaIcons.Shield : FaIcons.HardHat;
                    AddQueueItem(_constructionQueueContainer, b.baseName, b.regionName, 20, "In Progress", "BUILD", icon);
                    hasConstructionItems = true;
                }
            }

            // Legacy: factories at level 0 are also under construction
            if (country.defenseFactories != null)
            {
                foreach (var fac in country.defenseFactories)
                {
                    if (fac.level != 0) continue;
                    AddQueueItem(_constructionQueueContainer, fac.factoryName, "Defense Facility", 20, "In Progress", "BUILD", FaIcons.Wrench);
                    hasConstructionItems = true;
                }
            }

            if (!hasConstructionItems)
            {
                _constructionQueueContainer.Add(new Label("No facilities under construction") { style = { fontSize = 11, color = new Color(0.5f, 0.5f, 0.5f), unityTextAlign = TextAnchor.MiddleCenter, marginTop = 16, marginBottom = 16, paddingBottom = 12, paddingTop = 12 } });
            }

            // Upgrade queue
            if (country.facilityUpgradeOrders == null || country.facilityUpgradeOrders.Count == 0)
            {
                _upgradeQueueContainer.Add(new Label("No active upgrades") { style = { fontSize = 11, color = new Color(0.5f, 0.5f, 0.5f), unityTextAlign = TextAnchor.MiddleCenter, marginTop = 16, marginBottom = 16, paddingBottom = 12, paddingTop = 12 } });
            }
            else
            {
                foreach (var order in country.facilityUpgradeOrders)
                {
                    AddQueueItem(_upgradeQueueContainer, order.facilityName, order.upgradeType, (int)order.progressPct, $"{order.monthsRemaining} Months", "UPGRADE", FaIcons.Hammer);
                }
            }
        }

        private void AddBaseListItem(string name, string branch, string location, string status, Color statusColor, string personnel, string role, string icon, bool isActive, System.Action onClick = null)
        {
            var card = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingTop = 12, paddingBottom = 12, paddingLeft = 12, paddingRight = 12, marginBottom = 8, backgroundColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 0.15f) : new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopWidth = isActive ? 2 : 1, borderBottomWidth = isActive ? 2 : 1, borderLeftWidth = isActive ? 2 : 1, borderRightWidth = isActive ? 2 : 1, borderTopColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 1f) : new Color(0.2f, 0.25f, 0.35f, 0.5f), borderBottomColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 1f) : new Color(0.2f, 0.25f, 0.35f, 0.5f), borderLeftColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 1f) : new Color(0.2f, 0.25f, 0.35f, 0.5f), borderRightColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 1f) : new Color(0.2f, 0.25f, 0.35f, 0.5f), borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6 } };
            
            var iconBox = new VisualElement { style = { width = 36, height = 36, justifyContent = Justify.Center, alignItems = Align.Center, marginRight = 10, backgroundColor = new Color(1,1,1,0.05f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
            var iconLabel = new Label(icon) { style = { fontSize = 18, color = Color.white } };
            iconLabel.AddToClassList("fa-icon");
            iconBox.Add(iconLabel);
            card.Add(iconBox);
            
            var mid = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column, justifyContent = Justify.Center } };
            var nameRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            nameRow.Add(new Label(name) { style = { fontSize = 14, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } });
            
            // Branch badge
            string cleanBranch = branch == "AIR" ? "AIR FORCE" : branch;
            Color branchColor = cleanBranch == "AIR FORCE" ? new Color(0.4f, 0.7f, 1f) : cleanBranch == "NAVY" ? new Color(0.1f, 0.4f, 0.9f) : cleanBranch == "JOINT" ? new Color(0.7f, 0.4f, 0.9f) : cleanBranch == "RDF" ? Color.black : new Color(0.3f, 0.6f, 0.3f);
            var badge = new VisualElement { style = { backgroundColor = branchColor, paddingLeft = 4, paddingRight = 4, paddingTop = 2, paddingBottom = 2, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
            badge.Add(new Label(cleanBranch) { style = { fontSize = 10, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            nameRow.Add(badge);
            mid.Add(nameRow);

            mid.Add(new Label(location) { style = { fontSize = 11, color = Color.grey, marginBottom = 6 } });
            
            // Status and Capacity Bar
            var statRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            
            var statusBadge = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, backgroundColor = new Color(statusColor.r, statusColor.g, statusColor.b, 0.15f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(statusColor.r, statusColor.g, statusColor.b, 0.5f), borderBottomColor = new Color(statusColor.r, statusColor.g, statusColor.b, 0.5f), borderLeftColor = new Color(statusColor.r, statusColor.g, statusColor.b, 0.5f), borderRightColor = new Color(statusColor.r, statusColor.g, statusColor.b, 0.5f), borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6, paddingLeft = 6, paddingRight = 6, paddingTop = 2, paddingBottom = 2, marginRight = 10 } };
            statusBadge.Add(new VisualElement { style = { width = 4, height = 4, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, backgroundColor = statusColor, marginRight = 4 } });
            statusBadge.Add(new Label(status) { style = { fontSize = 10, color = statusColor, unityFontStyleAndWeight = FontStyle.Bold } });
            statRow.Add(statusBadge);
            
            // For now, Operational gets a full bar, construction gets a partial bar
            int progressVal = status == "OPERATIONAL" ? 100 : 45; 
            var pBar = new ProgressBar { value = progressVal, highValue = 100, style = { flexGrow = 1, height = 4, marginRight = 8 } };
            pBar.style.backgroundColor = statusColor; // color bar to match status
            statRow.Add(pBar);
            
            mid.Add(statRow);
            card.Add(mid);
            
            var right = new VisualElement { style = { width = 110, flexDirection = FlexDirection.Column, alignItems = Align.FlexEnd, justifyContent = Justify.Center } };
            right.Add(new Label(personnel) { style = { fontSize = 12, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
            
            var roleLabel = new Label(role) { style = { fontSize = 10, color = Color.grey, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap } };
            right.Add(roleLabel);
            card.Add(right);
            
            // Click to show details
            card.RegisterCallback<ClickEvent>(e => {
                if (onClick != null)
                {
                    onClick();
                    return;
                }

                if (_baseDetailsPanel != null) _baseDetailsPanel.style.display = DisplayStyle.Flex;
                if (_baseDetailsEmptyState != null) _baseDetailsEmptyState.style.display = DisplayStyle.None;
                
                if (_detailBaseName != null) _detailBaseName.text = name.ToUpper();
                if (_detailBaseIcon != null) _detailBaseIcon.text = icon;
                if (_detailBaseStatus != null) { _detailBaseStatus.text = status; _detailBaseStatus.style.color = statusColor; }
                
                // Dynamic Banner
                if (_baseBannerImage != null)
                {
                    string imgName = name.Contains("Air") ? "AirBase" : name.Contains("Nav") ? "NavalBase" : name.Contains("Joint") ? "JointBase" : "ArmyBase";
                    var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>($"EquipmentPhotos/{imgName}");
                    if (tex != null)
                    {
                        _baseBannerImage.style.backgroundImage = new StyleBackground(tex);
                        #pragma warning disable CS0618
                        _baseBannerImage.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
                        #pragma warning restore CS0618
                    }
                }
            });

            _basesListContainer.Add(card);
        }

        private void AddBaseInfoRow(string label, string value, string icon)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            var iconBox = new VisualElement { style = { width = 20, alignItems = Align.Center } };
            var iconLabel = new Label(icon) { style = { fontSize = 10, color = Color.grey } };
            iconLabel.AddToClassList("fa-icon");
            iconBox.Add(iconLabel);
            row.Add(iconBox);
            
            row.Add(new Label(label) { style = { fontSize = 9, color = Color.grey, width = 80 } });
            row.Add(new Label(value) { style = { fontSize = 9, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            _baseInfoListContainer.Add(row);
        }

        private void AddCapabilityRow(string label, float val, string icon)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
            
            var leftGroup = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = 110 } };
            var iconLabel = new Label(icon) { style = { fontSize = 10, color = Color.grey, marginRight = 6, width = 16, unityTextAlign = TextAnchor.MiddleCenter } };
            iconLabel.AddToClassList("fa-icon");
            leftGroup.Add(iconLabel);
            leftGroup.Add(new Label(label) { style = { fontSize = 9, color = Color.grey } });
            row.Add(leftGroup);
            
            var pBar = new ProgressBar { value = val, highValue = 100, style = { width = 80, height = 4 } };
            pBar.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f);
            row.Add(pBar);
            row.Add(new Label($"{val:F0}%") { style = { fontSize = 9, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, width = 30, unityTextAlign = TextAnchor.MiddleRight } });
            _baseCapabilitiesContainer.Add(row);
        }

        private void AddAssignedUnit(string name, string personnel, string icon)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var iconBox = new VisualElement { style = { width = 20, height = 20, backgroundColor = new Color(1,1,1,0.05f), borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2, justifyContent = Justify.Center, alignItems = Align.Center, marginRight = 8 } };
            var iconLabel = new Label(icon) { style = { fontSize = 10, color = Color.white } };
            iconLabel.AddToClassList("fa-icon");
            iconBox.Add(iconLabel);
            left.Add(iconBox);
            left.Add(new Label(name) { style = { fontSize = 10, color = Color.white } });
            row.Add(left);
            row.Add(new Label(personnel) { style = { fontSize = 10, color = new Color(0.14f, 0.93f, 0.34f), unityFontStyleAndWeight = FontStyle.Bold } });
            _assignedUnitsContainer.Add(row);
        }

        private void AddActivityIcon(string label, string val, string icon)
        {
            var col = new VisualElement { style = { flexDirection = FlexDirection.Column, alignItems = Align.Center } };
            var iconLabel = new Label(icon) { style = { fontSize = 16, color = Color.grey, marginBottom = 6 } };
            iconLabel.AddToClassList("fa-icon");
            col.Add(iconLabel);
            col.Add(new Label(label) { style = { fontSize = 8, color = Color.grey, marginBottom = 2 } });
            col.Add(new Label(val) { style = { fontSize = 12, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            _baseActivityContainer.Add(col);
        }

        private void AddOverviewRow(string label, string val, Color dotColor)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, height = 24, marginBottom = 10 } };
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            left.Add(new VisualElement { style = { width = 8, height = 8, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = dotColor, marginRight = 8 } });
            left.Add(new Label(label) { style = { fontSize = 10, color = Color.white } });
            row.Add(left);
            row.Add(new Label(val) { style = { fontSize = 10, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            _globalBasesOverviewContainer.Add(row);
        }

        private void ShowBaseDetails(MilitaryBase b, CountryDataSO country)
        {
            if (_baseDetailsPanel != null) _baseDetailsPanel.style.display = DisplayStyle.Flex;
            if (_baseDetailsEmptyState != null) _baseDetailsEmptyState.style.display = DisplayStyle.None;
            
            _currentSelectedBase = b;
            _currentSelectedFactory = null;

            // Reset Titles for Bases vs Training Academies
            bool isTraining = b.role == "Training Academy";
            if (_titleBaseEstablished != null) _titleBaseEstablished.text = "Established";
            if (_titleBaseGarrison != null) _titleBaseGarrison.text = isTraining ? "Class Capacity" : "Garrison Capacity";
            if (_titleBasePersonnel != null) _titleBasePersonnel.text = isTraining ? "Active Trainees" : "Current Personnel";
            if (_titleBaseSize != null) 
            {
                if (isTraining) _titleBaseSize.text = "Base Size";
                else if (b.branch == "AIR") _titleBaseSize.text = "Hangar Capacity";
                else if (b.branch == "NAVY") _titleBaseSize.text = "Dock Capacity";
                else _titleBaseSize.text = "Vehicle Capacity";
            }
            if (_titleBaseThreat != null) _titleBaseThreat.text = isTraining ? "Graduation Rate" : "Threat Level";

            RegionDataSO region = country.regions?.Find(r => r.regionName == b.regionName);
            string locationLabel = region != null ? $"{region.regionName}, {GameManager.Instance.playerCountry.countryName}" : b.regionName;
            string threatLevel   = GetThreatLevel(region);
            Color  threatColor   = GetThreatColor(threatLevel);
            string branchIcon    = b.branch == "AIR" ? FaIcons.Plane : b.branch == "NAVY" ? FaIcons.Anchor : b.branch == "JOINT" ? FaIcons.Shield : FaIcons.HardHat;

            if (_detailBaseIcon != null)       _detailBaseIcon.text = branchIcon;
            if (_detailBaseName != null)       _detailBaseName.text = b.baseName.ToUpper();
            if (_detailBaseLocation != null)   _detailBaseLocation.text = locationLabel;
            if (_detailBaseStatus != null) { _detailBaseStatus.text = b.status; _detailBaseStatus.style.color = b.status == "OPERATIONAL" ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f); }
            if (_detailBaseEstablished != null) _detailBaseEstablished.text = $"Est. {b.establishedYear}";

            // Dynamic Banner
            if (_baseBannerImage != null)
            {
                string imgName = b.baseName.Contains("Air") ? "AirBase" : b.baseName.Contains("Nav") ? "NavalBase" : b.baseName.Contains("Joint") ? "JointBase" : "ArmyBase";
                var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>($"EquipmentPhotos/{imgName}");
                if (tex != null)
                {
                    _baseBannerImage.style.backgroundImage = new StyleBackground(tex);
                    #pragma warning disable CS0618
                    _baseBannerImage.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
                    #pragma warning restore CS0618
                }
            }
            if (_detailBaseGarrison != null)   _detailBaseGarrison.text = $"{b.capacity:N0}";
            if (_detailBasePersonnel != null)
            {
                int pct = b.capacity > 0 ? Mathf.RoundToInt((b.currentPersonnel / (float)b.capacity) * 100f) : 0;
                _detailBasePersonnel.text = $"{b.currentPersonnel:N0} ({pct}%)";
            }
            if (_detailBaseSize != null)
            {
                if (isTraining) _detailBaseSize.text = $"{b.sizeHa} ha";
                else if (b.branch == "AIR") _detailBaseSize.text = $"{b.capacity / 20:N0} Aircraft";
                else if (b.branch == "NAVY") _detailBaseSize.text = $"{b.capacity / 50:N0} Ships";
                else _detailBaseSize.text = $"{b.capacity / 2:N0} Vehicles";
            }
            if (_detailBaseThreat != null)
            {
                _detailBaseThreat.text = isTraining ? "95%" : threatLevel;
                _detailBaseThreat.style.color = isTraining ? new Color(0.14f, 0.93f, 0.34f) : threatColor;
            }

            // Base Information panel
            _baseInfoListContainer.Clear();
            AddBaseInfoRow("Location", locationLabel, FaIcons.Globe);
            AddBaseInfoRow("Type", $"{b.branch} Base", FaIcons.Building);
            if (isTraining)
            {
                AddBaseInfoRow("Curriculum", "Standard Warfare", FaIcons.Scroll);
                AddBaseInfoRow("Course Length", "6 Months", FaIcons.Clock);
                AddBaseInfoRow("Instructor Ratio", "1:15", FaIcons.UserSecret);
            }
            else if (region != null)
            {
                AddBaseInfoRow("Stability", $"{region.stability:F0}%", FaIcons.Shield);
                AddBaseInfoRow("Security", $"{region.security:F0}%", FaIcons.UserSecret);
                AddBaseInfoRow("Insurgency", $"{region.insurgencyLevel:F0}%", FaIcons.Shield);
            }
            AddBaseInfoRow("Monthly Upkeep", $"₦{b.upkeepCost:F1}B", FaIcons.Coin);

            // Capabilities — from live data
            _baseCapabilitiesContainer.Clear();
            if (b.capabilities != null && b.capabilities.Count > 0)
            {
                foreach (var cap in b.capabilities)
                    AddCapabilityRow(cap.name, cap.level, cap.icon);
            }
            else if (isTraining)
            {
                AddCapabilityRow("Basic Training", 3, FaIcons.GraduationCap);
                AddCapabilityRow("Officer School", 1, FaIcons.GraduationCap);
            }

            // Assigned Units — from live data
            _assignedUnitsContainer.Clear();
            if (b.assignedUnits != null && b.assignedUnits.Count > 0)
            {
                foreach (var u in b.assignedUnits)
                    AddAssignedUnit(u.unitName, $"{u.personnel:N0}", u.icon);
            }
            else if (isTraining)
            {
                AddAssignedUnit("Current Class", $"{b.currentPersonnel:N0}", FaIcons.GraduationCap);
                AddAssignedUnit("Instructors", "120", FaIcons.UserSecret);
            }

            // Base Activity
            _baseActivityContainer.Clear();
            if (isTraining)
            {
                AddActivityIcon("Trainees",  $"{b.currentPersonnel:N0}",  FaIcons.GraduationCap);
                AddActivityIcon("Capacity",   $"{b.capacity:N0}",          FaIcons.Building);
                AddActivityIcon("Upkeep/Mo",  $"₦{b.upkeepCost:F1}B",      FaIcons.Coins);
                AddActivityIcon("Graduated", "4,250", FaIcons.CircleCheck);
                AddActivityIcon("Dropouts", "112", FaIcons.TriangleExclaim);
            }
            else
            {
                AddActivityIcon("Personnel",  $"{b.currentPersonnel:N0}",  FaIcons.UserSecret);
                AddActivityIcon("Capacity",   $"{b.capacity:N0}",          FaIcons.Building);
                AddActivityIcon("Upkeep/Mo",  $"₦{b.upkeepCost:F1}B",      FaIcons.Coins);
                if (region != null) AddActivityIcon("Region Stability", $"{region.stability:F0}%", FaIcons.Shield);
                AddActivityIcon("Transfers", "0", FaIcons.Clock);
            }
            
            // Revert icon color (might have been changed by factory)
            if (_titleBaseGarrison?.parent?.Q<Label>(className: "fa-icon") != null)
                _titleBaseGarrison.parent.Q<Label>(className: "fa-icon").style.color = new Color(0.64f, 0.69f, 0.73f);
        }

        private void ShowFactoryDetails(DefenseFactory fac, CountryDataSO country)
        {
            if (_baseDetailsPanel != null) _baseDetailsPanel.style.display = DisplayStyle.Flex;
            if (_baseDetailsEmptyState != null) _baseDetailsEmptyState.style.display = DisplayStyle.None;

            _currentSelectedBase = null;
            _currentSelectedFactory = fac;

            // Set Factory Titles
            if (_titleBaseEstablished != null) _titleBaseEstablished.text = "Established";
            if (_titleBaseGarrison != null) _titleBaseGarrison.text = "Factory Level";
            if (_titleBasePersonnel != null) _titleBasePersonnel.text = "Utilization";
            if (_titleBaseSize != null) _titleBaseSize.text = "Monthly Output";
            if (_titleBaseThreat != null) _titleBaseThreat.text = "Efficiency";

            // Update Icon Colors for Factory
            if (_titleBaseGarrison?.parent?.Q<Label>(className: "fa-icon") != null)
                _titleBaseGarrison.parent.Q<Label>(className: "fa-icon").style.color = new Color(1f, 0.67f, 0.25f);

            if (_detailBaseIcon != null)       _detailBaseIcon.text = FaIcons.Wrench;
            if (_detailBaseName != null)       _detailBaseName.text = fac.factoryName.ToUpper();
            if (_detailBaseLocation != null)   _detailBaseLocation.text = "National Industrial Zone";
            
            string status = fac.level > 0 ? "OPERATIONAL" : "UNDER CONSTRUCTION";
            if (_detailBaseStatus != null) { _detailBaseStatus.text = status; _detailBaseStatus.style.color = fac.level > 0 ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f); }
            if (_detailBaseEstablished != null) _detailBaseEstablished.text = $"Est. 2024";

            // Dynamic Banner
            if (_baseBannerImage != null)
            {
                var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>($"EquipmentPhotos/FactoryBanner");
                if (tex != null)
                {
                    _baseBannerImage.style.backgroundImage = new StyleBackground(tex);
                }
            }
            
            if (_detailBaseGarrison != null)   _detailBaseGarrison.text = $"Level {fac.level}";
            if (_detailBasePersonnel != null)
            {
                _detailBasePersonnel.text = $"{fac.utilizationPct:F0}%";
                _detailBasePersonnel.style.color = new Color(1f, 0.67f, 0.25f);
            }
            if (_detailBaseSize != null)   _detailBaseSize.text = $"{fac.level * 10:N0} Units";
            if (_detailBaseThreat != null)
            {
                _detailBaseThreat.text = "Optimal";
                _detailBaseThreat.style.color = new Color(0.14f, 0.93f, 0.34f);
            }

            // Factory Information panel
            _baseInfoListContainer.Clear();
            AddBaseInfoRow("Location", "National Industrial Zone", FaIcons.Globe);
            AddBaseInfoRow("Specialization", fac.produceType, FaIcons.Wrench);
            AddBaseInfoRow("Monthly Upkeep", $"₦{fac.level * 2f:F1}B", FaIcons.Coin);

            _baseCapabilitiesContainer.Clear();
            AddCapabilityRow($"Produces {fac.produceType}", fac.level, FaIcons.Wrench);

            _assignedUnitsContainer.Clear();
            AddAssignedUnit($"{fac.produceType} Production Line", "ACTIVE", FaIcons.IndustryAlt);

            _baseActivityContainer.Clear();
            AddActivityIcon("Produced", $"{fac.level * 10:N0}", FaIcons.Wrench);
            AddActivityIcon("Delivered", "0", FaIcons.CircleCheck);
            AddActivityIcon("Issues", "None", FaIcons.Shield);
        }

        private void OnUpgradeClicked()
        {
            if (_upgradeModalOverlay == null) return;

            var country = Core.GameManager.Instance?.playerCountry;

            if (_currentSelectedBase != null)
            {
                // Block if already queued
                bool alreadyQueued = country?.facilityUpgradeOrders?.Exists(o => o.facilityId == _currentSelectedBase.id) ?? false;
                if (alreadyQueued)
                {
                    Debug.Log($"[Bases] {_currentSelectedBase.baseName} already has a pending upgrade.");
                    if (_labelUpgradeName != null) _labelUpgradeName.text = _currentSelectedBase.baseName;
                    if (_labelUpgradeDesc != null) _labelUpgradeDesc.text = $"⚠ {_currentSelectedBase.baseName} is already queued for an upgrade. Wait for the current upgrade to complete before starting another.";
                    if (_labelUpgradeCost != null) _labelUpgradeCost.text = "—";
                    if (_labelUpgradeBenefitTitle != null) _labelUpgradeBenefitTitle.text = "STATUS";
                    if (_labelUpgradeBenefit != null) _labelUpgradeBenefit.text = "Upgrade In Progress";
                    if (_labelUpgradeTime != null) _labelUpgradeTime.text = "In Queue";
                    if (_btnConfirmUpgrade != null) _btnConfirmUpgrade.SetEnabled(false);
                    _upgradeModalOverlay.style.display = DisplayStyle.Flex;
                    return;
                }

                if (_btnConfirmUpgrade != null) _btnConfirmUpgrade.SetEnabled(true);
                if (_labelUpgradeName != null) _labelUpgradeName.text = _currentSelectedBase.baseName;
                if (_labelUpgradeDesc != null) _labelUpgradeDesc.text = "Expanding this military base increases its maximum personnel capacity by 500, allowing it to support more assigned units and defensive structures. Note: Upkeep costs will marginally increase.";
                if (_labelUpgradeCost != null) _labelUpgradeCost.text = "₦20.0B";
                if (_labelUpgradeBenefitTitle != null) _labelUpgradeBenefitTitle.text = "NEW CAPACITY";
                if (_labelUpgradeBenefit != null) _labelUpgradeBenefit.text = $"+500 Personnel";
                if (_labelUpgradeTime != null) _labelUpgradeTime.text = "~90 Days";
            }
            else if (_currentSelectedFactory != null)
            {
                if (_currentSelectedFactory.level >= 3)
                {
                    Debug.Log("Factory is already at maximum level.");
                    return;
                }

                // Block if already queued
                bool alreadyQueued = country?.facilityUpgradeOrders?.Exists(o => o.facilityId == _currentSelectedFactory.id) ?? false;
                if (alreadyQueued)
                {
                    Debug.Log($"[Bases] {_currentSelectedFactory.factoryName} already has a pending upgrade.");
                    if (_labelUpgradeName != null) _labelUpgradeName.text = _currentSelectedFactory.factoryName;
                    if (_labelUpgradeDesc != null) _labelUpgradeDesc.text = $"⚠ {_currentSelectedFactory.factoryName} is already queued for an upgrade. Wait for the current upgrade to complete before starting another.";
                    if (_labelUpgradeCost != null) _labelUpgradeCost.text = "—";
                    if (_labelUpgradeBenefitTitle != null) _labelUpgradeBenefitTitle.text = "STATUS";
                    if (_labelUpgradeBenefit != null) _labelUpgradeBenefit.text = "Upgrade In Progress";
                    if (_labelUpgradeTime != null) _labelUpgradeTime.text = "In Queue";
                    if (_btnConfirmUpgrade != null) _btnConfirmUpgrade.SetEnabled(false);
                    _upgradeModalOverlay.style.display = DisplayStyle.Flex;
                    return;
                }

                if (_btnConfirmUpgrade != null) _btnConfirmUpgrade.SetEnabled(true);
                if (_labelUpgradeName != null) _labelUpgradeName.text = _currentSelectedFactory.factoryName;
                if (_labelUpgradeDesc != null) _labelUpgradeDesc.text = "Upgrading this defense factory drastically increases its monthly production output for heavy equipment and arms. Note: Monthly upkeep costs will increase proportionally.";
                if (_labelUpgradeCost != null) _labelUpgradeCost.text = "₦35.0B";
                if (_labelUpgradeBenefitTitle != null) _labelUpgradeBenefitTitle.text = "NEW CAPACITY";
                if (_labelUpgradeBenefit != null) _labelUpgradeBenefit.text = $"Level {_currentSelectedFactory.level + 1}";
                if (_labelUpgradeTime != null) _labelUpgradeTime.text = "~180 Days";
            }
            else
            {
                return;
            }
            
            _upgradeModalOverlay.style.display = DisplayStyle.Flex;
        }

        private void OnConfirmUpgradeClicked()
        {
            var country = Core.GameManager.Instance?.playerCountry;
            if (country == null) return;

            if (country.facilityUpgradeOrders == null) country.facilityUpgradeOrders = new List<StateCraft.Data.FacilityUpgradeOrder>();

            // Hard guard: prevent duplicate queue entries
            string targetId = _currentSelectedBase?.id ?? _currentSelectedFactory?.id;
            if (!string.IsNullOrEmpty(targetId) && country.facilityUpgradeOrders.Exists(o => o.facilityId == targetId))
            {
                Debug.LogWarning($"[Bases] Upgrade already queued for facility {targetId}. Ignoring.");
                if (_upgradeModalOverlay != null) _upgradeModalOverlay.style.display = DisplayStyle.None;
                return;
            }
            if (_currentSelectedBase != null)
            {
                float costNGN = 20.0f; // 20B NGN to upgrade a base
                float costUSD = costNGN / country.exchangeRate;
                
                if (country.treasury >= costUSD)
                {
                    country.treasury -= costUSD;

                    var order = new StateCraft.Data.FacilityUpgradeOrder
                    {
                        id = $"UPG_{System.Guid.NewGuid().ToString().Substring(0,5)}",
                        facilityId = _currentSelectedBase.id,
                        facilityName = _currentSelectedBase.baseName,
                        upgradeType = "Capacity Increase",
                        monthsTotal = 3,
                        monthsRemaining = 3
                    };
                    country.facilityUpgradeOrders.Add(order);

                    if (Core.GameManager.Instance != null && Core.GameManager.Instance.playerCountry != null)
                    {
                        ShowBaseDetails(_currentSelectedBase, Core.GameManager.Instance.playerCountry);
                        PopulateQueues(Core.GameManager.Instance.playerCountry); // update the visual queues
                    }
                    
                    if (_upgradeModalOverlay != null) _upgradeModalOverlay.style.display = DisplayStyle.None;
                    Debug.Log($"Queued Upgrade for Base: {_currentSelectedBase.baseName}");
                }
                else
                {
                    Debug.LogWarning("Not enough treasury to upgrade base!");
                }
            }
            else if (_currentSelectedFactory != null)
            {
                float costNGN = 35.0f; // 35B NGN to upgrade a factory
                float costUSD = costNGN / country.exchangeRate;
                
                if (_currentSelectedFactory.level < 3)
                {
                    if (country.treasury >= costUSD)
                    {
                        country.treasury -= costUSD;
                        
                        var order = new StateCraft.Data.FacilityUpgradeOrder
                        {
                            id = $"UPG_{System.Guid.NewGuid().ToString().Substring(0,5)}",
                            facilityId = _currentSelectedFactory.id,
                            facilityName = _currentSelectedFactory.factoryName,
                            upgradeType = "Level Up",
                            monthsTotal = 6,
                            monthsRemaining = 6
                        };
                        country.facilityUpgradeOrders.Add(order);

                        if (Core.GameManager.Instance != null && Core.GameManager.Instance.playerCountry != null)
                            ShowFactoryDetails(_currentSelectedFactory, Core.GameManager.Instance.playerCountry);
                            PopulateQueues(Core.GameManager.Instance.playerCountry);
                        
                        if (_upgradeModalOverlay != null) _upgradeModalOverlay.style.display = DisplayStyle.None;
                        Debug.Log($"Queued Upgrade for Factory: {_currentSelectedFactory.factoryName}");
                    }
                    else
                    {
                        Debug.LogWarning("Not enough treasury to upgrade factory!");
                    }
                }
            }
        }

        private string GetThreatLevel(RegionDataSO region)
        {
            if (region == null) return "UNKNOWN";
            if (region.insurgencyLevel > 60 || region.security < 30) return "HIGH";
            if (region.insurgencyLevel > 30 || region.security < 60) return "MEDIUM";
            return "LOW";
        }

        private Color GetThreatColor(string level) => level switch
        {
            "HIGH"   => new Color(1f, 0.3f, 0.3f),
            "MEDIUM" => new Color(1f, 0.67f, 0.25f),
            _        => new Color(0.14f, 0.93f, 0.34f)
        };

        private void EnsureBasesData(CountryDataSO country)
        {
            if (country.militaryBases == null)
                country.militaryBases = new List<MilitaryBase>();

            // Check if existing bases belong to a different country (e.g. leftover serialized data)
            if (country.militaryBases.Count > 0 && country.regions != null && country.regions.Count > 0)
            {
                bool baseMatchesRegion = false;
                foreach (var region in country.regions)
                {
                    if (country.militaryBases[0].regionName == region.regionName)
                    {
                        baseMatchesRegion = true;
                        break;
                    }
                }
                
                if (!baseMatchesRegion)
                {
                    Debug.Log($"[Bases] Existing bases do not match {country.countryName}'s regions. Clearing and regenerating...");
                    country.militaryBases.Clear();
                }
            }

            if (country.militaryBases.Count == 0)
            {
                // Generate dynamic bases based on the country's actual regions
                if (country.regions != null && country.regions.Count > 0)
                {
                    int idCounter = 1;
                    
                    // Base 1: National HQ in the first region (often capital)
                    country.militaryBases.Add(new MilitaryBase($"BASE_{idCounter++}", $"{country.regions[0].regionName} Defense HQ", country.regions[0].regionName, "JOINT", 3000, "National Defense HQ"));
                    
                    // Create bases distributed among other regions
                    string[] branches = { "ARMY", "AIR", "NAVY", "ARMY" };
                    string[] roles = { "Training & Logistics", "Air Superiority", "Maritime Operations", "Armored Division" };
                    
                    for (int i = 1; i < Mathf.Min(country.regions.Count, 6); i++)
                    {
                        int branchIdx = (i - 1) % branches.Length;
                        string branch = branches[branchIdx];
                        string role = roles[branchIdx];
                        
                        // Default to JOINT if landlocked and NAVY is selected, but for simplicity let's just use it
                        string baseName = branch == "AIR" ? $"{country.regions[i].regionName} Air Base" :
                                          branch == "NAVY" ? $"{country.regions[i].regionName} Naval Base" :
                                          $"{country.regions[i].regionName} Army Garrison";
                                          
                        country.militaryBases.Add(new MilitaryBase($"BASE_{idCounter++}", baseName, country.regions[i].regionName, branch, UnityEngine.Random.Range(1500, 2500), role));
                    }
                    
                    // Add one under construction if they have enough regions
                    if (country.regions.Count > 2)
                    {
                        var bUnderConst = new MilitaryBase($"BASE_{idCounter++}", $"{country.regions[country.regions.Count - 1].regionName} Forward Op Base", country.regions[country.regions.Count - 1].regionName, "ARMY", 800, "Border Security");
                        bUnderConst.status = "UNDER CONSTRUCTION";
                        country.militaryBases.Add(bUnderConst);
                    }
                }
                else
                {
                    // Fallback if no regions defined
                    country.militaryBases.Add(new MilitaryBase("BASE_1", "National Defense HQ", "Capital", "JOINT", 3000, "National Defense HQ"));
                    country.militaryBases.Add(new MilitaryBase("BASE_2", "First Army Base", "Central", "ARMY", 2500, "Training & Logistics"));
                }
            }
            
            // Ensure any existing bases have the newly added fields populated
            foreach (var b in country.militaryBases)
            {
                if (b.establishedYear == 0) b.establishedYear = 2010 + UnityEngine.Random.Range(0, 10);
                if (b.sizeHa == 0) b.sizeHa = b.capacity / 6;
                
                if (b.capabilities == null || b.capabilities.Count == 0)
                {
                    b.capabilities = new List<BaseCapability> {
                        new BaseCapability("Command & Control", UnityEngine.Random.Range(50, 100), FaIcons.Tower),
                        new BaseCapability("Logistics Support", UnityEngine.Random.Range(40, 100), FaIcons.Road),
                        new BaseCapability("Maintenance", UnityEngine.Random.Range(40, 100), FaIcons.Wrench),
                        new BaseCapability("Power Supply", UnityEngine.Random.Range(60, 100), FaIcons.Bolt),
                        new BaseCapability("Defenses", UnityEngine.Random.Range(30, 90), FaIcons.Shield)
                    };
                }
                
                if (b.assignedUnits == null || b.assignedUnits.Count == 0)
                {
                    b.assignedUnits = new List<AssignedUnit> {
                        new AssignedUnit($"{b.branch} Command Group", (int)(b.currentPersonnel * 0.4f), FaIcons.Shield),
                        new AssignedUnit("Support Battalion", (int)(b.currentPersonnel * 0.3f), FaIcons.Wrench),
                        new AssignedUnit("Logistics Unit", (int)(b.currentPersonnel * 0.3f), FaIcons.Road)
                    };
                }
            }
        }

        private void AddQueueItem(VisualElement container, string name, string subtitle, int progress, string eta, string cost, string icon)
        {
            var card = new VisualElement { style = { flexDirection = FlexDirection.Column, backgroundColor = rgba(255,255,255,0.02f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = rgba(255,255,255,0.05f), borderBottomColor = rgba(255,255,255,0.05f), borderLeftColor = rgba(255,255,255,0.05f), borderRightColor = rgba(255,255,255,0.05f), paddingTop = 16, paddingBottom = 16, paddingLeft = 16, paddingRight = 16, marginBottom = 12, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
            var top = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 12 } };
            var iconLabel = new Label(icon) { style = { fontSize = 16, color = Color.grey, marginRight = 10 } };
            iconLabel.AddToClassList("fa-icon");
            top.Add(iconLabel);
            var mid = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            mid.Add(new Label(name) { style = { fontSize = 10, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            mid.Add(new Label(subtitle) { style = { fontSize = 9, color = Color.grey } });
            top.Add(mid);
            card.Add(top);
            
            var progRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            var pBar = new ProgressBar { value = progress, highValue = 100, style = { flexGrow = 1, height = 4, marginRight = 8 } };
            pBar.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f);
            progRow.Add(pBar);
            progRow.Add(new Label($"{progress:F0}%") { style = { fontSize = 9, color = Color.white } });
            card.Add(progRow);
            
            var botRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            botRow.Add(new Label($"ETA: {eta}") { style = { fontSize = 9, color = Color.grey } });
            
            var costBox = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            costBox.Add(new Label(cost) { style = { fontSize = 9, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } });
            costBox.Add(new Label("×") { style = { fontSize = 10, color = Color.grey } });
            botRow.Add(costBox);
            
            card.Add(botRow);
            container.Add(card);
        }
        
        private void OnConstructNewBaseClicked()
        {
            Debug.Log("[Bases] OnConstructNewBaseClicked triggered!");
            var country = GameManager.Instance?.playerCountry;
            if (country == null || country.regions == null) 
            {
                Debug.LogError("[Bases] Country or Regions is null!");
                return;
            }

            // Allow players to build in any region they control, even if a base already exists there
            _availableConstructRegions = country.regions;

            if (_availableConstructRegions == null || _availableConstructRegions.Count == 0)
            {
                Debug.Log("[Bases] No regions controlled. Cannot build a base.");
                return;
            }

            if (_dropdownConstructRegion != null)
            {
                var choices = new List<string>();
                foreach(var r in _availableConstructRegions) choices.Add(r.regionName);
                _dropdownConstructRegion.choices = choices;
                _dropdownConstructRegion.value = choices[0];
            }
            else
            {
                Debug.LogError("[Bases] _dropdownConstructRegion is null!");
            }

            Debug.Log($"[Bases] Opening modal. Modal is null? {_constructModalOverlay == null}");
            SetConstructType(0);
            if (_constructModalOverlay != null) _constructModalOverlay.style.display = DisplayStyle.Flex;
        }

        private void SetConstructType(int facilityType)
        {
            _constructFacilityType = facilityType;
            
            // Failsafe lazy-binding in case of hot-reload without BindBasesElements re-run
            if (_labelConstructCost == null && _constructModalOverlay != null)
            {
                _labelConstructCost = _constructModalOverlay.Q<Label>("LabelConstructCost");
                _labelConstructCapacityTitle = _constructModalOverlay.Q<Label>("LabelConstructCapacityTitle");
                _labelConstructCapacity = _constructModalOverlay.Q<Label>("LabelConstructCapacity");
                _labelConstructTime = _constructModalOverlay.Q<Label>("LabelConstructTime");
            }

            if (_btnTypeBase != null)
            {
                _btnTypeBase.style.backgroundColor = facilityType == 0 ? rgba(36, 238, 87, 0.2f) : rgba(255,255,255,0.05f);
                _btnTypeBase.style.color = facilityType == 0 ? Color.white : Color.grey;
            }
            if (_btnTypeFactory != null)
            {
                _btnTypeFactory.style.backgroundColor = facilityType == 1 ? rgba(36, 238, 87, 0.2f) : rgba(255,255,255,0.05f);
                _btnTypeFactory.style.color = facilityType == 1 ? Color.white : Color.grey;
            }
            if (_btnTypeTraining != null)
            {
                _btnTypeTraining.style.backgroundColor = facilityType == 2 ? rgba(36, 238, 87, 0.2f) : rgba(255,255,255,0.05f);
                _btnTypeTraining.style.color = facilityType == 2 ? Color.white : Color.grey;
            }

            if (_containerConstructBranch != null)
            {
                if (facilityType == 1) // Defense Factory
                {
                    _containerConstructBranch.style.display = DisplayStyle.None;
                }
                else // Military Base (0) or Training Academy (2)
                {
                    _containerConstructBranch.style.display = DisplayStyle.Flex;
                    if (_dropdownConstructBranch != null)
                    {
                        _dropdownConstructBranch.choices = new List<string> { "ARMY", "NAVY", "AIR FORCE", "RDF" };
                        if (string.IsNullOrEmpty(_dropdownConstructBranch.value))
                            _dropdownConstructBranch.value = "ARMY";
                    }
                }
            }

            if (_inputConstructName != null)
            {
                _inputConstructName.value = facilityType == 1 ? "New Defense Plant" : 
                                            facilityType == 2 ? "New Training Academy" : "New Forward Base";
            }

            UpdateConstructStatsUI();
        }

        private void UpdateConstructStatsUI()
        {
            float cost;
            int capacity;
            string timeStr;
            GetConstructStats(_constructFacilityType, _dropdownConstructBranch?.value ?? "ARMY", out cost, out capacity, out timeStr);

            if (_labelConstructCost != null) _labelConstructCost.text = $"₦{cost:N1}B";
            
            if (_labelConstructCapacityTitle != null)
                _labelConstructCapacityTitle.text = _constructFacilityType == 1 ? "EXPECTED OUTPUT" : "EXPECTED CAPACITY";

            if (_labelConstructCapacity != null)
                _labelConstructCapacity.text = _constructFacilityType == 1 ? $"+{capacity} Eqp/mo" : $"+{capacity:N0}";

            if (_labelConstructTime != null) _labelConstructTime.text = timeStr;
        }

        private void GetConstructStats(int type, string branch, out float cost, out int capacity, out string timeStr)
        {
            if (type == 1) // Defense Factory
            {
                cost = 120.0f;
                capacity = 100;
                timeStr = "~365 Days";
                return;
            }
            
            if (type == 0) // Military Base
            {
                cost = 50.0f;
                capacity = 1000;
                timeStr = "~180 Days";
                if (branch == "RDF") { cost = 80.0f; capacity = 300; }
                else if (branch == "AIR FORCE") { cost = 75.0f; capacity = 500; }
            }
            else // Training Academy (Type 2)
            {
                cost = 35.0f;
                capacity = 500;
                timeStr = "~120 Days";
                if (branch == "RDF") { cost = 55.0f; capacity = 150; }
                else if (branch == "AIR FORCE") { cost = 50.0f; capacity = 250; }
            }
        }

        private void OnConfirmConstructClicked()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            string selectedRegName = _dropdownConstructRegion?.value;
            string selBranch = _dropdownConstructBranch?.value;
            string selName = _inputConstructName?.value;

            string cleanBranch = selBranch == "AIR FORCE" ? "AIR" : selBranch;
            if (_constructFacilityType == 1) cleanBranch = "Not Applicable"; // Defense factory doesn't use branch

            if (country.facilityConstructionOrders == null) country.facilityConstructionOrders = new List<StateCraft.Data.FacilityConstructionOrder>();

            float costNGN;
            int capacity;
            string timeStr;
            GetConstructStats(_constructFacilityType, selBranch, out costNGN, out capacity, out timeStr);
            
            float costUSD = costNGN / country.exchangeRate;
            if (country.treasury < costUSD)
            {
                Debug.LogWarning("Not enough treasury to construct facility!");
                return;
            }
            country.treasury -= costUSD;

            var order = new StateCraft.Data.FacilityConstructionOrder
            {
                id = $"CON_{System.Guid.NewGuid().ToString().Substring(0,5)}",
                facilityName = string.IsNullOrEmpty(selName) ? $"{selectedRegName} " + (_constructFacilityType == 1 ? "Weapons Plant" : _constructFacilityType == 2 ? "Academy" : "Facility") : selName,
                facilityType = _constructFacilityType,
                regionName = selectedRegName,
                branch = cleanBranch,
                specialization = "None",
                capacity = capacity,
                monthsTotal = _constructFacilityType == 1 ? 12 : _constructFacilityType == 2 ? 4 : 6,
                monthsRemaining = _constructFacilityType == 1 ? 12 : _constructFacilityType == 2 ? 4 : 6
            };
            country.facilityConstructionOrders.Add(order);

            if (_constructModalOverlay != null) _constructModalOverlay.style.display = DisplayStyle.None;
            PopulateBases(country);
        }
        
        private Color rgba(int r, int g, int b, float a)
        {
            return new Color(r/255f, g/255f, b/255f, a);
        }
    }
}
