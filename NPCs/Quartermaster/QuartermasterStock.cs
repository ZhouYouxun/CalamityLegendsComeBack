using System;
using System.Collections.Generic;
using System.Linq;
using CalamityLegendsComeBack.Accssory;
using CalamityLegendsComeBack.QOL.BossSummon;
using CalamityLegendsComeBack.QOL.QuickStart;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using CalamityLegendsComeBack.Weapons.A_Upgrade.LowEndMurasama;
using CalamityMod;
using CalamityMod.CustomRecipes;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items.Fishing.BrimstoneCragCatches;
using CalamityMod.Items.Fishing.SunkenSeaCatches;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.DraedonStructures;
using CalamityMod.Items.Placeables.Furniture;
using CalamityMod.Items.Placeables.Ores;
using CalamityMod.Items.Placeables.SunkenSea;
using CalamityMod.Items.PermanentBoosters;
using CalamityMod.Items.Tools;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.NPCs.Quartermaster;

internal static partial class QuartermasterStock
{
    // A vanilla shop has 40 slots; leave the last slot available for sold items.
    internal const int PageSize = 39;
    internal sealed record Page(string Name, string Category, int Number, int Count);
    internal static readonly List<Page> Pages = new();

    private static Condition Gate(string key, Func<bool> predicate) => new(
        Language.GetText("Mods.CalamityLegendsComeBack.Quartermaster.Conditions." + key), predicate);

    private static int Mechs => (NPC.downedMechBoss1 ? 1 : 0) + (NPC.downedMechBoss2 ? 1 : 0) + (NPC.downedMechBoss3 ? 1 : 0);
    internal static readonly Condition TierTwoOre = Gate("TierTwoOre", () => Main.hardMode &&
        (!CalamityServerConfig.Instance.EarlyHardmodeProgressionRework || Mechs >= 1));
    internal static readonly Condition TierThreeOre = Gate("TierThreeOre", () => Main.hardMode &&
        (!CalamityServerConfig.Instance.EarlyHardmodeProgressionRework || Mechs >= 2));
    internal static readonly Condition StarterBundle = Gate("StarterBundle", () => NPC.downedBoss2 && NPC.downedGoblins);
    internal static readonly Condition HardmodeBundle = Gate("HardmodeBundle", () =>
        Main.hardMode && Mechs >= 1 && TierThreeOre.IsMet());
    internal static readonly Condition PlanteraBundle = Gate("PlanteraBundle", () =>
        NPC.downedPlantBoss && DownedBossSystem.downedCalamitasClone);
    private static readonly Condition Pirates = Gate("Pirates", () => Main.hardMode && NPC.downedPirates);
    private static readonly Condition VisitedUnderworld = Gate("VisitedUnderworld", () =>
        Main.LocalPlayer.GetModPlayer<QuartermasterShopPlayer>().VisitedUnderworld);
    private static readonly Condition VisitedJungle = Gate("VisitedJungle", () =>
        Main.LocalPlayer.GetModPlayer<QuartermasterShopPlayer>().VisitedJungle);
    private static readonly Condition VisitedAnySea = Gate("VisitedAnySea", () =>
        Main.LocalPlayer.GetModPlayer<QuartermasterShopPlayer>().VisitedAnySea);
    private static readonly Condition VisitedCaverns = Gate("VisitedCaverns", () =>
        Main.LocalPlayer.GetModPlayer<QuartermasterShopPlayer>().VisitedCaverns);
    private static readonly Condition VisitedSunkenSea = Gate("VisitedSunkenSea", () =>
        Main.LocalPlayer.GetModPlayer<QuartermasterShopPlayer>().VisitedSunkenSea);
    private static readonly Condition VisitedAstralInHardmode = Gate("VisitedAstralInHardmode", () =>
        Main.LocalPlayer.GetModPlayer<QuartermasterShopPlayer>().VisitedAstralInHardmode);
    private static readonly Condition BeforeHardmode = Gate("BeforeHardmode", () => !Main.hardMode);
    private static readonly Condition BeforePlantera = Gate("BeforePlantera", () => !NPC.downedPlantBoss);
    private static readonly Condition BeforeMoonLord = Gate("BeforeMoonLord", () => !NPC.downedMoonlord);
    private static readonly Condition BeforeYharon = Gate("BeforeYharon", () => !DownedBossSystem.downedYharon);
    private static Condition Arsenal(int tier) => new(
        ArsenalTierGatedRecipe.ConstructRecipeCondition(tier, out Func<bool> predicate), predicate);

    internal static void Register(int npcType)
    {
        Pages.Clear();
        var summons = new NPCShop(npcType, "BossSummonsSource");
        BossSummonShop.AddSummons(summons);
        RegisterPages(npcType, "BossSummons", summons);
        RegisterPages(npcType, "Codebreaker", Codebreaker(npcType));
        RegisterPages(npcType, "VanillaOres", VanillaOres(npcType));
        RegisterPages(npcType, "CalamityOres", CalamityOres(npcType));
        RegisterPages(npcType, "CraftedBlocks", CraftedBlocks(npcType));
        RegisterPages(npcType, "Materials", Materials(npcType));
        RegisterPages(npcType, "Valuables", Valuables(npcType));
        var supplies = new NPCShop(npcType, "SuppliesSource");
        AddSupplies(supplies);
        RegisterPages(npcType, "Supplies", supplies);
    }

    private static void RegisterPages(int npcType, string category, NPCShop source)
    {
        var entries = source.Entries.ToArray();
        // Every category uses all 39 sale slots; the 40th remains free for sold items.
        int pageSize = PageSize;
        int count = (entries.Length + pageSize - 1) / pageSize;
        for (int page = 0; page < count; page++)
        {
            string name = category + page;
            var shop = new NPCShop(npcType, name);
            foreach (var entry in entries.Skip(page * pageSize).Take(pageSize))
                shop.Add(entry.Item.Clone(), entry.Conditions.ToArray());
            shop.Register();
            Pages.Add(new Page(name, category, page + 1, count));
        }
    }

    // Default Item.value is a buy price. Avoid free items and never price below it.
    private static void Add(NPCShop shop, int type, int minimumPrice, params Condition[] conditions)
    {
        Item item = new(type);
        item.shopCustomPrice = Math.Max(item.value, minimumPrice);
        shop.Add(item, conditions);
    }
    private static void Add<T>(NPCShop shop, int minimumPrice, params Condition[] conditions) where T : ModItem =>
        Add(shop, ModContent.ItemType<T>(), minimumPrice, conditions);

    private static NPCShop Codebreaker(int type)
    {
        var s = new NPCShop(type, "CodebreakerSource");
        Add<CodebreakerBase>(s, Item.buyPrice(gold: 10));
        Add<DecryptionComputer>(s, Item.buyPrice(gold: 15), Condition.DownedSkeletron);
        Add<LongRangedSensorArray>(s, Item.buyPrice(gold: 25), TierTwoOre);
        Add<AdvancedDisplay>(s, Item.buyPrice(gold: 40), Condition.DownedPlantera, Condition.DownedGolem, CalamityConditions.DownedCryogen);
        Add<VoltageRegulationSystem>(s, Item.buyPrice(gold: 60), Condition.DownedMoonLord, CalamityConditions.DownedProvidence);
        Add<AuricQuantumCoolingCell>(s, Item.buyPrice(gold: 40), CalamityConditions.DownedYharon,
            CalamityConditions.DownedDevourerOfGods, CalamityConditions.DownedProvidence, Arsenal(5));
        Add<DraedonPowerCell>(s, Item.buyPrice(silver: 1));
        Add<PowerCellFactoryItem>(s, Item.buyPrice(gold: 15));
        Add<ChargingStationItem>(s, Item.buyPrice(gold: 10));
        Add<MysteriousCircuitry>(s, Item.buyPrice(silver: 50));
        Add<DubiousPlating>(s, Item.buyPrice(silver: 25));
        Add<PlasmaDriveCore>(s, Item.buyPrice(gold: 25));
        // All five schematics may be bought immediately. The first four upgrades
        // require only world progression; the Auric cell retains its T5 knowledge gate.
        Add<EncryptedSchematicSunkenSea>(s, Item.buyPrice(gold: 5));
        Add<EncryptedSchematicPlanetoid>(s, Item.buyPrice(gold: 10));
        Add<EncryptedSchematicJungle>(s, Item.buyPrice(gold: 15));
        Add<EncryptedSchematicHell>(s, Item.buyPrice(gold: 20));
        Add<EncryptedSchematicIce>(s, Item.buyPrice(gold: 30));
        return s;
    }

    private static NPCShop VanillaOres(int type)
    {
        var s = new NPCShop(type, "VanillaOresSource");
        foreach (int id in new[] { ItemID.CopperOre, ItemID.TinOre, ItemID.IronOre, ItemID.LeadOre,
            ItemID.SilverOre, ItemID.TungstenOre, ItemID.GoldOre, ItemID.PlatinumOre, ItemID.Obsidian })
            Add(s, id, Item.buyPrice(silver: 1));
        Add(s, ItemID.SiltBlock, Item.buyPrice(copper: 10));
        foreach (int id in new[] { ItemID.Amethyst, ItemID.Topaz, ItemID.Sapphire, ItemID.Emerald,
            ItemID.Ruby, ItemID.Diamond, ItemID.Amber })
            Add(s, id, Item.buyPrice(silver: 1));
        Add(s, ItemID.DemoniteOre, Item.buyPrice(silver: 5), Condition.DownedEyeOfCthulhu);
        Add(s, ItemID.CrimtaneOre, Item.buyPrice(silver: 5), Condition.DownedEyeOfCthulhu);
        Add(s, ItemID.Meteorite, Item.buyPrice(silver: 5), Condition.DownedEowOrBoc);
        Add(s, ItemID.Hellstone, Item.buyPrice(silver: 5), Condition.DownedEowOrBoc);
        Add(s, ItemID.CobaltOre, Item.buyPrice(silver: 10), Condition.Hardmode);
        Add(s, ItemID.PalladiumOre, Item.buyPrice(silver: 10), Condition.Hardmode);
        Add(s, ItemID.MythrilOre, Item.buyPrice(silver: 15), TierTwoOre);
        Add(s, ItemID.OrichalcumOre, Item.buyPrice(silver: 15), TierTwoOre);
        Add(s, ItemID.AdamantiteOre, Item.buyPrice(silver: 20), TierThreeOre);
        Add(s, ItemID.TitaniumOre, Item.buyPrice(silver: 20), TierThreeOre);
        Add(s, ItemID.ChlorophyteOre, Item.buyPrice(silver: 25), Condition.DownedMechBossAll);
        Add(s, ItemID.LunarOre, Item.buyPrice(silver: 50), Condition.DownedMoonLord);
        return s;
    }

    private static NPCShop CalamityOres(int type)
    {
        var s = new NPCShop(type, "CalamityOresSource");
        Add<SeaPrism>(s, Item.buyPrice(silver: 3), CalamityConditions.DownedDesertScourge);
        Add<AerialiteOre>(s, Item.buyPrice(silver: 6), CalamityConditions.DownedHiveMindOrPerforator);
        Add<InfernalSuevite>(s, Item.buyPrice(silver: 15), Condition.Hardmode, Condition.DownedMechBossAny);
        Add<CryonicOre>(s, Item.buyPrice(silver: 20), CalamityConditions.DownedCryogen, TierThreeOre);
        Add<HallowedOre>(s, Item.buyPrice(silver: 25), Condition.DownedMechBossAll);
        Add<PerennialOre>(s, Item.buyPrice(silver: 30), Condition.DownedPlantera);
        Add<ScoriaOre>(s, Item.buyPrice(silver: 40), Condition.DownedGolem);
        Add<AstralOre>(s, Item.buyPrice(silver: 50), CalamityConditions.DownedAstrumDeus);
        Add<ExodiumCluster>(s, Item.buyPrice(silver: 50), Condition.DownedMoonLord);
        Add<UelibloomOre>(s, Item.buyPrice(gold: 1), CalamityConditions.DownedProvidence);
        Add<AuricOre>(s, Item.buyPrice(gold: 1), CalamityConditions.DownedYharon);
        Add<CalamityMod.Items.Placeables.Abyss.PlantyMush>(s, Item.buyPrice(silver: 1), Condition.Hardmode);
        return s;
    }

    internal static NPCShop Materials(int type)
    {
        var s = new NPCShop(type, "MaterialsSource");
        // Exact active SHPC AmmoType set, not every item whose tooltip says material.
        Add<EnergyCore>(s, 1000);
        Add<StormlionMandible>(s, 1000);
        Add<PearlShard>(s, 1000, CalamityConditions.DownedDesertScourge);
        Add<SulphuricScale>(s, 1000, Condition.DownedEyeOfCthulhu);
        Add<PurifiedGel>(s, 2000, CalamityConditions.DownedSlimeGod);
        Add<EssenceofHavoc>(s, 2000, Condition.Hardmode);
        Add<EssenceofEleum>(s, 2000, Condition.Hardmode);
        Add<EssenceofSunlight>(s, 2000, Condition.Hardmode);
        Add<StarblightSoot>(s, 2000, Condition.Hardmode);
        Add(s, ItemID.SoulofLight, 2000, Condition.Hardmode);
        Add(s, ItemID.SoulofNight, 2000, Condition.Hardmode);
        Add(s, ItemID.SoulofFlight, 2000, Condition.Hardmode);
        Add(s, ItemID.SoulofFright, 5000, Condition.DownedSkeletronPrime);
        Add(s, ItemID.SoulofMight, 5000, Condition.DownedDestroyer);
        Add(s, ItemID.SoulofSight, 5000, Condition.DownedTwins);
        Add<AshesofCalamity>(s, 5000, CalamityConditions.DownedCalamitasClone);
        Add<LivingShard>(s, 5000, Condition.DownedPlantera);
        Add<DepthCells>(s, 5000, CalamityConditions.DownedLeviathan);
        Add<PlagueCellCanister>(s, 5000, Condition.DownedGolem);
        // Each fragment requires its own pillar; killing the cultist alone is insufficient.
        Add(s, ItemID.FragmentSolar, 10000, Gate("Solar", () => NPC.downedTowerSolar));
        Add(s, ItemID.FragmentVortex, 10000, Gate("Vortex", () => NPC.downedTowerVortex));
        Add(s, ItemID.FragmentNebula, 10000, Gate("Nebula", () => NPC.downedTowerNebula));
        Add(s, ItemID.FragmentStardust, 10000, Gate("Stardust", () => NPC.downedTowerStardust));
        Add<MeldBlob>(s, 10000, Gate("Cultist", () => NPC.downedAncientCultist));
        Add<UnholyEssence>(s, 10000, Condition.DownedMoonLord);
        Add<Necroplasm>(s, 10000, Condition.DownedMoonLord);
        Add<DivineGeode>(s, 20000, CalamityConditions.DownedProvidence);
        Add<Bloodstone>(s, 20000, CalamityConditions.DownedProvidence);
        Add<RuinousSoul>(s, 20000, CalamityConditions.DownedPolterghast);
        Add<TwistingNether>(s, 20000, CalamityConditions.DownedSignus);
        Add<DarkPlasma>(s, 20000, CalamityConditions.DownedCeaselessVoid);
        Add<ArmoredShell>(s, 20000, CalamityConditions.DownedStormWeaver);
        Add<EndothermicEnergy>(s, 20000, CalamityConditions.DownedDevourerOfGods);
        Add<NightmareFuel>(s, 20000, CalamityConditions.DownedDevourerOfGods);
        Add<DarksunFragment>(s, 20000, CalamityConditions.DownedDevourerOfGods);
        Add<YharonSoulFragment>(s, 50000, CalamityConditions.DownedYharon);
        Add<ExoPrism>(s, 50000, CalamityConditions.DownedExoMechs);
        Add<AshesofAnnihilation>(s, 50000, CalamityConditions.DownedSupremeCalamitas);
        return s;
    }

    private static NPCShop Valuables(int type)
    {
        var s = new NPCShop(type, "ValuablesSource");
        Add<LongLostWeaponBlueprints>(s, Item.buyPrice(gold: 10));
        Add<CorruptionEffigy>(s, Item.buyPrice(gold: 15));
        Add<CrimsonEffigy>(s, Item.buyPrice(gold: 15));
        Add(s, ItemID.LifeCrystal, Item.buyPrice(gold: 5));
        Add(s, ItemID.BloodMoonStarter, Item.buyPrice(gold: 5));
        Add(s, ItemID.Extractinator, Item.buyPrice(gold: 10));
        Add<LowEndMurasama>(s, Item.buyPrice(gold: 10));
        Add<ShrineModel>(s, Item.buyPrice(platinum: 2, gold: 10), Gate("GoblinsOrPreHardmodeBoss", () =>
            NPC.downedGoblins || NPC.downedSlimeKing || NPC.downedBoss1 || NPC.downedBoss2 ||
            NPC.downedQueenBee || NPC.downedBoss3 || NPC.downedDeerclops || Main.hardMode ||
            DownedBossSystem.downedDesertScourge || DownedBossSystem.downedCrabulon ||
            DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator || DownedBossSystem.downedSlimeGod));
        Add<LemonNade>(s, Item.buyPrice(gold: 15), Gate("Goblins", () => NPC.downedGoblins), BeforeHardmode);
        Add<YateveoBloom>(s, Item.buyPrice(gold: 50), VisitedJungle, BeforeHardmode);
        Add(s, ItemID.GoldenFishingRod, Item.buyPrice(gold: 50), VisitedAnySea);
        Add(s, ItemID.MasterBait, Item.buyPrice(gold: 1), VisitedAnySea);
        Add<EnchantedKnifeStaff>(s, Item.buyPrice(gold: 50), VisitedCaverns);
        Add<SparklingEmpress>(s, Item.buyPrice(gold: 50), VisitedSunkenSea, BeforeHardmode);
        Add<DragoonDrizzlefish>(s, Item.buyPrice(gold: 50), VisitedUnderworld, BeforeHardmode);
        Add<OldLordClaymore>(s, Item.buyPrice(gold: 50), VisitedUnderworld);
        Add<BladecrestOathsword>(s, Item.buyPrice(gold: 50), VisitedUnderworld);
        Add<Equanimity>(s, Item.buyPrice(platinum: 1), Condition.Hardmode);
        Add<PolarisParrotfish>(s, Item.buyPrice(platinum: 1), Condition.Hardmode, VisitedAstralInHardmode, BeforePlantera);
        Add<Murasama>(s, Item.buyPrice(platinum: 5), Condition.Hardmode);
        Add(s, ItemID.RodofDiscord, Item.buyPrice(platinum: 1), Condition.Hardmode, BeforeMoonLord);
        Add<CometShard>(s, Item.buyPrice(gold: 30), Condition.Hardmode);
        Add(s, ItemID.LifeFruit, Item.buyPrice(gold: 10), Condition.DownedMechBossAny);
        Add<SanguineTangerine>(s, Item.buyPrice(platinum: 1), Condition.DownedMechBossAll);
        Add<MiracleFruit>(s, Item.buyPrice(platinum: 1, gold: 50), Condition.DownedPlantera);
        Add<OmniGun>(s, Item.buyPrice(platinum: 1), Condition.DownedPlantera, BeforeMoonLord);
        Add<AbyssalDivingGear>(s, Item.buyPrice(platinum: 2), Condition.DownedPlantera,
            CalamityConditions.DownedLeviathan, BeforeMoonLord);
        Add(s, ItemID.CoinGun, Item.buyPrice(platinum: 1), Pirates);
        Add<EtherealCore>(s, Item.buyPrice(platinum: 1), Gate("Nebula", () => NPC.downedTowerNebula));
        Add<AbyssalDivingSuit>(s, Item.buyPrice(platinum: 4), Condition.DownedMoonLord);
        Add<NormalityRelocator>(s, Item.buyPrice(platinum: 2), Condition.DownedMoonLord);
        Add<TaintedCloudberry>(s, Item.buyPrice(platinum: 2), CalamityConditions.DownedProvidence);
        Add<BobbitHook>(s, Item.buyPrice(platinum: 2), CalamityConditions.DownedPolterghast);
        Add<PhantomHeart>(s, Item.buyPrice(platinum: 2), CalamityConditions.DownedPolterghast);
        Add<CalamarisLament>(s, Item.buyPrice(platinum: 2), CalamityConditions.DownedPolterghast, BeforeYharon);
        Add<DeepSeaDumbbell>(s, Item.buyPrice(platinum: 2), CalamityConditions.DownedPolterghast, BeforeYharon);
        Add<Valediction>(s, Item.buyPrice(platinum: 2), CalamityConditions.DownedPolterghast, BeforeYharon);
        Add<TheSponge>(s, Item.buyPrice(platinum: 5), CalamityConditions.DownedDevourerOfGods);
        Add<LiliesOfFinality>(s, Item.buyPrice(platinum: 5), CalamityConditions.DownedYharon);
        Add<SacredStrawberry>(s, Item.buyPrice(platinum: 5), CalamityConditions.DownedYharon);
        return s;
    }

    internal static void AddSupplies(NPCShop shop)
    {
        shop.AddWithCustomValue<RetroGameConsoleSupplyBox>(Item.buyPrice(gold: 5));
        shop.AddWithCustomValue<QuickStartBox>(Item.buyPrice(gold: 99), StarterBundle);
        shop.AddWithCustomValue<HardmodeSupplyBox>(Item.buyPrice(gold: 20), HardmodeBundle);
        shop.AddWithCustomValue<PostPlanteraSupplyBox>(Item.buyPrice(gold: 40), PlanteraBundle);
        shop.AddWithCustomValue<PostMoonLordSupplyBox>(Item.buyPrice(gold: 75), Condition.DownedMoonLord, Condition.DownedPlantera);
        shop.AddWithCustomValue<PostProvidenceSupplyBox>(Item.buyPrice(platinum: 1, gold: 50), CalamityConditions.DownedProvidence);
        shop.AddWithCustomValue<PostDevourerSupplyBox>(Item.buyPrice(platinum: 2, gold: 50), CalamityConditions.DownedDevourerOfGods);
        shop.AddWithCustomValue<PostYharonSupplyBox>(Item.buyPrice(platinum: 5), CalamityConditions.DownedYharon);
    }
}
