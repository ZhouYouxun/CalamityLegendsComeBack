using Terraria;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    internal static class Prime2041Compat
    {
        private static readonly int[] destroyerLaserColors = new int[Main.maxNPCs];

        static Prime2041Compat()
        {
            for (int i = 0; i < destroyerLaserColors.Length; i++)
                destroyerLaserColors[i] = -1;
        }

        public static double BossHealthBoost => 0D;
        public static bool EarlyHardmodeProgressionRework => false;
        public const double EarlyHardmodeProgressionReworkFirstMechStatMultiplierExpert = 1D;
        public const double EarlyHardmodeProgressionReworkSecondMechStatMultiplierExpert = 1D;

        public static int DestroyerLaserColor(this NPC npc)
        {
            return destroyerLaserColors[npc.whoAmI];
        }

        public static void SetDestroyerLaserColor(this NPC npc, int color)
        {
            destroyerLaserColors[npc.whoAmI] = color;
        }

        public static void SyncDestroyerLaserColor(this NPC npc)
        {
            npc.netUpdate = true;
        }

        public static int GetProjectileDamage(this NPC npc, int projType)
        {
            int damage = npc.defDamage > 0 ? npc.defDamage : npc.damage;
            if (Main.masterMode)
                damage = (int)(damage * 0.5f);
            else if (Main.expertMode)
                damage = (int)(damage * 0.67f);

            return damage > 0 ? damage : 1;
        }

        public static void GetNPCDamage(this NPC npc)
        {
            if (npc.damage <= 0 && npc.defDamage > 0)
                npc.damage = npc.defDamage;
        }

        public static double GetExpertDamageMultiplier(this NPC npc) => Main.masterMode ? 3D : Main.expertMode ? 2D : 1D;
    }
}
