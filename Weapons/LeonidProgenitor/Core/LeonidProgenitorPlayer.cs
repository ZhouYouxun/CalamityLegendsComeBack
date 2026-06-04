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
        public const float TitaniumShieldChargePerHit = 0.05f;
        public const float TitaniumShieldChargeMax = 1f;

        public int PalladiumHealCooldown;
        public float TitaniumShieldCharge;
        public int TitaniumStompersTimer;

        public override void ResetEffects()
        {
        }

        public override void UpdateDead()
        {
            PalladiumHealCooldown = 0;
            TitaniumShieldCharge = 0f;
            TitaniumStompersTimer = 0;
        }

        public override void PostUpdate()
        {
            if (PalladiumHealCooldown > 0)
                PalladiumHealCooldown--;

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
    }
}
