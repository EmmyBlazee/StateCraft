using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class IconSlicer : EditorWindow
{
    [MenuItem("Tools/StateCraft/Slice Military Icons")]
    public static void SliceIcons()
    {
        string sourcePath = @"C:\Users\Tobi\Downloads\military icon.png";
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("Could not find file at " + sourcePath);
            return;
        }

        string destDir = "Assets/Images/MilitaryIcons";
        if (!AssetDatabase.IsValidFolder(destDir))
        {
            AssetDatabase.CreateFolder("Assets/Images", "MilitaryIcons");
        }

        string destPath = destDir + "/military_icon_sheet.png";
        File.Copy(sourcePath, destPath, true);
        AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
        if (tex == null)
        {
            Debug.LogError("Failed to load texture after import");
            return;
        }

Color32[] pixels = tex.GetPixels32();
        int width = tex.width;
        int height = tex.height;
        
        Color32 bg = pixels[0];
        
        bool[,] isForeground = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 c = pixels[y * width + x];
                int diff = Mathf.Abs(c.r - bg.r) + Mathf.Abs(c.g - bg.g) + Mathf.Abs(c.b - bg.b);
                isForeground[x, y] = diff > 40; 
            }
        }
        
        List<RectInt> rects = new List<RectInt>();
        bool[,] visited = new bool[width, height];
        
        int dilateDist = 8;
        bool[,] dilated = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (isForeground[x, y])
                {
                    for (int dy = -dilateDist; dy <= dilateDist; dy++)
                    {
                        for (int dx = -dilateDist; dx <= dilateDist; dx++)
                        {
                            int ny = y + dy;
                            int nx = x + dx;
                            if (ny >= 0 && ny < height && nx >= 0 && nx < width)
                                dilated[nx, ny] = true;
                        }
                    }
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (dilated[x, y] && !visited[x, y])
                {
                    int minX = x, maxX = x, minY = y, maxY = y;
                    Queue<Vector2Int> q = new Queue<Vector2Int>();
                    q.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;
                    
                    while (q.Count > 0)
                    {
                        Vector2Int p = q.Dequeue();
                        if (p.x < minX) minX = p.x;
                        if (p.x > maxX) maxX = p.x;
                        if (p.y < minY) minY = p.y;
                        if (p.y > maxY) maxY = p.y;
                        
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = p.x + dx;
                                int ny = p.y + dy;
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny] && dilated[nx, ny])
                                {
                                    visited[nx, ny] = true;
                                    q.Enqueue(new Vector2Int(nx, ny));
                                }
                            }
                        }
                    }
                    
                    int w = maxX - minX;
                    int h = maxY - minY;
                    if (w > 15 && h > 15 && w < 300 && h < 300)
                    {
                        int pad = 10;
                        minX = Mathf.Max(0, minX - pad);
                        minY = Mathf.Max(0, minY - pad);
                        maxX = Mathf.Min(width - 1, maxX + pad);
                        maxY = Mathf.Min(height - 1, maxY + pad);
                        
                        rects.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
                    }
                }
            }
        }
        
        Debug.Log("Found " + rects.Count + " icons!");
        
        int count = 0;
        foreach (var r in rects)
        {
            Texture2D slice = new Texture2D(r.width, r.height, TextureFormat.RGBA32, false);
            Color32[] slicePixels = new Color32[r.width * r.height];
            for (int y = 0; y < r.height; y++)
            {
                for (int x = 0; x < r.width; x++)
                {
                    Color32 c = pixels[(r.y + y) * width + (r.x + x)];
                    int diff = Mathf.Abs(c.r - bg.r) + Mathf.Abs(c.g - bg.g) + Mathf.Abs(c.b - bg.b);
                    if (diff < 40)
                    {
                        c.a = 0;
                    }
                    slicePixels[y * r.width + x] = c;
                }
            }
            slice.SetPixels32(slicePixels);
            slice.Apply();
            
            byte[] pngData = slice.EncodeToPNG();
            File.WriteAllBytes(destDir + $"/icon_{count:D3}.png", pngData);
            Object.DestroyImmediate(slice);
            count++;
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Extraction complete! " + count + " icons saved to " + destDir);
    }
}
