using Terraria;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    public class BalanceBlossomFlux
    {
        private static readonly int[] LeftClickBaseDamage =
        {
            10, 12, 17, 27, 40, 54, 82, 96, 155, 185
        };

        private static readonly int[] RightClickBaseDamage =
        {
            10, 12, 17, 27, 40, 54, 82, 96, 155, 185
        };

        public int GetCompletedStageIndex() => BlossomFluxProgression.StageIndex;

        public int GetLeftClickBaseDamage() => GetValueForStage(LeftClickBaseDamage, GetCompletedStageIndex());

        public int GetRightClickBaseDamage() => GetValueForStage(RightClickBaseDamage, GetCompletedStageIndex());

        private static int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }
    }
}
