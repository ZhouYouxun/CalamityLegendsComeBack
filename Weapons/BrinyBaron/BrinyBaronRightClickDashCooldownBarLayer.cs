using CalamityLegendsComeBack.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    // Uses the shared hard-bounded short-bar renderer. Unlike the old compressed
    // shield frame, it has no L-brackets, crosses, or free line geometry.
    internal sealed class BrinyBaronRightClickDashCooldownBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
            drawInfo.drawPlayer.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().IsCoolingDown;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            BrinyBaronRightClickDashCooldownPlayer cooldown = player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();
            if (!cooldown.IsCoolingDown)
                return;

            float progress = MathHelper.Clamp(cooldown.CooldownCompletion, 0f, 1f);
            Vector2 center = player.Center - Main.screenPosition + new Vector2(0f, player.gfxOffY - 58f);
            BoundedHeadBarRenderer.AddToPlayerDrawCache(
                drawInfo.DrawDataCache,
                center,
                progress,
                new Color(5, 22, 42, 224),
                new Color(48, 155, 224),
                new Color(192, 246, 255),
                0.92f,
                0f,
                Main.GlobalTimeWrappedHourly + player.whoAmI * 0.17f);
        }
    }
}
