using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal enum AMRProgressionStage
    {
        Initial,
        EyeOfCthulhu,
        EvilBoss,
        WallOfFlesh,
        AnyMechanicalBoss,
        Plantera,
        MoonLord,
        Providence,
        DevourerOfGods,
        Finale
    }

    internal static class AMRBalance
    {
        private static readonly int[] BaseDamages =
        {
            168,
            220,
            300,
            480,
            700,
            1050,
            1818,
            3000,
            4200,
            7000
        };

        public const int InitialDamage = 168;
        public const int BaseFireInterval = 30;
        public const int MechanicalFireInterval = 24;
        public const int ScopeChargeFrames = 90;
        public const int MinimumScopeChargeFrames = 12;
        public const int SlideFrames = 15;
        public const int SlideChainWindowFrames = 120;
        public const int SlideCooldownFrames = 30 * 60;

        public static AMRProgressionStage Stage
        {
            get
            {
                AMRProgressionStage stage = AMRProgressionStage.Initial;

                if (NPC.downedBoss1)
                    stage = AMRProgressionStage.EyeOfCthulhu;
                if (NPC.downedBoss2)
                    stage = AMRProgressionStage.EvilBoss;
                if (Main.hardMode)
                    stage = AMRProgressionStage.WallOfFlesh;
                if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                    stage = AMRProgressionStage.AnyMechanicalBoss;
                if (NPC.downedPlantBoss)
                    stage = AMRProgressionStage.Plantera;
                if (NPC.downedMoonlord)
                    stage = AMRProgressionStage.MoonLord;
                if (DownedBossSystem.downedProvidence)
                    stage = AMRProgressionStage.Providence;
                if (DownedBossSystem.downedDoG)
                    stage = AMRProgressionStage.DevourerOfGods;
                if (DownedBossSystem.downedExoMechs || DownedBossSystem.downedCalamitas)
                    stage = AMRProgressionStage.Finale;

                return stage;
            }
        }

        public static int CurrentDamage => BaseDamages[(int)Stage];
        public static int FireInterval => Stage >= AMRProgressionStage.AnyMechanicalBoss ? MechanicalFireInterval : BaseFireInterval;
        public static bool DeathMarkUnlocked => Stage >= AMRProgressionStage.EyeOfCthulhu;
        public static bool MetalJetUnlocked => Stage >= AMRProgressionStage.EvilBoss;
        public static bool SlideUnlocked => Stage >= AMRProgressionStage.WallOfFlesh;
        public static bool CalibrationUnlocked => Stage >= AMRProgressionStage.Plantera;
        public static bool CriticalOverflowUnlocked => Stage >= AMRProgressionStage.MoonLord;
        public static bool CoreRuptureUnlocked => Stage >= AMRProgressionStage.Providence;
        public static bool DimensionalSlideUnlocked => Stage >= AMRProgressionStage.DevourerOfGods;
        public static bool OnyxSequenceUnlocked => Stage >= AMRProgressionStage.Finale;

        public static float LeftProjectileSpeed => 32f + (int)Stage * 1.8f;
        public static float RightProjectileSpeed(float charge) => MathHelper.Lerp(38f, 62f, charge);
        public static float RightDamageMultiplier(float charge) => MathHelper.Lerp(1.15f, 2.5f, charge);
    }
}
