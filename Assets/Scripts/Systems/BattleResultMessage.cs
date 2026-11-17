
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
        public string id;
        public string dateReceived;
        public int    turnReceived;
        public bool   isRead;
        public bool   isBattleResult = true;

        public string ourCountryName;
        public string enemyCountryName;
        public string enemyFlag;

        public bool   weWon;
        public string outcomeLabel;
        public string frontStatusLabel;
        public float  frontProgress;

        public int ourInfantryLost;
        public int ourTanksLost;
        public int ourAPCsLost;
        public int ourFightersLost;
        public int ourShipsLost;
        public int ourKIA;
        public int ourWounded;

        public int enemyInfantryLost;
        public int enemyTanksLost;
        public int enemyAPCsLost;
        public int enemyFightersLost;
        public int enemyShipsLost;
        public int enemyKIA;

        public float ammoRemaining;
        public float fuelRemaining;
        public float warExhaustion;
        public float willToFight;

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

            country.battleResults ??= new System.Collections.Generic.List<BattleResultMessage>();
            country.battleResults.Insert(0, msg);
            if (country.battleResults.Count > 20)
                country.battleResults.RemoveRange(20, country.battleResults.Count - 20);

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

