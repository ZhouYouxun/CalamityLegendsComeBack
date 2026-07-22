using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal static class CD_Balance
    {
        private const string SourceFile = "Weapons/CosmicDischarge/CD_Balance.cs";

        // 14 stages, matching the standard BB_Balance layout:
        // Initial, EoC, Evil, Skeletron, Hardmode, Mech, Plantera, Golem,
        // MoonLord, Providence, Polterghast, DoG, Yharon, Exo+SC
        //
        // CosmicDischarge is a CosmicPurple (post-Polterghast) weapon.
        // Values before stage 10 are placeholders — the weapon is not
        // obtainable that early.

        private static readonly int[] DefaultWhipBaseDamage =
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

        private static readonly int[] DefaultSwordBaseDamage =
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

        private static readonly int[] DefaultChainKnifeBaseDamage =
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

        // 大招（裂隙领域）伤害倍率：肉后/月后/神后三档
        // 大招伤害 = 当前左键基础伤害 × 本档倍率
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        public static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

        public static int GetBaseDamage(CosmicDischargeAttackMode mode)
        {
            int[] whip = RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultWhipBaseDamage), DefaultWhipBaseDamage);
            int[] sword = RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultSwordBaseDamage), DefaultSwordBaseDamage);
            int[] chainKnife = RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultChainKnifeBaseDamage), DefaultChainKnifeBaseDamage);
            int stage = Utils.Clamp(GetCompletedStageIndex(), 0, whip.Length - 1);
            return mode switch
            {
                CosmicDischargeAttackMode.Sword      => System.Math.Max(1, sword[Utils.Clamp(stage, 0, sword.Length - 1)]),
                CosmicDischargeAttackMode.ChainKnife => System.Math.Max(1, chainKnife[Utils.Clamp(stage, 0, chainKnife.Length - 1)]),
                _                                    => System.Math.Max(1, whip[stage]),
            };
        }

        public static int GetInitialBaseDamage() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultWhipBaseDamage), DefaultWhipBaseDamage)[0];

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
    }
}
