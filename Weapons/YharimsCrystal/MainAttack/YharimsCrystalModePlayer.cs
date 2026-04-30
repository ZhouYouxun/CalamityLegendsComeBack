using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack
{
    public class YharimsCrystalModePlayer : ModPlayer
    {
        public YharimsCrystalAttackMode CurrentMode = YharimsCrystalAttackMode.Warships;

        public void CycleMode(int direction)
        {
            int modeCount = System.Enum.GetValues(typeof(YharimsCrystalAttackMode)).Length;
            int next = ((int)CurrentMode + direction) % modeCount;
            if (next < 0)
                next += modeCount;

            CurrentMode = (YharimsCrystalAttackMode)next;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["YharimsCrystalMode"] = (int)CurrentMode;
        }

        public override void LoadData(TagCompound tag)
        {
            int mode = tag.GetInt("YharimsCrystalMode");
            int maxMode = System.Enum.GetValues(typeof(YharimsCrystalAttackMode)).Length - 1;
            CurrentMode = (YharimsCrystalAttackMode)Utils.Clamp(mode, 0, maxMode);
        }
    }
}
