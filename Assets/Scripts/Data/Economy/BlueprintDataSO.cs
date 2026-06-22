using UnityEngine;

namespace StateCraft.Data.Economy
{
    [CreateAssetMenu(fileName = "NewBlueprint", menuName = "StateCraft/Economy/Blueprint")]
    public class BlueprintDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string blueprintName;
        public string type;       // "Infra", "Energy", "Agri", "Tech"
        public string targetId;   // Maps to the field/SO Id it upgrades
        public string regionHint;

        [Header("Project Parameters")]
        public float totalCostB;
        public int   months;
        [TextArea(2, 4)] public string description;
    }
}
