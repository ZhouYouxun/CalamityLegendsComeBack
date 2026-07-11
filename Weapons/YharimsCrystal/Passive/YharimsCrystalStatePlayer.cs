using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive
{
    internal enum YCWeaponForm
    {
        Blade,
        Crystal
    }

    internal sealed class YharimsCrystalStatePlayer : ModPlayer
    {
        public YCWeaponForm LastWeapon = YCWeaponForm.Crystal;
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
