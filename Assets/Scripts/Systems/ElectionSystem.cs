using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using UnityEngine;

namespace StateCraft.Systems
{
    /// <summary>
    /// ElectionSystem — 4-year election cycle with full player communication.
    /// Player is voted out if approval < 50% when election arrives.
    /// Snap elections trigger if stability drops below 25%.
    /// </summary>
    public class ElectionSystem : IElectionSystem
    {
        // 48 months = 4 years (each turn = 1 month when turnDuration = 30 days)
        private const int ElectionIntervalMonths     = 48;
        private const float SnapElectionThreshold    = 25f;   // stability %
        private const float CoupThreshold            = 15f;   // below this = coup risk

        // Warning milestones (months before election)
        private static readonly int[] WarningMonths  = { 12, 6, 3, 1 };

        public void CheckForElection(CountryDataSO country)
        {
            if (country == null) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            int currentMonth = gm.TurnSystem?.CurrentTurn ?? 1;
            bool isPlayer    = (country == gm.playerCountry);

            // ── Coup check — below 15% stability before election triggers a coup ──
            if (isPlayer && country.stability < CoupThreshold)
            {
                CheckCoupRisk(country, currentMonth);
                return; // Coup supersedes election
            }

            // ── Scheduled election warning notifications ──
            if (isPlayer)
                SendElectionWarnings(country, currentMonth);

            // ── Snap election — instability forces early vote ──
            bool isSnapElection = country.stability < SnapElectionThreshold &&
                                  country.stability >= CoupThreshold &&
                                  currentMonth > 6; // Not in first 6 months

            // ── Scheduled 4-year election ──
            bool isScheduledElection = currentMonth > 1 &&
                                       (currentMonth % ElectionIntervalMonths == 0);

            if (!isScheduledElection && !isSnapElection) return;

            ResolveElection(country, isSnapElection, isPlayer, currentMonth);
        }

        // ─────────────────────────────────────────────────────────
        // Election warnings
        // ─────────────────────────────────────────────────────────

        private void SendElectionWarnings(CountryDataSO country, int currentMonth)
        {
            int monthsUntilElection = ElectionIntervalMonths - (currentMonth % ElectionIntervalMonths);

            foreach (int warningAt in WarningMonths)
            {
                if (monthsUntilElection != warningAt) continue;

                float approval = CalculateApproval(country, false);
                string approvalRating = approval >= 70f ? "STRONG" :
                                        approval >= 55f ? "MODERATE" :
                                        approval >= 45f ? "MARGINAL" :
                                        approval >= 35f ? "WEAK" : "CRITICAL — DEFEAT LIKELY";

                Color warningColor = approval >= 50f
                    ? new Color(0.2f, 0.85f, 0.4f)
                    : new Color(1f, 0.35f, 0.35f);

                string urgency = (warningAt <= 3 && approval < 50f) ? "Critical" : "Important";

                NotificationManager.Instance?.AddNotification(
                    $"🗳 ELECTION IN {warningAt} MONTH{(warningAt > 1 ? "S" : "")}: " +
                    $"Current approval: {approval:F0}% ({approvalRating})",
                    warningColor);

                InboxSystem.Instance?.SendMessage(country,
                    "PRESIDENTIAL CAMPAIGN OFFICE",
                    $"Election Briefing — {warningAt} Month{(warningAt > 1 ? "s" : "")} Remaining",
                    BuildElectionBriefing(country, approval, warningAt),
                    urgency);

                break;
            }
        }

        // ─────────────────────────────────────────────────────────
        // Main election resolution
        // ─────────────────────────────────────────────────────────

        private void ResolveElection(CountryDataSO country, bool isSnap, bool isPlayer, int currentMonth)
        {
            float approval      = CalculateApproval(country, isSnap);
            bool incumbentWins  = approval >= 50f;
            string electionType = isSnap ? "SNAP ELECTION" : "GENERAL ELECTION";

            // ── Publish news ──
            NewsSystem.Instance?.PublishNews(
                $"{electionType}: {country.countryName} goes to the polls. " +
                $"Incumbent approval rating: {approval:F0}%.");

            if (incumbentWins)
                HandleElectionWin(country, approval, isSnap, isPlayer, electionType, currentMonth);
            else
                HandleElectionLoss(country, approval, isSnap, isPlayer, electionType, currentMonth);
        }

        private void HandleElectionWin(CountryDataSO country, float approval, bool isSnap,
                                        bool isPlayer, string electionType, int currentMonth)
        {
            // Win effects
            country.stability       = Mathf.Min(100f, country.stability + 8f);
            country.globalPrestige  = Mathf.Min(100f, country.globalPrestige + 5f);

            NewsSystem.Instance?.PublishNews(
                $"ELECTION RESULT: Incumbent government wins re-election in {country.countryName} " +
                $"with {approval:F0}% approval. Mandate renewed.");

            if (!isPlayer) return;

            string margin = approval >= 70f ? "LANDSLIDE VICTORY" :
                            approval >= 60f ? "CONVINCING WIN" :
                            approval >= 55f ? "COMFORTABLE MAJORITY" : "NARROW VICTORY";

            // Show as critical dispatch so player sees it immediately
            InboxSystem.Instance?.SendMessage(country,
                "ELECTORAL COMMISSION",
                $"🗳 {margin} — RE-ELECTED AS PRESIDENT",
                $"FINAL ELECTION RESULTS — {electionType}\n\n" +
                $"APPROVAL RATING: {approval:F0}%\n" +
                $"RESULT: {margin}\n\n" +
                $"The people have spoken. Your mandate has been renewed.\n\n" +
                $"KEY FACTORS IN YOUR VICTORY:\n" +
                BuildWinFactors(country, approval) +
                $"\n\nYour next election is in {ElectionIntervalMonths} months.\n" +
                $"Stability +8%, Prestige +5% from electoral mandate.",
                "Critical");

            NotificationManager.Instance?.AddNotification(
                $"🗳 ELECTION WON: {margin} — Approval {approval:F0}%. " +
                $"Next election in {ElectionIntervalMonths} months.",
                new Color(0.2f, 0.9f, 0.45f));
        }

        private void HandleElectionLoss(CountryDataSO country, float approval, bool isSnap,
                                         bool isPlayer, string electionType, int currentMonth)
        {
            // Loss effects on country
            country.stability       = Mathf.Max(0f, country.stability - 12f);
            country.globalPrestige  = Mathf.Max(0f, country.globalPrestige - 8f);

            // New government shifts policy toward median
            country.taxRate      = Mathf.Lerp(country.taxRate, 22f, 0.3f);
            country.subsidyLevel = Mathf.Lerp(country.subsidyLevel, 30f, 0.3f);

            NewsSystem.Instance?.PublishNews(
                $"ELECTION RESULT: Opposition wins in {country.countryName}. " +
                $"Incumbent approval was {approval:F0}%. Power transfer begins.");

            if (!isPlayer) return;

            // ── GAME OVER — player is voted out ──
            // Show the critical dispatch first, then trigger game over screen
            InboxSystem.Instance?.SendMessage(country,
                "ELECTORAL COMMISSION",
                "🗳 ELECTORAL DEFEAT — YOUR PRESIDENCY HAS ENDED",
                $"FINAL ELECTION RESULTS — {electionType}\n\n" +
                $"APPROVAL RATING: {approval:F0}%\n" +
                $"RESULT: DEFEATED\n\n" +
                $"The people have voted. Your government has lost its mandate.\n\n" +
                $"KEY REASONS FOR DEFEAT:\n" +
                BuildLossReasons(country, approval) +
                $"\n\nThe opposition party will form a new government.\n" +
                $"Your presidency ends after {currentMonth} months in office.\n\n" +
                $"Press ACKNOWLEDGE to view your final legacy report.",
                "Critical");

            // Trigger game over after a short delay
            // UIStateManager handles the actual game over screen
            GameManager.Instance?.StartCoroutine(TriggerGameOver(country, approval, currentMonth));
        }

        // ─────────────────────────────────────────────────────────
        // Coup risk — if stability crashes before election
        // ─────────────────────────────────────────────────────────

        private void CheckCoupRisk(CountryDataSO country, int currentMonth)
        {
            bool isPlayer = (country == GameManager.Instance?.playerCountry);

            // Random roll — higher the deficit below threshold, higher the chance
            float coupChance = Mathf.InverseLerp(CoupThreshold, 0f, country.stability) * 0.4f;

            if (UnityEngine.Random.value > coupChance) return;

            // Coup happens
            country.stability       = Mathf.Max(5f, country.stability - 20f);
            country.globalPrestige  = Mathf.Max(0f, country.globalPrestige - 25f);

            NewsSystem.Instance?.PublishNews(
                $"COUP D'ÉTAT: Military leadership seizes power in {country.countryName} " +
                $"amid political crisis. Constitutional order suspended.");

            if (!isPlayer) return;

            InboxSystem.Instance?.SendMessage(country,
                "CHIEF OF ARMY STAFF",
                "🚨 MILITARY COUP — YOUR GOVERNMENT HAS BEEN OVERTHROWN",
                $"Mr. President,\n\n" +
                $"With national stability at {country.stability:F0}%, the Armed Forces Council " +
                $"has voted to suspend the constitution and assume emergency control of the government.\n\n" +
                $"Your failure to maintain order has made military intervention necessary.\n\n" +
                $"KEY FAILURES:\n" +
                BuildLossReasons(country, country.stability * 0.5f) +
                $"\n\nYour presidency ends after {currentMonth} months in office.",
                "Critical");

            GameManager.Instance?.StartCoroutine(TriggerGameOver(country,
                country.stability * 0.5f, currentMonth, isCoup: true));
        }

        // ─────────────────────────────────────────────────────────
        // Game over coroutine
        // ─────────────────────────────────────────────────────────

        private System.Collections.IEnumerator TriggerGameOver(CountryDataSO country,
            float approval, int monthsInOffice, bool isCoup = false)
        {
            // Wait for the Critical dispatch to be acknowledged
            yield return new UnityEngine.WaitForSeconds(0.5f);

            // Wait until the dispatch queue is empty
            float waitTime = 0f;
            while (CriticalDispatchController.Instance != null &&
                   waitTime < 30f) // max 30 seconds wait
            {
                waitTime += UnityEngine.Time.deltaTime;
                yield return null;
                // Check if modal is still showing
                // Once acknowledged, UIStateManager will handle it
            }

            yield return new UnityEngine.WaitForSeconds(1f);

            // Pause the game
            if (GameManager.Instance != null)
                GameManager.Instance.isRealTime = false;

            // Show game over via UIStateManager
            UIStateManager.Instance?.ShowGameOver(country, approval, monthsInOffice, isCoup);
        }

        // ─────────────────────────────────────────────────────────
        // Approval calculation
        // ─────────────────────────────────────────────────────────

        private float CalculateApproval(CountryDataSO country, bool isSnap)
        {
            // Stability is the biggest factor (40%)
            float stabilityScore = country.stability * 0.40f;

            // Economic health (30%) — income vs expenses ratio
            float economicScore = 0f;
            if (country.gdp > 0f)
            {
                float budgetBalance = country.income - country.spending;
                economicScore = Mathf.Clamp(30f + (budgetBalance * 5f), 0f, 30f);
            }

            // Prestige / international standing (15%)
            float prestigeScore = country.globalPrestige * 0.15f;

            // Military performance (10%) — if at war and winning
            float militaryScore = 10f; // baseline
            if (country.warExhaustion > 50f) militaryScore -= 5f;
            if (country.totalKIA > 5000)     militaryScore -= 5f;

            // Inflation penalty (5%)
            float inflationPenalty = Mathf.Clamp(country.inflationRate * 0.15f, 0f, 10f);

            float approval = stabilityScore + economicScore + prestigeScore
                             + militaryScore - inflationPenalty;

            // Snap elections carry an incumbent penalty
            if (isSnap) approval -= 20f;

            return Mathf.Clamp(approval, 0f, 100f);
        }

        // ─────────────────────────────────────────────────────────
        // Briefing builders
        // ─────────────────────────────────────────────────────────

        private string BuildElectionBriefing(CountryDataSO c, float approval, int monthsLeft)
        {
            string status = approval >= 50f ? "on track to win" : "at risk of losing";
            string action = approval >= 50f
                ? "• Maintain stability and avoid economic shocks\n• Do not start new wars\n• Keep prestige high"
                : "• URGENTLY raise stability — deploy security forces or increase social spending\n" +
                  "• Reduce inflation via Economy → Monetary Policy\n" +
                  "• Win a diplomatic victory to boost prestige\n" +
                  "• Avoid military disasters that raise war exhaustion";

            return
                $"Sir, with {monthsLeft} month{(monthsLeft > 1 ? "s" : "")} until the election " +
                $"our polling shows an approval rating of {approval:F0}% — you are {status}.\n\n" +
                $"CURRENT APPROVAL BREAKDOWN:\n" +
                $"• Stability ({c.stability:F0}%): {c.stability * 0.40f:F0} points\n" +
                $"• Economic health: {(c.income > c.spending ? "Surplus ✓" : "Deficit ✗")}\n" +
                $"• International standing ({c.globalPrestige:F0}): {c.globalPrestige * 0.15f:F0} points\n" +
                $"• Inflation ({c.inflationRate:F0}%): -{Mathf.Clamp(c.inflationRate * 0.15f, 0f, 10f):F0} points\n\n" +
                $"RECOMMENDED ACTIONS:\n{action}";
        }

        private string BuildWinFactors(CountryDataSO c, float approval)
        {
            var factors = new System.Text.StringBuilder();
            if (c.stability >= 60f)   factors.AppendLine("• ✓ Strong national stability");
            if (c.income > c.spending) factors.AppendLine("• ✓ Balanced national budget");
            if (c.globalPrestige >= 50f) factors.AppendLine("• ✓ Strong international standing");
            if (c.inflationRate < 10f) factors.AppendLine("• ✓ Low inflation");
            if (c.allianceCount > 0)   factors.AppendLine("• ✓ Active alliance network");
            if (factors.Length == 0)   factors.AppendLine("• Narrow win — opposition was weak");
            return factors.ToString();
        }

        private string BuildLossReasons(CountryDataSO c, float approval)
        {
            var reasons = new System.Text.StringBuilder();
            if (c.stability < 40f)    reasons.AppendLine("• ✗ Low national stability — citizens feel unsafe");
            if (c.income < c.spending) reasons.AppendLine("• ✗ Budget deficit — economic mismanagement");
            if (c.inflationRate > 20f) reasons.AppendLine($"• ✗ Hyperinflation at {c.inflationRate:F0}% — cost of living crisis");
            if (c.globalPrestige < 30f) reasons.AppendLine("• ✗ Low international prestige — weak foreign policy");
            if (c.warExhaustion > 60f) reasons.AppendLine("• ✗ War fatigue — public opposes ongoing conflict");
            if (c.totalKIA > 3000)    reasons.AppendLine($"• ✗ {c.totalKIA:N0} military casualties — unacceptable losses");
            if (c.sanctionReceivedCount > 0) reasons.AppendLine("• ✗ International sanctions harming the economy");
            if (reasons.Length == 0)   reasons.AppendLine("• Opposition ran a stronger campaign");
            return reasons.ToString();
        }
    }
}