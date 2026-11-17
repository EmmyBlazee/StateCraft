
using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using UnityEngine;
using System.Collections.Generic;

namespace StateCraft.Systems
{
    public class EventSystem : IEventSystem
    {
        private List<GameEventSO> possibleEvents;

        private Queue<PendingEvent> _playerEventQueue = new Queue<PendingEvent>();

        public EventSystem(List<GameEventSO> events)
        {
            possibleEvents = events;
        }

        public void CheckForEvents(CountryDataSO country)
        {
            if (possibleEvents == null || possibleEvents.Count == 0) return;
            if (Random.value >= 0.05f) return;

            TriggerRandomEvent(country);
        }

        private void TriggerRandomEvent(CountryDataSO country)
        {
            var valid = possibleEvents.FindAll(e =>
                country.stability >= e.minStability &&
                country.stability <= e.maxStability);

            if (valid.Count == 0) return;
            var chosen = valid[Random.Range(0, valid.Count)];

            var gm = GameManager.Instance;

            if (country == gm?.playerCountry)
            {
                const int MAX_PENDING = 5;
                if (_playerEventQueue.Count >= MAX_PENDING)
                {
                    var oldest = _playerEventQueue.Dequeue();
                    ApplyEventOutcome(oldest.country, oldest.ev, chooseA: false);
                    
                    EventBus.Publish(new WorldEvent {
                        eventType = WorldEventType.BreakingNews,
                        title = $"CRISIS UNRESOLVED: {oldest.ev.eventTitle} worsened due to inaction.",
                        sourceCountry = oldest.country
                    });
                }

                _playerEventQueue.Enqueue(new PendingEvent { country = country, ev = chosen });

                EventBus.Publish(new WorldEvent {
                    eventType = WorldEventType.BreakingNews,
                    title = "Critical Decision Required",
                    description = $"A new crisis ('{chosen.eventTitle}') requires your immediate attention. Open the Event Dashboard to review your options.",
                    sourceCountry = country
                });
            }
            else
            {
                bool chooseA = (chosen.optionAStability >= chosen.optionBStability);
                ApplyEventOutcome(country, chosen, chooseA);

                NewsSystem.Instance?.PublishNews(
                    $"REPORT: {country.countryName} faces {chosen.eventTitle}. " +
                    $"Government {(chooseA ? "takes decisive action" : "adopts measured response")}.");
            }
        }

        public List<GameEventSO> GetPendingEvents(CountryDataSO country)
        {
            var list = new List<GameEventSO>();
            foreach (var p in _playerEventQueue)
            {
                if (p.country == country) list.Add(p.ev);
            }
            return list;
        }

        public void ResolvePendingEvent(CountryDataSO country, GameEventSO ev, bool chooseA)
        {
            var temp = new Queue<PendingEvent>();
            bool resolvedOrRemoved = false;
            while (_playerEventQueue.Count > 0)
            {
                var p = _playerEventQueue.Dequeue();
                if (!resolvedOrRemoved && p.country == country && p.ev == ev)
                {
                    ApplyEventOutcome(country, ev, chooseA);
                    resolvedOrRemoved = true;
                }
                else
                {
                    temp.Enqueue(p);
                }
            }
            _playerEventQueue = temp;
        }

        public static void ApplyEventOutcome(CountryDataSO country, GameEventSO ev, bool chooseA)
        {
            float cost      = chooseA ? ev.optionACost      : ev.optionBCost;
            float stability = chooseA ? ev.optionAStability : ev.optionBStability;
            float gdpEffect = chooseA ? ev.optionAGDP       : ev.optionBGDP;
            float intelEff  = chooseA ? ev.optionAIntel     : ev.optionBIntel;
            float secEff    = chooseA ? ev.optionASecurity  : ev.optionBSecurity;
            string label    = chooseA ? ev.optionAText      : ev.optionBText;

            var gm = GameManager.Instance;

            if (cost != 0f)
            {
                country.treasury = Mathf.Max(0f, country.treasury - cost);
                if (country == gm?.playerCountry)
                    gm.playerBudget = country.treasury;
            }

            if (stability != 0f)
            {
                foreach (var region in country.regions)
                    region.stability = Mathf.Clamp(region.stability + stability, 0f, 100f);
                country.stability = Mathf.Clamp(country.stability + stability, 0f, 100f);
            }

            if (gdpEffect != 0f)
                country.gdp *= (1f + gdpEffect);

            if (intelEff != 0f)
                country.intelligenceLevel = Mathf.Clamp(country.intelligenceLevel + intelEff, 0f, 100f);

            if (secEff != 0f)
            {
                country.security = Mathf.Clamp(country.security + secEff, 0f, 100f);
                foreach (var region in country.regions)
                    region.security = Mathf.Clamp(region.security + secEff * 0.5f, 0f, 100f);
            }

            if (country == gm?.playerCountry)
            {
                string summary = BuildOutcomeSummary(cost, stability, gdpEffect, intelEff, secEff);

                EventBus.Publish(new WorldEvent {
                    eventType = WorldEventType.BreakingNews,
                    title = $"EVENT RESOLVED — {ev.eventTitle}: \"{label}\"",
                    description = $"You chose: \"{label}\"\n\nIMMEDIATE EFFECTS:{summary}\n\nSituation is being monitored. Further developments will be reported.",
                    sourceCountry = country
                });
            }

            EventBus.Publish(new WorldEvent {
                eventType = WorldEventType.BreakingNews,
                title = $"DOMESTIC: {country.countryName} government responds to {ev.eventTitle}.",
                sourceCountry = country
            });
        }

        private static string BuildOutcomeSummary(float cost, float stab, float gdp, float intel, float sec)
        {
            var parts = new List<string>();
            if (cost  != 0f) parts.Add($"Treasury {(cost < 0 ? "+" : "-")}${Mathf.Abs(cost):F1}B");
            if (stab  != 0f) parts.Add($"Stability {(stab  > 0 ? "+" : "")}{stab:F0}");
            if (gdp   != 0f) parts.Add($"GDP {(gdp   > 0 ? "+" : "")}{gdp * 100f:F1}%");
            if (intel != 0f) parts.Add($"Intel {(intel > 0 ? "+" : "")}{intel:F0}");
            if (sec   != 0f) parts.Add($"Security {(sec > 0 ? "+" : "")}{sec:F0}");
            return parts.Count > 0 ? " | " + string.Join(" | ", parts) : "";
        }

        private class PendingEvent
        {
            public CountryDataSO country;
            public GameEventSO   ev;
        }
    }
}
