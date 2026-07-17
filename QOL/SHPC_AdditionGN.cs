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

            if (shop.NpcType == NPCID.BestiaryGirl)
            {
                // 困难模式前，任何时期：10金
                shop.AddWithCustomValue<DesertMedallion>(Item.buyPrice(gold: 10))
                    .AddWithCustomValue<DecapoditaSprout>(Item.buyPrice(gold: 10))
                    .AddWithCustomValue<Teratoma>(Item.buyPrice(gold: 10))
                    .AddWithCustomValue<BloodyWormFood>(Item.buyPrice(gold: 10))
                    .AddWithCustomValue<OverloadedSludge>(Item.buyPrice(gold: 10));

                // 困难模式后、任意机械Boss后（仍属于世纪之花前的困难模式阶段）：20金
                shop.AddWithCustomValue<CryoKey>(Item.buyPrice(gold: 20), Condition.Hardmode)
                    .AddWithCustomValue<Seafood>(Item.buyPrice(gold: 20), Condition.Hardmode)
                    .AddWithCustomValue<CharredIdol>(Item.buyPrice(gold: 20), Condition.Hardmode)
                    .AddWithCustomValue<EyeofDesolation>(Item.buyPrice(gold: 20), Condition.DownedMechBossAny)
                    .AddWithCustomValue<Portabulb>(Item.buyPrice(gold: 20), Condition.DownedMechBossAny);

                // 世纪之花后：40金
                shop.AddWithCustomValue<AstralChunk>(Item.buyPrice(gold: 40), Condition.DownedPlantera)
                    .AddWithCustomValue<EidolonTablet>(Item.buyPrice(gold: 40), Condition.DownedPlantera);

                // 石巨人后：60金
                shop.AddWithCustomValue<Abombination>(Item.buyPrice(gold: 60), Condition.DownedGolem)
                    .AddWithCustomValue<MartianDistressRemote>(Item.buyPrice(gold: 60), Condition.DownedGolem)
                    .AddWithCustomValue<DeathWhistle>(Item.buyPrice(gold: 60), Condition.DownedGolem);

                // 月球领主后：100金
                shop.AddWithCustomValue<ExoticPheromones>(Item.buyPrice(gold: 100), Condition.DownedMoonLord)
                    .AddWithCustomValue<ProfanedShard>(Item.buyPrice(gold: 100), Condition.DownedMoonLord)
                    .AddWithCustomValue<ProfanedCore>(Item.buyPrice(gold: 100), Condition.DownedMoonLord)
                    .AddWithCustomValue<NecroplasmicBeacon>(Item.buyPrice(gold: 100), Condition.DownedMoonLord);
            }

            if (shop.NpcType == NPCID.Mechanic)
            {
                shop.Add<DraedonPowerCell>();
            }
        }

    }
}
