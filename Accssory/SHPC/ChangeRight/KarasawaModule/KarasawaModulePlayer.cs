using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.KarasawaModule
{
    public sealed class KarasawaModulePlayer : ModPlayer
    {
        public bool KarasawaModuleEquipped;

        public override void ResetEffects()
        {
            KarasawaModuleEquipped = false;
        }
    }
}
