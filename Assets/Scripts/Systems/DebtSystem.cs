using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using UnityEngine;

namespace StateCraft.Systems
{
    public class DebtSystem : IDebtSystem
    {
        public void UpdateDebt(CountryDataSO country)
        {
            // Calculate interest for both pools
            float domesticInterest = CalculateDomesticInterest(country);
            float foreignInterest = CalculateForeignInterest(country);
            
            // Interest is added to the principal each month (compounding)
            country.domesticDebt += domesticInterest;
            country.foreignDebt += foreignInterest;
            
            // Keep total debt in sync
            if (float.IsNaN(country.domesticDebt) || float.IsInfinity(country.domesticDebt)) country.domesticDebt = 0;
            if (float.IsNaN(country.foreignDebt) || float.IsInfinity(country.foreignDebt)) country.foreignDebt = 0;
            
            country.debt = country.domesticDebt + country.foreignDebt;
            
            // Check for high-debt stability penalties
            float totalDebt = country.debt;
            if (country.gdp > 0 && !float.IsNaN(country.gdp) && totalDebt > country.gdp * 0.8f)
            {
                // Debt-to-GDP ratio > 80% starts hurting stability
                float penalty = (totalDebt / country.gdp) * 1.5f;
                country.stability -= penalty;
                
                if (Random.value > 0.95f)
                {
                    bool globalOnly = country != GameManager.Instance?.playerCountry;
                    NewsSystem.Instance?.PublishNews($"MARKET WATCH: Credit rating agencies downgrade {country.countryName} outlook due to mounting debt.", globalOnly);
                }
            }

            country.stability = Mathf.Clamp(country.stability, 0f, 100f);
        }

        // Combined interest for the Treasury/Budget calculation
        public float CalculateInterest(CountryDataSO country)
        {
            return CalculateDomesticInterest(country) + CalculateForeignInterest(country);
        }

        public float CalculateDomesticInterest(CountryDataSO country)
        {
            // Domestic rates are tied to inflation to protect local bond holders
            float baseRate = 0.07f; // 7% base
            float inflationPremium = country.inflationRate * 1.5f;
            
            float annualRate = baseRate + inflationPremium;
            return (country.domesticDebt * annualRate) / 12f; // Monthly
        }

        public float CalculateForeignInterest(CountryDataSO country)
        {
            // Foreign rates are tied to geopolitical stability (Risk Premium)
            float baseRate = 0.04f; // 4% base (e.g. IMF/World Bank)
            
            // As stability drops, foreign lenders demand higher returns
            float riskPremium = (1f - (country.stability / 100f)) * 0.18f;
            
            float annualRate = baseRate + riskPremium;
            return (country.foreignDebt * annualRate) / 12f; // Monthly
        }
    }
}
