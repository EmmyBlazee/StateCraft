using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace StateCraft.Systems
{
    public class EventSystem : IEventSystem
    {
        private List<GameEventSO> possibleEvents;

        // Queue of events awaiting player choice (displayed on the event dashboard)
        private Queue<PendingEvent> _playerEventQueue = new Queue<PendingEvent>();

        public EventSystem(List<GameEventSO> events)
        {
            possibleEvents = events;
        }

        // ── Called monthly for every country ────────────────
        public void CheckForEvents(CountryDataSO country)
        {
            if (possibleEvents == null || possibleEvents.Count == 0) return;
            if (Random.value >= 0.05f) return; // 5% chance per month

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
                // Cap pending events to prevent stale queue overflow
                const int MAX_PENDING = 5;
                if (_playerEventQueue.Count >= MAX_PENDING)
                {
                    var oldest = _playerEventQueue.Dequeue();
                    if (oldest.ev.choices != null && oldest.ev.choices.Count > 0)
                        ApplyEventOutcome(oldest.country, oldest.ev, 0); // Force choice 0 (default/ignore)
                    
                    EventBus.Publish(new WorldEvent {
                        eventType = WorldEventType.BreakingNews,
                        title = $"CRISIS UNRESOLVED: {oldest.ev.eventTitle} worsened due to inaction.",
                        sourceCountry = oldest.country
                    });
                }

                // Queue for UI display — player must make a choice
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
                // AI: pick the option with better stability outcome (greedy)
                if (chosen.choices != null && chosen.choices.Count > 0)
                {
                    int bestChoice = 0;
                    float bestStab = -9999f;
                    for(int i = 0; i < chosen.choices.Count; i++)
                    {
                        if (chosen.choices[i].stabilityEffect > bestStab)
                        {
                            bestStab = chosen.choices[i].stabilityEffect;
                            bestChoice = i;
                        }
                    }
                    ApplyEventOutcome(country, chosen, bestChoice);
                }

                // Publish a short news item for flavour
                NewsSystem.Instance?.PublishNews(
                    $"REPORT: {country.countryName} faces {chosen.eventTitle}. " +
                    $"Government adopts response measures.");
            }
        }

        // ── Expose Pending Events to the Dashboard ──────────
        public List<GameEventSO> GetPendingEvents(CountryDataSO country)
        {
            var list = new List<GameEventSO>();
            foreach (var p in _playerEventQueue)
            {
                if (p.country == country) list.Add(p.ev);
            }
            return list;
        }

        public void ResolvePendingEvent(CountryDataSO country, GameEventSO ev, int choiceIndex)
        {
            var temp = new Queue<PendingEvent>();
            bool resolvedOrRemoved = false;
            while (_playerEventQueue.Count > 0)
            {
                var p = _playerEventQueue.Dequeue();
                if (!resolvedOrRemoved && p.country == country && p.ev == ev)
                {
                    ApplyEventOutcome(country, ev, choiceIndex);
                    resolvedOrRemoved = true;
                }
                else
                {
                    temp.Enqueue(p);
                }
            }
            _playerEventQueue = temp;
        }

        // ── Apply the mechanical consequences of a choice ───
        public static void ApplyEventOutcome(CountryDataSO country, GameEventSO ev, int choiceIndex)
        {
            if (ev.choices == null || ev.choices.Count <= choiceIndex) return;

            var choice = ev.choices[choiceIndex];

            float cost      = choice.costUSD;
            float stability = choice.stabilityEffect;
            float gdpEffect = choice.gdpEffect;
            float intelEff  = choice.intelEffect;
            float secEff    = choice.securityEffect;
            float presEff   = choice.prestigeEffect;
            string label    = choice.choiceText;

            var gm = GameManager.Instance;

            // Treasury cost
            if (cost != 0f)
            {
                country.treasury = Mathf.Max(0f, country.treasury - cost);
                if (country == gm?.playerCountry)
                    gm.playerBudget = country.treasury;
            }

            // Stability
            if (stability != 0f)
            {
                foreach (var region in country.regions)
                    region.stability = Mathf.Clamp(region.stability + stability, 0f, 100f);
                country.stability = Mathf.Clamp(country.stability + stability, 0f, 100f);
            }

            // GDP shock (expressed as fractional multiplier in the SO, e.g. -0.02 = -2%)
            if (gdpEffect != 0f)
                country.gdp *= (1f + gdpEffect);

            // Intelligence
            if (intelEff != 0f)
                country.intelligenceLevel = Mathf.Clamp(country.intelligenceLevel + intelEff, 0f, 100f);

            // Security
            if (secEff != 0f)
            {
                country.security = Mathf.Clamp(country.security + secEff, 0f, 100f);
                foreach (var region in country.regions)
                    region.security = Mathf.Clamp(region.security + secEff * 0.5f, 0f, 100f);
            }

            if (presEff != 0f)
            {
                country.globalPrestige = Mathf.Clamp(country.globalPrestige + presEff, 0f, 100f);
            }

            // Notify via EventBus (Decoupled)
            if (country == gm?.playerCountry)
            {
                string summary = BuildOutcomeSummary(cost, stability, gdpEffect, intelEff, secEff, presEff);

                EventBus.Publish(new WorldEvent {
                    eventType = WorldEventType.BreakingNews,
                    title = $"EVENT RESOLVED — {ev.eventTitle}",
                    description = $"You chose: \"{label}\"\n\nIMMEDIATE EFFECTS:{summary}",
                    sourceCountry = country
                });
            }

            EventBus.Publish(new WorldEvent {
                eventType = WorldEventType.BreakingNews,
                title = $"DOMESTIC: {country.countryName} government responds to {ev.eventTitle}.",
                sourceCountry = country
            });
        }

        private static string BuildOutcomeSummary(float cost, float stab, float gdp, float intel, float sec, float pres)
        {
            var parts = new List<string>();
            if (cost  != 0f) parts.Add($"Treasury {(cost < 0 ? "+" : "-")}${Mathf.Abs(cost):F1}B");
            if (stab  != 0f) parts.Add($"Stability {(stab  > 0 ? "+" : "")}{stab:F0}");
            if (gdp   != 0f) parts.Add($"GDP {(gdp   > 0 ? "+" : "")}{gdp * 100f:F1}%");
            if (intel != 0f) parts.Add($"Intel {(intel > 0 ? "+" : "")}{intel:F0}");
            if (sec   != 0f) parts.Add($"Security {(sec > 0 ? "+" : "")}{sec:F0}");
            if (pres  != 0f) parts.Add($"Prestige {(pres > 0 ? "+" : "")}{pres:F0}");
            return parts.Count > 0 ? " | " + string.Join(" | ", parts) : "";
        }

        private class PendingEvent
        {
            public CountryDataSO country;
            public GameEventSO   ev;
        }
    }
}
