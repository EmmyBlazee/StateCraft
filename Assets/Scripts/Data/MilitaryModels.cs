using System;
using System.Collections.Generic;
using UnityEngine;
using StateCraft.Data;

namespace StateCraft.Systems
{
    public enum MilitaryDoctrine
    {
        Defensive = 0,
        Aggressive = 1,
        Asymmetric = 2,
        Naval = 3,
    }

    public enum OperationType
    {
        CounterInsurgency, BorderPatrol, CoupSuppression, HostageRescue,
        CounterTerrorism, ConventionalAssault, DefensiveHold, MaritimePatrol,
        AirStrike, ReconFlight,
    }

    public enum BattleOutcome
    {
        MajorVictory, MinorVictory, Stalemate, MinorDefeat, MajorDefeat, Routing,
    }

    [Serializable]
    public class ArmyPersonnel
    {
        public int infantry;
        public int rdfPersonnel;
    }

    [Serializable]
    public class AirForcePersonnel
    {
        public int pilots;
        public int groundCrew;
        public int airControllers;
    }

    [Serializable]
    public class NavyPersonnel
    {
        public int sailors;
        public int navalOfficers;
        public int marines;
        public int technicalCrew;
    }

    [Serializable]
    public class BranchReadiness
    {
        [Range(0, 100)] public float personnel;
        [Range(0, 100)] public float training;
        [Range(0, 100)] public float equipment;
        [Range(0, 100)] public float funding;
        [Range(0, 100)] public float morale;

        public float OverallReadiness =>
            (personnel * 0.35f) + (training * 0.25f) +
            (equipment * 0.20f) + (funding * 0.12f) + (morale * 0.08f);

        public string ReadinessLabel()
        {
            float r = OverallReadiness;
            if (r >= 85) return "Combat Ready";
            if (r >= 70) return "Operational";
            if (r >= 50) return "Degraded";
            if (r >= 30) return "Marginal";
            return "Non-Functional";
        }
    }

    [Serializable]
    public class NavalMissions
    {
        public bool coastalPatrolActive;
        public bool antiPiracyMissionActive;
        public bool maritimeMonitoringActive;
        public bool offshoreResourceGuard;
        public bool seaTradeProtection;

        [NonSerialized] public float piraryIncidentReduction;
        [NonSerialized] public float tradeRouteProtection;
        [NonSerialized] public float navalIntelBonus;
        [NonSerialized] public float offshoreRevenueBonus;

        public void RecalculateEffects(float fleetReadiness, int marines)
        {
            float eff = fleetReadiness / 100f;
            piraryIncidentReduction = coastalPatrolActive ? eff * 0.7f : 0f;
            tradeRouteProtection = seaTradeProtection ? eff * 0.5f : 0f;
            navalIntelBonus = maritimeMonitoringActive ? eff * 0.4f : 0f;
            offshoreRevenueBonus = offshoreResourceGuard ? 1f + (eff * 0.15f) + NavyCalculator.CoastalDefenceBonus(marines) : 1f;
        }
    }

    [Serializable]
    public class ActiveOperation
    {
        public string operationName;
        public OperationType type;
        public string targetRegion;
        public int turnsRemaining;
        public bool usedRDF;
        public bool usedInfantry;
        public float successProbability;
    }

    [Serializable]
    public class WarState
    {
        public string enemyCountryName;
        public int turnsActive;
        public float warExhaustion;
        public float willToFight;
        public int totalKIA;
        public int totalWounded;
        public float frontlineAdvance;
        public bool airSuperiorityHeld;
        public string currentPhase;
        public List<BattleReport> recentBattles = new List<BattleReport>();
    }

    [Serializable]
    public class BattleReport
    {
        public string headline;
        public string details;
        public string phase;
        public int ourCasualties;
        public int enemyCasualties;
        public BattleOutcome outcome;
        public int turn;

        public static BattleReport InfantryCapture(string unit, string location, int casualties, int turn)
        {
            return new BattleReport { headline = $"The {unit} has captured {location}.", details = $"After {casualties} casualties, our forces have secured {location}. Supply lines are being established.", phase = "Offensive Push", ourCasualties = casualties, enemyCasualties = (int)(casualties * UnityEngine.Random.Range(1.2f, 2.5f)), outcome = BattleOutcome.MinorVictory, turn = turn };
        }
        public static BattleReport RDFHostageRescue(string location, bool success, int turn)
        {
            if (success) return new BattleReport { headline = $"RDF forces successfully rescued hostages in {location}.", details = "A rapid helicopter insertion by RDF operators neutralised the threat cell.", phase = "Special Operation", ourCasualties = UnityEngine.Random.Range(0, 3), enemyCasualties = UnityEngine.Random.Range(6, 18), outcome = BattleOutcome.MajorVictory, turn = turn };
            else return new BattleReport { headline = $"RDF rescue operation in {location} failed.", details = "Enemy forces were alerted before RDF could breach.", phase = "Special Operation", ourCasualties = UnityEngine.Random.Range(3, 12), enemyCasualties = UnityEngine.Random.Range(2, 8), outcome = BattleOutcome.MinorDefeat, turn = turn };
        }
        public static BattleReport EnemyOffensiveStalled(string reason, int turn)
        {
            return new BattleReport { headline = $"Enemy offensive has stalled — {reason}.", details = "Frontline advance has halted. Our forces are consolidating defensive positions.", phase = "Enemy Push Repelled", ourCasualties = UnityEngine.Random.Range(200, 800), enemyCasualties = UnityEngine.Random.Range(300, 1200), outcome = BattleOutcome.MinorVictory, turn = turn };
        }
        public static BattleReport AirSupremacyEstablished(string region, int turn)
        {
            return new BattleReport { headline = $"Air Force has established air superiority over {region}.", details = "Enemy fighter sorties have dropped 80%.", phase = "Air Campaign", ourCasualties = UnityEngine.Random.Range(0, 2), enemyCasualties = UnityEngine.Random.Range(8, 24), outcome = BattleOutcome.MajorVictory, turn = turn };
        }
        public static BattleReport CoupSuppressed(string region, bool rdfUsed, int turn)
        {
            return new BattleReport { headline = $"Coup attempt in {region} has been suppressed.", details = "Coup ringleaders are in custody. Stability impact minimised.", phase = "Internal Security", ourCasualties = rdfUsed ? UnityEngine.Random.Range(2, 10) : UnityEngine.Random.Range(20, 80), enemyCasualties = UnityEngine.Random.Range(10, 40), outcome = BattleOutcome.MinorVictory, turn = turn };
        }
        public static BattleReport FrontlineUpdate(string direction, float advanceKm, int turn)
        {
            bool advancing = advanceKm > 0;
            return new BattleReport { headline = advancing ? $"Our forces advance {advanceKm:0.0}km into {direction}." : $"Frontline retreated {Mathf.Abs(advanceKm):0.0}km in {direction}.", details = advancing ? "Enemy defences in this sector are thinning." : "A tactical withdrawal. Reserves have been committed.", phase = advancing ? "Advance" : "Withdrawal", ourCasualties = (int)(Mathf.Abs(advanceKm) * UnityEngine.Random.Range(80f, 200f)), enemyCasualties = (int)(Mathf.Abs(advanceKm) * UnityEngine.Random.Range(60f, 180f)), outcome = advancing ? BattleOutcome.MinorVictory : BattleOutcome.MinorDefeat, turn = turn };
        }
        public static BattleReport StrategicBombing(string target, bool success, int turn)
        {
            return new BattleReport { headline = success ? $"Air Force strike on {target} succeeds. Enemy logistics disrupted." : $"Air strike on {target} aborted — insufficient air crew effectiveness.", details = success ? $"Enemy supply chain impact estimated at 3 turns." : "Mission was recalled after navigation errors.", phase = "Air Campaign", ourCasualties = success ? 0 : UnityEngine.Random.Range(0, 2), enemyCasualties = success ? UnityEngine.Random.Range(20, 80) : 0, outcome = success ? BattleOutcome.MinorVictory : BattleOutcome.Stalemate, turn = turn };
        }
        public static BattleReport NavalEngagement(string waters, bool victory, int turn)
        {
            return new BattleReport { headline = victory ? $"Naval skirmish in {waters} — our patrol boats repel incursion." : $"Enemy vessel challenged us in {waters} — our fleet stood down.", details = victory ? "Enemy vessel withdrew under warning shots." : "Coastal vulnerability exposed.", phase = "Maritime Operations", ourCasualties = victory ? 0 : UnityEngine.Random.Range(0, 5), enemyCasualties = victory ? UnityEngine.Random.Range(0, 3) : 0, outcome = victory ? BattleOutcome.MinorVictory : BattleOutcome.Stalemate, turn = turn };
        }
        public static BattleReport StalemateNotice(int turn)
        {
            return new BattleReport { headline = "Front lines have stabilised. Neither side is advancing.", details = "Both armies are dug in. Casualty rates are high, territorial gains minimal.", phase = "Stalemate", ourCasualties = UnityEngine.Random.Range(300, 1000), enemyCasualties = UnityEngine.Random.Range(300, 1000), outcome = BattleOutcome.Stalemate, turn = turn };
        }
        public static BattleReport RoutingEnemy(string region, int turn)
        {
            return new BattleReport { headline = $"Enemy forces routing in {region}. Pursuit ordered.", details = "Our mechanised units are pursuing. Prisoner count rising.", phase = "Decisive Victory", ourCasualties = UnityEngine.Random.Range(50, 300), enemyCasualties = UnityEngine.Random.Range(800, 3000), outcome = BattleOutcome.MajorVictory, turn = turn };
        }
    }

    [Serializable]
    public class RecruitmentPipelineNew
    {
        public string personnelType;
        public int count;
        public int trainingMonthsRemaining;
        public float costPerTurn;
        public bool IsComplete => trainingMonthsRemaining <= 0;
        public void Tick() { if (trainingMonthsRemaining > 0) trainingMonthsRemaining--; }
    }

    [Serializable]
    public class MilitaryState
    {
        public ArmyPersonnel army = new ArmyPersonnel();
        public AirForcePersonnel airForce = new AirForcePersonnel();
        public NavyPersonnel navy = new NavyPersonnel();

        public BranchReadiness armyReadiness = new BranchReadiness();
        public BranchReadiness airForceReadiness = new BranchReadiness();
        public BranchReadiness navyReadiness = new BranchReadiness();

        public List<ActiveOperation> activeOperations = new List<ActiveOperation>();
        public List<BattleReport> battleLog = new List<BattleReport>();

        public WarState currentWar = null;
        public NavalMissions navalMissions = new NavalMissions();
        public List<RecruitmentPipelineNew> pipelines = new List<RecruitmentPipelineNew>();
    }

    public static class AirForceCalculator
    {
        public static int AircraftAvailable(int totalAircraft, int pilots, int groundCrew, int requiredCrewPerAircraft)
        {
            int pilotLimited = Mathf.Min(totalAircraft, pilots);
            float crewRatio = (requiredCrewPerAircraft > 0) ? Mathf.Clamp01((float)groundCrew / (totalAircraft * requiredCrewPerAircraft)) : 1f;
            return Mathf.FloorToInt(pilotLimited * crewRatio);
        }
        public static float MissionEffectiveness(BranchReadiness af, int airControllers, int activeBases)
        {
            float baseEff = af.OverallReadiness / 100f;
            float controllerRatio = activeBases > 0 ? Mathf.Clamp01((float)airControllers / activeBases) : 0f;
            return Mathf.Clamp01(baseEff + (controllerRatio * 0.2f));
        }
        public static float AccidentRate(int groundCrew, int aircraft, int requiredCrewPerAircraft)
        {
            float crewRatio = (aircraft > 0 && requiredCrewPerAircraft > 0) ? Mathf.Clamp01((float)groundCrew / (aircraft * requiredCrewPerAircraft)) : 1f;
            return Mathf.Lerp(8f, 0.5f, crewRatio);
        }
        public static float AirSuperiorityVsEnemy(int ourFighters, float ourEffectiveness, int enemyFighters, float enemyEffectiveness)
        {
            float ours = ourFighters * ourEffectiveness;
            float theirs = enemyFighters * enemyEffectiveness;
            float total = ours + theirs;
            return total > 0 ? ours / total : 0.5f;
        }
    }

    public static class NavyCalculator
    {
        public static float FleetReadiness(NavyPersonnel np, int patrolBoats, int frigates, int destroyers, MilitaryConfigSO cfg)
        {
            int requiredSailors = (patrolBoats * cfg.sailorsPerPatrolBoat) + (frigates * cfg.sailorsPerFrigate) + (destroyers * cfg.sailorsPerDestroyer);
            float sailorRatio = requiredSailors > 0 ? Mathf.Clamp01((float)np.sailors / requiredSailors) : 1f;
            int totalVessels = patrolBoats + frigates + destroyers;
            float officerRatio = totalVessels > 0 ? Mathf.Clamp01((float)np.navalOfficers / totalVessels) : 0f;
            int requiredTech = Mathf.Max(1, np.sailors / 10);
            float techRatio = Mathf.Clamp01((float)np.technicalCrew / requiredTech);
            return (sailorRatio * 0.5f) + (officerRatio * 0.3f) + (techRatio * 0.2f);
        }
        public static float BreakdownChancePerVessel(NavyPersonnel np, int totalVessels)
        {
            int requiredTech = Mathf.Max(1, np.sailors / 10);
            float techRatio = Mathf.Clamp01((float)np.technicalCrew / requiredTech);
            return Mathf.Lerp(0.25f, 0.01f, techRatio);
        }
        public static float CoastalDefenceBonus(int marines)
        {
            return Mathf.Clamp(marines / 1000f * 0.01f, 0f, 0.25f);
        }
    }

    public static class ArmyCalculator
    {
        public static float ArmyReadiness(ArmyPersonnel army, BranchReadiness readiness, int requiredInfantry)
        {
            float personnelRatio = requiredInfantry > 0 ? Mathf.Clamp01((float)army.infantry / requiredInfantry) : 1f;
            readiness.personnel = personnelRatio * 100f;
            return readiness.OverallReadiness;
        }
        public static float CounterInsurgencyScore(ArmyPersonnel army, BranchReadiness r)
        {
            float infantryScore = Mathf.Clamp01(army.infantry / 50000f) * 0.6f;
            float rdfScore = Mathf.Clamp01(army.rdfPersonnel / 3000f) * 0.4f;
            float readinessMod = r.OverallReadiness / 100f;
            return (infantryScore + rdfScore) * readinessMod;
        }
        public static bool CanDeployRDF(int rdfPersonnel, int rdfCooldownMonths, BranchReadiness rdfReadiness)
        {
            return rdfPersonnel >= 500 && rdfCooldownMonths <= 0 && rdfReadiness.OverallReadiness >= 40f;
        }
    }
}
