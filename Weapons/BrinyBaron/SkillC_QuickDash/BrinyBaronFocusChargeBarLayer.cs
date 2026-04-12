using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash
{
    internal class BrinyBaronFocusChargeBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            BrinyBaronFocusModePlayer focusPlayer = drawInfo.drawPlayer.GetModPlayer<BrinyBaronFocusModePlayer>();
            return focusPlayer.FocusChargeBarOpacity > 0f;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            BrinyBaronFocusModePlayer focusPlayer = player.GetModPlayer<BrinyBaronFocusModePlayer>();
            float opacity = focusPlayer.FocusChargeBarOpacity;
            if (opacity <= 0f)
                return;

            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            Vector2 drawPos = player.Center - Main.screenPosition + new Vector2(0f, player.gfxOffY - 56f) - barBackground.Size() / 1.5f;
            Rectangle frameCrop = new Rectangle(0, 0, (int)(barForeground.Width * focusPlayer.FocusChargeBarProgress), barForeground.Height);
            Color barColor = Color.Lerp(new Color(80, 170, 255), new Color(185, 245, 255), focusPlayer.FocusChargeBarProgress);
            const float drawScale = 1.5f;

            DrawData backgroundDraw = new DrawData(
                barBackground,
                drawPos,
                null,
                barColor * (opacity * 0.7f),
                0f,
                Vector2.Zero,
                drawScale,
                SpriteEffects.None,
                0);
            drawInfo.DrawDataCache.Add(backgroundDraw);

            if (frameCrop.Width <= 0)
                return;

            DrawData foregroundDraw = new DrawData(
                barForeground,
                drawPos,
                frameCrop,
                barColor * opacity,
                0f,
                Vector2.Zero,
                drawScale,
                SpriteEffects.None,
                0);
            drawInfo.DrawDataCache.Add(foregroundDraw);
        }
    }
}
