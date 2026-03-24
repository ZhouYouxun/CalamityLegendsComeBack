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


        // 注册与之相匹配的材料：XXXEffect会直接和对应的材料XXX相互匹配
        public override int AmmoType => ModContent.ItemType<PurifiedGel>();


        // 粉蓝主题（从图提取：粉为主，蓝做拖尾）
        public override Color ThemeColor => new Color(255, 140, 200); // 粉色主体
        public override Color StartColor => new Color(120, 200, 255); // 亮蓝
        public override Color EndColor => new Color(60, 120, 255);    // 深蓝

        public override void OnSpawn(Projectile projectile, Player owner)
        {
			// 初始直接设为X帧
			projectile.timeLeft = 30;

			// 记录初始速度
			projectile.ai[0] = projectile.velocity.X;
			projectile.ai[1] = projectile.velocity.Y;
		}

		public override void OnKill(Projectile projectile, Player owner, int timeLeft)
		{
			// ===== 使用初始速度 =====
			Vector2 baseVelocity = new Vector2(projectile.ai[0], projectile.ai[1]);
			float baseRot = baseVelocity.ToRotation();
			float baseSpeed = baseVelocity.Length();

			// ===== 可直接写角度（单位：度）=====
			float angleOffset = MathHelper.ToRadians(15f);

			// ===== 循环生成左右两个 =====
			for (int i = 0; i < 2; i++)
			{
				float sign = i == 0 ? 1f : -1f;

				float rot = baseRot + angleOffset * sign;
				Vector2 velocity = rot.ToRotationVector2() * baseSpeed;

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
}