using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatModule
{
    public sealed class HeatModulePlayer : ModPlayer
    {
        public bool HeatModuleEquipped;

        public float HeatGenerationMultiplier => HeatModuleEquipped ? 1.25f : 1f;
        public float HeatDissipationMultiplier => HeatModuleEquipped ? 0.67f : 1f;

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
