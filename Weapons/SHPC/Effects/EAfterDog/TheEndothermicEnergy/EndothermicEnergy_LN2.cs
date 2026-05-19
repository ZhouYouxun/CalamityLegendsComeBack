using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheEndothermicEnergy
{
    internal class EndothermicEnergy_LN2 : ModProjectile, ILocalizedModType
    {
        private class EndothermicCopyState
        {
            public bool PendingShadowRelease;
            public int MarkedTargetIndex = -1;
        }

        private readonly System.Collections.Generic.Dictionary<int, EndothermicCopyState> projectileStates = new();

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private EndothermicCopyState GetState()
        {
            if (!projectileStates.TryGetValue(Projectile.whoAmI, out EndothermicCopyState state))
            {
                state = new EndothermicCopyState();
                projectileStates[Projectile.whoAmI] = state;
            }

            return state;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 108;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.extraUpdates = 15;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            EndothermicCopyState state = GetState();
            state.PendingShadowRelease = false;
            state.MarkedTargetIndex = -1;
        }

        public override void AI()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float t = (float)Main.GameUpdateCount * 0.18f + Projectile.identity * 0.31f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(170, 220, 255).ToVector3() * 0.36f);

            if (Main.rand.NextBool(7))
            {
                float wave = (float)System.Math.Sin(t * 1.15f) * 4.2f;
                Vector2 spawnPos = Projectile.Center - forward * Main.rand.NextFloat(3f, 7f) + right * wave;
                Vector2 vel = -forward * Main.rand.NextFloat(0.45f, 1.15f) + right * (float)System.Math.Cos(t * 1.4f) * 0.16f;

                SquishyLightParticle particle = new(
                    spawnPos,
                    vel,
                    Main.rand.NextFloat(0.44f, 0.67f),
                    Color.Lerp(new Color(220, 240, 255), Color.White, Main.rand.NextFloat(0.18f, 0.55f)) * 0.7f,
                    Main.rand.Next(13, 19)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            int sparkCount = Main.rand.NextBool(5) ? 2 : 1;
            for (int i = 0; i < sparkCount; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float phase = t + i * 1.13f;
                float lateral = (float)System.Math.Sin(phase * 1.35f) * 5.2f;

                Vector2 spawnPos = Projectile.Center - forward * Main.rand.NextFloat(4f, 8f) + right * lateral * side;
                Vector2 sparkVelocity =
                    (-forward + right * side * 0.42f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.8f, 3.9f) +
                    right * side * (float)System.Math.Cos(phase) * 0.22f;

                GlowSparkParticle spark = new GlowSparkParticle(
                    spawnPos,
                    sparkVelocity,
                    false,
                    Main.rand.Next(6, 8),
                    Main.rand.NextFloat(0.011f, 0.017f),
                    Color.Lerp(new Color(220, 240, 255), Color.White, Main.rand.NextFloat(0.15f, 0.50f)) * 0.7f,
                    new Vector2(1.8f, 1f),
                    true,
                    false,
                    1.08f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(3))
            {
                float side = Main.rand.NextBool() ? -1f : 1f;
                float phase = t * 0.92f + side * 2.1f;
                Vector2 dustPos =
                    Projectile.Center
                    - forward * Main.rand.NextFloat(4f, 10f)
                    + right * (float)System.Math.Sin(phase) * Main.rand.NextFloat(2.5f, 5.5f) * side;

                Vector2 dustVel =
                    (-forward).RotatedBy((float)System.Math.Sin(phase * 1.4f) * 0.14f) * Main.rand.NextFloat(0.9f, 2.4f) +
                    right * side * Main.rand.NextFloat(0.08f, 0.36f);

                Dust dust = Dust.NewDustPerfect(
                    dustPos,
                    Main.rand.NextBool(2) ? DustID.IceTorch : DustID.GemDiamond,
                    dustVel,
                    0,
                    Color.Lerp(new Color(120, 170, 255), Color.White, Main.rand.NextFloat(0.28f, 0.72f)),
                    Main.rand.NextFloat(0.66f, 0.9f)
                );
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(5))
            {
                float angle = t * 0.85f;
                float radius = Main.rand.NextFloat(3f, 6f);

                Vector2 pos = Projectile.Center - forward * Main.rand.NextFloat(4f, 8f) + angle.ToRotationVector2() * radius;
                Vector2 vel = -forward * Main.rand.NextFloat(0.25f, 0.75f) + right * (float)System.Math.Sin(angle * 1.5f) * 0.18f;

                Particle mist = new MediumMistParticle(
                    pos,
                    vel,
                    Color.White * 0.7f,
                    Color.Transparent,
                    Main.rand.NextFloat(0.3f, 0.48f),
                    Main.rand.NextFloat(78f, 110f)
                );
                GeneralParticleHandler.SpawnParticle(mist);
            }

            if (Main.rand.NextFloat() < 0.6f)
            {
                Particle centerFlare = new CustomSpark(
                    Projectile.Center,
                    Projectile.velocity * 0.02f,
                    "CalamityLegendsComeBack/Texture/KsTexture/window_04",
                    false,
                    7,
                    0.18f,
                    new Color(160, 242, 255) * 1.37f,
                    new Vector2(0.39f, 1.5f),
                    glowCenter: true,
                    shrinkSpeed: 1.2f,
                    glowCenterScale: 0.64f,
                    glowOpacity: 0.5f);
                GeneralParticleHandler.SpawnParticle(centerFlare);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EndothermicCopyState state = GetState();

            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.Chilled, 180);
            state.PendingShadowRelease = true;
            state.MarkedTargetIndex = target.whoAmI;

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            EndothermicCopyState state = GetState();

            if (state.PendingShadowRelease && Main.npc.IndexInRange(state.MarkedTargetIndex))
            {
                NPC target = Main.npc[state.MarkedTargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                {
                    for (int arm = 0; arm < 6; arm++)
                    {
                        float armAngle = MathHelper.TwoPi * arm / 6f;
                        float[] branchAngles =
                        {
                            armAngle,
                            armAngle + MathHelper.Pi / 6f,
                            armAngle - MathHelper.Pi / 6f
                        };
                        float[] distances = { 210f, 330f, 330f };

                        for (int branch = 0; branch < branchAngles.Length; branch++)
                        {
                            float angle = branchAngles[branch];
                            Vector2 spawnOffset = angle.ToRotationVector2() * distances[branch];

                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                target.Center + spawnOffset,
                                Vector2.Zero,
                                ModContent.ProjectileType<EndothermicEnergy_Shadow>(),
                                (int)(Projectile.damage * 0.36f),
                                Projectile.knockBack,
                                Projectile.owner,
                                0f,
                                target.whoAmI,
                                angle
                            );
                        }
                    }
                }
            }

            projectileStates.Remove(Projectile.whoAmI);
        }
    }
}
