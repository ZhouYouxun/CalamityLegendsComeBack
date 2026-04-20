using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI
{
    internal sealed class BFRightHoldChargeBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            BFRightUIPlayer rightUIPlayer = drawInfo.drawPlayer.GetModPlayer<BFRightUIPlayer>();
            return rightUIPlayer.ShowRightHoldBar;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            BFRightUIPlayer rightUIPlayer = player.GetModPlayer<BFRightUIPlayer>();
            if (!rightUIPlayer.ShowRightHoldBar)
                return;

            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            Vector2 drawPos = player.Center - Main.screenPosition + new Vector2(0f, player.gfxOffY - 58f) - barBackground.Size() * 0.6f;
            Rectangle frameCrop = new Rectangle(0, 0, (int)(barForeground.Width * rightUIPlayer.RightHoldProgress), barForeground.Height);
            Color barColor = Color.Lerp(new Color(60, 150, 80), new Color(165, 255, 170), rightUIPlayer.RightHoldProgress);
            const float drawScale = 1.2f;

            drawInfo.DrawDataCache.Add(new DrawData(
                barBackground,
                drawPos,
                null,
                barColor * 0.65f,
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
