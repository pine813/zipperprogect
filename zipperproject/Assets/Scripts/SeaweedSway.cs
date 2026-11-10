using UnityEngine;

/// <summary>
/// 海草摇摆：整体轻微摆动 + 每片叶子错相位弯曲，看起来像被水流推动。
/// 只改旋转，不动位置，所以遮挡视线用的碰撞体仍然有效。
/// </summary>
public class SeaweedSway : MonoBehaviour
{
    [SerializeField] float amplitude = 7f;
    [SerializeField] float speed = 1.1f;
    [SerializeField] float bladePhaseStep = 0.7f;
    [SerializeField] Transform[] blades;

    Quaternion baseRotation;
    Quaternion[] bladeBaseRotations;
    float phaseOffset;

    public float Amplitude => amplitude;
    public float Speed => speed;
    public int BladeCount => blades != null ? blades.Length : 0;

    public void Configure(float amplitudeDegrees, float speedValue, Transform[] bladeTransforms)
    {
        amplitude = amplitudeDegrees;
        speed = speedValue;
        blades = bladeTransforms;
    }

    void Start()
    {
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        baseRotation = transform.rotation;

        if (blades != null)
        {
            bladeBaseRotations = new Quaternion[blades.Length];
            for (int i = 0; i < blades.Length; i++)
            {
                if (blades[i] != null) bladeBaseRotations[i] = blades[i].localRotation;
            }
        }
    }

    void Update()
    {
        float t = Time.time * speed + phaseOffset;

        // 整丛随水流方向轻摆
        float swayX = Mathf.Sin(t) * amplitude * 0.35f;
        float swayZ = Mathf.Cos(t * 0.8f) * amplitude * 0.22f;
        transform.rotation = baseRotation * Quaternion.Euler(swayX, 0f, swayZ);

        if (blades == null || bladeBaseRotations == null) return;

        for (int i = 0; i < blades.Length; i++)
        {
            if (blades[i] == null) continue;
            float bend = Mathf.Sin(t + i * bladePhaseStep) * amplitude;
            blades[i].localRotation = bladeBaseRotations[i] * Quaternion.Euler(bend, 0f, 0f);
        }
    }
}
