namespace CalamityRangerExpansion.Content.Ammunition.BPrePlantera.StarblightSootBullet
{
    internal class StarblightSootBullet : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Ammunition.BPrePlantera";


        public override void SetDefaults()
        {
            Item.width = 8;
            Item.height = 18;
            Item.damage = 20;
            Item.DamageType = DamageClass.Ranged;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.knockBack = 3f;
            Item.value = Item.sellPrice(copper: 12);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<StarblightSootBulletPROJ>();
            Item.shootSpeed = 6f;
            Item.ammo = AmmoID.Bullet;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(150);
            recipe.AddRecipeGroup("CalamityRangerExpansion:RecipeGroupBullet", 150);
            recipe.AddIngredient<StarblightSoot>(2);
            recipe.AddIngredient<AstralMonolith>(2);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}
