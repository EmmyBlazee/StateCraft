using System.Collections.Generic;
using UnityEngine;

namespace StateCraft.Data.Economy
{
    [CreateAssetMenu(fileName = "EconomyDatabase", menuName = "StateCraft/Economy/Database")]
    public class EconomyDatabaseSO : ScriptableObject
    {
        [Header("Infrastructure Definitions")]
        public List<InfraDataSO> infrastructure = new List<InfraDataSO>();

        [Header("Industry Definitions")]
        public List<IndustryDataSO> industries = new List<IndustryDataSO>();

        [Header("Commodity Definitions")]
        public List<CommodityDataSO> commodities = new List<CommodityDataSO>();

        [Header("Construction Blueprints")]
        public List<BlueprintDataSO> blueprints = new List<BlueprintDataSO>();

public InfraDataSO GetInfra(string id)
            => infrastructure?.Find(x => x.id == id);

        public IndustryDataSO GetIndustry(string id)
            => industries?.Find(x => x.id == id);

        public CommodityDataSO GetCommodity(string id)
            => commodities?.Find(x => x.id == id);

        public BlueprintDataSO GetBlueprint(string name)
            => blueprints?.Find(x => x.blueprintName == name);
    }
}
