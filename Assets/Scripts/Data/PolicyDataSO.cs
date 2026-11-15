using UnityEngine;

namespace StateCraft.Data
{
    public enum PolicyType
    {
        Economy,
        Security,
        Social
    }

    [CreateAssetMenu(fileName = "NewPolicy", menuName = "StateCraft/Policy Data")]
    public class PolicyDataSO : ScriptableObject
    {
        public string policyName;
        public PolicyType type;
        public string description;
        
        public float implementationCost;
        public float recurringCost;

        [Header("Runtime Info")]
        public int turnEnacted;

        [Header("Effects")]
        public float gdpImpact;
        public float stabilityImpact;
        public float securityImpact;
    }
}
