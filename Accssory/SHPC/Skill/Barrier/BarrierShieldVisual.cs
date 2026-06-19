using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.Barrier
{
    public class BarrierShieldVisual : ModProjectile
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

            BarrierPlayer modPlayer = owner.GetModPlayer<BarrierPlayer>();
            if (!modPlayer.ShouldDrawShield)
            {
                // Shattering visual only when shield was destroyed by damage
                bool brokeFromDamage = modPlayer.BarrierVisible && modPlayer.ShieldCurrentHitPoints == 0 && modPlayer.ShieldHitFlashTimer > 0;
                if (!Main.dedServ && brokeFromDamage)
                    SpawnBreakEffect(owner.Center);
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 2;
        }

        private static void SpawnBreakEffect(Vector2 worldCenter)
        {
            Color blue = new Color(40, 200, 255) with { A = 0 };
            for (int i = 0; i < 24; i++)
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-34f, 34f),
                    Main.rand.NextFloat(-52f, 52f));
                Vector2 vel = offset.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.5f, 7f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    worldCenter + offset, vel, false,
                    Main.rand.Next(14, 28),
                    Main.rand.NextFloat(0.55f, 1.15f),
                    blue));
            }
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                worldCenter, Vector2.Zero,
                new Color(40, 200, 255) * 0.52f,
                new Vector2(1.8f, 1.2f),
                0f, 0.04f, 1.1f, 20));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            BarrierPlayer modPlayer = owner.GetModPlayer<BarrierPlayer>();

            float time = Main.GlobalTimeWrappedHourly;
            float hit = modPlayer.ShieldHitFlashTimer / 18f;
            float charge = modPlayer.ShieldChargeRatio;
            bool recharging = modPlayer.RechargeDelayRemainingFrames == 0 && charge < 1f;

            Vector2 sc = owner.Center - Main.screenPosition;
            const float halfW = 34f;
            const float halfH = 52f;

            // SHPC tech blue palette
            Color techBlue = new Color(40, 200, 255);
            Color accent = new Color(70, 140, 255);

            float pulse = 0.72f + 0.28f * (float)Math.Sin(time * 3.0f + Projectile.identity * 0.4f);
            float rechargePulse = recharging ? (0.5f + 0.5f * (float)Math.Sin(time * 7f)) : 0f;
            float baseAlpha = 0.48f + 0.28f * charge + rechargePulse * 0.14f;
            if (hit > 0f)
                baseAlpha = Math.Min(1f, baseAlpha + hit * 0.52f);

            // Damage flicker
            if (hit > 0.2f && Main.rand.NextFloat() < hit * 0.55f)
                baseAlpha *= Main.rand.NextFloat(0.12f, 0.55f);

            Color mainCol = (techBlue with { A = 0 }) * baseAlpha;
            Color accentCol = (accent with { A = 0 }) * baseAlpha;
            Color brightMain = mainCol * 1.7f;
            Color hitCol = (Color.White with { A = 0 }) * hit;

            Vector2 tl = sc + new Vector2(-halfW, -halfH);
            Vector2 tr = sc + new Vector2(halfW, -halfH);
            Vector2 bl = sc + new Vector2(-halfW, halfH);
            Vector2 br = sc + new Vector2(halfW, halfH);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, GetAdditive(), SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            float edgeW = 1.3f + hit * 2.2f;

            // Dashed rectangle edges
            DrawDashedEdge(tl, tr, mainCol, edgeW, time * 0.55f);
            DrawDashedEdge(tr, br, accentCol, edgeW, time * 0.55f + 0.5f);
            DrawDashedEdge(br, bl, mainCol, edgeW, time * 0.55f + 1.0f);
            DrawDashedEdge(bl, tl, accentCol, edgeW, time * 0.55f + 1.5f);

            // Soft glow halo behind edges
            DrawDashedEdge(tl, tr, mainCol * 0.32f, edgeW * 4f, time * 0.35f + 0.18f);
            DrawDashedEdge(tr, br, accentCol * 0.28f, edgeW * 4f, time * 0.35f + 0.68f);
            DrawDashedEdge(br, bl, mainCol * 0.32f, edgeW * 4f, time * 0.35f + 1.18f);
            DrawDashedEdge(bl, tl, accentCol * 0.28f, edgeW * 4f, time * 0.35f + 1.68f);

            // Corner L-brackets
            float bSize = 14f + hit * 5f;
            float bW = 2.5f + hit * 2.8f;
            DrawCornerBracket(tl,  1f,  1f, brightMain, bSize, bW);
            DrawCornerBracket(tr, -1f,  1f, brightMain, bSize, bW);
            DrawCornerBracket(bl,  1f, -1f, brightMain, bSize, bW);
            DrawCornerBracket(br, -1f, -1f, brightMain, bSize, bW);

            // Corner cross-nodes
            float nSize = 5f + hit * 4.5f;
            DrawNode(tl, brightMain, nSize);
            DrawNode(tr, brightMain, nSize);
            DrawNode(bl, brightMain, nSize);
            DrawNode(br, brightMain, nSize);

            // Mid-edge nodes
            DrawNode(sc + new Vector2(0f, -halfH), accentCol, 3.5f);
            DrawNode(sc + new Vector2(0f,  halfH), accentCol, 3.5f);
            DrawNode(sc + new Vector2(-halfW, 0f), accentCol, 3.5f);
            DrawNode(sc + new Vector2( halfW, 0f), accentCol, 3.5f);

            // Horizontal scan line
            float scanY = sc.Y + (float)Math.Sin(time * 2.6f) * (halfH - 6f);
            DrawLineSegment(
                new Vector2(sc.X - halfW + 4f, scanY),
                new Vector2(sc.X + halfW - 4f, scanY),
                accentCol * (0.68f * pulse), 1.5f);

            // Moving data dot on right edge (charging / active animation)
            {
                float flowT = (time * 1.6f) % 1f;
                float flowY = MathHelper.Lerp(sc.Y + halfH, sc.Y - halfH, flowT);
                float dotBright = recharging ? (1.8f + 0.9f * rechargePulse) : 0.9f;
                DrawNode(new Vector2(sc.X + halfW, flowY), mainCol * dotBright, 4f);
            }

            // Charge level bar on left outer edge
            if (charge < 1f)
            {
                float barH = halfH * 2f * charge;
                Color barCol = recharging
                    ? Color.Lerp(accentCol, mainCol, rechargePulse) * (1.4f + 0.5f * rechargePulse)
                    : mainCol * 0.42f;
                DrawLineSegment(
                    new Vector2(sc.X - halfW - 7f, sc.Y + halfH),
                    new Vector2(sc.X - halfW - 7f, sc.Y + halfH - barH),
                    barCol, 2.5f);
                DrawNode(new Vector2(sc.X - halfW - 7f, sc.Y + halfH - barH), barCol * 2.2f, 4.5f);
            }

            // Hit bloom flash at corners
            if (hit > 0f)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                foreach (Vector2 corner in new[] { tl, tr, bl, br })
                {
                    Main.spriteBatch.Draw(bloom, corner, null,
                        hitCol, 0f, bloom.Size() * 0.5f,
                        0.14f + hit * 0.28f, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        // Animated dashed edge with moving segments
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
            // Ensure alpha is visible for additive blend
            if (color.A == 0 && (color.R | color.G | color.B) != 0)
                color.A = 255;
            Texture2D lineTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Line").Value;
            float rotation = (end - start).ToRotation();
            Vector2 scale = new(Vector2.Distance(start, end) / lineTex.Width, width);
            Main.spriteBatch.Draw(lineTex, start, null, color, rotation,
                lineTex.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);
        }

        // L-shaped corner bracket: dx/dy are ±1 toward center
        private static void DrawCornerBracket(Vector2 corner, float dx, float dy, Color color, float size, float width)
        {
            DrawLineSegment(corner, corner + new Vector2(dx * size, 0f), color, width);
            DrawLineSegment(corner, corner + new Vector2(0f, dy * size), color, width);
        }

        // Cross-shaped node marker (same style as HyperdimensionalMatrixCore)
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
