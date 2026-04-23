using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.S_OVR_ChargeLimitBreaker
{
    public class S_OVR_ChargeLimitBreakerPlayer : ModPlayer
    {
        public bool S_OVR_ChargeLimitBreakerEquipped;

        public override void ResetEffects()
        {
            S_OVR_ChargeLimitBreakerEquipped = false;
        }

        public override void UpdateLifeRegen()
        {
            if (!S_OVR_ChargeLimitBreakerEquipped || Player.lifeRegen <= 0)
                return;

            Player.lifeRegen /= 2;
        }
    }
}
