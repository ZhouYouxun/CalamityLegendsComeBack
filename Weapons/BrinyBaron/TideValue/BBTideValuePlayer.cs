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
        public const float TideDamageBonusPerStack = 0.03f;
        public const float FullTideDamageBonus = 0.10f;
        public const int TideChargeMax = 90 * 60;
        public const int TideDisplayMax = 90;

        private const int TideBladeHitCooldown = 120;
        private const int BossAutoGainInterval = 300;
        private const int NonBossDecayInterval = 300;
        private const int NoAttackClearDelay = 300;

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
        public int TideDisplayValue => Utils.Clamp(TideChargeValue / GetFramesPerDisplayUnit(), 0, TideDisplayMax);

        public float TideDamageMultiplier
        {
            get
            {
                float bonus = TideValue * TideDamageBonusPerStack;
                if (TideFull)
                    bonus += FullTideDamageBonus + Player.GetModPlayer<BBAccessoryPlayer>().BottleFullTideDamageBonus;
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
            if (noAttackTimer >= NoAttackClearDelay)
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
                        if (nonBossDecayTimer >= NonBossDecayInterval)
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

            TideValue += amount;
            if (TideValue > CurrentTideMax)
                TideValue = CurrentTideMax;
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
