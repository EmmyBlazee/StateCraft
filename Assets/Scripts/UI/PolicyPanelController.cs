using UnityEngine;
using UnityEngine.UI;
using StateCraft.Map;
using StateCraft.Core;
using StateCraft.Data;

namespace StateCraft.UI
{
    public class PolicyPanelController : MonoBehaviour
    {
        [Header("Buttons")]
        public Button deployPoliceBtn;
        public Button deployMilitaryBtn;
        public Button deployRDFBtn;
        public Button investIntelBtn;
        public Button fundEconomyBtn;

        private void Start()
        {
            deployPoliceBtn.onClick.AddListener(() => ExecuteSecurityAction("Police"));
            deployMilitaryBtn.onClick.AddListener(() => ExecuteSecurityAction("Military"));
            deployRDFBtn.onClick.AddListener(() => ExecuteSecurityAction("RDF"));
            
            if (investIntelBtn != null)
                investIntelBtn.onClick.AddListener(() => ExecuteEconomicAction("Intelligence"));
            if (fundEconomyBtn != null)
                fundEconomyBtn.onClick.AddListener(() => ExecuteEconomicAction("Economy"));
        }

        private void ExecuteSecurityAction(string type)
        {
            CountryDataSO selected = MapManager.Instance.currentSelectedCountry;
            if (selected == null) return;

            if (selected.regions.Count > 0)
            {
                var region = selected.regions[0];
                switch (type)
                {
                    case "Police":
                        GameManager.Instance.SecuritySystem.DeployPolice(selected, region);
                        break;
                    case "Military":
                        GameManager.Instance.SecuritySystem.DeployMilitary(selected, region);
                        break;
                    case "RDF":
                        GameManager.Instance.SecuritySystem.DeployRDF(selected, region);
                        break;
                }
                
}
            else
            {
                Debug.LogWarning($"{selected.countryName} has no regions defined!");
            }
        }

        private void ExecuteEconomicAction(string type)
        {
            CountryDataSO selected = MapManager.Instance.currentSelectedCountry;
            if (selected == null) return;

            float cost = 50f;
            if (GameManager.Instance.playerBudget < cost)
            {
return;
            }

            GameManager.Instance.playerBudget -= cost;

            if (type == "Intelligence")
            {
                selected.intelligenceLevel += 25f;
                selected.intelligenceLevel = Mathf.Clamp(selected.intelligenceLevel, 0, 100);
}
            else if (type == "Economy")
            {
                selected.gdp += 10f;
                selected.stability += 5f;
                selected.stability = Mathf.Clamp(selected.stability, 0, 100);
}
        }
    }
}
