using System;
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
        private const int SilverVolleyTarget = 3;
        private const int SilverVolleyPause = 18;
        private const int BarFadeInFrames = 15;
        private const int BarFadeOutFrames = 14;
        private const int BarDropFrames = 18;

        private int silverVolleyCounter;
        private int volleyPauseTimer;
        private float chargeBarProgress;
        private float chargeBarOpacity;
        private bool chargeReadyLastFrame;

        public bool HoldingDesertEagle { get; private set; }
        public bool PendingLifeRound { get; private set; }
        public float ChargeBarProgress => chargeBarProgress;
        public float ChargeBarOpacity => chargeBarOpacity;

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
    }
}
