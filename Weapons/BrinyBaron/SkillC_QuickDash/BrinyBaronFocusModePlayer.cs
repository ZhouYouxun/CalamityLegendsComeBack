using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash
{
    internal class BrinyBaronFocusModePlayer : ModPlayer
    {
        private const int FocusActivationTime = 60 * 5;

        private int focusIdleTimer;

        public bool HoldingBrinyBaron { get; private set; }
        public bool IsFocusModeActive { get; private set; }
        public float FocusVisualIntensity { get; private set; }

        public override void ResetEffects()
        {
            HoldingBrinyBaron = false;
        }

        public override void UpdateDead()
        {
            focusIdleTimer = 0;
            IsFocusModeActive = false;
            FocusVisualIntensity = 0f;
        }

        public override void PostUpdate()
        {
            bool isIdleWithBrinyBaron =
                HoldingBrinyBaron &&
                !Player.dead &&
                !Player.CCed &&
                !Player.noItems &&
                !Player.ItemAnimationActive &&
                !Main.mouseLeft &&
                !Main.mouseRight;

            if (isIdleWithBrinyBaron)
                focusIdleTimer++;
            else
                focusIdleTimer = 0;

            IsFocusModeActive = isIdleWithBrinyBaron && focusIdleTimer >= FocusActivationTime;

            float targetIntensity = IsFocusModeActive ? 1f : 0f;
            FocusVisualIntensity = MathHelper.Lerp(FocusVisualIntensity, targetIntensity, IsFocusModeActive ? 0.12f : 0.22f);
            if (FocusVisualIntensity < 0.01f)
                FocusVisualIntensity = 0f;
        }

        public void SetHoldingBrinyBaron()
        {
            HoldingBrinyBaron = true;
        }
    }
}
