from PIL import Image

try:
    img = Image.open(r"c:\Users\Tobi\StateCraft\Assets\Resources\EquipmentPhotos\rifle1.png")
    datas = img.getdata()
    transparent = 0
    opaque = 0
    for item in datas:
        if item[3] == 0:
            transparent += 1
        else:
            opaque += 1
    print(f"Rifle Image Stats: {img.size}, Opaque pixels: {opaque}, Transparent pixels: {transparent}")
except Exception as e:
    print(e)
