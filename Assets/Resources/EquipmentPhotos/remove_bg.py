import os
from PIL import Image

folder = r"c:\Users\Tobi\StateCraft\Assets\Resources\EquipmentPhotos"
for filename in os.listdir(folder):
    if filename.endswith(".png") and "combination" not in filename.lower():
        filepath = os.path.join(folder, filename)
        try:
            img = Image.open(filepath).convert("RGBA")
            datas = img.getdata()
            
            newData = []
            for item in datas:
                # Check for white or near-white background
                if item[0] > 240 and item[1] > 240 and item[2] > 240:
                    newData.append((255, 255, 255, 0))
                else:
                    newData.append(item)
                    
            img.putdata(newData)
            img.save(filepath, "PNG")
            print(f"Processed {filename}")
        except Exception as e:
            print(f"Failed to process {filename}: {e}")
