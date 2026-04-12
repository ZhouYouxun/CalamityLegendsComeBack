using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheEndothermicEnergy
{
    internal class EndothermicEnergy_Copy : ModProjectile, ILocalizedModType
    {
        private class EndothermicCopyState
        {
            public bool PendingShadowRelease;
            public int MarkedTargetIndex = -1;
        }

        private readonly System.Collections.Generic.Dictionary<int, EndothermicCopyState> projectileStates = new();

        public new string LocalizationCategory => "Projectiles";
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
            Projectile.timeLeft = 180;
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
            Lighting.AddLight(Projectile.Center, new Color(170, 220, 255).ToVector3() * 0.52f);

            if (Main.rand.NextBool(4))
            {
                float wave = (float)System.Math.Sin(t * 1.15f) * 4.2f;
                Vector2 spawnPos = Projectile.Center - forward * Main.rand.NextFloat(3f, 7f) + right * wave;
                Vector2 vel = -forward * Main.rand.NextFloat(0.45f, 1.15f) + right * (float)System.Math.Cos(t * 1.4f) * 0.16f;

                SquishyLightParticle particle = new(
                    spawnPos,
                    vel,
                    Main.rand.NextFloat(0.62f, 0.95f),
                    Color.Lerp(new Color(220, 240, 255), Color.White, Main.rand.NextFloat(0.18f, 0.55f)),
                    Main.rand.Next(18, 26)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            for (int i = 0; i < 2; i++)
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
                    Main.rand.Next(8, 11),
                    Main.rand.NextFloat(0.016f, 0.024f),
                    Color.Lerp(new Color(220, 240, 255), Color.White, Main.rand.NextFloat(0.15f, 0.50f)),
                    new Vector2(1.8f, 1f),
                    true,
                    false,
                    1.08f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float phase = t * 0.92f + i * 2.1f;
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
                    Main.rand.NextFloat(0.95f, 1.28f)
                );
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                float angle = t * 0.85f;
                float radius = Main.rand.NextFloat(3f, 6f);

                Vector2 pos = Projectile.Center - forward * Main.rand.NextFloat(4f, 8f) + angle.ToRotationVector2() * radius;
                Vector2 vel = -forward * Main.rand.NextFloat(0.25f, 0.75f) + right * (float)System.Math.Sin(angle * 1.5f) * 0.18f;

                Particle mist = new MediumMistParticle(
                    pos,
                    vel,
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.42f, 0.68f),
                    Main.rand.NextFloat(110f, 155f)
                );
                GeneralParticleHandler.SpawnParticle(mist);
            }

            Particle centerFlare = new CustomSpark(
                Projectile.Center,
                Projectile.velocity * 0.02f,
                "CalamityLegendsComeBack/Texture/KsTexture/window_04",
                false,
                10,
                0.26f,
                new Color(160, 242, 255) * 1.96f,
                new Vector2(0.56f, 2.15f),
                glowCenter: true,
                shrinkSpeed: 1.2f,
                glowCenterScale: 0.92f,
                glowOpacity: 0.72f);
            GeneralParticleHandler.SpawnParticle(centerFlare);
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
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        float distance = Main.rand.NextFloat(170f, 512f);
                        Vector2 spawnOffset = angle.ToRotationVector2() * distance;

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            target.Center + spawnOffset,
                            Vector2.Zero,
                            ModContent.ProjectileType<EndothermicEnergy_Shadow>(),
                            (int)(Projectile.damage * 0.42f),
                            Projectile.knockBack,
                            Projectile.owner,
                            0f,
                            target.whoAmI,
                            angle
                        );
                    }
                }
            }

            projectileStates.Remove(Projectile.whoAmI);
        }
    }
}
