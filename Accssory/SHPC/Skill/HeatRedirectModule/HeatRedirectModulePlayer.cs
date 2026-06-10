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
            if (!HeatRedirectModuleEquipped)
                return 1f;

            int cappedStage = System.Math.Min(2, System.Math.Max(0, heatStage));
            return 1f + cappedStage * 0.25f;
        }

        public override void ResetEffects()
        {
            HeatRedirectModuleEquipped = false;
        }
    }
}
