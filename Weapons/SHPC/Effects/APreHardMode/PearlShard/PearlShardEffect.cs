using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
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
            float speed = System.Math.Max(projectile.velocity.Length(), 17f);
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
                    Main.projectile[pearl].CritChance = projectile.CritChance;
            }

            SpawnMuzzlePearls(projectile.Center, forward);
        }

        private static void SpawnMuzzlePearls(Vector2 center, Vector2 forward)
        {
            for (int i = 0; i < 8; i++)
            {
                Color color = PearlShardVisuals.RandomPearlColor();
                Vector2 velocity = forward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.5f, 5f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center, velocity, false, Main.rand.Next(18, 26), Main.rand.NextFloat(0.35f, 0.58f), color));
            }
        }
    }

    public class PearlShardTriggerGlobal : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool FirstFrame;
    }
}
