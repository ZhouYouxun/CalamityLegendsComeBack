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
            /*float  FinalHeatDamage = HeatRedirectModuleEquipped ? 1f + System.Math.Max(0, heatStage) * 0.25f : 1f;
            if (FinalHeatDamage < 0.5f)
            {
                FinalHeatDamage = FinalHeatDamage;
            }
            else if (FinalHeatDamage >= 0.5f)
            {
                FinalHeatDamage = 0.5f;
            }

            return FinalHeatDamage;*/
            return HeatRedirectModuleEquipped ? 1f + System.Math.Max(0, heatStage) * 0.25f : 1f;
        }

        public override void ResetEffects()
        {
            HeatRedirectModuleEquipped = false;
        }
    }
}
