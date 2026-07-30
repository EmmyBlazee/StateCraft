using StateCraft.Interfaces;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.UI;
using UnityEngine;

namespace StateCraft.Systems
{
    public class EconomySystem : IEconomySystem
    {
        public void UpdateEconomy(CountryDataSO country)
        {
            if (float.IsNaN(country.gdp) || float.IsInfinity(country.gdp)) country.gdp = 100e9f;
            if (float.IsNaN(country.exchangeRate) || country.exchangeRate <= 0.01f || float.IsInfinity(country.exchangeRate)) country.exchangeRate = Mathf.Max(0.5f, country.baseExchangeRate);
            if (float.IsNaN(country.treasury) || float.IsInfinity(country.treasury)) country.treasury = 0f;
            if (float.IsNaN(country.stability) || float.IsInfinity(country.stability)) country.stability = 50f;

            // Calculate dynamic military upkeep based on active forces
            country.militaryUpkeepUSD = country.CalculateMilitaryUpkeep();

            // Reset income based on base values + policies
            country.income = CalculateProjectedIncome(country);
            
            // Subsidies are a direct cost based on GDP (applied monthly)
            float subsidyCost = (country.gdp / 12f) * (country.subsidyLevel / 100f);
            float uraniumImportFee = country.isImportingUranium ? 0.05f : 0f; // 0.05B = 
            
            // Check for underfunded military
            if (country.militaryUpkeepUSD > country.militaryBudgetUSD) {
                country.militaryReadiness = Mathf.Max(0f, country.militaryReadiness - 2f);
                country.stability = Mathf.Max(0f, country.stability - 0.5f);
                if (country == Core.GameManager.Instance?.playerCountry) {
                    NotificationManager.Instance?.AddNotification($"MILITARY UNDERFUNDED: Allocated budget is insufficient to cover active unit upkeep! Readiness dropping.", UnityEngine.Color.red);
                }
            } else {
                country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 0.5f);
            }

            // DO NOT OVERWRITE country.spending! Let it be ruled by the Economy UI True Deficit allocations!
            // Deduct mandatory background operations directly from income so they don't break the locked budget UI.
            country.income -= (subsidyCost + uraniumImportFee);

            // ── Tax Gap → Arrears Accumulation ───────────────────────────────
            // Theoretical full-compliance revenue vs what is actually collected.
            // The gap is accumulated monthly into taxArrears (unpaid taxes owed to the state).
            {
                float fullRevenue      = EconomyLogicEvaluator.GetTotalTaxRevenue(country);
                // Match the UI's compliance/efficiency formulas exactly
                float compliancePct    = Mathf.Clamp(100f - country.corruptionIndex, 10f, 100f);
                float collectionEff    = Mathf.Lerp(60f, 95f, country.stability / 100f) / 100f;
                float actualCollected  = fullRevenue * (compliancePct / 100f) * collectionEff;
                float monthlyGap       = Mathf.Max(0f, fullRevenue - actualCollected);
                country.taxArrears    += monthlyGap;

                // High unpaid arrears slowly erode the credit rating (~every 6 months of 20%+ GDP gap)
                float gdpMonthly = country.gdp / 12f;
                if (gdpMonthly > 0f && (country.taxArrears / gdpMonthly) > 0.20f)
                {
                    // Downgrade credit rating with low probability (≈10% per month at crisis level)
                    if (UnityEngine.Random.value < 0.10f)
                        country.creditRating = DegradeRating(country.creditRating);
                }
            }

            // Apply Policy Effects

            foreach (var policy in country.activePolicies)
            {
                country.income -= policy.recurringCost; // Some policies might be costs
                if (policy.type == PolicyType.Economy)
                {
                    // Economy policies might increase income (taxes) or decrease it (subsidies)
                    // but they definitely affect GDP growth
                }
            }

            // Add policy impacts on growth
            float growthRate = country.gdpGrowthRate * (country.stability / 100f);
            
            // Monetary Policy (Interest Rate) Impact on GDP Growth
            if (country.centralBankInterestRate > 6f)
            {
                growthRate -= (country.centralBankInterestRate - 6f) * 0.001f;
            }
            else if (country.centralBankInterestRate < 4f)
            {
                growthRate += (4f - country.centralBankInterestRate) * 0.001f;
            }

            // Trade Policy (Import Tariff) Impact on GDP Growth (Trade suppression)
            if (country.importTariffRate > 15f)
            {
                growthRate -= (country.importTariffRate - 15f) * 0.0005f;
            }
            
            // Infrastructure Growth Boost
            growthRate += (country.airportLevel * 0.002f) + (country.roadLevel * 0.001f) + (country.educationLevel * 0.0015f);
            
            // Health productivity boost (higher stability + small growth)
            growthRate += (country.healthcareLevel * 0.0005f);
            
            // ELECTRICITY IMPACT
            if (country.electricityLevel < 40f) growthRate -= 0.02f; // Blackout crisis
            else if (country.electricityLevel > 80f) growthRate += 0.01f; // Industrial surge
            
            // --- MODERN AGE RIPPLE EFFECTS ---
            
            // 1. Taxation Ripple: High taxes drain Foreign Investment and suppress growth
            if (country.taxRate > 25f)
            {
                float penalty = (country.taxRate - 25f) * 0.01f; // 1% drop per 1% over 25
                country.foreignInvestment = Mathf.Max(0, country.foreignInvestment - (penalty * 1e9f));
                growthRate -= penalty * 0.05f; 
            }
            else if (country.taxRate < 15f)
            {
                country.foreignInvestment += (15f - country.taxRate) * 0.005f * 1e9f;
                growthRate += (15f - country.taxRate) * 0.002f;
            }

            // 2. Welfare Ripple: Subsidies boost Stability but trigger Inflation
            if (country.subsidyLevel > 5f)
            {
                country.stability = Mathf.Min(100f, country.stability + (country.subsidyLevel - 5f) * 0.2f);
                country.inflationRate += (country.subsidyLevel - 5f) * 0.0005f;
            }
            else if (country.subsidyLevel < 2f)
            {
                country.stability -= (2f - country.subsidyLevel) * 0.5f;
            }

            if (float.IsNaN(growthRate) || float.IsInfinity(growthRate)) growthRate = 0.001f;
            country.gdp *= (1f + growthRate);

            // Inflation calculation (extended)
            if (country.gdp > 0 && !float.IsNaN(country.gdp))
            {
                float debtRatio = country.debt / country.gdp;
                if (debtRatio > 0.8f) country.inflationRate += 0.002f;
                else if (debtRatio < 0.2f) country.inflationRate -= 0.001f;
                
                // Printing money (implied by budget deficit if treasury < 0)
                if (country.treasury < 0) country.inflationRate += 0.005f;

                // Monetary Policy (Interest Rate) Impact on Inflation
                if (country.centralBankInterestRate > 6f)
                {
                    country.inflationRate -= (country.centralBankInterestRate - 6f) * 0.001f;
                }
                else if (country.centralBankInterestRate < 4f)
                {
                    country.inflationRate += (4f - country.centralBankInterestRate) * 0.0005f;
                }

                // Trade Policy (Tariffs) Cost-Push Inflation
                if (country.importTariffRate > 15f)
                {
                    country.inflationRate += (country.importTariffRate - 15f) * 0.0002f;
                }
            }

            country.inflationRate = Mathf.Clamp(country.inflationRate, 0f, 2.0f); // Allow hyperinflation

            
            // --- DEMOGRAPHICS & PRODUCTION ---
            long oldPopulation = country.population;
            float popGrowthModifier = country.populationGrowthRate / 12f;
            
            // Policies and Status affecting Population
            if (country.taxRate > 25f) popGrowthModifier -= (country.taxRate - 25f) * 0.0001f; // High tax emigration
            if (country.stability < 30f) popGrowthModifier -= 0.001f; // Conflict/instability flight
            if (country.reserveFood < 20f || country.foodSecurity < 50f) popGrowthModifier -= 0.003f; // Famine causes population loss
            if (country.healthcareLevel > 5) popGrowthModifier += 0.0005f; // Good healthcare boosts lifespan
            
            country.population += (long)(country.population * popGrowthModifier);
            country.populationChangeLastTurn = country.population - oldPopulation;

            ProcessProduction(country);
            ProcessTradeAgreements(country);
            ProcessDebtService(country);

            // --- SCARCITY PENALTY (EMBARGO/ISOLATION) ---
            if (country.resources != null)
            {
                bool hasOilScarcity = true;
                foreach (var res in country.resources)
                {
                    if (res.type == MineralType.Oil && res.abundance >= 20f)
                    {
                        hasOilScarcity = false;
                        break;
                    }
                }

                // Check imports (has active deal buying Oil)
                if (hasOilScarcity)
                {
                    if (country.activeTradeAgreements != null)
                    {
                        foreach (var deal in country.activeTradeAgreements)
                        {
                            if (deal.status == TradeAgreementStatus.Active && !deal.isSeller && deal.resource == TradeResourceType.Oil)
                            {
                                hasOilScarcity = false;
                                break;
                            }
                        }
                    }
                    if (Core.GameManager.Instance.DiplomacySystem is DiplomacySystem diplo)
                    {
                        foreach (var deal in diplo.ActiveDeals)
                        {
                            if (deal.buyerCountry == country.countryName && (deal.resource == StateCraft.Interfaces.TradeResourceType.Oil || deal.resource == StateCraft.Interfaces.TradeResourceType.Gas))
                            {
                                hasOilScarcity = false;
                                break;
                            }
                        }
                    }
                }

                // If fundamentally starved of Oil, GDP drops by 2% per month automatically (Economic collapse)
                if (hasOilScarcity)
                {
                    country.gdp *= 0.98f;
                    country.stability -= 1.0f;
                    
                    if (country != Core.GameManager.Instance.playerCountry && UnityEngine.Random.value > 0.85f)
                    {
                        NewsSystem.Instance?.PublishNews($"CRISIS: Severe fuel shortages cripple the {country.countryName} economy due to global isolation.", true);
                    }
                }
            }

            // --- EXCHANGE RATE DYNAMICS ---
            UpdateExchangeRate(country);
            
            // --- CONSTRUCTION PROJECTS ---
            ProcessConstructionProjects(country);
            
// ── Build the ledger entry for this month ─────────────────
            var gm = GameManager.Instance;
            if (gm?.Ledger != null && country == gm.playerCountry)
            {
                float subsidyCostCalc = (country.gdp / 12f) * (country.subsidyLevel / 100f);
                float milUpkeep       = country.militaryBudgetUSD;
                float constCost       = 0f;
                if (country.activeProjects != null)
                    foreach (var p in country.activeProjects)
                        if (p.stage == ProjectStage.UnderConstruction)
                            constCost += p.totalCost / Mathf.Max(1f, (float)p.totalMonths);

                float debtInt = gm.DebtSystem != null
                    ? (gm.DebtSystem.CalculateDomesticInterest(country) + gm.DebtSystem.CalculateForeignInterest(country)) * country.exchangeRate
                    : 0f;

                float corruptLeak = CorruptionSystem.GetLeakageAmount(country);

                float tradeInc = 0f;
                if (country.activeTradeAgreements != null)
                    foreach (var deal in country.activeTradeAgreements)
                        if (deal.status == TradeAgreementStatus.Active && deal.isSeller)
                            tradeInc += (deal.monthlyPaymentUSD * country.exchangeRate) / 1e9f;

                float opsCost = 0f;
                if (country.activeOperations != null)
                    foreach (var op in country.activeOperations)
                        opsCost += (op.costPerMonth > 0f ? op.costPerMonth / 1000f : 0.05f) * country.exchangeRate;

                float warCostCalc = 0f;
                if (gm.WarSystem?.GetWarStage(country) == WarStage.Conflict)
                    warCostCalc = country.gdp * 0.025f; // 2.5% GDP/month at war

                float net = country.income - country.spending - debtInt - corruptLeak - opsCost;

                if (country.budgetHistory == null) country.budgetHistory = new System.Collections.Generic.List<float>();
                country.budgetHistory.Add(net);
                if (country.budgetHistory.Count > 6) country.budgetHistory.RemoveAt(0);

                float monthlyGDP = country.gdp / 12f;
                float regionalDisruption = 1f;
                if (country.regions != null)
                {
                    foreach (var region in country.regions)
                    {
                        if (region.specialization == RegionSpecialization.ResourceRich && region.insurgencyLevel > 30f)
                        {
                            regionalDisruption -= (region.insurgencyLevel - 30f) * 0.005f;  
                        }
                    }
                }
                regionalDisruption = Mathf.Max(0.1f, regionalDisruption);
                monthlyGDP *= regionalDisruption;

                float exactTaxRevenue = StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "Income") + 
                                        StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "Corporate") + 
                                        StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "VAT");
                float exactTariffRevenue = StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "Import") + 
                                           StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "Export");

                float exactResourceIncome = 0;
                if (country.resources != null)
                {
                    foreach (var res in country.resources)
                    {
                        float infraBonus = 1f + (country.railLevel * 0.05f) + (country.portLevel * 0.03f) + (country.electricityLevel / 200f);
                        exactResourceIncome += (res.abundance / 100f) * (monthlyGDP * 0.05f) * infraBonus;
                    }
                }

                var entry = new EconomyLedgerEntry
                {
                    month           = gm.TurnSystem?.CurrentTurn ?? 0,
                    openingTreasury = country.treasury - net,
                    closingTreasury = country.treasury,
                    netChange       = net,
                    taxRevenue      = exactTaxRevenue,
                    resourceIncome  = exactResourceIncome,
                    tariffRevenue   = exactTariffRevenue,
                    tradeIncome     = tradeInc,
                    militaryUpkeep  = milUpkeep,
                    subsidyCost     = subsidyCostCalc,
                    debtInterest    = debtInt,
                    operationsCost  = opsCost,
                    constructionCost= constCost,
                    corruptionLeakage= corruptLeak,
                    uraniumImportFee= country.isImportingUranium ? 0.05f : 0f,
                    gdpGrowthEffect = country.gdp * country.gdpGrowthRate / 12f,
                    inflationEffect = country.inflationRate,
                    warCost         = warCostCalc,
                };

                // Build explanations for significant line items
                if (Mathf.Abs(net) > 0.5f)
                    entry.explanations.Add($"Net change {(net >= 0 ? "+" : "")}{net:F2}B: " +
                                           $"income {country.income:F2}B minus spending {country.spending:F2}B");
                if (debtInt > 0.1f)
                    entry.explanations.Add($"Debt service: −{debtInt:F2}B (interest payments)");
                if (corruptLeak > 0.05f)
                    entry.explanations.Add($"Corruption: −{corruptLeak:F2}B leaked from treasury");
                if (milUpkeep > 0.1f)
                    entry.explanations.Add($"Military upkeep: −{milUpkeep:F2}B (active forces)");
                if (tradeInc > 0.05f)
                    entry.explanations.Add($"Trade exports: +{tradeInc:F2}B (active agreements)");
                if (opsCost > 0.05f)
                    entry.explanations.Add($"Operations: −{opsCost:F2}B ({country.activeOperations?.Count ?? 0} active)");
                if (warCostCalc > 0.1f)
                    entry.explanations.Add($"War economy: −{warCostCalc:F2}B GDP drain from active conflict");
                if (country.inflationRate > 0.05f)
                    entry.explanations.Add($"High inflation ({country.inflationRate:P1}) devaluing currency");
                if (country.gdp * country.gdpGrowthRate / 12f < 0f)
                    entry.explanations.Add($"GDP contraction: economy shrinking at {country.gdpGrowthRate:P2}/month");

                gm.Ledger.Record(country, entry);
            }
        }

        private void ProcessTradeAgreements(CountryDataSO country)
        {
            if (country.activeTradeAgreements == null) return;

            for (int i = country.activeTradeAgreements.Count - 1; i >= 0; i--)
            {
                var deal = country.activeTradeAgreements[i];
                if (deal.status != TradeAgreementStatus.Active) continue;

                deal.durationMonths--;
                if (deal.durationMonths <= 0)
                {
                    deal.status = TradeAgreementStatus.Expired;
                    if (country == GameManager.Instance?.playerCountry)
                    {
                        NewsSystem.Instance?.PublishNews($"TRADE EXPIRED: Supply of {deal.resource} with {deal.partnerCountry} has concluded.");
                    }
                    continue;
                }

                float actualPayment = deal.monthlyPaymentUSD;

                if (deal.isSeller)
                {
                    float amt = deal.resourceQuantity;
                    float available = 0f;
                    switch (deal.resource)
                    {
                        case TradeResourceType.Oil: available = country.invOilBarrels; break;
                        case TradeResourceType.Gas: available = country.invGasUnits; break;
                        case TradeResourceType.Uranium: available = country.invUraniumUnits; break;
                        case TradeResourceType.Minerals: available = country.invMineralsTonnes; break;
                        case TradeResourceType.Food: available = country.invRiceTonnes + country.invCassavaTonnes + country.invCocoaTonnes; break;
                    }

                    if (available < amt)
                    {
                        country.globalPrestige = Mathf.Max(0, country.globalPrestige - 1f);
                        if (country == Core.GameManager.Instance?.playerCountry)
                            NotificationManager.Instance?.AddNotification($"PARTIAL DELIVERY: Unable to fulfill full {deal.resource} export to {deal.partnerCountry}.", Color.yellow);
                        
                        if (available <= 0f)
                        {
                            deal.status = TradeAgreementStatus.Expired;
                        }
                        
                        float deliveryRatio = amt > 0f ? available / amt : 0f;
                        actualPayment *= deliveryRatio;
                        amt = available;
                    }

                    switch (deal.resource)
                    {
                        case TradeResourceType.Oil: country.invOilBarrels = Mathf.Max(0, country.invOilBarrels - amt); break;
                        case TradeResourceType.Gas: country.invGasUnits = Mathf.Max(0, country.invGasUnits - amt); break;
                        case TradeResourceType.Uranium: country.invUraniumUnits = Mathf.Max(0, country.invUraniumUnits - amt); break;
                        case TradeResourceType.Minerals: country.invMineralsTonnes = Mathf.Max(0, country.invMineralsTonnes - amt); break;
                        case TradeResourceType.Food:
                            float remaining = amt;
                            float riceTake = Mathf.Min(country.invRiceTonnes, remaining);
                            country.invRiceTonnes -= riceTake; remaining -= riceTake;
                            float cassTake = Mathf.Min(country.invCassavaTonnes, remaining);
                            country.invCassavaTonnes -= cassTake; remaining -= cassTake;
                            country.invCocoaTonnes = Mathf.Max(0, country.invCocoaTonnes - remaining);
                            break;
                    }
                }
                else
                {
                    float amt = deal.resourceQuantity;
                    switch (deal.resource)
                    {
                        case TradeResourceType.Oil: country.invOilBarrels += amt; break;
                        case TradeResourceType.Gas: country.invGasUnits += amt; break;
                        case TradeResourceType.Uranium: country.invUraniumUnits += amt; break;
                        case TradeResourceType.Minerals: country.invMineralsTonnes += amt; break;
                        case TradeResourceType.Food: country.invRiceTonnes += amt; break;
                    }
                }
                
                if (deal.isBulkTransfer)
                {
                    deal.status = TradeAgreementStatus.Expired;
                    if (deal.isSeller)
                        country.treasury += (actualPayment * country.exchangeRate) / 1e9f;
                    else
                        country.treasury -= (actualPayment * country.exchangeRate) / 1e9f;
                }
                else
                {
                    if (deal.isSeller)
                        country.treasury += (actualPayment * country.exchangeRate) / 1e9f;
                    else
                        country.treasury -= (actualPayment * country.exchangeRate) / 1e9f;
                }
            }

            if (country == GameManager.Instance?.playerCountry)
            {
                GameManager.Instance.playerBudget = country.treasury;
            }
        }

        private void UpdateExchangeRate(CountryDataSO country)
        {
            // Currency devalues if inflation is high or stability is low
            float devaluationPressure = (country.inflationRate * 0.3f) + (1f - (country.stability / 100f)) * 0.05f;
            float improvementPressure = 0.01f; // Base 1% natural recovery/appreciation

            // Higher interest rates attract foreign capital, improving currency demand / reducing devaluation pressure
            float interestRateAppreciation = (country.centralBankInterestRate - 5f) * 0.005f;
            improvementPressure += interestRateAppreciation;

            float netChange = 1f + (devaluationPressure - improvementPressure);
            netChange = Mathf.Clamp(netChange, 0.90f, 1.15f); // Max ±15% swing per turn
            country.exchangeRate *= netChange;

            // Hard cap: never exceed 10× the starting rate
            float maxRate = country.baseExchangeRate > 0f ? country.baseExchangeRate * 10f : country.exchangeRate;
            country.exchangeRate = Mathf.Clamp(country.exchangeRate, country.baseExchangeRate * 0.5f, maxRate);

            if (netChange > 1.10f) // 10% drop in one turn is a crisis
            {
                NewsSystem.Instance?.PublishNews($"CURRENCY CRASH: THE {country.currencyName.ToUpper()} COLLAPSES BY {(netChange - 1f):P} IN MASSIVE SELL-OFF.");
            }
        }

        private void ProcessProduction(CountryDataSO country)
        {
            // Production output per level
            country.invRiceTonnes += country.indAgriRice * 50000f; // 50k tonnes per level per month
            country.invCassavaTonnes += country.indAgriCassava * 80000f; // 80k tonnes
            country.invCocoaTonnes += country.indAgriCocoa * 20000f; // 20k tonnes

            country.invMineralsTonnes += country.indMiningLocal * 100000f; // 100k tonnes
            country.invOilBarrels += country.indMiningOil * 2000000f; // 2M barrels
            
            if (country.resources != null)
            {
                if (country.resources.Exists(r => r.type == MineralType.NaturalGas))
                    country.invGasUnits += country.indMiningOil * 500000f;
                if (country.resources.Exists(r => r.type == MineralType.Uranium))
                    country.invUraniumUnits += country.indMiningLocal * 5000f;
            }
            
            // Tech Output modified by Productivity Multiplier
            float techOutput = (country.indTechIT + country.indTechFintech) * 5000f;
            country.invTechGoods += techOutput * (country.techIndustryOutput / 100f);


            // Domestic Consumption (Basic model: people eat rice and cassava)
            // 1kg of food per person per month = 0.001 tonnes
            float foodNeeded = country.population * 0.001f; 
            
            // Consume available food
            country.invRiceTonnes = Mathf.Max(0, country.invRiceTonnes - (foodNeeded * 0.4f));
            country.invCassavaTonnes = Mathf.Max(0, country.invCassavaTonnes - (foodNeeded * 0.6f));
        }

        public float CalculateProjectedIncome(CountryDataSO country)
        {
            // Monthly GDP
            float monthlyGDP = country.gdp / 12f;

            // SPECIALIZATION: Regional Economic Disruption
            // If ResourceRich regions fall to insurgency, national production tanks rapidly.
            float regionalDisruption = 1f;
            if (country.regions != null)
            {
                foreach (var region in country.regions)
                {
                    if (region.specialization == RegionSpecialization.ResourceRich && region.insurgencyLevel > 30f)
                    {
                        // E.g. at 100% insurgency, we lose 35% of national GDP per ResourceRich region lost.
                        float loss = (region.insurgencyLevel - 30f) * 0.005f;  
                        regionalDisruption -= loss;
                    }
                }
            }
            regionalDisruption = Mathf.Max(0.1f, regionalDisruption);
            monthlyGDP *= regionalDisruption;

            // Revenue based on all individual tax rates
            float totalTaxAndTariffRevenue = EconomyLogicEvaluator.GetTotalTaxRevenue(country);
            
            // Resource Dividend: 5% of Monthly GDP scaled by resource wealth
            float resourceIncome = 0;
            foreach (var res in country.resources)
            {
                // Rail, Ports and Power boost resource income significantly
                float infraBonus = 1f + (country.railLevel * 0.05f) + (country.portLevel * 0.03f) + (country.electricityLevel / 200f);
                resourceIncome += (res.abundance / 100f) * (monthlyGDP * 0.05f) * infraBonus;
            }
            
            return totalTaxAndTariffRevenue + resourceIncome;
        }

        private void ProcessDebtService(CountryDataSO country)
        {
            if (country.debt <= 0f) return;

            // 1% of total debt per month is the baseline principal repayment.
            // Slider at 100% = standard payment. 200% = accelerated payoff. 0% = skip (penalty).
            float principalRatio = country.debtServiceRatio / 100f;
            float principalRepaymentUSD = country.debt * 0.01f * principalRatio;

            if (principalRatio <= 0f)
            {
                // Skip payments — credit rating and stability penalty
                country.stability = Mathf.Max(0f, country.stability - 0.5f);
                country.creditRating = DegradeRating(country.creditRating);
                bool globalOnly = (country != GameManager.Instance?.playerCountry);
                NewsSystem.Instance?.PublishNews($"MARKETS: {country.countryName} skips debt servicing — credit outlook worsens.", globalOnly);
                return;
            }

            // Convert to local billions for treasury deduction
            float repayLocalB = (principalRepaymentUSD * country.exchangeRate);

            if (country.treasury >= repayLocalB)
            {
                country.treasury -= repayLocalB;

                // Split repayment proportionally between domestic and foreign
                float totalDebt = Mathf.Max(country.debt, 1f);
                country.domesticDebt = Mathf.Max(0f, country.domesticDebt - principalRepaymentUSD * (country.domesticDebt / totalDebt));
                country.foreignDebt  = Mathf.Max(0f, country.foreignDebt  - principalRepaymentUSD * (country.foreignDebt  / totalDebt));
                country.debt = country.domesticDebt + country.foreignDebt;

                // Paying above 120% of standard gradually improves credit
                if (principalRatio >= 1.2f)
                    country.creditRating = ImproveRating(country.creditRating);
            }
            else
            {
                // Can't afford it — partial default penalty
                country.stability = Mathf.Max(0f, country.stability - 1f);
                country.creditRating = DegradeRating(country.creditRating);
                bool globalOnly = (country != GameManager.Instance?.playerCountry);
                if (UnityEngine.Random.value > 0.7f)
                    NewsSystem.Instance?.PublishNews($"MARKETS: {country.countryName} unable to meet debt obligations this month.", globalOnly);
            }
        }

        private string ImproveRating(string current)
        {
            string[] ladder = { "D", "C", "CC", "CCC", "B", "B+", "BB", "BB+", "BBB", "BBB+", "A", "A+", "AA", "AA+", "AAA" };
            int idx = System.Array.IndexOf(ladder, current);
            return (idx >= 0 && idx < ladder.Length - 1) ? ladder[idx + 1] : current;
        }

        private string DegradeRating(string current)
        {
            string[] ladder = { "D", "C", "CC", "CCC", "B", "B+", "BB", "BB+", "BBB", "BBB+", "A", "A+", "AA", "AA+", "AAA" };
            int idx = System.Array.IndexOf(ladder, current);
            return (idx > 0) ? ladder[idx - 1] : current;
        }

        private void ProcessConstructionProjects(CountryDataSO country)
        {
            if (country.activeProjects == null) return;
            
            for (int i = country.activeProjects.Count - 1; i >= 0; i--)
            {
                var project = country.activeProjects[i];

                if (project.stage == ProjectStage.Planned || project.stage == ProjectStage.Funded)
                {
                    project.stage = ProjectStage.UnderConstruction;
                }
                else if (project.stage == ProjectStage.UnderConstruction)
                {
                    float monthlyCost = project.totalCost / Mathf.Max(1f, (float)project.totalMonths);
                    
                    // Only progress if treasury can cover the burn rate
                    if (country.treasury >= monthlyCost)
                    {
                        country.treasury -= monthlyCost;
                        project.monthsRemaining--;

                        // Corruption Risk checks logic
                        if (country.corruptionIndex > 40f && UnityEngine.Random.value > 0.8f)
                        {
                            project.monthsRemaining++; 
                            float stolenAmount = project.totalCost * 0.1f;
                            if (country.treasury > stolenAmount) country.treasury -= stolenAmount; 
                            NewsSystem.Instance?.PublishNews($"CORRUPTION: Construction of {project.projectName} delayed. Budget overruns reported.");
                        }

                        if (project.monthsRemaining <= 0)
                        {
                            project.stage = ProjectStage.Active;
                            ApplyProjectBenefits(country, project);
                            NewsSystem.Instance?.PublishNews($"INFRASTRUCTURE: {project.projectName} officially opened in {project.regionName}.");
                            // 10 years of peak active efficiency before maintenance
                            project.monthsRemaining = 120; 
                        }
                    }
                    else
                    {
                        // Deficit prevents progression
                        if (country == Core.GameManager.Instance.playerCountry && UnityEngine.Random.value > 0.7f) // Occasional reminder
                        {
                            NewsSystem.Instance?.PublishNews($"FUNDING SHORTFALL: {project.projectName} construction halted due to insufficient treasury.");
                        }
                    }
                }
                else if (project.stage == ProjectStage.Active)
                {
                    project.monthsRemaining--;
                    if (project.monthsRemaining <= 0)
                    {
                        project.stage = ProjectStage.Aging;
                        NewsSystem.Instance?.PublishNews($"MAINTENANCE: {project.projectName} is aging rapidly. Efficiency dropping.");
                        
                        // Decrement the benefit!
                        RevertProjectBenefits(country, project);
                    }
                }
            }
        }

        private void ApplyProjectBenefits(CountryDataSO country, ActiveProject project)
        {
            // Infrastructure levels
            if      (project.targetID == "Road"     && country.roadLevel     < 10) country.roadLevel++;
            else if (project.targetID == "Rail"     && country.railLevel     < 10) country.railLevel++;
            else if (project.targetID == "Port"     && country.portLevel     < 10) country.portLevel++;
            else if (project.targetID == "Airport"  && country.airportLevel  < 10) country.airportLevel++;
            else if (project.targetID == "Hospital" && country.hospitalLevel < 10) country.hospitalLevel++;
            else if (project.targetID == "Telecom"  && country.telecomLevel  < 10) country.telecomLevel++;
            else if (project.targetID == "Power"    && country.electricityLevel < 100f) country.electricityLevel = Mathf.Min(100f, country.electricityLevel + 5f);
            // Industry levels (shared with old ApplyProjectEffects)
            else if (project.targetID == "indAgriRice")    country.indAgriRice++;
            else if (project.targetID == "indAgriCassava") country.indAgriCassava++;
            else if (project.targetID == "indAgriCocoa")   country.indAgriCocoa++;
            else if (project.targetID == "indMiningLocal") country.indMiningLocal++;
            else if (project.targetID == "indMiningOil")   country.indMiningOil++;
            else if (project.targetID == "indTechIT")      country.indTechIT++;
            else if (project.targetID == "indTechFintech") country.indTechFintech++;
            else if (project.targetID == "PowerGas")       country.powerGasPlants++;
            else if (project.targetID == "PowerHydro")     country.powerHydroPlants++;
            else if (project.targetID == "PowerNuclear")   { country.powerNuclearPlants++; country.hasNuclearPower = true; }
            else if (project.targetID == "PowerSolar")     country.powerSolarPlants++;
            else if (project.targetID == "PowerWind")      country.powerWindPlants++;

            country.gdpGrowthRate += 0.002f;
        }

        private void RevertProjectBenefits(CountryDataSO country, ActiveProject project)
        {
            if (project.targetID == "Road") country.roadLevel = Mathf.Max(0, country.roadLevel - 1);
            else if (project.targetID == "Rail") country.railLevel = Mathf.Max(0, country.railLevel - 1);
            else if (project.targetID == "Port") country.portLevel = Mathf.Max(0, country.portLevel - 1);
            else if (project.targetID == "Airport") country.airportLevel = Mathf.Max(0, country.airportLevel - 1);
            else if (project.targetID == "Power") country.electricityLevel = Mathf.Max(0f, country.electricityLevel - 5f);
            else if (project.targetID == "Hospital") country.hospitalLevel = Mathf.Max(0, country.hospitalLevel - 1);
            else if (project.targetID == "Telecom") country.telecomLevel = Mathf.Max(0, country.telecomLevel - 1);
        }
    }
}

