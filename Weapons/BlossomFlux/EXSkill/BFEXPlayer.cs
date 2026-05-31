using Terraria;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFEXPlayer : ModPlayer
    {
        public const int EXMax = 60;
        public const int PassiveRegenInterval = 120;

        public int EXValue;

        private int passiveRegenTimer;
        private bool holdingBlossomFlux;
        private bool wasEXReady;

        public bool EXReady => EXValue >= EXMax;
        public bool ShouldShowDisplay =>
            holdingBlossomFlux ||
            Player.ownedProjectileCounts[ModContent.ProjectileType<BFEXWeapon>()] > 0;

        public override void ResetEffects()
        {
            holdingBlossomFlux = false;
        }

        public override void PostUpdate()
        {
            if (!holdingBlossomFlux)
            {
                passiveRegenTimer = 0;
                LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasEXReady, EXReady);
                return;
            }

            if (EXValue >= EXMax)
            {
                passiveRegenTimer = 0;
                EXValue = EXMax;
                LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasEXReady, true);
                return;
            }

            passiveRegenTimer++;
            if (passiveRegenTimer >= PassiveRegenInterval)
            {
                passiveRegenTimer = 0;
                GainEX(1);
            }

            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasEXReady, EXReady);
        }

        public void SetHoldingBlossomFlux()
        {
            holdingBlossomFlux = true;
        }

        public void GainEX(int amount)
        {
            if (amount <= 0)
                return;

            EXValue = Utils.Clamp(EXValue + amount, 0, EXMax);
        }

        public bool ConsumeAllEX()
        {
            if (!EXReady)
                return false;

            EXValue = 0;
            passiveRegenTimer = 0;
            return true;
        }
    }
}
