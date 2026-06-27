using System;
using CalamityMod.Items.Placeables.Furniture;
using CalamityMod.Items.Potions.Food;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    internal enum DragoonDrizzlefishMealType
    {
        None = 0,
        Gel = 1,
        Staple = 2,
        Meat = 3,
        Seafood = 4,
        Plant = 5,
        Sweet = 6,
        Spicy = 7,
        Drink = 8,
        Weird = 9,
        HadalStew = 10,
        DeliciousMeat = 11,
        BlasphemousDonut = 12,
        LavaChickenBroth = 13,
        TheSandwich = 14,
        OddMushroom = 15
    }

    internal static class DragoonDrizzlefishMeals
    {
        private const int MealMask = 31;
        private const int OverfedFlag = 32;
        private const int SecondaryFlag = 64;

        public static bool TryClassify(Item item, out DragoonDrizzlefishMealType meal)
        {
            meal = DragoonDrizzlefishMealType.None;
            if (item is null || item.IsAir || item.stack <= 0)
                return false;

            if (item.type == ItemID.Gel)
            {
                meal = DragoonDrizzlefishMealType.Gel;
                return true;
            }

            if (!IsFoodLike(item))
                return false;

            meal = ClassifyFood(item);
            return true;
        }

        public static DragoonDrizzlefishMealType ResolveRandomSlopMeal()
        {
            return Main.rand.Next(7) switch
            {
                0 => DragoonDrizzlefishMealType.Staple,
                1 => DragoonDrizzlefishMealType.Meat,
                2 => DragoonDrizzlefishMealType.Seafood,
                3 => DragoonDrizzlefishMealType.Plant,
                4 => DragoonDrizzlefishMealType.Sweet,
                5 => DragoonDrizzlefishMealType.Spicy,
                _ => DragoonDrizzlefishMealType.Drink
            };
        }

        public static int Pack(DragoonDrizzlefishMealType meal, bool overfed, bool secondary = false)
        {
            int packed = (int)meal & MealMask;
            if (overfed)
                packed |= OverfedFlag;
            if (secondary)
                packed |= SecondaryFlag;
            return packed;
        }

        public static DragoonDrizzlefishMealType GetMeal(Projectile projectile)
            => (DragoonDrizzlefishMealType)((int)projectile.ai[0] & MealMask);

        public static bool IsOverfed(Projectile projectile)
            => (((int)projectile.ai[0] & OverfedFlag) != 0);

        public static bool IsSecondary(Projectile projectile)
            => (((int)projectile.ai[0] & SecondaryFlag) != 0);

        public static int MealDuration(DragoonDrizzlefishMealType meal)
        {
            return meal switch
            {
                DragoonDrizzlefishMealType.HadalStew => 18 * 60,
                DragoonDrizzlefishMealType.DeliciousMeat => 16 * 60,
                DragoonDrizzlefishMealType.BlasphemousDonut => 11 * 60,
                DragoonDrizzlefishMealType.LavaChickenBroth => 16 * 60,
                DragoonDrizzlefishMealType.TheSandwich => 8 * 60,
                DragoonDrizzlefishMealType.OddMushroom => 14 * 60,
                DragoonDrizzlefishMealType.Gel => 8 * 60,
                _ => 12 * 60
            };
        }

        public static int MealShots(DragoonDrizzlefishMealType meal)
        {
            return meal switch
            {
                DragoonDrizzlefishMealType.HadalStew => 36,
                DragoonDrizzlefishMealType.DeliciousMeat => 28,
                DragoonDrizzlefishMealType.BlasphemousDonut => 24,
                DragoonDrizzlefishMealType.LavaChickenBroth => 30,
                DragoonDrizzlefishMealType.TheSandwich => 16,
                DragoonDrizzlefishMealType.OddMushroom => 26,
                DragoonDrizzlefishMealType.Gel => 14,
                _ => 24
            };
        }

        public static int FullnessGain(DragoonDrizzlefishMealType meal)
        {
            return meal switch
            {
                DragoonDrizzlefishMealType.HadalStew or
                DragoonDrizzlefishMealType.DeliciousMeat or
                DragoonDrizzlefishMealType.LavaChickenBroth or
                DragoonDrizzlefishMealType.TheSandwich => 3,
                DragoonDrizzlefishMealType.Gel => 1,
                _ => 2
            };
        }

        public static int UseTime(DragoonDrizzlefishMealType meal, bool overfed)
        {
            int useTime = meal switch
            {
                DragoonDrizzlefishMealType.Sweet or
                DragoonDrizzlefishMealType.BlasphemousDonut => 15,
                DragoonDrizzlefishMealType.Drink => 16,
                DragoonDrizzlefishMealType.Staple => 18,
                DragoonDrizzlefishMealType.Meat or
                DragoonDrizzlefishMealType.DeliciousMeat => 24,
                DragoonDrizzlefishMealType.TheSandwich => 17,
                _ => 20
            };

            return overfed ? useTime + 4 : useTime;
        }

        public static int BigFireInterval(DragoonDrizzlefishMealType meal)
        {
            return meal switch
            {
                DragoonDrizzlefishMealType.Sweet or
                DragoonDrizzlefishMealType.BlasphemousDonut => 3,
                DragoonDrizzlefishMealType.Meat or
                DragoonDrizzlefishMealType.DeliciousMeat => 5,
                _ => 4
            };
        }

        public static float SpreadRadians(DragoonDrizzlefishMealType meal, bool overfed)
        {
            float spread = meal switch
            {
                DragoonDrizzlefishMealType.Staple => MathHelper.ToRadians(2.5f),
                DragoonDrizzlefishMealType.Drink => MathHelper.ToRadians(13f),
                DragoonDrizzlefishMealType.OddMushroom or
                DragoonDrizzlefishMealType.TheSandwich => MathHelper.ToRadians(11f),
                _ => MathHelper.ToRadians(5.5f)
            };

            return overfed ? spread + MathHelper.ToRadians(5f) : spread;
        }

        public static float DamageMultiplier(DragoonDrizzlefishMealType meal)
        {
            return meal switch
            {
                DragoonDrizzlefishMealType.Gel => 1.06f,
                DragoonDrizzlefishMealType.Staple => 1.07f,
                DragoonDrizzlefishMealType.Meat => 1.18f,
                DragoonDrizzlefishMealType.DeliciousMeat => 1.32f,
                DragoonDrizzlefishMealType.Seafood => 1.08f,
                DragoonDrizzlefishMealType.HadalStew => 1.14f,
                DragoonDrizzlefishMealType.Spicy => 1.16f,
                DragoonDrizzlefishMealType.LavaChickenBroth => 1.24f,
                DragoonDrizzlefishMealType.Sweet => 0.94f,
                DragoonDrizzlefishMealType.BlasphemousDonut => 0.98f,
                DragoonDrizzlefishMealType.Drink => 0.95f,
                DragoonDrizzlefishMealType.TheSandwich => 1.05f,
                _ => 1f
            };
        }

        public static float ScaleMultiplier(DragoonDrizzlefishMealType meal)
        {
            return meal switch
            {
                DragoonDrizzlefishMealType.Meat => 1.14f,
                DragoonDrizzlefishMealType.DeliciousMeat => 1.25f,
                DragoonDrizzlefishMealType.HadalStew => 1.08f,
                DragoonDrizzlefishMealType.LavaChickenBroth => 1.12f,
                DragoonDrizzlefishMealType.Sweet or
                DragoonDrizzlefishMealType.BlasphemousDonut => 0.9f,
                _ => 1f
            };
        }

        public static Color MealColor(DragoonDrizzlefishMealType meal)
        {
            return meal switch
            {
                DragoonDrizzlefishMealType.Gel => Color.SkyBlue,
                DragoonDrizzlefishMealType.Staple => new Color(255, 220, 150),
                DragoonDrizzlefishMealType.Meat or
                DragoonDrizzlefishMealType.DeliciousMeat => new Color(255, 120, 70),
                DragoonDrizzlefishMealType.Seafood or
                DragoonDrizzlefishMealType.HadalStew => new Color(85, 210, 255),
                DragoonDrizzlefishMealType.Plant => new Color(95, 240, 95),
                DragoonDrizzlefishMealType.Sweet or
                DragoonDrizzlefishMealType.BlasphemousDonut => new Color(255, 150, 235),
                DragoonDrizzlefishMealType.Spicy or
                DragoonDrizzlefishMealType.LavaChickenBroth => new Color(255, 70, 20),
                DragoonDrizzlefishMealType.Drink => new Color(190, 120, 255),
                DragoonDrizzlefishMealType.Weird or
                DragoonDrizzlefishMealType.OddMushroom => new Color(175, 255, 70),
                DragoonDrizzlefishMealType.TheSandwich => new Color(255, 255, 120),
                _ => Color.OrangeRed
            };
        }

        private static bool IsFoodLike(Item item)
        {
            if (item.type > ItemID.None && item.type < ItemID.Sets.IsFood.Length && ItemID.Sets.IsFood[item.type])
                return true;

            if (item.useStyle == ItemUseStyleID.EatFood)
                return true;

            string internalName = StableName(item);
            return item.ModItem?.GetType().Namespace?.Contains(".Potions.Food", StringComparison.Ordinal) == true ||
                internalName.Contains("Food", StringComparison.OrdinalIgnoreCase) ||
                internalName.Contains("Stew", StringComparison.OrdinalIgnoreCase) ||
                internalName.Contains("Soup", StringComparison.OrdinalIgnoreCase);
        }

        private static DragoonDrizzlefishMealType ClassifyFood(Item item)
        {
            int type = item.type;
            if (type == ModContent.ItemType<HadalStew>())
                return DragoonDrizzlefishMealType.HadalStew;
            if (type == ModContent.ItemType<DeliciousMeat>())
                return DragoonDrizzlefishMealType.DeliciousMeat;
            if (type == ModContent.ItemType<BlasphemousDonut>())
                return DragoonDrizzlefishMealType.BlasphemousDonut;
            if (type == ModContent.ItemType<LavaChickenBroth>())
                return DragoonDrizzlefishMealType.LavaChickenBroth;
            if (type == ModContent.ItemType<TheSandwich>())
                return DragoonDrizzlefishMealType.TheSandwich;
            if (type == ModContent.ItemType<QualitySlop>())
                return ResolveRandomSlopMeal();
            if (type == ModContent.ItemType<CalamityMod.Items.Potions.Alcohol.OddMushroom>())
                return DragoonDrizzlefishMealType.OddMushroom;

            string stableName = StableName(item);
            string lowered = stableName.ToLowerInvariant();
            string ns = item.ModItem?.GetType().Namespace ?? string.Empty;

            if (ns.Contains(".Alcohol", StringComparison.Ordinal) || ContainsAny(lowered, "ale", "beer", "wine", "rum", "vodka", "tequila", "whiskey", "gin", "margarita", "coffee", "tea", "juice", "mule", "moonshine", "screwdriver"))
                return DragoonDrizzlefishMealType.Drink;

            if (ContainsAny(lowered, "lava", "brimstone", "fireball", "pepper", "spicy", "curry", "salsa"))
                return DragoonDrizzlefishMealType.Spicy;

            if (ContainsAny(lowered, "donut", "cake", "cookie", "pie", "cinnamon", "roll", "chocolate", "pudding", "marshmallow", "icecream", "milkshake"))
                return DragoonDrizzlefishMealType.Sweet;

            if (ContainsAny(lowered, "fish", "sushi", "sashimi", "seafood", "clam", "shrimp", "lobster", "oyster", "crab", "tuna", "salmon"))
                return DragoonDrizzlefishMealType.Seafood;

            if (ContainsAny(lowered, "meat", "bacon", "burger", "steak", "chicken", "ribs", "sausage"))
                return DragoonDrizzlefishMealType.Meat;

            if (ContainsAny(lowered, "fruit", "apple", "banana", "grape", "berry", "mango", "peach", "pineapple", "coconut", "lemon", "lotus", "melon", "plum"))
                return DragoonDrizzlefishMealType.Plant;

            if (ContainsAny(lowered, "mushroom", "slop", "strange", "odd"))
                return DragoonDrizzlefishMealType.Weird;

            return DragoonDrizzlefishMealType.Staple;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (value.Contains(needle, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string StableName(Item item)
        {
            if (item.ModItem is not null)
                return item.ModItem.Name;

            return item.type > ItemID.None && item.type < ItemID.Count ? ItemID.Search.GetName(item.type) : item.Name;
        }
    }
}
