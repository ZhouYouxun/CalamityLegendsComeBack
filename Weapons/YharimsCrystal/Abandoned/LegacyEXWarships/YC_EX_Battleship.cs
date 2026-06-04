using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_Battleship : YC_EX_WarshipBase
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        private int fireCooldown;

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YC_EX_Battleship";

        protected override float FormationRadius => 188f;
        protected override int FormationCount => YC_EX_VIP.BattleshipTotal;
        protected override float FormationAngleOffsetRadians => 0f;
        protected override float TargetRange => 1900f;
        protected override float ScaleBase => 1.03f;
        protected override float ScaleAmplitude => 0.03f;
        protected override float LightStrength => 0.56f;
        protected override Color AccentColor => new(255, 145, 104);

        protected override void OnStateChanged(YC_EX_VIP.EXVipState newState)
        {
            if (newState != YC_EX_VIP.EXVipState.Firing)
                fireCooldown = 0;
        }

        protected override void HandleFiringState(YC_EX_VIP vip, int timer)
        {
            if (fireCooldown > 0)
                fireCooldown--;

            if (Projectile.owner != Main.myPlayer || fireCooldown > 0)
                return;

            Vector2 fireDirection = CurrentForwardDirection.SafeNormalize((Projectile.Center - Owner.Center).SafeNormalize(Vector2.UnitX));
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + fireDirection * 22f,
                fireDirection * 19.5f,
                ModContent.ProjectileType<YC_WarshipArtilleryShell>(),
                (int)(Projectile.damage * 2.45f),
                Projectile.knockBack + 1.2f,
                Projectile.owner,
                0.085f,
                1f);

            EmitMuzzleBurst(fireDirection, 8, 5f);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.28f, Pitch = -0.26f + SlotIndex * 0.08f }, Projectile.Center);
            fireCooldown = SlotIndex == 0 ? 16 : 24;
        }

        protected override void KillOwnedAttackProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_WarshipArtilleryShell>())
                    continue;

                if (other.ai[1] == 1f)
                    other.Kill();
            }
        }
    }
}
