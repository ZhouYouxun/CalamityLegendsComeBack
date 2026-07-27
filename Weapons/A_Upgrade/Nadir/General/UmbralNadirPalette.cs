using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General
{
    /// <summary>冥蚀天底统一配色：纯黑吸光 + 冥思荧光绿。所有部件共用，保证家族质感一致。</summary>
    internal static class UmbralNadirPalette
    {
        public static readonly Color VoidBlack = Color.Black;
        public static readonly Color MeldGreen = Color.LightGreen;                 // 主荧光绿
        public static readonly Color MeldGreenBright = new Color(200, 255, 210);   // 高光
        public static readonly Color MeldGreenDeep = new Color(48, 120, 60);       // 拖尾中段
        public static readonly Color CorrosionAura = new Color(120, 255, 150);     // 蚀痕光环
    }
}
