using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 调试覆盖层：只有网址里带 ?debug=1 时才显示（桌面版不显示）。
/// 用来在网页版里一眼看清状态机、玩家状态和最近的报错。
/// </summary>
public class DebugOverlay : MonoBehaviour
{
    static bool enabledByUrl;
    static readonly List<string> errors = new List<string>();

    Level1Director level1;
    Level2Director level2;
    OceanPlayerController player;
    float fps = 60f;
    GUIStyle style;
    GUIStyle small;

    void Awake()
    {
        string url = Application.absoluteURL ?? string.Empty;
        enabledByUrl = url.Contains("debug=1");
        if (!enabledByUrl)
        {
            enabled = false;
            return;
        }

        Application.logMessageReceived += OnLog;
        Debug.Log("[DebugOverlay] enabled, url=" + url);
    }

    void OnDestroy()
    {
        if (enabledByUrl) Application.logMessageReceived -= OnLog;
    }

    void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        errors.Add(condition);
        if (errors.Count > 4) errors.RemoveAt(0);
    }

    void Update()
    {
        if (!enabledByUrl) return;
        if (Time.deltaTime > 0f) fps = Mathf.Lerp(fps, 1f / Time.deltaTime, 0.1f);

        if (level1 == null) level1 = FindObjectOfType<Level1Director>();
        if (level2 == null) level2 = FindObjectOfType<Level2Director>();
        if (player == null) player = FindObjectOfType<OceanPlayerController>();
    }

    void OnGUI()
    {
        if (!enabledByUrl) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 13;
            style.normal.textColor = Color.white;
            style.wordWrap = true;

            small = new GUIStyle(style);
            small.fontSize = 12;
            small.normal.textColor = new Color(1f, 0.75f, 0.7f);
        }

        string level1Text = level1 != null
            ? string.Format("L1 {0} 体力 {1:F1} 第 {2}/{3} 次 鲨鱼 {4} 虾 {5}",
                level1.CurrentState, level1.Stamina, Mathf.Max(1, level1.Attempt), level1.MaxAttempts,
                level1.ActiveSharkCount, level1.ActiveShrimpCount)
            : "L1 (null)";

        string level2Text = level2 != null
            ? string.Format("L2 {0} 贝壳开 {1}", level2.CurrentStage, CountOpenClams(level2))
            : "L2 (null)";

        string playerText = player != null
            ? string.Format("玩家 {0}/{1} {2} {3:F0}fps", player.Mode, player.CurrentVisual,
                player.transform.position.ToString("F1"), fps)
            : "玩家 (null)";

        float width = Mathf.Min(760f, Screen.width - 24f);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(8f, 8f, width, 92f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float y = 12f;
        GUI.Label(new Rect(14f, y, width - 12f, 20f), "debug  screen " + Screen.width + "x" + Screen.height, style);
        y += 18f;
        GUI.Label(new Rect(14f, y, width - 12f, 20f), level1Text, style);
        y += 18f;
        GUI.Label(new Rect(14f, y, width - 12f, 20f), level2Text, style);
        y += 18f;
        GUI.Label(new Rect(14f, y, width - 12f, 20f), playerText, style);

        if (errors.Count > 0)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(8f, 104f, width, 18f * errors.Count + 10f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            for (int i = 0; i < errors.Count; i++)
            {
                GUI.Label(new Rect(14f, 108f + i * 18f, width - 12f, 20f), "ERR " + errors[i], small);
            }
        }
    }

    static int CountOpenClams(Level2Director director)
    {
        ClamController[] clams = director.Clams;
        if (clams == null) return 0;

        int open = 0;
        for (int i = 0; i < clams.Length; i++)
        {
            if (clams[i] != null && clams[i].PearlAvailable) open++;
        }
        return open;
    }
}
