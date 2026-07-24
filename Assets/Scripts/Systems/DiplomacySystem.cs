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

        // ─── Treaty Duration table ─────────────────────────────────────────────
        private static int GetTreatyDuration(string treatyName)
        {
            switch (treatyName)
            {
                case "Non-Aggression Pact":     return 36;
                case "Mutual Defence Pact":     return 48;
                case "Trade Agreement":         return 24;
                case "Coalition Force Pact":    return 36;
                case "Ceasefire Agreement":     return 12;
                case "Military Access Rights":  return 18;
                default:                        return 24;
            }
        }

        // ─── AI Acceptance Evaluation ─────────────────────────────────────────
        /// <summary>
        /// The AI evaluates the treaty proposal from <paramref name="player"/> and either
          public bool ProposeTreaty(CountryDataSO player, CountryDataSO aiPartner, string treatyName)
        {
            var gm = GameManager.Instance;
            if (gm == null || player == null || aiPartner == null) return false;

            int currentTurn = gm.TurnSystem?.CurrentTurn ?? 0;
            float relations  = gm.Relations?.GetScore(player.countryName, aiPartner.countryName) ?? 0f;

            // Existing war: ceasefire may be accepted; offensive treaties blocked
            bool atWar = gm.WarSystem?.GetWarStage(player) == WarStage.Conflict ||
                         gm.WarSystem?.GetWarStage(aiPartner) == WarStage.Conflict;
            if (atWar && treatyName != "Ceasefire Agreement")
            {
                UI.NotificationManager.Instance?.AddNotification(
                    $"TREATY REJECTED: Cannot sign a {treatyName} while at war. Negotiate a Ceasefire first.",
                    Color.red);
                return false;
            }

            // Don't allow duplicate active or pending treaties of the same type
            bool alreadyExists = player.activeTreaties != null &&
                player.activeTreaties.Exists(t => t.treatyName == treatyName &&
                                                  t.partnerCountry == aiPartner.countryName &&
                                                  (t.status == "Active" || t.status == "Pending"));
            if (alreadyExists)
            {
                UI.NotificationManager.Instance?.AddNotification(
                    $"TREATY DUPLICATE: A {treatyName} with {aiPartner.countryName} is already active or pending.",
                    Color.yellow);
                return false;
            }

            if (player.activeTreaties == null)    player.activeTreaties    = new System.Collections.Generic.List<DiplomaticTreaty>();
            if (aiPartner.activeTreaties == null) aiPartner.activeTreaties = new System.Collections.Generic.List<DiplomaticTreaty>();

            // ── ADD PENDING TREATY to player's list. AI will resolve in 2-3 turns. ──
            int waitTurns = UnityEngine.Random.Range(1, 3);
            var pendingTreaty = new DiplomaticTreaty
            {
                treatyName            = treatyName,
                partnerCountry        = aiPartner.countryName,
                terms                 = $"Proposed turn {currentTurn}. Awaiting response from {aiPartner.countryName}.",
                monthsRemaining       = 0,
                turnSigned            = currentTurn,
                status                = "Pending",
                turnsUntilResolution  = waitTurns,
                tradeIncomeBonus      = 0f,
                blocksWar             = false,
            };
            player.activeTreaties.Add(pendingTreaty);

            UI.NotificationManager.Instance?.AddNotification(
                $"📨 TREATY PROPOSED: {treatyName} sent to {aiPartner.countryName}. Awaiting response...",
                new Color(1f, 0.8f, 0.2f));

            InboxSystem.Instance?.SendMessage(player,
                $"FOREIGN MINISTRY — {aiPartner.countryName.ToUpper()}",
                $"Treaty Proposal Submitted — {treatyName}",
                $"Your proposal for a {treatyName} has been formally submitted to the Government of {aiPartner.countryName}.\n\n" +
                $"Current bilateral relations: {relations:F0}.\n" +
                $"The response is expected within {waitTurns}-{waitTurns + 1} months. You can track the status in the TREATIES tab.",
                "Important");

            NewsSystem.Instance?.PublishNews(
                $"DIPLOMACY: {player.countryName} proposes a {treatyName} to {aiPartner.countryName}. Outcome pending.");

            return true;
        }

        // ─── Resolve Pending Treaties (called each turn) ───────────────────────
        /// <summary>
        /// Iterates over all pending treaties for the player and resolves them when
        /// turnsUntilResolution reaches zero, applying AI acceptance/rejection logic.
        /// </summary>
        public void ResolvePendingTreaties(CountryDataSO player, GameManager gm)
        {
            if (player?.activeTreaties == null || gm == null) return;

            int currentTurn = gm.TurnSystem?.CurrentTurn ?? 0;

            for (int i = player.activeTreaties.Count - 1; i >= 0; i--)
            {
                var t = player.activeTreaties[i];
                if (t.status != "Pending") continue;

                t.turnsUntilResolution--;
                if (t.turnsUntilResolution > 0) continue;

                // Time to resolve — find the AI partner
                var aiPartner = gm.allCountries?.Find(c => c.countryName == t.partnerCountry);
                if (aiPartner == null) { t.status = "Rejected"; t.rejectionReason = "Country not found."; continue; }

                float relations = gm.Relations?.GetScore(player.countryName, aiPartner.countryName) ?? 0f;

                float acceptanceThreshold = t.treatyName switch
                {
                    "Non-Aggression Pact"    => -20f,
                    "Mutual Defence Pact"    =>  20f,
                    "Trade Agreement"        => -10f,
                    "Coalition Force Pact"   =>  30f,
                    "Ceasefire Agreement"    => -60f,
                    "Military Access Rights" =>  10f,
                    _                        =>   0f,
                };

                bool accepted = relations >= acceptanceThreshold;
                float roll = UnityEngine.Random.value;
                if (!accepted && relations >= acceptanceThreshold - 15f && roll < 0.30f) accepted = true;
                if ( accepted && relations <  acceptanceThreshold + 5f  && roll < 0.20f) accepted = false;

                if (!accepted)
                {
                    // ── REJECTION: keep the treaty in history as Rejected ──
                    string rejReason = relations < acceptanceThreshold - 30f
                        ? "Bilateral relations are too strained for an agreement at this time."
                        : relations < acceptanceThreshold
                        ? "Conditions are not yet favourable. Improve relations and try again."
                        : "The proposal was declined due to domestic political pressures.";

                    t.status = "Rejected";
                    t.rejectionReason = rejReason;
                    t.terms = $"Proposed turn {t.turnSigned}. Rejected by {t.partnerCountry}.";

                    gm.Relations?.Adjust(player.countryName, aiPartner.countryName, -5f,
                        $"Treaty proposal rejected ({t.treatyName})", currentTurn);

                    InboxSystem.Instance?.SendMessage(player,
                        $"EMBASSY OF {aiPartner.countryName.ToUpper()}",
                        $"Treaty Proposal Declined — {t.treatyName}",
                        $"The Government of {aiPartner.countryName} regrets to inform you that, " +
                        $"at this time, conditions are not suitable for formalising a {t.treatyName}.\n\n" +
                        $"Reason: {rejReason}\n" +
                        $"Current bilateral relations score: {relations:F0}.\n" +
                        $"We advise working to improve diplomatic ties before re-submitting.",
                        "Important");

                    UI.NotificationManager.Instance?.AddNotification(
                        $"❌ TREATY REJECTED: {aiPartner.countryName} declined the {t.treatyName}. Relations −5.",
                        new Color(1f, 0.3f, 0.3f));

                    NewsSystem.Instance?.PublishNews(
                        $"DIPLOMACY: {aiPartner.countryName} rejects {player.countryName}'s proposed {t.treatyName}.");
                }
                else
                {
                    // ── ACCEPTANCE: convert to Active and apply all effects ──
                    int duration     = GetTreatyDuration(t.treatyName);
                    float tradeBonus = BuildTreatytTradeBonus(player, aiPartner, t.treatyName);
                    bool  blocksWar  = t.treatyName == "Non-Aggression Pact" || t.treatyName == "Mutual Defence Pact";

                    t.status           = "Active";
                    t.monthsRemaining  = duration;
                    t.tradeIncomeBonus = tradeBonus;
                    t.blocksWar        = blocksWar;
                    t.terms            = $"Signed turn {currentTurn}. Duration: {duration} months.";
                    t.rejectionReason  = null;

                    // Add treaty to the partner side
                    if (aiPartner.activeTreaties == null) aiPartner.activeTreaties = new System.Collections.Generic.List<DiplomaticTreaty>();
                    bool partnerAlreadyHas = aiPartner.activeTreaties.Exists(pt =>
                        pt.treatyName == t.treatyName && pt.partnerCountry == player.countryName && pt.status == "Active");
                    if (!partnerAlreadyHas)
                    {
                        aiPartner.activeTreaties.Add(new DiplomaticTreaty
                        {
                            treatyName       = t.treatyName,
                            partnerCountry   = player.countryName,
                            terms            = t.terms,
                            monthsRemaining  = duration,
                            turnSigned       = currentTurn,
                            status           = "Active",
                            tradeIncomeBonus = tradeBonus,
                            blocksWar        = blocksWar,
                        });
                    }

                    ApplyTreatyEffects(player, aiPartner, t.treatyName, gm, currentTurn, tradeBonus);

                    float relationsBump = t.treatyName switch
                    {
                        "Non-Aggression Pact"    => 20f,
                        "Mutual Defence Pact"    => 30f,
                        "Trade Agreement"        => 15f,
                        "Coalition Force Pact"   => 25f,
                        "Ceasefire Agreement"    => 10f,
                        "Military Access Rights" => 10f,
                        _                        => 10f,
                    };
                    gm.Relations?.Adjust(player.countryName, aiPartner.countryName, relationsBump,
                        $"{t.treatyName} signed", currentTurn);

                    InboxSystem.Instance?.SendMessage(player,
                        $"EMBASSY OF {aiPartner.countryName.ToUpper()}",
                        $"Treaty Signed — {t.treatyName}",
                        $"The Government of {aiPartner.countryName} is pleased to confirm the signing of " +
                        $"the {t.treatyName}. This agreement will remain in effect for {duration} months. " +
                        BuildTreatyEffectSummary(t.treatyName, tradeBonus, player.currencySymbol),
                        "Important");

                    UI.NotificationManager.Instance?.AddNotification(
                        $"✅ TREATY SIGNED: {t.treatyName} with {aiPartner.countryName} for {duration} months. Relations +{relationsBump:F0}.",
                        new Color(0f, 0.85f, 0.4f));

                    NewsSystem.Instance?.PublishNews(
                        $"DIPLOMACY: {player.countryName} and {aiPartner.countryName} sign {t.treatyName}.");
                }
            }
        }

        // ─── Treaty Effects ──────────────────────────────────────────────────
        private float BuildTreatytTradeBonus(CountryDataSO a, CountryDataSO b, string treatyName)
        {
            return treatyName switch
            {
                "Trade Agreement"     => Mathf.Min(a.income, b.income) * 0.02f, // 2% of smaller GDP
                "Coalition Force Pact"=> Mathf.Min(a.income, b.income) * 0.01f,
                _                     => 0f,
            };
        }

        private string BuildTreatyEffectSummary(string treatyName, float tradeBonus, string sym)
        {
            switch (treatyName)
            {
                case "Non-Aggression Pact":
                    return "Effect: Neither signatory may declare war on the other while the pact is active. " +
                           "Any violation immediately incurs a severe prestige penalty and a -30 relations adjustment.";
                case "Mutual Defence Pact":
                    return "Effect: If either signatory is attacked, the other is treaty-bound to join the war " +
                           "and contribute coalition forces automatically.";
                case "Trade Agreement":
                    return $"Effect: Both economies benefit from regularised trade. " +
                           $"Income bonus: +{sym}{tradeBonus:F2}B per turn each.";
                case "Coalition Force Pact":
                    return $"Effect: Shared security arrangement. Coalition forces pooled. " +
                           $"Income bonus: +{sym}{tradeBonus:F2}B per turn.";
                case "Ceasefire Agreement":
                    return "Effect: Active hostilities are suspended. Both sides return to War Stage: Tension. " +
                           "The ceasefire may be broken if diplomatic relations deteriorate below -40.";
                case "Military Access Rights":
                    return "Effect: Your armed forces may operate from the partner's bases. " +
                           "Air assets gain +15% effective range in operations targeting the partner's region.";
                default:
                    return "";
            }
        }

        private void ApplyTreatyEffects(CountryDataSO player, CountryDataSO partner,
                                        string treatyName, GameManager gm, int turn, float tradeBonus)
        {
            switch (treatyName)
            {
                case "Ceasefire Agreement":
                    // End active combat — reset both sides to Tension
                    if (gm.WarSystem != null)
                    {
                        gm.WarSystem.SetWarStage(player,  WarStage.Tension);
                        gm.WarSystem.SetWarStage(partner, WarStage.Tension);
                    }
                    break;

                case "Coalition Force Pact":
                    // Transfer 5% of partner's infantry and 4% of tanks as coalition contribution
                    int coInfantry = Mathf.RoundToInt(partner.unitInfantry * 0.05f);
                    int coTanks    = Mathf.RoundToInt(partner.unitTanks    * 0.04f);
                    player.unitInfantry  += coInfantry;
                    player.unitTanks     += coTanks;
                    partner.unitInfantry  = Mathf.Max(0, partner.unitInfantry - coInfantry);
                    partner.unitTanks     = Mathf.Max(0, partner.unitTanks    - coTanks);
                    UI.NotificationManager.Instance?.AddNotification(
                        $"🪖 COALITION ACTIVATED: {partner.countryName} contributes {coInfantry:N0} troops and {coTanks} tanks to your coalition.",
                        new Color(0f, 0.85f, 1f));
                    break;

                case "Trade Agreement":
                case "Non-Aggression Pact":
                case "Mutual Defence Pact":
                case "Military Access Rights":
                    // Per-turn effects are handled by ProcessTradeDeals via tradeIncomeBonus on the treaty object
                    break;
            }

            // Prestige gain for signing major treaties
            float prestigeGain = treatyName == "Mutual Defence Pact" ? 5f :
                                 treatyName == "Non-Aggression Pact" ? 3f : 1f;
            player.globalPrestige  = Mathf.Min(100f, player.globalPrestige  + prestigeGain);
            partner.globalPrestige = Mathf.Min(100f, partner.globalPrestige + prestigeGain);
        }

        // Called from ProcessTradeDeals when a treaty expires
        public void RevokeTreatyEffects(CountryDataSO country, DiplomaticTreaty treaty)
        {
            // Coalition force return is handled in ProcessTradeDeals — no extra logic needed here.
            // Prestige hit for letting treaty lapse
            if (treaty.treatyName == "Mutual Defence Pact" || treaty.treatyName == "Non-Aggression Pact")
                country.globalPrestige = Mathf.Max(0f, country.globalPrestige - 2f);
        }

        /// <summary>
        /// Returns true if both countries have a signed "Trade Agreement" treaty.
        /// This gate must pass before any resource trade can be conducted.
        /// </summary>
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
            // ── GATE: Trade Agreement treaty must be signed first ──────────
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

            // ── Apply per-turn income bonuses from active treaties ──
            if (country.activeTreaties != null)
            {
                foreach (var t in country.activeTreaties)
                {
                    if (t.status == "Active" && t.tradeIncomeBonus > 0f)
                        country.income += t.tradeIncomeBonus;
                }
            }

            // ── Treaty expiry with proper coalition cleanup ──
            if (country.activeTreaties != null)
            {
                for (int i = country.activeTreaties.Count - 1; i >= 0; i--)
                {
                    var treaty = country.activeTreaties[i];
                    treaty.monthsRemaining--;

                    // Warn 3 months before expiry
                    if (treaty.monthsRemaining == 3 && country == GameManager.Instance?.playerCountry)
                    {
                        UI.NotificationManager.Instance?.AddNotification(
                            $"⏰ TREATY EXPIRING: {treaty.treatyName} with {treaty.partnerCountry} expires in 3 months. " +
                            $"Renew via Diplomacy tab to maintain the alliance.", UnityEngine.Color.yellow);
                    }

                    if (treaty.monthsRemaining <= 0)
                    {
                        // Coalition pact: return contributed troops
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

                        treaty.status = "Expired";
                        RevokeTreatyEffects(country, treaty);
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
            // ── GATE ──────────────────────────────────────────────────────
            if (!HasSignedTradeAgreementTreaty(player, ai))
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
    }
}