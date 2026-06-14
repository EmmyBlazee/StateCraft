using UnityEngine;
using StateCraft.Map;
using System.Collections.Generic;

namespace StateCraft.UI
{
    public class WorldSpaceUIManager : MonoBehaviour
    {
        public GameObject labelPrefab;
        public float hoverHeight = 2.0f;

        private void Start()
        {
            SpawnLabels();
        }

        public void SpawnLabels()
        {
            // Find all countries in the scene
            CountryController[] countries = FindObjectsOfType<CountryController>();

            foreach (var country in countries)
            {
                if (country.countryData == null) continue;

                // Create a label at the center of the country object
                GameObject label = Instantiate(labelPrefab, country.transform.position + Vector3.up * hoverHeight, Quaternion.identity);
                label.transform.SetParent(country.transform);
                label.name = $"{country.countryData.countryName}_Label";

                var labelScript = label.GetComponent<CountryLabel3D>();
                if (labelScript != null)
                {
                    labelScript.Setup(country.countryData);
                }
            }
        }
    }
}
