using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.AegisBlade;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.CosmicDischarge;
using CalamityLegendsComeBack.Weapons.GaelsGreatsword;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor;
using CalamityLegendsComeBack.Weapons.PristineFury;
using CalamityLegendsComeBack.Weapons.SeasSearing;
using CalamityLegendsComeBack.Weapons.Vesuvius;
using CalamityLegendsComeBack.Weapons.YharimsCrystal;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    internal sealed class LegendaryBalanceStatusGlobalItem : GlobalItem
    {
        internal const string TooltipLineName = "LegendaryBalanceStatus";
        internal const string WarningText = "该武器尚未平衡，不建议游玩";

        private static readonly HashSet<int> UnbalancedWeapons = new()
        {
            ModContent.ItemType<AegisBlade>(),
            ModContent.ItemType<NewLegendBlossomFlux>(),
            ModContent.ItemType<NewLegendBrinyBaron>(),
            ModContent.ItemType<NewLegendCosmicDischarge>(),
            ModContent.ItemType<NewLegendGaelsGreatsword>(),
            ModContent.ItemType<GlacialEmbrace>(),
            ModContent.ItemType<LeonidProgenitor>(),
            ModContent.ItemType<NewLegendPristineFury>(),
            ModContent.ItemType<SeasSearing>(),
            ModContent.ItemType<NewVesuvius>(),
            ModContent.ItemType<NewLegendYharimsCrystal>()
        };

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (UnbalancedWeapons.Contains(item.type))
                tooltips.Insert(0, new TooltipLine(Mod, TooltipLineName, WarningText));
        }
    }
}
