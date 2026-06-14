using UnityEngine;

namespace StateCraft.Core
{
    [System.Serializable]
    public class WorldState
    {
        [Header("Global Markets")]

        public float oilPrice = 80f;

        public float foodPrice = 100f;

        public float tradeConfidence = 75f;

        [Header("Global Stability")]

        public float globalInstability = 10f;

        public float refugeePressure = 5f;

        public float militaryEscalation = 0f;

        [Header("Continental Pressure")]

        public float climateStress = 0f;

        public float insurgencyRisk = 0f;
    }
}