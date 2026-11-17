using UnityEngine;
using StateCraft.Data;
using System.Collections.Generic;

namespace StateCraft.Map
{
    public class MapVisualsManager : MonoBehaviour
    {
        public static MapVisualsManager Instance { get; private set; }

        private List<CountryController> countryControllers = new List<CountryController>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void RegisterCountry(CountryController controller)
        {
            if (!countryControllers.Contains(controller))
                countryControllers.Add(controller);
        }

        private void Update()
        {
            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;

            foreach (var country in countryControllers)
            {
                if (country.countryData == null) continue;

                if (country.countryData.stability < 30f)
                {
                    var renderer = country.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        Color targetColor = Color.Lerp(country.countryData.mapColor, Color.red, pulse);
                        renderer.color = targetColor;
                    }
                }
            }
        }

        public void RefreshAllVisuals()
        {
            foreach (var country in countryControllers)
            {
                country.UpdateVisuals();
            }
        }
    }
}
