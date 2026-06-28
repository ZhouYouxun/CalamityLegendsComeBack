using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    internal enum YCRightLaserVisualTier
    {
        Cell,
        Classic,
        MoonLord,
        Providence,
        Yharon
    }

    public class BalanceYharimsCrystal
    {
        private const string SourceFile = "Weapons/YharimsCrystal/BalanceYharimsCrystal.cs";
        public const float BaseLeftBladeScale = 0.65f;
        public const int InitialLeftClickBaseDamage = 30;

        private static readonly int[] DefaultLeftClickBaseDamage =
        {
            InitialLeftClickBaseDamage, // Stage 0: Initial
            44,                         // Stage 1: Desert Scourge
            58,                         // Stage 2: Evil Boss T1
            88,                         // Stage 3: Wall of Flesh
            126,                        // Stage 4: Brimstone Elemental
            190,                        // Stage 5: Golem
            250,                        // Stage 6: Empress of Light
            360,                        // Stage 7: Moon Lord
            485,                        // Stage 8: Providence
            720                         // Stage 9: Yharon
        };

        private static readonly int[] DefaultRightClickBaseDamage =
        {
            38,  // Stage 0: Initial
            54,  // Stage 1: Desert Scourge
            72,  // Stage 2: Evil Boss T1
            116, // Stage 3: Wall of Flesh
            165, // Stage 4: Brimstone Elemental
            250, // Stage 5: Golem
            330, // Stage 6: Empress of Light
            470, // Stage 7: Moon Lord
            640, // Stage 8: Providence
            940  // Stage 9: Yharon
        };

        private static readonly object[,] DefaultProgressionUnlocks =
        {
            //                               Left behavior / projectile notes                         Right behavior / projectile notes
            { "Initial",                    "Blade loop, directed throw, EssenceFlame support",        "Cell laser, 2 drones, drone splinters" },
            { "Desert Scourge",             "Fire debuff upgrades to OnFire3",                         "Same projectile set, higher damage" },
            { "Evil Boss T1",               "Blade scale and recovery improve",                        "Same projectile set, higher damage" },
            { "Wall of Flesh",              "Directed throw keeps no-gravity charge dash",             "Classic sustained laser visual" },
            { "Brimstone Elemental",        "Burning shards release brimstone missiles",               "Classic laser, larger blade scale" },
            { "Golem",                      "Recovery improves and blade grows",                       "Right charge time is halved" },
            { "Empress of Light",           "Burning shards release rainbow bolts",                    "Same laser set, higher damage" },
            { "Moon Lord",                  "Blade grows again",                                       "Moon Lord beam visual" },
            { "Providence",                 "Low-life recovery improves",                              "Providence holy-ray beam visual" },
            { "Yharon",                     "Ultimate empowers the current weapon form",               "Yharon tornado beam visual" }
        };

        public int GetCompletedStageIndex()
        {
            int stage = 0;
            if (DownedDesertScourge)
                stage = 1;
            if (DownedEvilTier1)
                stage = 2;
            if (DownedWallOfFlesh)
                stage = 3;
            if (DownedBrimstoneElemental)
                stage = 4;
            if (DownedGolem)
                stage = 5;
            if (DownedEmpress)
                stage = 6;
            if (DownedMoonLord)
                stage = 7;
            if (DownedProvidence)
                stage = 8;
            if (DownedYharon)
                stage = 9;

            return stage;
        }

        public int GetLeftClickBaseDamage() => GetValueForStage(GetLeftClickBaseDamageValues(), GetCompletedStageIndex());
        public int GetRightClickBaseDamage() => GetValueForStage(GetRightClickBaseDamageValues(), GetCompletedStageIndex());

        public static int GetInitialLeftClickBaseDamage() => GetLeftClickBaseDamageValues()[0];

        public float GetLeftBladeScale()
        {
            float scale = BaseLeftBladeScale;
            if (DownedEvilTier1)
                scale *= 1.15f;
            if (DownedBrimstoneElemental)
                scale *= 1.10f;
            if (DownedGolem)
                scale *= 1.10f;
            if (DownedEmpress)
                scale *= 1.10f;
            if (DownedMoonLord)
                scale *= 1.10f;
            if (DownedProvidence)
                scale *= 1.10f;
            if (DownedYharon)
                scale *= 1.10f;

            return scale;
        }

        public int GetRightChargeFrames() => DownedGolem ? 45 : 90;

        internal YCRightLaserVisualTier GetRightLaserTier()
        {
            if (DownedYharon)
                return YCRightLaserVisualTier.Yharon;
            if (DownedProvidence)
                return YCRightLaserVisualTier.Providence;
            if (DownedMoonLord)
                return YCRightLaserVisualTier.MoonLord;
            if (DownedWallOfFlesh)
                return YCRightLaserVisualTier.Classic;

            return YCRightLaserVisualTier.Cell;
        }

        public int GetPassiveManaRestore(Player player)
        {
            if (DownedProvidence && player.statLife <= player.statLifeMax2 / 2)
                return 5;
            if (DownedGolem)
                return 4;
            if (DownedEvilTier1)
                return 3;

            return 1;
        }

        public int GetPassiveLifeRestore(Player player)
        {
            if (DownedProvidence && player.statLife <= player.statLifeMax2 / 2)
                return 10;
            if (DownedGolem)
                return 8;
            if (DownedEvilTier1)
                return 7;

            return 5;
        }

        public int GetFireDebuffType()
        {
            if (DownedSupremeCalamitas)
                return ModContent.BuffType<TrueVulnerabilityHex>();
            if (DownedYharon)
                return ModContent.BuffType<Dragonfire>();
            if (DownedDoG)
                return ModContent.BuffType<GodSlayerInferno>();
            if (DownedProvidence)
                return ModContent.BuffType<BanishingFire>();
            if (DownedMoonLord)
                return ModContent.BuffType<HolyFlames>();
            if (NPC.downedAncientCultist)
                return ModContent.BuffType<Daybroken>();
            if (DownedBrimstoneElemental)
                return ModContent.BuffType<BrimstoneFlames>();
            if (DownedWallOfFlesh)
                return ModContent.BuffType<DemonicFlames>();
            if (DownedDesertScourge)
                return BuffID.OnFire3;

            return BuffID.OnFire;
        }

        public bool ShouldShardReleaseBrimstoneMissiles() => DownedBrimstoneElemental;
        public bool ShouldShardReleaseRainbowBolts() => DownedEmpress;
        public bool UltimateEmpowersAfterUse() => DownedYharon;

        public string BuildProgressionSummary(System.Func<string, string> localize)
        {
            List<string> parts = new();
            int stage = GetCompletedStageIndex();
            int maxStage = DefaultProgressionUnlocks.GetLength(0) - 1;
            parts.Add(string.Format(localize("Growth_Header"), stage, maxStage, GetLeftBladeScale().ToString("0.00")));

            if (stage >= 0 && stage < DefaultProgressionUnlocks.GetLength(0))
                parts.Add(localize($"Growth_Stage{stage}"));

            return string.Join("\n", parts);
        }

        private int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }

        private static int[] GetLeftClickBaseDamageValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultLeftClickBaseDamage), DefaultLeftClickBaseDamage);

        private static int[] GetRightClickBaseDamageValues() =>
            RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultRightClickBaseDamage), DefaultRightClickBaseDamage);

        public static bool DownedDesertScourge => DownedBossSystem.downedDesertScourge;
        public static bool DownedEvilTier1 => NPC.downedBoss2;
        public static bool DownedWallOfFlesh => Main.hardMode;
        public static bool DownedBrimstoneElemental => DownedBossSystem.downedBrimstoneElemental;
        public static bool DownedGolem => NPC.downedGolemBoss;
        public static bool DownedEmpress => NPC.downedEmpressOfLight;
        public static bool DownedMoonLord => NPC.downedMoonlord;
        public static bool DownedProvidence => DownedBossSystem.downedProvidence;
        public static bool DownedDoG => DownedBossSystem.downedDoG;
        public static bool DownedYharon => DownedBossSystem.downedYharon;
        public static bool DownedSupremeCalamitas => DownedBossSystem.downedCalamitas;
    }
}
