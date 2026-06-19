using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal static class NewLegendPristineFuryHoldOut_DragonDrawData
    {
        public static float DragonEyeHorizontalOffset() => 15f; // Longitudinal offset along the aim direction.
        public static float DragonEyeVerticalOffset() => -11f; // Mirrored visual-side offset, fixed for left-facing holdouts.
        public static int DragonEyeLayerCount() => 5; // SHPC right-click DrawApoctosisCoreGlow uses 5 layers.
        public static int DragonEyeStarArmsPerLayer() => 2; // Each layer draws two opposite HalfStar arms.
        public static float DragonEyeBloomScale() => 0.75f; // SHPC BloomCircle scale at maximum visual power.
        public static float DragonEyeHalfStarMinScale() => 3f; // SHPC HalfStar random scale lower bound.
        public static float DragonEyeHalfStarMaxScale() => 4.5f; // SHPC HalfStar random scale upper bound.

        public static string SourceFile => "Weapons/PristineFury/NewLegendPristineFuryHoldOut_DragonDrawData.cs";

        internal static Vector2 GetDragonEyePosition(Vector2 projectileCenter, Vector2 aimDirection, float gravityDirection, int spriteDirection) =>
            GetLocalOffsetPosition(
                projectileCenter,
                aimDirection,
                gravityDirection,
                spriteDirection,
                GetSourceFloat(nameof(DragonEyeHorizontalOffset), DragonEyeHorizontalOffset()),
                GetSourceFloat(nameof(DragonEyeVerticalOffset), DragonEyeVerticalOffset()));

        internal static string DragonEyeBloomTexturePath() => "CalamityMod/Particles/BloomCircle";
        internal static string DragonEyeHalfStarTexturePath() => "CalamityMod/Particles/HalfStar";

        private static float GetSourceFloat(string memberName, float fallback) =>
            RuntimeBalanceData.GetSourceFloatReturn(SourceFile, memberName, fallback);

        private static Vector2 GetLocalOffsetPosition(Vector2 projectileCenter, Vector2 aimDirection, float gravityDirection, int spriteDirection, float horizontalOffset, float verticalOffset)
        {
            int visualDirection = spriteDirection == 0 ? 1 : spriteDirection;
            Vector2 sideDirection = new Vector2(-aimDirection.Y, aimDirection.X) * gravityDirection * visualDirection;
            return projectileCenter + aimDirection * horizontalOffset + sideDirection * verticalOffset;
        }
    }
}
