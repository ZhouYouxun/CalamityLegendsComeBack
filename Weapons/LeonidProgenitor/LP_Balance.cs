using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    internal static class LP_Balance
    {
        private const string SourceFile = "Weapons/LeonidProgenitor/LP_Balance.cs";

        // 14 stages:
        // Initial, EoC, Evil, Skeletron, Hardmode, Mech, Plantera, Golem,
        // MoonLord, Providence, Polterghast, DoG, Yharon, Exo+SC
        //
        // LeonidProgenitor is a Yellow-rarity rogue weapon, available from
        // early Hardmode. Stealth strike applies StealthDamageMultiplier (1.2×)
        // on top of these values automatically via CalamityMod.

        private static readonly int[] DefaultLeftClickBaseDamage =
        {
            14,     // Initial
            20,     // Eye of Cthulhu
            28,     // Evil Boss
            36,     // Skeletron
            48,     // Hardmode
            72,     // Any Mechanical Boss
            112,    // Plantera           ← intended first-available stage
            150,    // Golem
            205,    // Moon Lord
            268,    // Providence
            355,    // Polterghast
            468,    // Devourer of Gods
            604,    // Yharon
            7650,   // Exo Mechs & Supreme Calamitas
        };

        public static int GetLeftClickBaseDamage()
        {
            int[] values = RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultLeftClickBaseDamage), DefaultLeftClickBaseDamage);
            int stage = Utils.Clamp(GetCompletedStageIndex(), 0, values.Length - 1);
            return System.Math.Max(1, values[stage]);
        }

        public static int GetInitialLeftClickBaseDamage() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultLeftClickBaseDamage), DefaultLeftClickBaseDamage)[0];

        public static int GetCompletedStageIndex()
        {
            bool[] clearedStages =
            {
                NPC.downedBoss1,
                NPC.downedBoss2,
                NPC.downedBoss3,
                Main.hardMode,
                NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3,
                NPC.downedPlantBoss,
                NPC.downedGolemBoss,
                NPC.downedMoonlord,
                DownedBossSystem.downedProvidence,
                DownedBossSystem.downedPolterghast,
                DownedBossSystem.downedDoG,
                DownedBossSystem.downedYharon,
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas
            };

            int stageIndex = 0;
            for (int i = 0; i < clearedStages.Length; i++)
            {
                if (!clearedStages[i])
                    break;

                stageIndex = i + 1;
            }

            return stageIndex;
        }
    }
}
