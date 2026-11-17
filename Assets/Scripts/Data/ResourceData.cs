using UnityEngine;

namespace StateCraft.Data
{
    public enum MineralType
    {
        Oil,
        Gold,
        Diamonds,
        Cobalt,
        Copper,
        Lithium,
        NaturalGas,
        Platinum,
        IronOre,
        Uranium,
        Coal,
        None
    }

    [System.Serializable]
    public struct ResourceDeposit
    {
        public MineralType type;
        [Range(0, 100)]
        public float abundance;
        public float marketValue;
    }
}
