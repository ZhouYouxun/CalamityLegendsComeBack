using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal static class BB_Balance
    {
        private static float[] BladeScales => new float[] { 0.75f, 1.05f, 1.35f, 1.65f, 1.95f, 2.25f };

        private static readonly float[] BladeGiantScaleFactors = { 2.4f, 2.4f, 2.4f, 2.4f, 2.4f, 2.4f };
        private static readonly int[] BladeGiantGrowFrames = { 42, 42, 42, 42, 42, 42 };
        private static readonly int[] BladeGiantShrinkFrames = { 38, 38, 38, 38, 38, 38 };

        private static readonly float[] WaveSizeScales = { 1f, 1.16f, 1.32f, 1.48f };
        private static readonly float[] WaveSpeedDrags = { 0.99f, 0.992f, 0.993f, 0.994f };
        private static readonly bool[] WaveTileCollides = { true, false, false, false };
        private static readonly float[] WaveTrailingStarDamageFactors = { 0f, 0f, 0.18f, 0.24f };

        private static readonly int[] TideMaxValues = { 2, 3, 4, 5, 6, 7, 8 };

        private static readonly float[] ShortDashSpeedMultipliers = { 0.5f, 0.575f, 0.65f, 0.725f, 0.8f };
        private static readonly float[] ShortDashContactDamageMultipliers = { 1f, 1f, 1f, 2f, 2f };
        private static readonly bool[] ShortDashEnemyReboundUnlocks = { false, true, true, true, true };

        private static readonly float[] SpinRushSizeMultipliers = { 1f, 1.2f, 1.5f, 2.75f };
        private static readonly float[] SpinRushSpeedMultipliers = { 1f, 1.15f, 1.25f, 1.35f };

        private static readonly float[] QuickDashBaseDamageMultipliers = { 1f, 1.25f, 1.55f, 2f };
        private static readonly int[] QuickDashCooldowns = { 120, 120, 90, 60 };

        private static readonly int[] ShurikenPenetrates = { 1, -1, -1, -1 };
        private static readonly int[] ShurikenStickySliceCounts = { 0, 3, 4, 5 };
        private static readonly bool[] ShurikenDeathSpawnsShpcExplosion = { false, false, true, true };
        private static readonly bool[] ShurikenDeathSpawnsCrossLights = { false, false, false, true };
        private static readonly float[] ShurikenTideHomingRanges = { 900f, 1040f, 1180f, 1320f };
        private static readonly float[] ShurikenRotationSpeeds = { 0.55f, 0.59f, 0.63f, 0.67f };
        private static readonly bool[] ShurikenStickySlashUnlocks = { false, false, true, true };

        private const int BaseShurikenVolley = 3;
        private const int TidePerExtraShuriken = 2;
        private const float TideHomingBlendTargetWeight = 25f;
        private const float TideHomingBlendTotalWeight = 18f;
        private const float TideHomingFinalSpeed = 14f;
        private const float TideHomingFinalSpeedLerp = 0.08f;
        private const float NonEmpoweredShurikenAcceleration = 1.01f;
        private const float StickySlashDamageFactor = 0.42f;
        private const float StickySlashBaseScale = 0.9f;
        private const float StickySlashScalePerTier = 0.08f;

        private const float SmallSlashScale = 0.41f;
        private const float GiantSlashScale = 0.775f;
        private const float SmallSlashDamageFactor = 0.28f;
        private const float GiantSlashDamageFactor = 0.34f;
        private const int GiantLaserCount = 3;
        private const float GiantLaserSpeed = 18f;
        private const float GiantLaserDamageFactor = 0.5f;

        public static bool SuperDashUsesTestUnlock = false;
        public static bool SuperDashRequiresFishron = true;
        public static bool SuperDashRequiresFullTide = true;

        public static int MaxTideCap => TideMaxValues[TideMaxValues.Length - 1];

        public static bool CanUseShortDash => NPC.downedBoss1 || Main.hardMode;
        public static bool CanUseSpinRush => Main.hardMode;
        public static bool CanUseQuickDash => Main.hardMode;
        public static bool HasDesignedSuperDashUnlock => !SuperDashRequiresFishron || NPC.downedFishron;

        public static bool CanActivateSuperDash(BBEXPlayer tidePlayer)
        {
            if (SuperDashUsesTestUnlock)
                return true;

            if (!HasDesignedSuperDashUnlock)
                return false;

            return !SuperDashRequiresFullTide || (tidePlayer?.TideFull ?? false);
        }

        public static BladeGrowthProfile GetBladeGrowthProfile()
        {
            int tier = GetBladeGrowthTier();
            return new BladeGrowthProfile(tier, BladeScales[tier], BladeGiantScaleFactors[tier], BladeGiantGrowFrames[tier], BladeGiantShrinkFrames[tier]);
        }

        public static WaveProfile GetWaveProfile()
        {
            int tier = GetWaveGrowthTier();
            return new WaveProfile(tier, WaveSizeScales[tier], WaveSpeedDrags[tier], WaveTileCollides[tier], WaveTrailingStarDamageFactors[tier]);
        }

        public static int GetCurrentTideMax()
        {
            return TideMaxValues[GetTideGrowthTier()];
        }

        public static int GetShurikenVolleyCount(int tideValue)
        {
            return BaseShurikenVolley + tideValue / TidePerExtraShuriken;
        }

        public static ShurikenProfile GetShurikenProfile()
        {
            int tier = GetShurikenGrowthTier();
            return new ShurikenProfile(
                tier,
                ShurikenPenetrates[tier],
                ShurikenStickySliceCounts[tier],
                ShurikenDeathSpawnsShpcExplosion[tier],
                ShurikenDeathSpawnsCrossLights[tier],
                ShurikenTideHomingRanges[tier],
                ShurikenRotationSpeeds[tier],
                ShurikenStickySlashUnlocks[tier],
                TideHomingBlendTargetWeight,
                TideHomingBlendTotalWeight,
                TideHomingFinalSpeed,
                TideHomingFinalSpeedLerp,
                NonEmpoweredShurikenAcceleration,
                StickySlashDamageFactor,
                StickySlashBaseScale + tier * StickySlashScalePerTier);
        }

        public static ShortDashProfile GetShortDashProfile()
        {
            int tier = GetShortDashGrowthTier();
            return new ShortDashProfile(tier, ShortDashSpeedMultipliers[tier], ShortDashContactDamageMultipliers[tier], ShortDashEnemyReboundUnlocks[tier]);
        }

        public static SpinRushProfile GetSpinRushProfile()
        {
            int tier = GetSpinRushGrowthTier();
            return new SpinRushProfile(tier, SpinRushSizeMultipliers[tier], SpinRushSpeedMultipliers[tier]);
        }

        public static QuickDashProfile GetQuickDashProfile()
        {
            int tier = GetQuickDashGrowthTier();
            return new QuickDashProfile(tier, QuickDashBaseDamageMultipliers[tier], QuickDashCooldowns[tier]);
        }

        public static SwingHitEffectProfile GetSwingHitEffectProfile()
        {
            return new SwingHitEffectProfile(Main.hardMode, SmallSlashScale, GiantSlashScale, SmallSlashDamageFactor, GiantSlashDamageFactor, GiantLaserCount, GiantLaserSpeed, GiantLaserDamageFactor);
        }

        private static int GetBladeGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedYharon)
                return 5;
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 4;
            if (NPC.downedFishron)
                return 3;
            if (Main.hardMode)
                return 2;
            if (NPC.downedBoss1)
                return 1;
            return 0;
        }

        private static int GetWaveGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 3;
            if (NPC.downedFishron)
                return 2;
            if (Main.hardMode)
                return 1;
            return 0;
        }

        private static int GetTideGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedYharon)
                return 6;
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 5;
            if (NPC.downedMoonlord)
                return 4;
            if (NPC.downedFishron)
                return 3;
            if (CalamityMod.DownedBossSystem.downedCalamitasClone || NPC.downedPlantBoss)
                return 2;
            if (Main.hardMode)
                return 1;
            return 0;
        }

        private static int GetShurikenGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 3;
            if (NPC.downedFishron)
                return 2;
            if (Main.hardMode)
                return 1;
            return 0;
        }

        private static int GetShortDashGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 4;
            if (NPC.downedFishron)
                return 3;
            if (CalamityMod.DownedBossSystem.downedCalamitasClone || NPC.downedPlantBoss)
                return 2;
            if (Main.hardMode)
                return 1;
            return 0;
        }

        private static int GetSpinRushGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedYharon)
                return 3;
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 2;
            if (NPC.downedFishron)
                return 1;
            return 0;
        }

        private static int GetQuickDashGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedYharon)
                return 3;
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 2;
            if (NPC.downedFishron)
                return 1;
            return 0;
        }

        internal readonly struct BladeGrowthProfile
        {
            public readonly int GrowthTier;
            public readonly float BladeScale;
            public readonly float GiantScaleFactor;
            public readonly int GiantGrowFrames;
            public readonly int GiantShrinkFrames;

            public BladeGrowthProfile(int growthTier, float bladeScale, float giantScaleFactor, int giantGrowFrames, int giantShrinkFrames)
            {
                GrowthTier = growthTier;
                BladeScale = bladeScale;
                GiantScaleFactor = giantScaleFactor;
                GiantGrowFrames = giantGrowFrames;
                GiantShrinkFrames = giantShrinkFrames;
            }
        }

        internal readonly struct WaveProfile
        {
            public readonly int GrowthTier;
            public readonly float SizeScale;
            public readonly float SpeedDrag;
            public readonly bool TileCollide;
            public readonly float TrailingStarDamageFactor;

            public WaveProfile(int growthTier, float sizeScale, float speedDrag, bool tileCollide, float trailingStarDamageFactor)
            {
                GrowthTier = growthTier;
                SizeScale = sizeScale;
                SpeedDrag = speedDrag;
                TileCollide = tileCollide;
                TrailingStarDamageFactor = trailingStarDamageFactor;
            }
        }

        internal readonly struct ShurikenProfile
        {
            public readonly int GrowthTier;
            public readonly int Penetrate;
            public readonly int StickySliceCount;
            public readonly bool SpawnsShpcExplosionOnDeath;
            public readonly bool SpawnsCrossLightsOnDeath;
            public readonly float TideHomingRange;
            public readonly float RotationSpeed;
            public readonly bool UnlocksStickySlash;
            public readonly float TideHomingTargetWeight;
            public readonly float TideHomingTotalWeight;
            public readonly float TideHomingFinalSpeed;
            public readonly float TideHomingFinalSpeedLerp;
            public readonly float NonEmpoweredAcceleration;
            public readonly float StickySlashDamageFactor;
            public readonly float StickySlashScale;

            public bool CanStick => StickySliceCount > 0;

            public ShurikenProfile(int growthTier, int penetrate, int stickySliceCount, bool spawnsShpcExplosionOnDeath, bool spawnsCrossLightsOnDeath, float tideHomingRange, float rotationSpeed, bool unlocksStickySlash, float tideHomingTargetWeight, float tideHomingTotalWeight, float tideHomingFinalSpeed, float tideHomingFinalSpeedLerp, float nonEmpoweredAcceleration, float stickySlashDamageFactor, float stickySlashScale)
            {
                GrowthTier = growthTier;
                Penetrate = penetrate;
                StickySliceCount = stickySliceCount;
                SpawnsShpcExplosionOnDeath = spawnsShpcExplosionOnDeath;
                SpawnsCrossLightsOnDeath = spawnsCrossLightsOnDeath;
                TideHomingRange = tideHomingRange;
                RotationSpeed = rotationSpeed;
                UnlocksStickySlash = unlocksStickySlash;
                TideHomingTargetWeight = tideHomingTargetWeight;
                TideHomingTotalWeight = tideHomingTotalWeight;
                TideHomingFinalSpeed = tideHomingFinalSpeed;
                TideHomingFinalSpeedLerp = tideHomingFinalSpeedLerp;
                NonEmpoweredAcceleration = nonEmpoweredAcceleration;
                StickySlashDamageFactor = stickySlashDamageFactor;
                StickySlashScale = stickySlashScale;
            }
        }

        internal readonly struct ShortDashProfile
        {
            public readonly int GrowthTier;
            public readonly float SpeedMultiplier;
            public readonly float ContactDamageMultiplier;
            public readonly bool EnemyReboundUnlocked;

            public ShortDashProfile(int growthTier, float speedMultiplier, float contactDamageMultiplier, bool enemyReboundUnlocked)
            {
                GrowthTier = growthTier;
                SpeedMultiplier = speedMultiplier;
                ContactDamageMultiplier = contactDamageMultiplier;
                EnemyReboundUnlocked = enemyReboundUnlocked;
            }
        }

        internal readonly struct SpinRushProfile
        {
            public readonly int GrowthTier;
            public readonly float SizeMultiplier;
            public readonly float SpeedMultiplier;

            public SpinRushProfile(int growthTier, float sizeMultiplier, float speedMultiplier)
            {
                GrowthTier = growthTier;
                SizeMultiplier = sizeMultiplier;
                SpeedMultiplier = speedMultiplier;
            }
        }

        internal readonly struct QuickDashProfile
        {
            public readonly int GrowthTier;
            public readonly float BaseDamageMultiplier;
            public readonly int DashCooldown;

            public QuickDashProfile(int growthTier, float baseDamageMultiplier, int dashCooldown)
            {
                GrowthTier = growthTier;
                BaseDamageMultiplier = baseDamageMultiplier;
                DashCooldown = dashCooldown;
            }
        }

        internal readonly struct SwingHitEffectProfile
        {
            public readonly bool SlashBurstUnlocked;
            public readonly float SmallSlashScale;
            public readonly float GiantSlashScale;
            public readonly float SmallSlashDamageFactor;
            public readonly float GiantSlashDamageFactor;
            public readonly int GiantLaserCount;
            public readonly float GiantLaserSpeed;
            public readonly float GiantLaserDamageFactor;

            public SwingHitEffectProfile(bool slashBurstUnlocked, float smallSlashScale, float giantSlashScale, float smallSlashDamageFactor, float giantSlashDamageFactor, int giantLaserCount, float giantLaserSpeed, float giantLaserDamageFactor)
            {
                SlashBurstUnlocked = slashBurstUnlocked;
                SmallSlashScale = smallSlashScale;
                GiantSlashScale = giantSlashScale;
                SmallSlashDamageFactor = smallSlashDamageFactor;
                GiantSlashDamageFactor = giantSlashDamageFactor;
                GiantLaserCount = giantLaserCount;
                GiantLaserSpeed = giantLaserSpeed;
                GiantLaserDamageFactor = giantLaserDamageFactor;
            }
        }
    }
}
