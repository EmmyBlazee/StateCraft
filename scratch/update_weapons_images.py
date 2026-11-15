import sys

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Replace Drone in EnsureResearchData
content = content.replace(
    'id = "RES_DRONE", projectName = "Advanced Drone Swarm Tactics", category = "Aviation Tech",',
    'id = "RES_FIGHTER", projectName = "Next-Gen Fighter Aircraft", category = "Aviation Tech",'
)

# 2. Add GetImageNameForUnit before PopulateProduction
if "private string GetImageNameForUnit" not in content:
    get_image_method = """
        private string GetImageNameForUnit(string label)
        {
            if (string.IsNullOrEmpty(label)) return null;
            label = label.ToLower();
            if (label.Contains("rifle")) return "rifle1";
            if (label.Contains("apc")) return "Apcs";
            if (label.Contains("tank")) return "maintank";
            if (label.Contains("artillery") || label.Contains("howitzer")) return "artilery";
            if (label.Contains("fighter") || label.Contains("jet") || label.Contains("jf-17")) return "fighter aircaft";
            if (label.Contains("transport")) return "transport aircraft";
            if (label.Contains("helicopter")) return "helicopter1";
            if (label.Contains("frigate")) return "frigate";
            if (label.Contains("destroyer")) return "destroyer";
            if (label.Contains("patrol") || label.Contains("boat")) return "patrol boat";
            if (label.Contains("sam") || label.Contains("missile")) return "artilery"; // fallback
            return null;
        }

        private void PopulateProduction"""
    content = content.replace("        private void PopulateProduction", get_image_method)

# 3. Modify AddProductionFacilityCard
content = content.replace(
    'string sym, Color cGreen, Color cCyan, Color cYellow, Color cOrange, Color cRed, Color cDim, Color cPanel, Color cBorder)',
    'string sym, Color cGreen, Color cCyan, Color cYellow, Color cOrange, Color cRed, Color cDim, Color cPanel, Color cBorder, string imageName = null)'
)

imgBox_replacement = """
            imgBox.style.borderBottomLeftRadius = imgBox.style.borderBottomRightRadius = 3;
            if (!string.IsNullOrEmpty(imageName))
            {
                var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imageName);
                if (tex != null)
                {
                    imgBox.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
                    imgBox.style.unityBackgroundScaleMode = UnityEngine.UIElements.ScaleMode.ScaleToFit;
                    imgBox.style.backgroundColor = UnityEngine.Color.clear;
                }
            }
            bodyRow.Add(imgBox);"""

content = content.replace(
    '            imgBox.style.borderBottomLeftRadius = imgBox.style.borderBottomRightRadius = 3;\n            bodyRow.Add(imgBox);',
    imgBox_replacement
)

content = content.replace(
    'AddProductionFacilityCard(facilityGrid, order, country, sym, cGreen, cCyan, cYellow, cOrange, cRed, cDim, cPanel, cBorder);',
    'AddProductionFacilityCard(facilityGrid, order, country, sym, cGreen, cCyan, cYellow, cOrange, cRed, cDim, cPanel, cBorder, GetImageNameForUnit(order.unitLabel));'
)

# 4. Modify AddOffer
content = content.replace(
    'void AddOffer(string title, string sub, string[] tags, string sup, string ctry, string unitC, string totC, string qty, string deliv, string flag)',
    'void AddOffer(string title, string sub, string[] tags, string sup, string ctry, string unitC, string totC, string qty, string deliv, string flag, string imageName = null)'
)

thumb_offer_replacement = """
                thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 4;
                thumb.style.marginRight = 12;
                if (!string.IsNullOrEmpty(imageName))
                {
                    var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imageName);
                    if (tex != null)
                    {
                        thumb.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
                        thumb.style.unityBackgroundScaleMode = UnityEngine.UIElements.ScaleMode.ScaleToFit;
                        thumb.style.backgroundColor = UnityEngine.Color.clear;
                    }
                }"""

content = content.replace(
    '                thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 4;\n                thumb.style.marginRight = 12;',
    thumb_offer_replacement
)

content = content.replace(
    'AddOffer(offer.title, offer.subtitle, offer.tags, offer.supplier, offer.country, offer.unitCost, offer.totalCost, offer.quantity, offer.deliveryTime, offer.flagEmoji);',
    'AddOffer(offer.title, offer.subtitle, offer.tags, offer.supplier, offer.country, offer.unitCost, offer.totalCost, offer.quantity, offer.deliveryTime, offer.flagEmoji, GetImageNameForUnit(offer.title));'
)

# 5. Modify AddResCard
content = content.replace(
    'void AddResCard(string icon, string title, string sub, string level, string desc, string[] benefits, string days, string cost, Color cColor, bool isStarred)',
    'void AddResCard(string icon, string title, string sub, string level, string desc, string[] benefits, string days, string cost, Color cColor, bool isStarred, string imageName = null)'
)

thumb_res_replacement = """
                var thumb = new VisualElement(); thumb.style.height = 70; thumb.style.backgroundColor = new Color(0,0,0,0.5f); thumb.style.marginBottom = 8; thumb.style.borderTopLeftRadius = thumb.style.borderTopRightRadius = thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 4;
                if (!string.IsNullOrEmpty(imageName))
                {
                    var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imageName);
                    if (tex != null)
                    {
                        thumb.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
                        thumb.style.unityBackgroundScaleMode = UnityEngine.UIElements.ScaleMode.ScaleToFit;
                        thumb.style.backgroundColor = UnityEngine.Color.clear;
                    }
                }
                c.Add(thumb);"""

content = content.replace(
    '                var thumb = new VisualElement(); thumb.style.height = 70; thumb.style.backgroundColor = new Color(0,0,0,0.5f); thumb.style.marginBottom = 8; thumb.style.borderTopLeftRadius = thumb.style.borderTopRightRadius = thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 4;\n                c.Add(thumb);',
    thumb_res_replacement
)

content = content.replace(
    'project.monthsRemaining > 0 ? $"{project.monthsRemaining} Months" : "Completed",\n                        "₦4.5B", cc, false);',
    'project.monthsRemaining > 0 ? $"{project.monthsRemaining} Months" : "Completed",\n                        "₦4.5B", cc, false, GetImageNameForUnit(project.projectName));'
)


with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Weapon script updated.")
