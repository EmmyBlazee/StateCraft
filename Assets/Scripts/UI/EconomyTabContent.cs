using System;
using System.Collections.Generic;
using UnityEngine;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;

namespace StateCraft.UI
{
    public static class EconomyTabContent
    {
        public enum InfraCategory { Transport, Energy, Social, Digital }

        public struct InfraDef
        {
            public string Id;
            public string Title;
            public InfraCategory Category;
            public string IconTag;
            public Func<CountryDataSO, float> GetRating;
            public Func<CountryDataSO, string> GetStatus;
            public Func<CountryDataSO, string> GetEffect;
            public Func<CountryDataSO, string> GetBottleneck;
            public Func<CountryDataSO, float> GetInvestCostB;
            public Action<CountryDataSO> ApplyInvest;
            public Func<CountryDataSO, bool> CanInvest;
        }

        public struct IndustryDef
        {
            public string Id;
            public string Title;
            public Func<CountryDataSO, int> GetLevel;
            public Func<CountryDataSO, float> GetWorkforce;
            public Func<CountryDataSO, float> GetRevenueB;
            public Func<CountryDataSO, string> GetSynergy;
            public Func<CountryDataSO, bool> IsBullish;
            public Func<CountryDataSO, float> UpgradeCostB;
            public Action<CountryDataSO> ApplyUpgrade;
        }

        public struct CommodityDef
        {
            public string Title;
            public string Category;
            public Func<CountryDataSO, float> GetStock;
            public Func<CountryDataSO, float> GetProduction;
            public Func<CountryDataSO, float> GetConsumption;
            public string Unit;
            public string Role;
        }

        public struct ProjectBlueprint
        {
            public string Name;
            public string Type;
            public string TargetId;
            public string RegionHint;
            public float TotalCostB;
            public int Months;
            public string Description;
        }

        public static readonly InfraDef[] Infrastructure = BuildInfraDefs();
        public static readonly IndustryDef[] Industries = BuildIndustryDefs();
        public static readonly CommodityDef[] Commodities = BuildCommodities();
        public static readonly ProjectBlueprint[] Blueprints = BuildBlueprints();

        public static float NationalInfraScore(CountryDataSO c)
        {
            float transport = (c.roadLevel + c.railLevel + c.portLevel + c.airportLevel) / 40f * 100f;
            float energy = c.electricityLevel;
            float social = (c.healthcareLevel + c.educationLevel + c.hospitalLevel) / 30f * 100f;
            float digital = c.telecomLevel * 10f;
            return Mathf.Clamp((transport * 0.3f + energy * 0.35f + social * 0.2f + digital * 0.15f), 0f, 100f);
        }

        public static float PowerCapacityMw(CountryDataSO c) =>
            c.powerGasPlants * 120f + c.powerHydroPlants * 80f + c.powerNuclearPlants * 400f
            + c.powerSolarPlants * 25f + c.powerWindPlants * 35f;

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

        static float TreasuryB => GameManager.Instance != null ? GameManager.Instance.playerBudget : 0f;

        static void SpendTreasury(float billions, CountryDataSO c)
        {
            if (GameManager.Instance != null) GameManager.Instance.playerBudget -= billions;
            if (c != null) c.treasury = GameManager.Instance?.playerBudget ?? c.treasury - billions;
        }

        public static bool TryStartProject(CountryDataSO c, ProjectBlueprint bp, out string message)
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
            float deposit = bp.TotalCostB * 0.1f;
            if (TreasuryB < deposit)
            {
                message = "Treasury cannot cover mobilization deposit.";
                return false;
            }

            SpendTreasury(deposit, c);
            c.activeProjects.Add(new ActiveProject
            {
                projectName = bp.Name,
                projectType = bp.Type,
                regionName = string.IsNullOrEmpty(c.capitalCity) ? c.countryName : c.capitalCity,
                targetID = bp.TargetId,
                totalCost = bp.TotalCostB,
                totalMonths = bp.Months,
                monthsRemaining = bp.Months,
                stage = ProjectStage.UnderConstruction
            });
            message = $"{bp.Name} added to pipeline.";
            NewsSystem.Instance?.PublishNews($"PROCUREMENT: {c.countryName} breaks ground on {bp.Name}.");
            return true;
        }

        public static bool TryInvestInfra(CountryDataSO c, InfraDef def, out string message)
        {
            if (!def.CanInvest(c))
            {
                message = $"{def.Title} is at maximum capacity.";
                return false;
            }
            float cost = def.GetInvestCostB(c);
            if (TreasuryB < cost)
            {
                message = $"Need {UiFormat.Money(cost)} in treasury.";
                return false;
            }
            SpendTreasury(cost, c);
            def.ApplyInvest(c);
            message = $"{def.Title} upgraded.";
            return true;
        }

        public static bool TryUpgradeIndustry(CountryDataSO c, IndustryDef def, out string message)
        {
            int level = def.GetLevel(c);
            if (level >= 10)
            {
                message = "Sector already at tier 10.";
                return false;
            }
            float cost = def.UpgradeCostB(c);
            if (TreasuryB < cost)
            {
                message = $"Need {UiFormat.Money(cost)} in treasury.";
                return false;
            }
            SpendTreasury(cost, c);
            def.ApplyUpgrade(c);
            message = $"{def.Title} upgraded to tier {def.GetLevel(c)}.";
            return true;
        }

        private static InfraDef[] BuildInfraDefs()
        {
            return new[]
            {
                new InfraDef
                {
                    Id = "Roads", Title = "NATIONAL HIGHWAYS", Category = InfraCategory.Transport, IconTag = "TRN",
                    GetRating = c => c.roadLevel * 10f,
                    GetStatus = c => $"Level {c.roadLevel}/10 · Tax reach +{c.roadLevel * 2}%",
                    GetEffect = c => "Links farms to cities; improves tax collection & army deployment speed.",
                    GetBottleneck = c => c.roadLevel < 3 ? "Rural districts disconnected from markets." : "—",
                    GetInvestCostB = c => 8f + c.roadLevel * 2.5f,
                    CanInvest = c => c.roadLevel < 10,
                    ApplyInvest = c => { c.roadLevel++; c.publicApproval = Mathf.Min(100f, c.publicApproval + 0.5f); }
                },
                new InfraDef
                {
                    Id = "Rail", Title = "RAIL FREIGHT CORRIDORS", Category = InfraCategory.Transport, IconTag = "TRN",
                    GetRating = c => c.railLevel * 10f,
                    GetStatus = c => $"Level {c.railLevel}/10 · Resource haul +{c.railLevel * 5}%",
                    GetEffect = c => "Bulk minerals & grain move inland; lowers export delays.",
                    GetBottleneck = c => c.railLevel < c.portLevel - 2 ? "Ports congested — no rail offload." : "—",
                    GetInvestCostB = c => 12f + c.railLevel * 3f,
                    CanInvest = c => c.railLevel < 10,
                    ApplyInvest = c => c.railLevel++
                },
                new InfraDef
                {
                    Id = "Ports", Title = "SEAPORTS & HARBORS", Category = InfraCategory.Transport, IconTag = "TRN",
                    GetRating = c => c.portLevel * 10f,
                    GetStatus = c => $"Level {c.portLevel}/10 · Export capacity ×{1f + c.portLevel * 0.03f:F2}",
                    GetEffect = c => "Drives resource dividends and trade deal value.",
                    GetBottleneck = c => c.portLevel < 2 ? "Export backlog costing $50M+ monthly." : "—",
                    GetInvestCostB = c => 15f + c.portLevel * 4f,
                    CanInvest = c => c.portLevel < 10,
                    ApplyInvest = c => { c.portLevel++; c.tradeVolumeUSD += 5e8f; }
                },
                new InfraDef
                {
                    Id = "Airports", Title = "AVIATION HUBS", Category = InfraCategory.Transport, IconTag = "TRN",
                    GetRating = c => c.airportLevel * 10f,
                    GetStatus = c => $"Level {c.airportLevel}/10 · FDI appeal +{c.airportLevel * 3}%",
                    GetEffect = c => "Business travel & air cargo; boosts GDP growth modifier.",
                    GetBottleneck = c => c.airportLevel < 2 ? "No direct long-haul routes to capital markets." : "—",
                    GetInvestCostB = c => 18f + c.airportLevel * 3.5f,
                    CanInvest = c => c.airportLevel < 10,
                    ApplyInvest = c => { c.airportLevel++; c.foreignInvestment += 0.5f; }
                },
                new InfraDef
                {
                    Id = "Power", Title = "NATIONAL POWER GRID", Category = InfraCategory.Energy, IconTag = "PWR",
                    GetRating = c => c.electricityLevel,
                    GetStatus = c =>
                    {
                        float mw = PowerCapacityMw(c);
                        string nuke = c.hasNuclearPower ? " · Nuclear online" : (c.isImportingUranium ? " · Uranium imports" : "");
                        return $"{c.electricityLevel:F0}% coverage · {mw:F0} MW{nuke}";
                    },
                    GetEffect = c => c.electricityLevel < 40f
                        ? "Blackouts throttle factories (−2% growth)."
                        : (c.electricityLevel > 80f ? "Industrial surge (+1% growth)." : "Stable supply for manufacturing."),
                    GetBottleneck = c => c.currentBlackoutRisk > 0.15f ? $"Blackout risk {c.currentBlackoutRisk:P0}" : (c.gridTransmissionEfficiency < 0.8f ? "Grid losses high" : "—"),
                    GetInvestCostB = c => 20f + (100f - c.electricityLevel) * 0.15f,
                    CanInvest = c => c.electricityLevel < 100f,
                    ApplyInvest = c =>
                    {
                        c.electricityLevel = Mathf.Min(100f, c.electricityLevel + 6f);
                        c.gridTransmissionEfficiency = Mathf.Min(1f, c.gridTransmissionEfficiency + 0.03f);
                        c.currentBlackoutRisk = Mathf.Max(0f, c.currentBlackoutRisk - 0.05f);
                        c.powerGasPlants++;
                    }
                },
                new InfraDef
                {
                    Id = "Health", Title = "HEALTHCARE NETWORK", Category = InfraCategory.Social, IconTag = "SOC",
                    GetRating = c => (c.healthcareLevel + c.hospitalLevel) * 5f,
                    GetStatus = c => $"Care Lv.{c.healthcareLevel} · Hospitals Lv.{c.hospitalLevel}",
                    GetEffect = c => "Raises stability & labour productivity.",
                    GetBottleneck = c => c.hospitalLevel < c.healthcareLevel ? "Clinics lack bed capacity." : "—",
                    GetInvestCostB = c => 6f + c.healthcareLevel * 1.5f,
                    CanInvest = c => c.healthcareLevel < 10,
                    ApplyInvest = c => { c.healthcareLevel++; c.stability = Mathf.Min(100f, c.stability + 1f); }
                },
                new InfraDef
                {
                    Id = "Education", Title = "EDUCATION SYSTEM", Category = InfraCategory.Social, IconTag = "SOC",
                    GetRating = c => c.educationLevel * 10f,
                    GetStatus = c => $"Level {c.educationLevel}/10 · Tech output ×{c.techIndustryOutput / 100f:F2}",
                    GetEffect = c => "Feeds IT/fintech sectors and long-run growth.",
                    GetBottleneck = c => c.educationLevel < 3 ? "Skills gap limits industrial upgrade." : "—",
                    GetInvestCostB = c => 7f + c.educationLevel * 2f,
                    CanInvest = c => c.educationLevel < 10,
                    ApplyInvest = c => { c.educationLevel++; c.techIndustryOutput = Mathf.Min(150f, c.techIndustryOutput + 3f); }
                },
                new InfraDef
                {
                    Id = "Telecom", Title = "DIGITAL BACKBONE", Category = InfraCategory.Digital, IconTag = "NET",
                    GetRating = c => c.telecomLevel * 10f,
                    GetStatus = c => $"Level {c.telecomLevel}/10 · Fintech readiness",
                    GetEffect = c => "Enables tech industry & intelligence coverage.",
                    GetBottleneck = c => c.telecomLevel < 4 ? "Rural areas off-grid for mobile banking." : "—",
                    GetInvestCostB = c => 9f + c.telecomLevel * 2.5f,
                    CanInvest = c => c.telecomLevel < 10,
                    ApplyInvest = c => { c.telecomLevel++; c.indTechFintech = Mathf.Min(10, c.indTechFintech + (c.telecomLevel % 3 == 0 ? 1 : 0)); }
                }
            };
        }

        private static IndustryDef[] BuildIndustryDefs() => new[]
        {
            new IndustryDef
            {
                Id = "Oil", Title = "OIL & GAS", GetLevel = c => c.indMiningOil,
                GetWorkforce = c => c.workforceOil,
                GetRevenueB = c => (c.gdp) * 0.05f * (c.indMiningOil / 10f) * c.exchangeRate,
                GetSynergy = c => $"Ports Lv.{c.portLevel} · Power {c.electricityLevel:F0}%",
                IsBullish = c => c.electricityLevel > 50f && c.portLevel > 1,
                UpgradeCostB = c => (10f + c.indMiningOil * 3f) * c.exchangeRate,
                ApplyUpgrade = c => { c.indMiningOil++; c.workforceOil += 2000f; }
            },
            new IndustryDef
            {
                Id = "Mining", Title = "MINING & MINERALS", GetLevel = c => c.indMiningLocal,
                GetWorkforce = c => c.workforceMining,
                GetRevenueB = c => (c.gdp) * 0.02f * (c.indMiningLocal / 10f) * c.exchangeRate,
                GetSynergy = c => $"Rail Lv.{c.railLevel} · Sovereignty {c.resourceSovereignty:F0}%",
                IsBullish = c => c.railLevel > 2 && c.resourceSovereignty > 60f,
                UpgradeCostB = c => (8f + c.indMiningLocal * 2.5f) * c.exchangeRate,
                ApplyUpgrade = c => { c.indMiningLocal++; c.workforceMining += 5000f; }
            },
            new IndustryDef
            {
                Id = "Agri", Title = "AGRICULTURE", GetLevel = c => c.indAgriRice + c.indAgriCocoa + c.indAgriCassava,
                GetWorkforce = c => c.workforceAgri,
                GetRevenueB = c => (c.gdp) * 0.03f * ((c.indAgriRice + c.indAgriCocoa + c.indAgriCassava) / 30f) * c.exchangeRate,
                GetSynergy = c => $"Food security {c.foodSecurity:F0}% · Roads Lv.{c.roadLevel}",
                IsBullish = c => c.foodSecurity > 55f && c.roadLevel > 2,
                UpgradeCostB = c => (5f + (c.indAgriRice + c.indAgriCocoa) * 1.5f) * c.exchangeRate,
                ApplyUpgrade = c =>
                {
                    if (c.indAgriRice <= c.indAgriCocoa) c.indAgriRice++;
                    else c.indAgriCocoa++;
                    c.foodSecurity = Mathf.Min(100f, c.foodSecurity + 2f);
                }
            },
            new IndustryDef
            {
                Id = "Mfg", Title = "MANUFACTURING", GetLevel = c => c.indManufacturing,
                GetWorkforce = c => c.workforceManufacturing,
                GetRevenueB = c => (c.gdp) * 0.025f * (c.indManufacturing / 10f) * c.exchangeRate,
                GetSynergy = c => $"Power {c.electricityLevel:F0}% · Roads Lv.{c.roadLevel}",
                IsBullish = c => c.electricityLevel > 55f && c.roadLevel > 3,
                UpgradeCostB = c => (12f + c.indManufacturing * 3f) * c.exchangeRate,
                ApplyUpgrade = c => { c.indManufacturing++; c.workforceManufacturing += 1500f; }
            },
            new IndustryDef
            {
                Id = "Tech", Title = "TECH & FINTECH", GetLevel = c => c.indTechIT + c.indTechFintech,
                GetWorkforce = c => (c.indTechIT + c.indTechFintech) * 800f,
                GetRevenueB = c => (c.gdp) * 0.02f * ((c.indTechIT + c.indTechFintech) / 20f) * c.exchangeRate,
                GetSynergy = c => $"Telecom Lv.{c.telecomLevel} · Education Lv.{c.educationLevel}",
                IsBullish = c => c.telecomLevel > 3 && c.educationLevel > 2,
                UpgradeCostB = c => (14f + (c.indTechIT + c.indTechFintech) * 2f) * c.exchangeRate,
                ApplyUpgrade = c =>
                {
                    if (c.indTechIT <= c.indTechFintech) c.indTechIT++;
                    else c.indTechFintech++;
                    c.techIndustryOutput = Mathf.Min(150f, c.techIndustryOutput + 5f);
                }
            }
        };

        private static CommodityDef[] BuildCommodities() => new[]
        {
            new CommodityDef
            {
                Title = "ELECTRICITY", Category = "Energy & Power", Unit = "MW", Role = "National power grid load",
                GetStock = c => PowerCapacityMw(c) - (c.indManufacturing * 10f + c.population * 0.0005f),
                GetProduction = c => PowerCapacityMw(c),
                GetConsumption = c => c.indManufacturing * 10f + c.population * 0.0005f
            },
            new CommodityDef
            {
                Title = "CRUDE OIL", Category = "Energy & Power", Unit = "Barrels", Role = "Export / refining feedstock",
                GetStock = c => c.invOilBarrels, GetProduction = c => c.indMiningOil * 2_000_000f,
                GetConsumption = c => c.indManufacturing * 50_000f
            },
            new CommodityDef
            {
                Title = "RICE", Category = "Food & Agriculture", Unit = "Tonnes", Role = "Staple food security",
                GetStock = c => c.invRiceTonnes, GetProduction = c => c.indAgriRice * 50_000f,
                GetConsumption = c => c.population * 0.001f * 0.4f
            },
            new CommodityDef
            {
                Title = "CASSAVA", Category = "Food & Agriculture", Unit = "Tonnes", Role = "Staple food security",
                GetStock = c => c.invCassavaTonnes, GetProduction = c => c.indAgriCassava * 80_000f,
                GetConsumption = c => c.population * 0.001f * 0.6f
            },
            new CommodityDef
            {
                Title = "COCOA", Category = "Food & Agriculture", Unit = "Tonnes", Role = "Cash crop export",
                GetStock = c => c.invCocoaTonnes, GetProduction = c => c.indAgriCocoa * 20_000f,
                GetConsumption = _ => 0f
            },
            new CommodityDef
            {
                Title = "PROCESSED MINERALS", Category = "Materials & Goods", Unit = "Tonnes", Role = "Industrial inputs",
                GetStock = c => c.invMineralsTonnes, GetProduction = c => c.indMiningLocal * 100_000f,
                GetConsumption = c => c.indManufacturing * 8_000f
            },
            new CommodityDef
            {
                Title = "TECH GOODS", Category = "Materials & Goods", Unit = "Units", Role = "High-value exports",
                GetStock = c => c.invTechGoods, GetProduction = c => (c.indTechIT + c.indTechFintech) * 5000f * (c.techIndustryOutput / 100f),
                GetConsumption = c => c.population * 0.00001f
            }
        };

        private static ProjectBlueprint[] BuildBlueprints() => new[]
        {
            new ProjectBlueprint { Name = "Coastal Highway Phase II", Type = "Infra", TargetId = "Roads", TotalCostB = 45f, Months = 18, Description = "Paved corridor linking ports to interior." },
            new ProjectBlueprint { Name = "Trans-National Rail Spine", Type = "Infra", TargetId = "Rail", TotalCostB = 80f, Months = 36, Description = "Heavy freight line for minerals & grain." },
            new ProjectBlueprint { Name = "Deep-Water Port Expansion", Type = "Infra", TargetId = "Ports", TotalCostB = 60f, Months = 24, Description = "Container terminals & export berths." },
            new ProjectBlueprint { Name = "Gas-Fired Power Complex", Type = "Energy", TargetId = "PowerGas", TotalCostB = 35f, Months = 14, Description = "+120 MW baseload capacity." },
            new ProjectBlueprint { Name = "Hydroelectric Dam", Type = "Energy", TargetId = "PowerHydro", TotalCostB = 55f, Months = 30, Description = "Renewable surge for industrial zones." },
            new ProjectBlueprint { Name = "Nuclear Power Station", Type = "Energy", TargetId = "PowerNuclear", TotalCostB = 120f, Months = 48, Description = "Requires uranium imports or domestic supply." },
            new ProjectBlueprint { Name = "Solar Belt Programme", Type = "Energy", TargetId = "PowerSolar", TotalCostB = 22f, Months = 10, Description = "Distributed grids for rural towns." },
            new ProjectBlueprint { Name = "Rice Belt Mechanization", Type = "Agri", TargetId = "indAgriRice", TotalCostB = 18f, Months = 12, Description = "Irrigation & mill upgrades." },
            new ProjectBlueprint { Name = "Cocoa Processing Park", Type = "Agri", TargetId = "indAgriCocoa", TotalCostB = 15f, Months = 10, Description = "Bean-to-bar export value chain." },
            new ProjectBlueprint { Name = "National Data Centre", Type = "Tech", TargetId = "indTechIT", TotalCostB = 25f, Months = 16, Description = "Cloud & fintech backbone." }
        };
    }
}
