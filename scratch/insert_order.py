import sys
import re

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# Make new orders appear at the top of the Active Production list so the user doesn't have to scroll down to see them.
content = content.replace("country.productionOrders.Add(order);", "country.productionOrders.Insert(0, order);")

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Changed Add(order) to Insert(0, order)")
