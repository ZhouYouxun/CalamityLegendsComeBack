using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    public class LeonidProgenitorPlayer : ModPlayer
    {
        public const int PalladiumHealCooldownMax = 6 * 60;
        public const int RightClickCooldownFrames = 2 * 60;
        public const float TitaniumShieldChargePerHit = 0.05f;
        public const float TitaniumShieldChargeMax = 1f;

        public int PalladiumHealCooldown;
        public int RightClickCooldownTimer;
        private int rightClickCooldownFeedbackTimer;
        public float TitaniumShieldCharge;
        public int TitaniumStompersTimer;

        public int UltimateEnergy;
        public int ultimateEnergyTimer;
        private bool wasUltimateReady;

        public bool IsHoldingLeonid => Player.HeldItem != null && !Player.HeldItem.IsAir && Player.HeldItem.type == ModContent.ItemType<LeonidProgenitor>();

        public override void ResetEffects()
        {
        }

        public override void UpdateDead()
        {
            PalladiumHealCooldown = 0;
            RightClickCooldownTimer = 0;
            rightClickCooldownFeedbackTimer = 0;
            TitaniumShieldCharge = 0f;
            TitaniumStompersTimer = 0;
            UltimateEnergy = 0;
            ultimateEnergyTimer = 0;
            wasUltimateReady = false;
        }

        public override void PostUpdate()
        {
            if (IsHoldingLeonid)
            {
                Player.gravControl = true;
                Player.slowFall = true;
                Player.buffImmune[BuffID.VortexDebuff] = true;
                if (ModContent.TryFind<ModBuff>("CalamityMod", "Warped", out var warped))
                    Player.buffImmune[warped.Type] = true;
                if (ModContent.TryFind<ModBuff>("CalamityMod", "DoGExtremeGravity", out var dogGrav))
                    Player.buffImmune[dogGrav.Type] = true;

                ultimateEnergyTimer++;
                if (ultimateEnergyTimer >= 60)
                {
                    ultimateEnergyTimer = 0;
                    AddUltimateEnergy(1);
                }
            }
            else
            {
                ultimateEnergyTimer = 0;
            }

            if (PalladiumHealCooldown > 0)
                PalladiumHealCooldown--;
            if (RightClickCooldownTimer > 0)
                RightClickCooldownTimer--;
            if (rightClickCooldownFeedbackTimer > 0)
                rightClickCooldownFeedbackTimer--;

            if (TitaniumStompersTimer > 0)
            {
                TitaniumStompersTimer--;
                ApplyTitaniumStompersMovement();
            }

            if (TitaniumShieldCharge > 0f)
            {
                Lighting.AddLight(Player.Center, new Vector3(0.15f, 0.2f, 0.28f) * TitaniumShieldCharge * 1.5f);
                if (Main.rand.NextBool(3))
                {
                    Dust shieldDust = Dust.NewDustPerfect(
                        Player.Center + Main.rand.NextVector2Circular(Player.width * 0.75f, Player.height * 0.95f),
                        DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(0.6f, 0.6f),
                        100,
                        Color.Lerp(new Color(185, 225, 255), Color.White, Main.rand.NextFloat(0.35f)),
                        0.9f + TitaniumShieldCharge * 0.6f);
                    shieldDust.noGravity = true;
                }
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (TitaniumShieldCharge <= 0f)
                return;

            modifiers.ModifyHurtInfo += ApplyTitaniumShield;
        }

        public void TryTriggerPalladiumHeal(int healAmount)
        {
            if (PalladiumHealCooldown > 0 || healAmount <= 0)
                return;

            PalladiumHealCooldown = PalladiumHealCooldownMax;
            Player.AddBuff(BuffID.RapidHealing, 300);
            Player.statLife = Math.Min(Player.statLife + healAmount, Player.statLifeMax2);
            Player.HealEffect(healAmount, true);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.45f, Pitch = 0.22f }, Player.Center);
        }

        public bool CanUseRightClick => RightClickCooldownTimer <= 0;

        public void StartRightClickCooldown()
        {
            RightClickCooldownTimer = RightClickCooldownFrames;
        }

        public void SpawnRightClickCooldownFeedback()
        {
            if (Player.whoAmI != Main.myPlayer || rightClickCooldownFeedbackTimer > 0 || Main.dedServ)
                return;

            rightClickCooldownFeedbackTimer = 10;
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1.1f, 1.1f) - Vector2.UnitY * Main.rand.NextFloat(0.3f, 1.1f);
                Dust dust = Dust.NewDustPerfect(
                    Player.MountedCenter + Main.rand.NextVector2Circular(12f, 18f),
                    DustID.TintableDustLighted,
                    velocity,
                    120,
                    Color.Lerp(new Color(82, 216, 255), new Color(224, 240, 255), Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.78f));
                dust.noGravity = true;
            }
        }

        public void AddTitaniumShieldCharge(float amount)
        {
            TitaniumShieldCharge = MathHelper.Clamp(TitaniumShieldCharge + amount, 0f, TitaniumShieldChargeMax);
        }

        public void ActivateTitaniumStompers(int duration)
        {
            TitaniumStompersTimer = Math.Max(TitaniumStompersTimer, duration);
        }

        private void ApplyTitaniumStompersMovement()
        {
            Player.noFallDmg = true;
            Player.moveSpeed += 0.06f;
            Player.jumpSpeedBoost += 1f;
            Player.runAcceleration *= 1.12f;
            Player.accRunSpeed += 0.45f;

            if (Player.velocity.Y * Player.gravDir > 0f)
            {
                Player.maxFallSpeed = Math.Max(Player.maxFallSpeed, 40f);
                Player.gravity = Math.Max(Player.gravity, 1.05f);
            }

            Player.Calamity().gSabaton = true;
            Player.Calamity().gSabatonTempJumpSpeed = Math.Max(Player.Calamity().gSabatonTempJumpSpeed, 8);

            Lighting.AddLight(Player.Center, new Vector3(0.14f, 0.2f, 0.3f));
            if (Main.rand.NextBool(4))
            {
                Dust stompDust = Dust.NewDustPerfect(
                    Player.Bottom + new Vector2(Main.rand.NextFloat(-Player.width * 0.45f, Player.width * 0.45f), Main.rand.NextFloat(-6f, 4f)),
                    DustID.TintableDustLighted,
                    new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-1.8f, -0.2f)),
                    100,
                    new Color(206, 232, 255),
                    Main.rand.NextFloat(0.7f, 1f));
                stompDust.noGravity = true;
            }
        }

        private void ApplyTitaniumShield(ref Player.HurtInfo info)
        {
            int absorbAmount = (int)Math.Round(MathHelper.Lerp(40f, 220f, TitaniumShieldCharge));
            absorbAmount = Math.Min(absorbAmount, info.Damage);
            if (absorbAmount <= 0)
                return;

            info.Damage -= absorbAmount;
            TitaniumShieldCharge = 0f;

            for (int i = 0; i < 12; i++)
            {
                Dust burst = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(Player.width * 0.6f, Player.height * 0.8f),
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(3.4f, 3.4f),
                    100,
                    Color.Lerp(new Color(186, 225, 255), Color.White, Main.rand.NextFloat(0.45f)),
                    Main.rand.NextFloat(1f, 1.5f));
                burst.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = 0.28f }, Player.Center);
        }

        public void AddUltimateEnergy(int amount)
        {
            if (amount <= 0)
                return;

            UltimateEnergy = Math.Clamp(UltimateEnergy + amount, 0, 100);
            
            // Ultimate is ready if energy is 100 AND player stealth is at 100%.
            bool ready = UltimateEnergy >= 100 && Player.Calamity().rogueStealth >= Player.Calamity().rogueStealthMax * 0.999f;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, ready);
        }
    }
}
