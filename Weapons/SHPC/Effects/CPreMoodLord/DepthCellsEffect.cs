using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal class DepthCellsEffect : DefaultEffect
    {
        private static readonly int[] AbyssDustTypes = { 191, 29, 104 };

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
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 9; i++)
            {
                float angle = MathHelper.TwoPi * i / 9f;
                Vector2 offset = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 7f);
                CreateAbyssDust(
                    projectile.Center + offset,
                    offset.SafeNormalize(forward).RotatedByRandom(0.28f) * Main.rand.NextFloat(0.5f, 2.4f),
                    Main.rand.NextFloat(1f, 1.45f),
                    Main.rand.NextFloat(0.45f, 1f),
                    120);
            }

            for (int i = 0; i < 4; i++)
            {
                Dust foam = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.Water,
                    forward * Main.rand.NextFloat(0.35f, 1.2f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    130,
                    Color.Lerp(cyan, toxic, Main.rand.NextFloat(0.2f, 0.7f)),
                    Main.rand.NextFloat(0.9f, 1.15f));
                foam.noGravity = true;
                foam.velocity *= 0.75f;
            }
        }

        private static void SpawnSplitBurst(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.7f) * Main.rand.NextFloat(1.3f, 4.8f) + Main.rand.NextVector2Circular(1.2f, 1.2f);
                CreateAbyssDust(
                    projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    velocity,
                    Main.rand.NextFloat(1.05f, 1.7f),
                    Main.rand.NextFloat(0.25f, 0.95f),
                    120);
            }

            for (int i = 0; i < 8; i++)
            {
                Dust foam = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.Water,
                    forward.RotatedByRandom(0.9f) * Main.rand.NextFloat(0.7f, 2.2f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    130,
                    Color.Lerp(DepthCells_Drop.AbyssCyan, DepthCells_Drop.AbyssFoam, Main.rand.NextFloat(0.2f, 0.85f)),
                    Main.rand.NextFloat(0.85f, 1.15f));
                foam.noGravity = true;
                foam.velocity *= 0.7f;
            }
        }

        private static Dust CreateAbyssDust(Vector2 position, Vector2 velocity, float scale, float colorInterpolant, int alpha)
        {
            Dust dust = Dust.NewDustPerfect(
                position,
                AbyssDustTypes[Main.rand.Next(AbyssDustTypes.Length)],
                velocity,
                alpha,
                Color.Lerp(DepthCells_Drop.AbyssDeep, DepthCells_Drop.AbyssToxic, colorInterpolant),
                scale);
            dust.noGravity = true;
            dust.fadeIn = scale * 1.05f;
            return dust;
        }
    }

    

    internal sealed class DepthCells_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
