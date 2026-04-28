using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod.Rarities;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    public class LegendarySupplyBox : ModItem, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/传奇补给箱";
        public new string LocalizationCategory => "Items.Consumables";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.value = Item.sellPrice(gold: 1);
        }

        public override bool CanRightClick() => true;

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NewLegendSHPC>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NewLegendBrinyBaron>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NewLegendBlossomFlux>()));
        }
    }
}
