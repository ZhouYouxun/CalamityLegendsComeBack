using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFArrow_ABreak : ModProjectile
    {
        private const float SlowdownStartFrame = 52f;
        private const float SlowdownDuration = 56f;
        private const float PostImpactSlowdownStartFrame = 36f;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/ABreak/BFArrow_ABreak";
        // A tactical right-click arrow: homing breakthrough that keeps flying after the first impact.
        private ref float BounceCounter => ref Projectile.ai[0];
        private ref float HomingDisabled => ref Projectile.ai[1];
        private ref float FlightTimer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 240, penetrate: 2, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage()
        {
            if (GetSlowdownProgress() > 0.78f)
                return false;

            return null;
        }

        public override void AI()
        {
            FlightTimer++;
            float slowdownProgress = GetSlowdownProgress();
            float glowStrength = MathHelper.Lerp(1f, 0.08f, slowdownProgress);
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak).ToVector3() * (0.18f + 0.32f * glowStrength));
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 1.05f);
            EmitBreakthroughFlightFX(glowStrength);

            if (slowdownProgress > 0f)
            {
                UpdateSlowdown(slowdownProgress);
                BFArrowCommon.FaceForward(Projectile);
                return;
            }

            if (HomingDisabled == 1f)
            {
                AccelerateStraightFlight(1.018f, 30f);
            }
            else if (FlightTimer >= 18f)
            {
                NPC target = Projectile.Center.ClosestNPCAt(1020f);
                if (target != null)
                {
                    BFArrowCommon.DirectHomeTowards(Projectile, target, 0.22f, 21f);
                    BFArrowCommon.MaintainSpeed(Projectile, 21f, 0.14f);
                }
                else
                {
                    AccelerateStraightFlight(1.006f, 21f);
                }
            }
            else
            {
                AccelerateStraightFlight(1.006f, 21f);
            }

            BFArrowCommon.FaceForward(Projectile);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.35f, Pitch = 0.2f }, Projectile.Center);
            if (BFArrowCommon.Bounce(Projectile, oldVelocity, ref BounceCounter, 3, 0.98f))
                return true;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 10, 0.9f, 2.8f, 0.8f, 1.25f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (GetSlowdownProgress() > 0.25f)
                SpawnBreakthroughVanishFX(Projectile.Center, 0.72f);
            else
                BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 10, 0.9f, 3.2f, 0.8f, 1.18f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HomingDisabled = 1f;
            FlightTimer = Math.Max(FlightTimer, PostImpactSlowdownStartFrame);
            Projectile.netUpdate = true;
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 8, 0.9f, 2.6f, 0.8f, 1.15f);
            SpawnBreakthroughImpactFX(target.Center, 1.1f);
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.22f, Pitch = 0.25f }, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_ABreak);
            return false;
        }

        private float GetSlowdownProgress()
        {
            float startFrame = HomingDisabled == 1f ? PostImpactSlowdownStartFrame : SlowdownStartFrame;
            return Utils.GetLerpValue(startFrame, startFrame + SlowdownDuration, FlightTimer, true);
        }

        private void UpdateSlowdown(float slowdownProgress)
        {
            float drag = MathHelper.Lerp(0.965f, 0.875f, slowdownProgress);
            Projectile.velocity *= drag;
            Projectile.Opacity = MathF.Pow(1f - slowdownProgress, 1.45f);

            if (Projectile.velocity.LengthSquared() < 1.8f * 1.8f || slowdownProgress >= 1f)
            {
                Projectile.Kill();
                return;
            }

            if (Main.dedServ || !Projectile.FinalExtraUpdate() || (int)FlightTimer % 4 != 0)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                DustID.TerraBlade,
                -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(0.45f, 1.25f),
                110,
                Color.Lerp(mainColor, Color.White, 0.22f),
                Main.rand.NextFloat(0.75f, 1.05f) * MathHelper.Lerp(0.9f, 0.35f, slowdownProgress));
            dust.noGravity = true;
        }

        private void EmitBreakthroughFlightFX(float glowStrength)
        {
            if (Main.dedServ || glowStrength <= 0.02f || !Projectile.FinalExtraUpdate())
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            if ((int)FlightTimer % 2 == 0)
            {
                DirectionalPulseRing pulse = new(
                    Projectile.Center + direction * 8f,
                    Projectile.velocity * 0.05f,
                    Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), Color.White, 0.2f),
                    new Vector2(0.36f, 0.925f),
                    direction.ToRotation(),
                    0.14f * glowStrength,
                    0.04f,
                    10);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            GenericSparkle edgeSpark = new(
                Projectile.Center + normal * Main.rand.NextFloat(-5f, 5f),
                Projectile.velocity * 0.04f,
                Color.White,
                BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_ABreak),
                0.475f * glowStrength,
                7,
                0f,
                0.6f * glowStrength);
            GeneralParticleHandler.SpawnParticle(edgeSpark);
        }

        private void AccelerateStraightFlight(float acceleration, float maxSpeed)
        {
            if (Projectile.velocity.LengthSquared() <= 0.0001f)
                return;

            Projectile.velocity *= acceleration;
            if (Projectile.velocity.LengthSquared() > maxSpeed * maxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * maxSpeed;
        }

        private void SpawnBreakthroughImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.75f,
                Color.Lerp(mainColor, Color.White, 0.18f),
                new Vector2(0.8f, 1.75f),
                Projectile.velocity.ToRotation(),
                0.15f * intensity,
                0.034f,
                10));

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, accentColor, 0.35f),
                0.36f * intensity,
                9));
        }

        private void SpawnBreakthroughVanishFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.2f),
                0.24f * intensity,
                8));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                mainColor,
                Vector2.One,
                Main.rand.NextFloat(-0.4f, 0.4f),
                0.08f * intensity,
                0.022f,
                8));

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 5, 0.65f, 1.8f, 0.55f, 0.9f);
        }
    }
}
