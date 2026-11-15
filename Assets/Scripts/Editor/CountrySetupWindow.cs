using UnityEngine;
using UnityEditor;
using StateCraft.Data;
using StateCraft.Map;
using System.IO;

namespace StateCraft.Editor
{
    public class CountrySetupWindow : EditorWindow
    {
        private string countryNamesRaw = "Nigeria, Algeria, Libya, Egypt, Sudan, Chad, Niger, Mali, Mauritania";
        private string assetPath = "Assets/Data/Countries";

        [MenuItem("StateCraft/Setup/Country Setup Helper")]
        public static void ShowWindow()
        {
            GetWindow<CountrySetupWindow>("Country Setup Helper");
        }

        private void OnGUI()
        {
            GUILayout.Label("Country Setup Automation", EditorStyles.boldLabel);
            
            countryNamesRaw = EditorGUILayout.TextArea(countryNamesRaw, GUILayout.Height(100));
            assetPath = EditorGUILayout.TextField("Asset Path", assetPath);

            if (GUILayout.Button("Generate Countries"))
            {
                Generate();
            }
        }

        private void Generate()
        {
            string[] folders = assetPath.Split('/');
            string currentPath = "";
            for (int i = 0; i < folders.Length; i++)
            {
                string folder = folders[i];
                if (i == 0)
                {
                    currentPath = folder;
                    continue;
                }
                
                string parentPath = currentPath;
                currentPath += "/" + folder;
                
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(parentPath, folder);
                }
            }

            string[] names = countryNamesRaw.Split(',');
            int count = 0;

            foreach (string rawName in names)
            {
                string name = rawName.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                string safeName = name.Replace("'", "").Replace(" ", "_");

                string fullPath = $"{assetPath}/{safeName}.asset";
                CountryDataSO data = AssetDatabase.LoadAssetAtPath<CountryDataSO>(fullPath);

                if (data == null)
                {
                    data = CreateInstance<CountryDataSO>();
                    data.countryName = name;
                    data.mapColor = new Color(Random.value, Random.value, Random.value, 1f);
                    AssetDatabase.CreateAsset(data, fullPath);
                }

                if (GameObject.Find(name) == null)
                {
                    GameObject go = new GameObject(name);
                    go.AddComponent<PolygonCollider2D>();
                    CountryController controller = go.AddComponent<CountryController>();
                    controller.countryData = data;

                    if (SceneView.lastActiveSceneView != null)
                    {
                        go.transform.position = SceneView.lastActiveSceneView.pivot;
                    }
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
}
    }
}
