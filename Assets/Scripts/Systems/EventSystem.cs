using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace StateCraft.Systems
{
    public class EventSystem : IEventSystem, ISaveable
    {
        [System.Serializable]
        private class SavedPendingEvent
        {
            public string countryName;
            public string eventName;
            public int daysRemaining;
        }

        [System.Serializable]
        private class EventSystemSaveData
        {
            public List<SavedPendingEvent> queuedEvents = new List<SavedPendingEvent>();
        }

        public string ExportState()
        {
            var data = new EventSystemSaveData();
            foreach (var pe in _playerEventQueue)
            {
                if (pe.country == null || pe.ev == null) continue;
                data.queuedEvents.Add(new SavedPendingEvent
                {
                    countryName = pe.country.countryName,
                    eventName = pe.ev.name,
                    daysRemaining = pe.daysRemaining
                });
            }
            return JsonUtility.ToJson(data);
        }

        public void ImportState(string json)
        {
            var data = JsonUtility.FromJson<EventSystemSaveData>(json);
            if (data == null) return;

            _playerEventQueue.Clear();
            foreach (var spe in data.queuedEvents)
            {
                var country = GameManager.Instance.GetCountryByName(spe.countryName);
                var ev = possibleEvents?.Find(e => e.name == spe.eventName);
                if (country != null && ev != null)
                {
                    _playerEventQueue.Enqueue(new PendingEvent
                    {
                        country = country,
                        ev = ev,
                        daysRemaining = spe.daysRemaining
                    });
                }
            }
        }

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
                    if (oldest.ev.choices != null && oldest.ev.choices.Count > 0)
                        ApplyEventOutcome(oldest.country, oldest.ev, 0);
                    
                    EventBus.Publish(new WorldEvent {
                        eventType = WorldEventType.BreakingNews,
                        title = $"CRISIS UNRESOLVED: {oldest.ev.eventTitle} worsened due to inaction.",
                        sourceCountry = oldest.country
                    });
                }

                _playerEventQueue.Enqueue(new PendingEvent { country = country, ev = chosen, daysRemaining = chosen.timeRemainingDays });

                EventBus.Publish(new WorldEvent {
                    eventType = WorldEventType.BreakingNews,
                    title = "Critical Decision Required",
                    description = $"A new crisis ('{chosen.eventTitle}') requires your immediate attention. Open the Event Dashboard to review your options.",
                    sourceCountry = country
                });
            }
            else
            {
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

                NewsSystem.Instance?.PublishNews(
                    $"REPORT: {country.countryName} faces {chosen.eventTitle}. " +
                    $"Government adopts response measures.");
            }
        }

        public List<PendingEvent> GetPendingEvents(CountryDataSO country)
        {
            var list = new List<PendingEvent>();
            foreach (var p in _playerEventQueue)
            {
                if (p.country == country) list.Add(p);
            }
            return list;
        }

        public void InjectUrgentEvent(CountryDataSO country, GameEventSO ev)
        {
            if (country != GameManager.Instance?.playerCountry) return;
            
            _playerEventQueue.Enqueue(new PendingEvent { country = country, ev = ev, daysRemaining = ev.timeRemainingDays });

            EventBus.Publish(new WorldEvent {
                eventType = WorldEventType.BreakingNews,
                title = $"URGENT CRISIS: {ev.eventTitle}",
                description = ev.description,
                sourceCountry = country
            });
        }

        public void TickPendingEvents(CountryDataSO country)
        {
            if (_playerEventQueue.Count == 0) return;

            var temp = new Queue<PendingEvent>();
            while (_playerEventQueue.Count > 0)
            {
                var p = _playerEventQueue.Dequeue();
                if (p.country == country)
                {
                    p.daysRemaining--;
                    if (p.daysRemaining <= 0)
                    {
                        if (p.ev.choices != null && p.ev.choices.Count > 0)
                        {
                            ApplyEventOutcome(p.country, p.ev, 0);
                        }
                        
                        EventBus.Publish(new WorldEvent {
                            eventType = WorldEventType.BreakingNews,
                            title = $"CRISIS UNRESOLVED: {p.ev.eventTitle} worsened due to inaction.",
                            sourceCountry = p.country
                        });
                        continue;
                    }
                }
                temp.Enqueue(p);
            }
            _playerEventQueue = temp;
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

            if (presEff != 0f)
            {
                country.globalPrestige = Mathf.Clamp(country.globalPrestige + presEff, 0f, 100f);
            }

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

}
}
