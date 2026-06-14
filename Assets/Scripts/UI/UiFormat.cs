using System;
using UnityEngine;

namespace StateCraft.UI
{
    /// <summary>
    /// Consistent money and percent formatting for UI Toolkit dashboards.
    /// Values are expected in billions unless noted.
    /// </summary>
    public static class UiFormat
    {
        public static string Money(float billions, string symbol = "$")
        {
            if (billions == 0f) return $"{symbol}0";
            return FormatNumberRaw(billions * 1e9, symbol);
        }

        public static string MoneyRaw(float raw, string symbol = "$")
        {
            return FormatNumberRaw(raw, symbol);
        }

        private static string FormatNumberRaw(double raw, string symbol)
        {
            if (raw == 0.0) return $"{symbol}0";

            double absRaw = Math.Abs(raw);
            string sign = raw < 0.0 ? "-" : "";

            if (absRaw >= 999_500_000_000_000_000_000_000_000_000_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000_000_000_000_000_000_000_000_000.0, "Dc", sign, symbol);
            }
            if (absRaw >= 999_500_000_000_000_000_000_000_000_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000_000_000_000_000_000_000_000.0, "No", sign, symbol);
            }
            if (absRaw >= 999_500_000_000_000_000_000_000_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000_000_000_000_000_000_000.0, "Oc", sign, symbol);
            }
            if (absRaw >= 999_500_000_000_000_000_000_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000_000_000_000_000_000.0, "Sp", sign, symbol);
            }
            if (absRaw >= 999_500_000_000_000_000_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000_000_000_000_000.0, "Sx", sign, symbol);
            }
            if (absRaw >= 999_500_000_000_000_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000_000_000_000.0, "Qi", sign, symbol);
            }
            if (absRaw >= 999_500_000_000_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000_000_000.0, "Qa", sign, symbol);
            }
            if (absRaw >= 999_500_000_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000_000.0, "T", sign, symbol);
            }
            if (absRaw >= 999_500_000.0)
            {
                return FormatSuffix(absRaw / 1_000_000_000.0, "B", sign, symbol);
            }
            if (absRaw >= 999_500.0)
            {
                return FormatSuffix(absRaw / 1_000_000.0, "M", sign, symbol);
            }
            if (absRaw >= 999.5)
            {
                return FormatSuffix(absRaw / 1_000.0, "K", sign, symbol);
            }

            // Sub-1000 (less than 4 digits before decimal)
            return $"{sign}{symbol}{Math.Round(absRaw).ToString("0")}";
        }

        private static string FormatSuffix(double display, string suffix, string sign, string symbol)
        {
            string fmt;
            if (display >= 99.95)      fmt = "0";
            else if (display >= 9.995) fmt = "0.#";
            else                       fmt = "0.##";

            return $"{sign}{symbol}{display.ToString(fmt)}{suffix}";
        }

        public static string MoneySigned(float billions, string symbol = "$")
        {
            if (billions >= 0f) return "+" + Money(billions, symbol);
            return Money(billions, symbol);
        }

        public static string Percent(float ratio) => $"{ratio * 100f:0.#}%";
    }
}
