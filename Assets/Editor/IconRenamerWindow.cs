using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class IconRenamerWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<Texture2D> icons = new List<Texture2D>();
    private List<string> newNames = new List<string>();
    private List<string> assetPaths = new List<string>();

    [MenuItem("Tools/StateCraft/Icon Renamer")]
    public static void ShowWindow()
    {
        GetWindow<IconRenamerWindow>("Icon Renamer");
    }

    private void OnEnable()
    {
        LoadIcons();
    }

    private void LoadIcons()
    {
        icons.Clear();
        newNames.Clear();
        assetPaths.Clear();

        string folderPath = "Assets/Images/MilitaryIcons";
        if (!AssetDatabase.IsValidFolder(folderPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                icons.Add(tex);
                newNames.Add(tex.name);
                assetPaths.Add(path);
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Rename Extracted Icons", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Reload Icons"))
        {
            LoadIcons();
        }

        if (GUILayout.Button("Apply All Renames", GUILayout.Height(30)))
        {
            ApplyRenames();
        }

        GUILayout.Space(10);
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < icons.Count; i++)
        {
            GUILayout.BeginHorizontal("box");
            
            // Draw the icon
            GUILayout.Box(icons[i], GUILayout.Width(64), GUILayout.Height(64));
            
            GUILayout.BeginVertical();
            GUILayout.Space(20);
            GUILayout.Label("Current: " + Path.GetFileNameWithoutExtension(assetPaths[i]));
            newNames[i] = GUILayout.TextField(newNames[i], GUILayout.Width(200));
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void ApplyRenames()
    {
        int renamedCount = 0;
        for (int i = 0; i < icons.Count; i++)
        {
            string oldName = Path.GetFileNameWithoutExtension(assetPaths[i]);
            string safeNewName = newNames[i].Replace(" ", "_").ToLower();

            if (oldName != safeNewName && !string.IsNullOrWhiteSpace(safeNewName))
            {
                string result = AssetDatabase.RenameAsset(assetPaths[i], safeNewName);
                if (string.IsNullOrEmpty(result)) // Success
                {
                    renamedCount++;
                }
                else
                {
                    Debug.LogWarning($"Failed to rename {oldName} to {safeNewName}: {result}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully renamed {renamedCount} icons.");
        LoadIcons();
    }
}
