
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using StateCraft.Systems;
using UnityEngine;
using System.Collections.Generic;

namespace StateCraft.Systems
{
    public class VictorySystem
    {
        private const float PRESTIGE_WIN_TARGET    = 100f;
        private const int   PRESTIGE_LEAD_MONTHS   = 6;
        private const int   DOMINATED_ALLY_COUNT   = 4;
        private const int   COLLAPSE_MONTHS        = 3;

        private int   _prestigeLeadMonths      = 0;
        private int   _collapseMonths          = 0;
        private bool  _victoryAnnounced        = false;
        private bool  _defeatAnnounced         = false;

        public float  CurrentPrestige   { get; private set; }
        public int    AllyCount         { get; private set; }
        public string VictoryStatus     { get; private set; } = "In Progress";
        public float  ProgressToWin     { get; private set; }

        public void CheckVictory(CountryDataSO player,
                                 List<CountryDataSO> allCountries,
                                 int currentMonth)
        {
            if (_victoryAnnounced || _defeatAnnounced) return;

            var gm = GameManager.Instance;

            if (player.stability <= 3f)
            {
                _collapseMonths++;
                if (_collapseMonths >= COLLAPSE_MONTHS)
                {
                    AnnounceDefeat(player, gm,
                        "Your nation has collapsed. Stability fell to zero for three consecutive months. " +
                        "Mass civil unrest, institutional breakdown, and economic ruin have ended your government.");
                    return;
                }
            }
            else
            {
                _collapseMonths = 0;
            }

            if (player.treasury < -50f && player.creditRating == "D")
            {
                AnnounceDefeat(player, gm,
                    "Sovereign default and credit collapse have paralysed the state. " +
                    "International creditors have seized assets. Your government has fallen.");
                return;
            }

            CurrentPrestige = player.globalPrestige;
            AllyCount       = CountAllies(player, allCountries, gm);

            bool isLeading = IsPrestigeLeader(player, allCountries);
            bool aboveTarget = player.globalPrestige >= PRESTIGE_WIN_TARGET;

            if (isLeading && aboveTarget)
            {
                _prestigeLeadMonths++;
                ProgressToWin = Mathf.Clamp01(_prestigeLeadMonths / (float)PRESTIGE_LEAD_MONTHS);

                if (_prestigeLeadMonths >= PRESTIGE_LEAD_MONTHS)
                {
                    AnnounceVictory(player, gm,
                        $"DIPLOMATIC SUPREMACY: {player.countryName} has maintained continental prestige " +
                        $"leadership for {PRESTIGE_LEAD_MONTHS} consecutive months. " +
                        $"Final prestige score: {player.globalPrestige:F0}.\n\n" +
                        $"Your nation is recognised as the dominant power of the region.");
                    return;
                }

                VictoryStatus = $"Prestige Lead: {_prestigeLeadMonths}/{PRESTIGE_LEAD_MONTHS} months";
                NotifyProgressMilestone(player, _prestigeLeadMonths, PRESTIGE_LEAD_MONTHS, "prestige leadership");
            }
            else
            {
                if (_prestigeLeadMonths > 0)
                {
                    UI.NotificationManager.Instance?.AddNotification(
                        $"⚠ PRESTIGE LEAD LOST: Another nation has overtaken your score. " +
                        $"Leadership streak reset. Rebuild your prestige to resume.", Color.yellow);
                    _prestigeLeadMonths = 0;
                    ProgressToWin = 0f;
                }

                VictoryStatus = $"Prestige: {player.globalPrestige:F0}/{PRESTIGE_WIN_TARGET} (need lead for {PRESTIGE_LEAD_MONTHS} months)";
                ProgressToWin = Mathf.Clamp01(player.globalPrestige / PRESTIGE_WIN_TARGET) * 0.5f
                              + Mathf.Clamp01(AllyCount / (float)DOMINATED_ALLY_COUNT)    * 0.5f;
            }

            if (isLeading && AllyCount >= DOMINATED_ALLY_COUNT)
            {
                AnnounceVictory(player, gm,
                    $"REGIONAL DOMINATION: {player.countryName} has forged a continent-spanning alliance " +
                    $"of {AllyCount} nations while holding the highest regional prestige.\n\n" +
                    $"No nation can challenge your diplomatic and military supremacy.");
                return;
            }

            if (player.globalPrestige >= 80f && player.globalPrestige < 100f && currentMonth % 6 == 0)
            {
                UI.NotificationManager.Instance?.AddNotification(
                    $"🌟 PRESTIGE {player.globalPrestige:F0}: Approaching the win threshold of {PRESTIGE_WIN_TARGET}. " +
                    $"Current allies: {AllyCount}/{DOMINATED_ALLY_COUNT} needed for dominance win.",
                    new Color(1f, 0.85f, 0.1f));
            }
        }

        private bool IsPrestigeLeader(CountryDataSO player, List<CountryDataSO> all)
        {
            foreach (var c in all)
            {
                if (c == player) continue;
                if (c.globalPrestige >= player.globalPrestige) return false;
            }
            return true;
        }

        private int CountAllies(CountryDataSO player, List<CountryDataSO> all, GameManager gm)
        {
            if (gm?.Relations == null) return 0;
            int count = 0;
            foreach (var c in all)
            {
                if (c == player) continue;
                if (gm.Relations.GetTier(player.countryName, c.countryName) == RelationTier.Allied)
                    count++;
            }
            return count;
        }

        private void NotifyProgressMilestone(CountryDataSO player, int current, int target, string label)
        {
            if (current == 1)
            {
                UI.NotificationManager.Instance?.AddNotification(
                    $"🏆 VICTORY PROGRESS: {label} streak started! " +
                    $"Maintain for {target - 1} more months to win.", Color.green);
            }
            else if (current == target - 1)
            {
                UI.NotificationManager.Instance?.AddNotification(
                    $"🏆 ONE MONTH AWAY from victory! Maintain {label} one more month.", Color.green);
            }
        }

        private void AnnounceVictory(CountryDataSO player, GameManager gm, string detail)
        {
            if (_victoryAnnounced) return;
            _victoryAnnounced = true;
            VictoryStatus     = "VICTORY";

            InboxSystem.Instance?.SendMessage(player, "UNITED AFRICAN COUNCIL",
                "🏆 VICTORY — YOUR NATION HAS PREVAILED",
                $"President,\n\n{detail}\n\n" +
                $"FINAL STATISTICS:\n" +
                $"• Prestige: {player.globalPrestige:F0}\n" +
                $"• Allies: {AllyCount}\n" +
                $"• GDP: ${player.gdp:N0}B\n" +
                $"• Stability: {player.stability:F0}%\n\n" +
                $"The history books will record your leadership.",
                "Critical");

            UI.NotificationManager.Instance?.AddNotification(
                "🏆 VICTORY ACHIEVED — Check your Inbox for the final summary.",
                Color.green);

            NewsSystem.Instance?.PublishNews(
                $"HISTORY: {player.countryName} emerges as the undisputed regional power. " +
                $"A new era begins.");

            if (gm != null) gm.isRealTime = false;
        }

        private void AnnounceDefeat(CountryDataSO player, GameManager gm, string reason)
        {
            if (_defeatAnnounced) return;
            _defeatAnnounced = true;
            VictoryStatus    = "DEFEATED";

            InboxSystem.Instance?.SendMessage(player, "SITUATION ROOM",
                "💀 GOVERNMENT COLLAPSED",
                $"{reason}\n\n" +
                $"FINAL STATISTICS:\n" +
                $"• Prestige: {player.globalPrestige:F0}\n" +
                $"• Stability: {player.stability:F0}%\n" +
                $"• Treasury: ${player.treasury:F1}B\n\n" +
                $"A caretaker government has been established. The people demand change.",
                "Critical");

            UI.NotificationManager.Instance?.AddNotification(
                "💀 GOVERNMENT COLLAPSED — Your nation has fallen.", Color.red);

            NewsSystem.Instance?.PublishNews(
                $"BREAKING: The government of {player.countryName} has collapsed. " +
                $"International community calls for emergency elections.");

            if (gm != null) gm.isRealTime = false;
        }

        public static void AwardPrestige(CountryDataSO country, float amount, string reason)
        {
            country.globalPrestige = Mathf.Clamp(country.globalPrestige + amount, 0f, 150f);

            if (country == GameManager.Instance?.playerCountry && Mathf.Abs(amount) >= 3f)
            {
                string sign = amount > 0 ? "+" : "";
                UI.NotificationManager.Instance?.AddNotification(
                    $"🌟 PRESTIGE {sign}{amount:F0}: {reason}. Total: {country.globalPrestige:F0}",
                    amount > 0 ? new Color(1f, 0.85f, 0.1f) : Color.yellow);
            }
        }
    }
}

