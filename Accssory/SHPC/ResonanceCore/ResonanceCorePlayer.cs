using CalamityLegendsComeBack.Weapons.SHPC;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ResonanceCore
{
    public class ResonanceCorePlayer : ModPlayer
    {
        public const int ShieldCooldownMax = 15 * 60;

        public bool ResonanceCoreEquipped;
        public int ShieldCooldown;
        public int ShieldHitFlashTimer;

        public bool HoldingSHPC =>
            Player.HeldItem?.ModItem is NewLegendSHPC;

        public bool ShieldReady =>
            ResonanceCoreEquipped &&
            HoldingSHPC &&
            ShieldCooldown <= 0 &&
            !Player.dead;

        public override void ResetEffects()
        {
            ResonanceCoreEquipped = false;
        }

        public override void UpdateDead()
        {
            ResonanceCoreEquipped = false;
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

            if (Player.ownedProjectileCounts[ModContent.ProjectileType<ResonanceCoreShieldVisual>()] <= 0)
            {
                Projectile.NewProjectile(
                    Player.GetSource_Accessory(Player.HeldItem),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<ResonanceCoreShieldVisual>(),
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
