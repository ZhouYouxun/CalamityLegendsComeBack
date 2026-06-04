using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_LaserCruiser : YC_RightWarshipBase, IYCRightBeamSource
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private static readonly Vector2[] RelativeOffsets =
        {
            new(-96f, -18f),
            new(-128f, -54f),
            new(96f, -18f),
            new(128f, -54f)
        };

        private static readonly float[] AngleOffsetsDegrees =
        {
            -4f,
            -6.5f,
            4f,
            6.5f
        };

        private ref float MissileTimer => ref Projectile.localAI[0];

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
            EnsurePersistentBeam(
                (int)(Projectile.damage * 1.38f),
                24f,
                1800f,
                new Color(255, 220, 145),
                Color.White,
                22f,
                8,
                4);

            if (MissileTimer > 0f)
                MissileTimer--;

            if (Projectile.owner != Main.myPlayer || MissileTimer > 0f)
                return;

            Vector2 right = CurrentForwardDirection.RotatedBy(MathHelper.PiOver2);
            float sideSign = CurrentForwardDirection.X >= 0f ? 1f : -1f;

            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 launchDirection = CurrentForwardDirection.RotatedBy(MathHelper.ToRadians(i * 3f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + CurrentForwardDirection * 20f + right * (sideSign * 7f + i * 5f),
                    launchDirection * Main.rand.NextFloat(10.2f, 12.2f),
                    ModContent.ProjectileType<YC_WarshipMissile>(),
                    (int)(Projectile.damage * 0.94f),
                    Projectile.knockBack + 0.45f,
                    Projectile.owner,
                    0.08f,
                    0f);
            }

            EmitMuzzleBurst(CurrentForwardDirection, AccentColor, 4.6f, 6);
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.22f, Pitch = -0.06f + SlotIndex * 0.03f }, Projectile.Center);
            MissileTimer = 46f;
        }
    }
}
