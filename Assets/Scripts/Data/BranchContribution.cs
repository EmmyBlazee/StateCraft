using System;
using System.Collections.Generic;

namespace StateCraft.Data
{
    [Serializable]
    public struct UnitCountEntry
    {
        public string unitName;
        public int count;
    }

    [Serializable]
    public class BranchContribution
    {
        public MilitaryBranch branch;
        public List<UnitCountEntry> unitCounts = new List<UnitCountEntry>();
        public int personnelAssigned;
        public int casualtiesSuffered;
        public int equipmentLost;

        private Dictionary<string, int> _dictCache = null;
        
        public Dictionary<string, int> GetUnitCountsDict()
        {
            if (_dictCache == null)
            {
                _dictCache = new Dictionary<string, int>();
                if (unitCounts != null)
                {
                    foreach (var entry in unitCounts)
                    {
                        _dictCache[entry.unitName] = entry.count;
                    }
                }
            }
            return _dictCache;
        }

        public void SetUnitCount(string unitName, int count)
        {
            _dictCache = null;
            if (unitCounts == null) unitCounts = new List<UnitCountEntry>();
            int idx = unitCounts.FindIndex(x => x.unitName == unitName);
            if (idx >= 0)
            {
                var entry = unitCounts[idx];
                entry.count = count;
                unitCounts[idx] = entry;
            }
            else
            {
                unitCounts.Add(new UnitCountEntry { unitName = unitName, count = count });
            }
        }
        
        public int GetUnitCount(string unitName)
        {
            if (unitCounts == null) return 0;
            var entry = unitCounts.Find(x => x.unitName == unitName);
            return entry.count;
        }
    }
}
