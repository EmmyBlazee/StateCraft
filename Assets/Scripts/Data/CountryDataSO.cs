using System.Collections.Generic;
using UnityEngine;

namespace StateCraft.Data
{
    [System.Serializable]
    public class BudgetAllocation
    {
        public string id;
        public string title;
        public string description;
        public string faIcon;
        [Range(0f, 100f)] public float sharePercentage;
    }

    [CreateAssetMenu(fileName = "NewCountry", menuName = "StateCraft/Country Data")]
    public class CountryDataSO : ScriptableObject
    {
        [Header("=== NEW Military System ===")]
        public StateCraft.Systems.MilitaryState militaryState = new StateCraft.Systems.MilitaryState();

        private void OnEnable()
        {
            EnsureBudgetAllocations();
            if (militaryState == null) militaryState = new StateCraft.Systems.MilitaryState();
            
            if (trainingPipelines == null) trainingPipelines = new List<RecruitmentPipeline>();
            if (productionOrders == null) productionOrders = new List<ProductionOrder>();
            if (completedProductions == null) completedProductions = new List<CompletedProductionItem>();
            if (availableOffers == null) availableOffers = new List<ProcurementOffer>();
            if (activeContracts == null) activeContracts = new List<ProcurementContract>();
            if (activeSupplyContracts == null) activeSupplyContracts = new List<SupplyContract>();
            if (researchProjects == null) researchProjects = new List<ResearchProject>();
            if (activeInsurgentCamps == null) activeInsurgentCamps = new List<StateCraft.Systems.InsurgentCamp>();
            if (activeRecommendations == null) activeRecommendations = new List<ThreatRecommendation>();
            if (defenseFactories == null) defenseFactories = new List<DefenseFactory>();
            if (militaryBases == null) militaryBases = new List<MilitaryBase>();
            if (facilityConstructionOrders == null) facilityConstructionOrders = new List<FacilityConstructionOrder>();
            if (facilityUpgradeOrders == null) facilityUpgradeOrders = new List<FacilityUpgradeOrder>();
            if (safeHouses == null) safeHouses = new List<SafeHouse>();
            if (covertResources == null) covertResources = new List<CovertResourceAllocation>();

            if (covertResources.Count == 0)
            {
                covertResources.Add(new CovertResourceAllocation { resourceName = "Field Agents", allocatedAmount = 65, maxAmount = 100 });
                covertResources.Add(new CovertResourceAllocation { resourceName = "Technical Assets", allocatedAmount = 55, maxAmount = 100 });
                covertResources.Add(new CovertResourceAllocation { resourceName = "Safe Houses", allocatedAmount = 40, maxAmount = 100 });
                covertResources.Add(new CovertResourceAllocation { resourceName = "Logistics", allocatedAmount = 60, maxAmount = 100 });
                covertResources.Add(new CovertResourceAllocation { resourceName = "Cyber Tools", allocatedAmount = 70, maxAmount = 100 });
            }

            if (militaryState.army.infantry == 0 && unitInfantry > 0)
            {
                militaryState.army.infantry = unitInfantry;
                militaryState.army.rdfPersonnel = rdfStats.personnel;
                militaryState.navy.sailors = navyStats.personnel;
                militaryState.airForce.pilots = Mathf.RoundToInt(airForceStats.personnel * 0.15f);
                militaryState.airForce.groundCrew = Mathf.RoundToInt(airForceStats.personnel * 0.60f);
                militaryState.airForce.airControllers = Mathf.RoundToInt(airForceStats.personnel * 0.25f);
            }
        }

        [Header("General Info")]
        public string countryName;
        public string presidentName;
        public string capitalCity;
        public string currencyName = "Dollar";
        public string currencySymbol = "$";

        public string GetPresidentName()
        {
            if (!string.IsNullOrWhiteSpace(presidentName)) return presidentName;

            int hash = Mathf.Abs(!string.IsNullOrEmpty(countryName) ? countryName.GetHashCode() : 0);
            string[] firstNames = { "Emmanuel", "Ibrahim", "Kwame", "Amadou", "Moussa", "Tariq", "Kofi", "Samuel", "Abdoulaye", "Ousmane", "Jean-Baptiste", "David", "Paul", "Felix" };
            string[] lastNames  = { "Nguema", "Traore", "Mensah", "Diallo", "Sankara", "Toure", "Keita", "Osei", "Bongo", "Kabore", "Camara", "Sow", "Diop", "Cisse" };
            
            string fn = firstNames[hash % firstNames.Length];
            string ln = lastNames[(hash / firstNames.Length) % lastNames.Length];
            return $"{fn} {ln}";
        }

        [Header("Scenario Briefing")]
        [TextArea(4, 8)]
        public string scenarioTitle   = "";
        [TextArea(4, 8)]
        public string scenarioContext = "";
        [TextArea(6, 12)]
        public string scenarioPriorities = "";
        public bool   hasShownBriefing   = false;
        public Sprite countryFlag;
        public Color mapColor = Color.white;

        [Header("Demographics")]
        public long population = 200000000;
        public float populationGrowthRate = 0.025f;
        public long populationChangeLastTurn;

        [Header("National Inventory")]
        public float invRiceTonnes = 0f;
        public float invCassavaTonnes = 0f;
        public float invCocoaTonnes = 0f;
        public float invMineralsTonnes = 0f;
        public float invOilBarrels = 0f;
        public float invGasUnits = 0f;
        public float invUraniumUnits = 0f;
        public float invTechGoods = 0f;

        [Header("Intelligence Dashboard Data")]
        public float intelligenceLevel = 10f;
        public float cyberDefenseLevel = 10f;
        public float signalInterceptCapability = 10f;
        public float intelligenceBudget = 50f;
        public float intelligenceFunds = 2000f;
        public int satelliteCount = 0;
        public int satellitesUnderConstruction = 0;
        public int satelliteBuildMonthsRemaining = 0;
        public int droneCount = 0;
        public int dronesUnderConstruction = 0;
        public int droneBuildMonthsRemaining = 0;

        public int operativeCount = 12;
        public float globalIntelCoverage = 15f;
        public int foreignAgentsDetected = 0;
        public float agencyIntegrity = 85f;
        public List<CovertOperation> activeCovertOps = new List<CovertOperation>();
        public List<string> recentReconReports = new List<string>();
        public List<IntelligenceThreat> detectedThreats = new List<IntelligenceThreat>();
        public List<SafeHouse> safeHouses = new List<SafeHouse>();
        public List<CovertResourceAllocation> covertResources = new List<CovertResourceAllocation>();

[Header("Economic Stats")]
        public float gdp = 477f;
        public float income = 8f;
        public float spending = 6f;
        public float treasury = 12f;

        public float inflationRate = 0.02f;
        public float gdpGrowthRate = 0.03f;
        public float playerBudget;
        public float exchangeRate = 1.0f;
        public float baseExchangeRate = 1.0f;

        public float debt = 95f;
        public float domesticDebt = 60f;
        public float foreignDebt = 35f;

        public float taxRate = 15f;
        public float subsidyLevel = 5f;
        public float importTariffRate = 10f;
        [Header("Detailed Economy Policy")]
        public float incomeTaxRate = 12f;
        public float corporateTaxRate = 18f;
        public float vatRate = 7.5f;
        public float exportDutyRate = 2f;
        public float draftIncomeTaxRate;
        public float draftCorporateTaxRate;
        public float draftVatRate;
        public float draftImportTariffRate;
        public float draftExportDutyRate;
        public bool hasBudgetDraft;
        [Header("Dynamic Budget Allocations")]
        public List<BudgetAllocation> budgetAllocations = new List<BudgetAllocation>();

        public float budgetMilitaryShare { get => GetBudgetShare("MILITARY"); set => SetBudgetShare("MILITARY", value); }
        public float budgetIntelligenceShare { get => GetBudgetShare("INTELLIGENCE"); set => SetBudgetShare("INTELLIGENCE", value); }
        public float budgetEducationShare { get => GetBudgetShare("EDUCATION"); set => SetBudgetShare("EDUCATION", value); }
        public float budgetHealthShare { get => GetBudgetShare("HEALTH"); set => SetBudgetShare("HEALTH", value); }
        public float budgetInfrastructureShare { get => GetBudgetShare("INFRASTRUCTURE"); set => SetBudgetShare("INFRASTRUCTURE", value); }
        public float budgetAgricultureShare { get => GetBudgetShare("AGRICULTURE"); set => SetBudgetShare("AGRICULTURE", value); }
        public float budgetEnergyShare { get => GetBudgetShare("ENERGY"); set => SetBudgetShare("ENERGY", value); }
        public float budgetWelfareShare { get => GetBudgetShare("WELFARE"); set => SetBudgetShare("WELFARE", value); }

        private float GetBudgetShare(string id) => budgetAllocations?.Find(b => b.id == id)?.sharePercentage ?? 0f;
        private void SetBudgetShare(string id, float val) { var b = budgetAllocations?.Find(x => x.id == id); if (b != null) b.sharePercentage = val; }

        public void EnsureBudgetAllocations()
        {
            if (budgetAllocations != null && budgetAllocations.Count > 0) return;
            
            budgetAllocations = new List<BudgetAllocation>
            {
                new BudgetAllocation { id = "MILITARY", title = "MILITARY", description = "Defense, operations, equipment and personnel.", faIcon = "\uf0e4", sharePercentage = 23f },
                new BudgetAllocation { id = "INTELLIGENCE", title = "INTELLIGENCE", description = "Surveillance and counter operations.", faIcon = "\uf8d9", sharePercentage = 7f },
                new BudgetAllocation { id = "EDUCATION", title = "EDUCATION", description = "Schools, universities and education programs.", faIcon = "\uf549", sharePercentage = 14f },
                new BudgetAllocation { id = "HEALTH", title = "HEALTH", description = "Hospitals, healthcare programs and research.", faIcon = "\uf0fe", sharePercentage = 12f },
                new BudgetAllocation { id = "INFRASTRUCTURE", title = "INFRASTRUCTURE", description = "Roads, rail, ports, airports and public works.", faIcon = "\uf6fc", sharePercentage = 18f },
                new BudgetAllocation { id = "AGRICULTURE", title = "AGRICULTURE", description = "Farming, irrigation, food security and rural development.", faIcon = "\uf4d8", sharePercentage = 9f },
                new BudgetAllocation { id = "ENERGY", title = "ENERGY", description = "Power generation, distribution and renewable energy.", faIcon = "\uf0e7", sharePercentage = 8f },
                new BudgetAllocation { id = "WELFARE", title = "SOCIAL WELFARE", description = "Pensions, welfare schemes and social support.", faIcon = "\uf4c8", sharePercentage = 5f }
            };
        }

        public string lastTaxReformSummary = "";
        public int lastBudgetApprovalTurn = -3;
        public string lastBudgetDraftSummary = "";
        public string lastAdviserDecision = "";
        public string latestElectionResult = "";
        public List<string> taxReformLog = new List<string>();
        public List<string> dismissedAdviserTitles = new List<string>();
        public int adviserDismissTurn = 0;
        public int adviserNextRecommendationTurn = 0;
        public float corruptionIndex = 30f;
        public float povertyRate = 45f;
        public float unemploymentRate = 12f;
        public float foreignInvestment = 50f;
        public float resourceSovereignty = 100f;
        public float foodSecurity = 70f;
        public float environmentScore = 50f;

        public float debtServiceRatio = 80f;
        [Header("Monetary & Trade Policy")]
        public float centralBankInterestRate = 5f;
        public float publicApproval = 65f;
        public string creditRating = "B+";
        public float foreignAidReceived = 0f;
        public float tradeVolumeUSD = 0f;        [Header("Historical & UI Tracking")]
        public float prevInflationRate;
        public float prevPublicApproval;
        public float prevMonthlyTaxRevenue;
        public float prevMonthBalance;
        public float taxArrears;
        public List<float> budgetHistory = new List<float>();

[Header("Natural Resources")]
        public List<ResourceDeposit> resources = new List<ResourceDeposit>();

        [Header("Infrastructure")]
        [Range(0, 10)] public int roadLevel = 1;
        [Range(0, 10)] public int railLevel = 0;
        [Range(0, 10)] public int portLevel = 1;
        [Range(0, 10)] public int airportLevel = 1;
        [Range(0, 10)] public int healthcareLevel = 1;
        [Range(0, 10)] public int educationLevel = 1;
        [Range(0, 10)] public int hospitalLevel = 1;
        [Range(0, 10)] public int telecomLevel = 1;

[Header("Power Grid System")]
        [Range(0, 100)] public float electricityLevel = 30f;
        public bool hasNuclearPower = false;
        public bool isImportingUranium = false;
        
        public int powerGasPlants = 2;
        public int powerHydroPlants = 1;
        public int powerNuclearPlants = 0;
        public int powerSolarPlants = 0;
        public int powerWindPlants = 0;
        
        public float gridTransmissionEfficiency = 0.85f;
        public float currentBlackoutRisk = 0f;

        [Header("Industry Tiers")]
        [Range(0, 10)] public int indAgriRice = 0;
        [Range(0, 10)] public int indAgriCassava = 0;
        [Range(0, 10)] public int indAgriCocoa = 0;
        [Range(0, 10)] public int indMiningLocal = 0;
        [Range(0, 10)] public int indMiningOil = 0;
        [Range(0, 10)] public int indTechIT = 0;
        [Range(0, 10)] public int indTechFintech = 0;
        [Range(0, 10)] public int indManufacturing = 0;
        public float techIndustryOutput = 100f;

public float workforceOil = 10000;
        public float workforceMining = 25000;
        public float workforceAgri = 150000;
        public float workforceManufacturing = 5000;

[Header("Political Stats")]
        [Range(0, 100)]
        public float stability = 70f;
        [Range(0, 100)]
        public float security = 50f;
        public int nationalPoliceSize = 15000;

        [Header("Military — Force Strength")]
        public float militaryStrength = 40f; 
        public float armyStrength = 40f;
        public float navyStrength = 20f;
        public float airForceStrength = 20f;
        
        [Header("Standing Units (Land)")]
        public int unitInfantry = 50000;
        public int unitTanks = 120;
        public int unitAPCs = 250;
        public int unitArtillery = 80;
        public int unitRifles = 80000;

        [Header("Standing Units (Air)")]
        public int unitFighters = 40;
        public int unitBombers = 10;
        public int unitAttackHelis = 25;
        public int unitUAVs = 50;

        [Header("Standing Units (Sea)")]
        public int unitFrigates = 5;
        public int unitDestroyers = 2;
        public int unitSubmarines = 1;
        public int unitPatrolBoats = 12;

        [Header("Out of Service Units")]
        public int oosRifles = 0;
        public int oosTanks = 0;
        public int oosAPCs = 0;
        public int oosArtillery = 0;
        public int oosFighters = 0;
        public int oosBombers = 0;
        public int oosAttackHelis = 0;
        public int oosUAVs = 0;
        public int oosFrigates = 0;
        public int oosDestroyers = 0;
        public int oosSubmarines = 0;
        public int oosPatrolBoats = 0;

        [Header("Strategic Bases")]
        public int baseArmyCount = 5;
        public int baseNavyCount = 2;
        public int baseAirForceCount = 3;
        public int baseRDFCount = 1;
        public int baseDefenseFactoryCount = 0;
    public int importsThisTurn = 0;

        public float rdfReadiness = 80f;

        public float militaryBudgetPercent = 2.5f;
        public int militaryDoctrine = 1;
        public float securitySpending = 0f;
        public List<MilitaryProject> militaryProjects = new List<MilitaryProject>();

        public float militaryBudgetUSD = 2.5f;
        public float militaryUpkeepUSD = 0f;

        public float militaryReadiness = 75f;
        
        public BranchStats armyStats = new BranchStats("Army", 50000, 250, 75, 80);
        public BranchStats navyStats = new BranchStats("Navy", 12000, 45, 60, 75);
        public BranchStats airForceStats = new BranchStats("Air Force", 8000, 120, 65, 85);
        public BranchStats rdfStats = new BranchStats("Rapid Deployable Force", 5000, 80, 90, 95);

        [Header("Military — Logistics")]
        public float reserveFuel = 100f;
        public float reserveAmmo = 100f;
        public float reserveMedical = 100f;
        public float reserveFood = 100f;

        [Header("Military — Operations")]
        public List<MilitaryOperation> activeOperations = new List<MilitaryOperation>();
        public List<CompletedMilitaryOperation> completedOperations = new List<CompletedMilitaryOperation>();
        public List<string> operationalLogs = new List<string>();
        public List<ThreatRecommendation> activeRecommendations = new List<ThreatRecommendation>();

        public List<StateCraft.Systems.InsurgentCamp> activeInsurgentCamps = new List<StateCraft.Systems.InsurgentCamp>();
        public List<RecruitmentPipeline> trainingPipelines = new List<RecruitmentPipeline>();
        public List<ProductionOrder> productionOrders = new List<ProductionOrder>();
        public List<CompletedProductionItem> completedProductions = new List<CompletedProductionItem>();
        public List<ProcurementOffer> availableOffers = new List<ProcurementOffer>();
        public List<ProcurementContract> activeContracts = new List<ProcurementContract>();
        public List<SupplyContract> activeSupplyContracts = new List<SupplyContract>();
        public List<ResearchProject> researchProjects = new List<ResearchProject>();
        public List<DefenseFactory> defenseFactories = new List<DefenseFactory>();
        public List<MilitaryBase> militaryBases = new List<MilitaryBase>();
        public List<FacilityConstructionOrder> facilityConstructionOrders = new List<FacilityConstructionOrder>();
        public List<FacilityUpgradeOrder> facilityUpgradeOrders = new List<FacilityUpgradeOrder>();

        public string warEnemyName = "";
        public int warTurnsActive = 0;
        public float warExhaustion = 0f;
        public float warWillToFight = 100f;
        
        public int totalKIA = 0;
        public int totalCivilianCasualties = 0;
        public int totalWounded = 0;
        public float warFatigueStabilityDrain = 0f;

        public List<General> assignedGenerals = new List<General>();

        public List<NavalBlockade> activeBlockades = new List<NavalBlockade>();
        public bool isBlockaded = false;

        public bool hasNuclearWeapon = false;
        public bool hasChemicalWeapon = false;
        public int wmdDevelopmentProgress = 0;
        public bool wmdProgramDiscovered = false;

        public int rdfCooldownMonths = 0;
        public int lastRDFTurn = -100;
        public int lastRDFRegionIndex = -1;

        [Header("Global Standing")]
        public float globalPrestige = 50f;
        public int allianceCount = 0;
        public int sanctionIssuedCount = 0;
        public int sanctionReceivedCount = 0;

        [Header("Inbox System")]
        public List<InboxMessage> inboxMessages = new List<InboxMessage>();
        public List<GlobalCrisis> activeCrises = new List<GlobalCrisis>();
        public List<StateCraft.Interfaces.TradeAgreement> activeTradeAgreements = new List<StateCraft.Interfaces.TradeAgreement>();
        public List<DiplomaticTreaty> activeTreaties = new List<DiplomaticTreaty>();

        [Header("Diplomacy Models")]
        public List<DiplomaticEvent> diplomacyEvents = new List<DiplomaticEvent>();
        public List<Embassy> embassies = new List<Embassy>();
        public List<InternationalOrganization> internationalOrgs = new List<InternationalOrganization>();
        public float diplomacyInfluence = 71f;
        public float embassyBudget = 1.2f;

        [Header("=== Battle History ===")]
        public List<StateCraft.Systems.BattleResultMessage> battleResults = new List<StateCraft.Systems.BattleResultMessage>();

        [Header("Intelligence & Covert")]
        public float covertBudget = 0f;
        public float counterIntelligence = 50f;
        public float exposureRisk = 0f;
        public List<ForeignAsset> recruitedAssets = new List<ForeignAsset>();
        public List<IntelligenceHardware> intelHardware = new List<IntelligenceHardware>();

[Header("Geography")]
        public List<RegionDataSO> regions = new List<RegionDataSO>();

        [Header("Global Faction")]
        public string globalFaction = "Independent";

        [Header("Factions")]
        public List<FactionDataSO> activeFactions = new List<FactionDataSO>();

        [Header("Policies")]
        public List<PolicyDataSO> activePolicies = new List<PolicyDataSO>();

        [Header("Construction & Projects")]
        public List<ActiveProject> activeProjects = new List<ActiveProject>();

        public float GetMilitaryFirepowerScore()
        {
            float landPower = (unitInfantry * 1.0f + unitRifles * 0.2f + unitTanks * 25.0f + unitAPCs * 10.0f + unitArtillery * 15.0f) * (armyStats.readiness / 100f) * (armyStats.morale / 100f);
            float airPower = (airForceStats.personnel * 5.0f + unitFighters * 150.0f + unitBombers * 250.0f + unitAttackHelis * 80.0f + unitUAVs * 30.0f) * (airForceStats.readiness / 100f) * (airForceStats.morale / 100f);
            float navalPower = (navyStats.personnel * 3.0f + unitFrigates * 500.0f + unitDestroyers * 1000.0f + unitSubmarines * 1200.0f + unitPatrolBoats * 40.0f) * (navyStats.readiness / 100f) * (navyStats.morale / 100f);
            float rdfPower = (rdfStats.personnel * 15.0f + rdfStats.equipment * 50.0f) * (rdfStats.readiness / 100f) * (rdfStats.morale / 100f);
            float infrastructure = baseArmyCount * 1000f + baseNavyCount * 1500f + baseAirForceCount * 1500f + baseRDFCount * 2000f;
            return landPower + airPower + navalPower + rdfPower + infrastructure;
        }

        public float CalculateMilitaryUpkeep()
        {

            float armyUpkeep = (unitInfantry    * 0.0000020f)
                             + (unitRifles       * 0.0000002f)
                             + (unitTanks        * 0.000030f)
                             + (unitAPCs         * 0.000010f)
                             + (unitArtillery    * 0.000015f);

            float airForceUpkeep = (airForceStats.personnel * 0.000006f)
                                 + (unitFighters       * 0.000150f)
                                 + (unitBombers        * 0.000280f)
                                 + (unitAttackHelis    * 0.000080f)
                                 + (unitUAVs           * 0.000020f);

            float navyUpkeep = (navyStats.personnel  * 0.000005f)
                             + (unitFrigates          * 0.000400f)
                             + (unitDestroyers        * 0.000800f)
                             + (unitSubmarines        * 0.001000f)
                             + (unitPatrolBoats       * 0.000040f);

            float rdfUpkeep = (rdfStats.personnel * 0.000010f)
                            + (rdfStats.equipment  * 0.000050f);

            float basesUpkeep = (baseArmyCount      * 0.006f)
                              + (baseNavyCount       * 0.010f)
                              + (baseAirForceCount   * 0.010f)
                              + (baseRDFCount        * 0.015f);

            float contractsUpkeep = 0f;
            if (activeSupplyContracts != null) {
                foreach (var sc in activeSupplyContracts) {
                    contractsUpkeep += sc.costPerTurn;
                }
            }

            float totalUpkeep = armyUpkeep     * (armyStats.readiness     / 100f)
                              + airForceUpkeep * (airForceStats.readiness  / 100f)
                              + navyUpkeep     * (navyStats.readiness      / 100f)
                              + rdfUpkeep      * (rdfStats.readiness       / 100f)
                              + basesUpkeep
                              + contractsUpkeep;

            return Mathf.Max(0.10f, totalUpkeep);
        }
    }

    public enum ProjectStage
    {
        Planned,
        Funded,
        UnderConstruction,
        Active,
        Aging
    }

    [System.Serializable]
    public class ForeignAsset
    {
        public string targetCountry;
        public string assetName;
        public float loyalty;
        public int intelLevelProvided;
    }

    [System.Serializable]
    public class ActiveProject
    {
        public string projectName;
        public string projectType;
        public string regionName; 
        public string targetID;
        public float totalCost;
        public int monthsRemaining;
        public int totalMonths;
        public ProjectStage stage = ProjectStage.Planned;
        public string description;
    }

[System.Serializable]
    public class MilitaryProject

    {
        public string projectName;
        public string branch;
        public float strengthGain;
        public float costBillions;
        public int monthsRemaining;
        public int totalMonths;
    }

    [System.Serializable]
    public class BranchStats
    {
        public string branchName;
        public int personnel;
        public int equipment;
        public float readiness;
        public float morale;

        public BranchStats(string name, int p, int e, float r, float m)
        {
            branchName = name; personnel = p; equipment = e; readiness = r; morale = m;
        }
    }

[System.Serializable]
    public class MilitaryOperation
    {
        public string operationId;
        public string missionId;
        public List<BranchContribution> contributions = new List<BranchContribution>();
        public bool IsJoint => contributions != null && contributions.Count > 1;

        public List<BranchContribution> GetContributionsMigrated()
        {
            if (contributions == null) contributions = new List<BranchContribution>();
            if (contributions.Count == 0 && (deployedTroops > 0 || deployedTanks > 0 || deployedAPCs > 0 || deployedFighters > 0 || deployedHelis > 0 || deployedFrigates > 0 || deployedSubmarines > 0 || deployedRDF > 0 || deployedBombers > 0 || deployedUAVs > 0 || deployedPatrolBoats > 0 || deployedArtillery > 0 || deployedDestroyers > 0))
            {
                MilitaryBranch branch = MilitaryBranch.Army;
                if (deployedFrigates > 0 || deployedSubmarines > 0 || deployedPatrolBoats > 0 || deployedNavy > 0 || deployedDestroyers > 0)
                {
                    branch = MilitaryBranch.Navy;
                }
                else if (deployedFighters > 0 || deployedBombers > 0 || deployedHelis > 0 || deployedUAVs > 0 || deployedAirForce > 0)
                {
                    branch = MilitaryBranch.AirForce;
                }
                else if (deployedRDF > 0)
                {
                    branch = MilitaryBranch.RDF;
                }
                
                var contr = new BranchContribution();
                contr.branch = branch;
                contr.personnelAssigned = branch == MilitaryBranch.RDF ? deployedRDF : (branch == MilitaryBranch.Navy ? deployedNavy : (branch == MilitaryBranch.AirForce ? deployedAirForce : deployedTroops));
                
                if (deployedTanks > 0) contr.SetUnitCount("Tanks", deployedTanks);
                if (deployedAPCs > 0) contr.SetUnitCount("APCs", deployedAPCs);
                if (deployedArtillery > 0) contr.SetUnitCount("Artillery", deployedArtillery);
                if (deployedFighters > 0) contr.SetUnitCount("Fighters", deployedFighters);
                if (deployedBombers > 0) contr.SetUnitCount("Bombers", deployedBombers);
                if (deployedHelis > 0) contr.SetUnitCount("Helicopters", deployedHelis);
                if (deployedUAVs > 0) contr.SetUnitCount("Drones", deployedUAVs);
                if (deployedFrigates > 0) contr.SetUnitCount("Frigates", deployedFrigates);
                if (deployedSubmarines > 0) contr.SetUnitCount("Submarines", deployedSubmarines);
                if (deployedPatrolBoats > 0) contr.SetUnitCount("Patrol Boats", deployedPatrolBoats);
                if (deployedDestroyers > 0) contr.SetUnitCount("Destroyers", deployedDestroyers);
                
                contributions.Add(contr);
            }
            return contributions;
        }

        public static void DeductFromReserves(CountryDataSO country, MilitaryBranch branch, Dictionary<string, int> units, int personnel)
        {
            switch (branch)
            {
                case MilitaryBranch.Army:
                    country.unitInfantry = UnityEngine.Mathf.Max(0, country.unitInfantry - personnel);
                    country.armyStats.personnel = UnityEngine.Mathf.Max(0, country.armyStats.personnel - personnel);
                    if (units.TryGetValue("Tanks", out int tanks))
                    {
                        country.unitTanks = UnityEngine.Mathf.Max(0, country.unitTanks - tanks);
                        country.armyStats.equipment = UnityEngine.Mathf.Max(0, country.armyStats.equipment - tanks);
                    }
                    if (units.TryGetValue("APCs", out int apcs))
                    {
                        country.unitAPCs = UnityEngine.Mathf.Max(0, country.unitAPCs - apcs);
                        country.armyStats.equipment = UnityEngine.Mathf.Max(0, country.armyStats.equipment - apcs);
                    }
                    if (units.TryGetValue("Artillery", out int artillery))
                    {
                        country.unitArtillery = UnityEngine.Mathf.Max(0, country.unitArtillery - artillery);
                        country.armyStats.equipment = UnityEngine.Mathf.Max(0, country.armyStats.equipment - artillery);
                    }
                    break;

                case MilitaryBranch.AirForce:
                    country.airForceStats.personnel = UnityEngine.Mathf.Max(0, country.airForceStats.personnel - personnel);
                    int totalAirEquip = 0;
                    if (units.TryGetValue("Fighters", out int fighters))
                    {
                        country.unitFighters = UnityEngine.Mathf.Max(0, country.unitFighters - fighters);
                        totalAirEquip += fighters;
                    }
                    if (units.TryGetValue("Bombers", out int bombers))
                    {
                        country.unitBombers = UnityEngine.Mathf.Max(0, country.unitBombers - bombers);
                        totalAirEquip += bombers;
                    }
                    if (units.TryGetValue("Helicopters", out int helis))
                    {
                        country.unitAttackHelis = UnityEngine.Mathf.Max(0, country.unitAttackHelis - helis);
                        totalAirEquip += helis;
                    }
                    if (units.TryGetValue("Drones", out int drones))
                    {
                        country.unitUAVs = UnityEngine.Mathf.Max(0, country.unitUAVs - drones);
                        totalAirEquip += drones;
                    }
                    country.airForceStats.equipment = UnityEngine.Mathf.Max(0, country.airForceStats.equipment - totalAirEquip);
                    break;

                case MilitaryBranch.Navy:
                    country.navyStats.personnel = UnityEngine.Mathf.Max(0, country.navyStats.personnel - personnel);
                    int totalNavyEquip = 0;
                    if (units.TryGetValue("Frigates", out int frigates))
                    {
                        country.unitFrigates = UnityEngine.Mathf.Max(0, country.unitFrigates - frigates);
                        totalNavyEquip += frigates;
                    }
                    if (units.TryGetValue("Submarines", out int subs))
                    {
                        country.unitSubmarines = UnityEngine.Mathf.Max(0, country.unitSubmarines - subs);
                        totalNavyEquip += subs;
                    }
                    if (units.TryGetValue("Patrol Boats", out int boats))
                    {
                        country.unitPatrolBoats = UnityEngine.Mathf.Max(0, country.unitPatrolBoats - boats);
                        totalNavyEquip += boats;
                    }
                    if (units.TryGetValue("Destroyers", out int destroyers))
                    {
                        country.unitDestroyers = UnityEngine.Mathf.Max(0, country.unitDestroyers - destroyers);
                        totalNavyEquip += destroyers;
                    }
                    country.navyStats.equipment = UnityEngine.Mathf.Max(0, country.navyStats.equipment - totalNavyEquip);
                    break;

                case MilitaryBranch.RDF:
                    country.rdfStats.personnel = UnityEngine.Mathf.Max(0, country.rdfStats.personnel - personnel);
                    country.unitInfantry = UnityEngine.Mathf.Max(0, country.unitInfantry - personnel);
                    break;
            }
        }

        public static void ReturnToReserves(CountryDataSO country, MilitaryBranch branch, Dictionary<string, int> units, int personnel)
        {
            switch (branch)
            {
                case MilitaryBranch.Army:
                    country.unitInfantry += personnel;
                    country.armyStats.personnel += personnel;
                    if (units.TryGetValue("Tanks", out int tanks))
                    {
                        country.unitTanks += tanks;
                        country.armyStats.equipment += tanks;
                    }
                    if (units.TryGetValue("APCs", out int apcs))
                    {
                        country.unitAPCs += apcs;
                        country.armyStats.equipment += apcs;
                    }
                    if (units.TryGetValue("Artillery", out int artillery))
                    {
                        country.unitArtillery += artillery;
                        country.armyStats.equipment += artillery;
                    }
                    break;

                case MilitaryBranch.AirForce:
                    country.airForceStats.personnel += personnel;
                    int totalAirEquip = 0;
                    if (units.TryGetValue("Fighters", out int fighters))
                    {
                        country.unitFighters += fighters;
                        totalAirEquip += fighters;
                    }
                    if (units.TryGetValue("Bombers", out int bombers))
                    {
                        country.unitBombers += bombers;
                        totalAirEquip += bombers;
                    }
                    if (units.TryGetValue("Helicopters", out int helis))
                    {
                        country.unitAttackHelis += helis;
                        totalAirEquip += helis;
                    }
                    if (units.TryGetValue("Drones", out int drones))
                    {
                        country.unitUAVs += drones;
                        totalAirEquip += drones;
                    }
                    country.airForceStats.equipment += totalAirEquip;
                    break;

                case MilitaryBranch.Navy:
                    country.navyStats.personnel += personnel;
                    int totalNavyEquip = 0;
                    if (units.TryGetValue("Frigates", out int frigates))
                    {
                        country.unitFrigates += frigates;
                        totalNavyEquip += frigates;
                    }
                    if (units.TryGetValue("Submarines", out int subs))
                    {
                        country.unitSubmarines += subs;
                        totalNavyEquip += subs;
                    }
                    if (units.TryGetValue("Patrol Boats", out int boats))
                    {
                        country.unitPatrolBoats += boats;
                        totalNavyEquip += boats;
                    }
                    if (units.TryGetValue("Destroyers", out int destroyers))
                    {
                        country.unitDestroyers += destroyers;
                        totalNavyEquip += destroyers;
                    }
                    country.navyStats.equipment += totalNavyEquip;
                    break;

                case MilitaryBranch.RDF:
                    country.rdfStats.personnel += personnel;
                    country.unitInfantry += personnel;
                    break;
            }
        }

        public static float CalculateUpkeep(List<BranchContribution> contributions)
        {
            float monthlyCost = 0.01f;
            foreach (var c in contributions)
            {
                var dict = c.GetUnitCountsDict();
                monthlyCost += (c.personnelAssigned / 1000f) * (c.branch == MilitaryBranch.RDF ? 0.02f : 0.01f);
                if (dict.TryGetValue("Tanks", out int tanks)) monthlyCost += tanks * 0.005f;
                if (dict.TryGetValue("APCs", out int apcs)) monthlyCost += apcs * 0.002f;
                if (dict.TryGetValue("Fighters", out int fighters)) monthlyCost += fighters * 0.015f;
                if (dict.TryGetValue("Helicopters", out int helis)) monthlyCost += helis * 0.008f;
                if (dict.TryGetValue("Frigates", out int frigates)) monthlyCost += frigates * 0.04f;
                if (dict.TryGetValue("Submarines", out int subs)) monthlyCost += subs * 0.06f;
                if (dict.TryGetValue("Patrol Boats", out int boats)) monthlyCost += boats * 0.02f;
                if (dict.TryGetValue("Bombers", out int bombers)) monthlyCost += bombers * 0.025f;
                if (dict.TryGetValue("Drones", out int drones)) monthlyCost += drones * 0.005f;
            }
            return monthlyCost;
        }

        public static float CalculateSecurityBoost(CountryDataSO country, string missionId, List<BranchContribution> contributions)
        {
            float score = 50f;
            foreach (var c in contributions)
            {
                score += CalculateBranchPowerScore(c);
            }

            if (contributions.Count >= 2)
            {
                score += (contributions.Count - 1) * 8f;
            }

            if (country != null && country.unitRifles < country.armyStats.personnel)
            {
                score *= 0.5f;
            }

            return UnityEngine.Mathf.Clamp(score, 0f, 96f);
        }

        public static float CalculateBranchPowerScore(BranchContribution c)
        {
            float power = (c.personnelAssigned / 1000f) * 0.5f;
            var dict = c.GetUnitCountsDict();
            if (dict.TryGetValue("Tanks", out int tanks)) power += tanks * 0.8f;
            if (dict.TryGetValue("APCs", out int apcs)) power += apcs * 0.4f;
            if (dict.TryGetValue("Fighters", out int fighters)) power += fighters * 2.0f;
            if (dict.TryGetValue("Helicopters", out int helis)) power += helis * 1.0f;
            if (dict.TryGetValue("Frigates", out int frigates)) power += frigates * 1.5f;
            if (dict.TryGetValue("Submarines", out int subs)) power += subs * 2.0f;
            if (dict.TryGetValue("Patrol Boats", out int boats)) power += boats * 0.8f;
            if (dict.TryGetValue("Bombers", out int bombers)) power += bombers * 3.0f;
            if (dict.TryGetValue("Drones", out int drones)) power += drones * 1.2f;

            if (c.branch == MilitaryBranch.RDF)
            {
                power += (c.personnelAssigned / 1000f) * 3.0f;
            }
            return power;
        }

        public void SyncFlatFields()
        {
            deployedTroops = 0;
            deployedHelis = 0;
            deployedTanks = 0;
            deployedAPCs = 0;
            deployedFighters = 0;
            deployedFrigates = 0;
            deployedSubmarines = 0;
            deployedRDF = 0;
            deployedUAVs = 0;
            deployedBombers = 0;
            deployedAirForce = 0;
            deployedNavy = 0;
            deployedPatrolBoats = 0;
            deployedArtillery = 0;
            deployedDestroyers = 0;

            if (contributions == null) return;
            foreach (var c in contributions)
            {
                var dict = c.GetUnitCountsDict();
                if (c.branch == MilitaryBranch.Army)
                {
                    deployedTroops += c.personnelAssigned;
                    if (dict.TryGetValue("Tanks", out int tanks)) deployedTanks += tanks;
                    if (dict.TryGetValue("APCs", out int apcs)) deployedAPCs += apcs;
                    if (dict.TryGetValue("Artillery", out int artillery)) deployedArtillery += artillery;
                }
                else if (c.branch == MilitaryBranch.AirForce)
                {
                    deployedAirForce += c.personnelAssigned;
                    if (dict.TryGetValue("Fighters", out int fighters)) deployedFighters += fighters;
                    if (dict.TryGetValue("Bombers", out int bombers)) deployedBombers += bombers;
                    if (dict.TryGetValue("Helicopters", out int helis)) deployedHelis += helis;
                    if (dict.TryGetValue("Drones", out int drones)) deployedUAVs += drones;
                }
                else if (c.branch == MilitaryBranch.Navy)
                {
                    deployedNavy += c.personnelAssigned;
                    if (dict.TryGetValue("Frigates", out int frigates)) deployedFrigates += frigates;
                    if (dict.TryGetValue("Submarines", out int subs)) deployedSubmarines += subs;
                    if (dict.TryGetValue("Patrol Boats", out int boats)) deployedPatrolBoats += boats;
                    if (dict.TryGetValue("Destroyers", out int destroyers)) deployedDestroyers += destroyers;
                }
                else if (c.branch == MilitaryBranch.RDF)
                {
                    deployedRDF += c.personnelAssigned;
                    deployedTroops += c.personnelAssigned;
                }
            }
        }

        public string opName;
        public string regionName;
        public string type;
        public string status;
        public float costPerMonth;
        public int deployedTroops;
        public int deployedHelis;
        public int deployedTanks;
        public int deployedAPCs;
        public int deployedFighters;
        public int deployedFrigates;
        public int deployedSubmarines;
        public int deployedRDF;
        public int deployedUAVs;
        public int deployedBombers;
        public int deployedAirForce;
        public int deployedNavy;
        public int deployedPatrolBoats;
        public int deployedArtillery;
        public int deployedDestroyers;
        public string casualtyRisk;
        public string civilianRisk;
        public float securityBoostRate;
        
        public bool isDecommissioning;
        public int turnsToDeploy;

        public string phase = "Preparing";
        public string commander = "";
        public string objective = "Neutralise Threats";
        public int turnsActive = 0;
        public float morale = 80f;
        public float territoryCaptured = 0f;
        public float enemyStrengthEst = 60f;
        public int casualtiesSustained = 0;
        public int enemyCasualtiesInflicted = 0;
        public int equipmentLost = 0;

        public int cooldownAirstrike = 0;
        public int cooldownRecon = 0;
        public int cooldownIntel = 0;

        public string lastNotification = "";

        public int attritionTroops = 0;
        public int attritionEquipment = 0;
        public float effectiveTroopStrength = 100f;
        public bool needsReinforcement = false;
        public List<MilitaryBranch> needsReinforcementBranches = new List<MilitaryBranch>();
        public List<int> friendlyCasualtyHistory = new List<int>();
        public List<int> enemyCasualtyHistory = new List<int>();
        public List<float> successChanceHistory = new List<float>();
        public int lastReinforcementTurn = 0;
        public int reinforcementCount = 0;
        public float objectiveProgress = 0f;
    }

    [System.Serializable]
    public class CompletedMilitaryOperation
    {
        public string opName;
        public string type;
        public string regionName;
        public string outcome;
        public int casualtiesSustained;
        public int equipmentLost;
        public float territoryCaptured;
        public int politicalImpact;
        public string dateCompleted;
        public int durationMonths;
        public string rewards;
    }

    [System.Serializable]
    public class CovertOperation
    {
        public string opName;
        public string type;
        public string target;
        public string operativeName;
        public string riskLevel;
        public string status;

        public int currentPhase = 1;
        public int totalPhases = 3;
        public int monthsRemaining = 3;
        public int daysRemaining = 0;
        public string priority = "";
        public float detectionRisk = 15f;
    }

    [System.Serializable]
    public class SafeHouse
    {
        public string locationName;
        public string status;
        public int capacity;
        public int currentOccupancy;
        public string coverType;
    }

    [System.Serializable]
    public class CovertResourceAllocation
    {
        public string resourceName;
        public int allocatedAmount;
        public int maxAmount;
    }

    [System.Serializable]
    public class IntelligenceThreat
    {
        public string threatName;
        public string category;
        public string severity;
        public string description;
    }

    [System.Serializable]
    public class DiplomaticTreaty
    {
        public string treatyName;
        public string partnerCountry;
        public string terms;
        public int monthsRemaining;
        public int turnSigned;
        public string status;
        public int turnsUntilResolution;
        public string rejectionReason;
        public float tradeIncomeBonus;
        public bool  blocksWar;
    }

    [System.Serializable]
    public class InboxMessage
    {
        public string id;
        public string sender;
        public string subject;
        public string body;
        public string dateReceived;
        public bool isRead;
        public int turnReceived;
        
        public string urgency;
    }

    [System.Serializable]
    public class GlobalCrisis
    {
        public string crisisName;
        public string affectedRegion;
        public string severity;
        public float internationalPressure;
    }

    [System.Serializable]
    public class RecruitmentPipeline
    {
        public string id;
        public string unitLabel;
        public int amount;
        public int monthsRemaining = 6;
        public bool isPersonnel;

        public RecruitmentPipeline(string id, string unitLabel, int amount, bool isPersonnel)
        {
            this.id = id;
            this.unitLabel = unitLabel;
            this.amount = amount;
            this.monthsRemaining = 6;
            this.isPersonnel = isPersonnel;
        }
    }

    public enum GeneralSpecialty { Army, AirForce, Navy, Special }

    [System.Serializable]
    public class General
    {
        public string generalName;
        public int rank;
        public GeneralSpecialty specialty;
        public float effectivenessBonus;
        public float moraleBonus;
        public string assignedOperation;
        public bool isDeployed;
    }

    [System.Serializable]
    public class NavalBlockade
    {
        public string blockerCountry;
        public string targetCountry;
        public int monthsActive;
        public float tradeSuppressionPercent;
        public int frigatesAssigned;
        public int destroyersAssigned;
        public int submarinesAssigned;
    }

[System.Serializable]
    public class ProductionOrder
    {
        public string id;
        public string unitLabel;
        public string facilityName;
        public string category;
        public int quantity;
        public int monthsTotal;
        public int monthsRemaining;
        public float monthlyCostB;
        public float monthlyOutput;
        public float progressPct => monthsTotal > 0 ? ((monthsTotal - monthsRemaining) / (float)monthsTotal) * 100f : 0f;
    }

    [System.Serializable]
    public class BaseCapability
    {
        public string name;
        public int level;
        public string icon;
        public BaseCapability(string n, int l, string i) { name = n; level = l; icon = i; }
    }

    [System.Serializable]
    public class AssignedUnit
    {
        public string unitName;
        public int personnel;
        public string icon;
        public AssignedUnit(string n, int p, string i) { unitName = n; personnel = p; icon = i; }
    }

    [System.Serializable]
    public class MilitaryBase
    {
        public string id;
        public string baseName;
        public string regionName;
        public string branch;
        public string status;
        public int capacity;
        public int currentPersonnel;
        public float upkeepCost;
        public string role;
        
        public int establishedYear;
        public int sizeHa;
        public List<BaseCapability> capabilities;
        public List<AssignedUnit> assignedUnits;
        
        public MilitaryBase(string id, string name, string regionName, string branch, int cap, string role)
        {
            this.id = id;
            this.baseName = name;
            this.regionName = regionName;
            this.branch = branch;
            this.status = "OPERATIONAL";
            this.capacity = cap;
            this.currentPersonnel = (int)(cap * 0.8f);
            this.upkeepCost = cap * 0.005f;
            this.role = role;
            this.establishedYear = 2010 + UnityEngine.Random.Range(0, 10);
            this.sizeHa = cap / 6;
            this.capabilities = new List<BaseCapability>();
            this.assignedUnits = new List<AssignedUnit>();
        }
    }

    [System.Serializable]
    public class DefenseFactory
    {
        public string id;
        public string factoryName;
        public string produceType;
        public int level;
        public float utilizationPct;
    }

    [System.Serializable]
    public class CompletedProductionItem
    {
        public string unitLabel;
        public int quantity;
        public string completedDate;
    }

    [System.Serializable]
    public class ProcurementOffer {
        public string id;
        public string title;
        public string subtitle;
        public string[] tags;
        public string supplier;
        public string country;
        public string unitCost;
        public string totalCost;
        public string quantity;
        public string deliveryTime;
        public string flagEmoji;
        
        public string produceType;
        public int unitQuantity;
        public int deliveryMonths;
        public float totalCostB;
    }

    [System.Serializable]
    public class ProcurementContract {
        public string id;
        public string unitLabel;
        public string supplier;
        public string category;
        public int quantity;
        public int monthsTotal;
        public int monthsRemaining;
        public float totalCostB;
        public float monthlyCostB;
        public string status;

        public string produceType;
        public int unitQuantity;
    }

    [System.Serializable]
    public class ResearchProject {
        public string id;
        public string projectName;
        public string category;
        public int monthsTotal;
        public int monthsRemaining;
        public string status;
        public string completedDate;
    }

    [System.Serializable]
    public class FacilityConstructionOrder {
        public string id;
        public string facilityName;
        public int facilityType;
        public string regionName;
        public string branch;
        public string specialization;
        public int capacity;
        public int monthsTotal;
        public int monthsRemaining;
        public float progressPct => monthsTotal > 0 ? ((monthsTotal - monthsRemaining) / (float)monthsTotal) * 100f : 0f;
    }

    [System.Serializable]
    public class FacilityUpgradeOrder {
        public string id;
        public string facilityId;
        public string facilityName;
        public string upgradeType;
        public int monthsTotal;
        public int monthsRemaining;
        public float progressPct => monthsTotal > 0 ? ((monthsTotal - monthsRemaining) / (float)monthsTotal) * 100f : 0f;
    }

    [System.Serializable]
    public class SupplyContract {
        public string id;
        public string resourceType;
        public string label;
        public float amountPerTurn;
        public float costPerTurn;
        public int turnsTotal;
        public int turnsRemaining;
    }

    [System.Serializable]
    public class IntelligenceHardware
    {
        public string id;
        public string assetName;
        public string type;
        public string status;
        public int monthsTotal;
        public int monthsRemaining;
    }

    [System.Serializable]
    public class ThreatRecommendation
    {
        public string regionName;
        public string threatType;
        public string urgency;
        public string justification;
        public int turnsActive = 0;
        public bool isAddressed = false;
    }
}
