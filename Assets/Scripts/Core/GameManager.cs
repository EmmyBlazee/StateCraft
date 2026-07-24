using UnityEngine;
using StateCraft.Interfaces;
using StateCraft.Systems;
using StateCraft.Data;
using StateCraft.UI;
using StateCraft.Map;
using System.Collections.Generic;

namespace StateCraft.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Global Data")]
        public List<CountryDataSO> allCountries;
        public CountryDataSO playerCountry;
        public float playerBudget = 1000f;

        [Header("Economy Database")]
        public Data.Economy.EconomyDatabaseSO EconomyDatabase;


        // Phase 2: Global World Tension (0-100). Rises with wars, falls with diplomacy.
        public float worldTension = 20f;

        // Systems
        [Header("Time System")]
        public bool isRealTime = true;
        public float turnDuration = 5f; // Seconds per turn
        private float turnTimer = 0f;

        [Header("Event Data")]
        public List<Data.GameEventSO> allPossibleEvents;

        public ITurnSystem TurnSystem { get; private set; }
        public IEconomySystem EconomySystem { get; private set; }
        public ISecuritySystem SecuritySystem { get; private set; }
        public IInsurgencySystem InsurgencySystem { get; private set; }
        public IWarSystem WarSystem { get; private set; }
        public IDiplomacySystem DiplomacySystem { get; private set; }
        public IAICountrySystem AICountrySystem { get; private set; }
        public IIntelligenceSystem IntelligenceSystem { get; private set; }
        public IEventSystem EventSystem { get; private set; }
        public IElectionSystem ElectionSystem { get; private set; }
        public IDebtSystem DebtSystem { get; private set; }
        public InboxSystem InboxSystem { get; private set; }
        public VictorySystem VictorySystem { get; private set; }
        public IThreatAssessmentSystem ThreatAssessmentSystem { get; private set; }
        public Data.RelationsMatrix Relations { get; private set; } = new Data.RelationsMatrix();
        public Systems.EconomyLedger Ledger { get; private set; } = new Systems.EconomyLedger();
        
        public NewsSystem NewsSystem { get; private set; }

        public bool hasGameEnded { get; private set; }
        public event System.Action<bool, string, string, string> OnGameOver; // isVictory, title, summary, stats

        public void TriggerGameOver(bool isVictory, string title, string summary, string stats)
        {
            if (hasGameEnded) return;
            hasGameEnded = true;
            isRealTime = false;
            OnGameOver?.Invoke(isVictory, title, summary, stats);
        }

        private void Start()
        {
            // Reset state
            hasGameEnded = false;
        }

        private void Awake()
        {
if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
InitializeSystems();
}
            else if (Instance != this)
            {
Destroy(gameObject);
                return; // Stop execution immediately
            }
        }

        private void InitializeSystems()
        {
            if (allCountries != null)
            {
                foreach (var country in allCountries)
                {
                    if (country != null)
                    {
                        SafeClearActiveOperations(country);
                        country.trainingPipelines?.Clear();
                        country.activeCovertOps?.Clear();
                        country.detectedThreats?.Clear();

                        // Reset exchange rate to baseline — prevents bloated rates from persisting across play sessions
                        if (country.baseExchangeRate > 0f)
                            country.exchangeRate = country.baseExchangeRate;
                    }
                }
            }

            if (playerCountry != null)
            {
                SafeClearActiveOperations(playerCountry);
                playerCountry.trainingPipelines?.Clear();
                playerCountry.activeCovertOps?.Clear();
                playerCountry.detectedThreats?.Clear();

                if (playerCountry.baseExchangeRate > 0f)
                    playerCountry.exchangeRate = playerCountry.baseExchangeRate;
            }

            // --- Save Clamping: Fix old broken save states ---
            // BUG 4 FIX: Clamp bounds updated to match GDP-in-billions scale.
            // Old bound of 20000f for gdp made sense as raw USD only if gdp was ~$20 trillion max.
            // Now gdp is in billions so 20000f = $20 trillion which is still a fine upper cap,
            // but the reset value changes from 1000f (raw) to 477f (billions).
            // Similarly debt/foreign/domestic reset to the new default billion-scale values.
            foreach (var country in allCountries)
            {
                if (country != null)
                {
                    if (float.IsNaN(country.treasury) || float.IsInfinity(country.treasury) || Mathf.Abs(country.treasury) > 10000f) country.treasury = 12f;
                    if (float.IsNaN(country.gdp) || float.IsInfinity(country.gdp) || country.gdp > 50000f || country.gdp < 0f) country.gdp = 477f;
                    if (float.IsNaN(country.debt) || float.IsInfinity(country.debt) || Mathf.Abs(country.debt) > 20000f) country.debt = 95f;
                    if (float.IsNaN(country.foreignDebt) || float.IsInfinity(country.foreignDebt) || Mathf.Abs(country.foreignDebt) > 20000f) country.foreignDebt = 35f;
                    if (float.IsNaN(country.domesticDebt) || float.IsInfinity(country.domesticDebt) || Mathf.Abs(country.domesticDebt) > 20000f) country.domesticDebt = 60f;
                    if (float.IsNaN(country.income) || float.IsInfinity(country.income) || Mathf.Abs(country.income) > 500f) country.income = 8f;
                    if (float.IsNaN(country.spending) || float.IsInfinity(country.spending) || Mathf.Abs(country.spending) > 500f) country.spending = 6f;
                    
                    if (country == playerCountry)
                    {
                        if (float.IsNaN(playerBudget) || float.IsInfinity(playerBudget) || Mathf.Abs(playerBudget) > 10000f) playerBudget = country.treasury;
                        else playerBudget = country.treasury;
                    }
                }
            }

            TurnSystem = new TurnSystem();
            EconomySystem = new EconomySystem();
            SecuritySystem = new SecuritySystem(TurnSystem);
            InsurgencySystem = new InsurgencySystem();
            WarSystem = new WarSystem();
            DiplomacySystem = new DiplomacySystem();
            AICountrySystem = new AICountrySystem();
            IntelligenceSystem = new IntelligenceSystem();
            EventSystem = new EventSystem(allPossibleEvents);
            ElectionSystem = new ElectionSystem();
            DebtSystem = new DebtSystem();
            InboxSystem = new InboxSystem();
            VictorySystem = new VictorySystem();
            ThreatAssessmentSystem = new ThreatAssessmentSystem();

            TurnSystem.OnTurnEnded  += ProcessTick;   // fires every 3 days — lightweight only
            TurnSystem.OnMonthEnded += ProcessMonth;  // fires once per calendar month — heavy sim
            TurnSystem.OnMonthEnded += () => Relations.SnapshotHistory();

            // ── Apply scenario briefing text to all countries ──
            ScenarioBriefings.Apply(allCountries);

            // Cleanly acquire NewsSystem — rely wholly on the Instance since it is a static singleton initialized in Awake.
            NewsSystem = Systems.NewsSystem.Instance;
        }

        private void SafeClearActiveOperations(CountryDataSO country)
        {
            if (country == null || country.activeOperations == null) return;
            foreach (var op in country.activeOperations)
            {
                if (op == null) continue;
                var contrList = op.GetContributionsMigrated();
                foreach (var c in contrList)
                {
                    MilitaryOperation.ReturnToReserves(country, c.branch, c.GetUnitCountsDict(), c.personnelAssigned);
                }
            }
            country.activeOperations.Clear();
            country.operationalLogs?.Clear();
        }

        // ── Fires every 3 game-days — lightweight, UI-facing updates only ──
        private void ProcessTick()
        {
            // Re-resolve singletons as safety net
            if (NewsSystem == null) NewsSystem = Systems.NewsSystem.Instance;
            if (playerCountry == null) playerCountry = MapManager.Instance?.currentSelectedCountry;

foreach (var country in allCountries)
            {
                // War status runs every tick (internally month-gated via _lastWarMonthCounted)
                WarSystem.UpdateWarStatus(country);

                // Naval blockades and WMD development run every tick (small per-tick effects)
                if (WarSystem is Systems.WarSystem warSys)
                {
                    warSys.ProcessNavalBlockades(country);
                    warSys.ProcessWMDDevelopment(country);
                }

                // Process Covert/Recon Operations
                if (country.activeCovertOps != null && country.activeCovertOps.Count > 0)
                {
                    for (int i_op = country.activeCovertOps.Count - 1; i_op >= 0; i_op--)
                    {
                        var op = country.activeCovertOps[i_op];
                        if (op.daysRemaining > 0)
                        {
                            op.daysRemaining--;
                            if (op.daysRemaining <= 0)
                            {
                                // Operation completes!
                                string report = $"RECON REPORT ({op.priority}): Enemy movement detected in Sector 4. 2x Mechanized Brigades spotted.";
                                if (country == playerCountry)
                                {
                                    NotificationManager.Instance?.AddNotification(report, new UnityEngine.Color(1.0f, 0.64f, 0.0f));
                                }
                                
                                if (country.recentReconReports == null) country.recentReconReports = new System.Collections.Generic.List<string>();
                                country.recentReconReports.Add($"[{TurnSystem?.CurrentDate ?? "Unknown Date"}] {op.opName} - {report}");
                                
                                country.activeCovertOps.RemoveAt(i_op);
                                
                                // Ideally we refresh the UI if it's open, but the player might not be on the tab
                                // The feed will pick it up next time it is drawn.
                            }
                        }
                    }                }

                // Active operations give security boost every tick (makes them feel responsive)
                if (country.activeOperations != null)
                {
                    bool isShortOfWeapons = country.unitRifles < country.armyStats.personnel;
                    if (isShortOfWeapons && country == playerCountry)
                        NotificationManager.Instance?.AddNotification(
                            $"CRITICAL WEAPONS SHORTAGE: Insufficient Service Rifles to equip active forces " +
                            $"({country.unitRifles:N0} rifles for {country.armyStats.personnel:N0} army personnel). Operations running at 50% efficiency!",
                            Color.red);

                    foreach (var op in country.activeOperations)
                    {
                        var reg = country.regions.Find(r => r.regionName == op.regionName);
                        if (reg != null)
                        {
                            float boost = (op.securityBoostRate > 0f ? op.securityBoostRate : 2.5f) / 10f; // scaled to per-tick
                            if (isShortOfWeapons) boost *= 0.5f;
                            reg.security  = Mathf.Min(100f, reg.security  + boost);
                            reg.stability = Mathf.Min(100f, reg.stability + boost * 0.6f);
                        }
                    }
                }
            }
        }

        // ── Fires once per calendar month — all heavy simulation ──
        private void ProcessMonth()
        {
            // Auto-save periodically (e.g., at the start of every new year)
            if (TurnSystem != null && TurnSystem.CurrentTurn > 1 && TurnSystem.CurrentTurn % 12 == 1)
            {
                SaveGame();
            }

            if (NewsSystem == null) NewsSystem = Systems.NewsSystem.Instance;
            if (playerCountry == null) playerCountry = MapManager.Instance?.currentSelectedCountry;

            foreach (var country in allCountries)
            {
                // 0. New Military System Tick
                StateCraft.Systems.MilitaryManager.Instance?.ProcessMonthlyTick(country);

                // 0.1 Dynamic Procurement Market (Refreshes every 3 months)
                if (TurnSystem != null && TurnSystem.CurrentTurn % 3 == 0)
                {
                    StateCraft.Systems.ProcurementMarketSystem.RefreshMarket(country);
                }

                // 1. Core Economic Update (monthly — GDP growth, income, spending)
                EconomySystem?.UpdateEconomy(country);

                // 2. Security maintenance and decay
                SecuritySystem.UpdateSecurity(country);

                // 3. Insurgency simulation
                InsurgencySystem.SimulateInsurgency(country);

                // 4. Intelligence
                country.intelligenceFunds += country.intelligenceBudget;
                IntelligenceSystem.SimulateIntelligence(country);

                // 5. Debt interest (monthly compounding)
                DebtSystem.UpdateDebt(country);

                // 6. AI for non-player countries
                if (country != playerCountry)
                    AICountrySystem.RunAI(country);

                // 7. Random events (5% per month, not per tick)
                EventSystem.CheckForEvents(country);

                // 8. Public approval and elections
                ApprovalSystem.ProcessApproval(country);
                ElectionSystem.CheckForElection(country);

                // 9. Monthly trade settlement
                DiplomacySystem?.ProcessTradeDeals(country);

                // 9b. Resolve pending treaty proposals (AI responds after 1-3 turns)
                if (country == playerCountry && DiplomacySystem is StateCraft.Systems.DiplomacySystem dipSysForTreaties)
                    dipSysForTreaties.ResolvePendingTreaties(country, this);

                // 10. Stability aggregate
                UpdateCountryStability(country);

                // 11. Security recalculation and corruption
                SecuritySystem.RecalculateSecurity(country);
                CorruptionSystem.ProcessCorruption(country);

                // 12. Player treasury update (monthly net income/spending)
                if (country == playerCountry)
                {
                    float incomeLocal        = country.income * country.exchangeRate;
                    float spendingLocal      = country.spending * country.exchangeRate;
                    float domesticInterestLocal = DebtSystem != null ? DebtSystem.CalculateDomesticInterest(country) * country.exchangeRate : 0;
                    float foreignInterestUSD    = DebtSystem != null ? DebtSystem.CalculateForeignInterest(country) : 0;

                    float serviceMultiplier = country.debtServiceRatio / 100f;
                    float actualDomesticPaidLocal = domesticInterestLocal * serviceMultiplier;
                    float actualForeignPaidUSD    = foreignInterestUSD    * serviceMultiplier;

                    if (serviceMultiplier < 1.0f)
                    {
                        float unpaidForeignUSD = foreignInterestUSD * (1.0f - serviceMultiplier);
                        country.foreignDebt += unpaidForeignUSD * 1.5f;
                        country.stability   -= (1.0f - serviceMultiplier) * 15f;
                        if (Random.value > 0.8f)
                            NewsSystem.Instance?.PublishNews($"DEBT ALERT: {country.countryName} enters partial default on international obligations.");
                    }
                    else if (serviceMultiplier > 1.0f)
                    {
                        float extraPayUSD = foreignInterestUSD * (serviceMultiplier - 1.0f);
                        country.foreignDebt = Mathf.Max(0, country.foreignDebt - extraPayUSD);
                        country.stability  += 0.5f;
                    }

                    float foreignInterestLocal = actualForeignPaidUSD * country.exchangeRate;
                    float netChangeRawLocal    = incomeLocal - spendingLocal - actualDomesticPaidLocal - foreignInterestLocal;
                    float corruptionLeakLocal  = CorruptionSystem.GetLeakageAmount(country);
                    netChangeRawLocal         -= corruptionLeakLocal;

                    if (float.IsNaN(netChangeRawLocal) || float.IsInfinity(netChangeRawLocal)) netChangeRawLocal = 0f;
                    if (float.IsNaN(playerBudget)      || float.IsInfinity(playerBudget))      playerBudget      = 12f;

                    playerBudget += netChangeRawLocal;
                    
                    // Deduct monthly active operations funding
                    if (country.activeOperations != null && country.activeOperations.Count > 0)
                    {
                        float totalOpCostUSD = 0f;
                        foreach (var op in country.activeOperations)
                            totalOpCostUSD += (op.costPerMonth > 0f) ? (op.costPerMonth / 1000f) : 0.05f;
                        float totalOpCostLocal = totalOpCostUSD * country.exchangeRate;
                        playerBudget -= totalOpCostLocal;
                        string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
}

                    country.treasury = playerBudget;
                    VictorySystem.CheckVictory(playerCountry, allCountries, TurnSystem.CurrentTurn);
                }

                ProcessRecruitmentPipelines(country);
                ProcessMilitaryProjects(country);
                ProcessProductionOrders(country);
                ProcessProcurementContracts(country);
                ProcessFacilityConstruction(country);
                ProcessFacilityUpgrades(country);
            }

            // 13. Monthly news generation
            if (NewsSystem == null) NewsSystem = Systems.NewsSystem.Instance;
            if (NewsSystem != null)
                NewsSystem.GenerateTurnNews(allCountries);
            else
// 14. Player briefings
            if (playerCountry != null)
                StateCraft.Systems.GameCommunicator.ProcessTurnBriefing(playerCountry, TurnSystem.CurrentTurn);

}

        private void ProcessFacilityConstruction(CountryDataSO country)
        {
            if (country.facilityConstructionOrders == null) return;
            for (int i = country.facilityConstructionOrders.Count - 1; i >= 0; i--)
            {
                var order = country.facilityConstructionOrders[i];
                order.monthsRemaining--;
                if (order.monthsRemaining <= 0)
                {
                    if (order.facilityType == 1) // Factory
                    {
                        var newFac = new StateCraft.Data.DefenseFactory
                        {
                            id = order.id,
                            factoryName = order.facilityName,
                            produceType = order.specialization,
                            level = 1,
                            utilizationPct = 0
                        };
                        country.defenseFactories.Add(newFac);
                        country.baseDefenseFactoryCount++;
                        if (country == playerCountry) NotificationManager.Instance?.AddNotification($"CONSTRUCTION COMPLETE: {order.facilityName} is now operational.", Color.green);
                    }
                    else // 0 = Base, 2 = Training
                    {
                        string roleStr = order.facilityType == 2 ? "Training" : "Combat";
                        var newBase = new StateCraft.Data.MilitaryBase(order.id, order.facilityName, order.regionName, order.branch, order.capacity, roleStr);
                        country.militaryBases.Add(newBase);
                        if (order.branch == "ARMY") country.baseArmyCount++;
                        else if (order.branch == "NAVY") country.baseNavyCount++;
                        else if (order.branch == "AIR") country.baseAirForceCount++;
                        else country.baseRDFCount++;
                        if (country == playerCountry) NotificationManager.Instance?.AddNotification($"CONSTRUCTION COMPLETE: {order.facilityName} is now operational.", Color.green);
                    }
                    country.facilityConstructionOrders.RemoveAt(i);
                }
            }
        }

        private void ProcessFacilityUpgrades(CountryDataSO country)
        {
            if (country.facilityUpgradeOrders == null) return;
            for (int i = country.facilityUpgradeOrders.Count - 1; i >= 0; i--)
            {
                var order = country.facilityUpgradeOrders[i];
                order.monthsRemaining--;
                if (order.monthsRemaining <= 0)
                {
                    // Find the facility and upgrade it
                    var fac = country.defenseFactories?.Find(f => f.id == order.facilityId);
                    if (fac != null)
                    {
                        fac.level++;
                        if (country == playerCountry) NotificationManager.Instance?.AddNotification($"UPGRADE COMPLETE: {fac.factoryName} is now Level {fac.level}.", Color.green);
                    }
                    else
                    {
                        var mbase = country.militaryBases?.Find(b => b.id == order.facilityId);
                        if (mbase != null)
                        {
                            mbase.capacity += 500;
                            mbase.upkeepCost = mbase.capacity * 0.005f;
                            if (country == playerCountry) NotificationManager.Instance?.AddNotification($"UPGRADE COMPLETE: {mbase.baseName} capacity increased by 500.", Color.green);
                        }
                    }
                    country.facilityUpgradeOrders.RemoveAt(i);
                }
            }
        }

        private void ProcessRecruitmentPipelines(CountryDataSO country)
        {
            if (country.trainingPipelines == null) return;

            for (int i = country.trainingPipelines.Count - 1; i >= 0; i--)
            {
                var pipe = country.trainingPipelines[i];
                pipe.monthsRemaining--;

                if (pipe.monthsRemaining <= 0)
                {
                    // Graduation! Add the troops to standing force
                    switch (pipe.id)
                    {
                        case "INF": 
                            country.unitInfantry += pipe.amount; 
                            country.armyStats.personnel += pipe.amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 0.5f);
                            break;
                        case "POLICE": 
                            country.nationalPoliceSize += pipe.amount; 
                            country.security = Mathf.Min(100f, country.security + 2.0f);
                            foreach (var r in country.regions)
                                r.security = Mathf.Min(100f, r.security + 1.5f);
                            break;
                        case "NAVY_PERS": 
                            country.navyStats.personnel += pipe.amount; 
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 0.4f);
                            break;
                        case "AIR_PERS": 
                            country.airForceStats.personnel += pipe.amount; 
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 0.3f);
                            break;
                        case "RDF_PERS": 
                            country.rdfStats.personnel += pipe.amount; 
                            country.rdfReadiness = Mathf.Min(100f, country.rdfReadiness + 0.5f);
                            break;
                    }

                    // Update combined strength
                    country.militaryStrength = (country.armyStrength * 0.5f) + (country.navyStrength * 0.2f) + (country.airForceStrength * 0.3f);

                    if (pipe.isPersonnel)
                    {
                        country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 0.5f);
                    }

                    // Send high-priority graduation notification
                    if (country == playerCountry)
                    {
                        NotificationManager.Instance?.AddNotification($"GRADUATION: {pipe.amount:N0} newly trained {pipe.unitLabel} completed their 6-month instruction and deployed!", Color.green);
                    }

                    // Publish news globally so players see other countries building up forces too!
                    NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} has completed training of {pipe.amount:N0} {pipe.unitLabel} active duty forces.");

                    country.trainingPipelines.RemoveAt(i);
                }
                else
                {
                    if (country == playerCountry)
                    {
}
                }
            }
        }

        private void ProcessProductionOrders(CountryDataSO country)
        {
            if (country.productionOrders == null) return;

            for (int i = country.productionOrders.Count - 1; i >= 0; i--)
            {
                var order = country.productionOrders[i];
                UnityEngine.Debug.Log($"[Backend] Processing order {order.id}. Months remaining: {order.monthsRemaining} -> {order.monthsRemaining - 1}");
                order.monthsRemaining--;

                if (country == playerCountry)
                {
                    playerBudget -= order.monthlyCostB;
                    country.treasury = playerBudget;
                }
                else
                {
                    country.treasury -= order.monthlyCostB * country.exchangeRate;
                }

                if (order.monthsRemaining <= 0)
                {
                    switch (order.id)
                    {
                        case "RIFLE": country.unitRifles += order.quantity; break;
                        case "APC": country.unitAPCs += order.quantity; break;
                        case "TNK": country.unitTanks += order.quantity; break;
                        case "ARTILLERY": country.unitArtillery += order.quantity; break;
                        case "UAV": country.unitUAVs += order.quantity; break;
                        case "FTR": country.unitFighters += order.quantity; break;
                        case "BOMBER": country.unitBombers += order.quantity; break;
                        case "HEL": country.unitAttackHelis += order.quantity; break;
                        case "PATROL_BOAT": country.unitPatrolBoats += order.quantity; break;
                        case "FRG": country.unitFrigates += order.quantity; break;
                        case "SUB": country.unitSubmarines += order.quantity; break;
                        case "DESTROYER": country.unitDestroyers += order.quantity; break;
                    }

                    if (country.completedProductions == null) country.completedProductions = new System.Collections.Generic.List<StateCraft.Data.CompletedProductionItem>();
                    string curDate = TurnSystem is TurnSystem ts ? ts.CurrentDate : "Unknown Date";
                    country.completedProductions.Insert(0, new StateCraft.Data.CompletedProductionItem
                    {
                        unitLabel = order.unitLabel,
                        quantity = order.quantity,
                        completedDate = curDate
                    });
                    
                    if (country.completedProductions.Count > 10)
                        country.completedProductions.RemoveAt(country.completedProductions.Count - 1);

                    if (country == playerCountry)
                    {
                        UI.NotificationManager.Instance?.AddNotification($"PRODUCTION COMPLETE: {order.quantity:N0} {order.unitLabel} delivered to Arsenal!", UnityEngine.Color.green);
                        UnityEngine.Debug.LogWarning($"[Arsenal Math - Production] Added {order.quantity:N0} {order.unitLabel} to Arsenal.");
                    }
                    if (NewsSystem != null)
                    {
                        NewsSystem.PublishNews($"MILITARY: {country.countryName} has completed production of {order.quantity:N0} {order.unitLabel}.");
                    }

                    country.productionOrders.RemoveAt(i);
                }
            }
        }

        private void ProcessProcurementContracts(CountryDataSO country)
        {
            if (country.activeContracts == null) return;

            for (int i = country.activeContracts.Count - 1; i >= 0; i--)
            {
                var contract = country.activeContracts[i];
                contract.monthsRemaining--;

                // Deduct monthly installment
                if (country == playerCountry)
                {
                    playerBudget -= contract.monthlyCostB;
                    country.treasury = playerBudget;
                }
                else
                {
                    country.treasury -= contract.monthlyCostB * country.exchangeRate;
                }

                if (contract.monthsRemaining <= 0)
                {
                    switch (contract.produceType)
                    {
                        case "RIFLE": country.unitRifles += contract.unitQuantity; break;
                        case "APC": country.unitAPCs += contract.unitQuantity; break;
                        case "TNK": country.unitTanks += contract.unitQuantity; break;
                        case "ARTILLERY": country.unitArtillery += contract.unitQuantity; break;
                        case "UAV": country.unitUAVs += contract.unitQuantity; break;
                        case "FTR": country.unitFighters += contract.unitQuantity; break;
                        case "BOMBER": country.unitBombers += contract.unitQuantity; break;
                        case "HEL": country.unitAttackHelis += contract.unitQuantity; break;
                        case "PATROL_BOAT": country.unitPatrolBoats += contract.unitQuantity; break;
                        case "FRG": country.unitFrigates += contract.unitQuantity; break;
                        case "SUB": country.unitSubmarines += contract.unitQuantity; break;
                        case "DESTROYER": country.unitDestroyers += contract.unitQuantity; break;
                    }

                    if (country == playerCountry)
                    {
                        UI.NotificationManager.Instance?.AddNotification($"PROCUREMENT DELIVERED: {contract.unitLabel} from {contract.supplier} has arrived!", UnityEngine.Color.green);
                        UnityEngine.Debug.LogWarning($"[Arsenal Math - Procurement] Added {contract.unitQuantity:N0} {contract.unitLabel} to Arsenal from {contract.supplier}.");
                    }
                    if (NewsSystem != null)
                    {
                        NewsSystem.PublishNews($"MILITARY: {country.countryName} has received delivery of {contract.unitLabel} from {contract.supplier}.");
                    }

                    country.activeContracts.RemoveAt(i);
                }
                else
                {
                    // Update status dynamically
                    if (contract.monthsRemaining <= 1) contract.status = "In Transit";
                    else if (contract.monthsRemaining <= contract.monthsTotal / 2) contract.status = "Production Phase";
                }
            }
        }

        private void ProcessMilitaryProjects(CountryDataSO country)
        {
            if (country.militaryProjects == null) return;

            for (int i = country.militaryProjects.Count - 1; i >= 0; i--)
            {
                var proj = country.militaryProjects[i];
                proj.monthsRemaining--;

                if (proj.monthsRemaining <= 0)
                {
                    // Apply strength gain
                    if (proj.branch == "Army") country.armyStrength = Mathf.Min(100f, country.armyStrength + proj.strengthGain);
                    else if (proj.branch == "Navy") country.navyStrength = Mathf.Min(100f, country.navyStrength + proj.strengthGain);
                    else if (proj.branch == "AirForce") country.airForceStrength = Mathf.Min(100f, country.airForceStrength + proj.strengthGain);
                    else if (proj.branch == "RDF") country.rdfReadiness = Mathf.Min(100f, country.rdfReadiness + proj.strengthGain);

                    // Update combined strength
                    country.militaryStrength = (country.armyStrength * 0.5f) + (country.navyStrength * 0.2f) + (country.airForceStrength * 0.3f);

                    if (country == playerCountry)
                    {
                        NotificationManager.Instance?.AddNotification($"PROJECT COMPLETE: {proj.projectName} is now operational! (+{proj.strengthGain:F0}% {proj.branch} Strength)", Color.green);
                    }
                    NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} has successfully completed the {proj.projectName} strategic project.");

                    country.militaryProjects.RemoveAt(i);
                }
            }
        }

        private void UpdateCountryStability(CountryDataSO country)
        {
            float averageRegionStability = 0;
            foreach (var region in country.regions)
            {
                averageRegionStability += region.stability;
            }

            if (country.regions.Count > 0)
            {
                country.stability = averageRegionStability / country.regions.Count;
            }

            // BUG 4 FIX: GDP is in billions — debt > gdp comparison is now apples-to-apples
            if (country.debt > country.gdp) country.stability -= 5f;
            if (country.inflationRate > 0.1f) country.stability -= 2f;

            // Punishment for late budget allocation
            if (TurnSystem != null && (TurnSystem.CurrentTurn - country.lastBudgetApprovalTurn) >= 4)
            {
                int monthsOverdue = (TurnSystem.CurrentTurn - country.lastBudgetApprovalTurn) - 3;
                country.stability -= (3f + monthsOverdue); // Increasing penalty over time
            }

            country.stability = Mathf.Clamp(country.stability, 0f, 100f);
        }

        private void Update()
        {
            if (Instance != this) return;
            if (TurnSystem == null) return;

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F5)) SaveGame();
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F9)) LoadGame();

            if (isRealTime)
            {
                turnTimer += Time.deltaTime;
                if (turnTimer >= turnDuration)
                {
                    EndTurn();
                    turnTimer = 0f;
                }
            }
        }

        public void EndTurn()
        {
            if (TurnSystem == null) return;
            TurnSystem.AdvanceTurn();
        }
        public CountryDataSO GetCountryByName(string name)
        {
            if (allCountries == null) return null;
            return allCountries.Find(c => c.countryName == name);
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
        public void SaveGame()
        {
            var data = new StateCraft.Data.GameSaveData();
            if (TurnSystem is TurnSystem ts) {
                data.currentTurn = ts.CurrentTurn;
                data.currentDay = ts.currentDay;
            }
            data.worldTension = this.worldTension;
            data.playerCountryName = this.playerCountry != null ? this.playerCountry.countryName : "";

            foreach (var c in allCountries) {
                if (c != null) data.countryJsonData.Add(UnityEngine.JsonUtility.ToJson(c));
            }

            if (Relations != null) {
                var rels = Relations.ExportRelations();
                foreach(var r in rels) data.relationsMatrix.Add(UnityEngine.JsonUtility.ToJson(r));
            }

            if (Ledger != null) {
                var ledgers = Ledger.ExportLedger();
                foreach(var l in ledgers) data.economyLedgers.Add(UnityEngine.JsonUtility.ToJson(l));
            }

            // Export ISaveable Systems
            if (WarSystem is ISaveable warSaveable)
            {
                data.systemStateKeys.Add("WarSystem");
                data.systemStateValues.Add(warSaveable.ExportState());
            }
            if (DiplomacySystem is ISaveable dipSaveable)
            {
                data.systemStateKeys.Add("DiplomacySystem");
                data.systemStateValues.Add(dipSaveable.ExportState());
            }
            if (EventSystem is ISaveable eventSaveable)
            {
                data.systemStateKeys.Add("EventSystem");
                data.systemStateValues.Add(eventSaveable.ExportState());
            }
            if (InsurgencySystem is ISaveable insSaveable)
            {
                data.systemStateKeys.Add("InsurgencySystem");
                data.systemStateValues.Add(insSaveable.ExportState());
            }

            string json = UnityEngine.JsonUtility.ToJson(data);
            System.IO.File.WriteAllText(UnityEngine.Application.persistentDataPath + "/savegame.json", json);
            
            if (Systems.NewsSystem.Instance != null)
                Systems.NewsSystem.Instance.PublishNews("SYSTEM: Quick Save Successful.");
            if (UI.NotificationManager.Instance != null)
                UI.NotificationManager.Instance.AddNotification("GAME SAVED TO DISK", Color.green);
        }

        public void LoadGame()
        {
            string path = UnityEngine.Application.persistentDataPath + "/savegame.json";
            if (!System.IO.File.Exists(path)) return;

            string json = System.IO.File.ReadAllText(path);
            var data = UnityEngine.JsonUtility.FromJson<StateCraft.Data.GameSaveData>(json);

            if (TurnSystem is TurnSystem ts) {
                ts.CurrentTurn = data.currentTurn;
                ts.currentDay = data.currentDay;
            }
            this.worldTension = data.worldTension;
            this.playerCountry = GetCountryByName(data.playerCountryName);

            // Restore countries
            for(int i=0; i < data.countryJsonData.Count; i++) {
                if (i < allCountries.Count && allCountries[i] != null)
                    UnityEngine.JsonUtility.FromJsonOverwrite(data.countryJsonData[i], allCountries[i]);
            }

            // Restore relations
            if (Relations != null) {
                var rels = new List<StateCraft.Data.BilateralRelation>();
                foreach(var rJson in data.relationsMatrix) rels.Add(UnityEngine.JsonUtility.FromJson<StateCraft.Data.BilateralRelation>(rJson));
                Relations.ImportRelations(rels);
            }

            // Restore ledgers
            if (Ledger != null) {
                var ledgers = new List<StateCraft.Systems.LedgerSaveWrapper>();
                foreach(var lJson in data.economyLedgers) ledgers.Add(UnityEngine.JsonUtility.FromJson<StateCraft.Systems.LedgerSaveWrapper>(lJson));
                Ledger.ImportLedger(ledgers);
            }

            // Import ISaveable Systems
            for (int i = 0; i < data.systemStateKeys.Count; i++)
            {
                if (i >= data.systemStateValues.Count) break;
                string key = data.systemStateKeys[i];
                string val = data.systemStateValues[i];

                if (key == "WarSystem" && WarSystem is ISaveable warSaveable) warSaveable.ImportState(val);
                if (key == "DiplomacySystem" && DiplomacySystem is ISaveable dipSaveable) dipSaveable.ImportState(val);
                if (key == "EventSystem" && EventSystem is ISaveable evSaveable) evSaveable.ImportState(val);
                if (key == "InsurgencySystem" && InsurgencySystem is ISaveable insSaveable) insSaveable.ImportState(val);
            }
            
            if (Systems.NewsSystem.Instance != null)
                Systems.NewsSystem.Instance.PublishNews("SYSTEM: Quick Load Successful. Welcome back.");
            if (UI.NotificationManager.Instance != null)
                UI.NotificationManager.Instance.AddNotification("GAME LOADED FROM DISK", Color.yellow);
        }
    }

    public class TurnSystem : ITurnSystem
    {
        public int CurrentTurn { get; set; } = 1;
        public int currentDay = 1;
        public System.Action OnTurnEnded { get; set; }
        public System.Action OnMonthEnded { get; set; }

        public string CurrentDate
        {
            get
            {
                // Start date: Jan 2026
                int startYear = 2026;
                int monthsSinceStart = Mathf.Max(0, CurrentTurn - 1);
                
                int yearsToAdd = monthsSinceStart / 12;
                int monthIndex = monthsSinceStart % 12;

                string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
                return $"{currentDay:D2} {months[monthIndex]} {startYear + yearsToAdd}";
            }
        }

        public void AdvanceTurn()
        {
            currentDay++;

            if (GameManager.Instance != null && GameManager.Instance.playerCountry != null)
            {
                GameManager.Instance.EventSystem?.TickPendingEvents(GameManager.Instance.playerCountry);
            }

            // Use real month lengths for the date display
            int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int monthIndex = Mathf.Max(0, CurrentTurn - 1) % 12;
            int daysThisMonth = daysInMonth[monthIndex];

            if (currentDay > daysThisMonth)
            {
                currentDay = 1;
                CurrentTurn++;
                OnMonthEnded?.Invoke();
                OnTurnEnded?.Invoke();
            }
            // Fire news/economy update every 3 days without changing the month counter
            else if (currentDay % 3 == 0)
            {
                OnTurnEnded?.Invoke();
            }
        }
    }
}

namespace StateCraft.Data
{
    [System.Serializable]
    public class GameSaveData
    {
        public int currentTurn;
        public int currentDay;
        public float worldTension;
        public string playerCountryName;
        public System.Collections.Generic.List<string> countryJsonData = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> relationsMatrix = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> economyLedgers = new System.Collections.Generic.List<string>();
        
        // Custom Systems
        public System.Collections.Generic.List<string> systemStateKeys = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> systemStateValues = new System.Collections.Generic.List<string>();
    }
}