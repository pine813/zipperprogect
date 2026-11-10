using UnityEngine;

namespace StarryAbyss
{
    public sealed class StarTheme
    {
        public string Id;
        public string Name;
        public Color Ink;
        public Color Deep;
        public Color Cream;
        public Color Gold;
        public Color GoldLight;
        public Color Teal;
        public Color Coral;
        public Color Violet;
        public Color Portal;
        public Color Star;
        public Color Moon;
        public Color[] Planets;
        public Color[] Meteors;
        public Color[] Stars;

        public static Color C(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }

    public static class StarPalette
    {
        public static readonly StarTheme[] Themes =
        {
            new StarTheme
            {
                Id = "starry",
                Name = "星夜",
                Ink = StarTheme.C("#030812"),
                Deep = StarTheme.C("#081d31"),
                Cream = StarTheme.C("#f8efd6"),
                Gold = StarTheme.C("#d9b36f"),
                GoldLight = StarTheme.C("#f1d79d"),
                Teal = StarTheme.C("#6ea7a1"),
                Coral = StarTheme.C("#c98368"),
                Violet = StarTheme.C("#5f6594"),
                Portal = StarTheme.C("#f8dfa2"),
                Star = StarTheme.C("#ffeec4"),
                Moon = StarTheme.C("#efd49a"),
                Planets = new[]
                {
                    StarTheme.C("#4b7f9e"), StarTheme.C("#5a9a98"), StarTheme.C("#ba8b69"),
                    StarTheme.C("#797dad"), StarTheme.C("#87a7a0")
                },
                Meteors = new[]
                {
                    StarTheme.C("#b58f67"), StarTheme.C("#8c705c"), StarTheme.C("#caa26f")
                },
                Stars = new[]
                {
                    StarTheme.C("#ffeec4"), StarTheme.C("#f5d998"), StarTheme.C("#fff6dd")
                }
            },
            new StarTheme
            {
                Id = "rebirth",
                Name = "新宇宙",
                Ink = StarTheme.C("#05040d"),
                Deep = StarTheme.C("#1a102d"),
                Cream = StarTheme.C("#fff1d8"),
                Gold = StarTheme.C("#e7be7b"),
                GoldLight = StarTheme.C("#f4ddad"),
                Teal = StarTheme.C("#7ba8a0"),
                Coral = StarTheme.C("#d68c7f"),
                Violet = StarTheme.C("#8c79a9"),
                Portal = StarTheme.C("#ffe4a6"),
                Star = StarTheme.C("#fff2c9"),
                Moon = StarTheme.C("#f2d7a0"),
                Planets = new[]
                {
                    StarTheme.C("#5c5f8e"), StarTheme.C("#619c9a"), StarTheme.C("#c08d7d"),
                    StarTheme.C("#8874a8"), StarTheme.C("#87a69c")
                },
                Meteors = new[]
                {
                    StarTheme.C("#bc8f7a"), StarTheme.C("#8f7280"), StarTheme.C("#d0a079")
                },
                Stars = new[]
                {
                    StarTheme.C("#fff1d3"), StarTheme.C("#f5cfa0"), StarTheme.C("#fff8e6")
                }
            },
            new StarTheme
            {
                Id = "verdigris",
                Name = "月潮",
                Ink = StarTheme.C("#020807"),
                Deep = StarTheme.C("#092522"),
                Cream = StarTheme.C("#fbf3da"),
                Gold = StarTheme.C("#d8b57b"),
                GoldLight = StarTheme.C("#f1dda8"),
                Teal = StarTheme.C("#7bae99"),
                Coral = StarTheme.C("#cb8b6e"),
                Violet = StarTheme.C("#6a808c"),
                Portal = StarTheme.C("#f9e7af"),
                Star = StarTheme.C("#fff4cf"),
                Moon = StarTheme.C("#f2ddb0"),
                Planets = new[]
                {
                    StarTheme.C("#4d8b86"), StarTheme.C("#75a88f"), StarTheme.C("#bd8b6a"),
                    StarTheme.C("#7f92a6"), StarTheme.C("#9cbaa6")
                },
                Meteors = new[]
                {
                    StarTheme.C("#b69469"), StarTheme.C("#7e7b62"), StarTheme.C("#c6a777")
                },
                Stars = new[]
                {
                    StarTheme.C("#fff4cf"), StarTheme.C("#f3dca8"), StarTheme.C("#fbffe9")
                }
            },
            new StarTheme
            {
                Id = "amber",
                Name = "琥珀",
                Ink = StarTheme.C("#080604"),
                Deep = StarTheme.C("#28180e"),
                Cream = StarTheme.C("#faedce"),
                Gold = StarTheme.C("#dba65e"),
                GoldLight = StarTheme.C("#f3d396"),
                Teal = StarTheme.C("#7da192"),
                Coral = StarTheme.C("#cf8164"),
                Violet = StarTheme.C("#746c81"),
                Portal = StarTheme.C("#fae0a2"),
                Star = StarTheme.C("#ffedc0"),
                Moon = StarTheme.C("#f3d49b"),
                Planets = new[]
                {
                    StarTheme.C("#527b88"), StarTheme.C("#6b9b8e"), StarTheme.C("#c18c64"),
                    StarTheme.C("#877a96"), StarTheme.C("#93a892")
                },
                Meteors = new[]
                {
                    StarTheme.C("#b98f61"), StarTheme.C("#8c755b"), StarTheme.C("#cda470")
                },
                Stars = new[]
                {
                    StarTheme.C("#ffedc0"), StarTheme.C("#f2cf92"), StarTheme.C("#fff7df")
                }
            }
        };

        public static StarTheme Get(int round)
        {
            var index = ((round - 1) % Themes.Length + Themes.Length) % Themes.Length;
            return Themes[index];
        }

        public static string[] BackgroundResources =
        {
            "Art/Background0",
            "Art/Background1",
            "Art/Background2",
            "Art/Background3"
        };
    }
}
