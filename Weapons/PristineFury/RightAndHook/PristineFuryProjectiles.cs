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
                SpawnTrackers();
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

        private void SpawnTrackers()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int starDamage = Math.Max(1, (int)(Projectile.damage * 0.5f));
            for (int i = 0; i < 7; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 9f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<PristineFuryHomingStar>(),
                    starDamage,
                    Projectile.knockBack * 0.3f,
                    Projectile.owner);
            }

            Main.player[Projectile.owner].SetScreenshake(5f);
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
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Charge > 300f ? true : false;

        public override void AI()
        {
            Timer++;
            int holdoutIndex = (int)HoldoutIndex;
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
            {
                Projectile.Kill();
                return;
            }

            float chargeRatio = MathHelper.Clamp(Charge / 120f, 0f, 1f);
            Vector2 direction = holdout.AimDirection;

            Vector2 vibrationOffset = Vector2.Zero;
            if (Charge > 300f)
            {
                float vibProgress = Math.Min(1f, (Charge - 300f) / 60f);
                float maxVib = 6f;
                vibrationOffset = Main.rand.NextVector2Circular(maxVib * vibProgress, maxVib * vibProgress);

                Projectile.friendly = true;
                Projectile.width = Projectile.height = 28; // Reduced size by 75%
                Projectile.damage = holdout.GetRightScaledDamage(PF_Balance.GetRightOverheatContactDamageMultiplier());
            }
            else
            {
                Projectile.friendly = false;
                Projectile.width = Projectile.height = 2;
            }

            Projectile.Center = holdout.GunTipPosition + direction * (6f + chargeRatio * 5f) + vibrationOffset;
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();
            Projectile.timeLeft = 2;

            Color themeColor = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            Color glow = Color.Lerp(themeColor, Color.White, chargeRatio * 0.34f);
            Lighting.AddLight(Projectile.Center, glow.ToVector3() * (0.35f + chargeRatio * 1.25f));

            if (Main.dedServ)
                return;

            SpawnChargeParticles(direction, Charge);
            if (Charge >= 120f && FullChargePulseCreated == 0f)
            {
                FullChargePulseCreated = 1f;
                SpawnFullChargePulse();
            }
        }

        private void SpawnChargeParticles(Vector2 direction, float chargeTimer)
        {
            float chargeRatio = MathHelper.Clamp(chargeTimer / 120f, 0f, 1f);
            float chance = 0.68f + chargeRatio * 0.28f;
            if (chargeTimer > 300f)
                chance = 1f;

            if (Main.rand.NextFloat() > chance)
                return;

            int holdoutIndex = (int)HoldoutIndex;
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
                return;

            Color themeColor = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            float pullOffsetBase = 76f + chargeRatio * 36f;
            if (chargeTimer > 300f)
                pullOffsetBase += 30f;

            Vector2 offset = -direction * Main.rand.NextFloat(22f, pullOffsetBase) + side * Main.rand.NextFloat(-15f - chargeRatio * 18f, 15f + chargeRatio * 18f);
            Vector2 spawnPosition = Projectile.Center + offset;
            Vector2 pullVelocity = -offset.SafeNormalize(-direction) * Main.rand.NextFloat(2.2f, 5.6f + chargeRatio * 2.4f);
            Color particleColor = Main.rand.NextBool(4)
                ? Color.White
                : Color.Lerp(themeColor, Color.Lerp(themeColor, Color.White, 0.5f), Main.rand.NextFloat(0.2f, 0.75f));

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                spawnPosition,
                pullVelocity,
                false,
                Main.rand.Next(16, 28),
                Main.rand.NextFloat(0.32f, 0.68f) * (0.75f + chargeRatio * 0.55f),
                particleColor,
                true,
                false,
                true));

            if (Main.rand.NextFloat() < 0.55f + chargeRatio * 0.35f)
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    spawnPosition + Main.rand.NextVector2Circular(4f, 4f),
                    pullVelocity * 0.55f,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.42f, 0.85f) * (0.7f + chargeRatio * 0.45f),
                    Main.rand.NextBool(3) ? Color.White : particleColor,
                    true));
            }

            if (chargeTimer > 300f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 extraVel = Main.rand.NextVector2Circular(5f, 5f) - direction * Main.rand.NextFloat(1f, 4f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                        extraVel,
                        false,
                        Main.rand.Next(14, 22),
                        Main.rand.NextFloat(0.45f, 0.85f),
                        Color.Lerp(themeColor, Color.White, Main.rand.NextFloat(0.3f, 0.9f)),
                        true, false, true));
                }
            }

            if (Main.rand.NextFloat() < 0.45f + chargeRatio * 0.45f)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + direction * Main.rand.NextFloat(-5f, 8f),
                    direction * Main.rand.NextFloat(0.2f, 0.8f),
                    Color.Lerp(themeColor, Color.White, 0.18f) * (0.24f + chargeRatio * 0.42f),
                    new Vector2(0.42f, 0.88f),
                    Projectile.rotation - MathHelper.PiOver2,
                    0.05f,
                    0.16f + chargeRatio * 0.16f,
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
            int holdoutIndex = (int)HoldoutIndex;
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
                return false;

            float chargeTimer = Charge;
            float chargeRatio = MathHelper.Clamp(chargeTimer / 120f, 0f, 1f);
            if (chargeTimer <= 0.02f || Main.dedServ)
                return false;

            Color themeColor = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color themeColorA0 = themeColor with { A = 0 };
            Color whiteA0 = Color.White with { A = 0 };
            float pulse = 0.88f + 0.16f * (float)Math.Sin(Timer * 0.16f);
            float chargeScale = 0.25f + chargeRatio * 0.95f;

            PristineFuryRightNovaVisuals.DrawArcNovaOrb(Projectile.Center, direction, Projectile.rotation, Timer, chargeRatio, chargeScale * pulse, themeColorA0, whiteA0);

            if (chargeTimer > 180f)
            {
                float progress3to5 = MathHelper.Clamp((chargeTimer - 180f) / 120f, 0f, 1f);
                Texture2D holyBlastTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/HolyBlast").Value;

                int frames = 4;
                if (ModContent.TryFind<ModProjectile>("CalamityMod", "HolyBlast", out var holyBlastProj))
                {
                    frames = Main.projFrames[holyBlastProj.Type];
                }
                int frameHeight = holyBlastTexture.Height / frames;
                int currentFrame = (int)(Timer / 5f) % frames;
                Rectangle sourceRect = new Rectangle(0, currentFrame * frameHeight, holyBlastTexture.Width, frameHeight);
                Vector2 origin = sourceRect.Size() * 0.5f;

                float holyBlastOpacity = MathHelper.Lerp(0f, 0.75f, progress3to5);
                Color drawColor = Color.White * (holyBlastOpacity * Projectile.Opacity);
                float drawScale = 0.3f; // Reduced size by 75%

                // Draw red outline
                Color outlineColor = Color.Red * (holyBlastOpacity * Projectile.Opacity);
                outlineColor.A = 0;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 outlineOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2f;
                    Main.EntitySpriteDraw(
                        holyBlastTexture,
                        Projectile.Center - Main.screenPosition + outlineOffset,
                        sourceRect,
                        outlineColor,
                        Projectile.rotation,
                        origin,
                        drawScale,
                        SpriteEffects.None,
                        0
                    );
                }

                Main.EntitySpriteDraw(
                    holyBlastTexture,
                    Projectile.Center - Main.screenPosition,
                    sourceRect,
                    drawColor,
                    Projectile.rotation,
                    origin,
                    drawScale,
                    SpriteEffects.None,
                    0
                );
            }
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
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 3; // Processing speed multiplied by 2x (runs 4 times per frame)
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => Projectile.ai[0] == 0f ? null : false;

        public override void AI()
        {
            Timer++;

            if (Timer == 1)
            {
                // 记录发射方向，供 SpawnLilyBurst 判断激光朝向
                Projectile.localAI[1] = Projectile.velocity.ToRotation();

                float chargeLevel = Projectile.ai[1];
                if (chargeLevel == 1f)
                {
                    Projectile.scale = 0.6f;
                    Projectile.width = Projectile.height = (int)(42 * 0.6f);
                }
                else if (chargeLevel == 2f)
                {
                    Projectile.scale = 1.0f;
                    Projectile.width = Projectile.height = 42;
                }
                else if (chargeLevel == 3f)
                {
                    Projectile.scale = 0.35f; // Reduced size by 75%
                    Projectile.width = Projectile.height = (int)(42 * 0.35f);
                }
            }

            if (Projectile.ai[0] == 0f)
            {
                Projectile.velocity *= 0.99f;
            }
            else if (Projectile.ai[0] == 1f)
            {
                SpawnLilyBurst();
                Projectile.ai[0] = 2f;
                Projectile.velocity = Vector2.Zero;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 36);
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, NovaRed.ToVector3() * (Projectile.ai[0] == 0f ? 0.92f : 0.35f));

            if (Main.dedServ || Projectile.ai[0] >= 2f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Main.rand.NextBool(2))
            {
                Particle flame = new MediumMistParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(8f, 22f) + Main.rand.NextVector2Circular(6f, 6f),
                    -direction.RotatedByRandom(0.34f) * Main.rand.NextFloat(1.3f, 3.8f),
                    Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat(0.2f, 0.75f)),
                    Color.Black,
                    Main.rand.NextFloat(0.62f, 1.08f) * Projectile.scale,
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
                    Main.rand.NextFloat(0.22f, 0.42f) * Projectile.scale,
                    Main.rand.NextBool(4) ? Color.White : NovaRed,
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.7f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if ((int)Timer % 3 == 0)
            {
                Vector2 side = direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-13f, 13f) * Projectile.scale;
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(2f, 18f) * Projectile.scale + side,
                    -direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(2.4f, 6.2f),
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.76f, 1.2f) * Projectile.scale,
                    Main.rand.NextBool(5) ? Color.White : Color.Lerp(NovaRed, NovaOrange, Main.rand.NextFloat()),
                    true));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.ai[0] == 0f)
            {
                Projectile.ai[0] = 1f;
                Projectile.velocity = Vector2.Zero;
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 300);
            if (Projectile.ai[0] == 0f)
                Projectile.ai[0] = 1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, NovaRed);
            float opacity = Projectile.Opacity;

            int frames = 1;
            int currentFrame = 0;
            bool isHeavy = Projectile.ai[1] == 3f;
            if (isHeavy)
            {
                texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/HolyBlast").Value;
                frames = 4;
                if (ModContent.TryFind<ModProjectile>("CalamityMod", "HolyBlast", out var holyBlastProj))
                {
                    frames = Main.projFrames[holyBlastProj.Type];
                }
                currentFrame = (int)(Timer / 5f) % frames;
            }

            int frameHeight = texture.Height / frames;
            Rectangle sourceRect = new Rectangle(0, currentFrame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() * 0.5f;

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

                float oldRot = Projectile.oldRot[i];
                if (!isHeavy)
                    oldRot += MathHelper.PiOver2; // Rotate core texture by 90 degrees for lower tiers

                Main.EntitySpriteDraw(
                    texture,
                    oldDrawPosition,
                    sourceRect,
                    trailColor,
                    oldRot,
                    origin,
                    trailScale,
                    SpriteEffects.None,
                    0);

                // Small glow behind each afterimage
                Main.EntitySpriteDraw(
                    bloom,
                    oldDrawPosition,
                    null,
                    trailColor * 0.45f,
                    oldRot,
                    bloom.Size() * 0.5f,
                    trailScale * 0.65f,
                    SpriteEffects.None,
                    0);
            }

            // Draw red outline if it is the heavy fireball
            if (isHeavy)
            {
                float outlineScale = Projectile.scale * 1.15f;
                Color outlineColor = Color.Red * opacity;
                outlineColor.A = 0;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 outlineOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2f;
                    Main.EntitySpriteDraw(
                        texture,
                        drawPosition + outlineOffset,
                        sourceRect,
                        outlineColor,
                        Projectile.rotation,
                        origin,
                        outlineScale,
                        SpriteEffects.None,
                        0);
                }
            }

            // Draw main fireball
            Color drawColor = Color.Lerp(theme, Color.White, 0.22f) * opacity;
            drawColor.A = 0;
            float drawRotation = Projectile.rotation;
            if (!isHeavy)
                drawRotation += MathHelper.PiOver2; // Rotate core texture by 90 degrees for lower tiers

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                sourceRect,
                drawColor,
                drawRotation,
                origin,
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

            // === Providence / Wrath-of-Gods orbital effects (8-weapon inspired) ===
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D halfStar  = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D fullStar  = ModContent.Request<Texture2D>("CalamityMod/Particles/FullStar").Value;
            Texture2D smear     = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;

            float chargeLevel = Projectile.ai[1];
            float visualScale = chargeLevel == 1f ? 0.55f : (chargeLevel == 3f ? 1.45f : 1.0f);
            float pulse       = 0.82f + 0.18f * (float)Math.Sin(Timer * 0.19f);

            Color gold = new Color(255, 200, 100) with { A = 0 };
            Color holy = new Color(255, 240, 180) with { A = 0 };
            Color wht  = Color.White with { A = 0 };

            // [Stratus Sphere] 四层同心光晕
            float[] bloomLayers = { 0.62f, 0.40f, 0.25f, 0.13f };
            for (int i = 0; i < bloomLayers.Length; i++)
            {
                Color bc = (i % 2 == 0 ? gold : holy) * (opacity * (0.92f - i * 0.17f));
                Main.EntitySpriteDraw(bloom, drawPosition, null, bc, 0f, bloom.Size() * 0.5f, bloomLayers[i] * visualScale * pulse, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(bloom, drawPosition, null, wht * (0.65f * opacity), 0f, bloom.Size() * 0.5f, 0.09f * visualScale, SpriteEffects.None, 0);

            // [Warloks' Moon Fist] 脉冲环
            float moonPulse = 0.78f + 0.22f * (float)Math.Sin(Timer * 0.22f);
            Main.EntitySpriteDraw(bloomRing, drawPosition, null, gold * (0.72f * opacity * moonPulse), Timer * 0.04f, bloomRing.Size() * 0.5f, new Vector2(0.44f, 0.44f) * visualScale * moonPulse, SpriteEffects.None, 0);

            // [Crescent Moon] 非对称压缩环
            Main.EntitySpriteDraw(bloomRing, drawPosition, null, holy * (0.50f * opacity), Projectile.rotation + Timer * 0.038f, bloomRing.Size() * 0.5f, new Vector2(0.30f, 0.62f) * visualScale, SpriteEffects.None, 0);

            // [Sirius] 4条向外发散的明亮星芒
            for (int i = 0; i < 4; i++)
            {
                float starAngle = Projectile.rotation + MathHelper.PiOver2 * i + Timer * 0.055f;
                Main.EntitySpriteDraw(fullStar, drawPosition, null, wht * (0.55f * opacity * pulse), starAngle, fullStar.Size() * 0.5f, new Vector2(0.04f, 0.58f) * visualScale, SpriteEffects.None, 0);
            }

            // [Alpha Draconis] 4条曲线羽毛拖尾
            for (int i = 0; i < 4; i++)
            {
                float featherAngle = Projectile.rotation + MathHelper.PiOver2 * i + Timer * 0.068f;
                Vector2 featherOff = featherAngle.ToRotationVector2() * visualScale * 10f;
                Main.EntitySpriteDraw(smear, drawPosition - featherOff, null, gold * (0.44f * opacity), featherAngle - MathHelper.PiOver2, new Vector2(smear.Width * 0.5f, smear.Height), new Vector2(0.009f, 0.14f * visualScale), SpriteEffects.None, 0);
            }

            // [Galileo Gladius] 3个轨道迷你光球
            for (int i = 0; i < 3; i++)
            {
                float orbitAngle = MathHelper.TwoPi * i / 3f + Timer * 0.13f;
                Vector2 orbitPos = drawPosition + orbitAngle.ToRotationVector2() * visualScale * 26f;
                Main.EntitySpriteDraw(bloom, orbitPos, null, gold * (0.75f * opacity), 0f, bloom.Size() * 0.5f, visualScale * 0.10f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(halfStar, orbitPos, null, wht * (0.55f * opacity), orbitAngle + Timer * 0.09f, halfStar.Size() * 0.5f, new Vector2(0.05f, 0.20f), SpriteEffects.None, 0);
            }

            // [Vega] 6颗闪烁小星，距离各异
            for (int i = 0; i < 6; i++)
            {
                float tinyAngle  = MathHelper.TwoPi * i / 6f + Timer * (i % 2 == 0 ? 0.16f : -0.11f);
                float tinyRadius = visualScale * (20f + i * 4.5f);
                Vector2 tinyPos  = drawPosition + tinyAngle.ToRotationVector2() * tinyRadius;
                float twinkle    = 0.55f + 0.45f * (float)Math.Sin(Timer * 0.30f + i * 1.2f);
                Main.EntitySpriteDraw(halfStar, tinyPos, null, holy * (0.40f * opacity * twinkle), tinyAngle, halfStar.Size() * 0.5f, new Vector2(0.04f, 0.15f), SpriteEffects.None, 0);
            }

            // [Halley's Inferno] 彗星金光叠加在拖尾上
            for (int i = 0; i < Projectile.oldPos.Length; i += 2)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float trailComp    = i / (float)Projectile.oldPos.Length;
                Vector2 oldDrawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, oldDrawPos, null, gold * (0.28f * (1f - trailComp) * opacity), 0f, bloom.Size() * 0.5f, visualScale * 0.28f * (1f - trailComp), SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] < 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") { Volume = 0.38f, Pitch = 0.1f }, Projectile.Center);
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f);
                        GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, vel, false, Main.rand.Next(10, 18), Main.rand.NextFloat(0.6f, 1.1f), Main.rand.NextBool(4) ? Color.White : NovaRed));
                    }
                }
            }
        }

        private void SpawnLilyBurst()
        {
            Color themeColor = PFLeftEffectRules.GetThemeColor(Projectile, NovaRed);
            float chargeLevel = Projectile.ai[1];

            if (chargeLevel == 1f) // Small
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = 0.1f }, Projectile.Center);
                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.Lerp(themeColor, NovaRed, 0.45f) * 0.8f, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.3f, 1.5f, 20));
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.White * 0.5f, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.1f, 1.2f, 20, true));
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8f);
                        GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, vel, false, Main.rand.Next(12, 24), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.NextBool(4) ? Color.White : Color.Lerp(NovaRed, themeColor, Main.rand.NextFloat())));
                    }
                }
                return;
            }

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") { Volume = chargeLevel == 3f ? 0.92f : 0.75f, PitchVariance = 0.12f }, Projectile.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyRay") { Volume = chargeLevel == 3f ? 0.66f : 0.4f, Pitch = -0.38f, MaxInstances = 4 }, Projectile.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserBigShot") { Volume = chargeLevel == 3f ? 0.52f : 0.35f, Pitch = -0.22f, PitchVariance = 0.1f }, Projectile.Center);

            if (!Main.dedServ)
            {
                float pulseRingFinalScale = chargeLevel == 3f ? 3.4f : 2.2f;
                float customPulseFinalScale = chargeLevel == 3f ? 2.2f : 1.5f;
                int sparkCount = chargeLevel == 3f ? 76 : 40;

                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.Lerp(themeColor, NovaRed, 0.45f) * 0.95f, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.25f, pulseRingFinalScale, 32));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.White * 0.72f, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.06f, customPulseFinalScale, 30, true));
                
                int pulseCount = chargeLevel == 3f ? 3 : 2;
                for (int k = 0; k < pulseCount; k++)
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(themeColor, Color.White, 0.25f) * (0.76f - k * 0.12f), "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.08f + k * 0.03f, 1.15f + k * 0.18f, 22 + k * 5, true));

                for (int i = 0; i < sparkCount; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 16f);
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, vel, false, Main.rand.Next(18, 42), Main.rand.NextFloat(0.88f, 1.95f), Main.rand.NextBool(4) ? Color.White : Color.Lerp(NovaRed, themeColor, Main.rand.NextFloat())));
                }

                // 视觉粒子条纹：跟随实际激光方向
                Vector2 forward = Projectile.localAI[1].ToRotationVector2();
                Vector2[] visualDirs = chargeLevel >= 3f
                    ? new[] {
                        (forward.ToRotation() + MathHelper.PiOver2).ToRotationVector2(),
                        (forward.ToRotation() + MathHelper.PiOver2 + MathHelper.TwoPi / 3f).ToRotationVector2(),
                        (forward.ToRotation() + MathHelper.PiOver2 + MathHelper.TwoPi * 2f / 3f).ToRotationVector2()
                    }
                    : new[] { forward };

                foreach (Vector2 vDir in visualDirs)
                {
                    int streakCount = chargeLevel >= 3f ? 20 : 12;
                    for (int j = 0; j < streakCount; j++)
                    {
                        float t = j / (float)streakCount;
                        Vector2 vel = vDir * Main.rand.NextFloat(7f + t * 9f, 11f + t * 14f);
                        GeneralParticleHandler.SpawnParticle(new PointParticle(
                            Projectile.Center + vDir * Main.rand.NextFloat(6f, 50f),
                            vel, false, Main.rand.Next(22, 42),
                            Main.rand.NextFloat(0.85f, 1.42f),
                            Main.rand.NextBool(5) ? Color.White : Color.Lerp(NovaRed, themeColor, Main.rand.NextFloat(0.2f, 0.9f)), true));
                    }
                }
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            Main.player[Projectile.owner].SetScreenshake(chargeLevel == 3f ? 10f : 6f);
            PristineFuryMark mark = (PristineFuryMark)(int)Projectile.ai[2];

            float damageMult = chargeLevel == 3f ? 0.90f : 0.70f;
            float waveletDamageMult = chargeLevel == 3f ? 0.58f : 0.45f;
            int mainBeamDamage = Math.Max(1, (int)(Projectile.damage * damageMult));
            int waveletDamage = Math.Max(1, (int)(Projectile.damage * waveletDamageMult));

            // 激光方向：最高级三道往旁边，次高级一道往正前方
            Vector2 fwd = Projectile.localAI[1].ToRotationVector2();
            Vector2[] beamDirs = chargeLevel >= 3f
                ? new[] {
                    (fwd.ToRotation() + MathHelper.PiOver2).ToRotationVector2(),
                    (fwd.ToRotation() + MathHelper.PiOver2 + MathHelper.TwoPi / 3f).ToRotationVector2(),
                    (fwd.ToRotation() + MathHelper.PiOver2 + MathHelper.TwoPi * 2f / 3f).ToRotationVector2()
                }
                : new[] { fwd };

            foreach (Vector2 beamDir in beamDirs)
            {
                int beam = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, beamDir, ModContent.ProjectileType<PristineFuryLilyMainBeam>(), mainBeamDamage, Projectile.knockBack * 0.55f, Projectile.owner);
                PFLeftEffectRules.ApplyTheme(beam, mark);

                if (chargeLevel < 3f)
                {
                    int waveletCount = Main.rand.Next(3, 6);
                    for (int j = 0; j < waveletCount; j++)
                    {
                        float spread = Main.rand.NextFloat(-0.52f, 0.52f);
                        Vector2 wDir = beamDir.RotatedBy(spread);
                        float freq = Main.rand.NextFloat(0.09f, 0.16f);
                        float wAmp = Main.rand.NextFloat(48f, 88f);
                        int wavelet = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, wDir * Main.rand.NextFloat(13f, 20f), ModContent.ProjectileType<PristineFuryLilyWavelet>(), waveletDamage, Projectile.knockBack * 0.25f, Projectile.owner, freq, wAmp);
                        PFLeftEffectRules.ApplyTheme(wavelet, mark);
                    }
                }
            }

            if (chargeLevel >= 3f)
            {
                int totalWavelets = Main.rand.Next(35, 42);
                for (int j = 0; j < totalWavelets; j++)
                {
                    float angle = MathHelper.TwoPi * j / totalWavelets + Main.rand.NextFloat(-0.08f, 0.08f);
                    Vector2 wDir = angle.ToRotationVector2();
                    float freq = Main.rand.NextFloat(0.07f, 0.14f);
                    float wAmp = Main.rand.NextFloat(55f, 95f);
                    int wavelet = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, wDir * Main.rand.NextFloat(16f, 24f), ModContent.ProjectileType<PristineFuryLilyWavelet>(), waveletDamage, Projectile.knockBack * 0.25f, Projectile.owner, freq, wAmp);
                    PFLeftEffectRules.ApplyTheme(wavelet, mark);
                }
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

    // 三向厚光束（百合主瓣）
    internal sealed class PristineFuryLilyMainBeam : ModProjectile, ILocalizedModType
    {
        private static readonly Color LilyRed = new(255, 54, 42);
        private static readonly Color LilyOrange = new(255, 126, 42);
        private const int Lifetime = 48;
        private const float MaxBeamLength = 1850f;
        private const float CollisionWidth = 56f;
        private const float MaxBeamScale = 3.8f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BeamLength => ref Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();
            if (Timer == 1f)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyRay") { Volume = 0.62f, Pitch = -0.28f, MaxInstances = 6 }, Projectile.Center);

            BeamLength = MathHelper.Lerp(BeamLength, MaxBeamLength, 0.75f);
            float completion = Timer / Lifetime;
            float fade = Utils.GetLerpValue(0f, 0.15f, completion, true) * Utils.GetLerpValue(1f, 0.65f, completion, true);
            Projectile.scale = MaxBeamScale * fade;
            Vector2 beamEnd = Projectile.Center + direction * BeamLength;
            Lighting.AddLight(Projectile.Center, LilyRed.ToVector3() * 0.88f);
            DelegateMethods.v3_1 = LilyRed.ToVector3() * 0.72f * fade;
            Utils.PlotTileLine(Projectile.Center, beamEnd, CollisionWidth * Math.Max(Projectile.scale, 0.1f), DelegateMethods.CastLight);

            if (Main.dedServ)
                return;

            if ((int)Timer % 3 == 0)
            {
                Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(BeamLength);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    position + Main.rand.NextVector2Circular(14f, 14f),
                    direction.RotatedByRandom(0.65f) * Main.rand.NextFloat(1.4f, 4.2f),
                    Color.Lerp(LilyRed, LilyOrange, Main.rand.NextFloat(0.25f, 0.75f)),
                    Color.Black, Main.rand.NextFloat(0.55f, 1.05f),
                    Main.rand.Next(22, 40), Main.rand.NextFloat(-0.06f, 0.06f)));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
            if (Main.rand.NextBool(5))
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceBurn") { Volume = 0.28f, Pitch = 0.08f }, target.Center);
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
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, LilyRed);
            float fade = Utils.GetLerpValue(0f, 9f, Projectile.timeLeft, true) * Utils.GetLerpValue(Lifetime, Lifetime - 10f, Projectile.timeLeft, true);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = Projectile.Center + direction * BeamLength - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();

            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            float drawScale = Projectile.scale * 0.65f;
            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            Vector2 scaleVec = new Vector2(drawScale, drawScale);

            Main.spriteBatch.Draw(startTex, start, null, theme * fade, rotation, startTex.Size() / 2f, scaleVec, SpriteEffects.None, 0f);

            float currentLength = BeamLength - (startTex.Height / 2 + endTex.Height) * drawScale;
            Vector2 drawCenter = Projectile.Center + direction * drawScale * startTex.Height / 2f;

            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                int frameHeight = 36;
                int frameY = frameHeight * (Projectile.timeLeft / 3 % 4);
                Rectangle sourceRect = new Rectangle(0, frameY, midTex.Width, frameHeight);

                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);
                    if (sourceRect.Height <= 0)
                        break;

                    Main.spriteBatch.Draw(midTex, drawCenter - Main.screenPosition, sourceRect, theme * fade, rotation, new Vector2(sourceRect.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);
                    lengthDrawn += sourceRect.Height * drawScale;
                    drawCenter += direction * sourceRect.Height * drawScale;

                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                        sourceRect.Y = 0;
                }
            }

            Main.spriteBatch.Draw(endTex, drawCenter - Main.screenPosition, null, theme * fade, rotation, new Vector2(endTex.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(bloom, start, null, theme * (1.05f * fade), 0f, bloom.Size() * 0.5f, 0.58f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloomRing, start, null, theme * (0.82f * fade), 0f, bloomRing.Size() * 0.5f, 0.85f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, theme * (1.12f * fade), 0f, bloom.Size() * 0.5f, 0.62f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, (Color.White with { A = 0 }) * (0.65f * fade), 0f, bloom.Size() * 0.5f, 0.25f * drawScale, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * BeamLength, Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
        }
    }

    // 正弦波动弹（百合副瓣）——围绕主激光大幅旋转摆动
    internal sealed class PristineFuryLilyWavelet : ModProjectile, ILocalizedModType
    {
        private static readonly Color LilyRed    = new(255, 54, 42);
        private static readonly Color LilyOrange = new(255, 126, 42);
        private const int Lifetime = 360;
        private const int HomingDelay = 240; // 120 game frames = 2 seconds (extraUpdates=1 → 2 AI calls/frame)
        private const float HomingRange = 1400f;
        private const float MaxHomingSpeed = 22f;
        private const float HomingInertia = 18f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer    => ref Projectile.localAI[0];
        private ref float BaseAngle => ref Projectile.localAI[1];
        private ref float BaseSpeed => ref Projectile.localAI[2];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 4;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Timer++;
            if (Timer == 1f)
            {
                BaseAngle = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
                BaseSpeed = Math.Max(Projectile.velocity.Length(), 8f);
            }

            if (Timer <= HomingDelay)
            {
                float frequency    = Projectile.ai[0] > 0f ? Projectile.ai[0] : 0.12f;
                float waveAmplitude = Projectile.ai[1] > 0f ? Projectile.ai[1] : 60f;
                Vector2 baseDir = BaseAngle.ToRotationVector2();
                Vector2 perpDir = baseDir.RotatedBy(MathHelper.PiOver2);
                float waveVelocity = waveAmplitude * (float)Math.Cos(frequency * Timer);
                Projectile.velocity = baseDir * BaseSpeed + perpDir * waveVelocity;
            }
            else
            {
                NPC homingTarget = PristineFuryTargeting.FindTarget(Projectile.Center, HomingRange, Main.player[Projectile.owner]);
                if (homingTarget != null)
                {
                    Vector2 toTarget = Projectile.SafeDirectionTo(homingTarget.Center);
                    float speed = Math.Min(Projectile.velocity.Length() + 0.5f, MaxHomingSpeed);
                    Projectile.velocity = (Projectile.velocity * HomingInertia + toTarget * speed) / (HomingInertia + 1f);
                    if (Projectile.velocity.Length() > MaxHomingSpeed)
                        Projectile.velocity = Projectile.velocity.SafeNormalize(toTarget) * MaxHomingSpeed;
                }
                else
                {
                    Projectile.velocity *= 0.97f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, LilyOrange);
            Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.55f);

            if (Main.dedServ)
                return;

            // 飞行中持续生成发光粒子，营造螺旋轨迹感
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    false,
                    Main.rand.Next(8, 16),
                    Main.rand.NextFloat(0.22f, 0.42f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.45f)),
                    true, false, true));
            }

            if ((int)Timer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.18f) + Main.rand.NextVector2Circular(1.2f, 1.2f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.32f, 0.65f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.2f, 0.6f))));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceBurn") { Volume = 0.18f, Pitch = 0.22f, MaxInstances = 8 }, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star  = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, LilyOrange);
            Color themeA0 = theme with { A = 0 };
            Color whiteA0 = Color.White with { A = 0 };
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float fade = Projectile.timeLeft / (float)Lifetime;
            float pulse = 0.88f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 11f + Projectile.identity * 0.55f);

            PFLeftEffectRules.BeginAdditive();

            // 拖尾——大尺寸 bloom 渐退
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 oldPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, oldPos, null,
                    themeA0 * (0.65f * (1f - completion) * fade),
                    0f, bloom.Size() * 0.5f,
                    MathHelper.Lerp(0.48f, 0.08f, completion), SpriteEffects.None, 0);
            }

            // 弹头主体——大 bloom + 白芯
            Main.EntitySpriteDraw(bloom, drawPos, null, themeA0 * (1.05f * fade) * pulse,
                0f, bloom.Size() * 0.5f, 0.52f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPos, null, whiteA0 * (0.72f * fade),
                0f, bloom.Size() * 0.5f, 0.22f, SpriteEffects.None, 0);

            // 四棱星光
            for (int i = 0; i < 4; i++)
            {
                float rot = Projectile.rotation + MathHelper.PiOver2 * i + Main.GlobalTimeWrappedHourly * (1.4f + i * 0.12f);
                Main.EntitySpriteDraw(star, drawPos, null,
                    themeA0 * (0.58f * fade) * pulse, rot,
                    star.Size() * 0.5f,
                    new Vector2(0.10f, 0.48f + fade * 0.22f),
                    SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
    internal sealed class PristineFuryHomingStar : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 180, 50));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/StarProj";

        private const float HomingRange = 1200f;
        private const float MaxSpeed = 28f;
        private const int HomingDelay = 60; // 5 times longer delay before homing starts
        private const float HomingInertia = 30f; // Lazy steering inertia
        private const float FreeFlightDamping = 0.995f;
        private const float NoTargetDamping = 0.99f;
        private const float WanderingTurnStrength = 0.005f;

        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240; // Increased to allow time for lazy drift and chase
            Projectile.tileCollide = false; // Prevent hitting blocks while flying out/homing
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2; // Snappy extra updates
        }

        public override void AI()
        {
            timer++;

            HomeTowardTarget();

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.66f);
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    -Projectile.velocity * 0.15f,
                    false,
                    10,
                    0.5f,
                    ThemeColor,
                    true,
                    false,
                    true
                ));
                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        Projectile.Center,
                        -Projectile.velocity.RotatedByRandom(0.2f) * 0.2f,
                        false,
                        15,
                        0.7f,
                        Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.2f, 0.7f))
                    ));
                }
            }
        }

        private void HomeTowardTarget()
        {
            if (timer <= HomingDelay)
            {
                FreeDrift();
                return;
            }

            NPC target = PristineFuryTargeting.FindTarget(Projectile.Center, HomingRange, Main.player[Projectile.owner]);
            if (target == null)
            {
                FreeDrift(NoTargetDamping);
                return;
            }

            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();

            if (currentSpeed < 0.1f)
                currentVelocity = Projectile.SafeDirectionTo(target.Center) * 4f;

            Vector2 desiredDirection = Projectile.SafeDirectionTo(target.Center);

            // Transition pull strength from 0.2 to 1.0 slowly over 60 updates
            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 60f, timer, true);
            float closePressure = Utils.GetLerpValue(360f, 80f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.2f, 1f, MathHelper.Max(warmup, closePressure * 0.75f));

            float targetSpeed = MathHelper.Lerp(12f, MaxSpeed, pullStrength);
            Vector2 desiredVelocity = desiredDirection * targetSpeed;

            // Lazy tracking: merge current velocity with desired velocity using inertia
            Projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

            // Gentle wandering side sway
            float sideSway = (float)Math.Sin((timer + Projectile.identity * 7f) * 0.06f) *
                MathHelper.Lerp(0.01f, 0.003f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            if (Projectile.velocity.Length() > MaxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxSpeed;
        }

        private void FreeDrift(float damping = FreeFlightDamping)
        {
            float wander = (float)Math.Sin((timer + Projectile.identity * 5f) * 0.08f) * WanderingTurnStrength;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color glow = ThemeColor with { A = 0 };

            for (int i = 0; i < 3; i++)
            {
                float rot = Projectile.rotation + MathHelper.PiOver2 * i * 0.2f;
                Main.EntitySpriteDraw(texture, drawPosition, null, glow * 0.25f, rot, origin, Projectile.scale * 1.1f, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Lerp(lightColor, ThemeColor, 0.45f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    internal sealed class PristineFuryHookSoul : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        private Color SoulColor
        {
            get
            {
                int v = (int)Projectile.ai[0];
                PristineFuryMark mark = System.Enum.IsDefined(typeof(PristineFuryMark), v)
                    ? (PristineFuryMark)v
                    : PristineFuryMark.Idle;
                return PristineFuryMarkHelper.GetColor(mark);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Timer++;
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            float dist = Projectile.Distance(owner.MountedCenter);
            float speed = MathHelper.Lerp(16f, 10f, MathHelper.Clamp(dist / 380f, 0f, 1f));
            Vector2 toPlayer = Projectile.SafeDirectionTo(owner.MountedCenter);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, toPlayer * speed, 0.1f);

            if (dist < 32f || Projectile.Hitbox.Intersects(owner.Hitbox))
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    int heal = 3;
                    owner.statLife = Math.Min(owner.statLifeMax2, owner.statLife + heal);
                    owner.HealEffect(heal, true);
                }
                Projectile.Kill();
                return;
            }

            Lighting.AddLight(Projectile.Center, SoulColor.ToVector3() * 0.45f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    false,
                    Main.rand.Next(6, 12),
                    Main.rand.NextFloat(0.45f, 0.82f),
                    Color.Lerp(SoulColor, Color.White, Main.rand.NextFloat(0.2f, 0.6f)),
                    true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D halfStar = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color soulA0 = SoulColor with { A = 0 };
            Color whiteA0 = Color.White with { A = 0 };
            float fade = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);
            float pulse = 0.82f + 0.18f * (float)Math.Sin(Timer * 0.24f + Projectile.identity * 0.8f);

            PFLeftEffectRules.BeginAdditive();

            Main.EntitySpriteDraw(bloom, drawPos, null, soulA0 * (0.85f * fade * pulse), 0f, bloom.Size() * 0.5f, 0.26f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPos, null, whiteA0 * (0.50f * fade), 0f, bloom.Size() * 0.5f, 0.09f, SpriteEffects.None, 0);

            for (int i = 0; i < 3; i++)
            {
                float rot = MathHelper.TwoPi * i / 3f + Timer * (i % 2 == 0 ? 0.07f : -0.05f);
                Main.EntitySpriteDraw(halfStar, drawPos, null, soulA0 * (0.56f * fade * pulse), rot,
                    halfStar.Size() * 0.5f, new Vector2(0.05f, 0.27f), SpriteEffects.None, 0);
            }

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
