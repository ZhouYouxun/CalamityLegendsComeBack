using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Systems
{
    /// <summary>
    /// 传奇武器大招伤害的统一档位判定。所有大招伤害都遵循同一套公式：
    /// 大招伤害 = 当前左键基础伤害 × 本档倍率，倍率分肉后 / 月后 / 神后三档。
    /// 各武器的倍率数值仍然写在自己的 Balance 文件里（方便 RuntimeBalanceData 单独热调），
    /// 这里只负责"当前处于哪一档"以及安全取值。
    /// </summary>
    internal static class UltimateDamageTier
    {
        /// <summary>肉后 = 0，月后 = 1，神后（亵渎天神之后） = 2。</summary>
        public static int GetTier()
        {
            if (DownedBossSystem.downedProvidence)
                return 2;
            if (NPC.downedMoonlord)
                return 1;
            return 0;
        }

        /// <summary>按当前档位从倍率表里取值，表长不足时钳制到最后一项。</summary>
        public static float Resolve(float[] multipliers)
        {
            if (multipliers == null || multipliers.Length == 0)
                return 1f;

            return multipliers[Utils.Clamp(GetTier(), 0, multipliers.Length - 1)];
        }

        /// <summary>Balance 文件的标准接线方式：既走 RuntimeBalanceData 热调，又走统一档位。</summary>
        public static float Resolve(string sourceRelativePath, string fieldName, float[] defaults)
        {
            return Resolve(RuntimeBalanceData.GetSourceFloatArray(sourceRelativePath, fieldName, defaults));
        }
    }
}
