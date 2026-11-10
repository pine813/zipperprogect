using System;
using UnityEngine;

/// <summary>
/// 小虾米：在出生点附近慢悠悠乱游。小鱼碰到它就被吃掉（给玩家 +1 体力），
/// 然后它化成一个水泡慢慢升到水面。
/// </summary>
public class ShrimpController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float wanderRadius = 6f;
    [SerializeField] float swimSpeed = 1.7f;
    [SerializeField] float collectRadius = 1.0f;
    [SerializeField] float waterLevel = 0f;
    [SerializeField] GameObject bubblePrefab;
    [SerializeField] Transform tail;

    Vector3 home;
    Vector3 destination;
    float retargetTimer;
    float wiggle;
    Action<ShrimpController> onCollected;
    bool collected;

    // 出生螺旋
    bool spiraling;
    Vector3 spiralCenter;
    float spiralAngle;
    float spiralRadius;
    float spiralRadialSpeed;
    float spiralAngularSpeed;
    float spiralTimer;
    float spiralDuration;
    float spiralDepth;

    public float CollectRadius => collectRadius;
    public bool IsCollected => collected;
    public bool IsSpiraling => spiraling;

    /// <summary>刚由水花变成小虾米时，螺旋散开一小段再自由游动。</summary>
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
        spiralAngle = Mathf.Atan2(transform.position.x - center.x, transform.position.z - center.z) * Mathf.Rad2Deg;
    }

    public void Configure(Transform playerTarget, float waterY, GameObject bubbleTemplate)
    {
        target = playerTarget;
        waterLevel = waterY;
        bubblePrefab = bubbleTemplate;
    }

    public void ConfigureVisual(Transform tailTransform)
    {
        tail = tailTransform;
    }

    public void SetCollectedCallback(Action<ShrimpController> callback)
    {
        onCollected = callback;
    }

    void Start()
    {
        home = transform.position;
        PickDestination();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || collected) return;

        if (spiraling)
        {
            UpdateSpiral(dt);
            UpdateTail(dt);
            return;
        }

        retargetTimer -= dt;
        if (retargetTimer <= 0f) PickDestination();

        Vector3 toDestination = destination - transform.position;
        if (toDestination.sqrMagnitude > 0.09f)
        {
            Vector3 direction = toDestination.normalized;
            transform.position += direction * swimSpeed * dt;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), 4f * dt);
        }

        if (target != null && Vector3.Distance(target.position, transform.position) <= collectRadius)
        {
            Collect();
            return;
        }

        if (tail != null) UpdateTail(dt);
    }

    void UpdateTail(float dt)
    {
        wiggle += dt * 9f;
        tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(wiggle) * 18f, 0f);
    }

    void UpdateSpiral(float dt)
    {
        spiralTimer += dt;
        spiralAngle += spiralAngularSpeed * dt;
        spiralRadius += spiralRadialSpeed * dt;

        float radians = spiralAngle * Mathf.Deg2Rad;
        Vector3 target = spiralCenter
                         + new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * spiralRadius
                         + Vector3.up * (spiralDepth + Mathf.Sin(spiralTimer * 2.6f) * 0.5f);

        Vector3 toTarget = target - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Vector3 direction = toTarget.normalized;
            transform.position += direction * Mathf.Min(toTarget.magnitude, 9f * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), 6f * dt);
        }

        if (spiralTimer >= spiralDuration)
        {
            spiraling = false;
            home = transform.position;
            PickDestination();
        }
    }

    void PickDestination()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * wanderRadius;
        destination = home + new Vector3(offset.x, UnityEngine.Random.Range(-1.5f, 1.5f), offset.y);
        destination.y = Mathf.Clamp(destination.y, waterLevel - 13f, waterLevel - 0.6f);
        retargetTimer = UnityEngine.Random.Range(1.5f, 3.2f);
    }

    /// <summary>被小鱼吃掉：变水泡，通知导演加体力。</summary>
    public void Collect()
    {
        if (collected) return;
        collected = true;

        if (bubblePrefab != null)
        {
            GameObject bubbleObject = Instantiate(bubblePrefab, transform.position, Quaternion.identity);
            Bubble bubble = bubbleObject.GetComponent<Bubble>();
            if (bubble != null) bubble.Configure(waterLevel, 8f);
        }

        if (onCollected != null) onCollected(this);
        Destroy(gameObject);
    }
}
