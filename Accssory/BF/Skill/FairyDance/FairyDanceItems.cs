using CalamityLegendsComeBack.Accssory.BF.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.FairyDance
{
    public sealed class FairyDance : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/FairyDance/妖精舞";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.lifeRegen += 4;
            player.statDefense += 6;
            player.GetModPlayer<BFAccessoryPlayer>().FairyDanceEquipped = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return equippedItem.type != ModContent.ItemType<RainbowSpiritDance>() &&
                   incomingItem.type != ModContent.ItemType<RainbowSpiritDance>() &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.FairyCritterPink)
                .AddIngredient(ItemID.FairyCritterGreen)
                .AddIngredient(ItemID.FairyCritterBlue)
                .AddIngredient(ItemID.PixieDust, 20)
                .AddIngredient(ItemID.SoulofLight, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public sealed class RainbowSpiritDance : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/FairyDance/虹灵";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 18);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.lifeRegen += 6;
            player.statDefense += 10;
            player.GetDamage(DamageClass.Ranged) += 0.10f;

            BFAccessoryPlayer accessoryPlayer = player.GetModPlayer<BFAccessoryPlayer>();
            accessoryPlayer.FairyDanceEquipped = true;
            accessoryPlayer.RainbowSpiritDanceEquipped = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return equippedItem.type != ModContent.ItemType<FairyDance>() &&
                   incomingItem.type != ModContent.ItemType<FairyDance>() &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<FairyDance>()
                .AddIngredient(ItemID.EmpressButterfly)
                .AddIngredient(ItemID.Ectoplasm, 15)
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public sealed class BadSeed : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/General/BFSpareQuiver";

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
