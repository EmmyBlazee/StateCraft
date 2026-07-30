using UnityEngine;

namespace StateCraft.Data
{
    /// <summary>
    /// ScriptableObject placed in Assets/Resources/ so all CountryDataSO assets
    /// can be loaded at runtime in standalone builds (where AssetDatabase is unavailable).
    /// Auto-populated by SplashSceneBuilder in-editor.
    /// </summary>
    [CreateAssetMenu(fileName = "CountryRegistry", menuName = "StateCraft/Country Registry")]
    public class CountryRegistry : ScriptableObject
    {
        [Tooltip("All available countries. Populated automatically by SplashSceneBuilder.")]
        public CountryDataSO[] countries;

        /// <summary>
        /// Finds a country by exact name match (case-sensitive).
        /// </summary>
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
