using UnityEngine;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;

namespace StateCraft.Systems
{
    public static class GameCommunicator
    {
        private static float _lastGdpWarning     = -1f;
        private static float _lastStabilityWarn  = -1f;

        public static void ProcessTurnBriefing(CountryDataSO country, int currentTurn)
        {
            if (country == null) return;

            CheckEconomy(country, currentTurn);
            CheckStability(country, currentTurn);
            CheckMilitary(country, currentTurn);
            CheckDiplomacy(country, currentTurn);
            CheckPrestige(country, currentTurn);
            CheckPopulation(country, currentTurn);
        }

        static void CheckEconomy(CountryDataSO c, int turn)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.playerBudget < 0.1f && (int)_lastGdpWarning != turn)
            {
                _lastGdpWarning = turn;
                NotificationManager.Instance?.AddNotification(
                    "🔴 TREASURY CRITICAL: Less than $0.1B remaining! Reduce military spending or sign a trade deal immediately.",
                    Color.red);
                InboxSystem.Instance?.SendMessage(c, "FINANCE MINISTRY",
                    "URGENT: Treasury Near Empty",
                    $"Our national treasury has fallen to ${gm.playerBudget:F2}B.\n\n" +
                    $"IMMEDIATE OPTIONS:\n" +
                    $"1. Open ECONOMY → BUDGET and reduce military/admin spending\n" +
                    $"2. Open DIPLOMACY → TRADE AGREEMENTS and sign a resource deal for monthly income\n" +
                    $"3. Open ECONOMY → DEBT and take an emergency loan\n\n" +
                    $"If the treasury hits zero, we cannot pay our troops, fund operations, or service our debt.", "Critical");
            }

            if (c.inflationRate > 25f && turn % 5 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    $"📈 HYPERINFLATION: {c.inflationRate:F0}% inflation is destroying purchasing power. " +
                    $"Raise interest rates or cut spending via Economy → Monetary Policy.",
                    new Color(1f, 0.6f, 0.1f));
            }

            if (c.exchangeRate > 2000f && turn % 4 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    $"💱 CURRENCY CRISIS: Exchange rate at {c.exchangeRate:N0}. " +
                    $"Consider foreign reserves intervention via Economy → Monetary Policy.",
                    new Color(1f, 0.5f, 0f));
            }

            if (c.gdp <= 0.01f && turn == 1)
            {
                NotificationManager.Instance?.AddNotification(
                    "⚠ GDP NOT INITIALIZED: Set starting GDP in Economy → Budget to begin economic simulation.",
                    Color.yellow);
            }

            if (c.income > 0f && turn % 6 == 0)
            {
                string trend = c.income > c.spending ? "SURPLUS" : "DEFICIT";
                Color col = c.income > c.spending ? Color.green : new Color(1f, 0.5f, 0f);
                NotificationManager.Instance?.AddNotification(
                    $"📊 BUDGET {trend}: Income ${c.income:F2}B vs Expenses ${c.spending:F2}B this month.",
                    col);
            }
        }

        static void CheckStability(CountryDataSO c, int turn)
        {
            if (c.stability < 20f && (int)_lastStabilityWarn != turn)
            {
                _lastStabilityWarn = turn;
                NotificationManager.Instance?.AddNotification(
                    $"🚨 STABILITY COLLAPSE IMMINENT: {c.stability:F0}% stability. " +
                    $"Risk of coup or civil war. Deploy security forces or declare emergency measures.",
                    Color.red);
                InboxSystem.Instance?.SendMessage(c, "NATIONAL SECURITY COUNCIL",
                    "⚠ STABILITY CRISIS — IMMEDIATE ACTION REQUIRED",
                    $"National stability has fallen to {c.stability:F0}%.\n\n" +
                    $"AT THIS LEVEL:\n" +
                    $"• Insurgency attacks increase significantly\n" +
                    $"• Foreign investors are pulling out\n" +
                    $"• Military loyalty may waver\n" +
                    $"• Risk of coup attempt increases\n\n" +
                    $"RECOMMENDED ACTIONS:\n" +
                    $"1. MILITARY → OPERATIONS → Launch STABILITY OPERATION in most unstable region\n" +
                    $"2. ECONOMY → BUDGET → Increase social spending\n" +
                    $"3. INTEL → COUNTER-ESPIONAGE → Arrest suspected agitators\n" +
                    $"4. DIPLOMACY → MULTILATERAL → Request international observers to ease tension", "Critical");
            }
            else if (c.stability < 40f && turn % 4 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    $"⚠ LOW STABILITY: {c.stability:F0}% — consider stability operations or social spending increase.",
                    new Color(1f, 0.7f, 0.2f));
            }

            if (c.regions != null)
            {
                foreach (var region in c.regions)
                {
                    if (region.security < 20f && turn % 3 == 0)
                    {
                        NotificationManager.Instance?.AddNotification(
                            $"🔴 REGION CRITICAL: {region.regionName} security at {region.security:F0}%. " +
                            $"Deploy troops via Military → Operations before it falls to insurgents.",
                            Color.red);
                    }
                }
            }
        }

        static void CheckMilitary(CountryDataSO c, int turn)
        {
            if (c.unitInfantry < 1000 && c.unitTanks < 10 && turn % 6 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    "⚠ MILITARY TOO SMALL: Under 1,000 infantry. Vulnerable to invasion and insurgency. " +
                    "Recruit via Military → Recruitment.",
                    new Color(1f, 0.6f, 0.1f));
            }

            if (c.unitFighters < 5 && c.unitBombers < 2 && turn % 8 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    "✈ NO AIR FORCE: Without fighters you cannot intercept enemy bombers or support ground ops. " +
                    "Train jets via Military → Recruitment.",
                    Color.yellow);
            }

            if (c.reserveAmmo < 50f && !IsAtWar(c) && turn % 5 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    $"📦 LOW AMMO RESERVES: {c.reserveAmmo:F0} units. " +
                    $"Restock via Military → Logistics before a conflict starts.",
                    Color.yellow);
            }

            if (c.unitInfantry > 10000 && (c.activeCovertOps == null || c.activeCovertOps.Count == 0) &&
                !IsAtWar(c) && turn % 10 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    $"💡 TIP: You have {c.unitInfantry:N0} troops standing idle. " +
                    $"Deploy them for stability ops, border patrols, or insurgency suppression via Military → Operations.",
                    new Color(0.4f, 0.8f, 1f));
            }
        }

        static void CheckDiplomacy(CountryDataSO c, int turn)
        {
            if (c.allianceCount == 0 && turn == 15)
            {
                NotificationManager.Instance?.AddNotification(
                    "💡 NO ALLIANCES: You have no allied nations. Build relations via Diplomacy → " +
                    "send ambassadors, sign trade deals, then propose an alliance for mutual security.",
                    new Color(0.4f, 0.8f, 1f));
            }

            if ((c.activeTradeAgreements == null || c.activeTradeAgreements.Count == 0) && turn == 6)
            {
                NotificationManager.Instance?.AddNotification(
                    "📦 NO TRADE DEALS: You have no active trade agreements. " +
                    "Open Diplomacy → Trade Agreements to sign resource deals for monthly income.",
                    Color.yellow);
            }

            if (c.sanctionReceivedCount > 0 && turn % 5 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    $"🚫 UNDER SANCTIONS: {c.sanctionReceivedCount} nation(s) have sanctioned you. " +
                    $"GDP growth reduced. Resolve via Diplomacy → improve relations with sanctioning countries.",
                    new Color(1f, 0.5f, 0.5f));
            }

            if (c.globalPrestige < 10f && turn % 4 == 0)
            {
                NotificationManager.Instance?.AddNotification(
                    $"📉 VERY LOW PRESTIGE: {c.globalPrestige:F0}/100. Other nations distrust you. " +
                    $"Send ambassadors, honour treaties, and avoid declaring war to rebuild standing.",
                    new Color(0.8f, 0.5f, 0.5f));
            }
        }

        static void CheckPrestige(CountryDataSO c, int turn)
        {
            if (turn % 12 != 0) return;

            string rank = c.globalPrestige >= 80f ? "REGIONAL POWER" :
                          c.globalPrestige >= 60f ? "RESPECTED NATION" :
                          c.globalPrestige >= 40f ? "EMERGING NATION" :
                          c.globalPrestige >= 20f ? "STRUGGLING STATE" : "PARIAH STATE";

            NotificationManager.Instance?.AddNotification(
                $"🌍 ANNUAL PRESTIGE REPORT: {c.globalPrestige:F0}/100 — {rank}. " +
                $"Alliance count: {c.allianceCount}. Sanctions issued: {c.sanctionIssuedCount}.",
                new Color(0.6f, 0.85f, 1f));

            InboxSystem.Instance?.SendMessage(c, "FOREIGN AFFAIRS MINISTRY",
                $"Annual Standing Report — {rank}",
                $"INTERNATIONAL STANDING REVIEW:\n\n" +
                $"• Global Prestige: {c.globalPrestige:F0}/100 ({rank})\n" +
                $"• Active Alliances: {c.allianceCount}\n" +
                $"• Trade Agreements: {c.activeTradeAgreements?.Count ?? 0}\n" +
                $"• Sanctions Issued: {c.sanctionIssuedCount}\n" +
                $"• Sanctions Received: {c.sanctionReceivedCount}\n" +
                $"• Active Treaties: {c.activeTreaties?.Count ?? 0}\n\n" +
                $"RECOMMENDED FOCUS FOR NEXT YEAR:\n" +
                (c.globalPrestige < 40f ? "• Prioritise diplomacy — send ambassadors, sign trade deals, avoid conflicts\n" : "") +
                (c.allianceCount == 0   ? "• Seek at least one formal alliance for security\n" : "") +
                (c.activeTradeAgreements?.Count == 0 ? "• No trade income — open Trade Agreements tab to fix this\n" : ""),
                "Standard");
        }

        static void CheckPopulation(CountryDataSO c, int turn)
        {
            if (turn % 6 != 0) return;

            if (c.population > 0)
            {
                long pop = (long)c.population;
                NotificationManager.Instance?.AddNotification(
                    $"👥 POPULATION: {pop:N0} citizens. " +
                    (c.stability > 60f ? "Stable growth conditions." : "Instability is suppressing growth."),
                    new Color(0.6f, 0.85f, 1f));
            }
        }

        static bool IsAtWar(CountryDataSO c)
        {
            var warSys = GameManager.Instance?.WarSystem;
            if (warSys == null) return false;
            foreach (var other in GameManager.Instance.allCountries)
            {
                if (other == c) continue;
                if (warSys.GetWarStage(other) == Interfaces.WarStage.Conflict)
                    return true;
            }
            return false;
        }
    }
}