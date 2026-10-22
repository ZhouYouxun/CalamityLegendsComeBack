using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle;
using CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote;
using CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty;
using CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor;
using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityLegendsComeBack.Weapons.SeasSearing;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    [LegacyName("LegendarySupplyBox")]
    public class LongLostWeaponBlueprints : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Consumables";

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 38;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.value = Item.sellPrice(gold: 1);
        }

        private static int[] GetMainLegendaryWeapons() =>
        [
            ModContent.ItemType<NewLegendBrinyBaron>(),
            ModContent.ItemType<NewLegendBlossomFlux>(),
            ModContent.ItemType<LeonidProgenitor>(),
            ModContent.ItemType<Malachite>(),
            ModContent.ItemType<NewLegendSHPC>(),
            ModContent.ItemType<SeasSearing>(),
        ];

        public override void ModifyTooltips(List<TooltipLine> tooltips) =>
            tooltips.Add(new TooltipLine(Mod, "FlavorText", this.GetLocalizedValue("FlavorText")));

        internal static int[] GetBlueprintRecipeWeapons()
        {
            List<int> weapons = new(GetMainLegendaryWeapons())
            {
                ModContent.ItemType<NewLegendAntiMaterielRifle>(),
                ModContent.ItemType<CallofDuty>(),
                ModContent.ItemType<NewDragoonDrizzlefish>(),
                ModContent.ItemType<UmbralNadir>(),
                ModContent.ItemType<AzureThunder>()
            };

            return weapons.ToArray();
        }

        internal static bool UsesBlueprintRecipe(int itemType) =>
            Array.IndexOf(GetBlueprintRecipeWeapons(), itemType) >= 0;
    }
}
