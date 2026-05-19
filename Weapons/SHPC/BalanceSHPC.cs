using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class BalanceSHPC
    {
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
            15,
            24,
            30,
            42,
            54,
            72,
            90,
            120,
            180,
            240,
            300,
            360,
            500,
            750
        };

        public static readonly int[] RightClickBaseDamage =
        {
            6,
            8,
            9,
            10,
            17,
            24,
            32,
            40,
            54,
            66,
            77,
            90,
            100,
            150
        };

        // Indexed by SHPC effect ID. Materials multiply the stage-based left-click damage.
        public static readonly float[] LeftClickMaterialDamageMultipliers =
        {
            0f,
            0.46f,
            0.54f,
            0.58f,
            0.70f,
            0.64f,
            0.60f,
            0.68f,
            0.33f,
            0.76f,
            0.82f,
            0.72f,
            0.90f,
            0.96f,
            0.93f,
            1.05f,
            0f,
            1.02f,
            1.08f,
            1.12f,
            0f,
            0.98f,
            0.96f,
            1.04f,
            1.00f,
            1.10f,
            1.16f,
            0f,
            1.22f,
            1.28f,
            1.34f,
            1.26f,
            1.42f,
            1.46f,
            1.52f,
            1.48f,
            1.56f,
            1.68f,
            1.82f,
            1.90f,
            1.74f
        };

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
            210,
            96,
            126,
            156,
            186
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
            if (effectID < 0 || effectID >= LeftClickMaterialDamageMultipliers.Length)
                return 0f;

            return System.Math.Max(0f, LeftClickMaterialDamageMultipliers[effectID]);
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
