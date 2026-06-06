using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityMod.CalPlayer.Dashes;
using CalamityMod.Particles;
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

    internal interface IBrinyBaronDashPassiveEffect
    {
        BrinyBaronQuickDashDevice Device { get; }
        void OnDashStarted(Player player);
        void UpdateWhileDashing(Player player, int dashTimer);
    }

    internal abstract class BrinyBaronDashPassiveEffect : IBrinyBaronDashPassiveEffect
    {
        private const int SideShurikenInterval = 7;

        public abstract BrinyBaronQuickDashDevice Device { get; }

        public void OnDashStarted(Player player)
        {
            OnSpecialDashStarted(player);
        }

        public void UpdateWhileDashing(Player player, int dashTimer)
        {
            SpawnDashSpray(player);

            if (dashTimer % SideShurikenInterval == 0)
                SpawnSideShurikenPair(player);

            OnSpecialDashUpdate(player, dashTimer);
        }

        protected virtual void OnSpecialDashStarted(Player player)
        {
        }

        protected virtual void OnSpecialDashUpdate(Player player, int dashTimer)
        {
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

            if (Main.rand.NextBool(3))
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
