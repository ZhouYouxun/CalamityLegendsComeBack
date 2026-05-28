using CalamityMod;
using Terraria;
using Terraria.Localization;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal sealed class BalanceMK14EBR
    {
        public const int StagePlantera = 0;
        public const int StageGolem = 1;
        public const int StagePlaguebringer = 2;
        public const int StageMoonLord = 3;
        public const int StageProvidence = 4;
        public const int StagePolterghast = 5;
        public const int StageDevourerOfGods = 6;
        public const int StageYharon = 7;
        public const int StageEndgame = 8;

        public static readonly string[] StageNames =
        {
            "Post-Plantera",
            "Golem",
            "Plaguebringer Goliath",
            "Moon Lord",
            "Providence",
            "Polterghast",
            "Devourer of Gods",
            "Yharon",
            "Exo Mechs and Supreme Calamitas"
        };

        public static readonly string[] StageLocalizationKeys =
        {
            "PostPlantera",
            "Golem",
            "Plaguebringer",
            "MoonLord",
            "Providence",
            "Polterghast",
            "DevourerOfGods",
            "Yharon",
            "Endgame"
        };

        public static readonly int[] BaseDamage =
        {
            82,
            96,
            108,
            122,
            150,
            176,
            224,
            278,
            346
        };

        public int GetCompletedStageIndex()
        {
            int stageIndex = StagePlantera;

            if (NPC.downedGolemBoss)
                stageIndex = StageGolem;

            if (DownedBossSystem.downedPlaguebringer)
                stageIndex = StagePlaguebringer;

            if (NPC.downedMoonlord)
                stageIndex = StageMoonLord;

            if (DownedBossSystem.downedProvidence)
                stageIndex = StageProvidence;

            if (DownedBossSystem.downedPolterghast)
                stageIndex = StagePolterghast;

            if (DownedBossSystem.downedDoG)
                stageIndex = StageDevourerOfGods;

            if (DownedBossSystem.downedYharon)
                stageIndex = StageYharon;

            if (DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas)
                stageIndex = StageEndgame;

            return stageIndex;
        }

        public static string GetStageNameKey(int stage)
        {
            stage = Utils.Clamp(stage, 0, StageLocalizationKeys.Length - 1);
            return $"Mods.CalamityLegendsComeBack.MK14EBR.UI.Stage.{StageLocalizationKeys[stage]}";
        }

        public static string GetLocalizedStageName(int stage)
        {
            stage = Utils.Clamp(stage, 0, StageNames.Length - 1);
            string localized = Language.GetTextValue(GetStageNameKey(stage));
            return string.IsNullOrWhiteSpace(localized) || localized == GetStageNameKey(stage)
                ? StageNames[stage]
                : localized;
        }

        public int GetBaseDamage()
        {
            int stage = Utils.Clamp(GetCompletedStageIndex(), 0, BaseDamage.Length - 1);
            return BaseDamage[stage];
        }
    }
}
