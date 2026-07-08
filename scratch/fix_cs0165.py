import sys
import re

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# We need to extract costLbl and UpdateCost and put them BEFORE minusBtn.
# Right now, minusBtn is around line 1195.

# Let's just remove the costLbl block and put it earlier.
cost_lbl_block = """            var costLbl = new Label();
            costLbl.style.color = cDim;
            costLbl.style.fontSize = 9;
            costLbl.style.marginTop = 6;
            costLbl.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
            controls.Add(costLbl);

            void UpdateCost()
            {
                float costPerMonth = GetUnitCostB(id) * qtyRef[0] / 12f;
                int months         = GetUnitProductionMonths(id, qtyRef[0], country.baseDefenseFactoryCount);
                float totalCost    = costPerMonth * months;
                costLbl.text = $"Est. Cost: <color=#00D2FF>{UiFormat.Money(totalCost * country.exchangeRate, sym)}</color> ({months} months)";
            }
            UpdateCost();"""

content = content.replace(cost_lbl_block, "")

# Find where qtyDisplay is defined, and insert costLbl block after it but before minusBtn
target_str = """            var qtyDisplay = new Label($"{qtyRef[0]:N0}");
            qtyDisplay.style.color   = Color.white;
            qtyDisplay.style.fontSize = 12;
            qtyDisplay.style.width   = 36;
            qtyDisplay.style.unityTextAlign = TextAnchor.MiddleCenter;
            qtyDisplay.style.unityFontStyleAndWeight = FontStyle.Bold;"""

new_target_str = target_str + "\n\n" + cost_lbl_block

content = content.replace(target_str, new_target_str)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Fixed CS0165 error.")
