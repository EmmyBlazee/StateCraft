using UnityEngine;
using StateCraft.Interfaces;
using StateCraft.Data;

namespace StateCraft.Systems
{
    public class IntelligenceSystem : IIntelligenceSystem
    {
        public void SimulateIntelligence(CountryDataSO country)
        {
            country.counterIntelligence = Mathf.Clamp(30f + (country.covertBudget * 3f) + (country.agencyIntegrity * 0.2f), 10f, 95f);
            country.intelligenceLevel   = Mathf.Clamp(country.intelligenceLevel * 0.98f + (country.covertBudget * 2f), 5f, 100f);

            float coverageGrowth = (country.covertBudget * 0.5f) + (country.satelliteCount * 1.5f);
            country.globalIntelCoverage = Mathf.Clamp(country.globalIntelCoverage + coverageGrowth - 1f, 5f, 100f);

            if (country.satellitesUnderConstruction > 0)
            {
                if (country.satelliteBuildMonthsRemaining > 0)
                {
                    country.satelliteBuildMonthsRemaining--;
                }
                
                if (country.satelliteBuildMonthsRemaining <= 0)
                {
                    country.satellitesUnderConstruction--;
                    country.satelliteCount++;
                    float coverageGain = country.satelliteCount == 1 ? 15f : country.satelliteCount == 2 ? 12f : 8f;
                    country.globalIntelCoverage = Mathf.Min(100f, country.globalIntelCoverage + coverageGain);
                    
                    StateCraft.UI.NotificationManager.Instance?.AddNotification($"SPY SATELLITE LAUNCHED. IMINT coverage +{coverageGain:F0}%. Total constellation: {country.satelliteCount} sat(s).", new Color(186/255f, 104/255f, 200/255f));
                    StateCraft.Systems.NewsSystem.Instance?.PublishNews($"SPACE COMMAND: {country.countryName} successfully deployed a new reconnaissance satellite into orbit.");
                }
            }

            if (country.covertBudget < 0.5f)
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

                    float riskGain = Random.Range(2f, 8f) + (targetCI * 0.1f) - (country.covertBudget * 0.5f);
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