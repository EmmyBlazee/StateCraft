using UnityEngine;

namespace StateCraft.Data
{
    [CreateAssetMenu(fileName = "MilitaryConfig", menuName = "StateCraft/Military Config")]
    public class MilitaryConfigSO : ScriptableObject
    {
        [Header("Infantry")]
        public float infantryCostPerUnitPerYear = 2000f;
        public int infantryTrainingMonths = 3;
        public int infantryDeploymentTurns = 2;

        [Header("RDF")]
        public float rdfCostPerUnitPerYear = 8000f;
        public int rdfTrainingMonths = 12;
        public int rdfDeploymentTurns = 0;
        public float rdfMaxAsFractionOfInfantry = 0.05f;
        public int rdfCooldownMonths = 3;

        [Header("Air Force")]
        public float pilotCostPerYear = 25000f;
        public float groundCrewCostPerYear = 6000f;
        public float airControllerCostPerYear = 12000f;
        public int groundCrewPerFighter = 8;
        public int groundCrewPerBomber = 12;
        public int groundCrewPerHeli = 6;

        [Header("Navy")]
        public float sailorCostPerYear = 5000f;
        public float navalOfficerCostPerYear = 18000f;
        public float marineCostPerYear = 7000f;
        public float techCrewCostPerYear = 10000f;
        public int sailorsPerPatrolBoat = 12;
        public int sailorsPerFrigate = 200;
        public int sailorsPerDestroyer = 280;

        [Header("Readiness Decay (per month, no funding)")]
        public float trainingDecayRate = 2.0f;
        public float equipmentDecayRate = 1.5f;
        public float moraleDecayRate = 1.0f;
    }
}
