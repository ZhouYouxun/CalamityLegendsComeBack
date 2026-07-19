using CalamityLegendsComeBack.Accssory.MC.General;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal enum MalachiteProgressionStage
    {
        Initial                  = 0,
        KingSlime                = 1,
        EyeOfCthulhu             = 2,
        EvilBoss                 = 3,
        Skeletron                = 4,
        QueenBee                 = 5,
        WallOfFlesh              = 6,
        AnyMechanicalBoss        = 7,
        Plantera                 = 8,
        Golem                    = 9,
        PlaguebringerGoliath     = 10,
        MoonLord                 = 11,
        Providence               = 12,
        Polterghast              = 13,
        OldDuke                  = 14,
        DevourerOfGods           = 15,
        Yharon                   = 16,
        ExoMechsAndCalamitas     = 17
    }

    internal enum MalachiteStageSwitch
    {
        LeftNoGravity            = 1,
        LeftMultiThrow           = 2,
        LeftScatterBurst         = 3,
        LeftOldDukeUpgrade       = 4,
        RightUnlocked            = 5,
        RightMoveBoost           = 6,
        RightLeftHaste           = 7,
        RightNoStealthCost       = 8,
        StealthAceKunai          = 9,
        StealthPlusWall          = 10,
        StealthPlusPlantera      = 11,
        StealthPlusPlaguebringer = 12,
        StealthPlusMoonLord      = 13
    }

    internal static class MalachiteBalance
    {
        private const string SourceFile = "Weapons/Malachite/MalachiteBalance.cs";
        private const int DamageLeftColumn = 1;
        private const int DamageRightColumn = 2;

        // Main edit table. Columns: stage, left-click base damage, right-click base damage.
        private static readonly object[,] DefaultDamageTable =
        {
            //                                          Left   Right
            { "Initial",                                  12,      8 },
            { "King Slime",                               14,     10 },
            { "Eye of Cthulhu",                           18,     12 },
            { "Evil Boss",                                28,     18 },
            { "Skeletron",                                38,     24 },
            { "Queen Bee",                                44,     29 },
            { "Hardmode",                                 48,     32 },
            { "Any Mechanical Boss",                      85,     60 },
            { "Plantera",                                130,     95 },
            { "Golem",                                   155,    110 },
            { "Plaguebringer Goliath",                   230,    165 },
            { "Moon Lord",                               480,    360 },
            { "Providence",                              490,    370 },
            { "Polterghast",                             520,    390 },
            { "Old Duke",                                620,    460 },
            { "Devourer of Gods",                       1280,    980 },
            { "Yharon",                                  1380,   1060 },
            { "Exo Mechs and Supreme Calamitas",        2000,  1750 }
        };

        // Stage switches. Any later row should include earlier unlocked behavior.
        private static readonly object[,] DefaultStageSwitches =
        {
            //                                      NoGrav  Multi  Scatter OldDuke Right  Move   Haste  FreeSS Ace    +WoF   +Plan  +PBG   +ML
            { "Initial",                            false,  false,  false,  false,  false, false, false, false, false, false, false, false, false },
            { "King Slime",                         false,  false,  false,  false,  true,  false, false, false, false, false, false, false, false },
            { "Eye of Cthulhu",                     false,  false,  false,  false,  true,  false, false, false, false, false, false, false, false },
            { "Evil Boss",                          false,  false,  false,  false,  true,  false, false, false, false, false, false, false, false },
            { "Skeletron",                          false,  false,  false,  false,  true,  false, false, false, false, false, false, false, false },
            { "Queen Bee",                          true,   false,  false,  false,  true,  true,  false, false, false, false, false, false, false },
            { "Hardmode",                           true,   true,   false,  false,  true,  true,  false, false, true,  true,  false, false, false },
            { "Any Mechanical Boss",                true,   true,   false,  false,  true,  true,  false, false, true,  true,  false, false, false },
            { "Plantera",                           true,   true,   false,  false,  true,  true,  false, false, true,  true,  true,  false, false },
            { "Golem",                              true,   true,   false,  false,  true,  true,  false, false, true,  true,  true,  false, false },
            { "Plaguebringer Goliath",              true,   true,   true,   false,  true,  true,  true,  false, true,  true,  true,  true,  false },
            { "Moon Lord",                          true,   true,   true,   false,  true,  true,  true,  false, true,  true,  true,  true,  true  },
            { "Providence",                         true,   true,   true,   false,  true,  true,  true,  false, true,  true,  true,  true,  true  },
            { "Polterghast",                        true,   true,   true,   false,  true,  true,  true,  false, true,  true,  true,  true,  true  },
            { "Old Duke",                           true,   true,   true,   true,   true,  true,  true,  true,  true,  true,  true,  true,  true  },
            { "Devourer of Gods",                   true,   true,   true,   true,   true,  true,  true,  true,  true,  true,  true,  true,  true  },
            { "Yharon",                             true,   true,   true,   true,   true,  true,  true,  true,  true,  true,  true,  true,  true  },
            { "Exo Mechs and Supreme Calamitas",    true,   true,   true,   true,   true,  true,  true,  true,  true,  true,  true,  true,  true  }
        };

        // Columns: left-click projectile speed, right-click projectile speed.
        private static readonly float[,] DefaultProjectileVelocities =
        {
            //    Left  Right
            {     18f,   24f }, // Pre-hardmode
            {     22f,   30f }, // Hardmode to Plantera
            {     26f,   36f }, // Plantera to Moon Lord
            {     30f,   42f }  // Post-Moon Lord
        };

        public const int DepletionBurstKunaiCount = 7;
        public const int RightFeatherMaxCount = 23;
        public const int RightFeatherGenerationFrames = 36;
        public const int PeacockBoxFeatherGenerationFrames = 60;
        public const int RightFeatherReleaseSpacingFrames = 2;
        public const int LeftClickScatterInterval = 9;
        public const int LeftClickScatterWaveCount = 2;
        public const int RightClickBoostFrames = 3 * 60;
        public const int LeftClickHasteFrames = 60;
        public const int FinaleStealthAttackAdvanceFrames = 5 * 60;

        public static int StageIndex => GetCompletedStageIndex();
        public static bool DownedWallOfFlesh => StageIndex >= (int)MalachiteProgressionStage.WallOfFlesh;
        public static bool DownedPlaguebringerGoliath => StageIndex >= (int)MalachiteProgressionStage.PlaguebringerGoliath;
        public static bool DownedMoonLord => StageIndex >= (int)MalachiteProgressionStage.MoonLord;
        public static bool DownedOldDuke => StageIndex >= (int)MalachiteProgressionStage.OldDuke;

        public static bool NormalKunaiIgnoresGravity(Player player) =>
            StageSwitch(MalachiteStageSwitch.LeftNoGravity) ||
            player.GetModPlayer<MCGeneralPlayer>().NormalKunaiIgnoresGravity;

        public static bool RightClickUnlocked => StageSwitch(MalachiteStageSwitch.RightUnlocked);
        public static bool RightClickMoveBoostUnlocked => StageSwitch(MalachiteStageSwitch.RightMoveBoost);
        public static bool RightClickLeftHasteUnlocked => StageSwitch(MalachiteStageSwitch.RightLeftHaste);
        public static bool RightClickConsumesNoStealth => StageSwitch(MalachiteStageSwitch.RightNoStealthCost);
        public static bool StealthAceKunaiUnlocked => StageSwitch(MalachiteStageSwitch.StealthAceKunai);

        public static float LeftClickUseSpeedMultiplier => DownedWallOfFlesh ? 1f : 1.35f;

        public static int NormalLeftClickKunaiCount =>
            StageSwitch(MalachiteStageSwitch.LeftOldDukeUpgrade) ? 3 :
            StageSwitch(MalachiteStageSwitch.LeftMultiThrow) ? 2 :
            1;

        public static int StoredPeacockKunaiPerLeftClick => NormalLeftClickKunaiCount;

        public static bool LeftClickScatterUnlocked => StageSwitch(MalachiteStageSwitch.LeftScatterBurst);

        public static int LeftClickScatterKunaiPerWave =>
            StageSwitch(MalachiteStageSwitch.LeftOldDukeUpgrade) ? 7 : 5;

        public static int StealthKunaiCount
        {
            get
            {
                int count = 15;
                if (StageSwitch(MalachiteStageSwitch.StealthPlusWall))
                    count += 2;
                if (StageSwitch(MalachiteStageSwitch.StealthPlusPlantera))
                    count += 2;
                if (StageSwitch(MalachiteStageSwitch.StealthPlusPlaguebringer))
                    count += 2;
                if (StageSwitch(MalachiteStageSwitch.StealthPlusMoonLord))
                    count += 2;

                return Utils.Clamp(count, 15, RightFeatherMaxCount);
            }
        }

        public static int GetCompletedStageIndex()
        {
            MalachiteProgressionStage stage = MalachiteProgressionStage.Initial;

            if (NPC.downedSlimeKing)
                stage = MalachiteProgressionStage.KingSlime;
            if (NPC.downedBoss1)
                stage = MalachiteProgressionStage.EyeOfCthulhu;
            if (NPC.downedBoss2)
                stage = MalachiteProgressionStage.EvilBoss;
            if (NPC.downedBoss3)
                stage = MalachiteProgressionStage.Skeletron;
            if (NPC.downedQueenBee)
                stage = MalachiteProgressionStage.QueenBee;
            if (Main.hardMode)
                stage = MalachiteProgressionStage.WallOfFlesh;
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                stage = MalachiteProgressionStage.AnyMechanicalBoss;
            if (NPC.downedPlantBoss)
                stage = MalachiteProgressionStage.Plantera;
            if (NPC.downedGolemBoss)
                stage = MalachiteProgressionStage.Golem;
            if (DownedBossSystem.downedPlaguebringer)
                stage = MalachiteProgressionStage.PlaguebringerGoliath;
            if (NPC.downedMoonlord)
                stage = MalachiteProgressionStage.MoonLord;
            if (DownedBossSystem.downedProvidence)
                stage = MalachiteProgressionStage.Providence;
            if (DownedBossSystem.downedPolterghast)
                stage = MalachiteProgressionStage.Polterghast;
            if (DownedBossSystem.downedBoomerDuke)
                stage = MalachiteProgressionStage.OldDuke;
            if (DownedBossSystem.downedDoG)
                stage = MalachiteProgressionStage.DevourerOfGods;
            if (DownedBossSystem.downedYharon)
                stage = MalachiteProgressionStage.Yharon;
            if (DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas)
                stage = MalachiteProgressionStage.ExoMechsAndCalamitas;

            return (int)stage;
        }

        public static int GetLeftClickBaseDamage() => GetDamage(DamageLeftColumn);

        public static int GetRightClickBaseDamage() => GetDamage(DamageRightColumn);

        public static int GetInitialLeftClickBaseDamage()
        {
            int fallback = (int)DefaultDamageTable[0, DamageLeftColumn];
            return RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(DefaultDamageTable), 0, DamageLeftColumn, fallback);
        }

        public static int GetLeftClickTooltipTier()
        {
            if (StageSwitch(MalachiteStageSwitch.LeftOldDukeUpgrade))
                return 4;
            if (StageSwitch(MalachiteStageSwitch.LeftScatterBurst))
                return 3;
            if (StageSwitch(MalachiteStageSwitch.LeftMultiThrow))
                return 2;
            if (StageSwitch(MalachiteStageSwitch.LeftNoGravity))
                return 1;
            return 0;
        }

        public static int GetRightClickTooltipTier()
        {
            if (StageSwitch(MalachiteStageSwitch.RightNoStealthCost))
                return 4;
            if (StageSwitch(MalachiteStageSwitch.RightLeftHaste))
                return 3;
            if (StageSwitch(MalachiteStageSwitch.RightMoveBoost))
                return 2;
            if (StageSwitch(MalachiteStageSwitch.RightUnlocked))
                return 1;
            return 0;
        }

        public static int GetStealthTooltipTier()
        {
            if (StageSwitch(MalachiteStageSwitch.StealthPlusMoonLord))
                return 4;
            if (StageSwitch(MalachiteStageSwitch.StealthPlusPlaguebringer))
                return 3;
            if (StageSwitch(MalachiteStageSwitch.StealthPlusPlantera))
                return 2;
            if (StageSwitch(MalachiteStageSwitch.StealthPlusWall))
                return 1;
            return 0;
        }

        public static int GetVelocityStageIndex()
        {
            if (!DownedWallOfFlesh)
                return 0;
            if (StageIndex < (int)MalachiteProgressionStage.Plantera)
                return 1;
            if (!DownedMoonLord)
                return 2;
            return 3;
        }

        public static float GetLeftClickVelocity()
        {
            int stage = GetVelocityStageIndex();
            float fallback = DefaultProjectileVelocities[stage, 0];
            return RuntimeBalanceData.GetSourceFloatTableColumn(SourceFile, nameof(DefaultProjectileVelocities), stage, 0, fallback);
        }

        public static float GetRightClickVelocity()
        {
            int stage = GetVelocityStageIndex();
            float fallback = DefaultProjectileVelocities[stage, 1];
            return RuntimeBalanceData.GetSourceFloatTableColumn(SourceFile, nameof(DefaultProjectileVelocities), stage, 1, fallback);
        }

        private static int GetDamage(int column)
        {
            int stageIndex = Utils.Clamp(StageIndex, 0, DefaultDamageTable.GetLength(0) - 1);
            int fallback = (int)DefaultDamageTable[stageIndex, column];
            return System.Math.Max(1, RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(DefaultDamageTable), stageIndex, column, fallback));
        }

        private static bool StageSwitch(MalachiteStageSwitch column)
        {
            int stageIndex = Utils.Clamp(StageIndex, 0, DefaultStageSwitches.GetLength(0) - 1);
            bool fallback = (bool)DefaultStageSwitches[stageIndex, (int)column];
            return RuntimeBalanceData.GetSourceTableBool(SourceFile, nameof(DefaultStageSwitches), stageIndex, (int)column, fallback);
        }
    }
}
