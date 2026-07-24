using System;
using System.Collections.Generic;
using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using UnityEngine;

namespace StateCraft.Systems
{
    // ─── Insurgent Camp Data ──────────────────────────────────────────────────
    [System.Serializable]
    public class InsurgentCamp
    {
        public string campId;
        public string regionName;
        public string campType;          // "Training Base", "Supply Depot", "Command Post", "Hidden Village"
        public float strength;           // 0-100
        public int estimatedFighters;
        public bool isDetected;
        public bool isDestroyed;
        public int turnsActive;
        public int turnsDetected;
        public string detectedBy;
    }

    // ─── Strike Result DTO ────────────────────────────────────────────────────
    public class CampStrikeResult
    {
        public bool success;
        public bool campDestroyed;
        public string message;
        public InsurgentCamp camp;
        public int killEstimate;
        public int fightersLost;
        public int helisLost;
        public float damageDealt;
        public float securityGained;
    }

    public class InsurgencySystem : IInsurgencySystem, ISaveable
    {
        private static readonly HashSet<string> _notifiedCamps = new HashSet<string>();

        [System.Serializable]
        private class InsurgencySaveData
        {
            public List<string> notifiedCamps = new List<string>();
        }

        public string ExportState()
        {
            var data = new InsurgencySaveData();
            foreach (var campId in _notifiedCamps)
            {
                data.notifiedCamps.Add(campId);
            }
            return JsonUtility.ToJson(data);
        }

        public void ImportState(string json)
        {
            var data = JsonUtility.FromJson<InsurgencySaveData>(json);
            if (data == null) return;
            
            _notifiedCamps.Clear();
            foreach (var campId in data.notifiedCamps)
            {
                _notifiedCamps.Add(campId);
            }
        }

        public void SimulateInsurgency(CountryDataSO country)
        {
            if (country.activeInsurgentCamps == null)
                country.activeInsurgentCamps = new List<InsurgentCamp>();

            foreach (var region in country.regions)
            {
                if (region.insurgencyLevel < region.baseInsurgencyRisk)
                    region.insurgencyLevel = Mathf.MoveTowards(region.insurgencyLevel, region.baseInsurgencyRisk, 2.5f);

                if (region.insurgencyLevel > 20f && region.insurgencyIdeology == InsurgencyIdeology.None)
                {
                    region.insurgencyIdeology = (InsurgencyIdeology)UnityEngine.Random.Range(1, 5);
                    NewsSystem.Instance?.PublishNews($"INTELLIGENCE: Militants in {region.regionName} have unified under a {region.insurgencyIdeology} ideology.");
                }

                float growthMultiplier = 1f;
                if (region.insurgencyIdeology == InsurgencyIdeology.Separatist && region.specialization == RegionSpecialization.BorderConflict) growthMultiplier = 1.5f;
                else if (region.insurgencyIdeology == InsurgencyIdeology.ReligiousExtremist && country.stability < 45f) growthMultiplier = 2f;
                else if (region.insurgencyIdeology == InsurgencyIdeology.CommunistMilitia && region.economicContribution < 8f) growthMultiplier = 2f;
                else if (region.insurgencyIdeology == InsurgencyIdeology.Cartel) { growthMultiplier = 1.2f; country.gdp += 0.05f; country.corruptionIndex += 0.5f; }

                if (region.stability < 55f) region.insurgencyLevel += ((55f - region.stability) * 0.25f) * growthMultiplier;
                if (region.heartsAndMinds > 60f) region.insurgencyLevel -= (region.heartsAndMinds - 60f) * 0.15f;
                else if (region.heartsAndMinds < 30f) region.insurgencyLevel += 2f;
                if (region.security > 65f) region.insurgencyLevel -= (region.security - 65f) * 0.15f;

                region.insurgentSupplyLines = Mathf.MoveTowards(region.insurgentSupplyLines, 100f, 2f);
                region.insurgencyLevel = Mathf.Clamp(region.insurgencyLevel, 0f, 100f);

                if (region.insurgencyLevel > 20f) { region.stability -= region.insurgencyLevel * 0.1f; region.security -= region.insurgencyLevel * 0.05f; }

                TrySpawnInsurgentCamp(country, region);
                TryDetectCamps(country, region);

                if (region.insurgencyLevel > 50f && UnityEngine.Random.value > 0.75f)
                    ExecuteAttack(country, region);
                else if (region.insurgencyLevel > 30f && UnityEngine.Random.value > 0.90f)
                    NewsSystem.Instance?.PublishNews($"INTELLIGENCE: Reports suggest growing militant activity in {region.regionName}, {country.countryName}.");
            }

            foreach (var region in country.regions)
            {
                if (region.insurgencyLevel > 40f)
                {
                    foreach (var neighbor in region.adjacentRegions)
                    {
                        float spreadAmount = region.insurgencyLevel * 0.1f;
                        neighbor.insurgencyLevel += spreadAmount;
                        neighbor.stability -= spreadAmount * 0.6f;
                        neighbor.security -= spreadAmount * 0.4f;
                    }
                }
            }

            foreach (var faction in country.activeFactions)
            {
                faction.influence += (1f - (country.stability / 100f)) * (faction.recruitmentRate * 1.5f);
                faction.influence = Mathf.Clamp(faction.influence, 0f, 100f);
            }

            country.activeInsurgentCamps.RemoveAll(c => c.isDestroyed && c.turnsActive > 3);

            foreach (var camp in country.activeInsurgentCamps)
            {
                camp.turnsActive++;
                if (camp.isDetected) camp.turnsDetected++;

                // Follow-up warning if detected but not struck
                if (camp.isDetected && !camp.isDestroyed && camp.turnsDetected == 3 && country == GameManager.Instance?.playerCountry)
                {
                    NotificationManager.Instance?.AddNotification(
                        $"WARNING: Insurgent {camp.campType} in {camp.regionName} still active for {camp.turnsDetected} turns — {camp.estimatedFighters} fighters now entrenched. Deploy air assets before it reinforces further.",
                        new Color(1f, 0.6f, 0f));
                }

                // Undetected / unstrikes camps reinforce
                if (!camp.isDestroyed && !camp.isDetected)
                {
                    camp.strength = Mathf.Min(100f, camp.strength + 1.5f);
                    camp.estimatedFighters = Mathf.RoundToInt(camp.estimatedFighters * 1.01f);
                }
                else if (!camp.isDestroyed && camp.isDetected && camp.turnsDetected > 3)
                {
                    camp.strength = Mathf.Min(100f, camp.strength + 2.5f); // Faster reinforcement once they know they're detected
                    camp.estimatedFighters = Mathf.RoundToInt(camp.estimatedFighters * 1.02f);
                }
            }
        }

        private void TrySpawnInsurgentCamp(CountryDataSO country, RegionDataSO region)
        {
            if (country.activeInsurgentCamps == null) country.activeInsurgentCamps = new List<InsurgentCamp>();
            if (region.insurgencyLevel < 35f) return;

            bool alreadyHasCamp = country.activeInsurgentCamps.Exists(c => c.regionName == region.regionName && !c.isDestroyed);
            if (alreadyHasCamp) return;

            float spawnChance = (region.insurgencyLevel - 35f) / 150f;
            if (UnityEngine.Random.value > spawnChance) return;

            string[] campTypes = { "Training Base", "Supply Depot", "Command Post", "Hidden Village" };
            string campType = campTypes[UnityEngine.Random.Range(0, campTypes.Length)];
            int fighters = Mathf.RoundToInt(region.insurgencyLevel * 4f + UnityEngine.Random.Range(50, 200));

            country.activeInsurgentCamps.Add(new InsurgentCamp
            {
                campId            = $"CAMP_{region.regionName.Replace(" ", "")}_{UnityEngine.Random.Range(1000, 9999)}",
                regionName        = region.regionName,
                campType          = campType,
                strength          = Mathf.Clamp(region.insurgencyLevel + UnityEngine.Random.Range(-10f, 20f), 10f, 100f),
                estimatedFighters = fighters,
                isDetected        = false,
                isDestroyed       = false,
                turnsActive       = 0,
                turnsDetected     = 0
            });
        }

        private void TryDetectCamps(CountryDataSO country, RegionDataSO region)
        {
            if (country.activeInsurgentCamps == null) return;
            bool isPlayer = (country == GameManager.Instance?.playerCountry);

            foreach (var camp in country.activeInsurgentCamps)
            {
                if (camp.regionName != region.regionName || camp.isDetected || camp.isDestroyed) continue;

                float detectionChance = 0.05f;
                bool hasUAVsDeployed = false;

                if (country.activeOperations != null)
                {
                    foreach (var op in country.activeOperations)
                    {
                        if (op.regionName == region.regionName && op.deployedUAVs > 0)
                        { detectionChance += op.deployedUAVs * 0.04f; hasUAVsDeployed = true; }
                    }
                }

                detectionChance += (region.security / 100f) * 0.15f;
                detectionChance += camp.turnsActive * 0.02f;
                detectionChance = Mathf.Clamp(detectionChance, 0f, 0.75f);

                if (UnityEngine.Random.value > detectionChance) continue;

                camp.isDetected = true;
                camp.detectedBy = hasUAVsDeployed ? "UAV Reconnaissance" : "Ground Intelligence Network";

                if (!isPlayer) continue;

                string notifKey = camp.campId;
                if (_notifiedCamps.Contains(notifKey)) continue;
                _notifiedCamps.Add(notifKey);

                // Inbox briefing
                InboxSystem.Instance?.SendMessage(country,
                    "DIRECTORATE OF MILITARY INTELLIGENCE",
                    $"CAMP DETECTED: Insurgent {camp.campType} — {camp.regionName}",
                    $"Source: {camp.detectedBy}\n\n" +
                    $"A clandestine insurgent {camp.campType} has been confirmed in {camp.regionName}. " +
                    $"Estimated {camp.estimatedFighters} armed militants, camp strength {camp.strength:F0}%.\n\n" +
                    $"THREAT: Active recruitment and training in progress. This camp will reinforce rapidly if not struck.\n\n" +
                    $"RECOMMENDED ACTION: Open the MILITARY OPERATIONS panel → COIN QUICK ACTIONS section → " +
                    $"'BOMB DETECTED INSURGENT CAMP' to assign air assets and destroy this position immediately.\n\n" +
                    $"WARNING: After 3 turns without a strike, camp strength increases by +25% and fighter count grows by 20%.",
                    "Critical");

                // Pop-up notification
                NotificationManager.Instance?.AddNotification(
                    $"🎯 CAMP DETECTED: Insurgent {camp.campType} in {camp.regionName} — ~{camp.estimatedFighters} fighters. " +
                    $"Go to MILITARY OPS → BOMB DETECTED CAMP to strike now!",
                    new Color(1f, 0.35f, 0f));

                NewsSystem.Instance?.PublishNews(
                    $"MILITARY INTELLIGENCE: {camp.detectedBy} has confirmed a clandestine insurgent {camp.campType} in {camp.regionName}. Strike options under evaluation.");
            }
        }

        // ─── Air Strike on Detected Camp (called from MilitaryDashboardController) ──
        public CampStrikeResult StrikeInsurgentCamp(CountryDataSO country, InsurgentCamp camp, int fighters, int bombers, int attackHelis, int uavs)
        {
            var result = new CampStrikeResult { camp = camp };

            if (camp == null || camp.isDestroyed)
            { result.success = false; result.message = "Camp no longer exists."; return result; }

            if (fighters + bombers + attackHelis + uavs == 0)
            { result.success = false; result.message = "Assign at least one air asset to conduct this strike."; return result; }

            float fuelRequired = (fighters * 20f) + (bombers * 35f) + (attackHelis * 15f) + (uavs * 8f);
            if (country.reserveFuel < fuelRequired)
            { result.success = false; result.message = $"Insufficient fuel. Need {fuelRequired:F0}, have {country.reserveFuel:F0}."; return result; }

            // Deduct assets
            country.reserveFuel     = Mathf.Max(0f, country.reserveFuel - fuelRequired);
            country.unitFighters    = Mathf.Max(0, country.unitFighters - fighters);
            country.unitBombers     = Mathf.Max(0, country.unitBombers - bombers);
            country.unitAttackHelis = Mathf.Max(0, country.unitAttackHelis - attackHelis);
            country.unitUAVs        = Mathf.Max(0, country.unitUAVs - uavs);

            float strikePower = (fighters * 15f) + (bombers * 40f) + (attackHelis * 20f) + (uavs * 8f);
            float campDefense = camp.strength * 0.5f;
            float netDamage   = Mathf.Clamp(strikePower - campDefense, 10f, strikePower * 1.5f);

            // AA fire risk
            float aaRisk = camp.strength / 200f;
            int fightersLost = 0, helisLost = 0;
            if (UnityEngine.Random.value < aaRisk)
            {
                fightersLost = UnityEngine.Random.Range(0, Mathf.Max(1, fighters / 2 + 1));
                helisLost    = UnityEngine.Random.Range(0, Mathf.Max(1, attackHelis / 2 + 1));
                country.unitFighters    = Mathf.Max(0, country.unitFighters    - fightersLost);
                country.unitAttackHelis = Mathf.Max(0, country.unitAttackHelis - helisLost);
            }

            camp.strength -= netDamage;
            int killEstimate = Mathf.RoundToInt(camp.estimatedFighters * (netDamage / 100f));
            camp.estimatedFighters = Mathf.Max(0, camp.estimatedFighters - killEstimate);

            if (camp.strength <= 0f || camp.estimatedFighters <= 0)
            { camp.strength = 0f; camp.estimatedFighters = 0; camp.isDestroyed = true; result.campDestroyed = true; }

            result.success      = true;
            result.killEstimate = killEstimate;
            result.fightersLost = fightersLost;
            result.helisLost    = helisLost;
            result.damageDealt  = netDamage;

            var targetRegion = country.regions.Find(r => r.regionName == camp.regionName);
            if (targetRegion != null)
            {
                float secBoost = result.campDestroyed ? 15f : netDamage * 0.3f;
                targetRegion.security        = Mathf.Min(100f, targetRegion.security + secBoost);
                targetRegion.insurgencyLevel = Mathf.Max(0f, targetRegion.insurgencyLevel - secBoost * 0.6f);
                result.securityGained        = secBoost;
            }

            string lossNote = (fightersLost + helisLost > 0) ? $" Friendly losses: {fightersLost} fighter(s), {helisLost} heli(s) lost to AA." : " No friendly losses.";
            result.message = result.campDestroyed
                ? $"DESTROYED: Insurgent {camp.campType} in {camp.regionName} neutralized. ~{killEstimate} fighters killed. Security +{result.securityGained:F0}%.{lossNote}"
                : $"STRIKE COMPLETE: {camp.campType} damaged ({netDamage:F0}% damage). ~{killEstimate} killed, ~{camp.estimatedFighters} remain — follow-up strike recommended.{lossNote}";

            // Remove from notification cache if destroyed (so a new camp spawning will fire again)
            if (result.campDestroyed) _notifiedCamps.Remove(camp.campId);

            return result;
        }

        private void PublishInsurgencyAttack(RegionDataSO region, float severity)
        {
            EventBus.Publish(new WorldEvent
            {
                eventType    = WorldEventType.InsurgencyAttack,
                title        = $"INSURGENCY ATTACK IN {region.regionName.ToUpper()}",
                description  = "Armed insurgent activity has destabilized the region.",
                severity     = AlertSeverity.Important,
                targetRegion = region,
                timestamp    = DateTime.Now,
                impactValue  = severity
            });
        }

        private void ExecuteAttack(CountryDataSO country, RegionDataSO region)
        {
            float stabilityLoss = UnityEngine.Random.Range(20f, 35f);
            float securityLoss  = UnityEngine.Random.Range(15f, 30f);
            float supplyEff     = region.insurgentSupplyLines / 100f;
            stabilityLoss *= supplyEff; securityLoss *= supplyEff;
            if (region.specialization == RegionSpecialization.Urban) stabilityLoss *= 2f;

            DiplomaticTreaty coalition = country.activeTreaties.Find(t => t.treatyName.Contains("Coalition"));
            if (coalition != null)
            {
                stabilityLoss *= 0.25f; securityLoss *= 0.25f;
                region.stability -= stabilityLoss; region.security -= securityLoss; country.stability -= stabilityLoss * 0.3f;
                NewsSystem.Instance?.PublishNews($"INSURGENCY: Joint coalition forces defended {region.regionName} — damage minimized.");
            }
            else
            {
                region.stability -= stabilityLoss; region.security -= securityLoss; country.stability -= stabilityLoss * 0.4f;
                string headline = $"BREAKING: Armed insurgency erupts in {region.regionName}, {country.countryName}! Local forces overwhelmed, heavy casualties reported.";
                NewsSystem.Instance?.PublishNews(headline);
                if (country == GameManager.Instance?.playerCountry)
                    NotificationManager.Instance?.AddNotification(headline, Color.red);
            }
            PublishInsurgencyAttack(region, stabilityLoss);
        }
    }
}
