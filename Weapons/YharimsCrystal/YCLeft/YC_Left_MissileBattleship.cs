using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_MissileBattleship : YC_LeftWarshipBase
    {
        private bool timerInitialized;

        protected override Color AccentColor => new(255, 130, 88);
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_MissileBattleship";

        protected override float PositionLerp => 0.14f;
        protected override float ScaleBase => 1f;
        protected override float ScaleAmplitude => 0.04f;
        protected override float LightStrength => 0.42f;

        private ref float AttackTimer => ref Projectile.localAI[0];
        private ref float BurstCounter => ref Projectile.localAI[1];

        protected override Vector2 CalculateLocalOffset(float globalTime)
        {
            float sideSign = SlotIndex == 0 ? -1f : 1f;
            float phase = globalTime * 1.35f + SlotIndex * MathHelper.Pi;

            float sideOffset = sideSign * (126f + (float)Math.Sin(phase * 0.9f) * 22f);
            float forwardOffset = 148f + (float)Math.Cos(phase * 1.15f) * 20f;
            return new Vector2(sideOffset, forwardOffset);
        }

        protected override Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            Vector2 baseForward = holdout.ForwardDirection;
            Vector2 aim = GetManualAimOrDefault(holdout, baseForward);

            if (!holdout.ManualAimMode)
            {
                NPC target = YC_LeftSquadronHelper.FindPriorityTarget(Owner, Projectile.Center, 1250f, baseForward, 80f);
                if (target != null)
                    aim = (target.Center - Projectile.Center).SafeNormalize(baseForward);
                else
                    aim = baseForward.RotatedBy(MathHelper.ToRadians(CurrentLocalOffset.X > 0f ? 4f : -4f));
            }

            return aim;
        }

        protected override void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            if (!timerInitialized)
            {
                AttackTimer = 8f + SlotIndex * 10f;
                timerInitialized = true;
            }

            if (AttackTimer > 0f)
            {
                AttackTimer--;
                return;
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            if (ManualAimActive)
            {
                Vector2 launchDirection = DesiredAimDirection;
                Vector2 manualRightDirection = ForwardDirection.RotatedBy(MathHelper.PiOver2);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + launchDirection * 18f + manualRightDirection * (CurrentLocalOffset.X > 0f ? 6f : -6f),
                    launchDirection * Main.rand.NextFloat(12f, 13.5f),
                    ModContent.ProjectileType<YC_Left_Rocket>(),
                    (int)(Projectile.damage * 1.15f),
                    Projectile.knockBack + 0.5f,
                    Projectile.owner);

                EmitMuzzleBurst(launchDirection, AccentColor, 4.2f, 5);
                SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.17f, Pitch = -0.08f + SlotIndex * 0.05f }, Projectile.Center);

                BurstCounter = 0f;
                AttackTimer = 5f;
                return;
            }

            NPC target = YC_LeftSquadronHelper.FindPriorityTarget(Owner, Projectile.Center, 1450f, DesiredAimDirection, 95f, false);
            Vector2 rightDirection = ForwardDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 rocketDirection = target != null
                ? Vector2.Lerp(DesiredAimDirection, (target.Center - Projectile.Center).SafeNormalize(DesiredAimDirection), 0.42f).SafeNormalize(DesiredAimDirection)
                : DesiredAimDirection;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + rocketDirection * 18f + rightDirection * (CurrentLocalOffset.X > 0f ? 6f : -6f),
                rocketDirection * Main.rand.NextFloat(11.5f, 13.5f),
                ModContent.ProjectileType<YC_Left_Rocket>(),
                (int)(Projectile.damage * 1.15f),
                Projectile.knockBack + 0.5f,
                Projectile.owner);

            EmitMuzzleBurst(rocketDirection, AccentColor, 4.2f, 6);
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.2f, Pitch = -0.12f + SlotIndex * 0.05f }, Projectile.Center);

            BurstCounter++;
            AttackTimer = BurstCounter % 3f == 0f ? 28f : 10f;
        }
    }
}
