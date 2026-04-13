using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_Drone : YC_RightWarshipBase, IYCRightBeamSource
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private static readonly Vector2[] RelativeOffsets =
        {
            new(-24f, 12f),
            new(-48f, -10f),
            new(-74f, -34f),
            new(24f, 12f),
            new(48f, -10f),
            new(74f, -34f)
        };

        private static readonly float[] AngleOffsetsDegrees =
        {
            -2f,
            -4.5f,
            -7.5f,
            2f,
            4.5f,
            7.5f
        };

        private ref float AttackTimer => ref Projectile.localAI[0];

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
            if (AttackTimer > 0f)
                AttackTimer--;

            if (Projectile.owner != Main.myPlayer || AttackTimer > 0f)
                return;

            YC_CBeam.SpawnBeam(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 18f,
                CurrentForwardDirection,
                (int)(Projectile.damage * 1.05f),
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.RightDrone,
                1680f,
                14f,
                16,
                false,
                false,
                AccentColor,
                Color.White,
                18f,
                0f,
                -1,
                12,
                2);

            Vector2 pulseSpawn = Projectile.owner == Main.myPlayer
                ? Main.MouseWorld
                : Projectile.Center + CurrentForwardDirection * 28f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                pulseSpawn,
                CurrentForwardDirection * 8.2f,
                ModContent.ProjectileType<YC_WarshipPulse>(),
                (int)(Projectile.damage * 0.72f),
                Projectile.knockBack * 0.45f,
                Projectile.owner,
                1.15f,
                0f);

            EmitMuzzleBurst(CurrentForwardDirection, AccentColor, 4f, 5);
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.14f, Pitch = -0.16f + SlotIndex * 0.03f }, Projectile.Center);
            AttackTimer = 18f;
        }
    }
}
