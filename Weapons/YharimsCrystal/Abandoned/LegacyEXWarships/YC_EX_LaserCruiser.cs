using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_LaserCruiser : YC_EX_WarshipBase
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        private ref float MissileTimer => ref Projectile.localAI[0];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YC_EX_LaserCruiser";

        protected override float FormationRadius => 136f;
        protected override int FormationCount => YC_EX_VIP.LaserCruiserTotal;
        protected override float FormationAngleOffsetRadians => -MathHelper.PiOver4;
        protected override float TargetRange => 1840f;
        protected override float ScaleBase => 0.98f;
        protected override float ScaleAmplitude => 0.035f;
        protected override float LightStrength => 0.52f;
        protected override Color AccentColor => new(255, 205, 118);

        protected override void OnStateChanged(YC_EX_VIP.EXVipState newState)
        {
            if (newState != YC_EX_VIP.EXVipState.Firing)
            {
                MissileTimer = 0f;
                KillAnchoredBeams();
            }
        }

        protected override void HandleFiringState(YC_EX_VIP vip, int timer)
        {
            EnsurePersistentBeam(
                (int)(Projectile.damage * 1.55f),
                26f,
                1820f,
                new Color(255, 225, 145),
                Color.White,
                24f,
                8,
                4);

            if (MissileTimer > 0f)
                MissileTimer--;

            if (Projectile.owner != Main.myPlayer || MissileTimer > 0f)
                return;

            Vector2 right = CurrentForwardDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 launchDirection = CurrentForwardDirection.RotatedBy(MathHelper.ToRadians(i * 2.5f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + CurrentForwardDirection * 22f + right * (i * 7f),
                    launchDirection * Main.rand.NextFloat(10.8f, 12.8f),
                    ModContent.ProjectileType<YC_WarshipMissile>(),
                    (int)(Projectile.damage * 0.96f),
                    Projectile.knockBack + 0.45f,
                    Projectile.owner,
                    0.085f,
                    1f);
            }

            EmitMuzzleBurst(CurrentForwardDirection, 7, 4.8f);
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.23f, Pitch = -0.08f + SlotIndex * 0.03f }, Projectile.Center);
            MissileTimer = 42f;
        }

        protected override void KillOwnedAttackProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_WarshipMissile>())
                    continue;

                if (other.ai[1] == 1f)
                    other.Kill();
            }
        }
    }
}
