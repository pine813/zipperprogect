using UnityEngine;

/// <summary>
/// 第一章（星渊归途）的手机触屏按钮：左/右转向 + 确认 + 暂停。
/// 场景里存在 StarryGame 时自动创建，不需要改场景。
/// </summary>
public class TouchControlsChapterOne : MonoBehaviour
{
    GUIStyle buttonStyle;
    GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        if (Object.FindObjectOfType<StarryAbyss.StarryGame>() == null) return;
        if (Object.FindObjectOfType<TouchControlsChapterOne>() != null) return;

        GameObject go = new GameObject("TouchControlsChapterOne");
        go.AddComponent<TouchControlsChapterOne>();
        Debug.Log("[Touch] 第一章触屏按钮已启用");
    }

    void OnGUI()
    {
        if (!Input.touchSupported && !Application.isMobilePlatform) return;

        // 上一帧留下的“按下”标记已在 Update 里消费，这里清掉
        TouchBridge.ClearDown();

        float size = Mathf.Clamp(Screen.height * 0.18f, 96f, 210f);
        float margin = size * 0.26f;
        float bottom = Screen.height - size - margin;

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = Mathf.RoundToInt(size * 0.42f);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = Mathf.RoundToInt(size * 0.16f);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = new Color(0.75f, 0.88f, 1f, 0.75f);
        }

        Color previous = GUI.backgroundColor;

        GUI.backgroundColor = new Color(0.10f, 0.18f, 0.30f, 0.62f);
        TouchBridge.LeftHeld = GUI.RepeatButton(new Rect(margin, bottom, size, size), "<", buttonStyle);
        TouchBridge.RightHeld = GUI.RepeatButton(new Rect(margin * 2f + size, bottom, size, size), ">", buttonStyle);

        GUI.backgroundColor = new Color(0.20f, 0.50f, 0.72f, 0.75f);
        if (GUI.Button(new Rect(Screen.width - size * 1.35f - margin, Screen.height - size * 1.35f - margin,
                                size * 1.35f, size * 1.35f), "OK", buttonStyle))
        {
            TouchBridge.ConfirmDown = true;
        }

        GUI.backgroundColor = new Color(0.14f, 0.18f, 0.26f, 0.55f);
        if (GUI.Button(new Rect(Screen.width - size * 0.55f - margin, margin, size * 0.55f, size * 0.55f), "II", buttonStyle))
        {
            TouchBridge.PauseDown = true;
        }

        GUI.backgroundColor = previous;

        GUI.Label(new Rect(margin, bottom - size * 0.24f, size * 2f + margin, size * 0.22f), "转向 / 确认", labelStyle);
    }
}
