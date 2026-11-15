from PIL import Image

try:
    img = Image.open(r"c:\Users\Tobi\StateCraft\Assets\Resources\EquipmentPhotos\rifle1.png")
    datas = img.getdata()
    r_sum, g_sum, b_sum = 0, 0, 0
    count = 0
    for item in datas:
        if item[3] > 0:
            r_sum += item[0]
            g_sum += item[1]
            b_sum += item[2]
            count += 1
    if count > 0:
        print(f"Average color of opaque pixels: R={r_sum//count}, G={g_sum//count}, B={b_sum//count}")
except Exception as e:
    print(e)
