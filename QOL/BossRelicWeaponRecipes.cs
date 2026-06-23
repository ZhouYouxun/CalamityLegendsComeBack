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
            { "CalamitasRelic", new[] { "Animosity", "ColdheartIcicle", "Condemnation", "EntropysVigil", "GaelsGreatsword", "Heresy", "LashesofChaos", "Oblivion", "Perdition", "Sacrifice", "Vehemence", "Vigilance", "Violence" } },
            { "CeaselessVoidRelic", new[] { "MirrorBlade", "VoidConcentrationStaff" } },
            { "CrabulonRelic", new[] { "Fungicide", "HyphaeRod", "InfestedClawmerang", "MycelialClaws", "Mycoroot", "PuffShroom" } },
            { "CragmawMireRelic", new[] { "SpentFuelContainer" } },
            { "CryogenRelic", new[] { "Avalanche", "GlacialEmbrace", "HoarfrostBow", "Icebreaker", "SnowstormStaff" } },
            { "DesertScourgeRelic", new[] { "Barinade", "BrittleStarStaff", "SaharaSlicers", "SandstreamScepter", "ScourgeoftheDesert" } },
            { "DevourerOfGodsRelic", new[] { "CosmicDischarge", "DimensionTearingDisk", "HyperdeathRiftScepter", "MawOfInfinity", "TheObliterator", "TheWand", "ThreadOfEradication", "VoidEaterMarionette" } },
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
            { "PolterghastRelic", new[] { "BansheeHook", "DaemonsFlame", "EtherealSubjugator", "FatesReveal", "GhastlyVisage", "GhoulishGouger", "TerrorBlade", "Violence" } },
            { "ProfanedGuardiansRelic", new[] { "RelicOfDeliverance" } },
            { "ProvidenceRelic", new[] { "BlissfulBombardier", "BurningRevelation", "DazzlingStabberStaff", "HolyCollider", "MoltenAmputator", "PristineFury", "PurgeGuzzler", "TelluricGlare" } },
            { "RavagerRelic", new[] { "CorpusAvertor", "CraniumSmasher", "Hematemesis", "RealmRavager", "SpikecragStaff", "UltimusCleaver", "Vesuvius" } },
            { "SignusRelic", new[] { "CosmicKunai", "Cosmilamp" } },
            { "SlimeGodRelic", new[] { "AbyssalTome", "CorroslimeStaff", "CrimslimeStaff", "EldritchTome", "OverloadedBlaster" } },
            { "StormWeaverRelic", new[] { "SkytideDragoon", "TheStorm", "Volterion" } },
            { "YharonRelic", new[] { "ChickenCannon", "DragonRage", "DragonsBreath", "PhoenixFlameBarrage", "TheBurningSky", "TheFinalDawn", "Wrathwing", "YharimsCrystal", "YharonsKindleStaff" } }
        };

        private static readonly Dictionary<string, string[]> VanillaRelicToWeapons = new()
        {
            { "KingSlimeRelic", new[] { "SlimeStaff" } },
            { "EyeofCthulhuRelic", new[] { "ShieldofCthulhu" } },
            { "BrainofCthulhuRelic", new[] { "TheRottedFork" } },
            { "QueenBeeRelic", new[] { "BeeGun", "TheBeesKnees", "BeeKeeper", "Beenade" } },
            { "SkeletronRelic", new[] { "BookofSkulls", "BoneGlove", "SkeletronHand" } },
            { "WallofFleshRelic", new[] { "BreakerBlade", "ClockworkAssaultRifle", "LaserRifle", "FireWhip", "Pwnhammer" } },
            { "QueenSlimeRelic", new[] { "QueenSlimeMinionStaff" } },
            { "PlanteraRelic", new[] { "GrenadeLauncher", "VenusMagnum", "NettleBurst", "LeafBlower", "FlowerPow", "WaspGun", "Seedler", "TheAxe", "PygmyStaff" } },
            { "GolemRelic", new[] { "Picksaw", "PossessedHatchet", "StaffofEarth", "HeatRay", "GolemFist", "Stynger" } },
            { "DukeFishronRelic", new[] { "BubbleGun", "Flairon", "RazorbladeTyphoon", "TempestStaff", "Tsunami" } },
            { "EmpressOfLightRelic", new[] { "Nightglow", "Starlight", "Kaleidoscope", "Eventide", "RainbowCursor", "Terraprisma" } },
            { "DeerclopsRelic", new[] { "WeatherPain", "HoundiusShootius", "LucyTheAxe" } },
            { "MoonLordRelic", new[] { "Meowmere", "StarWrath", "Terrarian", "SDMG", "LastPrism", "LunarFlareBook", "RainbowCrystalStaff", "LunarPortalStaff", "CelebrationMk2", "MoonlordTurretStaff" } }
        };

        public override void AddRecipes()
        {
            if (CalamityLegendsComeBackConfig.Instance?.AllowBossRelicWeaponRecipes != true)
                return;

            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;

            foreach (var kvp in RelicToWeapons)
            {
                string relicName = kvp.Key;
                if (!calamity.TryFind<ModItem>(relicName, out ModItem relicItem))
                    continue;

                foreach (string weaponName in kvp.Value)
                {
                    if (!calamity.TryFind<ModItem>(weaponName, out ModItem weaponItem))
                        continue;

                    // 1 Relic -> 1 Weapon (无任何工作站，空手合成)
                    Recipe recipe = Recipe.Create(weaponItem.Type);
                    recipe.AddIngredient(relicItem.Type, 1);
                    recipe.Register();
                }
            }

            foreach (var kvp in VanillaRelicToWeapons)
            {
                if (!ItemID.Search.TryGetId(kvp.Key, out int relicType))
                    continue;

                foreach (string weaponName in kvp.Value)
                {
                    if (!ItemID.Search.TryGetId(weaponName, out int weaponType))
                        continue;

                    Recipe recipe = Recipe.Create(weaponType);
                    recipe.AddIngredient(relicType);
                    recipe.Register();
                }
            }
        }
    }
}
