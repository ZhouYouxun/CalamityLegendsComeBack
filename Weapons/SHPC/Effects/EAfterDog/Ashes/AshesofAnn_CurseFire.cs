using CalamityLegendsComeBack.Systems;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    internal sealed class AshesofAnn_CurseFire : ModProjectile, ILocalizedModType
    {
        private const float BaseSpeed = 15.5f;
        private const float HomingRange = 150f * 16f;
        private const float HomingStartFrames = 9f;
        private const float HomingWarmupFrames = 18f;
        private const float MinHomingSpeed = 10.5f;
        private const float MaxHomingSpeed = 16.5f;
        private const float HomingInertia = 22f;
        private const float FreeFlightDamping = 0.996f;
        private const float NoTargetDamping = 0.992f;
        private const float WanderingTurnStrength = 0.006f;
        private const float NonHomingTurnStrength = 0.011f;
        private const int TotalRelayShots = 16;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float HitSomething => ref Projectile.localAI[1];

        private int ShotIndex => (int)Projectile.ai[1];
        private bool IsPiercingShot => Projectile.ai[0] > 0f;
        private float ShotCompletion => MathHelper.Clamp(ShotIndex / (float)(TotalRelayShots - 1), 0f, 1f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 26;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            // Each ember survives its first connection. The paired sweep is eight beats on
            // each side, and every individual ember is allowed to strike twice.
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = Vector2.UnitX * BaseSpeed;

            if (!IsPiercingShot)
                return;

            Projectile.penetrate = -1;
            Projectile.extraUpdates = 0;
            Projectile.timeLeft = 280; // non-homing / wandering template lifetime
            Projectile.localNPCHitCooldown = 8;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * BaseSpeed;
        }

        public override void AI()
        {
            if (Projectile.numUpdates == 0)
                Timer++;

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (IsPiercingShot)
            {
                UpdatePiercingFlight(currentDirection);
            }
            else if (Timer >= HomingStartFrames)
            {
                NPC target = FindNearestTarget(HomingRange);
                if (target is not null)
                    SoftHomeTowardTarget(target, currentDirection);
                else
                    FreeDrift(NoTargetDamping);
            }
            else
            {
                FreeDrift(FreeFlightDamping);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 24);
            Lighting.AddLight(Projectile.Center, new Color(255, 120, 48).ToVector3() * 0.55f);

            // Blazing-cursor fire rides ONLY the homing form; the piercing / self-wandering
            // form deliberately carries no fluid fire.
            if (!IsPiercingShot)
            {
                AshesFluidFieldSystem.RegisterSource(
                    Projectile.Center,
                    Projectile.velocity * -0.055f,
                    Color.Lerp(new Color(255, 92, 34), new Color(255, 206, 112), 0.28f + ShotCompletion * 0.22f),
                    0.7f * Utils.GetLerpValue(255f, 0f, Projectile.alpha, true));
            }

            if (!Main.dedServ)
            {
                SpawnFlightEffects();
                SpawnCalamitousDartMetaballs(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                SpawnAnnGlowOrb();
            }
        }

        private void SoftHomeTowardTarget(NPC target, Vector2 fallbackDirection)
        {
            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();
            if (currentSpeed < 0.1f)
                currentVelocity = (target.Center - Projectile.Center).SafeNormalize(fallbackDirection) * 4f;

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentVelocity.SafeNormalize(fallbackDirection));

            float homingTimer = Timer - HomingStartFrames;
            float warmup = Utils.GetLerpValue(0f, HomingWarmupFrames, homingTimer, true);
            float closePressure = Utils.GetLerpValue(360f, 70f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.35f, 1f, MathHelper.Max(warmup, closePressure * 0.75f));

            float targetSpeed = MathHelper.Lerp(MinHomingSpeed, MaxHomingSpeed, pullStrength);
            Vector2 desiredVelocity = desiredDirection * targetSpeed;
            Projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

            float sideSway = (float)Math.Sin((Timer + Projectile.identity * 7f) * 0.075f) *
                MathHelper.Lerp(0.012f, 0.004f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            if (Projectile.velocity.Length() > MaxHomingSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxHomingSpeed;
        }

        private void FreeDrift(float damping)
        {
            float wander = (float)Math.Sin((Timer + Projectile.identity * 5f) * 0.08f) * WanderingTurnStrength;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        private void UpdatePiercingFlight(Vector2 currentDirection)
        {
            float wander = (float)Math.Sin((Timer + Projectile.identity * 5f) * 0.08f) * NonHomingTurnStrength;
            wander += (float)Math.Sin((Timer + ShotIndex * 19f) * 0.047f) * 0.004f;
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), BaseSpeed, 0.08f);
            Projectile.velocity = currentDirection.RotatedBy(wander) * speed;
        }

        private NPC FindNearestTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestTarget = npc;
                bestDistance = distance;
            }

            return bestTarget;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float finalShotBoost = ShotIndex == TotalRelayShots - 1 ? 1.12f : 1f;
            modifiers.SourceDamage *= MathHelper.Lerp(0.92f, 1.08f, ShotCompletion) * finalShotBoost;
            modifiers.FlatBonusDamage += Math.Min(target.lifeMax / 420f, Projectile.damage * 0.45f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HitSomething = 1f;
            SoundEngine.PlaySound(SoundID.Item74, target.Center);
            target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 180);
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 240);
            target.AddBuff(BuffID.OnFire, 240);
            target.AddBuff(BuffID.CursedInferno, 180);

            if (!Main.dedServ)
                SpawnImpactEffects();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            SpawnImpactEffects();

            if (HitSomething != 1f)
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.22f, Pitch = -0.08f + ShotCompletion * 0.14f, PitchVariance = 0.12f, MaxInstances = 8 }, Projectile.Center);
        }

        private void SpawnImpactEffects()
        {
            Color orange = new(255, 140, 42);
            Color red = new(180, 12, 8);

            SpawnSoulSignatureImpact();

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    velocity,
                    "CalamityMod/Particles/VerticalSmear",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.6f, 1.2f),
                    Color.Lerp(orange, red, Main.rand.NextFloat(0.15f, 0.65f)),
                    new Vector2(0.18f, 0.86f),
                    true,
                    true,
                    shrinkSpeed: 0.82f,
                    glowOpacity: 0.48f));
            }
        }

        private void SpawnFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color orange = new(255, 140, 42);
            Color red = new(180, 12, 8);

            if (IsPiercingShot)
            {
                SpawnBladeDisc(direction, normal, orange, red);
                SpawnViolenceTearSparks(direction, normal);
            }
            else
            {
                SpawnSoulHunterOrbit(direction, normal);
                SpawnSupremeHomingFlightEffects(direction, normal);
            }
        }

        // The seeker borrows SCal's cast trail hierarchy: hot glow-orbs as the readable
        // silhouette, a restrained point wake, and occasional spell-ring/mist accents.
        private void SpawnSupremeHomingFlightEffects(Vector2 direction, Vector2 normal)
        {
            if (Projectile.numUpdates != 0 || (int)Timer % 3 != 0)
                return;

            Color brimstone = Main.rand.NextBool() ? Color.Red : Color.Lerp(Color.Red, Color.Magenta, 0.48f);
            Vector2 trailPosition = Projectile.Center - direction * Main.rand.NextFloat(8f, 18f) + normal * Main.rand.NextFloat(-5f, 5f);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                trailPosition,
                -direction * Main.rand.NextFloat(1.2f, 3.1f) + normal * Main.rand.NextFloat(-0.45f, 0.45f),
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.22f, 0.36f),
                brimstone,
                true,
                false));

            if ((int)Timer % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center - direction * 13f,
                    -direction * Main.rand.NextFloat(2.2f, 4.6f) + normal * Main.rand.NextFloat(-0.7f, 0.7f),
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.34f, 0.52f),
                    Color.Lerp(brimstone, Color.White, 0.24f)));
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center - direction * 15f,
                    -direction * Main.rand.NextFloat(0.35f, 0.9f),
                    brimstone,
                    new Color(46, 0, 18),
                    Main.rand.NextFloat(0.22f, 0.34f),
                    Main.rand.NextFloat(110f, 150f),
                    0.02f));
            }

            if ((int)Timer % 12 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center - direction * 5f,
                    -direction * 0.18f,
                    brimstone,
                    new Vector2(0.34f, 0.78f),
                    direction.ToRotation(),
                    0.03f,
                    0.21f,
                    11));
            }
        }

        private void SpawnBladeDisc(Vector2 direction, Vector2 normal, Color orange, Color red)
        {
            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center - direction * Main.rand.NextFloat(2f, 12f) + normal * Main.rand.NextFloat(-4f, 4f),
                Projectile.velocity * 0.02f,
                "CalamityMod/Particles/VerticalSmear",
                false,
                Main.rand.Next(13, 18),
                Main.rand.NextFloat(1.25f, 1.85f),
                Color.Lerp(orange, red, Main.rand.NextFloat(0.15f, 0.65f)),
                new Vector2(0.18f, 0.86f),
                true,
                true,
                shrinkSpeed: 0.82f,
                glowOpacity: 0.48f));
        }

        private void SpawnCalamitousDartMetaballs(Vector2 direction)
        {
            CalamitasMetaball.Particle point = CalamitasMetaball.SpawnParticle(
                Projectile.Center + Projectile.velocity * 2f,
                Vector2.Zero,
                40f * Projectile.scale);

            point.rotation = direction.ToRotation() + MathHelper.PiOver2;
            point.TextureToUse = ModContent.Request<Texture2D>("CalamityMod/Particles/PointParticle").Value;
            point.SizeScaling = 0.65f;

            CalamitasMetaball.Particle body = CalamitasMetaball.SpawnParticle(
                Projectile.Center,
                Main.rand.NextVector2Circular(3f, 3f),
                24f * Projectile.scale);

            body.SizeScaling = 0.8f;
        }

        private void SpawnSoulHunterOrbit(Vector2 direction, Vector2 normal)
        {
            if ((int)Timer % 4 != 0)
                return;

            float phase = Timer * 0.23f + ShotIndex * 0.61f;
            for (int i = 0; i < 2; i++)
            {
                float angle = phase + i * MathHelper.Pi;
                Vector2 offset = normal * MathF.Sin(angle) * 7f + direction * MathF.Cos(angle) * 2.5f;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    Main.rand.NextBool() ? DustID.BlueTorch : DustID.GemSapphire,
                    -direction * Main.rand.NextFloat(0.4f, 1.2f) - offset.SafeNormalize(Vector2.Zero) * 0.25f,
                    90,
                    Color.Lerp(new Color(20, 44, 122), new Color(64, 220, 255), Main.rand.NextFloat(0.25f, 0.72f)),
                    Main.rand.NextFloat(0.65f, 1.0f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center - direction * 6f,
                    -direction * 0.4f,
                    new Color(42, 82, 180) * 0.65f,
                    new Vector2(0.24f, 0.72f),
                    direction.ToRotation(),
                    0.08f,
                    0.035f,
                    10));
            }
        }

        private void SpawnViolenceTearSparks(Vector2 direction, Vector2 normal)
        {
            if ((int)Timer % 3 != 0)
                return;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 velocity = (-direction * Main.rand.NextFloat(1.2f, 3.6f) + normal * side * Main.rand.NextFloat(1.6f, 4.4f));
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(3f, 12f) + normal * side * Main.rand.NextFloat(3f, 7f),
                    velocity,
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.42f, 0.78f),
                    Color.Lerp(new Color(82, 0, 0), new Color(255, 64, 32), Main.rand.NextFloat(0.25f, 0.8f))));
            }
        }

        private void SpawnSoulSignatureImpact()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color mainColor = IsPiercingShot ? new Color(170, 16, 24) : new Color(30, 88, 190);
            Color accent = IsPiercingShot ? new Color(255, 118, 42) : new Color(84, 225, 255);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.35f,
                Color.Lerp(mainColor, accent, 0.28f),
                IsPiercingShot ? new Vector2(0.42f, 1.05f) : new Vector2(0.36f, 0.88f),
                direction.ToRotation(),
                0.08f,
                0.16f,
                12));

            int count = IsPiercingShot ? 5 : 4;
            for (int i = 0; i < count; i++)
            {
                Vector2 burstDirection = direction.RotatedBy(MathHelper.Lerp(-0.58f, 0.58f, count == 1 ? 0.5f : i / (count - 1f)));
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + burstDirection * Main.rand.NextFloat(3f, 10f),
                    burstDirection * Main.rand.NextFloat(3.5f, 8f),
                    false,
                    Main.rand.Next(9, 15),
                    Main.rand.NextFloat(0.18f, 0.36f),
                    i % 2 == 0 ? accent : mainColor));
            }
        }

        private void SpawnAnnGlowOrb()
        {
            if (!Main.rand.NextBool(IsPiercingShot ? 5 : 9))
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center - direction * Main.rand.NextFloat(3f, 12f) + Main.rand.NextVector2Circular(4f, 4f),
                -direction * Main.rand.NextFloat(0.2f, 0.8f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.12f, 0.22f),
                Color.Lerp(new Color(255, 76, 34), new Color(255, 220, 120), Main.rand.NextFloat(0.18f, 0.64f)),
                true,
                false,
                true));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmear").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(255f, 0f, Projectile.alpha, true);
            Color orange = new Color(255, 140, 42, 0) * opacity;
            Color red = new Color(170, 8, 8, 0) * opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, red * 0.52f, Projectile.rotation, bloom.Size() * 0.5f, 0.28f, SpriteEffects.None);
            Main.EntitySpriteDraw(smear, drawPosition, null, orange * 0.68f, Projectile.rotation, smear.Size() * 0.5f, new Vector2(0.12f, 0.58f), SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White * (0.28f * opacity), Projectile.rotation, star.Size() * 0.5f, new Vector2(0.18f, 0.42f), SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
