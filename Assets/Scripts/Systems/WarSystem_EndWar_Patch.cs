// ============================================================
// FILE: Scripts/Systems/WarSystem_EndWar_Patch.cs
// PURPOSE: Drop-in replacement for WarSystem.EndWar().
//          Copy this method body into WarSystem.cs, replacing
//          the existing private void EndWar() method.
//
//          Adds:
//            • Victor gains a resource bonus from the loser
//            • Loser cedes their weakest region (border change)
//            • Peace terms message includes the territorial outcome
//            • Prestige awarded via VictorySystem
// ============================================================

// ─── PASTE THIS INTO WarSystem.cs, replacing EndWar() ───────
/*
private void EndWar(CountryDataSO a, CountryDataSO b, string outcome)
{
    string key = WarKey(a.countryName, b.countryName);
    activeWars.Remove(key);
    warDuration.Remove(key);
    frontProgress.Remove(key);
    lastPhaseMonth.Remove(key);
    momentumVelocity.Remove(key);
    surpriseEventCooldown.Remove(key);

    countryWarStages[a] = WarStage.Peace;
    countryWarStages[b] = WarStage.Peace;

    a.warTurnsActive = 0; a.warExhaustion = 0f; a.warEnemyName = "";
    b.warTurnsActive = 0; b.warExhaustion = 0f; b.warEnemyName = "";
    _lastWarMonthCounted.Remove(a);
    _lastWarMonthCounted.Remove(b);

    a.globalPrestige = Mathf.Max(0f, a.globalPrestige - 5f);
    b.globalPrestige = Mathf.Max(0f, b.globalPrestige - 5f);

    var gm = GameManager.Instance;
    if (gm != null) gm.worldTension = Mathf.Max(0f, gm.worldTension - 15f);

    // ── TERRITORIAL CONSEQUENCES ─────────────────────────
    CountryDataSO winner = null, loser = null;
    bool isDecisiveVictory = outcome.Contains("Victory");

    if (isDecisiveVictory)
    {
        // Determine winner and loser from outcome string
        if (outcome.Contains(a.countryName) && outcome.Contains("Victory"))
        { winner = a; loser = b; }
        else if (outcome.Contains(b.countryName) && outcome.Contains("Victory"))
        { winner = b; loser = a; }
    }

    string territorialReport = "";

    if (winner != null && loser != null)
    {
        // ─ 1. Resource Reparations ────────────────────────
        // Loser surrenders a slice of their most abundant resource
        string capturedResource = "none";
        float  capturedAmount   = 0f;

        if (loser.resources != null && loser.resources.Count > 0)
        {
            // Find loser's richest resource
            ResourceDeposit richest = null;
            foreach (var res in loser.resources)
            {
                if (richest == null || res.abundance > richest.abundance)
                    richest = res;
            }

            if (richest != null && richest.abundance > 10f)
            {
                float transfer = richest.abundance * 0.25f; // 25% of their richest deposit
                richest.abundance = Mathf.Max(0f, richest.abundance - transfer);

                var winnerRes = winner.resources?.Find(r => r.type == richest.type);
                if (winnerRes != null)
                    winnerRes.abundance += transfer;
                else if (winner.resources != null)
                    winner.resources.Add(new ResourceDeposit { type = richest.type, abundance = transfer });

                capturedResource = richest.type.ToString();
                capturedAmount   = transfer;
                territorialReport += $"\n• Resource reparations: {winner.countryName} gains {transfer:F0}% {capturedResource} abundance from {loser.countryName}";
            }
        }

        // ─ 2. Border Change: Loser cedes weakest region ──
        string cededRegionName = "none";

        if (loser.regions != null && loser.regions.Count > 1) // must keep at least one region
        {
            // Find loser's most unstable / lowest-stability region (most plausible to cede)
            RegionDataSO cededRegion = null;
            float lowestStab = 101f;
            foreach (var reg in loser.regions)
            {
                if (reg.stability < lowestStab)
                { lowestStab = reg.stability; cededRegion = reg; }
            }

            if (cededRegion != null)
            {
                loser.regions.Remove(cededRegion);
                winner.regions.Add(cededRegion);

                // Ceded region starts at reduced stability/security under new administration
                cededRegion.stability = Mathf.Max(20f, cededRegion.stability * 0.6f);
                cededRegion.security  = Mathf.Max(10f, cededRegion.security  * 0.5f);
                cededRegion.insurgencyLevel = Mathf.Min(100f, cededRegion.insurgencyLevel + 20f);

                cededRegionName = cededRegion.regionName;
                territorialReport += $"\n• Border change: {loser.countryName} cedes {cededRegionName} to {winner.countryName}";

                // Winner receives a stability hit from administering restive territory
                foreach (var existing in winner.regions)
                    existing.stability = Mathf.Max(0f, existing.stability - 1f);
            }
        }

        // ─ 3. Prestige awards ──────────────────────────────
        VictorySystem.AwardPrestige(winner, +15f, $"Won war against {loser.countryName}");
        VictorySystem.AwardPrestige(loser,  -10f, $"Defeated by {winner.countryName}");

        // ─ 4. Relations update ────────────────────────────
        if (gm?.Relations != null)
        {
            int currentMonth = gm.TurnSystem?.CurrentTurn ?? 0;
            gm.Relations.Adjust(winner.countryName, loser.countryName, -20f,
                $"{winner.countryName} won decisive war", currentMonth);
        }

        NewsSystem.Instance?.PublishNews(
            $"PEACE TERMS: {loser.countryName} cedes {cededRegionName} and resource rights to {winner.countryName}. " +
            $"International community acknowledges new borders.");

        NewsSystem.Instance?.PublishNews(
            $"TERRITORY: {winner.countryName} formally annexes {cededRegionName}. " +
            $"Ceasefire monitors deployed to new border.");
    }
    else
    {
        // Negotiated ceasefire — minor prestige hit both sides, no territory changes
        VictorySystem.AwardPrestige(a, -3f, "Ceasefire negotiated");
        VictorySystem.AwardPrestige(b, -3f, "Ceasefire negotiated");
    }

    NewsSystem.Instance?.PublishNews(
        $"WAR ENDS: {outcome}. {a.countryName} and {b.countryName} observe ceasefire.");

    bool playerInvolved = gm?.playerCountry == a || gm?.playerCountry == b;
    if (playerInvolved)
    {
        var player  = gm.playerCountry;
        var enemy   = (player == a) ? b : a;
        bool playerWon = (winner == player);

        string playerOutcomeHeader = playerWon
            ? "🏆 WAR CONCLUDED — VICTORY"
            : (winner == null ? "🕊 WAR CONCLUDED — CEASEFIRE" : "💀 WAR CONCLUDED — DEFEAT");

        string territorialSummary = isDecisiveVictory && winner != null && loser != null
            ? $"\n\nTERRITORIAL CHANGES:{territorialReport}"
            : "\n\nNo territorial changes — status quo ante bellum.";

        InboxSystem.Instance?.SendMessage(player,
            "JOINT CHIEFS OF STAFF",
            playerOutcomeHeader,
            $"FINAL WAR SUMMARY\n\n" +
            $"Outcome: {outcome}\n" +
            $"Duration: {player.warTurnsActive} months\n" +
            $"Total KIA: {player.totalKIA:N0}\n" +
            $"Total Wounded: {player.totalWounded:N0}" +
            territorialSummary +
            $"\n\nThe ceasefire is now in effect. Diplomatic relations may be re-established " +
            $"via the Diplomacy tab after a cooling-off period.",
            "Critical");

        NotificationManager.Instance?.AddNotification(
            $"{(playerWon ? "🏆" : "🕊")} WAR ENDED: {outcome}." +
            (isDecisiveVictory && winner != null ? $" Territorial changes applied." : ""),
            playerWon ? Color.green : Color.yellow);
    }
}
*/
// ─── END OF PASTE BLOCK ──────────────────────────────────────

// This file is a documentation/patch file; no compiled code here.
// The actual replacement lives inside WarSystem.cs after you paste
// the method above in place of the old EndWar().
