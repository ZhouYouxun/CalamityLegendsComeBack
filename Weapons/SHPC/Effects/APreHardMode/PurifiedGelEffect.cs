using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class PurifiedGelEffect : DefaultEffect
    {
        public override int EffectID => 4;


        // 注册与之相匹配的材料：XXXEffect会直接和对应的材料XXX相互匹配
        public override int AmmoType => ModContent.ItemType<PurifiedGel>();


        // 粉蓝主题（从图提取：粉为主，蓝做拖尾）
        public override Color ThemeColor => new Color(255, 140, 200); // 粉色主体
        public override Color StartColor => new Color(120, 200, 255); // 亮蓝
        public override Color EndColor => new Color(60, 120, 255);    // 深蓝


        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.timeLeft = 30;

            // 确保这个弹幕编号是“未触发”状态
            releasedProjectiles.Remove(projectile.whoAmI);
        }

        private static readonly HashSet<int> releasedProjectiles = new();
        public override void AI(Projectile projectile, Player owner)
        {
            // 剩余时间 <= 10 时开始检测，但只能触发一次
            if (projectile.timeLeft <= 10 && !releasedProjectiles.Contains(projectile.whoAmI))
            {
                releasedProjectiles.Add(projectile.whoAmI);

                // 直接用当前速度即可
                // 因为你这里只有减速，没有转向，方向本身不会变
                Vector2 baseVelocity = projectile.velocity;
                float baseRot = baseVelocity.ToRotation();
                float baseSpeed = baseVelocity.Length();

                float angleOffset = MathHelper.ToRadians(15f);

                for (int i = 0; i < 2; i++)
                {
                    float sign = i == 0 ? 1f : -1f;

                    Vector2 velocity = baseRot.ToRotationVector2().RotatedBy(angleOffset * sign) * baseSpeed;

                    Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center,
                        velocity,
                        ModContent.ProjectileType<PurifiedGel_Ball>(),
                        (int)(projectile.damage * 0.95f),
                        projectile.knockBack,
                        projectile.owner
                    );
                }
            }
        }


        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
		{
		
		}










	}
}