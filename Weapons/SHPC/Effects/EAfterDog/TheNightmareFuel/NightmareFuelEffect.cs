using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheNightmareFuel
{
    internal class NightmareFuelEffect : DefaultEffect
    {
        public override int EffectID => 35;

        public override int AmmoType => ModContent.ItemType<NightmareFuel>();

        public override Color ThemeColor => new Color(200, 140, 40);
        public override Color StartColor => new Color(255, 210, 80);
        public override Color EndColor => new Color(40, 25, 10);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            var gp = projectile.GetGlobalProjectile<NightmareFuel_GP>();

            // 标记第一帧
            gp.firstFrame = true;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            var gp = projectile.GetGlobalProjectile<NightmareFuel_GP>();

            // 第一帧直接自杀
            if (gp.firstFrame)
            {
                gp.firstFrame = false;
                projectile.Kill();
                return;
            }
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 baseVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * 12f;

            // 中间直线
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                baseVelocity,
                ModContent.ProjectileType<NightmareFuel_ARC>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI,
                projectile.ai[0], // ✅ 继承EffectID
                0f
            );

            // 左弧
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                baseVelocity.RotatedBy(-0.23f),
                ModContent.ProjectileType<NightmareFuel_ARC>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI,
                projectile.ai[0], // ✅ 继承EffectID
                -1f
            );

            // 右弧
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                baseVelocity.RotatedBy(0.23f),
                ModContent.ProjectileType<NightmareFuel_ARC>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI,
                projectile.ai[0], // ✅ 继承EffectID
                1f
            );
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }
    }

    // ================= 独立实例数据 =================
    public class NightmareFuel_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}