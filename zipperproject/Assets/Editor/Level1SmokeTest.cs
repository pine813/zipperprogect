using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zipper.EditorTools
{
    /// <summary>
    /// 第一关无头验收：
    /// 待机 → 下落 → 落水溅 3 滴水花（2 条鲨鱼 + 1 只虾）→ 体力消耗 → 吃虾回体力 →
    /// 鲨鱼视线发现并咬死 → 沉底化泡沫 → 回到起点（第 2 次）→ 第二次水花 6 滴 → 体力耗尽死亡 → 再次重来 → 抵达沙滩过关。
    /// </summary>
    public static class Level1SmokeTest
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const float TimeoutSeconds = 160f;

        static Level1Director level1;
        static OceanPlayerController player;
        static bool bound;
        static bool finished;
        static double realStart;
        static float simulated;
        static float phaseTime;
        static int phase;
        static float staminaBeforeShrimp;
        static float spiralStartDistance;

        static readonly StringBuilder report = new StringBuilder();
        static readonly StringBuilder errors = new StringBuilder();
        static bool allChecksPassed = true;

        [MenuItem("Tools/Zipper/第一关验收（无头跑一遍）")]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath);

            level1 = null;
            player = null;
            bound = false;
            finished = false;
            phase = 0;
            phaseTime = 0f;
            simulated = 0f;
            allChecksPassed = true;
            report.Length = 0;
            errors.Length = 0;
            realStart = EditorApplication.timeSinceStartup;

            report.AppendLine("===== 第一关验收结果 =====");

            Application.logMessageReceived += OnLogMessage;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;

            Debug.Log("[Level1Test] 请求进入 Play 模式");
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
                    Check(level1.CurrentState == Level1Director.State.Ready, "开局处于 Ready");
                    Check(player.transform.position.y > 12f, "球体悬在空中 y=" + player.transform.position.y.ToString("F1"));
                    Check(player.CurrentVisual == PlayerVisual.Ball, "空中显示球体");
                    Check(UnityEngine.Object.FindObjectsOfType<SeaweedSway>().Length > 0,
                        "海里有会摇摆的海草 " + UnityEngine.Object.FindObjectsOfType<SeaweedSway>().Length + " 丛");
                    GameAudio audio = UnityEngine.Object.FindObjectOfType<GameAudio>();
                    Check(audio != null && audio.ClipCount >= 4,
                        "音效已生成（" + (audio != null ? audio.ClipCount : 0) + " 个）");
                    Check(audio != null && audio.HasAudibleContent(),
                        "音效波形非静音（" + (audio != null ? audio.AudioLevelsText() : "无") + "）");
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
                    else if (simulated - phaseTime > 3f) Fail("没有进入 Falling");
                    break;

                case 2:
                    if (level1.CurrentState == Level1Director.State.Swimming)
                    {
                        Check(player.IsSwimming, "落水后切换为 Swim 模式");
                        Check(player.FishVisual != null && player.FishVisual.gameObject.activeSelf, "落水后显示小鱼");
                        Check(Mathf.Abs(level1.Stamina - level1.MaxStamina) < 0.5f,
                            "入水体力满值 = " + level1.Stamina.ToString("F1"));
                        Check(level1.LastDropCount == 3, "第一次落水溅起 3 滴水花（实际 " + level1.LastDropCount + "）");
                        Check(level1.LastSharkCount >= 2, "第一次刷出的鲨鱼不少于 2 条（实际 " + level1.LastSharkCount + "）");
                        Check(Mathf.Abs(level1.LastSplashRingRadius - level1.ShoreRadius * 0.5f) < 0.5f,
                            "水花落点圆环半径 = 海岸半径的一半（" + level1.LastSplashRingRadius.ToString("F1") + "）");

                        level1.SetHuntingForAllSharks(false);
                        phase = 3;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 10f) Fail("球体没有落入水中");
                    break;

                case 3:
                    if (simulated - phaseTime > 2.8f)
                    {
                        Check(level1.ActiveSharkCount >= 2, "水花落地生成鲨鱼 " + level1.ActiveSharkCount + " 条");
                        Check(level1.ActiveShrimpCount >= 1, "水花落地生成小虾米 " + level1.ActiveShrimpCount + " 只");

                        SharkController spiralShark = level1.GetActiveShark(0);
                        if (spiralShark != null)
                        {
                            spiralStartDistance = new Vector2(spiralShark.transform.position.x, spiralShark.transform.position.z).magnitude;
                            phase = 33;
                            phaseTime = simulated;
                        }
                        else
                        {
                            phase = 4;
                            phaseTime = simulated;
                        }
                    }
                    break;

                case 33:
                    if (simulated - phaseTime > 0.9f)
                    {
                        SharkController spiralShark = level1.GetActiveShark(0);
                        if (spiralShark != null)
                        {
                            float distance = new Vector2(spiralShark.transform.position.x, spiralShark.transform.position.z).magnitude;
                            Check(distance > spiralStartDistance + 0.4f,
                                "出生后呈螺旋向外散开（距中心 " + spiralStartDistance.ToString("F1") + " → " + distance.ToString("F1") + " 米）");
                        }
                        phase = 4;
                        phaseTime = simulated;
                    }
                    break;

                case 4:
                    if (simulated - phaseTime > 1.5f)
                    {
                        Check(level1.Stamina < level1.MaxStamina - 0.8f,
                            "游泳消耗体力（" + level1.MaxStamina.ToString("F0") + " → " + level1.Stamina.ToString("F1") + "）");
                        staminaBeforeShrimp = level1.Stamina;

                        ShrimpController shrimp = level1.GetActiveShrimp(0);
                        if (shrimp == null) { Fail("找不到小虾米"); break; }
                        player.Teleport(shrimp.transform.position);
                        phase = 5;
                        phaseTime = simulated;
                    }
                    break;

                case 5:
                    if (simulated - phaseTime > 0.4f)
                    {
                        Check(level1.Stamina > staminaBeforeShrimp + 0.5f,
                            "吃到小虾米回体力（" + staminaBeforeShrimp.ToString("F1") + " → " + level1.Stamina.ToString("F1") + "）");
                        level1.SetHuntingForAllSharks(true);
                        phase = 6;
                        phaseTime = simulated;
                    }
                    break;

                case 6:
                    if (simulated - phaseTime > 0.3f)
                    {
                        SharkController shark = level1.GetActiveShark(0);
                        if (shark == null) { Fail("找不到鲨鱼"); break; }
                        player.Teleport(shark.transform.position + shark.transform.forward * 4f);
                        phase = 7;
                        phaseTime = simulated;
                    }
                    break;

                case 7:
                    SharkController watched = level1.GetActiveShark(0);
                    if (watched != null && (watched.IsAlerting || watched.IsChasing))
                    {
                        Check(true, "站进视线锥后鲨鱼进入警戒/追杀");
                        player.Teleport(watched.transform.position);
                        phase = 8;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 3f) Fail("鲨鱼视线没有发现玩家");
                    break;

                case 8:
                    if (level1.CurrentState == Level1Director.State.Dying)
                    {
                        Check(true, "被鲨鱼碰到后进入死亡（沉底化泡沫）");
                        phase = 9;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 2f) Fail("被鲨鱼碰到没有死亡，当前状态 " + level1.CurrentState);
                    break;

                case 9:
                    if (level1.CurrentState == Level1Director.State.Ready)
                    {
                        Check(level1.Failures == 1, "第一次失败计数 = " + level1.Failures);
                        Check(player.transform.position.y > 12f, "球体回到空中起点 y=" + player.transform.position.y.ToString("F1"));
                        phase = 10;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 14f) Fail("死亡后没有回到起点");
                    break;

                case 10:
                    if (simulated - phaseTime > 0.3f)
                    {
                        level1.StartRun();
                        phase = 11;
                        phaseTime = simulated;
                    }
                    break;

                case 11:
                    if (level1.CurrentState == Level1Director.State.Swimming)
                    {
                        Check(level1.LastDropCount == 6, "第二次落水溅起 6 滴水花（实际 " + level1.LastDropCount + "）");
                        Check(level1.LastSharkCount <= 2, "第二次刷出的鲨鱼不多于 2 条（实际 " + level1.LastSharkCount + "）");
                        level1.AddStamina(-999f);
                        phase = 12;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 10f) Fail("第二次没有落水");
                    break;

                case 12:
                    if (level1.CurrentState == Level1Director.State.Dying)
                    {
                        Check(true, "体力耗尽后进入死亡");
                        phase = 13;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 2f) Fail("体力耗尽没有死亡");
                    break;

                case 13:
                    if (level1.CurrentState == Level1Director.State.Ready)
                    {
                        Check(level1.Failures == 2, "第二次失败计数 = " + level1.Failures);
                        phase = 14;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 14f) Fail("体力耗尽后没有回到起点");
                    break;

                case 14:
                    if (simulated - phaseTime > 0.3f)
                    {
                        level1.StartRun();
                        phase = 15;
                        phaseTime = simulated;
                    }
                    break;

                case 15:
                    if (level1.CurrentState == Level1Director.State.Swimming)
                    {
                        player.Teleport(new Vector3(level1.ShoreRadius + 1.5f, 1f, 0f));
                        phase = 16;
                        phaseTime = simulated;
                    }
                    else if (simulated - phaseTime > 10f) Fail("第三次没有落水");
                    break;

                case 16:
                    if (level1.CurrentState == Level1Director.State.Cleared)
                    {
                        Check(true, "抵达沙滩触发过关（Cleared）");
                        Finish(true);
                    }
                    else if (simulated - phaseTime > 3f) Fail("到达沙滩没有触发过关");
                    break;
            }
        }

        static bool BindLiveReferences()
        {
            level1 = UnityEngine.Object.FindObjectOfType<Level1Director>();
            if (level1 == null) return false;

            player = level1.Player;
            if (player == null) return false;

            bound = true;
            Debug.Log("[Level1Test] 绑定完成");
            return true;
        }

        static void Check(bool condition, string description)
        {
            if (!condition) allChecksPassed = false;
            string line = (condition ? "  [OK]   " : "  [FAIL] ") + description;
            report.AppendLine(line);
            Debug.Log("[Level1Test] " + line);
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
                File.WriteAllText(Path.Combine(directory, "level1-smoke.txt"), report.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError("[Level1Test] 写结果失败: " + e.Message);
            }

            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLogMessage;
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
