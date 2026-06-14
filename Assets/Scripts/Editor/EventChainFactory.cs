using UnityEngine;
using UnityEditor;
using StateCraft.Data;
using System.IO;

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
            crisis.optionAText = "Print Money";
            crisis.optionAStability = -2f;
            crisis.optionAGDP = 0f;
            crisis.optionBText = "Borrow Foreign Aid";
            crisis.optionBCost = -5f; // Positive treasury injection but with debt implication later
            crisis.optionBStability = 0f;

            GameEventSO inflation = ScriptableObject.CreateInstance<GameEventSO>();
            inflation.eventTitle = "Inflation Surge";
            inflation.description = "The decision to print money has devalued our currency. Savings have been wiped out, and public anger is mounting rapidly.";
            inflation.optionAText = "Subsidize Basic Goods";
            inflation.optionACost = 3f;
            inflation.optionAStability = 5f;
            inflation.optionBText = "Ignore The Crisis";
            inflation.optionBCost = 0f;
            inflation.optionBStability = -15f;

            GameEventSO debtCrisis = ScriptableObject.CreateInstance<GameEventSO>();
            debtCrisis.eventTitle = "Debt Crisis";
            debtCrisis.description = "Our creditors are demanding immediate repayment of our recent massive loans. We must either default or enact severe austerity.";
            debtCrisis.optionAText = "Enact Austerity";
            debtCrisis.optionACost = 0f;
            debtCrisis.optionAStability = -20f;
            debtCrisis.optionBText = "Default on Debt";
            debtCrisis.optionBCost = 0f;
            debtCrisis.optionBStability = -10f; // Leads to debt default event via Economy system dynamically

            // Link them
            crisis.nextEventIfA = inflation;
            crisis.nextEventIfB = debtCrisis;
            crisis.turnsUntilNextEvent = 2; // Trigger consequence 2 months later

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
            coup.optionAText = "Crack Down Hard";
            coup.optionACost = 2f;
            coup.optionASecurity = -10f;
            coup.optionAStability = 5f; 
            coup.optionBText = "Attempt Negotiations";
            coup.optionBCost = 0f;
            coup.optionBStability = -5f;

            GameEventSO humanRights = ScriptableObject.CreateInstance<GameEventSO>();
            humanRights.eventTitle = "Human Rights Criticism";
            humanRights.description = "The international community heavily condemns our violent suppression of the coup. Sanctions are being considered.";
            humanRights.optionAText = "Defend Actions";
            humanRights.optionAStability = 5f;
            humanRights.optionBText = "Apologize";
            humanRights.optionBStability = -5f;

            GameEventSO factionSplit = ScriptableObject.CreateInstance<GameEventSO>();
            factionSplit.eventTitle = "Military Fracture";
            factionSplit.description = "By negotiating, we showed weakness. The military has fractured into loyalist and rebel factions, plunging us into civil strife.";
            factionSplit.optionAText = "Mobilize Loyalists";
            factionSplit.optionASecurity = -20f;
            factionSplit.optionBText = "Flee The Capital";
            factionSplit.optionBStability = -30f;

            // Link them
            coup.nextEventIfA = humanRights;
            coup.nextEventIfB = factionSplit;
            coup.turnsUntilNextEvent = 1;

            // Save Chain 2
            AssetDatabase.CreateAsset(humanRights, $"{folderPath}/Event_HumanRights.asset");
            AssetDatabase.CreateAsset(factionSplit, $"{folderPath}/Event_MilitaryFracture.asset");
            AssetDatabase.CreateAsset(coup, $"{folderPath}/Event_CoupAttempt.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
}
    }
}
