using CalamityLegendsComeBack.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle;

public sealed class AMRRangedStealthBarLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
        drawInfo.shadow == 0f && drawInfo.drawPlayer.whoAmI == Main.myPlayer &&
        drawInfo.drawPlayer.GetModPlayer<AMRRangedStealthPlayer>().ShowBar;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        var charge = player.GetModPlayer<AMRRangedStealthPlayer>();
        Vector2 center = player.Center - Main.screenPosition + new Vector2(0f, player.gfxOffY - 72f);
        BoundedHeadBarRenderer.AddToPlayerDrawCache(drawInfo.DrawDataCache, center,
            charge.Completion, new Color(12, 20, 28, 224),
            new Color(80, 165, 210), new Color(220, 245, 255),
            0.92f, 0f, Main.GlobalTimeWrappedHourly);
    }
}
