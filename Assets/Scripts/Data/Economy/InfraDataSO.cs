using UnityEngine;

namespace StateCraft.Data.Economy
{
    public enum InfraCategory { Transport, Energy, Social, Digital }

    [CreateAssetMenu(fileName = "NewInfra", menuName = "StateCraft/Economy/Infrastructure")]
    public class InfraDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string title;
        public InfraCategory category;
        public string iconTag;

        [Header("Description")]
        [TextArea(2, 4)] public string effectDescription;
        [TextArea(2, 4)] public string bottleneckThreshold; // e.g. "< 3"

        [Header("Cost")]
        public float baseCost   = 8f;
        public float costPerLevel = 2.5f;
    }
}
