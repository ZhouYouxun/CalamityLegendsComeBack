using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    internal sealed class SHPBowChargeBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.GetModPlayer<SHPBowPlayer>().ShowRightHoldBar;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            SHPBowPlayer bowPlayer = player.GetModPlayer<SHPBowPlayer>();
            if (!bowPlayer.ShowRightHoldBar)
                return;

            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            Vector2 drawPos = player.Center - Main.screenPosition + new Vector2(0f, player.gfxOffY - 62f) - barBackground.Size() * 0.6f;
            float displayOpacity = bowPlayer.ChargeBarOpacity > 0f ? bowPlayer.ChargeBarOpacity : bowPlayer.RightHoldProgress;
            float progress = bowPlayer.ChargeBarOpacity > 0f ? bowPlayer.ChargeBarProgress : bowPlayer.RightHoldProgress;
            Rectangle frameCrop = new(0, 0, (int)(barForeground.Width * progress), barForeground.Height);
            Color modeColor = SHPBowModeHelpers.SequenceColor(bowPlayer.PackedSequence, 0.15f);
            Color accentColor = SHPBowModeHelpers.SequenceColor(bowPlayer.PackedSequence, 1f);
            Color barColor = Color.Lerp(modeColor, accentColor, progress) * displayOpacity;
            float drawScale = 1.2f;

            drawInfo.DrawDataCache.Add(new DrawData(
                barBackground,
                drawPos,
                null,
                modeColor * (0.34f * displayOpacity),
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
