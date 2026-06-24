using CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.ElephantHunter.Projectiles;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DesertEagleBase = CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.DesertEagle;
using LightningEagleItem = CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.LightningEagle.LightningEagle;
using MagnumItem = CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.Magnum.Magnum;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.ElephantHunter
{
    public sealed class ElephantHunter : DesertEagleBase
    {
        public override string DesertEagleTextureAssetPath => "CalamityLegendsComeBack/Weapons/A_Olds/DesertEagle/ElephantHunter/ElephantHunter";
        public override int PrimaryVolleyProjectileType => ModContent.ProjectileType<ElephantHunterBigGameRound>();
        public override int LifeRoundProjectileType => ModContent.ProjectileType<ElephantHunterSiegeRound>();
        public override int ChargedRoundProjectileType => ModContent.ProjectileType<ElephantHunterSiegeRound>();
        public override bool UsesSilverVolleyVisuals => false;

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
                .AddIngredient<MagnumItem>()
                .AddIngredient<LightningEagleItem>()
                .AddIngredient<CosmiliteBar>(8)
                .AddIngredient<DarksunFragment>(5)
                .AddTile<CosmicAnvil>()
                .Register();
        }
    }
}
