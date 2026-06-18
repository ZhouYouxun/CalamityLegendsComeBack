using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public abstract class BBMeleeBonusAccessory : ModItem
    {
        public override string Texture => $"CalamityLegendsComeBack/Accssory/BB/General/{GetType().Name}/{GetType().Name}";

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
}
