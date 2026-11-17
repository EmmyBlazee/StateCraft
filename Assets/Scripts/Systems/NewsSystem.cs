using UnityEngine;
using System;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;

namespace StateCraft.Systems
{
    public class NewsSystem : MonoBehaviour
    {
        public static NewsSystem Instance { get; private set; }

        private Queue<string> newsQueue = new Queue<string>();
        public IEnumerable<string> NewsHistory => newsQueue;
        public event Action<string> OnNewsPublished;

        private Queue<string> globalNewsQueue = new Queue<string>();
        public IEnumerable<string> GlobalNewsHistory => globalNewsQueue;
        public event Action<string> OnGlobalNewsPublished;

        private Dictionary<string, float> lastTurnStability =
            new Dictionary<string, float>();

        private Dictionary<string, float> lastTurnGDP =
            new Dictionary<string, float>();

        private Dictionary<string, int> lastHeadlineTurn =
            new Dictionary<string, int>();

        private int currentTurn = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
}
            else if (Instance != this)
            {
this.enabled = false;
            }
        }

        private void OnEnable()
        {
            EventBus.OnWorldEvent += HandleWorldEvent;
        }

        private void OnDisable()
        {
            EventBus.OnWorldEvent -= HandleWorldEvent;
        }

        private void Start()
        {
            PublishNews(
                "STRATEGIC COMMAND: INTEL FEED SYNCHRONIZED. STANDING BY FOR CONTINENTAL REPORTS."
            );

            PublishNews(
                "SITUATION REPORT: ALL REGIONAL FREQUENCIES ARE NOW LIVE. WELCOME, COMMANDER."
            );
        }

        private void HandleWorldEvent(WorldEvent worldEvent)
        {
            if (worldEvent == null || this == null) return;

            PublishNews(worldEvent.title);
        }

        public void PublishNews(string headline, bool isGlobalOnly = false)
        {
            string boldHeadline = headline.ToUpper();
            int globalListeners = OnGlobalNewsPublished != null ? OnGlobalNewsPublished.GetInvocationList().Length : 0;
            int localListeners = OnNewsPublished != null ? OnNewsPublished.GetInvocationList().Length : 0;
globalNewsQueue.Enqueue(boldHeadline);

            while (globalNewsQueue.Count > 30)
            {
                globalNewsQueue.Dequeue();
            }

            OnGlobalNewsPublished?.Invoke(boldHeadline);

            if (!isGlobalOnly)
            {
                newsQueue.Enqueue(boldHeadline);

                while (newsQueue.Count > 30)
                {
                    newsQueue.Dequeue();
                }

                OnNewsPublished?.Invoke(boldHeadline);
            }

}

        private bool CanPublish(string key, int cooldownTurns = 4)
        {
            if (!lastHeadlineTurn.TryGetValue(key, out int last))
            {
                lastHeadlineTurn[key] = currentTurn;
                return true;
            }
            if (currentTurn - last >= cooldownTurns)
            {
                lastHeadlineTurn[key] = currentTurn;
                return true;
            }
            return false;
        }

        public void GenerateTurnNews()
        {
            if (GameManager.Instance == null) return;

            GenerateTurnNews(GameManager.Instance.allCountries);
        }

        public void GenerateTurnNews(List<CountryDataSO> countries)
        {
            var player = GameManager.Instance?.playerCountry;
            currentTurn++;

            float roll = UnityEngine.Random.value;
if (roll > 0.8f && countries != null && countries.Count > 0)
            {
                var flashCountry = countries[UnityEngine.Random.Range(0, countries.Count)];
                PublishNews(GetRandomGlobalFlash(flashCountry), true);
            }

            var countriesToProcess = new List<CountryDataSO>();
            if (player != null) countriesToProcess.Add(player);

            var aiPool = new List<CountryDataSO>();
            foreach (var c in countries)
            {
                if (c != null && c != player)
                    aiPool.Add(c);
            }

            for (int i = aiPool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (aiPool[i], aiPool[j]) = (aiPool[j], aiPool[i]);
            }

            int aiLimit = Mathf.Min(8, aiPool.Count);
            for (int i = 0; i < aiLimit; i++)
                countriesToProcess.Add(aiPool[i]);

            foreach (var country in countriesToProcess)
            {
                if (country == null) continue;

                string name = country.countryName;

                string cap =
                    string.IsNullOrEmpty(country.capitalCity)
                    ? name.ToUpper() + " CAPITAL"
                    : country.capitalCity;

                bool isPlayer = (country == player);

                bool globalOnly = !isPlayer;

                float prevStability =
                    lastTurnStability.ContainsKey(name)
                    ? lastTurnStability[name]
                    : country.stability;

                float stabilityChange =
                    country.stability - prevStability;

                lastTurnStability[name] = country.stability;

                if (stabilityChange < -5f && CanPublish($"{name}_stability_collapse", 3))
                {
                    PublishNews(
                        $"CRISIS: {cap} IN TURMOIL AS {name} STABILITY COLLAPSES AMID INTERNAL STRIFE.",
                        globalOnly
                    );
                }
                else if (country.stability < 30f &&
                         UnityEngine.Random.value > 0.7f &&
                         CanPublish($"{name}_low_stability", 5))
                {
                    PublishNews(
                        $"POLITICAL ALERT: GOVERNMENT CONTROL FALTERING IN {name}; SECURITY FORCES ON HIGH ALERT.",
                        globalOnly
                    );
                }
                else if (stabilityChange > 5f && CanPublish($"{name}_stability_surge", 3))
                {
                    PublishNews(
                        $"REFORM: STABILITY SURGES IN {name} AS NEW POLICY FRAMEWORK TAKES HOLD.",
                        globalOnly
                    );
                }

                float logChance = isPlayer ? 0.6f : 0.98f;

                if (country.gdp > 200f &&
                    UnityEngine.Random.value > logChance &&
                    CanPublish($"{name}_gdp_milestone", 6))
                {
                    PublishNews(
                        $"MARKETS: {name} POSITIONED AS REGIONAL POWERHOUSE AS GDP HITS STRATEGIC MILESTONE.",
                        globalOnly
                    );
                }
                else if (country.debt > country.gdp * 0.7f &&
                         UnityEngine.Random.value > 0.9f &&
                         CanPublish($"{name}_debt_crisis", 5))
                {
                    PublishNews(
                        $"DEBT CRISIS: {name} TREASURY WARNS OF FISCAL COLLAPSE AMID MOUNTING DEBT OBLIGATIONS.",
                        globalOnly
                    );
                }

                if (country.militaryStrength > 400 &&
                    UnityEngine.Random.value > 0.95f &&
                    CanPublish($"{name}_military_exercises", 6))
                {
                    PublishNews(
                        $"WAR ROOM: {name} MILITARY EXERCISES DETECTED; READINESS LEVELS AT RECORD HIGHS.",
                        globalOnly
                    );
                }

                bool hasInsurgency = false;

                foreach (var r in country.regions)
                {
                    if (r.insurgencyLevel > 50)
                    {
                        if (UnityEngine.Random.value > 0.8f &&
                            CanPublish($"{name}_{r.regionName}_insurgency", 4))
                        {
                            PublishNews(
                                $"INSURGENCY: MILITANT GROUPS EXPANDING OPERATIONS IN {r.regionName} REGION, {name}.",
                                globalOnly
                            );

                            hasInsurgency = true;

                            break;
                        }
                    }
                }

                if (!hasInsurgency &&
                    country.security < 40 &&
                    UnityEngine.Random.value > 0.95f &&
                    CanPublish($"{name}_intel_operatives", 5))
                {
                    PublishNews(
                        $"INTEL: UNKNOWN OPERATIVES SPOTTED NEAR {cap}; FOREIGN MEDDLING SUSPECTED.",
                        globalOnly
                    );
                }

                if (GameManager.Instance?.WarSystem != null)
                {
                    var warStage =
                        GameManager.Instance.WarSystem.GetWarStage(country);

                    if (warStage ==
                        StateCraft.Interfaces.WarStage.Conflict &&
                        UnityEngine.Random.value > 0.7f &&
                        CanPublish($"{name}_frontline", 3))
                    {
                        PublishNews(
                            $"TACTICAL: FRONTLINE REPORTS INDICATE INTENSE SKIRMISHES ON {name} BORDERS.",
                            globalOnly
                        );
                    }
                }
            }
        }

        private string GetRandomGlobalFlash(CountryDataSO country)
        {
            string name =
                country != null
                ? country.countryName
                : "THE REGION";

            string cap =
                country != null &&
                !string.IsNullOrEmpty(country.capitalCity)
                ? country.capitalCity
                : (country != null ? country.countryName.ToUpper() + " CAPITAL" : "THE CAPITAL");

            string[] flashes =
            {
                $"ECONOMY: {name} DIGITAL TRADE PROTOCOLS TRIGGER 12% GDP SURGE.",

                $"CRISIS: {name} REGIONAL POWER GRID FAILURE SHUTS DOWN COBALT PROCESSING.",

                $"ECONOMY: GLOBAL LITHIUM PRICES HIT ALL-TIME HIGH; {name} GAINS TRADE BOOST.",

                $"INFRASTRUCTURE: 6G UNDERSEA CABLE LANDS IN {cap}; TECH STOCKS RALLY.",

                $"ECONOMY: INFLATION HITS 30% IN {name}; FOOD RIOTS REPORTED.",

                $"TRADE: NEW LITHIUM-FOR-INFRASTRUCTURE PACT SIGNED BY {name}.",

                $"MILITARY: BORDER SKIRMISH REPORTED NEAR {name} DISPUTED ZONE.",

                $"STABILITY: AU PEACEKEEPERS DEPLOY TO {name} BUFFER REGION.",

                $"CRISIS: VIOLENT UNREST ERUPTS IN {name} OVER FUEL SUBSIDY TERMINATION.",

                $"WAR: INSURGENTS SEIZE CONTROL OF {name} NORTHERN PIPELINE.",

                $"MILITARY: RDF HIGH COMBAT READINESS ACTIVATED IN {name}.",

                $"INTEL: FOREIGN SPY NETWORK DISMANTLED IN {cap}.",

                $"CYBER: CENTRAL BANK OF {name} TARGETED BY RANSOMWARE ATTACK.",

                $"INTEL: SURVEILLANCE DRONES DETECTED OVER {name} BORDER CORRIDOR.",

                $"ALERT: MARTIAL LAW DECLARED IN {name} SOUTHERN PROVINCES."
            };

            return flashes[
                UnityEngine.Random.Range(0, flashes.Length)
            ];
        }
    }
}