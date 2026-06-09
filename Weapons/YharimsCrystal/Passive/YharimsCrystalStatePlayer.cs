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

        public bool BladeEmpowered => BladeEmpowerTimer > 0;
        public bool CrystalEmpowered => CrystalEmpowerTimer > 0;

        public override void PostUpdate()
        {
            if (BladeEmpowerTimer > 0)
                BladeEmpowerTimer--;
            if (CrystalEmpowerTimer > 0)
                CrystalEmpowerTimer--;
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
    }
}
