using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    // InvisibleProj prevents Terraria's normal held-item pass from rendering anything.
    // This layer is the explicit held-use counterpart to the inventory/world draw hooks.
    internal sealed class HDMCHeldCoreLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            return player.active && !player.dead && player.itemAnimation > 0 &&
                player.HeldItem.type == ModContent.ItemType<HDMCUncompiledCore>();
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            HDMCUncompiledCore.AddHeldDrawData(ref drawInfo, drawInfo.drawPlayer);
        }
    }
}
