using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal static class NewLegendPristineFuryHoldOut_DragonDrawData
    {
        private static float DragonEyeHorizontalOffset() => 16f; // 龙眼水平偏移：改大 = 更靠枪口/瞄准方向；改小 = 更靠玩家手部。
        private static float DragonEyeVerticalOffset() => -12f; // 龙眼垂直偏移：改大 = 往准星右侧的垂直方向移动；改小 = 往反方向移动。
        private static float DragonMouthHorizontalOffset() => 10f; // 龙嘴水平偏移：改大 = 更靠枪口/瞄准方向；改小 = 更靠玩家手部。
        private static float DragonMouthVerticalOffset() => 0f; // 龙嘴垂直偏移：改大 = 往准星右侧的垂直方向移动；改小 = 往反方向移动。

        private static string SourceFile => "Weapons/PristineFury/NewLegendPristineFuryHoldOut_DragonDrawData.cs";

        internal static Vector2 GetDragonMouthPosition(Vector2 projectileCenter, Vector2 aimDirection, float gravityDirection) =>
            GetLocalOffsetPosition(
                projectileCenter,
                aimDirection,
                gravityDirection,
                GetSourceFloat(nameof(DragonMouthHorizontalOffset), DragonMouthHorizontalOffset()),
                GetSourceFloat(nameof(DragonMouthVerticalOffset), DragonMouthVerticalOffset()));

        internal static Vector2 GetDragonEyePosition(Vector2 projectileCenter, Vector2 aimDirection, float gravityDirection) =>
            GetLocalOffsetPosition(
                projectileCenter,
                aimDirection,
                gravityDirection,
                GetSourceFloat(nameof(DragonEyeHorizontalOffset), DragonEyeHorizontalOffset()),
                GetSourceFloat(nameof(DragonEyeVerticalOffset), DragonEyeVerticalOffset()));

        internal static string DragonEyeBloomTexturePath() => "CalamityMod/Particles/BloomCircle";
        internal static string DragonEyeStarTexturePath() => "CalamityMod/Particles/FullStar";
        internal static string DragonEyeIrisTexturePath() => "CalamityMod/Particles/SmallBloomRingLayered";

        internal static string DragonMouthChargeBloomTexturePath() => "CalamityMod/Particles/BloomCircle";
        internal static string DragonMouthChargeSmearTexturePath() => "CalamityMod/Particles/ForwardSmear";
        internal static string DragonMouthChargeRingTexturePath() => "CalamityMod/Particles/BloomRing";
        internal static string DragonMouthMagicTexturePath() => "CalamityLegendsComeBack/Texture/KsTexture/magic_03";
        internal static string DragonMouthSmokeTexturePath() => "CalamityLegendsComeBack/Texture/KsTexture/smoke_04";

        private static float GetSourceFloat(string memberName, float fallback) =>
            RuntimeBalanceData.GetSourceFloatReturn(SourceFile, memberName, fallback);

        private static Vector2 GetLocalOffsetPosition(Vector2 projectileCenter, Vector2 aimDirection, float gravityDirection, float horizontalOffset, float verticalOffset)
        {
            Vector2 sideDirection = new Vector2(-aimDirection.Y, aimDirection.X) * gravityDirection;
            return projectileCenter + aimDirection * horizontalOffset + sideDirection * verticalOffset;
        }
    }
}
