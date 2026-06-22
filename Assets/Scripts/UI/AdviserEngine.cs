using System.Collections.Generic;
using UnityEngine;
using StateCraft.Data;

namespace StateCraft.UI
{
    /// <summary>
    /// Generates context-aware adviser recommendations from live CountryDataSO state.
    /// Each recommendation carries a real Apply() lambda that mutates country data.
    /// </summary>
    public static class AdviserEngine
    {
        private const int RecommendationCooldownTurns = 3;

        public struct AdviserRecommendation
        {
            public string Title;
            public string Category;
            public string Rationale;
            public string Benefits;
            public string Risks;
            public float Cost;           // Treasury cost in Billions (local currency)
            public int UrgencyScore;     // Higher = shown first
            public System.Action<CountryDataSO> Apply;
        }

        public static string GetEconomicAdviserName(CountryDataSO c)
        {
            // Future placeholder for actual Cabinet/Minister lookup
            return "Chief Economic Adviser";
        }

        public static Dictionary<string, float> GetAutoAllocateWeights(CountryDataSO c)
        {
            if (c == null) return new Dictionary<string, float>();

            // Base weights adjusted dynamically by crises or national state
            var weights = new Dictionary<string, float>
            {
                { "MILITARY", c.warTurnsActive > 0 ? 30f : 20f },
                { "INTELLIGENCE", c.exposureRisk > 50f ? 12f : 7f },
                { "EDUCATION", 16f },
                { "HEALTH", 14f },
                { "INFRASTRUCTURE", c.electricityLevel < 50f ? 20f : 18f },
                { "AGRICULTURE", c.foodSecurity < 50f ? 18f : 10f },
                { "ENERGY", 9f },
                { "WELFARE", c.povertyRate > 50f ? 8f : 4f }
            };

            // Normalize weights to 100%
            float total = 0f;
            foreach (var w in weights.Values) total += w;
            var normalized = new Dictionary<string, float>();
            foreach (var kvp in weights) normalized[kvp.Key] = (kvp.Value / total) * 100f;

            return normalized;
        }

        public static float GetDynamicBorrowAmount(CountryDataSO c)
        {
            if (c == null) return 50f;
            // Borrow tranche scales with GDP (e.g. 1% of GDP converted to local currency)
            float amt = (c.gdp * 0.01f) * c.exchangeRate;
            return Mathf.Max(10f, Mathf.Round(amt / 10f) * 10f); // Round to nearest 10
        }

        public static float GetDynamicRepayAmount(CountryDataSO c)
        {
            if (c == null) return 50f;
            // Repay tranche scales with total debt
            float debtLocal = c.debt * c.exchangeRate;
            float amt = debtLocal * 0.05f;
            return Mathf.Max(10f, Mathf.Round(amt / 10f) * 10f); // Round to nearest 10
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns up to <paramref name="maxCount"/> recommendations ranked by urgency,
        /// excluding any that the player dismissed this session.
        /// </summary>
        public static List<AdviserRecommendation> GenerateRecommendations(
            CountryDataSO c, int maxCount = 5)
        {
            var results = new List<AdviserRecommendation>();
            if (c == null) return results;

            int currentTurn = Core.GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            if (currentTurn < c.adviserNextRecommendationTurn) return results;

            foreach (var rec in BuildAll(c))
            {
                // Skip dismissed titles (10-turn cooldown)
                if (c.dismissedAdviserTitles != null &&
                    c.dismissedAdviserTitles.Contains(rec.Title) &&
                    currentTurn < c.adviserDismissTurn + 10)
                    continue;

                results.Add(rec);
                if (results.Count >= maxCount) break;
            }
            return results;
        }

        /// <summary>
        /// Dismiss the given title for 10 turns.
        /// </summary>
        public static void Dismiss(CountryDataSO c, string title)
        {
            if (c == null) return;
            if (c.dismissedAdviserTitles == null) c.dismissedAdviserTitles = new List<string>();
            if (!c.dismissedAdviserTitles.Contains(title))
                c.dismissedAdviserTitles.Add(title);
            c.adviserDismissTurn = Core.GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            c.adviserNextRecommendationTurn = c.adviserDismissTurn + RecommendationCooldownTurns;
            c.lastAdviserDecision = $"Declined: {title}";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rule engine — order determines display priority
        // ─────────────────────────────────────────────────────────────────────

        private static List<AdviserRecommendation> BuildAll(CountryDataSO c)
        {
            var list = new List<AdviserRecommendation>();
            string sym = string.IsNullOrEmpty(c.currencySymbol) ? "₦" : c.currencySymbol;
            float debtGDP = c.gdp > 0 ? c.debt / c.gdp : 0f;

            // ── 1. Debt Crisis ────────────────────────────────────────────────
            if (debtGDP > 0.5f)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Debt Restructuring Programme",
                    Category   = "Fiscal",
                    Rationale  = $"National debt is {debtGDP:P0} of GDP — above the 50% sustainability threshold.",
                    Benefits   = $"+ Improves credit rating\n+ Reduces monthly interest burden\n+ Stabilises exchange rate",
                    Risks      = "- Austerity signals dampen investor confidence\n- Stability −1",
                    Cost       = 0f,
                    UrgencyScore = 95,
                    Apply = country =>
                    {
                        country.debtServiceRatio = Mathf.Min(200f, country.debtServiceRatio + 20f);
                        country.stability        = Mathf.Max(0f,   country.stability        - 1f);
                        country.creditRating     = ImproveCreditStep(country.creditRating);
                        LogReform(country, "Debt Restructuring: service ratio +20%", sym);
                    }
                });

            // ── 2. High Inflation ─────────────────────────────────────────────
            if (c.inflationRate > 0.08f)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Raise Central Bank Interest Rate",
                    Category   = "Monetary Policy",
                    Rationale  = $"Inflation at {c.inflationRate:P1} — far above the 4% target. Monetary tightening is required.",
                    Benefits   = $"+ Inflation −1%\n+ Currency strengthens\n+ Investor confidence improves",
                    Risks      = "- GDP growth slows\n- Business borrowing costs rise\n- Approval −1%",
                    Cost       = 0f,
                    UrgencyScore = 90,
                    Apply = country =>
                    {
                        country.centralBankInterestRate = Mathf.Min(20f, country.centralBankInterestRate + 1.5f);
                        country.inflationRate           = Mathf.Max(0f,  country.inflationRate           - 0.01f);
                        country.publicApproval          = Mathf.Max(0f,  country.publicApproval          - 1f);
                        LogReform(country, $"Interest rate raised to {country.centralBankInterestRate:F1}%", sym);
                    }
                });

            // ── 3. Power / Electricity Crisis ─────────────────────────────────
            if (c.electricityLevel < 40f)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Emergency National Grid Expansion",
                    Category   = "Energy",
                    Rationale  = $"Only {c.electricityLevel:F0}% electricity coverage — factories constrained, blackout risk high.",
                    Benefits   = $"+ Electricity +8%\n+ Blackout risk −10%\n+ Industrial output boost",
                    Risks      = "- Treasury −20B",
                    Cost       = 20f,
                    UrgencyScore = 88,
                    Apply = country =>
                    {
                        if (SpendTreasury(country, 20f))
                        {
                            country.electricityLevel    = Mathf.Min(100f, country.electricityLevel    + 8f);
                            country.currentBlackoutRisk = Mathf.Max(0f,   country.currentBlackoutRisk - 0.1f);
                            country.powerGasPlants++;
                            LogReform(country, $"Emergency grid expansion: electricity now {country.electricityLevel:F0}%", sym);
                        }
                    }
                });

            // ── 4. Food Security Crisis ───────────────────────────────────────
            if (c.foodSecurity < 50f)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Agricultural Emergency Support Package",
                    Category   = "Food Security",
                    Rationale  = $"Food security at {c.foodSecurity:F0}% — below the 50% threshold. Famine risk rising.",
                    Benefits   = $"+ Food security +5%\n+ Stability +1\n+ Poverty rate −2%",
                    Risks      = "- Treasury −10B\n- Inflation +0.3%",
                    Cost       = 10f,
                    UrgencyScore = 85,
                    Apply = country =>
                    {
                        if (SpendTreasury(country, 10f))
                        {
                            country.foodSecurity  = Mathf.Min(100f, country.foodSecurity  + 5f);
                            country.stability     = Mathf.Min(100f, country.stability     + 1f);
                            country.povertyRate   = Mathf.Max(0f,   country.povertyRate   - 2f);
                            country.inflationRate += 0.003f;
                            LogReform(country, $"Agricultural emergency package deployed", sym);
                        }
                    }
                });

            // ── 5. Treasury Critically Low ────────────────────────────────────
            float treasury = Core.GameManager.Instance?.playerBudget ?? c.treasury;
            if (treasury < 5f)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Emergency Revenue Mobilisation",
                    Category   = "Revenue",
                    Rationale  = $"Treasury critical at {sym}{treasury:F1}B. Immediate revenue action required to avoid default.",
                    Benefits   = $"+ Tax rate +3% → immediate revenue lift\n+ Prevents credit rating collapse",
                    Risks      = "- Public approval −2%\n- FDI slightly reduced",
                    Cost       = 0f,
                    UrgencyScore = 92,
                    Apply = country =>
                    {
                        country.taxRate        = Mathf.Min(50f, country.taxRate        + 3f);
                        country.publicApproval = Mathf.Max(0f,  country.publicApproval - 2f);
                        LogReform(country, $"Emergency tax hike: rate now {country.taxRate:F0}%", sym);
                    }
                });

            // ── 6. High Unemployment ──────────────────────────────────────────
            if (c.unemploymentRate > 20f)
                list.Add(new AdviserRecommendation
                {
                    Title      = "National Job Creation Programme",
                    Category   = "Welfare",
                    Rationale  = $"Unemployment at {c.unemploymentRate:F0}% — public unrest risk elevated.",
                    Benefits   = $"+ Unemployment −3%\n+ Approval +2%\n+ Stability +1",
                    Risks      = "- Treasury −8B\n- Marginal inflation increase",
                    Cost       = 8f,
                    UrgencyScore = 75,
                    Apply = country =>
                    {
                        if (SpendTreasury(country, 8f))
                        {
                            country.unemploymentRate = Mathf.Max(0f,   country.unemploymentRate - 3f);
                            country.publicApproval   = Mathf.Min(100f, country.publicApproval   + 2f);
                            country.stability        = Mathf.Min(100f, country.stability        + 1f);
                            country.inflationRate   += 0.002f;
                            LogReform(country, "Job creation programme launched", sym);
                        }
                    }
                });

            // ── 7. Underdeveloped Manufacturing ───────────────────────────────
            if (c.indManufacturing < 2)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Industrial Base Development",
                    Category   = "Industry",
                    Rationale  = $"Manufacturing sector is at tier {c.indManufacturing} — far below potential. Economic diversification requires it.",
                    Benefits   = $"+ Manufacturing tier +1\n+ GDP growth +0.2%\n+ Workforce +1,500",
                    Risks      = "-  Treasury −15B\n- Short-term power demand increase",
                    Cost       = 15f,
                    UrgencyScore = 70,
                    Apply = country =>
                    {
                        if (SpendTreasury(country, 15f))
                        {
                            country.indManufacturing   = Mathf.Min(10, country.indManufacturing + 1);
                            country.workforceManufacturing += 1500f;
                            country.gdpGrowthRate       = Mathf.Min(0.15f, country.gdpGrowthRate + 0.002f);
                            LogReform(country, $"Manufacturing upgraded to tier {country.indManufacturing}", sym);
                        }
                    }
                });

            // ── 8. Strong Fiscal Position → FDI Incentive ────────────────────
            if (c.stability > 70f && debtGDP < 0.3f && c.taxRate > 20f)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Foreign Direct Investment Incentive Package",
                    Category   = "Growth",
                    Rationale  = $"Stable economy (stability {c.stability:F0}%, low debt) ready to attract FDI through competitive tax.",
                    Benefits   = $"+ Foreign investment +{sym}20B\n+ GDP growth +0.1%\n+ Export potential",
                    Risks      = "- Tax rate −2%\n- Marginal short-term revenue dip",
                    Cost       = 0f,
                    UrgencyScore = 60,
                    Apply = country =>
                    {
                        country.taxRate          = Mathf.Max(5f,    country.taxRate        - 2f);
                        country.foreignInvestment = Mathf.Min(500f, country.foreignInvestment + 20f);
                        country.gdpGrowthRate    = Mathf.Min(0.15f, country.gdpGrowthRate   + 0.001f);
                        LogReform(country, $"FDI incentive: tax reduced to {country.taxRate:F0}%", sym);
                    }
                });

            // ── 9. High Corruption ────────────────────────────────────────────
            if (c.corruptionIndex > 50f)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Anti-Corruption Drive",
                    Category   = "Governance",
                    Rationale  = $"Corruption leakage at {c.corruptionIndex:F0}% — significant treasury drain each month.",
                    Benefits   = $"+ Corruption −5%\n+ Stability +1\n+ Credit outlook improves",
                    Risks      = "- Treasury −5B (enforcement costs)\n- Political backlash risk",
                    Cost       = 5f,
                    UrgencyScore = 65,
                    Apply = country =>
                    {
                        if (SpendTreasury(country, 5f))
                        {
                            country.corruptionIndex = Mathf.Max(0f,   country.corruptionIndex - 5f);
                            country.stability       = Mathf.Min(100f, country.stability       + 1f);
                            LogReform(country, "Anti-corruption drive: leakage reduced", sym);
                        }
                    }
                });

            // 10. High Deficit / Blocked Budget Finance Lock
            float deficit = c.spending - c.income;
            if (deficit > 0f && c.treasury < deficit)
                list.Add(new AdviserRecommendation
                {
                    Title      = "Issue Sovereign Bonds (Deficit Finance)",
                    Category   = "Debt Management",
                    Rationale  = $"Treasury is insufficient to finance the projected deficit of {sym}{deficit * c.exchangeRate:F1}B. Operations are blocked.",
                    Benefits   = $"+ Instant {sym}2.5T injection to Treasury\n+ Unlocks budget approval",
                    Risks      = "- Massive increase in National Debt\n- Credit rating pressure",
                    Cost       = 0f,
                    UrgencyScore = 98,
                    Apply = country =>
                    {
                        float borrowAmountBUSD = 2500f / country.exchangeRate; // Approx 2.5T local
                        country.debt += borrowAmountBUSD;
                        if (Core.GameManager.Instance != null) {
                            Core.GameManager.Instance.playerBudget += borrowAmountBUSD;
                            country.treasury = Core.GameManager.Instance.playerBudget;
                        } else {
                            country.treasury += borrowAmountBUSD;
                        }
                        country.publicApproval     = Mathf.Max(0f, country.publicApproval - 2f);
                        LogReform(country, "Sovereign Bonds Issued: Treasury augmented to finance deficit.", sym);
                    }
                });

            // Sort by urgency descending
            list.Sort((a, b) => b.UrgencyScore.CompareTo(a.UrgencyScore));
            return list;

        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static bool SpendTreasury(CountryDataSO c, float billions)
        {
            var gm = Core.GameManager.Instance;
            float available = gm != null ? gm.playerBudget : c.treasury;
            if (available < billions)
            {
                NotificationManager.Instance?.AddNotification(
                    $"Insufficient treasury ({UiFormat.Money(available)}) to fund this recommendation.", Color.red);
                return false;
            }
            if (gm != null) { gm.playerBudget -= billions; c.treasury = gm.playerBudget; }
            else c.treasury -= billions;
            return true;
        }

        private static void LogReform(CountryDataSO c, string entry, string sym)
        {
            if (c.taxReformLog == null) c.taxReformLog = new List<string>();
            int turn = Core.GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            c.taxReformLog.Insert(0, $"[Turn {turn}] {entry}");
            if (c.taxReformLog.Count > 20) c.taxReformLog.RemoveAt(c.taxReformLog.Count - 1);
            c.lastAdviserDecision = entry;
        }

        private static string ImproveCreditStep(string current)
        {
            string[] ladder = { "D", "C", "CC", "CCC", "B", "B+", "BB", "BB+", "BBB", "BBB+", "A", "A+", "AA", "AA+", "AAA" };
            int idx = System.Array.IndexOf(ladder, current);
            return (idx >= 0 && idx < ladder.Length - 1) ? ladder[idx + 1] : current;
        }
    }
}

