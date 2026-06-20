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
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.timeLeft = 360;
            projectile.penetrate = 1;
            projectile.extraUpdates = 2;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 18;

            EssenceofSunlight_GP gp = projectile.GetGlobalProjectile<EssenceofSunlight_GP>();
            gp.flightTimer = 0;
            gp.homingTimer = 0;

            Vector2 direction = projectile.velocity.SafeNormalize((Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX));
            projectile.velocity = direction * System.Math.Max(projectile.velocity.Length() * 1.2f, 18f);
            projectile.rotation = direction.ToRotation();
        }

        public override void AI(Projectile projectile, Player owner)
        {
            NPC target = FindTarget(projectile, 1800f);
            EssenceofSunlight_GP gp = projectile.GetGlobalProjectile<EssenceofSunlight_GP>();

            Vector2 currentVelocity = projectile.velocity;
            float currentSpeed = currentVelocity.Length();
            Vector2 forward = currentVelocity.SafeNormalize(Vector2.UnitX);

            if (target is not null)
            {
                gp.homingTimer++;
                Vector2 desiredDirection = (target.Center - projectile.Center).SafeNormalize(forward);
                float warmup = Utils.GetLerpValue(0f, 30f, gp.homingTimer, true);
                float targetSpeed = MathHelper.Lerp(18f, 28f, warmup);
                Vector2 desiredVelocity = desiredDirection * targetSpeed;

                const float HomingInertia = 5f; // Very low inertia for extremely tight/fast homing
                projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

                if (projectile.velocity.Length() > 28f)
                    projectile.velocity = projectile.velocity.SafeNormalize(desiredDirection) * 28f;
            }
            else
            {
                gp.homingTimer = 0;
                float targetSpeed = MathHelper.Lerp(currentSpeed, 21f, 0.03f);
                projectile.velocity = forward * targetSpeed;
            }

            projectile.rotation = projectile.velocity.ToRotation();
            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.42f);

            if (projectile.numUpdates == 0)
            {
                gp.flightTimer++;

                if (Main.rand.NextBool(2))
                {
                    Vector2 sparkVelocity = -forward * Main.rand.NextFloat(1f, 3.5f) + Main.rand.NextVector2Circular(0.5f, 0.5f);
                    CritSpark spark = new CritSpark(
                        projectile.Center - forward * Main.rand.NextFloat(4f, 14f),
                        sparkVelocity,
                        Color.White,
                        new Color(255, 196, 70),
                        Main.rand.NextFloat(0.4f, 0.85f),
                        Main.rand.Next(8, 14)
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                if (Main.rand.NextBool(3))
                {
                    GlowOrbParticle orb = new GlowOrbParticle(
                        projectile.Center - forward * Main.rand.NextFloat(2f, 8f),
                        -forward * Main.rand.NextFloat(0.5f, 1.8f),
                        false,
                        Main.rand.Next(6, 12),
                        Main.rand.NextFloat(0.15f, 0.3f),
                        new Color(255, 220, 90),
                        true,
                        false,
                        true
                    );
                    GeneralParticleHandler.SpawnParticle(orb);
                }

                if (gp.flightTimer % 9 == 0)
                    SpawnFlightBackEffect(projectile);
            }
        }

        private static NPC FindTarget(Projectile projectile, float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(projectile))
                    continue;

                float distance = projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private void SpawnFlightBackEffect(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 4; i++)
            {
                Vector2 spawnPosition =
                    projectile.Center -
                    forward * Main.rand.NextFloat(5f, 18f) +
                    normal * Main.rand.NextFloat(-7f, 7f);

                SquishyLightParticle core = new(
                    spawnPosition,
                    back.RotatedByRandom(0.28f) * Main.rand.NextFloat(1.4f, 4.4f),
                    Main.rand.NextFloat(0.75f, 1.25f),
                    Color.Lerp(new Color(255, 255, 180), new Color(255, 200, 80), Main.rand.NextFloat()),
                    Main.rand.Next(14, 20));

                GeneralParticleHandler.SpawnParticle(core);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 sideOffset = normal * side;

                for (int i = 0; i < 2; i++)
                {
                    float t = i / 2f;
                    Vector2 velocity =
                        back * MathHelper.Lerp(3.4f, 7.2f, t) +
                        sideOffset * MathHelper.Lerp(0.45f, 1.8f, t);

                    SquishyLightParticle jet = new(
                        projectile.Center - forward * Main.rand.NextFloat(6f, 14f) + sideOffset * Main.rand.NextFloat(2f, 5f),
                        velocity,
                        MathHelper.Lerp(0.65f, 1.05f, 1f - t),
                        Color.Lerp(new Color(255, 255, 160), new Color(255, 180, 60), t),
                        14 + i * 2);

                    GeneralParticleHandler.SpawnParticle(jet);
                }
            }

            if (Main.rand.NextBool(2))
            {
                Particle pulse = new DirectionalPulseRing(
                    projectile.Center - forward * 8f,
                    back * 1.6f,
                    new Color(255, 230, 120),
                    new Vector2(0.75f, 2.6f),
                    projectile.rotation - MathHelper.PiOver4,
                    0.18f,
                    0.015f,
                    18);

                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.Center = target.Center;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            base.OnKill(projectile, owner, timeLeft);

            if (owner.whoAmI != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            
            // Spawn the portal cluster at the orb impact point.
            Vector2 spawnPos = projectile.Center;
            
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                spawnPos,
                forward,
                ModContent.ProjectileType<EssenceofSunlight_BurstRelay>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI);
        }
    }

    public class EssenceofSunlight_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public int flightTimer;
        public int homingTimer;
    }
}
