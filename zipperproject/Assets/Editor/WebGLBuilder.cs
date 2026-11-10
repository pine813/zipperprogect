using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Zipper.EditorTools
{
    /// <summary>
    /// 把当前场景打成 WebGL，输出到工程下的 Build/WebGL（index.html 可直接部署）。
    /// 用法：菜单 Tools/Zipper/打包 WebGL，或 -executeMethod Zipper.EditorTools.WebGLBuilder.Build
    /// </summary>
    public static class WebGLBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string OutputDirectory = "Build/WebGL";

        [MenuItem("Tools/Zipper/打包 WebGL")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void Build()
        {
            // Brotli 压缩 + 引擎内解压兜底：体积约 1/3，GitHub Pages 这类静态托管也能直接跑
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.runInBackground = true;

            // 裁剪体积
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[WebGL] 切换到 WebGL 平台失败，请确认编辑器装了 WebGL 模块");
                EditorApplication.Exit(2);
                return;
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[WebGL] 打包成功：" + summary.outputPath
                          + "，大小 " + (summary.totalSize / 1024f / 1024f).ToString("F1") + " MB"
                          + "，耗时 " + summary.totalTime.TotalSeconds.ToString("F0") + " 秒");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[WebGL] 打包失败：" + summary.result + "，错误 " + summary.totalErrors);
                EditorApplication.Exit(1);
            }
        }
    }
}
