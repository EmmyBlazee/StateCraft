using StateCraft.Data;
using UnityEngine;

namespace StateCraft.Systems
{
    public static class CorruptionSystem
    {
        public static float GetLeakageAmount(CountryDataSO country)
        {
            if (country == null) return 0f;
            
            float leakPercentage = (country.corruptionIndex / 100f) * 0.15f;
            float leakAmountLocal = country.income * country.exchangeRate * leakPercentage;
            
            return leakAmountLocal;
        }

        public static void ProcessCorruption(CountryDataSO country)
        {
            if (country == null) return;

            if (country.corruptionIndex < 25f)
            {
                country.publicApproval = Mathf.Max(0f, country.publicApproval - 0.5f);

                if (country == Core.GameManager.Instance?.playerCountry && UnityEngine.Random.value > 0.7f)
                {
                    UI.NotificationManager.Instance?.AddNotification(
                        $"ELITE BACKLASH: Anti-corruption crackdown draws resistance from entrenched interests. Public approval −0.5.",
                        UnityEngine.Color.yellow
                    );
                }
            }
        }
    }
}
