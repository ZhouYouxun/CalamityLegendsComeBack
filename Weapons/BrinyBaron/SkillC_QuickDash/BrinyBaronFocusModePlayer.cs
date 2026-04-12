using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash
{
    internal class BrinyBaronFocusModePlayer : ModPlayer
    {
        private const int FocusActivationTime = 60 * 5;
        private const int FocusBarDropFrames = 20;
        private const int FocusBarFadeInFrames = 18;
        private const int FocusBarFadeOutFrames = 24;

        private int focusIdleTimer;
        private float focusBarVisualProgress;
        private float focusBarOpacity;
        private bool focusActiveLastFrame;

        public bool HoldingBrinyBaron { get; private set; }
        public bool IsFocusModeActive { get; private set; }
        public float FocusVisualIntensity { get; private set; }
        public float FocusChargeBarProgress => focusBarVisualProgress;
        public float FocusChargeBarOpacity => focusBarOpacity;

        public override void ResetEffects()
        {
            HoldingBrinyBaron = false;
        }

        public override void UpdateDead()
        {
            focusIdleTimer = 0;
            IsFocusModeActive = false;
            FocusVisualIntensity = 0f;
            focusBarVisualProgress = 0f;
            focusBarOpacity = 0f;
            focusActiveLastFrame = false;
        }

        public override void PostUpdate()
        {
            bool preservingFocusViaSlashDash = HasActiveSlashDashProjectile();

            bool isIdleWithBrinyBaron =
                HoldingBrinyBaron &&
                !Player.dead &&
                !Player.CCed &&
                !Player.noItems &&
                (!Player.ItemAnimationActive || preservingFocusViaSlashDash) &&
                !Main.mouseLeft &&
                !Main.mouseRight;

            if (isIdleWithBrinyBaron)
                focusIdleTimer = System.Math.Min(focusIdleTimer + 1, FocusActivationTime);
            else
                focusIdleTimer = 0;

            IsFocusModeActive = isIdleWithBrinyBaron && focusIdleTimer >= FocusActivationTime;

            float targetIntensity = IsFocusModeActive ? 1f : 0f;
            FocusVisualIntensity = MathHelper.Lerp(FocusVisualIntensity, targetIntensity, IsFocusModeActive ? 0.12f : 0.22f);
            if (FocusVisualIntensity < 0.01f)
                FocusVisualIntensity = 0f;

            UpdateFocusChargeBar(isIdleWithBrinyBaron);
            HandleFocusStateSounds();
        }

        public void SetHoldingBrinyBaron()
        {
            HoldingBrinyBaron = true;
        }

        private bool HasActiveSlashDashProjectile()
        {
            int slashDashType = ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == slashDashType)
                    return true;
            }

            return false;
        }

        private void UpdateFocusChargeBar(bool isIdleWithBrinyBaron)
        {
            if (isIdleWithBrinyBaron)
            {
                float targetProgress = IsFocusModeActive ? 1f : focusIdleTimer / (float)FocusActivationTime;
                focusBarVisualProgress = targetProgress;
                focusBarOpacity = System.Math.Min(1f, focusBarOpacity + 1f / FocusBarFadeInFrames);
                return;
            }

            if (focusBarVisualProgress > 0f)
                focusBarVisualProgress = System.Math.Max(0f, focusBarVisualProgress - 1f / FocusBarDropFrames);
            else if (focusBarOpacity > 0f)
                focusBarOpacity = System.Math.Max(0f, focusBarOpacity - 1f / FocusBarFadeOutFrames);
        }

        private void HandleFocusStateSounds()
        {
            if (Main.myPlayer != Player.whoAmI)
            {
                focusActiveLastFrame = IsFocusModeActive;
                return;
            }

            if (!focusActiveLastFrame && IsFocusModeActive)
            {
                SoundEngine.PlaySound(SoundID.Item29 with
                {
                    Volume = 0.8f,
                    Pitch = 0.12f
                }, Player.Center);
            }
            else if (focusActiveLastFrame && !IsFocusModeActive)
            {
                SoundEngine.PlaySound(SoundID.Item4 with
                {
                    Volume = 0.7f,
                    Pitch = -0.2f
                }, Player.Center);
            }

            focusActiveLastFrame = IsFocusModeActive;
        }
    }
}
