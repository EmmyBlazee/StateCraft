using UnityEngine;
using StateCraft.Data;
using StateCraft.Data.Economy;

namespace StateCraft.Systems
{
    /// <summary>
    /// Bridges EconomyDatabaseSO (pure data) with CountryDataSO (live state).
    /// All gameplay logic that previously lived as inline delegates inside
    /// EconomyTabContent is now centralised here, keyed by the SO's string Id.
    /// </summary>
    public static class EconomyLogicEvaluator
    {
        // ── INFRASTRUCTURE ───────────────────────────────────────────────────

        public static float GetInfraRating(CountryDataSO c, string id) => id switch
        {
            "Roads"     => c.roadLevel      * 10f,
            "Rail"      => c.railLevel      * 10f,
            "Ports"     => c.portLevel      * 10f,
            "Airports"  => c.airportLevel   * 10f,
            "Power"     => c.electricityLevel,
            "Health"    => (c.healthcareLevel + c.hospitalLevel) * 5f,
            "Education" => c.educationLevel * 10f,
            "Telecom"   => c.telecomLevel   * 10f,
            _           => 0f
        };

        public static string GetInfraStatus(CountryDataSO c, string id) => id switch
        {
            "Roads"     => $"Level {c.roadLevel}/10 · Tax reach +{c.roadLevel * 2}%",
            "Rail"      => $"Level {c.railLevel}/10 · Resource haul +{c.railLevel * 5}%",
            "Ports"     => $"Level {c.portLevel}/10 · Export capacity ×{1f + c.portLevel * 0.03f:F2}",
            "Airports"  => $"Level {c.airportLevel}/10 · FDI appeal +{c.airportLevel * 3}%",
            "Power"     => BuildPowerStatus(c),
            "Health"    => $"Care Lv.{c.healthcareLevel} · Hospitals Lv.{c.hospitalLevel}",
            "Education" => $"Level {c.educationLevel}/10 · Tech output ×{c.techIndustryOutput / 100f:F2}",
            "Telecom"   => $"Level {c.telecomLevel}/10 · Fintech readiness",
            _           => "—"
        };

        public static string GetInfraEffect(CountryDataSO c, string id) => id switch
        {
            "Roads"     => "Links farms to cities; improves tax collection & army deployment speed.",
            "Rail"      => "Bulk minerals & grain move inland; lowers export delays.",
            "Ports"     => "Drives resource dividends and trade deal value.",
            "Airports"  => "Business travel & air cargo; boosts GDP growth modifier.",
            "Power"     => c.electricityLevel < 40f ? "Blackouts throttle factories (−2% growth)."
                           : c.electricityLevel > 80f ? "Industrial surge (+1% growth)."
                           : "Stable supply for manufacturing.",
            "Health"    => "Raises stability & labour productivity.",
            "Education" => "Feeds IT/fintech sectors and long-run growth.",
            "Telecom"   => "Enables tech industry & intelligence coverage.",
            _           => "—"
        };

        public static string GetInfraBottleneck(CountryDataSO c, string id) => id switch
        {
            "Roads"     => c.roadLevel < 3 ? "Rural districts disconnected from markets." : "—",
            "Rail"      => c.railLevel < c.portLevel - 2 ? "Ports congested — no rail offload." : "—",
            "Ports"     => c.portLevel < 2 ? "Export backlog costing $50M+ monthly." : "—",
            "Airports"  => c.airportLevel < 2 ? "No direct long-haul routes to capital markets." : "—",
            "Power"     => c.currentBlackoutRisk > 0.15f ? $"Blackout risk {c.currentBlackoutRisk:P0}"
                           : c.gridTransmissionEfficiency < 0.8f ? "Grid losses high" : "—",
            "Health"    => c.hospitalLevel < c.healthcareLevel ? "Clinics lack bed capacity." : "—",
            "Education" => c.educationLevel < 3 ? "Skills gap limits industrial upgrade." : "—",
            "Telecom"   => c.telecomLevel < 4 ? "Rural areas off-grid for mobile banking." : "—",
            _           => "—"
        };

        public static float GetInfraInvestCost(CountryDataSO c, InfraDataSO def) => def.id switch
        {
            "Roads"     => def.baseCost + c.roadLevel      * def.costPerLevel,
            "Rail"      => def.baseCost + c.railLevel      * def.costPerLevel,
            "Ports"     => def.baseCost + c.portLevel      * def.costPerLevel,
            "Airports"  => def.baseCost + c.airportLevel   * def.costPerLevel,
            "Power"     => def.baseCost + (100f - c.electricityLevel) * def.costPerLevel,
            "Health"    => def.baseCost + c.healthcareLevel * def.costPerLevel,
            "Education" => def.baseCost + c.educationLevel  * def.costPerLevel,
            "Telecom"   => def.baseCost + c.telecomLevel    * def.costPerLevel,
            _           => def.baseCost
        };

        public static bool CanInvest(CountryDataSO c, string id) => id switch
        {
            "Roads"     => c.roadLevel     < 10,
            "Rail"      => c.railLevel     < 10,
            "Ports"     => c.portLevel     < 10,
            "Airports"  => c.airportLevel  < 10,
            "Power"     => c.electricityLevel < 100f,
            "Health"    => c.healthcareLevel  < 10,
            "Education" => c.educationLevel   < 10,
            "Telecom"   => c.telecomLevel     < 10,
            _           => false
        };

        public static void ApplyInfraInvest(CountryDataSO c, string id)
        {
            switch (id)
            {
                case "Roads":
                    c.roadLevel++;
                    c.publicApproval = Mathf.Min(100f, c.publicApproval + 0.5f);
                    break;
                case "Rail":
                    c.railLevel++;
                    break;
                case "Ports":
                    c.portLevel++;
                    c.tradeVolumeUSD += 5e8f;
                    break;
                case "Airports":
                    c.airportLevel++;
                    c.foreignInvestment += 0.5f;
                    break;
                case "Power":
                    c.electricityLevel          = Mathf.Min(100f, c.electricityLevel + 6f);
                    c.gridTransmissionEfficiency = Mathf.Min(1f, c.gridTransmissionEfficiency + 0.03f);
                    c.currentBlackoutRisk        = Mathf.Max(0f, c.currentBlackoutRisk - 0.05f);
                    c.powerGasPlants++;
                    break;
                case "Health":
                    c.healthcareLevel++;
                    c.stability = Mathf.Min(100f, c.stability + 1f);
                    break;
                case "Education":
                    c.educationLevel++;
                    c.techIndustryOutput = Mathf.Min(150f, c.techIndustryOutput + 3f);
                    break;
                case "Telecom":
                    c.telecomLevel++;
                    c.indTechFintech = Mathf.Min(10, c.indTechFintech + (c.telecomLevel % 3 == 0 ? 1 : 0));
                    break;
            }
        }

        // ── INDUSTRY ─────────────────────────────────────────────────────────

        public static int GetIndustryLevel(CountryDataSO c, string id) => id switch
        {
            "Oil"    => c.indMiningOil,
            "Mining" => c.indMiningLocal,
            "Agri"   => c.indAgriRice + c.indAgriCocoa + c.indAgriCassava,
            "Mfg"    => c.indManufacturing,
            "Tech"   => c.indTechIT + c.indTechFintech,
            _        => 0
        };

        public static float GetIndustryWorkforce(CountryDataSO c, string id) => id switch
        {
            "Oil"    => c.workforceOil,
            "Mining" => c.workforceMining,
            "Agri"   => c.workforceAgri,
            "Mfg"    => c.workforceManufacturing,
            "Tech"   => (c.indTechIT + c.indTechFintech) * 800f,
            _        => 0f
        };

        public static float GetIndustryRevenue(CountryDataSO c, IndustryDataSO def) => def.id switch
        {
            "Oil"    => c.gdp * def.gdpShareAtMax * (c.indMiningOil  / 10f) * c.exchangeRate,
            "Mining" => c.gdp * def.gdpShareAtMax * (c.indMiningLocal / 10f) * c.exchangeRate,
            "Agri"   => c.gdp * def.gdpShareAtMax * ((c.indAgriRice + c.indAgriCocoa + c.indAgriCassava) / 30f) * c.exchangeRate,
            "Mfg"    => c.gdp * def.gdpShareAtMax * (c.indManufacturing / 10f) * c.exchangeRate,
            "Tech"   => c.gdp * def.gdpShareAtMax * ((c.indTechIT + c.indTechFintech) / 20f) * c.exchangeRate,
            _        => 0f
        };

        public static string GetIndustrySynergy(CountryDataSO c, string id) => id switch
        {
            "Oil"    => $"Ports Lv.{c.portLevel} · Power {c.electricityLevel:F0}%",
            "Mining" => $"Rail Lv.{c.railLevel} · Sovereignty {c.resourceSovereignty:F0}%",
            "Agri"   => $"Food security {c.foodSecurity:F0}% · Roads Lv.{c.roadLevel}",
            "Mfg"    => $"Power {c.electricityLevel:F0}% · Roads Lv.{c.roadLevel}",
            "Tech"   => $"Telecom Lv.{c.telecomLevel} · Education Lv.{c.educationLevel}",
            _        => "—"
        };

        public static bool IsIndustryBullish(CountryDataSO c, string id) => id switch
        {
            "Oil"    => c.electricityLevel > 50f && c.portLevel > 1,
            "Mining" => c.railLevel > 2 && c.resourceSovereignty > 60f,
            "Agri"   => c.foodSecurity > 55f && c.roadLevel > 2,
            "Mfg"    => c.electricityLevel > 55f && c.roadLevel > 3,
            "Tech"   => c.telecomLevel > 3 && c.educationLevel > 2,
            _        => false
        };

        public static float GetIndustryUpgradeCost(CountryDataSO c, IndustryDataSO def) => def.id switch
        {
            "Oil"    => (def.baseCost + c.indMiningOil  * def.costPerLevel) * c.exchangeRate,
            "Mining" => (def.baseCost + c.indMiningLocal * def.costPerLevel) * c.exchangeRate,
            "Agri"   => (def.baseCost + (c.indAgriRice + c.indAgriCocoa) * def.costPerLevel) * c.exchangeRate,
            "Mfg"    => (def.baseCost + c.indManufacturing * def.costPerLevel) * c.exchangeRate,
            "Tech"   => (def.baseCost + (c.indTechIT + c.indTechFintech) * def.costPerLevel) * c.exchangeRate,
            _        => def.baseCost
        };

        public static void ApplyIndustryUpgrade(CountryDataSO c, string id)
        {
            switch (id)
            {
                case "Oil":
                    c.indMiningOil++;
                    c.workforceOil += 2000f;
                    break;
                case "Mining":
                    c.indMiningLocal++;
                    c.workforceMining += 5000f;
                    break;
                case "Agri":
                    if (c.indAgriRice <= c.indAgriCocoa) c.indAgriRice++;
                    else c.indAgriCocoa++;
                    c.foodSecurity = Mathf.Min(100f, c.foodSecurity + 2f);
                    break;
                case "Mfg":
                    c.indManufacturing++;
                    c.workforceManufacturing += 1500f;
                    break;
                case "Tech":
                    if (c.indTechIT <= c.indTechFintech) c.indTechIT++;
                    else c.indTechFintech++;
                    c.techIndustryOutput = Mathf.Min(150f, c.techIndustryOutput + 5f);
                    break;
            }
        }

        // ── COMMODITIES ──────────────────────────────────────────────────────

        public static float GetCommodityStock(CountryDataSO c, string id) => id switch
        {
            "Electricity"       => PowerCapacityMw(c) - (c.indManufacturing * 10f + c.population * 0.0005f),
            "CrudeOil"          => c.invOilBarrels,
            "Rice"              => c.invRiceTonnes,
            "Cassava"           => c.invCassavaTonnes,
            "Cocoa"             => c.invCocoaTonnes,
            "ProcessedMinerals" => c.invMineralsTonnes,
            "TechGoods"         => c.invTechGoods,
            _                   => 0f
        };

        public static float GetCommodityProduction(CountryDataSO c, string id) => id switch
        {
            "Electricity"       => PowerCapacityMw(c),
            "CrudeOil"          => c.indMiningOil * 2_000_000f,
            "Rice"              => c.indAgriRice * 50_000f,
            "Cassava"           => c.indAgriCassava * 80_000f,
            "Cocoa"             => c.indAgriCocoa * 20_000f,
            "ProcessedMinerals" => c.indMiningLocal * 100_000f,
            "TechGoods"         => (c.indTechIT + c.indTechFintech) * 5000f * (c.techIndustryOutput / 100f),
            _                   => 0f
        };

        public static float GetCommodityConsumption(CountryDataSO c, string id) => id switch
        {
            "Electricity"       => c.indManufacturing * 10f + c.population * 0.0005f,
            "CrudeOil"          => c.indManufacturing * 50_000f,
            "Rice"              => c.population * 0.001f * 0.4f,
            "Cassava"           => c.population * 0.001f * 0.6f,
            "Cocoa"             => 0f,
            "ProcessedMinerals" => c.indManufacturing * 8_000f,
            "TechGoods"         => c.population * 0.00001f,
            _                   => 0f
        };

        // ── HELPERS & ACTIONS ────────────────────────────────────────────────

        private static float PowerCapacityMw(CountryDataSO c) =>
            c.powerGasPlants     * PowerGridManager.GasPlantCapacity +
            c.powerHydroPlants   * PowerGridManager.HydroPlantCapacity +
            c.powerNuclearPlants * PowerGridManager.NuclearPlantCapacity +
            c.powerSolarPlants   * PowerGridManager.SolarPlantCapacity +
            c.powerWindPlants    * PowerGridManager.WindPlantCapacity;

        private static string BuildPowerStatus(CountryDataSO c)
        {
            float mw = PowerCapacityMw(c);
            string nuke = c.hasNuclearPower ? " · Nuclear online"
                          : c.isImportingUranium ? " · Uranium imports" : "";
            return $"{c.electricityLevel:F0}% coverage · {mw:F0} MW{nuke}";
        }

        public static float NationalInfraScore(CountryDataSO c)
        {
            float transport = (c.roadLevel + c.railLevel + c.portLevel + c.airportLevel) / 40f * 100f;
            float energy = c.electricityLevel;
            float social = (c.healthcareLevel + c.educationLevel + c.hospitalLevel) / 30f * 100f;
            float digital = c.telecomLevel * 10f;
            return Mathf.Clamp((transport * 0.3f + energy * 0.35f + social * 0.2f + digital * 0.15f), 0f, 100f);
        }

        public static string FoodSecurityLabel(CountryDataSO c)
        {
            if (c.foodSecurity >= 75f) return "SECURE";
            if (c.foodSecurity >= 50f) return "ADEQUATE";
            if (c.foodSecurity >= 30f) return "STRESSED";
            return "CRITICAL";
        }

        public static Color FoodSecurityColor(CountryDataSO c)
        {
            if (c.foodSecurity >= 75f) return new Color(0f, 230f / 255f, 118f / 255f);
            if (c.foodSecurity >= 50f) return new Color(241f / 255f, 196f / 255f, 15f / 255f);
            return new Color(255f / 255f, 82f / 255f, 82f / 255f);
        }

        public static string StockpileStatus(float stock, float monthlyCons)
        {
            if (monthlyCons <= 0f) return stock > 0 ? "SURPLUS" : "EMPTY";
            float months = stock / monthlyCons;
            if (months >= 6f) return "STRATEGIC RESERVE";
            if (months >= 2f) return "HEALTHY";
            if (months >= 0.5f) return "LOW";
            return "CRITICAL";
        }

        private static float TreasuryB => Core.GameManager.Instance != null ? Core.GameManager.Instance.playerBudget : 0f;

        private static void SpendTreasury(float billions, CountryDataSO c)
        {
            if (Core.GameManager.Instance != null) Core.GameManager.Instance.playerBudget -= billions;
            if (c != null) c.treasury = Core.GameManager.Instance?.playerBudget ?? c.treasury - billions;
        }

        public static bool TryStartProject(CountryDataSO c, BlueprintDataSO bp, out string message)
        {
            int inPipeline = 0;
            foreach (var p in c.activeProjects)
            {
                if (p.stage == ProjectStage.Planned || p.stage == ProjectStage.Funded || p.stage == ProjectStage.UnderConstruction)
                    inPipeline++;
            }
            if (inPipeline >= 5)
            {
                message = "Construction queue full (max 5 active sites).";
                return false;
            }
            float deposit = bp.totalCostB * 0.1f;
            if (TreasuryB < deposit)
            {
                message = "Treasury cannot cover mobilization deposit.";
                return false;
            }

            SpendTreasury(deposit, c);
            c.activeProjects.Add(new ActiveProject
            {
                projectName = bp.blueprintName,
                projectType = bp.type,
                regionName = string.IsNullOrEmpty(c.capitalCity) ? c.countryName : c.capitalCity,
                targetID = bp.targetId,
                totalCost = bp.totalCostB,
                totalMonths = bp.months,
                monthsRemaining = bp.months,
                stage = ProjectStage.UnderConstruction
            });
            message = $"{bp.blueprintName} added to pipeline.";
            NewsSystem.Instance?.PublishNews($"PROCUREMENT: {c.countryName} breaks ground on {bp.blueprintName}.");
            return true;
        }

        public static bool TryInvestInfra(CountryDataSO c, InfraDataSO def, out string message)
        {
            if (!CanInvest(c, def.id))
            {
                message = $"{def.title} is at maximum capacity.";
                return false;
            }
            float cost = GetInfraInvestCost(c, def);
            if (TreasuryB < cost)
            {
                string sym = string.IsNullOrEmpty(c.currencySymbol) ? "$" : c.currencySymbol;
                message = $"Need {UI.UiFormat.Money(cost, sym)} in treasury.";
                return false;
            }
            SpendTreasury(cost, c);
            ApplyInfraInvest(c, def.id);
            message = $"{def.title} upgraded.";
            return true;
        }

        public static bool TryUpgradeIndustry(CountryDataSO c, IndustryDataSO def, out string message)
        {
            int level = GetIndustryLevel(c, def.id);
            if (level >= 10)
            {
                message = "Sector already at tier 10.";
                return false;
            }
            float cost = GetIndustryUpgradeCost(c, def);
            if (TreasuryB < cost)
            {
                string sym = string.IsNullOrEmpty(c.currencySymbol) ? "$" : c.currencySymbol;
                message = $"Need {UI.UiFormat.Money(cost, sym)} in treasury.";
                return false;
            }
            SpendTreasury(cost, c);
            ApplyIndustryUpgrade(c, def.id);
            message = $"{def.title} upgraded to tier {GetIndustryLevel(c, def.id)}.";
            return true;
        }

        // ── TAXATION ───────────────────────────────────────────────
        public static float GetTaxRevenue(CountryDataSO c, string taxType)
        {
            float rate = taxType switch
            {
                "Income"    => c.incomeTaxRate,
                "Corporate" => c.corporateTaxRate,
                "VAT"       => c.vatRate,
                "Import"    => c.importTariffRate,
                "Export"    => c.exportDutyRate,
                _ => 0f
            };
            return GetTaxRevenue(c, taxType, rate);
        }

        // Overload that lets callers (e.g. the tax UI's live slider preview) project revenue
        // at a not-yet-committed draft rate, instead of always reading the country's saved rate.
        public static float GetTaxRevenue(CountryDataSO c, string taxType, float rateOverride)
        {
            float monthlyGDP = c.gdp / 12f;
            float reachBonus = 1f + (c.roadLevel * 0.02f);
            float taxEfficiency = (0.3f + (c.stability / 142f)) * reachBonus; 

            // Base revenue multiplier to match previous generic tax scales and keep UI sane
            float revenue = taxType switch
            {
                "Income" => monthlyGDP * (rateOverride / 100f) * 0.35f,
                "Corporate" => monthlyGDP * (rateOverride / 100f) * 0.35f,
                "VAT" => monthlyGDP * (rateOverride / 100f) * 0.35f,
                "Import" => monthlyGDP * 0.015f * (rateOverride / 100f),
                "Export" => monthlyGDP * 0.01f * (rateOverride / 100f),
                _ => 0f
            };

            return revenue * taxEfficiency;
        }

        public static float GetTotalTaxRevenue(CountryDataSO c)
        {
            return GetTaxRevenue(c, "Income") + GetTaxRevenue(c, "Corporate") + GetTaxRevenue(c, "VAT") + GetTaxRevenue(c, "Import") + GetTaxRevenue(c, "Export");
        }
    }
}