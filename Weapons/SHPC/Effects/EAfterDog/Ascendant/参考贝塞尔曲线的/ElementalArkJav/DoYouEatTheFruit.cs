using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using CalamityMod.Items.PermanentBoosters;
using CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav;
using CalamityMod.CalPlayer;
using CalamityMod;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;

namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class DoYouEatTheFruit : GlobalNPC
    {
        private static bool hasGivenXiaoZhiTiaoEA = false; // 开关，确保世界中只触发一次

        //public override bool AppliesToEntity(NPC entity)
        //{
        //    // 仅对所有 NPC 类型启用这个逻辑
        //    return true;
        //}


        public override void OnKill(NPC npc)
        {
            // 如果已经触发过，直接返回
            if (hasGivenXiaoZhiTiaoEA)
                return;

            // 机械 Boss 状态检测
            bool mechBoss1Defeated = NPC.downedMechBoss1; // 毁灭者
            bool mechBoss2Defeated = NPC.downedMechBoss2; // 双子魔眼
            bool mechBoss3Defeated = NPC.downedMechBoss3; // 机械骷髅王
                                                          // NPC.downedMechBoss1 毁灭者
                                                          // NPC.downedMechBoss2 双子魔眼（Spazmatism魔焰眼，Retinazer激光眼）
                                                          // NPC.downedMechBoss3 机械骷髅王
            
            Player player = Main.LocalPlayer; // 获取当前玩家对象

            // 检查不同的组合条件
            if (mechBoss1Defeated && mechBoss2Defeated && !mechBoss3Defeated) // 机械骷髅王未被击败
            {
                if (npc.type == NPCID.SkeletronPrime)
                {
                    GiveReward(player);
                }
            }
            else if (mechBoss1Defeated && mechBoss3Defeated && !mechBoss2Defeated) // 双子魔眼未被击败
            {
                if (npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
                {
                    GiveReward(player);
                }
            }
            else if (mechBoss2Defeated && mechBoss3Defeated && !mechBoss1Defeated) // 毁灭者未被击败
            {
                if (npc.type == NPCID.TheDestroyer)
                {
                    GiveReward(player);
                }
            }
        }

        private void GiveReward(Player player)
        {
            hasGivenXiaoZhiTiaoEA = true; // 开启开关，防止再次触发

            // 给予物品
            player.QuickSpawnItem(null, ModContent.ItemType<XiaoZhiTiaoEA>(), 1);

            // 显示提示消息
            Main.NewText("你似乎知道了一些东西！", Color.LightGoldenrodYellow);
        }











    }
}