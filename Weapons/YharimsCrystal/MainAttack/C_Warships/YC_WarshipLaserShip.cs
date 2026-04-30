using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.C_Warships
{
    public class YC_WarshipLaserShip : YC_WarshipBase
    {
        private ref float PulseTimer => ref Projectile.localAI[0];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_LaserCruiser";
        protected override Color AccentColor => new(255, 236, 160);
        protected override Color OverlayColor => new Color(255, 250, 220, 0) * 0.85f;
        protected override float PositionLerp => 0.2f;
        protected override float ScaleBase => 1.05f;
        protected override float LightStrength => 0.72f;
        protected override int IdleDustInterval => 6;

        protected override Vector2 GetLocalOffset() => new(0f, 126f);
        protected override float GetAngleOffsetDegrees() => 0f;

        protected override void UpdateAttack(YC_WarshipHoldout holdout, Projectile holdoutProjectile)
        {
            EnsureSuperLaser();

            if (PulseTimer > 0f)
                PulseTimer--;

            if (Projectile.owner != Main.myPlayer || PulseTimer > 0f)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 28f,
                CurrentForwardDirection * 9.6f,
                ModContent.ProjectileType<YC_WarshipPulse>(),
                (int)(Projectile.damage * 0.95f),
                Projectile.knockBack * 0.45f,
                Projectile.owner,
                1.55f,
                0f);

            EmitMuzzleBurst(CurrentForwardDirection, AccentColor, 5.2f, 8);
            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.2f, Pitch = 0.1f }, Projectile.Center);
            PulseTimer = 24f;
        }

        private void EnsureSuperLaser()
        {
            if (Projectile.owner != Main.myPlayer || HasActiveSuperLaser())
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + CurrentForwardDirection * 26f,
                CurrentForwardDirection,
                ModContent.ProjectileType<YC_WarshipSuperLaser>(),
                (int)(Projectile.damage * 1.95f),
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI);
        }

        private bool HasActiveSuperLaser()
        {
            int laserType = ModContent.ProjectileType<YC_WarshipSuperLaser>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == Projectile.owner && other.type == laserType && (int)other.ai[0] == Projectile.whoAmI)
                    return true;
            }

            return false;
        }
    }
}
