using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.C_Warships
{
    public class YC_WarshipHoldout : YC_BaseHoldout
    {
        public const float MaxTargetRange = 100f * 16f;
        public const float TargetConeDegrees = 45f;
        public const int EscortCount = 6;
        public const int CruiserCount = 4;
        public const int BattleshipCount = 2;
        public const int LaserShipCount = 1;
        public const int RepairShipCount = 1;

        private bool shipsSpawned;

        protected override float SoundPitch => 0.02f;

        protected override void OnHoldoutAI()
        {
            EnsureShipsExist();
        }

        public override void OnKill(int timeLeft)
        {
            KillOwnedWarships();
        }

        private void EnsureShipsExist()
        {
            if (shipsSpawned || Projectile.owner != Main.myPlayer)
                return;

            shipsSpawned = true;
            KillOwnedWarships();

            for (int i = 0; i < EscortCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_WarshipEscort>(), Projectile.damage, Projectile.knockBack, Projectile.owner, i, Projectile.whoAmI);
            }

            for (int i = 0; i < CruiserCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_WarshipCruiser>(), Projectile.damage, Projectile.knockBack, Projectile.owner, i, Projectile.whoAmI);
            }

            for (int i = 0; i < BattleshipCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_WarshipBattleship>(), Projectile.damage, Projectile.knockBack, Projectile.owner, i, Projectile.whoAmI);
            }

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_WarshipLaserShip>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, Projectile.whoAmI);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_WarshipRepairShip>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, Projectile.whoAmI);
        }

        private void KillOwnedWarships()
        {
            List<int> beamAnchorIndices = new() { Projectile.whoAmI };

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner)
                    continue;

                if (!YC_WarshipHelper.IsOwnedWarshipType(other.type))
                    continue;

                beamAnchorIndices.Add(other.whoAmI);
                other.Kill();
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner)
                    continue;

                if (other.type == ModContent.ProjectileType<YC_CBeam>())
                {
                    YC_CBeam.BeamAnchorKind kind = (YC_CBeam.BeamAnchorKind)(int)other.ai[1];
                    int anchorIndex = (int)other.ai[0];
                    if (kind == YC_CBeam.BeamAnchorKind.RightDrone && beamAnchorIndices.Contains(anchorIndex))
                        other.Kill();
                }
                else if (other.type == ModContent.ProjectileType<YC_WarshipSuperLaser>() && beamAnchorIndices.Contains((int)other.ai[0]))
                    other.Kill();
            }
        }
    }
}
