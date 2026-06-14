using UnityEngine;
using StateCraft.Data;

namespace StateCraft.Systems
{
    public class PowerGridManager : MonoBehaviour
    {
        public static PowerGridManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ProcessGridTick(CountryDataSO country)
        {
            float capacity = CalculateCapacity(country);
            float demand = CalculateDemand(country);
            
            // Grid Coverage %
            float ratio = capacity / Mathf.Max(1f, demand);
            country.electricityLevel = Mathf.Clamp(ratio * 100f * country.gridTransmissionEfficiency, 0f, 100f);

            // Blackout Risk and Deficit Penalty
            float margin = capacity - demand;
            float blackoutRisk = 0f;
            if (margin < 0) 
            {
                blackoutRisk = Mathf.Abs(margin) / demand * 100f;
                // Continuous GDP penalty for running an energy deficit (up to -0.5% growth rate drag per tick)
                country.gdpGrowthRate -= 0.005f * (Mathf.Clamp(blackoutRisk, 0f, 100f) / 100f);
            }
            country.currentBlackoutRisk = Mathf.Clamp(blackoutRisk, 0f, 100f);

            // Trigger potential blackouts
            if (Random.Range(0f, 100f) < country.currentBlackoutRisk)
            {
                TriggerBlackout(country);
            }
        }
        
        public float CalculateCapacity(CountryDataSO country)
        {
            float total = 0;
            total += country.powerGasPlants * 500f; // 500 MW
            total += country.powerHydroPlants * 1200f; // 1200 MW
            total += country.powerNuclearPlants * 3000f; // 3000 MW
            total += country.powerSolarPlants * 200f; // 200 MW
            total += country.powerWindPlants * 150f; // 150 MW
            return total;
        }

        public float CalculateDemand(CountryDataSO country)
        {
            float civilianDemand = (country.population / 1000000f) * 50f; // 50 MW per 1M people
            
            float industrialDemand = (country.indAgriRice + country.indAgriCassava + country.indAgriCocoa + 
                                      country.indMiningLocal + country.indMiningOil + 
                                      country.indTechIT + country.indTechFintech) * 150f; // 150 MW per level
            
            float militaryDemand = 500f; // Base military
            
            return civilianDemand + industrialDemand + militaryDemand;
        }

        public void TriggerBlackout(CountryDataSO country)
        {
            country.stability -= 5f;
            country.gdpGrowthRate -= 0.01f;
            NewsSystem.Instance?.PublishNews($"CRISIS: MASSIVE GRID FAILURE IN {country.countryName.ToUpper()}!");
        }
    }
}
