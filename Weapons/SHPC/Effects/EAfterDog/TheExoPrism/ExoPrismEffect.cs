using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheExoPrism
{
    internal class ExoPrismEffect : DefaultEffect
    {
        public override int EffectID => 38;

        public override int AmmoType => ModContent.ItemType<ExoPrism>();

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            var gp = projectile.GetGlobalProjectile<ExoPrism_GP>();

            // 标记第一帧
            gp.firstFrame = true;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            var gp = projectile.GetGlobalProjectile<ExoPrism_GP>();

            // 第一帧直接自杀（确保状态稳定）
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
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);

            // ================= 左右两条平行 Light =================
            for (int i = -1; i <= 1; i += 2)
            {
                int projIndex = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + normal * (40f * i),
                    forward * 7.5f,
                    ModContent.ProjectileType<ExoPrism_Light>(),
                    projectile.damage,
                    projectile.knockBack,
                    owner.whoAmI,
                    projectile.ai[0], // ✅ 继承EffectID
                    projectile.ai[1],
                    projectile.ai[2]
                );

                if (Main.projectile.IndexInRange(projIndex))
                {
                    Main.projectile[projIndex].tileCollide = false;
                }
            }

            // ================= 中轴激光 =================
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                forward * 12f,
                ModContent.ProjectileType<ExoPrism_Lazer>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI,
                projectile.ai[0], // ✅ 继承EffectID
                projectile.ai[1],
                projectile.ai[2]
            );
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 300);
        }
    }

    // ================= 独立实例数据 =================
    public class ExoPrism_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}