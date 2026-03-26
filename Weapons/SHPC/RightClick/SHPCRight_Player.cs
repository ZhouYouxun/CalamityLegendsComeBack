using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    public class SHPCRight_Player : ModPlayer
    {
        public int HeatStage;

        private int heatUpTimer;
        private int coolDownTimer;

        public override void ResetEffects()
        {
            // ❌ 不再清零
        }

        public override void PostUpdate()
        {
            bool holdingRightClick = false;

            // ===== 检测是否存在 Holdout =====
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active &&
                    proj.owner == Player.whoAmI &&
                    proj.type == ModContent.ProjectileType<SHPCRight_HoulOut>())
                {
                    holdingRightClick = true;
                    break;
                }
            }

            // ===== 持续开火：升温 =====
            if (holdingRightClick)
            {
                heatUpTimer++;
                coolDownTimer = 0;

                if (heatUpTimer >= 180) // 3秒升一级
                {
                    HeatStage++;
                    heatUpTimer = 0;
                }
            }
            // ===== 没持枪：降温 =====
            else
            {
                coolDownTimer++;
                heatUpTimer = 0;

                if (coolDownTimer >= 300) // 5秒降一级
                {
                    HeatStage--;
                    coolDownTimer = 0;
                }
            }

            // ===== 限制范围 =====
            if (HeatStage < 0)
                HeatStage = 0;

            if (HeatStage > 7)
                HeatStage = 7;
        }
    }
}
