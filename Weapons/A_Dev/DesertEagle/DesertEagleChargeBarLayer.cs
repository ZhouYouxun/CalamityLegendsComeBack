using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEagleChargeBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.GetModPlayer<DesertEaglePlayer>().ChargeBarOpacity > 0f;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();
            if (eaglePlayer.ChargeBarOpacity <= 0f)
                return;

            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            Vector2 drawPosition = player.Center - Main.screenPosition + new Vector2(0f, player.gfxOffY - 60f) - barBackground.Size() / 1.5f;
            Rectangle frameCrop = new Rectangle(0, 0, (int)(barForeground.Width * eaglePlayer.ChargeBarProgress), barForeground.Height);
            Color barColor = Color.Lerp(new Color(122, 136, 156), new Color(236, 241, 248), eaglePlayer.ChargeBarProgress);
            const float drawScale = 1.5f;

            drawInfo.DrawDataCache.Add(new DrawData(
                barBackground,
                drawPosition,
                null,
                barColor * (eaglePlayer.ChargeBarOpacity * 0.65f),
                0f,
                Vector2.Zero,
                drawScale,
                SpriteEffects.None,
                0));

            if (frameCrop.Width <= 0)
                return;

            drawInfo.DrawDataCache.Add(new DrawData(
                barForeground,
                drawPosition,
                frameCrop,
                barColor * eaglePlayer.ChargeBarOpacity,
                0f,
                Vector2.Zero,
                drawScale,
                SpriteEffects.None,
                0));
        }
    }
}
