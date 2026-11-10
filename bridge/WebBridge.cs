using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// WebGL 通关桥：在网页版里通知外层“双游戏串联外壳”当前游戏已通关，外壳会自动切到下一个游戏。
/// 用法（放在任意地方，通关时调用一次即可）：
///     WebBridge.NotifyGameComplete();
///
/// 需要配合 Assets/Plugins/WebGL/ZipperBridge.jslib 一起使用。
/// </summary>
public static class WebBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ZipperGameComplete();
#endif

    public static void NotifyGameComplete()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ZipperGameComplete();
#else
        Debug.Log("[WebBridge] 游戏通关（非 WebGL 平台，仅记录）");
#endif
    }

    /// <summary>是否运行在网页里（被外壳 iframe 包着）。</summary>
    public static bool IsWebGL
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }
}
