using UnityEngine;

namespace StateCraft.Data.Power
{
    public enum PowerSourceType { Gas, Hydro, Nuclear, Solar, Wind, Imported }

    [CreateAssetMenu(fileName = "NewPowerPlant", menuName = "StateCraft/Power/Power Plant")]
    public class PowerPlantDataSO : ScriptableObject
    {
        [Header("General")]
        public string plantName;
        public PowerSourceType type;
        
        [Header("Output & Costs")]
        public float baseOutputMW;
        public float constructionCostB;
        public int constructionTimeMonths;
        public float maintenanceCostMo;
        
        [Header("Risks & Modifiers")]
        [Range(0, 100)] public float baseBlackoutRisk;
        public float fuelConsumptionRate;
        public float pollutionRate;
        public float publicOpinionImpact;
    }
}
