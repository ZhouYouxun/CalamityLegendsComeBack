using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using CalamityPearlShard = CalamityMod.Items.Materials.PearlShard;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode.PearlShard
{
    public class PearlShardEffect : DefaultEffect
    {
        public override int EffectID => 41;
        public override int AmmoType => ModContent.ItemType<CalamityPearlShard>();

        public override Color ThemeColor => new(255, 218, 188);
        public override Color StartColor => new(255, 238, 206);
        public override Color EndColor => new(255, 164, 214);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void PlayLeftClickReplacementFireSound(Projectile projectile, Player owner)
        {
            if (Main.dedServ || projectile.owner != Main.myPlayer)
                return;

            Vector2 position = projectile.Center;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/GunShotSmall")
            {
                Volume = 0.46f,
                Pitch = 0.08f,
                PitchVariance = 0.04f,
                MaxInstances = 5
            }, position);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/OpalFire")
            {
                Volume = 0.56f,
                Pitch = 0.04f,
                PitchVariance = 0.04f,
                MaxInstances = 5
            }, position);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastShoot")
            {
                Volume = 0.24f,
                Pitch = 0.18f,
                PitchVariance = 0.05f,
                MaxInstances = 4
            }, position);
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<PearlShardTriggerGlobal>().FirstFrame = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            PearlShardTriggerGlobal global = projectile.GetGlobalProjectile<PearlShardTriggerGlobal>();
            if (!global.FirstFrame)
                return;

            global.FirstFrame = false;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            float speed = System.Math.Max(projectile.velocity.Length(), 17f) * 0.4f * 3f;
            float[] angles =
            {
                MathHelper.ToRadians(-20f),
                0f,
                MathHelper.ToRadians(20f)
            };

            for (int i = 0; i < angles.Length; i++)
            {
                Vector2 velocity = forward.RotatedBy(angles[i]) * speed;
                int pearl = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + forward * 5f,
                    velocity,
                    ModContent.ProjectileType<PearlShardLargePearl>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner);

                if (Main.projectile.IndexInRange(pearl))
                {
                    Main.projectile[pearl].CritChance = projectile.CritChance;
                    Main.projectile[pearl].netUpdate = true;
                }
            }

            SpawnMuzzlePearls(projectile.Center, forward);
        }

        private static void SpawnMuzzlePearls(Vector2 center, Vector2 forward)
        {
            for (int i = 0; i < 12; i++)
            {
                Color color = PearlShardVisuals.RandomPearlColor();
                Vector2 velocity = forward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.5f, 5.5f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center + forward * 10f, velocity, false, Main.rand.Next(18, 26), Main.rand.NextFloat(0.35f, 0.58f), color));

                if (i < 7)
                {
                    PearlParticle pearl = new(
                        center + forward * 12f + Main.rand.NextVector2Circular(4f, 4f),
                        velocity.RotatedByRandom(0.18f) * Main.rand.NextFloat(0.18f, 0.72f),
                        false,
                        Main.rand.Next(34, 48),
                        Main.rand.NextFloat(0.42f, 0.7f),
                        color,
                        0.95f,
                        Main.rand.NextFloat(-0.25f, 0.25f),
                        true);
                    GeneralParticleHandler.SpawnParticle(pearl);
                }
            }
        }
    }

    public class PearlShardTriggerGlobal : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool FirstFrame;
    }
}
