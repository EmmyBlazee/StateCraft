using StateCraft.Data;
using StateCraft.Interfaces;
using UnityEngine;

namespace StateCraft.Systems
{
    public class SecuritySystem : ISecuritySystem
    {
        private ITurnSystem _turnSystem;

        public SecuritySystem(ITurnSystem turnSystem)
        {
            _turnSystem = turnSystem;
        }

        public void UpdateSecurity(CountryDataSO country)
        {
            if (country == null) return;
            
            // 1. Police presence 
            // Ideal ratio is roughly 1 officer per 400 citizens.
            float policeRatio = (country.nationalPoliceSize * 400f) / Mathf.Max(country.population, 1f);
            float policeScore = Mathf.Clamp(policeRatio * 100f, 0f, 100f);

            // 2. Military presence 
            float militaryScore = Mathf.Clamp(country.militaryStrength, 0f, 100f);

            // 3. Intelligence Coverage
            float intelScore = Mathf.Clamp(country.intelligenceLevel, 0f, 100f);
            
            // 4. Instability Penalty
            float instabilityPenalty = Mathf.Max(0f, 100f - country.stability);

            // 5. Insurgency Load (Average across all regions)
            float insurgencyLoad = 0f;
            if (country.regions != null && country.regions.Count > 0)
            {
                foreach (var reg in country.regions)
                {
                    insurgencyLoad += reg.insurgencyLevel;
                }
                insurgencyLoad /= country.regions.Count;
            }

            // The Formula
            float rawSecurity = (policeScore * 0.3f) 
                              + (militaryScore * 0.4f) 
                              + (intelScore * 0.2f)
                              - (country.corruptionIndex * 0.3f)
                              - (instabilityPenalty * 0.2f)
                              - (insurgencyLoad * 0.3f);

            // Give a baseline of 35% security before penalties drag it down
            float newSecurity = Mathf.Clamp(rawSecurity + 35f, 0f, 100f);
            
            country.security = newSecurity;
        }

        public void DeployPolice(CountryDataSO country, RegionDataSO region)
        {
            // Stub implementation
        }

        public void DeployMilitary(CountryDataSO country, RegionDataSO region)
        {
            // Stub implementation
        }

        public void DeployRDF(CountryDataSO country, RegionDataSO region)
        {
            // Stub implementation
        }
        
        public void RecalculateSecurity(CountryDataSO country)
        {
            UpdateSecurity(country);
        }
    }
}
