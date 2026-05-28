using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.Vesuvius
{
    internal static class VesuviusProgression
    {
        private static readonly int[] AdvancedStageFrames =
        {
            90,
            90,
            120,
            120,
            180
        };

        public const int EarlyStageOneFrames = 180;
        public const int ClickLockoutFrames = 30;

        public static int GetMaxStage()
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
            if (maxStage <= 1)
                return chargeFrames >= EarlyStageOneFrames ? 1 : 0;

            int requiredFrames = 0;
            for (int stage = 1; stage <= maxStage; stage++)
            {
                requiredFrames += AdvancedStageFrames[stage - 1];
                if (chargeFrames < requiredFrames)
                    return stage - 1;
            }

            return maxStage;
        }

        public static int GetStageStartFrame(int stage)
        {
            if (stage <= 0)
                return 0;

            if (GetMaxStage() <= 1)
                return EarlyStageOneFrames;

            int frames = 0;
            for (int i = 0; i < stage && i < AdvancedStageFrames.Length; i++)
                frames += AdvancedStageFrames[i];

            return frames;
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

        public static int GetLeftDamage(int stage, int itemDamage)
        {
            return stage switch
            {
                <= 0 => (int)(itemDamage * 0.58f),
                1 => (int)(itemDamage * 0.42f),
                2 => (int)(itemDamage * 0.62f),
                3 => (int)(itemDamage * 0.72f),
                4 => (int)(itemDamage * 0.54f),
                _ => (int)(itemDamage * 1.15f)
            };
        }

        public static int GetRightDamage(int itemDamage)
        {
            int stage = GetMaxStage();
            return (int)(itemDamage * (0.9f + stage * 0.08f));
        }

        public static Color GetStageColor(int stage)
        {
            return stage switch
            {
                <= 0 => new Color(255, 116, 40),
                1 => new Color(255, 86, 36),
                2 => new Color(255, 132, 43),
                3 => new Color(255, 190, 78),
                4 => new Color(255, 86, 52),
                _ => new Color(215, 66, 255)
            };
        }
    }
}
