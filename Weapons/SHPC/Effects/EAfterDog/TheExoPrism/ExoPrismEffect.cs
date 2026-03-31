using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            projectile.Kill();
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 baseVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * 12f;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);

            // ================= 左右两条平行 Light =================

            for (int i = -1; i <= 1; i += 2)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + normal * (40f * i), // 左右±X0偏移
                    forward * 7.5f,
                    ModContent.ProjectileType<ExoPrism_Light>(),
                    projectile.damage,
                    projectile.knockBack,
                    owner.whoAmI
                );
            }

            // ================= 中轴激光 =================
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                forward * 12f, // 保持直线高速
                ModContent.ProjectileType<ExoPrism_Lazer>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI
            );
        }









        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 300); // 超位崩解
        }

    }
}
