using UnityEngine;
using UnityEditor;
using StateCraft.Data;
using System.Collections.Generic;

namespace StateCraft.Editor
{
    public class EventChainFactory
    {
        [MenuItem("StateCraft/Generate Event Chains")]
        public static void GenerateDefaultChains()
        {
            string folderPath = "Assets/Resources/Events/Chains";
            
            // Create directories safely
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Events"))
                AssetDatabase.CreateFolder("Assets/Resources", "Events");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Events/Chains"))
                AssetDatabase.CreateFolder("Assets/Resources/Events", "Chains");

            // ==========================================
            // CHAIN 1: Economic Crisis -> Inflation / Debt
            // ==========================================
            GameEventSO crisis = ScriptableObject.CreateInstance<GameEventSO>();
            crisis.eventTitle = "Economic Crisis";
            crisis.description = "Treasury reserves are dwindling rapidly due to unforeseen market shocks. The people are looking for an immediate solution.";
            crisis.severity = "High";
            crisis.timeRemainingDays = 12;
            crisis.turnsUntilNextEvent = 2;

            GameEventSO inflation = ScriptableObject.CreateInstance<GameEventSO>();
            inflation.eventTitle = "Inflation Surge";
            inflation.description = "The decision to print money has devalued our currency. Savings have been wiped out, and public anger is mounting rapidly.";
            inflation.severity = "Critical";
            inflation.timeRemainingDays = 6;
            
            GameEventSO debtCrisis = ScriptableObject.CreateInstance<GameEventSO>();
            debtCrisis.eventTitle = "Debt Crisis";
            debtCrisis.description = "Our creditors are demanding immediate repayment of our recent massive loans. We must either default or enact severe austerity.";
            debtCrisis.severity = "Critical";
            debtCrisis.timeRemainingDays = 6;

            crisis.choices = new List<GameEventChoice> {
                new GameEventChoice {
                    choiceText = "Print Money",
                    stabilityEffect = -2f,
                    gdpEffect = 0f,
                    nextEvent = inflation
                },
                new GameEventChoice {
                    choiceText = "Borrow Foreign Aid",
                    costUSD = -5f, // Gain 5B
                    stabilityEffect = 0f,
                    nextEvent = debtCrisis
                }
            };

            inflation.choices = new List<GameEventChoice> {
                new GameEventChoice {
                    choiceText = "Subsidize Basic Goods",
                    costUSD = 3f,
                    stabilityEffect = 5f
                },
                new GameEventChoice {
                    choiceText = "Ignore The Crisis",
                    costUSD = 0f,
                    stabilityEffect = -15f
                }
            };

            debtCrisis.choices = new List<GameEventChoice> {
                new GameEventChoice {
                    choiceText = "Enact Austerity",
                    costUSD = 0f,
                    stabilityEffect = -20f
                },
                new GameEventChoice {
                    choiceText = "Default on Debt",
                    costUSD = 0f,
                    stabilityEffect = -10f
                }
            };

            // Save Chain 1
            AssetDatabase.CreateAsset(debtCrisis, $"{folderPath}/Event_DebtCrisis.asset");
            AssetDatabase.CreateAsset(inflation, $"{folderPath}/Event_InflationSurge.asset");
            AssetDatabase.CreateAsset(crisis, $"{folderPath}/Event_EconomicCrisis.asset");

            // ==========================================
            // CHAIN 2: Coup Attempt -> Human Rights / Fracture
            // ==========================================
            GameEventSO coup = ScriptableObject.CreateInstance<GameEventSO>();
            coup.eventTitle = "Coup Attempt";
            coup.description = "Dissident generals have mobilized troops in the capital. They demand your immediate resignation.";
            coup.severity = "Critical";
            coup.timeRemainingDays = 2;
            coup.turnsUntilNextEvent = 1;

            GameEventSO humanRights = ScriptableObject.CreateInstance<GameEventSO>();
            humanRights.eventTitle = "Human Rights Criticism";
            humanRights.description = "The international community heavily condemns our violent suppression of the coup. Sanctions are being considered.";
            humanRights.severity = "High";
            humanRights.timeRemainingDays = 12;

            GameEventSO factionSplit = ScriptableObject.CreateInstance<GameEventSO>();
            factionSplit.eventTitle = "Military Fracture";
            factionSplit.description = "By negotiating, we showed weakness. The military has fractured into loyalist and rebel factions, plunging us into civil strife.";
            factionSplit.severity = "Critical";
            factionSplit.timeRemainingDays = 6;

            coup.choices = new List<GameEventChoice> {
                new GameEventChoice {
                    choiceText = "Crack Down Hard",
                    costUSD = 2f,
                    securityEffect = -10f,
                    stabilityEffect = 5f,
                    nextEvent = humanRights
                },
                new GameEventChoice {
                    choiceText = "Attempt Negotiations",
                    costUSD = 0f,
                    stabilityEffect = -5f,
                    nextEvent = factionSplit
                }
            };

            humanRights.choices = new List<GameEventChoice> {
                new GameEventChoice {
                    choiceText = "Defend Actions",
                    stabilityEffect = 5f
                },
                new GameEventChoice {
                    choiceText = "Apologize",
                    stabilityEffect = -5f
                }
            };

            factionSplit.choices = new List<GameEventChoice> {
                new GameEventChoice {
                    choiceText = "Mobilize Loyalists",
                    securityEffect = -20f
                },
                new GameEventChoice {
                    choiceText = "Flee The Capital",
                    stabilityEffect = -30f
                }
            };

            // Save Chain 2
            AssetDatabase.CreateAsset(humanRights, $"{folderPath}/Event_HumanRights.asset");
            AssetDatabase.CreateAsset(factionSplit, $"{folderPath}/Event_MilitaryFracture.asset");
            AssetDatabase.CreateAsset(coup, $"{folderPath}/Event_CoupAttempt.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
