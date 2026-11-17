using System.Collections.Generic;
using UnityEngine;

namespace StateCraft.Data
{
    [CreateAssetMenu(fileName = "NewMissionDefinition", menuName = "StateCraft/Military/Mission Definition")]
    public class MissionDefinitionSO : ScriptableObject
    {
        public string missionId;
        public string displayName;
        [TextArea(3, 5)] public string description;
        public MilitaryBranch leadBranch;
        public List<MilitaryBranch> requiredBranches;
        public bool isJoint => requiredBranches != null && requiredBranches.Count > 1;

        public float baseSuccessWeight = 50f;
        public float baseCasualtyRisk;
        public List<string> validObjectiveIds;
    }
}
