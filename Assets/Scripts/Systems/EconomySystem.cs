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

            country.militaryBudgetUSD = country.CalculateMilitaryUpkeep();

            country.income = CalculateProjectedIncome(country);
            
            float subsidyCost = (country.gdp / 12f) * (country.subsidyLevel / 100f);
            
            float uraniumImportFee = country.isImportingUranium ? 0.05f : 0f;
            
            country.spending = subsidyCost + country.militaryBudgetUSD + uraniumImportFee; 

            foreach (var policy in country.activePolicies)
            {
                country.income -= policy.recurringCost;
                if (policy.type == PolicyType.Economy)
                {
                }
            }

            float growthRate = country.gdpGrowthRate * (country.stability / 100f);
            
            if (country.centralBankInterestRate > 6f)
            {
                growthRate -= (country.centralBankInterestRate - 6f) * 0.001f;
            }
            else if (country.centralBankInterestRate < 4f)
            {
                growthRate += (4f - country.centralBankInterestRate) * 0.001f;
            }

            if (country.importTariffRate > 15f)
            {
                growthRate -= (country.importTariffRate - 15f) * 0.0005f;
            }
            
            growthRate += (country.airportLevel * 0.002f) + (country.roadLevel * 0.001f) + (country.educationLevel * 0.0015f);
            
            growthRate += (country.healthcareLevel * 0.0005f);
            
            if (country.electricityLevel < 40f) growthRate -= 0.02f;
            else if (country.electricityLevel > 80f) growthRate += 0.01f;

if (country.taxRate > 25f)
            {
                float penalty = (country.taxRate - 25f) * 0.01f;
                country.foreignInvestment = Mathf.Max(0, country.foreignInvestment - (penalty * 1e9f));
                growthRate -= penalty * 0.05f; 
            }
            else if (country.taxRate < 15f)
            {
                country.foreignInvestment += (15f - country.taxRate) * 0.005f * 1e9f;
                growthRate += (15f - country.taxRate) * 0.002f;
            }

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

            if (country.gdp > 0 && !float.IsNaN(country.gdp))
            {
                float debtRatio = country.debt / country.gdp;
                if (debtRatio > 0.8f) country.inflationRate += 0.002f;
                else if (debtRatio < 0.2f) country.inflationRate -= 0.001f;
                
                if (country.treasury < 0) country.inflationRate += 0.005f;

                if (country.centralBankInterestRate > 6f)
                {
                    country.inflationRate -= (country.centralBankInterestRate - 6f) * 0.001f;
                }
                else if (country.centralBankInterestRate < 4f)
                {
                    country.inflationRate += (4f - country.centralBankInterestRate) * 0.0005f;
                }

                if (country.importTariffRate > 15f)
                {
                    country.inflationRate += (country.importTariffRate - 15f) * 0.0002f;
                }
            }

            country.inflationRate = Mathf.Clamp(country.inflationRate, 0f, 2.0f);

long oldPopulation = country.population;
            float popGrowthModifier = country.populationGrowthRate / 12f;
            
            if (country.taxRate > 25f) popGrowthModifier -= (country.taxRate - 25f) * 0.0001f;
            if (country.stability < 30f) popGrowthModifier -= 0.001f;
            if (country.reserveFood < 20f || country.foodSecurity < 50f) popGrowthModifier -= 0.003f;
            if (country.healthcareLevel > 5) popGrowthModifier += 0.0005f;
            
            country.population += (long)(country.population * popGrowthModifier);
            country.populationChangeLastTurn = country.population - oldPopulation;

            ProcessProduction(country);
            ProcessTradeAgreements(country);
            ProcessDebtService(country);

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

            UpdateExchangeRate(country);
            
            ProcessConstructionProjects(country);
            
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
                    warCostCalc = country.gdp * 0.025f;

                float net = country.income - country.spending - debtInt - corruptLeak - opsCost;

                var entry = new EconomyLedgerEntry
                {
                    month           = gm.TurnSystem?.CurrentTurn ?? 0,
                    openingTreasury = country.treasury - net,
                    closingTreasury = country.treasury,
                    netChange       = net,
                    taxRevenue      = country.income * 0.6f,
                    resourceIncome  = country.income * 0.3f,
                    tariffRevenue   = country.income * 0.1f,
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

                if (deal.isSeller)
                {
                    float amt = deal.resourceQuantity;
                    bool hasEnough = false;
                    switch (deal.resource)
                    {
                        case TradeResourceType.Oil: hasEnough = country.invOilBarrels >= amt; break;
                        case TradeResourceType.Gas: 
                            int idxGas = country.resources.FindIndex(r => r.type == MineralType.NaturalGas);
                            hasEnough = idxGas >= 0 && (country.resources[idxGas].abundance * 100000f) >= amt; 
                            break;
                        case TradeResourceType.Uranium: 
                            int idxUra = country.resources.FindIndex(r => r.type == MineralType.Uranium);
                            hasEnough = idxUra >= 0 && (country.resources[idxUra].abundance * 500f) >= amt; 
                            break;
                        case TradeResourceType.Minerals: hasEnough = country.invMineralsTonnes >= amt; break;
                        case TradeResourceType.Food: hasEnough = (country.invRiceTonnes + country.invCassavaTonnes + country.invCocoaTonnes) >= amt; break;
                    }

                    if (!hasEnough)
                    {
                        deal.status = TradeAgreementStatus.Expired;
                        country.globalPrestige = Mathf.Max(0, country.globalPrestige - 5f);
                        NewsSystem.Instance?.PublishNews($"BREACH OF CONTRACT: {country.countryName.ToUpper()} FAILS TO DELIVER {deal.resource.ToString().ToUpper()} TO {deal.partnerCountry.ToUpper()}!");
                        
                        country.activeCrises.Add(new GlobalCrisis { 
                            crisisName = $"Diplomatic Ultimatum: Broken Contract", 
                            affectedRegion = deal.partnerCountry, 
                            severity = "Critical", 
                            internationalPressure = 95f 
                        });

                        NotificationManager.Instance?.AddNotification($"ULTIMATUM RECEIVED: Failure to deliver {deal.resource} to {deal.partnerCountry}.", Color.red);
                        continue;
                    }

                    switch (deal.resource)
                    {
                        case TradeResourceType.Oil: country.invOilBarrels = Mathf.Max(0, country.invOilBarrels - amt); break;
                        case TradeResourceType.Minerals: country.invMineralsTonnes = Mathf.Max(0, country.invMineralsTonnes - amt); break;
                        case TradeResourceType.Food:
                            if (country.invRiceTonnes >= amt) country.invRiceTonnes -= amt;
                            else if (country.invCassavaTonnes >= amt) country.invCassavaTonnes -= amt;
                            else country.invCocoaTonnes = Mathf.Max(0, country.invCocoaTonnes - amt);
                            break;
                    }
                }
                else
                {
                    float amt = deal.resourceQuantity;
                    switch (deal.resource)
                    {
                        case TradeResourceType.Oil: country.invOilBarrels += amt; break;
                        case TradeResourceType.Minerals: country.invMineralsTonnes += amt; break;
                        case TradeResourceType.Food: country.invRiceTonnes += amt; break;
                    }
                }
                
                if (deal.isBulkTransfer)
                {
                    deal.status = TradeAgreementStatus.Expired;
                    if (deal.isSeller)
                        country.treasury += (deal.monthlyPaymentUSD * country.exchangeRate) / 1e9f;
                    else
                        country.treasury -= (deal.monthlyPaymentUSD * country.exchangeRate) / 1e9f;
                }
                else
                {
                    if (deal.isSeller)
                        country.treasury += (deal.monthlyPaymentUSD * country.exchangeRate) / 1e9f;
                    else
                        country.treasury -= (deal.monthlyPaymentUSD * country.exchangeRate) / 1e9f;
                }
            }

            if (country == GameManager.Instance?.playerCountry)
            {
                GameManager.Instance.playerBudget = country.treasury;
            }
        }

        private void UpdateExchangeRate(CountryDataSO country)
        {
            float devaluationPressure = (country.inflationRate * 0.3f) + (1f - (country.stability / 100f)) * 0.05f;
            float improvementPressure = 0.01f;

            float interestRateAppreciation = (country.centralBankInterestRate - 5f) * 0.005f;
            improvementPressure += interestRateAppreciation;

            float netChange = 1f + (devaluationPressure - improvementPressure);
            netChange = Mathf.Clamp(netChange, 0.90f, 1.15f);
            country.exchangeRate *= netChange;

            float maxRate = country.baseExchangeRate > 0f ? country.baseExchangeRate * 10f : country.exchangeRate;
            country.exchangeRate = Mathf.Clamp(country.exchangeRate, country.baseExchangeRate * 0.5f, maxRate);

            if (netChange > 1.10f)
            {
                NewsSystem.Instance?.PublishNews($"CURRENCY CRASH: THE {country.currencyName.ToUpper()} COLLAPSES BY {(netChange - 1f):P} IN MASSIVE SELL-OFF.");
            }
        }

        private void ProcessProduction(CountryDataSO country)
        {
            country.invRiceTonnes += country.indAgriRice * 50000f;
            country.invCassavaTonnes += country.indAgriCassava * 80000f;
            country.invCocoaTonnes += country.indAgriCocoa * 20000f;

            country.invMineralsTonnes += country.indMiningLocal * 100000f;
            country.invOilBarrels += country.indMiningOil * 2000000f;
            
            float techOutput = (country.indTechIT + country.indTechFintech) * 5000f;
            country.invTechGoods += techOutput * (country.techIndustryOutput / 100f);

float foodNeeded = country.population * 0.001f; 
            
            country.invRiceTonnes = Mathf.Max(0, country.invRiceTonnes - (foodNeeded * 0.4f));
            country.invCassavaTonnes = Mathf.Max(0, country.invCassavaTonnes - (foodNeeded * 0.6f));
        }

        public float CalculateProjectedIncome(CountryDataSO country)
        {
            float monthlyGDP = country.gdp / 12f;

            float regionalDisruption = 1f;
            if (country.regions != null)
            {
                foreach (var region in country.regions)
                {
                    if (region.specialization == RegionSpecialization.ResourceRich && region.insurgencyLevel > 30f)
                    {
                        float loss = (region.insurgencyLevel - 30f) * 0.005f;  
                        regionalDisruption -= loss;
                    }
                }
            }
            regionalDisruption = Mathf.Max(0.1f, regionalDisruption);
            monthlyGDP *= regionalDisruption;

            float taxRevenue = monthlyGDP * (country.taxRate / 100f);
            
            float tariffRevenue = monthlyGDP * 0.015f * (country.importTariffRate / 100f);
            
            float resourceIncome = 0;
            foreach (var res in country.resources)
            {
                float infraBonus = 1f + (country.railLevel * 0.05f) + (country.portLevel * 0.03f) + (country.electricityLevel / 200f);
                resourceIncome += (res.abundance / 100f) * (monthlyGDP * 0.05f) * infraBonus;
            }
            
            float reachBonus = 1f + (country.roadLevel * 0.02f);
            taxRevenue *= (0.3f + (country.stability / 142f)) * reachBonus; 
            
            return taxRevenue + resourceIncome + tariffRevenue;
        }

        private void ProcessDebtService(CountryDataSO country)
        {
            if (country.debt <= 0f) return;

            float principalRatio = country.debtServiceRatio / 100f;
            float principalRepaymentUSD = country.debt * 0.01f * principalRatio;

            if (principalRatio <= 0f)
            {
                country.stability = Mathf.Max(0f, country.stability - 0.5f);
                country.creditRating = DegradeRating(country.creditRating);
                bool globalOnly = (country != GameManager.Instance?.playerCountry);
                NewsSystem.Instance?.PublishNews($"MARKETS: {country.countryName} skips debt servicing — credit outlook worsens.", globalOnly);
                return;
            }

            float repayLocalB = (principalRepaymentUSD * country.exchangeRate);

            if (country.treasury >= repayLocalB)
            {
                country.treasury -= repayLocalB;

                float totalDebt = Mathf.Max(country.debt, 1f);
                country.domesticDebt = Mathf.Max(0f, country.domesticDebt - principalRepaymentUSD * (country.domesticDebt / totalDebt));
                country.foreignDebt  = Mathf.Max(0f, country.foreignDebt  - principalRepaymentUSD * (country.foreignDebt  / totalDebt));
                country.debt = country.domesticDebt + country.foreignDebt;

                if (principalRatio >= 1.2f)
                    country.creditRating = ImproveRating(country.creditRating);
            }
            else
            {
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
                    
                    if (country.treasury >= monthlyCost)
                    {
                        country.treasury -= monthlyCost;
                        project.monthsRemaining--;

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
                            project.monthsRemaining = 120; 
                        }
                    }
                    else
                    {
                        if (country == Core.GameManager.Instance.playerCountry && UnityEngine.Random.value > 0.7f)
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
                        
                        RevertProjectBenefits(country, project);
                    }
                }
            }
        }

        private void ApplyProjectBenefits(CountryDataSO country, ActiveProject project)
        {
            if      (project.targetID == "Road"     && country.roadLevel     < 10) country.roadLevel++;
            else if (project.targetID == "Rail"     && country.railLevel     < 10) country.railLevel++;
            else if (project.targetID == "Port"     && country.portLevel     < 10) country.portLevel++;
            else if (project.targetID == "Airport"  && country.airportLevel  < 10) country.airportLevel++;
            else if (project.targetID == "Hospital" && country.hospitalLevel < 10) country.hospitalLevel++;
            else if (project.targetID == "Telecom"  && country.telecomLevel  < 10) country.telecomLevel++;
            else if (project.targetID == "Power"    && country.electricityLevel < 100f) country.electricityLevel = Mathf.Min(100f, country.electricityLevel + 5f);
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
