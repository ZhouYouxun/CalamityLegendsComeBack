using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ascendant
{
    internal class AscendantSpirit_PROJ : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Ascendant/AscendantSpirit_PROJ";

        private const float DefaultLaunchDelayFrames = 7f;
        internal const float DefaultLaunchSpeed = 20.5f;
        private const float CollisionRadius = 5.5f;

        private static readonly Color[] ThemePalette =
        {
            new Color(255, 176, 68),
            new Color(90, 214, 255),
            new Color(255, 126, 218)
        };

        private Vector2 targetPoint;
        private Vector2 initialDirection = Vector2.UnitY;
        private Color currentColor = ThemePalette[0];
        private float timer;
        private float launchDelay;
        private float launchTimer;
        private float squash = 1f;
        private bool initializedNeedle;
        private bool launched;

        private Color VisualColor
        {
            get
            {
                float rate = timer * 0.025f + Math.Abs(Projectile.identity) * 0.17f;
                int colorIndex = (int)(rate % ThemePalette.Length);
                float colorInterpolant = rate % 1f;
                Color cyclingColor = Color.Lerp(ThemePalette[colorIndex], ThemePalette[(colorIndex + 1) % ThemePalette.Length], colorInterpolant);
                return Color.Lerp(currentColor, cyclingColor, 0.42f);
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 14;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150*3;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9;
            Projectile.alpha = 0;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            currentColor = RandomThemeColor();
            EnsureInitialized();
        }

        public static Color RandomThemeColor() => ThemePalette[Main.rand.Next(ThemePalette.Length)];

        public void InitializeNeedle(Vector2 target, Color color, float delayFrames)
        {
            currentColor = color;
            initialDirection = Projectile.velocity.SafeNormalize((target - Projectile.Center).SafeNormalize(Vector2.UnitY));
            targetPoint = Projectile.Center + initialDirection * 560f;
            launchDelay = Math.Max(2f, delayFrames) * (Projectile.extraUpdates + 1f);
            timer = 0f;
            launchTimer = 0f;
            squash = 1f;
            initializedNeedle = true;
            launched = false;

            Projectile.ai[0] = targetPoint.X;
            Projectile.ai[1] = targetPoint.Y;
            Projectile.ai[2] = launchDelay;
            Projectile.rotation = initialDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.netUpdate = true;

            SpawnNeedleBirthEffects();
        }

        private void EnsureInitialized()
        {
            if (initializedNeedle)
                return;

            targetPoint = Projectile.ai[0] != 0f || Projectile.ai[1] != 0f
                ? new Vector2(Projectile.ai[0], Projectile.ai[1])
                : Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 540f;

            initialDirection = Projectile.velocity.SafeNormalize((targetPoint - Projectile.Center).SafeNormalize(Vector2.UnitY));
            targetPoint = Projectile.Center + initialDirection * 560f;

            launchDelay = Projectile.ai[2] > 0f
                ? Projectile.ai[2]
                : DefaultLaunchDelayFrames * (Projectile.extraUpdates + 1f);

            Projectile.rotation = initialDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = 0.82f;
            initializedNeedle = true;
        }

        public override void AI()
        {
            EnsureInitialized();

            timer++;
            Color visualColor = VisualColor;
            Lighting.AddLight(Projectile.Center, visualColor.ToVector3() * (launched ? 0.8f : 0.45f));

            if (!launched)
            {
                UpdateCharge(visualColor);
                return;
            }

            UpdateLaunchedFlight(visualColor);
        }

        public override bool ShouldUpdatePosition() => launched;

        private void UpdateCharge(Color visualColor)
        {
            float chargeCompletion = Utils.GetLerpValue(0f, launchDelay, timer, true);
            float chargePower = (float)Math.Pow(chargeCompletion, 2.2f);
            Vector2 aimDirection = initialDirection;

            Projectile.rotation = Projectile.rotation.AngleLerp(aimDirection.ToRotation() + MathHelper.PiOver2, 0.08f + chargeCompletion * 0.12f);
            Projectile.scale = MathHelper.Lerp(0.78f, 1.05f, (float)Math.Pow(chargeCompletion, 0.65f));
            squash = MathHelper.Lerp(squash, MathHelper.Lerp(1.12f, 0.32f, chargePower), 0.14f);

            if (Projectile.FinalExtraUpdate())
                SpawnChargeParticles(aimDirection, visualColor, chargeCompletion);

            if (timer >= launchDelay)
                LaunchAtTarget(aimDirection);
        }

        private void LaunchAtTarget(Vector2 direction)
        {
            launched = true;
            launchTimer = 0f;
            float launchSpeed = Projectile.velocity.Length();
            if (launchSpeed <= 0.01f)
                launchSpeed = DefaultLaunchSpeed;
            Projectile.velocity = direction.SafeNormalize(initialDirection) * launchSpeed;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 95);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            SpawnLaunchStarburst(direction, VisualColor);
            Projectile.netUpdate = true;
        }

        private void UpdateLaunchedFlight(Color visualColor)
        {
            launchTimer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(initialDirection);

            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 0.92f, 0.04f);
            squash = MathHelper.Lerp(squash, 0.24f, 0.12f);

            if (Projectile.FinalExtraUpdate())
                SpawnFlightParticles(direction, visualColor);
        }

        private void SpawnNeedleBirthEffects()
        {
            Vector2 normal = initialDirection.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 4; i++)
            {
                Particle aura = new CustomSpark(
                    Projectile.Center + normal * Main.rand.NextFloat(-4f, 4f),
                    -initialDirection * Main.rand.NextFloat(0.4f, 1.2f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(16, 23),
                    Main.rand.NextFloat(0.18f, 0.28f),
                    Color.Lerp(currentColor, Color.White, 0.18f),
                    new Vector2(0.75f, 1.25f),
                    glowCenter: true,
                    shrinkSpeed: 0.25f,
                    glowOpacity: 0.65f);
                GeneralParticleHandler.SpawnParticle(aura);
            }

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>());
                dust.scale = Main.rand.NextFloat(0.8f, 1.3f);
                dust.velocity = -initialDirection.RotatedByRandom(0.48f) * Main.rand.NextFloat(2.2f, 4.5f);
                dust.noGravity = true;
                dust.color = Color.Lerp(currentColor, Color.White, Main.rand.NextFloat(0.08f, 0.28f));
                dust.fadeIn = 1.4f;
            }
        }

        private void SpawnChargeParticles(Vector2 aimDirection, Color visualColor, float chargeCompletion)
        {
            Vector2 normal = aimDirection.RotatedBy(MathHelper.PiOver2);

            if ((int)timer % 3 == 0)
            {
                Particle bloom = new CustomSpark(
                    Projectile.Center - aimDirection * 10f + normal * Main.rand.NextFloat(-4f, 4f),
                    -aimDirection * Main.rand.NextFloat(0.3f, 1.1f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.09f, 0.16f) * (0.75f + chargeCompletion * 0.65f),
                    visualColor * 0.75f,
                    new Vector2(0.72f, 1.35f),
                    glowCenter: true,
                    shrinkSpeed: 0.42f,
                    glowOpacity: 0.55f);
                GeneralParticleHandler.SpawnParticle(bloom);
            }

            if (Main.rand.NextBool(3))
            {
                Particle star = new CustomSpark(
                    Projectile.Center + normal * Main.rand.NextFloat(-8f, 8f),
                    -aimDirection.RotatedByRandom(0.55f) * Main.rand.NextFloat(0.5f, 1.8f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.08f, 0.14f) * (0.8f + chargeCompletion),
                    Color.Lerp(visualColor, Color.White, 0.25f),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.25f,
                    glowOpacity: 0.7f);
                GeneralParticleHandler.SpawnParticle(star);
            }

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    ModContent.DustType<SquashDust>());

                dust.scale = Main.rand.NextFloat(0.75f, 1.2f);
                dust.velocity = -aimDirection * Main.rand.NextFloat(1.2f, 2.8f) + normal * Main.rand.NextFloat(-0.9f, 0.9f);
                dust.noGravity = true;
                dust.color = visualColor;
                dust.fadeIn = 1.6f + chargeCompletion * 1.2f;
            }
        }

        private void SpawnLaunchStarburst(Vector2 direction, Color visualColor)
        {
            Particle core = new CustomSpark(
                Projectile.Center,
                Vector2.Zero,
                "CalamityMod/Particles/BloomCircle",
                false,
                22,
                0.72f,
                visualColor,
                new Vector2(0.8f, 1.25f),
                glowCenter: true,
                shrinkSpeed: 0.12f,
                glowOpacity: 0.85f,
                extraRotation: direction.ToRotation());
            GeneralParticleHandler.SpawnParticle(core);

            float ringRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int ring = 1; ring <= 2; ring++)
            {
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>());
                    dust.scale = 4.7f - ring * 0.55f;
                    dust.velocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi / 5f * i + ringRotation) * (ring * 1.2f + 1.4f);
                    dust.noGravity = true;
                    dust.color = GetRandomThemeColor(visualColor);
                    dust.fadeIn = 4.8f - ring * 0.35f;
                }
            }
        }

        private void SpawnFlightParticles(Vector2 direction, Color visualColor)
        {
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float speedFactor = Utils.GetLerpValue(10f, DefaultLaunchSpeed, Projectile.velocity.Length(), true);

            if ((int)timer % 2 == 0)
            {
                Particle trail = new CustomSpark(
                    Projectile.Center - direction * 20f,
                    Projectile.velocity * 0.03f,
                    "CalamityMod/Particles/DualTrail",
                    false,
                    15,
                    0.095f,
                    visualColor * 0.8f,
                    new Vector2(0.75f, 1.45f + speedFactor * 0.65f),
                    glowCenter: true,
                    shrinkSpeed: 0.42f,
                    glowOpacity: 0.55f);
                GeneralParticleHandler.SpawnParticle(trail);
            }

            if ((int)timer % 3 == 0)
            {
                Particle bloomTrail = new CustomSpark(
                    Projectile.Center - direction * 16f + normal * Main.rand.NextFloat(-3f, 3f),
                    -direction * Main.rand.NextFloat(0.4f, 1.2f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    12,
                    0.13f,
                    Color.Lerp(visualColor, Color.White, 0.15f) * 0.55f,
                    new Vector2(0.68f, 1.4f),
                    glowCenter: true,
                    shrinkSpeed: 0.5f,
                    glowOpacity: 0.48f);
                GeneralParticleHandler.SpawnParticle(bloomTrail);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    ModContent.DustType<SquashDust>());

                dust.scale = Main.rand.NextFloat(0.85f, 1.35f);
                dust.velocity = -Projectile.velocity.SafeNormalize(direction) * Main.rand.NextFloat(2.4f, 5.2f) + normal * Main.rand.NextFloat(-1f, 1f);
                dust.noGravity = true;
                dust.color = GetRandomThemeColor(visualColor);
                dust.fadeIn = 3.2f;
            }

            if (Main.rand.NextBool(4))
            {
                Particle star = new CustomSpark(
                    Projectile.Center + normal * Main.rand.NextFloat(-5f, 5f),
                    -direction * Main.rand.NextFloat(0.5f, 1.6f) + normal * Main.rand.NextFloat(-0.8f, 0.8f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.07f, 0.13f),
                    Color.Lerp(visualColor, Color.White, 0.22f),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.28f,
                    glowOpacity: 0.65f);
                GeneralParticleHandler.SpawnParticle(star);
            }

            SpawnRhythmicFlightStars(direction, normal, visualColor);
        }

        private void SpawnRhythmicFlightStars(Vector2 direction, Vector2 normal, Color visualColor)
        {
            int flightBeat = (int)(launchTimer / (Projectile.extraUpdates + 1f));
            if (flightBeat <= 0)
                return;

            float beatPhase = flightBeat * 0.62f + Projectile.identity * 0.19f;
            int starCount = flightBeat % 2 == 0 ? 2 : 1;
            Vector2 anchor = Projectile.Center - direction * 18f;

            for (int i = 0; i < starCount; i++)
            {
                float spiralPhase = beatPhase + i * MathHelper.Pi;
                float spiralOffset = (float)Math.Sin(spiralPhase) * 6.5f;
                float depthOffset = (float)Math.Cos(spiralPhase) * 3.2f;
                Vector2 starPosition = anchor - direction * (i * 4.2f + depthOffset) + normal * spiralOffset;
                Vector2 starVelocity = -direction * Main.rand.NextFloat(0.45f, 1.15f) + normal * ((float)Math.Cos(spiralPhase) * 0.55f);
                float scale = Main.rand.NextFloat(0.055f, 0.095f);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    starPosition,
                    starVelocity,
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(11, 17),
                    scale,
                    Color.Lerp(GetRandomThemeColor(visualColor), Color.White, 0.34f),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.3f,
                    glowOpacity: 0.58f));
            }

            if (flightBeat % 8 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    anchor,
                    -direction * 0.7f,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    12,
                    0.16f,
                    Color.Lerp(visualColor, Color.White, 0.18f) * 0.55f,
                    new Vector2(0.7f, 1.35f),
                    glowCenter: true,
                    shrinkSpeed: 0.42f,
                    glowOpacity: 0.5f,
                    extraRotation: direction.ToRotation()));
            }
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(initialDirection);
            Color visualColor = VisualColor;

            SoundEngine.PlaySound(SoundID.Item94 with { Volume = launched ? 0.18f : 0.1f, Pitch = launched ? 0.18f : 0.38f, PitchVariance = 0.1f, MaxInstances = 6 }, Projectile.Center);

            for (int i = 0; i < 10; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2Circular(2.4f, 2.4f) + direction * Main.rand.NextFloat(0.4f, 1.8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    burstVelocity,
                    false,
                    Main.rand.Next(11, 18),
                    Main.rand.NextFloat(0.22f, 0.38f),
                    Color.Lerp(visualColor, Color.White, Main.rand.NextFloat(0.12f, 0.32f)),
                    true,
                    true));
            }

            float rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            int rings = launched ? 5 : 3;
            for (int ring = 1; ring <= rings; ring++)
            {
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>());
                    dust.scale = (launched ? 6.4f : 3.8f) - ring * 0.55f;
                    dust.velocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi / 5f * i + rotation) * (ring * 1.45f + 1.7f);
                    dust.noGravity = true;
                    dust.color = GetRandomThemeColor(visualColor);
                    dust.fadeIn = 5.8f - ring * 0.42f;
                }
            }

            Particle bloom = new CustomSpark(
                Projectile.Center,
                Vector2.Zero,
                "CalamityMod/Particles/BloomCircle",
                false,
                25,
                launched ? 0.92f : 0.55f,
                visualColor,
                Vector2.One,
                true,
                true,
                0,
                false,
                false,
                glowOpacity: 0.85f);
            GeneralParticleHandler.SpawnParticle(bloom);

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.RainbowTorch,
                    Main.rand.NextVector2Circular(2.4f, 2.4f));

                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.65f, 1.05f);
                dust.color = visualColor;
            }
        }

        private static Color GetRandomThemeColor(Color fallback)
        {
            if (Main.rand.NextBool(5))
                return Color.Lerp(fallback, Color.White, Main.rand.NextFloat(0.18f, 0.42f));

            return ThemePalette[Main.rand.Next(ThemePalette.Length)];
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            launched && CalamityUtils.CircularHitboxCollision(Projectile.Center, CollisionRadius, targetHitbox);

        public override bool? CanDamage() => launched && launchTimer > 2f;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            Color visualColor = VisualColor;
            Vector2 hitPos = Projectile.Center;

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                hitPos,
                Vector2.Zero,
                "CalamityMod/Particles/BloomCircle",
                false,
                12,
                Main.rand.NextFloat(0.28f, 0.42f),
                Color.Lerp(visualColor, Color.White, 0.25f),
                new Vector2(0.85f, 1.15f),
                glowCenter: true,
                shrinkSpeed: 0.28f,
                glowOpacity: 0.65f,
                extraRotation: Projectile.velocity.ToRotation()));

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    hitPos + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextVector2Circular(2.8f, 2.8f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.06f, 0.11f),
                    Color.Lerp(GetRandomThemeColor(visualColor), Color.White, Main.rand.NextFloat(0.18f, 0.4f)),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.3f,
                    glowOpacity: 0.58f));
            }

            for (int i = 0; i < 4; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    hitPos + Main.rand.NextVector2Circular(3f, 3f),
                    ModContent.DustType<SquashDust>());
                dust.scale = Main.rand.NextFloat(0.65f, 1.05f);
                dust.velocity = Main.rand.NextVector2Circular(2.2f, 2.2f);
                dust.noGravity = true;
                dust.color = GetRandomThemeColor(visualColor);
                dust.fadeIn = 1.6f;
            }

            SpawnImpactSigil(hitPos, visualColor);
        }

        private void SpawnImpactSigil(Vector2 hitPos, Color visualColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(initialDirection);
            float baseRotation = direction.ToRotation() + MathHelper.PiOver2 + Main.rand.NextFloat(-0.18f, 0.18f);
            float radius = Main.rand.NextFloat(18f, 25f);

            for (int i = 0; i < 5; i++)
            {
                float angle = baseRotation + MathHelper.TwoPi * i / 5f;
                Vector2 point = hitPos + angle.ToRotationVector2() * radius;
                Vector2 nextPoint = hitPos + (angle + MathHelper.TwoPi * 2f / 5f).ToRotationVector2() * radius;
                Vector2 lineVelocity = (nextPoint - point) * 0.16f;

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    point,
                    lineVelocity,
                    false,
                    Main.rand.Next(13, 18),
                    Main.rand.NextFloat(0.62f, 0.95f),
                    Color.Lerp(GetRandomThemeColor(visualColor), Color.White, 0.38f) * 0.88f));

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    point,
                    (point - hitPos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.7f, 1.8f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.08f, 0.14f),
                    Color.Lerp(visualColor, Color.White, 0.32f),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.24f,
                    glowOpacity: 0.72f));
            }

            for (int i = 0; i < 10; i++)
            {
                float angle = baseRotation + MathHelper.TwoPi * i / 10f;
                Vector2 burstVelocity = angle.ToRotationVector2() * Main.rand.NextFloat(2.1f, 4.8f);
                Dust dust = Dust.NewDustPerfect(hitPos, ModContent.DustType<SquashDust>(), burstVelocity);
                dust.scale = Main.rand.NextFloat(0.8f, 1.35f);
                dust.noGravity = true;
                dust.color = GetRandomThemeColor(visualColor);
                dust.fadeIn = Main.rand.NextFloat(1.8f, 2.8f);
            }

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                hitPos,
                Vector2.Zero,
                "CalamityMod/Particles/BloomCircle",
                false,
                16,
                0.34f,
                Color.Lerp(visualColor, Color.White, 0.2f),
                Vector2.One,
                true,
                true,
                0,
                false,
                false,
                glowOpacity: 0.78f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Color visualColor = VisualColor;
            Color outlineColor = Color.Lerp(visualColor, Color.White, 0.2f);
            Color bodyColor = Color.Lerp(visualColor, Color.White, 0.16f);
            float fadeIn = Utils.GetLerpValue(0f, 10f, timer, true);
            float fadeOut = Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            float launchedStretch = launched ? 0.65f : 0f;
            Vector2 needleScale = new Vector2(0.86f + squash * 0.16f, 1.12f + (1f - squash) * 1.65f + launchedStretch) * Projectile.scale;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                visualColor with { A = 0 } * 0.24f * opacity,
                Projectile.rotation,
                bloomOrigin,
                new Vector2(0.18f, 0.52f + launchedStretch * 0.25f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                star,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.16f * opacity,
                Projectile.rotation,
                star.Size() * 0.5f,
                new Vector2(0.12f * squash, 0.22f + (1f - squash) * 0.22f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 10; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 2.35f;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + drawOffset,
                    null,
                    outlineColor with { A = 0 } * 0.5f * opacity,
                    Projectile.rotation,
                    origin,
                    needleScale,
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                bodyColor * opacity,
                Projectile.rotation,
                origin,
                needleScale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .ToArray();

            if (trailPoints.Length == 0)
                trailPoints = new[] { Projectile.position, Projectile.position - Projectile.velocity };
            else if (trailPoints[0] != Projectile.position)
                trailPoints = new[] { Projectile.position }.Concat(trailPoints).ToArray();

            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    TrailOffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                trailPoints.Length * 2);

            Vector2[] corePoints = trailPoints.Take(Math.Min(10, trailPoints.Length)).ToArray();
            if (corePoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(
                    TrailCoreWidthFunction,
                    TrailCoreColorFunction,
                    TrailOffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                corePoints.Length * 2);
        }

        private Vector2 TrailOffsetFunction(float completion, Vector2 _) => Projectile.Size * 0.5f;

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float maxWidth = Projectile.scale * (launched ? 32f : 22f);
            float curveRatio = 0.18f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxWidth, 0f);
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color trailColor = VisualColor;
            Color tailColor = Color.Lerp(trailColor, Color.Transparent, Utils.GetLerpValue(0.68f, 1f, completion, true));
            tailColor.A = 0;
            Color bodyColor = Color.Lerp(trailColor, Color.White, 0.18f);
            bodyColor.A = 0;
            return Color.Lerp(bodyColor, tailColor, completion);
        }

        private float TrailCoreWidthFunction(float completion, Vector2 _)
        {
            float maxWidth = Projectile.scale * (launched ? 15f : 10f);
            float curveRatio = 0.18f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxWidth, 0f);
        }

        private Color TrailCoreColorFunction(float completion, Vector2 _)
        {
            Color bodyColor = Color.Lerp(Color.White, VisualColor, 0.25f);
            bodyColor.A = 0;
            Color tailColor = Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.76f, 1f, completion, true));
            tailColor.A = 0;
            return Color.Lerp(bodyColor, tailColor, completion);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(targetPoint.X);
            writer.Write(targetPoint.Y);
            writer.Write(initialDirection.X);
            writer.Write(initialDirection.Y);
            writer.Write(timer);
            writer.Write(launchDelay);
            writer.Write(launchTimer);
            writer.Write(squash);
            writer.Write(initializedNeedle);
            writer.Write(launched);
            writer.Write(currentColor.R);
            writer.Write(currentColor.G);
            writer.Write(currentColor.B);
            writer.Write(currentColor.A);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            targetPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            initialDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            timer = reader.ReadSingle();
            launchDelay = reader.ReadSingle();
            launchTimer = reader.ReadSingle();
            squash = reader.ReadSingle();
            initializedNeedle = reader.ReadBoolean();
            launched = reader.ReadBoolean();
            currentColor = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }
    }
}
