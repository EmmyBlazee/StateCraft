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
            // Populate new personnel from existing legacy stats for backwards compatibility
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
        public string capitalCity;
        public string currencyName = "Dollar";
        public string currencySymbol = "$";

        [Header("Scenario Briefing")]
        [TextArea(4, 8)]
        public string scenarioTitle   = "";   // e.g. "Nigeria: Oil & Fire"
        [TextArea(4, 8)]
        public string scenarioContext = "";   // 2-3 sentence situation summary shown on briefing screen
        [TextArea(6, 12)]
        public string scenarioPriorities = ""; // Bullet-point priorities shown under "YOUR PRIORITIES:"
        public bool   hasShownBriefing   = false; // Set true after first-time briefing is dismissed
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
        public float invTechGoods = 0f;

        [Header("Intelligence Dashboard Data")]
        public float intelligenceLevel = 10f; // Legacy compatibility
        public float intelligenceBudget = 50f;
        public int satelliteCount = 0;
        public int satellitesUnderConstruction = 0;
        public int satelliteBuildMonthsRemaining = 0;

        public int operativeCount = 12;
        public float globalIntelCoverage = 15f; // Score out of 100
        public int foreignAgentsDetected = 0;
        public float agencyIntegrity = 85f;
        public List<CovertOperation> activeCovertOps = new List<CovertOperation>();
        public List<IntelligenceThreat> detectedThreats = new List<IntelligenceThreat>();


        [Header("Economic Stats")]
        // BUG 4 FIX: GDP, debt, and all monetary fields are stored in BILLIONS of USD.
        // This matches treasury/income/spending which were already in billions.
        // Old default was 1000f (raw float treated as raw USD = ~$1000, displayed as $0.0B).
        // New default of 477f represents ~$477B, a realistic mid-range African economy.
        public float gdp = 477f;             // Billions USD
        public float income = 8f;            // Billions USD/month
        public float spending = 6f;          // Billions USD/month
        public float treasury = 12f;         // Billions USD

        public float inflationRate = 0.02f;
        public float gdpGrowthRate = 0.03f;
        public float playerBudget;
        public float exchangeRate = 1.0f;     // Live rate — Local currency per USD
        public float baseExchangeRate = 1.0f; // Starting rate — used to cap devaluation and reset on new game

        // BUG 5 FIX: Debt defaults reduced from 500/300/200 to realistic levels.
        // Old defaults gave a 50% debt/GDP ratio on turn 1, immediately triggering debt crisis
        // headlines for every country and near-instant default for the player country.
        // New defaults give ~20% debt/GDP which is manageable at game start.
        public float debt = 95f;             // Billions USD (~20% of default GDP)
        public float domesticDebt = 60f;     // Billions USD
        public float foreignDebt = 35f;      // Billions USD

        public float taxRate = 15f; // Percentage
        public float subsidyLevel = 5f; // Percentage of GDP
        public float importTariffRate = 10f; // Percentage
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

        // Backwards compatibility properties mapping to budgetAllocations
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
                new BudgetAllocation { id = "MILITARY", title = "MILITARY", description = "Defense, operations, equipment and personnel.", faIcon = "&#xf0e4;", sharePercentage = 23f }, // f0e4 = shield-halved or gauge
                new BudgetAllocation { id = "INTELLIGENCE", title = "INTELLIGENCE", description = "Surveillance and counter operations.", faIcon = "&#xf8d9;", sharePercentage = 7f }, // secret
                new BudgetAllocation { id = "EDUCATION", title = "EDUCATION", description = "Schools, universities and education programs.", faIcon = "&#xf549;", sharePercentage = 14f }, // school
                new BudgetAllocation { id = "HEALTH", title = "HEALTH", description = "Hospitals, healthcare programs and research.", faIcon = "&#xf0fe;", sharePercentage = 12f }, // plus-square
                new BudgetAllocation { id = "INFRASTRUCTURE", title = "INFRASTRUCTURE", description = "Roads, rail, ports, airports and public works.", faIcon = "&#xf6fc;", sharePercentage = 18f }, // tractor/city
                new BudgetAllocation { id = "AGRICULTURE", title = "AGRICULTURE", description = "Farming, irrigation, food security and rural development.", faIcon = "&#xf4d8;", sharePercentage = 9f }, // seedling
                new BudgetAllocation { id = "ENERGY", title = "ENERGY", description = "Power generation, distribution and renewable energy.", faIcon = "&#xf0e7;", sharePercentage = 8f }, // bolt
                new BudgetAllocation { id = "WELFARE", title = "SOCIAL WELFARE", description = "Pensions, welfare schemes and social support.", faIcon = "&#xf4c8;", sharePercentage = 5f } // hand-holding-heart
            };
        }

        public string lastTaxReformSummary = "";
        public int lastBudgetApprovalTurn = -3; // Initialize negative to allow immediate day 1 budget passing
        public string lastBudgetDraftSummary = "";
        public string lastAdviserDecision = "";
        public List<string> taxReformLog = new List<string>();
        public List<string> dismissedAdviserTitles = new List<string>();
        public int adviserDismissTurn = 0;
        public int adviserNextRecommendationTurn = 0;
        public float corruptionIndex = 30f; // Percentage of state leakage
        public float povertyRate = 45f; // Percentage of population below poverty line
        public float unemploymentRate = 12f; // Percentage
        public float foreignInvestment = 50f; // Billions USD
        public float resourceSovereignty = 100f; // 0-100 score
        public float foodSecurity = 70f; // 0-100 score
        public float environmentScore = 50f; // 0-100 score

        // BUG 5 FIX: Start at 80% service ratio instead of 100% to give the player some breathing room
        public float debtServiceRatio = 80f; // 100% = Pay full interest
        [Header("Monetary & Trade Policy")]
        public float centralBankInterestRate = 5f; // Percentage
        public float publicApproval = 65f; // Percentage
        public string creditRating = "B+"; // AAA, AA, A, BBB, BB, B, CCC, CC, C, D
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
        [Range(0, 100)] public float electricityLevel = 30f; // Percentage of population with power
        public bool hasNuclearPower = false;
        public bool isImportingUranium = false;
        
        public int powerGasPlants = 2;
        public int powerHydroPlants = 1;
        public int powerNuclearPlants = 0;
        public int powerSolarPlants = 0;
        public int powerWindPlants = 0;
        
        public float gridTransmissionEfficiency = 0.85f; // 85% starting efficiency
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
        public float techIndustryOutput = 100f; // Productivity multiplier


        
        // Workforce distribution (abstracted)
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

        [Header("Strategic Bases")]
        public int baseArmyCount = 5;
        public int baseNavyCount = 2;
        public int baseAirForceCount = 3;
        public int baseRDFCount = 1;
        public int baseDefenseFactoryCount = 0;
    // Tracks number of imports this turn (per-turn import cap)
    public int importsThisTurn = 0;

        public float rdfReadiness = 80f;

        public float militaryBudgetPercent = 2.5f;
        public int militaryDoctrine = 1;
        public float securitySpending = 0f;
        public List<MilitaryProject> militaryProjects = new List<MilitaryProject>();

        public float militaryBudgetUSD = 2.5f; // Billions
        public float militaryUpkeepUSD = 0f;

        public float militaryReadiness = 75f; // Percentage
        
        // Detailed Branch Data
        public BranchStats armyStats = new BranchStats("Army", 50000, 250, 75, 80);
        public BranchStats navyStats = new BranchStats("Navy", 12000, 45, 60, 75);
        public BranchStats airForceStats = new BranchStats("Air Force", 8000, 120, 65, 85);
        public BranchStats rdfStats = new BranchStats("Rapid Deployable Force", 5000, 80, 90, 95);

        [Header("Military — Logistics")]
        public float reserveFuel = 100f; // Days of supply
        public float reserveAmmo = 100f;
        public float reserveMedical = 100f;
        public float reserveFood = 100f;

        [Header("Military — Operations")]
        public List<MilitaryOperation> activeOperations = new List<MilitaryOperation>();
        public List<StateCraft.Systems.InsurgentCamp> activeInsurgentCamps = new List<StateCraft.Systems.InsurgentCamp>();
        public List<RecruitmentPipeline> trainingPipelines = new List<RecruitmentPipeline>();

        // War Stamina Tracking
        public string warEnemyName = "";      // Name of the country we are at war with
        public int warTurnsActive = 0;        // How many turns the war has been going
        public float warExhaustion = 0f;      // 0-100. As it rises, military effectiveness falls
        public float warWillToFight = 100f;   // 0-100. Public/political support for the war
        
        // KIA & Casualty Tracking (Phase 2)
        public int totalKIA = 0;              // Cumulative killed in action
        public int totalWounded = 0;          // Cumulative wounded
        public float warFatigueStabilityDrain = 0f; // How much stability is bleeding per turn due to war casualties

        // Chain of Command (Phase 2)
        public List<General> assignedGenerals = new List<General>();

        // Naval Blockades (Phase 2)
        public List<NavalBlockade> activeBlockades = new List<NavalBlockade>();
        public bool isBlockaded = false;       // True when an enemy blockade cuts our maritime trade

        // WMD Deterrence (Phase 2)
        public bool hasNuclearWeapon = false;
        public bool hasChemicalWeapon = false;
        public int wmdDevelopmentProgress = 0; // 0-100, built turn by turn
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

        [Header("=== Battle History ===")]
        public List<StateCraft.Systems.BattleResultMessage> battleResults = new List<StateCraft.Systems.BattleResultMessage>();

        [Header("Intelligence & Covert")]
        public float covertBudget = 0f; // Billions USD per turn
        public float counterIntelligence = 50f; // 0-100, defense against spies
        public float exposureRisk = 0f; // 0-100. If it hits 100, huge scandal
        public List<ForeignAsset> recruitedAssets = new List<ForeignAsset>();


        [Header("Geography")]
        public List<RegionDataSO> regions = new List<RegionDataSO>();

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
            // Upkeep in Billions of USD per month.
            // Rates calibrated so a 200k-force developing nation costs ~$3-4B/year total.

            // Land Forces
            float armyUpkeep = (unitInfantry    * 0.0000020f)   // ~$2,000/soldier/month
                             + (unitRifles       * 0.0000002f)   // ~$200/rifle/month (ammo, maintenance)
                             + (unitTanks        * 0.000030f)    // ~$30k/tank/month
                             + (unitAPCs         * 0.000010f)    // ~$10k/APC/month
                             + (unitArtillery    * 0.000015f);   // ~$15k/gun/month

            // Air Force
            float airForceUpkeep = (airForceStats.personnel * 0.000006f)  // ~$6k/person/month
                                 + (unitFighters       * 0.000150f)        // ~$150k/fighter/month
                                 + (unitBombers        * 0.000280f)        // ~$280k/bomber/month
                                 + (unitAttackHelis    * 0.000080f)        // ~$80k/heli/month
                                 + (unitUAVs           * 0.000020f);       // ~$20k/UAV/month

            // Navy
            float navyUpkeep = (navyStats.personnel  * 0.000005f)   // ~$5k/person/month
                             + (unitFrigates          * 0.000400f)   // ~$400k/frigate/month
                             + (unitDestroyers        * 0.000800f)   // ~$800k/destroyer/month
                             + (unitSubmarines        * 0.001000f)   // ~$1M/submarine/month
                             + (unitPatrolBoats       * 0.000040f);  // ~$40k/boat/month

            // Rapid Deployable Force (elite, so slightly higher per-head)
            float rdfUpkeep = (rdfStats.personnel * 0.000010f)   // ~$10k/person/month
                            + (rdfStats.equipment  * 0.000050f);

            // Fixed base infrastructure costs (reduced)
            float basesUpkeep = (baseArmyCount      * 0.006f)   // ~$6M/base/month
                              + (baseNavyCount       * 0.010f)   // ~$10M/base/month
                              + (baseAirForceCount   * 0.010f)
                              + (baseRDFCount        * 0.015f);

            // Scale unit upkeep by branch readiness (lower readiness = lower active costs)
            float totalUpkeep = armyUpkeep     * (armyStats.readiness     / 100f)
                              + airForceUpkeep * (airForceStats.readiness  / 100f)
                              + navyUpkeep     * (navyStats.readiness      / 100f)
                              + rdfUpkeep      * (rdfStats.readiness       / 100f)
                              + basesUpkeep;

            // Minimum command & admin overhead: $100M/month
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
        public string assetName; // e.g. "Minister of Defense", "Central Bank Insider"
        public float loyalty; // 0-100
        public int intelLevelProvided; // 1-5
    }

    [System.Serializable]
    public class ActiveProject
    {
        public string projectName;
        public string projectType; // "Infra", "Agri", "Tech", "Special"
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
        public string projectName;       // e.g. "Army Expansion", "Naval Frigate"
        public string branch;            // "Army", "Navy", "AirForce", "RDF"
        public float strengthGain;       // How much strength it adds on completion
        public float costBillions;       // USD cost
        public int monthsRemaining;
        public int totalMonths;
    }

    [System.Serializable]
    public class BranchStats
    {
        public string branchName;
        public int personnel;
        public int equipment;
        public float readiness; // 0-100
        public float morale; // 0-100

        public BranchStats(string name, int p, int e, float r, float m)
        {
            branchName = name; personnel = p; equipment = e; readiness = r; morale = m;
        }
    }


    [System.Serializable]
    public class MilitaryOperation
    {
        public string opName;
        public string regionName;
        public string type; // "Offensive", "Defensive", "Air Patrol", "Naval Patrol", "COIN"
        public string status; // "Active", "Stalled", "Success", "Failed"
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
        public float securityBoostRate;
        
        public bool isDecommissioning;
        public int turnsToDeploy;
    }

    [System.Serializable]
    public class CovertOperation
    {
        public string opName;
        public string type; // "Sabotage", "Assassination", "Propaganda", "Economic Disruption", "Arms Supply"
        public string target;
        public string operativeName;
        public string riskLevel; // "Low", "Medium", "High", "Extreme"
        public string status;

        public int currentPhase = 1;
        public int totalPhases = 3;
        public int monthsRemaining = 3;
        public float detectionRisk = 15f;
    }

    [System.Serializable]
    public class IntelligenceThreat
    {
        public string threatName;
        public string category; // "Foreign Interference", "Insurgent Activity", "Internal Threats"
        public string severity; // "Minor", "Major", "Critical"
        public string description;
    }

    [System.Serializable]
    public class DiplomaticTreaty
    {
        public string treatyName;
        public string partnerCountry;
        public string terms;
        public int monthsRemaining;
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
        
        // Optional icon or color info
        public string urgency; // "Normal", "Important", "Critical"
    }

    [System.Serializable]
    public class GlobalCrisis
    {
        public string crisisName;
        public string affectedRegion;
        public string severity; // "Minor", "Severe", "Catastrophic"
        public float internationalPressure; // 0-100
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
        public int rank;                     // 1-5 stars
        public GeneralSpecialty specialty;
        public float effectivenessBonus;     // e.g. 0.15 = +15% combat dmg
        public float moraleBonus;            // e.g. 0.10 = +10% branch morale
        public string assignedOperation;     // Op name they are leading
        public bool isDeployed;
    }

    [System.Serializable]
    public class NavalBlockade
    {
        public string blockerCountry;
        public string targetCountry;
        public int monthsActive;
        public float tradeSuppressionPercent; // 0-100
        public int frigatesAssigned;
        public int destroyersAssigned;
        public int submarinesAssigned;
    }
}
