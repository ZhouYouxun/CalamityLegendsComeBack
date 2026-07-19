using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.ResponsibilityPhone
{
    internal enum ResponsibilityLanguage : byte
    {
        Alarm,
        Query,
        Static,
        Confirm
    }

    internal sealed class ResponsibilityLanguageDefinition
    {
        public ResponsibilityLanguage Id { get; }
        public string Symbol { get; }
        public string[] Tokens { get; }
        public Color Color { get; }
        public Color AccentColor { get; }
        public float TotalDamageMultiplier { get; }
        public int TokenInterval { get; }
        public float ProjectileSpeed { get; }
        public int Penetration { get; }
        public int Lifetime { get; }
        public float HomingStrength { get; }
        public int ConnectionValue { get; }

        public ResponsibilityLanguageDefinition(
            ResponsibilityLanguage id,
            string symbol,
            string[] tokens,
            Color color,
            Color accentColor,
            float totalDamageMultiplier,
            int tokenInterval,
            float projectileSpeed,
            int penetration,
            int lifetime,
            float homingStrength,
            int connectionValue)
        {
            Id = id;
            Symbol = symbol;
            Tokens = tokens;
            Color = color;
            AccentColor = accentColor;
            TotalDamageMultiplier = totalDamageMultiplier;
            TokenInterval = tokenInterval;
            ProjectileSpeed = projectileSpeed;
            Penetration = penetration;
            Lifetime = lifetime;
            HomingStrength = homingStrength;
            ConnectionValue = connectionValue;
        }
    }

    /// <summary>
    /// The wheel and firing code consume this table instead of assuming there are four channels.
    /// Adding or removing a language only requires changing this registry.
    /// </summary>
    internal static class ResponsibilityLanguageRegistry
    {
        public static readonly ResponsibilityLanguageDefinition[] Definitions =
        {
            new(
                ResponsibilityLanguage.Alarm,
                "!?",
                new[] { "!", "?", "!" },
                new Color(255, 79, 56),
                new Color(255, 221, 72),
                1.15f,
                3,
                18f,
                1,
                72,
                0f,
                1),
            new(
                ResponsibilityLanguage.Query,
                "??",
                new[] { "?", "?", "?" },
                new Color(67, 176, 255),
                new Color(150, 235, 255),
                1f,
                3,
                13.5f,
                1,
                105,
                0.105f,
                1),
            new(
                ResponsibilityLanguage.Static,
                "...",
                new[] { ".", "..", "#", "..." },
                new Color(154, 166, 174),
                new Color(208, 228, 225),
                0.9f,
                3,
                10f,
                3,
                108,
                0f,
                1),
            new(
                ResponsibilityLanguage.Confirm,
                ":)",
                new[] { ":", ")", ":", ")", "OK" },
                new Color(102, 232, 103),
                new Color(194, 255, 67),
                0.9f,
                2,
                12.5f,
                1,
                58,
                0.055f,
                2)
        };

        public static int Count => Definitions.Length;

        public static ResponsibilityLanguageDefinition Get(int index)
        {
            if (Count <= 0)
                return null;

            index %= Count;
            if (index < 0)
                index += Count;
            return Definitions[index];
        }
    }
}
