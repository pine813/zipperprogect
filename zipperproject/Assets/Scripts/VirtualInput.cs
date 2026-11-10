using UnityEngine;

/// <summary>
/// 手游式虚拟输入：左下角摇杆给出移动方向，右下角控制盘给出加速/上浮/下潜。
/// 键盘仍然可用，作为桌面调试的备用输入。
/// </summary>
public static class VirtualInput
{
    public static Vector2 Move;
    public static float Vertical;
    public static bool Sprint;

    public static void Clear()
    {
        Move = Vector2.zero;
        Vertical = 0f;
        Sprint = false;
    }
}
