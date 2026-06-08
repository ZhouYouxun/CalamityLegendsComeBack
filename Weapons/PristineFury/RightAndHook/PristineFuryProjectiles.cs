using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal static class PristineFuryRightNovaVisuals
    {
        public static void DrawArcNovaOrb(Vector2 center, Vector2 direction, float rotation, float timer, float intensity, float size, Color mainColor, Color accentColor)
        {
            if (Main.dedServ || intensity <= 0.01f)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/FullStar").Value;
            direction = direction.SafeNormalize(Vector2.UnitX);

            Vector2 drawPosition = center - Main.screenPosition;
            Color main = mainColor with { A = 0 };
            Color accent = accentColor with { A = 0 };
            Color white = Color.White with { A = 0 };
            float pulse = 0.92f + 0.08f * (float)Math.Sin(timer * 0.19f);
            float drawSize = size * intensity;

            PFLeftEffectRules.BeginAdditive();

            for (int i = 0; i < 4; i++)
            {
                Vector2 sparkOffset = Main.rand.NextVector2Circular(1f, 5.5f) * Math.Min(intensity, 1f);
                Vector2 smearPosition = drawPosition + sparkOffset - Vector2.Lerp(sparkOffset, -direction, 0.9f) * Main.rand.NextFloat(15f, 35f) - direction * MathHelper.Lerp(16f, 34f, intensity);
                Color smearColor = Main.rand.NextBool(3) ? accent : main;
                Main.EntitySpriteDraw(
                    smear,
                    smearPosition,
                    null,
                    smearColor * 0.72f * intensity,
                    direction.RotatedByRandom(0.3f).ToRotation() - MathHelper.PiOver2,
                    new Vector2(smear.Width * 0.5f, smear.Height),
                    new Vector2(0.008f + 0.024f * drawSize, 0.05f + 0.1f * intensity),
                    SpriteEffects.None,
                    0f);
            }

            for (int i = 0; i < 3; i++)
            {
                Color bloomColor = Color.Lerp(main, white, i * 0.25f);
                Main.EntitySpriteDraw(
                    bloom,
                    drawPosition,
                    null,
                    bloomColor * (0.86f - i * 0.16f) * intensity,
                    Main.rand.NextFloat(-5f, 5f),
                    bloom.Size() * 0.5f,
                    new Vector2(1.35f, 1f) * drawSize * pulse * (0.14f - i * 0.03f),
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                accent * 0.58f * intensity,
                rotation + timer * 0.045f,
                ring.Size() * 0.5f,
                new Vector2(0.56f, 1.35f) * drawSize * 0.15f * pulse,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 3; i++)
            {
                Vector2 orbit = (MathHelper.TwoPi * i / 3f + timer * 0.18f).ToRotationVector2();
                Vector2 offset = new Vector2(orbit.X * 0.7f, orbit.Y * 1.2f).RotatedBy(rotation) * drawSize * 8f;
                Main.EntitySpriteDraw(
                    bloom,
                    drawPosition + offset,
                    null,
                    Color.Lerp(accent, white, 0.55f) * 0.74f * intensity,
                    rotation,
                    bloom.Size() * 0.5f,
                    new Vector2(1f) * drawSize * 0.055f * pulse,
                    SpriteEffects.None,
                    0f);
            }

            for (int i = 0; i < 4; i++)
            {
                float starRotation = rotation + MathHelper.PiOver2 * i + timer * 0.08f;
                Main.EntitySpriteDraw(
                    star,
                    drawPosition,
                    null,
                    Color.Lerp(main, white, 0.35f) * 0.38f * intensity,
                    starRotation,
                    star.Size() * 0.5f,
                    new Vector2(0.12f, 0.52f) * drawSize * pulse,
                    SpriteEffects.None,
                    0f);
            }

            PFLeftEffectRules.EndAdditive();
        }
    }

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
        private const int HorizontalFrames = 5;
        private const int VerticalFrames = 4;
        private const int FrameLength = 2;
        private const int LaserCount = 28;
        private static readonly Color[] SupernovaColors =
        {
            new(255, 52, 42),
            new(255, 138, 42),
            new(255, 224, 92),
            new(255, 255, 255)
        };

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Rogue/SupernovaBoom";

        private ref float Timer => ref Projectile.localAI[0];
        private int frameX;
        private int frameY;
        private int currentFrame = 1;
        private bool damageDone;
        private Color variedColor = SupernovaColors[0];
        private Color mainColor = SupernovaColors[2];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 300;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 48;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            if (Projectile.ai[0] > 0f && Timer == 1f)
            {
                int size = (int)MathHelper.Clamp(Projectile.ai[0] * 2f, 160f, 520f);
                Projectile.Resize(size, size);
            }

            if ((int)Timer % 20 == 1)
                variedColor = SupernovaColors[Main.rand.Next(SupernovaColors.Length)];
            mainColor = Color.Lerp(mainColor, variedColor, 0.07f);
            Lighting.AddLight(Projectile.Center, mainColor.ToVector3() * 2.4f);

            if ((int)Timer % FrameLength == 1)
            {
                currentFrame++;
                frameY++;
                if (frameY >= VerticalFrames)
                {
                    frameX++;
                    frameY = 0;
                }

                if (frameX >= HorizontalFrames)
                    Projectile.Kill();
            }

            if (!damageDone && currentFrame >= 4)
            {
                damageDone = true;
                Projectile.Damage();
                SpawnSupernovaLasers();
            }

            if (Main.dedServ)
                return;

            if (Timer == 1f)
                SpawnExplosionEffects();

            if ((int)Timer % 3 == 0)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 10f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(12, 24),
                    Main.rand.NextFloat(0.85f, 1.65f),
                    Main.rand.NextBool(4) ? Color.White : mainColor));
            }
        }

        public override bool? CanDamage() => damageDone && currentFrame <= 13;

        private void SpawnSupernovaLasers()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int laserDamage = Math.Max(1, (int)(Projectile.damage * 0.58f));
            float rotationOffset = Projectile.ai[1];
            for (int i = 0; i < LaserCount; i++)
            {
                float rotation = rotationOffset + MathHelper.TwoPi * i / LaserCount + Main.rand.NextFloat(-0.025f, 0.025f);
                Vector2 velocity = rotation.ToRotationVector2();
                int laser = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<PristineFuryRightNovaPseudoLaser>(),
                    laserDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner,
                    i,
                    LaserCount);

                PFLeftEffectRules.ApplyTheme(laser, (PristineFuryMark)(int)Projectile.ai[2]);
            }

            // Diffuse shrapnel ring on explosion
            int shrapnelCount = 12;
            int shrapnelDamage = Math.Max(1, (int)(Projectile.damage * 0.45f));
            for (int i = 0; i < shrapnelCount; i++)
            {
                float angle = rotationOffset + MathHelper.TwoPi * i / shrapnelCount + Main.rand.NextFloat(-0.05f, 0.05f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(5.5f, 9.5f);
                int shrapnel = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<PFProvidence_HolyShrapnel>(),
                    shrapnelDamage,
                    Projectile.knockBack * 0.2f,
                    Projectile.owner,
                    Main.rand.NextFloat(0.8f, 1.2f),
                    Projectile.ai[2]);

                PFLeftEffectRules.ApplyTheme(shrapnel, (PristineFuryMark)(int)Projectile.ai[2]);
            }

            Main.player[Projectile.owner].SetScreenshake(7f);
        }

        private void SpawnExplosionEffects()
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") { Volume = 0.72f, PitchVariance = 0.12f }, Projectile.Center);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                mainColor * 0.9f,
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.18f,
                2.6f,
                24));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.White * 0.55f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.05f,
                2.2f,
                22,
                true));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                mainColor * 0.8f,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.06f,
                0.9f,
                22,
                true));

            for (int i = 0; i < 36; i++)
            {
                Particle spark = new SparkParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 13f),
                    false,
                    Main.rand.Next(16, 32),
                    Main.rand.NextFloat(0.85f, 1.8f),
                    Main.rand.NextBool(4) ? Color.White : Color.Lerp(new Color(255, 52, 42), mainColor, Main.rand.NextFloat()));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < LaserCount; i++)
            {
                float rotation = MathHelper.TwoPi * i / LaserCount + Projectile.ai[1];
                Vector2 velocity = rotation.ToRotationVector2() * Main.rand.NextFloat(5.5f, 10.5f);
                Particle burningPetal = new PointParticle(
                    Projectile.Center + rotation.ToRotationVector2() * Main.rand.NextFloat(10f, 42f),
                    velocity,
                    false,
                    Main.rand.Next(18, 32),
                    Main.rand.NextFloat(0.78f, 1.24f),
                    Main.rand.NextBool(5) ? Color.White : Color.Lerp(SupernovaColors[0], mainColor, Main.rand.NextFloat(0.2f, 0.85f)),
                    true);
                GeneralParticleHandler.SpawnParticle(burningPetal);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(HorizontalFrames, VerticalFrames, frameX, frameY);
            Vector2 origin = frame.Size() * 0.5f;
            float opacity = Utils.GetLerpValue(0f, 6f, Timer, true) * Utils.GetLerpValue(48f, 34f, Projectile.timeLeft, true);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Color color = mainColor with { A = 0 };
            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(texture, drawPosition, frame, color * 0.82f * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(texture, drawPosition, frame, (Color.White with { A = 0 }) * 0.28f * opacity, -Projectile.rotation * 0.4f, origin, Projectile.scale * 0.7f, SpriteEffects.None, 0f);
            PFLeftEffectRules.EndAdditive();
            return false;
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

            Color themeColor = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            Color glow = Color.Lerp(themeColor, Color.White, charge * 0.34f);
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
            float chance = 0.68f + charge * 0.28f;
            if (Main.rand.NextFloat() > chance)
                return;

            int holdoutIndex = (int)HoldoutIndex;
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
                return;

            Color themeColor = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 offset = -direction * Main.rand.NextFloat(22f, 76f + charge * 36f) + side * Main.rand.NextFloat(-15f - charge * 18f, 15f + charge * 18f);
            Vector2 spawnPosition = Projectile.Center + offset;
            Vector2 pullVelocity = -offset.SafeNormalize(-direction) * Main.rand.NextFloat(2.2f, 5.6f + charge * 2.4f);
            Color particleColor = Main.rand.NextBool(4)
                ? Color.White
                : Color.Lerp(themeColor, Color.Lerp(themeColor, Color.White, 0.5f), Main.rand.NextFloat(0.2f, 0.75f));

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

            if (Main.rand.NextFloat() < 0.45f + charge * 0.45f)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + direction * Main.rand.NextFloat(-5f, 8f),
                    direction * Main.rand.NextFloat(0.2f, 0.8f),
                    Color.Lerp(themeColor, Color.White, 0.18f) * (0.24f + charge * 0.42f),
                    new Vector2(0.42f, 0.88f),
                    Projectile.rotation - MathHelper.PiOver2,
                    0.05f,
                    0.16f + charge * 0.16f,
                    12));
            }
        }

        private void SpawnFullChargePulse()
        {
            int holdoutIndex = (int)HoldoutIndex;
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
                return;

            Color themeColor = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                themeColor * 0.86f,
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
                    Main.rand.NextBool(3) ? Color.White : themeColor,
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

            int holdoutIndex = (int)HoldoutIndex;
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
                return false;

            Color themeColor = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color themeColorA0 = themeColor with { A = 0 };
            Color whiteA0 = Color.White with { A = 0 };
            float pulse = 0.88f + 0.16f * (float)Math.Sin(Timer * 0.16f);
            float chargeScale = 0.25f + charge * 0.95f;

            PristineFuryRightNovaVisuals.DrawArcNovaOrb(Projectile.Center, direction, Projectile.rotation, Timer, charge, chargeScale * pulse, themeColorA0, whiteA0);
            return false;
        }
    }

    internal sealed class PristineFuryRightNovaFireball : ModProjectile, ILocalizedModType
    {
        private static readonly Color NovaRed = new(255, 54, 42);
        private static readonly Color NovaOrange = new(255, 126, 42);

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Ranged/NovaChargedShot";

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
                    -direction.RotatedByRandom(0.34f) * Main.rand.NextFloat(1.3f, 3.8f),
                    Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat(0.2f, 0.75f)),
                    Color.Black,
                    Main.rand.NextFloat(0.62f, 1.08f),
                    Main.rand.Next(22, 38),
                    Main.rand.NextFloat(-0.08f, 0.08f));
                GeneralParticleHandler.SpawnParticle(flame);
            }

            if (Main.rand.NextBool(2))
            {
                Particle spark = new CustomSpark(
                    Projectile.Center - direction * 12f + Main.rand.NextVector2Circular(5f, 5f),
                    -direction.RotatedByRandom(0.26f) * Main.rand.NextFloat(2.2f, 5.4f),
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.22f, 0.42f),
                    Main.rand.NextBool(4) ? Color.White : NovaRed,
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.7f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if ((int)Timer % 3 == 0)
            {
                Vector2 side = direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-13f, 13f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(2f, 18f) + side,
                    -direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(2.4f, 6.2f),
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.76f, 1.2f),
                    Main.rand.NextBool(5) ? Color.White : Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat()),
                    true));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 300);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, NovaRed);
            float opacity = Projectile.Opacity;

            PFLeftEffectRules.BeginAdditive();

            // Draw afterimages
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = theme * (0.45f * (1f - completion)) * opacity;
                trailColor.A = 0;
                float trailScale = Projectile.scale * MathHelper.Lerp(1.15f, 0.45f, completion);

                Main.EntitySpriteDraw(
                    texture,
                    oldDrawPosition,
                    null,
                    trailColor,
                    Projectile.oldRot[i],
                    texture.Size() * 0.5f,
                    trailScale,
                    SpriteEffects.None,
                    0);

                // Small glow behind each afterimage
                Main.EntitySpriteDraw(
                    bloom,
                    oldDrawPosition,
                    null,
                    trailColor * 0.45f,
                    Projectile.oldRot[i],
                    bloom.Size() * 0.5f,
                    trailScale * 0.65f,
                    SpriteEffects.None,
                    0);
            }

            // Draw main fireball
            Color drawColor = Color.Lerp(theme, Color.White, 0.22f) * opacity;
            drawColor.A = 0;
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                drawColor,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale * 1.15f,
                SpriteEffects.None,
                0);

            // Draw central glow
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                theme * (0.8f * opacity),
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.95f,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.White * (0.55f * opacity),
                -Projectile.rotation * 0.5f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.45f,
                SpriteEffects.None,
                0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") { Volume = 0.78f, PitchVariance = 0.16f }, Projectile.Center);

            if (Main.myPlayer == Projectile.owner)
            {
                int explosion = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PristineFuryImpactExplosion>(),
                    Math.Max(1, (int)(Projectile.damage * 0.76f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    165f,
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    Projectile.ai[2]);
                PFLeftEffectRules.ApplyTheme(explosion, (PristineFuryMark)(int)Projectile.ai[2]);
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

            for (int i = 0; i < 56; i++)
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

            for (int i = 0; i < 18; i++)
            {
                float rotation = MathHelper.TwoPi * i / 18f + Main.rand.NextFloat(-0.05f, 0.05f);
                Vector2 velocity = rotation.ToRotationVector2() * Main.rand.NextFloat(4.8f, 11.5f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + velocity.SafeNormalize(Vector2.UnitY) * 10f,
                    velocity * 0.45f,
                    Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat(0.2f, 0.85f)),
                    Color.Black,
                    Main.rand.NextFloat(0.72f, 1.22f),
                    Main.rand.Next(24, 42),
                    Main.rand.NextFloat(-0.05f, 0.05f)));
            }
        }

    }

    internal sealed class PristineFuryRightNovaPseudoLaser : ModProjectile, ILocalizedModType
    {
        private static readonly Color NovaRed = new(255, 54, 42);
        private static readonly Color NovaOrange = new(255, 126, 42);
        private const int Lifetime = 34;
        private const float MaxBeamLength = 1220f;
        private const float CollisionWidth = 28f;
        private const float MaxBeamScale = 1.9f;

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
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();
            if (Timer == 1f)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyRay") { Volume = 0.45f, Pitch = -0.15f, MaxInstances = 8 }, Projectile.Center);

            BeamLength = MathHelper.Lerp(BeamLength, MaxBeamLength, 0.72f);
            float completion = Timer / Lifetime;
            float fade = Utils.GetLerpValue(0f, 0.18f, completion, true) * Utils.GetLerpValue(1f, 0.68f, completion, true);
            Projectile.scale = MaxBeamScale * fade;
            Vector2 beamEnd = Projectile.Center + direction * BeamLength;
            Lighting.AddLight(Projectile.Center, NovaRed.ToVector3() * 0.72f);
            DelegateMethods.v3_1 = NovaRed.ToVector3() * 0.62f * fade;
            Utils.PlotTileLine(Projectile.Center, beamEnd, CollisionWidth * Math.Max(Projectile.scale, 0.1f), DelegateMethods.CastLight);

            if (Main.dedServ)
                return;

            if ((int)Timer % 2 == 0)
            {
                Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(BeamLength);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    position + Main.rand.NextVector2Circular(4f, 4f),
                    direction.RotatedByRandom(0.92f) * Main.rand.NextFloat(1.2f, 4.4f),
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.86f, 1.38f),
                    Main.rand.NextBool(5) ? Color.White : Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat()),
                    true));
            }

            if ((int)Timer % 4 == 1)
            {
                Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(BeamLength);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    position + Main.rand.NextVector2Circular(10f, 10f),
                    direction.RotatedByRandom(0.7f) * Main.rand.NextFloat(1.2f, 3.8f),
                    Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat(0.25f, 0.75f)),
                    Color.Black,
                    Main.rand.NextFloat(0.42f, 0.82f),
                    Main.rand.Next(20, 34),
                    Main.rand.NextFloat(-0.06f, 0.06f)));
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

            // Spawn SparkParticles along the laser length
            Color sparkColor = PFLeftEffectRules.GetThemeColor(Projectile, NovaRed);
            for (float offset = 0f; offset < BeamLength; offset += Main.rand.NextFloat(80f, 150f))
            {
                Vector2 sparkPos = Projectile.Center + direction * offset + Main.rand.NextVector2Circular(12f, 12f) * Projectile.scale;
                Vector2 sparkVelocity = direction * Main.rand.NextFloat(3f, 6f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    sparkPos,
                    sparkVelocity,
                    false,
                    5,
                    Main.rand.NextFloat(0.5f, 1.2f),
                    Color.Lerp(sparkColor, Color.White, Main.rand.NextFloat(0.2f, 0.5f))
                ));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
            if (Main.rand.NextBool(5))
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceBurn") { Volume = 0.25f, Pitch = 0.12f }, target.Center);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + direction * BeamLength, CollisionWidth, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.scale <= 0.03f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, NovaRed);
            float fade = Utils.GetLerpValue(0f, 9f, Projectile.timeLeft, true) * Utils.GetLerpValue(30f, 25f, Projectile.timeLeft, true);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = Projectile.Center + direction * BeamLength - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();

            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            float drawScale = Projectile.scale * 0.65f;
            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            Vector2 scaleVec = new Vector2(drawScale, drawScale);

            // Draw start piece
            Main.spriteBatch.Draw(startTex, start, null, theme * fade, rotation, startTex.Size() / 2f, scaleVec, SpriteEffects.None, 0f);

            float currentLength = BeamLength;
            currentLength -= (startTex.Height / 2 + endTex.Height) * drawScale;
            Vector2 center = Projectile.Center + direction * drawScale * startTex.Height / 2f;

            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                int frameHeight = 36;
                int frameY = frameHeight * (Projectile.timeLeft / 3 % 4);
                Rectangle sourceRect = new Rectangle(0, frameY, midTex.Width, frameHeight);

                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                    {
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);
                    }
                    if (sourceRect.Height <= 0)
                        break;

                    Main.spriteBatch.Draw(midTex, center - Main.screenPosition, sourceRect, theme * fade, rotation, new Vector2(sourceRect.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);
                    lengthDrawn += sourceRect.Height * drawScale;
                    center += direction * sourceRect.Height * drawScale;

                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                    {
                        sourceRect.Y = 0;
                    }
                }
            }

            Vector2 endPos = center - Main.screenPosition;
            Main.spriteBatch.Draw(endTex, endPos, null, theme * fade, rotation, new Vector2(endTex.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);

            // Origin glow overlays
            Main.EntitySpriteDraw(bloom, start, null, theme * (0.86f * fade), 0f, bloom.Size() * 0.5f, 0.45f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloomRing, start, null, theme * (0.65f * fade), 0f, bloomRing.Size() * 0.5f, 0.65f * drawScale, SpriteEffects.None, 0);

            // End glow overlays
            Main.EntitySpriteDraw(bloom, end, null, theme * (0.9f * fade), 0f, bloom.Size() * 0.5f, 0.5f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, (Color.White with { A = 0 }) * (0.52f * fade), 0f, bloom.Size() * 0.5f, 0.2f * drawScale, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * BeamLength, Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
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
