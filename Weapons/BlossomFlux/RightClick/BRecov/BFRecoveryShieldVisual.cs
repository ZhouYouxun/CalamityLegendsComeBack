using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    public class BFRecoveryShieldVisual : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static BlendState additiveBlend;

        private static BlendState GetAdditive()
        {
            if (additiveBlend == null)
            {
                additiveBlend = new BlendState
                {
                    ColorSourceBlend = Blend.One,
                    ColorDestinationBlend = Blend.One,
                    ColorBlendFunction = BlendFunction.Add,
                    AlphaSourceBlend = Blend.One,
                    AlphaDestinationBlend = Blend.One,
                    AlphaBlendFunction = BlendFunction.Add
                };
            }
            return additiveBlend;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            BFRecoveryShieldPlayer modPlayer = owner.GetModPlayer<BFRecoveryShieldPlayer>();
            if (!modPlayer.ShouldDrawShield)
            {
                bool brokeFromDamage = modPlayer.ShieldHitPoints == 0f && modPlayer.ShieldHitFlashTimer > 0;
                if (!Main.dedServ && brokeFromDamage)
                    SpawnGreenBreakEffect(owner.Center + new Vector2(0f, -46f));
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 2;
        }

        private static void SpawnGreenBreakEffect(Vector2 headCenter)
        {
            Color green = new Color(70, 240, 140) with { A = 0 };
            Color brightGreen = new Color(180, 255, 210) with { A = 0 };

            for (int i = 0; i < 18; i++)
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-24f, 24f),
                    Main.rand.NextFloat(-8f, 8f));
                Vector2 vel = offset.SafeNormalize(Vector2.UnitY * -1f) * Main.rand.NextFloat(1.5f, 5.5f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    headCenter + offset, vel, false,
                    Main.rand.Next(12, 24),
                    Main.rand.NextFloat(0.5f, 1.1f),
                    Color.Lerp(green, brightGreen, Main.rand.NextFloat())));
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                headCenter, Vector2.Zero,
                new Color(80, 240, 150) * 0.6f,
                new Vector2(1.4f, 0.6f),
                0f, 0.04f, 0.9f, 18));

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    headCenter + Main.rand.NextVector2Circular(16f, 6f),
                    Main.rand.NextBool(2) ? DustID.GemEmerald : DustID.GreenTorch,
                    Main.rand.NextVector2Circular(3f, 3f),
                    90,
                    Color.Lerp(green, brightGreen, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.0f, 1.4f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            BFRecoveryShieldPlayer modPlayer = owner.GetModPlayer<BFRecoveryShieldPlayer>();

            float time = Main.GlobalTimeWrappedHourly;
            float hit = modPlayer.ShieldHitFlashTimer / 18f;
            float charge = modPlayer.ShieldChargeRatio;

            // Position Matrix Head HUD Bar floating directly above player head
            Vector2 sc = owner.Center + new Vector2(0f, -48f) - Main.screenPosition;
            if (hit > 0f)
                sc += Main.rand.NextVector2Circular(1.5f * hit, 1.5f * hit);

            const float halfW = 28f;
            const float halfH = 6f;

            Color emerald = new Color(60, 240, 130);
            Color mint = new Color(170, 255, 205);
            Color darkBg = new Color(12, 35, 20, 210);

            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 3.6f + Projectile.identity * 0.4f);
            float baseAlpha = 0.65f + 0.35f * charge;
            if (hit > 0f)
                baseAlpha = Math.Min(1f, baseAlpha + hit * 0.5f);

            Color mainCol = (emerald with { A = 0 }) * baseAlpha;
            Color accentCol = (mint with { A = 0 }) * baseAlpha;
            Color brightMain = mainCol * 1.8f;
            Color hitCol = (Color.White with { A = 0 }) * hit;

            Vector2 tl = sc + new Vector2(-halfW, -halfH);
            Vector2 tr = sc + new Vector2(halfW, -halfH);
            Vector2 bl = sc + new Vector2(-halfW, halfH);
            Vector2 br = sc + new Vector2(halfW, halfH);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, GetAdditive(), SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 1. Dark Background Frame
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle bgRect = new((int)(sc.X - halfW), (int)(sc.Y - halfH), (int)(halfW * 2f), (int)(halfH * 2f));
            Main.spriteBatch.Draw(pixel, bgRect, darkBg * 0.6f);

            // 2. Animated Dashed Edge Borders (Top, Right, Bottom, Left)
            float edgeW = 1.4f + hit * 1.6f;
            DrawDashedEdge(tl, tr, mainCol, edgeW, time * 0.55f);
            DrawDashedEdge(tr, br, accentCol, edgeW, time * 0.55f + 0.5f);
            DrawDashedEdge(br, bl, mainCol, edgeW, time * 0.55f + 1.0f);
            DrawDashedEdge(bl, tl, accentCol, edgeW, time * 0.55f + 1.5f);

            // 3. Corner L-Brackets
            float bSize = 6f + hit * 2f;
            float bW = 2.2f + hit * 1.6f;
            DrawCornerBracket(tl, 1f, 1f, brightMain, bSize, bW);
            DrawCornerBracket(tr, -1f, 1f, brightMain, bSize, bW);
            DrawCornerBracket(bl, 1f, -1f, brightMain, bSize, bW);
            DrawCornerBracket(br, -1f, -1f, brightMain, bSize, bW);

            // 4. Corner Cross-Nodes & Mid-Nodes
            float nSize = 4.5f + hit * 2.5f;
            DrawNode(tl, brightMain, nSize);
            DrawNode(tr, brightMain, nSize);
            DrawNode(bl, brightMain, nSize);
            DrawNode(br, brightMain, nSize);

            // 5. Internal Matrix Segmented Meter
            float fillWidth = (halfW * 2f - 4f) * charge;
            if (fillWidth > 0f)
            {
                const int totalSegments = 8;
                float segWidth = (halfW * 2f - 4f) / totalSegments;
                float activeSegmentsF = totalSegments * charge;

                for (int i = 0; i < totalSegments; i++)
                {
                    if (i >= activeSegmentsF)
                        break;

                    float segFrac = Math.Min(1f, activeSegmentsF - i);
                    float segX = sc.X - halfW + 2f + i * segWidth;
                    float segW = (segWidth - 1.5f) * segFrac;

                    Color segCol = Color.Lerp(mainCol, accentCol, i / (float)totalSegments) * pulse;
                    if (hit > 0f)
                        segCol = Color.Lerp(segCol, hitCol, hit);

                    Rectangle segRect = new((int)segX, (int)(sc.Y - halfH + 2f), (int)Math.Max(1f, segW), (int)(halfH * 2f - 4f));
                    Main.spriteBatch.Draw(pixel, segRect, segCol);
                }

                // Moving Matrix Data Dot traversing along top dashed edge
                float flowT = (time * 1.8f) % 1f;
                float flowX = sc.X - halfW + (halfW * 2f) * flowT;
                DrawNode(new Vector2(flowX, sc.Y - halfH), brightMain * 1.6f, 4f);
            }

            // 6. Damage Hit Flash Bloom
            if (hit > 0f)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Main.spriteBatch.Draw(bloom, sc, null,
                    hitCol, 0f, bloom.Size() * 0.5f,
                    0.22f + hit * 0.25f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private static void DrawDashedEdge(Vector2 a, Vector2 b, Color color, float width, float animOffset)
        {
            float totalLen = Vector2.Distance(a, b);
            if (totalLen < 1f) return;

            Vector2 dir = (b - a) / totalLen;
            const float segLen = 8f;
            const float gapLen = 4f;
            const float cycle = segLen + gapLen;
            float phase = (animOffset * 26f) % cycle;

            for (float pos = -phase; pos < totalLen; pos += cycle)
            {
                float s = Math.Max(0f, pos);
                float e = Math.Min(totalLen, pos + segLen);
                if (e <= s) continue;
                DrawLineSegment(a + dir * s, a + dir * e, color, width);
            }
        }

        private static void DrawLineSegment(Vector2 start, Vector2 end, Color color, float width)
        {
            if (start == end) return;
            if (color.A == 0 && (color.R | color.G | color.B) != 0)
                color.A = 255;

            float dist = Vector2.Distance(start, end);
            if (dist <= 0.01f) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float rotation = (end - start).ToRotation();
            Vector2 scale = new(dist, width);
            Vector2 origin = new(0f, 0.5f);

            Main.spriteBatch.Draw(pixel, start, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }

        private static void DrawCornerBracket(Vector2 corner, float dx, float dy, Color color, float size, float width)
        {
            DrawLineSegment(corner, corner + new Vector2(dx * size, 0f), color, width);
            DrawLineSegment(corner, corner + new Vector2(0f, dy * size), color, width);
        }

        private static void DrawNode(Vector2 pos, Color color, float size)
        {
            if (color.A == 0 && (color.R | color.G | color.B) != 0)
                color.A = 255;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int w = Math.Max(1, (int)size);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)(pos.X - w * 0.5f), (int)(pos.Y - 1f), w, 2), color);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)(pos.X - 1f), (int)(pos.Y - w * 0.5f), 2, w), color);
        }
    }
}
