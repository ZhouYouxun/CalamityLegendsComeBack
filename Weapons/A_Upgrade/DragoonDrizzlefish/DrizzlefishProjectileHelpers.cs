using System;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    internal static class DrizzlefishProjectileHelpers
    {
        public static void ApplyInitialStats(Projectile projectile)
        {
            DragoonDrizzlefishMealType meal = DragoonDrizzlefishMeals.GetMeal(projectile);
            if (meal == DragoonDrizzlefishMealType.None)
                return;

            float damageMultiplier = DragoonDrizzlefishMeals.DamageMultiplier(meal);
            if (DragoonDrizzlefishMeals.IsOverfed(projectile))
                damageMultiplier *= 0.94f;

            projectile.damage = Math.Max(1, (int)(projectile.damage * damageMultiplier));
            projectile.scale *= DragoonDrizzlefishMeals.ScaleMultiplier(meal);

            if (meal is DragoonDrizzlefishMealType.Sweet or DragoonDrizzlefishMealType.BlasphemousDonut)
                projectile.velocity *= 1.08f;
            else if (meal is DragoonDrizzlefishMealType.Meat or DragoonDrizzlefishMealType.DeliciousMeat)
            {
                projectile.velocity *= 0.94f;
                projectile.knockBack += 1.2f;
            }
        }

        public static void ApplyMealMotion(Projectile projectile, int time, float strength = 1f)
        {
            DragoonDrizzlefishMealType meal = DragoonDrizzlefishMeals.GetMeal(projectile);
            bool overfed = DragoonDrizzlefishMeals.IsOverfed(projectile);

            switch (meal)
            {
                case DragoonDrizzlefishMealType.Seafood:
                    HomeTowardTarget(projectile, 520f, 0.025f * strength, 10.5f);
                    break;

                case DragoonDrizzlefishMealType.HadalStew:
                    HomeTowardTarget(projectile, 700f, 0.038f * strength, 11.5f);
                    break;

                case DragoonDrizzlefishMealType.Drink:
                    projectile.velocity = projectile.velocity.RotatedBy((float)Math.Sin((time + projectile.identity) * 0.16f) * 0.036f * strength);
                    break;

                case DragoonDrizzlefishMealType.Weird:
                case DragoonDrizzlefishMealType.OddMushroom:
                    projectile.velocity = projectile.velocity.RotatedBy((float)Math.Sin((time + projectile.identity) * 0.23f) * 0.055f * strength);
                    projectile.velocity *= 1f + (float)Math.Sin(time * 0.18f) * 0.004f;
                    break;

                case DragoonDrizzlefishMealType.Plant:
                    projectile.velocity *= 0.995f;
                    break;

                case DragoonDrizzlefishMealType.Sweet:
                case DragoonDrizzlefishMealType.BlasphemousDonut:
                    projectile.velocity *= 1.002f;
                    break;
            }

            if (overfed)
                projectile.velocity = projectile.velocity.RotatedBy((float)Math.Sin((time + projectile.identity) * 0.31f) * 0.045f);
        }

        public static int SplitCount(DragoonDrizzlefishMealType meal, bool overfed)
        {
            int count = meal switch
            {
                DragoonDrizzlefishMealType.Sweet => 4,
                DragoonDrizzlefishMealType.BlasphemousDonut => 5,
                DragoonDrizzlefishMealType.Spicy => 4,
                DragoonDrizzlefishMealType.LavaChickenBroth => 5,
                DragoonDrizzlefishMealType.Drink => 5,
                DragoonDrizzlefishMealType.Weird or
                DragoonDrizzlefishMealType.OddMushroom => Main.rand.Next(3, 7),
                _ => 3
            };

            return overfed ? count + 1 : count;
        }

        public static float SplitRotation(DragoonDrizzlefishMealType meal, bool overfed)
        {
            float degrees = meal switch
            {
                DragoonDrizzlefishMealType.Staple => 18f,
                DragoonDrizzlefishMealType.Meat or
                DragoonDrizzlefishMealType.DeliciousMeat => 14f,
                DragoonDrizzlefishMealType.Drink => 42f,
                DragoonDrizzlefishMealType.Weird or
                DragoonDrizzlefishMealType.OddMushroom => 55f,
                _ => Main.rand.Next(15, 26)
            };

            if (overfed)
                degrees += 16f;

            return MathHelper.ToRadians(degrees);
        }

        public static int SplitTimer(DragoonDrizzlefishMealType meal, bool overfed)
        {
            int timer = meal switch
            {
                DragoonDrizzlefishMealType.Sweet or
                DragoonDrizzlefishMealType.BlasphemousDonut => 34,
                DragoonDrizzlefishMealType.Staple => 38,
                DragoonDrizzlefishMealType.Meat or
                DragoonDrizzlefishMealType.DeliciousMeat => 52,
                DragoonDrizzlefishMealType.Drink => 32,
                _ => 45
            };

            return overfed ? Math.Max(25, timer - 8) : timer;
        }

        public static void ApplyBaseDebuff(Projectile projectile, NPC target, int brimstoneTime, int hellfireTime)
        {
            if (projectile.ai[1] == 1f)
                target.AddBuff(BuffID.OnFire3, hellfireTime);
            else
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), brimstoneTime);
        }

        public static void ApplyMealOnHit(Projectile projectile, NPC target, int damageDone)
        {
            DragoonDrizzlefishMealType meal = DragoonDrizzlefishMeals.GetMeal(projectile);

            switch (meal)
            {
                case DragoonDrizzlefishMealType.Plant:
                    target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 90);
                    BurstDust(projectile.Center, 74, 10, 1.1f);
                    break;

                case DragoonDrizzlefishMealType.Spicy:
                case DragoonDrizzlefishMealType.LavaChickenBroth:
                    target.AddBuff(BuffID.OnFire3, meal == DragoonDrizzlefishMealType.LavaChickenBroth ? 180 : 110);
                    BurstDust(projectile.Center, 6, 14, 1.35f);
                    break;

                case DragoonDrizzlefishMealType.Seafood:
                case DragoonDrizzlefishMealType.HadalStew:
                    SpawnExtraSplits(projectile, 2, 0.42f, MathHelper.ToRadians(28f));
                    break;

                case DragoonDrizzlefishMealType.Sweet:
                    if (Main.rand.NextBool(3))
                        SpawnExtraFireball(projectile, 0.45f, MathHelper.ToRadians(20f));
                    break;

                case DragoonDrizzlefishMealType.BlasphemousDonut:
                    if (Main.rand.NextBool(2))
                        SpawnExtraFireball(projectile, 0.5f, MathHelper.ToRadians(32f));
                    break;

                case DragoonDrizzlefishMealType.Drink:
                    if (Main.rand.NextBool(3))
                        SpawnExtraSplits(projectile, 1, 0.5f, MathHelper.ToRadians(75f));
                    break;

                case DragoonDrizzlefishMealType.Weird:
                case DragoonDrizzlefishMealType.OddMushroom:
                    if (Main.rand.NextBool(2))
                        SpawnExtraSplits(projectile, Main.rand.Next(1, 4), 0.38f, MathHelper.ToRadians(120f));
                    break;
            }
        }

        public static void MaybeSpawnSweetChild(Projectile projectile, int time)
        {
            DragoonDrizzlefishMealType meal = DragoonDrizzlefishMeals.GetMeal(projectile);
            if (DragoonDrizzlefishMeals.IsSecondary(projectile) ||
                projectile.owner != Main.myPlayer ||
                time != 12 ||
                (meal != DragoonDrizzlefishMealType.Sweet && meal != DragoonDrizzlefishMealType.BlasphemousDonut))
            {
                return;
            }

            int count = meal == DragoonDrizzlefishMealType.BlasphemousDonut ? 2 : 1;
            for (int i = 0; i < count; i++)
                SpawnExtraFireball(projectile, 0.42f, MathHelper.ToRadians(18f + i * 12f));
        }

        public static void SpawnMealDust(Projectile projectile, int count, float scale)
        {
            DragoonDrizzlefishMealType meal = DragoonDrizzlefishMeals.GetMeal(projectile);
            if (meal == DragoonDrizzlefishMealType.None || Main.rand.NextBool(3))
                return;

            Color color = DragoonDrizzlefishMeals.MealColor(meal);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(7f, 7f), DustID.RainbowMk2, -projectile.velocity * 0.2f, 0, color, scale);
                dust.noGravity = true;
                dust.velocity += Main.rand.NextVector2Circular(0.8f, 0.8f);
            }
        }

        private static void SpawnExtraFireball(Projectile projectile, float damageMultiplier, float rotation)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            int packed = DragoonDrizzlefishMeals.Pack(
                DragoonDrizzlefishMeals.GetMeal(projectile),
                DragoonDrizzlefishMeals.IsOverfed(projectile),
                true);

            Vector2 velocity = projectile.velocity.RotatedByRandom(rotation) * Main.rand.NextFloat(0.82f, 1.08f);
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                velocity,
                ModContent.ProjectileType<FoodDrizzlefishFireball>(),
                Math.Max(1, (int)(projectile.damage * damageMultiplier)),
                projectile.knockBack * 0.5f,
                projectile.owner,
                packed,
                projectile.ai[1]);
        }

        private static void SpawnExtraSplits(Projectile projectile, int count, float damageMultiplier, float rotation)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            int packed = DragoonDrizzlefishMeals.Pack(
                DragoonDrizzlefishMeals.GetMeal(projectile),
                DragoonDrizzlefishMeals.IsOverfed(projectile),
                true);

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(rotation) * Main.rand.NextFloat(4.5f, 8.5f);
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<FoodDrizzlefishFireSplit>(),
                    Math.Max(1, (int)(projectile.damage * damageMultiplier)),
                    projectile.knockBack * 0.5f,
                    projectile.owner,
                    packed,
                    projectile.ai[1]);
            }
        }

        private static void HomeTowardTarget(Projectile projectile, float range, float turnPower, float maxSpeed)
        {
            NPC target = null;
            float bestDistance = range * range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(projectile))
                    continue;

                float distance = Vector2.DistanceSquared(projectile.Center, npc.Center);
                if (distance >= bestDistance || !Collision.CanHitLine(projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                bestDistance = distance;
                target = npc;
            }

            if (target is null)
                return;

            float speed = MathHelper.Clamp(projectile.velocity.Length(), 4f, maxSpeed);
            Vector2 desiredVelocity = (target.Center - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, turnPower);
        }

        private static void BurstDust(Vector2 center, int dustType, int count, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, dustType, Main.rand.NextVector2Circular(3.5f, 3.5f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(scale * 0.65f, scale * 1.25f);
            }
        }
    }
}
