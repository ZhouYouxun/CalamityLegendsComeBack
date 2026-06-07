using CalamityMod;
using CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash.DashEffects;
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
        private const int DamageBuffFrames = 45;
        private const float DashVelocity = 20.5f;

        private int leftTapTimer;
        private int rightTapTimer;
        private int customDashTimer;
        private int customDashCooldown;
        private int customDashDirection;
        private int damageBuffTimer;
        private bool wasDashingLastFrame;

        public bool DamageBuffActive => damageBuffTimer > 0;

        public override void UpdateDead()
        {
            leftTapTimer = 0;
            rightTapTimer = 0;
            customDashTimer = 0;
            customDashCooldown = 0;
            damageBuffTimer = 0;
            wasDashingLastFrame = false;
        }

        public override void PostUpdate()
        {
            if (damageBuffTimer > 0)
                damageBuffTimer--;

            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_ABreak))
            {
                leftTapTimer = 0;
                rightTapTimer = 0;
                customDashTimer = 0;
                customDashCooldown = 0;
                wasDashingLastFrame = false;
                return;
            }

            if (customDashCooldown > 0)
                customDashCooldown--;

            if (Player.dashDelay > 0)
                Player.dashDelay = System.Math.Max(0, Player.dashDelay - 1);

            bool dashingNow = Player.dashDelay < 0 && Player.velocity.Length() > 4f;
            if (dashingNow && !wasDashingLastFrame)
                TriggerDamageBuff();

            wasDashingLastFrame = dashingNow;
            HandleCustomDashInput();
        }

        public override void PreUpdateMovement()
        {
            if (customDashTimer <= 0)
                return;

            customDashTimer--;
            Player.velocity.X = customDashDirection * DashVelocity;
            Player.maxFallSpeed = System.Math.Max(Player.maxFallSpeed, 18f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(12f, 20f),
                    DustID.GrassBlades,
                    new Vector2(-customDashDirection * Main.rand.NextFloat(1.8f, 4.2f), Main.rand.NextFloat(-0.8f, 0.8f)),
                    110,
                    new Color(142, 255, 118),
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            if (DamageBuffActive && item.type == ModContent.ItemType<NewLegendBlossomFlux>())
                damage *= 1.2f;
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
            TriggerDamageBuff();

            if (Main.myPlayer == Player.whoAmI)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, Pitch = 0.32f }, Player.Center);
        }

        private void TriggerDamageBuff()
        {
            damageBuffTimer = DamageBuffFrames;
        }

        private bool HasExternalDash()
        {
            string dashID = !string.IsNullOrEmpty(Player.Calamity().LastUsedDashID)
                ? Player.Calamity().LastUsedDashID
                : Player.Calamity().DashID;

            return Player.dashType != 0 ||
                BrinyBaronDashPassiveEffectRegistry.FromDashID(dashID) != BrinyBaronQuickDashDevice.None;
        }
    }
}
