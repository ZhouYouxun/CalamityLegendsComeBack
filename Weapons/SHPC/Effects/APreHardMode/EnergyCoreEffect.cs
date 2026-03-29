using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode;
using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects
{
    public class EnergyCoreEffect : DefaultEffect
    {
        public override int EffectID => 1;

        public override int AmmoType => ModContent.ItemType<EnergyCore>();
        public override int ShotsPerAmmo => 75;

        public override Color ThemeColor => new Color(120, 255, 120); // 稍微偏深的Lime
        public override Color StartColor => new Color(140, 255, 140); // 稍亮一点作为起点
        public override Color EndColor => new Color(40, 180, 40);     // 深绿色收尾
        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 模拟重力 =====

            float gravity = 0.18f;      // 重力强度（你可以调，0.1~0.3之间比较自然）
            float maxFallSpeed = 16f;   // 最大下落速度（防止无限加速）

            // 逐帧向下加速
            projectile.velocity.Y += gravity;

            // 限制最大下落速度
            if (projectile.velocity.Y > maxFallSpeed)
                projectile.velocity.Y = maxFallSpeed;

            // ===== 可选：根据速度调整朝向（更像箭）=====
            projectile.rotation = projectile.velocity.ToRotation();


            projectile.velocity *= 1.020408f;  // 抵消默认减速

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 1.8f);

        }
        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            // 反弹（入射=反射）
            if (projectile.velocity.X != oldVelocity.X)
                projectile.velocity.X = -oldVelocity.X;

            if (projectile.velocity.Y != oldVelocity.Y)
                projectile.velocity.Y = -oldVelocity.Y;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item110, projectile.Center);

            projectile.timeLeft -= 25;

            return false; // 不销毁
        }

		// 死亡时炸出6个电火花
		public override void OnKill(Projectile projectile, Player owner, int timeLeft)
		{
			int count = 7;

			// 随机整体旋转（避免每次都一样）
			float baseRotation = Main.rand.NextFloat(MathHelper.TwoPi);

			for (int i = 0; i < count; i++)
			{
				// 基础均分角度 + 随机扰动（±15°）
				float baseAngle = MathHelper.TwoPi / count * i;
				float randomOffset = Main.rand.NextFloat(-MathHelper.ToRadians(15f), MathHelper.ToRadians(15f));
				float angle = baseRotation + baseAngle + randomOffset;

				// 速度随机（4~8）
				float speed = Main.rand.NextFloat(4f, 8f);
				Vector2 velocity = angle.ToRotationVector2() * speed;

				// 伤害随机（0.X~0.Y倍）
				float damageFactor = Main.rand.NextFloat(0.3f, 0.5f);

				Projectile.NewProjectile(
					projectile.GetSource_FromThis(),
					projectile.Center,
					velocity,
					ModContent.ProjectileType<EnergyCore_Spark>(),
					(int)(projectile.damage * damageFactor),
					projectile.knockBack,
					projectile.owner
				);
			}

            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];
            proj.width = 75;
            proj.height = 75;
        }




	}
}