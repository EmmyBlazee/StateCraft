#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using StateCraft.UI;

namespace StateCraft.Editor
{
    public class TutorialSetupWindow : EditorWindow
    {
        [MenuItem("StateCraft/Setup & Verify Tutorial System", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<TutorialSetupWindow>("Tutorial Setup");
            window.minSize = new Vector2(480, 420);
            window.Show();
        }

        [MenuItem("StateCraft/Quick Setup Tutorial in Current Scene", false, 11)]
        public static void QuickSetupInCurrentScene()
        {
            SetupTutorialInActiveScene();
        }

        private void OnGUI()
        {
            GUILayout.Space(15);
            EditorGUILayout.LabelField("STATECRAFT DEDICATED TUTORIAL UI DOCUMENT SETUP", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Automated top-level UIDocument setup for Guided Tour & Executive Academy", EditorStyles.miniLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool creates a dedicated [TutorialUIDocument] GameObject with sortingOrder = 999999, " +
                "ensuring the Guided Tour overlay sits above all dashboards and windows under all conditions.",
                MessageType.Info);

            GUILayout.Space(15);

            if (GUILayout.Button("⚡ CREATE & CONFIGURE TUTORIAL UI DOCUMENT", GUILayout.Height(45)))
            {
                SetupTutorialInActiveScene();
            }

            GUILayout.Space(15);
            EditorGUILayout.LabelField("Tutorial Status Check:", EditorStyles.boldLabel);

            var docGO = GameObject.Find("[TutorialUIDocument]");
            if (docGO != null && docGO.GetComponent<UIDocument>() != null)
            {
                EditorGUILayout.LabelField("✔ Dedicated [TutorialUIDocument] GameObject active in scene.", EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.LabelField("⚠ [TutorialUIDocument] missing in scene. Click button above to generate.", EditorStyles.wordWrappedLabel);
            }
        }

        public static void SetupTutorialInActiveScene()
        {
            Debug.Log("[TutorialSetupWindow] Setting up dedicated top-level [TutorialUIDocument]...");

            var tourGO = GameObject.Find("[TutorialUIDocument]");
            if (tourGO == null)
            {
                tourGO = new GameObject("[TutorialUIDocument]");
                Undo.RegisterCreatedObjectUndo(tourGO, "Create [TutorialUIDocument]");
                Debug.Log("[TutorialSetupWindow] Created GameObject '[TutorialUIDocument]'.");
            }

            var uiDoc = tourGO.GetComponent<UIDocument>();
            if (uiDoc == null)
            {
                uiDoc = tourGO.AddComponent<UIDocument>();
                Undo.RegisterCreatedObjectUndo(uiDoc, "Add UIDocument to [TutorialUIDocument]");
            }

            var tourUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Layouts/TutorialTour.uxml");
            if (tourUxml != null)
            {
                uiDoc.visualTreeAsset = tourUxml;
                uiDoc.sortingOrder = 999999;
                Debug.Log("[TutorialSetupWindow] Assigned Assets/UI/Layouts/TutorialTour.uxml with sortingOrder = 999999.");
            }
            else
            {
                Debug.LogError("[TutorialSetupWindow] Could not find Assets/UI/Layouts/TutorialTour.uxml!");
            }

            var tourCtrl = tourGO.GetComponent<TutorialTourController>();
            if (tourCtrl == null)
            {
                tourCtrl = tourGO.AddComponent<TutorialTourController>();
                Undo.RegisterCreatedObjectUndo(tourCtrl, "Add TutorialTourController to [TutorialUIDocument]");
            }

            tourCtrl.EnsureBound();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Tutorial Setup Complete", 
                "Dedicated top-level [TutorialUIDocument] successfully created!\n\n" +
                "• GameObject: [TutorialUIDocument]\n" +
                "• Sorting Order: 999999 (Topmost)\n" +
                "• VisualTreeAsset: TutorialTour.uxml\n" +
                "• Controller: TutorialTourController", "OK");
        }
    }
}
#endif
