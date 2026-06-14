using UnityEngine;
using StateCraft.Data;
using System.Collections.Generic;

namespace StateCraft.Map
{
    public class MapIconManager : MonoBehaviour
    {
        public static MapIconManager Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject insurgencyIconPrefab;
        public GameObject economicBoomIconPrefab;

        private Dictionary<CountryDataSO, List<GameObject>> activeIcons = new Dictionary<CountryDataSO, List<GameObject>>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void UpdateIconsForCountry(CountryDataSO country, Vector3 position)
        {
            // Clear old icons
            if (activeIcons.ContainsKey(country))
            {
                foreach (var icon in activeIcons[country]) Destroy(icon);
                activeIcons[country].Clear();
            }
            else
            {
                activeIcons[country] = new List<GameObject>();
            }

            // Spawn insurgency icon if high
            if (country.stability < 40f && insurgencyIconPrefab != null)
            {
                GameObject icon = Instantiate(insurgencyIconPrefab, position + Vector3.up * 0.5f, Quaternion.identity, transform);
                activeIcons[country].Add(icon);
            }
        }
    }
}
