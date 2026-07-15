using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks
{
    internal enum LegendsWeaponBossMovementStyle
    {
        Hover,
        HeavyHover,
        Worm,
        VoidCore
    }

    internal enum LegendsWeaponAttackPattern
    {
        Auto,
        Slash,
        Gunline,
        MagicCore,
        SummonCore,
        ReturningBlade,
        BombRain,
        StarField,
        LightningChain,
        SpaceRift,
        AcidRain,
        CreatureRush,
        BloodPulse
    }

    internal sealed class LegendsWeaponBossAttack
    {
        public LegendsWeaponBossAttack(string itemName, string displayName, LegendsWeaponAttackPattern pattern = LegendsWeaponAttackPattern.Auto)
        {
            ItemName = itemName;
            DisplayName = displayName;
            Pattern = pattern;
        }

        public string ItemName { get; }
        public string DisplayName { get; }
        public LegendsWeaponAttackPattern Pattern { get; }
    }

    internal sealed class LegendsWeaponBossProfile
    {
        public LegendsWeaponBossProfile(string displayName, Color themeColor, LegendsWeaponBossMovementStyle movementStyle, string[] npcNames, LegendsWeaponBossAttack[] attacks)
        {
            DisplayName = displayName;
            ThemeColor = themeColor;
            MovementStyle = movementStyle;
            NpcNames = npcNames;
            Attacks = attacks;
        }

        public string DisplayName { get; }
        public Color ThemeColor { get; }
        public LegendsWeaponBossMovementStyle MovementStyle { get; }
        public string[] NpcNames { get; }
        public LegendsWeaponBossAttack[] Attacks { get; }
    }

    internal static class LegendsWeaponBossProfiles
    {
        public static readonly LegendsWeaponBossProfile[] All =
        {
            new(
                "极地冰灵",
                new Color(126, 224, 255),
                LegendsWeaponBossMovementStyle.HeavyHover,
                new[] { "Cryogen" },
                new[]
                {
                    A("HoarfrostBow", "白霜弓 / Hoarfrost Bow", LegendsWeaponAttackPattern.Gunline),
                    A("Icebreaker", "破冰者 / Icebreaker", LegendsWeaponAttackPattern.ReturningBlade),
                    A("Avalanche", "雪崩 / Avalanche", LegendsWeaponAttackPattern.BombRain),
                    A("SnowstormStaff", "冰晶风暴 / Snowstorm Staff", LegendsWeaponAttackPattern.MagicCore),
                    A("SoulofCryogen", "极寒之魂 / Soul of Cryogen", LegendsWeaponAttackPattern.CreatureRush),
                    A("GlacialEmbrace", "冰川之拥 / Glacial Embrace", LegendsWeaponAttackPattern.SummonCore),
                    A("CryoStone", "冰川石 / Cryo Stone", LegendsWeaponAttackPattern.StarField),
                    A("FrostFlare", "霜冻之炎 / Frost Flare", LegendsWeaponAttackPattern.BombRain),
                    A("DarklightGreatsword", "巨剑夜光 / Darklight Greatsword", LegendsWeaponAttackPattern.Slash),
                    A("StarnightLance", "星夜长枪 / Starnight Lance", LegendsWeaponAttackPattern.StarField),
                    A("Shimmerspark", "烁光 / Shimmerspark", LegendsWeaponAttackPattern.MagicCore),
                    A("DarkechoGreatbow", "暗之回响 / Darkecho Greatbow", LegendsWeaponAttackPattern.Gunline),
                    A("ShadecrystalBarrage", "暗晶风暴 / Shadecrystal Barrage", LegendsWeaponAttackPattern.MagicCore),
                    A("DaedalusGolemStaff", "代达罗斯守卫法杖 / Daedalus Golem Staff", LegendsWeaponAttackPattern.SummonCore),
                    A("CrystalPiercer", "水晶穿刺者 / Crystal Piercer", LegendsWeaponAttackPattern.Slash)
                }),

            new(
                "瘟疫使者歌莉娅",
                new Color(144, 255, 86),
                LegendsWeaponBossMovementStyle.Hover,
                new[] { "PlaguebringerGoliath" },
                new[]
                {
                    A("Virulence", "瘟疫毒刃 / Virulence", LegendsWeaponAttackPattern.Slash),
                    A("Malevolence", "蜂毒 / Malevolence", LegendsWeaponAttackPattern.Gunline),
                    A("PlagueStaff", "瘟疫法杖 / Plague Staff", LegendsWeaponAttackPattern.MagicCore),
                    A("FuelCellBundle", "燃料电池组 / Fuel Cell Bundle", LegendsWeaponAttackPattern.SummonCore),
                    A("InfectedRemote", "瘟疫控制器 / Infected Remote", LegendsWeaponAttackPattern.SummonCore),
                    A("TheSyringe", "瘟疫注射器 / The Syringe", LegendsWeaponAttackPattern.ReturningBlade),
                    A("DiseasedPike", "瘟疫长枪 / Diseased Pike", LegendsWeaponAttackPattern.Slash),
                    A("TheHive", "蜂巢发射器 / The Hive", LegendsWeaponAttackPattern.CreatureRush),
                    A("PestilentDefiler", "感染者 / Pestilent Defiler", LegendsWeaponAttackPattern.Gunline),
                    A("Malachite", "孔雀翎 / Malachite", LegendsWeaponAttackPattern.ReturningBlade),
                    A("ToxicHeart", "毒疫之心 / Toxic Heart", LegendsWeaponAttackPattern.AcidRain),
                    A("PlagueCaller", "瘟疫呼机 / Plague Caller", LegendsWeaponAttackPattern.CreatureRush),
                    A("BlightSpewer", "枯萎散播者 / Blight Spewer", LegendsWeaponAttackPattern.AcidRain),
                    A("Pandemic", "瘟疫狂潮 / Pandemic", LegendsWeaponAttackPattern.CreatureRush),
                    A("PlagueTaintedSMG", "玷污之疫 SMG / Plague Tainted SMG", LegendsWeaponAttackPattern.Gunline)
                }),

            new(
                "灾厄克隆体",
                new Color(255, 68, 70),
                LegendsWeaponBossMovementStyle.Hover,
                new[] { "CalamitasClone" },
                new[]
                {
                    A("Oblivion", "遗忘 / Oblivion", LegendsWeaponAttackPattern.SpaceRift),
                    A("Animosity", "敌意 / Animosity", LegendsWeaponAttackPattern.Gunline),
                    A("LashesofChaos", "混乱鞭笞 / Lashes of Chaos", LegendsWeaponAttackPattern.BombRain),
                    A("EntropysVigil", "熵之守望 / Entropy's Vigil", LegendsWeaponAttackPattern.SummonCore),
                    A("CrushsawCrasher", "碎锯冲击者 / Crushsaw Crasher", LegendsWeaponAttackPattern.ReturningBlade),
                    A("HavocsBreath", "浩劫之息 / Havoc's Breath", LegendsWeaponAttackPattern.BombRain)
                }),

            new(
                "利维坦和阿纳西塔",
                new Color(70, 210, 255),
                LegendsWeaponBossMovementStyle.HeavyHover,
                new[] { "Leviathan", "Anahita" },
                new[]
                {
                    A("Greentide", "碧潮 / Greentide", LegendsWeaponAttackPattern.Slash),
                    A("Leviatitan", "利维泰坦 / Leviatitan", LegendsWeaponAttackPattern.Slash),
                    A("AnahitasArpeggio", "阿纳西塔琶音 / Anahita's Arpeggio", LegendsWeaponAttackPattern.StarField),
                    A("Atlantis", "亚特兰蒂斯 / Atlantis", LegendsWeaponAttackPattern.Gunline),
                    A("GastricBelcherStaff", "胃液喷吐杖 / Gastric Belcher Staff", LegendsWeaponAttackPattern.AcidRain),
                    A("Whitewater", "白浪 / Whitewater", LegendsWeaponAttackPattern.MagicCore),
                    A("LeviathanTeeth", "利维坦之牙 / Leviathan Teeth", LegendsWeaponAttackPattern.CreatureRush)
                }),

            new(
                "白金星舰",
                new Color(214, 110, 255),
                LegendsWeaponBossMovementStyle.HeavyHover,
                new[] { "AstrumAureus" },
                new[]
                {
                    A("Nebulash", "星云鞭 / Nebulash", LegendsWeaponAttackPattern.Slash),
                    A("AuroraBlazer", "极光烈焰枪 / Aurora Blazer", LegendsWeaponAttackPattern.Gunline),
                    A("AlulaAustralis", "南翼星 / Alula Australis", LegendsWeaponAttackPattern.StarField),
                    A("BorealisBomber", "北辉轰炸器 / Borealis Bomber", LegendsWeaponAttackPattern.BombRain),
                    A("AuroradicalThrow", "极光回旋镖 / Auroradical Throw", LegendsWeaponAttackPattern.ReturningBlade),
                    A("AstralScythe", "星辉镰刀 / Astral Scythe", LegendsWeaponAttackPattern.Slash),
                    A("TitanArm", "泰坦臂 / Titan Arm", LegendsWeaponAttackPattern.Slash),
                    A("StellarCannon", "恒星炮 / Stellar Cannon", LegendsWeaponAttackPattern.Gunline),
                    A("StellarKnife", "恒星飞刀 / Stellar Knife", LegendsWeaponAttackPattern.ReturningBlade),
                    A("AstralachneaStaff", "星幻蛛法杖 / Astralachnea Staff", LegendsWeaponAttackPattern.SummonCore),
                    A("AbandonedSlimeStaff", "遗弃史莱姆法杖 / Abandoned Slime Staff", LegendsWeaponAttackPattern.CreatureRush),
                    A("HivePod", "蜂巢荚 / Hive Pod", LegendsWeaponAttackPattern.CreatureRush)
                }),

            new(
                "毁灭魔像",
                new Color(196, 44, 42),
                LegendsWeaponBossMovementStyle.HeavyHover,
                new[] { "RavagerBody" },
                new[]
                {
                    A("UltimusCleaver", "终极裂肉刀 / Ultimus Cleaver", LegendsWeaponAttackPattern.Slash),
                    A("RealmRavager", "领域毁灭者 / Realm Ravager", LegendsWeaponAttackPattern.SpaceRift),
                    A("Hematemesis", "咯血 / Hematemesis", LegendsWeaponAttackPattern.BloodPulse),
                    A("SpikecragStaff", "尖刺岩杖 / Spikecrag Staff", LegendsWeaponAttackPattern.MagicCore),
                    A("CraniumSmasher", "颅骨粉碎者 / Cranium Smasher", LegendsWeaponAttackPattern.ReturningBlade),
                    A("Vesuvius", "维苏威 / Vesuvius", LegendsWeaponAttackPattern.BombRain),
                    A("CorpusAvertor", "血肉转向器 / Corpus Avertor", LegendsWeaponAttackPattern.BloodPulse),
                    A("FleshTotem", "血肉图腾 / Flesh Totem", LegendsWeaponAttackPattern.SummonCore),
                    A("TheMutilator", "肢解者 / The Mutilator", LegendsWeaponAttackPattern.BloodPulse),
                    A("Lacerator", "撕裂者 / Lacerator", LegendsWeaponAttackPattern.BloodPulse),
                    A("ClaretCannon", "深红火炮 / Claret Cannon", LegendsWeaponAttackPattern.Gunline),
                    A("ArterialAssault", "动脉突袭 / Arterial Assault", LegendsWeaponAttackPattern.BloodPulse),
                    A("BloodBoiler", "沸血器 / Blood Boiler", LegendsWeaponAttackPattern.BombRain),
                    A("SanguineFlare", "血色耀斑 / Sanguine Flare", LegendsWeaponAttackPattern.BombRain),
                    A("Viscera", "内脏 / Viscera", LegendsWeaponAttackPattern.BloodPulse),
                    A("DragonbloodDisgorger", "龙血喷吐者 / Dragonblood Disgorger", LegendsWeaponAttackPattern.CreatureRush),
                    A("BloodsoakedCrasher", "浸血冲击锤 / Bloodsoaked Crasher", LegendsWeaponAttackPattern.ReturningBlade)
                }),

            new(
                "星神游龙",
                new Color(122, 104, 255),
                LegendsWeaponBossMovementStyle.Worm,
                new[] { "AstrumDeusHead" },
                new[]
                {
                    A("TheMicrowave", "微波炮 / The Microwave", LegendsWeaponAttackPattern.Gunline),
                    A("StarSputter", "星点溅射器 / Star Sputter", LegendsWeaponAttackPattern.StarField),
                    A("StarShower", "星雨 / Star Shower", LegendsWeaponAttackPattern.StarField),
                    A("StarspawnHelixStaff", "星裔螺旋杖 / Starspawn Helix Staff", LegendsWeaponAttackPattern.SummonCore),
                    A("RegulusRiot", "轩辕星暴动 / Regulus Riot", LegendsWeaponAttackPattern.StarField),
                    A("AstralPike", "星辉长枪 / Astral Pike", LegendsWeaponAttackPattern.Slash),
                    A("AstralBlaster", "星辉爆破枪 / Astral Blaster", LegendsWeaponAttackPattern.Gunline),
                    A("AstralStaff", "星辉法杖 / Astral Staff", LegendsWeaponAttackPattern.MagicCore),
                    A("RadiantStar", "辉耀星 / Radiant Star", LegendsWeaponAttackPattern.StarField),
                    A("TrueBiomeBlade", "真环境之刃 / True Biome Blade", LegendsWeaponAttackPattern.Slash)
                }),

            new(
                "Dragonfolly",
                new Color(255, 210, 76),
                LegendsWeaponBossMovementStyle.Hover,
                new[] { "Dragonfolly" },
                new[]
                {
                    A("GildedProboscis", "镀金长喙 / Gilded Proboscis", LegendsWeaponAttackPattern.CreatureRush),
                    A("GoldenEagle", "黄金之鹰 / Golden Eagle", LegendsWeaponAttackPattern.Gunline),
                    A("RougeSlash", "胭脂斩 / Rouge Slash", LegendsWeaponAttackPattern.Slash)
                }),

            new(
                "普罗维登斯",
                new Color(255, 194, 76),
                LegendsWeaponBossMovementStyle.HeavyHover,
                new[] { "Providence" },
                new[]
                {
                    A("HolyCollider", "神圣碰撞器 / Holy Collider", LegendsWeaponAttackPattern.Slash),
                    A("BurningRevelation", "燃烧启示录 / Burning Revelation", LegendsWeaponAttackPattern.BombRain),
                    A("TelluricGlare", "大地耀目 / Telluric Glare", LegendsWeaponAttackPattern.Gunline),
                    A("BlissfulBombardier", "至福轰炸器 / Blissful Bombardier", LegendsWeaponAttackPattern.BombRain),
                    A("PurgeGuzzler", "净化吞食者 / Purge Guzzler", LegendsWeaponAttackPattern.MagicCore),
                    A("DazzlingStabberStaff", "炫目刺击杖 / Dazzling Stabber Staff", LegendsWeaponAttackPattern.SummonCore),
                    A("MoltenAmputator", "熔火截肢者 / Molten Amputator", LegendsWeaponAttackPattern.ReturningBlade),
                    A("PristineFury", "圣洁怒火 / Pristine Fury", LegendsWeaponAttackPattern.Gunline),
                    A("AetherfluxCannon", "以太通量炮 / Aetherflux Cannon", LegendsWeaponAttackPattern.StarField),
                    A("AngelicShotgun", "天使霰弹枪 / Angelic Shotgun", LegendsWeaponAttackPattern.Gunline),
                    A("DarkSpark", "暗黑火花 / Dark Spark", LegendsWeaponAttackPattern.BombRain),
                    A("GalactusBlade", "星河吞噬之刃 / Galactus Blade", LegendsWeaponAttackPattern.Slash),
                    A("HandheldTank", "手持坦克 / Handheld Tank", LegendsWeaponAttackPattern.Gunline),
                    A("MirrorofKalandra", "卡兰德拉之镜 / Mirror of Kalandra", LegendsWeaponAttackPattern.SpaceRift),
                    A("Mourningstar", "哀悼之星 / Mourningstar", LegendsWeaponAttackPattern.StarField),
                    A("ShatteredDawn", "破晓碎光 / Shattered Dawn", LegendsWeaponAttackPattern.ReturningBlade),
                    A("SeekingScorcher", "追踪灼炎 / Seeking Scorcher", LegendsWeaponAttackPattern.BombRain),
                    A("TheMaelstrom", "大漩涡 / The Maelstrom", LegendsWeaponAttackPattern.Gunline),
                    A("ThePrince", "王子 / The Prince", LegendsWeaponAttackPattern.MagicCore)
                }),

            new(
                "风暴编织者",
                new Color(96, 232, 255),
                LegendsWeaponBossMovementStyle.Worm,
                new[] { "StormWeaverHead" },
                new[]
                {
                    A("SkytideDragoon", "天潮龙骑枪 / Skytide Dragoon", LegendsWeaponAttackPattern.Slash),
                    A("TheStorm", "风暴 / The Storm", LegendsWeaponAttackPattern.LightningChain),
                    A("Volterion", "伏特隆 / Volterion", LegendsWeaponAttackPattern.LightningChain),
                    A("AquasScepter", "碧水权杖 / Aqua's Scepter", LegendsWeaponAttackPattern.MagicCore),
                    A("CorinthPrime", "科林斯至尊 / Corinth Prime", LegendsWeaponAttackPattern.Gunline),
                    A("StellarTorusStaff", "星环法杖 / Stellar Torus Staff", LegendsWeaponAttackPattern.SummonCore),
                    A("Teslastaff", "特斯拉法杖 / Tesla Staff", LegendsWeaponAttackPattern.LightningChain),
                    A("TwistingThunder", "扭曲雷霆 / Twisting Thunder", LegendsWeaponAttackPattern.LightningChain),
                    A("ThePack", "群狼 / The Pack", LegendsWeaponAttackPattern.Gunline),
                    A("ShadowboltStaff", "暗影箭杖 / Shadowbolt Staff", LegendsWeaponAttackPattern.MagicCore),
                    A("Seadragon", "海龙 / Seadragon", LegendsWeaponAttackPattern.CreatureRush),
                    A("FourSeasonsGalaxia", "四季银河 / Four Seasons Galaxia", LegendsWeaponAttackPattern.StarField),
                    A("RealityRupture", "现实撕裂 / Reality Rupture", LegendsWeaponAttackPattern.SpaceRift)
                }),

            new(
                "无尽虚空",
                new Color(146, 86, 255),
                LegendsWeaponBossMovementStyle.VoidCore,
                new[] { "CeaselessVoid" },
                new[]
                {
                    A("MirrorBlade", "镜刃 / Mirror Blade", LegendsWeaponAttackPattern.SpaceRift),
                    A("VoidConcentrationStaff", "虚空凝聚杖 / Void Concentration Staff", LegendsWeaponAttackPattern.MagicCore),
                    A("DarkSpark", "暗黑火花 / Dark Spark", LegendsWeaponAttackPattern.BombRain),
                    A("EventHorizon", "事件视界 / Event Horizon", LegendsWeaponAttackPattern.SpaceRift),
                    A("Mistlestorm", "槲寄生风暴 / Mistlestorm", LegendsWeaponAttackPattern.StarField),
                    A("OntologicalDespoiler", "本体论亵渎者 / Ontological Despoiler", LegendsWeaponAttackPattern.Gunline),
                    A("SealedSingularity", "密封奇点 / Sealed Singularity", LegendsWeaponAttackPattern.SpaceRift),
                    A("TacticiansTrumpCard", "战术家的王牌 / Tactician's Trump Card", LegendsWeaponAttackPattern.SummonCore),
                    A("Eternity", "永恒 / Eternity", LegendsWeaponAttackPattern.MagicCore),
                    A("PhantasmalFury", "幻魂怒火 / Phantasmal Fury", LegendsWeaponAttackPattern.Gunline),
                    A("FourSeasonsGalaxia", "四季银河 / Four Seasons Galaxia", LegendsWeaponAttackPattern.StarField),
                    A("RealityRupture", "现实撕裂 / Reality Rupture", LegendsWeaponAttackPattern.SpaceRift)
                }),

            new(
                "西格纳斯",
                new Color(118, 86, 255),
                LegendsWeaponBossMovementStyle.Hover,
                new[] { "Signus" },
                new[]
                {
                    A("CosmicKunai", "宇宙苦无 / Cosmic Kunai", LegendsWeaponAttackPattern.ReturningBlade),
                    A("Cosmilamp", "宇宙灯 / Cosmilamp", LegendsWeaponAttackPattern.SummonCore),
                    A("AethersWhisper", "以太低语 / Aether's Whisper", LegendsWeaponAttackPattern.StarField),
                    A("DeathsAscension", "死亡升华 / Death's Ascension", LegendsWeaponAttackPattern.Slash),
                    A("EmpyreanKnives", "至天飞刀 / Empyrean Knives", LegendsWeaponAttackPattern.ReturningBlade),
                    A("KingofConstellationsTenryu", "星座之王天龙 / King of Constellations, Tenryu", LegendsWeaponAttackPattern.SummonCore),
                    A("MagneticMeltdown", "磁能熔毁 / Magnetic Meltdown", LegendsWeaponAttackPattern.LightningChain),
                    A("Nadir", "天底 / Nadir", LegendsWeaponAttackPattern.SpaceRift),
                    A("TheSevensStriker", "七发打击者 / The Sevens Striker", LegendsWeaponAttackPattern.Gunline),
                    A("VenusianTrident", "金星三叉戟 / Venusian Trident", LegendsWeaponAttackPattern.Slash),
                    A("FourSeasonsGalaxia", "四季银河 / Four Seasons Galaxia", LegendsWeaponAttackPattern.StarField),
                    A("RealityRupture", "现实撕裂 / Reality Rupture", LegendsWeaponAttackPattern.SpaceRift)
                }),

            new(
                "噬魂幽花",
                new Color(206, 86, 255),
                LegendsWeaponBossMovementStyle.Hover,
                new[] { "Polterghast" },
                new[]
                {
                    A("TerrorBlade", "惊惧之刃 / Terror Blade", LegendsWeaponAttackPattern.Slash),
                    A("BansheeHook", "女妖之钩 / Banshee Hook", LegendsWeaponAttackPattern.ReturningBlade),
                    A("DaemonsFlame", "魔鬼之焰 / Daemon's Flame", LegendsWeaponAttackPattern.BombRain),
                    A("FatesReveal", "命运揭示 / Fate's Reveal", LegendsWeaponAttackPattern.MagicCore),
                    A("GhastlyVisage", "幽魂面容 / Ghastly Visage", LegendsWeaponAttackPattern.SummonCore),
                    A("EtherealSubjugator", "虚灵支配者 / Ethereal Subjugator", LegendsWeaponAttackPattern.SummonCore),
                    A("GhoulishGouger", "食尸鬼钻掘者 / Ghoulish Gouger", LegendsWeaponAttackPattern.CreatureRush),
                    A("GalileoGladius", "伽利略短剑 / Galileo Gladius", LegendsWeaponAttackPattern.Slash),
                    A("CrescentMoon", "新月 / Crescent Moon", LegendsWeaponAttackPattern.ReturningBlade),
                    A("HalleysInferno", "哈雷地狱火 / Halley's Inferno", LegendsWeaponAttackPattern.BombRain),
                    A("AlphaDraconis", "右枢星 / Alpha Draconis", LegendsWeaponAttackPattern.StarField),
                    A("StratusSphere", "层云球 / Stratus Sphere", LegendsWeaponAttackPattern.MagicCore),
                    A("Sirius", "天狼星 / Sirius", LegendsWeaponAttackPattern.StarField),
                    A("WarloksMoonFist", "战月之拳 / Warloks' Moon Fist", LegendsWeaponAttackPattern.SummonCore),
                    A("Vega", "织女星 / Vega", LegendsWeaponAttackPattern.StarField)
                }),

            new(
                "渊海灾虫",
                new Color(68, 214, 180),
                LegendsWeaponBossMovementStyle.Worm,
                new[] { "AquaticScourgeHead" },
                new[]
                {
                    A("SubmarineShocker", "潜艇震击者 / Submarine Shocker", LegendsWeaponAttackPattern.LightningChain),
                    A("Barinautical", "巴利纳提卡 / Barinautical", LegendsWeaponAttackPattern.Gunline),
                    A("Downpour", "倾盆大雨 / Downpour", LegendsWeaponAttackPattern.AcidRain),
                    A("DeepseaStaff", "深海法杖 / Deepsea Staff", LegendsWeaponAttackPattern.MagicCore),
                    A("ScourgeoftheSeas", "海洋灾厄 / Scourge of the Seas", LegendsWeaponAttackPattern.CreatureRush),
                    A("FlakToxicannon", "毒性高射炮 / Flak Toxicannon", LegendsWeaponAttackPattern.Gunline),
                    A("SlitheringEels", "滑行电鳗 / Slithering Eels", LegendsWeaponAttackPattern.CreatureRush),
                    A("CausticCroakerStaff", "腐蚀蛙杖 / Caustic Croaker Staff", LegendsWeaponAttackPattern.SummonCore),
                    A("SkyfinBombers", "天鳍轰炸机 / Skyfin Bombers", LegendsWeaponAttackPattern.BombRain),
                    A("SpentFuelContainer", "废燃料容器 / Spent Fuel Container", LegendsWeaponAttackPattern.AcidRain),
                    A("SulphurousGrabber", "硫磺抓取器 / Sulphurous Grabber", LegendsWeaponAttackPattern.ReturningBlade)
                }),

            new(
                "硫海遗爵",
                new Color(176, 238, 80),
                LegendsWeaponBossMovementStyle.Hover,
                new[] { "OldDuke" },
                new[]
                {
                    A("InsidiousImpaler", "阴险穿刺者 / Insidious Impaler", LegendsWeaponAttackPattern.Slash),
                    A("FetidEmesis", "恶臭呕吐 / Fetid Emesis", LegendsWeaponAttackPattern.AcidRain),
                    A("SepticSkewer", "败血穿叉 / Septic Skewer", LegendsWeaponAttackPattern.Slash),
                    A("VitriolicViper", "硫酸毒蛇 / Vitriolic Viper", LegendsWeaponAttackPattern.CreatureRush),
                    A("MutatedTruffle", "变异松露 / Mutated Truffle", LegendsWeaponAttackPattern.CreatureRush),
                    A("CadaverousCarrion", "腐尸秃鹫 / Cadaverous Carrion", LegendsWeaponAttackPattern.CreatureRush),
                    A("ToxicantTwister", "毒素旋风 / Toxicant Twister", LegendsWeaponAttackPattern.AcidRain),
                    A("TheOldReaper", "老收割者 / The Old Reaper", LegendsWeaponAttackPattern.ReturningBlade),
                    A("SulphuricAcidCannon", "硫酸炮 / Sulphuric Acid Cannon", LegendsWeaponAttackPattern.Gunline),
                    A("GammaHeart", "伽马之心 / Gamma Heart", LegendsWeaponAttackPattern.AcidRain),
                    A("PhosphorescentGauntlet", "磷光拳套 / Phosphorescent Gauntlet", LegendsWeaponAttackPattern.Slash),
                    A("FlakToxicannon", "毒性高射炮 / Flak Toxicannon", LegendsWeaponAttackPattern.Gunline),
                    A("SlitheringEels", "滑行电鳗 / Slithering Eels", LegendsWeaponAttackPattern.CreatureRush),
                    A("SkyfinBombers", "天鳍轰炸机 / Skyfin Bombers", LegendsWeaponAttackPattern.BombRain),
                    A("SpentFuelContainer", "废燃料容器 / Spent Fuel Container", LegendsWeaponAttackPattern.AcidRain),
                    A("SulphurousGrabber", "硫磺抓取器 / Sulphurous Grabber", LegendsWeaponAttackPattern.ReturningBlade)
                })
        };

        private static LegendsWeaponBossAttack A(string itemName, string displayName, LegendsWeaponAttackPattern pattern = LegendsWeaponAttackPattern.Auto)
        {
            return new LegendsWeaponBossAttack(itemName, displayName, pattern);
        }
    }
}
