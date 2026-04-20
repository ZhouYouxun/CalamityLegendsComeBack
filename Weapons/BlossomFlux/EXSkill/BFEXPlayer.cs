using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal class BFEXPlayer : ModPlayer
    {
        public const int UltimateCooldownSeconds = 20;
        public const int FramesPerDisplayUnit = 60;
        public const int UltimateCooldownFrames = UltimateCooldownSeconds * FramesPerDisplayUnit;

        public int UltimateCooldownTimer;

        public int DisplayFrames => UltimateCooldownTimer;
        public int DisplayValue => UltimateCooldownTimer <= 0 ? 0 : (int)System.Math.Ceiling(UltimateCooldownTimer / (float)FramesPerDisplayUnit);
        public bool CanTriggerUltimate => Player.GetModPlayer<BFRightUIPlayer>().UltimateUnlocked && UltimateCooldownTimer <= 0;

        public override void ResetEffects()
        {
            if (UltimateCooldownTimer < 0)
                UltimateCooldownTimer = 0;
            else if (UltimateCooldownTimer > UltimateCooldownFrames)
                UltimateCooldownTimer = UltimateCooldownFrames;
        }

        public override void PostUpdate()
        {
            if (!Player.GetModPlayer<BFRightUIPlayer>().UltimateUnlocked)
            {
                UltimateCooldownTimer = 0;
                return;
            }

            if (UltimateCooldownTimer > 0)
            {
                UltimateCooldownTimer--;
                if (UltimateCooldownTimer == 0 && Player.whoAmI == Main.myPlayer)
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.32f }, Player.Center);
            }
        }

        public void StartUltimateCooldown()
        {
            UltimateCooldownTimer = UltimateCooldownFrames;
            if (Player.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.62f, Pitch = -0.05f }, Player.Center);
        }

        public void ResetUltimateCooldown()
        {
            UltimateCooldownTimer = 0;
        }
    }
}
