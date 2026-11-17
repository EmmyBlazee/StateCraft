using UnityEngine;
using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.UI;

namespace StateCraft.Systems
{
    public class IntelligenceSystem : IIntelligenceSystem
    {
        public void SimulateIntelligence(CountryDataSO country)
        {
            if (country.covertBudget > 0f)
            {
                country.treasury -= country.covertBudget;
                country.intelligenceFunds += (country.covertBudget * 1000f);
            }
            float satelliteCost = country.satelliteCount * 50f;
            float droneCost = country.droneCount * 10f;
            float operativeCost = country.operativeCount * 5f;
            float totalRunningCost = satelliteCost + droneCost + operativeCost;

            if (country.intelligenceFunds >= totalRunningCost)
            {
                country.intelligenceFunds -= totalRunningCost;
            }
            else
            {
                country.intelligenceFunds = 0f;
                country.agencyIntegrity = Mathf.Max(10f, country.agencyIntegrity - 3f);
                country.intelligenceLevel = Mathf.Max(5f, country.intelligenceLevel - 1f);
                StateCraft.UI.NotificationManager.Instance?.AddNotification($"CRITICAL SHORTAGE: Intelligence funds depleted! Agency integrity dropping. Increase Budget.", Color.red);
            }

            if (country.intelligenceFunds > 100f && country.operativeCount < 20 && Random.value < 0.25f)
            {
                float recruitCost = 50f; 
                if (country.intelligenceFunds >= recruitCost)
                {
                    country.intelligenceFunds -= recruitCost;
                    country.operativeCount++;
                    StateCraft.UI.NotificationManager.Instance?.AddNotification($"AGENCY UPDATE: New operative successfully recruited and deployed.", Color.cyan);
                }
            }
            
            if (country.intelligenceFunds > 200f && country.activeCovertOps.Count < 3 && Random.value < 0.15f && country.operativeCount >= 2)
            {
                float opCost = 100f;
                if (country.intelligenceFunds >= opCost)
                {
                    country.intelligenceFunds -= opCost;
                    country.activeCovertOps.Add(new CovertOperation 
                    {
                        opName = "Operation " + (Random.value < 0.5f ? "Silent Night" : "Black Watch"),
                        target = "Unknown Rival",
                        type = "Intelligence Gathering",
                        riskLevel = "Medium",
                        totalPhases = 3,
                        currentPhase = 1,
                        monthsRemaining = 3,
                        detectionRisk = 15f
                    });
                    StateCraft.UI.NotificationManager.Instance?.AddNotification($"AGENCY UPDATE: Covert Operation launched autonomously by Intelligence Directorate.", Color.cyan);
                }
            }

            if (country.globalIntelCoverage < 30f && country.satelliteCount < 2)
            {
                StateCraft.UI.NotificationManager.Instance?.AddNotification($"ADVISORY: Global intelligence coverage is critically low ({country.globalIntelCoverage:F0}%). Procure a Spy Satellite.", new Color(1f, 0.6f, 0f));
            }
            if (country.intelligenceLevel < 40f && country.droneCount < 2)
            {
                StateCraft.UI.NotificationManager.Instance?.AddNotification($"ADVISORY: Local tactical intelligence is weak. Deploy Recon Drones.", new Color(1f, 0.6f, 0f));
            }

            country.counterIntelligence = Mathf.Clamp(30f + (country.intelligenceBudget * 3f) + (country.agencyIntegrity * 0.2f), 10f, 95f);
            country.intelligenceLevel   = Mathf.Clamp(country.intelligenceLevel * 0.98f + (country.intelligenceBudget * 2f), 5f, 100f);
            
            country.cyberDefenseLevel = Mathf.Clamp(country.cyberDefenseLevel * 0.98f + (country.intelligenceLevel * 0.02f) + (country.intelligenceBudget * 1.5f), 5f, 100f);
            country.signalInterceptCapability = Mathf.Clamp(country.signalInterceptCapability * 0.98f + (country.intelligenceLevel * 0.02f) + (country.satelliteCount * 4f) + (country.droneCount * 2f) + (country.intelligenceBudget * 1f), 5f, 100f);

            float coverageGrowth = (country.intelligenceBudget * 0.5f) + (country.satelliteCount * 1.5f) + (country.droneCount * 0.5f);
            country.globalIntelCoverage = Mathf.Clamp(country.globalIntelCoverage + coverageGrowth - 1f, 5f, 100f);

            if (country.intelHardware != null)
            {
                foreach (var hw in country.intelHardware)
                {
                    if (hw.status == "Building")
                    {
                        if (hw.monthsRemaining > 0)
                            hw.monthsRemaining--;
                        
                        if (hw.monthsRemaining <= 0)
                        {
                            hw.status = "Active";
                            if (hw.type == "Satellite")
                            {
                                country.satellitesUnderConstruction = Mathf.Max(0, country.satellitesUnderConstruction - 1);
                                country.satelliteCount++;
                                float coverageGain = country.satelliteCount == 1 ? 15f : country.satelliteCount == 2 ? 12f : 8f;
                                country.globalIntelCoverage = Mathf.Min(100f, country.globalIntelCoverage + coverageGain);
                                StateCraft.UI.NotificationManager.Instance?.AddNotification($"SPY SATELLITE '{hw.assetName}' LAUNCHED. IMINT coverage +{coverageGain:F0}%.", new Color(186/255f, 104/255f, 200/255f));
                                StateCraft.Systems.NewsSystem.Instance?.PublishNews($"SPACE COMMAND: {country.countryName} successfully deployed {hw.assetName} into orbit.");
                            }
                            else if (hw.type == "Drone")
                            {
                                country.dronesUnderConstruction = Mathf.Max(0, country.dronesUnderConstruction - 1);
                                country.droneCount++;
                                country.intelligenceLevel = Mathf.Min(100f, country.intelligenceLevel + 5f);
                                StateCraft.UI.NotificationManager.Instance?.AddNotification($"RECON DRONES '{hw.assetName}' DEPLOYED. Local intel boosted.", new Color(35/255f, 237/255f, 86/255f));
                                StateCraft.Systems.NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} has activated a new drone fleet named {hw.assetName}.");
                            }
                        }
                    }
                }
            }

            if (country.intelligenceBudget < 0.5f)
                country.agencyIntegrity = Mathf.Max(10f, country.agencyIntegrity - 1.5f);

            if (UnityEngine.Random.value < 0.08f)
            {
                float detectionChance = country.counterIntelligence / 100f;
                if (UnityEngine.Random.value < detectionChance * 0.4f)
                {
                    country.foreignAgentsDetected = Mathf.Min(10, country.foreignAgentsDetected + 1);
                    StateCraft.UI.NotificationManager.Instance?.AddNotification(
                        $"COUNTER-INTEL ALERT: Foreign operative detected inside {country.countryName}. Check Counter-Espionage tab.",
                        new Color(1f, 0.67f, 0.25f));
                }
            }

            if (country.activeCovertOps == null || country.activeCovertOps.Count == 0)
            {
                float decayRate = 3f + (country.agencyIntegrity * 0.05f);
                country.exposureRisk = Mathf.Max(0f, country.exposureRisk - decayRate);
            }

            if (country.activeCovertOps != null && country.activeCovertOps.Count > 0)
            {
                float exposureSpike = country.activeCovertOps.Count * 2.5f;
                country.exposureRisk = Mathf.Clamp(country.exposureRisk + exposureSpike, 0f, 100f);

                for (int i = country.activeCovertOps.Count - 1; i >= 0; i--)
                {
                    var op = country.activeCovertOps[i];
                    var targetCountry = Core.GameManager.Instance?.GetCountryByName(op.target);
                    
                    float targetCI = targetCountry != null ? targetCountry.counterIntelligence : 50f;

                    op.monthsRemaining--;

                    float riskGain = Random.Range(2f, 8f) + (targetCI * 0.1f) - (country.intelligenceBudget * 0.5f);
                    if (op.riskLevel == "High") riskGain += 4f;
                    else if (op.riskLevel == "Extreme") riskGain += 8f;

                    if (country.exposureRisk > 80f) riskGain *= 1.5f;

                    op.detectionRisk = Mathf.Clamp(op.detectionRisk + riskGain, 0f, 100f);

                    if (op.monthsRemaining <= 0)
                    {
                        if (op.currentPhase < op.totalPhases)
                        {
                            op.currentPhase++;
                            op.monthsRemaining = 3;
                            StateCraft.UI.NotificationManager.Instance?.AddNotification(
                                $"PHASE PROGRESS: [📡 COVERT OPS] {op.opName} in {op.target} progressed to Phase {op.currentPhase}/{op.totalPhases}.",
                                Color.cyan);
                        }
                        else
                        {
                            country.operativeCount = Mathf.Min(country.operativeCount + 1, 20);
                            StateCraft.UI.NotificationManager.Instance?.AddNotification(
                                $"SUCCESS: [📡 COVERT OPS] {op.opName} completed! Agent extracted.",
                                Color.green);

                            if (op.type == "Propaganda" && targetCountry != null)
                            {
                                targetCountry.stability = Mathf.Max(0, targetCountry.stability - 15f);
                                NewsSystem.Instance?.PublishNews($"SUBVERSION: Massive protests erupt in {op.target} following coordinated illicit media campaigns.");
                            }
                            else if (op.type == "Sabotage" && targetCountry != null)
                            {
                                targetCountry.gdp *= 0.95f;
                                NewsSystem.Instance?.PublishNews($"TERROR: Major industrial pipelines destroyed in {op.target} by unknown saboteurs!");
                            }
                            else if (op.type == "Asset Recruitment")
                            {
                                country.recruitedAssets.Add(new ForeignAsset { targetCountry = op.target, assetName = "Senior Official Mole", loyalty = 80f, intelLevelProvided = 3 });
                                StateCraft.UI.NotificationManager.Instance?.AddNotification($"ASSET RECRUITED: We now have a high-level mole inside {op.target}'s government.", Color.magenta);
                            }
                            else
                            {
                                NewsSystem.Instance?.PublishNews($"INTELLIGENCE: High-grade diplomatic intelligence report leaked from {op.target}.");
                            }
                            
                            country.activeCovertOps.RemoveAt(i);
                        }
                    }
                    else if (op.detectionRisk >= 100f || (op.detectionRisk > 85f && Random.value < 0.25f))
                    {
                        StateCraft.UI.NotificationManager.Instance?.AddNotification(
                            $"CRITICAL COMPROMISE: [📡 COVERT OPS] {op.opName} in {op.target} discovered! Operative captured.",
                            Color.red);
                        
                        targetCountry.globalPrestige += 5f;
                        country.globalPrestige -= 10f;
                        country.exposureRisk += 20f;

                        NewsSystem.Instance?.PublishNews($"INTERNATIONAL INCIDENT: {op.target} counter-intelligence forces publicly parade captured {country.countryName} spy!");
                        
                        country.activeCrises.Add(new GlobalCrisis { crisisName = $"Diplomatic Blowout: Caught Spying on {op.target}", severity = "Critical", internationalPressure = 90f });

                        country.activeCovertOps.RemoveAt(i);
                    }
                }
            }

            if (country.activeRecommendations != null && country.activeRecommendations.Count > 0)
            {
                for (int i_rec = country.activeRecommendations.Count - 1; i_rec >= 0; i_rec--)
                {
                    var rec = country.activeRecommendations[i_rec];
                    
                    if (rec.isAddressed) continue;

                    rec.turnsActive++;

                    if (rec.urgency == "Low" && rec.turnsActive >= 3)
                    {
                        rec.urgency = "Medium";
                        if (country == Core.GameManager.Instance?.playerCountry)
                        {
                            NotificationManager.Instance?.AddNotification(
                                $"⚠ INTEL WARNING: Threat in {rec.regionName} ({rec.threatType}) has escalated to MEDIUM urgency.",
                                new Color(1f, 0.6f, 0f));
                        }
                    }
                    else if (rec.urgency == "Medium" && rec.turnsActive >= 6)
                    {
                        rec.urgency = "High";
                        if (country == Core.GameManager.Instance?.playerCountry)
                        {
                            NotificationManager.Instance?.AddNotification(
                                $"🚨 CRITICAL INTEL: Threat in {rec.regionName} ({rec.threatType}) has escalated to HIGH urgency! Immediate intervention required.",
                                new Color(1f, 0.22f, 0.22f));
                            NewsSystem.Instance?.PublishNews($"CRISIS: Intelligence reports critical instability in {rec.regionName} due to unaddressed {rec.threatType}!");
                        }
                    }
                }
            }

            if (country.regions != null)
            {
                foreach (var region in country.regions)
                {
                    if (region.security < 50f)
                    {
                        var existing = country.activeRecommendations.Find(r => r.regionName == region.regionName && r.threatType == "Terrorist Presence");
                        if (existing == null)
                        {
                            country.activeRecommendations.RemoveAll(r => r.regionName == region.regionName);
                            country.activeRecommendations.Add(new ThreatRecommendation
                            {
                                regionName = region.regionName,
                                threatType = "Terrorist Presence",
                                urgency = "High",
                                justification = $"Regional security dropped to {region.security:F0}%. High terrorist cell concentration detected.",
                                turnsActive = 0,
                                isAddressed = false
                            });
                        }
                    }
                    else if (region.security < 75f)
                    {
                        string threat = (region.regionName.Contains("Delta") || region.regionName.Contains("Coast")) ? "Piracy" : "Civil Unrest";
                        var existing = country.activeRecommendations.Find(r => r.regionName == region.regionName && (r.threatType == "Civil Unrest" || r.threatType == "Piracy"));
                        if (existing == null)
                        {
                            country.activeRecommendations.RemoveAll(r => r.regionName == region.regionName);
                            country.activeRecommendations.Add(new ThreatRecommendation
                            {
                                regionName = region.regionName,
                                threatType = threat,
                                urgency = "Medium",
                                justification = threat == "Piracy" ? "Commercial shipping channels report 34% increase in hostile intercepts." : $"Security deterioration detected ({region.security:F0}%). Riot warning active.",
                                turnsActive = 0,
                                isAddressed = false
                            });
                        }
                    }
                    else
                    {
                        country.activeRecommendations.RemoveAll(r => r.regionName == region.regionName);
                    }
                }
            }
        }

        public string GetFormattedData(float actualValue, float intelLevel, string format = "N1")
        {
            if (intelLevel < 20f)
            {
                return "???";
            }
            else if (intelLevel < 60f)
            {
                float variance = actualValue * 0.2f;
                return $"~{(actualValue - variance).ToString(format)} - {(actualValue + variance).ToString(format)}";
            }
            else
            {
                return actualValue.ToString(format);
            }
        }
    }
}