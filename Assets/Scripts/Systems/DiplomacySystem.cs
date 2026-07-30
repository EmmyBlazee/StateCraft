using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using UnityEngine;
using System.Collections.Generic;

namespace StateCraft.Systems
{
    public class DiplomacySystem : IDiplomacySystem, ISaveable
    {
        [System.Serializable]
        private class DiplomacySaveData
        {
            public List<TradeDeal> ActiveDeals = new List<TradeDeal>();
            public List<string> pendingProposalsKeys = new List<string>();
            public List<TradeAgreementProposal> pendingProposalsValues = new List<TradeAgreementProposal>();
        }

        public string ExportState()
        {
            var data = new DiplomacySaveData();
            data.ActiveDeals = ActiveDeals; // TradeDeal is already Serializable

            foreach (var kvp in PendingProposals)
            {
                data.pendingProposalsKeys.Add(kvp.Key);
                data.pendingProposalsValues.Add(kvp.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void ImportState(string json)
        {
            var data = JsonUtility.FromJson<DiplomacySaveData>(json);
            if (data == null) return;

            ActiveDeals = data.ActiveDeals ?? new List<TradeDeal>();

            PendingProposals.Clear();
            if (data.pendingProposalsKeys != null && data.pendingProposalsValues != null)
            {
                for (int i = 0; i < data.pendingProposalsKeys.Count; i++)
                {
                    if (i < data.pendingProposalsValues.Count)
                    {
                        PendingProposals[data.pendingProposalsKeys[i]] = data.pendingProposalsValues[i];
                    }
                }
            }
        }

        public List<TradeDeal> ActiveDeals = new List<TradeDeal>();
        public Dictionary<string, TradeAgreementProposal> PendingProposals { get; private set; } = new Dictionary<string, TradeAgreementProposal>();

        public void UpdateRelations(CountryDataSO country)
        {
            // Future: Relations matrix logic
        }

        
        // ─── FACTION SYSTEM ───────────────────────────────────────────────────
        public bool JoinFaction(CountryDataSO country, string factionName)
        {
            var gm = GameManager.Instance;
            if (gm == null || country == null) return false;

            if (country.globalFaction == factionName)
            {
                UI.NotificationManager.Instance?.AddNotification($"Already a member of {factionName}.", UnityEngine.Color.yellow);
                return false;
            }

            // Check if at war
            if (gm.WarSystem?.GetWarStage(country) == WarStage.Conflict)
            {
                UI.NotificationManager.Instance?.AddNotification("Cannot join a faction while at war!", UnityEngine.Color.red);
                return false;
            }

            string oldFaction = country.globalFaction;
            country.globalFaction = factionName;

            NewsSystem.Instance?.PublishNews($"GEOPOLITICS: {country.countryName} has officially joined the {factionName}.");
            UI.NotificationManager.Instance?.AddNotification($"✅ Joined {factionName}. Free trade and mutual defence activated.", new UnityEngine.Color(0f, 0.85f, 0.4f));

            // Small prestige boost for joining major faction
            country.globalPrestige = UnityEngine.Mathf.Min(100f, country.globalPrestige + 2f);

            return true;
        }

        public void LeaveFaction(CountryDataSO country)
        {
            if (country.globalFaction == "Independent" || country.globalFaction == "Non-Aligned")
            {
                UI.NotificationManager.Instance?.AddNotification("You are already Independent.", UnityEngine.Color.yellow);
                return;
            }

            string oldFaction = country.globalFaction;
            country.globalFaction = "Independent";

            NewsSystem.Instance?.PublishNews($"GEOPOLITICS: {country.countryName} has withdrawn from the {oldFaction} and is now Independent.");
            UI.NotificationManager.Instance?.AddNotification($"Withdrew from {oldFaction}. You have lost mutual defence protection.", UnityEngine.Color.yellow);
            
            // Prestige hit
            country.globalPrestige = UnityEngine.Mathf.Max(0f, country.globalPrestige - 5f);
        }

        public static bool CanTrade(CountryDataSO a, CountryDataSO b)
        {
            if (a == null || b == null) return false;
            // Same faction = always can trade
            if (a.globalFaction == b.globalFaction && a.globalFaction != "Independent") return true;
            // Otherwise, relations must be decent (> -20)
            var gm = GameManager.Instance;
            float rel = gm?.Relations?.GetScore(a.countryName, b.countryName) ?? 0f;
            return rel > -20f;
        }

        public void SignTradeDeal(CountryDataSO seller, CountryDataSO buyer, TradeResourceType resource, float paymentUSD)
        {
            // ── GATE: Trade Agreement treaty must be signed first ──────────
            if (!CanTrade(seller, buyer))
            {
                bool isPlayer = seller == GameManager.Instance?.playerCountry
                             || buyer  == GameManager.Instance?.playerCountry;
                if (isPlayer)
                {
                    string other = seller == GameManager.Instance?.playerCountry
                        ? buyer.countryName : seller.countryName;
                    UI.NotificationManager.Instance?.AddNotification(
                        $"TRADE BLOCKED: {other} refuses to trade. " +
                        "You must improve relations above -20 or join the same Faction.",
                        UnityEngine.Color.yellow);
                }
                return;
            }
            var deal = new TradeDeal
            {
                id = System.Guid.NewGuid().ToString(),
                sellerCountry = seller.countryName,
                buyerCountry = buyer.countryName,
                resource = resource,
                monthlyPaymentUSD = paymentUSD,
                durationTurns = 24 // 2 year standard deal
            };

            ActiveDeals.Add(deal);
            
            var gm2 = GameManager.Instance;
            if (gm2?.Relations != null)
            {
                int month2 = gm2.TurnSystem?.CurrentTurn ?? 0;
                gm2.Relations.Adjust(seller.countryName, buyer.countryName, +10f,
                    "Trade agreement signed", month2);
            }
            
            // Log to news
            NewsSystem.Instance?.PublishNews($"TRADE DEAL: {seller.countryName} to supply {resource} to {buyer.countryName} for ${paymentUSD/1e6f:N1}M monthly.");
            
            // Immediate Effect: If Uranium, buyer is now cleared to build Nuclear Plant
            if (resource == TradeResourceType.Uranium)
            {
                buyer.isImportingUranium = true;
            }
        }

        public void ProcessTradeDeals(CountryDataSO country)
        {
            foreach (var deal in ActiveDeals)
            {
                float paymentInBillions = deal.monthlyPaymentUSD / 1e9f;
                if (deal.buyerCountry == country.countryName)
                    country.income -= paymentInBillions;
                else if (deal.sellerCountry == country.countryName)
                    country.income += paymentInBillions;
            }

            
            // Faction economic integration bonus
            if (country.globalFaction != "Independent" && country.globalFaction != "Non-Aligned")
            {
                country.income += country.income * 0.05f; // 5% bonus for faction market integration
            }

        }

        public void CancelTradeDeal(string dealId)
        {
            TradeDeal removed = null;
            for (int i = ActiveDeals.Count - 1; i >= 0; i--)
            {
                if (ActiveDeals[i].id == dealId)
                {
                    removed = ActiveDeals[i];
                    ActiveDeals.RemoveAt(i);
                    break;
                }
            }

            if (removed == null || removed.resource != TradeResourceType.Uranium)
                return;

            var gm = GameManager.Instance;
            if (gm?.allCountries == null)
                return;

            foreach (var c in gm.allCountries)
            {
                if (c.countryName != removed.buyerCountry)
                    continue;

                bool stillImporting = false;
                foreach (var d in ActiveDeals)
                {
                    if (d.buyerCountry == c.countryName && d.resource == TradeResourceType.Uranium)
                    {
                        stillImporting = true;
                        break;
                    }
                }

                if (!stillImporting)
                    c.isImportingUranium = false;
                break;
            }
        }
        
        public TradeAgreementProposal CreateProposal(CountryDataSO player, CountryDataSO ai, TradeResourceType resource, float basePayment, int duration, bool playerIsSeller)
        {
            // ── GATE ──────────────────────────────────────────────────────
            bool hasTreaty = HasSignedTradeAgreementTreaty(player, ai);
            bool sameFaction = (player.globalFaction == ai.globalFaction && player.globalFaction != "Independent" && player.globalFaction != "Non-Aligned");
            if (!hasTreaty && !sameFaction)
                return null; // caller (TradeAgreementController) checks for null

            var proposal = new TradeAgreementProposal
            {
                partnerCountry = ai.countryName,
                resource = resource,
                baseMonthlyPaymentUSD = basePayment,
                baseDurationMonths = duration,
                playerIsSeller = playerIsSeller
            };
            
            // Forward to AI for counter-offer
            var aiSystem = GameManager.Instance.AICountrySystem;
            if (aiSystem != null)
            {
                proposal = aiSystem.EvaluateProposal(proposal);
            }
            
            PendingProposals[proposal.id] = proposal;
            return proposal;
        }

        public TradeAgreement AcceptProposal(string proposalId)
        {
            if (!PendingProposals.TryGetValue(proposalId, out var proposal))
                return null;
                
            var agreement = new TradeAgreement
            {
                id = proposal.id,
                partnerCountry = proposal.partnerCountry,
                resource = proposal.resource,
                monthlyPaymentUSD = proposal.baseMonthlyPaymentUSD,
                durationMonths = proposal.baseDurationMonths,
                status = TradeAgreementStatus.Active,
                resourceQuantity = proposal.resourceQuantity,
                isBulkTransfer = proposal.isBulkTransfer,
                isSeller = proposal.playerIsSeller,
                originalPaymentUSD = proposal.baseMonthlyPaymentUSD,
                originalDurationMonths = proposal.baseDurationMonths,
                counterReason = ""
            };
            
            // Assuming player is always the initiator in this context (as per UI)
            var gm = GameManager.Instance;
            CountryDataSO player = gm.playerCountry;
            CountryDataSO partner = null;
            
            foreach (var ai in gm.allCountries)
            {
                if (ai.countryName == agreement.partnerCountry)
                {
                    partner = ai;
                    break;
                }
            }

            if (player != null)
            {
                player.activeTradeAgreements.Add(agreement);
            }
            
            if (partner != null)
            {
                // We add a mirror agreement for the AI partner
                var mirror = new TradeAgreement
                {
                    id = agreement.id,
                    partnerCountry = player?.countryName,
                    resource = agreement.resource,
                    monthlyPaymentUSD = agreement.monthlyPaymentUSD,
                    durationMonths = agreement.durationMonths,
                    status = TradeAgreementStatus.Active,
                    resourceQuantity = agreement.resourceQuantity,
                    isBulkTransfer = agreement.isBulkTransfer,
                    isSeller = !proposal.playerIsSeller
                };
                partner.activeTradeAgreements.Add(mirror);
            }
            
            PendingProposals.Remove(proposalId);
            return agreement;
        }

        public void CancelProposal(string proposalId)
        {
            if (PendingProposals.ContainsKey(proposalId))
            {
                PendingProposals.Remove(proposalId);
            }
        }

        /// <summary>
        /// Proxy War: Secretly transfer sponsorAmount (USD billions) from 'sponsor' to boost
        /// insurgency strength in a random region of 'target'. Leaves no diplomatic fingerprints
        /// unless the operation is discovered.
        /// </summary>
        public void FundProxyInsurgency(CountryDataSO sponsor, CountryDataSO target, float amountBillions)
        {
            if (sponsor == null || target == null || target.regions == null || target.regions.Count == 0) return;
            if (sponsor.treasury < amountBillions) 
            {
                UI.NotificationManager.Instance?.AddNotification("PROXY WAR: Insufficient treasury to fund.", Color.red);
                return;
            }

            sponsor.treasury -= amountBillions;

            // Pick the most volatile region or a random one
            var borderRegions = target.regions.FindAll(r => r.specialization == Data.RegionSpecialization.BorderConflict);
            var targetRegion = borderRegions.Count > 0
                ? borderRegions[UnityEngine.Random.Range(0, borderRegions.Count)]
                : target.regions[UnityEngine.Random.Range(0, target.regions.Count)];

            // Scale boost to amount invested
            float boost = amountBillions * 10f;
            targetRegion.insurgencyLevel = Mathf.Min(100f, targetRegion.insurgencyLevel + boost);
            targetRegion.insurgentSupplyLines = Mathf.Min(100f, targetRegion.insurgentSupplyLines + boost * 0.5f);
            targetRegion.heartsAndMinds = Mathf.Max(0f, targetRegion.heartsAndMinds - boost * 0.3f);

            // Detection check: Based on target country's counter-intelligence
            float detectionChance = (target.counterIntelligence / 100f) * 0.25f;
            if (UnityEngine.Random.value < detectionChance)
            {
                // EXPOSED!
                sponsor.globalPrestige -= 15f;
                sponsor.exposureRisk += 30f;
                target.globalPrestige += 5f;
                NewsSystem.Instance?.PublishNews($"SCANDAL: {target.countryName} intelligence agencies have conclusive evidence that {sponsor.countryName} is funding anti-government militias. International community demands answers.");
                target.activeCrises.Add(new Data.GlobalCrisis { crisisName = $"Proxy War Exposed — {sponsor.countryName}", severity = "Critical", internationalPressure = 80f });
                var gm3 = GameManager.Instance;
                if (gm3?.Relations != null)
                {
                    int month3 = gm3.TurnSystem?.CurrentTurn ?? 0;
                    gm3.Relations.Adjust(sponsor.countryName, target.countryName, -25f,
                        "Caught funding insurgents", month3);
                }
            }
            else
            {
                // Silent success
                UI.NotificationManager.Instance?.AddNotification($"PROXY WAR: ${amountBillions:F1}B routed to destabilize {targetRegion.regionName}, {target.countryName}.", new Color(186/255f, 104/255f, 200/255f));
            }
        }

        public void RespondToUltimatum(CountryDataSO player, CountryDataSO sender, bool comply)
        {
            var gm = GameManager.Instance;
            if (gm?.Relations == null) return;
            var rel = gm.Relations.GetOrCreate(player.countryName, sender.countryName);
            int currentMonth = gm.TurnSystem?.CurrentTurn ?? 0;

            if (!rel.hasUltimatum || rel.ultimatumFromCountry != sender.countryName) return;

            rel.ultimatumResolved = true;
            rel.hasUltimatum      = false;

            if (comply)
            {
                // Player accepts terms — avoids war, pays prestige
                gm.Relations.Adjust(player.countryName, sender.countryName, +15f,
                    "Ultimatum accepted — tensions defused", currentMonth);
                VictorySystem.AwardPrestige(player, -5f, $"Conceded to {sender.countryName} ultimatum");
                UI.NotificationManager.Instance?.AddNotification(
                    $"ULTIMATUM ACCEPTED: Terms accepted. War with {sender.countryName} averted. Prestige −5.", UnityEngine.Color.yellow);
                NewsSystem.Instance?.PublishNews(
                    $"DIPLOMACY: {player.countryName} accepts {sender.countryName}'s demands. Crisis defused.");
            }
            else
            {
                // Player refuses — sender may now declare war
                gm.Relations.Adjust(player.countryName, sender.countryName, -20f,
                    "Ultimatum refused", currentMonth);
                VictorySystem.AwardPrestige(player, +3f, $"Refused to bow to {sender.countryName}");
                UI.NotificationManager.Instance?.AddNotification(
                    $"ULTIMATUM REFUSED: You have rejected {sender.countryName}'s demands. Expect military escalation. Prestige +3.", UnityEngine.Color.red);
                NewsSystem.Instance?.PublishNews(
                    $"CRISIS: {player.countryName} rejects {sender.countryName}'s ultimatum. War preparations underway.");
            }
        }
        public void ResolvePendingTreaties(CountryDataSO country, Core.GameManager gm)
        {
            if (country == null || country.activeTreaties == null) return;
            foreach (var treaty in country.activeTreaties)
            {
                if (treaty.status == "Pending")
                {
                    treaty.turnsUntilResolution--;
                    if (treaty.turnsUntilResolution <= 0)
                    {
                        // Simplistic resolution for now: AI accepts if relations > 30 (would be better in AICountrySystem)
                        var aiCountry = gm.GetCountryByName(treaty.partnerCountry);
                        if (aiCountry != null)
                        {
                            float relations = gm.Relations.GetScore(country.countryName, aiCountry.countryName);
                            if (relations >= 30f)
                            {
                                treaty.status = "Active";
                                UI.NotificationManager.Instance?.AddNotification($"TREATY ACCEPTED: {treaty.partnerCountry} signed the {treaty.treatyName}.", UnityEngine.Color.cyan);
                                // add to AI as well
                                if (aiCountry.activeTreaties == null) aiCountry.activeTreaties = new System.Collections.Generic.List<DiplomaticTreaty>();
                                aiCountry.activeTreaties.Add(new DiplomaticTreaty {
                                    treatyName = treaty.treatyName,
                                    partnerCountry = country.countryName,
                                    status = "Active",
                                    monthsRemaining = treaty.monthsRemaining
                                });
                            }
                            else
                            {
                                treaty.status = "Rejected";
                                UI.NotificationManager.Instance?.AddNotification($"TREATY REJECTED: {treaty.partnerCountry} declined the {treaty.treatyName}.", UnityEngine.Color.red);
                            }
                        }
                    }
                }
            }
        }

        public static bool HasSignedTradeAgreementTreaty(CountryDataSO country1, CountryDataSO country2)
        {
            if (country1 == null || country2 == null || country1.activeTreaties == null) return false;
            foreach (var treaty in country1.activeTreaties)
            {
                if (treaty.treatyName == "Trade Agreement" && treaty.status == "Active" && treaty.partnerCountry == country2.countryName)
                    return true;
            }
            return false;
        }
    }
}