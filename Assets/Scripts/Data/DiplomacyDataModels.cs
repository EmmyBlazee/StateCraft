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
        public string economicReportQuality; // Poor, Average, High, Excellent
        public string politicalReportQuality; // Poor, Average, High, Excellent
    }

    [Serializable]
    public class InternationalOrganization
    {
        public string orgName;
        public string membershipStatus; // Member, Observer, Suspended, None
        public string votingPower; // Low, Medium, High, Veto
        public string currentResolution;
        public string voteStatus; // Approve, Abstain, Reject, Pending
    }
}
