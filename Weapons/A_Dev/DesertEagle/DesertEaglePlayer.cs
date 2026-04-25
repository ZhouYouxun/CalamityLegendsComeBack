using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEaglePlayer : ModPlayer
    {
        public const int SpinChargeMax = 60 * 5 / 2;
        public const int RightHoldThresholdFrames = 9;
        private const int SilverVolleyTarget = 3;
        private const int SilverVolleyPause = 18;
        private const int BarFadeInFrames = 15;
        private const int BarFadeOutFrames = 14;
        private const int BarDropFrames = 18;

        private int silverVolleyCounter;
        private int volleyPauseTimer;
        private int rightPressFrames;
        private float chargeBarProgress;
        private float chargeBarOpacity;
        private bool chargeReadyLastFrame;
        private bool trackingRightPress;
        private ulong lastRightClickFrame;

        public bool HoldingDesertEagle { get; private set; }
        public bool PendingLifeRound { get; private set; }
        public float ChargeBarProgress => chargeBarProgress;
        public float ChargeBarOpacity => chargeBarOpacity;
        public int RightPressFrames => rightPressFrames;
        public bool TrackingRightPress => trackingRightPress;
        public bool LongHoldReachedThisFrame { get; private set; }
        public bool LongHoldReleasedThisFrame { get; private set; }
        public bool LongHoldActive => trackingRightPress && rightPressFrames > RightHoldThresholdFrames;

        public override void ResetEffects()
        {
            HoldingDesertEagle = false;
        }

        public override void UpdateDead()
        {
            silverVolleyCounter = 0;
            volleyPauseTimer = 0;
            PendingLifeRound = false;
            chargeBarProgress = 0f;
            chargeBarOpacity = 0f;
            chargeReadyLastFrame = false;
            ResetRightClickState();
        }

        public override void PostUpdate()
        {
            if (volleyPauseTimer > 0)
                volleyPauseTimer--;

            if (!HoldingDesertEagle && chargeBarOpacity > 0f)
            {
                chargeBarProgress = Math.Max(0f, chargeBarProgress - 1f / BarDropFrames);
                chargeBarOpacity = Math.Max(0f, chargeBarOpacity - 1f / BarFadeOutFrames);
                chargeReadyLastFrame = false;
            }

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (Player.HeldItem.type != ModContent.ItemType<DesertEagle>() &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<DesertEagleHoldout>()] <= 0)
            {
                ResetRightClickState();
            }
        }

        public void SetHoldingDesertEagle()
        {
            HoldingDesertEagle = true;
        }

        public bool CanUsePrimaryFire() => volleyPauseTimer <= 0;

        public void RegisterSilverVolley()
        {
            if (PendingLifeRound)
                return;

            silverVolleyCounter++;
            if (silverVolleyCounter < SilverVolleyTarget)
                return;

            silverVolleyCounter = 0;
            PendingLifeRound = true;
            volleyPauseTimer = SilverVolleyPause;
        }

        public void ConsumeLifeRound()
        {
            PendingLifeRound = false;
        }

        public void UpdateChargeBar(bool active, float progress)
        {
            bool ready = active && progress >= 1f;

            if (active)
            {
                chargeBarProgress = MathHelper.Clamp(progress, 0f, 1f);
                chargeBarOpacity = Math.Min(1f, chargeBarOpacity + 1f / BarFadeInFrames);
            }
            else
            {
                chargeBarProgress = Math.Max(0f, chargeBarProgress - 1f / BarDropFrames);
                chargeBarOpacity = Math.Max(0f, chargeBarOpacity - 1f / BarFadeOutFrames);
            }

            if (Main.myPlayer == Player.whoAmI && ready && !chargeReadyLastFrame)
            {
                SoundEngine.PlaySound(SoundID.Item29 with
                {
                    Volume = 0.95f,
                    Pitch = -0.22f
                }, Player.Center);
            }

            chargeReadyLastFrame = ready;
        }

        public void ProcessRightClickState()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (lastRightClickFrame == Main.GameUpdateCount)
                return;

            lastRightClickFrame = Main.GameUpdateCount;
            LongHoldReachedThisFrame = false;
            LongHoldReleasedThisFrame = false;
            bool rightMouseHeld = Player.Calamity().mouseRight || Main.mouseRight;

            bool validRightInput =
                Player.HeldItem.type == ModContent.ItemType<DesertEagle>() &&
                !Player.noItems &&
                !Player.CCed &&
                rightMouseHeld &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Player.HeldItem.type);

            if (validRightInput)
            {
                if (!trackingRightPress)
                {
                    trackingRightPress = true;
                    rightPressFrames = 0;
                }

                rightPressFrames++;
                if (rightPressFrames == RightHoldThresholdFrames + 1)
                    LongHoldReachedThisFrame = true;

                return;
            }

            if (!trackingRightPress)
                return;

            if (rightPressFrames > RightHoldThresholdFrames)
                LongHoldReleasedThisFrame = true;

            trackingRightPress = false;
            rightPressFrames = 0;
        }

        private void ResetRightClickState()
        {
            trackingRightPress = false;
            rightPressFrames = 0;
            LongHoldReachedThisFrame = false;
            LongHoldReleasedThisFrame = false;
        }
    }
}
