using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_Battleship : YC_RightWarshipBase
    {
        private const float DetectionRange = 112f * 16f;
        private const float DetectionConeDegrees = 13f;

        private static readonly Vector2[] RelativeOffsets =
        {
            new(-84f, -70f),
            new(84f, -70f)
        };

        private static readonly float[] AngleOffsetsDegrees =
        {
            -2.5f,
            2.5f
        };

        private int fireCooldown;

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_Battleship";
        protected override Color AccentColor => new(255, 145, 105);

        protected override float PositionLerp => 0.22f;
        protected override float ScaleBase => 1f;
        protected override float ScaleAmplitude => 0.03f;

        protected override Vector2 GetLocalOffset() => RelativeOffsets[Utils.Clamp(SlotIndex, 0, RelativeOffsets.Length - 1)];
        protected override float GetAngleOffsetDegrees() => AngleOffsetsDegrees[Utils.Clamp(SlotIndex, 0, AngleOffsetsDegrees.Length - 1)];

        protected override void UpdateAttack(YC_RightHoldOut holdout, Projectile holdoutProjectile)
        {
            if (fireCooldown > 0)
                fireCooldown--;

            if (Projectile.owner != Main.myPlayer || fireCooldown > 0 || FindTargetAhead(DetectionRange, DetectionConeDegrees, false) == null)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 20f,
                CurrentForwardDirection * 18f,
                ModContent.ProjectileType<YC_Right_HeavyBolt>(),
                (int)(Projectile.damage * 1.9f),
                Projectile.knockBack + 1f,
                Projectile.owner);

            EmitMuzzleBurst(CurrentForwardDirection, AccentColor, 4.8f, 7);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.22f, Pitch = -0.22f + SlotIndex * 0.07f }, Projectile.Center);
            fireCooldown = 24;
        }
    }
}
