using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    public sealed class DragoonDrizzlefishPlayer : ModPlayer
    {
        private const int OverfedThreshold = 7;
        private const int MaxFullness = 10;

        internal DragoonDrizzlefishMealType CurrentMeal;
        internal int MealTime;
        internal int MealShots;
        internal int Fullness;
        internal int ShotCounter;
        private int fullnessDecayTimer;

        internal bool HasMeal => CurrentMeal != DragoonDrizzlefishMealType.None && MealTime > 0 && MealShots > 0;
        internal bool Overfed => Fullness >= OverfedThreshold;
        internal DragoonDrizzlefishMealType ActiveMeal => HasMeal ? CurrentMeal : DragoonDrizzlefishMealType.None;

        public override void PostUpdate()
        {
            if (MealTime > 0)
                MealTime--;

            if (HasMeal && (MealTime <= 0 || MealShots <= 0))
            {
                CurrentMeal = DragoonDrizzlefishMealType.None;
                MealTime = 0;
                MealShots = 0;
            }

            if (Fullness <= 0)
            {
                fullnessDecayTimer = 0;
                return;
            }

            fullnessDecayTimer++;
            if (fullnessDecayTimer >= 90)
            {
                fullnessDecayTimer = 0;
                Fullness--;
            }
        }

        internal void Feed(Item food)
        {
            if (!DragoonDrizzlefishMeals.TryClassify(food, out DragoonDrizzlefishMealType meal))
                return;

            CurrentMeal = meal;
            MealTime = DragoonDrizzlefishMeals.MealDuration(meal);
            MealShots = DragoonDrizzlefishMeals.MealShots(meal);
            Fullness = Math.Min(MaxFullness, Fullness + DragoonDrizzlefishMeals.FullnessGain(meal));
            fullnessDecayTimer = 0;

            if (meal == DragoonDrizzlefishMealType.TheSandwich)
                Fullness = MaxFullness;

            CombatText.NewText(Player.Hitbox, DragoonDrizzlefishMeals.MealColor(meal), food.Name);
            SoundEngine.PlaySound(SoundID.Item2 with { Pitch = 0.25f, Volume = 0.9f }, Player.Center);
            SoundEngine.PlaySound(SoundID.Item20 with { Pitch = -0.25f, Volume = 0.65f }, Player.Center);

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextBool() ? 90 : 183,
                    Main.rand.NextVector2Circular(2.4f, 2.4f) - Vector2.UnitY * Main.rand.NextFloat(0.5f, 1.8f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.8f, 1.35f);
            }
        }

        internal void RegisterShot()
        {
            if (HasMeal)
                MealShots--;
        }

        internal int PackedMealForProjectile(bool secondary = false)
        {
            DragoonDrizzlefishMealType meal = ActiveMeal;
            if (meal == DragoonDrizzlefishMealType.TheSandwich)
                meal = Main.rand.Next(7) switch
                {
                    0 => DragoonDrizzlefishMealType.Meat,
                    1 => DragoonDrizzlefishMealType.Seafood,
                    2 => DragoonDrizzlefishMealType.Plant,
                    3 => DragoonDrizzlefishMealType.Sweet,
                    4 => DragoonDrizzlefishMealType.Spicy,
                    5 => DragoonDrizzlefishMealType.Drink,
                    _ => DragoonDrizzlefishMealType.Weird
                };

            return DragoonDrizzlefishMeals.Pack(meal, Overfed, secondary);
        }
    }
}
