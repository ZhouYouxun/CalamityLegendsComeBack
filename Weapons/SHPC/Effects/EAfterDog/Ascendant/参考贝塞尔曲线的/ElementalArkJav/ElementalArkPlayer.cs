using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class ElementalArkPlayer : ModPlayer
    {
        public int slowMotionTimer = 0;

        public override void ResetEffects()
        {
            if (slowMotionTimer > 0)
            {
                slowMotionTimer--;
                Player.moveSpeed *= 0.1f; // 将移动速度减少 90%
            }
        }
    }
}
