using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal static class SS_Balance
    {
        private const string SourceFile = "Weapons/SeasSearing/General/SS_Balance.cs";

        // 14 damage stages: Initial, EoC, Evil, Skeletron, Hardmode, Mech, Plantera, Golem,
        // MoonLord, Providence, Polterghast, DoG, Yharon, Exo+SC
        private static readonly int[] DefaultBaseDamage =
        {
            18,     // Initial
            26,     // Eye of Cthulhu
            36,     // Evil Boss
            48,     // Skeletron
            64,     // Hardmode
            96,     // Any Mechanical Boss  ← intended first-available stage
            140,    // Plantera
            188,    // Golem
            256,    // Moon Lord
            332,    // Providence
            444,    // Polterghast
            586,    // Devourer of Gods
            756,    // Yharon
            9550,   // Exo Mechs & Supreme Calamitas
        };

        public static int GetBaseDamage()
        {
            int[] values = RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultBaseDamage), DefaultBaseDamage);
            int stage = Utils.Clamp(GetCompletedStageIndex(), 0, values.Length - 1);
            return System.Math.Max(1, values[stage]);
        }

        public static int GetInitialBaseDamage() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultBaseDamage), DefaultBaseDamage)[0];

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
                if (clearedStages[i])
                    stageIndex = i + 1;
            }
            return stageIndex;
        }

        // Left-click progression stage (0-5), based on Acid Rain & key bosses.
        // Stage 5 = Aquatic Scourge Acid Rain beaten post-Moon Lord (T3 proxy).
        // Each stage implies all previous stages are also met.
        public static int GetLeftClickStage()
        {
            if (DownedBossSystem.downedAquaticScourgeAcidRain && NPC.downedMoonlord) return 5;
            if (NPC.downedMoonlord)                                                  return 4;
            if (DownedBossSystem.downedAquaticScourgeAcidRain)                       return 3;
            if (Main.hardMode)                                                       return 2;
            if (DownedBossSystem.downedEoCAcidRain)                                  return 1;
            return 0;
        }

        // How many rounds in a normal left-click burst at this stage.
        public static int GetBurstCount(int stage) => stage switch
        {
            0      => 1,
            1 or 2 => 2,
            3      => 3,
            _      => 4   // stages 4-5
        };

        // Legacy helper used by SeasSearing.cs GetProgressionStage().
        public static int GetProgressionStage()
        {
            if (NPC.downedMoonlord)   return 3;
            if (NPC.downedPlantBoss)  return 2;
            if (Main.hardMode)        return 1;
            return 0;
        }
    }
}
