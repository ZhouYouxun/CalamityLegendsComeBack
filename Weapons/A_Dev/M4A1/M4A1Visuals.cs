using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>M4A1 全家桶共享的视觉语言：阶段配色 + 阶段名本地化键。</summary>
    public static class M4A1Visuals
    {
        /// <summary>随战术同步率阶段升温：冷灰 -> 琥珀 -> 橙 -> 炽红。</summary>
        public static Color StageColor(int stage) => stage switch
        {
            0 => new Color(198, 202, 210), // 初始校准
            1 => new Color(255, 196, 120), // 战术锁定
            2 => new Color(255, 138, 66),  // 指挥接管
            _ => new Color(255, 92, 70)    // 完全同步
        };

        /// <summary>复仇印记的标志红。</summary>
        public static readonly Color MarkColor = new(255, 64, 58);

        private static readonly string[] StageKeys =
        {
            "Calibrating", "TacticalLock", "CommandOverride", "FullSync"
        };

        public static string StageName(int stage)
        {
            int i = stage < 0 ? 0 : stage > 3 ? 3 : stage;
            return Language.GetTextValue($"Mods.CalamityLegendsComeBack.M4A1.Stage.{StageKeys[i]}");
        }

        public static string SyncLabel() =>
            Language.GetTextValue("Mods.CalamityLegendsComeBack.M4A1.UI.SyncLabel");
    }
}
