using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityMod.CalPlayer.Dashes;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash.DashEffects
{
    internal enum BrinyBaronQuickDashDevice
    {
        None,
        OrnateShield,
        DeepDiver,
        AsgardsValor,
        ElysianAegis,
        AsgardianAegis
    }

    internal abstract class BrinyBaronDashPassiveEffect
    {
        private const int SideShurikenInterval = 7;

        public abstract BrinyBaronQuickDashDevice Device { get; }

        public virtual void OnDashStarted(Player player)
        {
            SpawnSlashDash(player);
        }

        public virtual void UpdateWhileDashing(Player player, int dashTimer)
        {
            SpawnDashSpray(player);

            if (dashTimer % SideShurikenInterval == 1)
                SpawnSideShuriken(player, dashTimer);
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
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == slashDashType)
                    return true;
            }

            return false;
        }

        private static void SpawnSideShuriken(Player player, int dashTimer)
        {
            if (Main.myPlayer != player.whoAmI)
                return;

            Vector2 forward = player.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            int sideSign = dashTimer % (SideShurikenInterval * 2) < SideShurikenInterval ? 1 : -1;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2 * sideSign);
            int damage = Math.Max(1, (int)(player.GetWeaponDamage(player.HeldItem) * 0.36f));

            Projectile.NewProjectile(
                player.GetSource_FromThis(),
                player.Center + side * 30f - forward * 10f,
                side * Main.rand.NextFloat(9f, 12f) + forward * Main.rand.NextFloat(2f, 4f),
                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                damage,
                player.GetWeaponKnockback(player.HeldItem) * 0.35f,
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
        }
    }

    internal static class BrinyBaronDashPassiveEffectRegistry
    {
        private static readonly BrinyBaronDashPassiveEffect[] Effects =
        {
            new OrnateShieldPassiveDashEffect(),
            new DeepDiverPassiveDashEffect(),
            new AsgardsValorPassiveDashEffect(),
            new ElysianAegisPassiveDashEffect(),
            new AsgardianAegisPassiveDashEffect()
        };

        public static BrinyBaronQuickDashDevice FromDashID(string dashID)
        {
            if (string.IsNullOrEmpty(dashID))
                return BrinyBaronQuickDashDevice.None;

            if (dashID == OrnateShieldDash.ID)
                return BrinyBaronQuickDashDevice.OrnateShield;
            if (dashID == DeepDiverDash.ID)
                return BrinyBaronQuickDashDevice.DeepDiver;
            if (dashID == AsgardsValorDash.ID)
                return BrinyBaronQuickDashDevice.AsgardsValor;
            if (dashID == ElysianAegisDash.ID)
                return BrinyBaronQuickDashDevice.ElysianAegis;
            if (dashID == AsgardianAegisDash.ID)
                return BrinyBaronQuickDashDevice.AsgardianAegis;

            return BrinyBaronQuickDashDevice.None;
        }

        public static string GetLocalizationKey(BrinyBaronQuickDashDevice device)
        {
            return device switch
            {
                BrinyBaronQuickDashDevice.OrnateShield => "PassiveDevice_OrnateShield",
                BrinyBaronQuickDashDevice.DeepDiver => "PassiveDevice_DeepDiver",
                BrinyBaronQuickDashDevice.AsgardsValor => "PassiveDevice_AsgardsValor",
                BrinyBaronQuickDashDevice.ElysianAegis => "PassiveDevice_ElysianAegis",
                BrinyBaronQuickDashDevice.AsgardianAegis => "PassiveDevice_AsgardianAegis",
                _ => "PassiveDevice_None",
            };
        }

        public static void ApplyDashStarted(Player player, BrinyBaronQuickDashDevice device)
        {
            GetEffect(device)?.OnDashStarted(player);
        }

        public static void ApplyDashUpdate(Player player, BrinyBaronQuickDashDevice device, int dashTimer)
        {
            GetEffect(device)?.UpdateWhileDashing(player, dashTimer);
        }

        private static BrinyBaronDashPassiveEffect GetEffect(BrinyBaronQuickDashDevice device)
        {
            foreach (BrinyBaronDashPassiveEffect effect in Effects)
            {
                if (effect.Device == device)
                    return effect;
            }

            return null;
        }
    }
}
