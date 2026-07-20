using CalamityLegendsComeBack.Accssory;
using CalamityMod.Items.Accessories;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.SummonItems.Invasion;
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items.Tools;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.SHPCBook;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL
{
    internal class SHPC_AdditionGN : GlobalNPC
    {

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.WallofFlesh)
            {
                LeadingConditionRule firstWallKill = new(DropHelper.If(() => !Main.hardMode, desc: DropHelper.FirstKillText));
                firstWallKill.Add(DropHelper.PerPlayer(ModContent.ItemType<LegendaryEmblem>()), hideLootReport: true);
                npcLoot.Add(firstWallKill);
            }

            if (CalamityLegendsComeBackConfig.Instance?.AllowMassMaterialRecipes != true)
                return;

            if (npc.type == ModContent.NPCType<Stormlion>())
            {
                // 添加：每次必掉（可自行调整为概率掉落）
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StormlionMandible>(), 2)); // 一半概率
            }

        }

        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.Merchant)
            {
                // SHPC 与 SHPC 光盘不再由商人出售，改为通过传奇补给箱获取。
                shop.AddWithCustomValue<LegendarySupplyBox>(Item.buyPrice(gold: 20));
                shop.AddWithCustomValue<RetroGameConsoleSupplyBox>(Item.buyPrice(gold: 5));
                shop.AddWithCustomValue<LegendaryCodex>(Item.buyPrice(gold: 5));
                shop.Add<SuperDummy>();
            }

            if (shop.NpcType == NPCID.Mechanic)
            {
                shop.Add<DraedonPowerCell>();
            }
        }

    }
}
