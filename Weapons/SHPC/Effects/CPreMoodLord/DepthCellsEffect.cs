using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal class DepthCellsEffect : DefaultEffect
    {
        public override int EffectID => 17;
        public override int AmmoType => ModContent.ItemType<DepthCells>();

        public override Color ThemeColor => new(34, 126, 116);
        public override Color StartColor => new(110, 255, 190);
        public override Color EndColor => new(8, 18, 34);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<DepthCells_GP>().firstFrame = true;
            SpawnConversionFlash(projectile);
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DepthCells_GP gp = projectile.GetGlobalProjectile<DepthCells_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SpawnSplitBurst(projectile);

            Vector2 baseVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX);
            float[] spread = { -0.18f, 0f, 0.18f };
            float[] speedScale = { 10.6f, 12.4f, 11.3f };

            for (int i = 0; i < spread.Length; i++)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + baseVelocity * 10f,
                    baseVelocity.RotatedBy(spread[i]) * speedScale[i],
                    ModContent.ProjectileType<DepthCells_Drop>(),
                    projectile.damage,
                    projectile.knockBack,
                    owner.whoAmI);
            }
        }

        private static void SpawnConversionFlash(Projectile projectile)
        {
            Color toxic = DepthCells_Drop.AbyssToxic;
            Color cyan = DepthCells_Drop.AbyssCyan;

            DirectionalPulseRing ring = new(
                projectile.Center,
                Vector2.Zero,
                toxic * 0.42f,
                Vector2.One,
                projectile.velocity.ToRotation(),
                0.02f,
                0.15f,
                20);
            GeneralParticleHandler.SpawnParticle(ring);

            StrongBloom bloom = new(
                projectile.Center,
                Vector2.Zero,
                Color.Lerp(toxic, cyan, 0.45f) * 0.46f,
                0.55f,
                18);
            GeneralParticleHandler.SpawnParticle(bloom);
        }

        private static void SpawnSplitBurst(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            BloomRing bloomRing = new(
                projectile.Center,
                Vector2.Zero,
                DepthCells_Drop.AbyssToxic * 0.35f,
                0.62f,
                24);
            GeneralParticleHandler.SpawnParticle(bloomRing);

            for (int i = 0; i < 7; i++)
            {
                WaterGlobParticle glob = new(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    forward * Main.rand.NextFloat(0.8f, 2.8f) + Main.rand.NextVector2Circular(2f, 2f),
                    Main.rand.NextFloat(0.8f, 1.15f));
                glob.Color = Color.Lerp(DepthCells_Drop.AbyssToxic, DepthCells_Drop.AbyssFoam, Main.rand.NextFloat()) * 0.48f;
                GeneralParticleHandler.SpawnParticle(glob);
            }

            for (int i = 0; i < 8; i++)
            {
                WaterFlavoredParticle shard = new(
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    forward.RotatedByRandom(0.55f) * Main.rand.NextFloat(1.2f, 4f),
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.9f, 1.2f),
                    Color.Lerp(DepthCells_Drop.AbyssCyan, DepthCells_Drop.AbyssFoam, Main.rand.NextFloat(0.2f, 0.8f)));
                GeneralParticleHandler.SpawnParticle(shard);
            }

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(3) ? 191 : (Main.rand.NextBool() ? 29 : 104),
                    forward.RotatedByRandom(0.65f) * Main.rand.NextFloat(1.8f, 5.4f),
                    120,
                    Color.Lerp(DepthCells_Drop.AbyssDeep, DepthCells_Drop.AbyssToxic, Main.rand.NextFloat(0.35f, 0.95f)),
                    Main.rand.NextFloat(1.1f, 1.75f));
                dust.noGravity = true;
            }
        }
    }

    

    internal sealed class DepthCells_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
