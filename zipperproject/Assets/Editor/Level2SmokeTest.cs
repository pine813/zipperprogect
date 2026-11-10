using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zipper.EditorTools
{
    /// <summary>
    /// 第二关无头验收：
    /// 第一关过关 → 第二关接管（球体在沙滩）→ 被贝壳夹住回起点 → 抢珍珠 → 变成珍珠 → 碰到女孩 → 结局图放大完成通关。
    /// 结果写入 &lt;工程&gt;/Logs/level2-smoke.txt，退出码 0=全过 1=失败。
    /// </summary>
    public static class Level2SmokeTest
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const float TimeoutSeconds = 150f;

        static Level1Director level1;
        static Level2Director level2;
        static OceanPlayerController player;
        static ClamController[] clams;
        static GirlNPC girl;
        static bool bound;
        static bool finished;
        static double realStart;
        static float simulated;
        static float phaseTime;
        static int phase;
    static int waterStep;
    static Vector3 waterSpot;
        static float zoomSampleA = -1f;
        static Vector3 girlPositionBeforeRefresh;
        static MobileControls mobile;
        static Vector3 movementStart;
        static Quaternion rotationStart;

        static readonly StringBuilder report = new StringBuilder();
        static readonly StringBuilder errors = new StringBuilder();
        static bool allChecksPassed = true;

        [MenuItem("Tools/Zipper/第二关验收（无头跑一遍）")]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath);

            level1 = null;
            level2 = null;
            player = null;
            clams = null;
            girl = null;
            bound = false;
            finished = false;
            phase = 0;
            phaseTime = 0f;
            simulated = 0f;
            zoomSampleA = -1f;
            allChecksPassed = true;
            report.Length = 0;
            errors.Length = 0;
            realStart = EditorApplication.timeSinceStartup;

            report.AppendLine("===== 第二关验收结果 =====");

            Application.logMessageReceived += OnLogMessage;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;

            Debug.Log("[Level2Test] 请求进入 Play 模式");
        }

        static void Tick()
        {
            if (finished) return;

            if (EditorApplication.timeSinceStartup - realStart > TimeoutSeconds)
            {
                Fail("超时");
                return;
            }

            if (!EditorApplication.isPlaying) return;

            simulated += Time.deltaTime;

            if (!bound && !BindLiveReferences()) return;

            switch (phase)
            {
                case 0:
                    if (simulated < 0.4f) break;
                    Check(level2.CurrentStage == Level2Director.Stage.Dormant, "开局第二关处于 Dormant");
                    level1.StartRun();
                    phase = 1;
                    phaseTime = simulated;
                    break;

                case 1:
                    if (level1.CurrentState == Level1Director.State.Falling)
                    {
                        phase = 2;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 3f) Fail("第一关没有进入 Falling");
                    break;

                case 2:
                    if (level1.CurrentState == Level1Director.State.Swimming)
                    {
                        player.Teleport(new Vector3(level1.ShoreRadius + 1.5f, 1f, 0f));
                        phase = 3;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 10f) Fail("第一关没有进入 Swimming");
                    break;

                case 3:
                    if (level1.CurrentState == Level1Director.State.Cleared)
                    {
                        Check(true, "到达沙滩，第一关 Cleared");
                        phase = 4;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 3f) Fail("沙滩上没有触发第一关过关");
                    break;

                case 4:
                    if (level2.CurrentStage == Level2Director.Stage.Beach)
                    {
                        Check(player.Mode == OceanPlayerMode.Ground, "第二关切到地面滚动模式");
                        Check(player.CurrentVisual == PlayerVisual.Ball, "到沙滩后是球体形态");
                        Check(player.transform.position.y > 0.2f, "球体站在沙滩上 y=" + player.transform.position.y.ToString("F2"));
                        Check(girl != null, "女孩（girl.fbx）存在");
                        Check(clams != null && clams.Length >= 10, "贝壳数量 = " + (clams != null ? clams.Length : 0));
                        Check(level2.FinaleImage != null && level2.FinaleImage.width >= 1000,
                            "结局图已换成新图 " + (level2.FinaleImage != null
                                ? level2.FinaleImage.width + "x" + level2.FinaleImage.height
                                : "null"));
                        Check(Mathf.Abs(level2.GirlRefreshInterval - 30f) < 0.01f,
                            "女孩刷新间隔 = " + level2.GirlRefreshInterval + " 秒");

                        SharkController sharkAfterClear = level1.GetActiveShark(0);
                        Check(sharkAfterClear != null && sharkAfterClear.HuntingEnabled,
                            "进入第二关后鲨鱼恢复自由狩猎");

                        float minWidth = float.MaxValue;
                        float maxWidth = 0f;
                        ClamController sample = null;
                        for (int i = 0; i < clams.Length; i++)
                        {
                            if (clams[i] == null) continue;
                            if (sample == null) sample = clams[i];

                            Transform bottom = clams[i].transform.Find("BottomShell");
                            Renderer renderer = bottom != null ? bottom.GetComponent<Renderer>() : null;
                            if (renderer == null) continue;

                            float width = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z);
                            if (width < minWidth) minWidth = width;
                            if (width > maxWidth) maxWidth = width;
                        }

                        Check(minWidth > 0.8f, "最小的贝壳也大于主控球体（最小 " + minWidth.ToString("F2") + " 米 vs 球 0.8 米）");
                        Check(maxWidth > minWidth * 1.15f, "贝壳大小不一（" + minWidth.ToString("F2") + " ~ " + maxWidth.ToString("F2") + " 米）");
                        Check(sample != null && Mathf.Abs(sample.PearlChance - 0.5f) < 0.01f,
                            "珍珠有无各半（" + (sample != null ? sample.PearlChance : -1f) + "）");

                        phase = 40;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 5f) Fail("第一关过关后第二关没有接管");
                    break;

                case 40:
                    // 球碰到水 → 变小鱼；被鲨鱼咬 → 泡沫死亡 → 回沙滩变回球
                    if (waterStep == 0)
                    {
                        waterSpot = player.transform.position;
                        player.Teleport(new Vector3(0f, level1.WaterLevel - 0.3f, 0f));
                        waterStep = 1;
                        phaseTime = simulated;
                    }
                    else if (waterStep == 1 && simulated - phaseTime > 0.4f)
                    {
                        Check(player.IsSwimming, "球碰到水后变成小鱼");

                        SharkController biteShark = level1.GetActiveShark(0);
                        if (biteShark != null)
                        {
                            player.Teleport(biteShark.transform.position);
                            waterStep = 4;
                        }
                        else
                        {
                            player.Teleport(waterSpot);
                            waterStep = 5;
                        }

                        phaseTime = simulated;
                    }
                    else if (waterStep == 4)
                    {
                        // 无敌时间过后，每帧把小鱼贴到鲨鱼身上，直到被咬
                        if (simulated - phaseTime > 1.7f)
                        {
                            SharkController biteShark = level1.GetActiveShark(0);
                            if (biteShark != null) player.Teleport(biteShark.transform.position);

                            if (!player.VisualsVisible)
                            {
                                Check(true, "被鲨鱼咬到后变成泡沫");
                                waterStep = 6;
                                phaseTime = simulated;
                            }
                            else if (simulated - phaseTime > 4.5f)
                            {
                                Check(false, "被鲨鱼咬到后变成泡沫（超时未触发）");
                                waterStep = 6;
                                phaseTime = simulated;
                            }
                        }
                    }
                    else if (waterStep == 6 && simulated - phaseTime > 1.8f)
                    {
                        Check(player.CurrentVisual == PlayerVisual.Ball, "泡沫消散后回到沙滩变回球体");
                        waterStep = 0;
                        phase = 41;
                        phaseTime = simulated;
                    }
                    else if (waterStep == 5 && simulated - phaseTime > 0.8f)
                    {
                        Check(!player.IsSwimming, "回到岸上重新变回球体");
                        waterStep = 0;
                        phase = 41;
                        phaseTime = simulated;
                    }
                    break;

                case 41:
                    if (simulated - phaseTime > 0.2f)
                    {
                        mobile = UnityEngine.Object.FindObjectOfType<MobileControls>();
                        Check(mobile != null, "手游控件存在（左下摇杆 + 右下控制盘）");

                        if (mobile != null) mobile.enabled = false;
                        movementStart = player.transform.position;
                        rotationStart = player.BallVisual.rotation;
                        VirtualInput.Move = Vector2.right;
                        phase = 42;
                        phaseTime = simulated;
                    }
                    break;

                case 42:
                    if (simulated - phaseTime > 0.8f)
                    {
                        VirtualInput.Move = Vector2.zero;

                        float moved = Vector3.Distance(player.transform.position, movementStart);
                        Check(moved > 0.4f, "虚拟摇杆能驱动球体滚动（移动 " + moved.ToString("F2") + " 米）");
                        Check(Quaternion.Angle(player.BallVisual.rotation, rotationStart) > 5f,
                            "滚动时球体确实在转（转了 " + Quaternion.Angle(player.BallVisual.rotation, rotationStart).ToString("F0") + "°）");

                        if (mobile != null) mobile.enabled = true;
                        phase = 5;
                        phaseTime = simulated;
                    }
                    break;

                case 5:
                    if (simulated - phaseTime > 0.3f)
                    {
                        girlPositionBeforeRefresh = girl.transform.position;
                        level2.RefreshGirlPosition();
                        phase = 6;
                        phaseTime = simulated;
                    }
                    break;

                case 6:
                    if (simulated - phaseTime > 0.3f)
                    {
                        float moved = Vector3.Distance(girl.transform.position, girlPositionBeforeRefresh);
                        Check(moved > 1f, "女孩可以随机刷新位置（移动 " + moved.ToString("F1") + " 米）");

                        ClamController dangerous = FindDangerousClam();
                        if (dangerous == null)
                        {
                            Fail("找不到正在闭合/已闭合的贝壳，无法测试夹人");
                            break;
                        }
                        player.Teleport(dangerous.transform.position + Vector3.up * 0.3f);
                        phase = 7;
                        phaseTime = simulated;
                    }
                    break;

                case 7:
                    if (simulated - phaseTime > 0.4f)
                    {
                        float distanceToSpawn = Vector3.Distance(player.transform.position, level2.PlayerBeachSpawn);
                        Check(level2.CaughtCount >= 1, "球形态贴住贝壳被判夹住（第 " + level2.CaughtCount + " 次）");
                        Check(distanceToSpawn < 3f, "被夹后回到第二关出生点（距离 " + distanceToSpawn.ToString("F1") + " 米）");
                        phase = 8;
                        phaseTime = simulated;
                    }
                    break;

                case 8:
                    ClamController pearlClam = FindOpenPearl();
                    if (pearlClam != null)
                    {
                        player.Teleport(pearlClam.PearlTransform.position);
                        Check(true, "等到张开的贝壳并撞上去");
                        phase = 9;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 10f) Fail("10 秒内没有等到带珍珠的张开贝壳");
                    break;

                case 9:
                    if (level2.CurrentStage == Level2Director.Stage.PearlFormed)
                    {
                        Check(player.CurrentVisual == PlayerVisual.Pearl, "撞到珍珠后变成珍珠形态");
                        Check(player.BallVisual != null && !player.BallVisual.gameObject.activeSelf, "珍珠形态下球体已隐藏");
                        phase = 10;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 3f) Fail("撞到珍珠后没有变成珍珠，当前阶段 " + level2.CurrentStage);
                    break;

                case 10:
                    if (girl != null)
                    {
                        player.Teleport(girl.transform.position + new Vector3(0f, 0.2f, 0f));
                        phase = 11;
                        phaseTime = simulated;
                    }
                    else Fail("找不到女孩");
                    break;

                case 11:
                    if (level2.CurrentStage == Level2Director.Stage.Zooming)
                    {
                        Check(true, "碰到女孩后开始放大结局图");
                        zoomSampleA = level2.ZoomProgress;
                        phase = 12;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 3f) Fail("碰到女孩没有触发结局，当前阶段 " + level2.CurrentStage);
                    break;

                case 12:
                    if (simulated - phaseTime > 1.6f)
                    {
                        Check(level2.ZoomProgress > zoomSampleA,
                            "结局图在持续放大（" + (zoomSampleA * 100f).ToString("F0") + "% → " + (level2.ZoomProgress * 100f).ToString("F0") + "%）");
                        phase = 13;
                        phaseTime = simulated;
                    }
                    break;

                case 13:
                    if (level2.CurrentStage == Level2Director.Stage.Cleared)
                    {
                        Check(level2.ZoomProgress >= 0.999f, "放大到 100% 触发通关");
                        Finish(true);
                    }
                    else if (simulated - phaseTime > 12f) Fail("结局图放大没有完成，进度 " + (level2.ZoomProgress * 100f).ToString("F0") + "%");
                    break;
            }
        }

        static bool BindLiveReferences()
        {
            level1 = UnityEngine.Object.FindObjectOfType<Level1Director>();
            level2 = UnityEngine.Object.FindObjectOfType<Level2Director>();
            if (level1 == null || level2 == null) return false;

            player = level2.Player;
            if (player == null) player = level1.Player;
            if (player == null) return false;

            clams = level2.Clams;
            girl = level2.Girl;
            if (clams == null || clams.Length == 0) return false;

            bound = true;
            Debug.Log("[Level2Test] 绑定完成，贝壳 " + clams.Length + " 个");
            return true;
        }

        static ClamController FindOpenPearl()
        {
            if (clams == null) return null;
            for (int i = 0; i < clams.Length; i++)
            {
                ClamController clam = clams[i];
                if (clam == null) continue;
                if (!clam.PearlAvailable) continue;
                if (clam.PearlTransform == null) continue;
                return clam;
            }
            return null;
        }

        static ClamController FindDangerousClam()
        {
            if (clams == null) return null;
            for (int i = 0; i < clams.Length; i++)
            {
                ClamController clam = clams[i];
                if (clam != null && clam.IsDangerous) return clam;
            }
            return null;
        }

        static void Check(bool condition, string description)
        {
            if (!condition) allChecksPassed = false;
            string line = (condition ? "  [OK]   " : "  [FAIL] ") + description;
            report.AppendLine(line);
            Debug.Log("[Level2Test] " + line);
        }

        static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (errors.Length > 2000) return;
            errors.AppendLine("[" + type + "] " + condition);
        }

        static void Fail(string message)
        {
            report.AppendLine("  [FAIL] " + message);
            Finish(false);
        }

        static void Finish(bool passed)
        {
            if (finished) return;
            finished = true;

            bool ok = passed && allChecksPassed && errors.Length == 0;

            report.AppendLine("运行期错误数: " + (errors.Length == 0 ? 0 : errors.Length));
            if (errors.Length > 0) report.AppendLine("错误摘录:\n" + errors);
            report.AppendLine("结论: " + (ok ? "PASS" : "FAIL"));

            try
            {
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "level2-smoke.txt"), report.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError("[Level2Test] 写结果失败: " + e.Message);
            }

            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLogMessage;
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
