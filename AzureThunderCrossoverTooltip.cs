using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    internal sealed class AzureThunderCrossoverTooltip : GlobalItem
    {
        internal const string TooltipLineName = "CrossoverWeapon";

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Type itemType = item.ModItem?.GetType();
            if (itemType == null)
                return;

            bool isAzureThunder = item.ModItem is AzureThunder;
            bool isAzureThunderAccessory = itemType.Namespace?.StartsWith(
                "CalamityLegendsComeBack.Accssory.TS", StringComparison.Ordinal) == true;

            if (isAzureThunder || isAzureThunderAccessory)
                tooltips.Add(new TooltipLine(Mod, TooltipLineName,
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.CrossoverItem")));
        }
    }
}
