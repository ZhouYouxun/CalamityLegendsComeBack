using System;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.PaRevo;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using Terraria.Audio;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using CalamityLegendsComeBack.Weapons.CosmicDischarge;
using CalamityLegendsComeBack.Weapons.Malachite.EXSkill;
using CalamityLegendsComeBack.Weapons.SHPC.EXSkill;
using CalamityLegendsComeBack.Weapons.Vesuvius.EXSkill;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill;
using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory
{
    internal sealed class LegendaryUltimateTester : ModItem
    {
        public new string LocalizationCategory => "Items.Accessories";
        //public override string Texture => "CalamityLegendsComeBack/Accssory/LegendaryEmblem";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = 0;
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<LegendaryEmblemPlayer>().EXAccessoryEquipped = true;
            player.GetModPlayer<LegendaryUltimateTesterPlayer>().Equipped = true;
        }
    }

    internal sealed class LegendaryUltimateTesterPlayer : ModPlayer
    {
        private const int FullChargeFrames = 60;
        private float brinyTideAccumulator;

        public bool Equipped;

        public override void ResetEffects()
        {
            Equipped = false;
        }

        public override void PostUpdate()
        {
            if (!Equipped || !Player.active || Player.dead)
            {
                brinyTideAccumulator = 0f;
                return;
            }

            ChargeSHPC();
            ChargeBlossomFlux();
            ChargeBlossomFluxPassive();
            ChargeBrinyBaron();
            ChargeBrinyBaronSuperDashCooldown();
            ChargeVesuvius();
            ChargeYharimsCrystal();
            ChargeCosmicDischarge();
            ChargeMalachite();
            ChargeAzureThunder();
        }

        private void ChargeSHPC()
        {
            NewLegend_EXPlayer exPlayer = Player.GetModPlayer<NewLegend_EXPlayer>();
            int max = NewLegend_EXPlayer.GetCurrentEXMax(Player);
            exPlayer.EXValue = Math.Min(max, exPlayer.EXValue + FramesPerTick(max));
            SetCooldownProgress(SHPC_EXCooldown.ID, max, exPlayer.EXValue);
        }

        private void ChargeBlossomFlux()
        {
            BFEXPlayer exPlayer = Player.GetModPlayer<BFEXPlayer>();
            exPlayer.EXValue = Math.Min(BFEXPlayer.EXMax, exPlayer.EXValue + FramesPerTick(BFEXPlayer.EXMax));
            SetCooldownProgress(BFEXCooldown.ID, BFEXPlayer.EXMax, exPlayer.EXValue);
        }

        private void ChargeBlossomFluxPassive()
        {
            BFPassivePlayer passivePlayer = Player.GetModPlayer<BFPassivePlayer>();
            if (passivePlayer.FinalStandActive || !passivePlayer.PassiveUnlocked)
                return;

            int max = BFPassivePlayer.PassiveCooldownFrames;
            int previousTimer = passivePlayer.PassiveCooldownTimer;
            passivePlayer.PassiveCooldownTimer = Math.Min(max, passivePlayer.PassiveCooldownTimer + FramesPerTick(max));

            if (previousTimer < max && passivePlayer.PassiveCooldownTimer >= max && Player.whoAmI == Main.myPlayer && Player.active && !Player.dead)
                SoundEngine.PlaySound(BlossomFluxSounds.PassivePlayerBuffSpawn, Player.Center);
        }

        private void ChargeBrinyBaron()
        {
            BBTideValuePlayer tidePlayer = Player.GetModPlayer<BBTideValuePlayer>();
            
            // Charge TideChargeValue
            tidePlayer.TideChargeValue = Math.Min(BBTideValuePlayer.TideChargeMax, tidePlayer.TideChargeValue + FramesPerTick(BBTideValuePlayer.TideChargeMax));

            // Charge TideValue
            int max = Math.Max(1, tidePlayer.CurrentTideMax);
            if (tidePlayer.TideValue >= max)
            {
                brinyTideAccumulator = 0f;
                SetCooldownProgress(BBTideValueCooldown.ID, BBTideValuePlayer.TideChargeMax, tidePlayer.TideChargeValue);
                return;
            }

            brinyTideAccumulator += max / (float)FullChargeFrames;
            int tideToAdd = (int)brinyTideAccumulator;
            if (tideToAdd > 0)
            {
                brinyTideAccumulator -= tideToAdd;
                tidePlayer.AddTide(tideToAdd);
            }

            SetCooldownProgress(BBTideValueCooldown.ID, BBTideValuePlayer.TideChargeMax, tidePlayer.TideChargeValue);
        }

        private void ChargeBrinyBaronSuperDashCooldown()
        {
            BBSuperDashCooldownPlayer cooldownPlayer = Player.GetModPlayer<BBSuperDashCooldownPlayer>();
            cooldownPlayer.FastForwardCooldown(FramesPerTick(Math.Max(1, cooldownPlayer.CooldownDuration)));
            SetCooldownProgress(BBSuperDashCooldownHandler.ID, cooldownPlayer.CooldownDuration, cooldownPlayer.RemainingFrames);
        }

        private void ChargeVesuvius()
        {
            VesuviusEXPlayer exPlayer = Player.GetModPlayer<VesuviusEXPlayer>();
            exPlayer.EXValue = Math.Min(VesuviusEXPlayer.EXMax, exPlayer.EXValue + FramesPerTick(VesuviusEXPlayer.EXMax));
            SetCooldownProgress(VesuviusEXCooldown.ID, VesuviusEXPlayer.EXMax, exPlayer.EXValue);
        }

        private void ChargeYharimsCrystal()
        {
            YCEXPlayer exPlayer = Player.GetModPlayer<YCEXPlayer>();
            exPlayer.CooldownValue = Math.Max(0, exPlayer.CooldownValue - FramesPerTick(YCEXPlayer.CooldownMax));
            exPlayer.ChargeValue = Math.Min(YCEXPlayer.ChargeMax, exPlayer.ChargeValue + FramesPerTick(YCEXPlayer.ChargeMax));
            SetCooldownProgress(YCEXCoolDown.ID, YCEXPlayer.ChargeMax, exPlayer.DisplayRawValue);
        }

        private void ChargeCosmicDischarge()
        {
            CosmicDischargePlayer dischargePlayer = Player.GetModPlayer<CosmicDischargePlayer>();
            if (!dischargePlayer.UltimateFieldActive)
                dischargePlayer.UltimateEnergy = Math.Min(CosmicDischargePlayer.UltimateEnergyMax, dischargePlayer.UltimateEnergy + FramesPerTick(CosmicDischargePlayer.UltimateEnergyMax));

            SetCooldownProgress(CosmicDischargeUltimateCooldown.ID, CosmicDischargePlayer.UltimateEnergyMax, dischargePlayer.UltimateEnergy);
        }

        private void ChargeMalachite()
        {
            CalamityMod.CalPlayer.CalamityPlayer calamityPlayer = Player.Calamity();
            if (calamityPlayer.rogueStealthMax <= 0f)
                calamityPlayer.rogueStealthMax = 1f;

            calamityPlayer.rogueStealth = Math.Min(
                calamityPlayer.rogueStealthMax,
                calamityPlayer.rogueStealth + calamityPlayer.rogueStealthMax / FullChargeFrames);

            if (Player.Calamity().cooldowns.TryGetValue(MalachiteEXCooldown.ID, out var cooldown))
                cooldown.timeLeft = Math.Max(0, cooldown.timeLeft - FramesPerTick(MalachiteEXCooldown.CooldownFrames));
        }

        private void ChargeAzureThunder()
        {
            AzureThunderPlayer thunderPlayer = Player.GetModPlayer<AzureThunderPlayer>();
            thunderPlayer.UltimateEnergy = Math.Min(AzureThunderPlayer.UltimateEnergyMax, thunderPlayer.UltimateEnergy + FramesPerTick(AzureThunderPlayer.UltimateEnergyMax));
            SetCooldownProgress(AzureThunderUltimateCooldown.ID, AzureThunderPlayer.UltimateEnergyMax, thunderPlayer.UltimateEnergy);
        }

        private static int FramesPerTick(int maxValue)
        {
            return Math.Max(1, (int)Math.Ceiling(maxValue / (float)FullChargeFrames));
        }

        private void SetCooldownProgress(string id, int duration, int timeLeft)
        {
            if (Player.Calamity().cooldowns.TryGetValue(id, out var cooldown))
            {
                cooldown.duration = duration;
                cooldown.timeLeft = timeLeft;
                return;
            }

            Player.AddCooldown(id, duration).timeLeft = timeLeft;
        }
    }
}
