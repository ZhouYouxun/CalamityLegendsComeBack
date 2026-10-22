using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.AegisBlade;
using CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper;
using CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty;
using CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir;
using CalamityLegendsComeBack.Weapons.A_Upgrade.P90;
using CalamityLegendsComeBack.Weapons.CosmicDischarge;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace;
using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityLegendsComeBack.Weapons.PristineFury;
using CalamityLegendsComeBack.Weapons.SeasSearing;
using CalamityLegendsComeBack.Weapons.Vesuvius;
using CalamityLegendsComeBack.Weapons.YharimsCrystal;
using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;
using CalamityMod;

namespace CalamityLegendsComeBack
{
    internal sealed class LegendaryBalanceStatusGlobalItem : GlobalItem
    {
        internal const string TooltipLineName = "LegendaryBalanceStatus";
        internal const string WarningText = "该武器尚未平衡，不建议游玩";
        internal const string UnfinishedWarningText = "该武器尚未制作完成，现已停止维护";

        private static readonly HashSet<int> ArchivedDescriptionWeapons = new()
        {
            ModContent.ItemType<NewLegendCosmicDischarge>(),
            ModContent.ItemType<GlacialEmbrace>(),
            ModContent.ItemType<NewLegendPristineFury>(),
            ModContent.ItemType<NewVesuvius>(),
            ModContent.ItemType<NewLegendYharimsCrystal>()
        };

        private static readonly HashSet<int> UnfinishedWeapons = new()
        {
            ModContent.ItemType<AegisBlade>(),
            ModContent.ItemType<NewLegendCosmicDischarge>(),
            ModContent.ItemType<GlacialEmbrace>(),
            ModContent.ItemType<NewLegendPristineFury>(),
            ModContent.ItemType<NewVesuvius>(),
            ModContent.ItemType<NewLegendYharimsCrystal>()
        };

        private static readonly HashSet<int> UnbalancedWeapons = new()
        {
            ModContent.ItemType<AegisBlade>(),
            ModContent.ItemType<NewLegendCosmicDischarge>(),
            ModContent.ItemType<GlacialEmbrace>(),
            ModContent.ItemType<Malachite>(),
            ModContent.ItemType<NewLegendPristineFury>(),
            ModContent.ItemType<SeasSearing>(),
            ModContent.ItemType<NewVesuvius>(),
            ModContent.ItemType<NewLegendYharimsCrystal>(),
            ModContent.ItemType<CallofDuty>(),
            ModContent.ItemType<NewDragoonDrizzlefish>(),
            ModContent.ItemType<UmbralNadir>()
        };

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (ArchivedDescriptionWeapons.Contains(item.type))
            {
                bool expanded = Main.keyState.PressingShift();
                int descriptionIndex = tooltips.FindIndex(line =>
                    line.Mod == "Terraria" && line.Name.StartsWith("Tooltip"));
                if (descriptionIndex < 0)
                {
                    descriptionIndex = tooltips.FindIndex(line => line.Mod == Mod.Name);
                    if (descriptionIndex < 0)
                        descriptionIndex = tooltips.Count;
                }

                if (!expanded)
                {
                    // ItemLoader has already run the weapon's ModifyTooltips. Remove its
                    // generated body and special text, retaining Terraria's stat/prefix lines.
                    tooltips.RemoveAll(line => line.Mod == Mod.Name ||
                        line.Mod == "Terraria" && line.Name.StartsWith("Tooltip"));
                }

                descriptionIndex = System.Math.Min(descriptionIndex, tooltips.Count);
                tooltips.Insert(descriptionIndex, new TooltipLine(Mod, TooltipLineName,
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.ArchivedWeaponTooltip.Warning")));
                tooltips.Insert(descriptionIndex + 1, new TooltipLine(Mod, "ArchivedWeaponShiftHint",
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.ArchivedWeaponTooltip.ShiftHint")));
                return;
            }

            if (UnbalancedWeapons.Contains(item.type) || item.ModItem is AethersWhisper or NewLegendP90)
                tooltips.Insert(0, new TooltipLine(Mod, TooltipLineName,
                    UnfinishedWeapons.Contains(item.type) ? UnfinishedWarningText : WarningText));
        }
    }
}
