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
            
            float policeRatio = (country.nationalPoliceSize * 400f) / Mathf.Max(country.population, 1f);
            float policeScore = Mathf.Clamp(policeRatio * 100f, 0f, 100f);

            float militaryScore = Mathf.Clamp(country.militaryStrength, 0f, 100f);

            float intelScore = Mathf.Clamp(country.intelligenceLevel, 0f, 100f);
            
            float instabilityPenalty = Mathf.Max(0f, 100f - country.stability);

            float insurgencyLoad = 0f;
            if (country.regions != null && country.regions.Count > 0)
            {
                foreach (var reg in country.regions)
                {
                    insurgencyLoad += reg.insurgencyLevel;
                }
                insurgencyLoad /= country.regions.Count;
            }

            float rawSecurity = (policeScore * 0.3f) 
                              + (militaryScore * 0.4f) 
                              + (intelScore * 0.2f)
                              - (country.corruptionIndex * 0.3f)
                              - (instabilityPenalty * 0.2f)
                              - (insurgencyLoad * 0.3f);

            float newSecurity = Mathf.Clamp(rawSecurity + 35f, 0f, 100f);
            
            country.security = newSecurity;
        }

        public void DeployPolice(CountryDataSO country, RegionDataSO region)
        {
        }

        public void DeployMilitary(CountryDataSO country, RegionDataSO region)
        {
        }

        public void DeployRDF(CountryDataSO country, RegionDataSO region)
        {
        }
        
        public void RecalculateSecurity(CountryDataSO country)
        {
            UpdateSecurity(country);
        }
    }
}
