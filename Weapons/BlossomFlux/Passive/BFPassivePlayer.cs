using CalamityMod;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive
{
    internal sealed class BFPassivePlayer : ModPlayer
    {
        public const int PassiveCooldownFrames = 150 * 60;
        public const int FinalStandDurationFrames = 15 * 60;

        private bool holdingBlossomFlux;

        public int PassiveCooldownTimer;
        public int FinalStandTimer;

        public bool PassiveUnlocked => Main.hardMode;
        public bool FinalStandActive => FinalStandTimer > 0;
        public bool PassiveReady => PassiveUnlocked && PassiveCooldownTimer <= 0 && !FinalStandActive;
        public bool ShouldShowCooldownDisplay =>
            (holdingBlossomFlux && Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>() && PassiveUnlocked) ||
            FinalStandActive ||
            PassiveCooldownTimer > 0;

        public int DisplayFrames => FinalStandActive ? FinalStandTimer : PassiveCooldownTimer;
        public int RemainingSeconds => DisplayFrames <= 0 ? 0 : (int)System.Math.Ceiling(DisplayFrames / 60f);

        public override void ResetEffects()
        {
            holdingBlossomFlux = false;
        }

        public override void UpdateDead()
        {
            holdingBlossomFlux = false;
            FinalStandTimer = 0;
            SyncPassiveDisplay();
        }

        public override void PostUpdate()
        {
            if (PassiveCooldownTimer > 0)
            {
                PassiveCooldownTimer--;
                if (PassiveCooldownTimer == 0 && Player.whoAmI == Main.myPlayer && Player.active && !Player.dead)
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.58f, Pitch = 0.18f }, Player.Center);
            }

            if (FinalStandTimer > 0)
            {
                FinalStandTimer--;
                if (FinalStandTimer == 0 && Player.active && !Player.dead)
                    ResolveFinalStandSurvival();
            }

            SyncPassiveDisplay();
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!FinalStandActive)
                return;

            modifiers.ModifyHurtInfo += ForceFinalStandDeath;
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (!CanTriggerFinalStand())
                return true;

            TriggerFinalStand();
            playSound = false;
            genGore = false;
            return false;
        }

        public void SetHoldingBlossomFlux()
        {
            holdingBlossomFlux = true;
        }

        public void SyncPassiveDisplay()
        {
            if (!PassiveUnlocked && PassiveCooldownTimer <= 0 && !FinalStandActive)
            {
                if (Player.Calamity().cooldowns.TryGetValue(BFPassiveCoolDown.ID, out var hiddenCooldown))
                    hiddenCooldown.timeLeft = 0;

                return;
            }

            int displayFrames = DisplayFrames;
            if (Player.Calamity().cooldowns.TryGetValue(BFPassiveCoolDown.ID, out var cooldown))
            {
                cooldown.timeLeft = displayFrames;
            }
            else
            {
                Player.AddCooldown(BFPassiveCoolDown.ID, displayFrames);
            }
        }

        private bool CanTriggerFinalStand()
        {
            return holdingBlossomFlux &&
                   Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>() &&
                   PassiveUnlocked &&
                   PassiveCooldownTimer <= 0 &&
                   FinalStandTimer <= 0;
        }

        private void TriggerFinalStand()
        {
            int healAmount = Player.statLifeMax2 - Player.statLife;
            Player.statLife = Player.statLifeMax2;
            if (healAmount > 0)
                Player.HealEffect(healAmount, true);

            FinalStandTimer = FinalStandDurationFrames;
            PassiveCooldownTimer = PassiveCooldownFrames;
            Player.immune = true;
            Player.immuneNoBlink = true;
            Player.immuneTime = System.Math.Max(Player.immuneTime, 45);

            if (Player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.75f, Pitch = -0.2f }, Player.Center);
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.65f, Pitch = -0.08f }, Player.Center);
            }
        }

        private void ResolveFinalStandSurvival()
        {
            int targetLife = Player.statLifeMax2 / 2;
            if (Player.statLife < targetLife)
            {
                int healAmount = targetLife - Player.statLife;
                Player.statLife = targetLife;
                Player.HealEffect(healAmount, true);
            }

            if (Player.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.58f, Pitch = 0.22f }, Player.Center);
        }

        private void ForceFinalStandDeath(ref Player.HurtInfo info)
        {
            info.Damage = System.Math.Max(info.Damage, Player.statLife + Player.statLifeMax2 + 1);
        }
    }
}
