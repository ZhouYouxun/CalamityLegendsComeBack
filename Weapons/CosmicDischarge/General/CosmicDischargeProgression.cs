using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal static class CosmicDischargeProgression
    {
        public static bool DownedAnyMech => NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;
        public static bool DownedPlantera => NPC.downedPlantBoss;
        public static bool DownedMoonLord => NPC.downedMoonlord;
        public static bool DownedDoG => DownedBossSystem.downedDoG;
        public static bool DownedYharon => DownedBossSystem.downedYharon;

        public static int QuickDrawRiftBombCount
        {
            get
            {
                if (DownedYharon)
                    return 5;
                if (DownedDoG)
                    return 4;
                if (DownedMoonLord)
                    return 3;
                if (DownedPlantera)
                    return 2;
                if (DownedAnyMech)
                    return 1;

                return 0;
            }
        }

        public static int QuickDrawRiftBombBursts => DownedMoonLord ? 3 : DownedPlantera ? 2 : 1;

        public static float QuickDrawRiftBombDamageFactor
        {
            get
            {
                if (DownedYharon)
                    return 0.34f;
                if (DownedDoG)
                    return 0.3f;
                if (DownedMoonLord)
                    return 0.26f;

                return 0.22f;
            }
        }
    }
}
