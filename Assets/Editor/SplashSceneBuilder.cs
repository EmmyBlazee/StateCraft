#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using System.IO;

namespace StateCraft.Editor
{
    public static class SplashSceneBuilder
    {
        private const string SCENE_PATH       = "Assets/Scenes/SplashScene.unity";
        private const string RT_PATH          = "Assets/Splash screens/VideoRT.renderTexture";
        private const string VIDEO_PATH       = "Assets/Splash screens/Devolution 1.mp4";
        private const string SPLASH_IMG_PATH  = "Assets/Splash screens/Splash.png";
        private const string UXML_SPLASH      = "Assets/UI/Layouts/SplashScreen.uxml";
        private const string UXML_MENU        = "Assets/UI/Layouts/MainMenu.uxml";
        private const string UXML_SELECTOR    = "Assets/UI/Layouts/CountrySelector.uxml";
        private const string PANEL_SETTINGS          = "Assets/Scripts/UI/StateCraftPanelSetting.asset";
        private const string PANEL_SETTINGS_MENU     = "Assets/Scripts/UI/MenuPanelSettings.asset";
        private const string PANEL_SETTINGS_SELECTOR = "Assets/Scripts/UI/SelectorPanelSettings.asset";
        private const string MAIN_SCENE_NAME         = "SampleScene";

        [MenuItem("StateCraft/Build Splash Scene")]
        public static void Build()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = Color.black;
            cam.orthographic     = true;
            cam.depth            = 0;

            var eventSysGO = new GameObject("EventSystem");
            eventSysGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSysGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var rt = EnsureRenderTexture();

            var videoCanvas    = CreateCanvas("VideoCanvas", sortOrder: 10, cam: cam);
            var videoCanvasGrp = videoCanvas.gameObject.AddComponent<CanvasGroup>();

            var videoDisplay = CreateRawImage(videoCanvas.transform, "VideoDisplay", rt);

var splashCanvas    = CreateCanvas("SplashCanvas", sortOrder: 5, cam: cam);
            var splashCanvasGrp = splashCanvas.gameObject.AddComponent<CanvasGroup>();
            splashCanvasGrp.alpha = 0f;

            CreateSplashImage(splashCanvas.transform);

            var uiDocGO = new GameObject("SplashUI");
            uiDocGO.transform.SetParent(splashCanvas.transform, false);
            var uiDoc = uiDocGO.AddComponent<UIDocument>();
            AssignUIDocumentAssets(uiDoc);

            var controllerGO = new GameObject("SplashController");
            var vp = controllerGO.AddComponent<VideoPlayer>();
            ConfigureVideoPlayer(vp, rt);

            var ctrl = controllerGO.AddComponent<StateCraft.Core.SplashController>();
            ctrl.videoDisplay       = videoDisplay;
            ctrl.videoCanvasGroup   = videoCanvasGrp;
            ctrl.splashCanvasGroup  = splashCanvasGrp;
            ctrl.splashUIDocument   = uiDoc;
            ctrl.mainSceneName      = MAIN_SCENE_NAME;
            ctrl.fadeDuration       = 0.8f;
            ctrl.minLoaderTime      = 2.5f;

            var menuDocGO = new GameObject("MenuUI");
            var menuDoc = menuDocGO.AddComponent<UIDocument>();
            AssignUIDocumentAssets(menuDoc, UXML_MENU, PANEL_SETTINGS_MENU, sortOrder: 25);
            ctrl.menuUIDocument = menuDoc;

            var selectorDocGO = new GameObject("SelectorUI");
            var selectorDoc = selectorDocGO.AddComponent<UIDocument>();
            AssignUIDocumentAssets(selectorDoc, UXML_SELECTOR, PANEL_SETTINGS_SELECTOR, sortOrder: 30);
            ctrl.selectorUIDocument = selectorDoc;

            var countryGuids = AssetDatabase.FindAssets("t:CountryDataSO", new[] { "Assets/Data/Countries" });
            var countryList  = new System.Collections.Generic.List<StateCraft.Data.CountryDataSO>();
            foreach (var guid in countryGuids)
            {
                var path  = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<StateCraft.Data.CountryDataSO>(path);
                if (asset != null) countryList.Add(asset);
            }
            countryList.Sort((a, b) => string.Compare(a.countryName, b.countryName, System.StringComparison.OrdinalIgnoreCase));
            ctrl.countryDataList = countryList.ToArray();
            Debug.Log($"[SplashSceneBuilder] Auto-assigned {ctrl.countryDataList.Length} countries to SplashController.");

            const string registryPath = "Assets/Resources/CountryRegistry.asset";
            var registry = AssetDatabase.LoadAssetAtPath<StateCraft.Data.CountryRegistry>(registryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<StateCraft.Data.CountryRegistry>();
                AssetDatabase.CreateAsset(registry, registryPath);
            }
            registry.countries = countryList.ToArray();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SplashSceneBuilder] CountryRegistry saved at {registryPath} with {registry.countries.Length} countries.");

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            RegisterInBuildSettings();

            Debug.Log("[SplashSceneBuilder] SplashScene built and saved to " + SCENE_PATH);
            EditorUtility.DisplayDialog("Done!",
                "SplashScene created at:\n" + SCENE_PATH +
                "\n\nIt has been added as scene index 0 in Build Settings.\nMake sure SampleScene is index 1.",
                "OK");
        }

private static RenderTexture EnsureRenderTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RT_PATH);
            if (existing != null) return existing;

            var rt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            rt.name = "VideoRT";
            AssetDatabase.CreateAsset(rt, RT_PATH);
            return rt;
        }

        private static Canvas CreateCanvas(string name, int sortOrder, Camera cam = null)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();

            if (cam != null)
            {
                canvas.renderMode  = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
            else
            {
                canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            }

            canvas.sortingOrder = sortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static RawImage CreateRawImage(Transform parent, string name, RenderTexture rt)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect  = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img  = go.AddComponent<RawImage>();
            img.texture = rt;
            img.color   = Color.white;
            return img;
        }

        private static void CreateSplashImage(Transform parent)
        {
            var go   = new GameObject("SplashImage");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img    = go.AddComponent<UnityEngine.UI.Image>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPLASH_IMG_PATH);
            if (sprite != null)
                img.sprite = sprite;
            else
                Debug.LogWarning("[SplashSceneBuilder] Splash.png not found at " + SPLASH_IMG_PATH);

            img.preserveAspect = false;
        }

        private static void AssignUIDocumentAssets(UIDocument doc, string uxmlPath = null, string panelSettingsPath = null, int sortOrder = 20)
        {
            string path = uxmlPath ?? UXML_SPLASH;
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            if (uxml != null)
                doc.visualTreeAsset = uxml;
            else
                Debug.LogWarning("[SplashSceneBuilder] UXML not found at " + path);

            string panelPath = panelSettingsPath ?? PANEL_SETTINGS;
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel != null)
                doc.panelSettings = panel;
            else
                Debug.LogWarning("[SplashSceneBuilder] PanelSettings not found at " + panelPath + ". Assign it manually.");

            doc.sortingOrder = sortOrder;
        }

        private static void ConfigureVideoPlayer(VideoPlayer vp, RenderTexture rt)
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VIDEO_PATH);
            if (clip != null)
                vp.clip = clip;
            else
                Debug.LogWarning("[SplashSceneBuilder] Devolution 1.mp4 not found at " + VIDEO_PATH);

            vp.renderMode        = VideoRenderMode.RenderTexture;
            vp.targetTexture     = rt;
            vp.playOnAwake       = false;
            vp.isLooping         = false;
            vp.waitForFirstFrame = true;
            vp.skipOnDrop        = false;
            vp.audioOutputMode   = VideoAudioOutputMode.Direct;
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            scenes.RemoveAll(s => s.path == SCENE_PATH);

            scenes.Insert(0, new EditorBuildSettingsScene(SCENE_PATH, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
