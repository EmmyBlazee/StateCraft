using System;
using StateCraft.Interfaces;

namespace StateCraft.Data
{
    // Internal object while the AI is processing the proposal
    [System.Serializable]
    public class TradeAgreementProposal
    {
        public string id = Guid.NewGuid().ToString();
        public string partnerCountry;               // AI country name
        public TradeResourceType resource;
        public float baseMonthlyPaymentUSD;
        public int baseDurationMonths;
        public float resourceQuantity;
        public bool isBulkTransfer;
        public bool playerIsSeller;
    }

    public static class GlobalTradeMarket
    {
        public static float GetPricePerUnit(TradeResourceType resource)
        {
            switch (resource)
            {
                case TradeResourceType.Oil: return 75f;          // $75 per barrel
                case TradeResourceType.Gas: return 40f;          // $40 per unit
                case TradeResourceType.Uranium: return 50000f;    // $50,000 per unit
                case TradeResourceType.Food: return 400f;         // $400 per tonne
                case TradeResourceType.Minerals: return 1200f;    // $1,200 per tonne
                case TradeResourceType.Timber: return 600f;       // $600 per unit
                default: return 100f;                             // Generic fallback
            }
        }
    }
}
