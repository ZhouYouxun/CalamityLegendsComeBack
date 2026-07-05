using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal enum PeaShooterFireStyle
    {
        Single,
        Double,
        ThreeLine,
        Gatling,
        WildGatling
    }

    internal sealed class BalancePeaShooter
    {
        public const int StageNameColumn = 0;
        public const int DamageColumn = 1;
        public const int ShootSpeedColumn = 2;

        public static readonly string[,] StageStats =
        {
            { "Initial",                          "30",  "14.5" },
            { "Eye of Cthulhu",                   "30",  "14.8" },
            { "Evil Boss",                        "30",  "15.1" },
            { "Skeletron",                        "30",  "15.6" },
            { "Hardmode",                         "30",  "16.2" },
            { "Any Mechanical Boss",              "30",  "16.8" },
            { "Plantera",                         "30",  "17.4" },
            { "Golem",                            "30",  "18.0" },
            { "Moon Lord",                        "50",  "18.8" },
            { "Providence",                       "76",  "19.6" },
            { "Polterghast",                      "98",  "20.4" },
            { "Devourer of Gods",                 "128", "21.2" },
            { "Yharon",                           "168", "22.0" },
            { "Exo Mechs and Supreme Calamitas",  "220", "22.8" }
        };

        public const int BaseBurstInterval = 90;
        public const int BurstShotSpacing = 4;
        public const float LineSpreadDegrees = 3f;
        public const int SplashRadius = 64;
        public const int DebuffDuration = 240;
        public const int FirePatchLifetime = 30;
        public const int FirePatchSize = 75;
        public const int IceFreezeTime = 60;
        public const int BossStunTime = 15;
        public const float AccessoryDamageMultiplier = 0.3f;
        public const float NormalDirectDamageMultiplier = 1.2f;
        public const float ZombieDamageMultiplier = 1.5f;
        public const float SplashDamageMultiplier = 0.45f;
        public const float FirePatchDamageMultiplier = 0.35f;
        public const float ElectricLightningDamageMultiplier = 0.34f;
        public const float RockKnockbackMultiplier = 6.5f;
        public const int ElectricLightningCount = 5;
        public const float RockSpeedMultiplier = 0.9f;
        public const float StarlightSpeedMultiplier = 1.1f;
        public const float PoisonSpeedMultiplier = 1.5f;
        public const float FireIceAcceleration = 1.01f;
        public const float PoisonDeceleration = 0.985f;
        public const int NormalHomingStageIndex = 6;
        public const float NormalHomingRange = 920f;
        public const float NormalHomingInertia = 27f;
        public const float NormalHomingDelay = 12f;
        public const float NormalHomingMaxSpeedMultiplier = 1.18f;
        public const float ElectricLightningSpeedMultiplier = 0.3f;
        public const float ElectricLightningSizeMultiplier = 0.55f;
        public const int AdrenalineStormFireInterval = 2;
        public const int AdrenalineStormMinPeas = 5;
        public const int AdrenalineStormMaxPeas = 10;
        public const float AdrenalineStormSpreadDegrees = 15f;
        public const float AdrenalineStormDamageMultiplier = 0.05f;
        public const float AdrenalineStormMinSpeedMultiplier = 0.72f;
        public const float AdrenalineStormMaxSpeedMultiplier = 1.35f;
        public const float AdrenalineStormMinScale = 0.95f;
        public const float AdrenalineStormMaxScale = 1.05f;

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

        public int GetBaseDamage() => GetIntStageValue(GetCompletedStageIndex(), DamageColumn);

        public float GetShootSpeed() => GetFloatStageValue(GetCompletedStageIndex(), ShootSpeedColumn);

        public static string GetStageName(int stageIndex)
        {
            int clampedIndex = Utils.Clamp(stageIndex, 0, StageStats.GetLength(0) - 1);
            return StageStats[clampedIndex, StageNameColumn];
        }

        public static PeaShooterFireStyle GetFireStyle(int stageIndex)
        {
            if (stageIndex >= 8)
                return PeaShooterFireStyle.WildGatling;

            if (stageIndex >= 6)
                return PeaShooterFireStyle.Gatling;

            if (stageIndex >= 4)
                return PeaShooterFireStyle.ThreeLine;

            if (stageIndex >= 3)
                return PeaShooterFireStyle.Double;

            return PeaShooterFireStyle.Single;
        }

        public static string GetFireStyleKey(PeaShooterFireStyle style) => style switch
        {
            PeaShooterFireStyle.Double => "Mods.CalamityLegendsComeBack.PeaShooter.FireStyleDouble",
            PeaShooterFireStyle.ThreeLine => "Mods.CalamityLegendsComeBack.PeaShooter.FireStyleThreeLine",
            PeaShooterFireStyle.Gatling => "Mods.CalamityLegendsComeBack.PeaShooter.FireStyleGatling",
            PeaShooterFireStyle.WildGatling => "Mods.CalamityLegendsComeBack.PeaShooter.FireStyleWildGatling",
            _ => "Mods.CalamityLegendsComeBack.PeaShooter.FireStyleSingle"
        };

        public static string GetPeaNameKey(PeaShooterPeaType peaType) => peaType switch
        {
            PeaShooterPeaType.Electric => "Mods.CalamityLegendsComeBack.PeaShooter.PeaNameElectric",
            PeaShooterPeaType.Fire => "Mods.CalamityLegendsComeBack.PeaShooter.PeaNameFire",
            PeaShooterPeaType.Ice => "Mods.CalamityLegendsComeBack.PeaShooter.PeaNameIce",
            PeaShooterPeaType.Starlight => "Mods.CalamityLegendsComeBack.PeaShooter.PeaNameStarlight",
            PeaShooterPeaType.Poison => "Mods.CalamityLegendsComeBack.PeaShooter.PeaNamePoison",
            PeaShooterPeaType.Rock => "Mods.CalamityLegendsComeBack.PeaShooter.PeaNameRock",
            _ => "Mods.CalamityLegendsComeBack.PeaShooter.PeaNameNormal"
        };

        public static string GetPeaEffectKey(PeaShooterPeaType peaType) => peaType switch
        {
            PeaShooterPeaType.Electric => "Mods.CalamityLegendsComeBack.PeaShooter.PeaEffectElectric",
            PeaShooterPeaType.Fire => "Mods.CalamityLegendsComeBack.PeaShooter.PeaEffectFire",
            PeaShooterPeaType.Ice => "Mods.CalamityLegendsComeBack.PeaShooter.PeaEffectIce",
            PeaShooterPeaType.Starlight => "Mods.CalamityLegendsComeBack.PeaShooter.PeaEffectStarlight",
            PeaShooterPeaType.Poison => "Mods.CalamityLegendsComeBack.PeaShooter.PeaEffectPoison",
            PeaShooterPeaType.Rock => "Mods.CalamityLegendsComeBack.PeaShooter.PeaEffectRock",
            _ => "Mods.CalamityLegendsComeBack.PeaShooter.PeaEffectNormal"
        };

        private static int GetIntStageValue(int stageIndex, int column)
        {
            int clampedIndex = Utils.Clamp(stageIndex, 0, StageStats.GetLength(0) - 1);
            return int.TryParse(StageStats[clampedIndex, column], out int value) ? System.Math.Max(1, value) : 1;
        }

        private static float GetFloatStageValue(int stageIndex, int column)
        {
            int clampedIndex = Utils.Clamp(stageIndex, 0, StageStats.GetLength(0) - 1);
            return float.TryParse(StageStats[clampedIndex, column], out float value) ? System.Math.Max(0.01f, value) : 1f;
        }
    }
}
