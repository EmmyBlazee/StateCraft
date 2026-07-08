import sys

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

content = content.replace("UnityEngine.UIElements.ScaleMode.ScaleToFit", "UnityEngine.ScaleMode.ScaleToFit")

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Fixed ScaleMode namespace.")
