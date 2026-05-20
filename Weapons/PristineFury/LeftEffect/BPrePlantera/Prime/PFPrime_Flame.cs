using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPrime_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BounceCount => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 96;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Timer++;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 206, 92));
            Projectile.rotation += 0.38f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
            Projectile.velocity *= 0.995f;
            Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.66f);

            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(3f, 12f),
                    -forward.RotatedByRandom(0.36f) * Main.rand.NextFloat(0.8f, 2.8f),
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.48f, 0.78f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.15f, 0.4f)),
                    true,
                    true));
            }

            if (Timer % 9f == 0f)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Projectile.velocity * 0.12f,
                    "CalamityMod/Particles/PulseStar",
                    false,
                    15,
                    0.06f,
                    theme,
                    new Vector2(0.6f, 1.7f),
                    true,
                    false,
                    extraRotation: Projectile.rotation));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnBounceExplosion();
            BounceCount++;
            if (BounceCount >= 5f)
                return true;

            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * 0.92f;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * 0.92f;

            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 42);
            Projectile.netUpdate = true;
            return false;
        }

        private void SpawnBounceExplosion()
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int explosion = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PFPrime_BounceExplosion>(),
                    Math.Max(1, (int)(Projectile.damage * 0.55f)),
                    Projectile.knockBack * 0.5f,
                    Projectile.owner);
                PFLeftEffectRules.ApplyTheme(explosion, (PristineFuryMark)(int)Projectile.ai[2]);
            }

            if (Main.dedServ)
                return;

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 206, 92));
            for (int i = 0; i < 9; i++)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.35f))));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 240);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/PulseStar").Value;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 206, 92)) with { A = 0 };
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 8f, Timer, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, trailPos, null, theme * opacity * (1f - completion) * 0.3f, Projectile.rotation, bloom.Size() * 0.5f, 0.18f * (1f - completion), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, drawPosition, null, theme * opacity * 0.64f, Projectile.rotation, bloom.Size() * 0.5f, 0.28f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.Lerp(theme, Color.White with { A = 0 }, 0.32f) * opacity * 0.8f, Projectile.rotation, star.Size() * 0.5f, 0.26f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PFPrime_BounceExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            Projectile.Resize(118, 118);
            Projectile.Damage();

            if (Main.dedServ)
                return;

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, Color.Cyan);
            Particle expandingPulse = new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                theme,
                new Vector2(1.2f, 1.2f),
                0f,
                0.5f,
                6.0f,
                20);
            GeneralParticleHandler.SpawnParticle(expandingPulse);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(theme, Color.White, 0.35f),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.18f,
                0.8f,
                14));
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
