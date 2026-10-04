using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.AegisBlade;
using CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper;
using CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty;
using CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir;
using CalamityLegendsComeBack.Weapons.A_Upgrade.P90;
using CalamityLegendsComeBack.Weapons.CosmicDischarge;
using CalamityLegendsComeBack.Weapons.GaelsGreatsword;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor;
using CalamityLegendsComeBack.Weapons.Malachite;
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
            ModContent.ItemType<NewLegendCosmicDischarge>(),
            ModContent.ItemType<NewLegendGaelsGreatsword>(),
            ModContent.ItemType<GlacialEmbrace>(),
            ModContent.ItemType<LeonidProgenitor>(),
            ModContent.ItemType<Malachite>(),
            ModContent.ItemType<NewLegendPristineFury>(),
            ModContent.ItemType<SeasSearing>(),
            ModContent.ItemType<NewVesuvius>(),
            ModContent.ItemType<NewLegendYharimsCrystal>(),
            ModContent.ItemType<LegendaryBlackHawkRemote>(),
            ModContent.ItemType<CallofDuty>(),
            ModContent.ItemType<NewDragoonDrizzlefish>(),
            ModContent.ItemType<UmbralNadir>()
        };

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (UnbalancedWeapons.Contains(item.type) || item.ModItem is AethersWhisper or NewLegendP90)
                tooltips.Insert(0, new TooltipLine(Mod, TooltipLineName, WarningText));
        }
    }
}
