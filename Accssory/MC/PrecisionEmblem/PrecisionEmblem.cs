using CalamityMod;
using CalamityMod.Items.Accessories;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.PrecisionEmblem
{
    public sealed class PrecisionEmblem : ModItem
    {
        public override string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 7);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<PrecisionEmblemPlayer>().PrecisionEmblemEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<RogueEmblem>()
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddIngredient(ItemID.Emerald, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public sealed class PrecisionEmblemPlayer : ModPlayer
    {
        public bool PrecisionEmblemEquipped;

        public override void ResetEffects()
        {
            PrecisionEmblemEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (PrecisionEmblemEquipped)
            {
                Player.GetDamage<RogueDamageClass>() += 0.05f;
                Player.GetCritChance<RogueDamageClass>() += 10f;
            }
        }
    }
}
