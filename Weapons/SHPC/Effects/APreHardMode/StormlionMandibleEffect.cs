using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    internal class StormlionMandibleEffect : DefaultEffect
    {
        public override int EffectID => 2;
        public override int AmmoType => ModContent.ItemType<StormlionMandible>();


        // ⚡ 闪电主题色（高亮电蓝）
        public override Color ThemeColor => new Color(120, 220, 255);
        public override Color StartColor => new Color(180, 240, 255);
        public override Color EndColor => new Color(60, 140, 255);

        public override void AI(Projectile projectile, Player owner)
        {
            // 抵消默认减速
            projectile.velocity *= 1.020408f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int count = 5;
            float spread = MathHelper.Pi / 3f;
            float baseRot = projectile.velocity.ToRotation();

            // ===== 找锁定目标（优先周围16*16范围）=====
            float searchRange = 16f * 16f; // 16格
            NPC lockedTarget = null;

            float minDist = searchRange;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float dist = Vector2.Distance(npc.Center, target.Center);

                if (dist < minDist)
                {
                    minDist = dist;
                    lockedTarget = npc;
                }
            }

            // 如果没找到，就锁自己这个目标
            if (lockedTarget == null)
                lockedTarget = target;

            int targetIndex = lockedTarget.whoAmI;

            // ===== 扇形释放 =====
            for (int i = 0; i < count; i++)
            {
                float offset = MathHelper.Lerp(-spread / 2f, spread / 2f, i / (float)(count - 1));
                float rot = baseRot + offset;

                Vector2 velocity = rot.ToRotationVector2() * 6f;

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<ArcZap>(),
                    (int)(projectile.damage * 0.25f),
                    hit.Knockback,
                    projectile.owner,
                    targetIndex, // ai[0]：锁定目标
                    3f           // ai[1]：连锁次数
                );
            }
        }

		public override void OnKill(Projectile projectile, Player owner, int timeLeft)
		{
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

			// 手动修改范围
			proj.width = 50;
			proj.height = 50;
		}



	}
}