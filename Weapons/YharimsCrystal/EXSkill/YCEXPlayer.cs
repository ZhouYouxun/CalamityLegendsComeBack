using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    internal class YCEXPlayer : ModPlayer
    {
        public int ChargeValue;
        public int CooldownValue;

        public const int ChargeMax = 30 * 60;
        public const int CooldownMax = 120 * 60;
        public const int ChargeDisplayMax = 30;
        public const int CooldownDisplayMax = 120;

        private bool WasUltimateActiveLastFrame;

        public bool IsUltimateActive => Player.ownedProjectileCounts[ModContent.ProjectileType<YC_EX_VIP>()] > 0;
        public bool IsCoolingDown => CooldownValue > 0;
        public bool IsReady => !IsUltimateActive && !IsCoolingDown && ChargeValue >= ChargeMax;
        public bool CanActivateUltimate => IsReady;

        public int DisplayRawValue => IsCoolingDown ? CooldownValue : IsUltimateActive ? ChargeMax : ChargeValue;
        public float DisplayCompletion =>
            IsCoolingDown ? 1f - CooldownValue / (float)CooldownMax :
            IsUltimateActive ? 1f :
            ChargeValue / (float)ChargeMax;

        public int DisplayValue =>
            IsCoolingDown ? Utils.Clamp((int)Math.Ceiling(CooldownValue / 60f), 0, CooldownDisplayMax) :
            Utils.Clamp(ChargeValue / 60, 0, ChargeDisplayMax);

        public override void ResetEffects()
        {
            ChargeValue = Utils.Clamp(ChargeValue, 0, ChargeMax);
            CooldownValue = Utils.Clamp(CooldownValue, 0, CooldownMax);
        }

        public override void PostUpdate()
        {
            bool active = IsUltimateActive;
            bool holdingCrystal = Player.HeldItem != null &&
                                  !Player.HeldItem.IsAir &&
                                  Player.HeldItem.ModItem is NewLegendYharimsCrystal;

            if (active)
            {
                ChargeValue = ChargeMax;
                CooldownValue = 0;
                WasUltimateActiveLastFrame = true;
                return;
            }

            if (WasUltimateActiveLastFrame)
            {
                WasUltimateActiveLastFrame = false;
                ChargeValue = 0;
                CooldownValue = CooldownMax;
                return;
            }

            if (CooldownValue > 0)
            {
                CooldownValue--;
                return;
            }

            if (holdingCrystal)
            {
                if (ChargeValue < ChargeMax)
                    ChargeValue++;
            }
            else
            {
                ChargeValue -= 2;
                if (ChargeValue < 0)
                    ChargeValue = 0;
            }
        }
    }
}
