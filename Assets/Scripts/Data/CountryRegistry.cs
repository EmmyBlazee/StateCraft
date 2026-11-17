using UnityEngine;

namespace StateCraft.Data
{
    [CreateAssetMenu(fileName = "CountryRegistry", menuName = "StateCraft/Country Registry")]
    public class CountryRegistry : ScriptableObject
    {
        [Tooltip("All available countries. Populated automatically by SplashSceneBuilder.")]
        public CountryDataSO[] countries;

        public CountryDataSO FindByName(string countryName)
        {
            if (countries == null) return null;
            foreach (var c in countries)
                if (c != null && c.countryName == countryName)
                    return c;
            return null;
        }
    }
}
