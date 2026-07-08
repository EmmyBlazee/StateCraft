import sys
import re

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Bump up the right sidebar fonts even bigger!
content = content.replace('catLbl.style.fontSize = 7;', 'catLbl.style.fontSize = 10;')
content = content.replace('catField.style.fontSize = 8;', 'catField.style.fontSize = 11;')
content = content.replace('qtyCaption.style.fontSize = 7;', 'qtyCaption.style.fontSize = 10;')
content = content.replace('itemLbl.style.fontSize = 10;', 'itemLbl.style.fontSize = 12;')
content = content.replace('itemField.style.fontSize = 8;', 'itemField.style.fontSize = 11;')
content = content.replace('qtyDisplay.style.fontSize = 8;', 'qtyDisplay.style.fontSize = 12;')
content = content.replace('minusBtn.style.fontSize        = 9;', 'minusBtn.style.fontSize        = 12;')
content = content.replace('plusBtn.style.fontSize        = 9;', 'plusBtn.style.fontSize        = 12;')
content = content.replace('startBtn.style.fontSize = 10;', 'startBtn.style.fontSize = 12;') # I already bumped it from 8 to 10 maybe?
content = content.replace('startBtn.style.fontSize = 8;', 'startBtn.style.fontSize = 12;')   # Just in case it's still 8

# 2. Add images to the dark squares in the New Production Order panel
img_loading = """            thumb.style.marginRight    = 10;

            string imgName = GetImageNameForUnit(item);
            if (!string.IsNullOrEmpty(imgName))
            {
                var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imgName);
                if (tex != null)
                {
                    thumb.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
                    thumb.style.unityBackgroundScaleMode = UnityEngine.ScaleMode.ScaleToFit;
                    thumb.style.backgroundColor = UnityEngine.Color.clear;
                }
            }

            row.Add(thumb);"""

content = content.replace('            thumb.style.marginRight    = 10;\n            row.Add(thumb);', img_loading)

# 3. Add an empty quantity check or error logging just to be safe. But the logic seems sound.
# The user said it doesn't add to the queue. Wait... what if gm.playerBudget is less than 0.5f of the total?
# We show an alert! But does the Alert system actually work? If it doesn't, the user might think "nothing happens".
# NotificationManager.Instance?.ShowAlert(...) - maybe NotificationManager is not in the scene or there's an error.
# Let's add a Debug.Log for both success and failure so they can see in the console.

btn_click_original = """                if (gm.playerBudget < totalCost * 0.5f)
                {
                    NotificationManager.Instance?.ShowAlert("INSUFFICIENT FUNDS",
                        $"Need at least {UiFormat.Money(totalCost * 0.5f * country.exchangeRate, sym)} to start production.", true);
                    return;
                }"""
btn_click_new = """                if (gm.playerBudget < totalCost * 0.5f)
                {
                    UnityEngine.Debug.Log($"[Production] Insufficient funds! Have: {gm.playerBudget}, Need: {totalCost * 0.5f}");
                    NotificationManager.Instance?.ShowAlert("INSUFFICIENT FUNDS",
                        $"Need at least {UiFormat.Money(totalCost * 0.5f * country.exchangeRate, sym)} to start production.", true);
                    return;
                }
                UnityEngine.Debug.Log($"[Production] Success! Adding {qtyRef[0]} {item} to queue. Cost: {totalCost}");"""

content = content.replace(btn_click_original, btn_click_new)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Updated sidebar fonts, added images, and added debug logs for queue adding.")
