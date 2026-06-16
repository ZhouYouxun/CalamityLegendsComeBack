using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal static class NewLegendPristineFuryHoldOut_DragonDrawData
    {
        // Position anchors. These are world-space offsets from NewLegendPristineFuryHoldOut.Projectile.Center.
        internal const float GunTipForwardOffset = 22f;
        internal const float DragonMouthForwardOffsetFromGunTip = 8f;
        internal const float DragonEyeForwardOffset = 16f;
        internal const float DragonEyeSideOffset = 12f;
        internal const float FakeCalamityChargeForwardTravelFromMouth = 6f;
        internal const float RightArcNovaChargeForwardTravelFromMouth = 5f;

        // Dragon eye starburst textures.
        internal const string DragonEyeBloomTexturePath = "CalamityMod/Particles/BloomCircle";
        internal const string DragonEyeStarTexturePath = "CalamityMod/Particles/FullStar";         // 四角对称，比 HalfStar 更完整，视觉上更像晶眼
        internal const string DragonEyeIrisTexturePath = "CalamityMod/Particles/SmallBloomRingLayered"; // 虹膜环，营造龙眼瞳孔感

        // Dragon mouth visual textures.
        internal const string DragonMouthChargeBloomTexturePath = "CalamityMod/Particles/BloomCircle";
        internal const string DragonMouthChargeSmearTexturePath = "CalamityMod/Particles/ForwardSmear";
        internal const string DragonMouthChargeRingTexturePath = "CalamityMod/Particles/BloomRing";
        internal const string DragonMouthMagicTexturePath = "CalamityLegendsComeBack/Texture/KsTexture/magic_03";
        internal const string DragonMouthSmokeTexturePath = "CalamityLegendsComeBack/Texture/KsTexture/smoke_04";

        internal static Vector2 GetGunTipPosition(Vector2 projectileCenter, Vector2 aimDirection) =>
            projectileCenter + aimDirection * GunTipForwardOffset;

        internal static Vector2 GetDragonMouthPosition(Vector2 projectileCenter, Vector2 aimDirection) =>
            GetGunTipPosition(projectileCenter, aimDirection) + aimDirection * DragonMouthForwardOffsetFromGunTip;

        internal static Vector2 GetDragonEyePosition(Vector2 projectileCenter, Vector2 aimDirection, float gravityDirection)
        {
            Vector2 sideDirection = new Vector2(-aimDirection.Y, aimDirection.X) * gravityDirection;
            return projectileCenter + aimDirection * DragonEyeForwardOffset - sideDirection * DragonEyeSideOffset;
        }
    }
}
