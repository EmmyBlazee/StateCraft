from PIL import Image

def process_rifle(img_path):
    img = Image.open(img_path).convert("RGBA")
    datas = img.getdata()
    
    newData = []
    for item in datas:
        # Average brightness
        brightness = (item[0] + item[1] + item[2]) / 3.0
        
        # Soft threshold to remove white background
        if brightness > 245:
            a = 0
        elif brightness > 200:
            # Smooth fade from opaque to transparent
            a = int(255 * (245 - brightness) / 45.0)
            # Cap a to original alpha
            a = min(a, item[3])
        else:
            a = item[3]
            
        if a > 0:
            # Lighten the actual rifle so it's visible on a dark background
            # but keep some of the original shading
            r = min(255, int(item[0] * 1.5 + 80))
            g = min(255, int(item[1] * 1.5 + 80))
            b = min(255, int(item[2] * 1.5 + 80))
            newData.append((r, g, b, a))
        else:
            newData.append((255, 255, 255, 0))
            
    img.putdata(newData)
    img.save(img_path, "PNG")

filepath = r"c:\Users\Tobi\StateCraft\Assets\Resources\EquipmentPhotos\rifle1.png"
try:
    process_rifle(filepath)
    print("Rifle image processed perfectly.")
except Exception as e:
    print(e)
