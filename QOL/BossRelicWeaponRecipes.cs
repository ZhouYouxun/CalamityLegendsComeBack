using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace CalamityLegendsComeBack.QOL
{
    public class BossRelicWeaponRecipes : ModSystem
    {
        private static readonly Dictionary<string, string[]> RelicToWeapons = new()
        {
            { "AquaticScourgeRelic", new[] { "Barinautical", "DeepseaStaff", "Downpour", "ScourgeoftheSeas", "SeasSearing", "SubmarineShocker" } },
            { "AstrumAureusRelic", new[] { "AlulaAustralis", "AuroraBlazer", "AuroradicalThrow", "BorealisBomber", "LeonidProgenitor", "Nebulash" } },
            { "AstrumDeusRelic", new[] { "RegulusRiot", "StarShower", "StarSputter", "StarspawnHelixStaff", "TheMicrowave" } },
            { "BrimstoneElementalRelic", new[] { "Brimlance", "DormantBrimseeker", "Hellborn", "SeethingDischarge" } },
            { "CalamitasCloneRelic", new[] { "Animosity", "EntropysVigil", "LashesofChaos", "Oblivion" } },
            { "CalamitasRelic", new[] { "ColdheartIcicle", "Condemnation", "GaelsGreatsword", "Heresy", "Perdition", "Sacrifice", "Vehemence", "Vigilance", "Violence" } },
            { "CeaselessVoidRelic", new[] { "MirrorBlade", "VoidConcentrationStaff" } },
            { "CrabulonRelic", new[] { "Fungicide", "HyphaeRod", "InfestedClawmerang", "MycelialClaws", "Mycoroot", "PuffShroom" } },
            { "CragmawMireRelic", new[] { "SpentFuelContainer" } },
            { "CryogenRelic", new[] { "Avalanche", "GlacialEmbrace", "HoarfrostBow", "Icebreaker", "SnowstormStaff" } },
            { "DesertScourgeRelic", new[] { "Barinade", "BrittleStarStaff", "SaharaSlicers", "SandstreamScepter", "ScourgeoftheDesert" } },
            { "DevourerOfGodsRelic", new[] { "CosmicDischarge", "DimensionTearingDisk", "HyperdeathRiftScepter", "MawOfInfinity", "TheObliterator", "ThreadOfEradication", "VoidEaterMarionette" } },
            { "DraedonRelic", new[] { "AresExoskeleton", "AtlasMunitionsBeacon", "PhotonRipper", "RefractionRotor", "SpineOfThanatos", "SurgeDriver", "TheAtomSplitter", "TheJailor" } },
            { "DragonfollyRelic", new[] { "GildedProboscis", "GoldenEagle", "RougeSlash" } },
            { "GiantClamRelic", new[] { "ClamCrusher", "ClamorRifle", "Poseidon", "ShellfishStaff" } },
            { "HiveMindRelic", new[] { "DankStaff", "PerfectDark", "RotBall", "ShaderainStaff", "Shadethrower" } },
            { "LeviathanAnahitaRelic", new[] { "AnahitasArpeggio", "Atlantis", "GastricBelcherStaff", "Greentide", "LeviathanTeeth", "Leviatitan", "Whitewater" } },
            { "MaulerRelic", new[] { "SulphuricAcidCannon" } },
            { "NuclearTerrorRelic", new[] { "GammaHeart", "PhosphorescentGauntlet" } },
            { "OldDukeRelic", new[] { "CadaverousCarrion", "FetidEmesis", "InsidiousImpaler", "MutatedTruffle", "SepticSkewer", "TheOldReaper", "ToxicantTwister", "VitriolicViper" } },
            { "PerforatorsRelic", new[] { "Aorta", "BloodBath", "Eviscerator", "FleshOfInfidelity", "SausageMaker", "ToothBall", "VeinBurster" } },
            { "PlaguebringerGoliathRelic", new[] { "FuelCellBundle", "InfectedRemote", "Malachite", "Malevolence", "PlagueStaff", "TheSyringe", "Virulence" } },
            { "PolterghastRelic", new[] { "BansheeHook", "DaemonsFlame", "EtherealSubjugator", "FatesReveal", "GhastlyVisage", "GhoulishGouger", "TerrorBlade" } },
            { "ProfanedGuardiansRelic", new[] { "RelicOfDeliverance", "RelicOfResilience", "RelicOfConvergence" } },
            { "ProvidenceRelic", new[] { "BlissfulBombardier", "BurningRevelation", "DazzlingStabberStaff", "HolyCollider", "MoltenAmputator", "PristineFury", "PurgeGuzzler", "TelluricGlare" } },
            { "RavagerRelic", new[] { "CorpusAvertor", "CraniumSmasher", "Hematemesis", "RealmRavager", "SpikecragStaff", "UltimusCleaver", "Vesuvius" } },
            { "SignusRelic", new[] { "CosmicKunai", "Cosmilamp" } },
            { "SlimeGodRelic", new[] { "AbyssalTome", "CorroslimeStaff", "CrimslimeStaff", "EldritchTome", "OverloadedBlaster" } },
            { "StormWeaverRelic", new[] { "SkytideDragoon", "TheStorm", "Volterion" } },
            { "YharonRelic", new[] { "ChickenCannon", "DragonRage", "DragonsBreath", "PhoenixFlameBarrage", "TheBurningSky", "TheFinalDawn", "TheWand", "Wrathwing", "YharimsCrystal", "YharonsKindleStaff" } }
        };

        private static readonly Dictionary<string, string[]> VanillaRelicToWeapons = new()
        {
            { "KingSlimeRelic", new[] { "SlimeStaff", "SlimySaddle", "SlimeHook", "SlimeGun", "NinjaHood", "NinjaShirt", "NinjaPants" } },
            { "EyeofCthulhuRelic", new[] { "Binoculars", "ShieldofCthulhu", "EoCShield" } },
            { "BrainofCthulhuRelic", new[] { "TheRottedFork" } },
            { "QueenBeeRelic", new[] { "BeeHat", "BeeShirt", "BeePants", "HoneyComb", "BeeGun", "TheBeesKnees", "BeesKnees", "BeeKeeper" } },
            { "SkeletronRelic", new[] { "BookofSkulls", "BoneGlove", "SkeletronHand" } },
            { "WallofFleshRelic", new[] { "BreakerBlade", "ClockworkAssaultRifle", "LaserRifle", "FireWhip", "Pwnhammer", "WarriorEmblem", "RangerEmblem", "SorcererEmblem", "SummonerEmblem" } },
            { "QueenSlimeRelic", new[] { "CrystalNinjaHelmet", "CrystalNinjaChestplate", "CrystalNinjaLeggings", "QueenSlimeMinionStaff", "Smolstar", "QueenSlimeMountSaddle", "QueenSlimeHook" } },
            { "PlanteraRelic", new[] { "TempleKey", "GrenadeLauncher", "VenusMagnum", "NettleBurst", "LeafBlower", "FlowerPow", "WaspGun", "Seedler", "PygmyStaff", "ThornHook", "TheAxe" } },
            { "GolemRelic", new[] { "Picksaw", "Stynger", "PossessedHatchet", "SunStone", "EyeoftheGolem", "HeatRay", "StaffofEarth", "GolemFist" } },
            { "DukeFishronRelic", new[] { "BubbleGun", "Flairon", "RazorbladeTyphoon", "TempestStaff", "Tsunami", "FishronWings" } },
            { "EmpressOfLightRelic", new[] { "Nightglow", "FairyQueenMagicItem", "Starlight", "PiercingStarlight", "Kaleidoscope", "RainbowWhip", "Eventide", "FairyQueenRangedItem", "RainbowWings", "HallowBossDye", "RainbowCursor", "Terraprisma" } },
            { "DeerclopsRelic", new[] { "WeatherPain", "HoundiusShootius", "LucyTheAxe" } },
            { "OgreRelic", new[] { "BookStaff", "DD2PhoenixBow", "DD2SquireDemonSword", "MonkStaffT1", "MonkStaffT2" } },
            { "BetsyRelic", new[] { "DD2BetsyBow", "ApprenticeStaffT3", "MonkStaffT3", "DD2SquireBetsySword" } },
            { "FlyingDutchmanRelic", new[] { "CoinGun", "Cutlass", "DiscountCard", "GoldRing", "LuckyCoin", "PirateStaff", "PirateShipMountItem" } },
            { "MourningWoodRelic", new[] { "StakeLauncher", "NecromanticScroll", "SpookyHook", "SpookyTwig", "WitchBroom", "SpookyWoodMountItem" } },
            { "PumpkingRelic", new[] { "TheHorsemansBlade", "RavenStaff", "BatScepter", "CandyCornRifle", "JackOLanternLauncher", "BlackFairyDust", "ScytheWhip" } },
            { "EverscreamRelic", new[] { "ChristmasTreeSword", "Razorpine", "FestiveWings", "ChristmasHook" } },
            { "SantaNK1Relic", new[] { "ElfMelter", "ChainGun", "SantankMountItem" } },
            { "IceQueenRelic", new[] { "BlizzardStaff", "NorthPole", "SnowmanCannon" } },
            { "MartianSaucerRelic", new[] { "LaserDrill", "ChargedBlasterCannon", "AntiGravityHook", "BrainScrambler", "LaserMachinegun", "Xenopopper", "XenoStaff", "CosmicCarKey", "ElectrosphereLauncher", "InfluxWaver" } },
            { "MoonLordRelic", new[] { "Meowmere", "Terrarian", "StarWrath", "SDMG", "LastPrism", "LunarFlareBook", "RainbowCrystalStaff", "MoonlordTurretStaff", "LunarPortalStaff", "CelebrationMk2", "Celeb2" } }
        };

        private static readonly Dictionary<string, string> LegacyRelicNames = new()
        {
            { "KingSlimeRelic", "KingSlimeMasterTrophy" },
            { "EyeofCthulhuRelic", "EyeofCthulhuMasterTrophy" },
            { "BrainofCthulhuRelic", "BrainofCthulhuMasterTrophy" },
            { "QueenBeeRelic", "QueenBeeMasterTrophy" },
            { "SkeletronRelic", "SkeletronMasterTrophy" },
            { "WallofFleshRelic", "WallofFleshMasterTrophy" },
            { "QueenSlimeRelic", "QueenSlimeMasterTrophy" },
            { "PlanteraRelic", "PlanteraMasterTrophy" },
            { "GolemRelic", "GolemMasterTrophy" },
            { "DukeFishronRelic", "DukeFishronMasterTrophy" },
            { "EmpressOfLightRelic", "FairyQueenMasterTrophy" },
            { "DeerclopsRelic", "DeerclopsMasterTrophy" },
            { "OgreRelic", "OgreMasterTrophy" },
            { "BetsyRelic", "BetsyMasterTrophy" },
            { "FlyingDutchmanRelic", "FlyingDutchmanMasterTrophy" },
            { "MourningWoodRelic", "MourningWoodMasterTrophy" },
            { "PumpkingRelic", "PumpkingMasterTrophy" },
            { "EverscreamRelic", "EverscreamMasterTrophy" },
            { "SantaNK1Relic", "SantankMasterTrophy" },
            { "IceQueenRelic", "IceQueenMasterTrophy" },
            { "MartianSaucerRelic", "UFOMasterTrophy" },
            { "MoonLordRelic", "MoonLordMasterTrophy" }
        };

        private static readonly string[][] MimicItemCycles = new[]
        {
            new[] { "DualHook", "MagicDagger", "PhilosophersStone", "TitanGlove", "CrossNecklace", "StarCloak" },
            new[] { "ToySled", "Frostbrand", "FlowerofFrost", "IceBow" },
            new[] { "DartRifle", "ClingerStaff", "ChainGuillotines", "PutridScent", "WormHook" },
            new[] { "DartPistol", "SoulDrain", "FetidBaghnakhs", "FleshKnuckles", "TendonHook" },
            new[] { "DaedalusStormbow", "CrystalVileShard", "FlyingKnife", "IlluminantHook" }
        };

        // Giant Clam本身困难模式前就能击败，但这4把武器只应该在困难模式掉落/合成，
        // 否则用雕像配方就能在进入困难模式前绕过掉落限制。
        private static readonly HashSet<string> HardmodeOnlyRelicWeapons = new()
        {
            "ClamCrusher", "ClamorRifle", "Poseidon", "ShellfishStaff"
        };

        private static readonly string[][] MaterialConversionPairs = new[]
        {
            new[] { "SilverBar", "TungstenBar" },
            new[] { "CobaltOre", "PalladiumOre" },
            new[] { "CobaltBar", "PalladiumBar" },
            new[] { "MythrilOre", "OrichalcumOre" },
            new[] { "MythrilBar", "OrichalcumBar" },
            new[] { "AdamantiteOre", "TitaniumOre" },
            new[] { "AdamantiteBar", "TitaniumBar" }
        };

        public override void AddRecipes()
        {
            if (CalamityLegendsComeBackConfig.Instance?.AllowBossRelicWeaponRecipes == true)
                RegisterCalamityBossRelicRecipes();

            if (CalamityLegendsComeBackConfig.Instance?.AllowOtherRecipes == true)
                RegisterVanillaExtraQolRecipes();
        }

        private static void RegisterCalamityBossRelicRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;

            foreach (var kvp in RelicToWeapons)
            {
                if (!calamity.TryFind<ModItem>(kvp.Key, out ModItem relicItem))
                    continue;

                foreach (string weaponName in kvp.Value)
                {
                    if (!calamity.TryFind<ModItem>(weaponName, out ModItem weaponItem))
                        continue;

                    Recipe recipe = Recipe.Create(weaponItem.Type);
                    recipe.AddIngredient(relicItem.Type);

                    if (HardmodeOnlyRelicWeapons.Contains(weaponName))
                        recipe.AddCondition(Condition.Hardmode);

                    recipe.Register();
                }
            }
        }

        private static void RegisterVanillaExtraQolRecipes()
        {
            foreach (var kvp in VanillaRelicToWeapons)
            {
                if (!TryGetVanillaItem(kvp.Key, out int relicType))
                    continue;

                foreach (string weaponName in kvp.Value)
                {
                    if (!TryGetVanillaItem(weaponName, out int weaponType))
                        continue;

                    Recipe recipe = Recipe.Create(weaponType);
                    recipe.AddIngredient(relicType);
                    recipe.Register();
                }
            }

            RegisterQueenBeeBeenadeRecipe();

            foreach (string[] cycle in MimicItemCycles)
                RegisterCycleRecipes(cycle, TileID.Anvils);

            foreach (string[] pair in MaterialConversionPairs)
                RegisterBidirectionalConversion(pair[0], pair[1]);
        }

        private static void RegisterQueenBeeBeenadeRecipe()
        {
            if (!TryGetVanillaItem("QueenBeeRelic", out int relicType))
                return;

            Recipe recipe = Recipe.Create(ItemID.Beenade, 100);
            recipe.AddIngredient(relicType);
            recipe.Register();
        }

        private static void RegisterCycleRecipes(string[] itemNames, int tileType)
        {
            int? firstType = null;
            int? previousType = null;

            foreach (string itemName in itemNames)
            {
                if (!TryGetVanillaItem(itemName, out int currentType))
                    continue;

                firstType ??= currentType;

                if (previousType.HasValue)
                    RegisterOneToOneRecipe(currentType, previousType.Value, tileType);

                previousType = currentType;
            }

            if (firstType.HasValue && previousType.HasValue && firstType.Value != previousType.Value)
                RegisterOneToOneRecipe(firstType.Value, previousType.Value, tileType);
        }

        private static void RegisterBidirectionalConversion(string firstItemName, string secondItemName)
        {
            if (!TryGetVanillaItem(firstItemName, out int firstType) || !TryGetVanillaItem(secondItemName, out int secondType))
                return;

            RegisterOneToOneRecipe(secondType, firstType);
            RegisterOneToOneRecipe(firstType, secondType);
        }

        private static void RegisterOneToOneRecipe(int resultType, int ingredientType, int? tileType = null)
        {
            Recipe recipe = Recipe.Create(resultType);
            recipe.AddIngredient(ingredientType);

            if (tileType.HasValue)
                recipe.AddTile(tileType.Value);

            recipe.Register();
        }

        private static bool TryGetVanillaItem(string itemName, out int itemType)
        {
            if (ItemID.Search.TryGetId(itemName, out itemType))
                return true;

            if (LegacyRelicNames.TryGetValue(itemName, out string legacyName) && ItemID.Search.TryGetId(legacyName, out itemType))
                return true;

            if (itemName == "SoulDrain" && ItemID.Search.TryGetId("LifeDrain", out itemType))
                return true;

            return false;
        }
    }
}
