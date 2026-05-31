using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatRedirectModule
{
    public sealed class HeatRedirectModulePlayer : ModPlayer
    {
        public bool HeatRedirectModuleEquipped;

        public float HeatGenerationMultiplier => HeatRedirectModuleEquipped ? 1.2f : 1f;
        public float HeatDissipationMultiplier => HeatRedirectModuleEquipped ? 0.67f : 1f;

        public float GetHeatDamageMultiplier(int heatStage)
        {
            return HeatRedirectModuleEquipped ? 1f + System.Math.Max(0, heatStage) * 0.33f : 1f;
        }

        public override void ResetEffects()
        {
            HeatRedirectModuleEquipped = false;
        }
    }
}
