using StateCraft.Data;
using UnityEngine;

namespace StateCraft.Systems
{
    /// <summary>
    /// Phase 2: Corruption system. Siphons treasury funds based on corruption index,
    /// and causes elite backlash if anti-corruption gets too strong.
    /// </summary>
    public static class CorruptionSystem
    {
        public static float GetLeakageAmount(CountryDataSO country)
        {
            if (country == null) return 0f;
            
            // Calculate money lost directly to corruption.
            // Say up to 15% of national income is siphoned off at 100% corruption.
            float leakPercentage = (country.corruptionIndex / 100f) * 0.15f;
            float leakAmountLocal = country.income * country.exchangeRate * leakPercentage;
            
            return leakAmountLocal;
        }

        public static void ProcessCorruption(CountryDataSO country)
        {
            if (country == null) return;

            // Elite Backlash: Entrenched elites resist anti-corruption efforts.
            // If corruption is below 25%, player suffers passive approval drops.
            // FIX: Surface this to the player via notification so the penalty isn't invisible.
            if (country.corruptionIndex < 25f)
            {
                country.publicApproval = Mathf.Max(0f, country.publicApproval - 0.5f);

                // Notify the player periodically (random so it doesn't fire every month)
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
