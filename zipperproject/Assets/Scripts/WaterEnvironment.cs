using UnityEngine;

/// <summary>
/// 相机水下/水上表现切换：水下开启蓝色雾、压暗环境光；出水恢复。
/// 挂在相机上和渲染逻辑分离，便于单独调整。
/// </summary>
public class WaterEnvironment : MonoBehaviour
{
    [SerializeField] float waterLevel = 0f;
    [SerializeField] Color underwaterFog = new Color(0.04f, 0.22f, 0.34f);
    [SerializeField] Color underwaterAmbient = new Color(0.12f, 0.32f, 0.44f);
    [SerializeField] Color aboveWaterAmbient = new Color(0.62f, 0.68f, 0.75f);
    [SerializeField] float fogDensity = 0.032f;
    [SerializeField] Camera targetCamera;

    bool initialized;
    bool isUnderwater;

    public bool IsUnderwater => isUnderwater;

    public void Configure(float waterY)
    {
        waterLevel = waterY;
    }

    void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        Apply(true);
    }

    void Update()
    {
        Apply(false);
    }

    void Apply(bool force)
    {
        bool under = transform.position.y < waterLevel;
        if (!force && initialized && under == isUnderwater) return;

        initialized = true;
        isUnderwater = under;

        RenderSettings.fog = under;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = underwaterFog;
        RenderSettings.ambientLight = under ? underwaterAmbient : aboveWaterAmbient;

        if (targetCamera != null)
        {
            targetCamera.clearFlags = under ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            if (under) targetCamera.backgroundColor = underwaterFog;
        }
    }
}
