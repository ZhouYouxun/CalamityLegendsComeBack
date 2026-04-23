using System;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFArrow_BRecov : ModProjectile
    {
        private const int LifetimeFrames = 14;
        private const float MaxFlightSpeed = 26.5f;
        private const float HitExplosionDamageMultiplier = 0.6f;

        private bool releasedRecoveryOrbs;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BRecov/BFArrow_BRecov";

        private ref float FlightTimer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 14;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 24, penetrate: -1, extraUpdates: 3, tileCollide: true);
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => null;

        public override bool? CanHitNPC(NPC target) => null;

        public override void AI()
        {
            FlightTimer++;
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov).ToVector3() * 0.58f);

            BFArrowCommon.FaceForward(Projectile);
            AccelerateFlight();
            EmitDenseRecoveryTrail();

            if (FlightTimer >= LifetimeFrames * Projectile.MaxUpdates || Projectile.timeLeft < 12 * Projectile.MaxUpdates)
                Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnPenetrationImpactFX(target.Center, 1.1f);
            SpawnHitBlast(target.Center);
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 16, 1.4f, 4.4f, 1f, 1.5f);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.42f, Pitch = 0.3f }, target.Center);

            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                float boostedSpeed = Math.Min(Projectile.velocity.Length() + 0.8f, MaxFlightSpeed + 1.5f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY) * boostedSpeed;
                Projectile.netUpdate = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnPenetrationImpactFX(Projectile.Center, 0.92f);
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.26f, Pitch = 0.18f }, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!releasedRecoveryOrbs)
            {
                releasedRecoveryOrbs = true;
                ReleaseRecoveryOrbs(Projectile.Center);
            }

            SpawnRecoveryPulse(Projectile.Center, 1.25f);
            SpawnOrbReleaseFX(Projectile.Center, 1.28f);
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 20, 1.5f, 5f, 1f, 1.6f);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.36f, Pitch = 0.22f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_BRecov, 1.04f);
            return false;
        }

        private void AccelerateFlight()
        {
            float currentSpeed = Projectile.velocity.Length();
            if (currentSpeed <= 0.01f)
                return;

            float nextSpeed = Math.Min(currentSpeed + 0.16f, MaxFlightSpeed);
            Projectile.velocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY) * nextSpeed;
        }

        private void EmitDenseRecoveryTrail()
        {
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 1.45f);
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 1.25f);

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            Vector2 direction = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                Vector2 spawnPosition = Projectile.Center - direction * Main.rand.NextFloat(4f, 12f) + normal * Main.rand.NextFloat(-9f, 9f);
                Dust dust = Dust.NewDustPerfect(
                    spawnPosition,
                    DustID.GemEmerald,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                    100,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.15f, 0.55f)),
                    Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }

            if (Main.dedServ || !Projectile.FinalExtraUpdate())
                return;

            if ((int)FlightTimer % 2 == 0)
            {
                GlowOrbParticle orb = new(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    -Projectile.velocity * 0.025f + Main.rand.NextVector2Circular(0.55f, 0.55f),
                    false,
                    8,
                    Main.rand.NextFloat(0.24f, 0.38f),
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.25f, 0.5f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if ((int)FlightTimer % 5 == 0)
            {
                DirectionalPulseRing pulse = new(
                    Projectile.Center - direction * 10f,
                    Projectile.velocity * 0.04f,
                    Color.Lerp(mainColor, Color.White, 0.22f),
                    new Vector2(0.8f, 2.4f),
                    direction.ToRotation(),
                    0.16f,
                    0.035f,
                    11);
                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        private void SpawnHitBlast(Vector2 center)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<BFArrow_BRecovBlast>(),
                Math.Max(1, (int)(Projectile.damage * HitExplosionDamageMultiplier)),
                Projectile.knockBack * 0.8f,
                Projectile.owner);
        }

        private void ReleaseRecoveryOrbs(Vector2 center)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
                return;

            int orbCount = Main.rand.Next(3, 8);
            for (int i = 0; i < orbCount; i++)
            {
                int targetIndex = BFArrow_BRecovTransfer.FindRandomInjuredPlayerIndex(owner, center, 2400f);
                Vector2 velocity =
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 6f) +
                    new Vector2(0f, Main.rand.NextFloat(-2.5f, -0.8f));

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center,
                    velocity,
                    ModContent.ProjectileType<BFArrow_BRecovTransfer>(),
                    0,
                    0f,
                    Projectile.owner,
                    targetIndex,
                    3f);
            }
        }

        private void SpawnRecoveryPulse(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.28f),
                Vector2.One,
                0f,
                0.2f * intensity,
                0.04f,
                16);
            GeneralParticleHandler.SpawnParticle(pulse);

            GenericSparkle sparkle = new(
                center,
                Vector2.Zero,
                Color.White,
                Color.Lerp(mainColor, Color.White, 0.4f),
                1.2f * intensity,
                8,
                0f,
                1.35f);
            GeneralParticleHandler.SpawnParticle(sparkle);
        }

        private void SpawnOrbReleaseFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color coreColor = new(110, 255, 150);
            StrongBloom bloom = new(center, Vector2.Zero, coreColor, 0.95f * intensity, 14);
            GeneralParticleHandler.SpawnParticle(bloom);

            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(coreColor, Color.White, 0.22f),
                new Vector2(1.2f, 1.2f),
                0f,
                0.16f * intensity,
                0.032f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 10; i++)
            {
                GlowOrbParticle orb = new(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 3.6f),
                    false,
                    12,
                    Main.rand.NextFloat(0.22f, 0.36f) * intensity,
                    Color.Lerp(coreColor, Color.White, Main.rand.NextFloat(0.15f, 0.5f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }

        private void SpawnPenetrationImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);

            StrongBloom bloom = new(center, Vector2.Zero, Color.Lerp(mainColor, Color.White, 0.18f), 0.7f * intensity, 10);
            GeneralParticleHandler.SpawnParticle(bloom);

            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.22f),
                new Vector2(1f, 1.6f),
                Main.rand.NextFloat(-0.2f, 0.2f),
                0.14f * intensity,
                0.03f,
                10);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GemEmerald,
                    Main.rand.NextVector2CircularEdge(2.8f, 2.8f) * Main.rand.NextFloat(1.2f, 3.2f),
                    100,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.15f, 0.55f)),
                    Main.rand.NextFloat(1f, 1.4f));
                dust.noGravity = true;
            }
        }
    }
}
