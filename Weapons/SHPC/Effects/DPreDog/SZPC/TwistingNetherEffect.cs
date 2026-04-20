using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    public class TwistingNetherEffect : DefaultEffect
    {
        public override int EffectID => 33;
        public override int AmmoType => ModContent.ItemType<TwistingNether>();

        public override Color ThemeColor => new(30, 0, 40);
        public override Color StartColor => new(120, 40, 160);
        public override Color EndColor => new(5, 0, 10);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.velocity *= 1.6f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            // 基础方向
            Vector2 baseVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Max(projectile.velocity.Length(), 16f);

            // 生成3个，角度微随机（±5度）
            for (int i = 0; i < 3; i++)
            {
                float randomAngle = MathHelper.ToRadians(Main.rand.NextFloat(-5f, 5f)); // 关键：±5度
                Vector2 newVelocity = baseVelocity.RotatedBy(randomAngle);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    newVelocity,
                    ModContent.ProjectileType<TwistingNether_Blade>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner
                );
            }
        }



    }
}
