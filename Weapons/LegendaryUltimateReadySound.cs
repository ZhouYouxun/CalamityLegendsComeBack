using Terraria;
using Terraria.Audio;

namespace CalamityLegendsComeBack.Weapons
{
    internal static class LegendaryUltimateReadySound
    {
        private static readonly SoundStyle ReadySound = new("CalamityLegendsComeBack/Sound/SHPC/PH2禅技准备就绪")
        {
            Volume = 0.95f,
            Pitch = 0f,
            MaxInstances = 3
        };

        public static void PlayIfReadyTransition(Player player, ref bool wasReady, bool ready)
        {
            if (player.whoAmI == Main.myPlayer && ready && !wasReady)
                SoundEngine.PlaySound(ReadySound, player.Center);

            wasReady = ready;
        }
    }
}
