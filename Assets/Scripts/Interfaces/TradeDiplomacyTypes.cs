namespace StateCraft.Interfaces
{
    public enum TradeResourceType
    {
        Oil,
        Gas,
        Uranium,
        Food,
        Minerals,
        Timber,
        Other
    }

    public enum TradeAgreementStatus
    {
        Proposed,
        Countered,
        Active,
        Cancelled,
        Expired
    }

    public class TradeDeal
    {
        public string id;
        public string sellerCountry;
        public string buyerCountry;
        public TradeResourceType resource;
        public float monthlyPaymentUSD;
        public int durationTurns;
    }

    [System.Serializable]
    public class TradeAgreement
    {
        public string id;
        public string partnerCountry;
        public TradeResourceType resource;
        public float monthlyPaymentUSD;
        public int durationMonths;
        public TradeAgreementStatus status;
        
        public float resourceQuantity;
        public bool isBulkTransfer;
        public bool isSeller;

        // Counter-offer tracking
        public float originalPaymentUSD;
        public int originalDurationMonths;
        public string counterReason;
    }
}
