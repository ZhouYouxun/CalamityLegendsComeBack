using System;
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

        public override void ResetEffects()
        {
        }

        public override void UpdateDead()
        {
            PalladiumHealCooldown = 0;
            TitaniumShieldCharge = 0f;
        }

        public override void PostUpdate()
        {
            if (PalladiumHealCooldown > 0)
                PalladiumHealCooldown--;

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
            Player.statLife = Math.Min(Player.statLife + healAmount, Player.statLifeMax2);
            Player.HealEffect(healAmount, true);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.45f, Pitch = 0.22f }, Player.Center);
        }

        public void AddTitaniumShieldCharge(float amount)
        {
            TitaniumShieldCharge = MathHelper.Clamp(TitaniumShieldCharge + amount, 0f, TitaniumShieldChargeMax);
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
