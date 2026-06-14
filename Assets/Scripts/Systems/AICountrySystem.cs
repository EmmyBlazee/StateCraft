// ============================================================
// FILE: Scripts/Systems/AICountrySystem.cs  (FULL REPLACEMENT)
// PURPOSE: AI can now:
//   - Declare war when conditions are met
//   - Issue ultimatums (pre-war pressure)
//   - All AI decisions driven by the RelationsMatrix
// ============================================================

using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using UnityEngine;
using System.Collections.Generic;

namespace StateCraft.Systems
{
    public class AICountrySystem : IAICountrySystem
    {
        // ── Cooldowns so AI doesn't spam ─────────────────────
        private Dictionary<string, int> _lastUltimatumMonth = new Dictionary<string, int>();
        private Dictionary<string, int> _lastWarDeclaredMonth = new Dictionary<string, int>();

        private const float WAR_DECLARATION_RELATION_THRESHOLD = -55f;  // below this score, AI may declare war
        private const float ULTIMATUM_RELATION_THRESHOLD       = -35f;  // below this, AI may issue ultimatum
        private const int   ULTIMATUM_COOLDOWN_MONTHS          = 12;    // don't repeat ultimatum within a year
        private const int   WAR_COOLDOWN_MONTHS                = 24;    // don't declare war again within 2 years
        private const float WAR_DECLARATION_POWER_THRESHOLD    = 0.75f; // AI declares war only if military ≥ 75% of target

        public void RunAI(CountryDataSO country)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            int currentMonth = gm.TurnSystem?.CurrentTurn ?? 0;

            // ── Security & internal stability ──────────────
            HandleInternalSecurity(country, gm);

            // ── Military expansion ──────────────────────────
            HandleMilitaryExpansion(country, gm);

            // ── Relations-driven foreign policy ────────────
            HandleForeignPolicy(country, gm, currentMonth);

            // ── Trade proposals ────────────────────────────
            HandleTradeProposals(country, gm);
        }

        // ── Internal Security (unchanged from original) ─────
        private void HandleInternalSecurity(CountryDataSO country, GameManager gm)
        {
            if (country.stability < 60f)
            {
                var worstRegion = GetWorstRegion(country);
                if (worstRegion != null)
                {
                    if (worstRegion.stability < 30f)
                        gm.SecuritySystem.DeployRDF(country, worstRegion);
                    else if (worstRegion.security < 40f)
                        gm.SecuritySystem.DeployMilitary(country, worstRegion);
                    else
                        gm.SecuritySystem.DeployPolice(country, worstRegion);
                }
            }

            if (country.stability < 20f && gm.WarSystem.GetWarStage(country) == WarStage.Peace)
                gm.WarSystem.SetWarStage(country, WarStage.Tension);
        }

        // ── Military Expansion (unchanged from original) ─────
        private void HandleMilitaryExpansion(CountryDataSO country, GameManager gm)
        {
            if (country.treasury <= 5f) return;

            var warStage = gm.WarSystem != null ? gm.WarSystem.GetWarStage(country) : WarStage.Peace;
            float spendFraction = 0.04f;
            if (warStage >= WarStage.Tension)  spendFraction = 0.12f;
            if (warStage >= WarStage.Conflict)  spendFraction = 0.30f;

            float milSpendB = country.treasury * spendFraction;
            if (milSpendB <= 0.04f) return;

            country.treasury -= milSpendB;

            if (country.unitRifles < country.armyStats.personnel)
            {
                int amount = Mathf.RoundToInt((milSpendB / 0.01f) * 1000f);
                country.unitRifles += amount;
            }
            else
            {
                float r = Random.value;
                if (r < 0.45f)
                {
                    int amount = Mathf.RoundToInt((milSpendB / 0.05f) * 1000f);
                    int cap = 5000 + (country.baseArmyCount * 15000);
                    if (country.armyStats.personnel + amount <= cap)
                    {
                        country.unitInfantry += amount;
                        country.armyStats.personnel += amount;
                    }
                    else
                    {
                        country.baseArmyCount += 1;
                    }
                }
                else if (r < 0.75f)
                {
                    int tanks = Mathf.RoundToInt((milSpendB / 0.25f) * 10f);
                    int apcs  = Mathf.RoundToInt((milSpendB / 0.15f) * 20f);
                    if (tanks > 0) { country.unitTanks += tanks; country.armyStats.equipment += tanks; }
                    if (apcs  > 0) { country.unitAPCs  += apcs;  country.armyStats.equipment += apcs; }
                }
                else
                {
                    int fighters = Mathf.RoundToInt((milSpendB / 0.5f) * 2f);
                    int helis    = Mathf.RoundToInt((milSpendB / 0.3f) * 4f);
                    if (fighters > 0) { country.unitFighters     += fighters; country.airForceStats.equipment += fighters; }
                    if (helis    > 0) { country.unitAttackHelis  += helis;    country.airForceStats.equipment += helis; }
                }
            }
        }

        // ── FOREIGN POLICY — driven by RelationsMatrix ───────
        private void HandleForeignPolicy(CountryDataSO country, GameManager gm, int currentMonth)
        {
            if (gm.allCountries == null) return;
            if (gm.Relations == null)    return;

            // Only check once every 2-3 months per country (add jitter)
            if (currentMonth % 2 != (Mathf.Abs(country.countryName.GetHashCode()) % 2)) return;

            foreach (var target in gm.allCountries)
            {
                if (target == country) return;

                float relation = gm.Relations.GetScore(country.countryName, target.countryName);
                var   warStage = gm.WarSystem.GetWarStage(country);

                // Skip if already at war with this target
                string warKey = country.countryName + "_" + target.countryName;
                if (warStage == WarStage.Conflict) continue;

                // ── ULTIMATUM LOGIC ──────────────────────────
                // AI issues an ultimatum when:
                //   - Relations below threshold
                //   - Target is weak OR has something AI wants
                //   - No recent ultimatum or war
                if (relation <= ULTIMATUM_RELATION_THRESHOLD
                    && !target.hasNuclearWeapon
                    && !_lastUltimatumMonth.ContainsKey(warKey + "_ult")
                    || (_lastUltimatumMonth.ContainsKey(warKey + "_ult")
                        && currentMonth - _lastUltimatumMonth[warKey + "_ult"] > ULTIMATUM_COOLDOWN_MONTHS))
                {
                    // Only pursue if AI has meaningful military
                    if (country.armyStats.personnel > 5000 && Random.value < 0.25f)
                    {
                        DeclareUltimatum(country, target, gm, currentMonth);
                        _lastUltimatumMonth[warKey + "_ult"] = currentMonth;
                        continue;
                    }
                }

                // ── WAR DECLARATION LOGIC ────────────────────
                // AI declares war when:
                //   - Relations below war threshold OR existing ultimatum expired
                //   - Military power ≥ 75% of target's
                //   - No recent war
                bool ultimatumExpired = false;
                var rel = gm.Relations.GetOrCreate(country.countryName, target.countryName);
                if (rel.hasUltimatum
                    && rel.ultimatumFromCountry == country.countryName
                    && !rel.ultimatumResolved
                    && currentMonth >= rel.ultimatumDeadlineMonth)
                {
                    ultimatumExpired = true;
                }

                bool relationsBelowWarThreshold = relation <= WAR_DECLARATION_RELATION_THRESHOLD;

                if ((relationsBelowWarThreshold || ultimatumExpired)
                    && !target.hasNuclearWeapon
                    && country.armyStats.personnel > 3000)
                {
                    // Cooldown check
                    bool onCooldown = _lastWarDeclaredMonth.ContainsKey(warKey)
                        && currentMonth - _lastWarDeclaredMonth[warKey] < WAR_COOLDOWN_MONTHS;

                    if (!onCooldown)
                    {
                        float myPower     = EstimateMilitaryPower(country);
                        float targetPower = EstimateMilitaryPower(target);

                        if (targetPower <= 0f || (myPower / targetPower) >= WAR_DECLARATION_POWER_THRESHOLD)
                        {
                            if (Random.value < (ultimatumExpired ? 0.85f : 0.20f))
                            {
                                // Mark ultimatum resolved before war
                                if (rel.hasUltimatum)
                                {
                                    rel.ultimatumResolved = true;
                                    rel.hasUltimatum      = false;
                                }

                                gm.WarSystem.DeclareWar(country, target);
                                _lastWarDeclaredMonth[warKey] = currentMonth;

                                // Notify player if the player is involved
                                if (gm.playerCountry == target)
                                {
                                    // Player is the defender — notifications come from DeclareWar
                                }
                                else if (gm.playerCountry != country)
                                {
                                    // Third-party observer
                                    UI.NotificationManager.Instance?.AddNotification(
                                        $"⚔ WAR: {country.countryName} declares war on {target.countryName}. " +
                                        $"World tension rising.",
                                        new Color(1f, 0.4f, 0.1f));
                                }

                                gm.Relations.Adjust(country.countryName, target.countryName, -40f,
                                    $"{country.countryName} declared war", currentMonth);

                                break; // Only declare one war per AI turn
                            }
                        }
                    }
                }
            }
        }

        // ── ULTIMATUM ─────────────────────────────────────────
        /// <summary>
        /// AI issues a formal ultimatum to <paramref name="target"/>.
        /// The demand is picked based on the AI's most pressing need.
        /// If the target (player) does not comply within 3 months, war follows.
        /// </summary>
        private void DeclareUltimatum(CountryDataSO sender, CountryDataSO target,
                                      GameManager gm, int currentMonth)
        {
            var rel = gm.Relations.GetOrCreate(sender.countryName, target.countryName);
            if (rel.hasUltimatum) return; // already one pending

            // Pick a demand based on what the AI needs most
            string demand = ChooseUltimatumDemand(sender, target);

            rel.hasUltimatum           = true;
            rel.ultimatumFromCountry   = sender.countryName;
            rel.ultimatumDemand        = demand;
            rel.ultimatumDeadlineMonth = currentMonth + 3; // 3-month deadline
            rel.ultimatumResolved      = false;

            gm.Relations.Adjust(sender.countryName, target.countryName, -15f,
                $"Ultimatum issued by {sender.countryName}", currentMonth);

            // If the target is the player, push an urgent inbox message
            if (gm.playerCountry == target)
            {
                InboxSystem.Instance?.SendMessage(target,
                    $"EMBASSY OF {sender.countryName.ToUpper()}",
                    $"⚠️ ULTIMATUM RECEIVED — RESPONSE REQUIRED",
                    $"The Government of {sender.countryName} formally demands:\n\n" +
                    $"\"{demand}\"\n\n" +
                    $"DEADLINE: {3} months from today.\n\n" +
                    $"Failure to comply will be treated as a hostile act and may result in military action. " +
                    $"Respond via DIPLOMACY → RELATIONS → select {sender.countryName} → ULTIMATUM RESPONSE.",
                    "Critical");

                UI.NotificationManager.Instance?.AddNotification(
                    $"⚠ ULTIMATUM from {sender.countryName}: \"{demand}\". " +
                    $"Respond within 3 months or face war. Check Inbox.",
                    Color.red);

                NewsSystem.Instance?.PublishNews(
                    $"CRISIS: {sender.countryName} issues formal ultimatum to {target.countryName}. " +
                    $"Diplomatic sources warn of military escalation if demands unmet.");
            }
            else
            {
                // Third-party observer sees the headline
                NewsSystem.Instance?.PublishNews(
                    $"ULTIMATUM: {sender.countryName} issues formal demands to {target.countryName}. " +
                    $"Regional tensions spike.");
            }
        }

        private string ChooseUltimatumDemand(CountryDataSO sender, CountryDataSO target)
        {
            // Priority 1: Resource access
            if (sender.resources != null)
            {
                foreach (var res in sender.resources)
                {
                    if (res.abundance < 15f)
                        return $"Guarantee unimpeded access to {res.type} exports from your territory, " +
                               $"effective immediately and for no less than 24 months.";
                }
            }

            // Priority 2: Territory / border incidents
            if (sender.stability < 40f)
                return $"Cease all cross-border activities that destabilise our border regions, " +
                       $"dismantle militia camps within 60 km of the shared border, " +
                       $"and submit to joint border monitoring within 30 days.";

            // Priority 3: Prestige / sanctions
            if (target.globalPrestige > sender.globalPrestige + 20f)
                return $"Publicly withdraw the economic sanctions imposed against our nation " +
                       $"and restore normal diplomatic relations by the stated deadline.";

            // Fallback
            return $"Reduce your military presence along our shared border by 50% " +
                   $"and provide written assurance of non-aggression for the next 36 months.";
        }

        // ── Helper: crude military power estimate ────────────
        private float EstimateMilitaryPower(CountryDataSO c)
        {
            return c.unitInfantry * 1f
                 + c.unitTanks    * 12f
                 + c.unitAPCs     * 5f
                 + c.unitFighters * 20f;
        }

        private RegionDataSO GetWorstRegion(CountryDataSO country)
        {
            RegionDataSO target = null;
            float worst = 101f;
            foreach (var r in country.regions)
            {
                if (r.stability < worst) { worst = r.stability; target = r; }
            }
            return target;
        }

        // ── Trade Proposals (unchanged from original) ─────────
        private void HandleTradeProposals(CountryDataSO country, GameManager gm)
        {
            if (Random.value >= 0.2f || gm.playerCountry?.resources == null) return;

            foreach (var res in country.resources)
            {
                if (res.abundance >= 20f) continue;

                var playerRes = gm.playerCountry.resources.Find(r => r.type == res.type);
                if (playerRes.type != res.type || playerRes.abundance <= 40f) continue;

                TradeResourceType tradeResType = TradeResourceType.Minerals;
                System.Enum.TryParse(res.type.ToString(), out tradeResType);

                float qty     = 2000f;
                float payment = GlobalTradeMarket.GetPricePerUnit(tradeResType) * qty;

                if (gm.DiplomacySystem is DiplomacySystem diplo)
                {
                    var proposal = diplo.CreateProposal(gm.playerCountry, country,
                        tradeResType, payment, 12, true);
                    proposal.resourceQuantity = qty;

                    InboxSystem.Instance?.SendMessage(gm.playerCountry,
                        $"Embassy of {country.countryName}", "Trade Agreement Proposed",
                        $"The government of {country.countryName} urgently requests a multi-month Trade Agreement. " +
                        $"We need to import {res.type} to support our economy. " +
                        $"We offer ${payment / 1e6f:N1}M per month. Please check the Trade Desk.",
                        "Important");
                }
                break;
            }
        }

        public TradeAgreementProposal EvaluateProposal(TradeAgreementProposal proposal)
        {
            float paymentFactor    = Random.Range(0.9f, 1.1f);
            int   durationAdjust   = Random.Range(-2, 3);
            proposal.baseMonthlyPaymentUSD = proposal.baseMonthlyPaymentUSD * paymentFactor;
            proposal.baseDurationMonths    = Mathf.Max(1, proposal.baseDurationMonths + durationAdjust);
            return proposal;
        }
    }
}
