using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.UI
{
    // 右侧弧形弹夹面板：将玩家存储的印记以圆形图标弧排列显示在玩家右侧。
    // 被选中的印记有旋转星芒高亮，空格暗化显示。
    internal sealed class PFMarkStatusArc : ModProjectile, IScreenOverlayProjectile
    {
        // 弧形布局参数（槽数由 pfPlayer.EffectiveMaxSlots 动态决定）
        private const float ArcBaseX = 82f;
        private const float ArcBulgeX = 17f;
        private const float ArcHalfSpan = 52f;
        private const int ScreenMargin = 12;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            FadeOut = owner.HeldItem?.ModItem is not NewLegendPristineFury;
            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.12f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            PristineFuryPlayer pfPlayer = owner.GetModPlayer<PristineFuryPlayer>();

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D halfStar = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;

            float opacity = Projectile.Opacity;
            float time = Main.GlobalTimeWrappedHourly;

            int slotCount = pfPlayer.EffectiveMaxSlots;

            // Compute the arc slot centers in screen space.
            Vector2[] slots = ComputeSlotPositions(owner, slotCount);

            // === Pass 1: alpha-blend backgrounds + connecting spine ===
            // Spine: thin line linking all slot centers.
            for (int i = 0; i < slotCount - 1; i++)
                DrawScreenLine(slots[i], slots[i + 1], new Color(62, 48, 88, 0) * (0.20f * opacity), 1.4f);

            // Boss head icons (alpha blend).
            for (int i = 0; i < pfPlayer.MarkQueueCount; i++)
            {
                if (i >= slotCount) break;
                Texture2D icon = PristineFuryMarkHelper.TryGetMarkBossHeadTexture(pfPlayer.MarkQueue[i]);
                if (icon == null)
                    continue;

                float maxDim = Math.Max(icon.Width, icon.Height);
                float iconScale = 20f / maxDim;
                Main.EntitySpriteDraw(icon, slots[i], null,
                    Color.White * (0.82f * opacity), 0f, icon.Size() * 0.5f, iconScale, SpriteEffects.None, 0);
            }

            // === Pass 2: additive glow rings / stars ===
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null,
                Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < slotCount; i++)
            {
                bool filled = i < pfPlayer.MarkQueueCount;
                bool selected = (i == pfPlayer.SelectedMarkIndex);
                Vector2 pos = slots[i];
                Color markColor = filled
                    ? PristineFuryMarkHelper.GetColor(pfPlayer.MarkQueue[i])
                    : new Color(60, 55, 80);

                float pulse = selected ? 0.86f + 0.14f * (float)Math.Sin(time * 6.2f + i * 0.9f) : 1f;

                if (!filled)
                {
                    // Empty: faint gray ring outline only.
                    Main.EntitySpriteDraw(ring, pos, null,
                        new Color(55, 50, 75, 0) * (0.22f * opacity), 0f,
                        ring.Size() * 0.5f, 0.14f, SpriteEffects.None, 0);
                }
                else if (selected)
                {
                    // Selected: bright ring + outer glow + 3 rotating HalfStars + white core.
                    Main.EntitySpriteDraw(ring, pos, null,
                        (markColor with { A = 0 }) * (0.92f * opacity) * pulse,
                        0f, ring.Size() * 0.5f, 0.26f * pulse, SpriteEffects.None, 0);

                    Main.EntitySpriteDraw(bloom, pos, null,
                        (markColor with { A = 0 }) * (0.48f * opacity) * pulse,
                        0f, bloom.Size() * 0.5f, 0.24f * pulse, SpriteEffects.None, 0);

                    Main.EntitySpriteDraw(bloom, pos, null,
                        (Color.White with { A = 0 }) * (0.62f * opacity),
                        0f, bloom.Size() * 0.5f, 0.07f, SpriteEffects.None, 0);

                    for (int s = 0; s < 3; s++)
                    {
                        float starRot = time * 1.85f + s * (MathHelper.TwoPi / 3f);
                        Main.EntitySpriteDraw(halfStar, pos, null,
                            (markColor with { A = 0 }) * (0.60f * opacity) * pulse,
                            starRot, halfStar.Size() * 0.5f,
                            new Vector2(0.09f, 0.40f + 0.18f * pulse), SpriteEffects.None, 0);
                    }
                }
                else
                {
                    // Filled but not selected: medium ring + subtle inner bloom.
                    Main.EntitySpriteDraw(ring, pos, null,
                        (markColor with { A = 0 }) * (0.58f * opacity),
                        0f, ring.Size() * 0.5f, 0.18f, SpriteEffects.None, 0);

                    Main.EntitySpriteDraw(bloom, pos, null,
                        (markColor with { A = 0 }) * (0.28f * opacity),
                        0f, bloom.Size() * 0.5f, 0.12f, SpriteEffects.None, 0);
                }
            }

            // Restore alpha blend.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private static Vector2[] ComputeSlotPositions(Player owner, int slotCount)
        {
            Vector2[] result = new Vector2[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                float t = slotCount > 1 ? i / (float)(slotCount - 1) : 0.5f; // 0..1
                float arcY = MathHelper.Lerp(-ArcHalfSpan, ArcHalfSpan, t) + owner.gfxOffY;
                // Arc bows outward in the middle (sin curve).
                float arcX = (ArcBaseX + (float)Math.Sin(t * MathHelper.Pi) * ArcBulgeX) * owner.direction;
                Vector2 raw = owner.Center - Main.screenPosition + new Vector2(arcX, arcY);
                result[i] = ClampToScreen(raw);
            }
            return result;
        }

        private static Vector2 ClampToScreen(Vector2 pos)
        {
            return new Vector2(
                MathHelper.Clamp(pos.X, ScreenMargin, Main.screenWidth - ScreenMargin),
                MathHelper.Clamp(pos.Y, ScreenMargin, Main.screenHeight - ScreenMargin));
        }

        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 edge = end - start;
            if (edge.LengthSquared() < 0.01f)
                return;

            Main.EntitySpriteDraw(pixel, start, new Rectangle(0, 0, 1, 1), color,
                edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), thickness),
                SpriteEffects.None, 0);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
    }
}
