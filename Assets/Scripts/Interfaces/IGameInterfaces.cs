namespace StateCraft.Interfaces
{
    public interface ITurnSystem
    {
        int CurrentTurn { get; }
        string CurrentDate { get; }
        void AdvanceTurn();
        System.Action OnTurnEnded { get; set; }
        /// <summary>Fires only when the month rolls over. Bind heavy monthly systems here.</summary>
        System.Action OnMonthEnded { get; set; }
    }

    public interface IEconomySystem
    {
        void UpdateEconomy(Data.CountryDataSO country);
        float CalculateProjectedIncome(Data.CountryDataSO country);
    }

    public interface IDebtSystem
    {
        void UpdateDebt(Data.CountryDataSO country);
        float CalculateInterest(Data.CountryDataSO country);
        float CalculateDomesticInterest(Data.CountryDataSO country);
        float CalculateForeignInterest(Data.CountryDataSO country);
    }

    public interface ISecuritySystem
    {
        void DeployPolice(Data.CountryDataSO country, Data.RegionDataSO region);
        void DeployMilitary(Data.CountryDataSO country, Data.RegionDataSO region);
        void DeployRDF(Data.CountryDataSO country, Data.RegionDataSO region);
        void UpdateSecurity(Data.CountryDataSO country);
        void RecalculateSecurity(Data.CountryDataSO country);
    }

    public interface IIntelligenceSystem
    {
        void SimulateIntelligence(Data.CountryDataSO country);
        string GetFormattedData(float actualValue, float intelLevel, string format = "N1");
    }

    public enum WarStage
    {
        Peace,
        Tension,
        Crisis,
        Conflict
    }

    public interface IWarSystem
    {
        void UpdateWarStatus(Data.CountryDataSO country);
        void DeclareWar(Data.CountryDataSO aggressor, Data.CountryDataSO defender);
        WarStage GetWarStage(Data.CountryDataSO country);
        void SetWarStage(Data.CountryDataSO country, WarStage stage);
        void ProposePeace(Data.CountryDataSO proposer, Data.CountryDataSO target);
    }

    public interface IDiplomacySystem
    {
        void UpdateRelations(Data.CountryDataSO country);
        void SignTradeDeal(Data.CountryDataSO seller, Data.CountryDataSO buyer, TradeResourceType resource, float paymentUSD);
        void CancelTradeDeal(string dealId);
        void ProcessTradeDeals(Data.CountryDataSO country);
        Data.TradeAgreementProposal CreateProposal(Data.CountryDataSO player, Data.CountryDataSO ai, TradeResourceType resource, float basePayment, int duration, bool playerIsSeller);
        TradeAgreement AcceptProposal(string proposalId);
        void CancelProposal(string proposalId);
    }

    public interface IAICountrySystem
    {
        void RunAI(Data.CountryDataSO country);
        Data.TradeAgreementProposal EvaluateProposal(Data.TradeAgreementProposal proposal);
    }

    public interface IInsurgencySystem
    {
        void SimulateInsurgency(Data.CountryDataSO country);
        StateCraft.Systems.CampStrikeResult StrikeInsurgentCamp(Data.CountryDataSO country, StateCraft.Systems.InsurgentCamp camp, int fighters, int bombers, int attackHelis, int uavs);
    }

    public interface IEventSystem
    {
        void CheckForEvents(Data.CountryDataSO country);
        System.Collections.Generic.List<Data.GameEventSO> GetPendingEvents(Data.CountryDataSO country);
        void ResolvePendingEvent(Data.CountryDataSO country, Data.GameEventSO ev, int choiceIndex);
    }

    public interface IElectionSystem
    {
        void CheckForElection(Data.CountryDataSO country);
    }
}
