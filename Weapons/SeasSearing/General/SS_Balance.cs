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

        // 大招（指示器炮击）伤害倍率：肉后/月后/神后三档。
        // 大招基准 = 当前左键基础伤害 × 本档倍率；指示光束自身的 1/8 弱化在这个基准之上继续生效。
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        public static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

        public static int GetBaseDamage()
        {
            int[] values = RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultBaseDamage), DefaultBaseDamage);
            int stage = Utils.Clamp(GetCompletedStageIndex(), 0, values.Length - 1);
            return System.Math.Max(1, values[stage]);
        }

        public static int GetInitialBaseDamage() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultBaseDamage), DefaultBaseDamage)[0];

        // 火枪弹被转换为污染弹后的初始速度倍率；只在生成时应用，不会逐帧减速。
        public const float PollutionRoundSpeedMultiplier = 0.8f;

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

        // ── Acid Rain progression (0-3) ──────────────────────────────────────
        // Drives the enemy pollution grade cap, the player Radiation Excitement
        // level cap, and the right-click tier.
        //   0 = no acid rain cleared
        //   1 = Eye of Cthulhu Acid Rain (T1)               DownedBossSystem.downedEoCAcidRain
        //   2 = Aquatic Scourge Acid Rain (T2)              DownedBossSystem.downedAquaticScourgeAcidRain
        //   3 = Aquatic Scourge Acid Rain + Polterghast     (T3 — NOT gated by the Moon Lord any more)
        public static int GetAcidRainTier()
        {
            if (DownedBossSystem.downedAquaticScourgeAcidRain && DownedBossSystem.downedPolterghast) return 3;
            if (DownedBossSystem.downedAquaticScourgeAcidRain)                                        return 2;
            if (DownedBossSystem.downedEoCAcidRain)                                                   return 1;
            return 0;
        }

        // Left-click progression phase (1-6). Highest satisfied phase wins; every
        // phase inherits the behaviours of the phases below it.
        //   1 = start
        //   2 = EoC Acid Rain (T1)
        //   3 = Hardmode
        //   4 = Aquatic Scourge Acid Rain (T2)
        //   5 = post Moon Lord
        //   6 = Acid Rain T3 (Aquatic Scourge Acid Rain + Polterghast) — full auto
        public static int GetLeftClickPhase()
        {
            if (DownedBossSystem.downedAquaticScourgeAcidRain && DownedBossSystem.downedPolterghast) return 6;
            if (NPC.downedMoonlord)                                                                   return 5;
            if (DownedBossSystem.downedAquaticScourgeAcidRain)                                         return 4;
            if (Main.hardMode)                                                                         return 3;
            if (DownedBossSystem.downedEoCAcidRain)                                                    return 2;
            return 1;
        }

        // Rounds per normal burst at each phase. Phase 6 is full-auto and is
        // driven by a rolling counter instead of discrete bursts, but keeps the
        // 5-round cadence value for any shared maths.
        public static int GetPhaseBurstCount(int phase) => phase switch
        {
            1 => 3,
            2 => 4,
            _ => 5   // phases 3, 4, 5, 6
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
