using System;
using CalamityMod.NPCs.NormalNPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    public sealed class DragoonDrizzlefishPlayer : ModPlayer
    {
        private static readonly string[] FeedFaces =
        {
            ":)", "(^_^)", "(>w<)", "(*'▽'*)", "(っ˘ڡ˘ς)"
        };

        private static readonly string[] HungryFaces =
        {
            ":(", "(:<", "(>_<)", "(；へ：)", "feed me..."
        };

        private static readonly string[] AttackFaces =
        {
            ":D", ">:3", "(ﾉ◕ヮ◕)ﾉ", "pew!", "nom!"
        };

        internal DragoonDrizzlefishFoodType CurrentFood;
        internal int ShotsRemaining;
        internal int ShotCounter;
        private int cuteAttackCountdown;
        private int hungryCooldown;

        internal bool HasFood => CurrentFood != DragoonDrizzlefishFoodType.None && ShotsRemaining > 0;
        internal DragoonDrizzlefishFoodType ActiveFood => HasFood ? CurrentFood : DragoonDrizzlefishFoodType.None;

        public override void Initialize()
        {
            ResetCuteCountdown();
        }

        public override void ResetEffects()
        {
            if (hungryCooldown > 0)
                hungryCooldown--;
        }

        internal void Feed(Item food)
        {
            if (!DragoonDrizzlefishFoods.TryClassify(food, out DragoonDrizzlefishFoodType meal))
                return;

            CurrentFood = meal;
            ShotsRemaining = DragoonDrizzlefishFoods.MagazineSize;
            ShotCounter = 0;
            ResetCuteCountdown();

            Color color = DragoonDrizzlefishFoods.FoodColor(meal);
            CuteText(FaceFrom(FeedFaces), color);
            PlayCuteSound(0.25f, 0.9f);
            SoundEngine.PlaySound(SoundID.Item2 with { Pitch = 0.28f, Volume = 0.9f }, Player.Center);
            SpawnHearts(12);

            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextBool() ? DustID.OrangeTorch : DustID.RainbowMk2,
                    Main.rand.NextVector2Circular(2.4f, 2.4f) - Vector2.UnitY * Main.rand.NextFloat(0.7f, 2.2f),
                    0,
                    color,
                    Main.rand.NextFloat(0.8f, 1.35f));
                dust.noGravity = true;
            }
        }

        internal void RegisterShot()
        {
            if (!HasFood)
                return;

            ShotsRemaining--;
            if (ShotsRemaining <= 0)
            {
                CurrentFood = DragoonDrizzlefishFoodType.None;
                ShotsRemaining = 0;
                ShotCounter = 0;
                HungryFeedback(force: true);
            }
        }

        internal void MaybeAttackCuteFeedback()
        {
            if (!HasFood)
                return;

            cuteAttackCountdown--;
            if (cuteAttackCountdown > 0)
                return;

            CuteText(FaceFrom(AttackFaces), DragoonDrizzlefishFoods.FoodColor(ActiveFood));
            if (Main.rand.NextBool(2))
                PlayCuteSound(Main.rand.NextFloat(-0.1f, 0.35f), 0.58f);
            ResetCuteCountdown();
        }

        internal void HungryFeedback(bool force = false)
        {
            if (!force && hungryCooldown > 0)
                return;

            hungryCooldown = 45;
            CuteText(FaceFrom(HungryFaces), new Color(255, 115, 85));
            PlayCuteSound(-0.35f, 0.68f);
        }

        internal int PackedFoodForProjectile(bool secondary = false)
            => DragoonDrizzlefishFoods.Pack(ActiveFood, secondary);

        private void ResetCuteCountdown()
        {
            cuteAttackCountdown = Main.rand.Next(3, 11);
        }

        private void SpawnHearts(int count)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit();
                velocity.X *= 0.66f;
                velocity *= Main.rand.NextFloat(1f, 2f);
                int heart = Gore.NewGore(Player.GetSource_FromThis(), Player.Center + Main.rand.NextVector2Circular(14f, 14f), velocity, 331, Main.rand.NextFloat(0.2f, 1.2f));
                Main.gore[heart].sticky = false;
                Main.gore[heart].velocity *= 0.4f;
                Main.gore[heart].velocity.Y -= 0.7f;
            }
        }

        private void PlayCuteSound(float pitch, float volume)
        {
            SoundEngine.PlaySound(Sunskater.DeathSound with { Pitch = pitch, Volume = volume, MaxInstances = 2 }, Player.Center);
        }

        private void CuteText(string text, Color color)
        {
            CombatText.NewText(Player.Hitbox, color, text);
        }

        private static string FaceFrom(string[] faces)
            => faces[Main.rand.Next(faces.Length)];
    }
}
