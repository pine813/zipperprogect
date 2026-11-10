using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarryAbyss
{
    /// <summary>
    /// Everything luminous in the game is painted here at runtime:
    /// soft glows, impasto stars, rough little meteorites, the accretion disk,
    /// the gate sigil and the quiet UI textures. No external art dependencies.
    /// </summary>
    public static class ProceduralArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Texture2D _whiteTexture;
        private static Sprite _whiteSprite;
        private static Material _additiveMaterial;
        private static Material _lineMaterial;
        private static Texture2D _softLineTexture;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    _whiteTexture = MakeTexture(4, 4, Fill(4, 4, Color.white));
                    _whiteTexture.name = "Starry_White";
                    _whiteSprite = Sprite.Create(_whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                    _whiteSprite.name = "Starry_White";
                }
                return _whiteSprite;
            }
        }

        public static Material AdditiveMaterial
        {
            get
            {
                if (_additiveMaterial == null)
                {
                    var shader = Resources.Load<Shader>("Starry/Additive");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    _additiveMaterial = new Material(shader) { name = "Starry Additive" };
                    _additiveMaterial.mainTexture = WhiteSprite.texture;
                    _additiveMaterial.SetColor("_Color", Color.white);
                    _additiveMaterial.SetFloat("_Intensity", 1f);
                }
                return _additiveMaterial;
            }
        }

        public static Material LineMaterial
        {
            get
            {
                if (_lineMaterial == null)
                {
                    var shader = Resources.Load<Shader>("Starry/Additive");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    _lineMaterial = new Material(shader) { name = "Starry Soft Line" };
                    if (_softLineTexture == null) _softLineTexture = MakeSoftLineTexture();
                    _lineMaterial.mainTexture = _softLineTexture;
                    _lineMaterial.SetColor("_Color", Color.white);
                    _lineMaterial.SetFloat("_Intensity", 1f);
                }
                return _lineMaterial;
            }
        }

        public static Sprite Glow(int size = 256, float softness = 3.2f)
        {
            string key = "glow_" + size + "_" + softness.ToString("F2");
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                float dy = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r >= 1f) continue;
                    float a = Mathf.Pow(1f - r, softness);
                    a += Mathf.Pow(Mathf.Max(0f, 1f - r), 10f) * 0.35f;
                    a = Mathf.Clamp01(a);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Glow";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite StarBody()
        {
            const string key = "star_body";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 256;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                float dy = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > 1f) continue;

                    float disc = 1f - SmoothStep(0.39f, 0.5f, r);
                    float aura = Mathf.Pow(Mathf.Max(0f, 1f - r), 3.4f) * 0.42f;

                    float angle = Mathf.Atan2(dy, dx);
                    float rays = 0f;
                    const int rayCount = 14;
                    float mod = Mathf.Repeat(angle / (Mathf.PI * 2f) * rayCount + 0.5f, 1f) - 0.5f;
                    float rayWidth = 0.055f + Mathf.Sin(angle * 5f + 1.3f) * 0.012f;
                    float rayAngular = 1f - SmoothStep(0.0f, rayWidth, Mathf.Abs(mod));
                    float rayLength = 1f - SmoothStep(0.5f, 0.92f + Mathf.Sin(angle * 3f) * 0.08f, r);
                    rays = rayAngular * rayLength * 0.54f;

                    float grain = Hash01(x, y, 77) * 0.08f - 0.04f;
                    float a = Mathf.Clamp01(disc + aura + rays + grain * disc);
                    a *= 1f - SmoothStep(0.94f, 1f, r);
                    float brightness = Mathf.Clamp01(0.84f + (1f - r) * 0.28f + grain);
                    var c = new Color(brightness, brightness * 0.96f, brightness * 0.88f, a);
                    px[y * size + x] = c;
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_StarBody";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite PlayerBody(int variant)
        {
            variant = Mathf.Clamp(variant, 0, StarAspect.Count - 1);
            string key = "player_body_" + variant;
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 256;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            var rnd = new System.Random(8100 + variant * 311);
            float phase = (float)rnd.NextDouble() * 9f;
            float[] sx = new float[5];
            float[] sy = new float[5];
            float[] sr = new float[5];
            for (int i = 0; i < 5; i++)
            {
                sx[i] = -0.55f + (float)rnd.NextDouble() * 1.1f;
                sy[i] = -0.55f + (float)rnd.NextDouble() * 1.1f;
                sr[i] = 0.06f + (float)rnd.NextDouble() * 0.12f;
            }

            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r > 1f) continue;
                    float angle = Mathf.Atan2(ny, nx);
                    float edge = 0.95f;
                    if (variant == 4)
                        edge = 0.86f + 0.05f * Mathf.Sin(angle * 6f + phase);
                    if (r > edge) continue;
                    float edgeA = 1f - SmoothStep(edge - 0.07f, edge, r);
                    float sphere = Mathf.Sqrt(Mathf.Max(0f, 1f - r * r));
                    float light = Mathf.Clamp01(0.55f + (-nx * 0.3f - ny * 0.34f + sphere * 0.32f));
                    float grain = Mathf.PerlinNoise(nx * 8f + 21f + variant * 7f, ny * 9f + 13f) - 0.5f;
                    float pattern = grain * 0.16f;

                    switch (variant)
                    {
                        case 0:
                            pattern += Mathf.Sin(angle * 11f + phase) * 0.035f + Mathf.Sin(r * 19f - phase) * 0.035f;
                            break;
                        case 1:
                            pattern += Mathf.Pow(Mathf.Max(0f, Mathf.Sin(angle * 8f + phase * 2f)), 7f) * 0.2f;
                            pattern += Mathf.Sin(r * 14f + phase) * 0.035f;
                            break;
                        case 2:
                            for (int i = 0; i < 5; i++)
                            {
                                float d = Mathf.Sqrt((nx - sx[i]) * (nx - sx[i]) + (ny - sy[i]) * (ny - sy[i]));
                                pattern -= (1f - SmoothStep(sr[i], sr[i] + 0.075f, d)) * 0.32f;
                            }
                            pattern += Mathf.Sin(angle * 7f + r * 18f + phase) * 0.035f;
                            break;
                        case 3:
                            pattern += Mathf.Sin(ny * 23f + Mathf.Sin(nx * 5f + phase) * 0.8f + phase) * 0.085f;
                            pattern += Mathf.Exp(-((nx + 0.15f) * (nx + 0.15f) + (ny - 0.1f) * (ny - 0.1f)) * 55f) * 0.16f;
                            break;
                        case 4:
                            float sector = Mathf.Floor((angle + Mathf.PI) / (Mathf.PI * 2f) * 7f);
                            pattern += (Hash01((int)sector, variant, 31) - 0.5f) * 0.22f;
                            pattern += Mathf.Pow(Mathf.Max(0f, Mathf.Sin(angle * 7f + phase)), 12f) * 0.16f;
                            pattern += Mathf.Pow(Mathf.Max(0f, Mathf.Sin(r * 12f + phase)), 10f) * 0.1f;
                            break;
                        case 5:
                            for (int i = 0; i < 5; i++)
                            {
                                float d = Mathf.Sqrt((nx - sx[i]) * (nx - sx[i]) + (ny - sy[i]) * (ny - sy[i]));
                                pattern -= (1f - SmoothStep(sr[i] * 0.65f, sr[i] + 0.055f, d)) * 0.2f;
                                pattern += (1f - SmoothStep(sr[i] * 0.55f, sr[i] * 0.65f, d)) * 0.08f;
                            }
                            pattern -= Mathf.Clamp01(nx * 0.5f + ny * 0.2f + 0.15f) * 0.12f;
                            break;
                    }

                    float rim = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(r - (edge - 0.075f)) / 0.065f), 2f) * 0.18f;
                    float brightness = Mathf.Clamp01(light * (0.93f + pattern) + rim);
                    brightness = Mathf.Clamp01(0.66f + brightness * 0.42f);
                    px[y * size + x] = new Color(brightness, brightness, brightness, edgeA);
                }
            }

            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_PlayerBody" + variant;
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Nebula(int seed)
        {
            int variant = Mathf.Abs(seed) % 3;
            string key = "nebula_" + variant;
            if (Cache.TryGetValue(key, out var cached)) return cached;
            const int size = 512;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            var rnd = new System.Random(1200 + variant * 617);
            float phase = (float)rnd.NextDouble() * 13f;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny * 1.18f);
                    if (r > 1f) continue;
                    float edge = 1f - SmoothStep(0.52f, 1f, r);
                    float broad = Mathf.PerlinNoise(nx * 1.9f + 11f + variant * 7f, ny * 1.7f + 5f);
                    float fine = Mathf.PerlinNoise(nx * 5.2f + 31f + variant * 3f, ny * 4.7f + 17f);
                    float ribbon = 0.5f + 0.5f * Mathf.Sin((nx * 1.3f + ny * 0.72f) * 5.5f + phase + broad * 2.2f);
                    float a = edge * Mathf.Clamp01(broad * 0.62f + fine * 0.38f - 0.18f) * (0.36f + ribbon * 0.5f);
                    a = Mathf.Clamp01(a * 0.78f);
                    float brightness = Mathf.Clamp01(0.72f + broad * 0.3f);
                    px[y * size + x] = new Color(brightness, brightness, brightness, a);
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Nebula" + variant;
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Stardust()
        {
            const string key = "stardust_spark";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            const int size = 128;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r2 = nx * nx + ny * ny;
                    if (r2 > 1f) continue;
                    float axialH = Mathf.Exp(-ny * ny * 210f) * Mathf.Clamp01(1f - Mathf.Abs(nx) * 1.05f);
                    float axialV = Mathf.Exp(-nx * nx * 210f) * Mathf.Clamp01(1f - Mathf.Abs(ny) * 1.05f);
                    float diag = Mathf.Exp(-Mathf.Abs(nx - ny) * 42f) + Mathf.Exp(-Mathf.Abs(nx + ny) * 42f);
                    diag *= Mathf.Clamp01(1f - Mathf.Sqrt(r2) * 1.1f) * 0.3f;
                    float core = Mathf.Exp(-r2 * 95f);
                    float alpha = Mathf.Clamp01(core + axialH * 0.5f + axialV * 0.5f + diag);
                    float brightness = Mathf.Clamp01(0.9f + core * 0.1f);
                    px[y * size + x] = new Color(brightness, brightness, brightness, alpha);
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Stardust";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite PlanetBody(int seed)
        {
            int variant = Mathf.Abs(seed) % 6;
            string key = "planet_" + variant;
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 256;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            var rnd = new System.Random(2000 + variant * 977);
            float phase = (float)rnd.NextDouble() * 10f;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r > 0.96f) continue;
                    float edgeA = 1f - SmoothStep(0.91f, 0.955f, r);
                    float sphere = Mathf.Sqrt(Mathf.Max(0f, 1f - r * r));
                    float lighting = Mathf.Clamp01(0.48f + (-nx * 0.30f - ny * 0.36f + sphere * 0.36f));
                    float bands = Mathf.Sin(ny * (10.5f + variant * 1.7f) + Mathf.Sin(nx * 4.2f + phase) * 0.55f + phase) * 0.5f + 0.5f;
                    float fleck = Mathf.PerlinNoise(nx * 5f + 22f + variant * 4f, ny * 7f + 19f) - 0.5f;
                    float brightness = Mathf.Clamp01(lighting * (0.75f + bands * 0.16f) + fleck * 0.13f);
                    if (variant == 4)
                    {
                        // Ice caps and a darker, smoother ocean belt.
                        float polar = SmoothStep(0.48f, 0.93f, Mathf.Abs(ny));
                        float sea = Mathf.PerlinNoise(nx * 2.2f + 44f, ny * 3.1f + 8f);
                        brightness = Mathf.Clamp01(brightness * 0.84f + polar * 0.28f + sea * 0.08f);
                    }
                    else if (variant == 5)
                    {
                        // Old, cratered moon: cooler and rougher than the gas giants.
                        float crater = Mathf.PerlinNoise(nx * 12f + 3f, ny * 13f + 61f);
                        brightness = Mathf.Clamp01(brightness * 0.9f + crater * 0.12f);
                    }
                    float rim = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(r - 0.9f) / 0.055f), 2f) * Mathf.Clamp01((-nx * 0.4f - ny * 0.6f + 0.1f)) * 0.25f;
                    brightness = Mathf.Clamp01(brightness + rim);
                    var c = new Color(brightness, brightness * 0.94f, brightness * 0.83f, edgeA);
                    px[y * size + x] = c;
                }
            }

            // A few darker weather stains; the moon variant also gets crater pits.
            for (int i = 0; i < 8; i++)
            {
                float cx = (float)rnd.NextDouble() * size;
                float cy = (float)rnd.NextDouble() * size;
                float rr = size * (0.014f + (float)rnd.NextDouble() * 0.06f);
                Stamp(px, size, size, cx, cy, rr, new Color(0.18f, 0.15f, 0.14f, 0.11f), 2f);
            }
            if (variant == 5)
            {
                for (int i = 0; i < 7; i++)
                {
                    float cx = size * (0.3f + (float)rnd.NextDouble() * 0.4f);
                    float cy = size * (0.3f + (float)rnd.NextDouble() * 0.4f);
                    float rr = size * (0.018f + (float)rnd.NextDouble() * 0.05f);
                    Stamp(px, size, size, cx, cy, rr, new Color(0.12f, 0.13f, 0.13f, 0.2f), 2f);
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Planet" + variant;
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite MeteorBody(int seed)
        {
            int variant = Mathf.Abs(seed) % 3;
            string key = "meteor_" + variant;
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 128;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            var rnd = new System.Random(900 + variant * 191);
            float[] edges = new float[14];
            for (int i = 0; i < edges.Length; i++) edges[i] = 0.66f + (float)rnd.NextDouble() * 0.31f;

            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r > 1f) continue;
                    float angle = Mathf.Atan2(ny, nx);
                    float t = (angle + Mathf.PI) / (Mathf.PI * 2f) * edges.Length;
                    int i0 = Mathf.FloorToInt(t) % edges.Length;
                    int i1 = (i0 + 1) % edges.Length;
                    float edge = Mathf.Lerp(edges[i0], edges[i1], t - Mathf.Floor(t));
                    if (r > edge) continue;
                    float edgeA = 1f - SmoothStep(edge - 0.035f, edge, r);
                    float lighting = Mathf.Clamp01(0.36f + (-nx * 0.42f - ny * 0.45f) + (1f - r) * 0.22f);
                    float grain = Mathf.PerlinNoise(nx * 9f + 43f + variant * 8f, ny * 11f + 7f) * 0.28f;
                    float crater = 0f;
                    if (r < 0.65f)
                    {
                        float cx1 = -0.18f + variant * 0.1f;
                        float cy1 = 0.12f;
                        float d = Mathf.Sqrt((nx - cx1) * (nx - cx1) + (ny - cy1) * (ny - cy1));
                        crater = (1f - SmoothStep(0.05f, 0.15f, d)) * 0.22f;
                    }
                    float brightness = Mathf.Clamp01(lighting + grain - crater);
                    var c = new Color(brightness, brightness * 0.93f, brightness * 0.82f, edgeA);
                    px[y * size + x] = c;
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Meteor" + variant;
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite PlanetRing()
        {
            const string key = "planet_ring";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 512;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float ex = nx / 0.91f;
                    float ey = ny / 0.24f;
                    float d = Mathf.Sqrt(ex * ex + ey * ey);
                    if (d < 0.68f || d > 1.03f) continue;
                    float ang = Mathf.Atan2(ny, nx);
                    float bands = 0.5f + 0.5f * Mathf.Sin(d * 58f + 1.7f);
                    float alpha = (1f - SmoothStep(0.93f, 1.02f, d)) * (1f - SmoothStep(0.68f, 0.73f, d));
                    alpha *= 0.25f + bands * 0.45f;
                    alpha *= 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(ang * 7f + d * 18f));
                    px[y * size + x] = new Color32(255, 245, 218, (byte)(Mathf.Clamp01(alpha) * 255f));
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_PlanetRing";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite StarHalo(int variant = 0)
        {
            string key = "star_halo_" + variant;
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 512;
            var alpha = new float[size * size];
            var rnd = new System.Random(1414 + variant * 733);
            float half = size * 0.5f;

            // Uneven concentric arcs of thick paint.
            for (int ring = 0; ring < 4; ring++)
            {
                float radius = (0.31f + ring * 0.085f + (float)rnd.NextDouble() * 0.015f) * size;
                float start = (float)rnd.NextDouble() * Mathf.PI * 2f;
                int pieces = 3 + rnd.Next(3);
                for (int p = 0; p < pieces; p++)
                {
                    float a0 = start + p / (float)pieces * Mathf.PI * 2f;
                    float a1 = a0 + 0.3f + (float)rnd.NextDouble() * 1.1f;
                    int steps = Mathf.Max(6, Mathf.CeilToInt((a1 - a0) * 38f));
                    for (int i = 0; i <= steps; i++)
                    {
                        float a = Mathf.Lerp(a0, a1, i / (float)steps);
                        float rr = radius + Mathf.Sin(a * 5f + ring) * 1.7f;
                        StampAdd(alpha, size, size, half + Mathf.Cos(a) * rr, half + Mathf.Sin(a) * rr, 1.1f + ring * 0.28f, 0.16f + ring * 0.018f);
                    }
                }
            }

            // Curling brush rays.
            int rays = 17;
            for (int i = 0; i < rays; i++)
            {
                float a = i / (float)rays * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.12f;
                float r0 = (0.34f + (float)rnd.NextDouble() * 0.1f) * size;
                float len = (0.06f + (float)rnd.NextDouble() * 0.15f) * size;
                int steps = 20;
                for (int j = 0; j < steps; j++)
                {
                    float t = j / (float)(steps - 1);
                    float rr = r0 + t * len;
                    float bend = a + t * 0.1f;
                    StampAdd(alpha, size, size, half + Mathf.Cos(bend) * rr, half + Mathf.Sin(bend) * rr, 0.8f + (1f - t) * 1.4f, 0.12f * (1f - t));
                }
            }

            // A few separate flecks.
            for (int i = 0; i < 48; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float rr = (0.32f + (float)rnd.NextDouble() * 0.16f) * size;
                StampAdd(alpha, size, size, half + Mathf.Cos(a) * rr, half + Mathf.Sin(a) * rr, 0.6f + (float)rnd.NextDouble() * 1.5f, 0.13f);
            }

            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                float a = Mathf.Clamp01(alpha[i]) * 0.85f;
                px[i] = new Color32(255, 246, 222, (byte)(a * 255f));
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_StarHalo" + variant;
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Accretion(StarTheme theme, int variant)
        {
            string key = "accretion_" + theme.Id + "_" + variant;
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 512;
            var px = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;

            // A physically softer disk: smooth radial falloff, turbulent gas,
            // and a subtle Doppler asymmetry. No hard concentric rings.
            Color innerColor = Color.Lerp(theme.GoldLight, theme.Cream, 0.42f);
            Color midColor = Color.Lerp(theme.Gold, theme.Cream, 0.18f);
            Color outerColor = Color.Lerp(theme.Teal, theme.Gold, 0.58f);
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r < 0.19f || r > 1f) continue;
                    float angle = Mathf.Atan2(ny, nx);
                    float inner = SmoothStep(0.19f, 0.28f, r);
                    float outer = 1f - SmoothStep(0.72f, 1f, r);
                    float turbulence = Mathf.PerlinNoise(nx * 3.1f + 5.2f, ny * 3.1f + variant * 1.7f);
                    turbulence = 0.35f + 0.65f * turbulence;
                    float fine = Mathf.PerlinNoise(nx * 8.4f + 19f, ny * 8.4f + 8f);
                    float doppler = 0.58f + 0.42f * Mathf.Cos(angle - 0.35f);
                    float edgeGlow = Mathf.Exp(-Mathf.Pow((r - 0.23f) * 12f, 2f)) * 0.18f;
                    float a = inner * outer * (0.075f + 0.14f * turbulence * doppler + 0.04f * fine * doppler) + edgeGlow * inner;
                    Color color;
                    if (r < 0.34f)
                        color = Color.Lerp(innerColor, midColor, Mathf.InverseLerp(0.2f, 0.34f, r) * 0.6f);
                    else if (r < 0.66f)
                        color = Color.Lerp(midColor, outerColor, Mathf.InverseLerp(0.34f, 0.66f, r) * 0.65f);
                    else
                        color = Color.Lerp(outerColor, theme.Cream, Mathf.InverseLerp(0.66f, 1f, r) * 0.14f);
                    color = Color.Lerp(color, theme.Cream, Mathf.Clamp01((doppler - 0.72f) * 0.5f));
                    color.a = Mathf.Clamp01(a * 1.65f);
                    px[y * size + x] = color;
                }
            }

            // No stamped filaments: at this scale they read as dotted rings.
            // The per-pixel turbulence already supplies the gas structure.

            var tex = MakeTexture(size, size, px, false);
            tex.name = "Starry_Accretion_" + theme.Id + "_" + variant;
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite BlackDisk(int size = 256)
        {
            string key = "black_disk_" + size;
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = 1f - SmoothStep(0.93f, 1f, r);
                    if (a <= 0f) continue;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_BlackDisk";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite PortalSigil(StarTheme theme)
        {
            string key = "portal_sigil_" + theme.Id;
            if (Cache.TryGetValue(key, out var cached)) return cached;

            const int size = 512;
            var px = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            var rnd = new System.Random(5209);

            // Rings.
            for (int ring = 0; ring < 3; ring++)
            {
                float rr = (0.3f + ring * 0.09f) * size;
                int dots = 130 + ring * 30;
                for (int i = 0; i < dots; i++)
                {
                    float a = i / (float)dots * Mathf.PI * 2f;
                    if (ring == 0 && (i % 7 == 0 || i % 11 == 0)) continue;
                    Color c = ring == 1 ? theme.Cream : theme.Gold;
                    StampAddColored(px, size, size, half + Mathf.Cos(a) * rr, half + Mathf.Sin(a) * rr, 0.55f + ring * 0.2f, c, 0.23f - ring * 0.04f);
                }
            }

            // Twelve pale glyphs.
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                float rr = 0.46f * size;
                float cx = half + Mathf.Cos(a) * rr;
                float cy = half + Mathf.Sin(a) * rr;
                float tang = a + Mathf.PI * 0.5f;
                int marks = i % 3 == 0 ? 5 : 2;
                for (int m = 0; m < marks; m++)
                {
                    float ox = Mathf.Cos(tang) * (m - (marks - 1) * 0.5f) * 4f;
                    float oy = Mathf.Sin(tang) * (m - (marks - 1) * 0.5f) * 4f;
                    StampAddColored(px, size, size, cx + ox, cy + oy, 1.1f, theme.Cream, 0.35f);
                }
            }

            // Inner spiral.
            int steps = 420;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)steps;
                float a = t * Mathf.PI * 5.2f + 0.4f;
                float rr = (0.035f + t * 0.24f) * size;
                StampAddColored(px, size, size, half + Mathf.Cos(a) * rr, half + Mathf.Sin(a) * rr, 1.2f + (1f - t) * 1.4f, i % 4 == 0 ? theme.Cream : theme.Gold, 0.2f * (1f - t * 0.5f));
            }

            // A little constellation of flecks.
            for (int i = 0; i < 55; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float rr = (0.2f + (float)rnd.NextDouble() * 0.3f) * size;
                StampAddColored(px, size, size, half + Mathf.Cos(a) * rr, half + Mathf.Sin(a) * rr, 0.45f + (float)rnd.NextDouble(), theme.Cream, 0.3f);
            }

            var tex = MakeTexture(size, size, px, false);
            tex.name = "Starry_PortalSigil_" + theme.Id;
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite CircleOutline(int size = 128, float lineRadius = 0.49f, float lineWidth = 1.2f)
        {
            string key = "circle_" + size + "_" + lineRadius + "_" + lineWidth;
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = 1f - SmoothStep(0f, lineWidth / size * 2f, Mathf.Abs(r - lineRadius));
                    if (a <= 0f) continue;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Circle";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Arrow()
        {
            const string key = "arrow";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            const int size = 96;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float width = Mathf.Lerp(0.5f, 0.018f, Mathf.Clamp01(ny / 0.92f));
                    if (ny > 0.06f && Mathf.Abs(nx) < width)
                    {
                        float alpha = Mathf.Clamp01((1f - Mathf.Abs(nx) / Mathf.Max(0.0001f, width)) * 3f);
                        float vertical = Mathf.Clamp01((ny - 0.06f) * 16f) * Mathf.Clamp01((0.96f - ny) * 20f);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * vertical * 230f));
                    }
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Arrow";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Diamond()
        {
            const string key = "diamond";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            const int size = 32;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int y = 0; y < size; y++)
            {
                float ny = Mathf.Abs((y + 0.5f - 16f) / 16f);
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs((x + 0.5f - 16f) / 16f);
                    if (nx + ny < 0.95f)
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01((0.95f - nx - ny) * 8f) * 255f));
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Diamond";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Frame()
        {
            const string key = "frame";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            const int size = 64;
            const int border = 12;
            var px = new Color32[size * size];
            var clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < border || y < border || x >= size - border || y >= size - border)
                    {
                        float edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                        float a = 1f - SmoothStep(0f, 1.5f, edge);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Frame";
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = "Starry_Frame";
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite HorizontalFade(int width = 512, int height = 2)
        {
            string key = "hfade_" + width;
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var px = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float a = 1f - SmoothStep(0.15f, 1f, x / (float)(width - 1));
                    px[y * width + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = MakeTexture(width, height, px);
            tex.name = "Starry_HFade";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite VerticalFade(int height = 512, int width = 2)
        {
            string key = "vfade_" + height + "_" + width;
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var px = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                float a = 1f - SmoothStep(0.0f, 1f, v);
                a = a * a;
                for (int x = 0; x < width; x++)
                    px[y * width + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            var tex = MakeTexture(width, height, px);
            tex.name = "Starry_VFade";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Vignette(int size = 512)
        {
            const string key = "vignette";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;
                    float a = Mathf.Pow(SmoothStep(0.37f, 1f, r), 1.8f) * 0.78f;
                    px[y * size + x] = new Color32(1, 4, 8, (byte)(a * 255f));
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Vignette";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Grain(int size = 256)
        {
            const string key = "grain";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var px = new Color32[size * size];
            var rnd = new System.Random(7719);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = (float)rnd.NextDouble();
                    byte a = (byte)(v * v * 65f);
                    byte c = (byte)(180 + v * 60);
                    px[y * size + x] = new Color32(c, c, c, a);
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_Grain";
            tex.wrapMode = TextureWrapMode.Repeat;
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite SoftDot()
        {
            const string key = "softdot";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            const int size = 64;
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f - half) / half;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Pow(Mathf.Max(0f, 1f - r), 2.2f) * 0.92f;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            var tex = MakeTexture(size, size, px);
            tex.name = "Starry_SoftDot";
            var sprite = MakeSprite(tex);
            Cache[key] = sprite;
            return sprite;
        }

        private static Texture2D MakeSoftLineTexture()
        {
            const int h = 32;
            var px = new Color32[h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h;
                float a = Mathf.Pow(Mathf.Sin(v * Mathf.PI), 1.4f);
                px[y] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false, false)
            {
                name = "Starry_SoftLine",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        // ---------------------------------------------------------
        // Utilities
        // ---------------------------------------------------------

        private static Color32[] Fill(int w, int h, Color c)
        {
            var px = new Color32[w * h];
            var c32 = (Color32)c;
            for (int i = 0; i < px.Length; i++) px[i] = c32;
            return px;
        }

        private static Texture2D MakeTexture(int w, int h, Color32[] px, bool bilinear = true)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = bilinear ? FilterMode.Bilinear : FilterMode.Bilinear,
                anisoLevel = 1
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        private static Sprite MakeSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / Mathf.Max(0.00001f, b - a));
            return t * t * (3f - 2f * t);
        }

        private static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        private static void Stamp(Color32[] px, int w, int h, float cx, float cy, float radius, Color color, float power)
        {
            int minX = Mathf.FloorToInt(cx - radius);
            int maxX = Mathf.CeilToInt(cx + radius);
            int minY = Mathf.FloorToInt(cy - radius);
            int maxY = Mathf.CeilToInt(cy + radius);
            for (int y = Mathf.Max(0, minY); y <= Mathf.Min(h - 1, maxY); y++)
            {
                float dy = y + 0.5f - cy;
                for (int x = Mathf.Max(0, minX); x <= Mathf.Min(w - 1, maxX); x++)
                {
                    float dx = x + 0.5f - cx;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    if (d >= 1f) continue;
                    float a = Mathf.Pow(1f - d, power);
                    int idx = y * w + x;
                    var dst = px[idx];
                    float srcA = color.a * a;
                    if (srcA <= 0f) continue;
                    Color blended = Color.Lerp(dst, color, srcA);
                    blended.a = Mathf.Clamp01(dst.a + srcA * (1f - dst.a));
                    px[idx] = blended;
                }
            }
        }

        private static void StampAdd(float[] alpha, int w, int h, float cx, float cy, float radius, float value)
        {
            int minX = Mathf.FloorToInt(cx - radius);
            int maxX = Mathf.CeilToInt(cx + radius);
            int minY = Mathf.FloorToInt(cy - radius);
            int maxY = Mathf.CeilToInt(cy + radius);
            for (int y = Mathf.Max(0, minY); y <= Mathf.Min(h - 1, maxY); y++)
            {
                float dy = y + 0.5f - cy;
                for (int x = Mathf.Max(0, minX); x <= Mathf.Min(w - 1, maxX); x++)
                {
                    float dx = x + 0.5f - cx;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    if (d >= 1f) continue;
                    float a = Mathf.Pow(1f - d, 1.8f);
                    alpha[y * w + x] = Mathf.Clamp01(alpha[y * w + x] + value * a);
                }
            }
        }

        private static void StampAddColored(Color32[] px, int w, int h, float cx, float cy, float radius, Color color, float alpha)
        {
            int minX = Mathf.FloorToInt(cx - radius);
            int maxX = Mathf.CeilToInt(cx + radius);
            int minY = Mathf.FloorToInt(cy - radius);
            int maxY = Mathf.CeilToInt(cy + radius);
            for (int y = Mathf.Max(0, minY); y <= Mathf.Min(h - 1, maxY); y++)
            {
                float dy = y + 0.5f - cy;
                for (int x = Mathf.Max(0, minX); x <= Mathf.Min(w - 1, maxX); x++)
                {
                    float dx = x + 0.5f - cx;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    if (d >= 1f) continue;
                    float add = color.a * alpha * Mathf.Pow(1f - d, 1.7f);
                    if (add <= 0f) continue;
                    int idx = y * w + x;
                    var dst = px[idx];
                    Color c = dst + color * add;
                    c.a = Mathf.Clamp01(dst.a + add);
                    px[idx] = c;
                }
            }
        }
    }
}
