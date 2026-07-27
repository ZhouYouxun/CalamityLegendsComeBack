using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash.DashEffects
{
    internal static class BrinyBaronDashPassiveEffect
    {
        private const int SideShurikenInterval = 7;

        public static void ApplyDashStarted(Player player)
        {
            SpawnSlashDash(player);
        }

        public static void ApplyDashUpdate(Player player, int dashTimer)
        {
            SpawnDashSpray(player);

            if (dashTimer % SideShurikenInterval == 0)
                SpawnSideShurikenPair(player);
        }

        private static void SpawnSlashDash(Player player)
        {
            if (Main.myPlayer != player.whoAmI || HasActiveSlashDash(player))
                return;

            int direction = Math.Sign(player.velocity.X);
            if (direction == 0)
                direction = player.direction == 0 ? 1 : player.direction;

            int damage = Math.Max(1, (int)(player.GetWeaponDamage(player.HeldItem) * 0.95f));
            Projectile.NewProjectile(
                player.GetSource_FromThis(),
                player.Center,
                Vector2.UnitX * direction,
                ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>(),
                damage,
                player.GetWeaponKnockback(player.HeldItem),
                player.whoAmI,
                0f,
                direction);
        }

        private static bool HasActiveSlashDash(Player player)
        {
            int slashDashType = ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == slashDashType)
                    return true;
            }

            return false;
        }

        private static void SpawnSideShurikenPair(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return;

            Vector2 forward = player.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            int damage = Math.Max(1, (int)(player.GetWeaponDamage(player.HeldItem) * 0.36f));
            float knockback = player.GetWeaponKnockback(player.HeldItem) * 0.35f;

            SpawnOneSideShuriken(player, side, damage, knockback);
            SpawnOneSideShuriken(player, -side, damage, knockback);
        }

        private static void SpawnOneSideShuriken(Player player, Vector2 side, int damage, float knockback)
        {
            Projectile.NewProjectile(
                player.GetSource_FromThis(),
                player.Center + side * 32f,
                side * 12.5f,
                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                damage,
                knockback,
                player.whoAmI);
        }

        private static void SpawnDashSpray(Player player)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            Vector2 forward = player.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 velocity = -forward.RotatedByRandom(0.55f) * Main.rand.NextFloat(2f, 5.5f);
            Dust dust = Dust.NewDustPerfect(
                player.Center - forward * Main.rand.NextFloat(16f, 42f) + Main.rand.NextVector2Circular(10f, 12f),
                Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                velocity,
                100,
                Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan,
                Main.rand.NextFloat(0.9f, 1.35f));
            dust.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GenericBubbleParticle(
                    player.Center - forward * Main.rand.NextFloat(18f, 48f) + Main.rand.NextVector2Circular(12f, 14f),
                    -forward * Main.rand.NextFloat(0.5f, 1.4f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    Main.rand.NextFloat(0.72f, 1.15f),
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    Main.rand.Next(28, 48)));
            }

            // Continuous WaterFoamParticle trail: half the old 1-in-3 emission rate.
            if (Main.rand.NextBool(6))
            {
                GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                    player.Center - forward * Main.rand.NextFloat(14f, 38f) + Main.rand.NextVector2Circular(10f, 12f),
                    velocity * Main.rand.NextFloat(0.35f, 0.72f),
                    Main.rand.Next(20, 34),
                    Main.rand.NextFloat(0.62f, 0.98f),
                    Color.Lerp(new Color(145, 225, 255), Color.White, Main.rand.NextFloat(0.18f, 0.48f))));
            }
        }
    }
}
