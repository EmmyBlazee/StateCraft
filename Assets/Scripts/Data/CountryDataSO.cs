using System.Collections.Generic;
using UnityEngine;

namespace StateCraft.Data
{
    [CreateAssetMenu(fileName = "NewCountry", menuName = "StateCraft/Country Data")]
    public class CountryDataSO : ScriptableObject
    {
        [Header("=== NEW Military System ===")]
        public StateCraft.Systems.MilitaryState militaryState = new StateCraft.Systems.MilitaryState();

        private void OnEnable()
        {
            if (militaryState == null) militaryState = new StateCraft.Systems.MilitaryState();
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
        public float invTechGoods = 0f;

        [Header("Intelligence Dashboard Data")]
        public float intelligenceLevel = 10f;
        public float intelligenceBudget = 50f;
        public int satelliteCount = 0;
        public int satellitesUnderConstruction = 0;
        public int satelliteBuildMonthsRemaining = 0;

        public int operativeCount = 12;
        public float globalIntelCoverage = 15f;
        public int foreignAgentsDetected = 0;
        public float agencyIntegrity = 85f;
        public List<CovertOperation> activeCovertOps = new List<CovertOperation>();
        public List<IntelligenceThreat> detectedThreats = new List<IntelligenceThreat>();

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
        public float budgetMilitaryShare = 23f;
        public float budgetIntelligenceShare = 7f;
        public float budgetEducationShare = 14f;
        public float budgetHealthShare = 12f;
        public float budgetInfrastructureShare = 18f;
        public float budgetAgricultureShare = 9f;
        public float budgetEnergyShare = 8f;
        public float budgetWelfareShare = 5f;
        public string lastTaxReformSummary = "";
        public string lastBudgetDraftSummary = "";
        public string lastAdviserDecision = "";
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
        public List<StateCraft.Systems.InsurgentCamp> activeInsurgentCamps = new List<StateCraft.Systems.InsurgentCamp>();
        public List<RecruitmentPipeline> trainingPipelines = new List<RecruitmentPipeline>();

        public string warEnemyName = "";
        public int warTurnsActive = 0;
        public float warExhaustion = 0f;
        public float warWillToFight = 100f;
        
        public int totalKIA = 0;
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

        [Header("=== Battle History ===")]
        public List<StateCraft.Systems.BattleResultMessage> battleResults = new List<StateCraft.Systems.BattleResultMessage>();

        [Header("Intelligence & Covert")]
        public float covertBudget = 0f;
        public float counterIntelligence = 50f;
        public float exposureRisk = 0f;
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

            float totalUpkeep = armyUpkeep     * (armyStats.readiness     / 100f)
                              + airForceUpkeep * (airForceStats.readiness  / 100f)
                              + navyUpkeep     * (navyStats.readiness      / 100f)
                              + rdfUpkeep      * (rdfStats.readiness       / 100f)
                              + basesUpkeep;

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
        public float securityBoostRate;
        
        public bool isDecommissioning;
        public int turnsToDeploy;
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
        public float detectionRisk = 15f;
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
}
