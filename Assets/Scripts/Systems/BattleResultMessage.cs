// ============================================================
// FILE: Scripts/Systems/BattleResultMessage.cs
// PURPOSE: Typed battle result that the inbox renders as a
//          structured card with a losses table — matching the
//          Supremacy 1914 battle report style.
//
// HOW TO USE:
//   Replace the plain InboxSystem.SendMessage() call inside
//   WarSystem.SendMonthlyWarReport() with:
//
//     BattleResultMessage.Send(country, enemy,
//         newKIA, newWounded, (int)equipLost,
//         estEnemyKIA, frontProgress[key]);
//
// ============================================================

using System;
using UnityEngine;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;

namespace StateCraft.Systems
{
    [Serializable]
    public class BattleResultMessage
    {
        // ── Metadata ───────────────────────────────────────────
        public string id;
        public string dateReceived;
        public int    turnReceived;
        public bool   isRead;
        public bool   isBattleResult = true;  // flag for UI to pick the card renderer

        // ── Combatants ─────────────────────────────────────────
        public string ourCountryName;
        public string enemyCountryName;
        public string enemyFlag;          // country flag path or name for UI lookup

        // ── Outcome ────────────────────────────────────────────
        public bool   weWon;
        public string outcomeLabel;       // "Victory", "Defeat", "Stalemate"
        public string frontStatusLabel;   // "Advancing", "Retreating", "Stalemate"
        public float  frontProgress;      // -100 to +100

        // ── Our losses ─────────────────────────────────────────
        public int ourInfantryLost;
        public int ourTanksLost;
        public int ourAPCsLost;
        public int ourFightersLost;
        public int ourShipsLost;
        public int ourKIA;
        public int ourWounded;

        // ── Estimated enemy losses ─────────────────────────────
        public int enemyInfantryLost;
        public int enemyTanksLost;
        public int enemyAPCsLost;
        public int enemyFightersLost;
        public int enemyShipsLost;
        public int enemyKIA;

        // ── Supply state snapshot ──────────────────────────────
        public float ammoRemaining;
        public float fuelRemaining;
        public float warExhaustion;
        public float willToFight;

        // ── Factory ────────────────────────────────────────────
        /// <summary>
        /// Call this from WarSystem.SendMonthlyWarReport() instead of
        /// the plain InboxSystem.SendMessage(). It builds the typed
        /// result and appends it to the country's inbox.
        /// </summary>
        public static void Send(
            CountryDataSO country,
            CountryDataSO enemy,
            int ourKIA,
            int ourWounded,
            int equipmentLost,
            int estimatedEnemyKIA,
            float frontProg,
            bool weWon)
        {
            if (country == null) return;

            var gm   = GameManager.Instance;
            int turn = gm?.TurnSystem?.CurrentTurn ?? 0;
            string date = gm?.TurnSystem?.CurrentDate ?? "Unknown";

            // Rough equipment split — refine if you track per-type losses later
            int tanksLost    = Mathf.RoundToInt(equipmentLost * 0.20f);
            int apcsLost     = Mathf.RoundToInt(equipmentLost * 0.40f);
            int fightersLost = Mathf.RoundToInt(equipmentLost * 0.15f);
            int infantryLost = ourKIA + ourWounded;

            string outcome = weWon      ? "Victory"
                           : frontProg < -10f ? "Defeat"
                           : "Stalemate";

            string frontLabel = frontProg > 15f  ? "Advancing"
                              : frontProg < -15f ? "Retreating"
                              : "Stalemate";

            var msg = new BattleResultMessage
            {
                id               = Guid.NewGuid().ToString(),
                dateReceived     = date,
                turnReceived     = turn,
                isRead           = false,
                isBattleResult   = true,

                ourCountryName   = country.countryName,
                enemyCountryName = enemy?.countryName ?? "Unknown",

                weWon            = weWon,
                outcomeLabel     = outcome,
                frontStatusLabel = frontLabel,
                frontProgress    = frontProg,

                ourInfantryLost  = infantryLost,
                ourTanksLost     = tanksLost,
                ourAPCsLost      = apcsLost,
                ourFightersLost  = fightersLost,
                ourShipsLost     = 0,
                ourKIA           = ourKIA,
                ourWounded       = ourWounded,

                enemyInfantryLost  = Mathf.RoundToInt(estimatedEnemyKIA * 1.3f),
                enemyTanksLost     = Mathf.RoundToInt(tanksLost    * (weWon ? 1.4f : 0.7f)),
                enemyAPCsLost      = Mathf.RoundToInt(apcsLost     * (weWon ? 1.4f : 0.7f)),
                enemyFightersLost  = Mathf.RoundToInt(fightersLost * (weWon ? 1.3f : 0.8f)),
                enemyShipsLost     = 0,
                enemyKIA           = estimatedEnemyKIA,

                ammoRemaining  = country.reserveAmmo,
                fuelRemaining  = country.reserveFuel,
                warExhaustion  = country.warExhaustion,
                willToFight    = country.warWillToFight,
            };

            // Store in the typed list on CountryDataSO (add this field — see note below)
            country.battleResults ??= new System.Collections.Generic.List<BattleResultMessage>();
            country.battleResults.Insert(0, msg);
            if (country.battleResults.Count > 20)
                country.battleResults.RemoveRange(20, country.battleResults.Count - 20);

            // Also send a short plain-text inbox alert so the existing
            // notification badge / critical dispatch still fires correctly.
            string urgency = outcome == "Defeat" ? "Critical"
                           : outcome == "Victory" ? "Important"
                           : "Normal";

            string shortSummary = $"BATTLE REPORT vs {msg.enemyCountryName} — {outcome.ToUpper()}. " +
                                  $"KIA: {ourKIA:N0} | Enemy KIA (est): {estimatedEnemyKIA:N0} | Front: {frontLabel}";

            InboxSystem.Instance?.SendMessage(country,
                "DEFENCE MINISTRY",
                $"Monthly War Report — {outcome} vs {msg.enemyCountryName}",
                shortSummary,
                urgency);

            // Fire on EventBus so news ticker picks it up
            EventBus.Publish(new WorldEvent
            {
                eventType     = weWon ? WorldEventType.RegionCaptured : WorldEventType.FrontlineCollapsed,
                title         = shortSummary,
                severity      = outcome == "Defeat" ? AlertSeverity.Critical : AlertSeverity.Important,
                sourceCountry = country,
                targetCountry = enemy,
                impactValue   = frontProg,
                timestamp     = DateTime.Now
            });
        }
    }
}

// ============================================================
// ADD THIS TO CountryDataSO.cs:
//
//   [Header("=== Battle History ===")]
//   public List<BattleResultMessage> battleResults = new List<BattleResultMessage>();
//
// ============================================================
