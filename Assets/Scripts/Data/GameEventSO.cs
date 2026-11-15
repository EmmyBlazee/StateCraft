using UnityEngine;
using System.Collections.Generic;

namespace StateCraft.Data
{
    [System.Serializable]
    public class GameEventChoice
    {
        public string choiceText;
        public float costUSD;
        public float stabilityEffect;
        public float gdpEffect;
        public float intelEffect;
        public float securityEffect;
        public float prestigeEffect;
        public GameEventSO nextEvent;
    }

    [System.Serializable]
    public class PendingEvent
    {
        public CountryDataSO country;
        public GameEventSO ev;
        public int daysRemaining;
    }

    [CreateAssetMenu(fileName = "NewEvent", menuName = "StateCraft/Data/Game Event")]
    public class GameEventSO : ScriptableObject
    {
        public string eventTitle;
        public string severity = "Medium";
        public int timeRemainingDays = 7;
        
        [TextArea(3, 10)]
        public string description;

        [Header("Choices")]
        public List<GameEventChoice> choices = new List<GameEventChoice>();

        [Header("Requirements")]
        public float minStability = 0f;
        public float maxStability = 100f;

        [Header("Event Chains")]
        public int turnsUntilNextEvent;
    }
}
