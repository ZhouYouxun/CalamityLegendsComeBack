using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    /// <summary>
    /// Shows an item's registration name beside its localized display name in non-English game cultures.
    /// </summary>
    internal sealed class InternalItemNameTooltipGlobal : GlobalItem
    {
        private const string ItemNameTooltipLine = "ItemName";

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (CLCBClientConfig.Instance?.ShowInternalEnglishNames != true ||
                Language.ActiveCulture.Name == GameCulture.CultureName.English.ToString())
            {
                return;
            }

            string internalName = item.ModItem?.Name ?? ItemID.Search.GetName(item.type);
            if (string.IsNullOrWhiteSpace(internalName))
            {
                return;
            }

            foreach (TooltipLine tooltip in tooltips)
            {
                if (tooltip.Mod == "Terraria" && tooltip.Name == ItemNameTooltipLine)
                {
                    tooltip.Text += $" [{internalName}]";
                    return;
                }
            }
        }
    }
}
