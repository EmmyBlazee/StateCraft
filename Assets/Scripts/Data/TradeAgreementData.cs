using System;
using StateCraft.Interfaces;

namespace StateCraft.Data
{
    public class TradeAgreementProposal
    {
        public string id = Guid.NewGuid().ToString();
        public string partnerCountry;
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
                case TradeResourceType.Oil: return 75f;
                case TradeResourceType.Gas: return 40f;
                case TradeResourceType.Uranium: return 50000f;
                case TradeResourceType.Food: return 400f;
                case TradeResourceType.Minerals: return 1200f;
                case TradeResourceType.Timber: return 600f;
                default: return 100f;
            }
        }
    }
}
