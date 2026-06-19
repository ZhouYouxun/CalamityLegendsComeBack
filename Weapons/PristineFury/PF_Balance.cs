using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal static class PF_Balance
    {
        private const string SourceFile = "Weapons/PristineFury/PF_Balance.cs";

        private const int RightClickLevel1Index = 0;
        private const int RightClickLevel2Index = 1;
        private const int RightClickLevel3Index = 2;
        private const int RightClickOverheatStarIndex = 3;
        private const int RightClickOverheatContactIndex = 4;

        // Weapon-owned progression damage. Marks do not replace this value.
        private static readonly int[] DefaultProgressBaseDamage =
        {
            18,   // Initial
            20,   // Desert Scourge
            24,   // Eye of Cthulhu
            28,   // Skeletron
            32,   // Evil Boss
            38,   // Slime God
            44,   // Hardmode
            58,   // Brimstone Elemental
            82,   // Any Mechanical Boss
            94,   // Calamitas Clone
            105,  // Plantera
            128,  // Golem
            155,  // Plaguebringer Goliath
            180,  // Empress of Light
            205,  // Moon Lord
            255,  // Profaned Guardians
            300,  // Polterghast
            440,  // Devourer of Gods
            500,  // Yharon
            1000  // Exo Mechs and Supreme Calamitas
        };

        // Indexed by PristineFuryMark. This controls only how the selected mark delivers left-click damage.
        private static readonly float[] DefaultLeftClickMarkDamageMultipliers =
        {
            0.78f, // Idle
            0.82f, // DesertScourge
            0.84f, // EyeOfCthulhu
            0.86f, // Skeletron
            0.88f, // EvilT2
            0.92f, // SlimeGod
            0.95f, // HardMode
            1.08f, // BrimstoneElemental
            0.98f, // Prime
            1.36f, // FakeCalamity
            1.10f, // Plantera
            1.12f, // Golem
            1.16f, // Goliath
            1.22f, // Empress
            1.24f, // Moonlord
            1.34f, // Providence
            1.42f, // Polterghast
            1.46f, // Dog
            1.58f, // Dragon
            1.70f, // ExoTwins
            1.84f, // ExoThanatos
            1.78f, // ExoAres
            1.88f  // Ravager
        };

        // Right-click damage uses the weapon base damage and this separate multiplier set.
        private static readonly float[] DefaultRightClickDamageMultipliers =
        {
            1.00f, // Charge level 1 fireball
            1.80f, // Charge level 2 fireball
            3.50f, // Charge level 3 fireball
            0.65f, // Overheat star
            6.00f  // Overheat contact field
        };

        public static int GetInitialBaseDamage() => GetProgressBaseDamage(0);

        public static int GetBaseDamage() => GetProgressBaseDamage(GetCompletedStageIndex());

        public static float GetLeftClickMarkDamageMultiplier(PristineFuryMark mark)
        {
            int index = (int)mark;
            float[] values = GetLeftClickMarkDamageMultiplierValues();
            if (index < 0 || index >= values.Length)
                return 1f;

            return System.Math.Max(0f, values[index]);
        }

        public static float GetRightFireballDamageMultiplier(float chargeLevel)
        {
            if (chargeLevel >= 3f)
                return GetRightClickDamageMultiplier(RightClickLevel3Index);
            if (chargeLevel >= 2f)
                return GetRightClickDamageMultiplier(RightClickLevel2Index);

            return GetRightClickDamageMultiplier(RightClickLevel1Index);
        }

        public static float GetRightOverheatStarDamageMultiplier() =>
            GetRightClickDamageMultiplier(RightClickOverheatStarIndex);

        public static float GetRightOverheatContactDamageMultiplier() =>
            GetRightClickDamageMultiplier(RightClickOverheatContactIndex);

        private static int GetCompletedStageIndex()
        {
            bool[] clearedStages =
            {
                DownedBossSystem.downedDesertScourge,
                NPC.downedBoss1,
                NPC.downedBoss3,
                NPC.downedBoss2,
                DownedBossSystem.downedSlimeGod,
                Main.hardMode,
                DownedBossSystem.downedBrimstoneElemental,
                NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3,
                DownedBossSystem.downedCalamitasClone,
                NPC.downedPlantBoss,
                NPC.downedGolemBoss,
                DownedBossSystem.downedPlaguebringer,
                NPC.downedEmpressOfLight,
                NPC.downedMoonlord,
                DownedBossSystem.downedGuardians,
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

        private static int GetProgressBaseDamage(int stageIndex)
        {
            int[] values = GetProgressBaseDamageValues();
            stageIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[stageIndex]);
        }

        private static float GetRightClickDamageMultiplier(int index)
        {
            float[] values = GetRightClickDamageMultiplierValues();
            if (index < 0 || index >= values.Length)
                return 1f;

            return System.Math.Max(0f, values[index]);
        }

        private static int[] GetProgressBaseDamageValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultProgressBaseDamage), DefaultProgressBaseDamage);

        private static float[] GetLeftClickMarkDamageMultiplierValues() =>
            RuntimeBalanceData.GetSourceFloatArray(SourceFile, nameof(DefaultLeftClickMarkDamageMultipliers), DefaultLeftClickMarkDamageMultipliers);

        private static float[] GetRightClickDamageMultiplierValues() =>
            RuntimeBalanceData.GetSourceFloatArray(SourceFile, nameof(DefaultRightClickDamageMultipliers), DefaultRightClickDamageMultipliers);
    }
}
