using StateCraft.Data;
using UnityEngine;

namespace StateCraft.Systems
{
    /// <summary>
    /// Phase 3: Drives public approval dynamically based on economic health, 
    /// stability, war fatigue, corruption, and food security.
    /// Approval directly drives Election outcomes.
    /// </summary>
    public static class ApprovalSystem
    {
        public static void ProcessApproval(CountryDataSO country)
        {
            if (country == null) return;
            
            float approvalDelta = 0f;

            // 1. Economic Health (Quick heuristic: is income outpacing base spending?)
            float incomeLocal = country.income * country.exchangeRate;
            float spendingLocal = country.spending * country.exchangeRate;
            if (incomeLocal > spendingLocal * 1.1f)
                approvalDelta += 0.5f;
            else if (incomeLocal < spendingLocal)
                approvalDelta -= 1.5f;

            // 2. Stability
            if (country.stability > 70f) approvalDelta += 0.5f;
            else if (country.stability < 40f) approvalDelta -= 2.0f;

            // 3. War Fatigue
            if (country.warTurnsActive > 6) approvalDelta -= 1.0f;
            if (country.warExhaustion > 60f) approvalDelta -= 2.0f;

            // 4. Corruption
            if (country.corruptionIndex > 50f) approvalDelta -= 1.0f;

            // 5. Food Security (Famine Risk)
            if (country.foodSecurity < 30f) approvalDelta -= 3.0f;

            // Apply Delta
            country.publicApproval = Mathf.Clamp(country.publicApproval + approvalDelta, 0f, 100f);
        }
    }
}
