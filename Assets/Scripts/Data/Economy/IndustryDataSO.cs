using UnityEngine;

namespace StateCraft.Data.Economy
{
    [CreateAssetMenu(fileName = "NewIndustry", menuName = "StateCraft/Economy/Industry")]
    public class IndustryDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string title;

        [Header("Economy Parameters")]
        [Tooltip("GDP share this industry contributes at max level (fraction, e.g. 0.05 = 5%).")]
        public float gdpShareAtMax = 0.05f;

        [Header("Cost")]
        public float baseCost     = 10f;
        public float costPerLevel = 3f;
    }
}
