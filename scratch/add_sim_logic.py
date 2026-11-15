import sys

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\Core\GameManager.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# Add call to ProcessProductionOrders inside ProcessMonth
find_call = """                ProcessRecruitmentPipelines(country);
                ProcessMilitaryProjects(country);"""
replace_call = """                ProcessRecruitmentPipelines(country);
                ProcessMilitaryProjects(country);
                ProcessProductionOrders(country);"""
content = content.replace(find_call, replace_call)

# Add ProcessProductionOrders function definition after ProcessMilitaryProjects
find_def = """        private void ProcessMilitaryProjects(CountryDataSO country)
        {"""
# We need to insert our new function right above ProcessMilitaryProjects
insert_def = """        private void ProcessProductionOrders(CountryDataSO country)
        {
            if (country.productionOrders == null) return;

            for (int i = country.productionOrders.Count - 1; i >= 0; i--)
            {
                var order = country.productionOrders[i];
                order.monthsRemaining--;

                if (country == playerCountry)
                {
                    playerBudget -= order.monthlyCostB * country.exchangeRate;
                    country.treasury = playerBudget;
                }
                else
                {
                    country.treasury -= order.monthlyCostB * country.exchangeRate;
                }

                if (order.monthsRemaining <= 0)
                {
                    switch (order.id)
                    {
                        case "RIFLE": country.unitRifles += order.quantity; break;
                        case "APC": country.unitAPCs += order.quantity; break;
                        case "TNK": country.unitTanks += order.quantity; break;
                        case "ARTILLERY": country.unitArtillery += order.quantity; break;
                        case "UAV": country.unitUAVs += order.quantity; break;
                        case "FTR": country.unitFighters += order.quantity; break;
                        case "BOMBER": country.unitBombers += order.quantity; break;
                        case "HEL": country.unitAttackHelis += order.quantity; break;
                        case "PATROL_BOAT": country.unitPatrolBoats += order.quantity; break;
                        case "FRG": country.unitFrigates += order.quantity; break;
                        case "SUB": country.unitSubmarines += order.quantity; break;
                        case "DESTROYER": country.unitDestroyers += order.quantity; break;
                    }

                    if (country.completedProductions == null) country.completedProductions = new System.Collections.Generic.List<StateCraft.Data.CompletedProductionItem>();
                    string curDate = TurnSystem is TurnSystem ts ? ts.CurrentDate : "Unknown Date";
                    country.completedProductions.Insert(0, new StateCraft.Data.CompletedProductionItem
                    {
                        unitLabel = order.unitLabel,
                        quantity = order.quantity,
                        completedDate = curDate
                    });
                    
                    if (country.completedProductions.Count > 10)
                        country.completedProductions.RemoveAt(country.completedProductions.Count - 1);

                    if (country == playerCountry)
                    {
                        UI.NotificationManager.Instance?.AddNotification($"PRODUCTION COMPLETE: {order.quantity:N0} {order.unitLabel} delivered to Arsenal!", UnityEngine.Color.green);
                    }
                    if (NewsSystem != null)
                    {
                        NewsSystem.PublishNews($"MILITARY: {country.countryName} has completed production of {order.quantity:N0} {order.unitLabel}.");
                    }

                    country.productionOrders.RemoveAt(i);
                }
            }
        }

        private void ProcessMilitaryProjects"""

content = content.replace("        private void ProcessMilitaryProjects", insert_def, 1)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Added ProcessProductionOrders to GameManager.cs")
