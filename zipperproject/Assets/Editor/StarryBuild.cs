using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarryAbyss.EditorTools
{
    public static class StarryBuild
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Starry Abyss/Open Main Scene", priority = 1)]
        public static void OpenMainScene()
        {
            CreateMainScene();
        }

        [MenuItem("Starry Abyss/Build Windows Player", priority = 20)]
        public static void BuildWindows()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var buildDir = Path.Combine(projectRoot, "Build", "StarryAbyss");
            Directory.CreateDirectory(buildDir);

            ConfigurePlayer();
            CreateMainScene();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(buildDir, "StarryAbyss.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("STARRY_BUILD_OK " + report.summary.outputPath + " bytes=" + report.summary.totalSize);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("STARRY_BUILD_FAILED " + report.summary.result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Atelier of the Painted Sky";
            PlayerSettings.productName = "Starry Abyss";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard_2_0);
            QualitySettings.vSyncCount = 1;
        }

        public static void CreateMainScene()
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Assets", "Scenes"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
