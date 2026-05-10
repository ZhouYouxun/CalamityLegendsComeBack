using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal enum BlossomFluxProgressionStage
    {
        Start = 0,
        EyeOfCthulhu = 1,
        QueenBee = 2,
        WallOfFlesh = 3,
        MechBoss = 4,
        Plantera = 5,
        PlaguebringerGoliath = 6,
        MoonLord = 7,
        Polterghast = 8,
        DevourerOfGods = 9
    }

    internal static class BlossomFluxProgression
    {
        public static int StageIndex => (int)GetDefeatedStage();

        public static bool DownedAtLeast(BlossomFluxProgressionStage stage) => GetDefeatedStage() >= stage;

        public static bool DownedAnyMechBoss() => NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;

        public static bool DownedAnyBossOrMiniboss()
        {
            return GetDefeatedStage() > BlossomFluxProgressionStage.Start ||
                NPC.downedSlimeKing ||
                NPC.downedBoss2 ||
                NPC.downedBoss3 ||
                NPC.downedGoblins ||
                NPC.downedFrost ||
                NPC.downedPirates ||
                NPC.downedGolemBoss ||
                DownedBossSystem.downedDesertScourge ||
                DownedBossSystem.downedCrabulon ||
                DownedBossSystem.downedHiveMind ||
                DownedBossSystem.downedPerforator ||
                DownedBossSystem.downedSlimeGod ||
                DownedBossSystem.downedCryogen ||
                DownedBossSystem.downedAquaticScourge ||
                DownedBossSystem.downedBrimstoneElemental ||
                DownedBossSystem.downedGSS ||
                DownedBossSystem.downedCLAM ||
                DownedBossSystem.downedCragmawMire ||
                DownedBossSystem.downedMauler ||
                DownedBossSystem.downedProvidence ||
                DownedBossSystem.downedYharon ||
                DownedBossSystem.downedExoMechs ||
                DownedBossSystem.downedCalamitas;
        }

        private static BlossomFluxProgressionStage GetDefeatedStage()
        {
            BlossomFluxProgressionStage stage = BlossomFluxProgressionStage.Start;

            if (NPC.downedBoss1)
                stage = BlossomFluxProgressionStage.EyeOfCthulhu;

            if (NPC.downedQueenBee)
                stage = BlossomFluxProgressionStage.QueenBee;

            if (Main.hardMode)
                stage = BlossomFluxProgressionStage.WallOfFlesh;

            if (DownedAnyMechBoss())
                stage = BlossomFluxProgressionStage.MechBoss;

            if (NPC.downedPlantBoss)
                stage = BlossomFluxProgressionStage.Plantera;

            if (DownedBossSystem.downedPlaguebringer)
                stage = BlossomFluxProgressionStage.PlaguebringerGoliath;

            if (NPC.downedMoonlord)
                stage = BlossomFluxProgressionStage.MoonLord;

            if (DownedBossSystem.downedPolterghast)
                stage = BlossomFluxProgressionStage.Polterghast;

            if (DownedBossSystem.downedDoG)
                stage = BlossomFluxProgressionStage.DevourerOfGods;

            return stage;
        }
    }
}
