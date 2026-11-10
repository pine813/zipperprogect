using UnityEngine;

public enum OceanPlayerMode
{
    Air,
    Swim,
    Ground
}

public enum PlayerVisual
{
    Ball,
    Fish,
    Pearl
}

/// <summary>
/// 主角：三种形态共用一套控制。
/// Air   第一关起点，只受重力落下；
/// Swim  落水变鱼，3D 自由游动；
/// Ground 第二关沙滩，球体（或珍珠）贴地滚动。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class OceanPlayerController : MonoBehaviour
{
    [Header("下落")]
    [SerializeField] float fallGravity = -25f;

    [Header("游泳")]
    [SerializeField] float swimSpeed = 7.5f;
    [SerializeField] float swimSprintMultiplier = 1.55f;
    [SerializeField] float swimAcceleration = 20f;
    [SerializeField] float waterDrag = 3f;
    [SerializeField] float surfacePushDown = -20f;
    [SerializeField] float turnResponsiveness = 9f;

    [Header("地面滚动（第二关）")]
    [SerializeField] float groundSpeed = 7f;
    [SerializeField] float groundSprintMultiplier = 1.5f;
    [SerializeField] float groundAcceleration = 32f;
    [SerializeField] float groundGravity = -25f;
    [SerializeField] float groundFriction = 14f;

    [Header("水")]
    [SerializeField] float waterLevel = 0f;
    [SerializeField] float shoreRadius = 46.5f;
    [SerializeField] bool autoReturnToGround;

    [Header("外观")]
    [SerializeField] Transform ballVisual;
    [SerializeField] Transform fishVisual;
    [SerializeField] Transform pearlVisual;
    [SerializeField] Transform fishTail;
    [SerializeField] float tailWiggleSpeed = 9f;
    [SerializeField] float tailWiggleAngle = 26f;
    [SerializeField] float rollRadius = 0.4f;

    CharacterController controller;
    Transform cameraTransform;
    Vector3 velocity;
    Vector3 planarVelocity;
    float verticalVelocity;
    OceanPlayerMode mode = OceanPlayerMode.Air;
    PlayerVisual visual = PlayerVisual.Ball;
    PlayerVisual landVisual = PlayerVisual.Ball;
    float wigglePhase;
    bool inputEnabled = true;
    bool visualsVisible = true;

    public OceanPlayerMode Mode => mode;
    public PlayerVisual CurrentVisual => visual;
    public bool IsSwimming => mode == OceanPlayerMode.Swim;
    public bool InputEnabled => inputEnabled;
    public bool VisualsVisible => visualsVisible;
    public Transform FishVisual => fishVisual;
    public Transform BallVisual => ballVisual;
    public Transform PearlVisual => pearlVisual;

    public void ConfigureVisuals(Transform ball, Transform fish, Transform tail, Transform pearl)
    {
        ballVisual = ball;
        fishVisual = fish;
        fishTail = tail;
        pearlVisual = pearl;
    }

    public void ConfigureWater(float waterY, float shore)
    {
        waterLevel = waterY;
        shoreRadius = shore;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        ApplyVisuals();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled)
        {
            velocity = Vector3.zero;
            planarVelocity = Vector3.zero;
        }
    }

    public void PrepareForFall()
    {
        SetMode(OceanPlayerMode.Air);
        inputEnabled = true;
        velocity = Vector3.zero;
        verticalVelocity = 0f;
    }

    public void EnterWater()
    {
        SetMode(OceanPlayerMode.Swim);
    }

    public void EnterBeach()
    {
        SetMode(OceanPlayerMode.Ground);
        SetVisual(PlayerVisual.Ball);
        inputEnabled = true;
    }

    public void SetMode(OceanPlayerMode newMode)
    {
        mode = newMode;
        velocity = Vector3.zero;
        planarVelocity = Vector3.zero;
        verticalVelocity = 0f;

        if (controller != null)
        {
            controller.radius = newMode == OceanPlayerMode.Swim ? 0.32f : 0.4f;
            controller.height = newMode == OceanPlayerMode.Swim ? 0.64f : 0.8f;
            controller.center = Vector3.zero;
        }

        if (newMode == OceanPlayerMode.Swim) SetVisual(PlayerVisual.Fish);
        else if (newMode == OceanPlayerMode.Air || newMode == OceanPlayerMode.Ground) SetVisual(PlayerVisual.Ball);
    }

    public void SetVisual(PlayerVisual kind)
    {
        visual = kind;
        if (kind != PlayerVisual.Fish) landVisual = kind;
        ApplyVisuals();
    }

    /// <summary>第二关用：小鱼重新上到沙滩时自动变回球体（或珍珠）。</summary>
    public void SetAutoReturnToGround(bool enabled)
    {
        autoReturnToGround = enabled;
    }

    /// <summary>死亡化泡沫时把外观整体藏起来（保留碰撞与相机跟随）。</summary>
    public void SetVisualsVisible(bool visible)
    {
        visualsVisible = visible;
        ApplyVisuals();
    }

    /// <summary>失去控制并下沉（体力耗尽/被咬），保留当前外观（小鱼）。</summary>
    public void BeginSink()
    {
        mode = OceanPlayerMode.Air;
        velocity = Vector3.zero;
        planarVelocity = Vector3.zero;
        verticalVelocity = 0f;
        ApplyVisuals();
    }

    public void Teleport(Vector3 position)
    {
        if (controller != null) controller.enabled = false;
        transform.position = position;
        if (controller != null) controller.enabled = true;
        velocity = Vector3.zero;
        planarVelocity = Vector3.zero;
        verticalVelocity = 0f;
    }

    void ApplyVisuals()
    {
        bool showBall = visualsVisible && visual == PlayerVisual.Ball;
        bool showFish = visualsVisible && visual == PlayerVisual.Fish;
        bool showPearl = visualsVisible && visual == PlayerVisual.Pearl;

        if (ballVisual != null) ballVisual.gameObject.SetActive(showBall);
        if (fishVisual != null) fishVisual.gameObject.SetActive(showFish);
        if (pearlVisual != null) pearlVisual.gameObject.SetActive(showPearl);
        if (fishTail != null) fishTail.localRotation = Quaternion.identity;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        switch (mode)
        {
            case OceanPlayerMode.Air:
                UpdateAir(dt);
                break;
            case OceanPlayerMode.Swim:
                UpdateSwim(dt);
                break;
            case OceanPlayerMode.Ground:
                UpdateGround(dt);
                break;
        }

        UpdateSwimAnimation(dt);
        UpdateAudio();
        UpdateWaterContact();
    }

    /// <summary>碰到水就变小鱼；从水里爬回沙滩再变回原来的形态。</summary>
    void UpdateWaterContact()
    {
        if (mode == OceanPlayerMode.Ground && transform.position.y <= waterLevel - 0.05f)
        {
            SetMode(OceanPlayerMode.Swim);
            SetInputEnabled(true);
            GameAudio.PlaySplash();
        }
        else if (mode == OceanPlayerMode.Swim && autoReturnToGround &&
                 transform.position.y >= waterLevel + 0.25f && controller.isGrounded)
        {
            SetMode(OceanPlayerMode.Ground);
            SetVisual(landVisual);
        }
    }

    /// <summary>把当前运动状态转成音效强度：水里=划水声，沙滩上=滚动沙沙声。</summary>
    void UpdateAudio()
    {
        if (mode == OceanPlayerMode.Swim)
        {
            float speed = inputEnabled ? Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.1f, swimSpeed)) : 0f;
            GameAudio.SetSwimIntensity(speed);
            GameAudio.SetSandIntensity(0f);
        }
        else if (mode == OceanPlayerMode.Ground)
        {
            float speed = Mathf.Clamp01(planarVelocity.magnitude / Mathf.Max(0.1f, groundSpeed));
            GameAudio.SetSandIntensity(speed);
            GameAudio.SetSwimIntensity(0f);
        }
        else
        {
            GameAudio.SetSwimIntensity(0f);
            GameAudio.SetSandIntensity(0f);
        }
    }

    void UpdateAir(float dt)
    {
        if (!inputEnabled) return;

        velocity.y += fallGravity * dt;
        controller.Move(velocity * dt);
    }

    void UpdateSwim(float dt)
    {
        Vector3 desired = Vector3.zero;

        if (inputEnabled)
        {
            Vector2 raw = ReadMoveInput();
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

            Vector3 direction = forward * raw.y + right * raw.x;

            float vertical = VirtualInput.Vertical;
            if (Mathf.Abs(vertical) < 0.01f)
            {
                if (Input.GetKey(KeyCode.Space)) vertical += 1f;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) vertical -= 1f;
            }
            direction += Vector3.up * vertical;

            if (direction.sqrMagnitude > 1f) direction.Normalize();

            bool sprinting = VirtualInput.Sprint ||
                             Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float speed = swimSpeed * (sprinting ? swimSprintMultiplier : 1f);
            desired = direction * speed;
        }

        velocity = Vector3.MoveTowards(velocity, desired, swimAcceleration * dt);
        if (desired.sqrMagnitude < 0.01f)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, waterDrag * dt);
        }

        float radius = new Vector2(transform.position.x, transform.position.z).magnitude;
        bool nearShore = radius > shoreRadius - 4f;
        if (!nearShore && transform.position.y > waterLevel - 0.2f)
        {
            velocity.y = Mathf.Min(velocity.y, 0f) + surfacePushDown * dt;
        }

        controller.Move(velocity * dt);

        Vector3 facing = new Vector3(velocity.x, velocity.y * 0.35f, velocity.z);
        if (fishVisual != null && facing.sqrMagnitude > 0.08f)
        {
            Quaternion target = Quaternion.LookRotation(facing.normalized, Vector3.up);
            fishVisual.rotation = Quaternion.Slerp(fishVisual.rotation, target, turnResponsiveness * dt);
        }
    }

    void UpdateGround(float dt)
    {
        if (!inputEnabled)
        {
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            return;
        }

        Vector2 raw = ReadMoveInput();
        Vector3 wish = CameraRelativeFlat(raw);

        bool sprinting = VirtualInput.Sprint ||
                         Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float speed = groundSpeed * (sprinting ? groundSprintMultiplier : 1f);
        Vector3 target = wish * speed;

        if (wish.sqrMagnitude < 0.0001f)
        {
            planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, groundFriction * dt);
        }
        else
        {
            planarVelocity = Vector3.MoveTowards(planarVelocity, target, groundAcceleration * dt);
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += groundGravity * dt;

        controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);

        RollActiveVisual(dt);
    }

    void RollActiveVisual(float dt)
    {
        Transform active = visual == PlayerVisual.Pearl ? pearlVisual : ballVisual;
        if (active == null) return;

        Vector3 planar = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
        if (planar.sqrMagnitude < 0.0004f) return;

        Vector3 axis = Vector3.Cross(Vector3.up, planar.normalized);
        float degrees = planar.magnitude * dt / Mathf.Max(0.01f, rollRadius) * Mathf.Rad2Deg;
        active.Rotate(axis, degrees, Space.World);
    }

    void UpdateSwimAnimation(float dt)
    {
        if (visual != PlayerVisual.Fish || fishTail == null) return;

        float speedFactor = Mathf.Clamp(velocity.magnitude / Mathf.Max(0.01f, swimSpeed), 0.25f, 1.4f);
        wigglePhase += dt * tailWiggleSpeed * speedFactor;
        fishTail.localRotation = Quaternion.Euler(0f, Mathf.Sin(wigglePhase) * tailWiggleAngle, 0f);
    }

    Vector2 ReadMoveInput()
    {
        Vector2 raw = VirtualInput.Move;
        if (raw.sqrMagnitude < 0.0001f)
        {
            raw = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        if (raw.sqrMagnitude > 1f) raw.Normalize();
        return raw;
    }

    Vector3 CameraRelativeFlat(Vector2 input)
    {
        if (cameraTransform == null) return new Vector3(input.x, 0f, input.y);

        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        else forward.Normalize();

        Vector3 right = cameraTransform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        else right.Normalize();

        return forward * input.y + right * input.x;
    }
}
