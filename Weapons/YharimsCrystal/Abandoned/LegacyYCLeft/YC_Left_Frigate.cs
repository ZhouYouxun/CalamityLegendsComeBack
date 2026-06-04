using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_Frigate : YC_LeftWarshipBase, IYCLeftBeamSource
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private const float BeamLength = 1320f;
        private const float BeamForwardOffset = 18f;
        private const float BeamTurnRate = 0.03f;

        private ref float AttackTimer => ref Projectile.localAI[0];

        protected override Color AccentColor => new(255, 176, 110);
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_Frigate";

        protected override float PositionLerp => 0.28f;
        protected override float ScaleBase => 0.88f;
        protected override float ScaleAmplitude => 0.04f;

        protected override Vector2 CalculateLocalOffset(float globalTime)
        {
            float sideSign = SlotIndex % 2 == 0 ? -1f : 1f;
            float lane = SlotIndex / 2f;
            float phase = globalTime * 2.8f + lane * 0.95f + (sideSign > 0f ? 0.65f : 0f);
            float ribbon = (float)Math.Sin(phase) * 18f;

            float sideOffset = sideSign * (38f + lane * 20f + ribbon);
            float forwardOffset = 74f + lane * 20f + (float)Math.Sin(phase * 2f) * 10f;
            return new Vector2(sideOffset, forwardOffset);
        }

        protected override Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            Vector2 baseForward = holdout.ForwardDirection;
            Vector2 aim = GetManualAimOrDefault(holdout, baseForward);

            if (!holdout.ManualAimMode)
            {
                float sideRatio = MathHelper.Clamp(CurrentLocalOffset.X / 105f, -1f, 1f);
                float disciplinedOffset = MathHelper.ToRadians(8f) * sideRatio;
                float sweep = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.15f + SlotIndex * 0.55f) * 0.06f;
                aim = baseForward.RotatedBy(disciplinedOffset + sweep);
            }

            return aim;
        }

        protected override void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            if (AttackTimer > 0f)
                AttackTimer--;

            if (AttackTimer > 0f || Projectile.owner != Main.myPlayer)
                return;

            float beamWidth = ManualAimActive ? 14f : 10f;
            float beamLength = ManualAimActive ? 1560f : BeamLength;
            int beamLifetime = ManualAimActive ? 16 : 12;

            YC_CBeam.SpawnBeam(
                Projectile.GetSource_FromThis(),
                Projectile.Center + DesiredAimDirection * BeamForwardOffset,
                DesiredAimDirection,
                (int)(Projectile.damage * 0.95f),
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.LeftDrone,
                beamLength,
                beamWidth,
                beamLifetime,
                false,
                false,
                AccentColor,
                Color.White,
                BeamForwardOffset,
                ManualAimActive ? 0.01f : BeamTurnRate,
                -1,
                12,
                2);

            Vector2 pulseDirection = DesiredAimDirection.SafeNormalize(ForwardDirection);
            Vector2 pulseSpawn = ManualAimActive && Projectile.owner == Main.myPlayer
                ? Main.MouseWorld
                : Projectile.Center + pulseDirection * 26f;
            float pulseScale = ManualAimActive ? 1.2f : 0.9f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                pulseSpawn,
                pulseDirection * (ManualAimActive ? 8f : 6.4f),
                ModContent.ProjectileType<YC_WarshipPulse>(),
                (int)(Projectile.damage * 0.68f),
                Projectile.knockBack * 0.4f,
                Projectile.owner,
                pulseScale,
                0f);

            EmitMuzzleBurst(pulseDirection, AccentColor, ManualAimActive ? 4.2f : 3.1f, ManualAimActive ? 6 : 4);
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.14f, Pitch = -0.22f + SlotIndex * 0.04f }, Projectile.Center);
            AttackTimer = ManualAimActive ? 18f : 34f;
        }

        public void OnLeftBeamHit(NPC target, NPC.HitInfo hit, int damageDone, Projectile beamProjectile)
        {
        }

        public float GetBeamLength(float defaultLength, float forwardOffset) => GetManualAimBeamLength(defaultLength, forwardOffset);

        public float GetBeamTurnRateRadians(float defaultTurnRateRadians) => ManualAimActive ? 0.01f : defaultTurnRateRadians;
    }
}
