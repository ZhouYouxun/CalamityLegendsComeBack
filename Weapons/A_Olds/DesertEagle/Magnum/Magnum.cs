using CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.Magnum.Projectiles;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DesertEagleBase = CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.DesertEagle;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.Magnum
{
    public sealed class Magnum : DesertEagleBase
    {
        public override string DesertEagleTextureAssetPath => "CalamityLegendsComeBack/Weapons/A_Olds/DesertEagle/Magnum/Magnum";
        public override bool HasDesertEagleSpin => false;
        public override int PrimaryVolleyProjectileType => ModContent.ProjectileType<MagnumSilverRound>();
        public override int LifeRoundProjectileType => ModContent.ProjectileType<MagnumLifeRound>();
        public override bool UsesSilverVolleyVisuals => false;

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
}
