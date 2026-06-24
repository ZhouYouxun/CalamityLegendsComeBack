using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DiseasedPike
{
    public class DiseasedPikePlayer : ModPlayer
    {
        public int ComboIndex = 0;
        public int ComboResetTimer = 0;

        public override void PostUpdate()
        {
            if (ComboResetTimer > 0)
            {
                ComboResetTimer--;
                if (ComboResetTimer == 0)
                {
                    ComboIndex = 0;
                }
            }
        }
    }
}
