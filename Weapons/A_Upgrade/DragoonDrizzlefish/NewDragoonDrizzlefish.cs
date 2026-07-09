using System;
using System.Collections.Generic;
using CalamityMod;
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
            DragoonDrizzlefishPlayer foodPlayer = player.GetModPlayer<DragoonDrizzlefishPlayer>();
            bool feeding = player.altFunctionUse == 2;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.UseSound = feeding ? null : SoundID.Item20;
            Item.shoot = ModContent.ProjectileType<FoodDrizzlefishFireball>();
            Item.shootSpeed = 11f;

            if (feeding)
            {
                Item.useTime = 24;
                Item.useAnimation = 24;
                return true;
            }

            int useTime = DragoonDrizzlefishFoods.UseTime(foodPlayer.ActiveFood);
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

            DragoonDrizzlefishPlayer foodPlayer = player.GetModPlayer<DragoonDrizzlefishPlayer>();
            if (!foodPlayer.HasFood)
            {
                foodPlayer.HungryFeedback();
                return false;
            }

            SpawnFoodShot(player, source, position, velocity, damage, knockback, foodPlayer);
            foodPlayer.RegisterShot();
            foodPlayer.MaybeAttackCuteFeedback();
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

        private void SpawnFoodShot(Player player, IEntitySource source, Vector2 position, Vector2 velocity,
            int damage, float knockback, DragoonDrizzlefishPlayer foodPlayer)
        {
            DragoonDrizzlefishFoodType food = foodPlayer.ActiveFood;
            int packedFood = foodPlayer.PackedFoodForProjectile();

            switch (food)
            {
                case DragoonDrizzlefishFoodType.Fruit:
                    FireFruitBurst(player, source, position, velocity, damage, knockback, packedFood);
                    break;

                case DragoonDrizzlefishFoodType.Fish:
                    FireFishBubbles(player, source, position, velocity, damage, knockback, packedFood);
                    break;

                case DragoonDrizzlefishFoodType.Alcohol:
                    FireSingle(player, source, position, velocity.SafeNormalize(Vector2.UnitX), ModContent.ProjectileType<FoodDrizzlefishBoozeLaser>(),
                        (int)(damage * 0.95f), knockback, packedFood);
                    break;

                case DragoonDrizzlefishFoodType.Feast:
                    FireSingle(player, source, position, velocity * 0.92f, ModContent.ProjectileType<FoodDrizzlefishFeastComet>(),
                        (int)(damage * 1.28f), knockback + 1.2f, packedFood);
                    break;

                case DragoonDrizzlefishFoodType.Snack:
                    FireSingle(player, source, position, velocity * 1.03f, ModContent.ProjectileType<FoodDrizzlefishSnackRocket>(),
                        (int)(damage * 0.95f), knockback, packedFood);
                    break;

                case DragoonDrizzlefishFoodType.Superfood:
                    FireSingle(player, source, position, velocity * 0.95f, ModContent.ProjectileType<FoodDrizzlefishGoldenSeeker>(),
                        (int)(damage * 2f), knockback + 2f, packedFood);
                    break;

                case DragoonDrizzlefishFoodType.OddMushroom:
                    FireOddMushroom(player, source, position, velocity, damage, knockback, packedFood);
                    break;

                default:
                    FireFishflames(player, source, position, velocity, damage, knockback, foodPlayer, packedFood);
                    break;
            }
        }

        private void FireFishflames(Player player, IEntitySource source, Vector2 position, Vector2 velocity,
            int damage, float knockback, DragoonDrizzlefishPlayer foodPlayer, int packedFood)
        {
            DragoonDrizzlefishFoodType food = foodPlayer.ActiveFood;
            velocity = velocity.RotatedByRandom(MathHelper.ToRadians(food == DragoonDrizzlefishFoodType.Meat ? 3.5f : 5.5f));

            int interval = food == DragoonDrizzlefishFoodType.Meat ? 3 : 4;
            int shotType;
            if (foodPlayer.ShotCounter < interval - 1)
            {
                shotType = ModContent.ProjectileType<FoodDrizzlefishFireball>();
                foodPlayer.ShotCounter++;
            }
            else
            {
                shotType = ModContent.ProjectileType<FoodDrizzlefishFire>();
                foodPlayer.ShotCounter = 0;
            }

            FireSingle(player, source, position, velocity, shotType, damage, knockback, packedFood, Main.rand.Next(2));
        }

        private void FireFruitBurst(Player player, IEntitySource source, Vector2 position, Vector2 velocity,
            int damage, float knockback, int packedFood)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            float[] rotations = { -0.16f, 0f, 0.16f };
            for (int i = 0; i < rotations.Length; i++)
            {
                Vector2 shotVelocity = direction.RotatedBy(rotations[i]) * velocity.Length() * Main.rand.NextFloat(0.92f, 1.08f);
                FireSingle(player, source, position + direction.RotatedBy(MathHelper.PiOver2) * (i - 1) * 5f,
                    shotVelocity, ModContent.ProjectileType<FoodDrizzlefishFruitNeedle>(), (int)(damage * 0.62f), knockback * 0.55f, packedFood);
            }
        }

        private void FireFishBubbles(Player player, IEntitySource source, Vector2 position, Vector2 velocity,
            int damage, float knockback, int packedFood)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 shotVelocity = velocity.RotatedBy(MathHelper.ToRadians(7f * i)) * Main.rand.NextFloat(0.92f, 1.04f);
                FireSingle(player, source, position, shotVelocity, ModContent.ProjectileType<FoodDrizzlefishFishBubble>(),
                    (int)(damage * 0.72f), knockback * 0.8f, packedFood);
            }
        }

        private void FireOddMushroom(Player player, IEntitySource source, Vector2 position, Vector2 velocity,
            int damage, float knockback, int packedFood)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            for (int i = 0; i < 4; i++)
            {
                Vector2 shotVelocity = direction.RotatedBy(MathHelper.PiOver2 * i) * velocity.Length();
                FireSingle(player, source, position, shotVelocity, ModContent.ProjectileType<FoodDrizzlefishOddSpore>(),
                    damage, knockback, packedFood);
            }
        }

        private void FireSingle(Player player, IEntitySource source, Vector2 position, Vector2 velocity,
            int projectileType, int damage, float knockback, int packedFood, float ai1 = 0f)
        {
            int projectile = Projectile.NewProjectile(source, position, velocity, projectileType,
                Math.Max(1, damage), knockback, player.whoAmI, packedFood, ai1);
            if (Main.projectile.IndexInRange(projectile))
                Main.projectile[projectile].CritChance = player.GetWeaponCrit(Item);
        }

        private static bool Feed(Player player)
        {
            int slot = FindFoodSlot(player);
            DragoonDrizzlefishPlayer foodPlayer = player.GetModPlayer<DragoonDrizzlefishPlayer>();
            if (slot < 0)
            {
                foodPlayer.HungryFeedback(force: true);
                return false;
            }

            Item food = player.inventory[slot];
            foodPlayer.Feed(food);

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

                if (DragoonDrizzlefishFoods.TryClassify(item, out _))
                    return i;
            }

            return -1;
        }
    }

    internal sealed class NewDragoonDrizzlefishShop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.BestiaryGirl)
                shop.AddWithCustomValue<NewDragoonDrizzlefish>(Item.buyPrice(gold: 20));
        }
    }
}
