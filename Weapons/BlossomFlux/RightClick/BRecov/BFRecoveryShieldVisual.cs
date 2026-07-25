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
                    SpawnGreenBreakEffect(owner.Center);
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 2;
        }

        private static void SpawnGreenBreakEffect(Vector2 worldCenter)
        {
            Color green = new Color(70, 240, 140) with { A = 0 };
            Color brightGreen = new Color(180, 255, 210) with { A = 0 };

            for (int i = 0; i < 26; i++)
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-36f, 36f),
                    Main.rand.NextFloat(-54f, 54f));
                Vector2 vel = offset.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.8f, 7.5f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    worldCenter + offset, vel, false,
                    Main.rand.Next(14, 28),
                    Main.rand.NextFloat(0.6f, 1.25f),
                    Color.Lerp(green, brightGreen, Main.rand.NextFloat())));
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                worldCenter, Vector2.Zero,
                new Color(80, 240, 150) * 0.6f,
                new Vector2(1.8f, 1.2f),
                0f, 0.04f, 1.15f, 22));

            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    worldCenter + Main.rand.NextVector2Circular(20f, 30f),
                    Main.rand.NextBool(2) ? DustID.GemEmerald : DustID.GreenTorch,
                    Main.rand.NextVector2Circular(4f, 4f),
                    90,
                    Color.Lerp(green, brightGreen, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.1f, 1.6f));
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

            Vector2 sc = owner.Center - Main.screenPosition;
            const float halfW = 36f;
            const float halfH = 54f;

            Color emerald = new Color(60, 230, 120);
            Color mint = new Color(150, 255, 190);

            float pulse = 0.72f + 0.28f * (float)Math.Sin(time * 3.2f + Projectile.identity * 0.4f);
            float baseAlpha = 0.52f + 0.32f * charge;
            if (hit > 0f)
                baseAlpha = Math.Min(1f, baseAlpha + hit * 0.55f);

            if (hit > 0.2f && Main.rand.NextFloat() < hit * 0.55f)
                baseAlpha *= Main.rand.NextFloat(0.15f, 0.6f);

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

            float edgeW = 1.4f + hit * 2.4f;

            // Dashed green matrix edges
            DrawDashedEdge(tl, tr, mainCol, edgeW, time * 0.55f);
            DrawDashedEdge(tr, br, accentCol, edgeW, time * 0.55f + 0.5f);
            DrawDashedEdge(br, bl, mainCol, edgeW, time * 0.55f + 1.0f);
            DrawDashedEdge(bl, tl, accentCol, edgeW, time * 0.55f + 1.5f);

            // Soft glow halo
            DrawDashedEdge(tl, tr, mainCol * 0.35f, edgeW * 4f, time * 0.35f + 0.18f);
            DrawDashedEdge(tr, br, accentCol * 0.3f, edgeW * 4f, time * 0.35f + 0.68f);
            DrawDashedEdge(br, bl, mainCol * 0.35f, edgeW * 4f, time * 0.35f + 1.18f);
            DrawDashedEdge(bl, tl, accentCol * 0.3f, edgeW * 4f, time * 0.35f + 1.68f);

            // Chloroplast leaf-style chamfered corner brackets
            float bSize = 15f + hit * 5f;
            float bW = 2.6f + hit * 3.0f;
            DrawCornerBracket(tl, 1f, 1f, brightMain, bSize, bW);
            DrawCornerBracket(tr, -1f, 1f, brightMain, bSize, bW);
            DrawCornerBracket(bl, 1f, -1f, brightMain, bSize, bW);
            DrawCornerBracket(br, -1f, -1f, brightMain, bSize, bW);

            // Corner cross nodes
            float nSize = 5.5f + hit * 4.5f;
            DrawNode(tl, brightMain, nSize);
            DrawNode(tr, brightMain, nSize);
            DrawNode(bl, brightMain, nSize);
            DrawNode(br, brightMain, nSize);

            // Mid-edge nodes
            DrawNode(sc + new Vector2(0f, -halfH), accentCol, 4f);
            DrawNode(sc + new Vector2(0f, halfH), accentCol, 4f);
            DrawNode(sc + new Vector2(-halfW, 0f), accentCol, 4f);
            DrawNode(sc + new Vector2(halfW, 0f), accentCol, 4f);

            // Horizontal matrix scan line
            float scanY = sc.Y + (float)Math.Sin(time * 2.8f) * (halfH - 6f);
            DrawLineSegment(
                new Vector2(sc.X - halfW + 4f, scanY),
                new Vector2(sc.X + halfW - 4f, scanY),
                accentCol * (0.7f * pulse), 1.6f);

            // Moving energy pulse on right edge
            {
                float flowT = (time * 1.8f) % 1f;
                float flowY = MathHelper.Lerp(sc.Y + halfH, sc.Y - halfH, flowT);
                DrawNode(new Vector2(sc.X + halfW, flowY), mainCol * 1.5f, 4.5f);
            }

            // Charge level indicator on outer left edge
            if (charge < 1f)
            {
                float barH = halfH * 2f * charge;
                Color barCol = mainCol * 0.5f;
                DrawLineSegment(
                    new Vector2(sc.X - halfW - 8f, sc.Y + halfH),
                    new Vector2(sc.X - halfW - 8f, sc.Y + halfH - barH),
                    barCol, 2.8f);
                DrawNode(new Vector2(sc.X - halfW - 8f, sc.Y + halfH - barH), barCol * 2.2f, 4.8f);
            }

            // Corner hit flash bloom
            if (hit > 0f)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                foreach (Vector2 corner in new[] { tl, tr, bl, br })
                {
                    Main.spriteBatch.Draw(bloom, corner, null,
                        hitCol, 0f, bloom.Size() * 0.5f,
                        0.15f + hit * 0.3f, SpriteEffects.None, 0f);
                }
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
            const float segLen = 9f;
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
            Texture2D lineTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Line").Value;
            float rotation = (end - start).ToRotation();
            Vector2 scale = new(Vector2.Distance(start, end) / lineTex.Width, width);
            Main.spriteBatch.Draw(lineTex, start, null, color, rotation,
                lineTex.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);
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
