using CalamityLegendsComeBack.Weapons.SHPC;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.S_RSU_PhaseResonanceGuard
{
    public class S_RSU_PhaseResonanceGuardPlayer : ModPlayer
    {
        public const int ShieldCooldownMax = 15 * 60;

        public bool S_RSU_PhaseResonanceGuardEquipped;
        public int ShieldCooldown;
        public int ShieldHitFlashTimer;

        public bool HoldingSHPC =>
            Player.HeldItem?.ModItem is NewLegendSHPC;

        public bool ShieldReady =>
            S_RSU_PhaseResonanceGuardEquipped &&
            HoldingSHPC &&
            ShieldCooldown <= 0 &&
            !Player.dead;

        public override void ResetEffects()
        {
            S_RSU_PhaseResonanceGuardEquipped = false;
        }

        public override void UpdateDead()
        {
            S_RSU_PhaseResonanceGuardEquipped = false;
            ShieldCooldown = 0;
            ShieldHitFlashTimer = 0;
        }

        public override void PostUpdate()
        {
            if (ShieldCooldown > 0)
                ShieldCooldown--;

            if (ShieldHitFlashTimer > 0)
                ShieldHitFlashTimer--;

            if (Main.myPlayer != Player.whoAmI || !ShieldReady)
                return;

            if (Player.ownedProjectileCounts[ModContent.ProjectileType<S_RSU_PhaseResonanceGuardShieldVisual>()] <= 0)
            {
                Projectile.NewProjectile(
                    Player.GetSource_Accessory(Player.HeldItem),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<S_RSU_PhaseResonanceGuardShieldVisual>(),
                    0,
                    0f,
                    Player.whoAmI);
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!ShieldReady)
                return;

            modifiers.FinalDamage *= 0.5f;
            ShieldCooldown = ShieldCooldownMax;
            ShieldHitFlashTimer = 18;
        }
    }
}
