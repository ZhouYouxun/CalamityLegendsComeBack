using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityMod;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.PaRevo
{
    internal sealed class BFPassivePlayer : ModPlayer
    {
        public const int PassiveCooldownFrames = 120 * 60;
        public const int FinalStandDurationFrames = 15 * 60;

        private bool holdingBlossomFlux;

        public int PassiveCooldownTimer;
        public int FinalStandTimer;

        private bool HoldingBlossomFluxNow => holdingBlossomFlux && Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>();
        public bool PassiveUnlocked => Main.hardMode;
        public bool FinalStandActive => FinalStandTimer > 0;
        public int PassiveChargeFramesRequired => Player.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped
            ? BFAccessoryPlayer.SilvaHarpPassiveCooldownFrames
            : PassiveCooldownFrames;
        public bool PassiveReady => PassiveUnlocked && PassiveCooldownTimer >= PassiveChargeFramesRequired && !FinalStandActive;
        public bool ShouldShowCooldownDisplay =>
            (HoldingBlossomFluxNow && PassiveUnlocked) ||
            FinalStandActive;

        public int DisplayFrames => FinalStandActive ? FinalStandTimer : PassiveCooldownTimer;
        public int ChargeSeconds => System.Math.Min(PassiveChargeFramesRequired / 60, PassiveCooldownTimer / 60);
        public int RequiredChargeSeconds => PassiveChargeFramesRequired / 60;
        public float PassiveChargeCompletion => PassiveChargeFramesRequired <= 0
            ? 1f
            : MathHelper.Clamp(PassiveCooldownTimer / (float)PassiveChargeFramesRequired, 0f, 1f);

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
            if (!PassiveUnlocked)
            {
                PassiveCooldownTimer = 0;
            }
            else if (!FinalStandActive && HoldingBlossomFluxNow)
            {
                int chargeRate = Player.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped ? 2 : 1;
                int requiredFrames = PassiveChargeFramesRequired;
                int previousTimer = PassiveCooldownTimer;
                PassiveCooldownTimer = System.Math.Min(requiredFrames, PassiveCooldownTimer + chargeRate);
                if (previousTimer < requiredFrames && PassiveCooldownTimer >= requiredFrames && Player.whoAmI == Main.myPlayer && Player.active && !Player.dead)
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.58f, Pitch = 0.18f }, Player.Center);
            }

            if (FinalStandTimer > 0)
            {
                FinalStandTimer--;
                EmitFinalStandVisuals();
                if (FinalStandTimer == 0 && Player.active && !Player.dead)
                    ResolveFinalStandSurvival();
            }

            SyncPassiveDisplay();
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!FinalStandActive)
                return;

            if (Player.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped)
            {
                modifiers.FinalDamage *= 0.1f;
                return;
            }

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

            int displayFrames = ShouldShowCooldownDisplay ? System.Math.Max(1, DisplayFrames) : 0;
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
            return HoldingBlossomFluxNow &&
                   PassiveUnlocked &&
                   PassiveReady &&
                   FinalStandTimer <= 0;
        }

        private void TriggerFinalStand()
        {
            int healAmount = Player.statLifeMax2 - Player.statLife;
            Player.statLife = Player.statLifeMax2;
            if (healAmount > 0)
                Player.HealEffect(healAmount, true);

            FinalStandTimer = FinalStandDurationFrames;
            PassiveCooldownTimer = 0;
            bool silvaHarp = Player.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped;
            Player.immune = true;
            Player.immuneNoBlink = true;
            Player.immuneTime = System.Math.Max(Player.immuneTime, silvaHarp ? BFAccessoryPlayer.SilvaHarpFinalStandImmuneFrames : 45);

            SpawnRecoveryField();

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

        private void SpawnRecoveryField()
        {
            if (Main.myPlayer != Player.whoAmI)
                return;

            int fieldType = ModContent.ProjectileType<BFPassiveRecoveryField>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == fieldType)
                    projectile.Kill();
            }

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                Vector2.Zero,
                fieldType,
                0,
                0f,
                Player.whoAmI);
        }

        private void EmitFinalStandVisuals()
        {
            Lighting.AddLight(Player.Center, new Vector3(0.12f, 0.34f, 0.16f));

            Vector2 bodyPoint = Player.Center + new Vector2(
                Main.rand.NextFloat(-Player.width * 0.42f, Player.width * 0.42f),
                Main.rand.NextFloat(-Player.height * 0.52f, Player.height * 0.28f));

            Vector2 riseVelocity = new Vector2(
                Main.rand.NextFloat(-0.55f, 0.55f),
                Main.rand.NextFloat(-2.8f, -1.3f));

            if (Main.rand.NextBool(2))
            {
                SquishyLightParticle lifeFlame = new(
                    bodyPoint,
                    riseVelocity.RotatedByRandom(0.42f),
                    Main.rand.NextFloat(0.42f, 0.7f),
                    Color.Lerp(new Color(86, 255, 150), Color.White, Main.rand.NextFloat(0.12f, 0.3f)),
                    Main.rand.Next(12, 20),
                    1f,
                    Main.rand.NextFloat(1.3f, 2f));
                GeneralParticleHandler.SpawnParticle(lifeFlame);
            }

            if (Main.rand.NextBool(3))
            {
                GlowOrbParticle glowOrb = new(
                    bodyPoint + Main.rand.NextVector2Circular(6f, 8f),
                    riseVelocity * 0.12f,
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.28f, 0.45f),
                    Color.Lerp(new Color(72, 255, 132), Color.White, Main.rand.NextFloat(0.18f, 0.36f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(glowOrb);
            }

            if (Main.rand.NextBool(2))
            {
                Dust glowDust = Dust.NewDustPerfect(
                    bodyPoint,
                    DustID.GemEmerald,
                    riseVelocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.45f, 1.2f),
                    100,
                    Color.Lerp(new Color(88, 255, 164), Color.White, Main.rand.NextFloat(0.15f, 0.28f)),
                    Main.rand.NextFloat(0.95f, 1.35f));
                glowDust.noGravity = true;
            }
        }
    }
}
