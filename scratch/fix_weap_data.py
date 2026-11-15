import sys
import re

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update GetImageNameForUnit to catch more keywords
get_image_replacement = """        private string GetImageNameForUnit(string label)
        {
            if (string.IsNullOrEmpty(label)) return null;
            label = label.ToLower();
            if (label.Contains("rifle") || label.Contains("infantry") || label.Contains("ballistic")) return "rifle1";
            if (label.Contains("apc") || label.Contains("personnel carrier") || label.Contains("carrier")) return "Apcs";
            if (label.Contains("tank") || label.Contains("armor")) return "maintank";
            if (label.Contains("artillery") || label.Contains("howitzer")) return "artilery";
            if (label.Contains("fighter") || label.Contains("jet") || label.Contains("jf-17")) return "fighter aircaft";
            if (label.Contains("transport") || label.Contains("cargo")) return "transport aircraft";
            if (label.Contains("helicopter")) return "helicopter1";
            if (label.Contains("frigate")) return "frigate";
            if (label.Contains("destroyer")) return "destroyer";
            if (label.Contains("patrol") || label.Contains("boat") || label.Contains("opv")) return "patrol boat";
            if (label.Contains("sam") || label.Contains("missile") || label.Contains("radar")) return "artilery";
            return null;
        }"""

content = re.sub(r'private string GetImageNameForUnit\(string label\).*?return null;\s*\}', get_image_replacement, content, flags=re.DOTALL)

# 2. Force Clear lists in Ensure methods to reset mock data
content = content.replace("if (country.researchProjects.Count == 0)", "country.researchProjects.Clear();\n            if (country.researchProjects.Count == 0)")
content = content.replace("if (country.productionOrders.Count == 0)", "country.productionOrders.Clear();\n            if (country.productionOrders.Count == 0)")
content = content.replace("if (country.defenseFactories.Count == 0)", "country.defenseFactories.Clear();\n            if (country.defenseFactories.Count == 0)")
content = content.replace("if (country.availableOffers.Count == 0)", "country.availableOffers.Clear();\n            if (country.availableOffers.Count == 0)")

# 3. Replace Drones in EnsureProductionData
content = content.replace(
    'id = "FAC_DRONE", factoryName = "Drone Facility",      produceType = "UAV"',
    'id = "FAC_FIGHTER", factoryName = "Aviation Plant",      produceType = "FTR"'
)
content = content.replace(
    'id = "UAV", unitLabel = "Reconnaissance Drones", facilityName = "Drone Facility",',
    'id = "FTR", unitLabel = "Fighter Jets", facilityName = "Aviation Plant",'
)
content = content.replace(
    '"UAV" or "FTR" or "BOMBER"       => "Drone / Aviation Facility",',
    '"UAV" or "FTR" or "BOMBER"       => "Aviation Facility",'
)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Updated script to clear mock data, fix image mapping, and remove drones.")
