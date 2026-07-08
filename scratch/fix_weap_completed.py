import sys
import re

filepath = r"c:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController_Weapons.cs"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update order forms from Recon Drone to Fighter Jet
content = content.replace(
    '("Aviation",       "Recon Drone",       "UAV",     25),',
    '("Aviation",       "Fighter Jets",      "FTR",     5),'
)

# 2. Add images to the "Recently Completed" cards
completed_img_replacement = """                        var img = new VisualElement();
                        img.style.width  = 50;
                        img.style.height = 32;
                        img.style.backgroundColor = new Color(0, 0, 0, 0.3f);
                        img.style.borderTopLeftRadius = img.style.borderTopRightRadius =
                        img.style.borderBottomLeftRadius = img.style.borderBottomRightRadius = 3;
                        img.style.marginBottom = 5;
                        img.style.alignSelf    = Align.Center;

                        string imageName = GetImageNameForUnit(item.unitLabel);
                        if (!string.IsNullOrEmpty(imageName))
                        {
                            var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imageName);
                            if (tex != null)
                            {
                                img.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
                                img.style.unityBackgroundScaleMode = UnityEngine.ScaleMode.ScaleToFit;
                                img.style.backgroundColor = UnityEngine.Color.clear;
                            }
                        }

                        card.Add(img);"""

content = re.sub(
    r'var img = new VisualElement\(\);\s*img\.style\.width\s*=\s*50;.*?img\.style\.alignSelf\s*=\s*Align\.Center;\s*card\.Add\(img\);',
    completed_img_replacement,
    content,
    flags=re.DOTALL
)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Updated script to replace order form drones and add images to recently completed.")
