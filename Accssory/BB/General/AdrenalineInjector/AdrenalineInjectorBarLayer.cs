using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    internal sealed class AdrenalineInjectorBarLayer : PlayerDrawLayer
    {
        private const int SegmentCount = 18;
        private const int SegmentWidth = 4;
        private const int SegmentGap = 2;
        private const int BarHeight = 5;

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Leggings);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            return player.active && !player.dead && player.GetModPlayer<BBAccessoryPlayer>().AdrenalineInjectorEquipped;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            BBAccessoryPlayer bbPlayer = player.GetModPlayer<BBAccessoryPlayer>();
            if (!bbPlayer.AdrenalineInjectorEquipped)
                return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int totalWidth = SegmentCount * SegmentWidth + (SegmentCount - 1) * SegmentGap;
            Vector2 basePosition = player.Bottom - Main.screenPosition + new Vector2(-totalWidth * 0.5f, player.gfxOffY + 9f);
            float time = Main.GlobalTimeWrappedHourly;
            float stackCompletion = MathHelper.Clamp(bbPlayer.AdrenalineStackCompletion, 0f, 1f);
            float timerCompletion = MathHelper.Clamp(bbPlayer.AdrenalineTimerCompletion, 0f, 1f);

            Rectangle frame = new((int)basePosition.X - 4, (int)basePosition.Y - 4, totalWidth + 8, BarHeight + 8);
            DrawRect(ref drawInfo, pixel, frame, new Color(3, 12, 32, 170));
            DrawRect(ref drawInfo, pixel, new Rectangle(frame.X, frame.Y, frame.Width, 1), new Color(38, 190, 255, 170));
            DrawRect(ref drawInfo, pixel, new Rectangle(frame.X, frame.Bottom - 1, frame.Width, 1), new Color(16, 80, 170, 150));
            DrawRect(ref drawInfo, pixel, new Rectangle(frame.X, frame.Y, 1, frame.Height), new Color(38, 190, 255, 150));
            DrawRect(ref drawInfo, pixel, new Rectangle(frame.Right - 1, frame.Y, 1, frame.Height), new Color(38, 190, 255, 150));

            for (int i = 0; i < SegmentCount; i++)
            {
                int x = (int)basePosition.X + i * (SegmentWidth + SegmentGap);
                int y = (int)basePosition.Y;
                float segmentProgress = MathHelper.Clamp(stackCompletion * SegmentCount - i, 0f, 1f);
                float wave = 0.5f + 0.5f * (float)Math.Sin(time * 6.5f + i * 0.72f);
                Color emptyColor = new(8, 38, 82, 150);
                Color filledColor = Color.Lerp(new Color(28, 142, 255), new Color(128, 246, 255), wave * 0.45f + timerCompletion * 0.25f);
                Color color = Color.Lerp(emptyColor, filledColor, segmentProgress);

                DrawRect(ref drawInfo, pixel, new Rectangle(x, y, SegmentWidth, BarHeight), color);

                if (segmentProgress > 0f && (i + (int)(time * 18f)) % 4 == 0)
                    DrawRect(ref drawInfo, pixel, new Rectangle(x, y - 2, SegmentWidth, 1), new Color(178, 250, 255, 130) * segmentProgress);
            }

            int scanX = frame.X + (int)((time * 44f) % frame.Width);
            DrawRect(ref drawInfo, pixel, new Rectangle(scanX, frame.Y + 1, 1, frame.Height - 2), new Color(154, 248, 255, 120));
        }

        private static void DrawRect(ref PlayerDrawSet drawInfo, Texture2D pixel, Rectangle rectangle, Color color)
        {
            drawInfo.DrawDataCache.Add(new DrawData(
                pixel,
                new Vector2(rectangle.X, rectangle.Y),
                new Rectangle(0, 0, 1, 1),
                color,
                0f,
                Vector2.Zero,
                new Vector2(rectangle.Width, rectangle.Height),
                SpriteEffects.None,
                0));
        }
    }
}
