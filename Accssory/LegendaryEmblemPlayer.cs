using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Accssory
{
    internal sealed class LegendaryEmblemPlayer : ModPlayer
    {
        private bool temporaryEXUnlock;

        public bool PermanentEXUnlock;
        public bool EXAccessoryEquipped
        {
            get => PermanentEXUnlock || temporaryEXUnlock;
            set => temporaryEXUnlock = value;
        }

        public override void ResetEffects()
        {
            temporaryEXUnlock = false;
        }

        public override void SaveData(TagCompound tag)
        {
            if (PermanentEXUnlock)
                tag["PermanentEXUnlock"] = true;
        }

        public override void LoadData(TagCompound tag)
        {
            PermanentEXUnlock = tag.GetBool("PermanentEXUnlock");
        }
    }
}
