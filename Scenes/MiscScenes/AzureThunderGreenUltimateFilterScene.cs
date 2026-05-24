using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Scenes.MiscScenes
{
    internal sealed class AzureThunderGreenUltimateFilterScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;

        public override bool IsSceneEffectActive(Player player)
        {
            return player.GetModPlayer<AzureThunderPlayer>().GreenUltimateFilterOpacity > 0f;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            float opacity = isActive ? player.GetModPlayer<AzureThunderPlayer>().GreenUltimateFilterOpacity : 0f;
            Filter filter = Filters.Scene[CalamityLegendsComeBack.AzureThunderGreenUltimateFilterKey];
            if (filter != null)
                filter.GetShader().UseOpacity(opacity);

            player.ManageSpecialBiomeVisuals(CalamityLegendsComeBack.AzureThunderGreenUltimateFilterKey, isActive);
        }
    }
}
