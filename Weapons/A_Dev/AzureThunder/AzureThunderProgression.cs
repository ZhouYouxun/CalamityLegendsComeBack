using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 集中管理青霆剑的进度门槛，避免各个弹幕文件散落 Boss 判定。
    internal static class AzureThunderProgression
    {
        // 灾厄和原版 Boss 进度映射：后续属性、技能和 tooltip 都依赖这些入口。
        public static bool DownedDesertScourge => DownedBossSystem.downedDesertScourge;
        public static bool DownedEvilTier2 => DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator;
        public static bool DownedWallOfFlesh => Main.hardMode;
        public static bool DownedAnyMech => NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;
        public static bool DownedPlantera => NPC.downedPlantBoss;
        public static bool DownedFishron => NPC.downedFishron;
        public static bool DownedMoonLord => NPC.downedMoonlord;
        public static bool DownedDragonfolly => DownedBossSystem.downedDragonfolly;
        public static bool DownedYharon => DownedBossSystem.downedYharon;

        // 主动/被动解锁节点：右键、闪避、自动地剑分别在不同阶段开放。
        public static bool RightClickUnlocked => DownedDesertScourge;
        public static bool DodgeUnlocked => DownedEvilTier2;
        public static bool FourSymbolsUnlocked => DownedWallOfFlesh;

        public static int DodgeHealAmount
        {
            get
            {
                // 闪避回血量随主要流程提高，终盘提供最大容错。
                if (DownedYharon)
                    return 200;
                if (DownedMoonLord)
                    return 150;
                if (DownedPlantera)
                    return 125;
                if (DownedWallOfFlesh)
                    return 80;

                return 40;
            }
        }

        // 月总后自动地剑上限提升到 6，否则维持 3 把。
        public static int AutomaticSwordLimit => DownedMoonLord ? 6 : 3;

        // 地剑基础存在 36 秒，世纪之花后额外延长 13 秒。
        public static int GroundSwordLifetime => 36 * 60 + (DownedPlantera ? 13 * 60 : 0);

        // 猪鲨后右键/自动生成地剑可以顺带回复生命。
        public static int FourSymbolsLifeRestore => DownedFishron ? 20 : 0;

        // 犽戎后终极技巨剑和右键终结雷击获得最终倍率强化。
        public static float UltimateGrandSwordDamageFactor => DownedYharon ? 3.6f : 3f;
        public static float UltimateRightClickFinalDamageFactor => DownedYharon ? 7.2f : 6f;

        // 右键消耗的雷息层数会在终极模式中额外放大最后一击。
        public static float UltimateRightClickChargeDamageBonus => DownedYharon ? 0.9f : 0.3f;
    }
}
