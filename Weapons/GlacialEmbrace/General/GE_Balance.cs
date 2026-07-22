using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.General
{
    internal static class GE_Balance
    {
        private const string SourceFile = "Weapons/GlacialEmbrace/General/GE_Balance.cs";

        // 大招（极寒钻）伤害倍率：肉后/月后/神后三档
        // 大招伤害 = 当前左键伤害 × 本档倍率
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        public static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);
    }
}
