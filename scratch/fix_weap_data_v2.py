import sys

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Revert the accidental Clear() calls
content = content.replace(
    "country.productionOrders.Clear();\n            if (country.productionOrders.Count == 0)",
    "if (country.productionOrders.Count == 0)"
)
content = content.replace(
    "country.researchProjects.Clear();\n            if (country.researchProjects.Count == 0)",
    "if (country.researchProjects.Count == 0)"
)
content = content.replace(
    "country.availableOffers.Clear();\n            if (country.availableOffers.Count == 0)",
    "if (country.availableOffers.Count == 0)"
)
content = content.replace(
    "country.defenseFactories.Clear();\n            if (country.defenseFactories.Count == 0)",
    "if (country.defenseFactories.Count == 0)"
)

# 2. Add cleanup logic at the start of EnsureProductionData
cleanup_production = """        private void EnsureProductionData(CountryDataSO country)
        {
            if (country.defenseFactories != null) country.defenseFactories.RemoveAll(f => f.produceType == "UAV");
            if (country.productionOrders != null) country.productionOrders.RemoveAll(o => o.id == "UAV");
            if (country.completedProductions != null) country.completedProductions.RemoveAll(c => c.unitLabel == "Military Trucks" || c.unitLabel == "Ammunition");
"""
content = content.replace(
    "        private void EnsureProductionData(CountryDataSO country)\n        {",
    cleanup_production
)

# 3. Add cleanup logic at the start of EnsureResearchData
cleanup_research = """        private void EnsureResearchData(CountryDataSO country)
        {
            if (country.researchProjects != null) country.researchProjects.RemoveAll(r => r.id == "RES_DRONE");
"""
content = content.replace(
    "        private void EnsureResearchData(CountryDataSO country)\n        {",
    cleanup_research
)

# 4. Comment out Military Trucks and Ammunition in EnsureProductionData
content = content.replace(
    'country.completedProductions.Add(new CompletedProductionItem { unitLabel = "Military Trucks",  quantity = 20,   completedDate = "28 Dec 2025" });',
    '// country.completedProductions.Add(new CompletedProductionItem { unitLabel = "Military Trucks",  quantity = 20,   completedDate = "28 Dec 2025" });'
)
content = content.replace(
    'country.completedProductions.Add(new CompletedProductionItem { unitLabel = "Ammunition",       quantity = 5000, completedDate = "25 Dec 2025" });',
    '// country.completedProductions.Add(new CompletedProductionItem { unitLabel = "Ammunition",       quantity = 5000, completedDate = "25 Dec 2025" });'
)


with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Restored Active Production array and removed non-existent units.")
