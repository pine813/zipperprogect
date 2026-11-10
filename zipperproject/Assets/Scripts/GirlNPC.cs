using UnityEngine;

/// <summary>
/// 沙滩上的女孩（girl.fbx）：始终慢慢转向玩家，并轻微上下浮动。
/// 碰撞判定用距离（由 Level2Director 调用），避免依赖物理层设置。
/// </summary>
public class GirlNPC : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Transform visual;
    [SerializeField] float touchRadius = 1.4f;
    [SerializeField] float turnSpeed = 2f;
    [SerializeField] float bobAmplitude = 0.03f;
    [SerializeField] float bobSpeed = 1.2f;
    /// <summary>gril.fbx 的正面朝 -Z，转向时补偿 180°，否则会背对玩家。</summary>
    [SerializeField] float facingOffsetDegrees = 180f;

    Vector3 basePosition;
    float phase;

    public float TouchRadius => touchRadius;
    public Transform Visual => visual;

    public void Configure(Transform playerTarget)
    {
        target = playerTarget;
    }

    public void ConfigureVisual(Transform visualRoot)
    {
        visual = visualRoot;
    }

    public void PlaceAt(Vector3 position)
    {
        transform.position = position;
        basePosition = position;
    }

    void Awake()
    {
        if (visual == null) visual = transform;
        basePosition = transform.position;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up)
                                  * Quaternion.Euler(0f, facingOffsetDegrees, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * dt);
            }
        }

        phase += dt * bobSpeed;
        Vector3 position = basePosition;
        position.y += Mathf.Sin(phase) * bobAmplitude;
        transform.position = position;
    }

    public bool IsTouching(Vector3 point)
    {
        Vector3 delta = point - transform.position;
        delta.y *= 0.5f;
        return delta.magnitude <= touchRadius;
    }
}
