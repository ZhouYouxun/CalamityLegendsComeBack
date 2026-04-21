using CalamityMod.CalPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class GiveWeapon : GlobalNPC
    {
        //public override bool AppliesToEntity(NPC entity)
        //{
        //    // 仅对所有 NPC 类型启用这个逻辑
        //    return true;
        //}

        public override void OnKill(NPC npc)
        {
            // 检测是否为月球领主的心脏
            if (npc.type == NPCID.MoonLordCore)
            {
                Player player = Main.player[npc.lastInteraction]; // 获取最后击杀的玩家

                if (player != null && player.TryGetModPlayer(out CalamityPlayer calamityPlayer))
                {
                    // 检查条件是否都为"否"
                    if (!calamityPlayer.sTangerine && !calamityPlayer.mFruit && !calamityPlayer.tCloudberry && !calamityPlayer.sStrawberry)
                    {
                        // 判定成功，发放武器
                        int itemID = ModContent.ItemType<ElementalArkJav>();
                        player.QuickSpawnItem(player.GetSource_FromThis(), itemID);

                        // 可以添加一条通知消息
                        //Main.NewText("???", 255, 255, 0);
                    }
                }
            }
        }
    }
}
