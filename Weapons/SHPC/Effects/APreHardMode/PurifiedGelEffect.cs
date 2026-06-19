using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class PurifiedGelEffect : DefaultEffect
    {
        public override int EffectID => 4;

        public override int AmmoType => ModContent.ItemType<PurifiedGel>();

        public override Color ThemeColor => new Color(255, 140, 200);
        public override Color StartColor => new Color(120, 200, 255);
        public override Color EndColor => new Color(60, 120, 255);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<PurifiedGel_GP>().firstFrame = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            PurifiedGel_GP gp = projectile.GetGlobalProjectile<PurifiedGel_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            float speed = System.Math.Max(projectile.velocity.Length(), 16f);
            float[] spreadAngles =
            {
                MathHelper.ToRadians(-14f),
                MathHelper.ToRadians(-7f),
                0f,
                MathHelper.ToRadians(7f),
                MathHelper.ToRadians(14f)
            };

            for (int i = 0; i < spreadAngles.Length; i++)
            {
                Vector2 velocity = forward.RotatedBy(spreadAngles[i]) * speed;
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + forward * 8f,
                    velocity,
                    ModContent.ProjectileType<PurifiedGel_Ball>(),
                    (int)(projectile.damage * 0.6),
                    projectile.knockBack,
                    projectile.owner,
                    projectile.identity);
            }
        }
    }

    public class PurifiedGel_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
