using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
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

            Texture2D texture = ModContent.Request<Texture2D>("Terraria/Images/Extra_98").Value;

            Vector2 eyeCenter = GetEyeCenter(drawInfo, player);
            Vector2 drawPosition = eyeCenter - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.96f + 0.16f * (float)System.Math.Sin(time * 8f);
            float flash = 0.55f + 0.45f * (float)System.Math.Sin(time * 18f + player.whoAmI * 0.9f);
            float scale = (0.4f + 0.1f * intensity) * pulse;
            SpriteEffects effects = player.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color glowColor = Color.Lerp(new Color(110, 220, 255, 0), new Color(190, 255, 255, 0), flash) * (0.95f * intensity);
            Color coreColor = Color.Lerp(new Color(170, 245, 255, 0), Color.White, 0.45f + flash * 0.35f) * (1.05f * intensity);

            //float jitterRadiusPrimary = 4f + 6f * intensity;
            //float jitterRadiusSecondary = 3f + 5f * intensity;
            //Vector2 jitterPrimary = new Vector2(
            //    (float)System.Math.Sin(time * 21f + player.whoAmI * 0.7f),
            //    (float)System.Math.Cos(time * 17f + player.whoAmI * 0.43f)) * jitterRadiusPrimary;
            //Vector2 jitterSecondary = new Vector2(
            //    (float)System.Math.Cos(time * 19f + player.whoAmI * 0.61f),
            //    (float)System.Math.Sin(time * 23f + player.whoAmI * 0.35f)) * jitterRadiusSecondary;

            float baseRotation = player.velocity.X * 0.01f;
            DrawGlowPair(drawInfo, texture, drawPosition, origin, glowColor, coreColor, baseRotation, scale, effects);
            DrawGlowPair(drawInfo, texture, drawPosition, origin, glowColor * 0.85f, coreColor * 0.9f, baseRotation + MathHelper.PiOver2, scale * 0.95f, effects);
        
        }

        private static void DrawGlowPair(PlayerDrawSet drawInfo, Texture2D texture, Vector2 drawPosition, Vector2 origin, Color glowColor, Color coreColor, float rotation, float scale, SpriteEffects effects)
        {
            DrawData backGlow = new DrawData(
                texture,
                drawPosition,
                null,
                glowColor,
                rotation,
                origin,
                scale * 1.42f,
                effects,
                0);
            backGlow.shader = drawInfo.cHead;
            drawInfo.DrawDataCache.Add(backGlow);

            DrawData coreGlow = new DrawData(
                texture,
                drawPosition,
                null,
                coreColor,
                rotation,
                origin,
                scale * 1.05f,
                effects,
                0);
            coreGlow.shader = drawInfo.cHead;
            drawInfo.DrawDataCache.Add(coreGlow);
        }

        private static Vector2 GetEyeCenter(PlayerDrawSet drawInfo, Player player)
        {
            Vector2 headCenter = player.headPosition + player.MountedCenter + new Vector2(
                0f,
                player.gfxOffY - 18f - 4f // ⭐这里改：整体上下（推荐先改这里）
            );

            // ⭐这里改：左右（6f是默认眼睛位置）
            float horizontalOffset = player.direction * (1f + 2f);

            // ⭐这里改：上下微调（-2f是默认）
            float offset = -16f; // ⭐只改这个数（整体偏移强度）
            float verticalOffset = (player.gravDir == 1f ? -2f : 2f) - offset;

            // ⭐一般不用动
            if (player.gravDir == -1f)
                headCenter.Y += 8f;

            return headCenter + new Vector2(horizontalOffset, verticalOffset);
        }
    }
}
