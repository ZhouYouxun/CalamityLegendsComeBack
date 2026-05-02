using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class BalanceSHPC
    {
        public static readonly string[] StageNames =
        {
            "Initial", // Initial / 初始
            "Eye of Cthulhu", // Eye of Cthulhu / 克苏鲁之眼
            "Evil Boss", // Evil Boss / 邪恶Boss
            "Skeletron", // Skeletron / 骷髅王
            "Hardmode", // Hardmode / 肉后
            "Any Mechanical Boss", // Any Mechanical Boss / 任意机械Boss后
            "Plantera", // Plantera / 世纪之花后
            "Golem", // Golem / 石巨人后
            "Moon Lord", // Moon Lord / 月亮领主后
            "Providence", // Providence / 亵渎天神后
            "Polterghast", // Polterghast / 幽花后
            "Devourer of Gods", // Devourer of Gods / 神明吞噬者后
            "Yharon", // Yharon / 犽戎后
            "Exo Mechs and Supreme Calamitas" // Exo Mechs and Supreme Calamitas / 星流巨械和至尊灾厄后
        };

        public static readonly int[] LeftClickProgressDamage =
        {
            10, // Initial / 初始
            16, // Eye of Cthulhu / 克苏鲁之眼
            16, // Evil Boss / 邪恶Boss
            26, // Skeletron / 骷髅王
            135, // Hardmode / 肉后
            135, // Any Mechanical Boss / 任意机械Boss后
            180, // Plantera / 世纪之花后
            180, // Golem / 石巨人后
            250, // Moon Lord / 月亮领主后
            290, // Providence / 亵渎天神后
            290, // Polterghast / 幽花后
            360, // Devourer of Gods / 神明吞噬者后
            500, // Yharon / 犽戎后
            750 // Exo Mechs and Supreme Calamitas / 星流巨械和至尊灾厄后
        };

        public static readonly int[] RightClickBaseDamage =
        {
            9, // Initial / 初始
            4, // Eye of Cthulhu / 克苏鲁之眼
            4, // Evil Boss / 邪恶Boss
            7, // Skeletron / 骷髅王
            34, // Hardmode / 肉后
            34, // Any Mechanical Boss / 任意机械Boss后
            45, // Plantera / 世纪之花后
            45, // Golem / 石巨人后
            63, // Moon Lord / 月亮领主后
            73, // Providence / 亵渎天神后
            73, // Polterghast / 幽花后
            90, // Devourer of Gods / 神明吞噬者后
            125, // Yharon / 犽戎后
            188 // Exo Mechs and Supreme Calamitas / 星流巨械和至尊灾厄后
        };

        // Indexed by SHPC effect ID. 0, 16, 20 and 27 are currently unused gaps.
        public static readonly int[] LeftClickMaterialDamage =
        {
            0, // EffectID 0: Unused gap / 未使用空位
            18, // EffectID 1: Energy Core / 钨钢能源核心
            22, // EffectID 2: Stormlion Mandible / 风暴之颚
            24, // EffectID 3: Sulphuric Scale / 硫磺鳞片
            20, // EffectID 4: Purified Gel / 纯净凝胶
            34, // EffectID 5: Essence of Havoc / 混沌精华
            34, // EffectID 6: Essence of Eleum / 冰精华
            36, // EffectID 7: Essence of Sunlight / 日光精华
            42, // EffectID 8: Titan Heart / 泰坦之心
            48, // EffectID 9: Soul of Light / 光明之魂
            48, // EffectID 10: Soul of Night / 暗影之魂
            50, // EffectID 11: Soul of Flight / 飞翔之魂
            58, // EffectID 12: Soul of Fright / 恐惧之魂
            62, // EffectID 13: Soul of Might / 力量之魂
            60, // EffectID 14: Soul of Sight / 视域之魂
            74, // EffectID 15: Living Shard / 生命碎片
            0, // EffectID 16: Unused gap / 未使用空位
            82, // EffectID 17: Depth Cells / 深渊细胞
            86, // EffectID 18: Plague Cell Canister / 瘟疫细胞罐
            94, // EffectID 19: Ashes of Calamity / 灾厄尘
            0, // EffectID 20: Beetle Husk, not implemented / 甲虫外壳，未实现
            110, // EffectID 21: Solar Fragment / 日曜碎片
            112, // EffectID 22: Vortex Fragment / 星旋碎片
            116, // EffectID 23: Nebula Fragment / 星云碎片
            114, // EffectID 24: Stardust Fragment / 星尘碎片
            124, // EffectID 25: Meld Blob / 融合团块
            140, // EffectID 26: Unholy Essence / 浊火精华
            0, // EffectID 27: Unused gap / 未使用空位
            158, // EffectID 28: Divine Geode / 神圣晶石
            168, // EffectID 29: Bloodstone Core / 血石核心
            176, // EffectID 30: Ruinous Soul / 毁灭之灵
            184, // EffectID 31: Necroplasm / 灵质
            198, // EffectID 32: Dark Plasma / 暗离子体
            210, // EffectID 33: Twisting Nether / 扭曲虚空
            260, // EffectID 34: Endothermic Energy / 恒温能量
            270, // EffectID 35: Nightmare Fuel / 梦魇魔能
            310, // EffectID 36: Ascendant Spirit Essence / 化神魂精
            360, // EffectID 37: Yharon Soul Fragment / 龙魂碎片
            430, // EffectID 38: Exo Prism / 星流棱晶
            500, // EffectID 39: Ashes of Annihilation / 湮灭余烬
            520 // EffectID 40: Armored Shell / 装甲外壳
        };

        private static readonly float[] DefaultOrbDamageMultipliers =
        {
            1f, // Initial / 初始
            1f, // Eye of Cthulhu / 克苏鲁之眼
            1f, // Evil Boss / 邪恶Boss
            1f, // Skeletron / 骷髅王
            1.12f, // Hardmode / 肉后
            1.12f, // Any Mechanical Boss / 任意机械Boss后
            1.18f, // Plantera / 世纪之花后
            1.18f, // Golem / 石巨人后
            1.35f, // Moon Lord / 月亮领主后
            1.35f, // Providence / 亵渎天神后
            1.35f, // Polterghast / 幽花后
            1.45f, // Devourer of Gods / 神明吞噬者后
            1.55f, // Yharon / 犽戎后
            1.65f // Exo Mechs and Supreme Calamitas / 星流巨械和至尊灾厄后
        };

        private static readonly int[] DefaultOrbExplosionSizes =
        {
            112, // Initial / 初始
            112, // Eye of Cthulhu / 克苏鲁之眼
            112, // Evil Boss / 邪恶Boss
            128, // Skeletron / 骷髅王
            168, // Hardmode / 肉后
            168, // Any Mechanical Boss / 任意机械Boss后
            184, // Plantera / 世纪之花后
            184, // Golem / 石巨人后
            240, // Moon Lord / 月亮领主后
            240, // Providence / 亵渎天神后
            240, // Polterghast / 幽花后
            280, // Devourer of Gods / 神明吞噬者后
            320, // Yharon / 犽戎后
            360 // Exo Mechs and Supreme Calamitas / 星流巨械和至尊灾厄后
        };

        private static readonly int[] HeatFillTimes =
        {
            210, // Heat level 1 / 热量等级1
            96, // Heat level 2 / 热量等级2
            126, // Heat level 3 / 热量等级3
            156, // Heat level 4 / 热量等级4
            186 // Heat level 5 / 热量等级5
        };

        public const int OverheatGraceTime = 90;
        public const int ForcedShutdownTime = 30;
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
                if (!clearedStages[i])
                    break;

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
            int materialDamage = GetMaterialLeftClickDamage(effectID);
            if (materialDamage > 0)
                return materialDamage;

            return System.Math.Max(1, (int)(GetLeftClickBaseDamage() * GetDefaultOrbDamageMultiplier()));
        }

        public int GetMaterialLeftClickDamage(int effectID)
        {
            if (effectID < 0 || effectID >= LeftClickMaterialDamage.Length)
                return 0;

            return System.Math.Max(0, LeftClickMaterialDamage[effectID]);
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
