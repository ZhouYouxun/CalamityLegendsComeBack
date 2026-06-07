using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentSolarEffect : DefaultEffect
    {
        public override int EffectID => 21;

        public override int AmmoType => ItemID.FragmentSolar;

        public override Color ThemeColor => new Color(255, 120, 40);
        public override Color StartColor => new Color(255, 180, 80);
        public override Color EndColor => new Color(180, 60, 20);
        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;
        public override bool EnableDefaultSlowdown => false;

        private int shootTimer;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            shootTimer = 0;
            projectile.timeLeft = 90;
            projectile.velocity *= 2.4f;
            projectile.penetrate = 1;
            projectile.localAI[0] = 0f;
            projectile.localAI[1] = -1f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            shootTimer++;
            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.45f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.localAI[0] = 1f;
            projectile.localAI[1] = target.whoAmI;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            const float explosionScale = 0.7f;

            if (projectile.localAI[0] != 1f)
                return;

            if (projectile.localAI[1] >= 0f && projectile.localAI[1] < Main.maxNPCs)
            {
                NPC target = Main.npc[(int)projectile.localAI[1]];
                if (target.active)
                    center = target.Center;
            }

            if (projectile.owner == Main.myPlayer)
            {
                int explosionIndex = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewLegendSHPE>(),
                    (int)(projectile.damage * 1.33),
                    projectile.knockBack,
                    projectile.owner);

                if (Main.projectile.IndexInRange(explosionIndex))
                {
                    Projectile explosion = Main.projectile[explosionIndex];
                    explosion.width = (int)(320 * explosionScale);
                    explosion.height = (int)(320 * explosionScale);
                    explosion.Center = center;
                }

                SpawnDawnbreakerSpears(projectile, center);
            }

            SoundEngine.PlaySound(SoundID.Item14 with
            {
                Volume = 0.72f,
                Pitch = 0.18f
            }, center);

            for (int i = 0; i < 4; i++)
            {
                Particle outerPulse = new CustomPulse(
                    center,
                    Vector2.Zero,
                    Color.Lerp(new Color(255, 105, 25), new Color(255, 175, 70), i / 3f),
                    "CalamityMod/Particles/BloomCircle",
                    Vector2.One,
                    Main.rand.NextFloat(-8f, 8f),
                    (0.52f + i * 0.2f) * explosionScale,
                    (0.28f + i * 0.08f) * explosionScale,
                    24 - i * 2,
                    true);
                GeneralParticleHandler.SpawnParticle(outerPulse);
            }

            for (int i = 0; i < 2; i++)
            {
                Particle flamePulse = new CustomPulse(
                    center,
                    Vector2.Zero,
                    i == 0 ? new Color(255, 235, 175) : new Color(255, 120, 35),
                    i == 0 ? "CalamityMod/Particles/SoftRoundExplosion" : "CalamityMod/Particles/FlameExplosion",
                    Vector2.One,
                    Main.rand.NextFloat(-6f, 6f),
                    0.08f * explosionScale,
                    (0.26f + i * 0.2f) * explosionScale,
                    20 - i * 2);
                GeneralParticleHandler.SpawnParticle(flamePulse);
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 sparkVelocity = (MathHelper.TwoPi * i / 6f).ToRotationVector2().RotatedBy(Main.rand.NextFloat(-0.2f, 0.2f)) * Main.rand.NextFloat(5.5f, 9.5f) * explosionScale;
                GeneralParticleHandler.SpawnParticle(
                    new GlowSparkParticle(
                        center,
                        sparkVelocity,
                        false,
                        10,
                        0.2f * explosionScale,
                        Color.Lerp(new Color(255, 150, 45), Color.White, 0.35f),
                        new Vector2(1.8f, 0.8f) * explosionScale,
                        true,
                        true,
                        1f));
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 smokeVelocity = (new Vector2(24f, 24f).RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(0.4f, 1.1f) - forward * Main.rand.NextFloat(0.4f, 1.5f)) * explosionScale;
                GeneralParticleHandler.SpawnParticle(
                    new HeavySmokeParticle(
                        center + smokeVelocity,
                        smokeVelocity,
                        Color.Lerp(Color.DimGray, Color.OrangeRed, Main.rand.NextFloat(0.2f, 0.45f)),
                        Main.rand.Next(20, 28),
                        Main.rand.NextFloat(0.65f, 1.05f) * explosionScale,
                        0.45f));
            }

            for (int i = 0; i < 26; i++)
            {
                Vector2 dustVelocity = new Vector2(30f, 30f).RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(0.25f, 1.35f) * explosionScale;
                Dust fire = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.SolarFlare : DustID.Torch,
                    dustVelocity,
                    0,
                    Color.Lerp(new Color(255, 230, 145), new Color(255, 105, 25), Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.95f, 1.45f) * explosionScale);
                fire.noGravity = true;
            }
        }

        private void SpawnDawnbreakerSpears(Projectile projectile, Vector2 center)
        {
            for (int i = 0; i < 7; i++)
            {
                float angle = MathHelper.ToRadians(MathHelper.Lerp(-15f, 15f, i / 6f) + Main.rand.NextFloat(-2f, 2f));
                Vector2 direction = Vector2.UnitY.RotatedBy(angle);
                Vector2 spawnPos = center - direction * Main.rand.NextFloat(520f, 720f) + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-70f, 70f);
                Vector2 velocity = direction * Main.rand.NextFloat(17f, 22f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FragmentSolar_Spear>(),
                    (int)(projectile.damage * 0.9f),
                    projectile.knockBack,
                    projectile.owner
                );
            }
        }
    }
}
