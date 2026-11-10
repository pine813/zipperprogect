using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace StarryAbyss.EditorTools
{
    /// <summary>
    /// 把 StarryAbyss 打成 WebGL（用于双游戏串联外壳 game-a）。
    /// 用法：-batchmode -executeMethod StarryAbyss.EditorTools.ZipperWebGLBuild.Build
    /// </summary>
    public static class ZipperWebGLBuild
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string OutputDirectory = "Build/WebGL";

        [MenuItem("Starry Abyss/Build WebGL (for Zipper chain)", priority = 21)]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void Build()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[StarryWebGL] 切换 WebGL 平台失败，确认编辑器装了 WebGL 模块");
                EditorApplication.Exit(2);
                return;
            }

            // 按 Build Settings 里启用的场景打包（Main = 星渊归途，ZipperLevel = 第二关）
            var scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) scenes.Add(scene.path);
            }
            if (scenes.Count == 0) scenes.Add(ScenePath);

            Debug.Log("[StarryWebGL] 打包场景: " + string.Join(", ", scenes.ToArray()));

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[StarryWebGL] 打包成功：" + summary.outputPath
                          + "，大小 " + (summary.totalSize / 1024f / 1024f).ToString("F1") + " MB"
                          + "，耗时 " + summary.totalTime.TotalSeconds.ToString("F0") + " 秒");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[StarryWebGL] 打包失败：" + summary.result + "，错误 " + summary.totalErrors);
                EditorApplication.Exit(1);
            }
        }
    }
}
