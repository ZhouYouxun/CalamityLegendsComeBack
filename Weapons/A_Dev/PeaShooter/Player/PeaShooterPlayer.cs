using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal sealed class PeaShooterPlayer : ModPlayer
    {
        public bool HoldingPeaShooter { get; private set; }
        public bool AccessoryEquipped { get; private set; }
        public PeaShooterPeaType SelectedPea { get; private set; } = PeaShooterPeaType.Normal;

        public override void ResetEffects()
        {
            HoldingPeaShooter = false;
            AccessoryEquipped = false;
        }

        public override void UpdateDead()
        {
            HoldingPeaShooter = false;
            AccessoryEquipped = false;
        }

        public void SetHoldingPeaShooter()
        {
            HoldingPeaShooter = true;
        }

        public void SetPeaShooterAccessory()
        {
            AccessoryEquipped = true;
        }

        public void CycleSelectedPea()
        {
            SelectedPea++;
            if (SelectedPea > PeaShooterPeaType.Rock)
                SelectedPea = PeaShooterPeaType.Normal;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["PeaShooterSelectedPea"] = (int)SelectedPea;
        }

        public override void LoadData(TagCompound tag)
        {
            int selected = tag.ContainsKey("PeaShooterSelectedPea") ? tag.GetInt("PeaShooterSelectedPea") : 0;
            SelectedPea = selected >= 0 && selected <= (int)PeaShooterPeaType.Rock
                ? (PeaShooterPeaType)selected
                : PeaShooterPeaType.Normal;
        }
    }
}
