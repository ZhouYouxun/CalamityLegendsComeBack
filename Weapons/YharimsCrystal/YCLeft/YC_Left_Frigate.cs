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
        private const float BeamLength = 1400f;
        private const float BeamWidth = 10f;
        private const float BeamForwardOffset = 20f;
        private const float BeamTurnRate = 0.03f;

        private bool spawnSoundPlayed;

        protected override Color AccentColor => new(255, 168, 110);
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_Frigate";

        protected override float PositionLerp => 0.28f;
        protected override float ScaleBase => 0.88f;
        protected override float ScaleAmplitude => 0.04f;

        protected override Vector2 CalculateLocalOffset(float globalTime)
        {
            float sideSign = SlotIndex % 2 == 0 ? -1f : 1f;
            float wingIndex = SlotIndex / 2f;
            float phase = globalTime * 2.75f + wingIndex * 0.85f;

            float sideOffset = sideSign * (34f + wingIndex * 22f + (float)Math.Cos(phase * 0.8f) * 15f);
            float forwardOffset = 76f + wingIndex * 12f + (float)Math.Sin(phase) * 26f;
            return new Vector2(sideOffset, forwardOffset);
        }

        protected override Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            Vector2 baseForward = holdout.ForwardDirection;
            Vector2 aim = GetManualAimOrDefault(holdout, baseForward);

            if (!holdout.ManualAimMode)
            {
                float sideRatio = MathHelper.Clamp(CurrentLocalOffset.X / 120f, -1f, 1f);
                float disciplinedOffset = MathHelper.ToRadians(7.5f) * sideRatio;
                float sweep = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.3f + SlotIndex * 0.4f) * 0.04f;
                aim = baseForward.RotatedBy(disciplinedOffset + sweep);
            }

            return aim;
        }

        protected override void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            EnsurePersistentBeam(
                Projectile.damage,
                BeamWidth,
                BeamLength,
                AccentColor,
                Color.White,
                BeamForwardOffset,
                BeamTurnRate,
                12,
                6);

            if (!spawnSoundPlayed && Projectile.owner == Main.myPlayer && HasPersistentBeam())
            {
                spawnSoundPlayed = true;
                SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.13f, Pitch = -0.25f + SlotIndex * 0.03f }, Projectile.Center);
            }
        }

        public void OnLeftBeamHit(NPC target, NPC.HitInfo hit, int damageDone, Projectile beamProjectile)
        {
        }

        public float GetBeamLength(float defaultLength, float forwardOffset) => GetManualAimBeamLength(BeamLength, BeamForwardOffset);

        public float GetBeamTurnRateRadians(float defaultTurnRateRadians) => ManualAimActive ? 0f : BeamTurnRate;
    }
}
