using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatModule;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class BalanceSHPC
    {
        private const string SourceFile = "Weapons/SHPC/BalanceSHPC.cs";

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

        private static readonly int[] DefaultLeftClickProgressDamage =
        {
            21, // Initial / 初始
            29, // Eye of Cthulhu / 克苏鲁之眼
            33, // Evil Boss / 腐化或猩红 Boss T2
            41, // Skeletron / 骷髅王
            45, // Hardmode / 困难模式
            50, // Any Mechanical Boss / 任意机械 Boss
            55, // Plantera / 世纪之花
            60, // Golem / 石巨人
            62, // Moon Lord / 月亮领主
            64, // Providence / 亵渎天神
            77, // Polterghast / 噬魂幽花
            81, // Devourer of Gods / 神明吞噬者
            93, // Yharon / 犽戎
            125 // Exo Mechs and Supreme Calamitas / 星流巨械与至尊灾厄
        };

        // 2. 右键基础倍率/伤害成长：顺序和 StageNames 完全一致。
        private static readonly int[] DefaultRightClickBaseDamage =
        {
            16, // Initial / 初始
            21, // Eye of Cthulhu / 克苏鲁之眼
            28, // Evil Boss / 腐化或猩红 Boss T2
            30, // Skeletron / 骷髅王
            45, // Hardmode / 困难模式
            56, // Any Mechanical Boss / 任意机械 Boss
            70, // Plantera / 世纪之花
            81, // Golem / 石巨人
            95, // Moon Lord / 月亮领主
            103, // Providence / 亵渎天神
            112, // Polterghast / 噬魂幽花
            142, // Devourer of Gods / 神明吞噬者
            225, // Yharon / 犽戎
            377 // Exo Mechs and Supreme Calamitas / 星流巨械与至尊灾厄
        };

        // 迫击炮模式倍率
        private static readonly int[] MortarRightClickBaseDamage =
        {
            675, // Providence / 亵渎天神
            750, // Polterghast / 噬魂幽花
            953, // Devourer of Gods / 神明吞噬者
            2500, // Yharon / 犽戎
            4163 // Exo Mechs and Supreme Calamitas / 星流巨械与至尊灾厄
        };

        // 炮台模式倍率
        private static readonly int[] TurretRightClickBaseDamage =
        {
            188, // Golem / 石巨人
            210, // Moon Lord / 月亮领主
            225, // Providence / 亵渎天神
            250, // Polterghast / 噬魂幽花
            318, // Devourer of Gods / 神明吞噬者
            500, // Yharon / 犽戎
            833 // Exo Mechs and Supreme Calamitas / 星流巨械与至尊灾厄
        };

        private static readonly float[] DefaultLeftClickMaterialDamageMultipliers =
        {
            0f, // EffectID 0: Unused gap / 未使用空位
            0.46f, // EffectID 1: Energy Core / 钨钢能量核心
            0.48f, // EffectID 2: Stormlion Mandible / 风暴之颚
            0.58f, // EffectID 3: Sulphuric Scale / 硫磺鳞片
            0.50f, // EffectID 4: Purified Gel / 纯净凝胶
            1.02f, // EffectID 5: Essence of Havoc / 混沌精华
            0.84f, // EffectID 6: Essence of Eleum / 冰精华
            1.03f, // EffectID 7: Essence of Sunlight / 日光精华
            1.06f, // EffectID 8: Titan Heart / 泰坦之心
            0.80f, // EffectID 9: Soul of Light / 光明之魂
            0.78f, // EffectID 10: Soul of Night / 暗影之魂
            0.90f, // EffectID 11: Soul of Flight / 飞翔之魂
            1.06f, // EffectID 12: Soul of Fright / 恐惧之魂
            1.08f, // EffectID 13: Soul of Might / 力量之魂
            0.80f, // EffectID 14: Soul of Sight / 视域之魂
            1.45f, // EffectID 15: Living Shard / 生命碎片
            0f, // EffectID 16: Unused gap / 未使用空位
            1.02f, // EffectID 17: Depth Cells / 深渊细胞
            1.40f, // EffectID 18: Plague Cell Canister / 瘟疫细胞罐
            1.13f, // EffectID 19: Ashes of Calamity / 灾厄尘
            0f, // EffectID 20: Beetle Husk / 甲虫外壳，未实现
            1.72f, // EffectID 21: Solar Fragment / 日曜碎片
            1.72f, // EffectID 22: Vortex Fragment / 星旋碎片
            1.50f, // EffectID 23: Nebula Fragment / 星云碎片
            1.73f, // EffectID 24: Stardust Fragment / 星尘碎片
            1.94f, // EffectID 25: Meld Blob / 冥思溶剂
            2.00f, // EffectID 26: Unholy Essence / 浊火精华
            0f, // EffectID 27: Unused gap / 未使用空位
            2.00f, // EffectID 28: Divine Geode / 神圣晶石
            2.48f, // EffectID 29: Bloodstone Core / 血神核心
            2.70f, // EffectID 30: Ruinous Soul / 毁灭之灵
            2.22f, // EffectID 31: Necroplasm / 灵质
            1.97f, // EffectID 32: Dark Plasma / 暗离子体
            2.20f, // EffectID 33: Twisting Nether / 扭曲虚空
            2.96f, // EffectID 34: Endothermic Energy / 恒温能量
            2.77f, // EffectID 35: Nightmare Fuel / 梦魇魔能
            2.15f, // EffectID 36: Ascendant Spirit Essence / 化神魂精
            3.25f, // EffectID 37: Yharon Soul Fragment / 龙魂碎片
            2.72f, // EffectID 38: Exo Prism / 星流棱晶
            2.69f, // EffectID 39: Ashes of Annihilation / 湮灭余烬
            2.87f, // EffectID 40: Armored Shell / 装甲外壳
            0.42f, // EffectID 41: Pearl Shard / 珍珠碎片
            2.64f, // EffectID 42: Darksun Fragment / 日蚀之阴碎片
            4.00f, // EffectID 43: Cynosure / 填 0 = 使用阶段默认倍率；填正数 n = 最终伤害倍率为 (1+n)，例如填 1.5 = 2.5 倍
            1.12f // EffectID 44: Core of Calamity / 灾劫核心，与瘟疫细胞罐一致
        };

        // 大招（超级激光）伤害倍率：肉后/月后/神后三档
        private static readonly float[] UltimateLaserDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        public float GetUltimateLaserDamageMultiplier()
        {
            return GetFloatValueForStage(GetUltimateLaserDamageMultiplierValues(), GetUltimateLaserDamageTier());
        }

        public int GetUltimateLaserDamageTier()
        {
            if (DownedBossSystem.downedProvidence)
                return 2;
            if (NPC.downedMoonlord)
                return 1;
            return 0;
        }

        public const int ForcedShutdownTime = 120;
        public const int NormalHeatDecayTime = 90;
        public const int ManualCoolingExtraLockout = 30;

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

        public int GetLeftClickBaseDamage()
        {
            return GetValueForStage(GetLeftClickProgressDamageValues(), GetCompletedStageIndex());
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
            float[] values = GetLeftClickMaterialDamageMultiplierValues();
            if (effectID < 0 || effectID >= values.Length)
                return 0f;

            float rawValue = values[effectID];
            if (rawValue <= 0f)
                return 0f;  // 0 → 触发 GetDefaultOrbDamageMultiplier() fallback

            return 1f + rawValue;
        }

        public float GetDefaultOrbDamageMultiplier()
        {
            return GetFloatValueForStage(GetDefaultOrbDamageMultiplierValues(), GetCompletedStageIndex());
        }

        public int GetDefaultOrbExplosionSize()
        {
            return GetDefaultLeftClickExplosionRadius();
        }

        public int GetDefaultLeftClickExplosionRadius()
        {
            return GetValueForStage(GetDefaultLeftClickExplosionRadiusValues(), GetCompletedStageIndex());
        }

        public int GetDefaultLeftClickNoAmmoExplosionRadius()
        {
            return GetValueForStage(GetDefaultLeftClickNoAmmoExplosionRadiusValues(), GetRightClickProgressState());
        }

        public int GetRightClickBaseDamage()
        {
            return GetValueForStage(GetRightClickBaseDamageValues(), GetCompletedStageIndex());
        }

        public int GetMortarRightClickBaseDamage()
        {
            return GetValueForStage(GetMortarRightClickBaseDamageValues(), GetCompletedStageIndex(), 9);
        }

        public int GetTurretRightClickBaseDamage()
        {
            return GetValueForStage(GetTurretRightClickBaseDamageValues(), GetCompletedStageIndex(), 7);
        }

        public int GetRightClickMaxHeatLevel()
        {
            return GetRightClickMaxHeatLevelForProgressState(GetRightClickProgressState());
        }

        public int GetRightClickMaxHeatLevelForProgressState(int progressState)
        {
            return GetValueForStage(RightClickMaxHeatLevelsByProgressState, progressState);
        }

        public int GetRightClickLaserCount()
        {
            return GetRightClickLaserCountForProgressState(GetRightClickProgressState());
        }

        public int GetRightClickLaserCountForProgressState(int progressState)
        {
            return GetValueForStage(RightClickLaserCountsByProgressState, progressState);
        }

        public int GetRightClickProgressState()
        {
            if (DownedBossSystem.downedDoG)
                return 4;

            if (NPC.downedMoonlord)
                return 3;

            if (NPC.downedPlantBoss)
                return 2;

            if (Main.hardMode)
                return 1;

            return 0;
        }

        public int GetHeatFillTime(int completedHeatLevel)
        {
            return GetHeatFillTime(completedHeatLevel, GetRightClickMaxHeatLevel());
        }

        public int GetHeatFillTime(int completedHeatLevel, int maxHeatLevel)
        {
            int progressIndex = Utils.Clamp(maxHeatLevel, 1, HeatFillTimesByProgressState.Length) - 1;
            int[] values = GetHeatFillTimesByProgressStateValues();
            progressIndex = Utils.Clamp(progressIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[progressIndex]);
        }

        public int GetOverheatGraceTime()
        {
            return GetOverheatGraceTime(GetRightClickMaxHeatLevel());
        }

        public int GetOverheatGraceTime(int maxHeatLevel)
        {
            int clampedIndex = Utils.Clamp(maxHeatLevel, 1, OverheatGraceTimes.Length) - 1;
            return System.Math.Max(1, OverheatGraceTimes[clampedIndex]);
        }

        public int GetForcedShutdownTime(Player player)
        {
            bool hasCoolingBoost = player != null &&
                (player.HasBuff(BuffID.Inferno) ||
                player.GetModPlayer<HeatModulePlayer>().HeatModuleEquipped);

            float multiplier = hasCoolingBoost
                ? AcceleratedForcedShutdownMultiplier
                : 1f;

            return System.Math.Max(1, (int)System.MathF.Ceiling(ForcedShutdownTime * multiplier));
        }

        // 下面这些是内部支撑参数，不是主要平衡入口。
        private static readonly float[] DefaultDefaultOrbDamageMultipliers =
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

        private static readonly int[] DefaultLeftClickExplosionRadii =
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

        private static readonly int[] DefaultLeftClickNoAmmoExplosionRadii =
        {
            112,
            228,
            344,
            460,
            576
        };

        // 右键热量总积攒时间固定为15秒；当前可用的热量等级平分这15秒。
        // 1级: 15秒；2级: 每级7.5秒；3级: 每级5秒；4级: 每级3.75秒；5级: 每级3秒。
        private static readonly int[] HeatFillTimesByProgressState =
        {
            300,
            300,
            300,
            300,
            300
        };

        private static readonly int[] RightClickMaxHeatLevelsByProgressState =
        {
            1,
            2,
            3,
            4,
            5
        };

        private static readonly int[] RightClickLaserCountsByProgressState =
        {
            1,
            2,
            2,
            3,
            3
        };

        private static readonly int[] OverheatGraceTimes =
        {
            180,
            150,
            120,
            90,
            60
        };

        private const float AcceleratedForcedShutdownMultiplier = 0.5f;

        private int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }

        private int GetValueForStage(int[] values, int stageIndex, int firstStageIndex)
        {
            return GetValueForStage(values, stageIndex - firstStageIndex);
        }

        private float GetFloatValueForStage(float[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1f;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(0.01f, values[clampedIndex]);
        }

        private static int[] GetLeftClickProgressDamageValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultLeftClickProgressDamage), DefaultLeftClickProgressDamage);

        private static int[] GetRightClickBaseDamageValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultRightClickBaseDamage), DefaultRightClickBaseDamage);

        private static int[] GetMortarRightClickBaseDamageValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(MortarRightClickBaseDamage), MortarRightClickBaseDamage);

        private static int[] GetTurretRightClickBaseDamageValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(TurretRightClickBaseDamage), TurretRightClickBaseDamage);

        private static float[] GetLeftClickMaterialDamageMultiplierValues() =>
            RuntimeBalanceData.GetSourceFloatArray(SourceFile, nameof(DefaultLeftClickMaterialDamageMultipliers), DefaultLeftClickMaterialDamageMultipliers);

        private static float[] GetDefaultOrbDamageMultiplierValues() =>
            RuntimeBalanceData.GetSourceFloatArray(SourceFile, nameof(DefaultDefaultOrbDamageMultipliers), DefaultDefaultOrbDamageMultipliers);

        private static int[] GetDefaultLeftClickExplosionRadiusValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultLeftClickExplosionRadii), DefaultLeftClickExplosionRadii);

        private static int[] GetDefaultLeftClickNoAmmoExplosionRadiusValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultLeftClickNoAmmoExplosionRadii), DefaultLeftClickNoAmmoExplosionRadii);

        private static int[] GetHeatFillTimesByProgressStateValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(HeatFillTimesByProgressState), HeatFillTimesByProgressState);

        private static float[] GetUltimateLaserDamageMultiplierValues() =>
            RuntimeBalanceData.GetSourceFloatArray(SourceFile, nameof(UltimateLaserDamageMultipliers), UltimateLaserDamageMultipliers);
    }
}
