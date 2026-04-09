using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_LaserCruiser : YC_RightWarshipBase, IYCRightBeamSource
    {
        private const int BeamDuration = 28;
        private const int BeamCooldown = 82;
        private const float DetectionRange = 92f * 16f;
        private const float DetectionConeDegrees = 10f;

        private static readonly Vector2[] RelativeOffsets =
        {
            new(-88f, -12f),
            new(-110f, -34f),
            new(88f, -12f),
            new(110f, -34f)
        };

        private static readonly float[] AngleOffsetsDegrees =
        {
            -4f,
            -6.5f,
            4f,
            6.5f
        };

        private int fireCooldown;

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_LaserCruiser";
        protected override Color AccentColor => new(255, 204, 118);

        protected override float PositionLerp => 0.25f;
        protected override float ScaleBase => 0.96f;
        protected override float ScaleAmplitude => 0.035f;
        protected override float LightStrength => 0.48f;

        protected override Vector2 GetLocalOffset() => RelativeOffsets[Utils.Clamp(SlotIndex, 0, RelativeOffsets.Length - 1)];
        protected override float GetAngleOffsetDegrees() => AngleOffsetsDegrees[Utils.Clamp(SlotIndex, 0, AngleOffsetsDegrees.Length - 1)];

        protected override void UpdateAttack(YC_RightHoldOut holdout, Projectile holdoutProjectile)
        {
            if (fireCooldown > 0)
                fireCooldown--;

            if (Projectile.owner != Main.myPlayer || fireCooldown > 0 || FindTargetAhead(DetectionRange, DetectionConeDegrees) == null)
                return;

            YC_CBeam.SpawnBeam(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 18f,
                CurrentForwardDirection,
                (int)(Projectile.damage * 1.5f),
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.RightDrone,
                DetectionRange,
                18f,
                BeamDuration,
                false,
                true,
                new Color(255, 192, 88),
                Color.White,
                18f,
                0f,
                2,
                8);

            fireCooldown = BeamDuration + BeamCooldown;
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.2f, Pitch = -0.12f + SlotIndex * 0.02f }, Projectile.Center);
        }
    }
}
