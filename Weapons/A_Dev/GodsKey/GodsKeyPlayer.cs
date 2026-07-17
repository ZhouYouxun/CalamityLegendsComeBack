using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Dev.GodsKey
{
    public sealed class GodsKeyPlayer : ModPlayer
    {
        public const float BoostedPanelMultiplier = 1.25f;

        public bool PanelBoostEnabled;

        public float PanelMultiplier => PanelBoostEnabled ? BoostedPanelMultiplier : 1f;

        public override void SaveData(TagCompound tag)
        {
            if (PanelBoostEnabled)
                tag["PanelBoostEnabled"] = true;
        }

        public override void LoadData(TagCompound tag)
        {
            PanelBoostEnabled = tag.GetBool("PanelBoostEnabled");
        }
    }
}
