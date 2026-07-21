using CalamityLegendsComeBack.Accssory.BF.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.FairyDance
{
    public sealed class BadSeed : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/FairyDance/坏种";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.Pink;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += BFAccessoryPlayer.BadSeedRangedDamageBonus;
            player.GetCritChance(DamageClass.Ranged) += BFAccessoryPlayer.BadSeedRangedCritBonus;
            player.GetModPlayer<BFAccessoryPlayer>().BadSeedEquipped = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return equippedItem.type != ModContent.ItemType<SilvaHarp.SilvaHarp>() &&
                   incomingItem.type != ModContent.ItemType<SilvaHarp.SilvaHarp>() &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.RottenChunk, 15)
                .AddIngredient(ItemID.Deathweed, 10)
                .AddIngredient(ItemID.SoulofNight, 10)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }

    internal sealed class BadSeedArrowSpeedGlobalItem : GlobalItem
    {
        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            BFAccessoryPlayer accessoryPlayer = player.GetModPlayer<BFAccessoryPlayer>();
            if (item.useAmmo == AmmoID.Arrow && accessoryPlayer.HasBadSeedAttributes)
                velocity *= 1f + BFAccessoryPlayer.BadSeedArrowSpeedBonus * accessoryPlayer.BadSeedAttributeMultiplier;
        }
    }
}
