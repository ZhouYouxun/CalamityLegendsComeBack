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

        // 大招（狮子座回响）伤害倍率：肉后/月后/神后三档
        // 大招伤害 = 当前左键基础伤害 × 本档倍率
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        public static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

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
                if (clearedStages[i])
                    stageIndex = i + 1;
            }

            return stageIndex;
        }
    }
}
