using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using UnityEngine;
using System.Collections.Generic;

namespace StateCraft.Systems
{
    public class DiplomacySystem : IDiplomacySystem
    {
        public List<TradeDeal> ActiveDeals = new List<TradeDeal>();
        public Dictionary<string, TradeAgreementProposal> PendingProposals { get; private set; } = new Dictionary<string, TradeAgreementProposal>();

        public void UpdateRelations(CountryDataSO country)
        {
        }

        public static bool HasSignedTradeAgreementTreaty(CountryDataSO a, CountryDataSO b)
        {
            if (a == null || b == null) return false;
            bool aHas = a.activeTreaties != null && a.activeTreaties.Exists(t =>
                t.treatyName == "Trade Agreement" && t.partnerCountry == b.countryName);
            bool bHas = b.activeTreaties != null && b.activeTreaties.Exists(t =>
                t.treatyName == "Trade Agreement" && t.partnerCountry == a.countryName);
            return aHas && bHas;
        }

                public void SignTradeDeal(CountryDataSO seller, CountryDataSO buyer, TradeResourceType resource, float paymentUSD)
        {
            if (!HasSignedTradeAgreementTreaty(seller, buyer))
            {
                bool isPlayer = seller == GameManager.Instance?.playerCountry
                             || buyer  == GameManager.Instance?.playerCountry;
                if (isPlayer)
                {
                    string other = seller == GameManager.Instance?.playerCountry
                        ? buyer.countryName : seller.countryName;
                    UI.NotificationManager.Instance?.AddNotification(
                        $"TRADE BLOCKED: Sign a Trade Agreement with {other} first. " +
                        "Go to Diplomacy → select the country → Trade Agreement.",
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
                durationTurns = 24
            };

            ActiveDeals.Add(deal);
            
            var gm2 = GameManager.Instance;
            if (gm2?.Relations != null)
            {
                int month2 = gm2.TurnSystem?.CurrentTurn ?? 0;
                gm2.Relations.Adjust(seller.countryName, buyer.countryName, +10f,
                    "Trade agreement signed", month2);
            }
            
            NewsSystem.Instance?.PublishNews($"TRADE DEAL: {seller.countryName} to supply {resource} to {buyer.countryName} for ${paymentUSD/1e6f:N1}M monthly.");
            
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

            if (country.activeTreaties != null)
            {
                for (int i = country.activeTreaties.Count - 1; i >= 0; i--)
                {
                    var treaty = country.activeTreaties[i];
                    treaty.monthsRemaining--;

                    if (treaty.monthsRemaining == 3 && country == GameManager.Instance?.playerCountry)
                    {
                        UI.NotificationManager.Instance?.AddNotification(
                            $"⏰ TREATY EXPIRING: {treaty.treatyName} with {treaty.partnerCountry} expires in 3 months. " +
                            $"Renew via Diplomacy tab to maintain the alliance.", UnityEngine.Color.yellow);
                    }

                    if (treaty.monthsRemaining <= 0)
                    {
                        if (treaty.treatyName == "Coalition Force Pact")
                        {
                            var partner = GameManager.Instance?.allCountries?.Find(c => c.countryName == treaty.partnerCountry);
                            if (partner != null)
                            {
                                int returnInfantry = UnityEngine.Mathf.RoundToInt(country.unitInfantry * 0.05f);
                                int returnTanks    = UnityEngine.Mathf.RoundToInt(country.unitTanks    * 0.04f);
                                country.unitInfantry  = UnityEngine.Mathf.Max(0, country.unitInfantry - returnInfantry);
                                country.unitTanks     = UnityEngine.Mathf.Max(0, country.unitTanks    - returnTanks);
                                partner.unitInfantry += returnInfantry;
                                partner.unitTanks    += returnTanks;

                                if (country == GameManager.Instance?.playerCountry)
                                {
                                    UI.NotificationManager.Instance?.AddNotification(
                                        $"📋 COALITION EXPIRED: {treaty.partnerCountry} withdraws their forces " +
                                        $"({returnInfantry:N0} troops, {returnTanks} tanks). Renew via Diplomacy to maintain joint security.",
                                        new UnityEngine.Color(0.6f, 0.6f, 0.7f));

                                    InboxSystem.Instance?.SendMessage(country,
                                        $"{treaty.partnerCountry} HIGH COMMAND",
                                        "Coalition Force Pact — Concluded",
                                        $"Our 36-month Coalition Force Pact has expired.\n\n" +
                                        $"Forces returned to {treaty.partnerCountry}:\n" +
                                        $"• {returnInfantry:N0} infantry recalled\n" +
                                        $"• {returnTanks} tanks withdrawn\n\n" +
                                        $"Renew via Diplomacy → select {treaty.partnerCountry} → Coalition Pact.", "Standard");
                                }
                            }
                        }

                        country.activeTreaties.RemoveAt(i);
                        NewsSystem.Instance?.PublishNews(
                            $"TREATY EXPIRED: {treaty.treatyName} between {country.countryName} and {treaty.partnerCountry} has concluded.");
                    }
                }
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
            if (!HasSignedTradeAgreementTreaty(player, ai))
                return null;

            var proposal = new TradeAgreementProposal
            {
                partnerCountry = ai.countryName,
                resource = resource,
                baseMonthlyPaymentUSD = basePayment,
                baseDurationMonths = duration,
                playerIsSeller = playerIsSeller
            };
            
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

        public void FundProxyInsurgency(CountryDataSO sponsor, CountryDataSO target, float amountBillions)
        {
            if (sponsor == null || target == null || target.regions == null || target.regions.Count == 0) return;
            if (sponsor.treasury < amountBillions) 
            {
                UI.NotificationManager.Instance?.AddNotification("PROXY WAR: Insufficient treasury to fund.", Color.red);
                return;
            }

            sponsor.treasury -= amountBillions;

            var borderRegions = target.regions.FindAll(r => r.specialization == Data.RegionSpecialization.BorderConflict);
            var targetRegion = borderRegions.Count > 0
                ? borderRegions[UnityEngine.Random.Range(0, borderRegions.Count)]
                : target.regions[UnityEngine.Random.Range(0, target.regions.Count)];

            float boost = amountBillions * 10f;
            targetRegion.insurgencyLevel = Mathf.Min(100f, targetRegion.insurgencyLevel + boost);
            targetRegion.insurgentSupplyLines = Mathf.Min(100f, targetRegion.insurgentSupplyLines + boost * 0.5f);
            targetRegion.heartsAndMinds = Mathf.Max(0f, targetRegion.heartsAndMinds - boost * 0.3f);

            float detectionChance = (target.counterIntelligence / 100f) * 0.25f;
            if (UnityEngine.Random.value < detectionChance)
            {
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
                gm.Relations.Adjust(player.countryName, sender.countryName, -20f,
                    "Ultimatum refused", currentMonth);
                VictorySystem.AwardPrestige(player, +3f, $"Refused to bow to {sender.countryName}");
                UI.NotificationManager.Instance?.AddNotification(
                    $"ULTIMATUM REFUSED: You have rejected {sender.countryName}'s demands. Expect military escalation. Prestige +3.", UnityEngine.Color.red);
                NewsSystem.Instance?.PublishNews(
                    $"CRISIS: {player.countryName} rejects {sender.countryName}'s ultimatum. War preparations underway.");
            }
        }
    }
}