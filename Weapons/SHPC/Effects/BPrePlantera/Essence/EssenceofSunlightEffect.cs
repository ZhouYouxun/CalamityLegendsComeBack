using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSunlightEffect : DefaultEffect
    {
        public override int EffectID => 7;

        public override int AmmoType => ModContent.ItemType<EssenceofSunlight>();

        public override Color ThemeColor => new Color(255, 220, 90);
        public override Color StartColor => new Color(255, 255, 160);
        public override Color EndColor => new Color(255, 180, 60);

        public override float SquishyLightParticleFactor => 1.35f;
        public override float ExplosionPulseFactor => 1.35f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.timeLeft = 54;
            projectile.penetrate = 1;
            projectile.extraUpdates = 3;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 18;

            EssenceofSunlight_GP gp = projectile.GetGlobalProjectile<EssenceofSunlight_GP>();
            gp.chargeTimer = 0;
            gp.isCharging = false;
            gp.chargeDirection = Vector2.Zero;

            Vector2 direction = projectile.velocity.SafeNormalize((Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX));
            projectile.velocity = direction * System.Math.Max(projectile.velocity.Length() * 1.3f, 19f);
            projectile.rotation = direction.ToRotation();
            SpawnChargeBackEffect(projectile);
        }

        public override void AI(Projectile projectile, Player owner)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            float targetSpeed = MathHelper.Lerp(projectile.velocity.Length(), 22f, 0.025f);
            projectile.velocity = forward * targetSpeed;
            projectile.rotation = forward.ToRotation();

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.42f);

            if (Main.rand.NextBool(2))
            {
                Particle streak = new GlowSparkParticle(
                    projectile.Center - forward * Main.rand.NextFloat(4f, 14f),
                    -forward * Main.rand.NextFloat(1.2f, 3.4f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    false,
                    Main.rand.Next(6, 10),
                    Main.rand.NextFloat(0.08f, 0.13f),
                    Color.Lerp(new Color(255, 255, 170), new Color(255, 196, 70), Main.rand.NextFloat()),
                    new Vector2(1.15f, 0.22f),
                    true,
                    false,
                    1f);
                GeneralParticleHandler.SpawnParticle(streak);
            }
        }

        private void SpawnChargeBackEffect(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;

            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;
                Vector2 dir = angle.ToRotationVector2();

                SquishyLightParticle core = new(
                    projectile.Center,
                    dir * Main.rand.NextFloat(2f, 6f),
                    Main.rand.NextFloat(1.2f, 1.8f),
                    Color.Lerp(new Color(255, 255, 180), new Color(255, 200, 80), Main.rand.NextFloat()),
                    18);

                GeneralParticleHandler.SpawnParticle(core);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 sideOffset = forward.RotatedBy((MathHelper.Pi / 2f) * side);

                for (int i = 0; i < 6; i++)
                {
                    float t = i / 6f;
                    Vector2 velocity =
                        back * MathHelper.Lerp(4f, 10f, t) +
                        sideOffset * MathHelper.Lerp(0.5f, 2.5f, t);

                    SquishyLightParticle jet = new(
                        projectile.Center + sideOffset * 4f,
                        velocity,
                        MathHelper.Lerp(0.8f, 1.4f, 1f - t),
                        Color.Lerp(new Color(255, 255, 160), new Color(255, 180, 60), t),
                        16 + i * 2);

                    GeneralParticleHandler.SpawnParticle(jet);
                }
            }

            for (int i = 0; i < 2; i++)
            {
                float rot = projectile.velocity.ToRotation() + (MathHelper.Pi / 2f) * i;

                Particle pulse = new DirectionalPulseRing(
                    projectile.Center,
                    back * 2f,
                    Color.Lerp(new Color(255, 255, 160), new Color(255, 200, 80), 0.5f),
                    new Vector2(1f, 3f),
                    rot,
                    0.25f,
                    0.02f,
                    24);

                GeneralParticleHandler.SpawnParticle(pulse);
            }

            Particle mainPulse = new DirectionalPulseRing(
                projectile.Center,
                back * 3f,
                new Color(255, 230, 120),
                new Vector2(1f, 4f),
                projectile.rotation - (MathHelper.Pi / 4f),
                0.35f,
                0.015f,
                28);

            GeneralParticleHandler.SpawnParticle(mainPulse);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Vector2 spawnPos = target.Center + normal * side * Main.rand.NextFloat(560f, 680f) - forward * Main.rand.NextFloat(40f, 110f);
                Vector2 missPoint = target.Center + forward * Main.rand.NextFloat(150f, 230f) + normal * side * Main.rand.NextFloat(70f, 120f);
                Vector2 direction = (missPoint - spawnPos).SafeNormalize(-normal * side);

                int beamIndex = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    direction * 38f,
                    ModContent.ProjectileType<EssenceofSunlight_Lighting>(),
                    System.Math.Max(1, (int)(projectile.damage * 0.72f)),
                    0f,
                    projectile.owner,
                    target.whoAmI,
                    side);

                if (Main.projectile.IndexInRange(beamIndex))
                    Main.projectile[beamIndex].netUpdate = true;
            }
        }
    }

    public class EssenceofSunlight_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public int chargeTimer;
        public bool isCharging;
        public Vector2 chargeDirection;
    }
}
