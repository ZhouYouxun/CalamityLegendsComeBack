using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class PristineFuryRightPellet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/RightAndHook/PristineFuryRightPellet";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 110;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 0.992f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.72f, 0.22f, 0.08f));
            if (Main.rand.NextBool())
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center - direction * 6f + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.Torch,
                    -direction.RotatedByRandom(0.36f) * Main.rand.NextFloat(0.35f, 1.35f),
                    120,
                    Color.Lerp(Color.OrangeRed, Color.Gold, Main.rand.NextFloat(0.2f, 0.65f)),
                    Main.rand.NextFloat(0.65f, 1.05f));
                ember.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryGroundFlame>(), Projectile.damage, 0f, Projectile.owner, 1f);
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 240);
    }

    internal sealed class PristineFuryGroundFlame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            float scale = Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
            Projectile.width = (int)(80f * scale);
            Projectile.height = (int)(36f * scale);
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.42f, 0.05f) * scale);
            if (!Main.dedServ)
            {
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-Projectile.width * 0.45f, Projectile.width * 0.45f), Main.rand.NextFloat(-8f, 6f));
                Vector2 velocity = new(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(-2.6f, -0.8f));
                Particle flame = new MediumMistParticle(
                    position,
                    velocity,
                    Color.Lerp(Color.OrangeRed, Color.Gold, Main.rand.NextFloat(0.2f, 0.55f)),
                    Color.Black,
                    Main.rand.NextFloat(0.45f, 0.9f) * scale,
                    Main.rand.Next(24, 42),
                    Main.rand.NextFloat(-0.08f, 0.08f));
                GeneralParticleHandler.SpawnParticle(flame);

                if (Main.rand.NextBool(3))
                {
                    Particle ember = new SparkParticle(
                        position + Main.rand.NextVector2Circular(8f, 4f),
                        velocity.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.2f, 2.8f),
                        true,
                        Main.rand.Next(14, 22),
                        Main.rand.NextFloat(0.55f, 0.9f) * scale,
                        Color.Orange);
                    GeneralParticleHandler.SpawnParticle(ember);
                }
            }
        }
    }

    internal sealed class PristineFuryImpactExplosion : ModProjectile, ILocalizedModType
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
            int radius = (int)MathHelper.Clamp(Projectile.ai[0] <= 0f ? 55f : Projectile.ai[0], 30f, 240f);
            Projectile.Resize(radius, radius);
            Projectile.Damage();
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                Color.OrangeRed * 0.75f,
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.08f,
                radius / 120f,
                18));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Gold,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.05f,
                radius / 95f,
                16,
                true));

            for (int i = 0; i < 18; i++)
            {
                Particle spark = new SparkParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f),
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.55f, 1.15f),
                    Color.Lerp(Color.Orange, Color.White, Main.rand.NextFloat(0.1f, 0.35f)));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
    }

    internal sealed class PristineFuryRightNovaChargeOrb : ModProjectile, ILocalizedModType
    {
        private static readonly Color NovaRed = new(255, 54, 42);
        private static readonly Color NovaOrange = new(255, 126, 42);

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float Charge => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float FullChargePulseCreated => ref Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            int holdoutIndex = (int)HoldoutIndex;
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
            {
                Projectile.Kill();
                return;
            }

            float charge = MathHelper.Clamp(Charge, 0f, 1f);
            Vector2 direction = holdout.AimDirection;
            Projectile.Center = holdout.GunTipPosition + direction * (6f + charge * 5f);
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();
            Projectile.timeLeft = 2;

            Color glow = Color.Lerp(NovaOrange, Color.White, charge * 0.34f);
            Lighting.AddLight(Projectile.Center, glow.ToVector3() * (0.35f + charge * 1.25f));

            if (Main.dedServ)
                return;

            SpawnChargeParticles(direction, charge);
            if (charge >= 1f && FullChargePulseCreated == 0f)
            {
                FullChargePulseCreated = 1f;
                SpawnFullChargePulse();
            }
        }

        private void SpawnChargeParticles(Vector2 direction, float charge)
        {
            float chance = 0.38f + charge * 0.52f;
            if (Main.rand.NextFloat() > chance)
                return;

            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 offset = -direction * Main.rand.NextFloat(22f, 76f + charge * 36f) + side * Main.rand.NextFloat(-15f - charge * 18f, 15f + charge * 18f);
            Vector2 spawnPosition = Projectile.Center + offset;
            Vector2 pullVelocity = -offset.SafeNormalize(-direction) * Main.rand.NextFloat(2.2f, 5.6f + charge * 2.4f);
            Color particleColor = Main.rand.NextBool(4)
                ? Color.White
                : Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat(0.2f, 0.75f));

            Particle spark = Main.rand.NextBool(3)
                ? new SparkParticle(
                    spawnPosition,
                    pullVelocity,
                    false,
                    Main.rand.Next(13, 24),
                    Main.rand.NextFloat(0.55f, 1.05f) * (0.75f + charge * 0.55f),
                    particleColor)
                : new PointParticle(
                    spawnPosition,
                    pullVelocity,
                    false,
                    Main.rand.Next(15, 26),
                    Main.rand.NextFloat(0.72f, 1.18f) * (0.85f + charge * 0.4f),
                    particleColor);

            GeneralParticleHandler.SpawnParticle(spark);
        }

        private void SpawnFullChargePulse()
        {
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                NovaRed * 0.86f,
                Vector2.One,
                Projectile.rotation,
                0.08f,
                1.05f,
                24));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.White * 0.55f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.08f,
                0.78f,
                18));

            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 28f).ToRotationVector2() * Main.rand.NextFloat(5.6f, 8.4f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    velocity,
                    0,
                    Main.rand.NextBool(3) ? Color.White : NovaRed,
                    Main.rand.NextFloat(1.05f, 1.45f));
                dust.noGravity = true;
                dust.fadeIn = 1.2f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float charge = MathHelper.Clamp(Charge, 0f, 1f);
            if (charge <= 0.02f || Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color red = (NovaRed with { A = 0 }) * charge;
            Color white = (Color.White with { A = 0 }) * charge;
            float pulse = 0.88f + 0.16f * (float)Math.Sin(Timer * 0.16f);
            float chargeScale = 0.35f + charge * 1.45f;

            PFLeftEffectRules.BeginAdditive();

            for (int i = 0; i < 5; i++)
            {
                Vector2 randomOffset = Main.rand.NextVector2Circular(5f, 5f) * charge;
                Vector2 smearPosition = drawPosition + randomOffset - direction * Main.rand.NextFloat(16f, 48f + charge * 42f);
                Color smearColor = Color.Lerp(NovaRed, Color.White, Main.rand.NextFloat(0.04f, 0.22f)) with { A = 0 };
                Main.EntitySpriteDraw(
                    smear,
                    smearPosition,
                    null,
                    smearColor * (0.36f + charge * 0.34f),
                    direction.ToRotation() - MathHelper.PiOver2,
                    new Vector2(smear.Width * 0.5f, smear.Height),
                    new Vector2(0.08f + charge * 0.18f, 0.12f + charge * 0.12f),
                    SpriteEffects.None,
                    0);
            }

            for (int i = 0; i < 3; i++)
            {
                Color bloomColor = Color.Lerp(red, white, i * 0.22f);
                float scale = (0.18f + chargeScale * (0.18f - i * 0.035f)) * pulse;
                Main.EntitySpriteDraw(bloom, drawPosition, null, bloomColor * (0.72f - i * 0.12f), Projectile.rotation + Main.rand.NextFloat(-0.3f, 0.3f), bloom.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(ring, drawPosition, null, red * (0.28f + charge * 0.42f), Projectile.rotation + Timer * 0.035f, ring.Size() * 0.5f, (0.16f + charge * 0.46f) * pulse, SpriteEffects.None, 0);

            for (int i = 0; i < 6; i++)
            {
                float rotation = Projectile.rotation + MathHelper.TwoPi * i / 6f + Timer * (0.018f + i * 0.002f);
                Main.EntitySpriteDraw(star, drawPosition, null, Color.Lerp(red, white, 0.28f) * 0.46f, rotation, star.Size() * 0.5f, new Vector2(0.12f + charge * 0.08f, 0.85f + charge * 2.35f) * pulse, SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PristineFuryRightNovaFireball : ModProjectile, ILocalizedModType
    {
        private static readonly Color NovaRed = new(255, 54, 42);
        private static readonly Color NovaOrange = new(255, 126, 42);

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, NovaRed.ToVector3() * 0.92f);

            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Main.rand.NextBool(2))
            {
                Particle flame = new MediumMistParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(8f, 22f) + Main.rand.NextVector2Circular(6f, 6f),
                    -direction.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.8f, 2.4f),
                    Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat(0.2f, 0.75f)),
                    Color.Black,
                    Main.rand.NextFloat(0.45f, 0.9f),
                    Main.rand.Next(18, 32),
                    Main.rand.NextFloat(-0.08f, 0.08f));
                GeneralParticleHandler.SpawnParticle(flame);
            }

            if (Main.rand.NextBool(3))
            {
                Particle spark = new CustomSpark(
                    Projectile.Center - direction * 12f + Main.rand.NextVector2Circular(5f, 5f),
                    -direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(1.4f, 3.6f),
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.18f, 0.32f),
                    Main.rand.NextBool(4) ? Color.White : NovaRed,
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.7f);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 300);

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeImpact") { Volume = 0.75f, PitchVariance = 0.18f }, Projectile.Center);

            if (Main.myPlayer == Projectile.owner)
            {
                int explosion = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PristineFuryImpactExplosion>(),
                    Math.Max(1, (int)(Projectile.damage * 0.62f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    150f);
                PFLeftEffectRules.ApplyTheme(explosion, (PristineFuryMark)(int)Projectile.ai[2]);

                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                float baseRotation = forward.ToRotation();
                int laserDamage = Math.Max(1, (int)(Projectile.damage * 0.42f));
                for (int group = 0; group < 4; group++)
                {
                    float groupRotation = baseRotation + group * MathHelper.PiOver2;
                    for (int beam = 0; beam < 3; beam++)
                    {
                        float spread = MathHelper.ToRadians((beam - 1) * 10f);
                        Vector2 velocity = (groupRotation + spread).ToRotationVector2() * 5.9f;
                        int laser = Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center,
                            velocity,
                            ModContent.ProjectileType<PristineFuryRightNovaPseudoLaser>(),
                            laserDamage,
                            Projectile.knockBack * 0.35f,
                            Projectile.owner,
                            group,
                            beam);
                        PFLeftEffectRules.ApplyTheme(laser, (PristineFuryMark)(int)Projectile.ai[2]);
                    }
                }
            }

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                NovaRed * 0.9f,
                Vector2.One,
                Projectile.rotation,
                0.18f,
                1.65f,
                22));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.White * 0.55f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.05f,
                1.35f,
                20,
                true));

            for (int i = 0; i < 34; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f);
                Particle spark = new SparkParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(14, 26),
                    Main.rand.NextFloat(0.75f, 1.4f),
                    Main.rand.NextBool(5) ? Color.White : Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat()));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fade = Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Color red = (NovaRed with { A = 0 }) * fade;
            Color white = (Color.White with { A = 0 }) * fade;

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, oldDrawPosition, null, red * (0.22f * (1f - completion)), Projectile.rotation, bloom.Size() * 0.5f, 0.2f * (1f - completion), SpriteEffects.None, 0);
            }

            float pulse = 0.9f + 0.12f * (float)Math.Sin(Timer * 0.2f);
            Main.EntitySpriteDraw(bloom, drawPosition, null, red * 0.88f, Projectile.rotation, bloom.Size() * 0.5f, 0.45f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, white * 0.42f, Projectile.rotation, bloom.Size() * 0.5f, 0.22f * pulse, SpriteEffects.None, 0);

            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver2 * i + Timer * 0.05f;
                Main.EntitySpriteDraw(star, drawPosition, null, Color.Lerp(red, white, 0.18f) * 0.52f, rotation, star.Size() * 0.5f, new Vector2(0.16f, 1.45f) * pulse, SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PristineFuryRightNovaPseudoLaser : ModProjectile, ILocalizedModType
    {
        private static readonly Color NovaRed = new(255, 54, 42);
        private static readonly Color NovaOrange = new(255, 126, 42);
        private const float MaxBeamLength = 520f;
        private const float CollisionWidth = 18f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BeamLength => ref Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();
            BeamLength = MathHelper.Lerp(BeamLength, MaxBeamLength, 0.38f);
            Vector2 beamEnd = Projectile.Center + direction * BeamLength;
            Lighting.AddLight(Projectile.Center, NovaRed.ToVector3() * 0.72f);
            DelegateMethods.v3_1 = NovaRed.ToVector3() * 0.46f;
            Utils.PlotTileLine(Projectile.Center, beamEnd, CollisionWidth, DelegateMethods.CastLight);

            if (Main.dedServ)
                return;

            if ((int)Timer % 2 == 0)
            {
                Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(BeamLength);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    position + Main.rand.NextVector2Circular(4f, 4f),
                    direction.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.8f, 2.8f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.72f, 1.18f),
                    Main.rand.NextBool(5) ? Color.White : Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat()),
                    true));
            }

            if ((int)Timer % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    beamEnd + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.Torch,
                    -direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.4f, 4.2f),
                    80,
                    Main.rand.NextBool(4) ? Color.White : NovaRed,
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 180);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + direction * BeamLength, CollisionWidth, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color red = NovaRed with { A = 0 };
            Color orange = NovaOrange with { A = 0 };
            float fade = Utils.GetLerpValue(0f, 9f, Projectile.timeLeft, true) * Utils.GetLerpValue(30f, 25f, Projectile.timeLeft, true);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = Projectile.Center + direction * BeamLength - Main.screenPosition;
            Vector2 midpoint = (start + end) * 0.5f;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(line, midpoint, null, red * (0.92f * fade), direction.ToRotation() + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.34f, BeamLength / line.Height), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(line, midpoint, null, (Color.White with { A = 0 }) * (0.64f * fade), direction.ToRotation() + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.13f, BeamLength / line.Height), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, start, null, red * (0.86f * fade), Projectile.rotation, bloom.Size() * 0.5f, 0.28f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, orange * (0.9f * fade), Projectile.rotation, bloom.Size() * 0.5f, 0.34f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, (Color.White with { A = 0 }) * (0.52f * fade), Projectile.rotation, bloom.Size() * 0.5f, 0.14f, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PristineFuryGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        internal int PlagueRelease;

        public override void ResetEffects(NPC npc)
        {
            if (PlagueRelease > 0)
                PlagueRelease--;
        }

        public override void OnKill(NPC npc)
        {
            if (PlagueRelease <= 0)
                return;
            Player owner = Main.LocalPlayer;
            for (int i = 0; i < 5; i++)
                Projectile.NewProjectile(npc.GetSource_Death(), npc.Center, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 8f), ModContent.ProjectileType<PFGoliath_Flame>(), 30, 0f, owner.whoAmI, 1f, i);
        }
    }
}
