using System.Collections.Generic;
using UnityEngine;

namespace StarryAbyss
{
    public enum BodyKind
    {
        Dust = 0,
        Meteor = 1,
        Planet = 2,
        Star = 3
    }

    public enum GamePhase
    {
        Title,
        Playing,
        Paused,
        Boon,
        DeathCinematic,
        WinCinematic,
        End
    }

    public sealed class BoonOption
    {
        public int Id;
        public string Title;
        public string Effect;
        public string Flavor;
    }

    public sealed class CelestialBody
    {
        public int Id;
        public BodyKind Kind;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Mass;
        public float Radius;
        public float Rotation;
        public float Spin;
        public float Grace;
        public float HitCooldown;
        public float BumpCooldown;
        public float Age;
        public float Pulse;
        public bool Dead;
        public bool IsChild;
        public bool HasRing;
        public bool Shower;
        public float TraceCooldown;
        public float FragmentCooldown;
        public int Seed;
        public Color Color;
        public Color Accent;
        public List<Vector2> Trail = new List<Vector2>(24);
        public CelestialBodyView View;

        public int Tier => (int)Kind;
    }

    public sealed class PlayerState
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Mass = 100f;
        public float Radius = 0.25f;
        public float Rotation;
        public float SteerVisual;
        public float CollisionPulse;
        public float HitCooldown;
        public float TrailTimer;
        public float EmitTimer;
        public bool Alive = true;
        public float WinAlpha = 1f;
        public List<Vector2> Trail = new List<Vector2>(32);
        public PlayerStarView View;
        public int StarVariant;
        public string StarName;
        public float StarTint;
        public float StarTilt;
        public float StarPhase;
        public bool Shield;
        public float RetroProgress;
        public bool RetroActive;

        public float Speed => Velocity.magnitude;
    }

    public sealed class StardustMote
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Phase;
        public bool Alive = true;
        public StardustView View;
    }

    public static class StarAspect
    {
        public const int Count = 6;
        public static readonly string[] Names = { "金乌", "苍蓝", "赤烬", "环星", "晶核", "月潮" };
        public static readonly string[] English = { "SOLAR", "AZURE", "EMBER", "HALO", "PRISM", "TIDE" };

        public static bool HasRing(int variant) => variant == 3;
        public static bool HasCompanion(int variant) => variant == 5;

        public static Color BodyColor(int variant, StarTheme theme)
        {
            switch (variant)
            {
                case 1: return new Color(0.72f, 0.9f, 1f);
                case 2: return new Color(1f, 0.62f, 0.48f);
                case 3: return new Color(1f, 0.9f, 0.7f);
                case 4: return new Color(0.86f, 0.82f, 1f);
                case 5: return new Color(0.82f, 0.94f, 0.9f);
                default: return theme.Star;
            }
        }

        public static Color GlowColor(int variant, StarTheme theme)
        {
            switch (variant)
            {
                case 1: return theme.Teal;
                case 2: return theme.Coral;
                case 3: return theme.Gold;
                case 4: return theme.Violet;
                case 5: return theme.Teal;
                default: return theme.Gold;
            }
        }

        public static Color AccentColor(int variant, StarTheme theme)
        {
            switch (variant)
            {
                case 1: return new Color(0.8f, 0.95f, 1f);
                case 2: return new Color(1f, 0.78f, 0.55f);
                case 3: return theme.Teal;
                case 4: return new Color(0.95f, 0.92f, 1f);
                case 5: return theme.Cream;
                default: return theme.GoldLight;
            }
        }
    }

    public sealed class GameResult
    {
        public int Round;
        public bool Escaped;
        public string Reason;
        public float Time;
        public int Hits;
        public float Mass;
        public float MaxRadius;
        public float Depth;
        public int Seed;
        public int TrailScore;
    }
}
