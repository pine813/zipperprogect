using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 第二关「沙滩」：
/// 第一关过关后自动接管 → 玩家以球体形态落在沙滩 → 撞开合贝壳里的珍珠 → 变成珍珠 → 碰到女孩 →
/// 屏幕上的《戴珍珠耳环的少女》缓慢放大，放满即通关。
/// </summary>
public class Level2Director : MonoBehaviour
{
    public enum Stage
    {
        Dormant,
        Beach,
        PearlFormed,
        Zooming,
        Cleared
    }

    [SerializeField] OceanPlayerController player;
    [SerializeField] Level1Director level1;
    [SerializeField] GirlNPC girl;
    const string ThirdChapterScene = "BlueFlightLevel";
    bool chapterFinished;
    [SerializeField] ClamController[] clams;

    [Header("流程")]
    [SerializeField] float startDelay = 1.4f;
    [SerializeField] float zoomDuration = 8f;
    [SerializeField] float beachRadius = 52f;
    [SerializeField] float girlMinRadius = 52f;
    [SerializeField] float girlMaxRadius = 62f;
    [SerializeField] float girlMinDistanceFromPlayer = 16f;
    [SerializeField] float girlRefreshInterval = 30f;
    [SerializeField] float respawnBelowY = -0.8f;
    [SerializeField] float titleDuration = 3.5f;

    [Header("结局图")]
    [SerializeField] Texture2D finaleImage;

    Stage stage = Stage.Dormant;
    float stageTimer;
    float zoomProgress;
    float girlRefreshTimer;
    int caughtCount;
    float titleTimer;
    Transform pearlHint;
    float hintPhase;
    float sharkBiteTimer;
    float fishGraceUntil;
    string toast = "";
    Vector3 playerBeachSpawn;
    bool usingPlaceholder;

    public Stage CurrentStage => stage;
    public float ZoomProgress => zoomProgress;
    public GirlNPC Girl => girl;
    public ClamController[] Clams => clams;
    public OceanPlayerController Player => player;
    public Vector3 PlayerBeachSpawn => playerBeachSpawn;
    public Texture2D FinaleImage => finaleImage;
    public float GirlRefreshInterval => girlRefreshInterval;
    public int CaughtCount => caughtCount;

    public void Configure(OceanPlayerController playerController, Level1Director level1Director,
        GirlNPC girlNpc, ClamController[] clamControllers, Texture2D image)
    {
        player = playerController;
        level1 = level1Director;
        girl = girlNpc;
        clams = clamControllers;
        finaleImage = image;
    }

    void Awake()
    {
        // 第一关期间女孩不能出现在沙滩上：她的碰撞体会把球顶住，让球上不了岸、第一关永远不结算
        SetGirlActive(false);
    }

    /// <summary>女孩只在第二关可见；激活时把她的碰撞体全部设成触发器，避免挡住球。</summary>
    void SetGirlActive(bool active)
    {
        if (girl == null) return;
        if (girl.gameObject.activeSelf != active) girl.gameObject.SetActive(active);
        if (!active) return;

        Collider[] colliders = girl.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].isTrigger = true;
        }
    }

    void Start()
    {
        Texture2D fromResources = Resources.Load<Texture2D>("GirlWithPearlEarring");
        if (fromResources != null)
        {
            finaleImage = fromResources;
        }
        else if (finaleImage == null)
        {
            finaleImage = BuildFallbackTexture();
            usingPlaceholder = true;
        }

        if (girl != null)
        {
            pearlHint = girl.transform.Find("PearlHint");
            if (pearlHint != null) pearlHint.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        stageTimer += dt;
        if (stage != Stage.Dormant && titleTimer < titleDuration) titleTimer += dt;

        // 被鲨鱼咬到后的泡沫死亡过程：只推进倒计时，其他逻辑暂停
        if (sharkBiteTimer > 0f)
        {
            UpdateSharkBite(dt);
            UpdatePearlHint(dt);
            return;
        }

        switch (stage)
        {
            case Stage.Dormant:
                if (level1 != null && level1.CurrentState == Level1Director.State.Cleared && stageTimer >= startDelay)
                {
                    BeginLevel2();
                }
                break;

            case Stage.Beach:
                UpdateBeach();
                break;

            case Stage.PearlFormed:
                UpdatePearlFormed();
                break;

            case Stage.Zooming:
                zoomProgress = Mathf.Clamp01(zoomProgress + dt / Mathf.Max(0.1f, zoomDuration));
                if (zoomProgress >= 1f)
                {
                    stage = Stage.Cleared;
                    toast = "通关！";
                    Debug.Log("[Level2] 结局画面放大完成，通关");
                    GoToNextChapter();
                }
                break;

            case Stage.Cleared:
                break;
        }

        UpdatePearlHint(dt);

        if (stage != Stage.Dormant)
        {
            UpdateWaterRule();
            UpdateSharkBite(dt);
        }
    }

    /// <summary>小鱼在水里被鲨鱼碰到 → 变泡沫死亡，再回到沙滩。</summary>
    void UpdateSharkBite(float dt)
    {
        if (sharkBiteTimer > 0f)
        {
            sharkBiteTimer -= dt;
            if (sharkBiteTimer <= 0f) RespawnAfterSharkBite();
            return;
        }

        if (stage != Stage.Beach && stage != Stage.PearlFormed) return;
        if (player == null || level1 == null) return;
        if (!player.IsSwimming) return;
        if (Time.time < fishGraceUntil) return;

        int count = level1.ActiveSharkCount;
        for (int i = 0; i < count; i++)
        {
            SharkController shark = level1.GetActiveShark(i);
            if (shark == null) continue;

            Vector3 delta = shark.transform.position - player.transform.position;
            if (delta.magnitude <= shark.ContactRadius)
            {
                BeginSharkBite();
                return;
            }
        }
    }

    void BeginSharkBite()
    {
        sharkBiteTimer = 1.4f;
        player.SetInputEnabled(false);
        player.BeginSink();
        player.SetVisualsVisible(false);

        if (level1 != null) level1.SpawnFoamAt(player.transform.position);
        GameAudio.PlaySplash();
        toast = "被鲨鱼咬了……变成泡沫";
        Debug.Log("[Level2] 小鱼被鲨鱼咬到，泡沫死亡");
    }

    void RespawnAfterSharkBite()
    {
        player.SetVisualsVisible(true);
        player.Teleport(playerBeachSpawn);
        player.EnterBeach();
        player.SetInputEnabled(true);
        toast = "在沙滩上重新变回球体";
        Debug.Log("[Level2] 泡沫消散，回到沙滩出生点");
    }

    /// <summary>第二关里球体碰到水就变小鱼，回到岸上再变回球；同时让鲨鱼恢复扫描/追杀。</summary>
    void UpdateWaterRule()
    {
        if (player == null || level1 == null) return;

        float waterLevel = level1.WaterLevel;
        float y = player.transform.position.y;

        // 回到岸上（明显高于水面）→ 变回球体
        if (player.IsSwimming && y > waterLevel + 0.6f)
        {
            player.EnterBeach();
            toast = "回到沙滩，变回球体";
            return;
        }

        bool touchingWater = y <= waterLevel + 0.05f;
        if (!touchingWater) return;
        if (player.IsSwimming || player.CurrentVisual != PlayerVisual.Ball) return;

        player.EnterWater();
        level1.ResumeSharkHunting();
        fishGraceUntil = Time.time + 1.5f;
        toast = "碰到水，变成小鱼了！小心鲨鱼";
        Debug.Log("[Level2] 球碰到水 → 变成小鱼，鲨鱼恢复追杀");
    }

    /// <summary>沙滩阶段在女孩头顶浮一颗珍珠：不加文字也能看懂“要带珍珠来”。</summary>
    void UpdatePearlHint(float dt)
    {
        if (pearlHint == null) return;

        bool show = stage == Stage.Beach;
        if (pearlHint.gameObject.activeSelf != show) pearlHint.gameObject.SetActive(show);
        if (!show) return;

        hintPhase += dt;
        pearlHint.localPosition = new Vector3(0f, 2.15f + Mathf.Sin(hintPhase * 2.2f) * 0.12f, 0f);
        pearlHint.Rotate(Vector3.up, 70f * dt, Space.Self);
    }

    /// <summary>开始第二关：球体落到沙滩，贝壳开始工作，女孩随机出现。</summary>
    public void BeginLevel2()
    {
        if (player == null) return;

        playerBeachSpawn = ResolveBeachSpawn();
        player.Teleport(playerBeachSpawn);
        player.EnterBeach();
        player.SetInputEnabled(true);
        player.SetAutoReturnToGround(true);

        SetGirlActive(true);
        PlaceGirlRandomly();
        ResetClams();

        // 进入第二关：鲨鱼恢复自由游动，并重新可以扫描/追杀小鱼
        if (level1 != null) level1.ResumeSharkHunting();

        if (level1 != null) level1.ShowHud = false;

        stage = Stage.Beach;
        stageTimer = 0f;
        zoomProgress = 0f;
        girlRefreshTimer = 0f;
        caughtCount = 0;
        titleTimer = 0f;
        toast = "第二关：沙滩 —— 等贝壳张开，抢珍珠";

        Debug.Log("[Level2] 第二关开始，出生点 " + playerBeachSpawn + "，女孩在 " + (girl != null ? girl.transform.position.ToString() : "无"));
    }

    void UpdateBeach()
    {
        if (CheckFellIntoWater()) return;
        UpdateGirlRefresh();

        if (clams == null) return;

        for (int i = 0; i < clams.Length; i++)
        {
            if (clams[i] == null) continue;
            if (clams[i].TryCollect(player.transform.position))
            {
                OnPearlCollected();
                return;
            }
        }

        // 还是球体形态时，贴着正在合上/已合上的贝壳会被夹住，弹回本关出生点
        if (player.CurrentVisual == PlayerVisual.Ball)
        {
            for (int i = 0; i < clams.Length; i++)
            {
                ClamController clam = clams[i];
                if (clam == null || !clam.IsDangerous) continue;

                Vector3 delta = clam.transform.position - player.transform.position;
                delta.y *= 0.5f;
                if (delta.magnitude <= clam.CatchRadius)
                {
                    OnCaughtByClam();
                    return;
                }
            }
        }
    }

    void UpdateGirlRefresh()
    {
        girlRefreshTimer += Time.deltaTime;
        if (girlRefreshTimer >= girlRefreshInterval)
        {
            girlRefreshTimer = 0f;
            RefreshGirlPosition();
            toast = "女孩换了个位置！";
        }
    }

    /// <summary>把女孩随机挪到沙滩上的新位置（每 30 秒触发一次）。</summary>
    public void RefreshGirlPosition()
    {
        PlaceGirlRandomly();
        Debug.Log("[Level2] 女孩刷新到 " + (girl != null ? girl.transform.position.ToString() : "无"));
    }

    void OnCaughtByClam()
    {
        caughtCount++;
        player.Teleport(playerBeachSpawn);
        player.SetVisual(PlayerVisual.Ball);
        toast = "被贝壳夹住了！回到沙滩起点";
        Debug.Log("[Level2] 被贝壳夹住，回到出生点（第 " + caughtCount + " 次）");
    }

    void UpdatePearlFormed()
    {
        if (CheckFellIntoWater()) return;
        UpdateGirlRefresh();

        if (girl != null && girl.IsTouching(player.transform.position))
        {
            BeginFinale();
        }
    }

    bool CheckFellIntoWater()
    {
        if (player.IsSwimming) return false;
        if (player.transform.position.y > respawnBelowY) return false;

        player.Teleport(playerBeachSpawn);
        return true;
    }

    /// <summary>第二关结束：有第三关场景就加载它，否则退回通知外壳。</summary>
    void GoToNextChapter()
    {
        if (chapterFinished) return;
        chapterFinished = true;

        if (Application.CanStreamedLevelBeLoaded(ThirdChapterScene))
        {
            Debug.Log("[Level2] 进入第三关：" + ThirdChapterScene);
            SceneManager.LoadScene(ThirdChapterScene);
            return;
        }

        WebBridge.NotifyGameComplete();
    }

    void OnPearlCollected()
    {
        player.SetVisual(PlayerVisual.Pearl);
        stage = Stage.PearlFormed;
        stageTimer = 0f;
        toast = "你变成了珍珠！去找那个女孩";
        GameAudio.PlayPearl();
        Debug.Log("[Level2] 撞到珍珠，玩家变为珍珠形态");
    }

    void BeginFinale()
    {
        stage = Stage.Zooming;
        stageTimer = 0f;
        zoomProgress = 0f;
        toast = "";
        player.SetInputEnabled(false);
        player.SetVisual(PlayerVisual.Pearl);
        Debug.Log("[Level2] 碰到女孩，结局画面开始放大");
    }

    void ResetClams()
    {
        if (clams == null) return;
        for (int i = 0; i < clams.Length; i++)
        {
            if (clams[i] != null) clams[i].ResetClam();
        }
    }

    Vector3 ResolveBeachSpawn()
    {
        Vector3 arrival = player.transform.position;
        float angle = Mathf.Atan2(arrival.z, arrival.x);
        Vector3 spawn = SampleGround(beachRadius, angle);
        spawn.y += 0.6f;
        return spawn;
    }

    void PlaceGirlRandomly()
    {
        if (girl == null) return;

        // 参照玩家“当前”位置，而不是记录下来的出生点
        Vector3 playerPos = player != null ? player.transform.position : playerBeachSpawn;

        // 全部候选都比一遍，取离玩家最远的（不再提前 break，避免刚好 16 米就贴脸）
        Vector3 best = Vector3.zero;
        float bestDistance = -1f;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(girlMinRadius, girlMaxRadius);
            Vector3 candidate = SampleGround(radius, angle);
            float distance = Vector3.Distance(candidate, playerPos);

            if (distance > bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        // 兜底：还不够 30 米就放到玩家正对面
        if (bestDistance < Mathf.Max(30f, girlMinDistanceFromPlayer))
        {
            float angle = Mathf.Atan2(playerPos.z, playerPos.x) + Mathf.PI;
            best = SampleGround((girlMinRadius + girlMaxRadius) * 0.5f, angle);
            bestDistance = Vector3.Distance(best, playerPos);
        }

        girl.PlaceAt(best);
        Debug.Log("[Level2] 女孩放在距玩家 " + bestDistance.ToString("F1") + " 米处，玩家 "
                  + playerPos.ToString("F1") + "，女孩 " + best.ToString("F1"));
    }

    static Vector3 SampleGround(float radius, float angle)
    {
        Vector3 origin = new Vector3(Mathf.Cos(angle) * radius, 40f, Mathf.Sin(angle) * radius);
        RaycastHit hit;
        if (Physics.Raycast(origin, Vector3.down, out hit, 90f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        return new Vector3(origin.x, 1f, origin.z);
    }

    static Texture2D BuildFallbackTexture()
    {
        Texture2D texture = new Texture2D(320, 400, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float v = y / (float)texture.height;
                texture.SetPixel(x, y, Color.Lerp(new Color(0.08f, 0.12f, 0.14f), new Color(0.30f, 0.22f, 0.14f), v));
            }
        }
        texture.Apply();
        return texture;
    }

    void OnGUI()
    {
        if (stage == Stage.Dormant) return;

        DrawTitleCard();

        if (stage == Stage.Zooming || stage == Stage.Cleared) DrawFinale();
    }

    /// <summary>第二关开场字幕：WHERE IS MY PEARL（淡入停留再淡出）。</summary>
    void DrawTitleCard()
    {
        if (titleTimer >= titleDuration) return;

        float fadeIn = Mathf.Clamp01(titleTimer / 0.4f);
        float fadeOut = Mathf.Clamp01((titleDuration - titleTimer) / 0.7f);
        float alpha = Mathf.Min(fadeIn, fadeOut);

        Color previous = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 46;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), "WHERE IS MY PEARL", style);

        GUI.color = previous;
    }

    void DrawFinale()
    {
        float t = Mathf.SmoothStep(0f, 1f, zoomProgress);

        float backdropAlpha = Mathf.Lerp(0f, 0.95f, Mathf.Clamp01(t * 1.3f));
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, backdropAlpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        if (finaleImage != null)
        {
            float aspect = finaleImage.width / (float)finaleImage.height;
            float height = Mathf.Lerp(Screen.height * 0.16f, Screen.height, t);
            float width = height * aspect;
            Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUI.color = Color.white;
            GUI.DrawTexture(rect, finaleImage, ScaleMode.ScaleToFit);
        }

        GUI.color = previous;

        if (usingPlaceholder)
        {
            GUIStyle hint = new GUIStyle(GUI.skin.label);
            hint.fontSize = 14;
            hint.alignment = TextAnchor.LowerCenter;
            hint.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
            GUI.Label(new Rect(0f, Screen.height - 46f, Screen.width, 34f),
                "占位图：把《戴珍珠耳环的少女》放到 Assets/Resources/GirlWithPearlEarring.png 即可替换", hint);
        }

        if (stage == Stage.Cleared)
        {
            GUIStyle win = new GUIStyle(GUI.skin.label);
            win.fontSize = 34;
            win.alignment = TextAnchor.UpperCenter;
            win.normal.textColor = Color.white;
            DrawShadowed(new Rect(0f, 24f, Screen.width, 48f), "通关！", win);
        }
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
