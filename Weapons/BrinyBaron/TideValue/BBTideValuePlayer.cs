using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue
{
    internal class BBTideValuePlayer : ModPlayer
    {
        public const int MaxDesignedTideCap = 8;
        public const float TideDamageBonusPerStack = 0.01f;
        public const float FullTideDamageBonus = 0.2f;
        public const int TideChargeMax = 90 * 60;
        public const int TideDisplayMax = 90;

        private const int TideBladeHitCooldown = 120;
        private const int BossAutoGainInterval = 300;
        private const int NonBossDecayInterval = 330;
        private const int NoAttackClearDelay = 330;

        public int TideValue;
        public int TideChargeValue;
        private bool wasTideReady;
        private bool wasTideChargeReady;

        private int bladeHitTideCooldownTimer;
        private int bossAutoGainTimer;
        private int nonBossDecayTimer;
        private int noAttackTimer;

        public int CurrentTideMax => 4 + Player.GetModPlayer<BBAccessoryPlayer>().BonusTideMax;
        public bool TideFull => TideValue >= CurrentTideMax;
        public bool TideChargeFull => TideChargeValue >= TideChargeMax;
        // This gauge represents tide stacks. Charge remains an internal ultimate gate.
        public int TideDisplayValue => Utils.Clamp(TideValue, 0, CurrentTideMax);

        public float TideDamageMultiplier
        {
            get
            {
                float bonus = TideValue * TideDamageBonusPerStack;
                if (TideFull)
                    bonus += FullTideDamageBonus;
                return 1f + bonus;
            }
        }

        public override void PostUpdateEquips()
        {
            if (TideValue > CurrentTideMax)
                TideValue = CurrentTideMax;

            if (bladeHitTideCooldownTimer > 0)
                bladeHitTideCooldownTimer--;

            noAttackTimer++;
            int clearDelay = Player.GetModPlayer<BBAccessoryPlayer>().TideWiseHatEquipped ? NoAttackClearDelay * 2 : NoAttackClearDelay;
            if (noAttackTimer >= clearDelay)
            {
                if (!Player.GetModPlayer<Accssory.LegendaryUltimateTesterPlayer>().Equipped)
                    TideValue = 0;
                noAttackTimer = 0;
                bossAutoGainTimer = 0;
                nonBossDecayTimer = 0;
            }
            else
            {
                bool bossPresent = IsBossPresent();
                if (bossPresent)
                {
                    nonBossDecayTimer = 0;
                    bossAutoGainTimer++;
                    if (bossAutoGainTimer >= BossAutoGainInterval)
                    {
                        bossAutoGainTimer = 0;
                        AddTide();
                    }
                }
                else
                {
                    bossAutoGainTimer = 0;
                    if (TideValue > 0)
                    {
                        nonBossDecayTimer++;
                        int decayInterval = Player.GetModPlayer<BBAccessoryPlayer>().TideWiseHatEquipped ? NonBossDecayInterval * 2 : NonBossDecayInterval;
                        if (nonBossDecayTimer >= decayInterval)
                        {
                            nonBossDecayTimer = 0;
                            TideValue--;
                        }
                    }
                    else
                    {
                        nonBossDecayTimer = 0;
                    }
                }
            }

            UpdateTideCharge();
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasTideReady, TideFull);
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasTideChargeReady, TideChargeFull);
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (TideValue > 0)
                TideValue--;
        }

        public void AddTide(int amount = 1)
        {
            if (amount <= 0)
                return;

            // A full gauge is sticky: further gain events must be a no-op, never a
            // wrapped/reapplied amount. Clamp both operands because accessory changes
            // can alter the cap during the same update that a hit grants tide.
            int cap = Math.Max(0, CurrentTideMax);
            TideValue = Utils.Clamp(TideValue, 0, cap);
            if (TideValue >= cap)
                return;

            TideValue = Math.Min(cap, TideValue + amount);
        }

        public bool TryAddTideFromBlade()
        {
            if (bladeHitTideCooldownTimer > 0)
                return false;

            bladeHitTideCooldownTimer = TideBladeHitCooldown;
            noAttackTimer = 0;
            AddTide();
            return true;
        }

        public bool TryConsumeTide(int amount = 1)
        {
            if (amount <= 0)
                return true;

            if (TideValue < amount)
                return false;

            TideValue -= amount;
            if (TideValue < 0)
                TideValue = 0;

            return true;
        }

        public void ResetTide()
        {
            TideValue = 0;
            bladeHitTideCooldownTimer = 0;
            bossAutoGainTimer = 0;
            nonBossDecayTimer = 0;
            noAttackTimer = 0;
        }

        public void ResetTideCharge()
        {
            TideChargeValue = 0;
        }

        public static int GetFramesPerDisplayUnit()
        {
            return Math.Max(1, TideChargeMax / TideDisplayMax);
        }

        private void UpdateTideCharge()
        {
            bool holdingBrinyBaron = Player.HeldItem != null &&
                                     !Player.HeldItem.IsAir &&
                                     Player.HeldItem.ModItem is NewLegendBrinyBaron;

            if (holdingBrinyBaron)
                TideChargeValue++;

            TideChargeValue = Utils.Clamp(TideChargeValue, 0, TideChargeMax);
        }

        private static bool IsBossPresent()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.boss)
                    return true;
            }
            return false;
        }
    }
}
