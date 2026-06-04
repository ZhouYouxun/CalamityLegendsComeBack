using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_MissileBattleship : YC_LeftWarshipBase
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        private bool timerInitialized;

        private ref float AttackTimer => ref Projectile.localAI[0];

        protected override Color AccentColor => new(255, 130, 88);
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_MissileBattleship";

        protected override float PositionLerp => 0.14f;
        protected override float ScaleBase => 1f;
        protected override float ScaleAmplitude => 0.04f;
        protected override float LightStrength => 0.42f;

        protected override Vector2 CalculateLocalOffset(float globalTime)
        {
            float sideSign = SlotIndex == 0 ? -1f : 1f;
            float sideOffset = sideSign * 138f;
            float forwardOffset = (float)System.Math.Sin(globalTime * 1.6f + SlotIndex * 1.2f) * 4f;
            return new Vector2(sideOffset, forwardOffset);
        }

        protected override Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            Vector2 baseForward = holdout.ForwardDirection;

            if (holdout.ManualAimMode)
                return GetManualAimOrDefault(holdout, baseForward);

            NPC target = YC_LeftSquadronHelper.FindPriorityTarget(Owner, Projectile.Center, 1900f, baseForward, 70f, false);
            if (target != null)
                return (target.Center - Projectile.Center).SafeNormalize(baseForward);

            return baseForward.RotatedBy(MathHelper.ToRadians(CurrentLocalOffset.X > 0f ? 2.5f : -2.5f));
        }

        protected override void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            if (!timerInitialized)
            {
                AttackTimer = SlotIndex == 0 ? 10f : 34f;
                timerInitialized = true;
            }

            if (AttackTimer > 0f)
            {
                AttackTimer--;
                return;
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 fireDirection = DesiredAimDirection.SafeNormalize(ForwardDirection);
            if (ManualAimActive && Projectile.owner == Main.myPlayer)
                fireDirection = (Main.MouseWorld - Projectile.Center).SafeNormalize(fireDirection);

            float trackingStrength = ManualAimActive ? 0.08f : 0.03f;
            float muzzleOffset = 22f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + fireDirection * muzzleOffset,
                fireDirection * (ManualAimActive ? 18.5f : 16.5f),
                ModContent.ProjectileType<YC_WarshipArtilleryShell>(),
                (int)(Projectile.damage * 2.35f),
                Projectile.knockBack + 1.1f,
                Projectile.owner,
                trackingStrength,
                0f);

            EmitMuzzleBurst(fireDirection, AccentColor, 5f, 8);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.26f, Pitch = -0.3f + SlotIndex * 0.08f }, Projectile.Center);
            AttackTimer = ManualAimActive ? 36f : 52f;
        }
    }
}
