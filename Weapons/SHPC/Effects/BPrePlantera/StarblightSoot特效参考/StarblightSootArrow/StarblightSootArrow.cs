namespace CalamityRangerExpansion.Content.Arrows.BPrePlantera.StarblightSootArrow
{
    public class StarblightSootArrow : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Arrows.BPrePlantera";
        public override void SetDefaults()
        {
            Item.damage = 8;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 14;
            Item.height = 32;
            Item.maxStack = 9999;
            Item.consumable = true; // 弹药是消耗品
            Item.knockBack = 3.5f;
            Item.value = 10;
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<StarblightSootArrowPROJ>();
            Item.shootSpeed = 15f;
            Item.ammo = AmmoID.Arrow; // 这是箭矢类型的弹药
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(100);
            recipe.AddRecipeGroup("CalamityRangerExpansion:RecipeGroupArrow", 100);
            recipe.AddIngredient<StarblightSoot>(2);
            recipe.AddIngredient<AstralMonolith>(2);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}
