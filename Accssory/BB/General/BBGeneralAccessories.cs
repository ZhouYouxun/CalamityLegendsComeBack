using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public abstract class BBMeleeBonusAccessory : ModItem
    {
        protected abstract float MeleeBonus { get; }
        protected virtual int Rarity => ItemRarityID.Yellow;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = Rarity;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BBAccessoryPlayer>().GeneralMeleeDamageBonus += MeleeBonus;
        }
    }

    public class BottledRaft : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.04f;
        protected override int Rarity => ItemRarityID.Orange;
    }

    public class BottledBoat : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.07f;
        protected override int Rarity => ItemRarityID.LightRed;
    }

    public class BottledBlackPearl : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.10f;
        protected override int Rarity => ItemRarityID.Yellow;
    }

    public class BottledAircraftCarrier : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.14f;
        protected override int Rarity => ItemRarityID.Cyan;
    }

    public class DrinkingFountain : ModItem
    {
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
            player.GetModPlayer<BBAccessoryPlayer>().DrinkingFountainEquipped = true;
        }
    }

    public class AdrenalineInjector : ModItem
    {
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
            player.GetModPlayer<BBAccessoryPlayer>().AdrenalineInjectorEquipped = true;
        }
    }

    public class TideRadar : ModItem
    {
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
            player.GetModPlayer<BBAccessoryPlayer>().TideRadarEquipped = true;
        }
    }
}
