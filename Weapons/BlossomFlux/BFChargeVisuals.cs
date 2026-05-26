using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal static class BFChargeVisualHelper
    {
        internal static bool TryGetHoldout(float holdoutIndex, int ownerIndex, out Projectile holdout, out Vector2 aimDirection)
        {
            holdout = null;
            aimDirection = ownerIndex >= 0 && ownerIndex < Main.maxPlayers ? Vector2.UnitX * Main.player[ownerIndex].direction : Vector2.UnitX;

            int index = (int)holdoutIndex;
            if (index < 0 || index >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[index];
            if (!candidate.active || candidate.type != ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>())
                return false;

            holdout = candidate;
            aimDirection = candidate.velocity.SafeNormalize(aimDirection);
            return true;
        }

        internal static Vector2 Muzzle(Projectile holdout, Vector2 aimDirection) => holdout.Center + aimDirection * 42f;

        internal static float PopOpacity(float timer, int lifetime) =>
            Utils.GetLerpValue(0f, 3f, timer, true) * Utils.GetLerpValue(0f, 6f, lifetime - timer, true);
    }

    internal sealed class BFRecoveryHeartConvergeFX : ModProjectile
    {
        private const int Lifetime = 18;
        private static readonly Color MainColor = new(92, 255, 132);
        private static readonly Color AccentColor = new(230, 255, 228);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float SideSign => ref Projectile.ai[1];
        private ref float Seed => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            if (!BFChargeVisualHelper.TryGetHoldout(HoldoutIndex, Projectile.owner, out Projectile holdout, out Vector2 aimDirection))
            {
                Projectile.Kill();
                return;
            }

            float progress = MathHelper.Clamp(Timer / Lifetime, 0f, 1f);
            Vector2 target = Vector2.Lerp(holdout.Center, BFChargeVisualHelper.Muzzle(holdout, aimDirection), 0.55f);
            Vector2 side = aimDirection.RotatedBy(MathHelper.PiOver2) * Math.Sign(SideSign == 0f ? 1f : SideSign);
            Vector2 snap = target - aimDirection * MathHelper.Lerp(42f, -10f, progress) + side * MathF.Sin(progress * MathHelper.TwoPi + Seed) * MathHelper.Lerp(24f, 0f, progress);
            Projectile.velocity = snap - Projectile.Center;
            Projectile.Center = snap;
            Projectile.rotation = aimDirection.ToRotation();
            Projectile.Opacity = BFChargeVisualHelper.PopOpacity(Timer, Lifetime);
            Projectile.scale = MathHelper.Lerp(0.34f, 0.08f, progress);
            Lighting.AddLight(Projectile.Center, MainColor.ToVector3() * 0.35f * Projectile.Opacity);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    -aimDirection * Main.rand.NextFloat(0.6f, 1.5f),
                    false,
                    7,
                    Main.rand.NextFloat(0.1f, 0.18f),
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat(0.2f, 0.55f)) * Projectile.Opacity,
                    true,
                    false,
                    true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawSnapFlash(Projectile.Center, Projectile.rotation, Projectile.Opacity, MainColor, AccentColor, Projectile.scale);
            return false;
        }

        internal static void DrawSnapFlash(Vector2 worldCenter, float rotation, float opacity, Color mainColor, Color accentColor, float scale)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = worldCenter - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, mainColor with { A = 0 } * (0.42f * opacity), 0f, bloom.Size() * 0.5f, scale * 1.8f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(spark, drawPosition, null, accentColor with { A = 0 } * (0.68f * opacity), rotation + MathHelper.PiOver2, spark.Size() * 0.5f, new Vector2(0.05f, 0.32f) * (1f + scale), SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(spark, drawPosition, null, mainColor with { A = 0 } * (0.46f * opacity), rotation, spark.Size() * 0.5f, new Vector2(0.035f, 0.18f) * (1f + scale), SpriteEffects.None, 0f);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }

    internal sealed class BFReconContractingWaveFX : ModProjectile
    {
        private const int Lifetime = 16;
        private static readonly Color MainColor = new(82, 232, 255);
        private static readonly Color AccentColor = new(170, 184, 255);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float StartRadius => ref Projectile.ai[1];
        private ref float Phase => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];
        private Vector2 aimDirection = Vector2.UnitX;

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            if (!BFChargeVisualHelper.TryGetHoldout(HoldoutIndex, Projectile.owner, out Projectile holdout, out aimDirection))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = BFChargeVisualHelper.Muzzle(holdout, aimDirection) - aimDirection * 14f;
            Projectile.rotation = aimDirection.ToRotation();
            Projectile.Opacity = BFChargeVisualHelper.PopOpacity(Timer, Lifetime);
            Lighting.AddLight(Projectile.Center, MainColor.ToVector3() * 0.28f * Projectile.Opacity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Timer / Lifetime, 0f, 1f);
            float radius = MathHelper.Lerp(StartRadius <= 0f ? 70f : StartRadius, 7f, MathHelper.SmoothStep(0f, 1f, progress));
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float ringScale = radius / (ring.Width * 0.5f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(ring, drawPosition, null, MainColor with { A = 0 } * (0.5f * Projectile.Opacity), Projectile.rotation + Phase, ring.Size() * 0.5f, new Vector2(ringScale, ringScale * 0.58f), SpriteEffects.None, 0f);
            for (int i = 0; i < 4; i++)
            {
                float angle = Projectile.rotation + Phase + MathHelper.PiOver2 * i;
                Main.EntitySpriteDraw(spark, drawPosition + angle.ToRotationVector2() * radius * 0.34f, null, AccentColor with { A = 0 } * (0.5f * Projectile.Opacity), angle + MathHelper.PiOver2, spark.Size() * 0.5f, new Vector2(0.026f, 0.15f), SpriteEffects.None, 0f);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    internal sealed class BFBombardStrafeFX : ModProjectile
    {
        private const int Lifetime = 14;
        private static readonly Color MainColor = new(255, 62, 42);
        private static readonly Color AccentColor = new(255, 204, 82);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float LaneOffset => ref Projectile.ai[1];
        private ref float BackOffset => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            if (Timer == 1f && BFChargeVisualHelper.TryGetHoldout(HoldoutIndex, Projectile.owner, out Projectile holdout, out Vector2 aimDirection))
            {
                Vector2 side = aimDirection.RotatedBy(MathHelper.PiOver2);
                Projectile.Center = holdout.Center + aimDirection * BackOffset + side * LaneOffset;
                Projectile.velocity = aimDirection.RotatedBy(Main.rand.NextFloat(-0.12f, 0.12f)) * Main.rand.NextFloat(18f, 24f) + side * Main.rand.NextFloat(-1.5f, 1.5f);
            }

            Projectile.velocity *= 1.04f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = BFChargeVisualHelper.PopOpacity(Timer, Lifetime);
            Projectile.scale = MathHelper.Lerp(0.42f, 0.06f, Timer / Lifetime);
            Lighting.AddLight(Projectile.Center, MainColor.ToVector3() * 0.35f * Projectile.Opacity);

            if (!Main.dedServ)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.08f, 80, Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat(0.2f, 0.6f)), Main.rand.NextFloat(0.7f, 1f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFRecoveryHeartConvergeFX.DrawSnapFlash(Projectile.Center, Projectile.rotation, Projectile.Opacity, MainColor, AccentColor, Projectile.scale);
            return false;
        }
    }

    internal sealed class BFPlagueNanomachineCloudFX : ModProjectile
    {
        private const int Lifetime = 20;
        private static readonly Color MainColor = new(178, 255, 58);
        private static readonly Color AccentColor = new(76, 220, 68);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Seed => ref Projectile.ai[0];
        private ref float RadiusScale => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= 0.78f;
            Projectile.rotation += 0.22f;
            Projectile.Opacity = BFChargeVisualHelper.PopOpacity(Timer, Lifetime);
            Projectile.scale = (RadiusScale <= 0f ? 1f : RadiusScale) * MathHelper.Lerp(0.35f, 1.05f, Timer / Lifetime);
            Lighting.AddLight(Projectile.Center, AccentColor.ToVector3() * 0.22f * Projectile.Opacity);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = (Seed + Timer * 0.5f + i * MathHelper.Pi).ToRotationVector2().RotatedByRandom(0.8f) * Main.rand.NextFloat(6f, 22f) * Projectile.scale;
                GeneralParticleHandler.SpawnParticle(new NanoParticle(
                    Projectile.Center + offset,
                    -offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.25f, 1.1f),
                    Color.Lerp(AccentColor, MainColor, Main.rand.NextFloat(0.2f, 0.65f)) * Projectile.Opacity,
                    Main.rand.NextFloat(0.3f, 0.55f),
                    Main.rand.Next(10, 18),
                    Main.rand.NextBool(4),
                    true,
                    true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(ring, drawPosition, null, MainColor with { A = 0 } * (0.32f * Projectile.Opacity), Projectile.rotation, ring.Size() * 0.5f, Projectile.scale * 0.18f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawPosition, null, AccentColor with { A = 0 } * (0.2f * Projectile.Opacity), 0f, bloom.Size() * 0.5f, Projectile.scale * 0.16f, SpriteEffects.None, 0f);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
