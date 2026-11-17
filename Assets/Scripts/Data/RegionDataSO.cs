using UnityEngine;
using System.Collections.Generic;

namespace StateCraft.Data
{
    public enum RegionSpecialization
    {
        Rural,
        Urban,
        ResourceRich,
        BorderConflict
    }

    public enum InsurgencyIdeology
    {
        None,
        Separatist,
        ReligiousExtremist,
        CommunistMilitia,
        Cartel
    }

    [CreateAssetMenu(fileName = "NewRegion", menuName = "StateCraft/Region Data")]
    public class RegionDataSO : ScriptableObject
    {
        public string regionName;
        public RegionSpecialization specialization = RegionSpecialization.Rural;
        
        [Range(0, 100)]
        public float stability = 70f;
        
        [Range(0, 100)]
        public float security = 50f;

        [Tooltip("Percentage of the country's national GDP generated here")]
        public float economicContribution = 10f; 
        
        [Header("Instability")]
        public float insurgencyLevel = 0f;
        [Tooltip("Floor risk. Insurgency will rapidly bounce back to this minimum level if unattended.")]
        public float baseInsurgencyRisk = 0f;
        
        public InsurgencyIdeology insurgencyIdeology = InsurgencyIdeology.None;
        [Range(0, 100)]
        public float heartsAndMinds = 50f;
        [Range(0, 100)]
        public float insurgentSupplyLines = 100f;
        public string regionalGovernor = "Placeholder Governor";

        [Header("Insurgent Camp Intel")]
        public bool insurgentCampDetected = false;
        [Range(0, 100)]
        public float campStrength = 0f;
        public bool campStrikeOrdered = false;

        [Header("Connections")]
        public List<RegionDataSO> adjacentRegions = new List<RegionDataSO>();
    }
}
