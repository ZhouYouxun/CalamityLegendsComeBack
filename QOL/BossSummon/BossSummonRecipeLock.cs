using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Items.SummonItems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL.BossSummon
{
    /// <summary>
    /// The Zoologist offers these summons as a recovery path before their first kill.
    /// Their recipes stay unavailable until then, so their gated ingredients cannot be bypassed.
    /// </summary>
    internal sealed class BossSummonRecipeLock : ModSystem
    {
        public override void PostAddRecipes()
        {
            if (CalamityLegendsComeBackConfig.Instance?.AllowBossSummonShop != true)
                return;

            foreach (Recipe recipe in Main.recipe)
            {
                if (recipe == null)
                    continue;

                switch (recipe.createItem.type)
                {
                    case int type when type == ModContent.ItemType<Teratoma>():
                        recipe.AddCondition(CalamityConditions.DownedHiveMind);
                        break;
                    case int type when type == ModContent.ItemType<BloodyWormFood>():
                        recipe.AddCondition(CalamityConditions.DownedPerforator);
                        break;
                    case int type when type == ModContent.ItemType<Portabulb>():
                        recipe.AddCondition(Condition.DownedPlantera);
                        break;
                }
            }
        }
    }

    internal sealed class BossSummonRecipeLockTooltip : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (CalamityLegendsComeBackConfig.Instance?.AllowBossSummonShop != true || !IsRecipeLocked(item.type))
                return;

            tooltips.Add(new TooltipLine(Mod, "ZoologistSaleChinese", "售卖自动物学家"));
            tooltips.Add(new TooltipLine(Mod, "ZoologistSaleEnglish", "Sold by the Zoologist"));
        }

        private static bool IsRecipeLocked(int itemType)
        {
            if (itemType == ModContent.ItemType<Teratoma>())
                return !DownedBossSystem.downedHiveMind;
            if (itemType == ModContent.ItemType<BloodyWormFood>())
                return !DownedBossSystem.downedPerforator;
            if (itemType == ModContent.ItemType<Portabulb>())
                return !NPC.downedPlantBoss;

            return false;
        }
    }
}
