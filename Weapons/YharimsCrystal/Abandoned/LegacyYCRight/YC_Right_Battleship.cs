using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_Battleship : YC_RightWarshipBase
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private static readonly Vector2[] RelativeOffsets =
        {
            new(-110f, -96f),
            new(110f, -96f)
        };

        private static readonly float[] AngleOffsetsDegrees =
        {
            -3f,
            3f
        };

        private bool timerInitialized;
        private ref float AttackTimer => ref Projectile.localAI[0];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_Battleship";
        protected override Color AccentColor => new(255, 145, 105);

        protected override float PositionLerp => 0.22f;
        protected override float ScaleBase => 1f;
        protected override float ScaleAmplitude => 0.03f;

        protected override Vector2 GetLocalOffset() => RelativeOffsets[Utils.Clamp(SlotIndex, 0, RelativeOffsets.Length - 1)];
        protected override float GetAngleOffsetDegrees() => AngleOffsetsDegrees[Utils.Clamp(SlotIndex, 0, AngleOffsetsDegrees.Length - 1)];

        protected override void UpdateAttack(YC_RightHoldOut holdout, Projectile holdoutProjectile)
        {
            if (!timerInitialized)
            {
                AttackTimer = SlotIndex == 0 ? 8f : 30f;
                timerInitialized = true;
            }

            if (AttackTimer > 0f)
            {
                AttackTimer--;
                return;
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 22f,
                CurrentForwardDirection * 19f,
                ModContent.ProjectileType<YC_WarshipArtilleryShell>(),
                (int)(Projectile.damage * 2.35f),
                Projectile.knockBack + 1.2f,
                Projectile.owner,
                0.08f,
                0f);

            EmitMuzzleBurst(CurrentForwardDirection, AccentColor, 5f, 8);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.26f, Pitch = -0.28f + SlotIndex * 0.08f }, Projectile.Center);
            AttackTimer = 32f;
        }
    }
}
