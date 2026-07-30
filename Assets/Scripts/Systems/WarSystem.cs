using System;
using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace StateCraft.Systems
{
    public class WarSystem : IWarSystem, ISaveable
    {
        [System.Serializable]
        private class WarSystemSaveData
        {
            public List<string> countryWarStagesKeys = new List<string>();
            public List<WarStage> countryWarStagesValues = new List<WarStage>();

            public List<string> activeWars = new List<string>();

            public List<string> warDurationKeys = new List<string>();
            public List<int> warDurationValues = new List<int>();

            public List<string> frontProgressKeys = new List<string>();
            public List<float> frontProgressValues = new List<float>();

            public List<string> lastPhaseMonthKeys = new List<string>();
            public List<int> lastPhaseMonthValues = new List<int>();

            public List<string> ceaseFireCalledKeys = new List<string>();
            public List<bool> ceaseFireCalledValues = new List<bool>();

            public List<string> momentumVelocityKeys = new List<string>();
            public List<float> momentumVelocityValues = new List<float>();

            public List<string> surpriseEventCooldownKeys = new List<string>();
            public List<int> surpriseEventCooldownValues = new List<int>();

            public List<string> lastWarMonthCountedKeys = new List<string>();
            public List<int> lastWarMonthCountedValues = new List<int>();
        }

        public string ExportState()
        {
            var data = new WarSystemSaveData();

            foreach (var kvp in countryWarStages) {
                if (kvp.Key == null) continue;
                data.countryWarStagesKeys.Add(kvp.Key.countryName);
                data.countryWarStagesValues.Add(kvp.Value);
            }
            data.activeWars = activeWars.ToList();

            foreach (var kvp in warDuration) { data.warDurationKeys.Add(kvp.Key); data.warDurationValues.Add(kvp.Value); }
            foreach (var kvp in frontProgress) { data.frontProgressKeys.Add(kvp.Key); data.frontProgressValues.Add(kvp.Value); }
            foreach (var kvp in lastPhaseMonth) { data.lastPhaseMonthKeys.Add(kvp.Key); data.lastPhaseMonthValues.Add(kvp.Value); }
            foreach (var kvp in ceaseFireCalled) { data.ceaseFireCalledKeys.Add(kvp.Key); data.ceaseFireCalledValues.Add(kvp.Value); }
            foreach (var kvp in momentumVelocity) { data.momentumVelocityKeys.Add(kvp.Key); data.momentumVelocityValues.Add(kvp.Value); }
            foreach (var kvp in surpriseEventCooldown) { data.surpriseEventCooldownKeys.Add(kvp.Key); data.surpriseEventCooldownValues.Add(kvp.Value); }
            foreach (var kvp in _lastWarMonthCounted) {
                if (kvp.Key == null) continue;
                data.lastWarMonthCountedKeys.Add(kvp.Key.countryName);
                data.lastWarMonthCountedValues.Add(kvp.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void ImportState(string json)
        {
            var data = JsonUtility.FromJson<WarSystemSaveData>(json);
            if (data == null) return;

            countryWarStages.Clear();
            for (int i = 0; i < data.countryWarStagesKeys.Count; i++) {
                var c = GameManager.Instance.GetCountryByName(data.countryWarStagesKeys[i]);
                if (c != null) countryWarStages[c] = data.countryWarStagesValues[i];
            }

            activeWars = new HashSet<string>(data.activeWars);

            warDuration.Clear();
            for (int i = 0; i < data.warDurationKeys.Count; i++) warDuration[data.warDurationKeys[i]] = data.warDurationValues[i];

            frontProgress.Clear();
            for (int i = 0; i < data.frontProgressKeys.Count; i++) frontProgress[data.frontProgressKeys[i]] = data.frontProgressValues[i];

            lastPhaseMonth.Clear();
            for (int i = 0; i < data.lastPhaseMonthKeys.Count; i++) lastPhaseMonth[data.lastPhaseMonthKeys[i]] = data.lastPhaseMonthValues[i];

            ceaseFireCalled.Clear();
            for (int i = 0; i < data.ceaseFireCalledKeys.Count; i++) ceaseFireCalled[data.ceaseFireCalledKeys[i]] = data.ceaseFireCalledValues[i];

            momentumVelocity.Clear();
            for (int i = 0; i < data.momentumVelocityKeys.Count; i++) momentumVelocity[data.momentumVelocityKeys[i]] = data.momentumVelocityValues[i];

            surpriseEventCooldown.Clear();
            for (int i = 0; i < data.surpriseEventCooldownKeys.Count; i++) surpriseEventCooldown[data.surpriseEventCooldownKeys[i]] = data.surpriseEventCooldownValues[i];

            _lastWarMonthCounted.Clear();
            for (int i = 0; i < data.lastWarMonthCountedKeys.Count; i++) {
                var c = GameManager.Instance.GetCountryByName(data.lastWarMonthCountedKeys[i]);
                if (c != null) _lastWarMonthCounted[c] = data.lastWarMonthCountedValues[i];
            }
        }
        // ─── State ────────────────────────────────────────────────────────────
        private Dictionary<CountryDataSO, WarStage> countryWarStages = new Dictionary<CountryDataSO, WarStage>();
        private HashSet<string> activeWars   = new HashSet<string>();
        private Dictionary<string, int>   warDuration    = new Dictionary<string, int>();
        private Dictionary<string, float> frontProgress  = new Dictionary<string, float>(); // -100 to +100
        private Dictionary<string, int>   lastPhaseMonth = new Dictionary<string, int>();
        private Dictionary<string, bool>  ceaseFireCalled= new Dictionary<string, bool>();

        // Chaos tracking — real wars have wild momentum swings
        private Dictionary<string, float> momentumVelocity = new Dictionary<string, float>(); // rate of change
        private Dictionary<string, int>   surpriseEventCooldown = new Dictionary<string, int>();

        // Month-gate: ensures warTurnsActive counts calendar months, not 3-day ticks
        private Dictionary<CountryDataSO, int> _lastWarMonthCounted = new Dictionary<CountryDataSO, int>();

        // ─── Tuning ───────────────────────────────────────────────────────────
        private const int   MIN_WAR_MONTHS_BEFORE_PEACE  = 6;
        private const float SUPPLY_DRAIN_FUEL_PER_TURN   = 8f;
        private const float SUPPLY_DRAIN_AMMO_PER_TURN   = 6f;
        private const float SUPPLY_DRAIN_FOOD_PER_TURN   = 4f;

        // ─── Authentic African battle location flavour ─────────────────────────
        private static readonly string[] OffensiveVerbs  = {
            "launches armoured thrust near", "breaches defensive lines at", "advances on",
            "strikes positions at", "encircles forces near", "pushes through",
            "seizes key crossroads at", "outflanks defenders near", "storms fortifications at"
        };
        private static readonly string[] DefensiveVerbs  = {
            "repels major assault near", "holds the line at", "digs in at",
            "mounts fierce counter-attack at", "reinforces collapsing positions at",
            "trades ground for time near", "conducts fighting withdrawal from",
            "destroys bridges and delays enemy near"
        };
        private static readonly string[] StalemateVerbs  = {
            "exchanges artillery fire near", "contests broken ground near",
            "faces entrenched resistance at", "probes defences near",
            "engages in skirmishing near", "trades casualties near",
            "conducts raids near", "holds exposed salient at"
        };

        // Surprise / chaos events — the unpredictable face of war
        private static readonly string[] SurpriseEvents = {
            "AMBUSH", "SUPPLY CONVOY DESTROYED", "NIGHT RAID", "ARTILLERY BARRAGE",
            "FLANKING MANOEUVRE", "BRIDGE DEMOLISHED", "IED STRIKE", "DEFECTION",
            "COMMUNICATIONS BLACKOUT", "FOG OF WAR — FRIENDLY FIRE INCIDENT",
            "UNEXPECTED RESISTANCE", "ENEMY REINFORCEMENTS ARRIVE", "MASS SURRENDER"
        };

        // ─── Public API ───────────────────────────────────────────────────────
        
        // ─── GEOGRAPHICAL WAR PROGRESSION ───────────────────────────────────────
        public List<string> GetInvasionPath(CountryDataSO targetCountry)
        {
            var path = new List<string> { "Border Defense" };
            string capital = targetCountry.capitalCity;
            bool foundCapitalRegion = false;
            
            if (targetCountry.regions != null)
            {
                foreach (var region in targetCountry.regions)
                {
                    if (region.regionName == capital)
                    {
                        foundCapitalRegion = true;
                    }
                    else
                    {
                        path.Add(region.regionName);
                    }
                }
            }
            
            if (foundCapitalRegion || !string.IsNullOrEmpty(capital))
            {
                path.Add(!string.IsNullOrEmpty(capital) ? capital : "Capital");
            }
            else 
            {
                path.Add("Capital");
            }
            
            return path;
        }

        public string GetActiveCombatZone(CountryDataSO defender, float frontProgress)
        {
            var path = GetInvasionPath(defender);
            // frontProgress is -100 to 100. If we are analyzing defender, 
            // progress < 0 means enemy is pushing into defender.
            // Let's map 0 to -100 across the path elements.
            if (frontProgress >= 0) return "Border Defense";
            
            float step = 100f / path.Count;
            int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Abs(frontProgress) / step), 0, path.Count - 1);
            return path[index];
        }

        public void UpdateWarStatus(CountryDataSO country)
        {
            if (!countryWarStages.ContainsKey(country))
                countryWarStages[country] = WarStage.Peace;

            WarStage stage = countryWarStages[country];

            switch (stage)
            {
                case WarStage.Tension:
                    country.stability        -= 0.5f;
                    country.globalPrestige    = Mathf.Max(0, country.globalPrestige - 0.2f);
                    country.warWillToFight    = Mathf.Max(50f, country.warWillToFight - 0.5f);
                    break;

                case WarStage.Crisis:
                    country.stability        -= 1.5f;
                    country.globalPrestige    = Mathf.Max(0, country.globalPrestige - 1.0f);
                    country.warWillToFight    = Mathf.Max(30f, country.warWillToFight - 1.0f);
                    country.gdp              *= 0.998f;

                    if (UnityEngine.Random.value > 0.85f && country.regions?.Count > 0)
                    {
                        var r = country.regions[UnityEngine.Random.Range(0, country.regions.Count)];
                        country.roadLevel = Mathf.Max(0, country.roadLevel - 1);
                        NewsSystem.Instance?.PublishNews(
                            $"SABOTAGE: Infrastructure destroyed in {r.regionName}. Suspected {country.warEnemyName} operatives.");
                    }
                    break;

                case WarStage.Conflict:
                    ProcessActiveCombat(country);
                    break;
            }

            if (country.stability > 90f && stage == WarStage.Tension)
            {
                countryWarStages[country] = WarStage.Peace;
                country.warTurnsActive    = 0;
                country.warExhaustion     = 0f;
                country.warWillToFight    = 100f;
            }
        }

        // ─── Declare War ─────────────────────────────────────────────────────
        public void DeclareWar(CountryDataSO aggressor, CountryDataSO defender)
        {
            if (defender.hasNuclearWeapon)
            {
                var gm0 = GameManager.Instance;
                if (gm0 != null) gm0.worldTension = Mathf.Min(100f, gm0.worldTension + 25f);
                NewsSystem.Instance?.PublishNews(
                    $"NUCLEAR STANDOFF: {aggressor.countryName} backs down — {defender.countryName}'s nuclear arsenal provides absolute deterrence.");
                NotificationManager.Instance?.AddNotification(
                    $"DETERRENCE: {defender.countryName}'s nuclear capability prevents invasion. World Tension +25.", Color.red);
                return;
            }

            // ── Faction Friendly Fire Check ────────────────────────────────────────────
            if (aggressor.globalFaction == defender.globalFaction && 
                aggressor.globalFaction != "Independent" && 
                aggressor.globalFaction != "Non-Aligned")
            {
                var gm1 = GameManager.Instance;
                if (gm1?.playerCountry == aggressor)
                    UI.NotificationManager.Instance?.AddNotification(
                        $"CANNOT DECLARE WAR: {defender.countryName} is a member of your Faction ({aggressor.globalFaction}).",
                        UnityEngine.Color.red);
                return; // Block the war declaration completely
            }

            countryWarStages[aggressor] = WarStage.Conflict;
            countryWarStages[defender]  = WarStage.Conflict;

            string key = WarKey(aggressor.countryName, defender.countryName);
            if (!activeWars.Contains(key))
            {
                activeWars.Add(key);
                warDuration[key]   = 0;
                // Start front slightly randomised — opening moves matter
                frontProgress[key] = UnityEngine.Random.Range(-10f, 10f);
                momentumVelocity[key] = 0f;
                lastPhaseMonth[key]= 0;
                surpriseEventCooldown[key] = 0;
                var gm4 = GameManager.Instance;
                if (gm4?.Relations != null)
                {
                    int month4 = gm4.TurnSystem?.CurrentTurn ?? 0;
                    gm4.Relations.Adjust(aggressor.countryName, defender.countryName, -40f,
                        $"{aggressor.countryName} declared war", month4);
                }
            }

            foreach (var c in new[] { aggressor, defender })
            {
                c.warTurnsActive  = 0;
                c.warExhaustion   = 0f;
                c.warWillToFight  = 100f;
                c.totalKIA        = 0;
                c.totalWounded    = 0;
                _lastWarMonthCounted.Remove(c);
            }

            var gm = GameManager.Instance;

            // ── Faction Mutual Defence Obligations ──
            if (gm?.allCountries != null)
            {
                foreach (var ally in gm.allCountries)
                {
                    if (ally == defender || ally == aggressor) continue;
                    
                    bool inSameFaction = (ally.globalFaction == defender.globalFaction && 
                                          defender.globalFaction != "Independent" && 
                                          defender.globalFaction != "Non-Aligned");

                    if (!inSameFaction) continue;

                    int reinforceInfantry = Mathf.RoundToInt(ally.unitInfantry * 0.10f);
                    int reinforceTanks    = Mathf.RoundToInt(ally.unitTanks    * 0.08f);

                    if (reinforceInfantry == 0 && reinforceTanks == 0)
                    {
                        string oldFaction = ally.globalFaction;
                        ally.globalFaction = "Independent";
                        ally.globalPrestige = Mathf.Max(0f, ally.globalPrestige - 15f);

                        NewsSystem.Instance?.PublishNews(
                            $"FACTION EXPULSION: {ally.countryName} failed to provide military support to {defender.countryName} and has been expelled from the {oldFaction}.");

                        if (gm.playerCountry == ally)
                        {
                            UI.NotificationManager.Instance?.AddNotification(
                                $"DISGRACED: You failed to defend {defender.countryName} and have been expelled from the {oldFaction}.",
                                new Color(1f, 0.32f, 0.32f));
                        }
                        continue;
                    }

                    defender.unitInfantry += reinforceInfantry;
                    defender.unitTanks    += reinforceTanks;
                    ally.unitInfantry     -= reinforceInfantry;
                    ally.unitTanks        -= reinforceTanks;
                    aggressor.globalPrestige = Mathf.Max(0f, aggressor.globalPrestige - 5f);

                    NewsSystem.Instance?.PublishNews(
                        $"COALITION RESPONSE: {ally.countryName} honours defence pact with {defender.countryName}. " +
                        $"{reinforceInfantry:N0} troops deployed.");

                    if (gm.playerCountry == defender)
                        NotificationManager.Instance?.AddNotification(
                            $"🛡 COALITION ACTIVATED: {ally.countryName} sends {reinforceInfantry:N0} troops and {reinforceTanks} tanks to your defence!",
                            new Color(0f, 0.85f, 1f));

                    if (gm.playerCountry == aggressor)
                        NotificationManager.Instance?.AddNotification(
                            $"⚠ COALITION WARNING: {ally.countryName} enters the war on {defender.countryName}'s side.",
                            new Color(1f, 0.5f, 0f));
                }
            }

            float mobCost = 0.5f;
            if (gm?.playerCountry == aggressor)
            {
                gm.playerBudget = Mathf.Max(0f, gm.playerBudget - mobCost);
                aggressor.treasury = gm.playerBudget;
            }

            aggressor.gdp      *= 0.94f;
            defender.gdp       *= 0.92f;
            aggressor.stability = Mathf.Max(5f, aggressor.stability - 8f);
            defender.stability  = Mathf.Max(5f, defender.stability  - 15f);
            aggressor.globalPrestige = Mathf.Max(0f, aggressor.globalPrestige - 10f);
            if (gm != null) gm.worldTension = Mathf.Min(100f, gm.worldTension + 30f);

            // Sever trade
            if (gm?.DiplomacySystem is DiplomacySystem diplo)
            {
                for (int i = diplo.ActiveDeals.Count - 1; i >= 0; i--)
                {
                    var d = diplo.ActiveDeals[i];
                    if ((d.sellerCountry == aggressor.countryName && d.buyerCountry == defender.countryName) ||
                        (d.sellerCountry == defender.countryName && d.buyerCountry == aggressor.countryName))
                        diplo.ActiveDeals.RemoveAt(i);
                }
            }

            if (gm?.playerCountry == aggressor)
            {
                // Force posture to OFFENSIVE
                aggressor.militaryDoctrine = 2;

                // Inject event into EventSystem
                if (gm?.EventSystem is EventSystem evSys)
                {
                    var warEv = UnityEngine.ScriptableObject.CreateInstance<GameEventSO>();
                    warEv.eventTitle = $"War Initiated: {defender.countryName}";
                    warEv.description = $"We have formally declared war on {defender.countryName}. The international community is reacting, and our armed forces await their marching orders.";
                    warEv.severity = "Critical";
                    warEv.timeRemainingDays = 30;
                    warEv.choices = new System.Collections.Generic.List<GameEventChoice> {
                        new GameEventChoice { choiceText = "Mobilize Forces", stabilityEffect = 0f }
                    };
                    evSys.InjectUrgentEvent(aggressor, warEv);
                }

                InboxSystem.Instance?.SendMessage(aggressor, "CHIEF OF DEFENCE STAFF",
                    "WAR DECLARED — COMMAND YOUR FORCES",
                    $"Sir,\n\n" +
                    $"We have declared war on {defender.countryName}. Our forces are on standby.\n\n" +
                    $"——— IMMEDIATE ORDERS ———\n" +
                    $"→ MILITARY → OPERATIONS → Deploy troops to {defender.countryName} front NOW\n" +
                    $"  (Standard infantry: {aggressor.unitInfantry:N0} available · Transit: ~2 months)\n" +
                    $"  (RDF operators: faster deployment, higher battlefield effectiveness)\n\n" +
                    $"→ MILITARY → LOGISTICS → Top up ammo and fuel before advance\n" +
                    $"→ EVENTS → DECISIONS → Watch for field reports and respond\n\n" +
                    $"——— CURRENT STATUS ———\n" +
                    $"Infantry available: {aggressor.unitInfantry:N0}\n" +
                    $"Tanks: {aggressor.unitTanks} · Fighters: {aggressor.unitFighters}\n" +
                    $"Treasury: {UiFormat.Money(gm.playerBudget, aggressor.currencySymbol)} · Mobilisation cost: $0.5B\n" +
                    $"World Tension: {gm.worldTension:F0}/100\n\n" +
                    $"NOTE: Troops not deployed within 2 months will leave your border undefended.",
                    "Critical");

                NotificationManager.Instance?.AddNotification(
                    $"WAR DECLARED on {defender.countryName}. Open MILITARY → OPERATIONS to deploy troops NOW.",
                    Color.red);
            }
            else if (gm?.playerCountry == defender)
            {
                // 1. Force posture to OFFENSIVE
                defender.militaryDoctrine = 2; // Offensive / War posture
                
                // 2. Trigger Hard-Interrupt Modal
                if (StateCraft.UI.MainDashboardToolkitController.Instance != null)
                {
                    StateCraft.UI.MainDashboardToolkitController.Instance.ShowWarDeclaredModal(aggressor);
                }

                // 3. Inject Urgent Event into EventSystem
                if (gm?.EventSystem is EventSystem evSys)
                {
                    var warEv = UnityEngine.ScriptableObject.CreateInstance<GameEventSO>();
                    warEv.eventTitle = $"War Declared by {aggressor.countryName}";
                    warEv.description = $"{aggressor.countryName} has launched an unprovoked military offensive against us. Our forces are mobilizing.";
                    warEv.severity = "Critical";
                    warEv.timeRemainingDays = 30;
                    warEv.choices = new System.Collections.Generic.List<GameEventChoice> {
                        new GameEventChoice { choiceText = "Mobilize Forces", stabilityEffect = 2f }
                    };
                    evSys.InjectUrgentEvent(defender, warEv);
                }

                InboxSystem.Instance?.SendMessage(defender, "CHIEF OF DEFENCE STAFF",
                    "🚨 UNDER ATTACK — DEFEND NOW",
                    $"Sir,\n\n" +
                    $"{aggressor.countryName} has declared war on us! Our borders are under threat.\n\n" +
                    $"——— IMMEDIATE ORDERS ———\n" +
                    $"→ MILITARY → OPERATIONS → Launch DEFEND BORDERS immediately\n" +
                    $"  (Deploy all infantry and armour — do not wait)\n" +
                    $"→ MILITARY → OPERATIONS → RDF RAPID RESPONSE if available\n" +
                    $"  (RDF deploys faster and hits harder — use them on the front line first)\n" +
                    $"→ DIPLOMACY → Request allied reinforcements from friendly nations\n\n" +
                    $"——— THREAT ASSESSMENT ———\n" +
                    $"Enemy infantry: ~{aggressor.unitInfantry:N0} · Tanks: {aggressor.unitTanks}\n" +
                    $"Our infantry: {defender.unitInfantry:N0} · Stability: {defender.stability:F0}%\n\n" +
                    $"Every month without troops deployed weakens our position.",
                    "Critical");

                NotificationManager.Instance?.AddNotification(
                    $"🚨 UNDER ATTACK: {aggressor.countryName} launches offensive! " +
                    $"Open MILITARY → OPERATIONS and deploy defenders NOW.",
                    Color.red);
            }

            NewsSystem.Instance?.PublishNews(
                $"WAR DECLARED: {aggressor.countryName} launches military offensive against {defender.countryName}. " +
                $"Markets crash. Borders sealed. World Tension: {gm?.worldTension ?? 0:F0}/100.");
            NewsSystem.Instance?.PublishNews(
                $"BREAKING: {defender.countryName} declares state of national emergency. Armed forces mobilised.");
        }

        // ─── War Stage Access ─────────────────────────────────────────────────
        public WarStage GetWarStage(CountryDataSO country)
        {
            return countryWarStages.TryGetValue(country, out WarStage s) ? s : WarStage.Peace;
        }

        public void SetWarStage(CountryDataSO country, WarStage stage)
        {
            countryWarStages[country] = stage;
        }

        // ─── Peace ────────────────────────────────────────────────────────────
        public void ProposePeace(CountryDataSO proposer, CountryDataSO target)
        {
            string key = WarKey(proposer.countryName, target.countryName);
            if (!activeWars.Contains(key)) return;

            int turns = warDuration.ContainsKey(key) ? warDuration[key] : 0;
            if (turns < MIN_WAR_MONTHS_BEFORE_PEACE)
            {
                NotificationManager.Instance?.AddNotification(
                    $"CEASEFIRE DENIED: War must last at least {MIN_WAR_MONTHS_BEFORE_PEACE} months first.", Color.red);
                return;
            }

            bool mutuallyTired  = proposer.warExhaustion >= 60f && target.warExhaustion >= 60f;
            bool proposerBroken = proposer.warWillToFight <= 10f || proposer.stability <= 15f;
            bool targetBroken   = target.warWillToFight   <= 10f || target.stability   <= 15f;

            if (mutuallyTired || proposerBroken || targetBroken)
            {
                string outcome = proposerBroken ? $"{target.countryName} Victory" :
                                 targetBroken   ? $"{proposer.countryName} Victory" :
                                                  "Negotiated Ceasefire";
                EndWar(proposer, target, outcome);
            }
            else
            {
                NotificationManager.Instance?.AddNotification(
                    $"PEACE REJECTED: {target.countryName} is not exhausted enough to accept terms. " +
                    $"Their exhaustion: {target.warExhaustion:F0}% — needs 60%+ to accept.", Color.yellow);
            }
        }

        public void ProposeReparationsPeace(CountryDataSO proposer, CountryDataSO target)
        {
            string key = WarKey(proposer.countryName, target.countryName);
            if (!activeWars.Contains(key)) return;

            if (target.warExhaustion > 60f || target.warWillToFight < 20f || target.stability < 20f)
            {
                if (GameManager.Instance?.DiplomacySystem is DiplomacySystem diplo)
                {
                    TradeResourceType tradeRes = TradeResourceType.Minerals;
                    if (target.resources.Any(r => r.type == MineralType.Uranium))
                        tradeRes = TradeResourceType.Uranium;
                    diplo.SignTradeDeal(target, proposer, tradeRes, 0f);

                    InboxSystem.Instance?.SendMessage(proposer, "DIPLOMATIC CORPS",
                        "Reparations Treaty Signed",
                        $"{target.countryName} has surrendered and signed a forced trade pact yielding {tradeRes}.",
                        "Important");
                }
                EndWar(proposer, target, $"{proposer.countryName} Victory — Capitulation");
            }
            else
            {
                NotificationManager.Instance?.AddNotification(
                    $"DEMAND REJECTED: {target.countryName} refuses unconditional surrender. " +
                    $"Break their will to fight below 20% first.", Color.red);
            }
        }

        // ─── CORE COMBAT ──────────────────────────────────────────────────────
        private void ProcessActiveCombat(CountryDataSO country)
        {
            var enemies = GetAllEnemies(country);
            if (enemies.Count == 0) { SetWarStage(country, WarStage.Peace); return; }

            // ── Only count a new war month when the calendar month actually turns ──
            // TurnSystem fires every 3 days AND at month-end; guard with a per-country gate.
            var ts = GameManager.Instance?.TurnSystem as TurnSystem;
            int calendarMonth = ts != null ? ts.CurrentTurn : 0; // CurrentTurn increments once per month
            bool isNewMonth = false;
            if (!_lastWarMonthCounted.ContainsKey(country) || _lastWarMonthCounted[country] != calendarMonth)
            {
                _lastWarMonthCounted[country] = calendarMonth;
                country.warTurnsActive++;
                isNewMonth = true;
            }
            var gm      = GameManager.Instance;
            bool isPlayer = (country == gm?.playerCountry);

            // ── Per-tick lightweight checks (run every 3-day tick) ──
            // (coalition maintenance runs every tick so ongoing costs stay proportional)
            HandleCoalitionMaintenance(country, gm, isPlayer);

            // ── Everything below is MONTHLY — skip if this tick isn't a new month ──
            if (!isNewMonth) return;

            ProcessSupplyDrain(country, isPlayer);

            float generalBonus = CalculateGeneralBonus(country);

            float totalInfantryKIA = 0;
            float totalTanksLost   = 0;
            float totalAPCsLost    = 0;
            float totalFightersLost= 0;

            foreach (var enemy in enemies)
            {
                float frontTanksLost = 0f;
                float frontAPCsLost = 0f;
                float frontFightersLost = 0f;
                System.Collections.Generic.List<string> frontCasualtyCauses = new System.Collections.Generic.List<string>();

                string key = WarKey(country.countryName, enemy.countryName);
                if (warDuration.ContainsKey(key)) warDuration[key]++; // safe — inside isNewMonth block
                int warMonth = warDuration.ContainsKey(key) ? warDuration[key] : 1;

                // Initialise momentum tracking
                if (!momentumVelocity.ContainsKey(key)) momentumVelocity[key] = 0f;
                if (!surpriseEventCooldown.ContainsKey(key)) surpriseEventCooldown[key] = 0;
                if (surpriseEventCooldown[key] > 0) surpriseEventCooldown[key]--;

                // ── PHASE SYSTEM ──────────────────────────────────────────
                string warPhase;
                if      (warMonth <= 2)  warPhase = "MOBILISATION";
                else if (warMonth <= 6)  warPhase = "OFFENSIVE";
                else if (warMonth <= 18) warPhase = "ATTRITION";
                else                     warPhase = "GRINDING WAR";

                if (isPlayer && lastPhaseMonth.ContainsKey(key) && lastPhaseMonth[key] != warMonth)
                {
                    string prevPhase = lastPhaseMonth[key] <= 2 ? "MOBILISATION" :
                                       lastPhaseMonth[key] <= 6 ? "OFFENSIVE" :
                                       lastPhaseMonth[key] <= 18 ? "ATTRITION" : "GRINDING WAR";
                    if (prevPhase != warPhase)
                        SendPhaseTransitionBriefing(country, enemy, warPhase, warMonth, isPlayer);
                }
                lastPhaseMonth[key] = warMonth;

                // ── FORCE CALCULATION ──────────────────────────────────────
                float myPower    = GetFrontlinePowerAgainst(country, enemy.countryName, isPlayer) * generalBonus;
                float enemyPower = GetFrontlinePowerAgainst(enemy, country.countryName, enemy == gm?.playerCountry);

                float phaseModifier = warPhase == "MOBILISATION" ? 0.3f :
                                      warPhase == "OFFENSIVE"    ? 1.2f :
                                      warPhase == "ATTRITION"    ? 1.0f :
                                                                    1.5f;

                // ── AIR SUPERIORITY ────────────────────────────────────────
                bool hasAirCover       = (country.unitFighters + country.airForceStats.equipment) > 10;
                bool enemyAirDominance = (enemy.unitFighters + enemy.airForceStats.equipment) >
                                         (country.unitFighters + country.airForceStats.equipment) * 1.5f;
                float airPenalty       = (!hasAirCover || enemyAirDominance) ? 1.4f : 1.0f;

                if (isPlayer && enemyAirDominance && country.warTurnsActive % 3 == 0)
                    NotificationManager.Instance?.AddNotification(
                        $"✈ AIR INFERIORITY: {enemy.countryName} controls the airspace. " +
                        $"Ground troops taking +40% casualties. Deploy fighters via Military → Operations.",
                        new Color(1f, 0.5f, 0f));

                // ── CASUALTY CALCULATION ───────────────────────────────────
                float powerRatio  = enemyPower > 0 ? (enemyPower / Mathf.Max(myPower, 1f)) : 0.1f;
                float baseCasRate = Mathf.Clamp(powerRatio * 0.012f * airPenalty * phaseModifier, 0.001f, 0.08f);

                if (country.reserveAmmo < 20f) baseCasRate *= 1.8f;
                if (country.reserveFuel < 20f) baseCasRate *= 1.5f;
                if (country.reserveFood < 15f) baseCasRate *= 1.2f;

                float exhaustionMod = 1f - (country.warExhaustion / 300f);
                baseCasRate *= Mathf.Clamp(exhaustionMod, 0.6f, 1f);

                // ── REALISTIC FRONT MOMENTUM ───────────────────────────────
                // Instead of a simple ratio shift, use an inertia system:
                // momentum builds up and creates genuine swings, not smooth lines
                if (!frontProgress.ContainsKey(key)) frontProgress[key] = 0f;

                float powerDiff = myPower - enemyPower;
                float totalPower = Mathf.Max(myPower + enemyPower, 1f);

                // Base shift: power advantage drives momentum, but slowly
                float baseShift = Mathf.Clamp(powerDiff / totalPower * 4f, -5f, 5f);

                // WAR FRICTION: Random events disrupt expected outcomes
                // Real wars rarely advance at a constant rate
                float friction = UnityEngine.Random.Range(-3.5f, 3.5f);

                // Terrain / weather / logistics disruption (more variance in long wars)
                float variance = warMonth > 6 ? UnityEngine.Random.Range(-2f, 2f) : 0f;

                // Momentum has inertia — hard to reverse a trend suddenly
                float targetVelocity = baseShift + friction + variance;
                momentumVelocity[key] = Mathf.Lerp(momentumVelocity[key], targetVelocity, 0.35f);

                // Apply momentum to front progress
                float progressShift = momentumVelocity[key];
                frontProgress[key] = Mathf.Clamp(frontProgress[key] + progressShift, -100f, 100f);

                // ── SURPRISE EVENTS — the chaos of real war ────────────────
                // Every 2-5 turns there's a chance of an unexpected event
                if (surpriseEventCooldown[key] <= 0 && warMonth > 1 && UnityEngine.Random.value < 0.25f)
                {
                    float surpriseEquipLost = 0;
                    ProcessSurpriseEvent(country, enemy, key, isPlayer, ref totalInfantryKIA,
                        ref surpriseEquipLost, baseCasRate, myPower, enemyPower);
                    totalTanksLost += surpriseEquipLost * 0.6f;
                    totalAPCsLost += surpriseEquipLost * 0.4f;
                    frontTanksLost += surpriseEquipLost * 0.6f;
                    frontAPCsLost += surpriseEquipLost * 0.4f;
                    frontCasualtyCauses.Add("Unexpected Events (Ambushes / Attrition)");
                    surpriseEventCooldown[key] = UnityEngine.Random.Range(2, 5);
                }

                // ── APPLY CASUALTIES TO OPERATIONS ────────────────────────
                int frontCasualties = 0;
                bool hasOpsOnFront  = false;

                if (country.activeOperations != null)
                {
                    foreach (var op in country.activeOperations.Where(o => o.regionName == enemy.countryName))
                    {
                        if (op.isDecommissioning) continue;
                        if (op.turnsToDeploy > 0)
                        {
                            op.turnsToDeploy--;
                            UnityEngine.Debug.Log($"[WAR DEPLOYMENT TICK] {op.opName} ({enemy.countryName} Front): {op.turnsToDeploy} month(s) remaining until theatre arrival.");
                            if (isPlayer)
                            {
                                NotificationManager.Instance?.AddNotification($"WAR FRONT DEPLOYMENT [{op.opName.ToUpper()}]: {op.turnsToDeploy} month(s) remaining.", Color.cyan);
                            }
                            continue;
                        }

                        hasOpsOnFront = true;

                        if (op.type == "STRATEGIC BOMBARDMENT")
                        {
                            ProcessBombardment(op, country, enemy, isPlayer);
                            continue;
                        }

                        int troopsLost   = Mathf.RoundToInt(op.deployedTroops   * baseCasRate);
                        int tanksLost    = Mathf.RoundToInt(op.deployedTanks    * baseCasRate * 0.5f);
                        int apcsLost     = Mathf.RoundToInt(op.deployedAPCs     * baseCasRate * 0.4f);
                        int fightersLost = Mathf.RoundToInt(op.deployedFighters * baseCasRate * 0.3f);

                        if (troopsLost > 0 && !frontCasualtyCauses.Contains("Frontline Infantry Combat")) frontCasualtyCauses.Add("Frontline Infantry Combat");
                        if ((tanksLost > 0 || apcsLost > 0) && !frontCasualtyCauses.Contains("Armoured Exhanges")) frontCasualtyCauses.Add("Armoured Exchanges");

                        // RDF is more effective: reduces casualty rate and gets a combat power bonus
                        bool hasRDF = op.deployedRDF > 0;
                        float rdfEffectivenessBonus = hasRDF ? Mathf.Clamp(op.deployedRDF / 500f, 0f, 1f) : 0f;
                        troopsLost = Mathf.RoundToInt(troopsLost * (1f - rdfEffectivenessBonus * 0.6f)); // RDF reduces casualties by up to 60%

                        int rdfLost = hasRDF ? Mathf.RoundToInt(op.deployedRDF * baseCasRate * 0.4f) : 0; // RDF takes fewer losses too

                        // --- Active Air-to-Air Combat & Dogfighting ---
                        int enemyFighters = enemy.unitFighters + enemy.airForceStats.equipment;
                        int ourFighters = country.unitFighters + country.airForceStats.equipment;
                        
                        // If both have fighters, they dogfight. The outnumbered side takes exponentially more losses.
                        if (op.deployedFighters > 0 && enemyFighters > 0)
                        {
                            float fighterRatio = (float)op.deployedFighters / Mathf.Max(1f, enemyFighters * 0.2f); // Assuming enemy commits 20% to this front
                            if (fighterRatio < 1f) fightersLost += Mathf.RoundToInt(op.deployedFighters * 0.15f); // Outnumbered = heavy losses
                            else fightersLost += Mathf.RoundToInt(op.deployedFighters * 0.05f); // Dominating = light losses
                            
                            if (fightersLost > 0 && !frontCasualtyCauses.Contains("Air-to-Air Dogfights")) frontCasualtyCauses.Add("Air-to-Air Dogfights");
                        }

                        // --- Close Air Support (CAS) ---
                        // If we have air dominance, our fighters bomb enemy tanks/APCs
                        bool weHaveAirDominance = op.deployedFighters > 20 && op.deployedFighters > enemyFighters;
                        if (weHaveAirDominance)
                        {
                            // We don't deduct enemy tanks here because the enemy's loop will calculate their own losses.
                            // But we DO boost our front momentum and reduce our tank losses.
                            tanksLost = Mathf.RoundToInt(tanksLost * 0.4f);
                            apcsLost = Mathf.RoundToInt(apcsLost * 0.4f);
                        }

                        op.deployedTroops   = Mathf.Max(0, op.deployedTroops   - troopsLost);
                        op.deployedTanks    = Mathf.Max(0, op.deployedTanks    - tanksLost);
                        op.deployedAPCs     = Mathf.Max(0, op.deployedAPCs     - apcsLost);
                        op.deployedFighters = Mathf.Max(0, op.deployedFighters - fightersLost);
                        if (hasRDF) op.deployedRDF = Mathf.Max(0, op.deployedRDF - rdfLost);

                        country.unitTanks    = Mathf.Max(0, country.unitTanks    - tanksLost);
                        country.unitAPCs     = Mathf.Max(0, country.unitAPCs     - apcsLost);
                        country.unitFighters = Mathf.Max(0, country.unitFighters - fightersLost);

                        frontCasualties      += troopsLost;
                        totalInfantryKIA     += troopsLost;
                        totalTanksLost       += tanksLost;
                        totalAPCsLost        += apcsLost;
                        totalFightersLost    += fightersLost;
                        frontTanksLost       += tanksLost;
                        frontAPCsLost        += apcsLost;
                        frontFightersLost    += fightersLost;

                        if (enemyAirDominance && UnityEngine.Random.value < 0.50f)
                        {
                            // Enemy Air Raid explicitly kills tanks and APCs
                            float raidEquipLost = 0;
                            ProcessEnemyAirRaid(op, country, enemy, ref totalInfantryKIA, ref raidEquipLost, isPlayer);
                            totalTanksLost += raidEquipLost * 0.6f;
                            totalAPCsLost += raidEquipLost * 0.4f;
                            frontTanksLost += raidEquipLost * 0.6f;
                            frontAPCsLost += raidEquipLost * 0.4f;
                            if (!frontCasualtyCauses.Contains("Enemy Close Air Support (Bombing)")) frontCasualtyCauses.Add("Enemy Close Air Support (Bombing)");
                        }

                        // RDF overpowered — request reinforcements
                        if (isPlayer && hasRDF && op.deployedRDF < 200 && rdfLost > 0)
                        {
                            InboxSystem.Instance?.SendMessage(country,
                                $"RDF COMMAND — {enemy.countryName.ToUpper()} FRONT",
                                "🔴 RDF UNIT CRITICALLY LOW — SEND REINFORCEMENTS",
                                $"Sir, our RDF operators on the {enemy.countryName} front are down to {op.deployedRDF:N0} men.\n\n" +
                                $"The enemy is concentrating fire on our special forces unit. Without reinforcement they will be overrun within 1-2 months.\n\n" +
                                $"——— IMMEDIATE OPTIONS ———\n" +
                                $"→ MILITARY → OPERATIONS → select '{op.opName}' → REINFORCE with standard infantry\n" +
                                $"→ Or deploy a second operation alongside to absorb pressure\n\n" +
                                $"NOTE: Each 500 RDF on the field reduces your total casualties by up to 60%. Losing them is costly.",
                                "Critical");
                        }

                        if (op.deployedTroops < 500 && troopsLost > 0 && isPlayer)
                        {
                            InboxSystem.Instance?.SendMessage(country,
                                $"GENERAL — {enemy.countryName.ToUpper()} FRONT",
                                "🔴 FRONTLINE COLLAPSING — REINFORCEMENTS CRITICAL",
                                $"Sir, we are down to {op.deployedTroops:N0} men on the {enemy.countryName} front.\n\n" +
                                $"At current casualty rates, total collapse is {(baseCasRate > 0 ? Mathf.RoundToInt(op.deployedTroops / (op.deployedTroops * baseCasRate + 1f)) : 0)} months away.\n\n" +
                                $"——— IMMEDIATE ACTION ———\n" +
                                $"→ MILITARY → OPERATIONS → select '{op.opName}' → REINFORCE\n" +
                                $"  Send at least 5,000 troops immediately or the line will break.",
                                "Critical");
                        }
                    }
                }

                if (!hasOpsOnFront)
                {
                    int spillover = Mathf.RoundToInt(country.unitInfantry * baseCasRate * 0.15f);
                    country.unitInfantry  = Mathf.Max(0, country.unitInfantry - spillover);
                    totalInfantryKIA     += spillover;

                    if (isPlayer && warMonth > 2)
                        NotificationManager.Instance?.AddNotification(
                            $"⚠ NO TROOPS DEPLOYED: Enemy forces advancing unopposed! " +
                            $"Open MILITARY → OPERATIONS and deploy to {enemy.countryName} front immediately.",
                            Color.red);
                }

                // ── NAMED BATTLE REPORTS & CASUALTY TICKER ──────────────────────────────────
                if (frontCasualties > 0 || frontTanksLost > 0 || frontAPCsLost > 0 || frontFightersLost > 0)
                {
                    int enemyCasualtiesEst = Mathf.RoundToInt(frontCasualties * (myPower / Mathf.Max(enemyPower, 1f)));

                    int tanks = Mathf.RoundToInt(frontTanksLost);
                    int apcs = Mathf.RoundToInt(frontAPCsLost);
                    int fighters = Mathf.RoundToInt(frontFightersLost);

                    var myEqList = new System.Collections.Generic.List<string>();
                    if (tanks > 0) myEqList.Add($"{tanks} Tank{(tanks > 1 ? "s" : "")}");
                    if (apcs > 0) myEqList.Add($"{apcs} APC{(apcs > 1 ? "s" : "")}");
                    if (fighters > 0) myEqList.Add($"{fighters} Fighter{(fighters > 1 ? "s" : "")}");

                    int enemyTanksEst = Mathf.RoundToInt(tanks * (myPower / Mathf.Max(enemyPower, 1f)));
                    int enemyApcsEst = Mathf.RoundToInt(apcs * (myPower / Mathf.Max(enemyPower, 1f)));

                    // Calculate civilian casualties if fighting inside country
                    string combatZone = GetActiveCombatZone(country, frontProgress.ContainsKey(key) ? frontProgress[key] : 0f);
                    if (combatZone != "Border Defense") {
                        int civCas = Mathf.RoundToInt(totalInfantryKIA * 0.2f * UnityEngine.Random.Range(0.5f, 1.5f));
                        country.totalCivilianCasualties += civCas;
                        
                        // Warn player
                        if (isPlayer && civCas > 0) {
                            NotificationManager.Instance?.AddNotification($"CIVILIAN CASUALTIES: {civCas} civilians killed in {combatZone} crossfire.", UnityEngine.Color.red);
                        }
                        NewsSystem.Instance?.PublishNews($"TRAGEDY: {civCas:N0} civilians reported dead in {country.countryName} ({combatZone}) crossfire.");
                    }

                    var enemyEqList = new System.Collections.Generic.List<string>();
                    if (enemyTanksEst > 0) enemyEqList.Add($"{enemyTanksEst} Tank{(enemyTanksEst > 1 ? "s" : "")}");
                    if (enemyApcsEst > 0) enemyEqList.Add($"{enemyApcsEst} APC{(enemyApcsEst > 1 ? "s" : "")}");

                    string myEquipStr = myEqList.Count > 0 ? $", {string.Join(", ", myEqList)} Lost" : "";
                    string enemyEquipStr = enemyEqList.Count > 0 ? $", {string.Join(", ", enemyEqList)} Lost" : "";

                    NewsSystem.Instance?.PublishNews(
                        $"FRONT CASUALTY REPORT [{country.countryName.ToUpper()} vs {enemy.countryName.ToUpper()} FRONT]: " +
                        $"{country.countryName}: {frontCasualties:N0} Personnel KIA{myEquipStr} | {enemy.countryName}: {enemyCasualtiesEst:N0} Personnel KIA{enemyEquipStr}.", true);
                }

                if (isPlayer && warMonth > 2)
                    GenerateBattleReport(country, enemy, key, frontCasualties,
                        totalInfantryKIA, myPower, enemyPower, warPhase, isPlayer, frontTanksLost, frontAPCsLost, frontFightersLost, frontCasualtyCauses);
            }

            // ── KIA / WOUNDED ──────────────────────────────────────────────
            int newKIA     = Mathf.RoundToInt(totalInfantryKIA * 0.30f);
            int newWounded = Mathf.RoundToInt(totalInfantryKIA * 0.70f);
            country.totalKIA     += newKIA;
            country.totalWounded += newWounded;
            country.armyStats.personnel = Mathf.Max(0, country.armyStats.personnel);
            country.unitInfantry        = Mathf.Max(0, country.unitInfantry);
            
            if (country.militaryState != null)
                country.militaryState.army.infantry = country.unitInfantry;

            // ── EXHAUSTION & WILL ──────────────────────────────────────────
            float exhaustionRise = 1.8f * enemies.Count;
            if (country.reserveAmmo < 30f) exhaustionRise += 2f;
            if (country.reserveFuel < 30f) exhaustionRise += 1.5f;
            if (newKIA > 200)              exhaustionRise += 2.5f;
            if (country.warTurnsActive > 18) exhaustionRise += 1f;
            country.warExhaustion = Mathf.Min(100f, country.warExhaustion + exhaustionRise);

            float willDrain = 1.5f;
            if (country.warExhaustion > 50f) willDrain += 1.5f;
            if (newKIA > 500)               willDrain += 3f;
            if (country.totalKIA > 10000)   willDrain += 2f;
            country.warWillToFight = Mathf.Max(0f, country.warWillToFight - willDrain);

            // ── STABILITY & ECONOMY ────────────────────────────────────────
            country.warFatigueStabilityDrain = Mathf.Clamp(country.totalKIA / 4000f, 0f, 4f);
            country.stability = Mathf.Max(5f,
                country.stability - (1.2f * enemies.Count) - country.warFatigueStabilityDrain);
            if (country.warExhaustion > 80f) country.stability -= 3f;
            country.gdp          *= 0.987f;
            country.domesticDebt += country.gdp * 0.025f;

            if (gm != null) gm.worldTension = Mathf.Max(0f, gm.worldTension - 0.1f);

            // ── LIVE WAR BULLETIN — fires every turn so combat never goes silent ──
            if (isPlayer)
            {
                // Build a compact front-line summary for the notification
                string frontBulletin = "";
                foreach (var en in enemies)
                {
                    string wk = WarKey(country.countryName, en.countryName);
                    float  fp = frontProgress.ContainsKey(wk) ? frontProgress[wk] : 0f;
                    string st = fp > 20f ? "ADVANCING ▲" : fp < -20f ? "RETREATING ▼" : "STALEMATE ◼";
                    frontBulletin += $" | vs {en.countryName}: {st}";
                }
                string supplyFlag = "";
                if (country.reserveAmmo < 30f) supplyFlag += " ⚠AMMO";
                if (country.reserveFuel < 20f) supplyFlag += " ⚠FUEL";
                if (country.reserveFood < 15f) supplyFlag += " ⚠FOOD";

                Color bulletinColor = supplyFlag.Length > 0
                    ? new Color(1f, 0.5f, 0f)          // orange when supplies critical
                    : new Color(0.9f, 0.75f, 0.25f);   // gold during normal combat

                NotificationManager.Instance?.AddNotification(
                    $"⚔ WAR UPDATE — Month {country.warTurnsActive}" +
                    $" | KIA this month: {newKIA:N0} | Wounded: {newWounded:N0}" +
                    frontBulletin + supplyFlag,
                    bulletinColor);
            }

            // ── INBOX REPORT every 3 turns ──
            if (isPlayer && country.warTurnsActive % 3 == 0)
                SendMonthlyWarReport(country, enemies, newKIA, newWounded, totalTanksLost, totalAPCsLost, totalFightersLost, gm);

            // ── EXHAUSTION WARNINGS ────────────────────────────────────────
            if (isPlayer)
            {
                if (country.warExhaustion >= 90f && country.warExhaustion < 92f)
                {
                    NotificationManager.Instance?.AddNotification(
                        "🚨 BREAKING POINT: War exhaustion critical! Troops refusing orders. " +
                        "Negotiate peace NOW or the war effort collapses.", Color.red);
                    InboxSystem.Instance?.SendMessage(country, "CHIEF OF DEFENCE STAFF",
                        "🚨 MILITARY BREAKING POINT",
                        $"Sir, war exhaustion has reached {country.warExhaustion:F0}%.\n\n" +
                        $"Our troops are at their absolute limit. Unit commanders are reporting:\n" +
                        $"• Mass desertions in forward positions\n" +
                        $"• Officers refusing orders to advance\n" +
                        $"• Civilians blocking supply convoys in protest\n\n" +
                        $"CRITICAL: If exhaustion reaches 100%, the war effort collapses automatically.\n\n" +
                        $"Open DIPLOMACY → Relations → select the enemy → PROPOSE PEACE immediately.\n" +
                        $"Time remaining before collapse: {Mathf.RoundToInt((100f - country.warExhaustion) / 2f)} turns.",
                        "Critical");
                }
                else if (country.warExhaustion >= 70f && country.warExhaustion < 72f)
                    NotificationManager.Instance?.AddNotification(
                        "⚠ HIGH WAR FATIGUE: Exhaustion at 70%. Morale declining. Consider opening peace talks.",
                        new Color(1f, 0.55f, 0f));
                else if (country.warExhaustion >= 50f && country.warExhaustion < 52f)
                    NotificationManager.Instance?.AddNotification(
                        "📋 WAR FATIGUE: Exhaustion crossing 50%. Public opposition growing.",
                        Color.yellow);
            }

            if (country.warWillToFight <= 0f || country.warExhaustion >= 100f)
            {
                foreach (var enemy in enemies)
                {
                    string outcome = country.warExhaustion >= 100f
                        ? $"{enemy.countryName} Victory — {country.countryName} Exhausted"
                        : $"{enemy.countryName} Victory — {country.countryName} Collapsed";
                    EndWar(country, enemy, outcome);
                }
            }
        }

        // ─── SURPRISE EVENTS — Chaos of war ──────────────────────────────────
        private void ProcessSurpriseEvent(CountryDataSO country, CountryDataSO enemy, string key,
            bool isPlayer, ref float totalKIA, ref float totalEquip, float baseCasRate,
            float myPower, float enemyPower)
        {
            // Pick a random event category
            int roll = UnityEngine.Random.Range(0, 13);
            float progress = frontProgress.ContainsKey(key) ? frontProgress[key] : 0f;

            switch (roll)
            {
                case 0: // AMBUSH — sudden casualties
                {
                    int ambushKIA = UnityEngine.Random.Range(80, 350);
                    country.unitInfantry = Mathf.Max(0, country.unitInfantry - ambushKIA);
                    totalKIA += ambushKIA;
                    // Ambush dents momentum
                    if (momentumVelocity.ContainsKey(key))
                        momentumVelocity[key] -= UnityEngine.Random.Range(1f, 4f);
                    if (isPlayer)
                    {
                        string loc = GetRandomEnemyLocation(enemy);
                        InboxSystem.Instance?.SendMessage(country,
                            $"FIELD COMMAND — {enemy.countryName.ToUpper()} FRONT",
                            $"🔴 AMBUSH NEAR {loc.ToUpper()}",
                            $"INCIDENT REPORT:\n" +
                            $"A column was ambushed near {loc}. Enemy forces used prepared positions and diverted us into a kill zone.\n\n" +
                            $"CASUALTIES:\n• ~{ambushKIA:N0} killed or wounded in action\n\n" +
                            $"ASSESSMENT: Intelligence failure. Enemy is using terrain effectively. " +
                            $"Expect further irregular tactics in this sector.\n\n" +
                            $"Recommend: increase reconnaissance before advancing.",
                            "Critical");
                        NewsSystem.Instance?.PublishNews(
                            $"AMBUSH: {country.countryName} column suffers heavy casualties near {loc}.");
                    }
                    break;
                }

                case 1: // SUPPLY CONVOY DESTROYED
                {
                    float fuelLost = UnityEngine.Random.Range(5f, 20f);
                    float ammoLost = UnityEngine.Random.Range(5f, 15f);
                    country.reserveFuel = Mathf.Max(0f, country.reserveFuel - fuelLost);
                    country.reserveAmmo = Mathf.Max(0f, country.reserveAmmo - ammoLost);
                    if (isPlayer)
                    {
                        NotificationManager.Instance?.AddNotification(
                            $"🚛 CONVOY DESTROYED: Enemy struck our supply lines. Fuel -{fuelLost:F0}, Ammo -{ammoLost:F0}.",
                            new Color(1f, 0.4f, 0f));
                        NewsSystem.Instance?.PublishNews(
                            $"LOGISTICS ATTACK: {enemy.countryName} destroys {country.countryName} supply convoy. Frontline shortages expected.");
                    }
                    break;
                }

                case 2: // UNEXPECTED ENEMY OFFENSIVE — sudden momentum reversal
                {
                    float swing = UnityEngine.Random.Range(5f, 18f);
                    frontProgress[key] = Mathf.Clamp(frontProgress[key] - swing, -100f, 100f);
                    momentumVelocity[key] = -UnityEngine.Random.Range(2f, 5f);
                    int surgeKIA = UnityEngine.Random.Range(150, 500);
                    country.unitInfantry = Mathf.Max(0, country.unitInfantry - surgeKIA);
                    totalKIA += surgeKIA;
                    if (isPlayer)
                    {
                        string loc = GetRandomEnemyLocation(enemy);
                        InboxSystem.Instance?.SendMessage(country,
                            $"JOINT CHIEFS OF STAFF",
                            $"🔴 ENEMY COUNTER-OFFENSIVE — LINES UNDER PRESSURE",
                            $"URGENT SITUATION REPORT:\n\n" +
                            $"{enemy.countryName} has launched an unexpected large-scale offensive near {loc}.\n\n" +
                            $"Our forward units were caught repositioning and took heavy losses. " +
                            $"The enemy committed fresh reserves we had not detected.\n\n" +
                            $"CASUALTIES: ~{surgeKIA:N0} in initial breakthrough fighting\n" +
                            $"FRONT: Falling back to secondary positions\n\n" +
                            $"IMMEDIATE ACTION REQUIRED:\n" +
                            $"• MILITARY → OPERATIONS → Send reinforcements to the {enemy.countryName} front\n" +
                            $"• Hold secondary positions — do not let the line collapse entirely",
                            "Critical");
                        NewsSystem.Instance?.PublishNews(
                            $"COUNTER-OFFENSIVE: {enemy.countryName} launches surprise assault, pushing {country.countryName} forces back.");
                    }
                    break;
                }

                case 3: // BREAKTHROUGH — sudden advance
                {
                    float swing = UnityEngine.Random.Range(8f, 20f);
                    frontProgress[key] = Mathf.Clamp(frontProgress[key] + swing, -100f, 100f);
                    momentumVelocity[key] = UnityEngine.Random.Range(2f, 6f);
                    if (isPlayer)
                    {
                        string loc = GetRandomEnemyLocation(enemy);
                        NotificationManager.Instance?.AddNotification(
                            $"⚡ BREAKTHROUGH: Our forces have broken through {enemy.countryName} lines near {loc}! Momentum surging.",
                            Color.green);
                        NewsSystem.Instance?.PublishNews(
                            $"BREAKTHROUGH: {country.countryName} armoured columns punch through {enemy.countryName} defensive lines near {loc}.");
                        InboxSystem.Instance?.SendMessage(country,
                            $"FIELD COMMAND — {enemy.countryName.ToUpper()} FRONT",
                            $"⚡ BREAKTHROUGH NEAR {loc.ToUpper()} — EXPLOIT NOW",
                            $"Sir, our armoured units have broken the enemy line near {loc}!\n\n" +
                            $"Enemy forces are in disarray. We have a window of 1-2 turns to exploit this before they consolidate.\n\n" +
                            $"RECOMMEND:\n" +
                            $"• Push additional tanks and infantry through the gap immediately\n" +
                            $"• Target enemy supply depots and command posts in the rear\n" +
                            $"• Do not let up — momentum is everything right now",
                            "Important");
                    }
                    break;
                }

                case 4: // MASS DEFECTION / SURRENDER
                {
                    if (UnityEngine.Random.value < 0.4f)
                    {
                        // Enemy soldiers defect — boosts our side
                        int defectors = UnityEngine.Random.Range(200, 1200);
                        if (isPlayer)
                        {
                            NotificationManager.Instance?.AddNotification(
                                $"🏳 DEFECTION: ~{defectors:N0} {enemy.countryName} troops surrender to our lines!",
                                Color.green);
                            NewsSystem.Instance?.PublishNews(
                                $"MASS SURRENDER: {defectors:N0} {enemy.countryName} soldiers defect, citing collapsing morale.");
                        }
                        enemy.unitInfantry = Mathf.Max(0, enemy.unitInfantry - defectors);
                        enemy.warWillToFight = Mathf.Max(0f, enemy.warWillToFight - 5f);
                    }
                    else
                    {
                        // Our soldiers desert
                        int deserters = UnityEngine.Random.Range(100, 600);
                        country.unitInfantry = Mathf.Max(0, country.unitInfantry - deserters);
                        country.warWillToFight = Mathf.Max(0f, country.warWillToFight - 3f);
                        if (isPlayer)
                        {
                            NotificationManager.Instance?.AddNotification(
                                $"🏃 DESERTION: ~{deserters:N0} soldiers have abandoned their posts. Morale falling.",
                                new Color(1f, 0.5f, 0f));
                        }
                    }
                    break;
                }

                case 5: // COMMUNICATIONS BLACKOUT — fog of war
                {
                    if (isPlayer)
                    {
                        InboxSystem.Instance?.SendMessage(country,
                            "SIGNALS COMMAND",
                            "📡 COMMUNICATIONS BLACKOUT — FOG OF WAR",
                            $"Sir, we have lost contact with forward units on the {enemy.countryName} front.\n\n" +
                            $"Cause is unknown — could be enemy jamming, destroyed relay stations, or operator error.\n\n" +
                            $"Current front status is UNCONFIRMED. The situation on the ground may differ significantly " +
                            $"from last reported positions.\n\n" +
                            $"Communications expected to be restored within 1-2 turns.",
                            "Important");
                    }
                    break;
                }

                case 6: // FRIENDLY FIRE INCIDENT
                {
                    int ffiKIA = UnityEngine.Random.Range(20, 100);
                    country.unitInfantry = Mathf.Max(0, country.unitInfantry - ffiKIA);
                    country.armyStats.morale = Mathf.Max(0f, country.armyStats.morale - 5f);
                    totalKIA += ffiKIA;
                    if (isPlayer)
                    {
                        NotificationManager.Instance?.AddNotification(
                            $"⚠ FRIENDLY FIRE: Misidentification incident. {ffiKIA} casualties. Morale -5.",
                            new Color(1f, 0.7f, 0f));
                    }
                    break;
                }

                case 7: // STRATEGIC VILLAGE / TOWN CAPTURED
                {
                    if (progress > 0f)
                    {
                        string loc = GetRandomEnemyLocation(enemy);
                        if (isPlayer)
                        {
                            NewsSystem.Instance?.PublishNews(
                                $"TERRITORY: {country.countryName} forces capture {loc}, a key logistics node.");
                            NotificationManager.Instance?.AddNotification(
                                $"🏴 CAPTURED: Our forces have seized {loc}. Enemy supply lines disrupted.",
                                Color.green);
                        }
                        enemy.reserveAmmo = Mathf.Max(0f, enemy.reserveAmmo - 8f);
                        enemy.reserveFuel = Mathf.Max(0f, enemy.reserveFuel - 6f);
                    }
                    else
                    {
                        string loc = GetRandomLocation(country);
                        if (isPlayer)
                        {
                            NewsSystem.Instance?.PublishNews(
                                $"TERRITORY LOST: {enemy.countryName} seizes {loc}. Civilians evacuating.");
                            NotificationManager.Instance?.AddNotification(
                                $"🔴 {loc} LOST: Enemy has captured a key position inside our territory!",
                                Color.red);
                        }
                        country.stability = Mathf.Max(5f, country.stability - 2f);
                    }
                    break;
                }

                case 8: // ARTILLERY BARRAGE — both sides take losses
                {
                    int artKIA = UnityEngine.Random.Range(50, 200);
                    int enemyArtKIA = UnityEngine.Random.Range(30, 150);
                    country.unitInfantry = Mathf.Max(0, country.unitInfantry - artKIA);
                    enemy.unitInfantry   = Mathf.Max(0, enemy.unitInfantry   - enemyArtKIA);
                    totalKIA += artKIA;
                    if (isPlayer)
                    {
                        string loc = GetRandomEnemyLocation(enemy);
                        NewsSystem.Instance?.PublishNews(
                            $"ARTILLERY DUEL: Intense shelling near {loc}. Both sides report heavy casualties.");
                    }
                    break;
                }

                case 9: // FLANKING MANOEUVRE
                {
                    if (myPower > enemyPower * 0.8f)
                    {
                        float flankBonus = UnityEngine.Random.Range(3f, 12f);
                        frontProgress[key] = Mathf.Clamp(frontProgress[key] + flankBonus, -100f, 100f);
                        int flankKIA = UnityEngine.Random.Range(50, 200);
                        enemy.unitInfantry = Mathf.Max(0, enemy.unitInfantry - flankKIA);
                        if (isPlayer)
                        {
                            string loc = GetRandomEnemyLocation(enemy);
                            NotificationManager.Instance?.AddNotification(
                                $"⚡ FLANKING SUCCESS: Our forces outmanoeuvre enemy near {loc}. Front shifting.",
                                Color.green);
                        }
                    }
                    else
                    {
                        float flankPenalty = UnityEngine.Random.Range(3f, 10f);
                        frontProgress[key] = Mathf.Clamp(frontProgress[key] - flankPenalty, -100f, 100f);
                        if (isPlayer)
                            NotificationManager.Instance?.AddNotification(
                                $"⚠ FLANKED: Enemy executed a flanking manoeuvre. Our lines are being pressured.",
                                new Color(1f, 0.4f, 0f));
                    }
                    break;
                }

                case 10: // DISEASE / HEAT / ENVIRONMENTAL ATTRITION
                {
                    int envKIA = UnityEngine.Random.Range(30, 150);
                    country.unitInfantry = Mathf.Max(0, country.unitInfantry - envKIA);
                    country.warExhaustion = Mathf.Min(100f, country.warExhaustion + 2f);
                    totalKIA += envKIA;
                    if (isPlayer)
                    {
                        string[] envEvents = { "disease outbreak in forward positions",
                            "severe heat casualties among deployed troops",
                            "flooding cutting off forward supply routes",
                            "malaria outbreak weakening frontline units" };
                        string ev = envEvents[UnityEngine.Random.Range(0, envEvents.Length)];
                        NotificationManager.Instance?.AddNotification(
                            $"🌡 NON-COMBAT ATTRITION: {ev}. {envKIA} casualties. Exhaustion rising.",
                            new Color(1f, 0.7f, 0f));
                    }
                    break;
                }

                case 11: // INTELLIGENCE COUP — affects next turn
                {
                    if (isPlayer)
                    {
                        float actualProgress = frontProgress.ContainsKey(key) ? frontProgress[key] : 0f;
                        string realStatus = actualProgress > 20f ? "WINNING — continue pressing" :
                                            actualProgress > 0f  ? "SLIGHTLY AHEAD — maintain pressure" :
                                            actualProgress > -20f ? "LOSING GROUND — consider regrouping" :
                                                                     "BADLY LOSING — urgent action needed";
                        InboxSystem.Instance?.SendMessage(country,
                            "INTELLIGENCE DIRECTORATE",
                            "🔍 INTELLIGENCE ASSESSMENT — ENEMY DISPOSITION",
                            $"Sir, we have obtained reliable intelligence on enemy positions.\n\n" +
                            $"ACTUAL FRONT STATUS: {realStatus}\n\n" +
                            $"Enemy reserves: approximately {enemy.unitInfantry:N0} infantry, {enemy.unitTanks} tanks\n" +
                            $"Enemy supply status: Ammo {enemy.reserveAmmo:F0} | Fuel {enemy.reserveFuel:F0}\n" +
                            $"Enemy exhaustion: {enemy.warExhaustion:F0}%\n" +
                            $"Enemy will to fight: {enemy.warWillToFight:F0}%\n\n" +
                            $"Recommend exploiting identified weaknesses in the next 2-3 turns.",
                            "Important");
                    }
                    break;
                }

                default: // STALEMATE — grinding, no progress
                {
                    // Dampen momentum — war grinds to a halt
                    momentumVelocity[key] = Mathf.Lerp(momentumVelocity[key], 0f, 0.6f);
                    if (isPlayer && UnityEngine.Random.value < 0.4f)
                    {
                        string loc = GetRandomEnemyLocation(enemy);
                        NewsSystem.Instance?.PublishNews(
                            $"STALEMATE: Lines near {loc} unchanged after intense fighting. Both sides consolidate.");
                    }
                    break;
                }
            }
        }

        // ─── Phase Transition Briefing ────────────────────────────────────────
        private void SendPhaseTransitionBriefing(CountryDataSO country, CountryDataSO enemy,
            string newPhase, int warMonth, bool isPlayer)
        {
            if (!isPlayer) return;

            string key = WarKey(country.countryName, enemy.countryName);
            float progress = frontProgress.ContainsKey(key) ? frontProgress[key] : 0f;
            string frontStatus = progress > 20f ? "ADVANCING — we hold the initiative" :
                                 progress > 0f  ? "SLIGHT ADVANTAGE — momentum is with us" :
                                 progress > -20f ? "CONTESTED — neither side dominates" :
                                                   "BEING PUSHED BACK — enemy has the initiative";

            string phaseDesc = newPhase switch
            {
                "OFFENSIVE" =>
                    "The mobilisation phase is over. Both sides have deployed their forces.\n\n" +
                    "OFFENSIVE PHASE — MONTHS 3-6:\n" +
                    "• This is when the initial push determines early momentum\n" +
                    "• Attacker advantage: your offensive operations deal +20% damage\n" +
                    "• KEY: Maintain supply lines — ammo and fuel consumption spikes now\n" +
                    "• Air superiority is decisive in this phase — deploy fighters if you haven't\n" +
                    "• A successful offensive in this window can break enemy will early\n" +
                    "• EXPECT: Surprise events, ambushes and momentum reversals — this is war",
                "ATTRITION" =>
                    "The initial offensive has settled into a grinding war of attrition.\n\n" +
                    "ATTRITION PHASE — MONTHS 7-18:\n" +
                    "• Neither side is advancing decisively — this is a war of endurance\n" +
                    "• The side that runs out of supplies, money or will first loses\n" +
                    "• CRITICAL: Monitor your treasury — war costs $0.03B/month minimum\n" +
                    "• Reinforcements matter now — keep deploying troops to maintain frontline strength\n" +
                    "• Unpredictable events will increasingly disrupt front lines\n" +
                    "• Consider if peace terms are acceptable — attrition kills both sides",
                "GRINDING WAR" =>
                    "This war has dragged on for over 18 months. Both nations are being bled dry.\n\n" +
                    "GRINDING WAR PHASE:\n" +
                    "• Exhaustion is rising faster — casualty rate accelerating\n" +
                    "• GDP is taking permanent damage every month this continues\n" +
                    "• Your approval rating is suffering — elections may be affected\n" +
                    "• The international community is pressuring for a ceasefire\n" +
                    "• STRONGLY CONSIDER: Peace negotiations — the longer this goes, the worse both sides look",
                _ => ""
            };

            InboxSystem.Instance?.SendMessage(country,
                "JOINT CHIEFS OF STAFF",
                $"⚔️ WAR PHASE UPDATE: {newPhase} BEGINS — Month {warMonth}",
                $"FRONT STATUS: {frontStatus}\n\n{phaseDesc}\n\n" +
                $"CURRENT STATS:\n" +
                $"• Our KIA so far: {country.totalKIA:N0}\n" +
                $"• War exhaustion: {country.warExhaustion:F0}%\n" +
                $"• Will to fight: {country.warWillToFight:F0}%\n" +
                $"• Supply: Ammo {country.reserveAmmo:F0} | Fuel {country.reserveFuel:F0}",
                "Critical");

            NotificationManager.Instance?.AddNotification(
                $"⚔️ WAR PHASE: {newPhase} — Month {warMonth}. {frontStatus}.",
                new Color(0.9f, 0.7f, 0.2f));
        }

        // ─── Named Battle Report ──────────────────────────────────────────────
        private void GenerateBattleReport(CountryDataSO country, CountryDataSO enemy, string key,
            int casualties, float totalKIA, float myPower, float enemyPower, string phase, bool isPlayer, float tanksLost, float apcsLost, float fightersLost, System.Collections.Generic.List<string> casualtyCauses)
        {
            if (!isPlayer) return;
            // ── Cooldown gate: max one battle report per 3 turns per front ──
            if (InboxSystem.Instance != null &&
                !InboxSystem.Instance.CanSendBattleReport(country.countryName, enemy.countryName))
                return;
            if (UnityEngine.Random.value > 0.35f) return; // 35% chance — higher since cooldown now limits frequency

            float progress = frontProgress.ContainsKey(key) ? frontProgress[key] : 0f;
            float velocity = momentumVelocity.ContainsKey(key) ? momentumVelocity[key] : 0f;

            string location = GetRandomEnemyLocation(enemy);

            string[] verbs = progress > 15f ? OffensiveVerbs :
                             progress < -15f ? DefensiveVerbs :
                                               StalemateVerbs;
            string verb = verbs[UnityEngine.Random.Range(0, verbs.Length)];

            string headline = $"COMBAT REPORT: {country.countryName} {verb} {location}";
            string frontTrend = progress > 20f  ? "🟢 ADVANCING" :
                                progress > 5f   ? "🟡 SLIGHT GAIN" :
                                progress > -5f  ? "⚪ STALEMATE" :
                                progress > -20f ? "🟡 SLIGHT LOSS" :
                                                  "🔴 RETREATING";

            // Authentic, varied battle summary
            string[] summaries;
            if (progress > 20f)
                summaries = new[] {
                    $"Our forces have seized the initiative near {location}. Enemy units are falling back under sustained pressure from armoured columns. Intelligence reports disrupted enemy command and control in this sector.",
                    $"A coordinated push near {location} has broken through the first line of defence. Enemy is conducting a fighting withdrawal. We have room to exploit if reinforced quickly.",
                    $"Armoured and infantry units are advancing near {location} after a night assault. Enemy artillery is still active but losing effectiveness as we close the distance.",
                };
            else if (progress < -20f)
                summaries = new[] {
                    $"Enemy forces mounted a major assault near {location}. Our defensive lines are holding but under severe pressure. We have withdrawn to secondary positions to consolidate. Reinforcements urgently needed.",
                    $"A gap opened in our lines near {location} after heavy artillery preparation by the enemy. Units have fallen back to defensible ground. The situation is serious but not yet critical.",
                    $"We are being pushed back near {location}. Enemy is using combined arms effectively — armour, infantry and air working together. Counter-battery fire is needed immediately.",
                };
            else
                summaries = new[] {
                    $"Heavy exchange of fire near {location}. Both sides are contesting the same ground. Neither offensive is achieving decisive results. Artillery and air assets are dominating.",
                    $"Grinding attritional fighting near {location}. Both sides have attacked and been repelled repeatedly. The front has barely moved in days. Casualties are mounting on both sides.",
                    $"Trench lines and fortified positions near {location} are making advance difficult for both sides. Snipers, mortars and patrol clashes are the main activity.",
                };
            string summary = summaries[UnityEngine.Random.Range(0, summaries.Length)];

            // Momentum note — is the trend shifting?
            string momentumNote = velocity > 2f  ? "Momentum is building in our favour." :
                                  velocity < -2f ? "Momentum is shifting toward the enemy." :
                                                   "Front is in equilibrium.";

            // ── Power display strings — show helpful status when units are still deploying ──
            string myPowerStr    = myPower > 0
                ? $"{myPower:N0} combat power"
                : "⏳ Deploying — units still in transit, check Operations tab";
            string enemyPowerStr = enemyPower > 0
                ? $"{enemyPower:N0} combat power"
                : "Unknown — no confirmed frontline contact yet";

            string causesStr = casualtyCauses != null && casualtyCauses.Count > 0 
                ? string.Join(", ", casualtyCauses) 
                : "Standard Combat";

            InboxSystem.Instance?.SendMessage(country,
                $"FIELD COMMAND — {enemy.countryName.ToUpper()} FRONT",
                headline,
                $"BATTLE REPORT — {phase}\n\n" +
                $"{summary} {momentumNote}\n\n" +
                $"FRONT STATUS: {frontTrend} (Momentum: {progress:+0.0;-0.0}/100)\n\n" +
                $"PRIMARY CASUALTY CAUSES:\n• {causesStr}\n\n" +
                $"LOSSES THIS ACTION:\n" +
                $"• Our casualties: ~{casualties:N0} soldiers\n" +
                $"• Equipment lost: {(int)tanksLost:N0} Tanks, {(int)apcsLost:N0} APCs, {(int)fightersLost:N0} Fighters\n" +
                $"• Enemy estimated losses: ~{Mathf.RoundToInt(casualties * (myPower / Mathf.Max(enemyPower, 1f))):N0} soldiers\n\n" +
                $"FORCE BALANCE:\n" +
                $"• Our frontline strength: {myPowerStr}\n" +
                $"• Enemy frontline strength: {enemyPowerStr}\n" +
                $"• Air: {(country.unitFighters > enemy.unitFighters ? "WE CONTROL AIRSPACE" : enemy.unitFighters > country.unitFighters * 1.5f ? "ENEMY AIR DOMINANCE" : "CONTESTED AIRSPACE")}\n\n" +
                $"SUPPLIES: Ammo {country.reserveAmmo:F0} | Fuel {country.reserveFuel:F0} | Food {country.reserveFood:F0}",
                "Important");

            NewsSystem.Instance?.PublishNews($"{headline}. {country.countryName} suffered ~{casualties:N0} military casualties and lost {(int)tanksLost:N0} Tanks.");
        }

        // ─── Monthly War Report ───────────────────────────────────────────────
        private void SendMonthlyWarReport(CountryDataSO country, List<CountryDataSO> enemies,
            int newKIA, int newWounded, float tanksLost, float apcsLost, float fightersLost, GameManager gm)
        {
            // ── CASUALTY AND BANKRUPTCY CAPITULATION ──
            if (country.treasury <= 0 && gm?.playerCountry == country) {
                NotificationManager.Instance?.AddNotification("FINANCE MINISTER: We are out of funds! Take out an emergency loan immediately or we will capitulate!", UnityEngine.Color.red);
            }
            if (country.treasury <= 0 && country.debt >= country.gdp * 0.8f) {
                // Bankrupt and high debt
                EndWar(enemies[0], country, $"{enemies[0].countryName} Victory — Complete Economic Collapse");
                return;
            }
            
            if (country.totalKIA > country.population * 0.05f) {
                // Casualties exceeded 5% of population
                EndWar(enemies[0], country, $"{enemies[0].countryName} Victory — Military Annihilation");
                return;
            }

            string supplyWarn = "";
            if (country.reserveAmmo < 30f) supplyWarn += "\n⚠ AMMO CRITICAL";
            if (country.reserveFuel < 20f) supplyWarn += "\n⚠ FUEL CRITICAL";
            if (country.reserveFood < 15f) supplyWarn += "\n⚠ FOOD CRITICAL";

            string exhaustWarn = country.warExhaustion > 70f
                ? $"\n🔴 EXHAUSTION CRITICAL ({country.warExhaustion:F0}%) — approach peace talks"
                : country.warExhaustion > 50f
                ? $"\n🟡 WAR FATIGUE ({country.warExhaustion:F0}%) — morale declining"
                : "";

            string frontSummary = "";
            int estimatedEnemyKIA = 0;
            int estimatedEnemyWounded = 0;

            foreach (var enemy in enemies)
            {
                string key = WarKey(country.countryName, enemy.countryName);
                float prog = frontProgress.ContainsKey(key) ? frontProgress[key] : 0f;
                float vel  = momentumVelocity.ContainsKey(key) ? momentumVelocity[key] : 0f;
                string status = prog > 20f ? "ADVANCING ▲" : prog < -20f ? "RETREATING ▼" : "STALEMATE ◼";
                string trend  = vel  > 1f  ? "(gaining)"  : vel < -1f    ? "(losing)"    : "(steady)";
                frontSummary += $"• {enemy.countryName}: {status} {trend} ({prog:+0.0;-0.0})\n";

                float myPower    = GetFrontlinePowerAgainst(country, enemy.countryName, country == gm?.playerCountry) * CalculateGeneralBonus(country);
                float enemyPower = GetFrontlinePowerAgainst(enemy, country.countryName, enemy == gm?.playerCountry);
                float ratio = myPower / Mathf.Max(enemyPower, 1f);
                
                estimatedEnemyKIA += Mathf.RoundToInt(newKIA * ratio);
                estimatedEnemyWounded += Mathf.RoundToInt(newWounded * ratio);
            }

            string reportBody = $"--- OUR LOSSES ---\n" +
                $"PERSONNEL: {newKIA:N0} KIA | {newWounded:N0} Wounded\n" +
                $"EQUIPMENT: {(int)tanksLost:N0} Tanks | {(int)apcsLost:N0} APCs | {(int)fightersLost:N0} Fighters\n" +
                $"CUMULATIVE KIA: {country.totalKIA:N0}\n\n" +
                $"--- ESTIMATED ENEMY LOSSES ---\n" +
                $"PERSONNEL: ~{estimatedEnemyKIA:N0} KIA | ~{estimatedEnemyWounded:N0} Wounded\n\n" +
                $"--- FRONT STATUS ---\n{frontSummary}\n" +
                $"EXHAUSTION: {country.warExhaustion:F0}% | WILL TO FIGHT: {country.warWillToFight:F0}%\n" +
                $"SUPPLIES: Ammo {country.reserveAmmo:F0} | Fuel {country.reserveFuel:F0} | Food {country.reserveFood:F0}" +
                supplyWarn + exhaustWarn;

            InboxSystem.Instance?.SendMessage(country, "DEFENCE MINISTRY",
                $"📋 MONTHLY WAR REPORT — Month {country.warTurnsActive}",
                reportBody,
                "Important");

            // If player, also inject this into the Event System so it's not missed
            if (gm?.playerCountry == country && gm?.EventSystem is EventSystem evSys)
            {
                var repEv = UnityEngine.ScriptableObject.CreateInstance<GameEventSO>();
                repEv.eventTitle = $"War Report (Month {country.warTurnsActive})";
                
                string medStrain = newWounded > 1000 ? "\n\nMedical infrastructure is severely strained by the influx of wounded personnel." : "";
                repEv.description = $"Ministry of Defence Casualty & Frontline Report:\n\n{reportBody}{medStrain}";
                repEv.severity = newKIA > 2000 ? "Critical" : newKIA > 500 ? "High" : "Medium";
                repEv.timeRemainingDays = 15;
                repEv.choices = new System.Collections.Generic.List<GameEventChoice> {
                    new GameEventChoice { choiceText = "Acknowledge Report", stabilityEffect = 0f }
                };
                
                if (newKIA > 1000) {
                    repEv.choices.Add(new GameEventChoice { 
                        choiceText = "Hold Public Mourning", 
                        costUSD = 0.1f, 
                        stabilityEffect = 2f 
                    });
                }
                
                evSys.InjectUrgentEvent(country, repEv);
            }
        }

        // ─── Bombardment ──────────────────────────────────────────────────────
        private void ProcessBombardment(MilitaryOperation op, CountryDataSO country,
            CountryDataSO enemy, bool isPlayer)
        {
            var campTarget = country.regions?.Find(r =>
                op.regionName.Contains(r.regionName) && r.insurgentCampDetected);

            if (campTarget != null)
            {
                float effectiveness = Mathf.Clamp01(op.deployedBombers / 5f);
                float damage = 40f + (effectiveness * 40f);
                campTarget.campStrength = Mathf.Max(0f, campTarget.campStrength - damage);
                campTarget.insurgentSupplyLines = Mathf.Max(0f, campTarget.insurgentSupplyLines - 30f);
                campTarget.insurgencyLevel = Mathf.Max(0f, campTarget.insurgencyLevel - (damage * 0.5f));
                campTarget.campStrikeOrdered = true;

                if (campTarget.campStrength <= 0f)
                {
                    campTarget.insurgentCampDetected = false;
                    campTarget.campStrikeOrdered     = false;
                    if (isPlayer)
                        NotificationManager.Instance?.AddNotification(
                            $"✅ CAMP DESTROYED: {campTarget.regionName} insurgent base eliminated!", Color.green);
                }
                else if (isPlayer)
                    NotificationManager.Instance?.AddNotification(
                        $"💥 CAMP STRUCK: {campTarget.regionName} at {campTarget.campStrength:F0}% strength remaining.", Color.yellow);

                if (UnityEngine.Random.value < 0.25f)
                {
                    op.deployedBombers = Mathf.Max(0, op.deployedBombers - 1);
                    country.unitBombers = Mathf.Max(0, country.unitBombers - 1);
                    if (isPlayer)
                        NotificationManager.Instance?.AddNotification(
                            $"⚠ BOMBER LOST: 1 aircraft shot down by insurgent AA fire over {campTarget.regionName}.",
                            new Color(1f, 0.5f, 0f));
                }
                return;
            }

            int tankDamage = op.deployedBombers * 5;
            enemy.unitTanks = Mathf.Max(0, enemy.unitTanks - tankDamage);
            enemy.armyStats.equipment = Mathf.Max(0, enemy.armyStats.equipment - tankDamage);

            if (isPlayer)
                NotificationManager.Instance?.AddNotification(
                    $"💥 STRATEGIC STRIKE: Bombers hit {enemy.countryName} supply depots. " +
                    $"{tankDamage} enemy vehicles destroyed.", new Color(0.9f, 0.7f, 0.2f));

            if (UnityEngine.Random.value < 0.3f && enemy.airForceStats.equipment > 0)
                op.deployedBombers = Mathf.Max(0, op.deployedBombers - 1);
        }

        // ─── Enemy Air Raid ───────────────────────────────────────────────────
        private void ProcessEnemyAirRaid(MilitaryOperation op, CountryDataSO country,
            CountryDataSO enemy, ref float totalKIA, ref float totalEquip, bool isPlayer)
        {
            int soldiers = UnityEngine.Random.Range(20, 120);
            int tanks    = UnityEngine.Random.Range(0, 3);
            int apcs     = UnityEngine.Random.Range(0, 4);

            op.deployedTroops = Mathf.Max(0, op.deployedTroops - soldiers);
            op.deployedTanks  = Mathf.Max(0, op.deployedTanks  - tanks);
            op.deployedAPCs   = Mathf.Max(0, op.deployedAPCs   - apcs);
            country.unitTanks = Mathf.Max(0, country.unitTanks - tanks);
            country.unitAPCs  = Mathf.Max(0, country.unitAPCs  - apcs);
            totalKIA  += soldiers;
            totalEquip+= tanks + apcs;

            if (!isPlayer) return;

            string detail = $"{soldiers} soldiers killed";
            if (tanks > 0) detail += $", {tanks} tank(s) destroyed";
            if (apcs  > 0) detail += $", {apcs} APC(s) destroyed";

            InboxSystem.Instance?.SendMessage(country,
                "AIR DEFENCE COMMAND",
                $"🔴 ENEMY AIRSTRIKE — {enemy.countryName} Front",
                $"STRIKE SUMMARY:\n" +
                $"• {detail}\n" +
                $"• Our fighters: {country.unitFighters} | Enemy aircraft: {enemy.unitFighters + enemy.unitBombers}\n\n" +
                $"ACTION: Open MILITARY → OPERATIONS → LAUNCH AIR INTERCEPT to scramble fighters.\n" +
                $"Repeated raids will inflict severe casualties if not countered.",
                "Critical");

            NotificationManager.Instance?.AddNotification(
                $"🚨 AIR RAID: {enemy.countryName} bombers hit our positions! {detail}. " +
                $"MILITARY → OPERATIONS → LAUNCH AIR INTERCEPT.",
                new Color(1f, 0.2f, 0.2f));
        }

        // ─── Coalition Maintenance ────────────────────────────────────────────
        private void HandleCoalitionMaintenance(CountryDataSO country, GameManager gm, bool isPlayer)
        {
            if (!isPlayer || country.activeTreaties == null) return;
            foreach (var treaty in country.activeTreaties)
            {
                if (treaty.treatyName != "Coalition Force Pact") continue;
                float cost = 0.02f;
                if (gm != null && gm.playerBudget >= cost)
                {
                    gm.playerBudget -= cost;
                    country.treasury = gm.playerBudget;
                }
                else
                {
                    var partner = gm?.allCountries?.Find(c => c.countryName == treaty.partnerCountry);
                    if (partner == null) continue;
                    int w1 = Mathf.RoundToInt(country.unitInfantry * 0.05f);
                    int w2 = Mathf.RoundToInt(country.unitTanks    * 0.05f);
                    country.unitInfantry  = Mathf.Max(0, country.unitInfantry - w1);
                    country.unitTanks     = Mathf.Max(0, country.unitTanks    - w2);
                    partner.unitInfantry += w1;
                    partner.unitTanks    += w2;
                    NotificationManager.Instance?.AddNotification(
                        $"⚠ COALITION STRAINED: Can't afford $0.02B — {w1:N0} troops recalled by {treaty.partnerCountry}.",
                        new Color(1f, 0.5f, 0f));
                }
            }
        }

        // ─── Supply Drain ─────────────────────────────────────────────────────
        private void ProcessSupplyDrain(CountryDataSO country, bool isPlayer)
        {
            int deployed = GetTotalDeployedForces(country);
            float mult   = 1f + (deployed / 10000f);
            float prevAmmo = country.reserveAmmo;
            float prevFuel = country.reserveFuel;
            float prevFood = country.reserveFood;

            country.reserveFuel = Mathf.Max(0f, country.reserveFuel - SUPPLY_DRAIN_FUEL_PER_TURN * mult);
            country.reserveAmmo = Mathf.Max(0f, country.reserveAmmo - SUPPLY_DRAIN_AMMO_PER_TURN * mult);
            country.reserveFood = Mathf.Max(0f, country.reserveFood - SUPPLY_DRAIN_FOOD_PER_TURN * mult);

            // ── Oil-to-Fuel Conversion ─────────────────────────────────────────
            // Countries with domestic oil stockpiles automatically refuel their
            // military from national reserves. 1,000,000 barrels = 1 fuel unit.
            // This ensures oil-producing nations (e.g. Nigeria) are never fuel-
            // starved simply because the logistics pipeline was missing.
            const float OIL_BARRELS_PER_FUEL_UNIT = 1_000_000f;
            const float MAX_RESERVE_FUEL           = 100f;
            if (country.invOilBarrels > OIL_BARRELS_PER_FUEL_UNIT && country.reserveFuel < MAX_RESERVE_FUEL)
            {
                float fuelNeeded    = MAX_RESERVE_FUEL - country.reserveFuel;
                float fuelGenerated = Mathf.Min(fuelNeeded, country.invOilBarrels / OIL_BARRELS_PER_FUEL_UNIT);
                country.reserveFuel    = Mathf.Min(MAX_RESERVE_FUEL, country.reserveFuel + fuelGenerated);
                country.invOilBarrels -= fuelGenerated * OIL_BARRELS_PER_FUEL_UNIT;
            }

            if (!isPlayer) return;
            if (prevAmmo >= 30f && country.reserveAmmo < 30f)
                NotificationManager.Instance?.AddNotification(
                    "⚠ AMMO CRITICAL: Less than 30 units! Casualty rate +80%. Resupply immediately.", new Color(1f, 0.4f, 0f));
            if (prevAmmo > 0f && country.reserveAmmo <= 0f)
                NotificationManager.Instance?.AddNotification(
                    "🚨 AMMO DEPLETED: Troops fighting with no ammunition! Casualties are catastrophic.", Color.red);
            if (prevFuel >= 20f && country.reserveFuel < 20f)
                NotificationManager.Instance?.AddNotification(
                    "⚠ FUEL CRITICAL: Armoured divisions halted. Casualty rate +50%.", new Color(1f, 0.4f, 0f));
            if (prevFood >= 15f && country.reserveFood < 15f)
                NotificationManager.Instance?.AddNotification(
                    "⚠ FOOD CRITICAL: Troop morale collapsing. Will to fight draining faster.", new Color(1f, 0.7f, 0f));
        }

        // ─── General Bonus ────────────────────────────────────────────────────
        private float CalculateGeneralBonus(CountryDataSO country)
        {
            float bonus = 1.0f;
            if (country.assignedGenerals == null) return bonus;
            foreach (var gen in country.assignedGenerals)
            {
                if (!gen.isDeployed) continue;
                if (gen.specialty == GeneralSpecialty.Army) bonus += gen.effectivenessBonus;
                country.armyStats.morale = Mathf.Min(100f, country.armyStats.morale + gen.moraleBonus);
            }
            return bonus;
        }

        // ─── End War ──────────────────────────────────────────────────────────
        private void EndWar(CountryDataSO a, CountryDataSO b, string outcome)
        {
            string key = WarKey(a.countryName, b.countryName);
            activeWars.Remove(key);
            warDuration.Remove(key);
            frontProgress.Remove(key);
            lastPhaseMonth.Remove(key);
            momentumVelocity.Remove(key);
            surpriseEventCooldown.Remove(key);

            countryWarStages[a] = WarStage.Peace;
            countryWarStages[b] = WarStage.Peace;

            a.warTurnsActive = 0; a.warExhaustion = 0f; a.warEnemyName = "";
            b.warTurnsActive = 0; b.warExhaustion = 0f; b.warEnemyName = "";
            _lastWarMonthCounted.Remove(a);
            _lastWarMonthCounted.Remove(b);

            a.globalPrestige = Mathf.Max(0f, a.globalPrestige - 5f);
            b.globalPrestige = Mathf.Max(0f, b.globalPrestige - 5f);

            var gm = GameManager.Instance;
            if (gm != null) gm.worldTension = Mathf.Max(0f, gm.worldTension - 15f);

            
            CountryDataSO winner = null;
            CountryDataSO loser = null;

            if (outcome.Contains($"{a.countryName} Victory"))
            {
                winner = a;
                loser = b;
            }
            else if (outcome.Contains($"{b.countryName} Victory"))
            {
                winner = b;
                loser = a;
            }
            
            float spoilsMoney = 0f;
            int spoilsTanks = 0, spoilsAPCs = 0, spoilsArtillery = 0, spoilsFighters = 0;

            if (winner != null && loser != null)
            {
                // Take 60% of treasury
                if (loser.treasury > 0)
                {
                    spoilsMoney = loser.treasury * 0.6f;
                    loser.treasury -= spoilsMoney;
                    winner.treasury += spoilsMoney;
                }

                // Take 60% of military hardware
                spoilsTanks = UnityEngine.Mathf.FloorToInt(loser.unitTanks * 0.6f);
                winner.unitTanks += spoilsTanks;
                loser.unitTanks -= spoilsTanks;

                spoilsAPCs = UnityEngine.Mathf.FloorToInt(loser.unitAPCs * 0.6f);
                winner.unitAPCs += spoilsAPCs;
                loser.unitAPCs -= spoilsAPCs;

                spoilsArtillery = UnityEngine.Mathf.FloorToInt(loser.unitArtillery * 0.6f);
                winner.unitArtillery += spoilsArtillery;
                loser.unitArtillery -= spoilsArtillery;

                spoilsFighters = UnityEngine.Mathf.FloorToInt(loser.unitFighters * 0.6f);
                winner.unitFighters += spoilsFighters;
                loser.unitFighters -= spoilsFighters;
                
                int spoilsBombers = UnityEngine.Mathf.FloorToInt(loser.unitBombers * 0.6f);
                winner.unitBombers += spoilsBombers;
                loser.unitBombers -= spoilsBombers;
                
                int spoilsHelis = UnityEngine.Mathf.FloorToInt(loser.unitAttackHelis * 0.6f);
                winner.unitAttackHelis += spoilsHelis;
                loser.unitAttackHelis -= spoilsHelis;
                
                outcome += $"\nSpoils of War Taken: ${(spoilsMoney/1000f):F1}B, {spoilsTanks} Tanks, {spoilsAPCs} APCs, {spoilsFighters} Fighters.";
            }

            NewsSystem.Instance?.PublishNews($"WAR ENDS: {outcome}. {a.countryName} and {b.countryName} observe ceasefire.");

            bool playerInvolved = gm?.playerCountry == a || gm?.playerCountry == b;
            if (playerInvolved)
            {
                var player = gm.playerCountry;
                bool playerWon = outcome.Contains(player.countryName) && outcome.Contains("Victory");
                InboxSystem.Instance?.SendMessage(player,
                    "JOINT CHIEFS OF STAFF",
                    playerWon ? "🏆 WAR CONCLUDED — VICTORY" : "🕊 WAR CONCLUDED — CEASEFIRE",
                    $"FINAL WAR SUMMARY\n\n" +
                    $"Outcome: {outcome}\n" +
                    $"Duration: {player.warTurnsActive} months\n" +
                    $"Total KIA: {player.totalKIA:N0}\n" +
                    $"Total Wounded: {player.totalWounded:N0}\n\n" +
                    $"The ceasefire is now in effect. Normal diplomatic relations may be re-established " +
                    $"via the Diplomacy tab after a cooling-off period.",
                    "Critical");

                NotificationManager.Instance?.AddNotification(
                    $"🕊 WAR ENDED: {outcome}. Check Events tab for full summary.",
                    playerWon ? Color.green : Color.yellow);
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────
        private List<CountryDataSO> GetAllEnemies(CountryDataSO country)
        {
            var enemies = new List<CountryDataSO>();
            var gm = GameManager.Instance;
            if (gm?.allCountries == null) return enemies;
            string key1 = country.countryName;
            foreach (var other in gm.allCountries)
            {
                if (other == country) continue;
                string key = WarKey(key1, other.countryName);
                if (activeWars.Contains(key)) enemies.Add(other);
            }
            return enemies;
        }

        private float GetFrontlinePowerAgainst(CountryDataSO country, string enemyName, bool isPlayerSide)
        {
            float power = 0f;
            if (country.activeOperations == null) return power;

            float readinessMod = 1f;
            if (country.militaryState?.armyReadiness != null)
                readinessMod = Mathf.Clamp(country.militaryState.armyReadiness.OverallReadiness / 100f, 0.1f, 1.5f);

            foreach (var op in country.activeOperations.Where(o => o.regionName == enemyName && !o.isDecommissioning))
            {
                if (op.turnsToDeploy > 0) continue;
                power += op.deployedTroops   * 1.0f * readinessMod;
                power += op.deployedTanks    * 12f * readinessMod;
                power += op.deployedAPCs     * 5f * readinessMod;
                power += op.deployedFighters * 20f * readinessMod;
                power += op.deployedBombers  * 15f * readinessMod;
                power += op.deployedUAVs     * 3f * readinessMod;
            }

            // Air Superiority Buff
            int ourFighters = country.unitFighters + (country.airForceStats != null ? country.airForceStats.equipment : 0);
            var gm = GameManager.Instance;
            if (gm != null)
            {
                var enemyData = gm.allCountries?.Find(c => c.countryName == enemyName);
                if (enemyData != null)
                {
                    int enemyFighters = enemyData.unitFighters + (enemyData.airForceStats != null ? enemyData.airForceStats.equipment : 0);
                    if (ourFighters > 20 && ourFighters > enemyFighters * 1.5f)
                    {
                        // We have air dominance -> +30% combat power
                        power *= 1.30f;
                    }
                }
            }

            return power;
        }

        private int GetTotalDeployedForces(CountryDataSO country)
        {
            int total = 0;
            if (country.activeOperations == null) return total;
            foreach (var op in country.activeOperations.Where(o => !o.isDecommissioning))
                total += op.deployedTroops;
            return total;
        }

        private string GetRandomEnemyLocation(CountryDataSO enemy)
        {
            if (enemy.regions?.Count > 0)
                return enemy.regions[UnityEngine.Random.Range(0, enemy.regions.Count)].regionName;
            return enemy.countryName;
        }

        private string GetRandomLocation(CountryDataSO country)
        {
            if (country.regions?.Count > 0)
                return country.regions[UnityEngine.Random.Range(0, country.regions.Count)].regionName;
            return country.countryName;
        }

        private static string WarKey(string a, string b)
        {
            var sorted = new[] { a, b };
            System.Array.Sort(sorted);
            return $"{sorted[0]}_vs_{sorted[1]}";
        }

        // ─── Treaty Processing ────────────────────────────────────────────────
        public void ProcessTradeDeals(CountryDataSO country)
        {
            // Treaty expiry handled in DiplomacySystem.ProcessTradeDeals
        }

        // ─── Phase 2 Capabilities (Naval/WMD/Generals/Air) ──────────────────
        public void ProcessNavalBlockades(CountryDataSO country)
        {
            if (country.activeBlockades == null) return;
            for (int i = country.activeBlockades.Count - 1; i >= 0; i--)
            {
                var blockade = country.activeBlockades[i];
                blockade.monthsActive++;
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    var target = gm.GetCountryByName(blockade.targetCountry);
                    if (target != null && GetWarStage(target) == WarStage.Peace)
                    {
                        country.activeBlockades.RemoveAt(i);
                        continue;
                    }
                }
            }
        }

        public void ProcessWMDDevelopment(CountryDataSO country)
        {
            if (country.hasNuclearWeapon) return;

            if (country.wmdDevelopmentProgress > 0 && country.wmdDevelopmentProgress < 100)
            {
                country.wmdDevelopmentProgress += 1;
                if (country.wmdDevelopmentProgress >= 100)
                {
                    country.wmdDevelopmentProgress = 100;
                    country.hasNuclearWeapon = true;
                    if (GameManager.Instance != null && GameManager.Instance.playerCountry == country)
                        NotificationManager.Instance?.AddNotification("🚨 NUCLEAR WEAPON DEVELOPMENT COMPLETE!", Color.red);
                    NewsSystem.Instance?.PublishNews($"GLOBAL ALERT: {country.countryName} has successfully tested a nuclear device. World tension rises.");
                    if (GameManager.Instance != null) GameManager.Instance.worldTension = Mathf.Min(100f, GameManager.Instance.worldTension + 20f);
                }
            }
        }

        public void PromoteGeneral(CountryDataSO country, string name, GeneralSpecialty spec, int rank)
        {
            if (country.assignedGenerals == null) country.assignedGenerals = new List<General>();
            var gen = new General {
                generalName = name,
                specialty = spec,
                rank = rank,
                effectivenessBonus = 0.05f * rank,
                moraleBonus = 2f * rank,
                isDeployed = false
            };
            country.assignedGenerals.Add(gen);
            if (GameManager.Instance?.playerCountry == country)
                NotificationManager.Instance?.AddNotification($"PROMOTION: General {name} commissioned.", Color.green);
        }

        public void DeclareBlockade(CountryDataSO aggressor, CountryDataSO target)
        {
            if (aggressor.activeBlockades == null) aggressor.activeBlockades = new List<NavalBlockade>();
            if (aggressor.unitFrigates > 0 || aggressor.unitDestroyers > 0)
            {
                var block = new NavalBlockade {
                    targetCountry = target.countryName,
                    blockerCountry = aggressor.countryName,
                    monthsActive = 0,
                    frigatesAssigned = Mathf.Min(aggressor.unitFrigates, 3),
                    destroyersAssigned = Mathf.Min(aggressor.unitDestroyers, 1),
                    tradeSuppressionPercent = 30f
                };
                aggressor.activeBlockades.Add(block);
                if (GameManager.Instance?.playerCountry == aggressor)
                    NotificationManager.Instance?.AddNotification($"BLOCKADE: Ordered against {target.countryName}.", Color.yellow);
            }
        }

        public void LaunchAirIntercept(CountryDataSO country, CountryDataSO enemy)
        {
            if (country.reserveFuel >= 40f && country.unitFighters > 0)
            {
                country.reserveFuel -= 40f;
                
                int fightersSent = Mathf.Min(country.unitFighters, 10);
                
                // Roll for air combat
                int bombersShotDown = 0;
                if (enemy.unitBombers > 0)
                {
                    bombersShotDown = Mathf.Min(enemy.unitBombers, Mathf.RoundToInt(fightersSent * UnityEngine.Random.Range(0.2f, 0.5f) + 1));
                    enemy.unitBombers -= bombersShotDown;
                }
                
                int fightersShotDown = 0;
                if (enemy.unitFighters > 0)
                {
                    fightersShotDown = Mathf.Min(enemy.unitFighters, Mathf.RoundToInt(fightersSent * UnityEngine.Random.Range(0.1f, 0.4f)));
                    enemy.unitFighters -= fightersShotDown;
                }
                
                // Our own losses
                int ourLosses = Mathf.Min(country.unitFighters, Mathf.RoundToInt((enemy.unitFighters + enemy.unitBombers) * UnityEngine.Random.Range(0.01f, 0.1f)));
                country.unitFighters -= ourLosses;

                if (GameManager.Instance?.playerCountry == country)
                {
                    string result = $"✈ AIR INTERCEPT SUCCESS: ";
                    if (bombersShotDown > 0 || fightersShotDown > 0)
                    {
                        result += $"Shot down ";
                        if (bombersShotDown > 0) result += $"{bombersShotDown} Bombers ";
                        if (fightersShotDown > 0) result += $"{fightersShotDown} Fighters ";
                        if (ourLosses > 0) result += $" (Lost {ourLosses} Jets)";
                    }
                    else
                    {
                        result += "No enemy aircraft found in sector.";
                    }
                    
                    NotificationManager.Instance?.AddNotification(result, new Color(0.2f, 0.8f, 1f));
                    if (bombersShotDown > 0 || fightersShotDown > 0)
                        NewsSystem.Instance?.PublishNews($"AIR COMBAT: {country.countryName} intercepts and destroys {bombersShotDown + fightersShotDown} {enemy.countryName} aircraft.");
                }
            }
            else
            {
                if (GameManager.Instance?.playerCountry == country)
                    NotificationManager.Instance?.AddNotification("INTERCEPT FAILED: Need 40 fuel and available fighters.", Color.red);
            }
        }
    }
}
