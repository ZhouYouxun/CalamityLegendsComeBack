using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_LaserCruiser : YC_LeftWarshipBase, IYCLeftBeamSource
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private const float BeamLength = 1640f;
        private const float BeamForwardOffset = 24f;
        private const float BeamTurnRate = 0.024f;

        private bool lastManualState;
        private ref float MissileTimer => ref Projectile.localAI[0];

        protected override Color AccentColor => new(255, 218, 128);
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_LaserCruiser";

        protected override float PositionLerp => 0.18f;
        protected override float ScaleBase => 0.96f;
        protected override float ScaleAmplitude => 0.045f;
        protected override float LightStrength => 0.45f;

        protected override Vector2 CalculateLocalOffset(float globalTime)
        {
            float sideSign = SlotIndex % 2 == 0 ? -1f : 1f;
            float row = SlotIndex / 2f;
            float sideOffset = sideSign * (86f + row * 32f);
            float forwardOffset = 44f - row * 40f;
            return new Vector2(sideOffset, forwardOffset);
        }

        protected override Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            Vector2 baseForward = holdout.ForwardDirection;
            Vector2 aim = GetManualAimOrDefault(holdout, baseForward);

            if (!holdout.ManualAimMode)
            {
                NPC target = YC_LeftSquadronHelper.FindPriorityTarget(Owner, Projectile.Center, 1650f, baseForward, 90f, false);
                if (target != null)
                    aim = (target.Center - Projectile.Center).SafeNormalize(baseForward);
                else
                    aim = baseForward.RotatedBy(MathHelper.ToRadians(CurrentLocalOffset.X > 0f ? 3.5f : -3.5f));
            }

            return aim;
        }

        protected override void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            if (lastManualState != ManualAimActive)
            {
                KillPersistentBeam();
                lastManualState = ManualAimActive;
            }

            EnsurePersistentBeam(
                (int)(Projectile.damage * 1.28f),
                ManualAimActive ? 24f : 17f,
                ManualAimActive ? 1820f : BeamLength,
                ManualAimActive ? new Color(255, 236, 162) : new Color(255, 198, 96),
                Color.White,
                BeamForwardOffset,
                ManualAimActive ? 0.008f : BeamTurnRate,
                ManualAimActive ? 8 : 10,
                4);

            if (MissileTimer > 0f)
                MissileTimer--;

            if (MissileTimer > 0f || Projectile.owner != Main.myPlayer)
                return;

            float homingTurnRate = ManualAimActive ? 0.08f : 0.045f;
            Vector2 fireDirection = DesiredAimDirection.SafeNormalize(ForwardDirection);
            Vector2 right = ForwardDirection.RotatedBy(MathHelper.PiOver2);
            float sideSign = CurrentLocalOffset.X >= 0f ? 1f : -1f;

            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 launchDirection = fireDirection.RotatedBy(MathHelper.ToRadians(i * (ManualAimActive ? 3f : 6f)));
                Vector2 spawnPosition = Projectile.Center + fireDirection * 18f + right * (sideSign * 6f + i * 5f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    launchDirection * Main.rand.NextFloat(9.5f, 11.5f),
                    ModContent.ProjectileType<YC_WarshipMissile>(),
                    (int)(Projectile.damage * 0.92f),
                    Projectile.knockBack + 0.4f,
                    Projectile.owner,
                    homingTurnRate,
                    0f);
            }

            EmitMuzzleBurst(fireDirection, AccentColor, 4.5f, 6);
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.2f, Pitch = -0.1f + SlotIndex * 0.04f }, Projectile.Center);
            MissileTimer = ManualAimActive ? 52f : 82f;
        }

        public void OnLeftBeamHit(NPC target, NPC.HitInfo hit, int damageDone, Projectile beamProjectile)
        {
        }

        public float GetBeamLength(float defaultLength, float forwardOffset) => GetManualAimBeamLength(defaultLength, forwardOffset);

        public float GetBeamTurnRateRadians(float defaultTurnRateRadians) => ManualAimActive ? 0.008f : defaultTurnRateRadians;
    }
}
