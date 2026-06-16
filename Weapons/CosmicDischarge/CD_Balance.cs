using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal static class CD_Balance
    {
        // 14 stages, matching the standard BB_Balance layout:
        // Initial, EoC, Evil, Skeletron, Hardmode, Mech, Plantera, Golem,
        // MoonLord, Providence, Polterghast, DoG, Yharon, Exo+SC
        //
        // CosmicDischarge is a CosmicPurple (post-Polterghast) weapon.
        // Values before stage 10 are placeholders — the weapon is not
        // obtainable that early.

        public static readonly int[] WhipBaseDamage =
        {
            35,     // Initial
            48,     // Eye of Cthulhu
            66,     // Evil Boss
            88,     // Skeletron
            120,    // Hardmode
            175,    // Any Mechanical Boss
            250,    // Plantera
            335,    // Golem
            450,    // Moon Lord
            595,    // Providence
            790,    // Polterghast      ← intended first-available stage
            1060,   // Devourer of Gods
            1360,   // Yharon
            17500,  // Exo Mechs & Supreme Calamitas
        };

        public static readonly int[] SwordBaseDamage =
        {
            42,     // Initial
            58,     // Eye of Cthulhu
            80,     // Evil Boss
            106,    // Skeletron
            144,    // Hardmode
            210,    // Any Mechanical Boss
            300,    // Plantera
            402,    // Golem
            540,    // Moon Lord
            714,    // Providence
            948,    // Polterghast
            1272,   // Devourer of Gods
            1632,   // Yharon
            21000,  // Exo Mechs & Supreme Calamitas
        };

        public static readonly int[] ChainKnifeBaseDamage =
        {
            28,     // Initial
            38,     // Eye of Cthulhu
            52,     // Evil Boss
            70,     // Skeletron
            95,     // Hardmode
            138,    // Any Mechanical Boss
            198,    // Plantera
            265,    // Golem
            356,    // Moon Lord
            470,    // Providence
            624,    // Polterghast
            836,    // Devourer of Gods
            1072,   // Yharon
            13800,  // Exo Mechs & Supreme Calamitas
        };

        public static int GetBaseDamage(CosmicDischargeAttackMode mode)
        {
            int stage = Utils.Clamp(GetCompletedStageIndex(), 0, WhipBaseDamage.Length - 1);
            return mode switch
            {
                CosmicDischargeAttackMode.Sword      => System.Math.Max(1, SwordBaseDamage[stage]),
                CosmicDischargeAttackMode.ChainKnife => System.Math.Max(1, ChainKnifeBaseDamage[stage]),
                _                                    => System.Math.Max(1, WhipBaseDamage[stage]),
            };
        }

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
