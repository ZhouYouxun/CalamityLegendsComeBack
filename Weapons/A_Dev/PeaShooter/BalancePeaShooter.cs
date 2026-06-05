using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal sealed class BalancePeaShooter
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

        public static readonly int[] BaseDamage =
        {
            6,
            8,
            10,
            13,
            18,
            24,
            32,
            42,
            58,
            76,
            98,
            128,
            168,
            220
        };

        public static readonly float[] ShootSpeeds =
        {
            14.5f,
            14.8f,
            15.1f,
            15.6f,
            16.2f,
            16.8f,
            17.4f,
            18.0f,
            18.8f,
            19.6f,
            20.4f,
            21.2f,
            22.0f,
            22.8f
        };

        public static readonly int[] SplashRadii =
        {
            48,
            50,
            52,
            54,
            58,
            62,
            66,
            70,
            76,
            82,
            88,
            96,
            104,
            112
        };

        public static readonly int[] RockSplashRadii =
        {
            82,
            86,
            90,
            94,
            100,
            106,
            112,
            118,
            126,
            134,
            142,
            152,
            164,
            176
        };

        public static readonly int[] ElectricCloudRadii =
        {
            56,
            58,
            60,
            62,
            66,
            70,
            74,
            78,
            84,
            90,
            96,
            104,
            112,
            120
        };

        public static readonly int[] DebuffDurations =
        {
            150,
            165,
            180,
            195,
            210,
            225,
            240,
            255,
            270,
            285,
            300,
            330,
            360,
            390
        };

        public const int AutoFireInterval = 2;
        public const int CritBonus = 10;
        public const float JumpSpeedBoost = 1.25f;
        public const float BaseSplashDamageMultiplier = 0.48f;
        public const float RockSplashDamageMultiplier = 0.62f;
        public const float ElectricCloudDamageMultiplier = 0.28f;
        public const float RockKnockbackMultiplier = 2.1f;
        public const int ElectricCloudLifetime = 92;
        public const int ElectricCloudHitCooldown = 18;

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

        public int GetBaseDamage() => GetValueForStage(BaseDamage, GetCompletedStageIndex());

        public float GetShootSpeed() => GetFloatValueForStage(ShootSpeeds, GetCompletedStageIndex());

        public static string GetStageName(int stageIndex)
        {
            int clampedIndex = Utils.Clamp(stageIndex, 0, StageNames.Length - 1);
            return StageNames[clampedIndex];
        }

        public static int GetSplashRadius(int stageIndex, PeaShooterPeaType peaType)
        {
            return GetValueForStage(peaType == PeaShooterPeaType.Rock ? RockSplashRadii : SplashRadii, stageIndex);
        }

        public static int GetElectricCloudRadius(int stageIndex) => GetValueForStage(ElectricCloudRadii, stageIndex);

        public static int GetDebuffDuration(int stageIndex) => GetValueForStage(DebuffDurations, stageIndex);

        public static float GetSplashDamageMultiplier(PeaShooterPeaType peaType)
        {
            return peaType == PeaShooterPeaType.Rock ? RockSplashDamageMultiplier : BaseSplashDamageMultiplier;
        }

        public static float GetKnockbackMultiplier(PeaShooterPeaType peaType)
        {
            return peaType == PeaShooterPeaType.Rock ? RockKnockbackMultiplier : 1f;
        }

        private static int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }

        private static float GetFloatValueForStage(float[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1f;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(0.01f, values[clampedIndex]);
        }
    }
}
