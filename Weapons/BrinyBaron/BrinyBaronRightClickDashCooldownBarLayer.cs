using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal sealed class BrinyBaronRightClickDashCooldownBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().IsCoolingDown;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            BrinyBaronRightClickDashCooldownPlayer dashCooldown = player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();
            if (!dashCooldown.IsCoolingDown)
                return;

            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            float progress = dashCooldown.CooldownCompletion;
            Rectangle frameCrop = new(0, 0, (int)(barForeground.Width * progress), barForeground.Height);
            Vector2 drawPos = player.Center - Main.screenPosition + new Vector2(0f, player.gfxOffY - 56f) - barBackground.Size() / 1.5f;
            Color barColor = new(92, 210, 255);
            const float drawScale = 1.5f;

            drawInfo.DrawDataCache.Add(new DrawData(
                barBackground,
                drawPos,
                null,
                barColor * 0.55f,
                0f,
                Vector2.Zero,
                drawScale,
                SpriteEffects.None,
                0));

            if (frameCrop.Width <= 0)
                return;

            drawInfo.DrawDataCache.Add(new DrawData(
                barForeground,
                drawPos,
                frameCrop,
                barColor,
                0f,
                Vector2.Zero,
                drawScale,
                SpriteEffects.None,
                0));
        }
    }
}
