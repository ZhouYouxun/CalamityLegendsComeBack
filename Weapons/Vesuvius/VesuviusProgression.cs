using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.Vesuvius
{
    internal static class VesuviusProgression
    {
        // Four charge tiers now (was three). Each threshold is the frame at which the matching
        // tier unlocks; tier 0 is the uncharged tap. World progression caps how high you can
        // actually reach (GetMaxStage).
        private static readonly int[] StageFrames =
        {
            50,
            100,
            150,
            200
        };

        public const int ClickLockoutFrames = 30;

        private const string SourceFile = "Weapons/Vesuvius/VesuviusProgression.cs";

        // 大招伤害倍率：肉后/月后/神后三档
        // 大招伤害 = 当前左键伤害 × 本档倍率
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        public static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

        public static int GetMaxStage()
        {
            if (NPC.downedMoonlord)
                return 4;

            if (NPC.downedPlantBoss)
                return 3;

            if (Main.hardMode)
                return 2;

            return 1;
        }

        public static int GetWorldPowerStage()
        {
            if (DownedBossSystem.downedDoG)
                return 5;

            if (NPC.downedMoonlord)
                return 4;

            if (NPC.downedPlantBoss)
                return 3;

            if (Main.hardMode)
                return 2;

            return 1;
        }

        public static int GetChargeStage(int chargeFrames)
        {
            int maxStage = GetMaxStage();
            for (int stage = maxStage; stage >= 1; stage--)
            {
                if (chargeFrames >= StageFrames[stage - 1])
                    return stage;
            }

            return 0;
        }

        public static int GetStageStartFrame(int stage)
        {
            if (stage <= 0)
                return 0;

            return StageFrames[Utils.Clamp(stage - 1, 0, StageFrames.Length - 1)];
        }

        public static float GetStageProgress(int chargeFrames, int stage)
        {
            int maxStage = GetMaxStage();
            if (stage >= maxStage)
                return 1f;

            int currentStart = GetStageStartFrame(stage);
            int nextStart = GetStageStartFrame(stage + 1);
            if (nextStart <= currentStart)
                return 1f;

            return MathHelper.Clamp((chargeFrames - currentStart) / (float)(nextStart - currentStart), 0f, 1f);
        }

        // Per-orb multiplier. Left click now fires a single Arc Nova style orb per release, so
        // these are the whole payload's damage (tier 0 is the weak tap orb).
        public static int GetLeftDamage(int stage, int itemDamage)
        {
            return stage switch
            {
                <= 0 => (int)(itemDamage * 0.34f),
                1 => (int)(itemDamage * 1.10f),
                2 => (int)(itemDamage * 2.05f),
                3 => (int)(itemDamage * 3.10f),
                _ => (int)(itemDamage * 4.20f)
            };
        }

        public static int GetRightDamage(int itemDamage)
        {
            int stage = GetWorldPowerStage();
            return (int)(itemDamage * (0.9f + stage * 0.08f));
        }

        public static Color GetStageColor(int stage)
        {
            return stage switch
            {
                <= 0 => new Color(255, 116, 40),
                1 => new Color(255, 86, 36),
                2 => new Color(255, 132, 43),
                3 => new Color(255, 226, 126),
                _ => new Color(255, 244, 190)
            };
        }
    }
}
