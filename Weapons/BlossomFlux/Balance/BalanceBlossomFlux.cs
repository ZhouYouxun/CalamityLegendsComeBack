using Terraria;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    public class BalanceBlossomFlux
    {
        public static readonly string[] StageNames =
        {
            "初始 / Initial",
            "击败克苏鲁之眼 / Eye of Cthulhu",
            "击败蜂王 / Queen Bee",
            "击败血肉墙 / Wall of Flesh",
            "击败任意机械 Boss / Any Mechanical Boss",
            "击败世纪之花 / Plantera",
            "击败瘟疫使者歌莉娅 / Plaguebringer Goliath",
            "击败月亮领主 / Moon Lord",
            "击败噬魂幽花 / Polterghast",
            "击败神明吞噬者 / Devourer of Gods"
        };

        // 左键基础伤害。数组顺序与 StageNames 完全一致。
        private static readonly int[] LeftClickBaseDamage =
        {
            10, // 初始 / Initial
            12, // 击败克苏鲁之眼 / Eye of Cthulhu
            17, // 击败蜂王 / Queen Bee
            27, // 击败血肉墙 / Wall of Flesh
            40, // 击败任意机械 Boss / Any Mechanical Boss
            54, // 击败世纪之花 / Plantera
            82, // 击败瘟疫使者歌莉娅 / Plaguebringer Goliath
            96, // 击败月亮领主 / Moon Lord
            155, // 击败噬魂幽花 / Polterghast
            185 // 击败神明吞噬者 / Devourer of Gods
        };

        // 右键基础伤害。数组顺序与 StageNames 完全一致。
        private static readonly int[] RightClickBaseDamage =
        {
            10, // 初始 / Initial
            12, // 击败克苏鲁之眼 / Eye of Cthulhu
            17, // 击败蜂王 / Queen Bee
            27, // 击败血肉墙 / Wall of Flesh
            40, // 击败任意机械 Boss / Any Mechanical Boss
            54, // 击败世纪之花 / Plantera
            82, // 击败瘟疫使者歌莉娅 / Plaguebringer Goliath
            96, // 击败月亮领主 / Moon Lord
            155, // 击败噬魂幽花 / Polterghast
            185 // 击败神明吞噬者 / Devourer of Gods
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
