using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatModule
{
    public sealed class HeatModulePlayer : ModPlayer
    {
        public const int HeatFillTimeWithModule = 3 * 60;

        public bool HeatModuleEquipped;

        public float HeatGenerationMultiplier => 1f;
        public float HeatDissipationMultiplier => 1f;

        public int GetHeatFillTime(int defaultFillTime)
        {
            return HeatModuleEquipped ? HeatFillTimeWithModule : defaultFillTime;
        }

        public float GetHeatDamageMultiplier(int heatStage)
        {
            if (!HeatModuleEquipped)
                return 1f;

            int cappedStage = System.Math.Min(5, System.Math.Max(0, heatStage));
            return System.MathF.Pow(1.075f / 1.05f, cappedStage);
        }

        public override void ResetEffects()
        {
            HeatModuleEquipped = false;
        }
    }
}
