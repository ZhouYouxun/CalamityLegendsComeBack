using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive
{
    internal enum YCWeaponForm
    {
        Blade,
        Crystal
    }

    internal sealed class YharimsCrystalStatePlayer : ModPlayer
    {
        public YCWeaponForm LastWeapon = YCWeaponForm.Blade;
        public int BladeEmpowerTimer;
        public int CrystalEmpowerTimer;

        public int RightClickCooldown;
        public int LeftClickCooldown;
        public int AuricJudgementCharges;

        public bool BladeEmpowered => BladeEmpowerTimer > 0;
        public bool CrystalEmpowered => CrystalEmpowerTimer > 0;
        public bool HasAuricJudgement => AuricJudgementCharges > 0;

        public override void PostUpdate()
        {
            if (BladeEmpowerTimer > 0)
                BladeEmpowerTimer--;
            if (CrystalEmpowerTimer > 0)
                CrystalEmpowerTimer--;
            if (RightClickCooldown > 0)
                RightClickCooldown--;
            if (LeftClickCooldown > 0)
                LeftClickCooldown--;

            EnsureAuricJudgementMatrix();
            EnsureBackgroundPassive();
        }

        private void EnsureAuricJudgementMatrix()
        {
            if (AuricJudgementCharges <= 0 || Player.whoAmI != Main.myPlayer)
                return;

            int matrixType = ModContent.ProjectileType<YCAuricJudgementMatrix>();
            if (Player.ownedProjectileCounts[matrixType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_Misc("YharimsCrystalAuricJudgementMatrix"),
                Player.Center,
                Vector2.Zero,
                matrixType,
                0,
                0f,
                Player.whoAmI);
        }

        private void EnsureBackgroundPassive()
        {
            // Only for the owning player, only while holding the crystal
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (Player.HeldItem == null || Player.HeldItem.IsAir)
                return;
            if (Player.HeldItem.type != ModContent.ItemType<NewLegendYharimsCrystal>())
                return;

            int bgBladeType = ModContent.ProjectileType<YC_BackgroundBlade>();
            int bgCrystalType = ModContent.ProjectileType<YC_BackgroundCrystal>();

            if (LastWeapon == YCWeaponForm.Crystal)
            {
                // Crystal form: spawn/maintain background blade
                if (Player.ownedProjectileCounts[bgBladeType] <= 0)
                {
                    Projectile.NewProjectile(
                        Player.GetSource_Misc("YharimsCrystalBackgroundBlade"),
                        Player.Center,
                        Vector2.Zero,
                        bgBladeType,
                        0,
                        0f,
                        Player.whoAmI);
                }
                // Kill background crystal if it's still alive
                if (Player.ownedProjectileCounts[bgCrystalType] > 0)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.active && p.owner == Player.whoAmI && p.type == bgCrystalType)
                            p.Kill();
                    }
                }
            }
            else
            {
                // Blade form: spawn/maintain background crystal
                if (Player.ownedProjectileCounts[bgCrystalType] <= 0)
                {
                    Projectile.NewProjectile(
                        Player.GetSource_Misc("YharimsCrystalBackgroundCrystal"),
                        Player.Center,
                        Vector2.Zero,
                        bgCrystalType,
                        0,
                        0f,
                        Player.whoAmI);
                }
                // Kill background blade if it's still alive
                if (Player.ownedProjectileCounts[bgBladeType] > 0)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.active && p.owner == Player.whoAmI && p.type == bgBladeType)
                            p.Kill();
                    }
                }
            }
        }

        public void SetLastWeapon(YCWeaponForm form)
        {
            LastWeapon = form;
        }

        public void EmpowerLastWeapon(int frames)
        {
            if (LastWeapon == YCWeaponForm.Blade)
                BladeEmpowerTimer = System.Math.Max(BladeEmpowerTimer, frames);
            else
                CrystalEmpowerTimer = System.Math.Max(CrystalEmpowerTimer, frames);
        }

        public void GrantAuricJudgementChain(int charges)
        {
            AuricJudgementCharges = System.Math.Max(AuricJudgementCharges, charges);
        }

        public bool TryConsumeAuricJudgement()
        {
            if (AuricJudgementCharges <= 0)
                return false;

            AuricJudgementCharges--;
            return true;
        }
    }
}
