using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_RepairShip : YC_LeftWarshipBase
    {
        private bool timerInitialized;

        protected override Color AccentColor => new(110, 255, 220);
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_RepairShip";

        protected override float PositionLerp => 0.2f;
        protected override float ScaleBase => 0.92f;
        protected override float ScaleAmplitude => 0.03f;
        protected override float LightStrength => 0.48f;

        private ref float SupportTimer => ref Projectile.localAI[0];

        protected override Vector2 CalculateLocalOffset(float globalTime)
        {
            float phase = globalTime * 1.9f;
            float sideOffset = (float)Math.Sin(phase) * 18f;
            float forwardOffset = -76f + (float)Math.Cos(phase * 1.4f) * 10f;
            return new Vector2(sideOffset, forwardOffset);
        }

        protected override Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            return (Owner.Center - Projectile.Center).SafeNormalize(holdout.ForwardDirection);
        }

        protected override void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            if (!timerInitialized)
            {
                SupportTimer = 45f;
                timerInitialized = true;
            }

            if (SupportTimer > 0f)
            {
                SupportTimer--;
                return;
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            int missingLife = Owner.statLifeMax2 - Owner.statLife;
            SupportTimer = missingLife > 0 ? 96f : 42f;

            if (missingLife <= 0)
                return;

            Vector2 launchDirection = (Owner.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + launchDirection * 12f,
                launchDirection * 4.5f,
                ModContent.ProjectileType<YC_Left_RepairBolt>(),
                0,
                0f,
                Projectile.owner);

            EmitMuzzleBurst(launchDirection, AccentColor, 2.8f, 5);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.16f, Pitch = 0.32f }, Projectile.Center);
        }
    }
}
