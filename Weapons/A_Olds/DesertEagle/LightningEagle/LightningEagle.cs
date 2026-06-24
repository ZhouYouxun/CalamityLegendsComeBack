using CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.LightningEagle.Projectiles;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DesertEagleBase = CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.DesertEagle;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.LightningEagle
{
    public sealed class LightningEagle : DesertEagleBase
    {
        public override string DesertEagleTextureAssetPath => "CalamityLegendsComeBack/Weapons/A_Olds/DesertEagle/LightningEagle/LightningEagle";
        public override bool HasDesertEaglePrimaryFire => false;
        public override int ChargedRoundProjectileType => ModContent.ProjectileType<LightningEagleArcRound>();

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
}
