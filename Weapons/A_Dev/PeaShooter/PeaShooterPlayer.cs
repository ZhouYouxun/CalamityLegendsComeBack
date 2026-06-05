using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal sealed class PeaShooterPlayer : ModPlayer
    {
        public bool HoldingPeaShooter { get; private set; }
        public bool AutomaticFire { get; private set; } = true;

        public override void ResetEffects()
        {
            HoldingPeaShooter = false;
        }

        public override void UpdateDead()
        {
            HoldingPeaShooter = false;
        }

        public void SetHoldingPeaShooter()
        {
            HoldingPeaShooter = true;
        }

        public void ToggleFireMode()
        {
            AutomaticFire = !AutomaticFire;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["PeaShooterAutomaticFire"] = AutomaticFire;
        }

        public override void LoadData(TagCompound tag)
        {
            AutomaticFire = !tag.ContainsKey("PeaShooterAutomaticFire") || tag.GetBool("PeaShooterAutomaticFire");
        }
    }
}
