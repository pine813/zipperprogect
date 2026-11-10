using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarryAbyss
{
    /// <summary>
    /// A quiet, calligraphic interface built at runtime from OS fonts and
    /// procedural textures. No prefab references, no imported UI sprites.
    /// </summary>
    public sealed class StarryUI : MonoBehaviour
    {
        private StarryGame _game;
        private StarTheme _theme;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Font _displayFont;
        private Font _bodyFont;
        private Font _latinFont;

        private CanvasGroup _titleGroup;
        private CanvasGroup _hudGroup;
        private CanvasGroup _pauseGroup;
        private CanvasGroup _endGroup;
        private CanvasGroup _cinemaGroup;
        private CanvasGroup _chapterGroup;
        private CanvasGroup _veilGroup;
        private CanvasGroup _dangerGroup;

        private Text _massValue;
        private Text _massPercent;
        private RectTransform _massFill;
        private Image _massFillImage;
        private Text _massNote;
        private RectTransform _minimapPlot;
        private RectTransform _minimapPlayer;
        private RectTransform _minimapGate;
        private RectTransform _minimapView;
        private readonly List<Image> _minimapDots = new List<Image>();

        private CanvasGroup _boonGroup;
        private float _boonTarget;
        private readonly BoonCard[] _boonCards = new BoonCard[3];

        private sealed class BoonCard
        {
            public GameObject Root;
            public Image Ink;
            public Image Frame;
            public Image Glyph;
            public Text Ordinal;
            public Text Title;
            public Text Effect;
            public Text Flavor;
            public Text Pick;
        }
        private Text _portalDistance;
        private Text _portalSub;
        private RectTransform _compassArrow;
        private Text _runStats;
        private Text _boonProgress;
        private Text _slingPrompt;
        private Text _assistLabel;
        private Text _starAspect;
        private string _starAspectName = "";
        private string _zoneName = "";
        private Color _starAspectColor = Color.white;
        private int _zoneGoalCurrent;
        private int _zoneGoalTarget;
        private bool _zoneGoalDone;
        private Text _stardustHud;
        private Text _boonSubtitle;
        private CanvasGroup _hintGroup;
        private float _hintTarget = 1f;
        private int _blackHoleState;
        private float _slingPromptTarget;
        private Text _chapterLabel;
        private Text _tideText;
        private Image _tideDot;
        private Text _soundButtonLabel;
        private Image _flashImage;

        private Text _endKicker;
        private Text _endTitle;
        private Text _endChapter;
        private Text _endPoem;
        private Text _endTime;
        private Text _endHits;
        private Text _endDepth;
        private Text _endMass;
        private Text _endMassLabel;
        private Text _autoNext;
        private Button _againButton;
        private Text _againLabel;
        private Text _againSub;
        private Text _titleRecord;

        private Text _cinemaKicker;
        private Text _cinemaTitle;
        private Text _cinemaSub;
        private Button _skipButton;
        private CanvasGroup _skipGroup;

        private RectTransform _chapterCardRect;
        private Text _chapterKicker;
        private Text _chapterTitle;
        private Text _chapterSub;

        private RectTransform _toastRect;
        private Text _toastText;
        private CanvasGroup _toastCanvasGroup;

        private Image _reticle;
        private RectTransform _reticleRect;
        private CanvasGroup _reticleGroup;
        private Text _reticleLabel;
        private RectTransform _portalMarkerRect;
        private CanvasGroup _portalMarkerGroup;
        private CanvasGroup _playerMarkerGroup;
        private RectTransform _playerMarkerRect;
        private bool _portalMarkerVisible;
        private bool _playerMarkerVisible;

        private readonly List<Impact> _impacts = new List<Impact>();
        private RectTransform _impactRoot;

        private float _titleTarget;
        private float _hudTarget;
        private float _pauseTarget;
        private float _endTarget;
        private float _cinemaTarget;
        private float _chapterTarget;
        private float _veilTarget;
        private float _toastTarget;
        private float _dangerTarget;
        private float _flashAlpha;
        private Color _flashColor = Color.white;
        private float _veilAlpha;
        private float _chapterHideAt;
        private float _toastHideAt;
        private bool _built;

        private sealed class Impact
        {
            public Text Text;
            public RectTransform Rect;
            public float Life;
            public float Duration;
            public Vector3 World;
            public Vector2 Drift;
        }

        public void Build(StarryGame game)
        {
            if (_built) return;
            _built = true;
            _game = game;
            _theme = StarPalette.Get(1);

            LoadFonts();
            BuildCanvas();
            BuildAtmosphere();
            BuildHud();
            BuildTitle();
            BuildPause();
            BuildEnd();
            BuildCinema();
            BuildTransient();
            BuildReticle();
            BuildImpacts();
            BuildBoons();
            BuildFlashAndVeil();

            ShowTitle(true);
            SetAutoNext(-1);
            SetHudVisible(false, true);
            SetCinemaVisible(false, true);
            SetEndVisible(false, true);
            SetPauseVisible(false, true);
            _chapterTarget = 0f;
            if (_chapterGroup != null) _chapterGroup.alpha = 0f;
            HideBoons(true);
            HideToast(true);
        }

        private void LoadFonts()
        {
            _displayFont = LoadFont(new[] { "STSong", "华文宋体", "Songti SC", "SimSun", "Noto Serif CJK SC", "Microsoft YaHei UI" }, 64);
            _bodyFont = LoadFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "PingFang SC", "SimSun" }, 32);
            _latinFont = LoadFont(new[] { "Georgia", "Palatino Linotype", "Baskerville", "Times New Roman", "Cambria" }, 48);
        }

        private static Font LoadFont(string[] names, int size)
        {
            try
            {
                var f = Font.CreateDynamicFontFromOSFont(names, size);
                if (f != null) return f;
            }
            catch
            {
                // Fall through to the built-in font.
            }
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void BuildCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _canvasRect = GetComponent<RectTransform>();
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(es);
            }
        }

        private void BuildAtmosphere()
        {
            var vignette = MakeImage("Vignette", transform, ProceduralArt.Vignette(), Color.white);
            Stretch(vignette.rectTransform);
            vignette.raycastTarget = false;
            vignette.preserveAspect = false;

            var grain = MakeImage("Grain", transform, ProceduralArt.Grain(), new Color(1f, 1f, 1f, 0.05f));
            Stretch(grain.rectTransform);
            grain.type = Image.Type.Tiled;
            grain.raycastTarget = false;

            var haze = MakeImage("Soft Haze", transform, ProceduralArt.Glow(256, 2.6f),
                new Color(0.86f, 0.78f, 0.67f, 0.02f));
            Stretch(haze.rectTransform);
            haze.preserveAspect = false;
            haze.raycastTarget = false;

            var danger = MakeImage("Danger", transform, ProceduralArt.Vignette(), new Color(0.32f, 0.055f, 0.03f, 0f));
            Stretch(danger.rectTransform);
            danger.raycastTarget = false;
            danger.preserveAspect = false;
            var group = danger.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            _dangerGroup = group;
            _dangerTarget = 0f;
        }

        // ---------------------------------------------------------
        // HUD
        // ---------------------------------------------------------

        private void BuildHud()
        {
            var root = MakeGroup("HUD", transform, out _hudGroup);
            var hud = root;

            var topScrim = MakeImage("Top Scrim", hud, ProceduralArt.VerticalFade(512, 2), new Color(0.006f, 0.017f, 0.03f, 0.82f));
            topScrim.rectTransform.anchorMin = new Vector2(0f, 1f);
            topScrim.rectTransform.anchorMax = new Vector2(1f, 1f);
            topScrim.rectTransform.pivot = new Vector2(0.5f, 1f);
            topScrim.rectTransform.anchoredPosition = Vector2.zero;
            topScrim.rectTransform.sizeDelta = new Vector2(0f, 220f);
            topScrim.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            topScrim.raycastTarget = false;
            var bottomScrim = MakeImage("Bottom Scrim", hud, ProceduralArt.VerticalFade(512, 2), new Color(0.006f, 0.017f, 0.03f, 0.64f));
            bottomScrim.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottomScrim.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottomScrim.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottomScrim.rectTransform.anchoredPosition = Vector2.zero;
            bottomScrim.rectTransform.sizeDelta = new Vector2(0f, 150f);
            bottomScrim.raycastTarget = false;

            var brand = new GameObject("Brand", typeof(RectTransform));
            var brandRect = brand.GetComponent<RectTransform>();
            brandRect.SetParent(hud, false);
            PinTopLeft(brandRect, 54, -30, 420, 70);

            var glyph = MakeImage("Glyph", brandRect, ProceduralArt.StarBody(), new Color(0.9f, 0.72f, 0.4f, 0.85f));
            glyph.rectTransform.anchorMin = new Vector2(0f, 1f);
            glyph.rectTransform.anchorMax = new Vector2(0f, 1f);
            glyph.rectTransform.pivot = new Vector2(0f, 1f);
            glyph.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            glyph.rectTransform.sizeDelta = new Vector2(38f, 38f);
            glyph.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
            glyph.raycastTarget = false;

            var brandTitle = MakeText("Brand Title", brandRect, "星 渊 归 途", _displayFont, 21, new Color(0.97f, 0.9f, 0.76f, 0.92f), TextAnchor.UpperLeft);
            Place(brandTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(52f, 1f), new Vector2(300f, 30f));
            var brandEn = MakeText("Brand EN", brandRect, "S T A R R Y   A B Y S S", _latinFont, 9, new Color(0.9f, 0.78f, 0.55f, 0.38f), TextAnchor.UpperLeft);
            Place(brandEn.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(53f, -26f), new Vector2(300f, 20f));

            BuildHudActions(hud);
            BuildMinimap(hud);
            BuildMassCard(hud);
            BuildObjective(hud);

            var tide = new GameObject("Tide", typeof(RectTransform));
            var tideRect = tide.GetComponent<RectTransform>();
            tideRect.SetParent(hud, false);
            Place(tideRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-54f, 92f), new Vector2(280f, 24f));
            var tideDotObj = MakeImage("Tide Dot", tideRect, ProceduralArt.Glow(64), new Color(0.55f, 0.82f, 0.78f, 0.9f));
            tideDotObj.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            tideDotObj.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            tideDotObj.rectTransform.pivot = new Vector2(1f, 0.5f);
            tideDotObj.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            tideDotObj.rectTransform.sizeDelta = new Vector2(7f, 7f);
            _tideDot = tideDotObj;
            _tideText = MakeText("Tide Text", tideRect, "引力潮汐 · 平静", _bodyFont, 13, new Color(0.93f, 0.86f, 0.73f, 0.42f), TextAnchor.MiddleRight);
            StretchWithMargins(_tideText.rectTransform, 0f, 0f, 0f, 0f);

            var meta = new GameObject("Run Meta", typeof(RectTransform));
            var metaRect = meta.GetComponent<RectTransform>();
            metaRect.SetParent(hud, false);
            Place(metaRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 38f), new Vector2(760f, 26f));
            var metaGroup = MakeText("Meta Text", metaRect, "第 001 夜    ·    借力 0    ·    00:00", _bodyFont, 14, new Color(0.92f, 0.85f, 0.72f, 0.38f), TextAnchor.MiddleLeft);
            Stretch(metaGroup.rectTransform);
            _runStats = metaGroup;

            var boonProgress = MakeText("Boon Progress", hud, "", _bodyFont, 12,
                new Color(0.88f, 0.8f, 0.64f, 0.34f), TextAnchor.MiddleLeft);
            Place(boonProgress.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 64f), new Vector2(660f, 22f));
            _boonProgress = boonProgress;

            var starAspect = MakeText("Star Aspect", hud, "", _bodyFont, 11,
                new Color(0.78f, 0.9f, 0.88f, 0.48f), TextAnchor.MiddleLeft);
            Place(starAspect.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 88f), new Vector2(620f, 20f));
            _starAspect = starAspect;

            var stardustHud = MakeText("Stardust", hud, "", _bodyFont, 11,
                new Color(0.94f, 0.82f, 0.58f, 0.58f), TextAnchor.MiddleLeft);
            Place(stardustHud.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 112f), new Vector2(520f, 20f));
            _stardustHud = stardustHud;

            var hint = MakeText("Hint", hud, "A / D  沿当前轨道逆 / 顺时针转      ·      Space 擦身 / 完美借力", _bodyFont, 13, new Color(0.92f, 0.85f, 0.72f, 0.3f), TextAnchor.MiddleCenter);
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 39f), new Vector2(1080f, 26f));
            _hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            _hintTarget = 1f;

            var slingPrompt = MakeText("Sling Prompt", hud, "", _bodyFont, 16,
                new Color(0.72f, 0.96f, 0.9f, 0f), TextAnchor.MiddleCenter);
            Place(slingPrompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(560f, 24f));
            AddShadow(slingPrompt, 0.9f, new Vector2(0f, -1f));
            AddOutline(slingPrompt, 0.9f, new Vector2(1.1f, -1.1f));
            _slingPrompt = slingPrompt;

            var chapter = MakeText("Chapter", hud, "", _bodyFont, 14, new Color(0.86f, 0.74f, 0.51f, 0.6f), TextAnchor.MiddleLeft);
            Place(chapter.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 64f), new Vector2(300f, 24f));
            _chapterLabel = chapter;
            _chapterLabel.gameObject.SetActive(false);

            BuildPortalMarker(hud);
            BuildPlayerMarker(hud);
        }

        private void BuildPortalMarker(Transform hud)
        {
            var go = new GameObject("Gate Marker", typeof(RectTransform));
            _portalMarkerRect = go.GetComponent<RectTransform>();
            _portalMarkerRect.SetParent(hud, false);
            _portalMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
            _portalMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
            _portalMarkerRect.pivot = new Vector2(0.5f, 1f);
            _portalMarkerRect.sizeDelta = new Vector2(190f, 54f);
            _portalMarkerGroup = go.AddComponent<CanvasGroup>();
            _portalMarkerGroup.alpha = 0f;
            _portalMarkerGroup.blocksRaycasts = false;
            _portalMarkerGroup.interactable = false;
            var line = MakeImage("Tether", _portalMarkerRect, ProceduralArt.WhiteSprite, new Color(0.93f, 0.81f, 0.56f, 0.35f));
            line.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            line.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            line.rectTransform.pivot = new Vector2(0.5f, 1f);
            line.rectTransform.anchoredPosition = Vector2.zero;
            line.rectTransform.sizeDelta = new Vector2(1f, 18f);
            line.raycastTarget = false;
            var label = MakeText("Gate Label", _portalMarkerRect, "归  航  阵", _displayFont, 15, new Color(0.98f, 0.9f, 0.73f, 0.92f), TextAnchor.UpperCenter);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(190f, 24f));
            AddOutline(label, 0.65f, new Vector2(1f, -1f));
            var en = MakeText("Gate EN", _portalMarkerRect, "W A Y F A R E R ' S   G A T E", _latinFont, 8, new Color(0.94f, 0.84f, 0.62f, 0.55f), TextAnchor.UpperCenter);
            Place(en.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(190f, 16f));
        }

        private void BuildPlayerMarker(Transform hud)
        {
            var go = new GameObject("Player Marker", typeof(RectTransform));
            _playerMarkerRect = go.GetComponent<RectTransform>();
            _playerMarkerRect.SetParent(hud, false);
            _playerMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
            _playerMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
            _playerMarkerRect.pivot = new Vector2(0.5f, 0f);
            _playerMarkerRect.sizeDelta = new Vector2(120f, 46f);
            _playerMarkerGroup = go.AddComponent<CanvasGroup>();
            _playerMarkerGroup.alpha = 0f;
            _playerMarkerGroup.blocksRaycasts = false;
            _playerMarkerGroup.interactable = false;
            var caret = MakeImage("Caret", _playerMarkerRect, ProceduralArt.Diamond(), new Color(0.98f, 0.84f, 0.57f, 0.9f));
            caret.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            caret.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            caret.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            caret.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            caret.rectTransform.sizeDelta = new Vector2(11f, 11f);
            caret.raycastTarget = false;
            var pin = MakeImage("Pin", _playerMarkerRect, ProceduralArt.WhiteSprite, new Color(0.95f, 0.82f, 0.54f, 0.4f));
            pin.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            pin.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            pin.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pin.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            pin.rectTransform.sizeDelta = new Vector2(1f, -14f);
            pin.raycastTarget = false;
            var label = MakeText("Player Label", _playerMarkerRect, "你", _displayFont, 14, new Color(0.98f, 0.9f, 0.72f, 0.86f), TextAnchor.LowerCenter);
            Place(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(120f, 22f));
            AddShadow(label, 0.8f, new Vector2(0f, -1f));
        }

        private void BuildHudActions(Transform hud)
        {
            var row = new GameObject("Actions", typeof(RectTransform));
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.SetParent(hud, false);
            rowRect.anchorMin = new Vector2(1f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(1f, 1f);
            rowRect.anchoredPosition = new Vector2(-54f, -30f);
            rowRect.sizeDelta = new Vector2(260f, 42f);

            var sound = MakeGhostButton("Sound", rowRect, "♪   乐 音", new Vector2(-102f, 0f), new Vector2(116f, 38f), _game.ToggleMute);
            _soundButtonLabel = sound.GetComponentInChildren<Text>();
            var pause = MakeGhostButton("Pause", rowRect, "Ⅱ   暂 停", new Vector2(0f, 0f), new Vector2(100f, 38f), _game.TogglePause);
        }

        private void BuildMinimap(Transform hud)
        {
            var root = new GameObject("Minimap", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            PinTopLeft(rect, 54, -104, 170, 178);

            var disc = MakeImage("Minimap Disc", rect, ProceduralArt.BlackDisk(256), new Color(0.004f, 0.012f, 0.024f, 0.9f));
            PinTopLeft(disc.rectTransform, 0, 0, 152, 152);
            disc.raycastTarget = false;
            var border = MakeImage("Minimap Border", rect, ProceduralArt.CircleOutline(256, 0.48f, 1.4f), new Color(0.9f, 0.78f, 0.52f, 0.72f));
            PinTopLeft(border.rectTransform, -3, -3, 158, 158);
            border.raycastTarget = false;
            var borderInner = MakeImage("Minimap Border Inner", rect, ProceduralArt.CircleOutline(256, 0.4f, 0.6f), new Color(0.68f, 0.84f, 0.8f, 0.18f));
            PinTopLeft(borderInner.rectTransform, 6, 6, 140, 140);
            borderInner.raycastTarget = false;
            var inner = MakeImage("Minimap Inner", rect, ProceduralArt.CircleOutline(256, 0.28f, 0.55f), new Color(0.72f, 0.84f, 0.78f, 0.08f));
            PinTopLeft(inner.rectTransform, 29, 29, 94, 94);
            inner.raycastTarget = false;

            var plot = new GameObject("Minimap Plot", typeof(RectTransform));
            _minimapPlot = plot.GetComponent<RectTransform>();
            _minimapPlot.SetParent(rect, false);
            _minimapPlot.anchorMin = new Vector2(0f, 1f);
            _minimapPlot.anchorMax = new Vector2(0f, 1f);
            _minimapPlot.pivot = new Vector2(0.5f, 0.5f);
            _minimapPlot.anchoredPosition = new Vector2(76f, -76f);
            _minimapPlot.sizeDelta = Vector2.zero;

            var viewGo = MakeImage("Minimap View", _minimapPlot, ProceduralArt.Frame(), new Color(0.67f, 0.84f, 0.79f, 0.11f));
            _minimapView = viewGo.rectTransform;
            _minimapView.anchorMin = new Vector2(0.5f, 0.5f);
            _minimapView.anchorMax = new Vector2(0.5f, 0.5f);
            _minimapView.pivot = new Vector2(0.5f, 0.5f);
            _minimapView.anchoredPosition = Vector2.zero;
            _minimapView.sizeDelta = new Vector2(58f, 32f);
            viewGo.type = Image.Type.Sliced;
            viewGo.raycastTarget = false;
            viewGo.enabled = false;

            var hole = MakeImage("Minimap Abyss", _minimapPlot, ProceduralArt.BlackDisk(128), new Color(0.001f, 0.004f, 0.008f, 0.95f));
            hole.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hole.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hole.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hole.rectTransform.sizeDelta = new Vector2(15f, 15f);
            hole.raycastTarget = false;
            var holeRim = MakeImage("Minimap Horizon", _minimapPlot, ProceduralArt.CircleOutline(128, 0.42f, 1.1f), new Color(0.95f, 0.82f, 0.57f, 0.65f));
            holeRim.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            holeRim.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            holeRim.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            holeRim.rectTransform.sizeDelta = new Vector2(19f, 19f);
            holeRim.raycastTarget = false;

            var gate = MakeImage("Minimap Gate", _minimapPlot, ProceduralArt.Diamond(), new Color(0.96f, 0.82f, 0.52f, 1f));
            _minimapGate = gate.rectTransform;
            _minimapGate.anchorMin = new Vector2(0.5f, 0.5f);
            _minimapGate.anchorMax = new Vector2(0.5f, 0.5f);
            _minimapGate.pivot = new Vector2(0.5f, 0.5f);
            _minimapGate.sizeDelta = new Vector2(11f, 11f);
            gate.raycastTarget = false;

            var player = MakeImage("Minimap Player", _minimapPlot, ProceduralArt.Diamond(), new Color(0.98f, 0.94f, 0.82f, 1f));
            _minimapPlayer = player.rectTransform;
            _minimapPlayer.anchorMin = new Vector2(0.5f, 0.5f);
            _minimapPlayer.anchorMax = new Vector2(0.5f, 0.5f);
            _minimapPlayer.pivot = new Vector2(0.5f, 0.5f);
            _minimapPlayer.sizeDelta = new Vector2(9f, 9f);
            player.raycastTarget = false;

            for (int i = 0; i < 18; i++)
            {
                var dot = MakeImage("Minimap Body " + i, _minimapPlot, ProceduralArt.SoftDot(), Color.clear);
                dot.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                dot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                dot.rectTransform.sizeDelta = new Vector2(3f, 3f);
                dot.raycastTarget = false;
                dot.gameObject.SetActive(false);
                _minimapDots.Add(dot);
            }

            hole.transform.SetAsLastSibling();
            holeRim.transform.SetAsLastSibling();
            _minimapGate.SetAsLastSibling();
            _minimapPlayer.SetAsLastSibling();

            var captionZh = MakeText("Minimap Caption", rect, "星 图", _displayFont, 12, new Color(0.9f, 0.82f, 0.66f, 0.6f), TextAnchor.MiddleLeft);
            Place(captionZh.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -157f), new Vector2(90f, 18f));
            var caption = MakeText("Minimap Caption En", rect, "O R B I T A L   C H A R T", _latinFont, 7, new Color(0.86f, 0.78f, 0.61f, 0.32f), TextAnchor.MiddleLeft);
            Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -159f), new Vector2(150f, 16f));
            var north = MakeText("Minimap North", rect, "N", _latinFont, 8, new Color(0.88f, 0.78f, 0.58f, 0.34f), TextAnchor.MiddleCenter);
            Place(north.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -8f), new Vector2(18f, 16f));
        }

        public void SetMinimap(Vector2 player, Vector2 portal, IList<CelestialBody> bodies, float worldRadius, bool portalVisible = true)
        {
            if (_minimapPlot == null) return;
            const float mapRadius = 68f;
            float scale = mapRadius / Mathf.Max(1f, worldRadius);
            _minimapPlayer.anchoredPosition = player * scale;
            _minimapGate.gameObject.SetActive(portalVisible);
            _minimapGate.anchoredPosition = portal * scale;
            _minimapView.anchoredPosition = player * scale;
            if (Camera.main != null)
            {
                float halfH = Camera.main.orthographicSize;
                float halfW = halfH * Camera.main.aspect;
                _minimapView.sizeDelta = new Vector2(Mathf.Max(20f, halfW * 2f * scale), Mathf.Max(12f, halfH * 2f * scale));
            }
            int visible = 0;
            for (int i = 0; i < bodies.Count && visible < _minimapDots.Count; i++)
            {
                var b = bodies[i];
                if (b == null || b.Dead || b.Kind == BodyKind.Dust) continue;
                var dot = _minimapDots[visible++];
                dot.gameObject.SetActive(true);
                dot.rectTransform.anchoredPosition = b.Position * scale;
                float px = b.Kind == BodyKind.Star ? 5.5f : b.Kind == BodyKind.Planet ? 4.2f : 2.8f;
                dot.rectTransform.sizeDelta = new Vector2(px, px);
                var c = b.Color;
                c.a = 0.92f;
                dot.color = c;
            }
            for (int i = visible; i < _minimapDots.Count; i++)
                _minimapDots[i].gameObject.SetActive(false);
        }

        private void BuildMassCard(Transform hud)
        {
            var card = new GameObject("Matter", typeof(RectTransform));
            var rect = card.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            PinTopLeft(rect, 328, -136, 286, 160);

            var micro = MakeText("Micro", rect, "S T E L L A R   M A T T E R", _latinFont, 10, new Color(0.86f, 0.77f, 0.61f, 0.34f), TextAnchor.UpperLeft);
            Place(micro.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(300f, 18f));

            _massValue = MakeText("Mass Value", rect, "100", _latinFont, 38, new Color(0.94f, 0.78f, 0.47f, 0.92f), TextAnchor.UpperRight);
            Place(_massValue.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -6f), new Vector2(130f, 44f));
            _massPercent = MakeText("Mass Percent", rect, "%", _latinFont, 16, new Color(0.86f, 0.77f, 0.61f, 0.4f), TextAnchor.UpperRight);
            Place(_massPercent.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -22f), new Vector2(22f, 22f));

            var label = MakeText("Mass Label", rect, "星 质", _displayFont, 17, new Color(0.91f, 0.84f, 0.7f, 0.55f), TextAnchor.UpperLeft);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -52f), new Vector2(120f, 28f));

            var track = MakeImage("Track", rect, ProceduralArt.WhiteSprite, new Color(0.9f, 0.84f, 0.7f, 0.1f));
            Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -88f), new Vector2(286f, 2f));
            track.raycastTarget = false;

            var fill = MakeImage("Fill", track.rectTransform, ProceduralArt.WhiteSprite, new Color(0.92f, 0.74f, 0.43f, 0.95f));
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            fill.raycastTarget = false;
            _massFill = fill.rectTransform;
            _massFillImage = fill;

            var diamond = MakeImage("Diamond", fill.rectTransform, ProceduralArt.Diamond(), new Color(1f, 0.96f, 0.83f, 0.9f));
            diamond.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            diamond.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            diamond.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            diamond.rectTransform.anchoredPosition = Vector2.zero;
            diamond.rectTransform.sizeDelta = new Vector2(8f, 8f);
            diamond.raycastTarget = false;

            _massNote = MakeText("Mass Note", rect, "每一次相撞，都会失去一部分自己", _bodyFont, 12, new Color(0.86f, 0.79f, 0.65f, 0.28f), TextAnchor.UpperLeft);
            Place(_massNote.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -108f), new Vector2(286f, 32f));
        }

        private void BuildObjective(Transform hud)
        {
            var root = new GameObject("Objective", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-54f, -104f);
            rect.sizeDelta = new Vector2(360f, 128f);

            var dial = MakeImage("Compass", rect, ProceduralArt.CircleOutline(128, 0.47f, 1.1f), new Color(0.9f, 0.8f, 0.61f, 0.42f));
            dial.rectTransform.anchorMin = new Vector2(1f, 1f);
            dial.rectTransform.anchorMax = new Vector2(1f, 1f);
            dial.rectTransform.pivot = new Vector2(1f, 1f);
            dial.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            dial.rectTransform.sizeDelta = new Vector2(74f, 74f);
            dial.raycastTarget = false;
            var dialInk = MakeImage("Compass Ink", dial.rectTransform, ProceduralArt.BlackDisk(256), new Color(0.004f, 0.012f, 0.022f, 0.52f));
            Stretch(dialInk.rectTransform);
            dialInk.raycastTarget = false;

            var inner = MakeImage("Compass Inner", dial.rectTransform, ProceduralArt.CircleOutline(128, 0.28f, 0.6f), new Color(0.9f, 0.8f, 0.61f, 0.22f));
            Stretch(inner.rectTransform);
            inner.raycastTarget = false;

            var arrow = MakeImage("Compass Arrow", dial.rectTransform, ProceduralArt.Arrow(), new Color(0.96f, 0.83f, 0.57f, 0.95f));
            arrow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            arrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            arrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            arrow.rectTransform.anchoredPosition = Vector2.zero;
            arrow.rectTransform.sizeDelta = new Vector2(42f, 42f);
            arrow.raycastTarget = false;
            _compassArrow = arrow.rectTransform;

            var n = MakeText("N", dial.rectTransform, "N", _latinFont, 9, new Color(0.9f, 0.8f, 0.61f, 0.25f), TextAnchor.UpperCenter);
            Place(n.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(20f, 16f));

            var micro = MakeText("Objective Micro", rect, "W A Y F A R E R ' S   G A T E   ·   归 航 阵", _latinFont, 10, new Color(0.86f, 0.77f, 0.61f, 0.34f), TextAnchor.UpperRight);
            Place(micro.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, -2f), new Vector2(300f, 18f));

            _portalDistance = MakeText("Portal Distance", rect, "—", _latinFont, 33, new Color(0.97f, 0.9f, 0.76f, 0.9f), TextAnchor.UpperRight);
            Place(_portalDistance.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, -26f), new Vector2(280f, 44f));

            _portalSub = MakeText("Objective Sub", rect, "航行一分钟后，归航阵才会显现", _bodyFont, 12, new Color(0.84f, 0.77f, 0.63f, 0.3f), TextAnchor.UpperRight);
            Place(_portalSub.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, -66f), new Vector2(280f, 24f));
        }

        // ---------------------------------------------------------
        // Title
        // ---------------------------------------------------------

        private void BuildTitle()
        {
            var root = MakeGroup("Title", transform, out _titleGroup);
            var shade = MakeImage("Title Shade", root, ProceduralArt.HorizontalFade(), new Color(0.012f, 0.035f, 0.065f, 0.9f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            var bottom = MakeImage("Title Bottom", root, ProceduralArt.Vignette(), new Color(0.004f, 0.012f, 0.024f, 0.72f));
            Stretch(bottom.rectTransform);
            bottom.raycastTarget = false;

            var ornament = MakeImage("Orbit", root, ProceduralArt.CircleOutline(512, 0.49f, 0.7f), new Color(0.86f, 0.74f, 0.46f, 0.07f));
            ornament.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            ornament.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            ornament.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ornament.rectTransform.anchoredPosition = new Vector2(-220f, 110f);
            ornament.rectTransform.sizeDelta = new Vector2(1150f, 1150f);
            ornament.raycastTarget = false;

            var content = new GameObject("Title Content", typeof(RectTransform));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.SetParent(root, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = new Vector2(126f, -125f);
            contentRect.sizeDelta = new Vector2(1000f, 740f);

            var kicker = MakeText("Kicker", contentRect, "1 8 7 2   ·   星 夜 之 后        A   P A I N T E D   C O S M O S", _latinFont, 12,
                new Color(0.88f, 0.78f, 0.6f, 0.44f), TextAnchor.UpperLeft);
            Place(kicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(900f, 22f));

            var title = MakeText("Game Title", contentRect,
                "<color=#F6EDD7>星  渊</color>   <color=#D9B36F>归  途</color>",
                _displayFont, 128, Color.white, TextAnchor.UpperLeft);
            title.fontStyle = FontStyle.Normal;
            title.lineSpacing = 0.9f;
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-3f, -38f), new Vector2(1120f, 170f));

            var titleEn = MakeText("Title EN", contentRect, "S T A R R Y   A B Y S S   ·   T H E   W A Y   H O M E",
                _latinFont, 13, new Color(0.88f, 0.78f, 0.6f, 0.42f), TextAnchor.UpperLeft);
            Place(titleEn.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(5f, -196f), new Vector2(900f, 24f));

            var brush = MakeImage("Brush Line", contentRect, ProceduralArt.WhiteSprite, new Color(0.88f, 0.74f, 0.48f, 0.34f));
            Place(brush.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(5f, -226f), new Vector2(410f, 1f));
            brush.raycastTarget = false;

            var copy = MakeText("Intro Copy", contentRect,
                "黑洞正在吞下最后的光。\n你是一颗恒星，不能横移，只能沿当前轨道逆时针或顺时针转向。\n航行一分钟后，归航阵才会显现；它会不断移动。",
                _displayFont, 19, new Color(0.94f, 0.88f, 0.77f, 0.63f), TextAnchor.UpperLeft);
            copy.lineSpacing = 1.6f;
            Place(copy.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(5f, -274f), new Vector2(780f, 130f));

            var button = MakePrimaryButton("Start", contentRect, "启 程", "E N T E R   T H E   N I G H T", new Vector2(5f, -414f), new Vector2(330f, 82f), _game.StartFromTitle);
            var note = MakeText("Sound Note", contentRect, "建议佩戴耳机   ·   乐音将在启程时苏醒   ·   F11 全屏", _bodyFont, 12,
                new Color(0.87f, 0.81f, 0.68f, 0.28f), TextAnchor.UpperLeft);
            Place(note.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -512f), new Vector2(600f, 24f));

            // Three quiet rules across the lower part of the screen.
            var rules = new GameObject("Rules", typeof(RectTransform));
            var rulesRect = rules.GetComponent<RectTransform>();
            rulesRect.SetParent(root, false);
            rulesRect.anchorMin = new Vector2(0f, 0f);
            rulesRect.anchorMax = new Vector2(1f, 0f);
            rulesRect.pivot = new Vector2(0.5f, 0f);
            rulesRect.anchoredPosition = new Vector2(0f, 78f);
            rulesRect.sizeDelta = new Vector2(-252f, 132f);
            var line = MakeImage("Rule Line", rulesRect, ProceduralArt.WhiteSprite, new Color(0.88f, 0.74f, 0.48f, 0.15f));
            line.rectTransform.anchorMin = new Vector2(0f, 1f);
            line.rectTransform.anchorMax = new Vector2(1f, 1f);
            line.rectTransform.pivot = new Vector2(0.5f, 1f);
            line.rectTransform.anchoredPosition = Vector2.zero;
            line.rectTransform.sizeDelta = new Vector2(0f, 1f);
            line.raycastTarget = false;

            for (int i = 0; i < 3; i++)
            {
                string[] nums = { "0 1", "0 2", "0 3" };
                string[] heads = { "只 可 转 向", "以 撞 借 力", "抵 达 归 航 阵" };
                string[] copies =
                {
                    "A / D 沿轨道转向，靠近天体时 Space 擦身 / 完美借力。惯性与引力，会替你记得每一步。",
                    "擦过天体外缘，以更少的星质换取更巧的偏转。角度，就是你的引擎。",
                    "星质耗尽或坠入视界，所有物质将归于一点，再爆炸成新的宇宙。"
                };
                var col = new GameObject("Rule " + i, typeof(RectTransform));
                var colRect = col.GetComponent<RectTransform>();
                colRect.SetParent(rulesRect, false);
                colRect.anchorMin = new Vector2(i / 3f, 0f);
                colRect.anchorMax = new Vector2((i + 1) / 3f, 1f);
                colRect.pivot = new Vector2(0f, 1f);
                colRect.offsetMin = new Vector2(0f, 0f);
                colRect.offsetMax = new Vector2(-30f, -18f);
                var num = MakeText("Num", colRect, nums[i], _latinFont, 12, new Color(0.88f, 0.74f, 0.48f, 0.55f), TextAnchor.UpperLeft);
                Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(100f, 20f));
                var head = MakeText("Head", colRect, heads[i], _displayFont, 19, new Color(0.95f, 0.88f, 0.74f, 0.78f), TextAnchor.UpperLeft);
                Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -25f), new Vector2(400f, 28f));
                var body = MakeText("Copy", colRect, copies[i], _bodyFont, 12, new Color(0.89f, 0.82f, 0.69f, 0.33f), TextAnchor.UpperLeft);
                body.lineSpacing = 1.45f;
                Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -58f), new Vector2(432f, 60f));
            }

            _titleRecord = MakeText("Title Record", root, "最深航迹 —   ·   最长航时 —   ·   星相图鉴 0 / 6", _bodyFont, 11,
                new Color(0.87f, 0.8f, 0.66f, 0.25f), TextAnchor.LowerLeft);
            Place(_titleRecord.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(126f, 26f), new Vector2(560f, 22f));
            var signature = MakeText("Signature", root, "A F T E R   V A N   G O G H   ·   A   M E D I T A T I O N   I N   C O L L I S I O N",
                _latinFont, 10, new Color(0.86f, 0.78f, 0.62f, 0.16f), TextAnchor.LowerRight);
            Place(signature.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-126f, 26f), new Vector2(800f, 22f));
        }

        // ---------------------------------------------------------
        // Pause
        // ---------------------------------------------------------

        private void BuildPause()
        {
            var root = MakeGroup("Pause", transform, out _pauseGroup);
            var shade = MakeImage("Pause Shade", root, ProceduralArt.WhiteSprite, new Color(0.007f, 0.021f, 0.037f, 1f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = true;
            var shadeGlow = MakeImage("Pause Shade Glow", root, ProceduralArt.BlackDisk(256), new Color(0.003f, 0.01f, 0.02f, 0.35f));
            shadeGlow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            shadeGlow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            shadeGlow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            shadeGlow.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            shadeGlow.rectTransform.sizeDelta = new Vector2(1450f, 880f);
            shadeGlow.raycastTarget = false;

            var panel = new GameObject("Pause Panel", typeof(RectTransform));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(root, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 12f);
            panelRect.sizeDelta = new Vector2(720f, 560f);

            var panelInk = MakeImage("Panel Ink", panelRect, ProceduralArt.WhiteSprite, new Color(0.007f, 0.022f, 0.038f, 0.995f));
            Stretch(panelInk.rectTransform);
            panelInk.rectTransform.offsetMin = new Vector2(-10f, -8f);
            panelInk.rectTransform.offsetMax = new Vector2(10f, 8f);
            panelInk.raycastTarget = false;
            var panelFrame = MakeImage("Panel Frame", panelRect, ProceduralArt.Frame(), new Color(0.9f, 0.77f, 0.53f, 0.28f));
            Stretch(panelFrame.rectTransform);
            panelFrame.rectTransform.offsetMin = new Vector2(-10f, -8f);
            panelFrame.rectTransform.offsetMax = new Vector2(10f, 8f);
            panelFrame.type = Image.Type.Sliced;
            panelFrame.raycastTarget = false;
            var panelContent = new GameObject("Pause Content", typeof(RectTransform));
            var panelContentRect = panelContent.GetComponent<RectTransform>();
            panelContentRect.SetParent(panelRect, false);
            Stretch(panelContentRect);
            panelContentRect.offsetMin = new Vector2(54f, 32f);
            panelContentRect.offsetMax = new Vector2(-54f, -30f);

            var top = MakeImage("Pause Line", panelContentRect, ProceduralArt.WhiteSprite, new Color(0.88f, 0.74f, 0.48f, 0.3f));
            Place(top.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(530f, 1f));
            top.raycastTarget = false;

            var kicker = MakeText("Pause Kicker", panelContentRect, "P A U S E      ·      星 夜 暂 歇", _latinFont, 11, new Color(0.86f, 0.73f, 0.49f, 0.55f), TextAnchor.UpperLeft);
            Place(kicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -28f), new Vector2(500f, 20f));
            var title = MakeText("Pause Title", panelContentRect, "引 力 仍 在 等 待", _displayFont, 43, new Color(0.96f, 0.9f, 0.79f, 0.95f), TextAnchor.UpperLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -64f), new Vector2(560f, 70f));
            var copy = MakeText("Pause Copy", panelContentRect, "星尘悬浮于半空，黑洞的呼吸声暂时变轻了。", _displayFont, 14, new Color(0.88f, 0.82f, 0.7f, 0.45f), TextAnchor.UpperLeft);
            Place(copy.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -138f), new Vector2(560f, 28f));

            var resume = MakePrimaryButton("Resume", panelContentRect, "继 续 航 行", "R E S U M E", new Vector2(0f, -200f), new Vector2(270f, 66f), _game.ResumeFromPause);
            var assistBtn = MakeTextButton("Assist", panelContentRect, "辅 助 模 式   ·   关", new Vector2(0f, -264f), new Vector2(260f, 34f), _game.ToggleAssist);
            _assistLabel = assistBtn.GetComponentInChildren<Text>();
            var restart = MakeTextButton("Restart", panelContentRect, "重 开 此 夜", new Vector2(0f, -306f), new Vector2(220f, 36f), _game.RestartFromPause);
            var quit = MakeTextButton("Quit", panelContentRect, "回 到 星 图", new Vector2(0f, -348f), new Vector2(220f, 36f), _game.QuitToTitle);
            var hint = MakeText("Pause Hint", panelContentRect, "P / ESC  继续      ·      M  静音      ·      F11  全屏      ·      V  辅助", _bodyFont, 11, new Color(0.88f, 0.81f, 0.68f, 0.28f), TextAnchor.UpperLeft);
            Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -400f), new Vector2(560f, 22f));
            var bottom = MakeImage("Pause Line Bottom", panelContentRect, ProceduralArt.WhiteSprite, new Color(0.88f, 0.74f, 0.48f, 0.12f));
            Place(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(360f, 1f));
            bottom.raycastTarget = false;
        }

        // ---------------------------------------------------------
        // End
        // ---------------------------------------------------------

        private void BuildEnd()
        {
            var root = MakeGroup("End", transform, out _endGroup);
            var shade = MakeImage("End Shade", root, ProceduralArt.HorizontalFade(), new Color(0.01f, 0.03f, 0.055f, 0.94f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = true;
            var bottom = MakeImage("End Vignette", root, ProceduralArt.Vignette(), new Color(0.005f, 0.015f, 0.027f, 0.52f));
            Stretch(bottom.rectTransform);
            bottom.raycastTarget = false;

            // A soft pool of ink behind the epitaph keeps the statistics legible
            // even when the newborn universe is glowing directly behind them.
            var endWash = MakeImage("End Wash", root, ProceduralArt.Glow(256, 2.2f), new Color(0.004f, 0.012f, 0.023f, 0.78f));
            endWash.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            endWash.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            endWash.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            endWash.rectTransform.anchoredPosition = new Vector2(530f, 0f);
            endWash.rectTransform.sizeDelta = new Vector2(1680f, 980f);
            endWash.raycastTarget = false;

            var content = new GameObject("End Content", typeof(RectTransform));
            var c = content.GetComponent<RectTransform>();
            c.SetParent(root, false);
            c.anchorMin = new Vector2(0f, 0.5f);
            c.anchorMax = new Vector2(0f, 0.5f);
            c.pivot = new Vector2(0f, 0.5f);
            c.anchoredPosition = new Vector2(144f, 6f);
            c.sizeDelta = new Vector2(900f, 760f);

            var topLine = MakeImage("End Line", c, ProceduralArt.WhiteSprite, new Color(0.9f, 0.76f, 0.5f, 0.34f));
            Place(topLine.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -34f), new Vector2(740f, 1f));
            topLine.raycastTarget = false;

            _endKicker = MakeText("End Kicker", c, "T H E   L A S T   O R B I T", _latinFont, 11, new Color(0.88f, 0.73f, 0.46f, 0.75f), TextAnchor.UpperLeft);
            Place(_endKicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -68f), new Vector2(700f, 22f));
            _endTitle = MakeText("End Title", c, "归 于 一 点", _displayFont, 78, new Color(0.98f, 0.93f, 0.82f, 1f), TextAnchor.UpperLeft);
            Place(_endTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -102f), new Vector2(880f, 102f));
            _endChapter = MakeText("End Chapter", c, "第 001 夜", _bodyFont, 12, new Color(0.88f, 0.8f, 0.65f, 0.56f), TextAnchor.UpperLeft);
            Place(_endChapter.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(494f, -172f), new Vector2(390f, 24f));
            _endPoem = MakeText("End Poem", c, "", _displayFont, 16, new Color(0.9f, 0.85f, 0.74f, 0.56f), TextAnchor.UpperLeft);
            _endPoem.lineSpacing = 1.6f;
            Place(_endPoem.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -226f), new Vector2(840f, 104f));

            var stats = new GameObject("Stats", typeof(RectTransform));
            var s = stats.GetComponent<RectTransform>();
            s.SetParent(c, false);
            s.anchorMin = new Vector2(0f, 1f);
            s.anchorMax = new Vector2(1f, 1f);
            s.pivot = new Vector2(0f, 1f);
            s.anchoredPosition = new Vector2(0f, -352f);
            s.sizeDelta = new Vector2(0f, 100f);
            var statsInk = MakeImage("Stats Ink", s, ProceduralArt.WhiteSprite, new Color(0.004f, 0.012f, 0.022f, 0.86f));
            Stretch(statsInk.rectTransform);
            statsInk.rectTransform.offsetMin = new Vector2(0f, -10f);
            statsInk.rectTransform.offsetMax = new Vector2(0f, 10f);
            statsInk.raycastTarget = false;
            var line1 = MakeImage("Stat Top", s, ProceduralArt.WhiteSprite, new Color(0.88f, 0.74f, 0.48f, 0.15f));
            Place(line1.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 1f));
            line1.raycastTarget = false;
            var line2 = MakeImage("Stat Bottom", s, ProceduralArt.WhiteSprite, new Color(0.88f, 0.74f, 0.48f, 0.15f));
            Place(line2.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 1f));
            line2.raycastTarget = false;

            string[] labels = { "航 时", "借 力", "最 深 航 迹", "剩 余 星 质" };
            var values = new Text[4];
            var labelTexts = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                var cell = new GameObject("Stat " + i, typeof(RectTransform));
                var cr = cell.GetComponent<RectTransform>();
                cr.SetParent(s, false);
                cr.anchorMin = new Vector2(i / 4f, 0f);
                cr.anchorMax = new Vector2((i + 1) / 4f, 1f);
                cr.offsetMin = new Vector2(0f, 0f);
                cr.offsetMax = new Vector2(-12f, 0f);
                var l = MakeText("Label", cr, labels[i], _bodyFont, 12, new Color(0.95f, 0.89f, 0.77f, 0.9f), TextAnchor.UpperLeft);
                Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -14f), new Vector2(180f, 20f));
                var v = MakeText("Value", cr, "00:00", _latinFont, 27, new Color(0.96f, 0.83f, 0.57f, 1f), TextAnchor.UpperLeft);
                Place(v.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 13f), new Vector2(200f, 38f));
                values[i] = v;
                labelTexts[i] = l;
            }
            _endTime = values[0];
            _endHits = values[1];
            _endDepth = values[2];
            _endMass = values[3];
            _endMassLabel = labelTexts[3];

            _againButton = MakePrimaryButton("Again", c, "再 入 星 渊", "B E G I N   A G A I N", new Vector2(0f, -476f), new Vector2(290f, 70f), _game.NextRoundFromEnd);
            _againLabel = _againButton.transform.Find("Label")?.GetComponent<Text>();
            _againSub = _againButton.transform.Find("Sub")?.GetComponent<Text>();
            var titleBtn = MakeTextButton("Back To Title", c, "回 到 星 图", new Vector2(320f, -462f), new Vector2(180f, 44f), _game.QuitToTitle);
            _autoNext = MakeText("Auto Next", c, "新的星夜将在 9 秒后醒来", _bodyFont, 12, new Color(0.9f, 0.83f, 0.68f, 0.4f), TextAnchor.UpperLeft);
            Place(_autoNext.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -572f), new Vector2(500f, 22f));
        }

        // ---------------------------------------------------------
        // Cinematic
        // ---------------------------------------------------------

        private void BuildCinema()
        {
            var root = MakeGroup("Cinema", transform, out _cinemaGroup);
            var scrim = MakeImage("Cinema Scrim", root, ProceduralArt.WhiteSprite, new Color(0.002f, 0.006f, 0.012f, 0.2f));
            Stretch(scrim.rectTransform);
            scrim.raycastTarget = false;
            var centerShade = MakeImage("Cinema Shade", root, ProceduralArt.BlackDisk(256), new Color(0.004f, 0.012f, 0.021f, 0.44f));
            centerShade.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            centerShade.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            centerShade.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            centerShade.rectTransform.anchoredPosition = new Vector2(0f, 26f);
            centerShade.rectTransform.sizeDelta = new Vector2(1480f, 680f);
            centerShade.raycastTarget = false;
            var textBloom = MakeImage("Cinema Bloom", root, ProceduralArt.Glow(256, 2.1f), new Color(0.003f, 0.009f, 0.017f, 0.58f));
            textBloom.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textBloom.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textBloom.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            textBloom.rectTransform.anchoredPosition = new Vector2(0f, -160f);
            textBloom.rectTransform.sizeDelta = new Vector2(1740f, 470f);
            textBloom.raycastTarget = false;

            var topBar = MakeImage("Letterbox Top", root, ProceduralArt.WhiteSprite, new Color(0.004f, 0.008f, 0.014f, 0.93f));
            topBar.rectTransform.anchorMin = new Vector2(0f, 1f);
            topBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            topBar.rectTransform.pivot = new Vector2(0.5f, 1f);
            topBar.rectTransform.anchoredPosition = Vector2.zero;
            topBar.rectTransform.sizeDelta = new Vector2(0f, 86f);
            topBar.raycastTarget = false;
            var bottomBar = MakeImage("Letterbox Bottom", root, ProceduralArt.WhiteSprite, new Color(0.004f, 0.008f, 0.014f, 0.93f));
            bottomBar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottomBar.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottomBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottomBar.rectTransform.anchoredPosition = Vector2.zero;
            bottomBar.rectTransform.sizeDelta = new Vector2(0f, 86f);
            bottomBar.raycastTarget = false;

            var center = new GameObject("Cinema Text", typeof(RectTransform));
            var c = center.GetComponent<RectTransform>();
            c.SetParent(root, false);
            c.anchorMin = new Vector2(0.5f, 0.5f);
            c.anchorMax = new Vector2(0.5f, 0.5f);
            c.pivot = new Vector2(0.5f, 0.5f);
            c.anchoredPosition = new Vector2(0f, -250f);
            c.sizeDelta = new Vector2(1100f, 440f);

            _cinemaKicker = MakeText("Cinema Kicker", c, "", _latinFont, 12, new Color(0.91f, 0.79f, 0.55f, 0.82f), TextAnchor.MiddleCenter);
            Place(_cinemaKicker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 112f), new Vector2(800f, 28f));
            _cinemaTitle = MakeText("Cinema Title", c, "", _displayFont, 82, new Color(0.99f, 0.95f, 0.86f, 1f), TextAnchor.MiddleCenter);
            Place(_cinemaTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(1100f, 120f));
            _cinemaSub = MakeText("Cinema Sub", c, "", _displayFont, 19, new Color(0.96f, 0.91f, 0.8f, 0.84f), TextAnchor.MiddleCenter);
            _cinemaSub.lineSpacing = 1.55f;
            Place(_cinemaSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -64f), new Vector2(1000f, 108f));
            AddShadow(_cinemaKicker, 0.7f, new Vector2(0f, -1f));
            AddShadow(_cinemaTitle, 0.95f, new Vector2(0f, -2f));
            AddShadow(_cinemaSub, 0.92f, new Vector2(0f, -2f));
            AddOutline(_cinemaKicker, 0.65f, new Vector2(0.9f, -0.9f));
            AddOutline(_cinemaTitle, 0.78f, new Vector2(1.4f, -1.4f));
            AddOutline(_cinemaSub, 0.95f, new Vector2(1.2f, -1.2f));

            var skip = MakeTextButton("Skip", transform, "跳 过 过 场   ·   S P A C E", new Vector2(0f, 0f), new Vector2(240f, 32f), _game.SkipCinematic);
            var sr = skip.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(1f, 0f);
            sr.anchorMax = new Vector2(1f, 0f);
            sr.pivot = new Vector2(1f, 0f);
            sr.anchoredPosition = new Vector2(-54f, 100f);
            var skipLabel = skip.GetComponentInChildren<Text>();
            if (skipLabel != null)
            {
                skipLabel.color = new Color(0.94f, 0.87f, 0.72f, 0.85f);
                skipLabel.fontSize = 12;
                AddShadow(skipLabel, 0.9f, new Vector2(0f, -1f));
            }
            _skipButton = skip.GetComponent<Button>();
            _skipGroup = skip.gameObject.AddComponent<CanvasGroup>();
            _skipGroup.alpha = 0f;
            _skipGroup.interactable = false;
            _skipGroup.blocksRaycasts = false;
        }

        private static void AddShadow(Text text, float alpha, Vector2 distance)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddOutline(Text text, float alpha, Vector2 distance)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, alpha);
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private void BuildTransient()
        {
            var chapter = new GameObject("Chapter", typeof(RectTransform));
            var root = chapter.GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.anchorMin = new Vector2(0f, 0.5f);
            root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0f, 0.5f);
            root.anchoredPosition = new Vector2(126f, 72f);
            root.sizeDelta = new Vector2(620f, 220f);
            _chapterGroup = chapter.AddComponent<CanvasGroup>();
            _chapterGroup.alpha = 0f;
            _chapterGroup.blocksRaycasts = false;
            _chapterGroup.interactable = false;
            _chapterKicker = MakeText("Chapter Kicker", root, "第 001 夜", _latinFont, 11, new Color(0.88f, 0.73f, 0.48f, 0.65f), TextAnchor.UpperLeft);
            Place(_chapterKicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(400f, 20f));
            _chapterTitle = MakeText("Chapter Title", root, "近 星 点 · 苏 醒", _displayFont, 32, new Color(0.96f, 0.9f, 0.78f, 0.9f), TextAnchor.UpperLeft);
            Place(_chapterTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -30f), new Vector2(600f, 56f));
            _chapterSub = MakeText("Chapter Sub", root, "A   N E W   O R B I T", _latinFont, 10, new Color(0.88f, 0.81f, 0.66f, 0.3f), TextAnchor.UpperLeft);
            Place(_chapterSub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -82f), new Vector2(500f, 20f));
            _chapterCardRect = root;

            var toastGo = new GameObject("Toast", typeof(RectTransform));
            var tr = toastGo.GetComponent<RectTransform>();
            tr.SetParent(transform, false);
            tr.anchorMin = new Vector2(0.5f, 0f);
            tr.anchorMax = new Vector2(0.5f, 0f);
            tr.pivot = new Vector2(0.5f, 0f);
            tr.anchoredPosition = new Vector2(0f, 106f);
            tr.sizeDelta = new Vector2(620f, 44f);
            var tbg = MakeImage("Toast Background", tr, ProceduralArt.Frame(), new Color(0.78f, 0.68f, 0.48f, 0.25f));
            Stretch(tbg.rectTransform);
            tbg.type = Image.Type.Sliced;
            tbg.raycastTarget = false;
            var tdim = MakeImage("Toast Ink", tr, ProceduralArt.WhiteSprite, new Color(0.01f, 0.033f, 0.06f, 0.7f));
            Stretch(tdim.rectTransform);
            tdim.raycastTarget = false;
            var dot = MakeImage("Toast Dot", tr, ProceduralArt.Glow(64), new Color(0.9f, 0.77f, 0.53f, 0.9f));
            dot.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            dot.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            dot.rectTransform.pivot = new Vector2(0f, 0.5f);
            dot.rectTransform.anchoredPosition = new Vector2(22f, 0f);
            dot.rectTransform.sizeDelta = new Vector2(8f, 8f);
            dot.raycastTarget = false;
            _toastText = MakeText("Toast Text", tr, "", _bodyFont, 13, new Color(0.94f, 0.88f, 0.76f, 0.82f), TextAnchor.MiddleCenter);
            StretchWithMargins(_toastText.rectTransform, 54f, 16f, 4f, 4f);
            _toastRect = tr;
            _toastCanvasGroup = toastGo.AddComponent<CanvasGroup>();
            _toastCanvasGroup.alpha = 0f;
            _toastCanvasGroup.blocksRaycasts = false;
        }

        private void BuildReticle()
        {
            var go = new GameObject("Tutorial Reticle", typeof(RectTransform));
            _reticleRect = go.GetComponent<RectTransform>();
            _reticleRect.SetParent(transform, false);
            _reticleRect.anchorMin = new Vector2(0.5f, 0.5f);
            _reticleRect.anchorMax = new Vector2(0.5f, 0.5f);
            _reticleRect.pivot = new Vector2(0.5f, 0.5f);
            _reticleRect.sizeDelta = new Vector2(116f, 116f);
            _reticleGroup = go.AddComponent<CanvasGroup>();
            _reticleGroup.alpha = 0f;
            _reticleGroup.blocksRaycasts = false;
            _reticleGroup.interactable = false;

            _reticle = MakeImage("Ring", _reticleRect, ProceduralArt.CircleOutline(128, 0.47f, 0.9f), new Color(0.94f, 0.82f, 0.56f, 0.5f));
            Stretch(_reticle.rectTransform);
            _reticle.raycastTarget = false;
            _reticleLabel = MakeText("Reticle Label", _reticleRect, "借 力\n撞 击 此 处", _bodyFont, 12, new Color(0.94f, 0.85f, 0.69f, 0.7f), TextAnchor.MiddleCenter);
            _reticleLabel.lineSpacing = 1.2f;
            Place(_reticleLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, -56f), new Vector2(160f, 60f));
        }

        private void BuildImpacts()
        {
            var root = new GameObject("Impact Texts", typeof(RectTransform));
            _impactRoot = root.GetComponent<RectTransform>();
            _impactRoot.SetParent(transform, false);
            Stretch(_impactRoot);
            for (int i = 0; i < 10; i++)
            {
                var t = MakeText("Impact " + i, _impactRoot, "", _displayFont, 20, new Color(0.95f, 0.87f, 0.72f, 0f), TextAnchor.MiddleCenter);
                t.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                t.rectTransform.sizeDelta = new Vector2(440f, 76f);
                AddShadow(t, 0.7f, new Vector2(0f, -1f));
                AddOutline(t, 0.7f, new Vector2(1f, -1f));
                t.gameObject.SetActive(false);
                _impacts.Add(new Impact { Text = t, Rect = t.rectTransform, Life = 0f });
            }
        }

        private void BuildBoons()
        {
            var root = MakeGroup("Boon Draft", transform, out _boonGroup);
            var shade = MakeImage("Boon Shade", root, ProceduralArt.WhiteSprite, new Color(0.004f, 0.013f, 0.025f, 0.95f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = true;
            var vignette = MakeImage("Boon Vignette", root, ProceduralArt.Vignette(), new Color(0.002f, 0.008f, 0.016f, 0.5f));
            Stretch(vignette.rectTransform);
            vignette.raycastTarget = false;

            var panel = new GameObject("Boon Panel", typeof(RectTransform));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(root, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 12f);
            panelRect.sizeDelta = new Vector2(1280f, 680f);
            var panelInk = MakeImage("Boon Panel Ink", panelRect, ProceduralArt.WhiteSprite, new Color(0.005f, 0.018f, 0.032f, 0.45f));
            Stretch(panelInk.rectTransform);
            panelInk.raycastTarget = false;

            var topLine = MakeImage("Boon Top Line", panelRect, ProceduralArt.WhiteSprite, new Color(0.9f, 0.77f, 0.52f, 0.3f));
            Place(topLine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(880f, 1f));
            topLine.raycastTarget = false;

            var kicker = MakeText("Boon Kicker", panelRect, "T H E   C O N S T E L L A T I O N   P A C T", _latinFont, 11,
                new Color(0.9f, 0.77f, 0.52f, 0.7f), TextAnchor.MiddleCenter);
            Place(kicker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 20f));
            var title = MakeText("Boon Title", panelRect, "星 象 契 约", _displayFont, 46,
                new Color(0.98f, 0.92f, 0.81f, 1f), TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1000f, 64f));
            AddOutline(title, 0.65f, new Vector2(1.2f, -1.2f));
            var sub = MakeText("Boon Sub", panelRect, "第 7 次有效碰撞 · 本次航行唯一一次星象契约", _displayFont, 14,
                new Color(0.9f, 0.84f, 0.73f, 0.6f), TextAnchor.MiddleCenter);
            Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(1000f, 24f));
            _boonSubtitle = sub;

            for (int i = 0; i < 3; i++)
            {
                int choice = i;
                var cardGo = new GameObject("Boon Card " + i, typeof(RectTransform));
                var cr = cardGo.GetComponent<RectTransform>();
                cr.SetParent(panelRect, false);
                cr.anchorMin = new Vector2(0.5f, 0.5f);
                cr.anchorMax = new Vector2(0.5f, 0.5f);
                cr.pivot = new Vector2(0.5f, 0.5f);
                cr.anchoredPosition = new Vector2((i - 1) * 370f, -78f);
                cr.sizeDelta = new Vector2(330f, 380f);

                var ink = MakeImage("Card Ink", cr, ProceduralArt.WhiteSprite, new Color(0.007f, 0.024f, 0.043f, 1f));
                Stretch(ink.rectTransform);
                ink.raycastTarget = true;
                var frame = MakeImage("Card Frame", cr, ProceduralArt.Frame(), new Color(0.86f, 0.73f, 0.48f, 0.22f));
                Stretch(frame.rectTransform);
                frame.type = Image.Type.Sliced;
                frame.raycastTarget = false;
                var glyph = MakeImage("Card Glyph", cr, ProceduralArt.StarBody(), new Color(0.94f, 0.82f, 0.58f, 0.42f));
                glyph.rectTransform.anchorMin = new Vector2(1f, 1f);
                glyph.rectTransform.anchorMax = new Vector2(1f, 1f);
                glyph.rectTransform.pivot = new Vector2(1f, 1f);
                glyph.rectTransform.anchoredPosition = new Vector2(-18f, -14f);
                glyph.rectTransform.sizeDelta = new Vector2(34f, 34f);
                glyph.raycastTarget = false;
                var ordinal = MakeText("Card Ordinal", cr, (i + 1).ToString("00"), _latinFont, 10,
                    new Color(0.92f, 0.78f, 0.52f, 0.55f), TextAnchor.UpperLeft);
                Place(ordinal.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(60f, 18f));
                var cardTitle = MakeText("Card Title", cr, "", _displayFont, 26,
                    new Color(0.98f, 0.9f, 0.75f, 0.95f), TextAnchor.UpperLeft);
                Place(cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -44f), new Vector2(285f, 44f));
                var rule = MakeImage("Card Rule", cr, ProceduralArt.WhiteSprite, new Color(0.86f, 0.73f, 0.48f, 0.18f));
                Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -94f), new Vector2(294f, 1f));
                rule.raycastTarget = false;
                var effect = MakeText("Card Effect", cr, "", _bodyFont, 15,
                    new Color(0.94f, 0.88f, 0.76f, 0.7f), TextAnchor.UpperLeft);
                effect.lineSpacing = 1.55f;
                effect.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(effect.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -112f), new Vector2(292f, 126f));
                var flavor = MakeText("Card Flavor", cr, "", _displayFont, 12,
                    new Color(0.86f, 0.78f, 0.64f, 0.38f), TextAnchor.UpperLeft);
                flavor.lineSpacing = 1.35f;
                flavor.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(flavor.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -263f), new Vector2(292f, 58f));
                var pick = MakeText("Card Pick", cr, "◆   当 前 选 择", _bodyFont, 10,
                    new Color(0.98f, 0.84f, 0.55f, 0f), TextAnchor.UpperLeft);
                Place(pick.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -336f), new Vector2(292f, 22f));

                var button = cardGo.AddComponent<Button>();
                button.targetGraphic = ink;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => _game.ChooseBoon(choice));

                _boonCards[i] = new BoonCard
                {
                    Root = cardGo,
                    Ink = ink,
                    Frame = frame,
                    Glyph = glyph,
                    Ordinal = ordinal,
                    Title = cardTitle,
                    Effect = effect,
                    Flavor = flavor,
                    Pick = pick
                };
            }

            var hint = MakeText("Boon Hint", panelRect, "A / D 选择      ·      SPACE 确认      ·      也可直接点击卡牌", _bodyFont, 12,
                new Color(0.88f, 0.81f, 0.68f, 0.4f), TextAnchor.MiddleCenter);
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(900f, 24f));
            var bottomLine = MakeImage("Boon Bottom Line", panelRect, ProceduralArt.WhiteSprite, new Color(0.9f, 0.77f, 0.52f, 0.2f));
            Place(bottomLine.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(880f, 1f));
            bottomLine.raycastTarget = false;

            _boonTarget = 0f;
            _boonGroup.alpha = 0f;
            _boonGroup.blocksRaycasts = false;
            _boonGroup.interactable = false;
        }

        public void ShowBoons(IList<BoonOption> options, int selected)
        {
            if (_boonGroup == null) return;
            for (int i = 0; i < _boonCards.Length; i++)
            {
                var card = _boonCards[i];
                if (card == null) continue;
                bool has = options != null && i < options.Count;
                card.Root.SetActive(has);
                if (!has) continue;
                card.Title.text = options[i].Title;
                card.Effect.text = options[i].Effect;
                card.Flavor.text = options[i].Flavor;
            }
            _boonTarget = 1f;
            _boonGroup.alpha = Mathf.Max(_boonGroup.alpha, 0.01f);
            _boonGroup.blocksRaycasts = true;
            _boonGroup.interactable = true;
            SetBoonSelection(selected);
        }

        public void SetBoonSelection(int selected)
        {
            for (int i = 0; i < _boonCards.Length; i++)
            {
                var card = _boonCards[i];
                if (card == null || !card.Root.activeSelf) continue;
                bool on = i == selected;
                card.Ink.color = on ? new Color(0.028f, 0.075f, 0.11f, 1f) : new Color(0.004f, 0.014f, 0.026f, 1f);
                card.Frame.color = on ? new Color(0.96f, 0.81f, 0.51f, 0.95f) : new Color(0.86f, 0.73f, 0.48f, 0.12f);
                card.Glyph.color = on ? new Color(1f, 0.9f, 0.66f, 1f) : new Color(0.94f, 0.82f, 0.58f, 0.22f);
                card.Ordinal.color = on ? new Color(1f, 0.88f, 0.61f, 0.9f) : new Color(0.92f, 0.78f, 0.52f, 0.32f);
                card.Title.color = on ? new Color(1f, 0.95f, 0.83f, 1f) : new Color(0.98f, 0.9f, 0.75f, 0.55f);
                card.Effect.color = on ? new Color(0.97f, 0.92f, 0.82f, 0.98f) : new Color(0.94f, 0.88f, 0.76f, 0.38f);
                card.Flavor.color = on ? new Color(0.92f, 0.82f, 0.66f, 0.68f) : new Color(0.86f, 0.78f, 0.64f, 0.2f);
                if (card.Pick != null) card.Pick.color = new Color(0.98f, 0.84f, 0.55f, on ? 0.92f : 0f);
            }
        }

        public void HideBoons(bool instant = false)
        {
            _boonTarget = 0f;
            if (_boonGroup == null) return;
            if (instant) _boonGroup.alpha = 0f;
            _boonGroup.blocksRaycasts = false;
            _boonGroup.interactable = false;
        }

        private void BuildFlashAndVeil()
        {
            _flashImage = MakeImage("Flash", transform, ProceduralArt.WhiteSprite, new Color(1f, 0.95f, 0.83f, 0f));
            Stretch(_flashImage.rectTransform);
            _flashImage.raycastTarget = false;
            _veilGroup = new GameObject("Veil Group", typeof(RectTransform)).AddComponent<CanvasGroup>();
            var veilRoot = _veilGroup.GetComponent<RectTransform>();
            veilRoot.SetParent(transform, false);
            Stretch(veilRoot);
            _veilGroup.alpha = 0f;
            _veilGroup.blocksRaycasts = false;
            var veil = MakeImage("Veil", veilRoot, ProceduralArt.WhiteSprite, new Color(0.004f, 0.012f, 0.021f, 0.98f));
            Stretch(veil.rectTransform);
            veil.raycastTarget = true;
            _veilTarget = 0f;
        }

        // ---------------------------------------------------------
        // Public state API
        // ---------------------------------------------------------

        public System.Action ClickSound;

        public void PlayClick()
        {
            ClickSound?.Invoke();
        }

        public void SetDanger(float danger)
        {
            _dangerTarget = Mathf.Clamp01(danger);
        }

        public void ApplyTheme(StarTheme theme)
        {
            _theme = theme;
        }

        public void ShowTitle(bool instant = false)
        {
            _titleTarget = 1f;
            _hudTarget = 0f;
            _pauseTarget = 0f;
            _endTarget = 0f;
            _cinemaTarget = 0f;
            if (instant)
            {
                _titleGroup.alpha = 1f;
                _hudGroup.alpha = 0f;
                _pauseGroup.alpha = 0f;
                _endGroup.alpha = 0f;
                _cinemaGroup.alpha = 0f;
            }
            UpdateInteractivity();
        }

        public void ShowPlaying(bool instant = false)
        {
            _titleTarget = 0f;
            _hudTarget = 1f;
            _pauseTarget = 0f;
            _endTarget = 0f;
            _cinemaTarget = 0f;
            if (instant) SetGroupAlpha(_hudGroup, 1f);
            UpdateInteractivity();
        }

        public void SetHudVisible(bool visible, bool instant = false)
        {
            _hudTarget = visible ? 1f : 0f;
            if (instant) SetGroupAlpha(_hudGroup, _hudTarget);
            UpdateInteractivity();
        }

        public void ShowPaused()
        {
            _pauseTarget = 1f;
            _hudTarget = 0f;
            _titleTarget = 0f;
            _endTarget = 0f;
            _cinemaTarget = 0f;
            UpdateInteractivity();
        }

        public void HidePaused()
        {
            _pauseTarget = 0f;
            _hudTarget = 1f;
            UpdateInteractivity();
        }

        public void SetPauseVisible(bool visible, bool instant = false)
        {
            _pauseTarget = visible ? 1f : 0f;
            if (instant) SetGroupAlpha(_pauseGroup, _pauseTarget);
            UpdateInteractivity();
        }

        public void SetCinemaVisible(bool visible, bool instant = false)
        {
            _cinemaTarget = visible ? 1f : 0f;
            if (instant) SetGroupAlpha(_cinemaGroup, _cinemaTarget);
            UpdateInteractivity();
        }

        public void SetSkipVisible(bool visible)
        {
            _skipGroup.alpha = visible ? 1f : 0f;
            _skipGroup.interactable = visible;
            _skipGroup.blocksRaycasts = visible;
        }

        public void UpdateCinema(string kicker, string title, string sub)
        {
            _cinemaKicker.text = kicker;
            _cinemaTitle.text = title;
            _cinemaSub.text = sub;
            _cinemaTitle.transform.localScale = Vector3.one;
        }

        public void SetCinemaMuted(bool muted)
        {
            var target = muted ? 0f : 1f;
            var c = _cinemaGroup.alpha;
            // The group itself is controlled by SetCinemaVisible; mute only
            // the text by toggling its child alpha through a color tween.
            var col = _cinemaTitle.color;
            col.a = target;
            _cinemaTitle.color = col;
            var subCol = _cinemaSub.color;
            subCol.a = target * 0.8f;
            _cinemaSub.color = subCol;
            var kickCol = _cinemaKicker.color;
            kickCol.a = target * 0.75f;
            _cinemaKicker.color = kickCol;
        }

        public void ShowEnd(GameResult result, int nextSeconds)
        {
            _titleTarget = 0f;
            _hudTarget = 0f;
            _pauseTarget = 0f;
            _endTarget = 1f;
            _cinemaTarget = 0f;
            SetCinemaVisible(false);
            SetEndVisible(true);
            UpdateInteractivity();

            bool escaped = result.Escaped;
            _endKicker.text = escaped ? "T H E   W A Y F A R E R ' S   G A T E" : "T H E   L A S T   O R B I T";
            _endTitle.text = escaped ? "归 航" : (result.Reason == "horizon" ? "坠 入 视 界" : "归 于 一 点");
            _endChapter.text = "第 " + result.Round.ToString("000") + " 夜   ·   " + (escaped ? "抵达" : "终");
            _endPoem.text = escaped
                ? "万千星辰的碎片，终于把你推到门前。\n归途不是终点，只是另一片星夜的开始。"
                : (result.Reason == "horizon"
                    ? "你越过了最后一道光子环。\n时间在视界里折成一枚暗色的种子，随后，一切重新开始。"
                    : "你把自己交给了每一次相撞。\n最后一粒星尘熄灭时，黑洞收拢了所有轨道。\n现在，等待另一次睁眼。");
            _endTime.text = FormatTime(result.Time);
            _endHits.text = result.Hits.ToString();
            _endDepth.text = Mathf.RoundToInt(result.Depth * 100f) + "%";
            _endMassLabel.text = escaped ? "穿越星质" : "终局星质";
            _endMass.text = Mathf.RoundToInt(result.Mass) + "%";
            if (_againLabel != null) _againLabel.text = escaped ? "继 续 星 旅" : "再 入 星 渊";
            if (_againSub != null) _againSub.text = escaped ? "W A N D E R   O N" : "B E G I N   A G A I N";
            SetAutoNext(nextSeconds);
            _endGroup.alpha = 0f;
            _endGroup.blocksRaycasts = true;
            _endGroup.interactable = true;
        }

        public void SetEndVisible(bool visible, bool instant = false)
        {
            _endTarget = visible ? 1f : 0f;
            if (instant) SetGroupAlpha(_endGroup, _endTarget);
            UpdateInteractivity();
        }

        public void SetAutoNext(int seconds)
        {
            if (seconds < 0)
            {
                _autoNext.text = "";
                return;
            }
            _autoNext.text = "新的星夜将在 " + seconds + " 秒后醒来";
        }

        public void SetHud(StarryGame.HudSnapshot snapshot)
        {
            _massValue.text = snapshot.Mass >= 99.5f ? "100" : (snapshot.Mass >= 10f ? Mathf.CeilToInt(snapshot.Mass).ToString() : snapshot.Mass.ToString("0.0"));
            _massFill.anchorMax = new Vector2(Mathf.Clamp01(snapshot.Mass / 100f), 1f);
            _massFillImage.color = snapshot.Mass < 32f
                ? new Color(0.82f, 0.43f, 0.32f, 0.95f)
                : new Color(0.92f, 0.74f, 0.43f, 0.95f);
            _massNote.text = snapshot.Mass < 12f ? "星核正在消散，抓住最后的光" : snapshot.Mass < 32f ? "星质所剩无几" : "每一次相撞，都会失去一部分自己";
            _massNote.color = snapshot.Mass < 32f ? new Color(0.84f, 0.54f, 0.4f, 0.55f) : new Color(0.86f, 0.79f, 0.65f, 0.28f);

            if (snapshot.PortalActive)
            {
                _portalDistance.fontSize = 33;
                _portalDistance.text = snapshot.PortalDistance >= 1000f
                    ? (snapshot.PortalDistance / 1000f).ToString("0.0") + "k 光里"
                    : Mathf.RoundToInt(snapshot.PortalDistance) + " 光里";
                if (_portalSub != null) _portalSub.text = "它正在移动 · 抓住靠近的窗口";
                _compassArrow.gameObject.SetActive(true);
                _compassArrow.localRotation = Quaternion.Euler(0f, 0f, snapshot.CompassAngle);
            }
            else
            {
                _portalDistance.fontSize = 27;
                _portalDistance.text = Mathf.CeilToInt(snapshot.PortalRemain).ToString("00") + " 秒后显现";
                if (_portalSub != null) _portalSub.text = "航行一分钟后，归航阵才会出现";
                _compassArrow.gameObject.SetActive(false);
            }
            _runStats.text = "第 " + snapshot.Round.ToString("000") + " 夜    ·    借力 " + snapshot.Hits + "    ·    " + FormatTime(snapshot.Time);
            if (_chapterLabel != null) _chapterLabel.text = "";
            _tideText.text = _blackHoleState == 2
                ? "黑洞吸积 · 引力增强"
                : _blackHoleState == 1
                    ? "黑洞苏醒 · 五秒后吸积"
                    : "引力潮汐 · " + snapshot.TideName;
            _tideDot.color = snapshot.TideLevel switch
            {
                0 => new Color(0.55f, 0.82f, 0.78f, 0.85f),
                1 => new Color(0.93f, 0.79f, 0.48f, 0.9f),
                _ => new Color(0.84f, 0.44f, 0.31f, 0.95f)
            };
            _dangerTarget = snapshot.Danger;
        }

        public void SetAssist(bool on)
        {
            if (_assistLabel == null) return;
            _assistLabel.text = "辅 助 模 式   ·   " + (on ? "开" : "关");
            _assistLabel.color = on
                ? new Color(0.94f, 0.8f, 0.52f, 0.82f)
                : new Color(0.9f, 0.84f, 0.7f, 0.5f);
        }

        public void SetStardust(int stardust, int combo, int nextReward, bool shield, bool maxed, int trailScore)
        {
            if (_stardustHud == null) return;
            string status;
            if (maxed)
                status = "航迹 " + trailScore + (shield ? "  ·  护盾" : "");
            else if (shield)
                status = "星辉护盾就绪";
            else
                status = "还差 " + Mathf.Max(0, nextReward - stardust) + " 星屑";
            _stardustHud.text = "星屑 " + stardust + "   ·   连携 ×" + combo + "   ·   " + status;
            _stardustHud.color = shield || maxed
                ? new Color(0.62f, 0.92f, 0.86f, 0.72f)
                : new Color(0.94f, 0.82f, 0.58f, 0.58f);
        }

        public void SetStarAspect(string name, Color color)
        {
            _starAspectName = name;
            _starAspectColor = color;
            RefreshStarAspect();
        }

        public void SetZoneName(string zone)
        {
            _zoneName = zone;
            RefreshStarAspect();
        }

        public void SetZoneGoal(int current, int target, bool done)
        {
            _zoneGoalCurrent = current;
            _zoneGoalTarget = target;
            _zoneGoalDone = done;
            RefreshStarAspect();
        }

        private void RefreshStarAspect()
        {
            if (_starAspect == null) return;
            var color = _starAspectColor;
            color.a = 0.52f;
            _starAspect.color = color;
            string goal = _zoneGoalTarget <= 0
                ? ""
                : _zoneGoalDone
                    ? "  ·  星屑目标达成"
                    : "  ·  星屑 " + Mathf.Clamp(_zoneGoalCurrent, 0, _zoneGoalTarget) + " / " + _zoneGoalTarget;
            _starAspect.text = (string.IsNullOrEmpty(_zoneName) ? "" : _zoneName + "  ·  ") + "星相  " + _starAspectName + goal;
        }

        public void SetBoonSubtitle(string text, Color color)
        {
            if (_boonSubtitle == null) return;
            _boonSubtitle.text = text;
            _boonSubtitle.color = color;
        }

        public void SetSlingPrompt(bool visible, int charges, bool perfect = false)
        {
            if (_slingPrompt == null) return;
            _slingPromptTarget = visible && charges > 0 ? 1f : 0f;
            if (_slingPromptTarget > 0f)
            {
                _slingPrompt.text = perfect
                    ? "SPACE  ·  完美借力  ×" + charges
                    : "SPACE  ·  擦身借力  ×" + charges;
                _slingPrompt.color = perfect
                    ? new Color(0.98f, 0.84f, 0.55f, 0.95f)
                    : new Color(0.72f, 0.96f, 0.9f, 0.9f);
            }
        }

        public void SetBoonProgress(int effectiveHits, int required, string chosenTitle)
        {
            if (_boonProgress == null) return;
            if (!string.IsNullOrEmpty(chosenTitle))
            {
                _boonProgress.text = "星象契约 · 已缔结 · " + chosenTitle;
                _boonProgress.color = new Color(0.96f, 0.82f, 0.56f, 0.52f);
            }
            else
            {
                _boonProgress.text = "星象契约 · 有效碰撞 " + Mathf.Clamp(effectiveHits, 0, required) + " / " + required;
                _boonProgress.color = new Color(0.88f, 0.8f, 0.64f, 0.34f);
            }
        }

        public void SetHintVisible(bool visible)
        {
            _hintTarget = visible ? 1f : 0f;
        }

        public void SetBlackHoleState(int state)
        {
            _blackHoleState = state;
        }

        public void SetSoundMuted(bool muted)
        {
            if (_soundButtonLabel == null) return;
            _soundButtonLabel.text = muted ? "♪   静 音" : "♪   乐 音";
            _soundButtonLabel.color = muted ? new Color(0.88f, 0.81f, 0.67f, 0.26f) : new Color(0.88f, 0.81f, 0.67f, 0.55f);
        }

        public void SetRecord(int bestDepthPercent, float bestTime, int escapes, int codexFound)
        {
            if (_titleRecord == null) return;
            string record;
            if (bestDepthPercent <= 0 && bestTime <= 0f)
                record = "最深航迹 —   ·   最长航时 —";
            else
                record = "最深航迹 " + Mathf.Clamp(bestDepthPercent, 0, 100) + "%   ·   最长航时 " + FormatTime(bestTime) + (escapes > 0 ? "   ·   已穿越 " + escapes + " 次" : "");
            _titleRecord.text = record + "   ·   星相图鉴 " + Mathf.Clamp(codexFound, 0, StarAspect.Count) + " / " + StarAspect.Count;
        }

        public void SetEndTrail(int trailScore, int bestScore)
        {
            if (_endChapter == null) return;
            _endChapter.text = _endChapter.text + "   ·   航迹 " + trailScore + (bestScore > 0 ? " / 最佳 " + bestScore : "");
        }

        public void ShowChapter(string kicker, string title, string sub, float duration)
        {
            _chapterKicker.text = kicker;
            _chapterTitle.text = title;
            _chapterSub.text = sub;
            _chapterHideAt = Time.unscaledTime + duration;
            _chapterTarget = 1f;
        }

        public void HideChapter()
        {
            _chapterTarget = 0f;
        }

        public void ShowToast(string message, float duration = 4f)
        {
            _toastText.text = message;
            _toastHideAt = Time.unscaledTime + duration;
            _toastTarget = 1f;
        }

        public void HideToast(bool instant = false)
        {
            _toastTarget = 0f;
            if (instant) _toastCanvasGroup.alpha = 0f;
        }

        private bool _reticleVisible;

        public void HideReticleInstant()
        {
            _reticleVisible = false;
            if (_reticleGroup != null) _reticleGroup.alpha = 0f;
        }

        public void SetReticle(bool visible, Vector3 worldPosition, float radiusFactor = 1f)
        {
            if (_reticleGroup == null) return;
            _reticleVisible = visible;
            if (!visible) return;
            var screen = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPosition) : Vector3.zero;
            if (screen.z < 0f)
            {
                _reticleGroup.alpha = 0f;
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
            _reticleRect.anchoredPosition = local;
            float size = Mathf.Clamp(116f * radiusFactor, 80f, 240f);
            _reticleRect.sizeDelta = new Vector2(size, size);
        }

        public void SetPortalMarker(Vector3 worldPos, bool visible)
        {
            _portalMarkerVisible = visible;
            if (!visible) return;
            var screen = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : Vector3.zero;
            if (screen.z < 0f || screen.x < -40f || screen.x > Screen.width + 40f ||
                screen.y < -40f || screen.y > Screen.height + 40f)
            {
                _portalMarkerVisible = false;
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
            var cr = _canvasRect.rect;
            local.x = Mathf.Clamp(local.x, cr.xMin + 140f, cr.xMax - 140f);
            local.y = Mathf.Clamp(local.y, cr.yMin + 130f, cr.yMax - 100f);
            _portalMarkerRect.anchoredPosition = local + new Vector2(0f, -72f);
        }

        public void SetPlayerMarker(Vector3 worldPos, bool visible)
        {
            _playerMarkerVisible = visible;
            if (!visible) return;
            var screen = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : Vector3.zero;
            if (screen.z < 0f)
            {
                _playerMarkerVisible = false;
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
            _playerMarkerRect.anchoredPosition = local + new Vector2(0f, 34f);
        }

        public void ShowImpact(Vector3 worldPos, string line, Color color)
        {
            Impact target = null;
            float oldest = float.MaxValue;
            for (int i = 0; i < _impacts.Count; i++)
            {
                if (_impacts[i].Life <= 0f)
                {
                    target = _impacts[i];
                    break;
                }
                if (_impacts[i].Life < oldest)
                {
                    oldest = _impacts[i].Life;
                    target = _impacts[i];
                }
            }
            if (target == null) return;
            int active = 0;
            for (int i = 0; i < _impacts.Count; i++)
                if (_impacts[i].Life > 0f) active++;
            target.World = worldPos + new Vector3(0f, Mathf.Min(active, 4) * 0.85f, 0f);
            target.Life = 1.35f;
            target.Duration = 1.35f;
            target.Drift = new Vector2((UnityEngine.Random.value - 0.5f) * 16f, 12f);
            target.Text.text = line;
            target.Text.color = new Color(color.r, color.g, color.b, 0f);
            target.Text.gameObject.SetActive(true);
            UpdateImpactPosition(target);
        }

        public void Flash(float strength, Color color)
        {
            _flashAlpha = Mathf.Max(_flashAlpha, Mathf.Clamp01(strength));
            _flashColor = color;
            _flashImage.color = new Color(color.r, color.g, color.b, _flashAlpha);
        }

        public void ClearFlash()
        {
            _flashAlpha = 0f;
            if (_flashImage != null) _flashImage.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
        }

        public float FlashAlpha => _flashAlpha;
        public float DangerAlpha => _dangerGroup != null ? _dangerGroup.alpha : 0f;

        public void SetVeil(float target)
        {
            _veilTarget = Mathf.Clamp01(target);
            _veilGroup.blocksRaycasts = _veilTarget > 0f;
        }

        public void SnapVeil(float value)
        {
            _veilTarget = Mathf.Clamp01(value);
            _veilGroup.alpha = _veilTarget;
            _veilAlpha = _veilGroup.alpha;
            _veilGroup.blocksRaycasts = _veilTarget > 0f;
        }

        public float VeilAlpha => _veilAlpha;

        // ---------------------------------------------------------
        // Frame update
        // ---------------------------------------------------------

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float ease = 1f - Mathf.Exp(-7f * dt);
            _titleGroup.alpha = Mathf.Lerp(_titleGroup.alpha, _titleTarget, ease);
            _hudGroup.alpha = Mathf.Lerp(_hudGroup.alpha, _hudTarget, ease);
            _pauseGroup.alpha = Mathf.Lerp(_pauseGroup.alpha, _pauseTarget, ease);
            _endGroup.alpha = Mathf.Lerp(_endGroup.alpha, _endTarget, ease);
            _cinemaGroup.alpha = Mathf.Lerp(_cinemaGroup.alpha, _cinemaTarget, ease);
            _boonGroup.alpha = Mathf.Lerp(_boonGroup.alpha, _boonTarget, ease);
            if (_slingPrompt != null)
            {
                var sp = _slingPrompt.color;
                sp.a = Mathf.Lerp(sp.a, _slingPromptTarget * 0.9f, ease);
                _slingPrompt.color = sp;
            }
            _chapterGroup.alpha = Mathf.Lerp(_chapterGroup.alpha, _chapterTarget, ease);
            _toastCanvasGroup.alpha = Mathf.Lerp(_toastCanvasGroup.alpha, _toastTarget, ease);
            _veilGroup.alpha = Mathf.Lerp(_veilGroup.alpha, _veilTarget, 1f - Mathf.Exp(-5f * dt));
            _veilAlpha = _veilGroup.alpha;
            _veilGroup.blocksRaycasts = _veilTarget > 0f;
            _dangerGroup.alpha = Mathf.Lerp(_dangerGroup.alpha, _dangerTarget, 1f - Mathf.Exp(-3f * dt));
            if (_hintGroup != null)
                _hintGroup.alpha = Mathf.Lerp(_hintGroup.alpha, _hintTarget, 1f - Mathf.Exp(-2.5f * dt));

            if (_chapterTarget > 1f) _chapterTarget = 1f;
            if (_chapterTarget > 0.99f && Time.unscaledTime > _chapterHideAt) _chapterTarget = 0f;
            if (_toastTarget > 0.99f && Time.unscaledTime > _toastHideAt) _toastTarget = 0f;

            _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, dt * 1.3f);
            _flashImage.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, _flashAlpha);

            if (_reticleGroup != null)
                _reticleGroup.alpha = Mathf.MoveTowards(_reticleGroup.alpha, _reticleVisible ? 1f : 0f, dt * 4f);
            if (_portalMarkerGroup != null)
                _portalMarkerGroup.alpha = Mathf.MoveTowards(_portalMarkerGroup.alpha, _portalMarkerVisible ? 1f : 0f, dt * 3f);
            if (_playerMarkerGroup != null)
                _playerMarkerGroup.alpha = Mathf.MoveTowards(_playerMarkerGroup.alpha, _playerMarkerVisible ? 1f : 0f, dt * 3f);

            UpdateInteractivity();
            UpdateImpacts(dt);
        }

        private void UpdateInteractivity()
        {
            SetRaycast(_titleGroup, _titleTarget > 0.5f);
            SetRaycast(_hudGroup, _hudTarget > 0.5f);
            SetRaycast(_pauseGroup, _pauseTarget > 0.5f);
            SetRaycast(_endGroup, _endTarget > 0.5f);
            SetRaycast(_cinemaGroup, false);
            SetRaycast(_boonGroup, _boonTarget > 0.5f);
            if (_skipGroup != null)
            {
                bool skip = _cinemaTarget > 0.5f && _skipGroup.alpha > 0.5f;
                _skipGroup.blocksRaycasts = skip;
                _skipGroup.interactable = skip;
            }
        }

        private static void SetRaycast(CanvasGroup group, bool on)
        {
            if (group == null) return;
            group.blocksRaycasts = on;
            group.interactable = on;
        }

        private void UpdateImpacts(float dt)
        {
            if (_impacts == null) return;
            for (int i = 0; i < _impacts.Count; i++)
            {
                var im = _impacts[i];
                if (im.Life <= 0f) continue;
                im.Life -= dt;
                if (im.Life <= 0f)
                {
                    im.Text.gameObject.SetActive(false);
                    continue;
                }
                float p = 1f - im.Life / im.Duration;
                im.World += new Vector3(im.Drift.x * dt * 0.02f, im.Drift.y * dt * 0.02f, 0f);
                UpdateImpactPosition(im);
                var c = im.Text.color;
                c.a = Mathf.Clamp01(1f - Mathf.Pow(p, 2.2f)) * 0.9f;
                im.Text.color = c;
                im.Rect.localScale = Vector3.one * (1f + Mathf.Sin(p * 13f) * 0.015f);
            }

            if (_reticleGroup != null && _reticle != null)
            {
                var c = _reticle.color;
                c.a = 0.35f + 0.12f * Mathf.Sin(Time.unscaledTime * 2.2f);
                _reticle.color = c;
            }
        }

        private void UpdateImpactPosition(Impact im)
        {
            if (Camera.main == null) return;
            var screen = Camera.main.WorldToScreenPoint(im.World);
            if (screen.z < 0f)
            {
                im.Rect.anchoredPosition = new Vector2(10000f, 10000f);
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
            float rise = (1f - im.Life / im.Duration) * 34f;
            im.Rect.anchoredPosition = local + new Vector2(0f, rise);
        }

        // ---------------------------------------------------------
        // Factory helpers
        // ---------------------------------------------------------

        private Transform MakeGroup(string name, Transform parent, out CanvasGroup group)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return go.transform;
        }

        private void SetGroupAlpha(CanvasGroup g, float a)
        {
            if (g != null) g.alpha = a;
        }

        private Text MakeText(string name, Transform parent, string content, Font font, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = anchor;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Button MakeGhostButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, Action action)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.13f, 0.2f, 0.25f);
            bg.raycastTarget = true;
            var frame = MakeImage("Frame", rect, ProceduralArt.Frame(), new Color(0.88f, 0.79f, 0.6f, 0.36f));
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            var text = MakeText("Label", rect, label, _bodyFont, 13, new Color(0.91f, 0.84f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            var button = go.AddComponent<Button>();
            button.targetGraphic = frame;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.62f, 1.15f);
            colors.pressedColor = new Color(0.76f, 0.63f, 0.42f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.2f);
            colors.fadeDuration = 0.18f;
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }

        private Button MakePrimaryButton(string name, Transform parent, string label, string sub, Vector2 pos, Vector2 size, Action action)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.1f, 0.16f, 0.34f);
            bg.raycastTarget = true;
            var frame = MakeImage("Frame", rect, ProceduralArt.Frame(), new Color(0.9f, 0.78f, 0.55f, 0.44f));
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            var ink = MakeImage("Glow", rect, ProceduralArt.Glow(128), new Color(0.78f, 0.68f, 0.45f, 0.06f));
            ink.rectTransform.anchorMin = new Vector2(0f, 0f);
            ink.rectTransform.anchorMax = new Vector2(0f, 1f);
            ink.rectTransform.pivot = new Vector2(0f, 0.5f);
            ink.rectTransform.sizeDelta = new Vector2(110f, 0f);
            ink.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            ink.raycastTarget = false;
            var main = MakeText("Label", rect, label, _displayFont, 20, new Color(0.97f, 0.91f, 0.8f, 0.96f), TextAnchor.MiddleLeft);
            Place(main.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(24f, 5f), new Vector2(-70f, 26f));
            var em = MakeText("Sub", rect, sub, _latinFont, 9, new Color(0.88f, 0.78f, 0.58f, 0.38f), TextAnchor.MiddleRight);
            Place(em.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-22f, 6f), new Vector2(300f, 20f));
            var button = go.AddComponent<Button>();
            button.targetGraphic = frame;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.64f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.68f, 0.46f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.2f);
            colors.fadeDuration = 0.2f;
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }

        private Button MakeTextButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, Action action)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var hit = go.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0.01f);
            hit.raycastTarget = true;
            var text = MakeText("Label", rect, label, _bodyFont, 12, new Color(0.9f, 0.84f, 0.72f, 0.62f), TextAnchor.MiddleLeft);
            Stretch(text.rectTransform);
            var underline = MakeImage("Underline", rect, ProceduralArt.WhiteSprite, new Color(0.88f, 0.75f, 0.5f, 0.34f));
            underline.rectTransform.anchorMin = new Vector2(0f, 0f);
            underline.rectTransform.anchorMax = new Vector2(0.72f, 0f);
            underline.rectTransform.pivot = new Vector2(0f, 0f);
            underline.rectTransform.sizeDelta = new Vector2(0f, 1f);
            underline.raycastTarget = false;
            var button = go.AddComponent<Button>();
            button.targetGraphic = text;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.86f, 0.55f);
            colors.pressedColor = new Color(0.75f, 0.6f, 0.35f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.15f;
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWithMargins(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static void PinTopLeft(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static Text FindChildText(Transform parent, string name)
        {
            var t = parent.Find(name);
            return t != null ? t.GetComponent<Text>() : null;
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int s = Mathf.FloorToInt(seconds);
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }
    }
}
