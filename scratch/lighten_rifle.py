from PIL import Image

try:
    filepath = r"c:\Users\Tobi\StateCraft\Assets\Resources\EquipmentPhotos\rifle1.png"
    img = Image.open(filepath).convert("RGBA")
    datas = img.getdata()
    
    newData = []
    for item in datas:
        if item[3] > 0:
            # Lighten the pixel by increasing brightness
            r = min(255, int(item[0] * 2.5 + 50))
            g = min(255, int(item[1] * 2.5 + 50))
            b = min(255, int(item[2] * 2.5 + 50))
            newData.append((r, g, b, item[3]))
        else:
            newData.append(item)
            
    img.putdata(newData)
    img.save(filepath, "PNG")
    print("Rifle image lightened successfully.")
except Exception as e:
    print(e)
