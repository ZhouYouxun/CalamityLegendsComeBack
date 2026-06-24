using CalamityMod;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.Pa5
{
    internal sealed class BFPa5BreakthroughPlayer : ModPlayer
    {
        private const int DashInputWindow = 15;
        private const int DashDuration = 14;
        private const int DashCooldown = 50;
        private const float DashVelocity = 20.5f;

        private int leftTapTimer;
        private int rightTapTimer;
        private int customDashTimer;
        private int customDashCooldown;
        private int customDashDirection;
        public bool IsCustomDashing => customDashTimer > 0;

        public override void UpdateDead()
        {
            leftTapTimer = 0;
            rightTapTimer = 0;
            customDashTimer = 0;
            customDashCooldown = 0;
        }

        public override void PostUpdate()
        {
            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_ABreak))
            {
                leftTapTimer = 0;
                rightTapTimer = 0;
                customDashTimer = 0;
                customDashCooldown = 0;
                return;
            }

            if (customDashCooldown > 0)
                customDashCooldown--;

            if (Player.dashDelay > 0)
                Player.dashDelay = System.Math.Max(0, Player.dashDelay - 1);

            HandleCustomDashInput();
        }

        public override void PreUpdateMovement()
        {
            if (customDashTimer <= 0)
                return;

            customDashTimer--;
            Player.velocity.X = customDashDirection * DashVelocity;
            Player.maxFallSpeed = System.Math.Max(Player.maxFallSpeed, 18f);
            Player.immuneNoBlink = true;
            Player.GiveUniversalIFrames(2, false);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(12f, 20f),
                    DustID.GrassBlades,
                    new Vector2(-customDashDirection * Main.rand.NextFloat(1.8f, 4.2f), Main.rand.NextFloat(-0.8f, 0.8f)),
                    110,
                    Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), Color.White, 0.22f),
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        private void HandleCustomDashInput()
        {
            if (Player.mount.Active || Player.CCed || Player.noItems || HasExternalDash())
                return;

            if (leftTapTimer > 0)
                leftTapTimer--;

            if (rightTapTimer > 0)
                rightTapTimer--;

            if (Player.controlLeft && Player.releaseLeft)
            {
                if (leftTapTimer > 0)
                    StartCustomDash(-1);
                else
                    leftTapTimer = DashInputWindow;
            }

            if (Player.controlRight && Player.releaseRight)
            {
                if (rightTapTimer > 0)
                    StartCustomDash(1);
                else
                    rightTapTimer = DashInputWindow;
            }
        }

        private void StartCustomDash(int direction)
        {
            if (customDashCooldown > 0)
                return;

            customDashDirection = direction;
            customDashTimer = DashDuration;
            customDashCooldown = DashCooldown;
            Player.ChangeDir(direction);
            Player.velocity.X = direction * DashVelocity;
            Player.velocity.Y *= 0.82f;
            Player.immuneNoBlink = true;
            Player.GiveUniversalIFrames(2, false);
            SpawnDashHitbox();

            if (Main.myPlayer == Player.whoAmI)
                SoundEngine.PlaySound(BlossomFluxSounds.Pa5BreakthroughSound, Player.Center);
        }

        private void SpawnDashHitbox()
        {
            if (Main.myPlayer != Player.whoAmI)
                return;

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BFPa5BreakthroughDashHitbox>(),
                System.Math.Max(1, Player.GetWeaponDamage(Player.HeldItem)),
                Player.GetWeaponKnockback(Player.HeldItem),
                Player.whoAmI,
                customDashDirection);
        }

        private bool HasExternalDash()
        {
            return Player.dashType != 0 ||
                !string.IsNullOrEmpty(Player.Calamity().DashID);
        }
    }
}
