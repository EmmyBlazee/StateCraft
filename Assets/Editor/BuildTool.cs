#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

namespace StateCraft.Editor
{
    /// <summary>
    /// One-click Windows EXE builder: StateCraft → Build Windows EXE
    /// 
    /// Steps performed automatically:
    ///   1. Rebuilds the SplashScene (wires all country data, creates CountryRegistry)
    ///   2. Runs BuildPipeline for Windows x86_64 Standalone
    ///   3. Opens the output folder in Explorer when done
    /// </summary>
    public static class BuildTool
    {
        // Output folder relative to the project root (i.e., c:\Users\Tobi\StateCraft\Build\)
        private const string BUILD_FOLDER = "Build";
        private const string EXE_NAME     = "StateCraft.exe";

        [MenuItem("StateCraft/Build Windows EXE", priority = 20)]
        public static void BuildWindowsExe()
        {
            // ── Step 1: Rebuild SplashScene & CountryRegistry ──────────────
            Debug.Log("[BuildTool] Step 1: Rebuilding SplashScene and CountryRegistry...");
            SplashSceneBuilder.Build();

            // ── Step 2: Resolve scene list from Build Settings ─────────────
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

            // ── Step 3: Prepare output path ────────────────────────────────
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDir    = Path.Combine(projectRoot, BUILD_FOLDER);
            string exePath     = Path.Combine(buildDir,   EXE_NAME);

            // Clean previous build for a fresh output
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

            // ── Step 4: Build ──────────────────────────────────────────────
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

            // ── Step 5: Report result ──────────────────────────────────────
            if (summary.result == BuildResult.Succeeded)
            {
                double sizeMB = summary.totalSize / (1024.0 * 1024.0);
                string msg = $"Build succeeded!\n\nOutput: {buildDir}\nSize: {sizeMB:F1} MB\nTime: {summary.totalTime.TotalSeconds:F1}s";
                Debug.Log($"[BuildTool] ✓ {msg}");

                EditorUtility.DisplayDialog("Build Succeeded!", msg, "Open Folder");

                // Open the build folder in Windows Explorer
                EditorUtility.RevealInFinder(exePath);
            }
            else
            {
                string msg = $"Build FAILED with result: {summary.result}\nErrors: {summary.totalErrors}\n\nCheck the Console for details.";
                Debug.LogError($"[BuildTool] ✗ {msg}");
                EditorUtility.DisplayDialog("Build Failed", msg, "OK");
            }
        }

        /// <summary>
        /// Menu item to just open the Build folder in Explorer without building.
        /// </summary>
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
