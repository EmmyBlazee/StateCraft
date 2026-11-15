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

        private readonly Dictionary<string, int> _lastBattleReportTurn = new Dictionary<string, int>();
        private const int BATTLE_REPORT_COOLDOWN_TURNS = 3;

        private int _routineUnreadCount = 0;

        public InboxSystem()
        {
            if (Instance == null) Instance = this;
        }

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

        public int GetRoutineUnreadCount() => _routineUnreadCount;

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

            if (targetCountry.inboxMessages.Count > 50)
                targetCountry.inboxMessages.RemoveRange(50, targetCountry.inboxMessages.Count - 50);

            if (targetCountry != gm?.playerCountry) return;

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
            else if (urgency == "Important")
            {
                Color importantColor = new Color(1f, 0.72f, 0.1f);
                NotificationManager.Instance?.AddNotification(
                    $"📋 {subject}", importantColor);
            }
            else
            {
                _routineUnreadCount++;
                if (_routineUnreadCount == 1)
                    NotificationManager.Instance?.AddNotification(
                        "📩 New dispatches in inbox.", new Color(0f, 0.8f, 1f));
            }
        }
    }
}