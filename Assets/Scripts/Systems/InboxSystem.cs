using System;
using System.Collections.Generic;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using UnityEngine;

namespace StateCraft.Systems
{
    public class InboxSystem
    {
        public static InboxSystem Instance { get; private set; }

        // ── Battle report cooldown: prevents spam on active fronts ──
        // Key = "CountryName_vs_EnemyName", Value = turn last report was sent
        private readonly Dictionary<string, int> _lastBattleReportTurn = new Dictionary<string, int>();
        private const int BATTLE_REPORT_COOLDOWN_TURNS = 3;

        // ── Routine message batching: Normal urgency messages only show a
        //    badge count rather than a popup toast, preventing notification spam ──
        private int _routineUnreadCount = 0;

        public InboxSystem()
        {
            if (Instance == null) Instance = this;
        }

        // ── Call before sending a battle report to enforce cooldown ──
        public bool CanSendBattleReport(string countryA, string countryB)
        {
            string key = $"{countryA}_vs_{countryB}";
            GameManager gm = GameManager.Instance;
            int currentTurn = gm?.TurnSystem?.CurrentTurn ?? 0;

            if (_lastBattleReportTurn.TryGetValue(key, out int lastTurn))
            {
                if (currentTurn - lastTurn < BATTLE_REPORT_COOLDOWN_TURNS)
                    return false;
            }
            _lastBattleReportTurn[key] = currentTurn;
            return true;
        }

        // ── Returns unread routine message count for badge display ──
        public int GetRoutineUnreadCount() => _routineUnreadCount;

        // ── Call when player opens inbox to reset routine badge ──
        public void ClearRoutineBadge() => _routineUnreadCount = 0;

        public void SendMessage(CountryDataSO targetCountry, string sender, string subject, string body, string urgency = "Normal")
        {
            if (targetCountry == null) return;

            GameManager gm = GameManager.Instance;
            int turn    = gm?.TurnSystem != null ? gm.TurnSystem.CurrentTurn : 0;
            string date = gm?.TurnSystem != null ? gm.TurnSystem.CurrentDate  : "Unknown Date";

            var msg = new InboxMessage
            {
                id           = Guid.NewGuid().ToString(),
                sender       = sender,
                subject      = subject,
                body         = body,
                dateReceived = date,
                isRead       = false,
                turnReceived = turn,
                urgency      = urgency
            };

            targetCountry.inboxMessages.Insert(0, msg);

            // Cap inbox at 50 messages
            if (targetCountry.inboxMessages.Count > 50)
                targetCountry.inboxMessages.RemoveRange(50, targetCountry.inboxMessages.Count - 50);

            if (targetCountry != gm?.playerCountry) return;

            // ── Critical messages: blocking modal dispatch ──────────────────────
            if (urgency == "Critical")
            {
                Color accentColor = new Color(1f, 0.22f, 0.22f);
                if (CriticalDispatchController.Instance != null)
                    CriticalDispatchController.Instance.ShowDispatch(sender, subject, body, accentColor);
                else
                    NotificationManager.Instance?.AddNotification(
                        $"🚨 URGENT DISPATCH FROM {sender.ToUpper()}: {subject} — Check Events tab.",
                        accentColor);
            }
            // ── Important messages: amber toast notification ─────────────────────
            else if (urgency == "Important")
            {
                Color importantColor = new Color(1f, 0.72f, 0.1f);
                NotificationManager.Instance?.AddNotification(
                    $"📋 {subject}", importantColor);
            }
            // ── Normal/Routine messages: silent — increment badge only ───────────
            // No toast popup for routine messages. Players check the inbox
            // on their own terms. This prevents battle report spam.
            else
            {
                _routineUnreadCount++;
                // Show a single consolidated badge notification only when the
                // count reaches a threshold, not on every individual message.
                if (_routineUnreadCount == 1)
                    NotificationManager.Instance?.AddNotification(
                        "📩 New dispatches in inbox.", new Color(0f, 0.8f, 1f));
                // Additional routine messages beyond the first: purely silent.
            }
        }
    }
}