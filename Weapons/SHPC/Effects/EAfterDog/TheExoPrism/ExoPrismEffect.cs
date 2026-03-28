using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
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

            // 随机散射 2~4 条
            int laserCount = Main.rand.Next(2, 5);

            for (int i = 0; i < laserCount; i++)
            {
                // ===== 多链：霰射感 =====
                float speedX = baseVelocity.X + Main.rand.Next(-20, 21) * 0.15f;
                float speedY = baseVelocity.Y + Main.rand.Next(-20, 21) * 0.15f;

                Vector2 velocity = new Vector2(speedX, speedY);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<ExoPrism_Lazer>(),
                    projectile.damage,
                    projectile.knockBack,
                    owner.whoAmI
                );
            }
        }



    }
}
