// ============================================================
// FILE: Scripts/Data/RelationsMatrix.cs
// PURPOSE: Persistent bilateral relations matrix stored in
//          game state. Add this file and integrate it with
//          CountryDataSO + AICountrySystem (see INTEGRATION
//          NOTES below).
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace StateCraft.Data
{
    // ── Relation tier labels ─────────────────────────────────
    public enum RelationTier
    {
        Allied       = 4,   // +80 to +100 — mutual-defence threshold
        Friendly     = 3,   // +40 to +79
        Neutral      = 2,   //  -20 to +39
        Hostile      = 1,   //  -60 to -21
        AtWar        = 0    //  -61 and below (or explicit war)
    }

    // ── One row in the matrix ────────────────────────────────
    [Serializable]
    public class BilateralRelation
    {
        public string countryA;         // alphabetically first
        public string countryB;
        [Range(-100f, 100f)]
        public float score = 0f;        // -100 (enemy) → +100 (ally)

        // History — last 6 months of score so UI can draw a trend line
        public List<float> scoreHistory = new List<float>();

        // Pending ultimatum (set by DeclareUltimatum, cleared on response)
        public bool     hasUltimatum;
        public string   ultimatumFromCountry;
        public string   ultimatumDemand;
        public int      ultimatumDeadlineMonth;  // game month by which target must comply
        public bool     ultimatumResolved;

        // Last notable diplomatic incident (shown in UI tooltip)
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

    // ── The full matrix, owned by GameManager ───────────────
    [Serializable]
    public class RelationsMatrix
    {
        // Keyed by "CountryA_CountryB" (alphabetical) so lookups are O(1)
        private Dictionary<string, BilateralRelation> _matrix
            = new Dictionary<string, BilateralRelation>(StringComparer.OrdinalIgnoreCase);

        // ── CRUD ────────────────────────────────────────────
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

        /// <summary>
        /// Apply a delta to the relation score and record it in history.
        /// Call this for every diplomatic action.
        /// </summary>
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

        /// <summary>Snapshot the current scores into each relation's history (call monthly).</summary>
        public void SnapshotHistory()
        {
            foreach (var rel in _matrix.Values)
            {
                rel.scoreHistory.Add(rel.score);
                if (rel.scoreHistory.Count > 6)
                    rel.scoreHistory.RemoveAt(0);
            }
        }

        // ── Convenience list access for UI ──────────────────
        public IEnumerable<BilateralRelation> AllRelations => _matrix.Values;

        // ── Serialize / Deserialize hooks ───────────────────
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

        // ── Helpers ─────────────────────────────────────────
        private static string MakeKey(string a, string b)
        {
            if (string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0)
                return a + "_" + b;
            return b + "_" + a;
        }
    }
}

// ============================================================
// INTEGRATION NOTES
// ============================================================
// 1. In GameManager.cs, add:
//
//    public RelationsMatrix Relations { get; private set; } = new RelationsMatrix();
//
//    In InitializeSystems(), after the TurnSystem line add:
//    TurnSystem.OnMonthEnded += () => Relations.SnapshotHistory();
//
// 2. Replace the empty DiplomacySystem.UpdateRelations() body with:
//
//    public void UpdateRelations(CountryDataSO country) { }
//    // (Relations are now adjusted at the point of each action — see below)
//
// 3. Call Relations.Adjust() whenever a diplomatic event occurs:
//
//    // War declaration:
//    gm.Relations.Adjust(aggressor.countryName, defender.countryName, -40f,
//        $"{aggressor.countryName} declared war", currentMonth);
//
//    // Trade deal signed:
//    gm.Relations.Adjust(seller.countryName, buyer.countryName, +10f,
//        "Trade agreement signed", currentMonth);
//
//    // Proxy war exposure:
//    gm.Relations.Adjust(sponsor.countryName, target.countryName, -25f,
//        "Caught funding insurgents", currentMonth);
//
//    // Peace treaty:
//    gm.Relations.Adjust(a.countryName, b.countryName, +15f,
//        "Peace treaty signed", currentMonth);
//
//    // Ultimatum issued:
//    gm.Relations.Adjust(sender.countryName, target.countryName, -15f,
//        "Ultimatum issued", currentMonth);
//
//    // Coalition pact:
//    gm.Relations.Adjust(a.countryName, b.countryName, +20f,
//        "Coalition force pact", currentMonth);
// ============================================================
