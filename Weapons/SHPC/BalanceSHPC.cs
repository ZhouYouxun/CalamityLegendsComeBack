using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class BalanceSHPC
    {
        // 1. 左键基础倍率/伤害成长：平衡时先看这个进度表和下面的左键基础值。
        public static readonly string[] StageNames =
        {
            "Initial",
            "Eye of Cthulhu",
            "Evil Boss",
            "Skeletron",
            "Hardmode",
            "Any Mechanical Boss",
            "Plantera",
            "Golem",
            "Moon Lord",
            "Providence",
            "Polterghast",
            "Devourer of Gods",
            "Yharon",
            "Exo Mechs and Supreme Calamitas"
        };

        public static readonly int[] LeftClickProgressDamage =
        {
            15, // 15Initial / 初始
            24, // 24Eye of Cthulhu / 克苏鲁之眼
            28, // 30Evil Boss / 腐化或猩红 Boss T2
            36, // 42Skeletron / 骷髅王
            40, // 54Hardmode / 困难模式
            45, // 72Any Mechanical Boss / 任意机械 Boss
            50, // 90Plantera / 世纪之花
            54, // 120Golem / 石巨人
            55, // 180Moon Lord / 月亮领主
            60, // 240Providence / 亵渎天神
            72, // 300Polterghast / 噬魂幽花
            81, // 360Devourer of Gods / 神明吞噬者
            90, // 500Yharon / 犽戎
            100 // 750Exo Mechs and Supreme Calamitas / 星流巨械与至尊灾厄
        };

        // 2. 右键基础倍率/伤害成长：顺序和 StageNames 完全一致。
        public static readonly int[] RightClickBaseDamage =
        {
            15, // 6Initial / 初始
            21, // 8Eye of Cthulhu / 克苏鲁之眼
            27, // 9Evil Boss / 腐化或猩红 Boss T2
            33, // 11Skeletron / 骷髅王
            45, // 17Hardmode / 困难模式
            54, // 24Any Mechanical Boss / 任意机械 Boss
            66, // 32Plantera / 世纪之花
            75, // 40Golem / 石巨人
            95, // 54Moon Lord / 月亮领主
            125, // 66Providence / 亵渎天神
            155, // 77Polterghast / 噬魂幽花
            200, // 90Devourer of Gods / 神明吞噬者
            250, // 100Yharon / 犽戎
            333 // 150Exo Mechs and Supreme Calamitas / 星流巨械与至尊灾厄
        };

        // 3. 左键材料控制倍率：按 SHPC EffectID 索引，材料会乘到左键基础伤害上。
        public static readonly float[] LeftClickMaterialDamageMultipliers =
        {
            0f, // EffectID 0: Unused gap / 未使用空位
            0.46f, // EffectID 1: Energy Core / 钨钢能量核心
            0.46f, // EffectID 2: Stormlion Mandible / 风暴之颚
            0.58f, // EffectID 3: Sulphuric Scale / 硫磺鳞片
            0.80f, // EffectID 4: Purified Gel / 纯净凝胶
            1.02f, // EffectID 5: Essence of Havoc / 混沌精华
            0.84f, // EffectID 6: Essence of Eleum / 冰精华
            1.03f, // EffectID 7: Essence of Sunlight / 日光精华
            1.33f, // EffectID 8: Titan Heart / 泰坦之心
            0.80f, // EffectID 9: Soul of Light / 光明之魂
            0.78f, // EffectID 10: Soul of Night / 暗影之魂
            0.90f, // EffectID 11: Soul of Flight / 飞翔之魂
            0.91f, // EffectID 12: Soul of Fright / 恐惧之魂
            1.08f, // EffectID 13: Soul of Might / 力量之魂
            0.80f, // EffectID 14: Soul of Sight / 视域之魂
            1.05f, // EffectID 15: Living Shard / 生命碎片
            0f, // EffectID 16: Unused gap / 未使用空位
            1.05f, // EffectID 17: Depth Cells / 深渊细胞
            1.44f, // EffectID 18: Plague Cell Canister / 瘟疫细胞罐
            1.13f, // EffectID 19: Ashes of Calamity / 灾厄尘
            0f, // EffectID 20: Beetle Husk / 甲虫外壳，未实现
            1.72f, // EffectID 21: Solar Fragment / 日曜碎片
            1.72f, // EffectID 22: Vortex Fragment / 星旋碎片
            1.50f, // EffectID 23: Nebula Fragment / 星云碎片
            1.73f, // EffectID 24: Stardust Fragment / 星尘碎片
            1.94f, // EffectID 25: Meld Blob / 冥思溶剂
            2.25f, // EffectID 26: Unholy Essence / 浊火精华
            0f, // EffectID 27: Unused gap / 未使用空位
            2.00f, // EffectID 28: Divine Geode / 神圣晶石
            2.48f, // EffectID 29: Bloodstone Core / 血神核心
            2.70f, // EffectID 30: Ruinous Soul / 毁灭之灵
            2.24f, // EffectID 31: Necroplasm / 灵质
            1.97f, // EffectID 32: Dark Plasma / 暗离子体
            2.30f, // EffectID 33: Twisting Nether / 扭曲虚空
            2.85f, // EffectID 34: Endothermic Energy / 恒温能量
            2.07f, // EffectID 35: Nightmare Fuel / 梦魇魔能
            2.42f, // EffectID 36: Ascendant Spirit Essence / 化神魂精
            3.25f, // EffectID 37: Yharon Soul Fragment / 龙魂碎片
            2.22f, // EffectID 38: Exo Prism / 星流棱晶
            2.69f, // EffectID 39: Ashes of Annihilation / 湮灭余烬
            2.80f, // EffectID 40: Armored Shell / 装甲外壳
            0.75f, // EffectID 41: Pearl Shard / 珍珠碎片
            2.52f, // EffectID 42: Darksun Fragment / 日蚀之阴碎片
            0f, // EffectID 43: Cynosure / 保持使用阶段默认倍率
            1.53f // EffectID 44: Core of Calamity / 灾劫核心，与瘟疫细胞罐一致
        };

        public const int OverheatGraceTime = 60;
        public const int ForcedShutdownTime = 120;
        public const int NormalHeatDecayTime = 90;
        public const int ManualCoolingExtraLockout = 30;

        public int GetCompletedStageIndex()
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

        public int GetLeftClickBaseDamage()
        {
            return GetValueForStage(LeftClickProgressDamage, GetCompletedStageIndex());
        }

        public int GetLeftClickBaseDamageForEffect(int effectID)
        {
            int baseDamage = GetLeftClickBaseDamage();
            float materialMultiplier = GetLeftClickMaterialDamageMultiplier(effectID);
            float finalMultiplier = materialMultiplier > 0f ? materialMultiplier : GetDefaultOrbDamageMultiplier();

            return System.Math.Max(1, (int)System.Math.Round(baseDamage * finalMultiplier));
        }

        public float GetLeftClickMaterialDamageMultiplier(int effectID)
        {
            // Cynosure 过去依赖数组越界返回 0，从而使用阶段默认倍率。
            // 新增 EffectID 44 后需要保留该行为，避免唯一弹药被意外改成固定 1 倍伤害。
            if (effectID == 43)
                return 0f;

            if (effectID < 0 || effectID >= LeftClickMaterialDamageMultipliers.Length)
                return 0f;

            return 1f + System.Math.Max(0f, LeftClickMaterialDamageMultipliers[effectID]);
        }

        public float GetDefaultOrbDamageMultiplier()
        {
            return GetFloatValueForStage(DefaultOrbDamageMultipliers, GetCompletedStageIndex());
        }

        public int GetDefaultOrbExplosionSize()
        {
            return GetValueForStage(DefaultOrbExplosionSizes, GetCompletedStageIndex());
        }

        public int GetRightClickBaseDamage()
        {
            return GetValueForStage(RightClickBaseDamage, GetCompletedStageIndex());
        }

        public int GetRightClickMaxHeatLevel()
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

        public int GetRightClickLaserCount()
        {
            if (NPC.downedMoonlord)
                return 3;

            if (Main.hardMode)
                return 2;

            return 1;
        }

        public int GetRightClickProgressState()
        {
            return GetRightClickMaxHeatLevel() - 1;
        }

        public int GetHeatFillTime(int completedHeatLevel)
        {
            int clampedIndex = Utils.Clamp(completedHeatLevel, 0, HeatFillTimes.Length - 1);
            return HeatFillTimes[clampedIndex];
        }

        // 下面这些是内部支撑参数，不是主要平衡入口。
        private static readonly float[] DefaultOrbDamageMultipliers =
        {
            1f,
            1f,
            1f,
            1f,
            1.12f,
            1.12f,
            1.18f,
            1.18f,
            1.35f,
            1.35f,
            1.35f,
            1.45f,
            1.55f,
            1.65f
        };

        private static readonly int[] DefaultOrbExplosionSizes =
        {
            112,
            112,
            112,
            128,
            168,
            168,
            184,
            184,
            240,
            240,
            240,
            280,
            320,
            360
        };

        private static readonly int[] HeatFillTimes =
        {
            116,
            116,
            116,
            116,
            116
        };

        private int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }

        private float GetFloatValueForStage(float[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1f;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(0.01f, values[clampedIndex]);
        }
    }
}
