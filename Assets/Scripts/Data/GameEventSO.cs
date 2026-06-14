using UnityEngine;

namespace StateCraft.Data
{
    [CreateAssetMenu(fileName = "NewEvent", menuName = "StateCraft/Data/Game Event")]
    public class GameEventSO : ScriptableObject
    {
        public string eventTitle;
        [TextArea(3, 10)]
        public string description;

        [Header("Choice A")]
        public string optionAText;
        public float optionACost; // Money removed from treasury
        public float optionAStability;
        public float optionAGDP;
        public float optionAIntel;
        public float optionASecurity;

        [Header("Choice B")]
        public string optionBText;
        public float optionBCost; 
        public float optionBStability;
        public float optionBGDP;
        public float optionBIntel;
        public float optionBSecurity;

        [Header("Requirements")]
        public float minStability = 0f;
        public float maxStability = 100f;

        [Header("Event Chains")]
        public GameEventSO nextEventIfA;
        public GameEventSO nextEventIfB;
        public int turnsUntilNextEvent;
    }
}
