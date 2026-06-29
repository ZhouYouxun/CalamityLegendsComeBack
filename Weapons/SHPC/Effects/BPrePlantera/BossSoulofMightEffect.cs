using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofMightEffect : DefaultEffect
    {
        public override int EffectID => 13;

        public override int AmmoType => ItemID.SoulofMight;

        public override Color ThemeColor => new Color(70, 110, 255);
        public override Color StartColor => new Color(150, 190, 255);
        public override Color EndColor => new Color(20, 40, 120);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<BossSoulofMight_GP>().firstFrame = true;
            projectile.timeLeft = 2;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            BossSoulofMight_GP gp = projectile.GetGlobalProjectile<BossSoulofMight_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 fallback = owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f);
            Vector2 forward = projectile.velocity.SafeNormalize(fallback);
            float speed = System.Math.Max(projectile.velocity.Length(), 16f);
            float[] scatterAngles =
            {
                MathHelper.ToRadians(-10f),
                0f,
                MathHelper.ToRadians(10f)
            };

            foreach (float angle in scatterAngles)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    forward.RotatedBy(angle) * speed,
                    ModContent.ProjectileType<BossSoulofMight_Ball>(),
                    System.Math.Max(1, (int)(projectile.damage * 0.85f)),
                    projectile.knockBack,
                    projectile.owner);
            }
        }
    }

    internal class BossSoulofMight_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool firstFrame;
    }
}
