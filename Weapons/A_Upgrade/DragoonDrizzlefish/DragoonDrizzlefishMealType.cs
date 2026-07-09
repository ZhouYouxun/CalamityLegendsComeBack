using System;
using System.Collections.Generic;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    internal enum DragoonDrizzlefishFoodType
    {
        None = 0,
        Gel = 1,
        Fruit = 2,
        Meat = 3,
        Fish = 4,
        Alcohol = 5,
        Feast = 6,
        Snack = 7,
        Superfood = 8,
        OddMushroom = 9
    }

    internal static class DragoonDrizzlefishFoods
    {
        public const int MagazineSize = 50;

        private const int FoodMask = 31;
        private const int SecondaryFlag = 32;

        private static readonly HashSet<string> GelNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Gel", "PinkGel"
        };

        private static readonly HashSet<string> FruitNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Apple", "Apricot", "Banana", "Blackcurrant", "BlackCurrant", "BloodOrange",
            "Cherry", "Coconut", "Dragonfruit", "DragonFruit", "Elderberry", "ElderBerry",
            "Grapefruit", "GrapeFruit", "Grapes", "Lemon", "Mango", "Peach", "Pineapple",
            "Plum", "Pomegranate", "Rambutan", "Starfruit", "StarFruit",
            "Barberry", "Cometfruit", "Jackfruit", "Lotus", "Mangosteen", "Salak"
        };

        private static readonly HashSet<string> MeatNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Bacon", "ChickenNugget", "FriedEgg", "GrilledSquirrel", "RoastedBird",
            "RoastedDuck", "Steak"
        };

        private static readonly HashSet<string> FishNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "BlackenedFish", "CookedFish", "CookedShrimp", "LobsterTail", "Sashimi",
            "ShuckedOyster"
        };

        private static readonly HashSet<string> AlcoholNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Ale", "Sake", "Wiesnbrau", "Wiesnbräu", "BloodyMoscato",
            "FrozenBananaDaiquiri", "PeachSangria", "PinaColada",
            "BaconOil", "BloodyMary", "CaribbeanRum", "CinnamonRoll", "Everclear",
            "EvergreenGin", "Fireball", "GrapeBeer", "Manhattan", "Margarita",
            "Moonshine", "MoscowMule", "OldFashioned", "PurpleHaze", "RedWine",
            "Rum", "Screwdriver", "StarBeamRye", "Tequila", "TequilaSunrise",
            "Vodka", "Whiskey", "WhiteWine"
        };

        private static readonly HashSet<string> FeastNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "ApplePie", "BananaSplit", "BBQRibs", "BowlOfSoup", "BunnyStew", "Burger",
            "CookedMarshmallow", "ChristmasPudding", "Escargot", "FroggleBunwich",
            "FruitSalad", "GrubSoup", "Hotdog", "MonsterLasagna", "PadThai", "Pho",
            "Pizza", "PumpkinPie", "SauteedFrogLegs", "SeafoodDinner", "ShrimpPoBoy",
            "Spaghetti",
            "Baguette", "BlasphemousDonut", "DeliciousMeat", "HadalStew",
            "LavaChickenBroth", "ShroomBowl", "TheSandwich"
        };

        private static readonly HashSet<string> SnackNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "AppleJuice", "CartonOfMilk", "MilkCarton", "ChocolateChipCookie",
            "Coffee", "CoffeeCup", "CreamSoda", "Eggnog", "Fries", "FruitJuice",
            "GingerbreadCookie", "GrapeJuice", "IceCream", "JojaCola", "JungleJuice",
            "Lemonade", "Marshmallow", "Milkshake", "Nachos", "PotatoChips",
            "PrismaticPunch", "RockCandy", "SmoothieofDarkness", "SmoothieOfDarkness",
            "SpicyPepper", "SugarCookie", "Teacup", "TropicalSmoothie"
        };

        private static readonly HashSet<string> SuperfoodNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "GoldenDelight"
        };

        private static readonly HashSet<string> UniqueNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "OddMushroom"
        };

        private static readonly HashSet<string> ExcludedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "ApplePieSlice", "Seafood", "WormFood", "BloodyWormFood",
            "SparklingEmpress", "GacruxianMollusk", "DragoonDrizzlefish",
            "PolarisParrotfish", "SerpentsBite", "SerpentBite", "QualitySlop"
        };

        public static bool TryClassify(Item item, out DragoonDrizzlefishFoodType food)
        {
            food = DragoonDrizzlefishFoodType.None;
            if (item is null || item.IsAir || item.stack <= 0)
                return false;

            string name = StableName(item);
            if (ExcludedNames.Contains(name))
                return false;

            if (GelNames.Contains(name))
            {
                food = DragoonDrizzlefishFoodType.Gel;
                return true;
            }

            if (UniqueNames.Contains(name))
                food = DragoonDrizzlefishFoodType.OddMushroom;
            else if (SuperfoodNames.Contains(name))
                food = DragoonDrizzlefishFoodType.Superfood;
            else if (AlcoholNames.Contains(name))
                food = DragoonDrizzlefishFoodType.Alcohol;
            else if (FeastNames.Contains(name))
                food = DragoonDrizzlefishFoodType.Feast;
            else if (FishNames.Contains(name))
                food = DragoonDrizzlefishFoodType.Fish;
            else if (MeatNames.Contains(name))
                food = DragoonDrizzlefishFoodType.Meat;
            else if (FruitNames.Contains(name))
                food = DragoonDrizzlefishFoodType.Fruit;
            else if (SnackNames.Contains(name))
                food = DragoonDrizzlefishFoodType.Snack;

            return food != DragoonDrizzlefishFoodType.None;
        }

        public static int Pack(DragoonDrizzlefishFoodType food, bool secondary = false)
        {
            int packed = (int)food & FoodMask;
            if (secondary)
                packed |= SecondaryFlag;
            return packed;
        }

        public static DragoonDrizzlefishFoodType GetFood(Projectile projectile)
            => (DragoonDrizzlefishFoodType)((int)projectile.ai[0] & FoodMask);

        public static bool IsSecondary(Projectile projectile)
            => (((int)projectile.ai[0] & SecondaryFlag) != 0);

        public static int UseTime(DragoonDrizzlefishFoodType food)
        {
            return food switch
            {
                DragoonDrizzlefishFoodType.Fruit => 9,
                DragoonDrizzlefishFoodType.Alcohol => 14,
                DragoonDrizzlefishFoodType.Fish => 17,
                DragoonDrizzlefishFoodType.Snack => 18,
                DragoonDrizzlefishFoodType.Gel => 20,
                DragoonDrizzlefishFoodType.Feast => 22,
                DragoonDrizzlefishFoodType.Meat => 24,
                DragoonDrizzlefishFoodType.OddMushroom => 27,
                DragoonDrizzlefishFoodType.Superfood => 32,
                _ => 24
            };
        }

        public static float DamageMultiplier(DragoonDrizzlefishFoodType food)
        {
            return food switch
            {
                DragoonDrizzlefishFoodType.Gel => 1f,
                DragoonDrizzlefishFoodType.Meat => 1.12f,
                DragoonDrizzlefishFoodType.Feast => 1.22f,
                DragoonDrizzlefishFoodType.Superfood => 2f,
                _ => 1f
            };
        }

        public static Color FoodColor(DragoonDrizzlefishFoodType food)
        {
            return food switch
            {
                DragoonDrizzlefishFoodType.Gel => new Color(115, 210, 255),
                DragoonDrizzlefishFoodType.Fruit => new Color(115, 255, 150),
                DragoonDrizzlefishFoodType.Meat => new Color(255, 115, 65),
                DragoonDrizzlefishFoodType.Fish => new Color(90, 225, 255),
                DragoonDrizzlefishFoodType.Alcohol => new Color(200, 110, 255),
                DragoonDrizzlefishFoodType.Feast => new Color(255, 205, 90),
                DragoonDrizzlefishFoodType.Snack => new Color(255, 145, 225),
                DragoonDrizzlefishFoodType.Superfood => new Color(255, 230, 70),
                DragoonDrizzlefishFoodType.OddMushroom => new Color(220, 150, 85),
                _ => Color.OrangeRed
            };
        }

        public static void ApplyFishflameStats(Projectile projectile)
        {
            DragoonDrizzlefishFoodType food = GetFood(projectile);
            projectile.damage = Math.Max(1, (int)(projectile.damage * DamageMultiplier(food)));

            if (food == DragoonDrizzlefishFoodType.Meat)
            {
                projectile.scale *= 1.55f;
                projectile.knockBack += 2.25f;
                projectile.width = Math.Max(projectile.width, (int)(projectile.width * 1.35f));
                projectile.height = Math.Max(projectile.height, (int)(projectile.height * 1.35f));
                if (projectile.penetrate > 0)
                    projectile.penetrate += 5;
                projectile.velocity *= 0.94f;
            }
            else if (food == DragoonDrizzlefishFoodType.Feast)
            {
                projectile.scale *= 1.22f;
                projectile.knockBack += 0.8f;
                if (projectile.penetrate > 0)
                    projectile.penetrate += 2;
            }
        }

        public static int SplitCount(DragoonDrizzlefishFoodType food)
        {
            return food switch
            {
                DragoonDrizzlefishFoodType.Meat => 5,
                DragoonDrizzlefishFoodType.Feast => 6,
                _ => 3
            };
        }

        public static float SplitRotation(DragoonDrizzlefishFoodType food)
        {
            float degrees = food switch
            {
                DragoonDrizzlefishFoodType.Meat => 22f,
                DragoonDrizzlefishFoodType.Feast => 34f,
                _ => Main.rand.Next(15, 26)
            };

            return MathHelper.ToRadians(degrees);
        }

        public static int SplitTimer(DragoonDrizzlefishFoodType food)
        {
            return food switch
            {
                DragoonDrizzlefishFoodType.Meat => 54,
                DragoonDrizzlefishFoodType.Feast => 40,
                _ => 45
            };
        }

        public static void ApplyBaseDebuff(Projectile projectile, NPC target, int brimstoneTime, int hellfireTime)
        {
            if (projectile.ai[1] == 1f)
                target.AddBuff(BuffID.OnFire3, hellfireTime);
            else
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), brimstoneTime);
        }

        public static void SpawnFoodDust(Projectile projectile, int count, float scale)
        {
            DragoonDrizzlefishFoodType food = GetFood(projectile);
            if (food == DragoonDrizzlefishFoodType.None || Main.rand.NextBool(3))
                return;

            Color color = FoodColor(food);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(7f, 7f), DustID.RainbowMk2, -projectile.velocity * 0.12f, 0, color, scale);
                dust.noGravity = true;
                dust.velocity += Main.rand.NextVector2Circular(0.8f, 0.8f);
            }
        }

        public static NPC FindTarget(Projectile projectile, float range, bool ignoreTiles = false)
        {
            NPC target = null;
            float bestDistance = range * range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(projectile))
                    continue;

                float distance = Vector2.DistanceSquared(projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                if (!ignoreTiles && !Collision.CanHitLine(projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                bestDistance = distance;
                target = npc;
            }

            return target;
        }

        public static void HomeTowardTarget(Projectile projectile, float range, float turnPower, float maxSpeed, bool ignoreTiles = false)
        {
            NPC target = FindTarget(projectile, range, ignoreTiles);
            if (target is null)
                return;

            float speed = MathHelper.Clamp(projectile.velocity.Length(), 4f, maxSpeed);
            Vector2 desiredVelocity = (target.Center - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, turnPower);
        }

        public static void SpawnImpactDust(Vector2 center, Color color, int count, float speed, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.RainbowMk2, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(speed * 0.35f, speed), 0, color, Main.rand.NextFloat(scale * 0.75f, scale * 1.25f));
                dust.noGravity = true;
            }
        }

        private static string StableName(Item item)
        {
            if (item.ModItem is not null)
                return item.ModItem.Name;

            return item.type > ItemID.None && item.type < ItemID.Count ? ItemID.Search.GetName(item.type) : item.Name;
        }
    }
}
