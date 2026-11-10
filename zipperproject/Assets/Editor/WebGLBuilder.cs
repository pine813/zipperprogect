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
            // 关掉压缩，方便本地用任意静态服务器直接打开；部署到 Pages 也一样能跑
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.runInBackground = true;

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
