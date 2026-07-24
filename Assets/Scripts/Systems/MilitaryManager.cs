using System;
using System.Collections.Generic;
using UnityEngine;
using StateCraft.Data;
using StateCraft.UI;
using StateCraft.Core;

namespace StateCraft.Systems
{
    public class MilitaryManager : MonoBehaviour
    {
        public static MilitaryManager Instance { get; private set; }

        [SerializeField] private MilitaryConfigSO config;

        public static event Action<BattleReport> OnBattleReport;
        public static event Action<string, float> OnReadinessChanged;
        public static event Action<string> OnOperationComplete;
        public static event Action<RecruitmentPipelineNew> OnRecruitmentComplete;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ProcessMonthlyTick(CountryDataSO country)
        {
            var mil = country.militaryState;
            if (config == null) return; // Wait until config is hooked up

            TickRecruitment(country);
            ProcessWeaponDegradation(country);
            RecalculateReadiness(country);

            float fleetR = NavyCalculator.FleetReadiness(
                mil.navy, country.unitPatrolBoats,
                country.unitFrigates, country.unitDestroyers, config);
            mil.navalMissions.RecalculateEffects(fleetR, mil.navy.marines);

            if (mil.navalMissions.offshoreResourceGuard)
                country.invOilBarrels = Mathf.RoundToInt(
                    country.invOilBarrels * mil.navalMissions.offshoreRevenueBonus);

            ProcessOperationTicks(country);
            ProcessProactiveOperations(country);
            ProcessSupplyContracts(country);

            if (country.rdfCooldownMonths > 0) country.rdfCooldownMonths--;
        }

        private void ProcessSupplyContracts(CountryDataSO country)
        {
            if (country.activeSupplyContracts == null || country.activeSupplyContracts.Count == 0) return;

            var done = new List<StateCraft.Data.SupplyContract>();

            foreach (var sc in country.activeSupplyContracts)
            {
                // Resources supplied per turn
                switch (sc.resourceType)
                {
                    case "FUEL": country.reserveFuel = Mathf.Min(100f, country.reserveFuel + sc.amountPerTurn); break;
                    case "AMMO": country.reserveAmmo = Mathf.Min(100f, country.reserveAmmo + sc.amountPerTurn); break;
                    case "MEDICAL": country.reserveMedical = Mathf.Min(100f, country.reserveMedical + sc.amountPerTurn); break;
                    case "FOOD": country.reserveFood = Mathf.Min(100f, country.reserveFood + sc.amountPerTurn); break;
                }

                sc.turnsRemaining--;

                if (sc.turnsRemaining <= 0)
                {
                    done.Add(sc);
                }
            }

            foreach (var sc in done)
            {
                country.activeSupplyContracts.Remove(sc);
                NotificationManager.Instance?.AddNotification($"Supply Contract Completed: {sc.label} has expired.", Color.yellow);
            }
        }

        public void RecruitPersonnel(CountryDataSO country, string type, int count)
        {
            if (!CanAffordRecruitment(country, type, count))
            {
                Debug.LogWarning($"[Military] Cannot afford {count} {type} recruitment.");
                return;
            }

            var pipeline = new RecruitmentPipelineNew
            {
                personnelType = type,
                count = count,
                trainingMonthsRemaining = GetTrainingMonths(type),
                costPerTurn = GetMonthlyCost(type) * count
            };

            country.militaryState.pipelines.Add(pipeline);
        }

        private void TickRecruitment(CountryDataSO country)
        {
            var mil = country.militaryState;
            var done = new List<RecruitmentPipelineNew>();

            foreach (var p in mil.pipelines)
            {
                country.treasury -= p.costPerTurn / 12f;
                p.Tick();
                if (p.IsComplete) done.Add(p);
            }

            foreach (var p in done)
            {
                DeliverPersonnel(country, p);
                mil.pipelines.Remove(p);
                OnRecruitmentComplete?.Invoke(p);
            }
        }

        private void DeliverPersonnel(CountryDataSO country, RecruitmentPipelineNew p)
        {
            var army = country.militaryState.army;
            var af = country.militaryState.airForce;
            var nav = country.militaryState.navy;

            switch (p.personnelType)
            {
                case "Infantry": army.infantry += p.count; break;
                case "RDF": army.rdfPersonnel += p.count; break;
                case "Pilot": af.pilots += p.count; break;
                case "GroundCrew": af.groundCrew += p.count; break;
                case "AirController": af.airControllers += p.count; break;
                case "Sailor": nav.sailors += p.count; break;
                case "NavalOfficer": nav.navalOfficers += p.count; break;
                case "Marine": nav.marines += p.count; break;
                case "TechnicalCrew": nav.technicalCrew += p.count; break;
            }
        }

        public void RecalculateReadiness(CountryDataSO country)
        {
            var mil = country.militaryState;

            int totalAircraft = country.unitFighters + country.unitBombers + country.unitAttackHelis;
            float budgetRatio = country.militaryBudgetPercent / 3.0f;

            mil.armyReadiness.personnel = country.unitInfantry > 0 ? Mathf.Clamp01((float)mil.army.infantry / country.unitInfantry) * 100f : 0f;
            mil.armyReadiness.funding = Mathf.Clamp01(budgetRatio) * 100f;

            int requiredGroundCrew = (country.unitFighters * config.groundCrewPerFighter) + (country.unitBombers * config.groundCrewPerBomber) + (country.unitAttackHelis * config.groundCrewPerHeli);

            mil.airForceReadiness.personnel = requiredGroundCrew > 0 ? Mathf.Clamp01((float)(mil.airForce.pilots + mil.airForce.groundCrew) / (country.unitFighters + requiredGroundCrew)) * 100f : 0f;
            mil.airForceReadiness.funding = Mathf.Clamp01(budgetRatio) * 100f;

            float fleetR = NavyCalculator.FleetReadiness(mil.navy, country.unitPatrolBoats, country.unitFrigates, country.unitDestroyers, config);
            mil.navyReadiness.personnel = fleetR * 100f;
            mil.navyReadiness.funding = Mathf.Clamp01(budgetRatio) * 100f;

            OnReadinessChanged?.Invoke("Army", mil.armyReadiness.OverallReadiness);
            OnReadinessChanged?.Invoke("AirForce", mil.airForceReadiness.OverallReadiness);
            OnReadinessChanged?.Invoke("Navy", mil.navyReadiness.OverallReadiness);

            country.militaryReadiness = (int)mil.armyReadiness.OverallReadiness;

            // Sync legacy fields so the existing UI seamlessly reflects new data!
            country.armyStats.readiness = mil.armyReadiness.OverallReadiness;
            country.airForceStats.readiness = mil.airForceReadiness.OverallReadiness;
            country.navyStats.readiness = mil.navyReadiness.OverallReadiness;
            country.armyStats.morale = mil.armyReadiness.morale;
            country.armyStats.personnel = mil.army.infantry + mil.army.rdfPersonnel;
            country.navyStats.personnel = mil.navy.sailors + mil.navy.navalOfficers + mil.navy.marines + mil.navy.technicalCrew;
            country.airForceStats.personnel = mil.airForce.pilots + mil.airForce.groundCrew + mil.airForce.airControllers;
            
            // Also sync top level unit fields heavily relied on by GameManager/WarSystem
            country.unitInfantry = mil.army.infantry;
            country.rdfStats.personnel = mil.army.rdfPersonnel;
        }

        public bool DeployRDF(CountryDataSO country, OperationType opType, string targetRegion, int turn)
        {
            var mil = country.militaryState;

            if (!ArmyCalculator.CanDeployRDF(mil.army.rdfPersonnel, country.rdfCooldownMonths, mil.armyReadiness)) return false;

            var op = new ActiveOperation
            {
                operationName = $"RDF Op: {opType} in {targetRegion}",
                type = opType,
                targetRegion = targetRegion,
                turnsRemaining = opType == OperationType.HostageRescue ? 0 : 1,
                usedRDF = true,
                successProbability = CalcRDFSuccessProbability(country, opType)
            };

            mil.activeOperations.Add(op);
            country.rdfCooldownMonths = config.rdfCooldownMonths;
            country.lastRDFTurn = turn;

            if (op.turnsRemaining == 0) ResolveOperation(country, op, turn);
            return true;
        }

        private float CalcRDFSuccessProbability(CountryDataSO country, OperationType opType)
        {
            float base_p = country.militaryState.armyReadiness.OverallReadiness / 100f;
            float rdfStr = Mathf.Clamp01(country.militaryState.army.rdfPersonnel / 3000f);

            return opType switch
            {
                OperationType.HostageRescue => base_p * 0.7f + rdfStr * 0.3f,
                OperationType.CoupSuppression => base_p * 0.8f + rdfStr * 0.2f,
                OperationType.CounterTerrorism => base_p * 0.6f + rdfStr * 0.4f,
                OperationType.BorderPatrol => base_p * 0.9f,
                _ => base_p
            };
        }

        private void ProcessOperationTicks(CountryDataSO country)
        {
            int turn = country.warTurnsActive;
            var completed = new List<ActiveOperation>();

            foreach (var op in country.militaryState.activeOperations)
            {
                op.turnsRemaining--;
                if (op.turnsRemaining <= 0) completed.Add(op);
            }

            foreach (var op in completed)
            {
                ResolveOperation(country, op, turn);
                country.militaryState.activeOperations.Remove(op);
            }
        }

        public void ProcessProactiveOperations(CountryDataSO country)
        {
            if (country.activeOperations == null || country.activeOperations.Count == 0) return;

            string dateStr = GameManager.Instance?.TurnSystem != null ? GameManager.Instance.TurnSystem.CurrentDate : "Unknown";
            bool isPlayer = country == GameManager.Instance?.playerCountry;

            foreach (var op in country.activeOperations)
            {
                if (string.IsNullOrEmpty(op.commander))
                    op.commander = "Unassigned";
            }

            for (int i = country.activeOperations.Count - 1; i >= 0; i--)
            {
                var op = country.activeOperations[i];

                if (op.turnsToDeploy > 0)
                {
                    op.turnsToDeploy--;
                    UnityEngine.Debug.Log($"[DEPLOYMENT TIMER TICK] {op.opName} ({op.regionName}): {op.turnsToDeploy} month(s) remaining until full combat readiness.");

                    if (isPlayer)
                    {
                        NotificationManager.Instance?.AddNotification($"DEPLOYMENT TRANSIT [{op.opName.ToUpper()}]: {op.turnsToDeploy} month(s) remaining.", Color.cyan);
                        StateCraft.Systems.NewsSystem.Instance?.PublishNews($"OPERATIONAL TRANSIT: {country.countryName} forces en route to {op.regionName}. Deployment ETA: {op.turnsToDeploy} month(s).", false);
                    }

                    if (op.turnsToDeploy == 0)
                    {
                        op.phase = "Advancing";
                        op.status = "Active";
                        UnityEngine.Debug.Log($"[DEPLOYMENT COMPLETE] {op.opName} in {op.regionName}: All units reached forward positions! Status set to ACTIVE.");
                        if (isPlayer)
                        {
                            NotificationManager.Instance?.AddNotification($"DEPLOYMENT COMPLETE: {op.opName} in {op.regionName} is now ACTIVE!", new Color(0.14f, 0.93f, 0.34f));
                            StateCraft.Systems.NewsSystem.Instance?.PublishNews($"DEPLOYMENT COMPLETE: {country.countryName} forces have fully arrived in {op.regionName}. Operation {op.opName} is now in the ADVANCING phase.", true);
                            InboxSystem.Instance?.SendMessage(country, "FIELD COMMAND",
                                $"DEPLOYMENT COMPLETE: {op.opName}",
                                $"{op.commander} reports all units have reached their forward positions in {op.regionName}. Operation is now in the Advancing phase.",
                                "Important");
                        }
                    }
                    continue;
                }

                op.turnsActive++;

                if (op.cooldownAirstrike > 0) op.cooldownAirstrike--;
                if (op.cooldownRecon > 0) op.cooldownRecon--;
                if (op.cooldownIntel > 0) op.cooldownIntel--;

                string prevPhase = op.phase;

                if (op.turnsActive == 2 && op.phase == "Advancing")
                {
                    op.phase = "Engaged";
                    op.enemyStrengthEst = Mathf.Clamp(op.enemyStrengthEst + UnityEngine.Random.Range(-10f, 15f), 10f, 100f);
                }

                if (op.phase == "Engaged" || op.phase == "Securing")
                {
                    float enemyPressure = op.enemyStrengthEst / 100f;
                    var contrList = op.GetContributionsMigrated();

                    foreach (var c in contrList)
                    {
                        float monthlyAttrition = c.personnelAssigned * UnityEngine.Random.Range(0.005f, 0.02f) * (1f + enemyPressure);
                        int troopLoss = Mathf.RoundToInt(monthlyAttrition);
                        if (c.personnelAssigned > 10)
                        {
                            troopLoss = Mathf.Min(troopLoss, c.personnelAssigned - 10);
                        }
                        else
                        {
                            troopLoss = 0;
                        }

                        if (troopLoss > 0)
                        {
                            c.personnelAssigned -= troopLoss;
                            op.attritionTroops += troopLoss;
                            op.casualtiesSustained += troopLoss;
                            c.casualtiesSuffered += troopLoss;

                            int enemyLoss = Mathf.RoundToInt(troopLoss * UnityEngine.Random.Range(1.2f, 2.8f));
                            op.enemyCasualtiesInflicted += enemyLoss;
                        }

                        var dict = c.GetUnitCountsDict();
                        var keys = new List<string>(dict.Keys);
                        List<string> equipLostNames = new List<string>();

                        foreach (var key in keys)
                        {
                            int count = dict[key];
                            if (count > 0)
                            {
                                float wearChance = key == "Tanks" ? 0.3f : (key == "APCs" ? 0.2f : 0.15f);
                                if (UnityEngine.Random.value < wearChance)
                                {
                                    c.SetUnitCount(key, count - 1);
                                    op.attritionEquipment++;
                                    c.equipmentLost++;
                                    equipLostNames.Add($"1 {key}");

                                    if (key == "Tanks") country.oosTanks++;
                                    else if (key == "APCs") country.oosAPCs++;
                                    else if (key == "Fighters") country.oosFighters++;
                                    else if (key == "Bombers") country.oosBombers++;
                                    else if (key == "Helis") country.oosAttackHelis++;
                                    else if (key == "UAVs") country.oosUAVs++;
                                    else if (key == "Frigates") country.oosFrigates++;
                                    else if (key == "Submarines") country.oosSubmarines++;
                                    else if (key == "Destroyers") country.oosDestroyers++;
                                    else if (key == "PatrolBoats") country.oosPatrolBoats++;
                                    else if (key == "Artillery") country.oosArtillery++;
                                    else if (key == "Rifles") country.oosRifles++;
                                }
                            }
                        }

                        if (troopLoss > 0 || equipLostNames.Count > 0)
                        {
                            string myEqStr = equipLostNames.Count > 0 ? $", {string.Join(", ", equipLostNames)} Lost" : "";
                            NewsSystem.Instance?.PublishNews(
                                $"WAR CASUALTY REPORT [{op.opName.ToUpper()} - {op.regionName.ToUpper()}]: " +
                                $"{country.countryName}: {troopLoss:N0} Personnel KIA{myEqStr} | Hostile Forces: {op.enemyCasualtiesInflicted:N0} Total Enemy KIA.", true);
                        }
                    }

                    op.SyncFlatFields();

                    int originalTroops = op.deployedTroops + op.attritionTroops;
                    op.effectiveTroopStrength = originalTroops > 0
                        ? Mathf.Clamp((float)op.deployedTroops / originalTroops * 100f, 0f, 100f)
                        : 0f;

                    if (op.effectiveTroopStrength < 60f && !op.needsReinforcement)
                    {
                        op.needsReinforcement = true;
                        op.needsReinforcementBranches.Clear();
                        foreach (var c in contrList)
                        {
                            if (c.casualtiesSuffered > 0 && !op.needsReinforcementBranches.Contains(c.branch))
                            {
                                op.needsReinforcementBranches.Add(c.branch);
                            }
                        }
                        if (op.needsReinforcementBranches.Count == 0)
                        {
                            foreach (var c in contrList)
                            {
                                op.needsReinforcementBranches.Add(c.branch);
                            }
                        }

                        if (isPlayer)
                        {
                            string branchListStr = string.Join(", ", op.needsReinforcementBranches);
                            InboxSystem.Instance?.SendMessage(country, "FIELD COMMAND",
                                $"REINFORCEMENTS NEEDED: {op.opName}",
                                $"{op.commander} reports that {op.opName} effective strength has fallen to {op.effectiveTroopStrength:F0}% due to combat attrition. " +
                                $"Branches needing reinforcements: {branchListStr}. " +
                                $"{op.attritionTroops:N0} troops lost to date. Use the REINFORCE button to send additional forces.",
                                "Critical");
                        }
                    }

                    if (op.effectiveTroopStrength < 25f)
                    {
                        op.status = "Stalled";
                        if (isPlayer && op.lastNotification != "STALLED")
                        {
                            op.lastNotification = "STALLED";
                            InboxSystem.Instance?.SendMessage(country, "FIELD COMMAND",
                                $"OP STALLED: {op.opName}",
                                $"{op.opName} has stalled. Effective troop strength is at only {op.effectiveTroopStrength:F0}%. " +
                                $"The enemy is regaining ground. Reinforce immediately or consider withdrawing to avoid further losses.",
                                "Critical");
                        }
                    }
                    else if (op.status == "Stalled" && op.effectiveTroopStrength >= 40f)
                    {
                        op.status = "Active";
                        op.needsReinforcement = false;
                        op.needsReinforcementBranches.Clear();
                        op.lastNotification = "";
                    }
                }

                if (op.phase == "Engaged" || op.phase == "Securing")
                {
                    var opRegion = country.regions.Find(r => r.regionName == op.regionName);
                    float strengthFactor = op.effectiveTroopStrength / 100f;
                    float rdfBonus = op.deployedRDF > 0 ? (op.deployedRDF / 500f) * 0.5f : 0f;
                    float enemyResistance = opRegion != null ? opRegion.insurgencyLevel / 100f : 0.5f;

                    float progressGain = UnityEngine.Random.Range(1f, 5f) * strengthFactor * (1f - enemyResistance * 0.6f);
                    progressGain += rdfBonus * 2f;
                    if (op.status == "Stalled") progressGain = Mathf.Min(progressGain, 0f);
                    op.objectiveProgress = Mathf.Clamp(op.objectiveProgress + progressGain, 0f, 100f);

                    if (op.objectiveProgress >= 50f && op.phase == "Engaged")
                    {
                        op.phase = "Securing";
                        op.territoryCaptured = Mathf.Clamp(op.territoryCaptured + UnityEngine.Random.Range(15f, 35f), 0f, 100f);
                    }

                    if (opRegion != null && op.phase != "Stalled")
                    {
                        float insurgencyReduction = strengthFactor * UnityEngine.Random.Range(0.3f, 1.2f);
                        opRegion.insurgencyLevel = Mathf.Max(0f, opRegion.insurgencyLevel - insurgencyReduction);
                        opRegion.security = Mathf.Clamp(opRegion.security + strengthFactor * 0.5f, 0f, 100f);
                    }
                }

                op.friendlyCasualtyHistory.Add(op.casualtiesSustained);
                op.enemyCasualtyHistory.Add(op.enemyCasualtiesInflicted);
                
                float dynamicStrength = 1.0f + (op.deployedTroops / 1000f) * 0.5f + op.deployedTanks * 0.8f
                               + op.deployedAPCs * 0.4f + op.deployedFighters * 2.0f + op.deployedHelis * 1.0f
                               + op.deployedFrigates * 1.5f + op.deployedSubmarines * 2.0f
                               + (op.deployedRDF / 1000f) * 3.5f + op.deployedBombers * 3.0f + op.deployedUAVs * 1.2f;
                float baseRate = Mathf.Clamp(40f + dynamicStrength * 1.8f, 10f, 95f);
                baseRate += (op.morale - 70f) * 0.1f;
                baseRate -= (op.enemyStrengthEst - 50f) * 0.15f;
                if (country.unitRifles < country.armyStats.personnel) baseRate *= 0.85f;
                op.successChanceHistory.Add(Mathf.Clamp(baseRate, 5f, 95f));

                if (op.objectiveProgress >= 100f)
                {
                    var contrList = op.GetContributionsMigrated();
                    foreach (var c in contrList)
                    {
                        MilitaryOperation.ReturnToReserves(country, c.branch, c.GetUnitCountsDict(), c.personnelAssigned);
                    }

                    string outcome = op.attritionTroops > op.casualtiesSustained / 2 ? "Pyrrhic Victory" : "Victory";
                    var completedOp = new StateCraft.Data.CompletedMilitaryOperation
                    {
                        opName = op.opName,
                        regionName = op.regionName,
                        type = op.type,
                        outcome = outcome,
                        durationMonths = op.turnsActive,
                        casualtiesSustained = op.casualtiesSustained,
                        equipmentLost = op.attritionEquipment,
                        territoryCaptured = op.territoryCaptured,
                        politicalImpact = 5,
                        dateCompleted = GameManager.Instance?.TurnSystem != null ? GameManager.Instance.TurnSystem.CurrentDate : "Unknown",
                        rewards = "Increased Regional Stability"
                    };

                    country.completedOperations.Add(completedOp);
                    while (country.completedOperations.Count > 20)
                        country.completedOperations.RemoveAt(0);

                    // Resolve recommendations when completed successfully!
                    country.activeRecommendations.RemoveAll(r => r.regionName == op.regionName);

                    country.activeOperations.RemoveAt(i);

                    if (isPlayer)
                    {
                        InboxSystem.Instance?.SendMessage(country, "FIELD COMMAND",
                            $"OP COMPLETE: {op.opName}",
                            $"Operation {op.opName} has achieved its objectives in {op.regionName} after {op.turnsActive} months. " +
                            $"Total casualties: {op.casualtiesSustained:N0}. Equipment lost: {op.attritionEquipment}. " +
                            $"All surviving forces have returned to reserve.",
                            "Success");
                        NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} declares {outcome} in {op.opName}, securing {op.regionName} after {op.turnsActive} months.");
                    }
                    continue;
                }

                float moraleDrift = op.phase == "Engaged" ? UnityEngine.Random.Range(-3f, 1f) : UnityEngine.Random.Range(-1f, 2f);
                op.morale = Mathf.Clamp(op.morale + moraleDrift, 20f, 100f);

                if (isPlayer && op.phase != prevPhase)
                {
                    string msg = op.phase switch {
                        "Engaged"  => $"SITREP: {op.opName} has entered active combat in {op.regionName}. Enemy resistance is {(op.enemyStrengthEst > 60f ? "strong" : "moderate")}.",
                        "Securing" => $"SITREP: {op.opName} is now securing {op.regionName}. Objective progress: {op.objectiveProgress:F0}%. {op.commander} recommends maintaining pressure and reinforcing if needed.",
                        _          => $"SITREP: {op.opName} has transitioned to {op.phase} phase."
                    };
                    InboxSystem.Instance?.SendMessage(country, "FIELD COMMAND", $"OP STATUS: {op.opName} — {op.phase.ToUpper()}", msg, "Important");
                    NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName}'s {op.opName} has entered the {op.phase} phase in {op.regionName}.");
                }

                if (isPlayer && op.turnsActive > 0 && op.turnsActive % 6 == 0)
                {
                    InboxSystem.Instance?.SendMessage(country, "FIELD COMMAND",
                        $"6-MONTH SITREP: {op.opName}",
                        $"{op.opName} has been running for {op.turnsActive} months. " +
                        $"Objective progress: {op.objectiveProgress:F0}%. Effective strength: {op.effectiveTroopStrength:F0}%. " +
                        $"Total casualties: {op.casualtiesSustained:N0}. Morale: {op.morale:F0}%. " +
                        (op.needsReinforcement ? "⚠ REINFORCEMENTS REQUIRED." : "Force is holding."),
                        "Important");
                }

                if (isPlayer && country.reserveAmmo < 30f && op.lastNotification != "SUPPLY_AMMO")
                {
                    op.lastNotification = "SUPPLY_AMMO";
                    InboxSystem.Instance?.SendMessage(country, "LOGISTICS COMMAND",
                        $"SUPPLY CRITICAL: {op.opName}",
                        $"Ammunition reserves have dropped below 30 units. {op.opName} may lose effectiveness. Resupply via the Logistics tab immediately.",
                        "Critical");
                }
                if (isPlayer && country.reserveFuel < 20f && op.lastNotification != "SUPPLY_FUEL")
                {
                    op.lastNotification = "SUPPLY_FUEL";
                    InboxSystem.Instance?.SendMessage(country, "LOGISTICS COMMAND",
                        $"FUEL CRITICAL: {op.opName}",
                        $"Fuel reserves have dropped below 20 units. Air assets and armoured units in {op.opName} may be grounded. Resupply via the Logistics tab.",
                        "Critical");
                }

                if (op.phase == "Engaged" && UnityEngine.Random.value < 0.20f && isPlayer)
                {
                    int counterCasualties = UnityEngine.Random.Range(50, 300);
                    op.casualtiesSustained += counterCasualties;
                    foreach (var c in op.GetContributionsMigrated())
                    {
                        if (op.deployedTroops > 0)
                        {
                            int branchLoss = Mathf.RoundToInt(counterCasualties * ((float)c.personnelAssigned / op.deployedTroops));
                            branchLoss = Mathf.Clamp(branchLoss, 0, c.personnelAssigned);
                            c.personnelAssigned -= branchLoss;
                            c.casualtiesSuffered += branchLoss;
                        }
                    }
                    op.morale = Mathf.Max(20f, op.morale - UnityEngine.Random.Range(5f, 15f));
                    InboxSystem.Instance?.SendMessage(country, "FIELD COMMAND",
                        $"COUNTERATTACK: {op.opName} under fire",
                        $"Enemy forces launched a coordinated counterattack on {op.opName} positions in {op.regionName}. {counterCasualties} casualties reported. {op.commander} has requested reinforcements.",
                        "Critical");
                }

                bool isReconOp = op.type.Contains("RECON") || op.type == "AirRecon" || op.type == "AirStrike";
                if (isReconOp)
                {
                    var targetRegion = country.regions.Find(r => r.regionName == op.regionName);
                    if (targetRegion != null)
                    {
                        float intelGain = (op.deployedUAVs * 3f) + (op.deployedFighters * 1.5f) + (op.deployedAirForce / 100f * 2f);
                        intelGain = Mathf.Clamp(intelGain, 2f, 15f);
                        targetRegion.intelCoverage = Mathf.Clamp(targetRegion.intelCoverage + intelGain, 0f, 100f);

                        float actualStrength = targetRegion.insurgencyLevel;
                        float noise = Mathf.Max(0f, (100f - targetRegion.intelCoverage) / 100f) * 30f;
                        op.enemyStrengthEst = Mathf.Clamp(actualStrength + UnityEngine.Random.Range(-noise, noise), 5f, 100f);

                        if (targetRegion.intelCoverage > 50f && country.activeInsurgentCamps != null)
                        {
                            foreach (var camp in country.activeInsurgentCamps)
                            {
                                if (camp.regionName == op.regionName && !camp.isDetected)
                                {
                                    camp.isDetected = true;
                                    InboxSystem.Instance?.SendMessage(country, "INTELLIGENCE COMMAND",
                                        $"BOMBABLE TARGET IDENTIFIED: {camp.campType}",
                                        $"Reconnaissance has localized an insurgent {camp.campType} in {op.regionName}. Air assets can now perform targeted precision strikes.",
                                        "Important");
                                }
                            }
                        }

                        if (isPlayer && op.turnsActive % 2 == 0)
                        {
                            string intelQuality = targetRegion.intelCoverage >= 80f ? "HIGH CONFIDENCE"
                                                : targetRegion.intelCoverage >= 50f ? "MODERATE CONFIDENCE"
                                                : "LOW CONFIDENCE";
                            InboxSystem.Instance?.SendMessage(country, "INTELLIGENCE COMMAND",
                                $"INTEL UPDATE: {op.regionName}",
                                $"Air reconnaissance assets have updated the threat picture for {op.regionName}. " +
                                $"Intel coverage is now {targetRegion.intelCoverage:F0}% ({intelQuality}). " +
                                $"Estimated enemy strength: {op.enemyStrengthEst:F0}%.",
                                "Important");
                        }
                    }
                }

                if (op.deployedRDF > 0)
                {
                    var rdfRegion = country.regions.Find(r => r.regionName == op.regionName);
                    if (rdfRegion != null)
                    {
                        float supplyLineDamage = (op.deployedRDF / 500f) * UnityEngine.Random.Range(2f, 6f);
                        rdfRegion.insurgentSupplyLines = Mathf.Max(0f, rdfRegion.insurgentSupplyLines - supplyLineDamage);

                        float heartsBonus = (op.deployedRDF / 2000f) * UnityEngine.Random.Range(0.5f, 2f);
                        rdfRegion.heartsAndMinds = Mathf.Clamp(rdfRegion.heartsAndMinds + heartsBonus, 0f, 100f);

                        if (rdfRegion.insurgentSupplyLines < 30f)
                        {
                            float insurgencyHit = (op.deployedRDF / 1000f) * UnityEngine.Random.Range(1f, 3f);
                            rdfRegion.insurgencyLevel = Mathf.Max(0f, rdfRegion.insurgencyLevel - insurgencyHit);
                        }

                        if (isPlayer && rdfRegion.insurgentSupplyLines < 20f && op.turnsActive % 3 == 0)
                        {
                            InboxSystem.Instance?.SendMessage(country, "RDF COMMAND",
                                $"SUPPLY LINES DISRUPTED: {op.regionName}",
                                $"RDF units in {op.regionName} have cut insurgent supply lines to {rdfRegion.insurgentSupplyLines:F0}%. " +
                                $"Enemy resupply is critically impaired. Insurgency level: {rdfRegion.insurgencyLevel:F0}%.",
                                "Important");
                        }
                    }

                    float rdfMoraleBonus = (op.deployedRDF / 500f) * 0.5f;
                    op.morale = Mathf.Clamp(op.morale + rdfMoraleBonus, 0f, 100f);
                }
            }
        }

        private void ResolveOperation(CountryDataSO country, ActiveOperation op, int turn)
        {
            bool success = UnityEngine.Random.value < op.successProbability;
            BattleReport report = op.type switch
            {
                OperationType.HostageRescue => BattleReport.RDFHostageRescue(op.targetRegion, success, turn),
                OperationType.CoupSuppression => BattleReport.CoupSuppressed(op.targetRegion, op.usedRDF, turn),
                _ => null
            };

            if (report != null)
            {
                country.militaryState.battleLog.Add(report);
                OnBattleReport?.Invoke(report);
            }

            // Combat Decay: 5% of involved units go Out of Service, 10% if op failed
            float combatDecay = success ? 0.05f : 0.10f;
            if (op.usedRDF)
            {
                int brokenRifles = Mathf.FloorToInt(country.unitRifles * combatDecay * 0.1f); // just a fraction
                if (brokenRifles > 0)
                {
                    country.unitRifles -= brokenRifles;
                    country.oosRifles += brokenRifles;
                    NotificationManager.Instance?.AddNotification($"Combat Wear: {brokenRifles} rifles went Out of Service during {op.operationName}.", Color.yellow);
                }
            }

            OnOperationComplete?.Invoke(op.operationName);
        }

        private int GetTrainingMonths(string type) => type switch
        {
            "Infantry" => config.infantryTrainingMonths,
            "RDF" => config.rdfTrainingMonths,
            "Pilot" => 24, "GroundCrew" => 6, "AirController" => 12,
            "Sailor" => 6, "NavalOfficer" => 18, "Marine" => 9, "TechnicalCrew" => 12, _ => 3
        };

        private float GetMonthlyCost(string type) => type switch
        {
            "Infantry" => config.infantryCostPerUnitPerYear / 12f,
            "RDF" => config.rdfCostPerUnitPerYear / 12f,
            "Pilot" => config.pilotCostPerYear / 12f,
            "GroundCrew" => config.groundCrewCostPerYear / 12f,
            "AirController" => config.airControllerCostPerYear / 12f,
            "Sailor" => config.sailorCostPerYear / 12f,
            "NavalOfficer" => config.navalOfficerCostPerYear / 12f,
            "Marine" => config.marineCostPerYear / 12f,
            "TechnicalCrew" => config.techCrewCostPerYear / 12f, _ => 100f
        };

        private bool CanAffordRecruitment(CountryDataSO country, string type, int count)
        {
            float annualCost = GetMonthlyCost(type) * 12f * count;
            float annualIncome = country.income * 1_000_000_000f;
            return annualCost < annualIncome * 0.02f;
        }

        private void ProcessWeaponDegradation(CountryDataSO country)
        {
            // Base wear and tear
            float baseDecayRate = 0.015f; 
            
            // Repair offsets wear and tear. Max budget (100%) = 10% repair per month
            float repairRate = (country.militaryBudgetPercent / 100f) * 0.10f; 
            
            int totalRepaired = 0;
            int totalBroken = 0;

            void DegradeAndRepair(ref int active, ref int oos)
            {
                int broken = Mathf.FloorToInt(active * baseDecayRate);
                if (broken > 0)
                {
                    active -= broken;
                    oos += broken;
                    totalBroken += broken;
                }

                int repaired = Mathf.FloorToInt(oos * repairRate);
                if (repaired > 0 && oos > 0)
                {
                    int actualRepair = Mathf.Min(oos, repaired);
                    oos -= actualRepair;
                    active += actualRepair;
                    totalRepaired += actualRepair;
                }
            }

            DegradeAndRepair(ref country.unitRifles, ref country.oosRifles);
            DegradeAndRepair(ref country.unitAPCs, ref country.oosAPCs);
            DegradeAndRepair(ref country.unitTanks, ref country.oosTanks);
            DegradeAndRepair(ref country.unitArtillery, ref country.oosArtillery);
            DegradeAndRepair(ref country.unitFighters, ref country.oosFighters);
            DegradeAndRepair(ref country.unitBombers, ref country.oosBombers);
            DegradeAndRepair(ref country.unitAttackHelis, ref country.oosAttackHelis);
            DegradeAndRepair(ref country.unitUAVs, ref country.oosUAVs);
            DegradeAndRepair(ref country.unitFrigates, ref country.oosFrigates);
            DegradeAndRepair(ref country.unitDestroyers, ref country.oosDestroyers);
            DegradeAndRepair(ref country.unitSubmarines, ref country.oosSubmarines);
            DegradeAndRepair(ref country.unitPatrolBoats, ref country.oosPatrolBoats);

            if (totalBroken > 1000 && UnityEngine.Random.value < 0.2f)
                NotificationManager.Instance?.AddNotification($"Wear & Tear: {totalBroken} weapons became unserviceable this month.", Color.yellow);
                
            if (totalRepaired > 500 && UnityEngine.Random.value < 0.2f)
                NotificationManager.Instance?.AddNotification($"Maintenance: Engineering corps repaired {totalRepaired} weapons.", new Color(0f, 210/255f, 130/255f));
        }
    }
}
