using System;
using System.Collections.Generic;

namespace StateCraft.Data
{
    [Serializable]
    public class DiplomaticEvent
    {
        public string eventName;
        public string description;
        public int turnOccurred;
    }

    [Serializable]
    public class Embassy
    {
        public string countryName;
        public bool isOperational;
        public int staffCount;
        public float influencePercentage;
        public string economicReportQuality;
        public string politicalReportQuality;
    }

    [Serializable]
    public class InternationalOrganization
    {
        public string orgName;
        public string membershipStatus;
        public string votingPower;
        public string currentResolution;
        public string voteStatus;
    }

    [Serializable]
    public class ThreatAlert
    {
        public string title;
        public string region;
        public float probability;
        public string impact;
        public string icon;
        public UnityEngine.Color color;
    }
}
