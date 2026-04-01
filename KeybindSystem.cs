using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria.Localization;

namespace CalamityLegendsComeBack
{
    public class KeybindSystem : ModSystem
    {
        public static ModKeybind LegendarySkill { get; private set; }

        public override void Load()
        {
            LegendarySkill = KeybindLoader.RegisterKeybind(Mod, Language.GetTextValue("Mods.CalamityLegendsComeBack.Keybinds.LegendarySkill"), "P");
        }

        public override void Unload()
        {
            LegendarySkill = null;
        }
    }
}