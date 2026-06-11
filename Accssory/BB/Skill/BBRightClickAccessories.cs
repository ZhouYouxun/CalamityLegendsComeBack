using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill
{
    public abstract class BBRightClickAccessory : ModItem
    {
        protected abstract BBRightClickMode Mode { get; }
        protected abstract int Priority { get; }

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
            player.GetModPlayer<BBAccessoryPlayer>().SetRightClickMode(Mode, Priority);
        }
    }

    public class VortexPortal : BBRightClickAccessory
    {
        protected override BBRightClickMode Mode => BBRightClickMode.VortexPortal;
        protected override int Priority => 30;
    }

    public class LostGarment : BBRightClickAccessory
    {
        protected override BBRightClickMode Mode => BBRightClickMode.LostGarment;
        protected override int Priority => 10;
    }

    public class CeruleanShield : BBRightClickAccessory
    {
        protected override BBRightClickMode Mode => BBRightClickMode.CeruleanShield;
        protected override int Priority => 20;
    }
}
