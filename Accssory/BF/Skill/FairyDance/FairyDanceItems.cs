using CalamityLegendsComeBack.Accssory.BF.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.FairyDance
{
    public sealed class FairyDance : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/General/BFTunedQuiver";

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
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/General/BFDominationQuiver";

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
            player.GetDamage(DamageClass.Ranged) += 0.20f;
            player.GetCritChance(DamageClass.Ranged) += 10f;
            player.GetModPlayer<BFAccessoryPlayer>().BadSeedEquipped = true;
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

    internal abstract class FairyDanceBlessing : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/贴图/复苏之叶";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }

    internal sealed class PinkFairyBlessing : FairyDanceBlessing
    {
        public override void Update(Player player, ref int buffIndex) => player.lifeRegen += 4;
    }

    internal sealed class GreenFairyBlessing : FairyDanceBlessing
    {
        public override void Update(Player player, ref int buffIndex) => player.GetDamage(DamageClass.Ranged) += 0.06f;
    }

    internal sealed class BlueFairyBlessing : FairyDanceBlessing
    {
        public override void Update(Player player, ref int buffIndex) => player.statDefense += 6;
    }

    internal sealed class BadSeedArrowSpeedGlobalItem : GlobalItem
    {
        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (item.useAmmo == AmmoID.Arrow && player.GetModPlayer<BFAccessoryPlayer>().BadSeedEquipped)
                velocity *= 1.15f;
        }
    }
}
