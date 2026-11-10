using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 第一关「海洋」：
/// Ready（球体悬空）→ Falling（下落）→ 落水溅水花（水花落地变鲨鱼/小虾米）→ Swimming（体力消耗、躲避鲨鱼视线）
/// → 体力耗尽或被咬 → 小鱼沉底化泡沫 → 回到起点重来（最多 5 次）→ 游到沙滩过关。
/// </summary>
public class Level1Director : MonoBehaviour
{
    public enum State
    {
        Ready,
        Falling,
        Swimming,
        Dying,
        Cleared,
        Failed
    }

    [Header("引用")]
    [SerializeField] OceanPlayerController player;
    [SerializeField] GameObject sharkPrefab;
    [SerializeField] GameObject shrimpPrefab;
    [SerializeField] GameObject splashDropletPrefab;
    [SerializeField] GameObject bubblePrefab;
    [SerializeField] Transform creatureParent;

    [Header("空间")]
    [SerializeField] Vector3 startPosition = new Vector3(0f, 26f, 0f);
    [SerializeField] float waterLevel = 0f;
    [SerializeField] float shoreRadius = 46.5f;
    [SerializeField] float shoreClearHeight = -0.4f;
    [SerializeField] float seabedY = -14.5f;

    [Header("体力")]
    [SerializeField] float maxStamina = 10f;
    [SerializeField] float staminaDrainPerSecond = 1f;

    [Header("流程")]
    [SerializeField] int maxAttempts = 5;
    [SerializeField] float sinkDuration = 3f;
    [SerializeField] float foamDuration = 1.6f;
    [SerializeField] float failedResetDelay = 3.5f;
    [SerializeField] float spawnInvulnerability = 1.5f;

    [Header("开场")]
    [SerializeField] Texture2D introImage;
    [SerializeField] float introDuration = 2f;
    float introTimer;

    State state = State.Ready;
    float stateTimer;
    float shoreStuckTimer;
    float stamina;
    int attempt;
    int failures;
    int lastDropCount;
    int lastSharkCount;
    float invulnerableUntil;
    int dyingPhase;
    string toast = "按 [空格] 开始";

    float lastSplashRingRadius;
    Vector2 lastSplashRingCenter;

    readonly List<SharkController> sharks = new List<SharkController>();
    readonly List<ShrimpController> shrimps = new List<ShrimpController>();

    public State CurrentState => state;
    public OceanPlayerController Player => player;
    public float ShoreRadius => shoreRadius;
    public float WaterLevel => waterLevel;
    public float Stamina => stamina;
    public float MaxStamina => maxStamina;
    public int Attempt => attempt;
    public int Failures => failures;
    public int EatenCount => failures;
    public int MaxAttempts => maxAttempts;
    public int LastDropCount => lastDropCount;
    public int LastSharkCount => lastSharkCount;
    public int ActiveSharkCount => CountAlive(sharks);
    public int ActiveShrimpCount => CountAlive(shrimps);
    public float LastSplashRingRadius => lastSplashRingRadius;
    public Vector2 LastSplashRingCenter => lastSplashRingCenter;
    public bool ShowHud { get; set; } = true;

    public int AlertingSharkCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < sharks.Count; i++)
            {
                if (sharks[i] != null && (sharks[i].IsAlerting || sharks[i].IsChasing)) count++;
            }
            return count;
        }
    }

    public void Configure(OceanPlayerController playerController, GameObject sharkTemplate, GameObject shrimpTemplate,
        GameObject dropletTemplate, GameObject bubbleTemplate, Transform creatures,
        Vector3 start, float waterY, float shore)
    {
        player = playerController;
        sharkPrefab = sharkTemplate;
        shrimpPrefab = shrimpTemplate;
        splashDropletPrefab = dropletTemplate;
        bubblePrefab = bubbleTemplate;
        creatureParent = creatures;
        startPosition = start;
        waterLevel = waterY;
        shoreRadius = shore;
    }

    /// <summary>测试/调试用：直接改体力。</summary>
    public void AddStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina + amount, 0f, maxStamina);
    }

    void Start()
    {
        if (player == null) player = FindObjectOfType<OceanPlayerController>();
        if (introImage == null) introImage = Resources.Load<Texture2D>("IntroLevel1");
        introTimer = 0f;
        attempt = 0;
        failures = 0;
        stamina = maxStamina;
        ResetToReady();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        stateTimer += dt;
        if (introTimer < introDuration) introTimer += dt;

        switch (state)
        {
            case State.Ready:
                if (introTimer < introDuration) break;
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    StartRun();
                }
                break;

            case State.Falling:
                if (player.transform.position.y <= waterLevel) EnterWater();
                break;

            case State.Swimming:
                UpdateSwimming(dt);
                break;

            case State.Dying:
                UpdateDying(dt);
                break;

            case State.Failed:
                if (stateTimer >= failedResetDelay)
                {
                    failures = 0;
                    attempt = 0;
                    ResetToReady();
                }
                break;

            case State.Cleared:
                break;
        }
    }

    // ---------------- 流程 ----------------

    public void StartRun()
    {
        if (state == State.Falling || state == State.Swimming || state == State.Dying) return;

        attempt++;
        ClearCreatures();

        player.Teleport(startPosition);
        player.SetVisualsVisible(true);
        player.PrepareForFall();
        player.SetInputEnabled(true);

        stamina = maxStamina;
        state = State.Falling;
        stateTimer = 0f;
        toast = "下落中…";
    }

    void EnterWater()
    {
        Vector3 position = player.transform.position;
        player.Teleport(new Vector3(position.x, waterLevel - 0.6f, position.z));
        player.EnterWater();
        player.SetInputEnabled(true);

        stamina = maxStamina;
        SpawnSplash(new Vector3(position.x, waterLevel, position.z));
        GameAudio.PlaySplash();

        invulnerableUntil = Time.time + spawnInvulnerability;
        state = State.Swimming;
        stateTimer = 0f;
        shoreStuckTimer = 0f;
        toast = "躲开鲨鱼，游到沙滩！";
    }

    void UpdateSwimming(float dt)
    {
        stamina -= staminaDrainPerSecond * dt;
        if (stamina <= 0f)
        {
            stamina = 0f;
            BeginDeath("体力耗尽……");
            return;
        }

        Vector3 position = player.transform.position;
        float radius = new Vector2(position.x, position.z).magnitude;
        if (radius >= shoreRadius && position.y > shoreClearHeight)
        {
            ReachBeach();
            return;
        }

        // 兜底：已经游到岸边半径内却卡住（地形/碰撞/掉帧隧穿），1.5 秒后强制判定上岸
        if (radius >= shoreRadius - 2f)
        {
            shoreStuckTimer += dt;
            if (shoreStuckTimer >= 1.5f)
            {
                Debug.Log("[Level1] 玩家已在岸边卡住 1.5 秒，强制判定上岸");
                ReachBeach();
                return;
            }
        }
        else
        {
            shoreStuckTimer = 0f;
        }

        if (Time.time >= invulnerableUntil)
        {
            for (int i = 0; i < sharks.Count; i++)
            {
                SharkController shark = sharks[i];
                if (shark == null) continue;
                if (Vector3.Distance(shark.transform.position, position) <= shark.ContactRadius)
                {
                    BeginDeath("被鲨鱼咬到了……");
                    return;
                }
            }
        }
    }

    void ReachBeach()
    {
        state = State.Cleared;
        stateTimer = 0f;
        toast = "第一关通过！你抵达了沙滩";
        player.SetInputEnabled(false);
        SetSharksHunting(false);
        Debug.Log("[Level1] 过关，尝试次数 " + attempt + "，失败 " + failures + " 次");
    }

    void BeginDeath(string reason)
    {
        if (state != State.Swimming) return;

        state = State.Dying;
        stateTimer = 0f;
        dyingPhase = 0;
        toast = reason;

        player.SetInputEnabled(false);
        player.BeginSink();
        SetSharksHunting(false);
        Debug.Log("[Level1] 小鱼死亡：" + reason);
    }

    void UpdateDying(float dt)
    {
        if (dyingPhase == 0)
        {
            // 沉到海底（或者超时）后化成泡沫
            bool reachedSeabed = player.transform.position.y <= seabedY || player.transform.position.y < waterLevel - 12f;
            bool grounded = player.transform.position.y <= seabedY + 0.4f;
            if (reachedSeabed || grounded || stateTimer >= sinkDuration)
            {
                SpawnFoam(player.transform.position);
                player.SetVisualsVisible(false);
                dyingPhase = 1;
                stateTimer = 0f;
            }
            return;
        }

        if (stateTimer >= foamDuration)
        {
            failures++;
            ClearCreatures();

            if (failures >= maxAttempts)
            {
                state = State.Failed;
                stateTimer = 0f;
                toast = maxAttempts + " 次机会用完了……";
            }
            else
            {
                ResetToReady();
            }
        }
    }

    void ResetToReady()
    {
        player.Teleport(startPosition);
        player.SetMode(OceanPlayerMode.Air);
        player.SetVisualsVisible(true);
        player.SetInputEnabled(false);
        stamina = maxStamina;

        state = State.Ready;
        stateTimer = 0f;
        toast = "第 " + (failures + 1) + " / " + maxAttempts + " 次机会 · 按 [空格] 开始";
    }

    // ---------------- 水花与生物 ----------------

    void SpawnSplash(Vector3 origin)
    {
        int drops = Triangular(attempt + 1);
        lastDropCount = drops;

        // 第一次至少 2 条鲨鱼；之后每次最多 2 条。至少留一只小虾米给玩家回体力。
        int sharkCount = attempt == 1 ? 2 : Random.Range(1, 3);
        sharkCount = Mathf.Clamp(sharkCount, 1, Mathf.Max(1, drops - 1));
        lastSharkCount = sharkCount;

        bool[] sharkFlags = new bool[drops];
        for (int i = 0; i < sharkCount; i++) sharkFlags[i] = true;
        for (int i = drops - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            bool temporary = sharkFlags[i];
            sharkFlags[i] = sharkFlags[j];
            sharkFlags[j] = temporary;
        }

        // 水花落点：落水点与海洋边界连线中点的轨迹 —— 圆心在落点与中心的中点，半径为海岸半径的一半
        Vector2 entryXZ = new Vector2(origin.x, origin.z);
        lastSplashRingCenter = entryXZ * 0.5f;
        lastSplashRingRadius = shoreRadius * 0.5f;

        for (int i = 0; i < drops; i++)
        {
            float angle = i / (float)drops * Mathf.PI * 2f + Random.Range(-0.06f, 0.06f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            bool spawnShark = sharkFlags[i];

            Vector3 target = new Vector3(
                lastSplashRingCenter.x + direction.x * lastSplashRingRadius,
                waterLevel,
                lastSplashRingCenter.y + direction.y * lastSplashRingRadius);

            GameObject dropletObject = Instantiate(splashDropletPrefab, origin, Quaternion.identity);
            SplashDroplet droplet = dropletObject.GetComponent<SplashDroplet>();
            if (droplet == null)
            {
                Destroy(dropletObject);
                continue;
            }

            // 抛物线打到圆环上的目标点：给定飞行时间反解初速度
            const float gravityMagnitude = 20f;
            float flightTime = Random.Range(1.6f, 2.0f);
            Vector3 delta = target - origin;
            Vector3 velocity = new Vector3(
                delta.x / flightTime,
                0.5f * gravityMagnitude * flightTime,
                delta.z / flightTime);

            droplet.Launch(velocity, waterLevel, (self, point) => SpawnCreature(point, spawnShark));
        }

        Debug.Log("[Level1] 第 " + attempt + " 次落水，溅起 " + drops + " 滴水花（落点圆环半径 "
            + lastSplashRingRadius.ToString("F1") + "），其中鲨鱼 " + sharkCount + " 条");
    }

    void SpawnCreature(Vector3 point, bool spawnShark)
    {
        if (spawnShark) SpawnShark(point);
        else SpawnShrimp(point);
    }

    void SpawnShark(Vector3 point)
    {
        if (sharkPrefab == null) return;

        GameObject sharkObject = Instantiate(sharkPrefab, point, Quaternion.identity, creatureParent);
        SharkController shark = sharkObject.GetComponent<SharkController>();
        if (shark == null) return;

        float radius = Mathf.Clamp(new Vector2(point.x, point.z).magnitude, 8f, 40f);
        float angularSpeed = Random.Range(6f, 10f) * (Random.value > 0.5f ? 1f : -1f);
        float depth = Mathf.Clamp(point.y - 3f, -13f, -3f);

        shark.Configure(player.transform, radius, depth, angularSpeed);
        shark.SetHuntingEnabled(state == State.Swimming);

        // 出生后绕着海洋中心螺旋散开
        shark.BeginSpiralIntro(Vector3.zero,
            Random.Range(1.2f, 2.2f),
            Random.Range(20f, 38f) * (Random.value > 0.5f ? 1f : -1f),
            Random.Range(2.0f, 2.8f),
            depth);

        sharks.Add(shark);
    }

    void SpawnShrimp(Vector3 point)
    {
        if (shrimpPrefab == null) return;

        Vector3 position = new Vector3(point.x, Mathf.Clamp(point.y - 1.5f, waterLevel - 13f, waterLevel - 1.2f), point.z);
        GameObject shrimpObject = Instantiate(shrimpPrefab, position, Quaternion.identity, creatureParent);
        ShrimpController shrimp = shrimpObject.GetComponent<ShrimpController>();
        if (shrimp == null) return;

        shrimp.Configure(player.transform, waterLevel, bubblePrefab);
        shrimp.SetCollectedCallback(OnShrimpCollected);

        shrimp.BeginSpiralIntro(Vector3.zero,
            Random.Range(0.8f, 1.6f),
            Random.Range(30f, 55f) * (Random.value > 0.5f ? 1f : -1f),
            Random.Range(1.4f, 2.0f),
            position.y - waterLevel);

        shrimps.Add(shrimp);
    }

    void OnShrimpCollected(ShrimpController shrimp)
    {
        shrimps.Remove(shrimp);
        AddStamina(1f);
        toast = "吃到小虾米，体力 +1";
    }

    public void SpawnFoamAt(Vector3 position)
    {
        SpawnFoam(position);
    }

    void SpawnFoam(Vector3 position)
    {
        if (bubblePrefab == null) return;

        for (int i = 0; i < 7; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.7f;
            GameObject bubbleObject = Instantiate(bubblePrefab, position + offset, Quaternion.identity);
            Bubble bubble = bubbleObject.GetComponent<Bubble>();
            if (bubble != null) bubble.Configure(waterLevel, Random.Range(4f, 7f));
            if (bubbleObject.GetComponent<Bubble>() == null) Destroy(bubbleObject);
        }
    }

    void SetSharksHunting(bool hunting)
    {
        for (int i = 0; i < sharks.Count; i++)
        {
            if (sharks[i] != null) sharks[i].SetHuntingEnabled(hunting);
        }
    }

    /// <summary>
    /// 进入第二关（或玩家在水里又变成小鱼）时调用：鲨鱼恢复自由游动，
    /// 并重新具备扫描 + 追杀小鱼的能力。
    /// </summary>
    public void ResumeSharkHunting()
    {
        // 第一关结束时鲨鱼可能已经被清掉了：第二关重新补几条，保证还能扫描/追杀
        if (sharks.Count == 0 && sharkPrefab != null)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(16f, 34f);
                Vector3 point = new Vector3(
                    Mathf.Cos(angle) * radius,
                    waterLevel - Random.Range(4f, 8f),
                    Mathf.Sin(angle) * radius);
                SpawnShark(point);
            }
        }

        for (int i = 0; i < sharks.Count; i++)
        {
            SharkController shark = sharks[i];
            if (shark == null) continue;
            shark.SetHuntingEnabled(true);
            shark.ResetToPatrol();
        }

        Debug.Log("[Level1] 鲨鱼恢复自由游动与追杀，共 " + sharks.Count + " 条");
    }

    /// <summary>供测试/调试：临时开关鲨鱼狩猎。</summary>
    public void SetHuntingForAllSharks(bool hunting)
    {
        SetSharksHunting(hunting);
    }

    public SharkController GetActiveShark(int index)
    {
        int counter = 0;
        for (int i = 0; i < sharks.Count; i++)
        {
            if (sharks[i] == null) continue;
            if (counter == index) return sharks[i];
            counter++;
        }
        return null;
    }

    public ShrimpController GetActiveShrimp(int index)
    {
        int counter = 0;
        for (int i = 0; i < shrimps.Count; i++)
        {
            if (shrimps[i] == null) continue;
            if (counter == index) return shrimps[i];
            counter++;
        }
        return null;
    }

    void ClearCreatures()
    {
        for (int i = 0; i < sharks.Count; i++)
        {
            if (sharks[i] != null) Destroy(sharks[i].gameObject);
        }
        sharks.Clear();

        for (int i = 0; i < shrimps.Count; i++)
        {
            if (shrimps[i] != null) Destroy(shrimps[i].gameObject);
        }
        shrimps.Clear();
    }

    static int Triangular(int n)
    {
        return n * (n + 1) / 2;
    }

    static int CountAlive<T>(List<T> list) where T : Component
    {
        int count = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null) count++;
        }
        return count;
    }

    // ---------------- HUD ----------------

    void OnGUI()
    {
        DrawIntro();

        if (!ShowHud) return;

        // 字幕全部撤掉，只保留体力条（不显示文字）
        DrawStaminaBar();
    }

    /// <summary>第一关开场：显示 2 秒图片，淡入淡出。</summary>
    void DrawIntro()
    {
        if (introImage == null || introTimer >= introDuration) return;

        float fadeIn = Mathf.Clamp01(introTimer / 0.25f);
        float fadeOut = Mathf.Clamp01((introDuration - introTimer) / 0.35f);
        float alpha = Mathf.Min(fadeIn, fadeOut);

        Color previous = GUI.color;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), introImage, ScaleMode.ScaleAndCrop);

        GUI.color = previous;
    }

    void DrawStaminaBar()
    {
        const float pipWidth = 22f;
        const float pipHeight = 12f;
        const float gap = 5f;

        float totalWidth = maxStamina * pipWidth + (maxStamina - 1) * gap;
        float startX = (Screen.width - totalWidth) * 0.5f;
        float y = Screen.height - 74f;

        for (int i = 0; i < maxStamina; i++)
        {
            float filled = Mathf.Clamp01(stamina - i);
            Color color = filled > 0.02f
                ? Color.Lerp(new Color(1f, 0.42f, 0.35f), new Color(0.45f, 0.9f, 0.55f), (i + filled) / maxStamina)
                : new Color(1f, 1f, 1f, 0.18f);

            GUI.color = color;
            GUI.DrawTexture(new Rect(startX + i * (pipWidth + gap), y, pipWidth, pipHeight), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }

    static void DrawShadowed(Rect rect, string text, GUIStyle style)
    {
        Color previous = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.65f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
        style.normal.textColor = previous;
        GUI.Label(rect, text, style);
    }
}
