using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.Passive
{
    internal sealed class AzureThunderPassivePlayer : ModPlayer
    {
        private const int NormalCooldown = 99 * 60;
        private const int HarmonyCooldown = 9 * 60;

        private bool holdingAzureThunder;
        private int dodgeCooldown;

        public override void ResetEffects()
        {
            holdingAzureThunder = false;
        }

        public override void UpdateDead()
        {
            holdingAzureThunder = false;
            dodgeCooldown = 0;
        }

        public override void PostUpdate()
        {
            if (dodgeCooldown > 0)
                dodgeCooldown--;

            if (IsHarmonyActive() && dodgeCooldown > HarmonyCooldown)
                dodgeCooldown = HarmonyCooldown;
        }

        public void SetHoldingAzureThunder()
        {
            holdingAzureThunder = true;
        }

        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            if (!CanDodge())
                return false;

            TriggerDodge();
            return true;
        }

        private bool CanDodge()
        {
            return holdingAzureThunder &&
                AzureThunderProgression.DodgeUnlocked &&
                Player.active &&
                !Player.dead &&
                Player.HeldItem != null &&
                Player.HeldItem.type == ModContent.ItemType<AzureThunder>() &&
                dodgeCooldown <= 0;
        }

        private bool IsHarmonyActive()
        {
            return Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>());
        }

        private void TriggerDodge()
        {
            dodgeCooldown = IsHarmonyActive() ? HarmonyCooldown : NormalCooldown;

            int healAmount = System.Math.Min(AzureThunderProgression.DodgeHealAmount, Player.statLifeMax2 - Player.statLife);
            if (healAmount > 0)
            {
                Player.statLife += healAmount;
                Player.HealEffect(healAmount, true);
            }

            Player.immune = true;
            Player.immuneNoBlink = true;
            Player.immuneTime = System.Math.Max(Player.immuneTime, 60);

            for (int i = 0; i < 36; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height),
                    DustID.FireworksRGB,
                    Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2f, 8f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.8f, 1.4f));
                dust.noGravity = true;
            }

            if (Player.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.72f, Pitch = 0.18f }, Player.Center);
        }
    }
}
