using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // B 战术右键箭：击中后爆散治疗光球，每颗独立追踪随机伤员并治疗。
    internal class BFArrow_BRecov : ModProjectile
    {
        private bool releasedRecoveryOrbs;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BRecov/BFArrow_BRecov";

        private ref float FlightTimer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 210, penetrate: 1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => null;

        public override bool? CanHitNPC(NPC target) => null;

        public override void AI()
        {
            FlightTimer++;
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov).ToVector3() * 0.42f);

            BFArrowCommon.FaceForward(Projectile);
            Projectile.velocity *= 0.998f;
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 1.08f);

            if (FlightTimer >= 34f || Projectile.timeLeft < 120)
                Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (releasedRecoveryOrbs)
                return;

            releasedRecoveryOrbs = true;
            Projectile.friendly = false;
            Projectile.damage = 0;
            Projectile.tileCollide = false;
            Projectile.velocity *= 0.2f;

            SpawnRecoveryPulse(target.Center, 1.15f);
            SpawnOrbReleaseFX(target.Center, 1.1f);
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 10, 0.9f, 2.8f, 0.9f, 1.25f);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.38f, Pitch = 0.32f }, target.Center);
            ReleaseRecoveryOrbs(target.Center);
            Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.22f, Pitch = 0.18f }, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 10, 0.9f, 2.6f, 0.9f, 1.35f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_BRecov);
            return false;
        }

        private void ReleaseRecoveryOrbs(Vector2 center)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
                return;

            int orbCount = Main.rand.Next(5, 9);
            for (int i = 0; i < orbCount; i++)
            {
                int targetIndex = BFArrow_BRecovTransfer.FindRandomInjuredPlayerIndex(owner, center, 2400f);
                Vector2 velocity =
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3.2f, 6.8f) +
                    new Vector2(0f, Main.rand.NextFloat(-2.8f, -0.9f));

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
                0.18f * intensity,
                0.038f,
                16);
            GeneralParticleHandler.SpawnParticle(pulse);

            GenericSparkle sparkle = new(
                center,
                Vector2.Zero,
                Color.White,
                Color.Lerp(mainColor, Color.White, 0.35f),
                1.15f * intensity,
                8,
                0f,
                1.3f);
            GeneralParticleHandler.SpawnParticle(sparkle);
        }

        private void SpawnOrbReleaseFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color coreColor = new(110, 255, 150);
            StrongBloom bloom = new(center, Vector2.Zero, coreColor, 0.85f * intensity, 14);
            GeneralParticleHandler.SpawnParticle(bloom);

            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(coreColor, Color.White, 0.2f),
                new Vector2(1.15f, 1.15f),
                0f,
                0.14f * intensity,
                0.03f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 8; i++)
            {
                GlowOrbParticle orb = new(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 3.2f),
                    false,
                    12,
                    Main.rand.NextFloat(0.2f, 0.34f) * intensity,
                    Color.Lerp(coreColor, Color.White, Main.rand.NextFloat(0.15f, 0.45f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }
    }
}
