using UnityEngine;

namespace StateCraft.Data
{
    [CreateAssetMenu(fileName = "NewRDFUnit", menuName = "StateCraft/RDF Unit Data")]
    public class RDFUnitDataSO : ScriptableObject
    {
        public string unitName;
        public float deploymentCost;
        public float securityImpact;
        public float stabilityImpact;
        public int cooldownTurns;
        public Sprite unitIcon;
    }
}
