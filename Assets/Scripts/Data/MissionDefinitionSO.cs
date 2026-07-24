using System.Collections.Generic;
using UnityEngine;

namespace StateCraft.Data
{
    [CreateAssetMenu(fileName = "NewMissionDefinition", menuName = "StateCraft/Military/Mission Definition")]
    public class MissionDefinitionSO : ScriptableObject
    {
        public string missionId;              // "ground_assault", "amphibious_landing"
        public string displayName;
        [TextArea(3, 5)] public string description;
        public MilitaryBranch leadBranch;     // primary branch for single-branch missions
        public List<MilitaryBranch> requiredBranches; // >1 = joint-only mission
        public bool isJoint => requiredBranches != null && requiredBranches.Count > 1;

        public float baseSuccessWeight = 50f;       // used in success formula
        public float baseCasualtyRisk;        // 0 = low, 1 = medium, 2 = high
        public List<string> validObjectiveIds; // links to existing objective list
    }
}
