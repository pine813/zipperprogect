using UnityEngine;

/// <summary>
/// 沙滩贝壳：闭合/张开循环，张开时有一定概率吐出一颗珍珠。
/// 珍珠只在张开期间可以被撞到，闭合会收回（珍珠数量不限，可以反复取）。
/// </summary>
public class ClamController : MonoBehaviour
{
    enum Phase
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    [SerializeField] Transform lid;
    [SerializeField] GameObject pearl;
    [SerializeField] float openAngle = 62f;
    [SerializeField] float openDuration = 0.22f;
    [SerializeField] float closedMin = 0.7f;
    [SerializeField] float closedMax = 1.8f;
    [SerializeField] float openMin = 1.0f;
    [SerializeField] float openMax = 2.0f;
    [SerializeField] float pearlChance = 0.5f;
    [SerializeField] float collectRadius = 1.2f;
    [SerializeField] float catchRadius = 1.1f;

    Phase phase = Phase.Closed;
    float timer;
    float phaseLength = 1f;
    bool hasPearl;

    public bool PearlAvailable => phase == Phase.Open && hasPearl;
    public float CollectRadius => collectRadius;
    public float CatchRadius => catchRadius;
    public float PearlChance => pearlChance;
    public Transform PearlTransform => pearl != null ? pearl.transform : null;
    public string StatusText => phase + (hasPearl ? "+pearl" : "");

    /// <summary>正在合上或已合上 —— 此时贴着贝壳会被夹住。</summary>
    public bool IsDangerous => phase == Phase.Closing || phase == Phase.Closed;

    public void Configure(Transform lidTransform, GameObject pearlObject, float sizeScale)
    {
        lid = lidTransform;
        pearl = pearlObject;

        // 贝壳整体放大了，拾取/夹人范围也跟着放大
        float scale = Mathf.Max(0.2f, sizeScale);
        collectRadius = 1.2f * scale;
        catchRadius = 1.1f * scale;
    }

    void Start()
    {
        ResetClam();
    }

    public void ResetClam()
    {
        phase = Phase.Closed;
        timer = 0f;
        phaseLength = Random.Range(closedMin, closedMax);
        hasPearl = false;
        ApplyLid(0f);
        if (pearl != null) pearl.SetActive(false);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        timer += dt;

        switch (phase)
        {
            case Phase.Closed:
                if (timer >= phaseLength) EnterOpening();
                break;

            case Phase.Opening:
                float openT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / Mathf.Max(0.01f, openDuration)));
                ApplyLid(openT);
                if (timer >= openDuration)
                {
                    phase = Phase.Open;
                    timer = 0f;
                    phaseLength = Random.Range(openMin, openMax);
                    TrySpawnPearl();
                }
                break;

            case Phase.Open:
                if (timer >= phaseLength)
                {
                    // 一开始合上就把珍珠收掉：从外面无法判断贝壳里有没有珍珠
                    DespawnPearl();
                    phase = Phase.Closing;
                    timer = 0f;
                }
                break;

            case Phase.Closing:
                float closeT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / Mathf.Max(0.01f, openDuration)));
                ApplyLid(1f - closeT);
                if (timer >= openDuration)
                {
                    phase = Phase.Closed;
                    timer = 0f;
                    phaseLength = Random.Range(closedMin, closedMax);
                    DespawnPearl();
                }
                break;
        }
    }

    void EnterOpening()
    {
        phase = Phase.Opening;
        timer = 0f;
    }

    void ApplyLid(float open01)
    {
        if (lid == null) return;
        lid.localRotation = Quaternion.Euler(-openAngle * open01, 0f, 0f);
    }

    void TrySpawnPearl()
    {
        if (pearl == null || hasPearl) return;
        if (Random.value > pearlChance) return;

        hasPearl = true;
        pearl.SetActive(true);
    }

    void DespawnPearl()
    {
        hasPearl = false;
        if (pearl != null) pearl.SetActive(false);
    }

    /// <summary>玩家撞到珍珠则收走，返回 true。只要珍珠露出来，碰到就算。</summary>
    public bool TryCollect(Vector3 playerPosition)
    {
        if (!hasPearl || pearl == null || !pearl.activeSelf) return false;

        Vector3 delta = pearl.transform.position - playerPosition;
        delta.y *= 0.9f;

        // 加上主控球体自身半径，真正做到“碰到就变珍珠”
        if (delta.magnitude > collectRadius + 0.4f) return false;

        hasPearl = false;
        pearl.SetActive(false);
        return true;
    }
}
