using UnityEngine;

/// <summary>
/// 通关后延时进入下一章（Zipper：海洋与珍珠）。
/// 在结局界面调用 ZipperHandoff.Schedule(秒数)，到点会通过 WebBridge 通知外层外壳跳转。
/// 单机版里只是打一条日志，不影响原有流程。
/// </summary>
public class ZipperHandoff : MonoBehaviour
{
    private static ZipperHandoff _instance;

    private float _remaining = -1f;

    public static void Schedule(float delaySeconds)
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("ZipperHandoff");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ZipperHandoff>();
        }

        _instance._remaining = Mathf.Max(0.1f, delaySeconds);
        Debug.Log("[ZipperHandoff] 将在 " + delaySeconds.ToString("F1") + " 秒后进入下一章");
    }

    private void Update()
    {
        if (_remaining < 0f) return;

        _remaining -= Time.unscaledDeltaTime;
        if (_remaining > 0f) return;

        _remaining = -1f;
        Debug.Log("[ZipperHandoff] 进入下一章：Zipper");
        WebBridge.NotifyGameComplete();
    }
}
