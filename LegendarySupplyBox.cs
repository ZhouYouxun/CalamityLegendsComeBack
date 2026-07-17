using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.SHPCBook;
using CalamityMod.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    public class LegendarySupplyBox : ModItem, ILocalizedModType
    {
        //public override string Texture => "CalamityLegendsComeBack/传奇补给箱";
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
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SHPCBook>()));
        }

        public override void RightClick(Player player)
        {
            IEntitySource source = player.GetSource_FromThis();
            foreach (int itemType in GetFixedPrefixLegendaryWeapons())
                QuickSpawnNoPrefixItem(player, source, itemType);
        }

        private static int[] GetFixedPrefixLegendaryWeapons()
        {
            return new[]
            {
                ModContent.ItemType<NewLegendSHPC>(),
            };
        }

        private static void QuickSpawnNoPrefixItem(Player player, IEntitySource source, int itemType)
        {
            Item spawnedItem = new();
            spawnedItem.SetDefaults(itemType);
            spawnedItem.prefix = 0;
            player.QuickSpawnItem(source, spawnedItem, spawnedItem.stack);
        }
    }
}
