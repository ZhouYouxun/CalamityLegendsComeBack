using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.OverchargeCore
{
    public class OverchargeCorePlayer : ModPlayer
    {
        public bool OverchargeCoreEquipped;

        public override void ResetEffects()
        {
            OverchargeCoreEquipped = false;
        }

        public override void UpdateLifeRegen()
        {
            if (!OverchargeCoreEquipped || Player.lifeRegen <= 0)
                return;

            Player.lifeRegen /= 2;
        }
    }
}
