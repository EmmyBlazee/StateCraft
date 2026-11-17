
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StateCraft.Data
{
    public enum RelationTier
    {
        Allied       = 4,
        Friendly     = 3,
        Neutral      = 2,
        Hostile      = 1,
        AtWar        = 0
    }

    [Serializable]
    public class BilateralRelation
    {
        public string countryA;
        public string countryB;
        [Range(-100f, 100f)]
        public float score = 0f;

        public List<float> scoreHistory = new List<float>();

        public bool     hasUltimatum;
        public string   ultimatumFromCountry;
        public string   ultimatumDemand;
        public int      ultimatumDeadlineMonth;
        public bool     ultimatumResolved;

        public string   lastIncident = "";
        public int      lastIncidentMonth;

        public RelationTier Tier
        {
            get
            {
                if (score >= 80f)  return RelationTier.Allied;
                if (score >= 40f)  return RelationTier.Friendly;
                if (score > -20f)  return RelationTier.Neutral;
                if (score > -60f)  return RelationTier.Hostile;
                return RelationTier.AtWar;
            }
        }
    }

    [Serializable]
    public class RelationsMatrix
    {
        private Dictionary<string, BilateralRelation> _matrix
            = new Dictionary<string, BilateralRelation>(StringComparer.OrdinalIgnoreCase);

        public BilateralRelation GetOrCreate(string a, string b)
        {
            string key = MakeKey(a, b);
            if (!_matrix.TryGetValue(key, out var rel))
            {
                rel = new BilateralRelation
                {
                    countryA = string.Compare(a, b, StringComparison.Ordinal) <= 0 ? a : b,
                    countryB = string.Compare(a, b, StringComparison.Ordinal) <= 0 ? b : a,
                    score    = 0f
                };
                _matrix[key] = rel;
            }
            return rel;
        }

        public float GetScore(string a, string b)
            => GetOrCreate(a, b).score;

        public RelationTier GetTier(string a, string b)
            => GetOrCreate(a, b).Tier;

        public void Adjust(string a, string b, float delta, string incidentNote = "", int currentMonth = 0)
        {
            var rel = GetOrCreate(a, b);
            rel.score = Mathf.Clamp(rel.score + delta, -100f, 100f);

            if (!string.IsNullOrEmpty(incidentNote))
            {
                rel.lastIncident      = incidentNote;
                rel.lastIncidentMonth = currentMonth;
            }
        }

        public void SnapshotHistory()
        {
            foreach (var rel in _matrix.Values)
            {
                rel.scoreHistory.Add(rel.score);
                if (rel.scoreHistory.Count > 6)
                    rel.scoreHistory.RemoveAt(0);
            }
        }

        public IEnumerable<BilateralRelation> AllRelations => _matrix.Values;

        public List<BilateralRelation> ExportRelations()
        {
            return new List<BilateralRelation>(_matrix.Values);
        }

        public void ImportRelations(List<BilateralRelation> relations)
        {
            _matrix.Clear();
            if (relations == null) return;
            foreach (var rel in relations)
                _matrix[MakeKey(rel.countryA, rel.countryB)] = rel;
        }

        private static string MakeKey(string a, string b)
        {
            if (string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0)
                return a + "_" + b;
            return b + "_" + a;
        }
    }
}

