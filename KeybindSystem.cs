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
        public static ModKeybind LegendaryWeaponFormSwitch { get; private set; }
        public static ModKeybind WeaponLoadingUI { get; private set; }
        public static ModKeybind LeonidConstellationChart { get; private set; }
        public static ModKeybind ExtraBackpack { get; private set; }

        public override void Load()
        {
            LegendarySkill = KeybindLoader.RegisterKeybind(Mod, "LegendarySkill", "P");
            LegendaryWeaponFormSwitch = KeybindLoader.RegisterKeybind(Mod, "LegendaryWeaponFormSwitch", "LeftControl");
            WeaponLoadingUI = KeybindLoader.RegisterKeybind(Mod, "WeaponLoadingUI", "None");
            LeonidConstellationChart = KeybindLoader.RegisterKeybind(Mod, "LeonidConstellationChart", "LeftControl");
            ExtraBackpack = KeybindLoader.RegisterKeybind(Mod, "ExtraBackpack", "P");
        }

        public override void Unload()
        {
            LegendarySkill = null;
            LegendaryWeaponFormSwitch = null;
            WeaponLoadingUI = null;
            LeonidConstellationChart = null;
            ExtraBackpack = null;
        }
    }
}
