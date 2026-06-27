using System;
using System.Collections.Generic;
using CalamityMod.Items;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    public sealed class NewDragoonDrizzlefish : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityMod/Items/Fishing/BrimstoneCragCatches/DragoonDrizzlefish";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Type] = true;
            Item.staff[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 38;
            Item.damage = 32;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 1.1f;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item20;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FoodDrizzlefishFireball>();
            Item.shootSpeed = 11f;
        }

        public override Vector2? HoldoutOrigin() => new(7f, 7f);

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            DragoonDrizzlefishPlayer mealPlayer = player.GetModPlayer<DragoonDrizzlefishPlayer>();
            bool feeding = player.altFunctionUse == 2;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.UseSound = feeding ? SoundID.Item2 : SoundID.Item20;
            Item.shoot = ModContent.ProjectileType<FoodDrizzlefishFireball>();
            Item.shootSpeed = 11f;

            if (feeding)
            {
                Item.useTime = 24;
                Item.useAnimation = 24;
                return FindFoodSlot(player) >= 0;
            }

            int useTime = DragoonDrizzlefishMeals.UseTime(mealPlayer.ActiveMeal, mealPlayer.Overfed);
            Item.useTime = useTime;
            Item.useAnimation = useTime;
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!Main.keyState.PressingShift())
                return;

            tooltips.RemoveAll(line => line.Mod == "Terraria" &&
                line.Name.StartsWith("Tooltip", StringComparison.Ordinal));
            tooltips.Add(new TooltipLine(Mod, "MealDetails", this.GetLocalizedValue("MealDetails"))
            {
                OverrideColor = new Color(230, 238, 255)
            });
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
                return Feed(player);

            DragoonDrizzlefishPlayer mealPlayer = player.GetModPlayer<DragoonDrizzlefishPlayer>();
            DragoonDrizzlefishMealType meal = mealPlayer.ActiveMeal;
            bool overfed = mealPlayer.Overfed;

            velocity = velocity.RotatedByRandom(DragoonDrizzlefishMeals.SpreadRadians(meal, overfed));
            if (overfed)
                velocity *= Main.rand.NextFloat(0.86f, 1.16f);

            int interval = DragoonDrizzlefishMeals.BigFireInterval(meal);
            int shotType;
            if (mealPlayer.ShotCounter < interval - 1)
            {
                shotType = ModContent.ProjectileType<FoodDrizzlefishFireball>();
                mealPlayer.ShotCounter++;
            }
            else
            {
                shotType = ModContent.ProjectileType<FoodDrizzlefishFire>();
                mealPlayer.ShotCounter = 0;
            }

            int packedMeal = mealPlayer.PackedMealForProjectile();
            int projectile = Projectile.NewProjectile(source, position, velocity, shotType, damage, knockback, player.whoAmI, packedMeal, Main.rand.Next(2));
            if (Main.projectile.IndexInRange(projectile))
                Main.projectile[projectile].CritChance = player.GetWeaponCrit(Item);

            mealPlayer.RegisterShot();
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<CalamityMod.Items.Fishing.BrimstoneCragCatches.DragoonDrizzlefish>())
                .AddIngredient(ItemID.Gel, 50)
                .AddTile(TileID.CookingPots)
                .Register();
        }

        private static bool Feed(Player player)
        {
            int slot = FindFoodSlot(player);
            if (slot < 0)
                return false;

            Item food = player.inventory[slot];
            DragoonDrizzlefishPlayer mealPlayer = player.GetModPlayer<DragoonDrizzlefishPlayer>();
            mealPlayer.Feed(food);

            food.stack--;
            if (food.stack <= 0)
                food.TurnToAir();

            return false;
        }

        private static int FindFoodSlot(Player player)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item is null || item.IsAir || item.favorited)
                    continue;

                if (DragoonDrizzlefishMeals.TryClassify(item, out _))
                    return i;
            }

            return -1;
        }
    }
}
