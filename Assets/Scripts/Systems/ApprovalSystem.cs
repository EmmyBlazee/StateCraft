using StateCraft.Data;
using UnityEngine;

namespace StateCraft.Systems
{
    public static class ApprovalSystem
    {
        public static void ProcessApproval(CountryDataSO country)
        {
            if (country == null) return;
            
            float approvalDelta = 0f;

            float incomeLocal = country.income * country.exchangeRate;
            float spendingLocal = country.spending * country.exchangeRate;
            if (incomeLocal > spendingLocal * 1.1f)
                approvalDelta += 0.5f;
            else if (incomeLocal < spendingLocal)
                approvalDelta -= 1.5f;

            if (country.stability > 70f) approvalDelta += 0.5f;
            else if (country.stability < 40f) approvalDelta -= 2.0f;

            if (country.warTurnsActive > 6) approvalDelta -= 1.0f;
            if (country.warExhaustion > 60f) approvalDelta -= 2.0f;

            if (country.corruptionIndex > 50f) approvalDelta -= 1.0f;

            if (country.foodSecurity < 30f) approvalDelta -= 3.0f;

            country.publicApproval = Mathf.Clamp(country.publicApproval + approvalDelta, 0f, 100f);
        }
    }
}
