using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
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

        private int shootTimer;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            shootTimer = 0;
            projectile.timeLeft = 50;
            projectile.velocity *= 0.3f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            shootTimer++;

            if (projectile.timeLeft > 21)
            {
                projectile.velocity *= 1.02f;
            }
            else
            {
                projectile.velocity *= 0.99f;

                if (shootTimer % 3 == 0)
                {
                    float[] angles = { -12f, -8f, -4f, 0f, 4f, 8f, 12f };

                    int index = (21 - projectile.timeLeft) / 3;
                    if (index >= 0 && index < angles.Length)
                    {
                        Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
                        Vector2 shootVelocity = forward.RotatedBy(MathHelper.ToRadians(angles[index])) * 16f;

                        Projectile.NewProjectile(
                            projectile.GetSource_FromThis(),
                            projectile.Center,
                            shootVelocity,
                            ModContent.ProjectileType<FragmentSolar_Spear>(),
                            (int)(projectile.damage * 0.7f),
                            projectile.knockBack,
                            projectile.owner
                        );
                    }
                }
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

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
                    1.1f + i * 0.45f,
                    0.55f + i * 0.18f,
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
                    0.08f,
                    0.26f + i * 0.2f,
                    20 - i * 2);
                GeneralParticleHandler.SpawnParticle(flamePulse);
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 sparkVelocity = (MathHelper.TwoPi * i / 6f).ToRotationVector2().RotatedBy(Main.rand.NextFloat(-0.2f, 0.2f)) * Main.rand.NextFloat(5.5f, 9.5f);
                GeneralParticleHandler.SpawnParticle(
                    new GlowSparkParticle(
                        center,
                        sparkVelocity,
                        false,
                        10,
                        0.2f,
                        Color.Lerp(new Color(255, 150, 45), Color.White, 0.35f),
                        new Vector2(1.8f, 0.8f),
                        true,
                        true,
                        1f));
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 smokeVelocity = new Vector2(24f, 24f).RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(0.4f, 1.1f) - forward * Main.rand.NextFloat(0.4f, 1.5f);
                GeneralParticleHandler.SpawnParticle(
                    new HeavySmokeParticle(
                        center + smokeVelocity,
                        smokeVelocity,
                        Color.Lerp(Color.DimGray, Color.OrangeRed, Main.rand.NextFloat(0.2f, 0.45f)),
                        Main.rand.Next(20, 28),
                        Main.rand.NextFloat(0.65f, 1.05f),
                        0.45f));
            }

            for (int i = 0; i < 26; i++)
            {
                Vector2 dustVelocity = new Vector2(30f, 30f).RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(0.25f, 1.35f);
                Dust fire = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.SolarFlare : DustID.Torch,
                    dustVelocity,
                    0,
                    Color.Lerp(new Color(255, 230, 145), new Color(255, 105, 25), Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.95f, 1.45f));
                fire.noGravity = true;
            }
        }
    }
}
