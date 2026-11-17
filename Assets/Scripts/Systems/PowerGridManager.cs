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
            
            float ratio = capacity / Mathf.Max(1f, demand);
            country.electricityLevel = Mathf.Clamp(ratio * 100f * country.gridTransmissionEfficiency, 0f, 100f);

            float margin = capacity - demand;
            float blackoutRisk = 0f;
            if (margin < 0) 
            {
                blackoutRisk = Mathf.Abs(margin) / demand * 100f;
                country.gdpGrowthRate -= 0.005f * (Mathf.Clamp(blackoutRisk, 0f, 100f) / 100f);
            }
            country.currentBlackoutRisk = Mathf.Clamp(blackoutRisk, 0f, 100f);

            if (Random.Range(0f, 100f) < country.currentBlackoutRisk)
            {
                TriggerBlackout(country);
            }
        }
        
        public const float GasPlantCapacity = 500f;
        public const float HydroPlantCapacity = 1200f;
        public const float NuclearPlantCapacity = 3000f;
        public const float SolarPlantCapacity = 200f;
        public const float WindPlantCapacity = 150f;

        public float CalculateCapacity(CountryDataSO country)
        {
            float total = 0;
            total += country.powerGasPlants * GasPlantCapacity;
            total += country.powerHydroPlants * HydroPlantCapacity;
            total += country.powerNuclearPlants * NuclearPlantCapacity;
            total += country.powerSolarPlants * SolarPlantCapacity;
            total += country.powerWindPlants * WindPlantCapacity;
            return total;
        }

        public float CalculateDemand(CountryDataSO country)
        {
            float civilianDemand = (country.population / 1000000f) * 50f;
            
            float industrialDemand = (country.indAgriRice + country.indAgriCassava + country.indAgriCocoa + 
                                      country.indMiningLocal + country.indMiningOil + 
                                      country.indTechIT + country.indTechFintech) * 150f;
            
            float militaryDemand = 500f;
            
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
