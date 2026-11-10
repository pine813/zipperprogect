using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// WebGL 通关桥：网页版里通知外层「双游戏串联外壳」本章已通关，外壳会自动切到下一章（Zipper）。
/// 需要配合 Assets/Plugins/WebGL/ZipperBridge.jslib。
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
