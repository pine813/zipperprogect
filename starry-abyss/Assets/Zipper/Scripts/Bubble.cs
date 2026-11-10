using UnityEngine;

/// <summary>
/// 水泡：缓慢上升、轻微摇摆、到水面或超时后消散。
/// 小虾米被吃掉后变成它，小鱼死亡后的泡沫也用它。
/// </summary>
public class Bubble : MonoBehaviour
{
    [SerializeField] float riseSpeed = 0.9f;
    [SerializeField] float lifetime = 9f;
    [SerializeField] float wobble = 0.18f;
    [SerializeField] float growthPerSecond = 0.12f;
    [SerializeField] float waterLevel = 0f;

    float age;
    float phase;
    Vector3 baseScale = Vector3.one;

    public void Configure(float waterY, float life)
    {
        waterLevel = waterY;
        lifetime = life;
        baseScale = transform.localScale;
    }

    void Start()
    {
        phase = Random.Range(0f, Mathf.PI * 2f);
        if (baseScale == Vector3.zero) baseScale = Vector3.one;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        age += dt;
        phase += dt * 3.4f;

        Vector3 position = transform.position;
        position.y += riseSpeed * dt;
        position.x += Mathf.Sin(phase) * wobble * dt;
        position.z += Mathf.Cos(phase * 0.8f) * wobble * dt;
        transform.position = position;

        transform.localScale = baseScale * (1f + age * growthPerSecond);

        if (position.y >= waterLevel + 0.2f || age >= lifetime) Destroy(gameObject);
    }
}
