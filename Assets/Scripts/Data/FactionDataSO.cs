using UnityEngine;

namespace StateCraft.Data
{
    [CreateAssetMenu(fileName = "NewFaction", menuName = "StateCraft/Faction Data")]
    public class FactionDataSO : ScriptableObject
    {
        public string factionName;
        
        [Range(0, 100)]
        public float influence = 10f;
        
        public float recruitmentRate = 0.05f;
        public float fundingLevel = 1000f;
        
        public bool isInternational = false;
    }
}
