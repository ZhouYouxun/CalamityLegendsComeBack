using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.C_Warships
{
    public class YC_WarshipEscort : YC_WarshipBase
    {
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

        private bool burstInitialized;
        private ref float AttackTimer => ref Projectile.localAI[0];
        private ref float BurstShots => ref Projectile.localAI[1];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_Drone";
        protected override Color AccentColor => new(255, 238, 178);
        protected override Color OverlayColor => new Color(255, 255, 245, 0) * 0.75f;
        protected override float PositionLerp => 0.35f;
        protected override float ScaleBase => 0.88f;

        protected override Vector2 GetLocalOffset() => RelativeOffsets[Utils.Clamp(SlotIndex, 0, RelativeOffsets.Length - 1)];
        protected override float GetAngleOffsetDegrees() => AngleOffsetsDegrees[Utils.Clamp(SlotIndex, 0, AngleOffsetsDegrees.Length - 1)];

        protected override void UpdateAttack(YC_WarshipHoldout holdout, Projectile holdoutProjectile)
        {
            if (!burstInitialized)
            {
                AttackTimer = SlotIndex * 2f;
                BurstShots = 0f;
                burstInitialized = true;
            }

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
                8,
                false,
                false,
                AccentColor,
                Color.White,
                18f,
                0f,
                -1,
                5,
                2);

            EmitMuzzleBurst(CurrentForwardDirection, AccentColor, 4f, 5);
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.14f, Pitch = -0.16f + SlotIndex * 0.03f }, Projectile.Center);
            BurstShots++;
            if (BurstShots >= 8f)
            {
                BurstShots = 0f;
                AttackTimer = 25f;
            }
            else
                AttackTimer = 3f;
        }
    }
}
