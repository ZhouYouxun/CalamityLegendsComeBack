using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_Drone : YC_RightWarshipBase, IYCRightBeamSource
    {
        private const int BeamDuration = 18;
        private const int BeamCooldown = 56;
        private const float DetectionRange = 70f * 16f;
        private const float DetectionConeDegrees = 8f;

        private static readonly Vector2[] RelativeOffsets =
        {
            new(-18f, -4f),
            new(-32f, -16f),
            new(-48f, -30f),
            new(-66f, -46f),
            new(18f, -4f),
            new(32f, -16f),
            new(48f, -30f),
            new(66f, -46f)
        };

        private static readonly float[] AngleOffsetsDegrees =
        {
            -1.5f,
            -3.5f,
            -6f,
            -8.5f,
            1.5f,
            3.5f,
            6f,
            8.5f
        };

        private int fireCooldown;

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_Drone";
        protected override Color AccentColor => new(255, 238, 178);
        protected override Color OverlayColor => new Color(255, 255, 245, 0) * 0.75f;

        protected override float PositionLerp => 0.35f;
        protected override float ScaleBase => 0.88f;
        protected override float ScaleAmplitude => 0.04f;

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
                Projectile.Center + CurrentForwardDirection * 14f,
                CurrentForwardDirection,
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.RightDrone,
                DetectionRange,
                10f,
                BeamDuration,
                false,
                false,
                AccentColor,
                Color.White,
                14f,
                0f,
                1,
                -1);

            fireCooldown = BeamDuration + BeamCooldown;
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.12f, Pitch = -0.18f + SlotIndex * 0.03f }, Projectile.Center);
        }
    }
}
