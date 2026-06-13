using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    internal class FragmentNebula_Star : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float RegularSpeed = 15.6f;
        private const float BlueSpeed = 16.8f;

        private bool IsBlueVariant => Projectile.ai[0] == 1f;
        private ref float Timer => ref Projectile.localAI[0];
        private ref float HitSomething => ref Projectile.localAI[1];

        private Color PrimaryColor => IsBlueVariant ? new Color(72, 196, 255) : new Color(190, 72, 255);
        private Color SecondaryColor => IsBlueVariant ? new Color(32, 88, 255) : new Color(255, 102, 216);
        private Color CoreColor => IsBlueVariant ? new Color(126, 230, 255) : new Color(246, 134, 255);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 28;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 190;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.scale = IsBlueVariant ? 1.18f : 1f;
            if (IsBlueVariant)
                Projectile.timeLeft += 36;

            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = Vector2.UnitX * (IsBlueVariant ? BlueSpeed : RegularSpeed);
        }

        public override void AI()
        {
            Timer++;

            float idealSpeed = IsBlueVariant ? BlueSpeed : RegularSpeed;
            float homingDelay = Math.Max(1f, Projectile.ai[1] * (Projectile.extraUpdates + 1f));
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (Timer >= homingDelay)
                UpdateHoming(idealSpeed, direction, homingDelay);
            else
                UpdateLaunchDrift(idealSpeed, direction, homingDelay);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, PrimaryColor.ToVector3() * (IsBlueVariant ? 0.72f : 0.52f));

            SpawnFlightParticles(Timer >= homingDelay);

            if ((int)Timer == (int)homingDelay)
                SpawnHomingWake();
        }

        private void UpdateLaunchDrift(float idealSpeed, Vector2 direction, float homingDelay)
        {
            float windup = Utils.GetLerpValue(0f, homingDelay, Timer, true);

            Projectile.velocity = Projectile.velocity.SafeNormalize(direction) * MathHelper.Lerp(Projectile.velocity.Length(), idealSpeed * MathHelper.Lerp(0.82f, 1.04f, windup), 0.06f);
        }

        private void UpdateHoming(float idealSpeed, Vector2 currentDirection, float homingDelay)
        {
            NPC target = FindTarget(IsBlueVariant ? 5200f : 4600f);
            if (target is null)
            {
                Projectile.velocity = currentDirection * MathHelper.Lerp(Projectile.velocity.Length(), idealSpeed, 0.08f);
                return;
            }

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float distance = Projectile.Distance(target.Center);
            float timePower = Utils.GetLerpValue(0f, 50f * (Projectile.extraUpdates + 1f), Timer - homingDelay, true);
            float closePower = Utils.GetLerpValue(900f, 120f, distance, true);
            float trackingPower = MathHelper.Max(timePower, closePower);
            float targetSpeed = idealSpeed * MathHelper.Lerp(1.04f, IsBlueVariant ? 1.46f : 1.38f, trackingPower);
            float inertia = MathHelper.Lerp(IsBlueVariant ? 11f : 10f, IsBlueVariant ? 1.8f : 1.5f, trackingPower);

            Projectile.velocity = (Projectile.velocity * inertia + desiredDirection * targetSpeed) / (inertia + 1f);

            float speed = Projectile.velocity.Length();
            float minSpeed = idealSpeed * 0.92f;
            float maxSpeed = idealSpeed * (IsBlueVariant ? 1.58f : 1.48f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MathHelper.Clamp(speed, minSpeed, maxSpeed);
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestScore = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance > range)
                    continue;

                float score = npc.boss ? distance * 0.72f : distance;
                if (score >= bestScore)
                    continue;

                bestTarget = npc;
                bestScore = score;
            }

            return bestTarget;
        }

        private void SpawnFlightParticles(bool homingActive)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color primary = PrimaryColor;
            Color secondary = SecondaryColor;

            if (Main.rand.NextBool(homingActive ? 2 : 3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(6f, 18f) + normal * Main.rand.NextFloat(-4f, 4f),
                    IsBlueVariant ? DustID.BlueTorch : DustID.PurpleTorch,
                    -direction * Main.rand.NextFloat(0.8f, 2.6f) + normal * Main.rand.NextFloat(-0.7f, 0.7f),
                    0,
                    Color.Lerp(primary, CoreColor, Main.rand.NextFloat(0.1f, 0.42f)),
                    Main.rand.NextFloat(0.85f, IsBlueVariant ? 1.35f : 1.18f));

                dust.noGravity = true;
                dust.fadeIn = Main.rand.NextFloat(0.2f, 0.55f);
            }

            if ((int)Timer % (IsBlueVariant ? 5 : 4) == 0)
            {
                Particle ribbon = new CustomSpark(
                    Projectile.Center - direction * 14f + normal * Main.rand.NextFloat(-2.2f, 2.2f),
                    -direction * Main.rand.NextFloat(0.28f, 0.82f),
                    "CalamityMod/Particles/FadeStreak",
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.04f, 0.08f) * (IsBlueVariant ? 1.18f : 1f),
                    Color.Lerp(primary, secondary, Main.rand.NextFloat(0.2f, 0.65f)),
                    new Vector2(Main.rand.NextFloat(0.28f, 0.42f), Main.rand.NextFloat(1.45f, 2.15f)),
                    shrinkSpeed: 0.76f,
                    extraRotation: direction.ToRotation() + MathHelper.PiOver2);
                GeneralParticleHandler.SpawnParticle(ribbon);
            }

            if (Main.rand.NextBool(IsBlueVariant ? 3 : 4))
            {
                Particle star = new CustomSpark(
                    Projectile.Center + normal * Main.rand.NextFloat(-7f, 7f),
                    -direction * Main.rand.NextFloat(0.3f, 1.2f) + normal * Main.rand.NextFloat(-0.35f, 0.35f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.07f, 0.13f),
                    Color.Lerp(CoreColor, primary, Main.rand.NextFloat(0.28f, 0.7f)),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.25f,
                    glowOpacity: 0.62f);
                GeneralParticleHandler.SpawnParticle(star);
            }

            SpawnTipHighlight(direction, primary);
        }

        private void SpawnTipHighlight(Vector2 direction, Color primary)
        {
            Particle tipGlow = new CustomSpark(
                Projectile.Center + direction * (IsBlueVariant ? 15f : 12f),
                Projectile.velocity * 0.04f,
                "CalamityMod/Particles/BloomCircle",
                false,
                6,
                IsBlueVariant ? 0.13f : 0.1f,
                Color.Lerp(CoreColor, primary, 0.35f),
                new Vector2(0.36f, 0.88f),
                glowCenter: true,
                shrinkSpeed: 0.35f,
                glowOpacity: 0.5f,
                extraRotation: direction.ToRotation());
            GeneralParticleHandler.SpawnParticle(tipGlow);
        }

        private void SpawnHomingWake()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color primary = PrimaryColor;

            Particle pulse = new CustomSpark(
                Projectile.Center,
                Vector2.Zero,
                "CalamityMod/Particles/BloomCircle",
                false,
                18,
                IsBlueVariant ? 0.36f : 0.28f,
                Color.Lerp(primary, Color.White, 0.18f),
                new Vector2(0.8f, 1.35f),
                glowCenter: true,
                shrinkSpeed: 0.18f,
                glowOpacity: 0.58f,
                extraRotation: direction.ToRotation());
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + normal * Main.rand.NextFloat(-6f, 6f),
                    IsBlueVariant ? DustID.BlueTorch : DustID.PurpleCrystalShard,
                    -direction.RotatedByRandom(0.36f) * Main.rand.NextFloat(2.2f, 5f),
                    0,
                    Color.Lerp(primary, Color.White, Main.rand.NextFloat(0.12f, 0.35f)),
                    Main.rand.NextFloat(0.9f, 1.45f));

                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item66, target.Center);
            HitSomething = 1f;
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            bool hitTriggered = HitSomething == 1f || timeLeft > 2;

            SpawnNebulaBurst(hitTriggered);

            if (hitTriggered)
                SpawnBlueGrandDeathEffect(IsBlueVariant ? 1f : 0.5f);

            if (!IsBlueVariant || !hitTriggered || Projectile.owner != Main.myPlayer)
                return;

            int explosionIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<global::CalamityLegendsComeBack.Weapons.SHPC.NewLegendSHPE>(),
                (int)(Projectile.damage * 0.5f),
                Projectile.knockBack,
                Projectile.owner);

            if (Main.projectile.IndexInRange(explosionIndex))
            {
                Projectile explosion = Main.projectile[explosionIndex];
                explosion.width = 190;
                explosion.height = 190;
                explosion.Center = Projectile.Center;
                explosion.netUpdate = true;
            }
        }

        private void SpawnBlueGrandDeathEffect(float visualScale)
        {
            Vector2 center = Projectile.Center;
            Vector2 baseVelocity = Projectile.velocity;
            Vector2 forward = baseVelocity.SafeNormalize(Vector2.UnitY);

            Color mainColor = PrimaryColor;
            Color startColor = IsBlueVariant ? new Color(132, 232, 255) : new Color(255, 122, 230);
            Color endColor = SecondaryColor;
            Color coreColor = Color.Lerp(startColor, mainColor, 0.35f);

            float goldenAngle = MathHelper.TwoPi * 0.38196601125f;
            float baseRadius = 54f * visualScale;
            float outerRadius = 120f * visualScale;
            float spiralScale = 8.5f * visualScale;

            for (int i = 0; i < Math.Max(12, (int)(34 * visualScale)); i++)
            {
                float t = i + 1f;
                float angle = goldenAngle * t;
                float radius = spiralScale * (float)Math.Sqrt(t);
                Vector2 offset = angle.ToRotationVector2() * radius;
                Vector2 radialDir = offset.SafeNormalize(Vector2.UnitX);
                Vector2 tangentDir = radialDir.RotatedBy(MathHelper.PiOver2);
                Vector2 velocity = radialDir * (1.2f + t * 0.06f) + tangentDir * (2.2f + t * 0.035f);
                float scale = (0.9f + t * 0.018f) * visualScale;
                int lifetime = 24 + i % 10;
                Color particleColor = Color.Lerp(startColor, coreColor, 0.32f + 0.68f * (i / 33f));

                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(center + offset, velocity, scale, particleColor, lifetime));
            }

            int ringCount = Math.Max(18, (int)(48 * visualScale));
            for (int i = 0; i < ringCount; i++)
            {
                float theta = MathHelper.TwoPi * i / ringCount;
                float modulation = 1f + 0.22f * (float)Math.Cos(3f * theta) + 0.14f * (float)Math.Sin(5f * theta);
                float radius = outerRadius * modulation;
                Vector2 dir = theta.ToRotationVector2();
                Vector2 pos = center + dir * radius;
                float dr = outerRadius * (-0.22f * 3f * (float)Math.Sin(3f * theta) + 0.14f * 5f * (float)Math.Cos(5f * theta));
                Vector2 tangent = new Vector2(
                    dr * (float)Math.Cos(theta) - radius * (float)Math.Sin(theta),
                    dr * (float)Math.Sin(theta) + radius * (float)Math.Cos(theta)
                ).SafeNormalize(Vector2.UnitY);

                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    pos,
                    tangent * 5.8f + dir * 1.8f,
                    Color.Lerp(coreColor, startColor, 0.45f),
                    Color.Lerp(mainColor, endColor, 0.45f),
                    1.25f * visualScale,
                    24));

                GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                    pos - tangent * 18f,
                    tangent * 0.42f,
                    false,
                    14,
                    1.7f * visualScale,
                    Color.Lerp(startColor, mainColor, 0.5f) * 0.28f));

                Dust dust = Dust.NewDustPerfect(
                    pos,
                    DustID.Electric,
                    dir * 1.8f + tangent * 1.2f,
                    0,
                    Color.Lerp(startColor, mainColor, 0.4f),
                    1.45f * visualScale);
                dust.noGravity = true;
            }

            int lissajousCount = Math.Max(18, (int)(54 * visualScale));
            float amplitudeX = 108f * visualScale;
            float amplitudeY = 84f * visualScale;
            for (int i = 0; i < lissajousCount; i++)
            {
                float t = MathHelper.TwoPi * i / lissajousCount;
                Vector2 pos = center + new Vector2(
                    amplitudeX * (float)Math.Sin(3f * t + MathHelper.PiOver2),
                    amplitudeY * (float)Math.Sin(4f * t));
                Vector2 deriv = new Vector2(
                    3f * amplitudeX * (float)Math.Cos(3f * t + MathHelper.PiOver2),
                    4f * amplitudeY * (float)Math.Cos(4f * t)
                ).SafeNormalize(Vector2.UnitX);

                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    pos,
                    deriv * 4.8f,
                    false,
                    18,
                    1.18f * visualScale,
                    Color.Lerp(mainColor, startColor, 0.55f)));

                GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                    pos - deriv * 10f,
                    deriv * 0.38f,
                    false,
                    12,
                    1.45f * visualScale,
                    Color.Lerp(endColor, startColor, 0.5f) * 0.24f));
            }

            int roseCount = Math.Max(16, (int)(42 * visualScale));
            float roseRadius = 132f * visualScale;
            for (int i = 0; i < roseCount; i++)
            {
                float theta = MathHelper.TwoPi * i / roseCount;
                float rose = roseRadius * (float)Math.Cos(7f * theta);
                Vector2 dir = theta.ToRotationVector2();
                float projection = Vector2.Dot(dir, forward) * 18f;
                Vector2 pos = center + dir * rose + forward * projection;
                Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);
                Vector2 velocity = normal * 4.2f + dir * 2.4f;

                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    pos,
                    velocity,
                    Color.Lerp(coreColor, startColor, 0.5f),
                    Color.Lerp(mainColor, endColor, 0.6f),
                    1.05f * visualScale,
                    20));

                Dust dust = Dust.NewDustPerfect(
                    pos,
                    DustID.BlueTorch,
                    velocity * 0.55f,
                    0,
                    Color.Lerp(mainColor, endColor, 0.45f),
                    1.35f * visualScale);
                dust.noGravity = true;
                dust.fadeIn = 0.45f;
            }

            int vortexCount = Math.Max(14, (int)(36 * visualScale));
            for (int i = 0; i < vortexCount; i++)
            {
                float theta = MathHelper.TwoPi * i / vortexCount;
                Vector2 dir = theta.ToRotationVector2();
                Vector2 posA = center + dir * (42f + i * 2.2f) * visualScale;
                Vector2 posB = center - dir * (58f + i * 1.65f) * visualScale;
                Vector2 tangentA = dir.RotatedBy(MathHelper.PiOver2);
                Vector2 tangentB = dir.RotatedBy(-MathHelper.PiOver2);

                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    posA,
                    tangentA * (3.2f + i * 0.05f),
                    (0.82f + i * 0.01f) * visualScale,
                    Color.Lerp(startColor, coreColor, 0.35f),
                    18 + i % 8));

                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    posB,
                    tangentB * (2.8f + i * 0.05f),
                    (0.78f + i * 0.012f) * visualScale,
                    Color.Lerp(mainColor, endColor, 0.45f),
                    18 + i % 7));
            }

            int forwardBurstCount = Math.Max(10, (int)(28 * visualScale));
            for (int i = 0; i < forwardBurstCount; i++)
            {
                float lerp = i / (float)(forwardBurstCount - 1);
                float angleOffset = MathHelper.Lerp(-MathHelper.Pi / 3f, MathHelper.Pi / 3f, lerp);
                Vector2 dir = forward.RotatedBy(angleOffset);
                float speed = MathHelper.Lerp(7f, 18f, (float)Math.Sin(lerp * MathHelper.Pi));
                Vector2 velocity = dir * speed;

                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    center + dir * Main.rand.NextFloat(6f, 22f),
                    velocity,
                    false,
                    20,
                    1.25f * visualScale,
                    Color.Lerp(coreColor, startColor, 0.4f + 0.5f * (1f - Math.Abs(lerp - 0.5f) * 2f))));

                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    center,
                    velocity * 0.85f,
                    coreColor,
                    Color.Lerp(startColor, mainColor, 0.55f),
                    1.15f * visualScale,
                    18));
            }

            int dustCount = Math.Max(20, (int)(72 * visualScale));
            for (int i = 0; i < dustCount; i++)
            {
                float theta = MathHelper.TwoPi * i / dustCount;
                float wave = 0.5f + 0.5f * (float)Math.Sin(6f * theta);
                float radius = baseRadius + 110f * wave;
                Vector2 dir = theta.ToRotationVector2();
                Vector2 pos = center + dir * radius;
                Vector2 velocity = dir * (3.5f + 4.5f * wave) + dir.RotatedBy(MathHelper.PiOver2) * 1.8f;

                Dust dust = Dust.NewDustPerfect(
                    pos,
                    DustID.Electric,
                    velocity,
                    0,
                    Color.Lerp(startColor, endColor, wave * 0.55f),
                    (1.25f + wave * 0.85f) * visualScale);
                dust.noGravity = true;
                dust.fadeIn = 0.35f;
            }

            for (int i = 0; i < Math.Max(6, (int)(20 * visualScale)); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 13f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    center,
                    velocity,
                    Main.rand.NextFloat(1.2f, 1.9f) * visualScale,
                    Color.Lerp(coreColor, startColor, Main.rand.NextFloat(0.2f, 0.55f)),
                    Main.rand.Next(18, 30)));
            }

            Lighting.AddLight(center, mainColor.ToVector3() * 1.85f * visualScale);
        }

        private void SpawnNebulaBurst(bool hitTriggered)
        {
            SpawnRestoredNebulaBurst(hitTriggered);
        }

        private void SpawnRestoredNebulaBurst(bool hitTriggered)
        {
            Vector2 center = Projectile.Center;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Color mainColor = PrimaryColor;
            Color startColor = CoreColor;
            Color endColor = SecondaryColor;
            Color coreColor = CoreColor;
            float power = hitTriggered ? (IsBlueVariant ? 0.74f : 0.58f) : 0.34f;
            float radiusScale = IsBlueVariant ? 0.82f : 0.66f;
            float goldenAngle = MathHelper.TwoPi * 0.38196601125f;

            int spiralCount = Math.Max(8, (int)(18 * power));
            for (int i = 0; i < spiralCount; i++)
            {
                float t = i + 1f;
                float angle = goldenAngle * t;
                float radius = 6.5f * radiusScale * (float)Math.Sqrt(t);
                Vector2 offset = angle.ToRotationVector2() * radius;
                Vector2 radialDir = offset.SafeNormalize(Vector2.UnitX);
                Vector2 tangentDir = radialDir.RotatedBy(MathHelper.PiOver2);

                SquishyLightParticle particle = new(
                    center + offset,
                    radialDir * (0.9f + t * 0.045f) + tangentDir * (1.55f + t * 0.025f),
                    (0.55f + t * 0.012f) * power,
                    Color.Lerp(startColor, coreColor, 0.3f + 0.55f * (i / (float)Math.Max(1, spiralCount - 1))),
                    18 + (i % 8)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            int ringCount = Math.Max(16, (int)(30 * power));
            float outerRadius = 88f * radiusScale;
            for (int i = 0; i < ringCount; i++)
            {
                float theta = MathHelper.TwoPi * i / ringCount;
                float modulation = 1f + 0.16f * (float)Math.Cos(3f * theta) + 0.1f * (float)Math.Sin(5f * theta);
                float radius = outerRadius * modulation;
                Vector2 dir = theta.ToRotationVector2();
                Vector2 pos = center + dir * radius;
                Vector2 tangent = dir.RotatedBy(MathHelper.PiOver2);

                CritSpark spark = new(
                    pos,
                    tangent * 3.7f + dir * 1.15f,
                    Color.Lerp(coreColor, startColor, 0.45f),
                    Color.Lerp(mainColor, endColor, 0.45f),
                    0.72f * power,
                    17
                );
                GeneralParticleHandler.SpawnParticle(spark);

                if (i % 2 == 0)
                {
                    AltSparkParticle sparkLine = new(
                        pos - tangent * 10f,
                        tangent * 0.28f,
                        false,
                        10,
                        0.95f * power,
                        Color.Lerp(startColor, mainColor, 0.5f) * 0.22f
                    );
                    GeneralParticleHandler.SpawnParticle(sparkLine);
                }
            }

            int lissajousCount = Math.Max(16, (int)(28 * power));
            float amplitudeX = 74f * radiusScale;
            float amplitudeY = 58f * radiusScale;
            for (int i = 0; i < lissajousCount; i++)
            {
                float t = MathHelper.TwoPi * i / lissajousCount;
                Vector2 pos = center + new Vector2(
                    amplitudeX * (float)Math.Sin(3f * t + MathHelper.PiOver2),
                    amplitudeY * (float)Math.Sin(4f * t)
                );
                Vector2 deriv = new Vector2(
                    3f * amplitudeX * (float)Math.Cos(3f * t + MathHelper.PiOver2),
                    4f * amplitudeY * (float)Math.Cos(4f * t)
                ).SafeNormalize(Vector2.UnitX);

                PointParticle point = new(
                    pos,
                    deriv * 3.1f,
                    false,
                    13,
                    0.72f * power,
                    Color.Lerp(mainColor, startColor, 0.55f)
                );
                GeneralParticleHandler.SpawnParticle(point);
            }

            int roseCount = Math.Max(14, (int)(22 * power));
            float roseRadius = 92f * radiusScale;
            for (int i = 0; i < roseCount; i++)
            {
                float theta = MathHelper.TwoPi * i / roseCount;
                float rose = roseRadius * (float)Math.Cos(7f * theta);
                Vector2 dir = theta.ToRotationVector2();
                Vector2 pos = center + dir * rose + forward * (Vector2.Dot(dir, forward) * 12f);
                Vector2 velocity = dir.RotatedBy(MathHelper.PiOver2) * 2.7f + dir * 1.5f;

                CritSpark petalSpark = new(
                    pos,
                    velocity,
                    Color.Lerp(coreColor, startColor, 0.5f),
                    Color.Lerp(mainColor, endColor, 0.6f),
                    0.65f * power,
                    15
                );
                GeneralParticleHandler.SpawnParticle(petalSpark);
            }

            int vortexCount = Math.Max(12, (int)(22 * power));
            for (int i = 0; i < vortexCount; i++)
            {
                float theta = MathHelper.TwoPi * i / vortexCount;
                Vector2 dir = theta.ToRotationVector2();
                Vector2 posA = center + dir * (32f + i * 1.4f) * radiusScale;
                Vector2 posB = center - dir * (44f + i * 1.05f) * radiusScale;

                SquishyLightParticle particleA = new(
                    posA,
                    dir.RotatedBy(MathHelper.PiOver2) * (2.2f + i * 0.035f),
                    (0.5f + i * 0.006f) * power,
                    Color.Lerp(startColor, coreColor, 0.35f),
                    14 + (i % 6)
                );
                GeneralParticleHandler.SpawnParticle(particleA);

                SquishyLightParticle particleB = new(
                    posB,
                    dir.RotatedBy(-MathHelper.PiOver2) * (1.95f + i * 0.035f),
                    (0.48f + i * 0.007f) * power,
                    Color.Lerp(mainColor, endColor, 0.45f),
                    14 + (i % 6)
                );
                GeneralParticleHandler.SpawnParticle(particleB);
            }

            int forwardBurstCount = Math.Max(10, (int)(18 * power));
            for (int i = 0; i < forwardBurstCount; i++)
            {
                float lerp = i / (float)Math.Max(1, forwardBurstCount - 1);
                Vector2 dir = forward.RotatedBy(MathHelper.Lerp(-0.9f, 0.9f, lerp));
                Vector2 velocity = dir * MathHelper.Lerp(4.8f, 12.5f, (float)Math.Sin(lerp * MathHelper.Pi));

                PointParticle point = new(
                    center + dir * Main.rand.NextFloat(4f, 16f),
                    velocity,
                    false,
                    15,
                    0.8f * power,
                    Color.Lerp(coreColor, startColor, 0.35f + 0.45f * (1f - Math.Abs(lerp - 0.5f) * 2f))
                );
                GeneralParticleHandler.SpawnParticle(point);
            }

            int dustCount = Math.Max(18, (int)(40 * power));
            for (int i = 0; i < dustCount; i++)
            {
                float theta = MathHelper.TwoPi * i / dustCount;
                float wave = 0.5f + 0.5f * (float)Math.Sin(6f * theta);
                Vector2 dir = theta.ToRotationVector2();
                Vector2 velocity = dir * (2.2f + 3.2f * wave) + dir.RotatedBy(MathHelper.PiOver2) * 1.1f;

                Dust dust = Dust.NewDustPerfect(
                    center + dir * ((36f + 72f * wave) * radiusScale),
                    IsBlueVariant ? DustID.BlueTorch : DustID.PurpleTorch,
                    velocity,
                    0,
                    Color.Lerp(startColor, endColor, wave * 0.55f),
                    (0.75f + wave * 0.5f) * power
                );
                dust.noGravity = true;
                dust.fadeIn = 0.24f;
            }

            for (int i = 0; i < Math.Max(5, (int)(10 * power)); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3.5f, 9f);
                SquishyLightParticle particle = new(
                    center,
                    velocity,
                    Main.rand.NextFloat(0.75f, 1.15f) * power,
                    Color.Lerp(coreColor, startColor, Main.rand.NextFloat(0.2f, 0.55f)),
                    Main.rand.Next(15, 24)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            Lighting.AddLight(center, PrimaryColor.ToVector3() * (IsBlueVariant ? 1.15f : 0.85f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Texture2D fadeStreak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;
            Texture2D magicPoint = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
            Texture2D runeStar = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_05").Value;
            Texture2D magicRune = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_02").Value;
            Texture2D twirlRune = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/twirl_02").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 tipPosition = drawPosition + forward * Projectile.scale * (IsBlueVariant ? 18f : 15f);
            Color primary = PrimaryColor;
            Color secondary = SecondaryColor;
            Color core = CoreColor;
            float fadeIn = Utils.GetLerpValue(0f, 14f, Timer, true);
            float fadeOut = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            float tipOpacity = MathHelper.Max(opacity, 0.62f * fadeIn);
            float pulse = 1f + (float)Math.Sin((Timer + Projectile.ai[2] * 17f) * 0.16f) * 0.08f;
            const float magicDrawScale = 1.3f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            float magicRotation = Main.GlobalTimeWrappedHourly * MathHelper.TwoPi * (IsBlueVariant ? 0.72f : 0.56f);
            for (int i = 0; i < 3; i++)
            {
                float angle = magicRotation * (1f + i * 0.18f) + MathHelper.TwoPi * i / 3f;
                Color tipColor = Color.Lerp(core, i == 1 ? secondary : primary, 0.45f);
                tipColor.A = 220;

                Main.EntitySpriteDraw(
                    magicPoint,
                    tipPosition,
                    null,
                    tipColor * tipOpacity * (IsBlueVariant ? 1.8f : 1.45f),
                    angle,
                    magicPoint.Size() * 0.5f,
                    (0.34f + i * 0.08f) * pulse * Projectile.scale * magicDrawScale,
                    SpriteEffects.None,
                    0f);
            }

            Texture2D[] tipRunes = { runeStar, magicRune, twirlRune };
            for (int i = 0; i < tipRunes.Length; i++)
            {
                Texture2D rune = tipRunes[i];
                Color runeColor = Color.Lerp(primary, i == 0 ? core : secondary, 0.38f);
                runeColor.A = 185;

                Main.EntitySpriteDraw(
                    rune,
                    tipPosition,
                    null,
                    runeColor * tipOpacity * (IsBlueVariant ? 0.7f : 0.58f),
                    Projectile.rotation + magicRotation * (i % 2 == 0 ? 0.42f : -0.34f) + MathHelper.TwoPi * i / 3f,
                    rune.Size() * 0.5f,
                    (0.25f * (0.32f + i * 0.08f)) * Projectile.scale * pulse * magicDrawScale,
                    SpriteEffects.None,
                    0f);
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = magicRotation + MathHelper.TwoPi * i / 4f;
                Color ringColor = Color.Lerp(primary, i % 2 == 0 ? secondary : core, 0.35f);
                ringColor.A = 0;
                float ringScale = (IsBlueVariant ? 0.34f : 0.27f) + i * 0.045f;

                Main.EntitySpriteDraw(
                    magicPoint,
                    drawPosition,
                    null,
                    ringColor * opacity * (IsBlueVariant ? 1.25f : 1f),
                    angle,
                    magicPoint.Size() * 0.5f,
                    ringScale * pulse * Projectile.scale * magicDrawScale,
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                primary with { A = 0 } * 0.32f * opacity,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * (IsBlueVariant ? 0.44f : 0.34f) * pulse * magicDrawScale,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 3; i++)
            {
                float rotation = Projectile.rotation + MathHelper.TwoPi * i / 3f;
                Main.EntitySpriteDraw(
                    fadeStreak,
                    drawPosition,
                    null,
                    Color.Lerp(primary, secondary, i / 2f) with { A = 0 } * 0.42f * opacity,
                    rotation,
                    new Vector2(fadeStreak.Width * 0.5f, 0f),
                    new Vector2(0.84f + i * 0.12f, 0.34f) * Projectile.scale * pulse * magicDrawScale,
                    SpriteEffects.FlipVertically,
                    0f);
            }

            Main.EntitySpriteDraw(
                star,
                drawPosition,
                null,
                core with { A = 0 } * 0.72f * opacity,
                Projectile.rotation,
                star.Size() * 0.5f,
                new Vector2(0.28f, 0.54f) * Projectile.scale * pulse * magicDrawScale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                star,
                drawPosition,
                null,
                secondary with { A = 0 } * 0.46f * opacity,
                Projectile.rotation + MathHelper.PiOver2,
                star.Size() * 0.5f,
                new Vector2(0.18f, 0.42f) * Projectile.scale * pulse * magicDrawScale,
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
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                Projectile.oldPos.Length * 2);
        }

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float width = Projectile.scale * (IsBlueVariant ? 18f : 13f);
            float spawnFade = MathHelper.Lerp(0.28f, 1f, Utils.GetLerpValue(0f, 28f, Timer, true));
            float startTaper = MathHelper.Lerp(0.35f, 1f, Utils.GetLerpValue(0.06f, 0.32f, completion, true));
            return MathF.Sin((1f - completion) * MathHelper.PiOver2) * width * spawnFade * startTaper;
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color start = Color.Lerp(CoreColor, PrimaryColor, 0.35f);
            start.A = 0;
            Color end = Color.Lerp(SecondaryColor, Color.Transparent, Utils.GetLerpValue(0.58f, 1f, completion, true));
            end.A = 0;
            return Color.Lerp(start, end, completion);
        }
    }
}
