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
    /// <summary>
    /// One-click builder: StateCraft → Build Splash Scene
    /// Creates SplashScene.unity with all GameObjects, components and
    /// cross-references already wired.  Run this once; edit the scene
    /// normally afterwards.
    /// </summary>
    public static class SplashSceneBuilder
    {
        private const string SCENE_PATH       = "Assets/Scenes/SplashScene.unity";
        private const string RT_PATH          = "Assets/Splash screens/VideoRT.renderTexture";
        private const string VIDEO_PATH       = "Assets/Splash screens/Devolution 1.mp4";
        private const string SPLASH_IMG_PATH  = "Assets/Splash screens/Splash.png";
        private const string UXML_PATH        = "Assets/UI/Layouts/SplashScreen.uxml";
        private const string PANEL_SETTINGS   = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.asset";
        private const string MAIN_SCENE_NAME  = "SampleScene";

        // ── Entry point ────────────────────────────────────────────────
        [MenuItem("StateCraft/Build Splash Scene")]
        public static void Build()
        {
            // 1. Save current scene
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // 2. Create / open a blank scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Render Texture ─────────────────────────────────────────
            var rt = EnsureRenderTexture();

            // ── Video Canvas (Sort 10) ─────────────────────────────────
            var videoCanvas    = CreateCanvas("VideoCanvas", sortOrder: 10);
            var videoCanvasGrp = videoCanvas.gameObject.AddComponent<CanvasGroup>();

            var videoDisplay = CreateRawImage(videoCanvas.transform, "VideoDisplay", rt);

            // VideoPlayer lives on the SplashController GO (RequireComponent)
            // We create VideoCanvas just for display; VideoPlayer goes on controller.

            // ── Splash Canvas (Sort 5) ─────────────────────────────────
            var splashCanvas    = CreateCanvas("SplashCanvas", sortOrder: 5);
            var splashCanvasGrp = splashCanvas.gameObject.AddComponent<CanvasGroup>();
            splashCanvasGrp.alpha = 0f;   // starts hidden

            CreateSplashImage(splashCanvas.transform);

            // ── UI Document GO (child of SplashCanvas) ─────────────────
            var uiDocGO = new GameObject("SplashUI");
            uiDocGO.transform.SetParent(splashCanvas.transform, false);
            var uiDoc = uiDocGO.AddComponent<UIDocument>();
            AssignUIDocumentAssets(uiDoc);

            // ── SplashController GO ────────────────────────────────────
            var controllerGO = new GameObject("SplashController");
            // VideoPlayer must be on same GO as SplashController (RequireComponent)
            var vp = controllerGO.AddComponent<VideoPlayer>();
            ConfigureVideoPlayer(vp, rt);

            var ctrl = controllerGO.AddComponent<StateCraft.Core.SplashController>();
            ctrl.videoDisplay       = videoDisplay;
            ctrl.videoCanvasGroup   = videoCanvasGrp;
            ctrl.splashCanvasGroup  = splashCanvasGrp;
            ctrl.splashUIDocument   = uiDoc;
            ctrl.mainSceneName      = MAIN_SCENE_NAME;
            ctrl.fadeDuration       = 0.8f;
            ctrl.minSplashTime      = 2.0f;

            // ── Save scene ─────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            // ── Register in Build Settings ─────────────────────────────
            RegisterInBuildSettings();

            Debug.Log("[SplashSceneBuilder] SplashScene built and saved to " + SCENE_PATH);
            EditorUtility.DisplayDialog("Done!",
                "SplashScene created at:\n" + SCENE_PATH +
                "\n\nIt has been added as scene index 0 in Build Settings.\nMake sure SampleScene is index 1.",
                "OK");
        }

        // ── Helpers ────────────────────────────────────────────────────

        /// Creates or reuses the VideoRT render texture asset.
        private static RenderTexture EnsureRenderTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RT_PATH);
            if (existing != null) return existing;

            var rt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            rt.name = "VideoRT";
            AssetDatabase.CreateAsset(rt, RT_PATH);
            return rt;
        }

        /// Creates a full-screen Screen Space — Overlay Canvas.
        private static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// Creates a full-stretch RawImage parented to `parent` showing `rt`.
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

        /// Creates a full-stretch Image parented to `parent` using Splash.png.
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

        /// Assigns the UXML and PanelSettings to a UIDocument component.
        private static void AssignUIDocumentAssets(UIDocument doc)
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
            if (uxml != null)
                doc.visualTreeAsset = uxml;
            else
                Debug.LogWarning("[SplashSceneBuilder] SplashScreen.uxml not found at " + UXML_PATH);

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PANEL_SETTINGS);
            if (panel != null)
                doc.panelSettings = panel;
            else
                Debug.LogWarning("[SplashSceneBuilder] PanelSettings not found. Assign it manually on SplashUI.");

            doc.sortingOrder = 20;
        }

        /// Configures the VideoPlayer to use the render texture and the intro clip.
        private static void ConfigureVideoPlayer(VideoPlayer vp, RenderTexture rt)
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VIDEO_PATH);
            if (clip != null)
                vp.clip = clip;
            else
                Debug.LogWarning("[SplashSceneBuilder] Devolution 1.mp4 not found at " + VIDEO_PATH);

            vp.renderMode        = VideoRenderMode.RenderTexture;
            vp.targetTexture     = rt;
            vp.playOnAwake       = false; // SplashController calls Play()
            vp.isLooping         = false;
            vp.waitForFirstFrame = true;
            vp.skipOnDrop        = false;
            vp.audioOutputMode   = VideoAudioOutputMode.Direct;
        }

        /// Inserts SplashScene as index 0 and keeps existing scenes at higher indices.
        private static void RegisterInBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            // Remove any stale entry for SplashScene
            scenes.RemoveAll(s => s.path == SCENE_PATH);

            // Insert at index 0
            scenes.Insert(0, new EditorBuildSettingsScene(SCENE_PATH, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
