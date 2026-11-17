#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

namespace StateCraft.Editor
{
    public static class BuildTool
    {
        private const string BUILD_FOLDER = "Build";
        private const string EXE_NAME     = "StateCraft.exe";

        [MenuItem("StateCraft/Build Windows EXE", priority = 20)]
        public static void BuildWindowsExe()
        {
            Debug.Log("[BuildTool] Step 1: Rebuilding SplashScene and CountryRegistry...");
            SplashSceneBuilder.Build();

            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Build Failed",
                    "No scenes are registered in Build Settings!\n\nRun StateCraft → Build Splash Scene first.",
                    "OK");
                return;
            }

            var scenePaths = new string[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
                scenePaths[i] = scenes[i].path;

            Debug.Log($"[BuildTool] Building {scenePaths.Length} scene(s): {string.Join(", ", scenePaths)}");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDir    = Path.Combine(projectRoot, BUILD_FOLDER);
            string exePath     = Path.Combine(buildDir,   EXE_NAME);

            if (Directory.Exists(buildDir))
            {
                bool clean = EditorUtility.DisplayDialog(
                    "Clean Build Folder?",
                    $"Delete existing build at:\n{buildDir}\n\nand rebuild from scratch?",
                    "Yes, clean build", "No, keep existing files");

                if (clean)
                {
                    Directory.Delete(buildDir, recursive: true);
                    Debug.Log($"[BuildTool] Cleaned old build at: {buildDir}");
                }
            }

            Directory.CreateDirectory(buildDir);

            var options = new BuildPlayerOptions
            {
                scenes           = scenePaths,
                locationPathName = exePath,
                target           = BuildTarget.StandaloneWindows64,
                options          = BuildOptions.None,
            };

            Debug.Log($"[BuildTool] Starting build → {exePath}");
            BuildReport  report  = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                double sizeMB = summary.totalSize / (1024.0 * 1024.0);
                string msg = $"Build succeeded!\n\nOutput: {buildDir}\nSize: {sizeMB:F1} MB\nTime: {summary.totalTime.TotalSeconds:F1}s";
                Debug.Log($"[BuildTool] ✓ {msg}");

                EditorUtility.DisplayDialog("Build Succeeded!", msg, "Open Folder");

                EditorUtility.RevealInFinder(exePath);
            }
            else
            {
                string msg = $"Build FAILED with result: {summary.result}\nErrors: {summary.totalErrors}\n\nCheck the Console for details.";
                Debug.LogError($"[BuildTool] ✗ {msg}");
                EditorUtility.DisplayDialog("Build Failed", msg, "OK");
            }
        }

        [MenuItem("StateCraft/Open Build Folder", priority = 21)]
        public static void OpenBuildFolder()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDir    = Path.Combine(projectRoot, BUILD_FOLDER);

            if (!Directory.Exists(buildDir))
            {
                EditorUtility.DisplayDialog("No Build Yet",
                    $"No build folder found at:\n{buildDir}\n\nRun StateCraft → Build Windows EXE first.",
                    "OK");
                return;
            }

            EditorUtility.RevealInFinder(buildDir);
        }
    }
}
#endif
