using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Interfaces;
using StateCraft.Systems;

namespace StateCraft.UI
{
    /// <summary>
    /// Shared builders for live dashboard feeds, briefings, and game-driven status text.
    /// </summary>
    public static class DashboardLiveContent
    {
        public struct FeedLine
        {
            public string Tag;
            public string Message;
            public Color Color;
        }

        public static string TurnStamp(GameManager gm)
        {
            if (gm?.TurnSystem == null) return "LIVE";
            return gm.TurnSystem.CurrentDate;
        }

        public static float ComputeRelation(CountryDataSO player, CountryDataSO other, GameManager gm)
        {
            if (player == null || other == null) return 50f;

            float score = 50f + (other.stability - 50f) * 0.25f;

            if (gm?.WarSystem != null)
            {
                var stage = gm.WarSystem.GetWarStage(other);
                if (stage == WarStage.Conflict) score -= 45f;
                else if (stage == WarStage.Crisis) score -= 25f;
                else if (stage == WarStage.Tension) score -= 12f;
            }

            if (gm?.DiplomacySystem is DiplomacySystem diplo)
            {
                foreach (var deal in diplo.ActiveDeals)
                {
                    if (deal.sellerCountry == other.countryName || deal.buyerCountry == other.countryName)
                        score += 12f;
                }
            }

            return Mathf.Clamp(score, 0f, 100f);
        }

        public static string RelationLabel(float relation)
        {
            if (relation >= 75f) return "FRIENDLY";
            if (relation >= 55f) return "WARM";
            if (relation >= 35f) return "NEUTRAL";
            if (relation >= 20f) return "COOL";
            return "HOSTILE";
        }

        public static Color RelationColor(float relation)
        {
            if (relation >= 70f) return new Color(0f, 230f / 255f, 118f / 255f);
            if (relation >= 40f) return new Color(241f / 255f, 196f / 255f, 15f / 255f);
            return new Color(255f / 255f, 82f / 255f, 82f / 255f);
        }

        public static string WarStageLabel(GameManager gm, CountryDataSO country)
        {
            if (gm?.WarSystem == null || country == null) return "PEACE";
            return gm.WarSystem.GetWarStage(country).ToString().ToUpper();
        }

        public static Color WarStageColor(WarStage stage)
        {
            switch (stage)
            {
                case WarStage.Conflict: return new Color(255f / 255f, 82f / 255f, 82f / 255f);
                case WarStage.Crisis: return new Color(255f / 255f, 171f / 255f, 64f / 255f);
                case WarStage.Tension: return new Color(241f / 255f, 196f / 255f, 15f / 255f);
                default: return new Color(0f, 230f / 255f, 118f / 255f);
            }
        }

        public static string BuildEconomyBriefing(CountryDataSO c, GameManager gm)
        {
            if (c == null) return "Awaiting fiscal telemetry…";

            var sb = new StringBuilder();
            float debtRatio = c.gdp > 0 ? c.debt / c.gdp : 0f;
            float net = c.income - c.spending;

            if (net >= 0)
                sb.Append($"Treasury trending positive this cycle. ");
            else
                sb.Append($"Deficit pressure building — spending exceeds receipts. ");

            if (c.inflationRate > 0.15f)
                sb.Append("Inflation is eroding purchasing power. ");
            else if (c.inflationRate < 0.03f)
                sb.Append("Price stability supports investor confidence. ");

            if (debtRatio > 0.8f)
                sb.Append("Debt load is stressing sovereign credit. ");
            else if (debtRatio < 0.3f)
                sb.Append("Fiscal headroom remains comfortable. ");

            if (c.electricityLevel < 40f)
                sb.Append("Grid failures are throttling industrial output.");
            else if (c.electricityLevel > 75f)
                sb.Append("Power surplus is lifting factory utilization.");

            if (c.isImportingUranium)
                sb.Append(" Uranium imports sustaining nuclear ambitions.");

            int tradeDeals = 0;
            if (gm?.DiplomacySystem is DiplomacySystem ds)
            {
                foreach (var d in ds.ActiveDeals)
                    if (d.buyerCountry == c.countryName || d.sellerCountry == c.countryName)
                        tradeDeals++;
            }
            if (tradeDeals > 0)
                sb.Append($" {tradeDeals} active trade agreement(s) feeding the ledger.");

            return sb.ToString().Trim();
        }

        public static List<FeedLine> BuildEconomyFeed(CountryDataSO c, GameManager gm)
        {
            var lines = new List<FeedLine>();
            string stamp = TurnStamp(gm);
            var cyan = new Color(0f, 210f / 255f, 255f / 255f);
            var green = new Color(0f, 230f / 255f, 118f / 255f);
            var amber = new Color(241f / 255f, 196f / 255f, 15f / 255f);
            var red = new Color(255f / 255f, 82f / 255f, 82f / 255f);

            lines.Add(new FeedLine { Tag = stamp, Message = $"FX desk: {c.currencyName} trading at {c.exchangeRate:F2} per USD.", Color = cyan });

            if (c.gdpGrowthRate > 0.04f)
                lines.Add(new FeedLine { Tag = "GDP", Message = $"Growth print strong at {c.gdpGrowthRate:P1} — markets upbeat.", Color = green });
            else if (c.gdpGrowthRate < 0f)
                lines.Add(new FeedLine { Tag = "GDP", Message = "Output contracting. Industrial orders slowing.", Color = red });

            if (c.inflationRate > 0.12f)
                lines.Add(new FeedLine { Tag = "CPI", Message = $"Inflation at {c.inflationRate:P1} — central bank under pressure.", Color = red });
            else
                lines.Add(new FeedLine { Tag = "CPI", Message = $"Consumer prices stable ({c.inflationRate:P1}).", Color = green });

            if (c.foodSecurity < 50f)
                lines.Add(new FeedLine { Tag = "AGRI", Message = "Food security index flashing amber — rationing risk.", Color = amber });
            if (c.currentBlackoutRisk > 0.2f || c.electricityLevel < 35f)
                lines.Add(new FeedLine { Tag = "GRID", Message = "Rolling blackouts reported in manufacturing hubs.", Color = red });

            foreach (var res in c.resources)
            {
                if (res.abundance >= 70f)
                {
                    lines.Add(new FeedLine { Tag = "COMM", Message = $"{res.type} futures firm on export demand.", Color = green });
                    break;
                }
            }

            if (gm?.DiplomacySystem is DiplomacySystem diplo)
            {
                foreach (var deal in diplo.ActiveDeals)
                {
                    if (deal.buyerCountry != c.countryName && deal.sellerCountry != c.countryName) continue;
                    bool paying = deal.buyerCountry == c.countryName;
                    lines.Add(new FeedLine
                    {
                        Tag = "TRADE",
                        Message = paying
                            ? $"Import bill: {deal.resource} from {deal.sellerCountry} (${deal.monthlyPaymentUSD / 1e6f:F1}M/mo)."
                            : $"Export revenue: {deal.resource} to {deal.buyerCountry} (${deal.monthlyPaymentUSD / 1e6f:F1}M/mo).",
                        Color = paying ? amber : green
                    });
                }
            }

            if (c.activeProjects.Count > 0)
                lines.Add(new FeedLine { Tag = "BUILD", Message = $"{c.activeProjects.Count} megaproject(s) on the construction pipeline.", Color = cyan });

            AppendNewsHeadlines(lines, 2);
            return lines;
        }

        public static List<FeedLine> BuildMilitaryFeed(CountryDataSO c, GameManager gm)
        {
            var lines = new List<FeedLine>();
            string stamp = TurnStamp(gm);
            var cyan = new Color(0f, 210f / 255f, 255f / 255f);
            var green = new Color(0f, 230f / 255f, 118f / 255f);
            var amber = new Color(241f / 255f, 196f / 255f, 15f / 255f);
            var red = new Color(255f / 255f, 82f / 255f, 82f / 255f);

            var stage = gm?.WarSystem != null ? gm.WarSystem.GetWarStage(c) : WarStage.Peace;
            lines.Add(new FeedLine { Tag = stamp, Message = $"Theatre posture: {stage}. Readiness {c.militaryReadiness:F0}%.", Color = WarStageColor(stage) });

            if (c.reserveAmmo < 50f)
                lines.Add(new FeedLine { Tag = "LOG", Message = "Ammunition reserves below operational threshold.", Color = red });
            if (c.reserveFuel < 50f)
                lines.Add(new FeedLine { Tag = "LOG", Message = "Fuel stockpile critical — convoys prioritized.", Color = amber });
            else if (c.reserveFuel > 80f && c.reserveAmmo > 80f)
                lines.Add(new FeedLine { Tag = "LOG", Message = "Strategic reserves fully replenished.", Color = green });

            foreach (var op in c.activeOperations)
                lines.Add(new FeedLine { Tag = "OPS", Message = $"{op.opName} [{op.type}] — {op.status} in {op.regionName}.", Color = cyan });

            foreach (var region in c.regions)
            {
                if (region.security < 40f)
                {
                    lines.Add(new FeedLine { Tag = "COIN", Message = $"{region.regionName}: security {region.security:F0}% — reinforcements requested.", Color = red });
                    break;
                }
            }

            if (c.rdfCooldownMonths > 0)
                lines.Add(new FeedLine { Tag = "RDF", Message = $"Rapid deployment force regrouping ({c.rdfCooldownMonths} mo).", Color = amber });

            if (c.unitFighters > 0 || c.unitFrigates > 0)
                lines.Add(new FeedLine { Tag = "ORBAT", Message = $"Order of battle: {c.unitInfantry:N0} infantry, {c.unitTanks} MBTs, {c.unitFighters} fighters.", Color = cyan });

            AppendNewsHeadlines(lines, 2);
            return lines;
        }

        public static List<FeedLine> BuildDiplomacyFeed(CountryDataSO c, GameManager gm)
        {
            var lines = new List<FeedLine>();
            string stamp = TurnStamp(gm);
            var cyan = new Color(76f / 255f, 175f / 255f, 80f / 255f);
            var amber = new Color(241f / 255f, 196f / 255f, 15f / 255f);
            var red = new Color(255f / 255f, 82f / 255f, 82f / 255f);

            lines.Add(new FeedLine { Tag = stamp, Message = $"Prestige index {c.globalPrestige:F0}/100 — {c.allianceCount} alliance(s) active.", Color = cyan });

            if (gm?.DiplomacySystem is DiplomacySystem diplo)
            {
                foreach (var deal in diplo.ActiveDeals)
                {
                    if (deal.buyerCountry != c.countryName && deal.sellerCountry != c.countryName) continue;
                    lines.Add(new FeedLine
                    {
                        Tag = "DEAL",
                        Message = $"{deal.sellerCountry} → {deal.buyerCountry}: {deal.resource} (${deal.monthlyPaymentUSD / 1e6f:F1}M/mo, {deal.durationTurns} mo).",
                        Color = cyan
                    });
                }
            }

            if (gm?.allCountries != null)
            {
                CountryDataSO hottest = null;
                float lowest = 100f;
                foreach (var other in gm.allCountries)
                {
                    if (other == c) continue;
                    float rel = ComputeRelation(c, other, gm);
                    if (rel < lowest) { lowest = rel; hottest = other; }
                }
                if (hottest != null && lowest < 35f)
                    lines.Add(new FeedLine { Tag = "ALERT", Message = $"Bilateral strain with {hottest.countryName} ({RelationLabel(lowest)}).", Color = red });
            }

            foreach (var crisis in c.activeCrises)
                lines.Add(new FeedLine { Tag = "UN", Message = $"{crisis.crisisName} — pressure {crisis.internationalPressure:F0}%.", Color = amber });

            if (c.sanctionReceivedCount > 0)
                lines.Add(new FeedLine { Tag = "SANCTION", Message = $"{c.sanctionReceivedCount} sanction package(s) targeting our economy.", Color = red });
            if (c.sanctionIssuedCount > 0)
                lines.Add(new FeedLine { Tag = "SANCTION", Message = $"We have issued {c.sanctionIssuedCount} sanction measure(s).", Color = amber });

            foreach (var treaty in c.activeTreaties)
                lines.Add(new FeedLine { Tag = "TREATY", Message = $"{treaty.treatyName} with {treaty.partnerCountry} ({treaty.monthsRemaining} mo left).", Color = cyan });

            AppendNewsHeadlines(lines, 2);
            return lines;
        }

        public static List<FeedLine> BuildIntelFeed(CountryDataSO c, GameManager gm)
        {
            var lines = new List<FeedLine>();
            string stamp = TurnStamp(gm);
            var purple = new Color(186f / 255f, 104f / 255f, 200f / 255f);
            var amber = new Color(241f / 255f, 196f / 255f, 15f / 255f);
            var red = new Color(255f / 255f, 82f / 255f, 82f / 255f);
            var green = new Color(0f, 230f / 255f, 118f / 255f);

            lines.Add(new FeedLine { Tag = stamp, Message = $"SIGINT coverage {c.globalIntelCoverage:F0}% — {c.operativeCount} field operatives.", Color = purple });

            if (c.foreignAgentsDetected > 0)
                lines.Add(new FeedLine { Tag = "CI", Message = $"{c.foreignAgentsDetected} foreign asset(s) flagged inside the republic.", Color = red });
            else
                lines.Add(new FeedLine { Tag = "CI", Message = "Domestic counter-intelligence sweep: no new moles.", Color = green });

            if (c.agencyIntegrity < 60f)
                lines.Add(new FeedLine { Tag = "AUDIT", Message = "Agency integrity compromised — leak risk elevated.", Color = red });

            foreach (var op in c.activeCovertOps)
                lines.Add(new FeedLine { Tag = "BLACK", Message = $"{op.opName} targeting {op.target} — {op.status} ({op.riskLevel} risk).", Color = purple });

            foreach (var threat in c.detectedThreats)
                lines.Add(new FeedLine { Tag = "THREAT", Message = $"{threat.threatName}: {threat.severity} ({threat.category}).", Color = threat.severity == "Critical" ? red : amber });

            if (gm?.allCountries != null)
            {
                foreach (var other in gm.allCountries)
                {
                    if (other == c) continue;
                    if (gm.WarSystem != null && gm.WarSystem.GetWarStage(other) >= WarStage.Crisis)
                    {
                        lines.Add(new FeedLine { Tag = "HUMINT", Message = $"Surveillance tasking increased on {other.countryName} ({gm.WarSystem.GetWarStage(other)}).", Color = amber });
                        break;
                    }
                }
            }

            AppendNewsHeadlines(lines, 2);
            return lines;
        }

        private static void AppendNewsHeadlines(List<FeedLine> lines, int max)
        {
            if (NewsSystem.Instance == null) return;
            int added = 0;
            foreach (var headline in NewsSystem.Instance.NewsHistory)
            {
                if (added >= max) break;
                lines.Add(new FeedLine { Tag = "WIRE", Message = headline, Color = new Color(0.6f, 0.6f, 0.65f) });
                added++;
            }
        }

        public static void PopulateFeed(VisualElement container, List<FeedLine> lines, int maxItems = 8)
        {
            if (container == null) return;
            container.Clear();

            if (lines == null || lines.Count == 0)
            {
                container.Add(new Label("No live signals this cycle.")
                {
                    style = { color = new Color(0.5f, 0.5f, 0.5f), fontSize = 10, unityTextAlign = TextAnchor.MiddleCenter, marginTop = 8 }
                });
                return;
            }

            int count = Mathf.Min(maxItems, lines.Count);
            for (int i = 0; i < count; i++)
            {
                var line = lines[i];
                var row = new VisualElement();
                row.AddToClassList("live-feed-row");

                var tag = new Label(line.Tag.ToUpper())
                {
                    style =
                    {
                        color = line.Color,
                        fontSize = 8,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        minWidth = 42,
                        marginRight = 6
                    }
                };
                row.Add(tag);

                var msg = new Label(line.Message)
                {
                    style =
                    {
                        color = new Color(0.85f, 0.85f, 0.88f),
                        fontSize = 9,
                        whiteSpace = WhiteSpace.Normal,
                        flexGrow = 1
                    }
                };
                row.Add(msg);
                container.Add(row);
            }
        }

        public static void PopulateStatusPills(VisualElement container, CountryDataSO c, GameManager gm)
        {
            if (container == null || c == null) return;
            container.Clear();

            float debtRatio = c.gdp > 0 ? c.debt / c.gdp : 0f;

            if (c.stability >= 60f) AddPill(container, "STABLE", "status-green");
            else if (c.stability >= 40f) AddPill(container, "UNREST", "status-amber");
            else AddPill(container, "CRISIS", "status-red");

            if (debtRatio > 0.8f) AddPill(container, "DEBT WARNING", "status-amber");
            if (c.inflationRate > 0.12f) AddPill(container, "INFLATION", "status-red");
            if (c.electricityLevel < 40f) AddPill(container, "BLACKOUT RISK", "status-red");
            if (c.isImportingUranium) AddPill(container, "URANIUM IMPORTS", "status-green");
            if (c.foodSecurity < 50f) AddPill(container, "FOOD ALERT", "status-amber");

            if (gm?.WarSystem != null && gm.WarSystem.GetWarStage(c) != WarStage.Peace)
                AddPill(container, WarStageLabel(gm, c), "status-red");
        }

        public static void AddPill(VisualElement container, string text, string ussClass)
        {
            var pill = new VisualElement();
            pill.AddToClassList("status-pill");
            pill.AddToClassList(ussClass);
            pill.Add(new Label(text) { style = { fontSize = 8 } });
            container.Add(pill);
        }

        public static string TrendArrow(float value, float goodAbove, float badBelow)
        {
            if (value >= goodAbove) return " ▲";
            if (value <= badBelow) return " ▼";
            return " ●";
        }
    }
}
