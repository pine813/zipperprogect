using UnityEngine;

/// <summary>
/// 程序化音效：落水、游泳划水、沙滩滚动的沙沙声、珍珠碰撞。
/// 全部用代码合成 AudioClip，不依赖任何外部音频文件。
/// </summary>
public class GameAudio : MonoBehaviour
{
    const int SampleRate = 44100;

    [SerializeField, Range(0f, 1f)] float masterVolume = 0.85f;
    [SerializeField, Range(0f, 1f)] float splashVolume = 0.9f;
    [SerializeField, Range(0f, 1f)] float swimVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] float sandVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] float pearlVolume = 0.85f;

    AudioSource oneShotSource;
    AudioSource swimSource;
    AudioSource sandSource;

    AudioClip splashClip;
    AudioClip swimClip;
    AudioClip sandClip;
    AudioClip pearlClip;

    float targetSwim;
    float targetSand;
    float currentSwim;
    float currentSand;

    public static GameAudio Instance { get; private set; }

    public int ClipCount
    {
        get
        {
            int count = 0;
            if (splashClip != null) count++;
            if (swimClip != null) count++;
            if (sandClip != null) count++;
            if (pearlClip != null) count++;
            return count;
        }
    }

    public float SwimIntensity => currentSwim;
    public float SandIntensity => currentSand;

    /// <summary>读回采样峰值，确认不是静音（无音频设备时也能检查）。</summary>
    public bool HasAudibleContent()
    {
        return PeakOf(splashClip) > 0.05f
               && PeakOf(swimClip) > 0.01f
               && PeakOf(sandClip) > 0.01f
               && PeakOf(pearlClip) > 0.05f;
    }

    public string AudioLevelsText()
    {
        return string.Format("落水 {0:F2} / 游泳 {1:F2} / 沙 {2:F2} / 珍珠 {3:F2}",
            PeakOf(splashClip), PeakOf(swimClip), PeakOf(sandClip), PeakOf(pearlClip));
    }

    static float PeakOf(AudioClip clip)
    {
        if (clip == null || clip.samples <= 0) return 0f;

        float[] data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);

        float peak = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float value = Mathf.Abs(data[i]);
            if (value > peak) peak = value;
        }
        return peak;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.spatialBlend = 0f;

        swimSource = CreateLoopSource();
        sandSource = CreateLoopSource();

        try
        {
            // 优先用 Resources 里的音频文件（SfxSplash / SfxSwim / SfxSand / SfxPearl），
            // 没有就退回程序化合成，方便以后换成真人录音。
            splashClip = LoadOrBuild("SfxSplash", BuildSplash);
            swimClip = LoadOrBuild("SfxSwim", BuildSwimLoop);
            sandClip = LoadOrBuild("SfxSand", BuildSandLoop);
            pearlClip = LoadOrBuild("SfxPearl", BuildPearlChime);

            swimSource.clip = swimClip;
            sandSource.clip = sandClip;
            swimSource.volume = 0f;
            sandSource.volume = 0f;
            swimSource.Play();
            sandSource.Play();

            Debug.Log("[Audio] 程序化音效已生成：" + ClipCount + " 个");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Audio] 音频初始化失败（batchmode 无音频设备时属正常）：" + e.Message);
        }
    }

    AudioSource CreateLoopSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
        source.spatialBlend = 0f;
        return source;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        currentSwim = Mathf.MoveTowards(currentSwim, targetSwim, dt * 2.5f);
        currentSand = Mathf.MoveTowards(currentSand, targetSand, dt * 4f);

        if (swimSource != null)
        {
            swimSource.volume = currentSwim * swimVolume * masterVolume;
            swimSource.pitch = 0.9f + currentSwim * 0.3f;
        }

        if (sandSource != null)
        {
            sandSource.volume = currentSand * sandVolume * masterVolume;
            sandSource.pitch = 0.85f + currentSand * 0.5f;
        }
    }

    // ---------------- 对外的播放接口 ----------------

    public static void PlaySplash()
    {
        if (Instance != null) Instance.PlaySplashInternal();
    }

    public static void PlayPearl()
    {
        if (Instance != null) Instance.PlayPearlInternal();
    }

    public static void SetSwimIntensity(float value)
    {
        if (Instance != null) Instance.targetSwim = Mathf.Clamp01(value);
    }

    public static void SetSandIntensity(float value)
    {
        if (Instance != null) Instance.targetSand = Mathf.Clamp01(value);
    }

    void PlaySplashInternal()
    {
        if (oneShotSource == null || splashClip == null) return;
        oneShotSource.PlayOneShot(splashClip, splashVolume * masterVolume);
    }

    void PlayPearlInternal()
    {
        if (oneShotSource == null || pearlClip == null) return;
        oneShotSource.PlayOneShot(pearlClip, pearlVolume * masterVolume);
    }

    // ---------------- 合成 ----------------

    static AudioClip CreateClip(string name, float[] samples)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip LoadOrBuild(string resourceName, System.Func<AudioClip> builder)
    {
        AudioClip existing = Resources.Load<AudioClip>(resourceName);
        if (existing != null) return existing;
        return builder();
    }

    /// <summary>清脆落水：低频"扑通"下滑音 + 高频水花噪声。</summary>
    static AudioClip BuildSplash()
    {
        int length = Mathf.RoundToInt(SampleRate * 0.85f);
        float[] samples = new float[length];
        System.Random random = new System.Random(20771);

        float highPass = 0f;
        float previousNoise = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)SampleRate;

            float frequency = Mathf.Lerp(460f, 110f, Mathf.Clamp01(t / 0.30f));
            float plunk = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 8.5f);

            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            highPass = 0.92f * (highPass + noise - previousNoise);
            previousNoise = noise;

            float noiseEnvelope = Mathf.Exp(-t * 7f) * (1f - Mathf.Exp(-t * 120f));

            float value = plunk * 0.5f + highPass * 0.45f * noiseEnvelope;
            samples[i] = Mathf.Clamp(value, -1f, 1f);
        }

        return CreateClip("SfxSplash", samples);
    }

    /// <summary>游泳划水：低频水流噪声，按划水节奏起伏（循环）。</summary>
    static AudioClip BuildSwimLoop()
    {
        const float duration = 2f;
        int length = Mathf.RoundToInt(SampleRate * duration);
        float[] samples = new float[length];
        System.Random random = new System.Random(90210);

        float lowPass = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            lowPass = Mathf.Lerp(lowPass, noise, 0.05f);

            // 两拍划水，保证循环点相位一致
            float stroke = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * (t / duration) * 2f), 2f);
            samples[i] = lowPass * stroke * 0.8f;
        }

        CrossfadeLoopEdges(samples, SampleRate / 30);
        return CreateClip("SfxSwim", samples);
    }

    /// <summary>沙滩滚动：偏高频的沙沙噪声，带轻微抖动（循环）。</summary>
    static AudioClip BuildSandLoop()
    {
        const float duration = 1.2f;
        int length = Mathf.RoundToInt(SampleRate * duration);
        float[] samples = new float[length];
        System.Random random = new System.Random(4401);

        float band = 0f;
        float previousNoise = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);

            band = 0.55f * (band + noise - previousNoise);
            previousNoise = noise;

            float flutter = 0.62f + 0.38f * Mathf.Sin(2f * Mathf.PI * (t / duration) * 6f);
            samples[i] = band * 0.4f * flutter;
        }

        CrossfadeLoopEdges(samples, SampleRate / 25);
        return CreateClip("SfxSand", samples);
    }

    /// <summary>珍珠碰撞：几个高频泛音一起衰减，像小珠子相碰的清脆声。</summary>
    static AudioClip BuildPearlChime()
    {
        int length = Mathf.RoundToInt(SampleRate * 0.9f);
        float[] samples = new float[length];

        float[] partials = { 1568f, 2093f, 3136f };
        float[] gains = { 0.32f, 0.18f, 0.10f };

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)SampleRate;
            float value = 0f;

            for (int p = 0; p < partials.Length; p++)
            {
                value += Mathf.Sin(2f * Mathf.PI * partials[p] * t) * Mathf.Exp(-t * (7f + p * 4f)) * gains[p];
            }

            if (t < 0.02f)
            {
                value += Mathf.Sin(2f * Mathf.PI * 3000f * t) * 0.08f * (1f - t / 0.02f);
            }

            samples[i] = Mathf.Clamp(value, -1f, 1f);
        }

        return CreateClip("SfxPearl", samples);
    }

    static void CrossfadeLoopEdges(float[] samples, int fadeLength)
    {
        if (fadeLength * 2 >= samples.Length) return;

        for (int i = 0; i < fadeLength; i++)
        {
            float k = i / (float)fadeLength;
            int tail = samples.Length - fadeLength + i;
            samples[i] = samples[i] * k + samples[tail] * (1f - k);
        }
    }
}
