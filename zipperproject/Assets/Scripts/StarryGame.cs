using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StarryAbyss
{
    /// <summary>
    /// 星渊 · 归途
    /// A collision odyssey painted after Van Gogh. You are a star with only
    /// left/right steering; every impact spends a little of your matter and
    /// lends you a new direction. Reach the gate before the dark does.
    /// </summary>
    public sealed class StarryGame : MonoBehaviour
    {
        public const float RHorizon = 1.8f;
        public const float RInner = 7.2f;
        public const float RMid = 14.2f;
        public const float RPortal = 20.0f;
        public const float RBound = 21.5f;
        public const float PortalRadius = 1.85f;
        public const float RStart = 8.4f;
        public const float StartAngle = 3.98f;
        public const float PortalAngle = 2.38f;
        public const float TurnRate = 1.72f;
        private const float Mu = 24.5f;
        private const int MaxBodies = 150;
        private const float TitleZoom = 0.76f;

        public sealed class HudSnapshot
        {
            public int Round;
            public float Mass;
            public int Hits;
            public float Time;
            public float PortalDistance;
            public float PortalRemain;
            public bool PortalActive;
            public float CompassAngle;
            public string TideName;
            public int TideLevel;
            public float Danger;
        }

        public GamePhase Phase { get; private set; } = GamePhase.Title;
        public StarTheme Theme => _theme;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindObjectOfType<StarryGame>() != null) return;
            var go = new GameObject("STARRY ABYSS");
            go.AddComponent<StarryGame>();
        }

        private Camera _camera;
        private StarryUI _ui;
        private StarryAudio _audio;
        private StarryBackdrop _backdrop;
        private BlackHoleView _blackHole;
        private PortalView _portal;
        private LineRenderer _boundaryRing;
        private LineRenderer _zoneInnerRing;
        private LineRenderer _zoneMidRing;
        private LineRenderer _orbitPreview;
        private int _currentZone = -1;
        private float _zoneToastCooldown;
        private LineRenderer _slingRing;
        private LineRenderer _slingTimingRing;
        private LineRenderer _encounterRing;
        private float _slingTimingPhase;
        private CelestialBody _slingTimingTarget;
        private float _orbitPreviewTimer;
        private bool _assist;
        private float _autoTurn;
        private Transform _worldRoot;
        private ParticleSystem _impactParticles;
        private ParticleSystem _gritParticles;
        private ParticleSystem _dustParticles;
        private ParticleSystem _genesisParticles;
        private readonly List<ParticleSystem> _allParticles = new List<ParticleSystem>();
        private readonly List<BoonOption> _boonOptions = new List<BoonOption>();
        private readonly HashSet<int> _boonsChosen = new HashSet<int>();
        private int _boonSelection;
        private int _boonsTaken;
        private int _effectiveHits;
        private bool _boonPending;
        private string _lastBoonTitle;
        private int _slingCharges;
        private float _slingCooldown;
        private CelestialBody _slingTarget;
        private int _codexMask;
        private bool _newStarDiscovery;
        private int _forcedStarVariant = -1;
        private static int _aspectSerial;
        private readonly List<StardustMote> _stardustMotes = new List<StardustMote>();
        private int _stardust;
        private int _stardustCombo;
        private float _stardustComboTimer;
        private int _stardustNextReward = 8;
        private int _stardustRewards;
        private bool _stardustShield;
        private float _stardustSpawnTimer;
        private int _stardustSerial;
        private const int StardustCap = 50;
        private const int StardustRewardCap = 4;
        private float _retroAngle;
        private float _lastOrbitAngle;
        private bool _hasOrbitAngle;
        private int _retroLapShown;
        private bool _riverPending;
        private bool _riverUsed;
        private bool _riverBoonActive;
        private float _nextMeteorShower = 120f;
        private bool _showerWarned;
        private int _showerSerial;
        private int _trailScore;
        private int _bestTrailScore;
        private readonly int[] _zoneDust = new int[3];
        private readonly bool[] _zoneGoalDone = new bool[3];
        private static readonly int[] ZoneGoalTargets = { 4, 10, 14 };
        private float _meteorBlessing;
        private readonly List<MeteorWarningView> _meteorWarnings = new List<MeteorWarningView>();
        private readonly List<Vector2> _showerWarnTargets = new List<Vector2>();
        private readonly List<FragmentBurst> _fragmentBursts = new List<FragmentBurst>();

        private struct FragmentBurst
        {
            public CelestialBody Source;
            public Vector2 Contact;
            public int Count;
            public float Force;
        }
        private int _starTraceCount;
        private float _blackHoleActivity;
        private int _blackHolePhase;
        private float _blackHoleTimer = 28f;
        private float _blackHolePulseTimer;
        private bool _cameraLocked;
        private float _boonKick = 1f;
        private float _boonDamage = 1f;
        private float _boonShock = 1f;
        private float _boonTide = 1f;
        private bool _boonLowMassKick;
        private GameObject _genesisBloomGo;
        private SpriteRenderer _genesisBloomMain;
        private SpriteRenderer _genesisBloomCore;
        private SpriteRenderer _genesisBloomFlare;
        private float _genesisVisualT = -1f;
        private float _cineEmberTimer;
        private readonly List<CelestialBody> _bodies = new List<CelestialBody>();
        private readonly List<ShockwaveRing> _shockwaves = new List<ShockwaveRing>();
        private PlayerState _player;

        private StarTheme _theme;
        private int _round = 1;
        private int _bodyId;
        private float _globalTime;
        private float _runTime;
        private int _hits;
        private float _maxRadius;
        private float _tide = 1f;
        private float _pendingDeath;
        private string _deathReason = "mass";
        private float _hitStop;
        private float _shake;
        private bool _reducedMotion;
        private float _uiTick;
        private bool _sawFirstHit;
        private bool _lowMassWarned;
        private bool _portalWarned;
        private bool _portalActive;
        private bool _portalMoveWarned;
        private float _portalMoveTimer;
        private Vector2 _portalPosition;
        private float _portalRemain;
        private int _tideWarned;
        private float _lastBoundaryToast = -99f;
        private float _lastBodySound = -99f;
        private float _childCooldown;
        private float _spawnTimer;
        private float _titleSpawnTimer;
        private CelestialBody _guideBody;
        private float _guideUntil;
        private bool _transitioning;
        private bool _endingShown;
        private bool _imploded;
        private float _cineT;
        private int _cinePhase = -1;
        private float _autoRemain = -1f;
        private int _autoDisplay = -1;
        private GameResult _lastResult;
        private float _cameraZoom = 1f;
        private Vector2 _cameraFocus;
        private Vector2 _cameraFocusTarget;
        private bool _audioInitialized;
        private bool _musicOnce;
        private bool _muted;
        private Vector3 _winPlayerStart;
        private Vector3 _winGate;
        private bool _focused = true;
        private float _lastOrtho = -1f;

        private float _bestDepth;
        private float _bestTime;
        private int _escapes;
        private float _savedDepth;
        private float _savedTime;
        private int _savedEscapes;

        private string _autoShotPath;
        private string _autoScene;

        private float _lastDanger;
        private float _lastNear;

        private void Awake()
        {
            _reducedMotion = Application.isMobilePlatform;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            Application.runInBackground = true;
            Cursor.visible = true;

            LoadRecords();
            SetupCamera();
            SetupWorld();
            SetupUI();
            SetupAudio();
            SetupParticles();
            SetCameraSize();

            _theme = StarPalette.Get(_round);
            _backdrop.ApplyTheme(_theme, _round);
            _blackHole.Build(_theme, RHorizon);
            _portal.Build(_theme);
            _ui.ApplyTheme(_theme);
            _ui.SetRecord(Mathf.RoundToInt(_bestDepth * 100f), _bestTime, _escapes, CodexCount());
            _ui.SetSoundMuted(_muted);
            _ui.SetAssist(_assist);

            ParseAutomation();
            EnterTitle(true);

            if (!string.IsNullOrEmpty(_autoShotPath))
            {
                StartCoroutine(AutomationRoutine());
            }
        }

        private void LoadRecords()
        {
            _savedDepth = PlayerPrefs.GetFloat("StarryBestDepth", 0f);
            _savedTime = PlayerPrefs.GetFloat("StarryBestTime", 0f);
            _savedEscapes = PlayerPrefs.GetInt("StarryEscapes", 0);
            _bestDepth = _savedDepth;
            _bestTime = _savedTime;
            _escapes = _savedEscapes;
            _bestTrailScore = PlayerPrefs.GetInt("StarryBestTrail", 0);
            _muted = PlayerPrefs.GetInt("StarryMuted", 0) == 1;
            _assist = PlayerPrefs.GetInt("StarryAssist", 0) == 1;
            _codexMask = PlayerPrefs.GetInt("StarryCodex", 0);
        }

        private void SaveRecordsIfNeeded()
        {
            PlayerPrefs.SetFloat("StarryBestDepth", _bestDepth);
            PlayerPrefs.SetFloat("StarryBestTime", _bestTime);
            PlayerPrefs.SetInt("StarryEscapes", _escapes);
            PlayerPrefs.Save();
            _savedDepth = _bestDepth;
            _savedTime = _bestTime;
            _savedEscapes = _escapes;
        }

        private void SetupCamera()
        {
            var existing = FindObjectsOfType<Camera>();
            foreach (var cam in existing)
            {
                if (cam != null && cam.gameObject != gameObject)
                    cam.enabled = false;
            }
            var go = new GameObject("Starry Camera");
            go.tag = "MainCamera";
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5.34f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = StarPalette.Get(1).Ink;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 60f;
            _camera.allowHDR = false;
            _camera.allowMSAA = true;
            go.transform.position = new Vector3(0f, 0f, -10f);
            if (FindObjectsOfType<AudioListener>().Length == 0)
                go.AddComponent<AudioListener>();
        }

        private void SetupWorld()
        {
            var world = new GameObject("Starry World");
            _worldRoot = world.transform;
            _worldRoot.SetParent(transform, false);

            var backdropGo = new GameObject("Painted Backdrop");
            backdropGo.transform.SetParent(transform, false);
            _backdrop = backdropGo.AddComponent<StarryBackdrop>();
            _backdrop.Initialize(_camera);

            var bhGo = new GameObject("Black Hole");
            bhGo.transform.SetParent(_worldRoot, false);
            _blackHole = bhGo.AddComponent<BlackHoleView>();

            var portalGo = new GameObject("Portal");
            portalGo.transform.SetParent(_worldRoot, false);
            portalGo.transform.localScale = new Vector3(1.45f, 1.45f, 1f);
            _portal = portalGo.AddComponent<PortalView>();

            // A barely-there edge makes the enlarged arena readable without
            // turning the sky into a hard wall.
            _boundaryRing = ViewKit.CreateLine("Starfield Boundary", _worldRoot, -8, 0.016f,
                new Color(0.52f, 0.72f, 0.68f, 0.075f), new Color(0.52f, 0.72f, 0.68f, 0.02f), true, 180);
            ViewKit.SetCircle(_boundaryRing, RBound - 0.015f, 180);
            _boundaryRing.startColor = new Color(0.52f, 0.72f, 0.68f, 0.075f);
            _boundaryRing.endColor = new Color(0.52f, 0.72f, 0.68f, 0.02f);

            _zoneInnerRing = ViewKit.CreateLine("Inner Ring", _worldRoot, -7, 0.016f,
                new Color(0.64f, 0.84f, 0.78f, 0.12f), new Color(0.64f, 0.84f, 0.78f, 0.02f), true, 160);
            ViewKit.SetCircle(_zoneInnerRing, RInner, 160);
            _zoneMidRing = ViewKit.CreateLine("Middle Ring", _worldRoot, -7, 0.016f,
                new Color(0.86f, 0.73f, 0.48f, 0.11f), new Color(0.86f, 0.73f, 0.48f, 0.02f), true, 180);
            ViewKit.SetCircle(_zoneMidRing, RMid, 180);

            _orbitPreview = ViewKit.CreateLine("Current Orbit", _worldRoot, 5, 0.036f,
                new Color(0.82f, 0.92f, 0.84f, 0.46f), new Color(0.72f, 0.86f, 0.8f, 0.05f), false, 64, true);
            _orbitPreview.positionCount = 0;

            _slingRing = ViewKit.CreateLine("Sling Window", _worldRoot, 36, 0.022f,
                new Color(0.62f, 0.95f, 0.88f, 0.55f), new Color(0.62f, 0.95f, 0.88f, 0.08f), true, 72);
            ViewKit.SetCircle(_slingRing, 1f, 72);
            _slingRing.gameObject.SetActive(false);

            _slingTimingRing = ViewKit.CreateLine("Sling Timing", _worldRoot, 38, 0.018f,
                new Color(0.98f, 0.84f, 0.55f, 0.8f), new Color(0.98f, 0.84f, 0.55f, 0.08f), true, 64);
            ViewKit.SetCircle(_slingTimingRing, 1f, 64);
            _slingTimingRing.gameObject.SetActive(false);

            _encounterRing = ViewKit.CreateLine("Closest Approach", _worldRoot, 35, 0.026f,
                new Color(0.96f, 0.82f, 0.54f, 0.72f), new Color(0.96f, 0.82f, 0.54f, 0.08f), true, 48);
            ViewKit.SetCircle(_encounterRing, 0.34f, 48);
            _encounterRing.gameObject.SetActive(false);
        }

        private void SetupUI()
        {
            var uiGo = new GameObject("Starry UI", typeof(RectTransform), typeof(StarryUI));
            _ui = uiGo.GetComponent<StarryUI>();
            _ui.Build(this);
            _ui.ClickSound = () =>
            {
                if (_audioInitialized) _audio.PlayUi();
            };
        }

        private void SetupAudio()
        {
            var audioGo = new GameObject("Star Score");
            audioGo.transform.SetParent(transform, false);
            _audio = audioGo.AddComponent<StarryAudio>();
            _audio.SetMuted(_muted);
        }

        private bool _audioUnavailable;

        private void EnsureAudio()
        {
            if (_audioInitialized || _audioUnavailable) return;
            try
            {
                _audio.Initialize();
                _audio.SetMuted(_muted);
                _audioInitialized = _audio.Ready;
                _audioUnavailable = !_audioInitialized;
            }
            catch (System.Exception e)
            {
                _audioUnavailable = true;
                Debug.LogWarning("Running without sound: " + e.Message);
            }
        }

        private void SetupParticles()
        {
            _impactParticles = WorldFx.CreateParticles(_worldRoot, "Collision Shards", 60);
            _gritParticles = WorldFx.CreateParticles(_worldRoot, "Collision Grit", 62);
            _dustParticles = WorldFx.CreateParticles(_worldRoot, "Stardust", 58);
            _genesisParticles = WorldFx.CreateParticles(_worldRoot, "New Universe", 64);
            var impactRenderer = _impactParticles.GetComponent<ParticleSystemRenderer>();
            impactRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            impactRenderer.velocityScale = 0.035f;
            impactRenderer.lengthScale = 0.02f;
            impactRenderer.cameraVelocityScale = 0.004f;
            var gritRenderer = _gritParticles.GetComponent<ParticleSystemRenderer>();
            gritRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            gritRenderer.sharedMaterial = WorldFx.GritMaterial;
            gritRenderer.sortingOrder = 62;
            var genesisRenderer = _genesisParticles.GetComponent<ParticleSystemRenderer>();
            genesisRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            genesisRenderer.velocityScale = 0.055f;
            genesisRenderer.lengthScale = 0.025f;
            genesisRenderer.cameraVelocityScale = 0.008f;
            _allParticles.Add(_impactParticles);
            _allParticles.Add(_gritParticles);
            _allParticles.Add(_dustParticles);
            _allParticles.Add(_genesisParticles);

            _genesisBloomGo = new GameObject("Genesis Bloom");
            _genesisBloomGo.transform.SetParent(_worldRoot, false);
            var glow = ProceduralArt.Glow(256, 2.15f);
            _genesisBloomMain = ViewKit.CreateSprite("Bloom", _genesisBloomGo.transform, glow, true, 16, new Color(1f, 1f, 1f, 0f));
            _genesisBloomCore = ViewKit.CreateSprite("Core", _genesisBloomGo.transform, ProceduralArt.Glow(128, 3.2f), true, 17, new Color(1f, 1f, 1f, 0f));
            ViewKit.SetDiameter(_genesisBloomCore.transform, _genesisBloomCore.sprite, 0.1f);
            _genesisBloomFlare = ViewKit.CreateSprite("Flare", _genesisBloomGo.transform, glow, true, 17, new Color(1f, 1f, 1f, 0f));
            _genesisBloomGo.SetActive(false);
        }

        private void SetCameraSize()
        {
            float aspect = Mathf.Max(0.55f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float ortho = Mathf.Max(5.35f, 5.05f / aspect);
            _camera.orthographicSize = ortho;
            _lastOrtho = ortho;
        }

        // ---------------------------------------------------------
        // Title, runs and transitions
        // ---------------------------------------------------------

        private void EnterTitle(bool initial = false)
        {
            ClearWorld();
            Phase = GamePhase.Title;
            _player = null;
            _cameraZoom = TitleZoom;
            _cameraFocus = Vector2.zero;
            _cameraFocusTarget = Vector2.zero;
            if (_orbitPreview != null) _orbitPreview.positionCount = 0;
            _shake = 0f;
            _blackHole.SetVisible(true);
            _portal.SetVisible(false);
            _blackHole.Tick(0f, 0f, 0f, 0f);
            _ui.SetSkipVisible(false);
            _ui.SetEndVisible(false, true);
            _ui.SetPauseVisible(false, true);
            _ui.SetCinemaMuted(false);
            _ui.SetCinemaVisible(false, true);
            _ui.SetHudVisible(false, true);
            _ui.SetDanger(0f);
            _ui.ClearFlash();
            if (_boundaryRing != null) _boundaryRing.enabled = true;
            _ui.ShowTitle(true);
            _ui.SetReticle(false, Vector3.zero);
            _ui.SetPortalMarker(Vector3.zero, false);
            _ui.SetPlayerMarker(Vector3.zero, false);
            HideTransient();
            _audio?.SetPaused(false);
            if (_audioInitialized && _musicOnce) _audio.StartMusic();
            UpdateCameraTransform();

            SpawnTitleField();
            _titleSpawnTimer = 5f;
            _ui.SetRecord(Mathf.RoundToInt(_bestDepth * 100f), _bestTime, _escapes, CodexCount());
            if (!initial) _ui.SetVeil(0f);
        }

        public void StartFromTitle()
        {
            if (Phase != GamePhase.Title || _transitioning) return;
            _ui.PlayClick();
            StartCoroutine(StartRunRoutine(true));
        }

        private IEnumerator StartRunRoutine(bool fromTitle)
        {
            _transitioning = true;
            _autoRemain = -1f;
            _autoDisplay = -1;
            _ui.SnapVeil(1f);
            yield return null;
            EnsureAudio();
            _audio.SetPaused(false);
            _audio.SetMuted(_muted);
            if (_audioInitialized) _audio.StartMusic();
            _musicOnce = true;
            yield return new WaitForSecondsRealtime(0.32f);
            SetupRun();
            _ui.ShowPlaying(true);
            _ui.SetEndVisible(false, true);
            _ui.SetPauseVisible(false, true);
            _ui.SetCinemaVisible(false, true);
            _ui.SetHudVisible(true, true);
            _ui.SetSkipVisible(false);
            _ui.SetVeil(0f);
            yield return new WaitForSecondsRealtime(0.35f);
            ShowChapterForCurrentRun(fromTitle);
            if (_newStarDiscovery)
                ShowToast("新星相 · " + _player.StarName + "    ·    已录入星相图鉴", 5.6f);
            else
                ShowToast(fromTitle
                    ? "A / D 沿轨道转向 · 靠近天体时 Space 擦身 / 完美借力"
                    : "星夜重开 · 找到属于这一次的轨道", 5.6f);
            _transitioning = false;
        }

        private void SetupRun()
        {
            ClearWorld();
            Phase = GamePhase.Playing;
            _theme = StarPalette.Get(_round);
            _ui.ApplyTheme(_theme);
            _currentZone = ZoneAt(RStart);
            _zoneToastCooldown = 0f;
            _backdrop.SetZone(_currentZone);
            _ui.SetZoneName(ZoneShort(_currentZone));
            _worldRoot.localPosition = Vector3.zero;
            _worldRoot.localScale = Vector3.one;
            _cameraZoom = 1f;
            _cameraFocus = Vector2.zero;
            _cameraFocusTarget = Vector2.zero;
            _shake = 0f;
            _hitStop = 0f;
            _runTime = 0f;
            _hits = 0;
            _maxRadius = 0f;
            _tide = 1f;
            _pendingDeath = 0f;
            _deathReason = "mass";
            _endingShown = false;
            _imploded = false;
            _cineT = 0f;
            _cinePhase = -1;
            _autoRemain = -1f;
            _sawFirstHit = false;
            _autoTurn = 0f;
            _lowMassWarned = false;
            _portalWarned = false;
            _portalActive = false;
            _portalMoveWarned = false;
            _portalMoveTimer = 0f;
            _portalRemain = 60f;
            _portalPosition = new Vector2(Mathf.Cos(PortalAngle), Mathf.Sin(PortalAngle)) * RPortal;
            _tideWarned = 0;
            _lastBoundaryToast = -99f;
            _childCooldown = 0f;
            _spawnTimer = 2.8f;
            _lastDanger = 0f;
            _lastNear = 0f;
            _guideUntil = 24f;
            _boonsTaken = 0;
            _effectiveHits = 0;
            _boonPending = false;
            _boonSelection = 0;
            _lastBoonTitle = null;
            _boonKick = 1f;
            _boonDamage = 1f;
            _boonShock = 1f;
            _boonTide = 1f;
            _boonLowMassKick = false;
            _boonsChosen.Clear();
            _boonOptions.Clear();
            _slingCharges = 3;
            _slingCooldown = 0f;
            _slingTarget = null;
            if (_slingRing != null) _slingRing.gameObject.SetActive(false);
            if (_slingTimingRing != null) _slingTimingRing.gameObject.SetActive(false);
            if (_encounterRing != null) _encounterRing.gameObject.SetActive(false);
            _slingTimingTarget = null;
            _slingTimingPhase = 0f;
            _ui.SetSlingPrompt(false, 0);
            _ui.SetHintVisible(true);
            _stardustMotes.Clear();
            _fragmentBursts.Clear();
            _stardust = 0;
            _stardustCombo = 0;
            _stardustComboTimer = 0f;
            _stardustNextReward = 8;
            _stardustRewards = 0;
            _stardustShield = false;
            _stardustSpawnTimer = 1.85f;
            _stardustSerial = 0;
            _trailScore = 0;
            _meteorBlessing = 0f;
            _starTraceCount = 0;
            _blackHoleActivity = 0f;
            _blackHolePhase = 0;
            _blackHoleTimer = 28f;
            _blackHolePulseTimer = 0f;
            _cameraLocked = false;
            _blackHole?.SetActivity(0f);
            _ui?.SetBlackHoleState(0);
            for (int i = 0; i < 3; i++) { _zoneDust[i] = 0; _zoneGoalDone[i] = false; }
            for (int i = 0; i < _meteorWarnings.Count; i++) _meteorWarnings[i]?.Destroy();
            _meteorWarnings.Clear();
            _showerWarnTargets.Clear();
            _retroAngle = 0f;
            _lastOrbitAngle = 0f;
            _hasOrbitAngle = false;
            _retroLapShown = 0;
            _riverPending = false;
            _riverUsed = false;
            _riverBoonActive = false;
            _nextMeteorShower = 120f;
            _showerWarned = false;
            _showerSerial = 0;
            _ui.SetStardust(0, 0, 8, false, false, 0);
            _ui.SetZoneGoal(0, ZoneGoalTargets[_currentZone], false);
            _ui.HideBoons(true);
            _ui.ClearFlash();
            _ui.SetDanger(0f);
            _blackHole.SetVisible(true);
            _portal.SetVisible(false);
            _ui.SetPortalMarker(Vector3.zero, false);
            if (_boundaryRing != null) _boundaryRing.enabled = true;
            var portalPos = PortalPosition;
            _portal.SetWorldPosition(portalPos);
            _portal.Tick(0f, 0f, 0f, 0f);

            _audio.SetPaused(false);
            if (_audioInitialized)
            {
                _audio.StartMusic();
            }

            CreatePlayer();
            SpawnRunField();
            SpawnStardustField();
            _cameraFocus = _player.Position;
            _cameraFocusTarget = _player.Position;
            _cameraZoom = 1f;
            SetCameraTransformInstant();
            _ui.SetHudVisible(true, true);
            _ui.SetReticle(false, Vector3.zero);
            UpdateHud();
        }

        private void ShowChapterForCurrentRun(bool first)
        {
            if (first)
                _ui.ShowChapter("第 " + _round.ToString("000") + " 夜", "近 星 点 · 苏 醒", "A   N E W   O R B I T", 3.1f);
            else
                _ui.ShowChapter("第 " + _round.ToString("000") + " 夜", "新 宇 宙 · 微 光", "A   N E W   H O R I Z O N", 3.1f);
        }

        public void NextRoundFromEnd()
        {
            if (Phase != GamePhase.End || _transitioning) return;
            _ui.PlayClick();
            StartCoroutine(NextRoundRoutine());
        }

        private IEnumerator NextRoundRoutine()
        {
            _transitioning = true;
            _autoRemain = -1f;
            _audio?.SetPaused(false);
            _ui.SetVeil(1f);
            yield return new WaitForSecondsRealtime(0.46f);
            SetupRun();
            _ui.ShowPlaying(true);
            _ui.SetEndVisible(false, true);
            _ui.SetHudVisible(true, true);
            _ui.SetSkipVisible(false);
            _ui.SetVeil(0f);
            yield return new WaitForSecondsRealtime(0.35f);
            ShowChapterForCurrentRun(false);
            ShowToast("新的星夜已经展开 · 归航阵在星图外环", 4.6f);
            _transitioning = false;
        }

        public void RestartFromPause()
        {
            if (Phase != GamePhase.Paused || _transitioning) return;
            _ui.PlayClick();
            StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            _transitioning = true;
            _audio?.SetPaused(false);
            _ui.SetVeil(1f);
            yield return new WaitForSecondsRealtime(0.42f);
            _ui.SetPauseVisible(false, true);
            SetupRun();
            _ui.ShowPlaying(true);
            _ui.SetHudVisible(true, true);
            _ui.SetVeil(0f);
            yield return new WaitForSecondsRealtime(0.32f);
            _ui.ShowChapter("第 " + _round.ToString("000") + " 夜", "再 次 睁 眼", "T H E   S A M E   S K Y", 2.6f);
            ShowToast("同一条星轨，可以有另一种走法", 4f);
            _transitioning = false;
        }

        public void TogglePause()
        {
            if (_transitioning) return;
            if (Phase == GamePhase.Playing) PauseGame();
            else if (Phase == GamePhase.Paused) ResumeFromPause();
        }

        private void PauseGame()
        {
            if (Phase != GamePhase.Playing) return;
            Phase = GamePhase.Paused;
            _ui.ShowPaused();
            _ui.HideReticleInstant();
            if (_player?.View != null) _player.View.SetVisible(false);
            _audio?.SetPaused(true);
            PauseParticles(true);
            ClearTurningInput();
            _ui.PlayClick();
        }

        public void ResumeFromPause()
        {
            if (Phase != GamePhase.Paused || _transitioning) return;
            Phase = GamePhase.Playing;
            _ui.HidePaused();
            _ui.SetHudVisible(true);
            if (_player?.View != null) _player.View.SetVisible(true);
            _audio?.SetPaused(false);
            PauseParticles(false);
            _ui.PlayClick();
        }

        public void QuitToTitle()
        {
            if (_transitioning) return;
            if (Phase != GamePhase.Paused && Phase != GamePhase.End) return;
            _ui.PlayClick();
            StartCoroutine(QuitToTitleRoutine());
        }

        private IEnumerator QuitToTitleRoutine()
        {
            _transitioning = true;
            _autoRemain = -1f;
            _audio?.SetPaused(false);
            _ui.SetVeil(1f);
            yield return new WaitForSecondsRealtime(0.42f);
            _ui.SetPauseVisible(false, true);
            _ui.SetCinemaVisible(false, true);
            _ui.SetSkipVisible(false);
            EnterTitle(false);
            _ui.SetVeil(0f);
            yield return new WaitForSecondsRealtime(0.4f);
            _transitioning = false;
        }

        public void ToggleMute()
        {
            _muted = !_muted;
            PlayerPrefs.SetInt("StarryMuted", _muted ? 1 : 0);
            PlayerPrefs.Save();
            if (_audioInitialized) _audio.SetMuted(_muted);
            _ui.SetSoundMuted(_muted);
            _ui.PlayClick();
        }

        public void SkipCinematic()
        {
            if (Phase == GamePhase.DeathCinematic)
            {
                if (!_imploded) _cineT = 7.35f;
                else _cineT = 11.92f;
            }
            else if (Phase == GamePhase.WinCinematic)
            {
                if (!_imploded) _cineT = 2.22f;
                else _cineT = 5.32f;
            }
            _ui.SetSkipVisible(false);
    }

        // ---------------------------------------------------------
        // Player and bodies
        // ---------------------------------------------------------

        private void CreatePlayer()
        {
            var rand = new System.Random(_round * 4871 + 19);
            _aspectSerial++;
            var aspectRand = new System.Random(Environment.TickCount + _round * 9137 + _aspectSerial * 7919 + _hits * 71);
            int starVariant = _forcedStarVariant >= 0
                ? Mathf.Clamp(_forcedStarVariant, 0, StarAspect.Count - 1)
                : aspectRand.Next(StarAspect.Count);
            var pos = new Vector2(Mathf.Cos(StartAngle), Mathf.Sin(StartAngle)) * RStart;
            // Begin on a gently elliptical orbit so the first pass already
            // crosses the nearby planets and the guide star.
            float s = Mathf.Sqrt(Mu / RStart) * 1.08f;
            var tangent = new Vector2(Mathf.Sin(StartAngle), -Mathf.Cos(StartAngle));
            _player = new PlayerState
            {
                Position = pos,
                Velocity = tangent * s,
                Mass = 100f,
                Radius = 0.48f,
                Rotation = Mathf.Atan2(tangent.y, tangent.x),
                HitCooldown = 0.9f,
                Trail = new List<Vector2>(32),
                StarVariant = starVariant,
                StarName = StarAspect.Names[starVariant],
                StarTint = (float)aspectRand.NextDouble(),
                StarTilt = ((float)aspectRand.NextDouble() * 2f - 1f) * 8f,
                StarPhase = (float)aspectRand.NextDouble() * Mathf.PI * 2f
            };
            for (int i = 0; i < 8; i++) _player.Trail.Add(pos - tangent * i * 0.02f);
            var playerViewGo = new GameObject("Player Star View");
            _player.View = playerViewGo.AddComponent<PlayerStarView>();
            _player.View.Build(_player, _worldRoot, _theme);
            _newStarDiscovery = UnlockStarAspect(starVariant);
            _ui.SetStarAspect(_player.StarName, StarAspect.AccentColor(starVariant, _theme));
            _maxRadius = pos.magnitude;
        }

        private bool UnlockStarAspect(int variant)
        {
            int bit = 1 << Mathf.Clamp(variant, 0, StarAspect.Count - 1);
            bool fresh = (_codexMask & bit) == 0;
            if (!fresh) return false;
            _codexMask |= bit;
            PlayerPrefs.SetInt("StarryCodex", _codexMask);
            PlayerPrefs.Save();
            return true;
        }

        private int CodexCount()
        {
            int count = 0;
            for (int i = 0; i < StarAspect.Count; i++)
            {
                if ((_codexMask & (1 << i)) != 0) count++;
            }
            return count;
        }

        private static float ZoneSizeScale(float radius)
        {
            return radius < RInner ? 0.64f : radius < RMid ? 1f : 1.3f;
        }

        private void SpawnRunField()
        {
            var rand = new System.Random(_round * 7919 + 17);

            // Middle field: the starting shell. Medium planets, meteors and one guide star.
            AddOrbitalBody(BodyKind.Planet, StartAngle - 0.42f, 9.40f, 0.96f, true, rand, 0.9f);
            _guideBody = AddOrbitalBody(BodyKind.Star, StartAngle - 0.92f, 10.60f, 0.965f, false, rand, 1.0f);
            AddOrbitalBody(BodyKind.Star, PortalAngle + 0.72f, 12.60f, 0.97f, false, rand, 1.02f);
            AddOrbitalBody(BodyKind.Planet, StartAngle + 0.8f, 10.20f, 0.9f, false, rand, 1.0f);
            AddOrbitalBody(BodyKind.Planet, StartAngle - 1.55f, 13.20f, 0.98f, false, rand, 1.04f);
            AddOrbitalBody(BodyKind.Planet, PortalAngle - 0.7f, 11.60f, 0.92f, true, rand, 0.96f);

            // Cross-zone bodies: one small planet drifts up from the inner rim,
            // one large star dives in from the outer side.
            AddOrbitalBody(BodyKind.Planet, StartAngle + 2.10f, RInner + 0.12f, 0.72f, true, rand, 0.85f);
            AddOrbitalBody(BodyKind.Star, PortalAngle - 0.18f, RMid - 0.16f, 1.12f, false, rand, 1.12f);

            // Inner field: small, sparse matter near the black hole.
            AddOrbitalBody(BodyKind.Planet, StartAngle - 2.42f, 5.35f, 0.9f, false, rand, 0.58f);
            AddOrbitalBody(BodyKind.Star, PortalAngle + 2.7f, 6.35f, 0.93f, false, rand, 0.62f);

            // Outer field: the last shell, with the largest bodies and the gate approach.
            AddOrbitalBody(BodyKind.Star, PortalAngle + 0.22f, 15.40f, 0.94f, false, rand, 1.22f);
            AddOrbitalBody(BodyKind.Planet, PortalAngle - 0.34f, 16.20f, 0.9f, true, rand, 1.18f);
            AddOrbitalBody(BodyKind.Planet, PortalAngle + 0.55f, 17.50f, 0.86f, false, rand, 1.28f);
            AddOrbitalBody(BodyKind.Star, PortalAngle + 0.92f, 18.80f, 0.9f, false, rand, 1.35f);
            AddOrbitalBody(BodyKind.Planet, PortalAngle - 0.92f, 19.45f, 0.86f, true, rand, 1.24f);

            // Outer-shell wanderers at other angles keep the largest material visible
            // even when the player approaches the gate from a different direction.
            AddOrbitalBody(BodyKind.Planet, StartAngle + 0.45f, 16.80f, 0.92f, true, rand, 1.30f);
            AddOrbitalBody(BodyKind.Star, StartAngle - 1.90f, 18.00f, 0.90f, false, rand, 1.36f);
            AddOrbitalBody(BodyKind.Planet, StartAngle + 2.30f, 19.10f, 0.88f, true, rand, 1.32f);
            AddOrbitalBody(BodyKind.Planet, StartAngle - 2.70f, 17.40f, 0.91f, false, rand, 1.25f);

            // Meteors across all three shells; their size and density grow outward.
            for (int i = 0; i < 7; i++)
            {
                float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                float r = 3.2f + (float)rand.NextDouble() * 3.6f;
                AddOrbitalBody(BodyKind.Meteor, a, r, 0.88f + (float)rand.NextDouble() * 0.18f, false, rand, ZoneSizeScale(r) * 0.95f);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = StartAngle - 0.7f + (float)rand.NextDouble() * 4.0f;
                float r = RInner + 0.5f + (float)rand.NextDouble() * (RMid - RInner - 1.0f);
                AddOrbitalBody(BodyKind.Meteor, a, r, 0.88f + (float)rand.NextDouble() * 0.18f, false, rand, ZoneSizeScale(r));
            }
            for (int i = 0; i < 12; i++)
            {
                float a = PortalAngle - 1.2f + (float)rand.NextDouble() * 2.4f;
                float r = RMid + 0.4f + (float)rand.NextDouble() * (RPortal - RMid - 0.7f);
                AddOrbitalBody(BodyKind.Meteor, a, r, 0.86f + (float)rand.NextDouble() * 0.18f, false, rand, ZoneSizeScale(r));
            }

            // Dust rivers become wider and more material-rich in the outer shells.
            for (int stream = 0; stream < 6; stream++)
            {
                float baseAngle = StartAngle - 0.15f + stream * 0.58f + (float)rand.NextDouble() * 0.3f;
                float baseRadius = 7.4f + stream * 2.22f + (float)rand.NextDouble() * 0.35f;
                int count = 8 + rand.Next(4);
                float scale = ZoneSizeScale(baseRadius);
                for (int j = 0; j < count; j++)
                {
                    float a = baseAngle - j * 0.043f;
                    float rr = baseRadius + Mathf.Sin(j * 0.65f + stream) * 0.12f;
                    AddOrbitalBody(BodyKind.Dust, a, rr, 0.96f + (float)rand.NextDouble() * 0.08f, false, rand, scale);
                }
            }
            for (int i = 0; i < 30; i++)
            {
                float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                float rr = 5.4f + (float)rand.NextDouble() * 15.0f;
                AddOrbitalBody(BodyKind.Dust, a, rr, 0.9f + (float)rand.NextDouble() * 0.16f, false, rand, ZoneSizeScale(rr));
            }
        }

        private void SpawnTitleField()
        {
            var rand = new System.Random(9231);
            AddOrbitalBody(BodyKind.Star, 2.24f, 3.70f, 0.95f, false, rand);
            AddOrbitalBody(BodyKind.Star, 0.38f, 4.90f, 0.94f, false, rand);
            AddOrbitalBody(BodyKind.Planet, 0.95f, 3.20f, 0.9f, true, rand);
            AddOrbitalBody(BodyKind.Planet, 2.95f, 4.50f, 0.97f, true, rand);
            AddOrbitalBody(BodyKind.Planet, 4.9f, 4.05f, 0.96f, false, rand);
            for (int i = 0; i < 10; i++)
            {
                float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                float rr = 2.6f + (float)rand.NextDouble() * 3.0f;
                AddOrbitalBody(BodyKind.Meteor, a, rr, 0.84f + (float)rand.NextDouble() * 0.22f, false, rand);
            }
            for (int i = 0; i < 46; i++)
            {
                float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                float rr = 1.8f + (float)rand.NextDouble() * 4.1f;
                AddOrbitalBody(BodyKind.Dust, a, rr, 0.9f + (float)rand.NextDouble() * 0.15f, false, rand);
            }
        }

        private CelestialBody AddOrbitalBody(BodyKind kind, float angle, float radius, float speedFactor, bool ring, System.Random rand, float sizeScale = 1f)
        {
            var pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            float speed = Mathf.Sqrt(Mu / Mathf.Max(0.4f, radius)) * speedFactor;
            var tangent = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
            var vel = tangent * speed;
            return AddBody(kind, pos, vel, rand.Next(), ring, false, sizeScale);
        }

        private CelestialBody AddBody(BodyKind kind, Vector2 pos, Vector2 vel, int seed, bool ring, bool child, float sizeScale = 1f)
        {
            if (_bodies.Count >= MaxBodies) return null;
            var rand = new System.Random(seed);
            float radius;
            float mass;
            switch (kind)
            {
                case BodyKind.Dust:
                    radius = 0.022f + (float)rand.NextDouble() * 0.038f;
                    mass = 0.28f;
                    break;
                case BodyKind.Meteor:
                    radius = 0.08f + (float)rand.NextDouble() * 0.1f;
                    mass = 2.2f;
                    break;
                case BodyKind.Planet:
                    radius = 0.16f + (float)rand.NextDouble() * 0.16f;
                    mass = 8f;
                    break;
                default:
                    radius = 0.32f + (float)rand.NextDouble() * 0.26f;
                    mass = 25f;
                    break;
            }
            if (child) radius *= 0.82f;
            float scale = Mathf.Clamp(sizeScale, 0.55f, 1.55f);
            radius *= scale;
            mass *= scale * scale * scale;
            Color color;
            Color accent;
            switch (kind)
            {
                case BodyKind.Star:
                    color = _theme.Stars[rand.Next(_theme.Stars.Length)];
                    accent = _theme.Gold;
                    break;
                case BodyKind.Planet:
                    color = _theme.Planets[rand.Next(_theme.Planets.Length)];
                    accent = rand.NextDouble() > 0.5 ? _theme.Teal : _theme.Gold;
                    break;
                case BodyKind.Meteor:
                    color = _theme.Meteors[rand.Next(_theme.Meteors.Length)];
                    accent = _theme.Coral;
                    break;
                default:
                    color = rand.NextDouble() > 0.55 ? _theme.Cream : _theme.Gold;
                    accent = _theme.Teal;
                    break;
            }
            var body = new CelestialBody
            {
                Id = ++_bodyId,
                Kind = kind,
                Position = pos,
                Velocity = vel,
                Mass = mass,
                Radius = radius,
                Rotation = (float)rand.NextDouble() * Mathf.PI * 2f,
                Spin = ((float)rand.NextDouble() - 0.5f) * 0.5f,
                Grace = child ? 1.2f : 0.4f,
                Seed = seed,
                HasRing = kind == BodyKind.Planet && (ring || rand.NextDouble() > 0.62f),
                Color = color,
                Accent = accent,
                Trail = new List<Vector2>(24),
                Age = 0f
            };
            body.Trail.Add(pos);
            body.View = CelestialBodyView.Create(body, _worldRoot, _theme);
            _bodies.Add(body);
            return body;
        }

        private void ClearWorld()
        {
            for (int i = 0; i < _bodies.Count; i++)
            {
                _bodies[i].View?.Destroy();
            }
            _bodies.Clear();
            _fragmentBursts.Clear();
            for (int i = 0; i < _stardustMotes.Count; i++) _stardustMotes[i]?.View?.Destroy();
            _stardustMotes.Clear();
            for (int i = 0; i < _shockwaves.Count; i++) _shockwaves[i]?.DestroyRoot();
            _shockwaves.Clear();
            if (_player != null)
            {
                _player.View?.DestroyView();
                _player = null;
            }
            _guideBody = null;
            _impactParticles?.Clear();
            _gritParticles?.Clear();
            _dustParticles?.Clear();
            _genesisParticles?.Clear();
            if (_genesisBloomGo != null) _genesisBloomGo.SetActive(false);
            _genesisVisualT = -1f;
            _blackHole?.SetVisible(true);
            _portal?.SetVisible(false);
        }

        private void UpdateTitle(float dt)
        {
            _globalTime += dt;
            float sim = Mathf.Min(dt, 0.033f) * 0.42f;
            UpdateBodies(sim, true, 0f, false);
            UpdateBodyViews(dt);
            UpdateShockwaves(dt);
            _blackHole.Tick(_globalTime, dt, 0f, 0f);
            _portal.SetVisible(false);
            _titleSpawnTimer -= dt;
            if (_titleSpawnTimer <= 0f && _bodies.Count < 42)
            {
                _titleSpawnTimer = 5.5f;
                var rand = new System.Random(Environment.TickCount + _bodyId * 31);
                float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                float rr = 3.2f + (float)rand.NextDouble() * 2.8f;
                AddOrbitalBody(rand.NextDouble() > 0.8 ? BodyKind.Meteor : BodyKind.Dust, a, rr, 0.9f + (float)rand.NextDouble() * 0.15f, false, rand);
            }
            _cameraZoom = Mathf.Lerp(_cameraZoom, TitleZoom, 1f - Mathf.Exp(-2f * dt));
            _cameraFocus = Vector2.Lerp(_cameraFocus, Vector2.zero, 1f - Mathf.Exp(-2f * dt));
            UpdateCameraTransform();
            _ui.SetReticle(false, Vector3.zero);
        }

        private void UpdatePlaying(float dt)
        {
            _globalTime += dt;
            _runTime += dt;
            _tide = 1f + Mathf.Min(0.68f, _runTime * 0.0019f * _boonTide);

            float simDt = Mathf.Min(dt, 0.035f);
            if (_hitStop > 0f)
            {
                _hitStop -= dt;
                simDt *= 0.06f;
            }

            UpdateTutorialTimers();
            StepPlayer(simDt);
            UpdateStarTrace();
            UpdateBodies(simDt, false, 0f, false);
            ResolveBodyCollisions();
            RemoveDeadBodies();
            UpdateSpawner(simDt);
            UpdateParticlesAndShockwaves(dt);
            UpdateBodyViews(dt);
            UpdatePlayerView(dt);
            UpdateSlingshot(dt);
            UpdateStardust(dt);
            UpdateZone(dt);
            UpdateRetrograde(dt);
            UpdateMeteorShower();
            UpdateMeteorWarnings(dt);
            UpdateBlackHoleEcology(dt);
            if (_meteorBlessing > 0f) _meteorBlessing = Mathf.Max(0f, _meteorBlessing - dt);

            UpdatePortal(dt);
            float portalDist = _portalActive ? Vector2.Distance(_player.Position, PortalPosition) : 0f;
            bool nearPortal = _portalActive && portalDist < 5.0f;
            if (_portalActive)
            {
                _portal.SetVisible(true);
                _portal.SetWorldPosition(PortalPosition);
                _portal.Tick(_globalTime, dt, nearPortal ? Mathf.Clamp01(1f - portalDist / 5.0f) : 0f, 0f);
                var portalScreenWorld = _worldRoot.TransformPoint(new Vector3(PortalPosition.x, PortalPosition.y, 0f));
                _ui.SetPortalMarker(portalScreenWorld, true);
            }
            else
            {
                _portal.SetVisible(false);
                _ui.SetPortalMarker(Vector3.zero, false);
            }
            var playerScreenWorld = _worldRoot.TransformPoint(new Vector3(_player.Position.x, _player.Position.y, 0f));
            _ui.SetPlayerMarker(playerScreenWorld, _runTime < 8f);
            _ui.SetMinimap(_player.Position, PortalPosition, _bodies, RBound, _portalActive);

            float danger = Mathf.Clamp01(1f - (Vector2.Distance(_player.Position, Vector2.zero) - RHorizon) / 5.3f);
            danger = Mathf.Max(danger, _player.Mass < 20f ? 0.8f : 0f);
            _ui.SetDanger(danger);
            _blackHole.Tick(_globalTime, dt, danger, 0f);
            _uiTick -= dt;
            if (_uiTick <= 0f)
            {
                _uiTick = 0.075f;
                UpdateHud();
            }

            if (nearPortal && !_portalWarned)
            {
                _portalWarned = true;
                ShowToast("归航阵已在眼前 · 穿过它", 3f);
            }

            UpdateGuideReticle();

            if (_portalActive && Vector2.Distance(_player.Position, PortalPosition) < PortalRadius + _player.Radius * 0.18f)
            {
                BeginWin();
                return;
            }

            if (Vector2.Distance(_player.Position, Vector2.zero) < RHorizon * 1.02f)
            {
                BeginDeath("horizon");
                return;
            }

            if (Vector2.Distance(_player.Position, Vector2.zero) > RBound - _player.Radius)
            {
                BouncePlayerFromBoundary();
            }

            if (_pendingDeath > 0f)
            {
                _pendingDeath -= dt;
                if (_pendingDeath <= 0f)
                {
                    BeginDeath(_deathReason);
                    return;
                }
            }

            if (_boonPending && _pendingDeath <= 0f)
            {
                BeginBoon();
                if (Phase == GamePhase.Boon) return;
            }

            if (_riverPending && _pendingDeath <= 0f)
            {
                _riverPending = false;
                BeginBoon(true);
                if (Phase == GamePhase.Boon) return;
            }

            float skyDanger = Mathf.Clamp01(1f - (Vector2.Distance(_player.Position, Vector2.zero) - RHorizon) / 7.2f);
            _ui.SetDanger(skyDanger * 0.62f + (_player.Mass < 25f ? 0.07f : 0f));
            var follow = _player.Position + _player.Velocity * 0.13f;
            if (!_cameraLocked)
                _cameraFocus = Vector2.Lerp(_cameraFocus, follow, 1f - Mathf.Exp(-4.8f * dt));
            _cameraZoom = Mathf.Lerp(_cameraZoom, 1f, 1f - Mathf.Exp(-4f * dt));
            UpdateOrbitPreview(dt);
            UpdateCameraTransform();
        }

        private void UpdateTutorialTimers()
        {
            if (_sawFirstHit && _guideBody != null)
            {
                _guideBody = null;
            }
            if (!_sawFirstHit && _runTime > 24f)
            {
                _guideBody = null;
            }
            if (_runTime > 65f && _tideWarned < 1)
            {
                _tideWarned = 1;
                ShowToast("引力潮汐正在增强 · 黑洞开始收紧轨道", 4f);
                _shake = Mathf.Max(_shake, 0.08f);
            }
            else if (_runTime > 125f && _tideWarned < 2)
            {
                _tideWarned = 2;
                ShowToast("潮汐回响 · 借力必须更大胆", 4f);
                _shake = Mathf.Max(_shake, 0.12f);
            }
            else if (_runTime > 185f && _tideWarned < 3)
            {
                _tideWarned = 3;
                ShowToast("黑洞正在吞噬这片星穹 · 尽快离开", 4.2f);
                _shake = Mathf.Max(_shake, 0.16f);
            }
        }

        private void StepPlayer(float dt)
        {
            if (_player == null) return;
            var p = _player;
            if (p.Mass > 0f)
            {
                float turn = _autoTurn;
                if (Mathf.Abs(turn) < 0.01f)
                {
                    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn += 1f;
                    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn -= 1f;
                }
                p.SteerVisual = Mathf.MoveTowards(p.SteerVisual, turn, dt * 5f);
                if (Mathf.Abs(turn) > 0.01f)
                {
                    // Steering is orbital only: align the velocity with the
                    // clockwise / counter-clockwise tangent of the current
                    // position. Speed (and therefore orbital energy) is not
                    // created by the controls; collisions still do that.
                    Vector2 radial = p.Position.sqrMagnitude > 0.0001f ? p.Position.normalized : Vector2.right;
                    Vector2 target = new Vector2(-radial.y, radial.x) * Mathf.Sign(turn);
                    float speed = p.Velocity.magnitude;
                    if (speed > 0.03f)
                    {
                        float currentAngle = Mathf.Atan2(p.Velocity.y, p.Velocity.x) * Mathf.Rad2Deg;
                        float targetAngle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
                        float delta = Mathf.DeltaAngle(currentAngle, targetAngle) * Mathf.Deg2Rad;
                        float turnRate = _assist ? TurnRate * 0.82f : TurnRate;
                        float step = Mathf.Clamp(delta, -turnRate * dt, turnRate * dt);
                        float cos = Mathf.Cos(step);
                        float sin = Mathf.Sin(step);
                        var v = p.Velocity;
                        p.Velocity = new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
                    }
                }
            }
            else
            {
                p.SteerVisual = Mathf.MoveTowards(p.SteerVisual, 0f, dt * 5f);
            }

            var pos = p.Position;
            float r2 = pos.sqrMagnitude + 0.08f;
            float r = Mathf.Sqrt(r2);
            var dir = pos / r;
            p.Velocity -= dir * (Mu * _tide * (1f + _blackHoleActivity * 0.12f) / r2) * dt;
            p.Velocity *= 1f - 0.00075f * dt;
            if (p.Velocity.sqrMagnitude > 22f) p.Velocity = p.Velocity.normalized * 4.69f;
            p.Position += p.Velocity * dt;
            p.Rotation = Mathf.Atan2(p.Velocity.y, p.Velocity.x);
            p.Radius = 0.16f + 0.32f * Mathf.Pow(Mathf.Clamp01(p.Mass / 100f), 0.72f);
            p.CollisionPulse = Mathf.MoveTowards(p.CollisionPulse, 0f, dt * 3.5f);
            p.HitCooldown = Mathf.Max(0f, p.HitCooldown - dt);

            p.TrailTimer -= dt;
            if (p.TrailTimer <= 0f)
            {
                p.TrailTimer = 0.038f;
                p.Trail.Insert(0, p.Position);
                int maxTrail = 24;
                if (p.Trail.Count > maxTrail) p.Trail.RemoveAt(p.Trail.Count - 1);
            }
            p.EmitTimer -= dt;
            if (p.EmitTimer <= 0f)
            {
                p.EmitTimer = 0.075f;
                if (_dustParticles != null && p.Speed > 0.1f)
                {
                    var back = p.Position - p.Velocity.normalized * p.Radius * 0.9f;
                    var color = Color.Lerp(_theme.Star, _theme.Coral, 1f - Mathf.Clamp01(p.Mass / 100f));
                    WorldFx.Emit(_dustParticles, back, 1, color, _theme.Gold, 0.1f, 0.025f, 0.8f, -p.Velocity * 0.08f);
                }
            }

            if (p.Mass <= 0f) return;

            float contactCooldown = p.HitCooldown;
            if (contactCooldown > 0f) return;
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                if (b.Dead || b.Grace > 0f) continue;
                float rr = p.Radius * 0.9f + b.Radius * 0.9f;
                if ((b.Position - p.Position).sqrMagnitude <= rr * rr)
                {
                    HandlePlayerCollision(b);
                    break;
                }
            }
        }

        private void UpdatePlayerView(float dt)
        {
            if (_player != null && _player.View != null)
                _player.View.Tick(_player, _globalTime, dt);
        }

        private void UpdateStarTrace()
        {
            if (_player == null || _player.Mass <= 0f || _player.HitCooldown > 0f || _player.Speed < 0.3f) return;
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                if (b == null || b.Dead || b.Grace > 0f || b.Kind == BodyKind.Dust || b.TraceCooldown > 0f) continue;
                var delta = _player.Position - b.Position;
                float d = delta.magnitude;
                float inner = (_player.Radius + b.Radius) * 0.9f;
                if (d < inner * 1.04f || d > inner + 1.15f) continue;
                var rel = _player.Velocity - b.Velocity;
                if (rel.magnitude < 0.35f) continue;
                var normal = delta / Mathf.Max(0.001f, d);
                if (Mathf.Abs(Vector2.Dot(rel, normal)) > rel.magnitude * 0.78f) continue;
                b.TraceCooldown = 4f;
                AddStarTrace(b);
            }
        }

        private void AddStarTrace(CelestialBody body)
        {
            _starTraceCount++;
            _trailScore += 40;
            AddShockwave((_player.Position + body.Position) * 0.5f, _theme.Gold, 0.8f, 0.65f);
            WorldFx.Emit(_dustParticles, (_player.Position + body.Position) * 0.5f, 10, _theme.Gold, _theme.Cream, 0.6f, 0.035f, 0.9f);
            if (_audioInitialized) _audio.PlaySpark();
            if (_starTraceCount >= 3)
            {
                _starTraceCount = 0;
                _trailScore += 120;
                _slingCharges = Mathf.Min(5, _slingCharges + 1);
                _ui.Flash(0.055f, _theme.GoldLight);
                ShowToast("星座成形 · 航迹 +120 · 借力 +1", 3.6f);
            }
            else
            {
                ShowToast("星痕 · " + _starTraceCount + " / 3", 2.2f);
            }
            UpdateHud();
        }

        private void UpdateSlingshot(float dt)
        {
            if (_player == null) return;
            _slingCooldown = Mathf.Max(0f, _slingCooldown - dt);
            _slingTarget = FindSlingTarget();
            bool ready = _slingTarget != null && _slingCharges > 0 && _slingCooldown <= 0f;
            bool perfect = false;
            if (ready)
            {
                if (_slingTimingTarget != _slingTarget)
                {
                    _slingTimingTarget = _slingTarget;
                    _slingTimingPhase = 0f;
                }
                _slingTimingPhase += dt / 1.1f;
                if (_slingTimingPhase > 1f) _slingTimingPhase -= 1f;
                perfect = _slingTimingPhase >= 0.74f && _slingTimingPhase <= 0.9f;
            }
            else
            {
                _slingTimingTarget = null;
                _slingTimingPhase = 0f;
            }
            _ui.SetSlingPrompt(ready, _slingCharges, perfect);
            if (_slingRing != null)
            {
                _slingRing.gameObject.SetActive(ready);
                if (ready)
                {
                    _slingRing.transform.localPosition = new Vector3(_slingTarget.Position.x, _slingTarget.Position.y, 0f);
                    float radius = (_player.Radius + _slingTarget.Radius) * 1.05f + 0.08f;
                    ViewKit.SetCircle(_slingRing, radius, 72);
                    _slingRing.startColor = new Color(0.62f, 0.95f, 0.88f, 0.5f);
                    _slingRing.endColor = new Color(0.62f, 0.95f, 0.88f, 0.08f);

                    if (_slingTimingRing != null)
                    {
                        _slingTimingRing.gameObject.SetActive(true);
                        _slingTimingRing.transform.localPosition = new Vector3(_slingTarget.Position.x, _slingTarget.Position.y, 0f);
                        float timingRadius = Mathf.Lerp(radius + 1.35f, radius + 0.04f, _slingTimingPhase);
                        ViewKit.SetCircle(_slingTimingRing, timingRadius, 64);
                        Color timingColor = perfect ? _theme.GoldLight : _theme.Gold;
                        float timingAlpha = perfect ? 0.95f : 0.38f;
                        _slingTimingRing.startColor = new Color(timingColor.r, timingColor.g, timingColor.b, timingAlpha);
                        _slingTimingRing.endColor = new Color(timingColor.r, timingColor.g, timingColor.b, 0.05f);
                    }
                }
                else if (_slingTimingRing != null)
                {
                    _slingTimingRing.gameObject.SetActive(false);
                }
            }
        }

        private CelestialBody FindSlingTarget()
        {
            if (_player == null) return null;
            CelestialBody best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                if (b == null || b.Dead || b.Kind == BodyKind.Dust || b.Grace > 0f) continue;
                float d = Vector2.Distance(_player.Position, b.Position);
                float inner = (_player.Radius + b.Radius) * 1.05f + 0.04f;
                float outer = inner + 2.4f;
                if (d < inner || d > outer) continue;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = b;
                }
            }
            return best;
        }

        private void TrySlingshot()
        {
            if (Phase != GamePhase.Playing || _player == null || _slingCharges <= 0 || _slingCooldown > 0f) return;
            var target = FindSlingTarget();
            if (target == null) return;
            bool perfect = _slingTimingPhase >= 0.74f && _slingTimingPhase <= 0.9f;
            var p = _player;
            var away = p.Position - target.Position;
            if (away.sqrMagnitude < 0.0001f) away = -p.Position.normalized;
            else away.Normalize();
            var dir = p.Velocity.sqrMagnitude > 0.04f ? p.Velocity.normalized : away;
            _slingCharges--;
            _slingCooldown = perfect ? 0.65f : 1.1f;
            float boost = perfect ? 0.27f : 0.18f;
            float awayKick = perfect ? 0.045f : 0.035f;
            if (perfect) _slingCharges = Mathf.Min(5, _slingCharges + 1);
            p.Velocity += dir * boost + away * awayKick;
            target.Velocity -= dir * (perfect ? 0.018f : 0.012f);
            p.CollisionPulse = 1f;
            _shake = Mathf.Min(0.62f, _shake + (perfect ? 0.05f : 0.035f));
            AddShockwave(p.Position, perfect ? _theme.Gold : _theme.Teal, perfect ? 1.45f : 1.1f, perfect ? 0.95f : 0.75f);
            WorldFx.Emit(_dustParticles, p.Position, perfect ? 34 : 22,
                perfect ? _theme.Gold : _theme.Teal, _theme.Cream, perfect ? 1.0f : 0.85f, 0.055f, 1.1f, dir * 0.08f);
            if (perfect)
                WorldFx.Emit(_gritParticles, p.Position, 12, _theme.Gold, _theme.Cream, 0.7f, 0.04f, 0.8f, dir * 0.12f);
            var world = _worldRoot.TransformPoint(new Vector3(p.Position.x, p.Position.y, 0f)) + Vector3.up * 0.35f;
            _ui.ShowImpact(world,
                perfect
                    ? "<color=#F8DFA2>完美借力</color>   <size=13><color=#E8DCC0>借力返还 · 星质未损</color></size>"
                    : "<color=#BFE8DD>擦身借力</color>   <size=13><color=#E8DCC0>星质未损</color></size>",
                perfect ? _theme.Gold : _theme.Teal);
            _ui.SetSlingPrompt(false, 0);
            _slingTarget = null;
            _slingTimingTarget = null;
            _slingTimingPhase = 0f;
            if (_slingTimingRing != null) _slingTimingRing.gameObject.SetActive(false);
            _audio.PlayThud(perfect ? 0.3f : 0.2f);
        }

        private void SpawnStardustField()
        {
            var rand = new System.Random(_round * 6143 + 31);
            for (int stream = 0; stream < 6; stream++)
            {
                float baseAngle = StartAngle - 0.25f + stream * 0.46f + (float)rand.NextDouble() * 0.24f;
                float baseRadius = 7.2f + stream * 2.18f + (float)rand.NextDouble() * 0.32f;
                for (int j = 0; j < 9; j++)
                {
                    float a = baseAngle - j * 0.036f + Mathf.Sin(j * 0.7f + stream) * 0.012f;
                    float r = baseRadius + Mathf.Sin(j * 0.63f + stream * 1.7f) * 0.16f;
                    AddStardust(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, rand);
                }
            }
            for (int i = 0; i < 18; i++)
            {
                float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                float r = 5.0f + (float)rand.NextDouble() * 15.7f;
                AddStardust(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, rand);
            }
        }

        private void AddStardust(Vector2 position, System.Random rand)
        {
            int active = 0;
            for (int i = 0; i < _stardustMotes.Count; i++)
                if (_stardustMotes[i] != null && _stardustMotes[i].Alive) active++;
            if (active >= StardustCap || _player == null) return;
            float r = position.magnitude;
            if (r < RHorizon * 1.25f || r > RBound * 1.01f) return;
            var tangent = new Vector2(-position.y, position.x) / r;
            var mote = new StardustMote
            {
                Position = position,
                Velocity = tangent * (Mathf.Sqrt(Mu / r) * (0.94f + (float)rand.NextDouble() * 0.07f)),
                Phase = (float)rand.NextDouble() * 10f,
                Alive = true
            };
            Color color = _stardustSerial % 3 == 0
                ? Color.Lerp(_theme.Teal, _theme.Cream, 0.18f)
                : Color.Lerp(_theme.GoldLight, _theme.Cream, 0.2f);
            _stardustSerial++;
            mote.View = StardustView.Create(_worldRoot, color);
            mote.View.Tick(_globalTime, mote.Position);
            _stardustMotes.Add(mote);
        }

        private void UpdateStardust(float dt)
        {
            if (_player == null) return;
            _stardustSpawnTimer -= dt;
            if (_stardustCombo > 0)
            {
                _stardustComboTimer -= dt;
                if (_stardustComboTimer <= 0f) _stardustCombo = 0;
            }

            for (int i = 0; i < _stardustMotes.Count; i++)
            {
                var mote = _stardustMotes[i];
                if (mote == null || !mote.Alive) continue;
                float r = Mathf.Max(0.25f, mote.Position.magnitude);
                mote.Velocity -= mote.Position / r * (Mu * _tide / (r * r)) * dt;
                mote.Position += mote.Velocity * dt;
                r = mote.Position.magnitude;
                if (r < RHorizon * 1.12f || r > RBound * 1.03f)
                {
                    mote.Alive = false;
                    if (mote.View != null) mote.View.SetVisible(false);
                    continue;
                }
                mote.View.Tick(_globalTime, mote.Position);
                float collect = _player.Radius + 0.3f;
                if ((_player.Position - mote.Position).sqrMagnitude < collect * collect)
                    CollectStardust(mote);
            }

            if (_stardustSpawnTimer <= 0f)
            {
                _stardustSpawnTimer = 1.2f;
                int alive = 0;
                for (int i = 0; i < _stardustMotes.Count; i++)
                    if (_stardustMotes[i] != null && _stardustMotes[i].Alive) alive++;
                if (alive < StardustCap && _stardustRewards < StardustRewardCap)
                {
                    var rand = new System.Random(Environment.TickCount + _stardustSerial * 733 + _round * 17);
                    float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                    float rr = 5.0f + (float)rand.NextDouble() * 15.7f;
                    AddStardust(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr, rand);
                }
            }

            for (int i = _stardustMotes.Count - 1; i >= 0; i--)
            {
                var mote = _stardustMotes[i];
                if (mote == null || !mote.Alive)
                {
                    mote?.View?.Destroy();
                    _stardustMotes.RemoveAt(i);
                }
            }
        }

        private static int ZoneAt(float radius)
        {
            if (radius < RInner) return 0;
            if (radius < RMid) return 1;
            return 2;
        }

        private static string ZoneName(int zone)
        {
            return zone == 0 ? "内圈 · 尘光带" : zone == 1 ? "中圈 · 行星空" : "外圈 · 归航带";
        }

        private static string ZoneShort(int zone)
        {
            return zone == 0 ? "内圈" : zone == 1 ? "中圈" : "外圈";
        }

        private void UpdateZone(float dt)
        {
            if (_player == null) return;
            _zoneToastCooldown -= dt;
            int zone = ZoneAt(_player.Position.magnitude);
            if (zone == _currentZone) return;
            _currentZone = zone;
            _backdrop.SetZone(zone);
            _ui.SetZoneName(ZoneShort(zone));
            _ui.SetZoneGoal(_zoneDust[zone], ZoneGoalTargets[zone], _zoneGoalDone[zone]);
            if (_zoneToastCooldown <= 0f)
            {
                ShowToast("进入 · " + ZoneName(zone), 2.8f);
                _zoneToastCooldown = 3.5f;
            }
        }

        private void UpdateRetrograde(float dt)
        {
            if (_player == null || _riverUsed || _riverPending) return;
            var p = _player.Position;
            var v = _player.Velocity;
            float cross = p.x * v.y - p.y * v.x;
            if (cross <= 0.002f)
            {
                _retroAngle = 0f;
                _hasOrbitAngle = false;
                _retroLapShown = 0;
                _player.RetroProgress = 0f;
                _player.RetroActive = false;
                return;
            }

            float angle = Mathf.Atan2(p.y, p.x);
            if (_hasOrbitAngle)
            {
                float delta = Mathf.DeltaAngle(_lastOrbitAngle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                if (delta > 0f) _retroAngle += delta;
                else if (delta < -0.01f) _retroAngle = 0f;
            }
            _lastOrbitAngle = angle;
            _hasOrbitAngle = true;
            _player.RetroProgress = Mathf.Clamp01(_retroAngle / (Mathf.PI * 6f));
            _player.RetroActive = _retroAngle > 0.06f;
            int retroLap = Mathf.FloorToInt(_retroAngle / (Mathf.PI * 2f));
            if (retroLap > _retroLapShown && retroLap < 3)
            {
                _retroLapShown = retroLap;
                ShowToast("逆流 · 第 " + retroLap + " 圈 / 3", 2.8f);
            }

            if (_retroAngle >= Mathf.PI * 6f)
            {
                _retroAngle = 0f;
                _hasOrbitAngle = false;
                _player.RetroProgress = 0f;
                _player.RetroActive = false;
                _retroLapShown = 0;
                TriggerRiver();
            }
        }

        private void UpdateBlackHoleEcology(float dt)
        {
            _blackHoleTimer -= dt;
            if (_blackHolePhase == 0)
            {
                _blackHoleActivity = 0f;
                if (_blackHoleTimer <= 0f)
                {
                    _blackHolePhase = 1;
                    _blackHoleTimer = 5f;
                    _blackHoleActivity = 0.22f;
                    ShowToast("黑洞苏醒 · 五秒后进入吸积期", 4f);
                    _ui.Flash(0.025f, _theme.Gold);
                }
            }
            else if (_blackHolePhase == 1)
            {
                _blackHoleActivity = Mathf.Lerp(0.22f, 1f, 1f - Mathf.Clamp01(_blackHoleTimer / 5f));
                if (_blackHoleTimer <= 0f)
                {
                    _blackHolePhase = 2;
                    _blackHoleTimer = 9f;
                    _blackHolePulseTimer = 0f;
                    _blackHoleActivity = 1f;
                    ShowToast("吸积期 · 引力增强，内圈星屑被抛起", 4.2f);
                    PulseBlackHole();
                    _shake = Mathf.Max(_shake, 0.08f);
                }
            }
            else
            {
                _blackHoleActivity = 1f;
                _blackHolePulseTimer -= dt;
                if (_blackHolePulseTimer <= 0f)
                {
                    _blackHolePulseTimer = 2.6f;
                    PulseBlackHole();
                }
                if (_blackHoleTimer <= 0f)
                {
                    _blackHolePhase = 0;
                    _blackHoleTimer = 30f;
                    _blackHoleActivity = 0f;
                    ShowToast("黑洞重新安静下来", 3f);
                }
            }
            _blackHole?.SetActivity(_blackHoleActivity);
            _ui.SetBlackHoleState(_blackHolePhase);
        }

        private void UpdatePortal(float dt)
        {
            if (!_portalActive)
            {
                _portalRemain = Mathf.Max(0f, 60f - _runTime);
                if (_runTime >= 60f) ActivatePortal();
                return;
            }

            _portalRemain = 0f;
            _portalMoveTimer -= dt;
            if (_portalMoveTimer <= 3f && !_portalMoveWarned)
            {
                _portalMoveWarned = true;
                ShowToast("归航阵正在移动 · 注意它的新方位", 2.8f);
            }
            if (_portalMoveTimer <= 0f)
            {
                float d = Vector2.Distance(_player.Position, _portalPosition);
                if (d > 5.2f) RelocatePortal(false);
                else _portalMoveTimer = 1.5f;
            }
        }

        private void ActivatePortal()
        {
            _portalActive = true;
            _portalRemain = 0f;
            _portalWarned = false;
            RelocatePortal(true);
            _portal.SetVisible(true);
            _portal.SetWorldPosition(_portalPosition);
            _portal.Tick(_globalTime, 0f, 0f, 0f);
            AddShockwave(_portalPosition, _theme.Gold, 2.2f, 1.4f);
            _ui.Flash(0.04f, _theme.Gold);
            ShowToast("归航阵显现 · 它会不断改变位置", 4.5f);
        }

        private void RelocatePortal(bool initial)
        {
            Vector2 old = _portalPosition;
            Vector2 next = old;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float radius = RPortal - 1.35f + UnityEngine.Random.Range(0f, 1.65f);
                next = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (_player == null || Vector2.Distance(next, _player.Position) > 6f) break;
            }
            _portalPosition = next;
            _portalWarned = false;
            _portalMoveWarned = false;
            _portalMoveTimer = UnityEngine.Random.Range(15f, 21f);
            if (!initial)
            {
                AddShockwave(old, _theme.Gold, 1.2f, 1.2f);
                AddShockwave(next, _theme.Gold, 2.2f, 1.6f);
                _ui.Flash(0.035f, _theme.Gold);
                ShowToast("归航阵移向了新的轨道", 3f);
            }
        }

        private void PulseBlackHole()
        {
            if (_player == null) return;
            AddShockwave(Vector2.zero, _theme.Gold, 8f, 2.5f);
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                if (b == null || b.Dead) continue;
                float d = b.Position.magnitude;
                if (d < 0.3f || d > 9f) continue;
                b.Velocity -= b.Position / d * (0.04f / (1f + d * 0.22f));
            }
            float pr = _player.Position.magnitude;
            if (pr > 0.3f)
                _player.Velocity -= _player.Position / pr * 0.02f;
            for (int i = 0; i < 2; i++)
            {
                var rand = new System.Random(Environment.TickCount + _round * 977 + i * 31 + _blackHolePhase);
                float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                float r = 5.0f + (float)rand.NextDouble() * 2.4f;
                AddStardust(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, rand);
            }
            _ui.Flash(0.02f, _theme.Gold);
        }

        private void UpdateMeteorShower()
        {
            if (_runTime >= _nextMeteorShower - 4f && !_showerWarned)
            {
                _showerWarned = true;
                CreateShowerWarnings();
                ShowToast("流星雨锁定三处方位 · 红圈内会被贯穿", 4f);
                _ui.Flash(0.035f, _theme.Teal);
            }
            if (_runTime >= _nextMeteorShower)
            {
                SpawnMeteorShower();
                _nextMeteorShower += 120f;
                _showerWarned = false;
            }
        }

        private void UpdateMeteorWarnings(float dt)
        {
            for (int i = _meteorWarnings.Count - 1; i >= 0; i--)
            {
                if (_meteorWarnings[i] == null || !_meteorWarnings[i].Tick(dt))
                    _meteorWarnings.RemoveAt(i);
            }
        }

        private void CreateShowerWarnings()
        {
            ClearShowerWarnings();
            _showerWarnTargets.Clear();
            _showerWarnTargets.Add(_player.Position);
            var candidates = new List<CelestialBody>();
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                if (b != null && !b.Dead && (b.Kind == BodyKind.Planet || b.Kind == BodyKind.Star))
                    candidates.Add(b);
            }
            var rand = new System.Random(Environment.TickCount + _round * 2711);
            for (int i = 0; i < 2 && candidates.Count > 0; i++)
            {
                var b = candidates[rand.Next(candidates.Count)];
                candidates.Remove(b);
                _showerWarnTargets.Add(b.Position);
            }
            for (int i = 0; i < _showerWarnTargets.Count; i++)
            {
                float warningRadius = i == 0 ? 2.4f : 2.0f;
                var warning = MeteorWarningView.Create(_worldRoot, _showerWarnTargets[i],
                    i == 0 ? _theme.Coral : _theme.Gold, warningRadius);
                _meteorWarnings.Add(warning);
            }
        }

        private void ClearShowerWarnings()
        {
            for (int i = 0; i < _meteorWarnings.Count; i++) _meteorWarnings[i]?.Destroy();
            _meteorWarnings.Clear();
        }

        private void SpawnMeteorShower()
        {
            if (_player == null) return;
            var rand = new System.Random(Environment.TickCount + _round * 3181 + _showerSerial++);
            var targets = new List<CelestialBody>();
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                if (b == null || b.Dead || b.Kind == BodyKind.Dust) continue;
                if (b.Kind == BodyKind.Planet || b.Kind == BodyKind.Star) targets.Add(b);
            }
            int count = 13 + rand.Next(5);
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                bool warned = i < _showerWarnTargets.Count;
                bool atPlayer = i < 2;
                CelestialBody target = null;
                Vector2 aim;
                if (warned)
                {
                    aim = _showerWarnTargets[i];
                }
                else if (!atPlayer && targets.Count > 0)
                {
                    target = targets[rand.Next(targets.Count)];
                    aim = target.Position;
                }
                else
                {
                    aim = _player.Position + _player.Velocity * 0.8f;
                }
                float angle = Mathf.Atan2(aim.y, aim.x) + ((float)rand.NextDouble() - 0.5f) * 0.45f;
                float radius = RBound + 1.2f + (float)rand.NextDouble() * 2.2f;
                Vector2 start = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (target != null)
                {
                    // Light lead so the faster streak intersects the moving planet.
                    float travel = Vector2.Distance(start, aim) / 5.0f;
                    aim = target.Position + target.Velocity * travel * 0.65f;
                }
                Vector2 dir = (aim - start).normalized;
                float speed = 4.2f + (float)rand.NextDouble() * 1.8f;
                var body = AddBody(BodyKind.Meteor, start, dir * speed,
                    rand.Next(), false, false, 1.12f + (float)rand.NextDouble() * 0.28f);
                if (body == null) break;
                body.Grace = 0.05f;
                body.Pulse = 0.7f;
                body.Shower = true;
                spawned++;
                WorldFx.Emit(_dustParticles, start, 5, _theme.Gold, _theme.Coral, 0.9f, 0.045f, 1.4f, -dir * 0.2f);
            }
            ClearShowerWarnings();
            _showerWarnTargets.Clear();
            if (spawned == 0) return;
            _shake = Mathf.Max(_shake, 0.1f);
            _ui.Flash(0.055f, _theme.Teal);
            _audio.PlayThud(0.3f);
            ShowToast("流星雨 · 星场被贯穿", 3.2f);
        }

        private void DropShowerLoot(Vector2 position)
        {
            var rand = new System.Random(Environment.TickCount + _round * 1709);
            for (int i = 0; i < 2; i++)
            {
                float a = (float)rand.NextDouble() * Mathf.PI * 2f;
                AddStardust(position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.35f + (float)rand.NextDouble() * 0.35f), rand);
            }
            _trailScore += 25;
            AddShockwave(position, _theme.Gold, 1.0f, 0.7f);
            WorldFx.Emit(_impactParticles, position, 10, _theme.Gold, _theme.Cream, 0.65f, 0.06f, 0.9f);
            if (_meteorBlessing <= 0f)
            {
                _meteorBlessing = 14f;
                _slingCharges = Mathf.Min(5, _slingCharges + 1);
                ShowToast("流星碎片 · 借力 +1 · 14 秒冲量强化", 3.6f);
            }
        }

        private void TriggerRiver()
        {
            if (_riverUsed || _riverPending || _player == null || Phase != GamePhase.Playing) return;
            _riverUsed = true;
            _riverPending = true;
            _ui.Flash(0.16f, _theme.Teal);
            _shake = Mathf.Max(_shake, 0.12f);
            AddShockwave(_player.Position, _theme.Teal, 2.4f, 1.4f);
            WorldFx.Emit(_dustParticles, _player.Position, 36, _theme.Teal, _theme.Cream, 1.1f, 0.06f, 1.8f);
            _audio.PlayThud(0.25f);
        }

        private void CollectStardust(StardustMote mote)
        {
            mote.Alive = false;
            if (mote.View != null) mote.View.SetVisible(false);
            _stardustCombo++;
            _stardustComboTimer = 2.8f;
            int gain = 1 + (_stardustCombo >= 6 ? 1 : 0);
            _stardust += gain;
            _trailScore += 10 + Mathf.Min(30, _stardustCombo * 2);
            int moteZone = ZoneAt(mote.Position.magnitude);
            if (moteZone == _currentZone && !_zoneGoalDone[moteZone])
            {
                _zoneDust[moteZone]++;
                if (_zoneDust[moteZone] >= ZoneGoalTargets[moteZone])
                    CompleteZoneGoal(moteZone);
                else
                    _ui.SetZoneGoal(_zoneDust[moteZone], ZoneGoalTargets[moteZone], false);
            }
            WorldFx.Emit(_dustParticles, mote.Position, 8, _theme.GoldLight, _theme.Cream, 0.6f, 0.05f, 1.5f);
            WorldFx.Emit(_impactParticles, mote.Position, 4, _theme.Cream, _theme.Gold, 0.45f, 0.08f, 0.8f);
            if (_audioInitialized) _audio.PlaySpark();
            if (_stardustRewards < StardustRewardCap && _stardust >= _stardustNextReward)
                GrantStardustReward();
            UpdateHud();
        }

        private void CompleteZoneGoal(int zone)
        {
            if (zone < 0 || zone > 2 || _zoneGoalDone[zone] || _player == null) return;
            _zoneGoalDone[zone] = true;
            int bonus = zone == 0 ? 200 : zone == 1 ? 300 : 450;
            _trailScore += bonus;
            _slingCharges = Mathf.Min(5, _slingCharges + 1);
            _player.Mass = Mathf.Min(100f, _player.Mass + 3f);
            _ui.SetZoneGoal(_zoneDust[zone], ZoneGoalTargets[zone], true);
            AddShockwave(_player.Position, _theme.Gold, 1.8f, 1.1f);
            WorldFx.Emit(_gritParticles, _player.Position, 20, _theme.Gold, _theme.Cream, 0.9f, 0.05f, 1.4f, Vector2.zero);
            ShowToast("星屑目标达成 · " + ZoneShort(zone) + " · 航迹 +" + bonus + " · 借力 +1", 3.8f);
            UpdateHud();
        }

        private void GrantStardustReward()
        {
            if (_player == null || _stardustRewards >= StardustRewardCap) return;
            _stardustRewards++;
            if ((_stardustRewards % 2 == 0) && !_stardustShield)
            {
                _stardustShield = true;
                _player.Shield = true;
                AddShockwave(_player.Position, _theme.Teal, 1.4f, 0.85f);
                WorldFx.Emit(_dustParticles, _player.Position, 24, _theme.Teal, _theme.Cream, 0.8f, 0.06f, 1.8f);
                ShowToast("星屑共鸣 · 星辉护盾就绪", 3.2f);
            }
            else
            {
                _slingCharges = Mathf.Min(5, _slingCharges + 1);
                _player.Mass = Mathf.Min(100f, _player.Mass + 4f);
                WorldFx.Emit(_gritParticles, _player.Position, 12, _theme.Gold, _theme.Cream, 0.8f, 0.05f, 1.2f, Vector2.zero);
                ShowToast("星屑共鸣 · 星质 +4 · 借力 +1", 3.2f);
            }
            _stardustNextReward += 8;
            while (_stardustNextReward <= _stardust) _stardustNextReward += 8;
            UpdateHud();
        }

        private void UpdateOrbitPreview(float dt)
        {
            if (_orbitPreview == null || _player == null) return;
            _orbitPreviewTimer -= dt;
            if (_orbitPreviewTimer > 0f) return;
            _orbitPreviewTimer = 0.12f;
            Vector2 pos = _player.Position;
            Vector2 vel = _player.Velocity;
            int steps = _assist ? 110 : 58;
            const float step = 0.085f;
            _orbitPreview.positionCount = steps;
            float bestGap = float.MaxValue;
            Vector2 bestPoint = Vector2.zero;

            List<CelestialBody> encBodies = null;
            List<Vector2> encPos = null;
            List<Vector2> encVel = null;
            if (_assist)
            {
                encBodies = new List<CelestialBody>();
                encPos = new List<Vector2>();
                encVel = new List<Vector2>();
                for (int i = 0; i < _bodies.Count; i++)
                {
                    var b = _bodies[i];
                    if (b == null || b.Dead || b.Kind == BodyKind.Dust) continue;
                    encBodies.Add(b);
                    encPos.Add(b.Position);
                    encVel.Add(b.Velocity);
                }
            }

            int count = 0;
            for (int i = 0; i < steps; i++)
            {
                _orbitPreview.SetPosition(count++, new Vector3(pos.x, pos.y, 0f));
                float rr = Mathf.Max(0.25f, pos.magnitude);
                vel -= pos / rr * (Mu * _tide / (rr * rr)) * step;
                pos += vel * step;

                if (_assist)
                {
                    for (int j = 0; j < encBodies.Count; j++)
                    {
                        Vector2 bp = encPos[j];
                        Vector2 bv = encVel[j];
                        float br = Mathf.Max(0.25f, bp.magnitude);
                        bv -= bp / br * (Mu * _tide / (br * br)) * step;
                        bp += bv * step;
                        encPos[j] = bp;
                        encVel[j] = bv;
                        float gap = Vector2.Distance(pos, bp) - (_player.Radius + encBodies[j].Radius);
                        if (gap < bestGap)
                        {
                            bestGap = gap;
                            bestPoint = pos;
                        }
                    }
                }

                if (pos.magnitude > RBound || pos.magnitude < RHorizon) break;
            }
            _orbitPreview.positionCount = count >= 2 ? count : 0;
            Color start = Color.Lerp(_theme.Teal, _theme.Cream, 0.32f);
            start.a = 0.46f;
            _orbitPreview.startColor = start;
            start.a = 0.05f;
            _orbitPreview.endColor = start;

            if (_encounterRing != null)
            {
                bool show = _assist && count > 2 && bestGap < 1.8f;
                _encounterRing.gameObject.SetActive(show);
                if (show)
                {
                    _encounterRing.transform.localPosition = new Vector3(bestPoint.x, bestPoint.y, 0f);
                    _encounterRing.startColor = new Color(0.98f, 0.84f, 0.55f, 0.72f);
                    _encounterRing.endColor = new Color(0.98f, 0.84f, 0.55f, 0.08f);
                }
            }
        }

        private void UpdateBodies(float dt, bool title, float collapse, bool cinematic)
        {
            if (_bodies.Count == 0) return;
            float tide = title ? 1f : _tide;
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                if (b.Dead) continue;
                float r2 = Mathf.Max(0.16f, b.Position.sqrMagnitude + 0.09f);
                float r = Mathf.Sqrt(r2);
                var dir = b.Position / r;
                float gravity = Mu * tide * (1f + collapse * 1.25f) * (1f + _blackHoleActivity * 0.12f) / r2;
                b.Velocity -= dir * gravity * dt;
                if (cinematic)
                {
                    b.Velocity -= dir * (0.004f + collapse * collapse * collapse * 0.8f) * dt;
                    var tangent = new Vector2(-dir.y, dir.x);
                    b.Velocity += tangent * collapse * 0.22f * dt;
                }
                b.Velocity *= 1f - 0.0015f * dt;
                b.Position += b.Velocity * dt;
                b.Rotation += b.Spin * dt;
                b.Age += dt;
                b.Grace = Mathf.Max(0f, b.Grace - dt);
                b.HitCooldown = Mathf.Max(0f, b.HitCooldown - dt);
                b.BumpCooldown = Mathf.Max(0f, b.BumpCooldown - dt);
                b.TraceCooldown = Mathf.Max(0f, b.TraceCooldown - dt);
                b.FragmentCooldown = Mathf.Max(0f, b.FragmentCooldown - dt);
                b.Pulse = Mathf.MoveTowards(b.Pulse, 0f, dt * 4f);

                if (!title && b.Grace <= 0.0f)
                {
                    // Trails are sampled often; during collapse they become long comet strokes.
                    var trail = b.Trail;
                    if (cinematic || b.Kind != BodyKind.Dust)
                    {
                        var last = trail.Count > 0 ? trail[0] : b.Position;
                        if ((last - b.Position).sqrMagnitude > (b.Kind == BodyKind.Dust ? 0.003f : 0.001f))
                        {
                            trail.Insert(0, b.Position);
                            int cap = cinematic ? 28 : (b.Kind == BodyKind.Star ? 18 : b.Kind == BodyKind.Planet ? 14 : 9);
                            if (trail.Count > cap) trail.RemoveAt(trail.Count - 1);
                        }
                    }
                }
                else if (!title)
                {
                    b.Trail.Insert(0, b.Position);
                    if (b.Trail.Count > 10) b.Trail.RemoveAt(b.Trail.Count - 1);
                }

                if (r < RHorizon * 1.055f || b.Position.sqrMagnitude > 29.5f * 29.5f)
                {
                    if (r < RHorizon * 1.055f && !title)
                    {
                        var worldPos = b.Position;
                        WorldFx.Emit(_dustParticles, worldPos, 2, _theme.Gold, _theme.Cream, 0.55f, 0.03f, 1.2f, -b.Velocity * 0.1f);
                    }
                    b.Dead = true;
                }
            }
        }

        private void ResolveBodyCollisions()
        {
            if (Phase != GamePhase.Playing) return;
            for (int i = 0; i < _bodies.Count; i++)
            {
                var a = _bodies[i];
                if (a.Dead) continue;
                for (int j = i + 1; j < _bodies.Count; j++)
                {
                    var b = _bodies[j];
                    if (b.Dead) continue;
                    if (a.Kind == BodyKind.Dust && b.Kind == BodyKind.Dust) continue;
                    var delta = b.Position - a.Position;
                    float minDist = a.Radius + b.Radius;
                    float sq = delta.sqrMagnitude;
                    if (sq > minDist * minDist || sq < 0.0000001f) continue;
                    float dist = Mathf.Sqrt(sq);
                    var n = delta / dist;
                    var correction = n * (minDist - dist) * 0.52f;
                    a.Position -= correction;
                    b.Position += correction;
                    float relativeNormal = Vector2.Dot(b.Velocity - a.Velocity, n);
                    if (relativeNormal < 0f)
                    {
                        float invA = 1f / Mathf.Max(0.05f, a.Mass);
                        float invB = 1f / Mathf.Max(0.05f, b.Mass);
                        float impulse = -(0.92f) * relativeNormal / (invA + invB);
                        a.Velocity -= n * (impulse * invA);
                        b.Velocity += n * (impulse * invB);
                        float impact = Mathf.Abs(relativeNormal);
                        int maxTier = Mathf.Max((int)a.Kind, (int)b.Kind);
                        var planetContact = (a.Position + b.Position) * 0.5f;
                        if (a.Kind == BodyKind.Planet) DamagePlanet(a, planetContact, impact, false);
                        if (b.Kind == BodyKind.Planet) DamagePlanet(b, planetContact, impact, false);
                        bool showerHit = false;
                        if (a.Shower) { a.Shower = false; showerHit = true; }
                        if (b.Shower) { b.Shower = false; showerHit = true; }
                        if (showerHit && impact > 0.3f)
                            DropShowerLoot((a.Position + b.Position) * 0.5f);
                        if ((a.BumpCooldown <= 0f || b.BumpCooldown <= 0f) && impact > 0.18f && _childCooldown <= 0f)
                        {
                            var larger = (int)a.Kind >= (int)b.Kind ? a : b;
                            SpawnChild(larger, (a.Position + b.Position) * 0.5f, true);
                            a.BumpCooldown = 0.28f;
                            b.BumpCooldown = 0.28f;
                        }
                        if (impact > 0.35f && Time.unscaledTime - _lastBodySound > 0.15f)
                        {
                            _lastBodySound = Time.unscaledTime;
                            float pan = Mathf.Clamp((a.Position.x + b.Position.x) * 0.08f, -0.7f, 0.7f);
                            _audio.PlayCollision(Mathf.Clamp01(impact * 0.34f), Mathf.Min(2, (int)maxTier), pan);
                            var bodyContact = (a.Position + b.Position) * 0.5f;
                            WorldFx.Emit(_impactParticles, new Vector3(bodyContact.x, bodyContact.y, 0f), 6, a.Color, b.Color, 0.5f, 0.045f, 0.75f);
                        }
                    }
                }
            }
            FlushFragments();
        }

        private void RemoveDeadBodies()
        {
            for (int i = _bodies.Count - 1; i >= 0; i--)
            {
                var b = _bodies[i];
                if (!b.Dead) continue;
                b.View?.Destroy();
                if (b == _guideBody) _guideBody = null;
                _bodies.RemoveAt(i);
            }
        }

        private void HandlePlayerCollision(CelestialBody body)
        {
            if (_player == null) return;
            var p = _player;
            var n = p.Position - body.Position;
            if (n.sqrMagnitude < 0.00001f) n = new Vector2(1f, 0f);
            else n.Normalize();
            var rel = p.Velocity - body.Velocity;
            float normalSpeed = Mathf.Max(0.02f, -Vector2.Dot(rel, n));
            float relSpeed = Mathf.Max(0.01f, rel.magnitude);
            float glance = Mathf.Clamp01(normalSpeed / relSpeed);
            int tier = (int)body.Kind;
            float baseDamage = tier == 0 ? 0.54f : tier == 1 ? 2.2f : tier == 2 ? 4.0f : 6.6f;
            float loss = baseDamage * (0.6f + normalSpeed * 0.65f) * (0.62f + glance * 0.88f);
            if (tier == 0) loss = Mathf.Min(loss, 0.9f);
            loss *= (0.9f + (float)UnityEngine.Random.value * 0.2f) * _boonDamage;
            bool shieldUsed = false;
            if (tier >= 1 && _stardustShield)
            {
                _stardustShield = false;
                p.Shield = false;
                shieldUsed = true;
                loss *= 0.35f;
            }
            float actualLoss = Mathf.Min(p.Mass, loss);
            p.Mass = Mathf.Max(0f, p.Mass - actualLoss);
            p.HitCooldown = 0.48f;
            body.HitCooldown = 1.0f;
            body.Pulse = 1f;
            if (body.Kind == BodyKind.Dust)
            {
                body.Dead = true;
            }

            float kickBase = tier == 0 ? 0.025f : tier == 1 ? 0.14f : tier == 2 ? 0.25f : 0.4f;
            float kick = (kickBase + normalSpeed * 0.27f + 0.035f) * _boonKick;
            if (_boonLowMassKick && p.Mass < 40f) kick *= 1.25f;
            if (_meteorBlessing > 0f) kick *= 1.18f;
            var preVel = p.Velocity;
            p.Velocity += n * kick;
            p.Velocity *= 0.992f;
            body.Velocity -= n * kick * 0.16f;
            p.Position += n * 0.035f;
            var contact = body.Position + n * body.Radius;
            if (body.Kind == BodyKind.Planet)
            {
                float impactStrength = Mathf.Clamp01(actualLoss / 12f) + normalSpeed * 0.25f;
                DamagePlanet(body, contact, impactStrength, true);
                FlushFragments();
            }

            // A heavy contact sends a pressure ring through nearby matter.
            // It makes a good hit feel like an event, and can set off chains.
            if (tier >= 1)
            {
                float shockRadius = 3.2f + tier * 0.55f;
                float shockPower = 0.22f + tier * 0.13f;
                for (int i = 0; i < _bodies.Count; i++)
                {
                    var other = _bodies[i];
                    if (other == null || other == body || other.Dead) continue;
                    var d = other.Position - contact;
                    float dist = d.magnitude;
                    if (dist > shockRadius || dist < 0.001f) continue;
                    float falloff = 1f - dist / shockRadius;
                    other.Velocity += d / dist * shockPower * falloff * _boonShock;
                    other.Pulse = Mathf.Max(other.Pulse, 0.65f);
                }
            }
            float deflect = Vector2.Angle(preVel, p.Velocity);
            p.CollisionPulse = 1f;
            _hits++;
            if (tier >= 1)
            {
                _effectiveHits++;
                _stardustCombo = 0;
                _stardustComboTimer = 0f;
                _starTraceCount = 0;
                if (_boonsTaken < 1 && _effectiveHits >= 7) _boonPending = true;
            }

            int particleCount = tier == 0 ? 9 : tier == 1 ? 32 : tier == 2 ? 58 : 84;
            WorldFx.Emit(_impactParticles, contact, particleCount, body.Color, _theme.Cream, 1.15f + tier * 0.4f, 0.1f + tier * 0.02f, 1.05f + tier * 0.26f, n * 0.15f);
            WorldFx.Emit(_gritParticles, contact, 14 + tier * 10, body.Color, _theme.Gold, 1.8f + tier * 0.35f, 0.045f + tier * 0.012f, 1.4f + tier * 0.16f, n * 0.24f);
            WorldFx.Emit(_dustParticles, contact, particleCount / 2, _theme.Gold, _theme.Teal, 0.62f, 0.065f, 1.55f);
            AddShockwave(contact, body.Color, 1.2f + tier * 0.6f, 0.95f + tier * 0.22f);
            if (tier >= 1) AddShockwave(contact, _theme.Cream, 0.75f + tier * 0.4f, 0.75f + tier * 0.15f);
            _shake = Mathf.Min(0.62f, _shake + 0.035f + tier * 0.05f);
            _hitStop = Mathf.Max(_hitStop, tier == 0 ? 0.012f : tier == 1 ? 0.03f : tier == 2 ? 0.055f : 0.08f);
            _ui.Flash(0.012f + tier * 0.022f, _theme.GoldLight);
            _audio.PlayCollision(Mathf.Clamp01(0.18f + normalSpeed * 0.5f + tier * 0.06f), tier, Mathf.Clamp(body.Position.x * 0.1f, -0.75f, 0.75f));
            if (tier >= 1) SpawnChild(body, contact, true);

            if (tier >= 1)
            {
                var worldContact = _worldRoot.TransformPoint(new Vector3(contact.x, contact.y, 0f));
                _ui.ShowImpact(worldContact + Vector3.up * 0.2f,
                    "<color=#F1D79D>" + Mathf.RoundToInt(deflect) + "°  借力</color>   <size=13><color=#E8DCC0>" + actualLoss.ToString("0.0") + " 星质</color></size>",
                    _theme.Gold);
            }
            if (shieldUsed)
            {
                AddShockwave(contact, _theme.Teal, 1.5f, 0.95f);
                ShowToast("星辉护盾吸收冲击 · 伤害降至 35%", 2.8f);
            }

            if (!_sawFirstHit && tier >= 1)
            {
                _sawFirstHit = true;
                _guideBody = null;
                ShowToast("漂亮的借力。擦撞外缘，星质损耗会更少。", 3.4f);
            }
            if (!_lowMassWarned && p.Mass < 26f)
            {
                _lowMassWarned = true;
                ShowToast("星核不稳 · 你只剩下 " + Mathf.CeilToInt(p.Mass) + "% 星质", 3.8f);
            }
            if (p.Mass <= 0f)
            {
                _pendingDeath = 0.6f;
                _deathReason = "mass";
            }
            UpdateHud();
        }

        private void DamagePlanet(CelestialBody body, Vector2 contact, float strength, bool fromPlayer)
        {
            if (body == null || body.Dead || body.Kind != BodyKind.Planet) return;
            float shrink = Mathf.Clamp((fromPlayer ? 0.09f : 0.035f) + strength * 0.075f, 0.025f, 0.22f);
            body.Radius *= 1f - shrink;
            body.Mass = Mathf.Max(0.35f, body.Mass * Mathf.Pow(1f - shrink, 3f));
            body.Pulse = 1f;
            if (body.FragmentCooldown <= 0f && body.Radius > 0.1f)
            {
                QueueFragments(body, contact, fromPlayer ? 6 : 4, fromPlayer ? 0.9f : 0.65f);
                body.FragmentCooldown = fromPlayer ? 0.35f : 0.5f;
            }
            if (body.Radius < 0.1f)
            {
                body.Dead = true;
                QueueFragments(body, contact, 12, 1.25f);
                WorldFx.Emit(_gritParticles, contact, 30, body.Color, _theme.Cream, 1.1f, 0.05f, 1.5f);
                AddShockwave(contact, body.Color, 1.4f, 0.9f);
            }
        }

        private void QueueFragments(CelestialBody source, Vector2 contact, int count, float force)
        {
            if (source == null) return;
            _fragmentBursts.Add(new FragmentBurst
            {
                Source = source,
                Contact = contact,
                Count = count,
                Force = force
            });
        }

        private void FlushFragments()
        {
            for (int burstIndex = 0; burstIndex < _fragmentBursts.Count; burstIndex++)
            {
                var burst = _fragmentBursts[burstIndex];
                if (burst.Source == null) continue;
                var rand = new System.Random(burst.Source.Seed + _bodyId * 61 + burstIndex * 13);
                for (int i = 0; i < burst.Count; i++)
                {
                    if (_bodies.Count >= MaxBodies - 2) break;
                    float angle = (float)rand.NextDouble() * Mathf.PI * 2f;
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var pos = burst.Contact + dir * (burst.Source.Radius * 0.35f + 0.04f);
                    var vel = burst.Source.Velocity * 0.72f + dir * (0.22f + (float)rand.NextDouble() * 0.75f) * burst.Force;
                    BodyKind kind = i % 4 == 0 ? BodyKind.Dust : BodyKind.Meteor;
                    var fragment = AddBody(kind, pos, vel, rand.Next(), false, true,
                        0.38f + (float)rand.NextDouble() * 0.42f);
                    if (fragment == null) break;
                    fragment.Grace = 0.55f;
                    fragment.Pulse = 0.55f;
                    fragment.Color = Color.Lerp(burst.Source.Color, new Color(0.42f, 0.39f, 0.35f), 0.58f);
                    fragment.Accent = Color.Lerp(burst.Source.Accent, new Color(0.55f, 0.49f, 0.41f), 0.5f);
                    fragment.Trail.Clear();
                    fragment.Trail.Add(pos);
                }
                WorldFx.Emit(_gritParticles, burst.Contact, Mathf.Min(24, burst.Count * 4),
                    burst.Source.Color, _theme.Cream, 0.9f, 0.045f, 1.2f);
            }
            _fragmentBursts.Clear();
        }

        private void SpawnChild(CelestialBody parent, Vector2 position, bool force)
        {
            if (parent == null || parent.Dead || parent.Kind == BodyKind.Dust) return;
            if (_bodies.Count >= MaxBodies) return;
            if (!force && _childCooldown > 0f) return;
            int tier = (int)parent.Kind - 1;
            if (tier < 0) return;
            var kind = (BodyKind)tier;
            var rand = new System.Random(parent.Seed + _bodyId * 17 + Environment.TickCount);
            float angle = (float)rand.NextDouble() * Mathf.PI * 2f;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (parent.Radius + 0.06f);
            var pos = position + offset;
            var vel = parent.Velocity * 0.6f + offset.normalized * (0.25f + (float)rand.NextDouble() * 0.55f);
            var child = AddBody(kind, pos, vel, rand.Next(), false, true);
            _childCooldown = 0.23f;
            if (child != null)
            {
                child.Trail.Clear();
                child.Trail.Add(pos);
            }
        }

        private void StepChildCooldowns(float dt)
        {
            _childCooldown = Mathf.Max(0f, _childCooldown - dt);
            if (_guideBody != null && _guideBody.Dead) _guideBody = null;
        }

        private void UpdateSpawner(float dt)
        {
            StepChildCooldowns(dt);
            _spawnTimer -= dt;
            if (_spawnTimer > 0f || _bodies.Count >= MaxBodies - 8) return;
            _spawnTimer = Mathf.Lerp(3.6f, 1.8f, Mathf.InverseLerp(0f, 130f, _runTime));
            var rand = new System.Random((int)(_runTime * 130f) + _round * 811 + _bodyId);
            int spawnCount = 1 + rand.Next(2);
            for (int i = 0; i < spawnCount; i++)
            {
                double zoneRoll = rand.NextDouble();
                int zone = zoneRoll < 0.2 ? 0 : zoneRoll < 0.66 ? 1 : 2;
                float angle = (float)rand.NextDouble() * Mathf.PI * 2f;
                float radius;
                double typeRoll = rand.NextDouble();
                BodyKind kind;
                if (zone == 0)
                {
                    radius = 3.2f + (float)rand.NextDouble() * 3.7f;
                    kind = typeRoll < 0.64 ? BodyKind.Dust : typeRoll < 0.94 ? BodyKind.Meteor : BodyKind.Planet;
                }
                else if (zone == 1)
                {
                    radius = RInner + 0.45f + (float)rand.NextDouble() * (RMid - RInner - 0.9f);
                    kind = typeRoll < 0.42 ? BodyKind.Dust : typeRoll < 0.74 ? BodyKind.Meteor : typeRoll < 0.94 ? BodyKind.Planet : BodyKind.Star;
                }
                else
                {
                    radius = RMid + 0.4f + (float)rand.NextDouble() * (RPortal - RMid - 0.7f);
                    kind = typeRoll < 0.24 ? BodyKind.Dust : typeRoll < 0.52 ? BodyKind.Meteor : typeRoll < 0.82 ? BodyKind.Planet : BodyKind.Star;
                }

                // A minority of bodies are deliberately placed on a shell boundary
                // with an eccentric orbit, so the three rings visibly overlap.
                bool crosser = rand.NextDouble() < 0.15;
                float speedFactor = crosser
                    ? (rand.NextDouble() > 0.5 ? 0.68f : 1.16f)
                    : 0.82f + (float)rand.NextDouble() * 0.22f;
                if (crosser)
                {
                    radius = (rand.NextDouble() > 0.5 ? RInner : RMid) + ((float)rand.NextDouble() - 0.5f) * 0.7f;
                    kind = kind == BodyKind.Dust ? BodyKind.Meteor : kind;
                }
                AddOrbitalBody(kind, angle, radius, speedFactor, rand.NextDouble() > 0.5, rand, ZoneSizeScale(radius));
            }
        }

        private void UpdateParticlesAndShockwaves(float dt)
        {
            for (int i = _shockwaves.Count - 1; i >= 0; i--)
            {
                if (_shockwaves[i] == null || !_shockwaves[i].Tick(dt))
                    _shockwaves.RemoveAt(i);
            }
        }

        private void UpdateShockwaves(float dt)
        {
            for (int i = _shockwaves.Count - 1; i >= 0; i--)
            {
                if (_shockwaves[i] == null || !_shockwaves[i].Tick(dt))
                    _shockwaves.RemoveAt(i);
            }
        }

        private void AddShockwave(Vector2 pos, Color color, float radius, float duration)
        {
            if (_shockwaves.Count > 12) return;
            _shockwaves.Add(new ShockwaveRing(_worldRoot, pos, color, radius, duration, 56));
        }

        private void UpdateBodyViews(float dt)
        {
            for (int i = 0; i < _bodies.Count; i++)
            {
                var b = _bodies[i];
                if (!b.Dead) b.View?.Tick(_globalTime, dt);
            }
        }

        private void BouncePlayerFromBoundary()
        {
            if (_player == null) return;
            var p = _player;
            var radial = p.Position.normalized;
            p.Position = radial * (RBound - p.Radius);
            float outward = Vector2.Dot(p.Velocity, radial);
            if (outward > 0f)
            {
                p.Velocity -= radial * outward * 1.45f;
                p.Velocity *= 0.86f;
                p.CollisionPulse = 1f;
                AddShockwave(p.Position, _theme.Teal, 1.1f, 0.7f);
                WorldFx.Emit(_impactParticles, p.Position, 12, _theme.Teal, _theme.Cream, 0.55f, 0.05f, 1f, -radial * 0.2f);
                if (Time.unscaledTime - _lastBoundaryToast > 5f)
                {
                    _lastBoundaryToast = Time.unscaledTime;
                    ShowToast("星穹边界 · 归航阵是唯一的出口", 3.4f);
                }
                _audio.PlayThud(0.35f);
            }
        }

        private Vector2 PortalPosition => _portalPosition;

        private void UpdateGuideReticle()
        {
            if (_sawFirstHit || _guideBody == null || _guideBody.Dead || _runTime > _guideUntil ||
                (_player != null && Vector2.Distance(_guideBody.Position, _player.Position) < 1.45f))
            {
                _ui.SetReticle(false, Vector3.zero);
                return;
            }
            var world = _worldRoot.TransformPoint(new Vector3(_guideBody.Position.x, _guideBody.Position.y, 0f));
            _ui.SetReticle(true, world, Mathf.Clamp(_guideBody.Radius / 0.24f, 0.8f, 2.2f));
        }

        // ---------------------------------------------------------
        // Constellation pacts
        // ---------------------------------------------------------

        private void BeginBoon(bool river = false)
        {
            if (Phase != GamePhase.Playing || _player == null || _player.Mass <= 0f) return;
            if (!river && _boonsTaken >= 1) return;
            if (river && _riverBoonActive) return;
            _riverBoonActive = river;
            _boonPending = false;
            var pool = new List<int> { 0, 1, 2, 3, 4, 5 };
            pool.RemoveAll(_boonsChosen.Contains);
            if (_player.Mass >= 90f) pool.Remove(3);
            if (pool.Count == 0)
            {
                _riverBoonActive = false;
                return;
            }
            var rand = new System.Random(_round * 7919 + _hits * 97 + _boonsTaken * 31 + Environment.TickCount);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                int tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }
            _boonOptions.Clear();
            for (int i = 0; i < Mathf.Min(3, pool.Count); i++)
                _boonOptions.Add(MakeBoonOption(pool[i]));
            if (_boonOptions.Count == 0) return;
            _boonSelection = 0;
            Phase = GamePhase.Boon;
            _ui.SetBoonSubtitle(
                river
                    ? "逆流河 · 你逆着整条星河游过三圈 · 再选一条星轨"
                    : "第 7 次有效碰撞 · 本次航行唯一一次星象契约",
                river
                    ? new Color(0.62f, 0.94f, 0.88f, 0.82f)
                    : new Color(0.9f, 0.84f, 0.73f, 0.6f));
            _ui.ShowBoons(_boonOptions, _boonSelection);
            _ui.SetHudVisible(false);
            HideTransient();
            _ui.HideReticleInstant();
            _ui.SetPortalMarker(Vector3.zero, false);
            _ui.SetPlayerMarker(Vector3.zero, false);
            PauseParticles(true);
            _audio.PlayUi();
        }

        public void ChooseBoon(int index)
        {
            if (Phase != GamePhase.Boon || _boonOptions == null || index < 0 || index >= _boonOptions.Count) return;
            var chosen = _boonOptions[index];
            bool river = _riverBoonActive;
            ApplyBoon(chosen.Id);
            _boonsChosen.Add(chosen.Id);
            _lastBoonTitle = chosen.Title;
            if (river)
            {
                _riverBoonActive = false;
                _riverPending = false;
                _ui.Flash(0.12f, _theme.Teal);
                AddShockwave(_player.Position, _theme.Teal, 1.8f, 1.0f);
                _ui.ShowToast("逆流河 · 星轨重写 · " + chosen.Title, 3.8f);
            }
            else
            {
                _boonsTaken = 1;
                _ui.ShowToast("星象契约 · " + chosen.Title, 3.2f);
            }
            _boonPending = false;
            _ui.HideBoons();
            Phase = GamePhase.Playing;
            _ui.SetHudVisible(true);
            if (_player != null) _player.HitCooldown = Mathf.Max(_player.HitCooldown, 0.9f);
            PauseParticles(false);
            _audio.PlayUi();
            UpdateHud();
        }

        private void ApplyBoon(int id)
        {
            switch (id)
            {
                case 0:
                    _boonKick *= 1.3f;
                    _boonDamage *= 1.12f;
                    break;
                case 1:
                    _boonDamage *= 0.76f;
                    _boonKick *= 0.92f;
                    break;
                case 2:
                    _boonShock *= 1.55f;
                    break;
                case 3:
                    if (_player != null) _player.Mass = Mathf.Min(100f, _player.Mass + 18f);
                    _boonKick *= 0.94f;
                    break;
                case 4:
                    _boonTide *= 0.7f;
                    break;
                case 5:
                    _boonLowMassKick = true;
                    break;
            }
        }

        private static BoonOption MakeBoonOption(int id)
        {
            switch (id)
            {
                case 0:
                    return new BoonOption { Id = 0, Title = "炽 核", Effect = "撞击冲量 +30%\n星质损耗 +12%", Flavor = "更亮，也更快燃尽。" };
                case 1:
                    return new BoonOption { Id = 1, Title = "薄 壳", Effect = "星质损耗 -24%\n撞击冲量 -8%", Flavor = "把一次相撞，拉成更长的呼吸。" };
                case 2:
                    return new BoonOption { Id = 2, Title = "回 声", Effect = "冲击波推力 +55%\n波及更远", Flavor = "让一次相撞，变成一场星潮。" };
                case 3:
                    return new BoonOption { Id = 3, Title = "拾 光", Effect = "立即回复 18 星质\n此后冲量 -6%", Flavor = "从黑暗里，捡回一小块自己。" };
                case 4:
                    return new BoonOption { Id = 4, Title = "潮 汐 低 语", Effect = "潮汐增长 -30%", Flavor = "黑洞的呼吸，慢了下来。" };
                default:
                    return new BoonOption { Id = 5, Title = "余 烬", Effect = "星质低于 40% 时\n撞击冲量 +25%", Flavor = "越接近熄灭，越懂得借力。" };
            }
        }

        // ---------------------------------------------------------
        // Death and victory
        // ---------------------------------------------------------

        private void BeginDeath(string reason)
        {
            if (Phase != GamePhase.Playing) return;
            _deathReason = reason;
            _player.Mass = reason == "mass" ? 0f : _player.Mass;
            var result = BuildResult(false, reason);
            _lastResult = result;
            Phase = GamePhase.DeathCinematic;
            _cineT = 0f;
            _cinePhase = -1;
            _imploded = false;
            _endingShown = false;
            _shake = 0.28f;
            _ui.SetReticle(false, Vector3.zero);
            _ui.SetPortalMarker(Vector3.zero, false);
            _ui.SetPlayerMarker(Vector3.zero, false);
            _ui.SetHudVisible(false);
            _ui.SetCinemaMuted(false);
            _ui.SetCinemaVisible(true);
            _ui.SetSkipVisible(true);
            if (_orbitPreview != null) _orbitPreview.positionCount = 0;
            _ui.UpdateCinema("T H E   L A S T   O R B I T",
                reason == "horizon" ? "坠 入 视 界" : "星 质 耗 尽",
                reason == "horizon"
                    ? "你越过了最后一道光子环。\n时间在视界里，变得很慢、很慢。"
                    : "最后一点星质，成了黑暗里最轻的灰烬。");
            HideTransient();
            _audio.SetPaused(false);
            _audio.BeginCollapse();
            _blackHole.SetVisible(true);
            _portal.SetVisible(false);
            _cineEmberTimer = 0f;
            if (_player != null)
            {
                var contact = _player.Position;
                WorldFx.Emit(_impactParticles, contact, 100, _theme.Cream, _theme.Gold, 1.5f, 0.09f, 1.8f);
                WorldFx.Emit(_dustParticles, contact, 80, _theme.Gold, _theme.Coral, 1.2f, 0.06f, 2.2f);
                AddShockwave(contact, _theme.Cream, 2.4f, 1.4f);
                _player.View?.SetVisible(false);
            }
            _audio.SetDanger(1f, 0f);
        }

        private void UpdateDeathCinematic(float dt)
        {
            _globalTime += dt;
            _cineT += dt;
            float t = _cineT;
            if (!_imploded)
            {
                float p = Mathf.Clamp01(t / 7.35f);
                if (t > 2.3f && _cinePhase < 1)
                {
                    _cinePhase = 1;
                    _ui.UpdateCinema("T H E   L A S T   O R B I T", "万 物 失 重",
                        "恒星、行星与尘埃，\n所有轨道都在向内坠落。");
                }
                if (t > 4.7f && _cinePhase < 2)
                {
                    _cinePhase = 2;
                    _ui.UpdateCinema("S I N G U L A R I T Y", "一 点",
                        "黑洞把最后的光，\n收进一枚看不见的种子。");
                }
                if (t > 6.25f && _cinePhase < 3)
                {
                    _cinePhase = 3;
                    _ui.SetCinemaMuted(true);
                }

                UpdateBodies(dt, false, p, true);
                UpdateBodyViews(dt);
                UpdatePlayerView(dt);
                UpdateShockwaves(dt);

                _blackHole.Tick(_globalTime, dt, 1f, p);
                _blackHole.SetVisible(true);
                _shake = Mathf.MoveTowards(_shake, 0.22f + p * 0.32f, dt * 0.25f);
                _cineEmberTimer -= dt;
                if (_cineEmberTimer <= 0f)
                {
                    _cineEmberTimer = 0.085f;
                    for (int i = 0; i < 3; i++)
                    {
                        float a = UnityEngine.Random.value * Mathf.PI * 2f;
                        float rad = Mathf.Lerp(6.0f, 1.15f, p) + UnityEngine.Random.Range(-0.65f, 0.6f);
                        var pos = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                        var inward = -pos.normalized * (0.8f + UnityEngine.Random.value * 1.5f);
                        WorldFx.Emit(_dustParticles, pos, 1, _theme.Gold, _theme.Teal, 0.45f, 0.05f, 1.4f, inward);
                    }
                }

                _cameraZoom = Mathf.Lerp(_cameraZoom, 1f + p * 0.08f, 1f - Mathf.Exp(-2f * dt));
                _cameraFocus = Vector2.Lerp(_cameraFocus, Vector2.zero, 1f - Mathf.Exp(-2f * dt));
                UpdateCameraTransform();
                if (t >= 7.35f)
                    ImplodeUniverse("death");
            }
            else
            {
                _blackHole.SetVisible(false);
                _portal.SetVisible(false);
                if (t > 8.8f && _cinePhase < 5)
                {
                    _cinePhase = 5;
                    _ui.SetCinemaMuted(false);
                    _ui.UpdateCinema("A   N E W   C O S M O S", "新 宇 宙",
                        "一点爆炸，\n写成了另一片星空。");
                }
                UpdateShockwaves(dt);
                _cameraZoom = Mathf.Lerp(_cameraZoom, 1f, 1f - Mathf.Exp(-2f * dt));
                _cameraFocus = Vector2.Lerp(_cameraFocus, Vector2.zero, 1f - Mathf.Exp(-2f * dt));
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 0.8f);
                UpdateCameraTransform();
                UpdateGenesisVisuals(dt);
                if (t >= 12.0f)
                {
                    ShowEnd(_lastResult);
                }
            }
        }

        private void BeginWin()
        {
            if (Phase != GamePhase.Playing) return;
            var result = BuildResult(true, "escaped");
            _lastResult = result;
            Phase = GamePhase.WinCinematic;
            _cineT = 0f;
            _cinePhase = -1;
            _imploded = false;
            _endingShown = false;
            _winPlayerStart = new Vector3(_player.Position.x, _player.Position.y, 0f);
            _winGate = new Vector3(PortalPosition.x, PortalPosition.y, 0f);
            _player.Velocity = Vector2.zero;
            _player.HitCooldown = 99f;
            _ui.SetReticle(false, Vector3.zero);
            _ui.SetPortalMarker(Vector3.zero, false);
            _ui.SetPlayerMarker(Vector3.zero, false);
            _ui.SetHudVisible(false);
            _ui.SetCinemaMuted(false);
            _ui.SetCinemaVisible(true);
            _ui.SetSkipVisible(true);
            if (_orbitPreview != null) _orbitPreview.positionCount = 0;
            _ui.UpdateCinema("T H E   W A Y F A R E R ' S   G A T E", "归 航",
                "万千次的碰撞，终于把你送到这里。");
            HideTransient();
            _audio.SetPaused(false);
            _audio.PlayPortal();
            _portal.Tick(_globalTime, 0f, 1f, 1f);
        }

        private void UpdateWinCinematic(float dt)
        {
            _globalTime += dt;
            _cineT += dt;
            float t = _cineT;
            if (!_imploded)
            {
                float p = Mathf.Clamp01(t / 1.45f);
                float eased = p * p * (3f - 2f * p);
                _cameraZoom = Mathf.Lerp(1f, 1.65f, eased);
                _cameraFocus = Vector2.Lerp(Vector2.zero, PortalPosition, eased);
                if (_player != null)
                {
                    var start = new Vector2(_winPlayerStart.x, _winPlayerStart.y);
                    var target = PortalPosition;
                    var mid = Vector2.Lerp(start, target, 0.5f) + new Vector2(0.22f, 0.22f);
                    Vector2 a = Vector2.Lerp(start, mid, eased);
                    Vector2 b = Vector2.Lerp(mid, target, eased);
                    _player.Position = Vector2.Lerp(a, b, Mathf.Clamp01(t / 1.75f));
                    _player.WinAlpha = Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(t / 1.75f));
                    _player.Trail.Insert(0, _player.Position);
                    if (_player.Trail.Count > 24) _player.Trail.RemoveAt(_player.Trail.Count - 1);
                    _player.View?.Tick(_player, _globalTime, dt);
                }
                _portal.SetWorldPosition(PortalPosition);
                _portal.Tick(_globalTime, dt, 1f, p);
                _blackHole.Tick(_globalTime, dt, 0f, 0f);
                if (p > 0.55f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float a = UnityEngine.Random.value * Mathf.PI * 2f;
                        WorldFx.Emit(_dustParticles, (Vector2)_winGate + UnityEngine.Random.insideUnitCircle * 0.55f, 1, _theme.Portal, _theme.Cream, 0.6f, 0.038f, 1.2f, Vector2.zero);
                    }
                }
                if (t >= 2.25f)
                {
                    ImplodeUniverse("win");
                }
                UpdateBodyViews(dt);
                UpdateShockwaves(dt);
            }
            else
            {
                _blackHole.SetVisible(false);
                _portal.SetVisible(false);
                if (t > 3.3f && _cinePhase < 3)
                {
                    _cinePhase = 3;
                    _ui.SetCinemaMuted(false);
                    _ui.UpdateCinema("T H E   O T H E R   S I D E", "另 一 片 星 夜",
                        "归途不是终点，\n只是另一片星空的开始。");
                }
                _cameraZoom = Mathf.Lerp(_cameraZoom, 1f, 1f - Mathf.Exp(-2f * dt));
                _cameraFocus = Vector2.Lerp(_cameraFocus, Vector2.zero, 1f - Mathf.Exp(-2f * dt));
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 0.7f);
                UpdateCameraTransform();
                UpdateShockwaves(dt);
                UpdateGenesisVisuals(dt);
                if (t >= 5.45f) ShowEnd(_lastResult);
            }
            UpdateCameraTransform();
        }

        private void ImplodeUniverse(string kind)
        {
            if (_imploded) return;
            _imploded = true;
            _cineT = kind == "win" ? Mathf.Max(_cineT, 2.26f) : Mathf.Max(_cineT, 7.36f);
            _ui.Flash(1.05f, _theme.Cream);
            _ui.SetCinemaMuted(true);
            _ui.SetSkipVisible(true);
            _shake = 0f;
            _audio.PlayBigBang();

            // The next universe is written while the old one is still white.
            _round++;
            _theme = StarPalette.Get(_round);
            _ui.ApplyTheme(_theme);
            _backdrop.ApplyTheme(_theme, _round);
            _blackHole.Build(_theme, RHorizon);
            _blackHole.SetVisible(false);
            _portal.Build(_theme);
            _portal.SetVisible(false);
            _ui.SetCinemaMuted(true);

            for (int i = 0; i < _bodies.Count; i++) _bodies[i].View?.Destroy();
            _bodies.Clear();
            if (_player != null)
            {
                _player.View?.DestroyView();
                _player = null;
            }
            for (int i = 0; i < _shockwaves.Count; i++) _shockwaves[i]?.DestroyRoot();
            _shockwaves.Clear();
            _impactParticles.Clear();
            _gritParticles.Clear();
            _dustParticles.Clear();
            _genesisParticles.Clear();
            _cameraZoom = 1f;
            _cameraFocus = Vector2.zero;
            SetCameraTransformInstant();
            if (_boundaryRing != null) _boundaryRing.enabled = false;
            if (_orbitPreview != null) _orbitPreview.positionCount = 0;

            var colors = new[] { _theme.Cream, _theme.Gold, _theme.GoldLight, _theme.Teal, _theme.Coral, _theme.Violet, _theme.Star };
            AddShockwave(Vector2.zero, _theme.Cream, 10.6f, 2.8f);
            AddShockwave(Vector2.zero, _theme.Gold, 6.7f, 3.4f);
            AddShockwave(Vector2.zero, _theme.Teal, 12.8f, 4.2f);
            _genesisParticles.Clear();
            int birthCount = kind == "win" ? 420 : 720;
            for (int i = 0; i < birthCount; i++)
            {
                var ep = new ParticleSystem.EmitParams
                {
                    position = Vector3.zero,
                    velocity = UnityEngine.Random.insideUnitCircle.normalized * (1.4f + UnityEngine.Random.value * (kind == "win" ? 5.0f : 7.8f)),
                    startColor = Color.Lerp(colors[UnityEngine.Random.Range(0, colors.Length)], _theme.Cream, UnityEngine.Random.value * 0.55f),
                    startSize = 0.07f + UnityEngine.Random.value * 0.25f,
                    startLifetime = 3.0f + UnityEngine.Random.value * 5.1f
                };
                _genesisParticles.Emit(ep, 1);
            }
            int softCount = kind == "win" ? 100 : 200;
            for (int i = 0; i < softCount; i++)
            {
                var c = colors[UnityEngine.Random.Range(0, colors.Length)];
                c.a = 0.55f;
                var ep = new ParticleSystem.EmitParams
                {
                    position = Vector3.zero,
                    velocity = UnityEngine.Random.insideUnitCircle.normalized * (0.5f + UnityEngine.Random.value * 1.8f),
                    startColor = c,
                    startSize = 0.2f + UnityEngine.Random.value * 0.38f,
                    startLifetime = 4f + UnityEngine.Random.value * 4f
                };
                _genesisParticles.Emit(ep, 1);
            }
            _genesisVisualT = 0f;
            if (_genesisBloomGo != null)
            {
                _genesisBloomGo.SetActive(true);
                _genesisBloomMain.color = new Color(_theme.Cream.r, _theme.Cream.g, _theme.Cream.b, 0.7f);
                _genesisBloomCore.color = new Color(1f, 0.98f, 0.9f, 0.95f);
                _genesisBloomFlare.color = new Color(_theme.Gold.r, _theme.Gold.g, _theme.Gold.b, 0.55f);
            }
        }

        private void UpdateGenesisVisuals(float dt)
        {
            if (_genesisVisualT < 0f || _genesisBloomGo == null) return;
            _genesisVisualT += dt;
            float t = _genesisVisualT;
            if (t > 6.5f)
            {
                _genesisBloomGo.SetActive(false);
                _genesisVisualT = -1f;
                return;
            }
            if (!_genesisBloomGo.activeSelf) return;
            float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 3.0f), 2.4f);
            float alpha = 1f - Mathf.Clamp01((t - 0.4f) / 5.2f);
            ViewKit.SetDiameter(_genesisBloomMain.transform, _genesisBloomMain.sprite, Mathf.Lerp(1.5f, 14.5f, grow));
            ViewKit.SetDiameter(_genesisBloomCore.transform, _genesisBloomCore.sprite, Mathf.Lerp(0.3f, 2.8f, grow));
            float baseSize = Mathf.Max(0.001f, _genesisBloomFlare.sprite.bounds.size.x);
            _genesisBloomFlare.transform.localScale = new Vector3(
                Mathf.Lerp(1.5f, 19f, grow) / baseSize,
                Mathf.Lerp(0.5f, 0.15f, grow) / baseSize,
                1f);
            var mainC = _genesisBloomMain.color;
            mainC.a = 0.62f * alpha;
            _genesisBloomMain.color = mainC;
            var coreC = _genesisBloomCore.color;
            coreC.a = 0.9f * Mathf.Clamp01(1f - t / 2.2f);
            _genesisBloomCore.color = coreC;
            var flareC = _genesisBloomFlare.color;
            flareC.a = 0.38f * alpha;
            _genesisBloomFlare.color = flareC;
        }

        private void ShowEnd(GameResult result)
        {
            if (_endingShown) return;
            _endingShown = true;
            Phase = GamePhase.End;
            _ui.SetCinemaVisible(false);
            _ui.SetSkipVisible(false);
            _ui.SetPortalMarker(Vector3.zero, false);
            _ui.SetPlayerMarker(Vector3.zero, false);
            if (_orbitPreview != null) _orbitPreview.positionCount = 0;
            _autoRemain = 10f;
            _autoDisplay = 10;
            _ui.ShowEnd(result, 10);
            if (result.TrailScore > _bestTrailScore)
            {
                _bestTrailScore = result.TrailScore;
                PlayerPrefs.SetInt("StarryBestTrail", _bestTrailScore);
                PlayerPrefs.Save();
            }
            _ui.SetEndTrail(result.TrailScore, _bestTrailScore);
            _ui.SetReticle(false, Vector3.zero);
            _audio.SetDanger(0f, 0f);
            _audio.SetPaused(false);
            _audio.PlayEnding();
            if (result.Depth > _bestDepth) _bestDepth = result.Depth;
            if (result.Time > _bestTime) _bestTime = result.Time;
            if (result.Escaped) _escapes++;
            SaveRecordsIfNeeded();
            _ui.SetRecord(Mathf.RoundToInt(_bestDepth * 100f), _bestTime, _escapes, CodexCount());
            _cameraZoom = 1f;
            _cameraFocus = Vector2.zero;
            SetCameraTransformInstant();
        }

        private GameResult BuildResult(bool escaped, string reason)
        {
            float depth = escaped ? 1f : Mathf.Clamp01(_maxRadius / RPortal);
            return new GameResult
            {
                Round = _round,
                Escaped = escaped,
                Reason = reason,
                Time = _runTime,
                Hits = _hits,
                Mass = _player != null ? _player.Mass : 0f,
                MaxRadius = _maxRadius,
                Depth = depth,
                Seed = _round * 1009,
                TrailScore = _trailScore
            };
        }

        // ---------------------------------------------------------
        // HUD / camera / input
        // ---------------------------------------------------------

        private void UpdateHud()
        {
            if (_player == null) return;
            var portal = PortalPosition;
            var delta = _portalActive ? portal - _player.Position : Vector2.zero;
            float compass = _portalActive ? Mathf.Atan2(-delta.x, delta.y) * Mathf.Rad2Deg : 0f;
            float distance = _portalActive ? delta.magnitude * 100f : 0f;
            float r = _player.Position.magnitude;
            float horizonDanger = Mathf.Clamp01(1f - (r - RHorizon) / 5.2f);
            float massDanger = _player.Mass < 30f ? 0.7f : 0f;
            float nearPortal = _portalActive ? Mathf.Clamp01(1f - delta.magnitude / 5.0f) : 0f;
            int tideLevel = _runTime > 125f ? 2 : _runTime > 65f ? 1 : 0;
            string tideName = tideLevel == 0 ? "平静" : tideLevel == 1 ? "涌动" : "回响";
            var snapshot = new HudSnapshot
            {
                Round = _round,
                Mass = _player.Mass,
                Hits = _hits,
                Time = _runTime,
                PortalDistance = distance,
                PortalRemain = _portalRemain,
                PortalActive = _portalActive,
                CompassAngle = compass,
                TideName = tideName,
                TideLevel = tideLevel,
                Danger = Mathf.Max(horizonDanger, massDanger)
            };
            _ui.SetHud(snapshot);
            _ui.SetBoonProgress(_effectiveHits, 7, _boonsTaken > 0 ? _lastBoonTitle : null);
            _ui.SetStardust(_stardust, _stardustCombo, _stardustNextReward, _stardustShield, _stardustRewards >= StardustRewardCap, _trailScore);
            _ui.SetZoneGoal(_zoneDust[_currentZone], ZoneGoalTargets[_currentZone], _zoneGoalDone[_currentZone]);
            _ui.SetHintVisible(_runTime < 20f);
            _ui.SetSoundMuted(_muted);
            _audio?.SetDanger(horizonDanger, nearPortal);
            _lastDanger = horizonDanger;
            _lastNear = nearPortal;
            if (_guideBody != null && _guideBody.Dead) _guideBody = null;
            _maxRadius = Mathf.Max(_maxRadius, r);
        }

        private void UpdateCameraTransform()
        {
            if (_worldRoot == null) return;
            _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 1.35f);
            float shake = _reducedMotion ? Mathf.Min(_shake, 0.035f) : (_assist ? _shake * 0.55f : _shake);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * shake;
            _worldRoot.localScale = new Vector3(_cameraZoom, _cameraZoom, 1f);
            _worldRoot.localPosition = new Vector3(
                -_cameraFocus.x * _cameraZoom + offset.x,
                -_cameraFocus.y * _cameraZoom + offset.y,
                0f);
        }

        private void SetCameraTransformInstant()
        {
            if (_worldRoot == null) return;
            _worldRoot.localScale = new Vector3(_cameraZoom, _cameraZoom, 1f);
            _worldRoot.localPosition = new Vector3(-_cameraFocus.x * _cameraZoom, -_cameraFocus.y * _cameraZoom, 0f);
        }

        private void ShowToast(string message, float duration)
        {
            _ui.ShowToast(message, duration);
        }

        private void HideTransient()
        {
            _ui.HideToast(true);
            _ui.HideChapter();
        }

        private void ClearTurningInput()
        {
            // Old Input Manager is polled each frame; no cached axes to clear.
        }

        private void PauseParticles(bool pause)
        {
            for (int i = 0; i < _allParticles.Count; i++)
            {
                if (_allParticles[i] == null) continue;
                if (pause) _allParticles[i].Pause();
                else _allParticles[i].Play();
            }
        }

        // ---------------------------------------------------------
        // Frame loop
        // ---------------------------------------------------------

        private void Update()
        {
            if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
            {
                _lastScreenW = Screen.width;
                _lastScreenH = Screen.height;
                SetCameraSize();
            }

            HandleKeyboard();

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (!_focused && Phase == GamePhase.Playing) PauseGame();

            switch (Phase)
            {
                case GamePhase.Title:
                    UpdateTitle(dt);
                    break;
                case GamePhase.Playing:
                    if (!_transitioning)
                        UpdatePlaying(dt);
                    else
                        UpdateCameraTransform();
                    break;
                case GamePhase.Paused:
                    UpdateCameraTransform();
                    break;
                case GamePhase.Boon:
                    break;
                case GamePhase.DeathCinematic:
                    UpdateDeathCinematic(dt);
                    break;
                case GamePhase.WinCinematic:
                    UpdateWinCinematic(dt);
                    break;
                case GamePhase.End:
                    UpdateEnd(dt);
                    break;
            }

            if (Phase != GamePhase.Playing)
            {
                if (_slingRing != null && _slingRing.gameObject.activeSelf)
                    _slingRing.gameObject.SetActive(false);
                if (_slingTimingRing != null && _slingTimingRing.gameObject.activeSelf)
                    _slingTimingRing.gameObject.SetActive(false);
                if (_encounterRing != null && _encounterRing.gameObject.activeSelf)
                    _encounterRing.gameObject.SetActive(false);
            }
            HandleAudioFocus();
        }

        private int _lastScreenW;
        private int _lastScreenH;

        public void ToggleAssist()
        {
            _assist = !_assist;
            PlayerPrefs.SetInt("StarryAssist", _assist ? 1 : 0);
            PlayerPrefs.Save();
            _ui.SetAssist(_assist);
            if (!_assist && _encounterRing != null) _encounterRing.gameObject.SetActive(false);
            ShowToast(_assist
                ? "辅助模式 · 轨迹延长 / 最近交会 / 柔和转向"
                : "辅助模式 · 已关闭", 3f);
            _audio.PlayUi();
        }

        private void ToggleFullscreen()
        {
            bool isFullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;
            if (isFullscreen)
            {
                Screen.SetResolution(
                    Mathf.Max(960, _windowedWidth),
                    Mathf.Max(540, _windowedHeight),
                    FullScreenMode.Windowed);
                ShowToast("窗口模式 · F11 再次全屏", 2.2f);
            }
            else
            {
                if (Screen.width > 0 && Screen.height > 0)
                {
                    _windowedWidth = Screen.width;
                    _windowedHeight = Screen.height;
                }
                Screen.SetResolution(
                    Screen.currentResolution.width,
                    Screen.currentResolution.height,
                    FullScreenMode.FullScreenWindow);
                ShowToast("全屏 · F11 切回窗口", 2.2f);
            }
        }

        private int _windowedWidth = 1600;
        private int _windowedHeight = 900;

        private void HandleKeyboard()
        {
            bool altEnter = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
                            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
            if (Input.GetKeyDown(KeyCode.F11) || altEnter)
            {
                ToggleFullscreen();
                return;
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMute();
            }
            if (Phase != GamePhase.Boon && Input.GetKeyDown(KeyCode.V))
            {
                ToggleAssist();
            }
            switch (Phase)
            {
                case GamePhase.Title:
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                        StartFromTitle();
                    break;
                case GamePhase.Playing:
                    if (Input.GetKeyDown(KeyCode.Space)) TrySlingshot();
                    if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
                        TogglePause();
                    break;
                case GamePhase.Paused:
                    if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
                        ResumeFromPause();
                    break;
                case GamePhase.Boon:
                    int dir = 0;
                    if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = -1;
                    if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = 1;
                    if (dir != 0 && _boonOptions.Count > 0)
                    {
                        _boonSelection = (_boonSelection + dir + _boonOptions.Count) % _boonOptions.Count;
                        _ui.SetBoonSelection(_boonSelection);
                        _audio.PlayUi();
                    }
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                        ChooseBoon(_boonSelection);
                    break;
                case GamePhase.DeathCinematic:
                case GamePhase.WinCinematic:
                    if (Input.GetKeyDown(KeyCode.Space))
                        SkipCinematic();
                    break;
                case GamePhase.End:
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                        NextRoundFromEnd();
                    if (Input.GetKeyDown(KeyCode.Escape))
                        QuitToTitle();
                    break;
            }
        }

        private void UpdateEnd(float dt)
        {
            _globalTime += dt;
            UpdateShockwaves(dt);
            UpdateGenesisVisuals(dt);
            _uiTick -= dt;
            if (_autoRemain > 0f && !_transitioning)
            {
                _autoRemain -= dt;
                int seconds = Mathf.Max(0, Mathf.CeilToInt(_autoRemain));
                if (seconds != _autoDisplay)
                {
                    _autoDisplay = seconds;
                    _ui.SetAutoNext(seconds);
                }
                if (_autoRemain <= 0f)
                {
                    _autoRemain = -1f;
                    NextRoundFromEnd();
                }
            }
        }

        private void HandleAudioFocus()
        {
            // Music continues softly while paused thanks to AudioListener.pause.
        }

        private void OnApplicationFocus(bool focus)
        {
            _focused = focus;
            if (!focus && Phase == GamePhase.Playing && !_transitioning)
                PauseGame();
        }

        // ---------------------------------------------------------
        // Automation for art direction screenshots
        // ---------------------------------------------------------

        private void ParseAutomation()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-starryShot" && i + 1 < args.Length) _autoShotPath = args[++i];
                else if (args[i] == "-starryScene" && i + 1 < args.Length) _autoScene = args[++i];
            }
            if (!string.IsNullOrEmpty(_autoScene) && _autoScene.StartsWith("star"))
            {
                int forced;
                if (int.TryParse(_autoScene.Substring(4), out forced)) _forcedStarVariant = forced;
            }
        }

        private IEnumerator AutomationRoutine()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            if (string.IsNullOrEmpty(_autoScene) || _autoScene == "title")
            {
                yield return new WaitForSecondsRealtime(0.6f);
            }
            else if (_autoScene == "shower")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                SpawnMeteorShower();
                yield return new WaitForSecondsRealtime(2.0f);
            }
            else if (_autoScene == "showerwarn")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _runTime = 116f;
                yield return new WaitForSecondsRealtime(1.2f);
            }
            else if (_autoScene == "river")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                var p = new Vector2(Mathf.Cos(StartAngle), Mathf.Sin(StartAngle)) * RStart;
                _player.Position = p;
                _player.Velocity = new Vector2(-Mathf.Sin(StartAngle), Mathf.Cos(StartAngle)) * Mathf.Sqrt(Mu / RStart);
                _lastOrbitAngle = Mathf.Atan2(p.y, p.x);
                _hasOrbitAngle = true;
                _retroAngle = Mathf.PI * 6f - 0.08f;
                _cameraFocus = p;
                _cameraFocusTarget = p;
                SetCameraTransformInstant();
                yield return new WaitForSecondsRealtime(1.2f);
            }
            else if (_autoScene == "riverlap")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                var p = new Vector2(Mathf.Cos(StartAngle), Mathf.Sin(StartAngle)) * RStart;
                _player.Position = p;
                _player.Velocity = new Vector2(-Mathf.Sin(StartAngle), Mathf.Cos(StartAngle)) * Mathf.Sqrt(Mu / RStart);
                _lastOrbitAngle = Mathf.Atan2(p.y, p.x);
                _hasOrbitAngle = true;
                _retroAngle = Mathf.PI * 2f;
                _retroLapShown = 0;
                _cameraFocus = p;
                _cameraFocusTarget = p;
                SetCameraTransformInstant();
                yield return new WaitForSecondsRealtime(1.0f);
            }
            else if (_autoScene == "inner" || _autoScene == "outer")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                float rr = _autoScene == "inner" ? 4.8f : 16.4f;
                var p = new Vector2(Mathf.Cos(StartAngle), Mathf.Sin(StartAngle)) * rr;
                var t = new Vector2(Mathf.Sin(StartAngle), -Mathf.Cos(StartAngle));
                _player.Position = p;
                _player.Velocity = t * Mathf.Sqrt(Mu / rr);
                _currentZone = ZoneAt(rr);
                _backdrop.SetZone(_currentZone);
                _ui.SetZoneName(ZoneShort(_currentZone));
                _cameraFocus = p;
                _cameraFocusTarget = p;
                SetCameraTransformInstant();
                yield return new WaitForSecondsRealtime(1.4f);
            }
            else if (_autoScene != null && _autoScene.StartsWith("star"))
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _ui.SetEndVisible(false, true);
                _ui.SetPauseVisible(false, true);
                _ui.SetCinemaVisible(false, true);
                _ui.SetHudVisible(true, true);
                yield return new WaitForSecondsRealtime(1.5f);
            }
            else if (_autoScene == "play")
            {
                _ui.SetVeil(0f);
                SetupRun();
                _ui.ShowPlaying(true);
                _ui.SetEndVisible(false, true);
                _ui.SetPauseVisible(false, true);
                _ui.SetCinemaVisible(false, true);
                _ui.SetHudVisible(true, true);
                yield return new WaitForSecondsRealtime(1.5f);
            }
            else if (_autoScene == "start")
            {
                StartFromTitle();
                yield return new WaitForSecondsRealtime(4.2f);
            }
            else if (_autoScene == "death")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _ui.SetHudVisible(false);
                _ui.SetCinemaVisible(true);
                BeginDeath("mass");
                yield return new WaitForSecondsRealtime(3.2f);
            }
            else if (_autoScene == "genesis")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                BeginDeath("mass");
                yield return new WaitForSecondsRealtime(8.7f);
            }
            else if (_autoScene == "end")
            {
                SetupRun();
                var result = BuildResult(false, "mass");
                _lastResult = result;
                ShowEnd(result);
                yield return new WaitForSecondsRealtime(1.5f);
            }
            else if (_autoScene == "win")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                BeginWin();
                yield return new WaitForSecondsRealtime(1.4f);
            }
            else if (_autoScene == "newuniverse")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                BeginWin();
                yield return new WaitForSecondsRealtime(4.2f);
            }
            else if (_autoScene == "orbit")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _autoTurn = 1f;
                yield return new WaitForSecondsRealtime(3.6f);
                _autoTurn = 0f;
            }
            else if (_autoScene == "fullscreen")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                ToggleFullscreen();
                yield return new WaitForSecondsRealtime(2.0f);
            }
            else if (_autoScene == "boon")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _hits = 3;
                BeginBoon();
                yield return new WaitForSecondsRealtime(1.2f);
            }
            else if (_autoScene == "boonpick")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _hits = 3;
                BeginBoon();
                yield return new WaitForSecondsRealtime(0.7f);
                ChooseBoon(1);
                yield return new WaitForSecondsRealtime(1.4f);
            }
            else if (_autoScene == "boontrigger")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _hits = 6;
                _effectiveHits = 6;
                var triggerBody = AddBody(BodyKind.Planet, _player.Position + new Vector2(0.2f, 0.03f), Vector2.zero, 8801, false, false);
                if (triggerBody != null)
                {
                    triggerBody.Grace = 0f;
                    _player.HitCooldown = 0f;
                    HandlePlayerCollision(triggerBody);
                }
                yield return new WaitForSecondsRealtime(1.0f);
            }
            else if (_autoScene == "shrink")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _player.Mass = 24f;
                yield return new WaitForSecondsRealtime(1.2f);
            }
            else if (_autoScene == "sling")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                var nearBody = AddBody(BodyKind.Planet, _player.Position + new Vector2(1.35f, 0.15f), Vector2.zero, 7001, false, false);
                if (nearBody != null) nearBody.Grace = 0f;
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "slinguse")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                var nearBody = AddBody(BodyKind.Planet, _player.Position + new Vector2(1.35f, 0.15f), Vector2.zero, 7001, false, false);
                if (nearBody != null) nearBody.Grace = 0f;
                yield return new WaitForSecondsRealtime(0.35f);
                TrySlingshot();
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "thrust")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _autoTurn = 1f;
                yield return new WaitForSecondsRealtime(2.8f);
            }
            else if (_autoScene == "planetimpact")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _player.HitCooldown = 0f;
                var planet = AddBody(BodyKind.Planet, _player.Position + new Vector2(1.1f, 0f), Vector2.zero, 7101, false, false);
                if (planet != null) planet.Grace = 0f;
                yield return new WaitForSecondsRealtime(0.08f);
                if (planet != null) HandlePlayerCollision(planet);
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "planetshrink")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                var planet = AddBody(BodyKind.Planet, _player.Position + new Vector2(2.5f, 0f), Vector2.zero, 7102, false, false);
                if (planet != null) planet.Grace = 0f;
                yield return new WaitForSecondsRealtime(0.2f);
                if (planet != null)
                {
                    DamagePlanet(planet, planet.Position, 0.7f, true);
                    FlushFragments();
                }
                yield return new WaitForSecondsRealtime(0.7f);
            }
            else if (_autoScene == "portalwait")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _runTime = 55f;
                _portalRemain = 5f;
                UpdateHud();
                yield return new WaitForSecondsRealtime(1.2f);
            }
            else if (_autoScene == "portalactive")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _runTime = 60f;
                UpdatePortal(0f);
                _portalMoveTimer = 30f;
                _cameraLocked = true;
                _cameraFocus = _portalPosition;
                _cameraFocusTarget = _portalPosition;
                SetCameraTransformInstant();
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "portalwin")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _runTime = 60f;
                UpdatePortal(0f);
                _portalMoveTimer = 30f;
                _player.Position = _portalPosition;
                _player.Velocity = Vector2.zero;
                _player.HitCooldown = 99f;
                _cameraFocus = _portalPosition;
                _cameraFocusTarget = _portalPosition;
                SetCameraTransformInstant();
                yield return new WaitForSecondsRealtime(1.2f);
            }
            else if (_autoScene == "portalmove")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _runTime = 60f;
                UpdatePortal(0f);
                var before = _portalPosition;
                _portalMoveTimer = 0f;
                UpdatePortal(0f);
                _cameraLocked = true;
                _cameraFocus = _portalPosition;
                _cameraFocusTarget = _portalPosition;
                SetCameraTransformInstant();
                yield return new WaitForSecondsRealtime(0.8f);
                Debug.Log("PORTAL_MOVE " + before + " -> " + _portalPosition);
            }
            else if (_autoScene == "ecologyclose")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _player.Position = new Vector2(0f, 3.8f);
                _player.Velocity = new Vector2(1.4f, 0f);
                _cameraLocked = true;
                _cameraFocus = Vector2.zero;
                _cameraFocusTarget = Vector2.zero;
                SetCameraTransformInstant();
                _blackHolePhase = 2;
                _blackHoleTimer = 8f;
                _blackHoleActivity = 1f;
                _blackHole.SetActivity(1f);
                PulseBlackHole();
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "ecology")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _blackHolePhase = 2;
                _blackHoleTimer = 8f;
                _blackHoleActivity = 1f;
                _blackHole.SetActivity(1f);
                PulseBlackHole();
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "trace")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _player.HitCooldown = 0f;
                var nearBody = AddBody(BodyKind.Planet, _player.Position + new Vector2(1.5f, 0f), Vector2.zero, 7011, false, false);
                if (nearBody != null) nearBody.Grace = 0f;
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "slingready")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                var nearBody = AddBody(BodyKind.Planet, _player.Position + new Vector2(1.35f, 0.15f), Vector2.zero, 7001, false, false);
                if (nearBody != null) nearBody.Grace = 0f;
                yield return new WaitForSecondsRealtime(0.35f);
                _slingTimingPhase = 0.8f;
                yield return new WaitForSecondsRealtime(0.08f);
            }
            else if (_autoScene == "slingperfect")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                var nearBody = AddBody(BodyKind.Planet, _player.Position + new Vector2(1.35f, 0.15f), Vector2.zero, 7001, false, false);
                if (nearBody != null) nearBody.Grace = 0f;
                yield return new WaitForSecondsRealtime(0.35f);
                _slingTimingPhase = 0.82f;
                TrySlingshot();
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "dust")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                for (int i = 0; i < _stardustMotes.Count; i++)
                {
                    if (_stardustMotes[i].View != null) _stardustMotes[i].View.SetVisible(false);
                }
                _stardustMotes.Clear();
                var rand = new System.Random(4411);
                for (int i = 0; i < 30; i++)
                {
                    AddStardust(_player.Position + new Vector2(0.28f, 0f), rand);
                    yield return new WaitForSecondsRealtime(0.13f);
                }
                yield return new WaitForSecondsRealtime(0.8f);
            }
            else if (_autoScene == "assist")
            {
                _assist = true;
                _ui.SetAssist(true);
                SetupRun();
                _ui.ShowPlaying(true);
                var nearBody = AddBody(BodyKind.Planet, _player.Position + new Vector2(2.0f, 0.2f), Vector2.zero, 7002, false, false);
                if (nearBody != null) nearBody.Grace = 0f;
                yield return new WaitForSecondsRealtime(1.0f);
            }
            else if (_autoScene == "impact")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                var incoming = AddBody(BodyKind.Star, _player.Position + new Vector2(0.18f, 0.04f), Vector2.zero, 4242, false, false);
                if (incoming != null)
                {
                    incoming.Grace = 0f;
                    _player.HitCooldown = 0f;
                    HandlePlayerCollision(incoming);
                }
                yield return new WaitForSecondsRealtime(0.24f);
            }
            else if (_autoScene == "pause")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                yield return new WaitForSecondsRealtime(0.4f);
                PauseGame();
                yield return new WaitForSecondsRealtime(0.6f);
            }
            else if (_autoScene == "afterdeath")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _ui.SetHudVisible(false);
                _ui.SetCinemaVisible(true);
                BeginDeath("mass");
                yield return new WaitForSecondsRealtime(13.4f);
            }
            else if (_autoScene == "loop")
            {
                SetupRun();
                _ui.ShowPlaying(true);
                _ui.SetHudVisible(false);
                _ui.SetCinemaVisible(true);
                BeginDeath("mass");
                yield return new WaitForSecondsRealtime(25.5f);
            }
            else if (_autoScene == "round2")
            {
                _round = 2;
                _theme = StarPalette.Get(_round);
                _backdrop.ApplyTheme(_theme, _round);
                _blackHole.Build(_theme, RHorizon);
                _portal.Build(_theme);
                _ui.ApplyTheme(_theme);
                SetupRun();
                _ui.ShowPlaying(true);
                _ui.ClearFlash();
                _ui.SetDanger(0f);
                yield return new WaitForSecondsRealtime(2.0f);
            }
            yield return new WaitForSecondsRealtime(0.35f);
            Debug.Log("STARRY_STATE phase=" + Phase + " round=" + _round + " hits=" + _hits +
                " flash=" + _ui.FlashAlpha.ToString("F3") + " danger=" + _ui.DangerAlpha.ToString("F3") +
                " sling=" + _slingCharges + " trace=" + _starTraceCount + " mass=" + (_player != null ? _player.Mass.ToString("F1") : "0") +
                " player=" + (_player != null ? _player.Position.ToString() : "none") +
                " portalActive=" + _portalActive + " portal=" + PortalPosition + " dist=" + (_player != null ? Vector2.Distance(_player.Position, PortalPosition) : -1f));
            var dir = Path.GetDirectoryName(_autoShotPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(_autoShotPath);
            Debug.Log("STARRY_SHOT " + _autoShotPath);
            yield return new WaitForSecondsRealtime(1.2f);
            Application.Quit();
        }
    }
}
