using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.NormalNPCs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    internal class AdditionGN : GlobalNPC
    {

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == ModContent.NPCType<Stormlion>())
            {
                // 添加：每次必掉（可自行调整为概率掉落）
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StormlionMandible>(), 2)); // 一半概率
            }

        }


    }
}
