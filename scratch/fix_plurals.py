import sys

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# Fix the pluralization issue in AddProductionFacilityCard
# order.unitLabel.Split(' ')[^1] + "s" causes "Rifless" and "Tankss"

old_qtyLbl = """            var qtyLbl = new Label($"{order.quantity:N0} {order.unitLabel.Split(' ')[^1]}s");"""
new_qtyLbl = """            string lastWord = order.unitLabel.Split(' ')[^1];
            string unitPlural = lastWord.EndsWith("s") || lastWord.EndsWith("y") ? (lastWord.EndsWith("y") && lastWord != "Artillery" ? lastWord.Substring(0, lastWord.Length - 1) + "ies" : lastWord) : lastWord + "s";
            if (lastWord == "Artillery") unitPlural = "Artillery";
            var qtyLbl = new Label($"{order.quantity:N0} {unitPlural}");"""

content = content.replace(old_qtyLbl, new_qtyLbl)


old_monthlyOut = """            AddMiniStat(statsRow, "MONTHLY OUTPUT", $"{order.monthlyOutput:F0} {order.unitLabel.Split(' ')[^1]}s");"""
new_monthlyOut = """            AddMiniStat(statsRow, "MONTHLY OUTPUT", $"{order.monthlyOutput:F0} {unitPlural}");"""

content = content.replace(old_monthlyOut, new_monthlyOut)


with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Fixed unit pluralization typo (e.g. Rifless -> Rifles).")
