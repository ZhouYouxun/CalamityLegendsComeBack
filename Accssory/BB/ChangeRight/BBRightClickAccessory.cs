using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.ChangeRight
{
    public abstract class BBRightClickAccessory : ModItem
    {
        protected abstract BBRightClickMode Mode { get; }

        public override string Texture => $"CalamityLegendsComeBack/Accssory/BB/ChangeRight/{GetType().Name}/{GetType().Name}";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BBAccessoryPlayer>().SetRightClickMode(Mode);
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return BBChangeRightAccessoryRules.CanEquipWith(equippedItem, incomingItem) &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }
    }
}
