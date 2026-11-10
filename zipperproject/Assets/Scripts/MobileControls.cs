using UnityEngine;

/// <summary>
/// 手游操作界面（纯 OnGUI 绘制，不依赖 UGUI）：
/// 左下角：摇杆，控制移动方向；
/// 右下角：一个圆形 + 上下两个底边相对的正三角形 —— 中间圆形加速，上三角上浮，下三角下潜。
/// 同时支持触屏与鼠标（桌面调试）。
/// </summary>
public class MobileControls : MonoBehaviour
{
    [SerializeField] float joystickRadius = 78f;
    [SerializeField] float joystickMargin = 140f;
    [SerializeField] float buttonRadius = 46f;
    [SerializeField] float buttonMargin = 120f;
    [SerializeField] float triangleHeight = 78f;
    [SerializeField] float triangleGap = 26f;
    [SerializeField] float opacity = 0.55f;

    Texture2D ringTexture;
    Texture2D discTexture;
    Texture2D triangleUpTexture;
    Texture2D triangleDownTexture;

    Vector2 joystickKnob;
    bool joystickActive;

    bool mouseDraggingJoystick;

    void Awake()
    {
        ringTexture = BuildRing(160, 0.82f, 0.16f);
        discTexture = BuildDisc(128);
        triangleUpTexture = BuildTriangle(128, true);
        triangleDownTexture = BuildTriangle(128, false);
    }

    void Update()
    {
        Vector2 joystickCenter = new Vector2(joystickMargin, joystickMargin);
        Vector2 clusterCenter = new Vector2(Screen.width - buttonMargin, buttonMargin);

        Vector2 pointer = Vector2.zero;
        bool pointerDown = false;
        bool pointerHeld = false;

        Vector2 move = Vector2.zero;
        float vertical = 0f;
        bool sprint = false;
        bool joystickInUse = false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 position = touch.position;
                bool began = touch.phase == TouchPhase.Began;
                bool active = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;

                if (!joystickInUse && (began || active) && Vector2.Distance(position, joystickCenter) <= joystickRadius * 1.9f)
                {
                    joystickInUse = true;
                    joystickActive = true;
                    joystickKnob = ClampToRadius(position - joystickCenter, joystickRadius);
                    move = joystickKnob / joystickRadius;
                    continue;
                }

                if (!active) continue;

                if (Vector2.Distance(position, clusterCenter) <= buttonRadius * 1.35f) sprint = true;
                else if (InsideTriangle(position, clusterCenter, true)) vertical = 1f;
                else if (InsideTriangle(position, clusterCenter, false)) vertical = -1f;
            }

            if (!joystickInUse) joystickActive = false;
        }
        else
        {
            pointer = Input.mousePosition;
            pointerDown = Input.GetMouseButtonDown(0);
            pointerHeld = Input.GetMouseButton(0);

            if (pointerDown)
            {
                mouseDraggingJoystick = Vector2.Distance(pointer, joystickCenter) <= joystickRadius * 1.9f;
            }

            if (mouseDraggingJoystick && pointerHeld)
            {
                joystickActive = true;
                joystickKnob = ClampToRadius(pointer - joystickCenter, joystickRadius);
                move = joystickKnob / joystickRadius;
            }
            else
            {
                joystickActive = false;
                if (!mouseDraggingJoystick && pointerHeld)
                {
                    if (Vector2.Distance(pointer, clusterCenter) <= buttonRadius * 1.35f) sprint = true;
                    else if (InsideTriangle(pointer, clusterCenter, true)) vertical = 1f;
                    else if (InsideTriangle(pointer, clusterCenter, false)) vertical = -1f;
                }
            }

            if (!pointerHeld) mouseDraggingJoystick = false;
        }

        if (!joystickActive) joystickKnob = Vector2.zero;

        VirtualInput.Move = move.sqrMagnitude > 1f ? move.normalized : move;
        VirtualInput.Vertical = vertical;
        VirtualInput.Sprint = sprint;
    }

    bool InsideTriangle(Vector2 position, Vector2 clusterCenter, bool up)
    {
        float centerY = up
            ? clusterCenter.y + buttonRadius + triangleGap + triangleHeight * 0.5f
            : clusterCenter.y - buttonRadius - triangleGap - triangleHeight * 0.5f;

        float halfWidth = triangleHeight * 0.58f;
        float halfHeight = triangleHeight * 0.5f;

        if (Mathf.Abs(position.x - clusterCenter.x) > halfWidth) return false;
        if (Mathf.Abs(position.y - centerY) > halfHeight) return false;

        // 三角形判定：底边靠近圆形，顶点朝外
        float dx = Mathf.Abs(position.x - clusterCenter.x) / halfWidth;
        float dy = up
            ? (position.y - (centerY - halfHeight)) / (halfHeight * 2f)
            : ((centerY + halfHeight) - position.y) / (halfHeight * 2f);

        return dy >= 0f && dy <= 1f && dx <= 1f - dy;
    }

    Vector2 ClampToRadius(Vector2 offset, float radius)
    {
        if (offset.magnitude <= radius) return offset;
        return offset.normalized * radius;
    }

    void OnGUI()
    {
        Vector2 joystickCenter = new Vector2(joystickMargin, Screen.height - joystickMargin);
        Vector2 clusterCenter = new Vector2(Screen.width - buttonMargin, Screen.height - buttonMargin);

        Color previous = GUI.color;

        // 摇杆底盘
        GUI.color = new Color(1f, 1f, 1f, opacity * 0.55f);
        GUI.DrawTexture(new Rect(joystickCenter.x - joystickRadius, joystickCenter.y - joystickRadius,
            joystickRadius * 2f, joystickRadius * 2f), ringTexture, ScaleMode.ScaleToFit);

        // 摇杆头
        float knobRadius = joystickRadius * 0.42f;
        Vector2 knobPosition = joystickCenter + new Vector2(joystickKnob.x, -joystickKnob.y);
        GUI.color = new Color(1f, 1f, 1f, opacity);
        GUI.DrawTexture(new Rect(knobPosition.x - knobRadius, knobPosition.y - knobRadius,
            knobRadius * 2f, knobRadius * 2f), discTexture, ScaleMode.ScaleToFit);

        // 中间圆形：加速
        bool sprintPressed = VirtualInput.Sprint;
        GUI.color = sprintPressed ? new Color(1f, 0.85f, 0.35f, 0.85f) : new Color(1f, 1f, 1f, opacity * 0.8f);
        GUI.DrawTexture(new Rect(clusterCenter.x - buttonRadius, clusterCenter.y - buttonRadius,
            buttonRadius * 2f, buttonRadius * 2f), discTexture, ScaleMode.ScaleToFit);

        // 上三角：上浮
        float triangleWidth = triangleHeight * 0.9f;
        bool upPressed = VirtualInput.Vertical > 0.5f;
        GUI.color = upPressed ? new Color(1f, 0.85f, 0.35f, 0.85f) : new Color(1f, 1f, 1f, opacity * 0.8f);
        GUI.DrawTexture(new Rect(
            clusterCenter.x - triangleWidth * 0.5f,
            clusterCenter.y - buttonRadius - triangleGap - triangleHeight,
            triangleWidth, triangleHeight), triangleUpTexture, ScaleMode.ScaleToFit);

        // 下三角：下潜
        bool downPressed = VirtualInput.Vertical < -0.5f;
        GUI.color = downPressed ? new Color(1f, 0.85f, 0.35f, 0.85f) : new Color(1f, 1f, 1f, opacity * 0.8f);
        GUI.DrawTexture(new Rect(
            clusterCenter.x - triangleWidth * 0.5f,
            clusterCenter.y + buttonRadius + triangleGap,
            triangleWidth, triangleHeight), triangleDownTexture, ScaleMode.ScaleToFit);

        GUI.color = previous;
    }

    // ---------------- 贴图生成 ----------------

    static Texture2D BuildDisc(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                float alpha = Mathf.Clamp01((radius - distance) / 3f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    static Texture2D BuildRing(int size, float innerRatio, float softness)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius)) / radius;
                float edge = 1f - Mathf.Abs(distance - innerRatio) / softness;
                float alpha = Mathf.Clamp01(edge) * 0.9f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    static Texture2D BuildTriangle(int size, bool pointUp)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;

                // 顶点朝上：v=1 时只剩中点；否则底边在 v=0
                float height = pointUp ? v : 1f - v;
                float halfWidth = (1f - height) * 0.5f;
                float distance = Mathf.Abs(u - 0.5f);

                float alpha = Mathf.Clamp01((halfWidth - distance) * size * 0.35f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }
}
