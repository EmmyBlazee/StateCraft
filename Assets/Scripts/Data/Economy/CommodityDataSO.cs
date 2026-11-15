using UnityEngine;

namespace StateCraft.Data.Economy
{
    [CreateAssetMenu(fileName = "NewCommodity", menuName = "StateCraft/Economy/Commodity")]
    public class CommodityDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string title;
        public string category;
        public string unit;
        public string role;
    }
}
