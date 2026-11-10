using BlueRunner;
using UnityEngine;

/// <summary>
/// 第三关（蓝星失重飞行）的接入层：
/// - 配置 GameRoot（外部输入、不自己建相机、不显示它的 HUD）
/// - 把键盘 / 触屏输入转成 FeedFlightInput + Fire
/// - 通关后显示「您已完成旅程」
/// </summary>
public class BlueFlightEntry : MonoBehaviour
{
    [SerializeField] Camera flightCamera;

    public Camera FlightCamera => flightCamera;

    GameRoot root;
    PlayerController player;
    Texture2D banner;

    bool finished;
    float finishedTimer;

    // 触屏状态
    int leftFinger = -1;
    Vector2 leftStart;
    int rightFinger = -1;
    float lastTapTime = -10f;
    const float DoubleTapWindow = 0.35f;
    const float DragRange = 90f;

    void Awake()
    {
        root = GetComponent<GameRoot>();
        if (root == null) root = FindObjectOfType<GameRoot>();
        if (root == null)
        {
            Debug.LogError("[BlueFlight] 找不到 GameRoot");
            enabled = false;
            return;
        }

        root.autoStart = false;
        root.demoKeyboard = false;
        root.createCamera = false;
        root.showHud = false;

        Camera cam = flightCamera != null ? flightCamera : Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("BlueFlight Camera");
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.031f, 0.090f, 0.207f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.tag = "MainCamera";

        root.hostCamera = cam;
        flightCamera = cam;

        banner = Resources.Load<Texture2D>("JourneyComplete");

        root.Completed += OnCompleted;
        Debug.Log("[BlueFlight] 第三关初始化完成，相机=" + cam.name + "，横幅=" + (banner != null));
    }

    void Start()
    {
        if (root != null) root.BeginLevel();
        Debug.Log("[BlueFlight] BeginLevel 已调用");
    }

    void OnDestroy()
    {
        if (root != null) root.Completed -= OnCompleted;
    }

    void OnCompleted(LevelResult result)
    {
        finished = true;
        finishedTimer = 0f;
        leftFinger = -1;
        rightFinger = -1;
        Debug.Log("[BlueFlight] 第三关通关：用时 " + result.seconds.ToString("F1") + " 秒，能量 " + result.energy
                  + "，击破 " + result.defeated + "，重试 " + result.retries);
    }

    void Update()
    {
        if (root == null) return;

        if (finished)
        {
            finishedTimer += Time.deltaTime;
            return;
        }

        player = root.Player;
        if (player == null) return;

        float vertical = 0f;
        float forward = 0f;
        bool fire = false;
        bool dash = false;

        ReadKeyboard(ref vertical, ref forward, ref fire, ref dash);
        ReadTouch(ref vertical, ref forward, ref fire, ref dash);

        player.FeedFlightInput(vertical, forward, dash);
        if (fire) root.Fire();

        if (Input.GetKeyDown(KeyCode.Escape)) root.SetPaused(root.State == RunState.Playing);
    }

    void ReadKeyboard(ref float vertical, ref float forward, ref bool fire, ref bool dash)
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) forward += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) forward -= 1f;

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K) ||
            Input.GetKeyDown(KeyCode.Space)) dash = true;
        if (Input.GetKey(KeyCode.J)) fire = true;
    }

    /// <summary>左半屏上下拖动 = 升降；右半屏按住 = 发射；右半屏双击 = 冲刺。</summary>
    void ReadTouch(ref float vertical, ref float forward, ref bool fire, ref bool dash)
    {
        float half = Screen.width * 0.5f;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            bool onLeft = touch.position.x < half;

            if (touch.phase == TouchPhase.Began)
            {
                if (onLeft)
                {
                    leftFinger = touch.fingerId;
                    leftStart = touch.position;
                }
                else
                {
                    rightFinger = touch.fingerId;
                    fire = true;
                    RegisterRightTap(ref dash);
                }
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (touch.fingerId == leftFinger)
                {
                    vertical = Mathf.Clamp((touch.position.y - leftStart.y) / DragRange, -1f, 1f);
                }
                else if (touch.fingerId == rightFinger)
                {
                    fire = true;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (touch.fingerId == leftFinger) leftFinger = -1;
                if (touch.fingerId == rightFinger) rightFinger = -1;
            }
        }

        // 桌面/编辑器用鼠标模拟同一套操作
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 p = Input.mousePosition;
            if (p.x < half)
            {
                leftFinger = -2;
                leftStart = p;
            }
            else
            {
                fire = true;
                RegisterRightTap(ref dash);
            }
        }

        if (leftFinger == -2)
        {
            if (Input.GetMouseButton(0))
            {
                vertical = Mathf.Clamp((Input.mousePosition.y - leftStart.y) / DragRange, -1f, 1f);
            }
            else
            {
                leftFinger = -1;
            }
        }

        if (Input.GetMouseButton(0) && Input.mousePosition.x >= half) fire = true;
    }

    void RegisterRightTap(ref bool dash)
    {
        if (Time.unscaledTime - lastTapTime <= DoubleTapWindow)
        {
            dash = true;
            lastTapTime = -10f;
        }
        else
        {
            lastTapTime = Time.unscaledTime;
        }
    }

    void OnGUI()
    {
        if (!finished) return;

        float alpha = Mathf.Clamp01(finishedTimer / 1.2f);

        GUI.color = new Color(0f, 0f, 0f, 0.82f * alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (banner == null) return;

        float height = Mathf.Min(Screen.height * 0.26f, Screen.width * 0.24f);
        float width = height * banner.width / Mathf.Max(1, banner.height);
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(rect, banner, ScaleMode.ScaleToFit, true);
        GUI.color = Color.white;
    }
}
