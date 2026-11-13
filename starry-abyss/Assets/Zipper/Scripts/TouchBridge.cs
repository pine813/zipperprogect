using UnityEngine;

/// <summary>触屏输入到键盘输入的桥：第一章（星渊归途）用。</summary>
public static class TouchBridge
{
    public static bool LeftHeld;
    public static bool RightHeld;

    public static bool LeftDown;
    public static bool RightDown;
    public static bool ConfirmDown;
    public static bool PauseDown;

    public static void ClearDown()
    {
        LeftDown = false;
        RightDown = false;
        ConfirmDown = false;
        PauseDown = false;
    }
}
