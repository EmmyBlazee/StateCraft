import sys

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Add cost label to AddNewOrderForm
cost_label_code = """            qtyBox.Add(minusBtn);
            qtyBox.Add(qtyDisplay);
            qtyBox.Add(plusBtn);

            var costLbl = new Label();
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

content = content.replace(
    '            qtyBox.Add(minusBtn);\n            qtyBox.Add(qtyDisplay);\n            qtyBox.Add(plusBtn);',
    cost_label_code
)

# 2. Add UpdateCost() call to button handlers
minus_original = """            minusBtn.clicked += () =>
            {
                int step = id == "RIFLE" ? 100 : id == "APC" || id == "TNK" ? 5 : 1;
                qtyRef[0] = Mathf.Max(step, qtyRef[0] - step);
                qtyDisplay.text = $"{qtyRef[0]:N0}";
            };"""
minus_new = """            minusBtn.clicked += () =>
            {
                int step = id == "RIFLE" ? 100 : id == "APC" || id == "TNK" ? 5 : 1;
                qtyRef[0] = Mathf.Max(step, qtyRef[0] - step);
                qtyDisplay.text = $"{qtyRef[0]:N0}";
                UpdateCost();
            };"""
content = content.replace(minus_original, minus_new)

plus_original = """            plusBtn.clicked += () =>
            {
                int step = id == "RIFLE" ? 100 : id == "APC" || id == "TNK" ? 5 : 1;
                qtyRef[0] = Mathf.Min(10000, qtyRef[0] + step);
                qtyDisplay.text = $"{qtyRef[0]:N0}";
            };"""
plus_new = """            plusBtn.clicked += () =>
            {
                int step = id == "RIFLE" ? 100 : id == "APC" || id == "TNK" ? 5 : 1;
                qtyRef[0] = Mathf.Min(10000, qtyRef[0] + step);
                qtyDisplay.text = $"{qtyRef[0]:N0}";
                UpdateCost();
            };"""
content = content.replace(plus_original, plus_new)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Added dynamic cost label to New Production Order cards.")
