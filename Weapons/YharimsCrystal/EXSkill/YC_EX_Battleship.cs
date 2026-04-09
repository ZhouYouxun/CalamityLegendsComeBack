using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using YCRight = CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_Battleship : YC_EX_WarshipBase
    {
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
                fireDirection * 18f,
                ModContent.ProjectileType<YCRight.YC_Right_HeavyBolt>(),
                (int)(Projectile.damage * 1.9f),
                Projectile.knockBack + 1f,
                Projectile.owner,
                Projectile.whoAmI,
                1f);

            EmitMuzzleBurst(fireDirection, 7, 4.8f);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.24f, Pitch = -0.24f + SlotIndex * 0.08f }, Projectile.Center);
            fireCooldown = 12;
        }

        protected override void KillOwnedAttackProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YCRight.YC_Right_HeavyBolt>())
                    continue;

                if (other.ai[1] == 1f && (int)other.ai[0] == Projectile.whoAmI)
                    other.Kill();
            }
        }
    }
}
