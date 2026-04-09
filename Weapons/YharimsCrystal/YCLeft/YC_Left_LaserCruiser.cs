using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_LaserCruiser : YC_LeftWarshipBase, IYCLeftBeamSource
    {
        private const float BeamLength = 1600f;
        private const float BeamWidth = 18f;
        private const float BeamForwardOffset = 24f;
        private const float BeamTurnRate = 0.022f;

        private bool spawnSoundPlayed;

        protected override Color AccentColor => new(255, 220, 130);
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_LaserCruiser";

        protected override float PositionLerp => 0.18f;
        protected override float ScaleBase => 0.96f;
        protected override float ScaleAmplitude => 0.045f;
        protected override float LightStrength => 0.45f;

        protected override Vector2 CalculateLocalOffset(float globalTime)
        {
            float sideSign = SlotIndex % 2 == 0 ? -1f : 1f;
            float columnIndex = SlotIndex / 2f;
            float phase = globalTime * 1.95f + SlotIndex * 0.8f;

            float sideOffset = sideSign * (92f + columnIndex * 16f + (float)Math.Cos(phase) * 12f);
            float forwardOffset = 116f + columnIndex * 12f + (float)Math.Sin(phase * 1.2f) * 24f;
            return new Vector2(sideOffset, forwardOffset);
        }

        protected override Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            Vector2 baseForward = holdout.ForwardDirection;
            Vector2 aim = GetManualAimOrDefault(holdout, baseForward);

            if (!holdout.ManualAimMode)
            {
                float sideRatio = MathHelper.Clamp(CurrentLocalOffset.X / 150f, -1f, 1f);
                float disciplinedOffset = MathHelper.ToRadians(4f) * sideRatio;
                float sweep = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.5f + SlotIndex) * 0.025f;
                aim = baseForward.RotatedBy(disciplinedOffset + sweep);
            }

            return aim;
        }

        protected override void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            EnsurePersistentBeam(
                (int)(Projectile.damage * 1.45f),
                BeamWidth,
                BeamLength,
                new Color(255, 192, 88),
                Color.White,
                BeamForwardOffset,
                BeamTurnRate,
                10,
                5);

            if (!spawnSoundPlayed && Projectile.owner == Main.myPlayer && HasPersistentBeam())
            {
                spawnSoundPlayed = true;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.18f, Pitch = -0.18f + SlotIndex * 0.03f }, Projectile.Center);
            }
        }

        public void OnLeftBeamHit(NPC target, NPC.HitInfo hit, int damageDone, Projectile beamProjectile)
        {
            if (Projectile.owner != Main.myPlayer || !target.active)
                return;

            Vector2 fallbackDirection = beamProjectile.velocity.SafeNormalize(ForwardDirection);
            NPC chainedTarget = YC_LeftSquadronHelper.FindPriorityTarget(Owner, target.Center, 520f, fallbackDirection, 110f, false);
            Vector2 fakeLaserDirection = chainedTarget != null && chainedTarget.whoAmI != target.whoAmI
                ? (chainedTarget.Center - target.Center).SafeNormalize(fallbackDirection)
                : fallbackDirection.RotatedByRandom(0.24f);

            Projectile.NewProjectile(
                beamProjectile.GetSource_FromThis(),
                target.Center,
                fakeLaserDirection * Main.rand.NextFloat(11f, 15f),
                ModContent.ProjectileType<YC_Left_FakeLazer>(),
                Math.Max(1, (int)(beamProjectile.damage * 0.42f)),
                beamProjectile.knockBack * 0.35f,
                beamProjectile.owner);
        }

        public float GetBeamLength(float defaultLength, float forwardOffset) => GetManualAimBeamLength(BeamLength, BeamForwardOffset);

        public float GetBeamTurnRateRadians(float defaultTurnRateRadians) => ManualAimActive ? 0f : BeamTurnRate;
    }
}
