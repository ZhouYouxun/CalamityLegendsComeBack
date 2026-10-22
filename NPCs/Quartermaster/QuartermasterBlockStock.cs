using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.NPCs.Quartermaster;

internal static partial class QuartermasterStock
{
    // Inventory matches the PDF's Crafted Blocks and Bricks table (77 entries).
    // Sort by material/station availability, not the wiki's alphabetical order.
    private static NPCShop CraftedBlocks(int type)
    {
        var s = new NPCShop(type, "CraftedBlocksSource");

        // 初始可获得（35 种）。
        Add<CalamityMod.Items.Placeables.FurnitureNavystone.FurnitureAncientNavystone.AncientSmoothNavystone>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.AnodizedWulfrumPanels>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.AnodizedWulfrumTrim>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.AridSoil>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.Basalt>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureBasalt.BasaltSlab>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.BlackPearlPile>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.ChargedWulfrumEnergyBarrier>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.Dunesand>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.EutrophicGlass>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.DraedonStructures.HazardChevronPanels>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.DraedonStructures.LaboratoryPanels>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.DraedonStructures.LaboratoryPipePlating>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.DraedonStructures.LaboratoryPlating>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.Limestone>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.MossyStone>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.PinkPearlPile>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureMarnite.PolishedMarniteBlock>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureNavystone.PolishedNavystoneBrick>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.PolypSand>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.RoundedAnodizedWulfrumPanels>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.Runestone>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.DraedonStructures.RustedPipes>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.DraedonStructures.RustedPlating>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.SeaPrismBrick>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.Shellstone>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureShellstone.ShellstoneSlab>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureNavystone.SmoothNavystone>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.VolcanicSand>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.SunkenSea.WhitePearlPile>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.WulfrumEnergyBarrier>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.WulfrumPanels>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.WulfrumPlating>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.WulfrumSheets>(s, Item.buyPrice(copper: 20));
        Add<CalamityMod.Items.Placeables.FurnitureWulfrum.WulfrumSiding>(s, Item.buyPrice(copper: 20));

        // 史莱姆王后（1 种）。
        Add<CalamityMod.Items.Placeables.SunkenSea.HardenedEutrophicSand>(s, Item.buyPrice(copper: 25), Condition.DownedKingSlime);

        // 到访地狱后（2 种）。
        Add<CalamityMod.Items.Placeables.Plates.Navyplate>(s, Item.buyPrice(copper: 25), VisitedUnderworld);
        Add<CalamityMod.Items.Placeables.Plates.Onyxplate>(s, Item.buyPrice(copper: 25), VisitedUnderworld);

        // 世界吞噬怪或克苏鲁之脑后（5 种）。
        Add<CalamityMod.Items.Placeables.FurnitureAbyss.SmoothAbyssGravel>(s, Item.buyPrice(copper: 25), Condition.DownedEowOrBoc);
        Add<CalamityMod.Items.Placeables.Crags.BrimstoneSlab>(s, Item.buyPrice(copper: 25), Condition.DownedEowOrBoc);
        Add<CalamityMod.Items.Placeables.FurnitureAshen.SmoothBrimstoneSlag>(s, Item.buyPrice(copper: 25), Condition.DownedEowOrBoc);
        Add<CalamityMod.Items.Placeables.FurnitureAshen.AshenSlab>(s, Item.buyPrice(copper: 25), Condition.DownedEowOrBoc);
        Add<CalamityMod.Items.Placeables.FurnitureAshen.AshenAccentSlab>(s, Item.buyPrice(copper: 25), Condition.DownedEowOrBoc);

        // 腐巢意志或血肉宿主后（1 种）。
        Add<CalamityMod.Items.Placeables.AerialiteBrick>(s, Item.buyPrice(copper: 30), CalamityConditions.DownedHiveMindOrPerforator);

        // 史莱姆之神后（1 种）。
        Add<CalamityMod.Items.Placeables.FurnitureStatigel.StatigelBlock>(s, Item.buyPrice(copper: 30), CalamityConditions.DownedSlimeGod);

        // 困难模式（4 种）。
        Add<CalamityMod.Items.Placeables.Plates.Cinderplate>(s, Item.buyPrice(copper: 50), Condition.Hardmode);
        Add<CalamityMod.Items.Placeables.Plates.Elumplate>(s, Item.buyPrice(copper: 50), Condition.Hardmode);
        Add<CalamityMod.Items.Placeables.Plates.Havocplate>(s, Item.buyPrice(copper: 50), Condition.Hardmode);
        Add<CalamityMod.Items.Placeables.LivingFire.LivingBrimstoneFireBlock>(s, Item.buyPrice(copper: 50), Condition.Hardmode);

        // 困难模式三阶矿石可用；Voidstone 需要 180% 镐力（2 种）。
        Add<CalamityMod.Items.Placeables.FurnitureVoid.SmoothVoidstone>(s, Item.buyPrice(copper: 75), TierThreeOre);
        Add<CalamityMod.Items.Placeables.FurnitureVoid.VoidstoneSlab>(s, Item.buyPrice(copper: 75), TierThreeOre);

        // 极地之灵与三阶矿石可用（1 种）。
        Add<CalamityMod.Items.Placeables.CryonicBrick>(s, Item.buyPrice(copper: 100), CalamityConditions.DownedCryogen, TierThreeOre);

        // 世纪之花后（1 种）。
        Add<CalamityMod.Items.Placeables.PerennialBrick>(s, Item.buyPrice(copper: 100), Condition.DownedPlantera);

        // 石巨人后（4 种）。
        Add<CalamityMod.Items.Placeables.ScoriaBrick>(s, Item.buyPrice(copper: 100), Condition.DownedGolem);
        Add<CalamityMod.Items.Placeables.Plates.Plagueplate>(s, Item.buyPrice(copper: 100), Condition.DownedGolem);
        Add<CalamityMod.Items.Placeables.FurniturePlagued.PlaguedContainmentBrick>(s, Item.buyPrice(copper: 100), Condition.DownedGolem);
        Add<CalamityMod.Items.Placeables.LivingFire.LivingPlagueFireBlock>(s, Item.buyPrice(copper: 100), Condition.DownedGolem);

        // 星神游龙后，允许开采星辉矿（1 种）。
        Add<CalamityMod.Items.Placeables.Astral.AstralBrick>(s, Item.buyPrice(copper: 100), CalamityConditions.DownedAstrumDeus);

        // 月亮领主后（5 种）。
        Add<CalamityMod.Items.Placeables.FurnitureProfaned.ProfanedRock>(s, Item.buyPrice(copper: 200), Condition.DownedMoonLord);
        Add<CalamityMod.Items.Placeables.FurnitureProfaned.ProfanedCrystal>(s, Item.buyPrice(copper: 200), Condition.DownedMoonLord);
        Add<CalamityMod.Items.Placeables.FurnitureProfaned.ProfanedSlab>(s, Item.buyPrice(copper: 200), Condition.DownedMoonLord);
        Add<CalamityMod.Items.Placeables.FurnitureProfaned.RunicProfanedBrick>(s, Item.buyPrice(copper: 200), Condition.DownedMoonLord);
        Add<CalamityMod.Items.Placeables.LivingFire.LivingHolyFireBlock>(s, Item.buyPrice(copper: 200), Condition.DownedMoonLord);

        // 亵渎天神后（1 种）。
        Add<CalamityMod.Items.Placeables.UelibloomBrick>(s, Item.buyPrice(copper: 300), CalamityConditions.DownedProvidence);

        // 三名神之使徒均击败后（1 种）。
        Add<CalamityMod.Items.Placeables.FurnitureOtherworldly.OtherworldlyStone>(s, Item.buyPrice(copper: 300), Gate("AllSentinels", () => DownedBossSystem.downedCeaselessVoid && DownedBossSystem.downedStormWeaver && DownedBossSystem.downedSignus));

        // 噬魂幽花后（1 种）。
        Add<CalamityMod.Items.Placeables.FurnitureStratus.StratusBricks>(s, Item.buyPrice(copper: 400), CalamityConditions.DownedPolterghast);

        // 神明吞噬者后（2 种）。
        Add<CalamityMod.Items.Placeables.FurnitureCosmilite.CosmiliteBrick>(s, Item.buyPrice(copper: 500), CalamityConditions.DownedDevourerOfGods);
        Add<CalamityMod.Items.Placeables.LivingFire.LivingGodSlayerFireBlock>(s, Item.buyPrice(copper: 500), CalamityConditions.DownedDevourerOfGods);

        // 犽戎后（5 种）。
        Add<CalamityMod.Items.Placeables.FurnitureAuric.AuricPanel>(s, Item.buyPrice(copper: 800), CalamityConditions.DownedYharon);
        Add<CalamityMod.Items.Placeables.FurnitureAuric.ActivatedAuricPanel>(s, Item.buyPrice(copper: 800), CalamityConditions.DownedYharon);
        Add<CalamityMod.Items.Placeables.FurnitureAuric.AuricAbsorber>(s, Item.buyPrice(copper: 800), CalamityConditions.DownedYharon);
        Add<CalamityMod.Items.Placeables.FurnitureAuric.AuricRepulserPanel>(s, Item.buyPrice(copper: 800), CalamityConditions.DownedYharon);
        Add<CalamityMod.Items.Placeables.FurnitureSilva.SilvaCrystal>(s, Item.buyPrice(copper: 800), CalamityConditions.DownedYharon);

        // 星流巨械与犽戎后，具备嘉登熔炉材料（3 种）。
        Add<CalamityMod.Items.Placeables.FurnitureAuric.ExoAuricPanel>(s, Item.buyPrice(copper: 1000), CalamityConditions.DownedExoMechs, CalamityConditions.DownedYharon);
        Add<CalamityMod.Items.Placeables.FurnitureExo.ExoPlating>(s, Item.buyPrice(copper: 1000), CalamityConditions.DownedExoMechs, CalamityConditions.DownedYharon);
        Add<CalamityMod.Items.Placeables.FurnitureExo.ExoPrismPanel>(s, Item.buyPrice(copper: 1000), CalamityConditions.DownedExoMechs, CalamityConditions.DownedYharon);

        // 至尊灾厄后（1 种）。
        Add<CalamityMod.Items.Placeables.FurnitureSacrilegious.OccultBrickItem>(s, Item.buyPrice(copper: 1000), CalamityConditions.DownedSupremeCalamitas);
        return s;
    }
}
