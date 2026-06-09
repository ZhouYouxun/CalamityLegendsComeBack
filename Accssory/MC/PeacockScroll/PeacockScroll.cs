using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityMod;
using CalamityMod.Items.Accessories;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.PeacockScroll
{
    public sealed class PeacockScroll : ModItem
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
            player.GetModPlayer<PeacockScrollPlayer>().PeacockScrollEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<SilencingSheath>()
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.Emerald, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public sealed class PeacockScrollPlayer : ModPlayer
    {
        public bool PeacockScrollEquipped;

        public bool HoldingMalachite => Player.HeldItem.type == ModContent.ItemType<Malachite>();

        public bool ShouldRegenerateFrenzyFan =>
            PeacockScrollEquipped &&
            NPC.downedPlantBoss &&
            HoldingMalachite &&
            MalachiteRightFeather.HasStoredRightFeathers(Player);

        public override void ResetEffects()
        {
            PeacockScrollEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (PeacockScrollEquipped)
            {
                Player.GetDamage<RogueDamageClass>() += 0.10f;

                if (HoldingMalachite && MalachiteRightFeather.HasStoredRightFeathers(Player))
                    Player.GetAttackSpeed<RogueDamageClass>() += 0.15f;
            }
        }
    }
}
