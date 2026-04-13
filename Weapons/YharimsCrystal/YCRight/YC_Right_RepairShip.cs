using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using YCLeft = CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_RepairShip : YC_RightWarshipBase
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        private bool shieldsSpawned;
        private ref float SupportTimer => ref Projectile.localAI[0];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_RepairShip";
        protected override Color AccentColor => new(110, 235, 255);

        protected override float PositionLerp => 0.22f;
        protected override float ScaleBase => 0.94f;
        protected override float ScaleAmplitude => 0.03f;
        protected override float LightStrength => 0.55f;

        protected override Vector2 GetLocalOffset() => new(0f, -118f);
        protected override float GetAngleOffsetDegrees() => 0f;

        protected override void UpdateAttack(YC_RightHoldOut holdout, Projectile holdoutProjectile)
        {
            if (!shieldsSpawned && Projectile.owner == Main.myPlayer)
            {
                shieldsSpawned = true;
                for (int i = 0; i < YC_Right_RepairShield.ShieldCount; i++)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Owner.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<YC_Right_RepairShield>(),
                        0,
                        0f,
                        Projectile.owner,
                        i,
                        ParentHoldoutIndex);
                }

                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.16f, Pitch = 0.18f }, Projectile.Center);
                SupportTimer = 52f;
            }

            if (SupportTimer > 0f)
            {
                SupportTimer--;
                return;
            }

            SupportTimer = 74f;
            if (Projectile.owner != Main.myPlayer)
                return;

            int missingLife = Owner.statLifeMax2 - Owner.statLife;
            if (missingLife <= 0)
                return;

            Vector2 launchDirection = (Owner.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + launchDirection * 12f,
                launchDirection * 4.8f,
                ModContent.ProjectileType<YCLeft.YC_Left_RepairBolt>(),
                0,
                0f,
                Projectile.owner);

            EmitMuzzleBurst(launchDirection, AccentColor, 2.8f, 5);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.16f, Pitch = 0.32f }, Projectile.Center);
        }
    }
}
