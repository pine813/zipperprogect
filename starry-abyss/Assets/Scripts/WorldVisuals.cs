using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarryAbyss
{
    public static class ViewKit
    {
        public static SpriteRenderer CreateSprite(
            string name,
            Transform parent,
            Sprite sprite,
            bool additive,
            int sortingOrder,
            Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.shadowCastingMode = ShadowCastingMode.Off;
            sr.receiveShadows = false;
            if (additive)
            {
                sr.sharedMaterial = ProceduralArt.AdditiveMaterial;
            }
            return sr;
        }

        public static void SetDiameter(Transform t, Sprite sprite, float diameter)
        {
            if (sprite == null) return;
            float baseSize = Mathf.Max(0.0001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
            float s = diameter / baseSize;
            t.localScale = new Vector3(s, s, s);
        }

        public static LineRenderer CreateLine(
            string name,
            Transform parent,
            int sortingOrder,
            float width,
            Color startColor,
            Color endColor,
            bool loop = false,
            int pointCount = 32,
            bool taper = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = ProceduralArt.LineMaterial;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = Mathf.Max(2, pointCount);
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.widthMultiplier = width;
            line.widthCurve = taper
                ? AnimationCurve.EaseInOut(0f, 1f, 1f, 0.08f)
                : AnimationCurve.Linear(0f, 1f, 1f, 1f);
            line.startColor = startColor;
            line.endColor = endColor;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.generateLightingData = false;
            line.sortingOrder = sortingOrder;
            line.enabled = true;
            return line;
        }

        public static void SetLineColors(LineRenderer line, Color start, Color end)
        {
            if (line == null) return;
            line.startColor = start;
            line.endColor = end;
        }

        public static void SetCircle(LineRenderer line, float radius, int count)
        {
            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
        }

        public static Vector3[] Arc(Vector2 center, float radX, float radY, float start, float end, int count, float tilt = 0f)
        {
            var pts = new Vector3[count];
            float cosT = Mathf.Cos(tilt);
            float sinT = Mathf.Sin(tilt);
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(start, end, i / (float)(count - 1));
                float x = Mathf.Cos(a) * radX;
                float y = Mathf.Sin(a) * radY;
                pts[i] = new Vector3(center.x + x * cosT - y * sinT, center.y + x * sinT + y * cosT, 0f);
            }
            return pts;
        }

        public static void DestroyChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
        }

        public static Camera MainCamera
        {
            get
            {
                var c = Camera.main;
                if (c == null)
                {
                    var go = new GameObject("Starry Camera");
                    c = go.AddComponent<Camera>();
                    c.orthographic = true;
                    c.orthographicSize = 5.95f;
                    c.transform.position = new Vector3(0, 0, -10);
                    go.tag = "MainCamera";
                }
                return c;
            }
        }
    }

    public sealed class StarryBackdrop : MonoBehaviour
    {
        private Camera _camera;
        private SpriteRenderer _sky;
        private Sprite _skySprite;
        private readonly List<SpriteRenderer> _twinkles = new List<SpriteRenderer>();
        private readonly List<float> _twinklePhase = new List<float>();
        private readonly List<float> _twinkleSpeed = new List<float>();
        private readonly List<Vector3> _twinkleOffsets = new List<Vector3>();
        private readonly List<SpriteRenderer> _nebulae = new List<SpriteRenderer>();
        private readonly List<Vector3> _nebulaOffsets = new List<Vector3>();
        private readonly List<float> _nebulaPhase = new List<float>();
        private readonly List<float> _nebulaBaseAlpha = new List<float>();
        private LineRenderer _shooting;
        private float _shootTimer = 4.2f;
        private float _shootLife;
        private float _shootDuration;
        private Vector3 _shootStart;
        private Vector3 _shootEnd;
        private Color _zoneTint = Color.white;
        private Color _zoneTintTarget = Color.white;
        private int _zone = 1;
        private StarTheme _theme;

        public void Initialize(Camera camera)
        {
            _camera = camera;
            var go = new GameObject("Painted Sky");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0f, 12f);
            _sky = go.AddComponent<SpriteRenderer>();
            _sky.sortingOrder = -1000;
            _sky.shadowCastingMode = ShadowCastingMode.Off;
            _sky.receiveShadows = false;

            _shooting = ViewKit.CreateLine("Shooting Star", transform, -960, 0.022f,
                new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f), false, 2);
            _shooting.positionCount = 2;
            _shooting.gameObject.SetActive(false);
        }

        public void SetZone(int zone)
        {
            _zone = Mathf.Clamp(zone, 0, 2);
            if (_theme == null) return;
            Color accent = _zone == 0 ? _theme.Teal : _zone == 1 ? _theme.Violet : _theme.Gold;
            _zoneTintTarget = Color.Lerp(Color.white, accent, _zone == 0 ? 0.08f : _zone == 1 ? 0.05f : 0.08f);
        }

        public void ApplyTheme(StarTheme theme, int round)
        {
            _theme = theme;
            if (_sky == null) return;
            var index = Mathf.Clamp((round - 1) % StarPalette.BackgroundResources.Length, 0, 3);
            var texture = Resources.Load<Texture2D>(StarPalette.BackgroundResources[index]);
            if (texture == null)
                texture = Resources.Load<Texture2D>(StarPalette.BackgroundResources[0]);
            if (texture == null) return;

            if (_skySprite != null) Destroy(_skySprite);
            _skySprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            _skySprite.name = "Sky " + round;
            _sky.sprite = _skySprite;
            _sky.color = Color.white;
            FitSky();
            BuildTwinkles(round);
            BuildNebulae(round);
            SetZone(_zone);
        }

        private void FitSky()
        {
            if (_camera == null || _sky == null || _sky.sprite == null) return;
            float worldH = _camera.orthographicSize * 2f * 1.03f;
            float worldW = worldH * _camera.aspect;
            var size = _sky.sprite.bounds.size;
            float scale = Mathf.Max(worldW / Mathf.Max(0.001f, size.x), worldH / Mathf.Max(0.001f, size.y));
            _sky.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void BuildTwinkles(int round)
        {
            for (int i = 0; i < _twinkles.Count; i++)
            {
                if (_twinkles[i] != null) Destroy(_twinkles[i].gameObject);
            }
            _twinkles.Clear();
            _twinklePhase.Clear();
            _twinkleSpeed.Clear();
            _twinkleOffsets.Clear();

            var rnd = new System.Random(_theme != null ? _theme.Id.GetHashCode() ^ (round * 7919) : 88);
            float h = _camera.orthographicSize * 1.9f;
            float w = h * _camera.aspect;
            int count = 58;
            var glow = ProceduralArt.Glow(128, 3.8f);
            Color[] palette = { _theme.Cream, _theme.Gold, _theme.Teal, _theme.Violet };
            for (int i = 0; i < count; i++)
            {
                float x = ((float)rnd.NextDouble() * 2f - 1f) * w * 0.52f;
                float y = ((float)rnd.NextDouble() * 2f - 1f) * h * 0.5f;
                if (Mathf.Abs(x) < 0.7f && Mathf.Abs(y) < 0.9f) continue;
                Color tint = palette[i % palette.Length];
                float size = 0.05f + (float)rnd.NextDouble() * 0.4f;
                var sr = ViewKit.CreateSprite("Twinkle", transform, glow, true, -950,
                    new Color(tint.r, tint.g, tint.b, 0.2f));
                sr.transform.position = new Vector3(x, y, 11.8f);
                ViewKit.SetDiameter(sr.transform, glow, size);
                _twinkles.Add(sr);
                _twinkleOffsets.Add(new Vector3(x, y, 0f));
                _twinklePhase.Add((float)rnd.NextDouble() * Mathf.PI * 2f);
                _twinkleSpeed.Add(0.3f + (float)rnd.NextDouble() * 1.1f);
            }
        }

        private void BuildNebulae(int round)
        {
            for (int i = 0; i < _nebulae.Count; i++)
            {
                if (_nebulae[i] != null) Destroy(_nebulae[i].gameObject);
            }
            _nebulae.Clear();
            _nebulaOffsets.Clear();
            _nebulaPhase.Clear();
            _nebulaBaseAlpha.Clear();

            var rnd = new System.Random((_theme != null ? _theme.Id.GetHashCode() : 33) ^ (round * 4513));
            float h = _camera.orthographicSize * 1.9f;
            float w = h * _camera.aspect;
            Color[] palette = { _theme.Gold, _theme.Teal, _theme.Violet, _theme.Coral, _theme.Cream };
            for (int i = 0; i < 7; i++)
            {
                Color tint = palette[i % palette.Length];
                var sprite = ProceduralArt.Nebula(i + round);
                float alpha = i == 6 ? 0.08f : 0.12f;
                var sr = ViewKit.CreateSprite("Nebula Veil", transform, sprite, true, -985,
                    new Color(tint.r, tint.g, tint.b, alpha));
                float size = 7.5f + (float)rnd.NextDouble() * 9.0f;
                float baseSize = Mathf.Max(0.001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
                sr.transform.localScale = new Vector3(size / baseSize, size * 0.72f / baseSize, 1f);
                Vector3 offset = new Vector3(
                    ((float)rnd.NextDouble() * 2f - 1f) * w * 0.48f,
                    ((float)rnd.NextDouble() * 2f - 1f) * h * 0.42f,
                    0f);
                _nebulae.Add(sr);
                _nebulaOffsets.Add(offset);
                _nebulaPhase.Add((float)rnd.NextDouble() * Mathf.PI * 2f);
                _nebulaBaseAlpha.Add(alpha);
            }

            // Two tall, quiet aurora curtains add a vertical color axis to the sky.
            for (int i = 0; i < 2; i++)
            {
                Color tint = i == 0 ? _theme.Teal : _theme.Violet;
                float alpha = 0.1f;
                var sprite = ProceduralArt.Nebula(7 + i + round);
                var sr = ViewKit.CreateSprite("Aurora Curtain", transform, sprite, true, -984,
                    new Color(tint.r, tint.g, tint.b, alpha));
                float size = 10.5f + (float)rnd.NextDouble() * 7.0f;
                float baseSize = Mathf.Max(0.001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
                sr.transform.localScale = new Vector3(size * 0.34f / baseSize, size * 1.25f / baseSize, 1f);
                Vector3 offset = new Vector3(
                    (i == 0 ? -1f : 1f) * w * (0.24f + (float)rnd.NextDouble() * 0.14f),
                    ((float)rnd.NextDouble() * 2f - 1f) * h * 0.22f,
                    0f);
                _nebulae.Add(sr);
                _nebulaOffsets.Add(offset);
                _nebulaPhase.Add((float)rnd.NextDouble() * Mathf.PI * 2f);
                _nebulaBaseAlpha.Add(alpha);
            }
        }

        private void LateUpdate()
        {
            FitSky();
            if (_theme == null || _camera == null) return;
            float t = Time.unscaledTime;
            Vector3 cam = _camera.transform.position;
            if (_sky != null) _sky.transform.position = new Vector3(cam.x, cam.y, 12f);

            for (int i = 0; i < _twinkles.Count; i++)
            {
                var sr = _twinkles[i];
                if (sr == null) continue;
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * _twinkleSpeed[i] + _twinklePhase[i]);
                var p = cam + _twinkleOffsets[i];
                p.z = 11.8f;
                sr.transform.position = p;
                var c = sr.color;
                c.a = 0.025f + pulse * 0.16f;
                sr.color = c;
            }

            for (int i = 0; i < _nebulae.Count; i++)
            {
                var sr = _nebulae[i];
                if (sr == null) continue;
                float phase = _nebulaPhase[i];
                var p = cam + _nebulaOffsets[i] + new Vector3(Mathf.Sin(t * 0.035f + phase) * 0.35f, Mathf.Cos(t * 0.028f + phase) * 0.28f, 0f);
                p.z = 11.5f;
                sr.transform.position = p;
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.02f + phase) * 8f);
                var c = sr.color;
                c.a = _nebulaBaseAlpha[i] * (0.8f + 0.2f * Mathf.Sin(t * 0.35f + phase));
                sr.color = c;
            }

            _zoneTint = Color.Lerp(_zoneTint, _zoneTintTarget, 1f - Mathf.Exp(-1.2f * Time.deltaTime));
            if (_sky != null) _sky.color = _zoneTint;
            UpdateShootingStar(Time.deltaTime);
        }

        private void StartShootingStar()
        {
            if (_camera == null || _shooting == null) return;
            float h = _camera.orthographicSize * 1.9f;
            float w = h * _camera.aspect;
            bool fromLeft = UnityEngine.Random.value > 0.5f;
            _shootStart = new Vector3(fromLeft ? -w * 0.55f : w * 0.55f, h * (0.18f + UnityEngine.Random.value * 0.34f), 0f);
            _shootEnd = _shootStart + new Vector3(fromLeft ? w * 0.55f : -w * 0.55f,
                -h * (0.25f + UnityEngine.Random.value * 0.32f), 0f);
            _shootDuration = 1.1f + UnityEngine.Random.value * 0.8f;
            _shootLife = _shootDuration;
            _shooting.transform.position = new Vector3(_camera.transform.position.x, _camera.transform.position.y, 11.7f);
            _shooting.gameObject.SetActive(true);
        }

        private void UpdateShootingStar(float dt)
        {
            if (_shooting == null || _camera == null) return;
            if (_shootLife > 0f)
            {
                _shooting.transform.position = new Vector3(_camera.transform.position.x, _camera.transform.position.y, 11.7f);
                _shootLife -= dt;
                float q = 1f - Mathf.Clamp01(_shootLife / Mathf.Max(0.01f, _shootDuration));
                Vector3 head = Vector3.Lerp(_shootStart, _shootEnd, q);
                Vector3 tail = Vector3.Lerp(_shootStart, _shootEnd, Mathf.Max(0f, q - 0.16f));
                _shooting.SetPosition(0, tail);
                _shooting.SetPosition(1, head);
                float a = Mathf.Sin(q * Mathf.PI) * 0.65f;
                Color c = Color.Lerp(_theme.Cream, _theme.Gold, 0.35f);
                c.a = a;
                _shooting.startColor = new Color(c.r, c.g, c.b, 0f);
                _shooting.endColor = c;
                if (_shootLife <= 0f) _shooting.gameObject.SetActive(false);
            }
            else
            {
                _shootTimer -= dt;
                if (_shootTimer <= 0f)
                {
                    StartShootingStar();
                    _shootTimer = 3.8f + UnityEngine.Random.value * 4.6f;
                }
            }
        }
    }

    public sealed class MeteorWarningView
    {
        public GameObject Root { get; private set; }
        private LineRenderer _ring;
        private float _life;
        private float _duration;
        private Color _color;

        public static MeteorWarningView Create(Transform parent, Vector2 position, Color color, float radius)
        {
            var view = new MeteorWarningView();
            view.Root = new GameObject("Meteor Warning");
            view.Root.transform.SetParent(parent, false);
            view.Root.transform.localPosition = new Vector3(position.x, position.y, 0f);
            view._color = color;
            view._duration = 4.2f;
            view._life = view._duration;
            view._ring = ViewKit.CreateLine("Lock", view.Root.transform, 37, 0.034f,
                new Color(color.r, color.g, color.b, 0.85f),
                new Color(color.r, color.g, color.b, 0.05f), true, 64);
            ViewKit.SetCircle(view._ring, Mathf.Max(1.1f, radius), 64);
            return view;
        }

        public bool Tick(float dt)
        {
            if (Root == null) return false;
            _life -= dt;
            if (_life <= 0f)
            {
                Destroy();
                return false;
            }
            float q = 1f - _life / _duration;
            float pulse = 0.92f + Mathf.Sin(q * Mathf.PI * 6f) * 0.08f;
            Root.transform.localScale = new Vector3(pulse, pulse, 1f);
            Root.transform.localRotation = Quaternion.Euler(0f, 0f, q * 55f);
            _ring.startColor = new Color(_color.r, _color.g, _color.b, 0.5f + 0.25f * Mathf.Sin(q * Mathf.PI * 3f));
            _ring.endColor = new Color(_color.r, _color.g, _color.b, 0.04f);
            return true;
        }

        public void Destroy()
        {
            if (Root != null) Object.Destroy(Root);
            Root = null;
        }
    }

    public sealed class StardustView
    {
        public GameObject Root { get; private set; }
        private SpriteRenderer _core;
        private SpriteRenderer _glow;
        private Color _color;

        public static StardustView Create(Transform parent, Color color)
        {
            var view = new StardustView();
            view.Build(parent, color);
            return view;
        }

        private void Build(Transform parent, Color color)
        {
            _color = color;
            Root = new GameObject("Stardust");
            Root.transform.SetParent(parent, false);
            _glow = ViewKit.CreateSprite("Dust Glow", Root.transform, ProceduralArt.Glow(64, 2.4f), true, 28,
                new Color(color.r, color.g, color.b, 0.16f));
            ViewKit.SetDiameter(_glow.transform, _glow.sprite, 0.82f);
            _core = ViewKit.CreateSprite("Dust Core", Root.transform, ProceduralArt.Stardust(), true, 29,
                new Color(color.r, color.g, color.b, 0.92f));
            ViewKit.SetDiameter(_core.transform, _core.sprite, 0.22f);
        }

        public void Tick(float time, Vector2 position)
        {
            if (Root == null) return;
            Root.transform.localPosition = new Vector3(position.x, position.y, 0f);
            float pulse = 1f + Mathf.Sin(time * 2.6f + position.x * 1.7f) * 0.16f;
            Root.transform.localRotation = Quaternion.Euler(0f, 0f, time * 22f);
            _core.transform.localScale = Vector3.one * (0.22f * pulse / Mathf.Max(0.001f, _core.sprite.bounds.size.x));
        }

        public void SetVisible(bool visible)
        {
            if (Root != null) Root.SetActive(visible);
        }

        public void Destroy()
        {
            if (Root != null) Object.Destroy(Root);
        }
    }

    public sealed class BlackHoleView : MonoBehaviour
    {
        private StarTheme _theme;
        private Transform _root;
        private Transform _diskBack;
        private Transform _diskFront;
        private Transform _core;
        private SpriteRenderer _glowOuter;
        private SpriteRenderer _glowInner;
        private SpriteRenderer _diskBackSprite;
        private SpriteRenderer _diskFrontSprite;
        private SpriteRenderer _coreSprite;
        private SpriteRenderer _softShadow;
        private SpriteRenderer _hotspot;
        private SpriteRenderer _veilWarm;
        private SpriteRenderer _veilCold;
        private LineRenderer _photon;
        private LineRenderer _photonHalo;
        private LineRenderer _crescent;
        private LineRenderer _arcTop;
        private LineRenderer _arcBottom;
        private readonly List<LineRenderer> _swirls = new List<LineRenderer>();
        private readonly List<SpriteRenderer> _sparks = new List<SpriteRenderer>();
        private readonly List<float> _sparkPhase = new List<float>();
        private readonly List<float> _sparkRadius = new List<float>();
        private float _baseDiskBackX;
        private float _baseDiskBackY;
        private float _baseDiskFrontX;
        private float _baseDiskFrontY;
        private float _baseCoreScale;
        private float _visualScale = 1f;
        private float _activity;
        public void SetActivity(float activity) => _activity = Mathf.Clamp01(activity);

        public float Horizon { get; private set; } = 0.8f;

        public void Build(StarTheme theme, float horizon = 0.8f)
        {
            if (_root == null)
            {
                _root = new GameObject("Black Hole").transform;
                _root.SetParent(transform, false);
            }
            _theme = theme;
            Horizon = horizon;
            _visualScale = Mathf.Clamp(horizon * 0.82f, 1f, 1.5f);
            ViewKit.DestroyChildren(_root);
            _swirls.Clear();
            _sparks.Clear();
            _sparkPhase.Clear();
            _sparkRadius.Clear();

            _glowOuter = ViewKit.CreateSprite("Gravity Halo", _root, ProceduralArt.Glow(256, 3.1f), true, 0, WithAlpha(theme.Gold, 0.06f));
            ViewKit.SetDiameter(_glowOuter.transform, _glowOuter.sprite, 12.5f * _visualScale);
            _glowInner = ViewKit.CreateSprite("Cold Halo", _root, ProceduralArt.Glow(256, 3.5f), true, 0, WithAlpha(theme.Teal, 0.035f));
            ViewKit.SetDiameter(_glowInner.transform, _glowInner.sprite, 7.4f * _visualScale);

            // A smooth, elliptical breath of warm light beneath the painted disk.
            var breath = ViewKit.CreateSprite("Disk Breath", _root, ProceduralArt.Glow(256, 2.4f), true, 1,
                WithAlpha(theme.Gold, 0.10f));
            float breathBase = Mathf.Max(0.001f, breath.sprite.bounds.size.x);
            breath.transform.localScale = new Vector3(6.2f * _visualScale / breathBase, 3.1f * _visualScale / breathBase, 1f);
            breath.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

            // Two soft, asymmetric veils make the accretion disk read as a
            // nebular atmosphere rather than a set of concentric rings.
            var veilSprite = ProceduralArt.Glow(256, 2.2f);
            _veilWarm = ViewKit.CreateSprite("Warm Veil", _root, veilSprite, true, 1,
                WithAlpha(theme.Gold, 0.035f));
            float veilBase = Mathf.Max(0.001f, veilSprite.bounds.size.x);
            _veilWarm.transform.localScale = new Vector3(Horizon * 8.4f / veilBase, Horizon * 3.25f / veilBase, 1f);
            _veilWarm.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            _veilCold = ViewKit.CreateSprite("Cold Veil", _root, veilSprite, true, 0,
                WithAlpha(theme.Teal, 0.025f));
            _veilCold.transform.localScale = new Vector3(Horizon * 10.0f / veilBase, Horizon * 4.2f / veilBase, 1f);
            _veilCold.transform.localRotation = Quaternion.Euler(0f, 0f, 19f);

            var diskBackGo = new GameObject("Accretion Far");
            diskBackGo.transform.SetParent(_root, false);
            _diskBack = diskBackGo.transform;
            _diskBackSprite = diskBackGo.AddComponent<SpriteRenderer>();
            _diskBackSprite.sprite = ProceduralArt.Accretion(theme, 1);
            _diskBackSprite.sharedMaterial = ProceduralArt.AdditiveMaterial;
            _diskBackSprite.sortingOrder = 2;
            _diskBackSprite.color = new Color(1f, 0.9f, 0.78f, 0.74f);
            _diskBackSprite.shadowCastingMode = ShadowCastingMode.Off;
            _diskBackSprite.receiveShadows = false;

            var diskFrontGo = new GameObject("Accretion Near");
            diskFrontGo.transform.SetParent(_root, false);
            _diskFront = diskFrontGo.transform;
            _diskFrontSprite = diskFrontGo.AddComponent<SpriteRenderer>();
            _diskFrontSprite.sprite = ProceduralArt.Accretion(theme, 0);
            _diskFrontSprite.sharedMaterial = ProceduralArt.AdditiveMaterial;
            _diskFrontSprite.sortingOrder = 3;
            _diskFrontSprite.color = new Color(1f, 0.95f, 0.86f, 0.84f);
            _diskFrontSprite.shadowCastingMode = ShadowCastingMode.Off;
            _diskFrontSprite.receiveShadows = false;

            var b1 = _diskBackSprite.sprite.bounds.size.x;
            _baseDiskBackX = 5.8f * _visualScale / b1;
            _baseDiskBackY = 3.05f * _visualScale / b1;
            _diskBack.localScale = new Vector3(_baseDiskBackX, _baseDiskBackY, 1f);
            _diskBack.localRotation = Quaternion.Euler(0f, 0f, -13f);
            var b2 = _diskFrontSprite.sprite.bounds.size.x;
            _baseDiskFrontX = 4.6f * _visualScale / b2;
            _baseDiskFrontY = 2.42f * _visualScale / b2;
            _diskFront.localScale = new Vector3(_baseDiskFrontX, _baseDiskFrontY, 1f);
            _diskFront.localRotation = Quaternion.Euler(0f, 0f, 11f);

            _softShadow = ViewKit.CreateSprite("Event Shadow", _root, ProceduralArt.Glow(256, 2.3f), false, 6,
                new Color(0.001f, 0.003f, 0.008f, 0.45f));
            ViewKit.SetDiameter(_softShadow.transform, _softShadow.sprite, Horizon * 4.4f);

            _coreSprite = ViewKit.CreateSprite("Event Horizon", _root, ProceduralArt.BlackDisk(256), false, 7, Color.black);
            ViewKit.SetDiameter(_coreSprite.transform, _coreSprite.sprite, Horizon * 2f * 1.075f);
            _core = _coreSprite.transform;
            _baseCoreScale = _core.localScale.x;

            _photon = ViewKit.CreateLine("Photon Ring", _root, 8, 0.032f * _visualScale, WithAlpha(theme.Cream, 0.72f), WithAlpha(theme.Gold, 0.3f), true, 128);
            ViewKit.SetCircle(_photon, Horizon * 1.025f, 128);
            _photonHalo = ViewKit.CreateLine("Photon Halo", _root, 8, 0.12f * _visualScale, WithAlpha(theme.Gold, 0.06f), WithAlpha(theme.Cream, 0.02f), true, 128);
            ViewKit.SetCircle(_photonHalo, Horizon * 1.045f, 128);
            _crescent = ViewKit.CreateLine("Photon Crescent", _root, 9, 0.09f * _visualScale,
                WithAlpha(theme.Cream, 0.52f), new Color(1f, 1f, 1f, 0f), false, 32, true);
            var crescent = ViewKit.Arc(Vector2.zero, Horizon * 1.055f, Horizon * 1.055f, -0.9f, 0.72f, 32, 0f);
            _crescent.SetPositions(crescent);

            _arcTop = ViewKit.CreateLine("Lensing North", _root, 6, 0.04f * _visualScale, WithAlpha(theme.Cream, 0.42f), new Color(1f, 1f, 1f, 0f), false, 42);
            var top = ViewKit.Arc(Vector2.zero, Horizon * 2.0f, Horizon * 0.76f, 0.17f, Mathf.PI - 0.17f, 42, -0.18f);
            _arcTop.SetPositions(top);
            _arcBottom = ViewKit.CreateLine("Lensing South", _root, 6, 0.028f * _visualScale, WithAlpha(theme.Gold, 0.28f), new Color(1f, 1f, 1f, 0f), false, 42);
            var bottom = ViewKit.Arc(Vector2.zero, Horizon * 2.25f, Horizon * 0.82f, Mathf.PI + 0.22f, Mathf.PI * 2f - 0.22f, 42, 0.12f);
            _arcBottom.SetPositions(bottom);

            for (int i = 0; i < 4; i++)
            {
                var line = ViewKit.CreateLine(
                    "Nebula Current",
                    _root,
                    4,
                    (0.012f + i * 0.004f) * _visualScale,
                    WithAlpha(i % 2 == 0 ? theme.Gold : theme.Teal, 0.07f),
                    new Color(1f, 1f, 1f, 0f),
                    false,
                    46);
                _swirls.Add(line);
            }

            var sparkSprite = ProceduralArt.Glow(64, 2.3f);
            for (int i = 0; i < 20; i++)
            {
                var sr = ViewKit.CreateSprite("Rim Spark", _root, sparkSprite, true, 9,
                    WithAlpha(i % 3 == 0 ? theme.Cream : (i % 3 == 1 ? theme.Gold : theme.Teal), 0.2f));
                ViewKit.SetDiameter(sr.transform, sparkSprite, (0.055f + (i % 4) * 0.018f) * _visualScale);
                _sparks.Add(sr);
                _sparkPhase.Add(i * 2.399963f);
                _sparkRadius.Add(1.16f + (i % 7) * 0.11f);
            }

            _hotspot = ViewKit.CreateSprite("Inner Flare", _root, ProceduralArt.Glow(128, 2.2f), true, 9,
                WithAlpha(theme.Gold, 0.18f));
            ViewKit.SetDiameter(_hotspot.transform, _hotspot.sprite, Horizon * 0.38f);

            // Keep only the smooth lensed arcs and gas disk. The older
            // calligraphic swirls read as dotted rings at small sizes.
            for (int i = 0; i < _swirls.Count; i++) if (_swirls[i] != null) _swirls[i].enabled = false;
            for (int i = 0; i < _sparks.Count; i++) if (_sparks[i] != null) _sparks[i].enabled = false;
        }

        public void Tick(float time, float dt, float danger, float collapse)
        {
            if (_root == null || _theme == null) return;
            float pulse = 1f + Mathf.Sin(time * 0.85f) * 0.01f + collapse * 0.1f + _activity * 0.008f;
            _root.localScale = new Vector3(pulse, pulse, 1f);

            _diskBack.Rotate(0f, 0f, dt * (1.3f + _activity * 0.7f + collapse * 6f), Space.Self);
            _diskFront.Rotate(0f, 0f, -dt * (1.7f + _activity * 0.9f + collapse * 8f), Space.Self);
            _diskBack.localScale = new Vector3(_baseDiskBackX * (1f + collapse * 0.2f), _baseDiskBackY * (1f + collapse * 0.2f), 1f);
            _diskFront.localScale = new Vector3(_baseDiskFrontX * (1f + collapse * 0.15f), _baseDiskFrontY * (1f + collapse * 0.15f), 1f);

            _glowOuter.color = WithAlpha(_theme.Gold, 0.055f + _activity * 0.05f + danger * 0.025f + collapse * 0.07f);
            _glowInner.color = WithAlpha(_theme.Teal, 0.025f + _activity * 0.03f + collapse * 0.05f);
            _diskBackSprite.color = new Color(1f, 0.9f, 0.78f, 0.7f + _activity * 0.14f + collapse * 0.16f);
            _diskFrontSprite.color = new Color(1f, 0.95f, 0.86f, 0.8f + _activity * 0.1f + collapse * 0.1f);
            _veilWarm.color = WithAlpha(_theme.Gold, 0.025f + _activity * 0.03f + danger * 0.02f + collapse * 0.05f);
            _veilCold.color = WithAlpha(_theme.Teal, 0.02f + _activity * 0.02f + collapse * 0.04f);
            _veilWarm.transform.Rotate(0f, 0f, dt * (0.2f + _activity * 0.3f), Space.Self);
            _veilCold.transform.Rotate(0f, 0f, -dt * (0.15f + _activity * 0.2f), Space.Self);
            _softShadow.color = new Color(0.001f, 0.003f, 0.008f, 0.4f + collapse * 0.16f);
            if (_hotspot != null)
            {
                float hotAngle = time * 0.08f - 0.62f;
                float hotRadius = Horizon * 1.16f;
                _hotspot.transform.localPosition = new Vector3(Mathf.Cos(hotAngle) * hotRadius, Mathf.Sin(hotAngle) * hotRadius * 0.56f, 0f);
                var hotColor = _hotspot.color;
                hotColor.a = 0.1f + _activity * 0.12f + Mathf.Sin(time * 2.6f) * 0.035f + collapse * 0.1f;
                _hotspot.color = hotColor;
                ViewKit.SetDiameter(_hotspot.transform, _hotspot.sprite, Horizon * 0.38f * (1f + Mathf.Sin(time * 2.1f) * 0.04f));
            }
            if (_crescent != null)
            {
                var cs = _crescent.startColor;
                cs.a = 0.42f + _activity * 0.2f + Mathf.Sin(time * 1.7f) * 0.06f + collapse * 0.1f;
                _crescent.startColor = cs;
            }

            _core.localScale = Vector3.one * (_baseCoreScale * (1f + Mathf.Sin(time * 1.3f) * 0.004f - collapse * 0.05f));
            float photonWidth = (0.024f + Mathf.Sin(time * 2.2f) * 0.003f + _activity * 0.006f + collapse * 0.03f) * _visualScale;
            _photon.widthMultiplier = photonWidth;
            _photonHalo.widthMultiplier = (0.08f + _activity * 0.04f + collapse * 0.1f + danger * 0.02f) * _visualScale;

            UpdateSwirls(time, collapse);
            for (int i = 0; i < _sparks.Count; i++)
            {
                var s = _sparks[i];
                if (s == null) continue;
                float a = _sparkPhase[i] + time * (0.09f + (i % 5) * 0.014f) * (i % 2 == 0 ? 1f : -0.8f);
                float rr = Horizon * _sparkRadius[i] * (1f + collapse * 0.3f);
                s.transform.localPosition = new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr * 0.54f, 0f);
                var c = s.color;
                c.a = (0.035f + (i % 4) * 0.014f + _activity * 0.02f) * (1f + collapse * 0.8f);
                s.color = c;
            }
        }

        private void UpdateSwirls(float time, float collapse)
        {
            for (int lineIndex = 0; lineIndex < _swirls.Count; lineIndex++)
            {
                var line = _swirls[lineIndex];
                if (line == null) continue;
                int count = 46;
                line.positionCount = count;
                float direction = lineIndex % 2 == 0 ? 1f : -1f;
                float baseAngle = time * (0.045f + lineIndex * 0.008f + _activity * 0.02f) * direction + lineIndex * 1.9f;
                for (int i = 0; i < count; i++)
                {
                    float q = i / (float)(count - 1);
                    float radius = Horizon * (1.22f + q * (2.2f + lineIndex * 0.35f)) * (1f + collapse * 0.22f);
                    float a = baseAngle + q * (2.1f + lineIndex * 0.3f) * direction;
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * 0.57f, 0f));
                }
                var start = line.startColor;
                start.a = (0.012f + lineIndex * 0.004f + _activity * 0.012f) * (1f + collapse * 0.7f);
                var end = line.endColor;
                end.a = 0f;
                line.startColor = start;
                line.endColor = end;
            }
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    public sealed class PortalView : MonoBehaviour
    {
        private StarTheme _theme;
        private Transform _root;
        private Transform _sigil;
        private SpriteRenderer _glow;
        private SpriteRenderer _core;
        private readonly List<LineRenderer> _rings = new List<LineRenderer>();
        private readonly List<SpriteRenderer> _sparks = new List<SpriteRenderer>();
        private readonly List<float> _phases = new List<float>();
        private float _near;
        private float _flare;
        private const float Radius = 0.62f;

        public void Build(StarTheme theme)
        {
            if (_root == null)
            {
                _root = new GameObject("Wayfarer's Gate").transform;
                _root.SetParent(transform, false);
            }
            _theme = theme;
            ViewKit.DestroyChildren(_root);
            _rings.Clear();
            _sparks.Clear();
            _phases.Clear();

            var shadow = ViewKit.CreateSprite("Gate Shadow", _root, ProceduralArt.Glow(256, 2.2f), false, 8,
                WithAlpha(theme.Ink, 0.6f));
            ViewKit.SetDiameter(shadow.transform, shadow.sprite, 6.6f);

            var beam = ViewKit.CreateSprite("Gate Beam", _root, ProceduralArt.Glow(256, 2.8f), true, 9,
                WithAlpha(theme.Portal, 0.12f));
            float beamBase = Mathf.Max(0.001f, beam.sprite.bounds.size.x);
            beam.transform.localScale = new Vector3(2.25f / beamBase, 8.8f / beamBase, 1f);

            _glow = ViewKit.CreateSprite("Gate Aura", _root, ProceduralArt.Glow(256, 2.8f), true, 10,
                WithAlpha(theme.Portal, 0.38f));
            ViewKit.SetDiameter(_glow.transform, _glow.sprite, 5.5f);
            _core = ViewKit.CreateSprite("Gate Core", _root, ProceduralArt.Glow(128, 3.2f), true, 10,
                WithAlpha(theme.Cream, 0.42f));
            ViewKit.SetDiameter(_core.transform, _core.sprite, 1.65f);

            // A dark, quiet opening gives the gate depth instead of reading like
            // just another bright star.
            var opening = ViewKit.CreateSprite("Gate Opening", _root, ProceduralArt.BlackDisk(256), false, 11,
                new Color(0.006f, 0.018f, 0.03f, 0.9f));
            ViewKit.SetDiameter(opening.transform, opening.sprite, 1.38f);

            var sigilGo = new GameObject("Gate Sigil");
            sigilGo.transform.SetParent(_root, false);
            _sigil = sigilGo.transform;
            var sigil = sigilGo.AddComponent<SpriteRenderer>();
            sigil.sprite = ProceduralArt.PortalSigil(theme);
            sigil.sharedMaterial = ProceduralArt.AdditiveMaterial;
            sigil.sortingOrder = 12;
            sigil.color = Color.white;
            sigil.shadowCastingMode = ShadowCastingMode.Off;
            sigil.receiveShadows = false;
            var bounds = sigil.sprite.bounds.size.x;
            float s = 2.55f / bounds;
            _sigil.localScale = new Vector3(s, s, 1f);

            AddRing(1.45f, theme.Gold, 0.52f, 110);
            AddRing(1.18f, theme.Cream, 0.58f, 100);
            AddRing(0.9f, theme.Teal, 0.46f, 94);
            AddRing(0.69f, theme.Cream, 0.5f, 88);

            var sparkSprite = ProceduralArt.Glow(64, 2.5f);
            for (int i = 0; i < 12; i++)
            {
                var sp = ViewKit.CreateSprite("Gate Dust", _root, sparkSprite, true, 14,
                    WithAlpha(i % 2 == 0 ? theme.Portal : theme.Cream, 0.34f));
                ViewKit.SetDiameter(sp.transform, sparkSprite, 0.04f + (i % 3) * 0.03f);
                _sparks.Add(sp);
                _phases.Add(i * 2.399963f);
            }
        }

        private void AddRing(float radius, Color color, float alpha, int count)
        {
            var line = ViewKit.CreateLine("Gate Ring", _root, 13, 0.034f, WithAlpha(color, alpha), WithAlpha(color, alpha * 0.25f), true, count);
            ViewKit.SetCircle(line, radius, count);
            _rings.Add(line);
        }

        public void SetWorldPosition(Vector2 pos)
        {
            transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        }

        public void Tick(float time, float dt, float near, float flare)
        {
            if (_root == null) return;
            _near = Mathf.Clamp01(near);
            _flare = Mathf.Clamp01(flare);
            float pulse = 1f + Mathf.Sin(time * 1.8f) * 0.025f + _near * 0.06f + _flare * 0.22f;
            _root.localScale = new Vector3(pulse, pulse, 1f);
            if (_sigil != null) _sigil.Rotate(0f, 0f, -dt * (10f + _near * 18f), Space.Self);
            _glow.color = WithAlpha(_theme.Portal, 0.3f + _near * 0.16f + _flare * 0.35f);
            _core.color = WithAlpha(_theme.Cream, 0.46f + _near * 0.2f + _flare * 0.3f);
            for (int i = 0; i < _rings.Count; i++)
            {
                var ring = _rings[i];
                if (ring == null) continue;
                ring.transform.Rotate(0f, 0f, dt * (i % 2 == 0 ? 12f : -18f) * (0.5f + _near), Space.Self);
                var sc = ring.startColor;
                sc.a = (0.25f + i * 0.04f) * (1f + _near * 0.8f + _flare * 0.5f);
                ring.startColor = sc;
                var ec = ring.endColor;
                ec.a = sc.a * 0.2f;
                ring.endColor = ec;
            }
            for (int i = 0; i < _sparks.Count; i++)
            {
                float a = _phases[i] + time * (0.3f + (i % 4) * 0.06f);
                float rr = 1.58f + (i % 3) * 0.18f + Mathf.Sin(time * 0.5f + i) * 0.08f;
                _sparks[i].transform.localPosition = new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr * 0.83f, 0f);
                var c = _sparks[i].color;
                c.a = 0.18f + (i % 3) * 0.09f + _near * 0.08f;
                _sparks[i].color = c;
            }
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    public sealed class PlayerStarView : MonoBehaviour
    {
        private Transform _root;
        private Transform _visual;
        private Transform _trailGo;
        private SpriteRenderer _glowBig;
        private SpriteRenderer _glowSmall;
        private SpriteRenderer _body;
        private SpriteRenderer _silhouette;
        private SpriteRenderer _halo;
        private SpriteRenderer _aspectRing;
        private LineRenderer _aspectRingLine;
        private SpriteRenderer _companion;
        private LineRenderer _shieldRing;
        private LineRenderer _retroRing;
        private LineRenderer _trail;
        private LineRenderer _gaugeBase;
        private LineRenderer _gaugeFill;
        private StarTheme _theme;
        private Sprite _haloSpirte;
        private Sprite _ringSprite;
        private Sprite _companionSprite;
        private int _variant;

        public void Build(PlayerState player, Transform worldRoot, StarTheme theme)
        {
            _theme = theme;
            _variant = player.StarVariant;
            _root = new GameObject("The Player Star").transform;
            _root.SetParent(worldRoot, false);
            _visual = new GameObject("Star Heart").transform;
            _visual.SetParent(_root, false);

            _silhouette = ViewKit.CreateSprite("Star Silhouette", _visual, ProceduralArt.BlackDisk(256), false, 37,
                new Color(0.004f, 0.012f, 0.022f, 0.42f));
            ViewKit.SetDiameter(_silhouette.transform, _silhouette.sprite, 1.1f);

            var glow = ProceduralArt.Glow(256, 2.7f);
            _glowBig = ViewKit.CreateSprite("Corona", _visual, glow, true, 38, WithAlpha(theme.Gold, 0.30f));
            ViewKit.SetDiameter(_glowBig.transform, _glowBig.sprite, 3.4f);
            _glowSmall = ViewKit.CreateSprite("Star Glow", _visual, glow, true, 39, WithAlpha(theme.Star, 0.46f));
            ViewKit.SetDiameter(_glowSmall.transform, _glowSmall.sprite, 1.55f);
            _body = ViewKit.CreateSprite("Star Body", _visual, ProceduralArt.PlayerBody(_variant), true, 41,
                StarAspect.BodyColor(_variant, theme));
            ViewKit.SetDiameter(_body.transform, _body.sprite, 0.7f);
            _haloSpirte = ProceduralArt.StarHalo(_variant % 4);
            _halo = ViewKit.CreateSprite("Painted Halo", _visual, _haloSpirte, true, 40,
                WithAlpha(StarAspect.GlowColor(_variant, theme), 0.68f));
            ViewKit.SetDiameter(_halo.transform, _halo.sprite, 1.9f);

            if (StarAspect.HasRing(_variant))
            {
                _ringSprite = ProceduralArt.PlanetRing();
                _aspectRing = ViewKit.CreateSprite("Star Ring", _visual, _ringSprite, true, 42,
                    WithAlpha(StarAspect.AccentColor(_variant, theme), 0.78f));
                float b = Mathf.Max(0.001f, _ringSprite.bounds.size.x);
                _aspectRing.transform.localScale = new Vector3(1.6f / b, 0.55f / b, 1f);
                _aspectRing.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                _aspectRingLine = ViewKit.CreateLine("Star Ring Orbit", _root, 42, 0.032f,
                    WithAlpha(StarAspect.AccentColor(_variant, theme), 0.8f),
                    WithAlpha(StarAspect.AccentColor(_variant, theme), 0.18f), true, 72);
                _aspectRingLine.transform.localRotation = Quaternion.Euler(0f, 0f, -18f + player.StarTilt * 0.8f);
            }
            if (StarAspect.HasCompanion(_variant))
            {
                _companionSprite = ProceduralArt.PlanetBody(_variant + 31);
                _companion = ViewKit.CreateSprite("Star Companion", _root, _companionSprite, false, 39,
                    WithAlpha(theme.Moon, 0.95f));
                ViewKit.SetDiameter(_companion.transform, _companion.sprite, 0.3f);
            }

            _trail = ViewKit.CreateLine("Star Wake", worldRoot, 35, 0.16f,
                WithAlpha(theme.Star, 0.25f), new Color(1f, 1f, 1f, 0f), false, 24, true);
            _trailGo = _trail.gameObject.transform;

            _gaugeBase = ViewKit.CreateLine("Matter Ring", _root, 42, 0.009f,
                WithAlpha(theme.Cream, 0.16f), WithAlpha(theme.Cream, 0.04f), true, 80);
            ViewKit.SetCircle(_gaugeBase, 0.53f, 80);
            _gaugeFill = ViewKit.CreateLine("Matter Gauge", _root, 43, 0.016f,
                WithAlpha(theme.Cream, 0.75f), WithAlpha(theme.Gold, 0.6f), false, 80);

            _shieldRing = ViewKit.CreateLine("Stardust Shield", _root, 39, 0.028f,
                WithAlpha(theme.Teal, 0.5f), WithAlpha(theme.Cream, 0.1f), true, 72);
            ViewKit.SetCircle(_shieldRing, 1f, 72);
            _shieldRing.gameObject.SetActive(false);

            _retroRing = ViewKit.CreateLine("Retrograde Laps", _root, 44, 0.022f,
                WithAlpha(theme.Teal, 0.55f), WithAlpha(theme.Teal, 0.1f), false, 80, true);
            _retroRing.gameObject.SetActive(false);

        }

        public void Tick(PlayerState player, float time, float dt)
        {
            if (_root == null) return;
            _root.localPosition = new Vector3(player.Position.x, player.Position.y, 0f);
            _root.localScale = Vector3.one * (player.WinAlpha < 1f ? Mathf.Lerp(0.7f, 1f, player.WinAlpha) : 1f);
            _visual.Rotate(0f, 0f, dt * 9f, Space.Self);
            float pulse = 1f + Mathf.Sin(time * 2.2f) * 0.022f + player.CollisionPulse * 0.12f;
            _visual.localScale = new Vector3(pulse, pulse, 1f);

            float radius = Mathf.Max(0.1f, player.Radius);
            ViewKit.SetDiameter(_glowBig.transform, _glowBig.sprite, radius * 9.4f);
            ViewKit.SetDiameter(_glowSmall.transform, _glowSmall.sprite, radius * 4.4f);
            ViewKit.SetDiameter(_body.transform, _body.sprite, radius * 2.6f);
            ViewKit.SetDiameter(_halo.transform, _halo.sprite, radius * 7.4f);
            ViewKit.SetDiameter(_silhouette.transform, _silhouette.sprite, radius * 4.6f);
            _halo.transform.Rotate(0f, 0f, dt * 7f, Space.Self);

            float low = 1f - Mathf.Clamp01(player.Mass / 100f);
            var bodyBase = Color.Lerp(StarAspect.BodyColor(_variant, _theme), _theme.Cream, player.StarTint * 0.12f);
            var glowBase = StarAspect.GlowColor(_variant, _theme);
            var accent = StarAspect.AccentColor(_variant, _theme);
            var core = Color.Lerp(bodyBase, _theme.Coral, Mathf.Clamp01((low - 0.5f) * 1.6f));
            _body.color = WithAlpha(core, player.WinAlpha);
            _glowSmall.color = WithAlpha(core, (0.38f + player.CollisionPulse * 0.24f) * player.WinAlpha);
            _glowBig.color = WithAlpha(Color.Lerp(glowBase, _theme.Coral, low * 0.8f), (0.25f + player.CollisionPulse * 0.16f) * player.WinAlpha);
            _halo.color = WithAlpha(Color.Lerp(glowBase, _theme.Coral, low), (0.56f + player.CollisionPulse * 0.2f) * player.WinAlpha);
            if (_aspectRing != null)
            {
                float ringBase = Mathf.Max(0.001f, _ringSprite.bounds.size.x);
                _aspectRing.transform.localScale = new Vector3(radius * 4.4f / ringBase, radius * 1.45f / ringBase, 1f);
                _aspectRing.transform.localRotation = Quaternion.Euler(0f, 0f, -20f + player.StarTilt + Mathf.Sin(time * 0.35f) * 3f);
                _aspectRing.color = WithAlpha(Color.Lerp(accent, _theme.Coral, low), (0.68f + player.CollisionPulse * 0.15f) * player.WinAlpha);
                if (_aspectRingLine != null)
                {
                    int points = 72;
                    _aspectRingLine.positionCount = points;
                    for (int i = 0; i < points; i++)
                    {
                        float a = i / (float)points * Mathf.PI * 2f;
                        _aspectRingLine.SetPosition(i, new Vector3(Mathf.Cos(a) * radius * 2.65f, Mathf.Sin(a) * radius * 0.78f, 0f));
                    }
                    _aspectRingLine.startColor = WithAlpha(Color.Lerp(accent, _theme.Coral, low), (0.62f + player.CollisionPulse * 0.15f) * player.WinAlpha);
                    _aspectRingLine.endColor = WithAlpha(Color.Lerp(accent, _theme.Coral, low), 0.08f * player.WinAlpha);
                }
            }
            if (_companion != null)
            {
                float orbit = time * 0.8f + player.StarPhase + 1.1f;
                _companion.transform.localPosition = new Vector3(Mathf.Cos(orbit) * radius * 2.7f, Mathf.Sin(orbit) * radius * 1.25f, 0f);
                ViewKit.SetDiameter(_companion.transform, _companion.sprite, radius * 0.72f);
                _companion.color = WithAlpha(Color.Lerp(_theme.Moon, _theme.Coral, low * 0.7f), 0.86f * player.WinAlpha);
            }

            // Trail.
            int trailCount = Mathf.Min(player.Trail.Count, 26);
            _trail.positionCount = Mathf.Max(2, trailCount);
            if (trailCount >= 2)
            {
                for (int i = 0; i < trailCount; i++)
                {
                    var p = player.Trail[i];
                    _trail.SetPosition(i, new Vector3(p.x, p.y, 0f));
                }
            }
            else
            {
                _trail.SetPosition(0, new Vector3(player.Position.x + 0.01f, player.Position.y, 0f));
                _trail.SetPosition(1, new Vector3(player.Position.x, player.Position.y, 0f));
            }
            _trail.startColor = WithAlpha(Color.Lerp(bodyBase, _theme.Coral, low), 0.22f * player.WinAlpha);
            _trail.endColor = new Color(1f, 1f, 1f, 0f);
            _trail.widthMultiplier = radius * 0.72f;

            // Matter ring.
            float gaugeRadius = radius * 2.05f;
            ViewKit.SetCircle(_gaugeBase, gaugeRadius, 80);
            _gaugeBase.startColor = WithAlpha(_theme.Cream, 0.11f * player.WinAlpha);
            _gaugeBase.endColor = WithAlpha(_theme.Cream, 0.03f * player.WinAlpha);
            float fill = Mathf.Clamp01(player.Mass / 100f);
            int filledPoints = Mathf.Max(2, Mathf.RoundToInt(80f * fill));
            _gaugeFill.positionCount = filledPoints;
            _gaugeFill.enabled = fill > 0.002f;
            for (int i = 0; i < filledPoints; i++)
            {
                float q = fill <= 0.0001f ? 0f : i / (float)(filledPoints - 1);
                float a = Mathf.PI * 0.5f - q * Mathf.PI * 2f * fill;
                _gaugeFill.SetPosition(i, new Vector3(Mathf.Cos(a) * gaugeRadius, Mathf.Sin(a) * gaugeRadius, 0f));
            }
            var gaugeColor = Color.Lerp(_theme.Cream, _theme.Coral, Mathf.Clamp01((low - 0.55f) * 2f));
            _gaugeFill.startColor = WithAlpha(gaugeColor, 0.65f * player.WinAlpha);
            _gaugeFill.endColor = WithAlpha(gaugeColor, 0.25f * player.WinAlpha);

            if (_shieldRing != null)
            {
                if (_shieldRing.gameObject.activeSelf != player.Shield)
                    _shieldRing.gameObject.SetActive(player.Shield);
                if (player.Shield)
                {
                    float shieldRadius = radius * 3.35f;
                    ViewKit.SetCircle(_shieldRing, shieldRadius, 72);
                    float shieldPulse = 0.38f + Mathf.Sin(time * 3.1f) * 0.1f;
                    _shieldRing.startColor = WithAlpha(_theme.Teal, shieldPulse * player.WinAlpha);
                    _shieldRing.endColor = WithAlpha(_theme.Cream, shieldPulse * 0.22f * player.WinAlpha);
                }
            }

            if (_retroRing != null)
            {
                bool showRetro = player.RetroActive && player.RetroProgress > 0.01f;
                if (_retroRing.gameObject.activeSelf != showRetro)
                    _retroRing.gameObject.SetActive(showRetro);
                if (showRetro)
                {
                    float retroRadius = radius * 2.72f;
                    int points = Mathf.Clamp(Mathf.CeilToInt(80f * player.RetroProgress), 2, 80);
                    _retroRing.positionCount = points;
                    for (int i = 0; i < points; i++)
                    {
                        float a = Mathf.PI * 0.5f - (i / (float)(points - 1)) * Mathf.PI * 2f * player.RetroProgress;
                        _retroRing.SetPosition(i, new Vector3(Mathf.Cos(a) * retroRadius, Mathf.Sin(a) * retroRadius, 0f));
                    }
                    float retroAlpha = 0.26f + player.RetroProgress * 0.3f;
                    _retroRing.startColor = WithAlpha(_theme.Teal, retroAlpha * player.WinAlpha);
                    _retroRing.endColor = WithAlpha(_theme.Cream, 0.12f * player.WinAlpha);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
            if (_trailGo != null) _trailGo.gameObject.SetActive(visible);
        }

        public void DestroyView()
        {
            if (_root != null) Destroy(_root.gameObject);
            if (_trailGo != null) Destroy(_trailGo.gameObject);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    public sealed class CelestialBodyView
    {
        public GameObject Root { get; private set; }
        private Transform _visual;
        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _glow;
        private SpriteRenderer _halo;
        private SpriteRenderer _ring;
        private SpriteRenderer _moon;
        private LineRenderer _trail;
        private StarTheme _theme;
        private CelestialBody _body;
        private float _baseScale;
        private float _glowDiameter;
        private float _haloDiameter;
        private float _ringBase;
        private Color _mutedBody;
        private Color _mutedAccent;

        private static Color Mute(Color c, float saturation = 0.55f, float value = 0.9f)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            return Color.HSVToRGB(h, s * saturation, Mathf.Clamp01(v * value));
        }

        public static CelestialBodyView Create(CelestialBody body, Transform worldRoot, StarTheme theme)
        {
            var view = new CelestialBodyView();
            view.Build(body, worldRoot, theme);
            return view;
        }

        private void Build(CelestialBody body, Transform worldRoot, StarTheme theme)
        {
            _body = body;
            _theme = theme;
            Root = new GameObject("Body " + body.Kind);
            Root.transform.SetParent(worldRoot, false);
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(Root.transform, false);

            int sort = 20 + body.Tier;
            Sprite bodySprite;
            bool additiveBody = false;
            switch (body.Kind)
            {
                case BodyKind.Dust:
                    bodySprite = ProceduralArt.SoftDot();
                    additiveBody = true;
                    break;
                case BodyKind.Meteor:
                    bodySprite = ProceduralArt.MeteorBody(body.Seed);
                    break;
                case BodyKind.Planet:
                    bodySprite = ProceduralArt.PlanetBody(body.Seed);
                    break;
                default:
                    bodySprite = ProceduralArt.StarBody();
                    additiveBody = true;
                    break;
            }

            _mutedBody = Mute(body.Color, body.Kind == BodyKind.Star ? 0.7f : 0.52f, 0.9f);
            _mutedAccent = Mute(body.Accent, 0.55f, 0.9f);
            var glowSprite = ProceduralArt.Glow(128, 3f);
            _glowDiameter = body.Radius * (body.Kind == BodyKind.Star ? 7.2f : body.Kind == BodyKind.Planet ? 3.8f : body.Kind == BodyKind.Meteor ? 4.6f : 8f);
            _glow = ViewKit.CreateSprite("Mist", _visual, glowSprite, true, sort - 1,
                WithAlpha(Color.Lerp(_mutedBody, theme.Cream, 0.14f), body.Kind == BodyKind.Star ? 0.16f : 0.065f));
            ViewKit.SetDiameter(_glow.transform, glowSprite, _glowDiameter);

            _bodyRenderer = ViewKit.CreateSprite("Body", _visual, bodySprite, additiveBody, sort,
                Color.Lerp(_mutedBody, Color.white, body.Kind == BodyKind.Planet ? 0.16f : 0.08f));
            _baseScale = Mathf.Max(0.0001f, Mathf.Max(bodySprite.bounds.size.x, bodySprite.bounds.size.y));
            float diameter = body.Radius * (body.Kind == BodyKind.Star ? 2.25f : 2f);
            _bodyRenderer.transform.localScale = Vector3.one * (diameter / _baseScale);
            if (additiveBody && body.Kind == BodyKind.Dust)
                _bodyRenderer.transform.localScale = Vector3.one * (body.Radius * 4f / _baseScale);

            if (body.Kind == BodyKind.Star)
            {
                var halo = ProceduralArt.StarHalo(body.Seed % 2);
                _haloDiameter = body.Radius * 6.8f;
                _halo = ViewKit.CreateSprite("Halo", _visual, halo, true, sort - 1,
                    WithAlpha(Color.Lerp(_mutedAccent, theme.Cream, 0.2f), 0.3f));
                ViewKit.SetDiameter(_halo.transform, halo, _haloDiameter);
            }

            if (body.Kind == BodyKind.Planet && body.HasRing)
            {
                var ringSprite = ProceduralArt.PlanetRing();
                _ring = ViewKit.CreateSprite("Ring", _visual, ringSprite, true, sort - 1,
                    WithAlpha(_mutedAccent, 0.34f));
                _ringBase = Mathf.Max(0.001f, ringSprite.bounds.size.x);
                _ring.transform.localScale = new Vector3(body.Radius * 4.6f / _ringBase, body.Radius * 1.5f / _ringBase, 1f);
                _ring.transform.localRotation = Quaternion.Euler(0f, 0f, body.Seed % 2 == 0 ? -24f : 22f);
            }

            bool hasMoon = (body.Kind == BodyKind.Planet && body.Radius > 0.22f && body.Seed % 4 == 0)
                || (body.Kind == BodyKind.Star && body.Seed % 5 == 0);
            if (hasMoon)
            {
                var moonSprite = ProceduralArt.PlanetBody(body.Seed + 101);
                _moon = ViewKit.CreateSprite("Moon", Root.transform, moonSprite, false, sort - 2,
                    WithAlpha(Color.Lerp(theme.Moon, _mutedBody, 0.25f), 0.82f));
                ViewKit.SetDiameter(_moon.transform, moonSprite, body.Radius * 0.55f);
            }

            if (body.Kind != BodyKind.Dust)
            {
                _trail = ViewKit.CreateLine("Orbit Trail", worldRoot, sort - 2, body.Radius * 0.38f,
                    WithAlpha(_mutedBody, body.Kind == BodyKind.Star ? 0.15f : 0.08f),
                    new Color(1f, 1f, 1f, 0f), false, 18, true);
            }
        }

        public void Tick(float time, float dt)
        {
            if (Root == null || _body == null) return;
            Root.transform.localPosition = new Vector3(_body.Position.x, _body.Position.y, 0f);
            _visual.localRotation = Quaternion.Euler(0f, 0f, _body.Rotation * Mathf.Rad2Deg);
            float pulse = 1f + Mathf.Sin(time * 1.7f + _body.Seed * 0.017f) * 0.012f + _body.Pulse * 0.06f;
            _visual.localScale = new Vector3(pulse, pulse, 1f);

            float radius = Mathf.Max(0.01f, _body.Radius);
            float diameter = radius * (_body.Kind == BodyKind.Star ? 2.25f : 2f);
            _bodyRenderer.transform.localScale = Vector3.one * (diameter / _baseScale);
            if (_body.Kind == BodyKind.Dust)
                _bodyRenderer.transform.localScale = Vector3.one * (radius * 4f / _baseScale);
            if (_glow != null)
                ViewKit.SetDiameter(_glow.transform, _glow.sprite, radius *
                    (_body.Kind == BodyKind.Star ? 7.2f : _body.Kind == BodyKind.Planet ? 3.8f : _body.Kind == BodyKind.Meteor ? 4.6f : 8f));
            if (_halo != null)
                ViewKit.SetDiameter(_halo.transform, _halo.sprite, radius * 6.8f);
            if (_ring != null)
                _ring.transform.localScale = new Vector3(radius * 4.6f / _ringBase, radius * 1.5f / _ringBase, 1f);
            if (_moon != null)
                ViewKit.SetDiameter(_moon.transform, _moon.sprite, radius * 0.55f);

            float graceAlpha = _body.Grace > 0f ? 0.62f + 0.26f * Mathf.Sin(time * 22f + _body.Seed) : 1f;
            _bodyRenderer.color = WithAlpha(_bodyRenderer.color, graceAlpha);
            _glow.color = WithAlpha(_glow.color, (_body.Kind == BodyKind.Star ? 0.14f : 0.055f) * graceAlpha + _body.Pulse * 0.16f);
            if (_halo != null)
            {
                _halo.transform.Rotate(0f, 0f, dt * (_body.Kind == BodyKind.Star ? 3f : 2f), Space.Self);
                _halo.color = WithAlpha(_halo.color, 0.28f * graceAlpha);
            }
            if (_ring != null)
            {
                _ring.transform.Rotate(0f, 0f, dt * 1.8f, Space.Self);
            }
            if (_moon != null)
            {
                float orbit = time * (0.55f + (_body.Seed % 5) * 0.04f) + _body.Seed * 0.7f;
                _moon.transform.localPosition = new Vector3(
                    Mathf.Cos(orbit) * radius * 2.0f,
                    Mathf.Sin(orbit) * radius * 1.05f, 0f);
                _moon.color = WithAlpha(_moon.color, 0.72f * graceAlpha);
            }

            if (_trail != null)
            {
                int count = Mathf.Min(_body.Trail.Count, 28);
                _trail.positionCount = Mathf.Max(2, count);
                if (count >= 2)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var p = _body.Trail[i];
                        _trail.SetPosition(i, new Vector3(p.x, p.y, 0f));
                    }
                }
                else
                {
                    _trail.SetPosition(0, new Vector3(_body.Position.x + 0.01f, _body.Position.y, 0f));
                    _trail.SetPosition(1, new Vector3(_body.Position.x, _body.Position.y, 0f));
                }
                _trail.widthMultiplier = _body.Radius * 0.42f;
                _trail.startColor = WithAlpha(_mutedBody, (_body.Kind == BodyKind.Star ? 0.2f : 0.11f) * graceAlpha);
                _trail.endColor = new Color(1f, 1f, 1f, 0f);
            }
        }

        public void Destroy()
        {
            if (Root != null) Object.Destroy(Root);
            if (_trail != null) Object.Destroy(_trail.gameObject);
        }

        public void SetVisible(bool visible)
        {
            if (Root != null) Root.SetActive(visible);
            if (_trail != null) _trail.gameObject.SetActive(visible);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    public sealed class ShockwaveRing
    {
        private readonly LineRenderer _line;
        private readonly GameObject _root;
        private float _life;
        private float _maxLife;
        private float _maxRadius;
        private Color _color;

        public ShockwaveRing(Transform parent, Vector2 position, Color color, float maxRadius, float duration, int sortingOrder = 34)
        {
            _root = new GameObject("Shockwave");
            _root.transform.SetParent(parent, false);
            _root.transform.localPosition = new Vector3(position.x, position.y, 0f);
            _line = ViewKit.CreateLine("Ring", _root.transform, sortingOrder, 0.045f, WithAlpha(color, 0.65f), WithAlpha(color, 0f), true, 80);
            ViewKit.SetCircle(_line, 1f, 80);
            _maxLife = duration;
            _life = duration;
            _maxRadius = maxRadius;
            _color = color;
        }

        public bool Tick(float dt)
        {
            _life -= dt;
            if (_life <= 0f)
            {
                DestroyRoot();
                return false;
            }
            float p = 1f - Mathf.Clamp01(_life / _maxLife);
            float radius = Mathf.Max(0.01f, _maxRadius * (0.12f + p * 0.88f));
            _root.transform.localScale = new Vector3(radius, radius, 1f);
            float alpha = Mathf.Pow(1f - p, 1.5f) * 0.5f;
            _line.startColor = WithAlpha(_color, alpha);
            _line.endColor = WithAlpha(_color, alpha * 0.1f);
            _line.widthMultiplier = 0.045f / Mathf.Max(0.2f, radius) * (1f + p * 1.5f);
            return true;
        }

        public void DestroyRoot()
        {
            if (_root != null) Object.Destroy(_root);
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    public static class WorldFx
    {
        private static Material _particleMaterial;
        private static Material _gritMaterial;

        public static ParticleSystem CreateParticles(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 0.08f;
            main.startColor = Color.white;
            main.maxParticles = 1200;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = sortingOrder;
            r.sharedMaterial = ParticleMaterial;
            r.alignment = ParticleSystemRenderSpace.View;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        public static void Emit(ParticleSystem ps, Vector3 pos, int count, Color a, Color b, float speed, float size, float life, Vector2 bias = default)
        {
            if (ps == null || count <= 0) return;
            if (!ps.isPlaying) ps.Play();
            var ep = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = false,
                position = pos,
                startLifetime = life,
                startSize = size,
                startColor = a
            };
            for (int i = 0; i < count; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float s = speed * (0.25f + Random.value * 0.95f);
                ep.velocity = new Vector3(Mathf.Cos(angle) * s + bias.x, Mathf.Sin(angle) * s + bias.y, 0f);
                ep.startLifetime = life * (0.55f + Random.value * 0.8f);
                ep.startSize = size * (0.5f + Random.value * 1.1f);
                ep.startColor = Color.Lerp(a, b, Random.value);
                ep.rotation = Random.value * Mathf.PI * 2f;
                ps.Emit(ep, 1);
            }
        }

        public static Material ParticleMaterial
        {
            get
            {
                if (_particleMaterial == null)
                {
                    var shader = Resources.Load<Shader>("Starry/Additive");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    _particleMaterial = new Material(shader) { name = "Starry Particles" };
                    _particleMaterial.mainTexture = ProceduralArt.SoftDot().texture;
                    _particleMaterial.SetColor("_Color", Color.white);
                }
                return _particleMaterial;
            }
        }

        public static Material GritMaterial
        {
            get
            {
                if (_gritMaterial == null)
                {
                    var shader = Resources.Load<Shader>("Starry/Additive");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    _gritMaterial = new Material(shader) { name = "Starry Grit" };
                    _gritMaterial.mainTexture = ProceduralArt.StarBody().texture;
                    _gritMaterial.SetColor("_Color", Color.white);
                }
                return _gritMaterial;
            }
        }
    }
}
