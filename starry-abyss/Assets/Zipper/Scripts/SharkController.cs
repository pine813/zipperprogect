using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 鲨鱼：巡逻 + 视线锥检测 + 看到玩家后的闪红警告与锁定追击。
/// 视线锥从眼睛发出，落在物体上的位置显示一个半透明圆圈；视线会被海草/礁石挡住。
/// </summary>
public class SharkController : MonoBehaviour
{
    enum Phase
    {
        Patrol,
        Alert,
        Chase
    }

    [SerializeField] Transform target;
    [SerializeField] float patrolRadius = 30f;
    [SerializeField] float patrolDepth = -6f;
    [SerializeField] float patrolAngularSpeed = 8f;
    [SerializeField] float patrolSpeed = 4.5f;
    [SerializeField] float chaseSpeed = 10f;
    [SerializeField] float acceleration = 4f;

    [Header("视线")]
    [SerializeField] float visionRange = 15f;
    [SerializeField] float visionHalfAngle = 26f;
    [SerializeField] float alertDuration = 0.9f;
    [SerializeField] float chaseMaxDuration = 9f;
    [SerializeField] float loseDistance = 28f;
    [SerializeField] float contactRadius = 1.7f;

    [SerializeField] Transform tail;
    [SerializeField] float tailSpeed = 5f;
    [SerializeField] float tailAngle = 22f;

    Phase phase = Phase.Patrol;
    float angle;
    float wiggle;
    float phaseTimer;
    float chaseTimer;
    Vector3 velocity;
    bool huntingEnabled = true;
    bool seesTarget;

    // 出生螺旋
    bool spiraling;
    Vector3 spiralCenter;
    float spiralRadius;
    float spiralRadialSpeed;
    float spiralAngularSpeed;
    float spiralTimer;
    float spiralDuration;
    float spiralDepth;
    float spiralAngle;

    GameObject coneObject;
    Material coneMaterial;
    GameObject markerObject;
    Material markerMaterial;
    Material eyeMaterial;
    Color eyeBaseColor = new Color(0.02f, 0.02f, 0.03f);

    static Texture2D ringTexture;
    static Mesh cachedConeMesh;
    static Mesh cachedMarkerMesh;

    public bool IsAlerting => phase == Phase.Alert;
    public bool IsChasing => phase == Phase.Chase;
    public bool SeesTarget => seesTarget;
    public float ContactRadius => contactRadius;
    public float VisionRange => visionRange;

    public void Configure(Transform playerTarget, float radius, float depth, float angularSpeed)
    {
        target = playerTarget;
        patrolRadius = Mathf.Max(6f, radius);
        patrolDepth = depth;
        patrolAngularSpeed = angularSpeed;
        angle = Mathf.Atan2(transform.position.x, transform.position.z) * Mathf.Rad2Deg;
    }

    public void ConfigureVisual(Transform tailTransform)
    {
        tail = tailTransform;
    }

    public void SetHuntingEnabled(bool enabled)
    {
        huntingEnabled = enabled;
        if (!enabled)
        {
            phase = Phase.Patrol;
            phaseTimer = 0f;
            chaseTimer = 0f;
        }
    }

    /// <summary>刚由水花变成鲨鱼时，绕着海洋中心螺旋散开一小段，再进入巡逻。</summary>
    public void BeginSpiralIntro(Vector3 center, float radialSpeed, float angularSpeed, float duration, float depth)
    {
        spiraling = true;
        spiralCenter = center;
        spiralRadius = new Vector2(transform.position.x - center.x, transform.position.z - center.z).magnitude;
        spiralRadialSpeed = radialSpeed;
        spiralAngularSpeed = angularSpeed;
        spiralDuration = Mathf.Max(0.1f, duration);
        spiralTimer = 0f;
        spiralDepth = depth;
        spiralAngle = Mathf.Atan2(transform.position.x - center.x, transform.position.z - center.z);
    }

    public bool IsSpiraling => spiraling;

    void Awake()
    {
        BuildVisionVisuals();
        CollectEyes();
    }

    void Start()
    {
        if (target == null)
        {
            OceanPlayerController player = FindObjectOfType<OceanPlayerController>();
            if (player != null) target = player.transform;
        }
    }

    Vector3 PatrolPoint()
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * patrolRadius
               + Vector3.up * (patrolDepth + Mathf.Sin(radians * 1.7f) * 0.9f);
    }

    void UpdateSpiral(float dt)
    {
        spiralTimer += dt;
        spiralAngle += spiralAngularSpeed * dt;
        spiralRadius += spiralRadialSpeed * dt;

        float radians = spiralAngle * Mathf.Deg2Rad;
        Vector3 target = spiralCenter
                         + new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * spiralRadius
                         + Vector3.up * (spiralDepth + Mathf.Sin(spiralTimer * 2.2f) * 0.8f);

        Vector3 toTarget = target - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Vector3 direction = toTarget.normalized;
            transform.position += direction * Mathf.Min(toTarget.magnitude, 16f * dt);
            Quaternion look = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 6f * dt);
        }

        if (spiralTimer >= spiralDuration)
        {
            spiraling = false;
            patrolRadius = Mathf.Max(8f, spiralRadius);
            patrolDepth = spiralDepth;
            angle = spiralAngle;
            velocity = Vector3.zero;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (spiraling)
        {
            UpdateSpiral(dt);
            UpdateTail(dt);
            UpdateVisionVisuals();
            seesTarget = false;
            return;
        }

        angle += patrolAngularSpeed * dt;
        seesTarget = huntingEnabled && target != null && CanSeeTarget();

        switch (phase)
        {
            case Phase.Patrol:
                MoveTowards(PatrolPoint(), patrolSpeed, dt);
                if (seesTarget) EnterAlert();
                break;

            case Phase.Alert:
                phaseTimer += dt;
                velocity = Vector3.Lerp(velocity, Vector3.zero, 8f * dt);
                transform.position += velocity * dt;
                if (phaseTimer >= alertDuration) EnterChase();
                break;

            case Phase.Chase:
                chaseTimer += dt;
                if (target != null) MoveTowards(target.position, chaseSpeed, dt);
                if (chaseTimer >= chaseMaxDuration ||
                    (target != null && Vector3.Distance(transform.position, target.position) > loseDistance))
                {
                    ReturnToPatrol();
                }
                break;
        }

        UpdateTail(dt);
        UpdateVisionVisuals();
    }

    void MoveTowards(Vector3 destination, float speed, float dt)
    {
        Vector3 toDestination = destination - transform.position;
        Vector3 desired = toDestination.sqrMagnitude > 0.01f ? toDestination.normalized * speed : Vector3.zero;

        velocity = Vector3.Lerp(velocity, desired, acceleration * dt);
        transform.position += velocity * dt;

        if (velocity.sqrMagnitude > 0.05f)
        {
            Quaternion look = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 5f * dt);
        }
    }

    void UpdateTail(float dt)
    {
        if (tail == null) return;
        wiggle += dt * tailSpeed * (1f + velocity.magnitude * 0.1f);
        tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(wiggle) * tailAngle, 0f);
    }

    void EnterAlert()
    {
        phase = Phase.Alert;
        phaseTimer = 0f;
        velocity *= 0.25f;
    }

    void EnterChase()
    {
        phase = Phase.Chase;
        chaseTimer = 0f;
    }

    void ReturnToPatrol()
    {
        phase = Phase.Patrol;
        chaseTimer = 0f;
    }

    Vector3 EyesPosition()
    {
        float scaleZ = Mathf.Max(1f, transform.lossyScale.z);
        float scaleY = Mathf.Max(1f, transform.lossyScale.y);
        return transform.position + transform.forward * (0.75f * scaleZ) + Vector3.up * (0.12f * scaleY);
    }

    bool CanSeeTarget()
    {
        if (target == null) return false;

        Vector3 eyes = EyesPosition();
        Vector3 toTarget = target.position - eyes;
        float distance = toTarget.magnitude;
        if (distance < 0.05f || distance > visionRange) return false;
        if (Vector3.Angle(transform.forward, toTarget) > visionHalfAngle) return false;

        // 视线遮挡：海草、礁石、地形都能挡住
        RaycastHit[] hits = Physics.RaycastAll(eyes, toTarget / distance, distance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null) continue;
            if (hitCollider.transform.IsChildOf(transform)) continue;
            if (hitCollider.transform.IsChildOf(target)) continue;
            if (hits[i].distance < distance - 0.4f) return false;
        }

        return true;
    }

    // ---------------- 视线可视化 ----------------

    void BuildVisionVisuals()
    {
        // 鲨鱼本体放大了，圆锥/圆圈是它的子物体，需要反向补偿缩放，
        // 否则视线锥会跟着放大成两倍长。
        Vector3 sharkScale = transform.lossyScale;
        float sx = Mathf.Approximately(sharkScale.x, 0f) ? 1f : sharkScale.x;
        float sy = Mathf.Approximately(sharkScale.y, 0f) ? 1f : sharkScale.y;
        float sz = Mathf.Approximately(sharkScale.z, 0f) ? 1f : sharkScale.z;

        float lengthScale = visionRange / 15f;
        float radialScale = Mathf.Tan(visionHalfAngle * Mathf.Deg2Rad) / Mathf.Tan(26f * Mathf.Deg2Rad);
        coneObject = new GameObject("VisionCone");
        coneObject.transform.SetParent(transform, false);
        coneObject.transform.localPosition = new Vector3(0f, 0.1f / sy, 0.4f / sz);
        coneObject.transform.localScale = new Vector3(radialScale / sx, radialScale / sy, lengthScale / sz);

        MeshFilter coneFilter = coneObject.AddComponent<MeshFilter>();
        coneFilter.sharedMesh = GetConeMesh();

        MeshRenderer coneRenderer = coneObject.AddComponent<MeshRenderer>();
        Shader coneShader = Shader.Find("Zipper/VisionCone");
        coneMaterial = new Material(coneShader != null ? coneShader : Shader.Find("Unlit/Color"));
        coneMaterial.SetFloat("_Range", visionRange);
        coneMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0.24f));
        coneRenderer.sharedMaterial = coneMaterial;
        coneRenderer.shadowCastingMode = ShadowCastingMode.Off;
        coneRenderer.receiveShadows = false;

        markerObject = new GameObject("VisionMarker");
        markerObject.transform.SetParent(transform, false);
        markerObject.transform.localScale = new Vector3(1f / sx, 1f / sy, 1f / sz);

        MeshFilter markerFilter = markerObject.AddComponent<MeshFilter>();
        markerFilter.sharedMesh = GetMarkerMesh();

        MeshRenderer markerRenderer = markerObject.AddComponent<MeshRenderer>();
        Shader markerShader = Shader.Find("Zipper/VisionMarker");
        markerMaterial = new Material(markerShader != null ? markerShader : Shader.Find("Unlit/Transparent"));
        markerMaterial.mainTexture = GetRingTexture();
        markerMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0.55f));
        markerRenderer.sharedMaterial = markerMaterial;
        markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
        markerRenderer.receiveShadows = false;
        markerObject.SetActive(false);
    }

    void CollectEyes()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Material sharedEyeMaterial = null;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].gameObject.name.StartsWith("Eye")) continue;
            if (sharedEyeMaterial == null && renderers[i].sharedMaterial != null)
            {
                sharedEyeMaterial = renderers[i].sharedMaterial;
                eyeBaseColor = sharedEyeMaterial.color;
                eyeMaterial = new Material(sharedEyeMaterial);
            }
            if (eyeMaterial != null) renderers[i].sharedMaterial = eyeMaterial;
        }
    }

    void UpdateVisionVisuals()
    {
        bool danger = phase == Phase.Alert || phase == Phase.Chase;
        float alpha = 0.24f;
        Color eyeColor = eyeBaseColor;

        if (phase == Phase.Alert)
        {
            // 快速闪三次
            bool flashOn = Mathf.Repeat(phaseTimer * 20f, 1f) > 0.5f;
            alpha = flashOn ? 0.65f : 0.12f;
            eyeColor = flashOn ? new Color(1f, 0.12f, 0.10f) : new Color(0.55f, 0.04f, 0.04f);
        }
        else if (phase == Phase.Chase)
        {
            alpha = 0.55f;
            eyeColor = new Color(1f, 0.1f, 0.08f);
        }

        if (coneMaterial != null)
        {
            coneMaterial.SetColor("_Color", danger
                ? new Color(1f, 0.16f, 0.12f, alpha)
                : new Color(1f, 1f, 1f, alpha));
        }

        if (markerMaterial != null)
        {
            markerMaterial.SetColor("_Color", danger
                ? new Color(1f, 0.22f, 0.18f, 0.8f)
                : new Color(1f, 1f, 1f, 0.55f));
        }

        if (eyeMaterial != null) eyeMaterial.color = eyeColor;

        UpdateMarker();
    }

    void UpdateMarker()
    {
        if (markerObject == null) return;

        Vector3 eyes = EyesPosition();
        RaycastHit hit;
        if (Physics.Raycast(eyes, transform.forward, out hit, visionRange, ~0, QueryTriggerInteraction.Ignore) &&
            !hit.collider.transform.IsChildOf(transform))
        {
            markerObject.SetActive(true);
            markerObject.transform.position = hit.point + hit.normal * 0.04f;
            markerObject.transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
        }
        else
        {
            markerObject.SetActive(false);
        }
    }

    static Mesh GetConeMesh()
    {
        if (cachedConeMesh != null) return cachedConeMesh;

        const int segments = 26;
        const int rings = 8;
        const float range = 15f;
        float radiusScale = Mathf.Tan(26f * Mathf.Deg2Rad);

        Vector3[] vertices = new Vector3[1 + rings * segments];
        int[] triangles = new int[segments * 3 + (rings - 1) * segments * 6];

        vertices[0] = Vector3.zero;
        int index = 1;
        for (int ring = 1; ring <= rings; ring++)
        {
            float z = range * ring / rings;
            float radius = z * radiusScale;
            for (int s = 0; s < segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                vertices[index++] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, z);
            }
        }

        int t = 0;
        // 锥尖扇形
        for (int s = 0; s < segments; s++)
        {
            int next = (s + 1) % segments;
            triangles[t++] = 0;
            triangles[t++] = 1 + next;
            triangles[t++] = 1 + s;
        }
        // 环与环之间
        for (int ring = 0; ring < rings - 1; ring++)
        {
            int startA = 1 + ring * segments;
            int startB = 1 + (ring + 1) * segments;
            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;
                triangles[t++] = startA + s;
                triangles[t++] = startB + s;
                triangles[t++] = startB + next;

                triangles[t++] = startA + s;
                triangles[t++] = startB + next;
                triangles[t++] = startA + next;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "VisionCone";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        cachedConeMesh = mesh;
        return mesh;
    }

    static Mesh GetMarkerMesh()
    {
        if (cachedMarkerMesh != null) return cachedMarkerMesh;

        const int segments = 40;
        const float radius = 1.1f;

        Vector3[] vertices = new Vector3[1 + segments];
        Vector2[] uvs = new Vector2[1 + segments];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int s = 0; s < segments; s++)
        {
            float a = s / (float)segments * Mathf.PI * 2f;
            vertices[s + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            uvs[s + 1] = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
        }

        for (int s = 0; s < segments; s++)
        {
            int next = (s + 1) % segments;
            triangles[s * 3] = 0;
            triangles[s * 3 + 1] = next + 1;
            triangles[s * 3 + 2] = s + 1;
        }

        Mesh mesh = new Mesh();
        mesh.name = "VisionMarker";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        cachedMarkerMesh = mesh;
        return mesh;
    }

    static Texture2D GetRingTexture()
    {
        if (ringTexture != null) return ringTexture;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);

                float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.78f) / 0.16f);
                float fill = Mathf.Clamp01(1f - r) * 0.22f;
                float alpha = Mathf.Max(ring, fill) * Mathf.Clamp01((1f - r) * 8f);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        ringTexture = texture;
        return texture;
    }
}
