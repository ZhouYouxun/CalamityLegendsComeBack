using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash
{
    internal class BrinyBaronFocusEyesLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.GetModPlayer<BrinyBaronFocusModePlayer>().FocusVisualIntensity > 0f;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            BrinyBaronFocusModePlayer focusPlayer = player.GetModPlayer<BrinyBaronFocusModePlayer>();
            float intensity = focusPlayer.FocusVisualIntensity;
            if (intensity <= 0f)
                return;

            Texture2D texture = ModContent.Request<Texture2D>("Terraria/Images/Extra_89").Value;

            Vector2 eyeCenter = GetEyeCenter(drawInfo, player);
            Vector2 drawPosition = eyeCenter - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float pulse = 0.92f + 0.12f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            float scale = (0.34f + 0.08f * intensity) * pulse;
            SpriteEffects effects = player.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color glowColor = new Color(110, 220, 255, 0) * (0.55f * intensity);
            float rotation = player.velocity.X * 0.01f;

            DrawData backGlow = new DrawData(
                texture,
                drawPosition,
                null,
                glowColor * 0.75f,
                rotation,
                origin,
                scale * 1.3f,
                effects,
                0);
            backGlow.shader = drawInfo.cHead;
            drawInfo.DrawDataCache.Add(backGlow);

            DrawData coreGlow = new DrawData(
                texture,
                drawPosition,
                null,
                Color.White * (0.7f * intensity),
                rotation,
                origin,
                scale,
                effects,
                0);
            coreGlow.shader = drawInfo.cHead;
            drawInfo.DrawDataCache.Add(coreGlow);
        }

        private static Vector2 GetEyeCenter(PlayerDrawSet drawInfo, Player player)
        {
            Vector2 headCenter = player.headPosition + player.MountedCenter + new Vector2(0f, player.gfxOffY - 18f);
            float horizontalOffset = player.direction * 6f;
            float verticalOffset = player.gravDir == 1f ? -2f : 2f;

            if (player.gravDir == -1f)
                headCenter.Y += 8f;

            return headCenter + new Vector2(horizontalOffset, verticalOffset);
        }
    }
}
