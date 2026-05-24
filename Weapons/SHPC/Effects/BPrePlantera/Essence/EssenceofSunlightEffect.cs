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
            projectile.timeLeft = 164;
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
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            float targetSpeed = MathHelper.Lerp(projectile.velocity.Length(), 21f, 0.03f);
            NPC target = FindTarget(projectile, 1200f);

            EssenceofSunlight_GP gp = projectile.GetGlobalProjectile<EssenceofSunlight_GP>();

            if (target is not null)
            {
                gp.homingTimer++;

                Vector2 predictedCenter = target.Center + target.velocity * 8f;
                Vector2 desiredDirection = (predictedCenter - projectile.Center).SafeNormalize(forward);
                float trackingPower = Utils.GetLerpValue(0f, 80f, gp.homingTimer, true);
                float closeTargetBoost = Utils.GetLerpValue(260f, 70f, projectile.Distance(target.Center), true);
                float turnPower = MathHelper.Max(trackingPower, closeTargetBoost * 0.65f);
                float maxTurn = MathHelper.Lerp(MathHelper.ToRadians(1.4f), MathHelper.ToRadians(6.2f), turnPower);
                float easedRotation = forward.ToRotation().AngleTowards(desiredDirection.ToRotation(), maxTurn);

                forward = easedRotation.ToRotationVector2();
                targetSpeed = MathHelper.Lerp(targetSpeed, 24f, 0.05f + turnPower * 0.05f);
            }
            else
                gp.homingTimer = 0;

            projectile.velocity = forward * targetSpeed;
            projectile.rotation = forward.ToRotation();

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.42f);

            if (projectile.numUpdates == 0)
            {
                gp.flightTimer++;

                if (Main.rand.NextBool(1))
                {
                    Particle streak = new GlowSparkParticle(
                        projectile.Center - forward * Main.rand.NextFloat(4f, 14f),
                        -forward * Main.rand.NextFloat(1.0f, 2.8f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                        false,
                        Main.rand.Next(6, 9),
                        Main.rand.NextFloat(0.07f, 0.11f),
                        Color.Lerp(new Color(255, 255, 170), new Color(255, 196, 70), Main.rand.NextFloat()),
                        new Vector2(1.05f, 0.2f),
                        true,
                        false,
                        1f);
                    GeneralParticleHandler.SpawnParticle(streak);
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

        public int flightTimer;
        public int homingTimer;
    }
}
