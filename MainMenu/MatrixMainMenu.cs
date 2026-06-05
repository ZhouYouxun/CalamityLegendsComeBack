using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.MainMenu
{
    public sealed class MatrixMainMenu : ModMenu
    {
        private static readonly string[] Glyphs =
        [
            "0", "1", "I", "O", "X", "Z", "7", "9",
            "CL", "CB", "LC", "BK", "SYS", "RUN", "NET", "ERR",
            "OK", ">>", "::", "[]", "{}", "//", "##", "01"
        ];

        private readonly List<CodeStream> codeStreams = [];

        private Asset<Texture2D> blankTexture;
        private int nextStreamId;
        private int streamSpawnTimer;
        private float logoGlitchTime;

        public override string DisplayName => "CLCB Matrix Interface";

        public override Asset<Texture2D> Logo => blankTexture ??= ModContent.Request<Texture2D>(MatrixMenuAssets.BlankPixelFullPath);

        public override Asset<Texture2D> SunTexture => Logo;

        public override Asset<Texture2D> MoonTexture => Logo;

        public override int Music => MusicID.Title;

        public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<MatrixNullSurfaceBackground>();

        public override void Load()
        {
            if (Main.dedServ)
                return;

            blankTexture = ModContent.Request<Texture2D>(MatrixMenuAssets.BlankPixelFullPath, AssetRequestMode.ImmediateLoad);
        }

        public override void Unload()
        {
            blankTexture = null;
            codeStreams.Clear();
        }

        public override void OnSelected()
        {
            SeedInitialStreams();
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.52f, Pitch = 0.12f });
        }

        public override void OnDeselected()
        {
            codeStreams.Clear();
        }

        public override void Update(bool isOnTitleScreen)
        {
            Main.time = 27000.0;
            Main.dayTime = true;

            UpdateStreams();

            if (logoGlitchTime > 0f)
                logoGlitchTime--;
            else if (Main.rand.NextBool(150))
                logoGlitchTime = Main.rand.Next(5, 12);
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            Main.time = 27000.0;
            Main.dayTime = true;
            drawColor = Color.White;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            DrawBackdrop(spriteBatch);
            DrawCodeSpace(spriteBatch);
            DrawMatrixRain(spriteBatch);
            DrawInterfaceIndicators(spriteBatch);
            DrawProgramLogo(spriteBatch, logoRotation);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            return false;
        }

        private void SeedInitialStreams()
        {
            codeStreams.Clear();
            nextStreamId = 0;

            int initialCount = Math.Clamp(Main.screenWidth / 17, 56, 150);
            for (int i = 0; i < initialCount; i++)
            {
                CodeStream stream = CreateStream(true);
                stream.HeadY = Main.rand.NextFloat(-Main.screenHeight * 0.15f, Main.screenHeight * 1.15f);
                codeStreams.Add(stream);
            }
        }

        private void UpdateStreams()
        {
            if (codeStreams.Count == 0)
                SeedInitialStreams();

            for (int i = codeStreams.Count - 1; i >= 0; i--)
            {
                CodeStream stream = codeStreams[i];
                stream.Time++;
                stream.HeadY += stream.Speed;
                stream.X += MathF.Sin((stream.Time + stream.Phase) * 0.018f) * stream.Drift;

                float lineHeight = FontAssets.MouseText.Value.LineSpacing * stream.Scale * 0.86f;
                if (stream.HeadY - stream.Length * lineHeight > Main.screenHeight + 140f || stream.Time >= stream.Lifetime)
                    codeStreams.RemoveAt(i);
            }

            int desiredCount = Math.Clamp(Main.screenWidth / 12, 72, 190);
            streamSpawnTimer++;
            int spawnInterval = Main.screenWidth >= 1600 ? 1 : 2;
            if (streamSpawnTimer < spawnInterval)
                return;

            streamSpawnTimer = 0;
            int spawnAttempts = Main.screenWidth >= 1920 ? 3 : 2;
            for (int i = 0; i < spawnAttempts && codeStreams.Count < desiredCount; i++)
            {
                if (Main.rand.NextBool(2))
                    codeStreams.Add(CreateStream(false));
            }
        }

        private CodeStream CreateStream(bool initial)
        {
            float x = ChooseStreamX();
            float scale = Main.rand.NextFloat(0.52f, 0.86f);
            bool prominent = Main.rand.NextBool(8);

            return new CodeStream
            {
                Id = nextStreamId++,
                X = x,
                HeadY = initial ? Main.rand.NextFloat(-Main.screenHeight, Main.screenHeight) : Main.rand.NextFloat(-220f, -24f),
                Speed = Main.rand.NextFloat(2.6f, 7.8f) * (prominent ? 1.25f : 1f),
                Length = Main.rand.Next(prominent ? 18 : 10, prominent ? 34 : 24),
                Scale = scale,
                Opacity = Main.rand.NextFloat(prominent ? 0.46f : 0.18f, prominent ? 0.92f : 0.54f),
                Drift = Main.rand.NextFloat(-0.08f, 0.08f),
                Phase = Main.rand.NextFloat(0f, MathHelper.TwoPi),
                Lifetime = Main.rand.Next(300, 620),
                GlyphSeed = Main.rand.Next(100000),
                Prominent = prominent
            };
        }

        private static float ChooseStreamX()
        {
            float roll = Main.rand.NextFloat();
            float leftEdge = Main.screenWidth * 0.28f;
            float rightEdge = Main.screenWidth * 0.72f;

            if (roll < 0.43f)
                return Main.rand.NextFloat(-16f, leftEdge);
            if (roll < 0.86f)
                return Main.rand.NextFloat(rightEdge, Main.screenWidth + 16f);

            return Main.rand.NextFloat(leftEdge, rightEdge);
        }

        private static void DrawBackdrop(SpriteBatch spriteBatch)
        {
            Rectangle screen = new(0, 0, Main.screenWidth, Main.screenHeight);
            DrawRectangle(spriteBatch, screen, new Color(1, 4, 3));

            Color gridColor = new(20, 118, 76, 34);
            for (int x = -1; x < Main.screenWidth; x += 48)
                DrawRectangle(spriteBatch, new Rectangle(x, 0, 1, Main.screenHeight), gridColor);

            for (int y = 0; y < Main.screenHeight; y += 42)
                DrawRectangle(spriteBatch, new Rectangle(0, y, Main.screenWidth, 1), gridColor * 0.72f);

            for (int y = 0; y < Main.screenHeight; y += 4)
                DrawRectangle(spriteBatch, new Rectangle(0, y, Main.screenWidth, 1), new Color(0, 0, 0, 92));

            float scanY = Main.GlobalTimeWrappedHourly * 76f % Math.Max(1, Main.screenHeight);
            DrawRectangle(spriteBatch, new Rectangle(0, (int)scanY, Main.screenWidth, 2), new Color(72, 255, 156, 42));
            DrawRectangle(spriteBatch, new Rectangle(0, (int)scanY + 5, Main.screenWidth, 1), new Color(72, 255, 156, 20));

            DrawVignette(spriteBatch);
        }

        private static void DrawCodeSpace(SpriteBatch spriteBatch)
        {
            float time = Main.GlobalTimeWrappedHourly;
            Color panelLine = new(42, 255, 144, 52);

            int midY = Main.screenHeight / 2;
            DrawRectangle(spriteBatch, new Rectangle(0, midY - 1, Main.screenWidth, 1), panelLine * 0.38f);

            for (int i = 0; i < 10; i++)
            {
                float pulse = (MathF.Sin(time * 1.7f + i * 0.8f) + 1f) * 0.5f;
                int width = (int)MathHelper.Lerp(34f, 140f, pulse);
                int y = 62 + i * 47;
                DrawRectangle(spriteBatch, new Rectangle(24, y, width, 1), panelLine * (0.22f + pulse * 0.38f));
                DrawRectangle(spriteBatch, new Rectangle(Main.screenWidth - 24 - width, y + 18, width, 1), panelLine * (0.22f + pulse * 0.38f));
            }
        }

        private void DrawMatrixRain(SpriteBatch spriteBatch)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            foreach (CodeStream stream in codeStreams)
            {
                float lineHeight = font.LineSpacing * stream.Scale * 0.86f;
                for (int row = 0; row < stream.Length; row++)
                {
                    float y = stream.HeadY - row * lineHeight;
                    if (y < -lineHeight || y > Main.screenHeight + lineHeight)
                        continue;

                    float trailCompletion = 1f - row / (float)Math.Max(1, stream.Length - 1);
                    float alpha = stream.Opacity * trailCompletion * trailCompletion;
                    if (alpha <= 0.015f)
                        continue;

                    string glyph = PickGlyph(stream, row);
                    Color color = row == 0
                        ? Color.Lerp(new Color(160, 255, 190), Color.White, stream.Prominent ? 0.42f : 0.18f) * MathHelper.Clamp(alpha + 0.25f, 0f, 1f)
                        : Color.Lerp(new Color(16, 94, 46), new Color(58, 255, 130), trailCompletion) * alpha;

                    Vector2 position = new(stream.X, y);
                    ChatManager.DrawColorCodedString(spriteBatch, font, glyph, position, color, 0f, Vector2.Zero, Vector2.One * stream.Scale);
                }
            }
        }

        private static string PickGlyph(CodeStream stream, int row)
        {
            int animationStep = stream.Time / (stream.Prominent ? 3 : 5);
            int index = stream.GlyphSeed + row * 17 + animationStep * 31 + stream.Id * 13;
            index %= Glyphs.Length;
            if (index < 0)
                index += Glyphs.Length;

            return Glyphs[index];
        }

        private static void DrawInterfaceIndicators(SpriteBatch spriteBatch)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float pulse = (MathF.Sin(Main.GlobalTimeWrappedHourly * 5.8f) + 1f) * 0.5f;
            Color bright = new Color(94, 255, 166) * (0.56f + pulse * 0.32f);
            Color dim = new Color(34, 156, 96) * 0.48f;

            int margin = 28;
            DrawCorner(spriteBatch, new Vector2(margin, margin), 1, 1, bright);
            DrawCorner(spriteBatch, new Vector2(Main.screenWidth - margin, margin), -1, 1, bright);
            DrawCorner(spriteBatch, new Vector2(margin, Main.screenHeight - margin), 1, -1, bright);
            DrawCorner(spriteBatch, new Vector2(Main.screenWidth - margin, Main.screenHeight - margin), -1, -1, bright);

            DrawRectangle(spriteBatch, new Rectangle(margin + 4, margin + 46, 5, 5), bright);
            DrawRectangle(spriteBatch, new Rectangle(margin + 14, margin + 46, 48, 2), dim);
            DrawRectangle(spriteBatch, new Rectangle(Main.screenWidth - margin - 62, margin + 46, 48, 2), dim);
            DrawRectangle(spriteBatch, new Rectangle(Main.screenWidth - margin - 9, margin + 46, 5, 5), bright);

            string upperStatus = $"SYS: CLCB_MAINFRAME // LINK {Main.screenWidth}x{Main.screenHeight}";
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, upperStatus, new Vector2(margin + 4, margin + 58), bright, 0f, Vector2.Zero, Vector2.One * 0.62f);

            string lowerStatus = "ACCESS CHANNEL OPEN  >>  FALLING CODE FIELD ACTIVE";
            Vector2 lowerSize = font.MeasureString(lowerStatus) * 0.62f;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, lowerStatus, new Vector2(Main.screenWidth - margin - lowerSize.X, Main.screenHeight - margin - 70), bright, 0f, Vector2.Zero, Vector2.One * 0.62f);

            if (pulse > 0.48f)
            {
                int cursorWidth = 11;
                Vector2 cursorPosition = new(margin + 4 + font.MeasureString(upperStatus).X * 0.62f + 8f, margin + 62);
                DrawRectangle(spriteBatch, new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, cursorWidth, 16), bright * 0.78f);
            }

            DrawReticle(spriteBatch, new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.58f), 44f + pulse * 8f, bright * 0.58f);
        }

        private void DrawProgramLogo(SpriteBatch spriteBatch, float logoRotation)
        {
            DynamicSpriteFont titleFont = FontAssets.DeathText.Value;
            DynamicSpriteFont smallFont = FontAssets.MouseText.Value;
            string title = "CALAMITY LEGENDS COME BACK";
            string subtitle = "MATRIX MENU INTERFACE";

            Vector2 titleSize = titleFont.MeasureString(title);
            float targetWidth = Math.Min(Main.screenWidth * 0.78f, 980f);
            float titleScale = MathHelper.Clamp(targetWidth / Math.Max(1f, titleSize.X), 0.54f, 1.05f);
            Vector2 titleCenter = new(Main.screenWidth * 0.5f, Math.Max(86f, Main.screenHeight * 0.16f));

            float floatOffset = MathF.Sin(Main.GlobalTimeWrappedHourly * 1.2f) * 2.5f;
            Vector2 drawCenter = titleCenter + new Vector2(0f, floatOffset);
            Color backGlow = new(8, 88, 34, 0);
            Color titleColor = Color.Lerp(new Color(116, 255, 166), Color.White, 0.24f);
            Color shadowColor = new Color(0, 0, 0, 210);

            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, titleFont, title, drawCenter + new Vector2(0f, 7f), shadowColor, logoRotation, titleSize * 0.5f, Vector2.One * titleScale);
            ChatManager.DrawColorCodedString(spriteBatch, titleFont, title, drawCenter + new Vector2(-4f, 2f), backGlow * 0.46f, logoRotation, titleSize * 0.5f, Vector2.One * (titleScale * 1.04f));
            ChatManager.DrawColorCodedString(spriteBatch, titleFont, title, drawCenter, titleColor, logoRotation, titleSize * 0.5f, Vector2.One * titleScale);

            if (logoGlitchTime > 0f)
            {
                float direction = logoGlitchTime % 2f == 0f ? 1f : -1f;
                ChatManager.DrawColorCodedString(spriteBatch, titleFont, title, drawCenter + new Vector2(7f * direction, -3f), new Color(42, 255, 130, 150), logoRotation, titleSize * 0.5f, Vector2.One * titleScale);
                ChatManager.DrawColorCodedString(spriteBatch, titleFont, title, drawCenter + new Vector2(-5f * direction, 3f), new Color(180, 255, 214, 110), logoRotation, titleSize * 0.5f, Vector2.One * titleScale);

                int glitchY = (int)(drawCenter.Y + Main.rand.NextFloat(-20f, 24f));
                DrawRectangle(spriteBatch, new Rectangle((int)(drawCenter.X - titleSize.X * titleScale * 0.48f), glitchY, (int)(titleSize.X * titleScale * 0.96f), 2), new Color(92, 255, 148, 130));
            }

            Vector2 subtitleSize = smallFont.MeasureString(subtitle);
            float subtitleScale = MathHelper.Clamp(targetWidth / Math.Max(1f, subtitleSize.X) * 0.25f, 0.7f, 0.92f);
            Vector2 subtitlePosition = new(Main.screenWidth * 0.5f, drawCenter.Y + titleSize.Y * titleScale * 0.52f + 18f);
            Color subtitleColor = new Color(86, 255, 156) * 0.78f;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, smallFont, subtitle, subtitlePosition, subtitleColor, 0f, subtitleSize * 0.5f, Vector2.One * subtitleScale);

            int underlineWidth = (int)(titleSize.X * titleScale * 0.76f);
            int underlineX = (int)(Main.screenWidth * 0.5f - underlineWidth * 0.5f);
            int underlineY = (int)(subtitlePosition.Y + 20f);
            DrawRectangle(spriteBatch, new Rectangle(underlineX, underlineY, underlineWidth, 2), new Color(54, 255, 136, 126));
            DrawRectangle(spriteBatch, new Rectangle(underlineX + underlineWidth / 3, underlineY + 5, underlineWidth / 3, 1), new Color(170, 255, 206, 100));
        }

        private static void DrawVignette(SpriteBatch spriteBatch)
        {
            int passes = 34;
            for (int i = 0; i < passes; i++)
            {
                float completion = 1f - i / (float)passes;
                Color shade = Color.Black * (completion * completion * 0.12f);
                DrawRectangle(spriteBatch, new Rectangle(i, 0, 1, Main.screenHeight), shade);
                DrawRectangle(spriteBatch, new Rectangle(Main.screenWidth - i - 1, 0, 1, Main.screenHeight), shade);
                DrawRectangle(spriteBatch, new Rectangle(0, i, Main.screenWidth, 1), shade);
                DrawRectangle(spriteBatch, new Rectangle(0, Main.screenHeight - i - 1, Main.screenWidth, 1), shade);
            }

            int edgeWidth = Math.Max(120, Main.screenWidth / 8);
            DrawRectangle(spriteBatch, new Rectangle(0, 0, edgeWidth, Main.screenHeight), new Color(0, 0, 0, 76));
            DrawRectangle(spriteBatch, new Rectangle(Main.screenWidth - edgeWidth, 0, edgeWidth, Main.screenHeight), new Color(0, 0, 0, 76));
        }

        private static void DrawCorner(SpriteBatch spriteBatch, Vector2 corner, int xSign, int ySign, Color color)
        {
            int length = 78;
            int thickness = 2;
            int x = (int)corner.X;
            int y = (int)corner.Y;

            DrawRectangle(spriteBatch, new Rectangle(x, y, length * xSign, thickness * ySign), color);
            DrawRectangle(spriteBatch, new Rectangle(x, y, thickness * xSign, length * ySign), color);
        }

        private static void DrawReticle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
        {
            int x = (int)center.X;
            int y = (int)center.Y;
            int r = (int)radius;

            DrawRectangle(spriteBatch, new Rectangle(x - r, y, r / 2, 1), color);
            DrawRectangle(spriteBatch, new Rectangle(x + r / 2, y, r / 2, 1), color);
            DrawRectangle(spriteBatch, new Rectangle(x, y - r, 1, r / 2), color);
            DrawRectangle(spriteBatch, new Rectangle(x, y + r / 2, 1, r / 2), color);

            DrawRectangle(spriteBatch, new Rectangle(x - 3, y - 3, 6, 1), color * 0.85f);
            DrawRectangle(spriteBatch, new Rectangle(x - 3, y + 2, 6, 1), color * 0.85f);
            DrawRectangle(spriteBatch, new Rectangle(x - 3, y - 3, 1, 6), color * 0.85f);
            DrawRectangle(spriteBatch, new Rectangle(x + 2, y - 3, 1, 6), color * 0.85f);
        }

        private static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
        {
            if (rectangle.Width == 0 || rectangle.Height == 0 || color.A == 0)
                return;

            Rectangle normalized = rectangle;
            if (normalized.Width < 0)
            {
                normalized.X += normalized.Width;
                normalized.Width = -normalized.Width;
            }

            if (normalized.Height < 0)
            {
                normalized.Y += normalized.Height;
                normalized.Height = -normalized.Height;
            }

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, normalized, color);
        }

        private sealed class CodeStream
        {
            public int Id;
            public float X;
            public float HeadY;
            public float Speed;
            public int Length;
            public float Scale;
            public float Opacity;
            public float Drift;
            public float Phase;
            public int Time;
            public int Lifetime;
            public int GlyphSeed;
            public bool Prominent;
        }
    }
}
