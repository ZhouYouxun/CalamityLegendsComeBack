using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
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
        public override bool PlayDefaultLeftClickFireSound => false;

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

            // 深渊细胞不再直接射出三枚液滴。
            // 普通 SHPC 光球只负责转换成一条受重力影响的深渊鲨鱼，液滴会在鲨鱼死亡时喷出。
            if (projectile.owner == Main.myPlayer)
            {
                Vector2 sharkVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction) * 22.275f;
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + sharkVelocity.SafeNormalize(Vector2.UnitX) * 12f,
                    sharkVelocity,
                    ModContent.ProjectileType<DepthCells_Shark>(),
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

            //GeneralParticleHandler.SpawnParticle(new CustomPulse(
            //    projectile.Center,
            //    Vector2.Zero,
            //    Color.Lerp(DepthCells_Drop.AbyssDeep, toxic, 0.18f),
            //    "CalamityMod/Particles/BloomCircle",
            //    Vector2.One,
            //    Main.rand.NextFloat(MathHelper.TwoPi),
            //    0.08f,
            //    0.32f,
            //    22,
            //    false));

            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f;
                Vector2 offset = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 7f);
                CreateAbyssDust(
                    projectile.Center + offset,
                    offset.SafeNormalize(forward).RotatedByRandom(0.28f) * Main.rand.NextFloat(0.5f, 2.4f),
                    Main.rand.NextFloat(1f, 1.45f),
                    Main.rand.NextFloat(0.45f, 1f),
                    120);
            }

            for (int i = 0; i < 7; i++)
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

            for (int i = 0; i < 6; i++)
            {
                HeavySmokeParticle smoke = new(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    -forward * Main.rand.NextFloat(0.2f, 1.1f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    Color.Lerp(DepthCells_Drop.AbyssDeep, DepthCells_Drop.AbyssBlue, Main.rand.NextFloat(0.1f, 0.5f)),
                    Main.rand.Next(26, 42),
                    Main.rand.NextFloat(0.45f, 0.9f),
                    0.45f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        private static void SpawnSplitBurst(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            //GeneralParticleHandler.SpawnParticle(new CustomPulse(
            //    projectile.Center,
            //    Vector2.Zero,
            //    Color.Lerp(DepthCells_Drop.AbyssDeep, DepthCells_Drop.AbyssCyan, 0.22f),
            //    "CalamityMod/Particles/BloomRing",
            //    Vector2.One,
            //    Main.rand.NextFloat(MathHelper.TwoPi),
            //    0.06f,
            //    0.42f,
            //    20,
            //    false));

            for (int i = 0; i < 22; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.7f) * Main.rand.NextFloat(1.3f, 4.8f) + Main.rand.NextVector2Circular(1.2f, 1.2f);
                CreateAbyssDust(
                    projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    velocity,
                    Main.rand.NextFloat(1.05f, 1.7f),
                    Main.rand.NextFloat(0.25f, 0.95f),
                    120);
            }

            for (int i = 0; i < 12; i++)
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

            for (int i = 0; i < 8; i++)
            {
                HeavySmokeParticle smoke = new(
                    projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    -forward.RotatedByRandom(0.9f) * Main.rand.NextFloat(0.4f, 2f) + Main.rand.NextVector2Circular(0.55f, 0.55f),
                    Main.rand.NextBool(3) ? DepthCells_Drop.AbyssDeep : Color.Lerp(DepthCells_Drop.AbyssBlue, DepthCells_Drop.AbyssDeep, 0.72f),
                    Main.rand.Next(28, 48),
                    Main.rand.NextFloat(0.5f, 1.05f),
                    0.42f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
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
