using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal static class SeasSearingPalette
    {
        public static readonly Color AbyssBlack       = new(4, 9, 18);
        public static readonly Color DeepBlue         = new(20, 72, 122);
        public static readonly Color PressureBlue     = new(34, 120, 185);
        public static readonly Color RadioactiveCyan  = new(88, 255, 218);
        public static readonly Color ToxicGreen       = new(68, 210, 104);
        public static readonly Color BiohazardLime    = new(140, 255, 60);
        public static readonly Color FalloutAsh       = new(90, 112, 120);
        public static readonly Color WarningOrange    = new(255, 132, 48);

        public static Color PollutionColor(float completion)
        {
            if (completion < 0.45f)
                return Color.Lerp(DeepBlue, RadioactiveCyan, completion / 0.45f);
            return Color.Lerp(RadioactiveCyan, ToxicGreen, (completion - 0.45f) / 0.55f);
        }

        public static Color GradeColor(int grade) => grade switch
        {
            1 => PressureBlue,
            2 => RadioactiveCyan,
            3 => ToxicGreen,
            4 => BiohazardLime,
            5 => new Color(210, 255, 140),
            _ => DeepBlue
        };
    }
}
