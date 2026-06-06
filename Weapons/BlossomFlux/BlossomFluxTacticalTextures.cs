using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal static class BlossomFluxTacticalTextures
    {
        public const string RecoveryWeaponTexture = "CalamityLegendsComeBack/Weapons/BlossomFlux/NewLegendBlossomFlux";

        public static string GetWeaponTexturePath(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => "CalamityLegendsComeBack/Weapons/BlossomFlux/贴图/突破叶流",
            BlossomFluxChloroplastPresetType.Chlo_CDetec => "CalamityLegendsComeBack/Weapons/BlossomFlux/贴图/侦察叶流",
            BlossomFluxChloroplastPresetType.Chlo_DBomb => "CalamityLegendsComeBack/Weapons/BlossomFlux/贴图/轰炸叶流",
            BlossomFluxChloroplastPresetType.Chlo_EPlague => "CalamityLegendsComeBack/Weapons/BlossomFlux/贴图/瘟疫叶流",
            _ => RecoveryWeaponTexture
        };

        public static Texture2D GetWeaponTexture(BlossomFluxChloroplastPresetType preset) =>
            ModContent.Request<Texture2D>(GetWeaponTexturePath(preset)).Value;

        public static BlossomFluxChloroplastPresetType GetLocalDisplayedPreset()
        {
            if (Main.LocalPlayer?.active != true)
                return BlossomFluxChloroplastPresetType.Chlo_BRecov;

            return Main.LocalPlayer.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
        }
    }
}
