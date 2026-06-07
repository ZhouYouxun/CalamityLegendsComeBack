using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DesertEagleBase = CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.DesertEagle;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle
{
    public sealed class Magnum : DesertEagleBase
    {
        public override string DesertEagleTextureAssetPath => "CalamityLegendsComeBack/Weapons/A_Olds/DesertEagle/马格南";
        public override bool HasDesertEagleSpin => false;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 52;
            Item.height = 28;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 6);
            Item.Calamity().devItem = false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<SlagMagnum>()
                .AddIngredient<PearlGod>()
                .AddIngredient<CosmiliteBar>(4)
                .AddTile<CosmicAnvil>()
                .Register();
        }
    }

    public sealed class LightningEagle : DesertEagleBase
    {
        public override string DesertEagleTextureAssetPath => "CalamityLegendsComeBack/Weapons/A_Olds/DesertEagle/闪电鹰";
        public override bool HasDesertEaglePrimaryFire => false;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 50;
            Item.height = 28;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 10);
            Item.Calamity().devItem = false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<Hellborn>()
                .AddIngredient<CosmiliteBar>(4)
                .AddIngredient<DarksunFragment>(5)
                .AddTile<CosmicAnvil>()
                .Register();
        }
    }

    public sealed class ElephantHunter : DesertEagleBase
    {
        public override string DesertEagleTextureAssetPath => "CalamityLegendsComeBack/Weapons/A_Olds/DesertEagle/猎象者";

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 56;
            Item.height = 26;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 20);
            Item.Calamity().devItem = false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<Magnum>()
                .AddIngredient<LightningEagle>()
                .AddIngredient<CosmiliteBar>(8)
                .AddIngredient<DarksunFragment>(5)
                .AddTile<CosmicAnvil>()
                .Register();
        }
    }
}
