using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal static class AzureThunderProgression
    {
        public static bool DownedDesertScourge => DownedBossSystem.downedDesertScourge;
        public static bool DownedEvilTier2 => DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator;
        public static bool DownedWallOfFlesh => Main.hardMode;
        public static bool DownedAnyMech => NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;
        public static bool DownedPlantera => NPC.downedPlantBoss;
        public static bool DownedFishron => NPC.downedFishron;
        public static bool DownedMoonLord => NPC.downedMoonlord;
        public static bool DownedDragonfolly => DownedBossSystem.downedDragonfolly;
        public static bool DownedYharon => DownedBossSystem.downedYharon;

        public static bool RightClickUnlocked => DownedDesertScourge;
        public static bool DodgeUnlocked => DownedEvilTier2;
        public static bool FourSymbolsUnlocked => DownedWallOfFlesh;

        public static int DodgeHealAmount
        {
            get
            {
                if (DownedYharon)
                    return 200;
                if (DownedMoonLord)
                    return 150;
                if (DownedPlantera)
                    return 125;
                if (DownedWallOfFlesh)
                    return 80;

                return 40;
            }
        }

        public static int AutomaticSwordLimit => DownedMoonLord ? 6 : 3;
        public static int GroundSwordLifetime => 36 * 60 + (DownedPlantera ? 13 * 60 : 0);
        public static int FourSymbolsLifeRestore => DownedFishron ? 20 : 0;
        public static float UltimateFinalDamageMultiplier => DownedYharon ? 1.25f : 1f;
    }
}
