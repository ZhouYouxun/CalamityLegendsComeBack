using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public abstract class BBMeleeBonusAccessory : ModItem
    {
        protected abstract float MeleeBonus { get; }
        protected virtual int TideCapBonus => 0;
        protected virtual float FullTideDamageBonus => 0f;
        protected virtual bool ShurikenBoatEnhanced => false;
        protected virtual bool WaveInfinitePenetration => false;
        protected virtual bool BottledBlackPearlEquipped => false;
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
            BBAccessoryPlayer acc = player.GetModPlayer<BBAccessoryPlayer>();
            acc.GeneralMeleeDamageBonus += MeleeBonus;
            if (TideCapBonus > acc.BottleTideCapBonus)
                acc.BottleTideCapBonus = TideCapBonus;
            if (FullTideDamageBonus > acc.BottleFullTideDamageBonus)
                acc.BottleFullTideDamageBonus = FullTideDamageBonus;
            if (ShurikenBoatEnhanced)
                acc.ShurikenBoatEnhanced = true;
            if (WaveInfinitePenetration)
                acc.WaveInfinitePenetration = true;
            if (BottledBlackPearlEquipped)
                acc.BottledBlackPearlEquipped = true;
        }
    }

    public class BottledRaft : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.05f;
        protected override int TideCapBonus => 2;
        protected override float FullTideDamageBonus => 0.07f;
        protected override int Rarity => ItemRarityID.Orange;
    }

    public class BottledBoat : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.08f;
        protected override int TideCapBonus => 4;
        protected override float FullTideDamageBonus => 0.10f;
        protected override bool ShurikenBoatEnhanced => true;
        protected override int Rarity => ItemRarityID.LightRed;
    }

    public class BottledBlackPearl : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.12f;
        protected override int TideCapBonus => 6;
        protected override float FullTideDamageBonus => 0.14f;
        protected override bool WaveInfinitePenetration => true;
        protected override bool BottledBlackPearlEquipped => true;
        protected override int Rarity => ItemRarityID.Yellow;
    }

    public class BottledAircraftCarrier : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.15f;
        protected override int TideCapBonus => 9;
        protected override float FullTideDamageBonus => 0.18f;
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
}
