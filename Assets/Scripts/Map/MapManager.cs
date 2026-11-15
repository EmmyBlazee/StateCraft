using UnityEngine;
using StateCraft.Data;
using System;

namespace StateCraft.Map
{
    public class MapManager : MonoBehaviour
    {
        private static MapManager _instance;
        public static MapManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType<MapManager>();
                return _instance;
            }
        }

        public event Action<CountryDataSO> OnCountrySelected;

        public CountryDataSO currentSelectedCountry;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void SelectCountry(CountryDataSO country)
        {
            currentSelectedCountry = country;
            OnCountrySelected?.Invoke(country);
}
    }
}
