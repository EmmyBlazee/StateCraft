using System;
using System.Collections.Generic;
using UnityEngine;
using StateCraft.Data;

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
            RecalculateReadiness(country);

            float fleetR = NavyCalculator.FleetReadiness(
                mil.navy, country.unitPatrolBoats,
                country.unitFrigates, country.unitDestroyers, config);
            mil.navalMissions.RecalculateEffects(fleetR, mil.navy.marines);

            if (mil.navalMissions.offshoreResourceGuard)
                country.invOilBarrels = Mathf.RoundToInt(
                    country.invOilBarrels * mil.navalMissions.offshoreRevenueBonus);

            ProcessOperationTicks(country);

            if (country.rdfCooldownMonths > 0) country.rdfCooldownMonths--;
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
    }
}
