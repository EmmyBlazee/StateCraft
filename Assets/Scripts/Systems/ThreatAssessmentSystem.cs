using System.Collections.Generic;
using UnityEngine;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.Interfaces;

namespace StateCraft.Systems
{
    public class ThreatAssessmentSystem : IThreatAssessmentSystem
    {
        public List<ThreatAlert> GenerateThreats(CountryDataSO country, GameManager gm)
        {
            List<ThreatAlert> alerts = new List<ThreatAlert>();

            if (gm.allCountries != null)
            {
                foreach (var other in gm.allCountries)
                {
                    if (other == country) continue;

                    var warStage = gm.WarSystem?.GetWarStage(other) ?? WarStage.Peace;
                    if (warStage >= WarStage.Tension)
                    {
                        float militaryRatio = other.militaryStrength / Mathf.Max(1f, country.militaryStrength);
                        
                        string impactStr = militaryRatio > 1.5f ? "Critical" : militaryRatio > 0.8f ? "High" : "Medium";
                        Color color = warStage == WarStage.Conflict ? new Color(1f, 0.32f, 0.32f) : new Color(0.95f, 0.77f, 0.06f);
                        string icon = warStage == WarStage.Conflict ? "\uf071" : "\uf05a";
                        
                        string title = warStage == WarStage.Conflict ? "Active Conflict" : "Military Build-up";
                        if (warStage == WarStage.Crisis) title = "Border Clashes";

                        alerts.Add(new ThreatAlert
                        {
                            title = $"{title} ({other.countryName})",
                            region = "Foreign Border",
                            probability = (int)warStage * 25f + Random.Range(5f, 15f),
                            impact = impactStr,
                            icon = icon,
                            color = color
                        });
                    }
                }
            }

            if (country.security < 40f)
            {
                alerts.Add(new ThreatAlert
                {
                    title = "Organized Crime Syndicate",
                    region = "Domestic Urban Centers",
                    probability = 100f - country.security,
                    impact = "Medium",
                    icon = "\uf21b",
                    color = new Color(0.95f, 0.77f, 0.06f)
                });
            }

            if (country.security < 20f || country.stability < 30f)
            {
                alerts.Add(new ThreatAlert
                {
                    title = "Insurgency Risk",
                    region = "Rural Provinces",
                    probability = Mathf.Clamp(100f - (country.security + country.stability) / 2f + Random.Range(0, 10), 0, 100),
                    impact = "High",
                    icon = "\uf06d",
                    color = new Color(1f, 0.32f, 0.32f)
                });
            }

            if (country.exposureRisk > 60f)
            {
                alerts.Add(new ThreatAlert
                {
                    title = "Covert Exposure Risk",
                    region = "Global",
                    probability = country.exposureRisk,
                    impact = "High",
                    icon = "\uf06e",
                    color = new Color(0.73f, 0.41f, 0.78f)
                });
            }

            alerts.Sort((a, b) => b.probability.CompareTo(a.probability));

            if (alerts.Count > 5)
            {
                return alerts.GetRange(0, 5);
            }
            return alerts;
        }
    }
}
