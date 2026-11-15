import sys
import re

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# Hide Research Tab in InitializeWeapons
content = content.replace(
    '_wTabResearch    = DeepQ<Button>(root, "WTabResearch");',
    '_wTabResearch    = DeepQ<Button>(root, "WTabResearch");\n            if (_wTabResearch != null) _wTabResearch.style.display = UnityEngine.UIElements.DisplayStyle.None;'
)

# Hide Research Tab in EnsureWeaponsWired
content = content.replace(
    '_wTabResearch    = DeepQ<Button>(searchRoot, "WTabResearch");',
    '_wTabResearch    = DeepQ<Button>(searchRoot, "WTabResearch");\n                if (_wTabResearch != null) _wTabResearch.style.display = UnityEngine.UIElements.DisplayStyle.None;'
)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Research tab hidden.")
