using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks
{
    internal enum IUMWWeaponBossMovementStyle
    {
        Hover,
        HeavyHover,
        Worm,
        VoidCore
    }

    internal enum IUMWWeaponAttackPattern
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

    internal sealed class IUMWWeaponBossAttack
    {
        public IUMWWeaponBossAttack(string itemName, string displayName, IUMWWeaponAttackPattern pattern = IUMWWeaponAttackPattern.Auto)
        {
            ItemName = itemName;
            DisplayName = displayName;
            Pattern = pattern;
        }

        public string ItemName { get; }
        public string DisplayName { get; }
        public IUMWWeaponAttackPattern Pattern { get; }
    }

    internal sealed class IUMWWeaponBossProfile
    {
        public IUMWWeaponBossProfile(string displayName, Color themeColor, IUMWWeaponBossMovementStyle movementStyle, string[] npcNames, IUMWWeaponBossAttack[] attacks)
        {
            DisplayName = displayName;
            ThemeColor = themeColor;
            MovementStyle = movementStyle;
            NpcNames = npcNames;
            Attacks = attacks;
        }

        public string DisplayName { get; }
        public Color ThemeColor { get; }
        public IUMWWeaponBossMovementStyle MovementStyle { get; }
        public string[] NpcNames { get; }
        public IUMWWeaponBossAttack[] Attacks { get; }
    }

    internal static class IUMWWeaponBossProfiles
    {
        public static readonly IUMWWeaponBossProfile[] All =
        {
            new(
                "极地冰灵",
                new Color(126, 224, 255),
                IUMWWeaponBossMovementStyle.HeavyHover,
                new[] { "Cryogen" },
                new[]
                {
                    A("HoarfrostBow", "白霜弓 / Hoarfrost Bow", IUMWWeaponAttackPattern.Gunline),
                    A("Icebreaker", "破冰者 / Icebreaker", IUMWWeaponAttackPattern.ReturningBlade),
                    A("Avalanche", "雪崩 / Avalanche", IUMWWeaponAttackPattern.BombRain),
                    A("SnowstormStaff", "冰晶风暴 / Snowstorm Staff", IUMWWeaponAttackPattern.MagicCore),
                    A("SoulofCryogen", "极寒之魂 / Soul of Cryogen", IUMWWeaponAttackPattern.CreatureRush),
                    A("GlacialEmbrace", "冰川之拥 / Glacial Embrace", IUMWWeaponAttackPattern.SummonCore),
                    A("CryoStone", "冰川石 / Cryo Stone", IUMWWeaponAttackPattern.StarField),
                    A("FrostFlare", "霜冻之炎 / Frost Flare", IUMWWeaponAttackPattern.BombRain),
                    A("DarklightGreatsword", "巨剑夜光 / Darklight Greatsword", IUMWWeaponAttackPattern.Slash),
                    A("StarnightLance", "星夜长枪 / Starnight Lance", IUMWWeaponAttackPattern.StarField),
                    A("Shimmerspark", "烁光 / Shimmerspark", IUMWWeaponAttackPattern.MagicCore),
                    A("DarkechoGreatbow", "暗之回响 / Darkecho Greatbow", IUMWWeaponAttackPattern.Gunline),
                    A("ShadecrystalBarrage", "暗晶风暴 / Shadecrystal Barrage", IUMWWeaponAttackPattern.MagicCore),
                    A("DaedalusGolemStaff", "代达罗斯守卫法杖 / Daedalus Golem Staff", IUMWWeaponAttackPattern.SummonCore),
                    A("CrystalPiercer", "水晶穿刺者 / Crystal Piercer", IUMWWeaponAttackPattern.Slash)
                }),

            new(
                "瘟疫使者歌莉娅",
                new Color(144, 255, 86),
                IUMWWeaponBossMovementStyle.Hover,
                new[] { "PlaguebringerGoliath" },
                new[]
                {
                    A("Virulence", "瘟疫毒刃 / Virulence", IUMWWeaponAttackPattern.Slash),
                    A("Malevolence", "蜂毒 / Malevolence", IUMWWeaponAttackPattern.Gunline),
                    A("PlagueStaff", "瘟疫法杖 / Plague Staff", IUMWWeaponAttackPattern.MagicCore),
                    A("FuelCellBundle", "燃料电池组 / Fuel Cell Bundle", IUMWWeaponAttackPattern.SummonCore),
                    A("InfectedRemote", "瘟疫控制器 / Infected Remote", IUMWWeaponAttackPattern.SummonCore),
                    A("TheSyringe", "瘟疫注射器 / The Syringe", IUMWWeaponAttackPattern.ReturningBlade),
                    A("DiseasedPike", "瘟疫长枪 / Diseased Pike", IUMWWeaponAttackPattern.Slash),
                    A("TheHive", "蜂巢发射器 / The Hive", IUMWWeaponAttackPattern.CreatureRush),
                    A("PestilentDefiler", "感染者 / Pestilent Defiler", IUMWWeaponAttackPattern.Gunline),
                    A("Malachite", "孔雀翎 / Malachite", IUMWWeaponAttackPattern.ReturningBlade),
                    A("ToxicHeart", "毒疫之心 / Toxic Heart", IUMWWeaponAttackPattern.AcidRain),
                    A("PlagueCaller", "瘟疫呼机 / Plague Caller", IUMWWeaponAttackPattern.CreatureRush),
                    A("BlightSpewer", "枯萎散播者 / Blight Spewer", IUMWWeaponAttackPattern.AcidRain),
                    A("Pandemic", "瘟疫狂潮 / Pandemic", IUMWWeaponAttackPattern.CreatureRush),
                    A("PlagueTaintedSMG", "玷污之疫 SMG / Plague Tainted SMG", IUMWWeaponAttackPattern.Gunline)
                }),

            new(
                "灾厄克隆体",
                new Color(255, 68, 70),
                IUMWWeaponBossMovementStyle.Hover,
                new[] { "CalamitasClone" },
                new[]
                {
                    A("Oblivion", "遗忘 / Oblivion", IUMWWeaponAttackPattern.SpaceRift),
                    A("Animosity", "敌意 / Animosity", IUMWWeaponAttackPattern.Gunline),
                    A("LashesofChaos", "混乱鞭笞 / Lashes of Chaos", IUMWWeaponAttackPattern.BombRain),
                    A("EntropysVigil", "熵之守望 / Entropy's Vigil", IUMWWeaponAttackPattern.SummonCore),
                    A("CrushsawCrasher", "碎锯冲击者 / Crushsaw Crasher", IUMWWeaponAttackPattern.ReturningBlade),
                    A("HavocsBreath", "浩劫之息 / Havoc's Breath", IUMWWeaponAttackPattern.BombRain)
                }),

            new(
                "利维坦和阿纳西塔",
                new Color(70, 210, 255),
                IUMWWeaponBossMovementStyle.HeavyHover,
                new[] { "Leviathan", "Anahita" },
                new[]
                {
                    A("Greentide", "碧潮 / Greentide", IUMWWeaponAttackPattern.Slash),
                    A("Leviatitan", "利维泰坦 / Leviatitan", IUMWWeaponAttackPattern.Slash),
                    A("AnahitasArpeggio", "阿纳西塔琶音 / Anahita's Arpeggio", IUMWWeaponAttackPattern.StarField),
                    A("Atlantis", "亚特兰蒂斯 / Atlantis", IUMWWeaponAttackPattern.Gunline),
                    A("GastricBelcherStaff", "胃液喷吐杖 / Gastric Belcher Staff", IUMWWeaponAttackPattern.AcidRain),
                    A("Whitewater", "白浪 / Whitewater", IUMWWeaponAttackPattern.MagicCore),
                    A("LeviathanTeeth", "利维坦之牙 / Leviathan Teeth", IUMWWeaponAttackPattern.CreatureRush)
                }),

            new(
                "白金星舰",
                new Color(214, 110, 255),
                IUMWWeaponBossMovementStyle.HeavyHover,
                new[] { "AstrumAureus" },
                new[]
                {
                    A("Nebulash", "星云鞭 / Nebulash", IUMWWeaponAttackPattern.Slash),
                    A("AuroraBlazer", "极光烈焰枪 / Aurora Blazer", IUMWWeaponAttackPattern.Gunline),
                    A("AlulaAustralis", "南翼星 / Alula Australis", IUMWWeaponAttackPattern.StarField),
                    A("BorealisBomber", "北辉轰炸器 / Borealis Bomber", IUMWWeaponAttackPattern.BombRain),
                    A("AuroradicalThrow", "极光回旋镖 / Auroradical Throw", IUMWWeaponAttackPattern.ReturningBlade),
                    A("AstralScythe", "星辉镰刀 / Astral Scythe", IUMWWeaponAttackPattern.Slash),
                    A("TitanArm", "泰坦臂 / Titan Arm", IUMWWeaponAttackPattern.Slash),
                    A("StellarCannon", "恒星炮 / Stellar Cannon", IUMWWeaponAttackPattern.Gunline),
                    A("StellarKnife", "恒星飞刀 / Stellar Knife", IUMWWeaponAttackPattern.ReturningBlade),
                    A("AstralachneaStaff", "星幻蛛法杖 / Astralachnea Staff", IUMWWeaponAttackPattern.SummonCore),
                    A("AbandonedSlimeStaff", "遗弃史莱姆法杖 / Abandoned Slime Staff", IUMWWeaponAttackPattern.CreatureRush),
                    A("HivePod", "蜂巢荚 / Hive Pod", IUMWWeaponAttackPattern.CreatureRush)
                }),

            new(
                "毁灭魔像",
                new Color(196, 44, 42),
                IUMWWeaponBossMovementStyle.HeavyHover,
                new[] { "RavagerBody" },
                new[]
                {
                    A("UltimusCleaver", "终极裂肉刀 / Ultimus Cleaver", IUMWWeaponAttackPattern.Slash),
                    A("RealmRavager", "领域毁灭者 / Realm Ravager", IUMWWeaponAttackPattern.SpaceRift),
                    A("Hematemesis", "咯血 / Hematemesis", IUMWWeaponAttackPattern.BloodPulse),
                    A("SpikecragStaff", "尖刺岩杖 / Spikecrag Staff", IUMWWeaponAttackPattern.MagicCore),
                    A("CraniumSmasher", "颅骨粉碎者 / Cranium Smasher", IUMWWeaponAttackPattern.ReturningBlade),
                    A("Vesuvius", "维苏威 / Vesuvius", IUMWWeaponAttackPattern.BombRain),
                    A("CorpusAvertor", "血肉转向器 / Corpus Avertor", IUMWWeaponAttackPattern.BloodPulse),
                    A("FleshTotem", "血肉图腾 / Flesh Totem", IUMWWeaponAttackPattern.SummonCore),
                    A("TheMutilator", "肢解者 / The Mutilator", IUMWWeaponAttackPattern.BloodPulse),
                    A("Lacerator", "撕裂者 / Lacerator", IUMWWeaponAttackPattern.BloodPulse),
                    A("ClaretCannon", "深红火炮 / Claret Cannon", IUMWWeaponAttackPattern.Gunline),
                    A("ArterialAssault", "动脉突袭 / Arterial Assault", IUMWWeaponAttackPattern.BloodPulse),
                    A("BloodBoiler", "沸血器 / Blood Boiler", IUMWWeaponAttackPattern.BombRain),
                    A("SanguineFlare", "血色耀斑 / Sanguine Flare", IUMWWeaponAttackPattern.BombRain),
                    A("Viscera", "内脏 / Viscera", IUMWWeaponAttackPattern.BloodPulse),
                    A("DragonbloodDisgorger", "龙血喷吐者 / Dragonblood Disgorger", IUMWWeaponAttackPattern.CreatureRush),
                    A("BloodsoakedCrasher", "浸血冲击锤 / Bloodsoaked Crasher", IUMWWeaponAttackPattern.ReturningBlade)
                }),

            new(
                "星神游龙",
                new Color(122, 104, 255),
                IUMWWeaponBossMovementStyle.Worm,
                new[] { "AstrumDeusHead" },
                new[]
                {
                    A("TheMicrowave", "微波炮 / The Microwave", IUMWWeaponAttackPattern.Gunline),
                    A("StarSputter", "星点溅射器 / Star Sputter", IUMWWeaponAttackPattern.StarField),
                    A("StarShower", "星雨 / Star Shower", IUMWWeaponAttackPattern.StarField),
                    A("StarspawnHelixStaff", "星裔螺旋杖 / Starspawn Helix Staff", IUMWWeaponAttackPattern.SummonCore),
                    A("RegulusRiot", "轩辕星暴动 / Regulus Riot", IUMWWeaponAttackPattern.StarField),
                    A("AstralPike", "星辉长枪 / Astral Pike", IUMWWeaponAttackPattern.Slash),
                    A("AstralBlaster", "星辉爆破枪 / Astral Blaster", IUMWWeaponAttackPattern.Gunline),
                    A("AstralStaff", "星辉法杖 / Astral Staff", IUMWWeaponAttackPattern.MagicCore),
                    A("RadiantStar", "辉耀星 / Radiant Star", IUMWWeaponAttackPattern.StarField),
                    A("TrueBiomeBlade", "真环境之刃 / True Biome Blade", IUMWWeaponAttackPattern.Slash)
                }),

            new(
                "Dragonfolly",
                new Color(255, 210, 76),
                IUMWWeaponBossMovementStyle.Hover,
                new[] { "Dragonfolly" },
                new[]
                {
                    A("GildedProboscis", "镀金长喙 / Gilded Proboscis", IUMWWeaponAttackPattern.CreatureRush),
                    A("GoldenEagle", "黄金之鹰 / Golden Eagle", IUMWWeaponAttackPattern.Gunline),
                    A("RougeSlash", "胭脂斩 / Rouge Slash", IUMWWeaponAttackPattern.Slash)
                }),

            new(
                "普罗维登斯",
                new Color(255, 194, 76),
                IUMWWeaponBossMovementStyle.HeavyHover,
                new[] { "Providence" },
                new[]
                {
                    A("HolyCollider", "神圣碰撞器 / Holy Collider", IUMWWeaponAttackPattern.Slash),
                    A("BurningRevelation", "燃烧启示录 / Burning Revelation", IUMWWeaponAttackPattern.BombRain),
                    A("TelluricGlare", "大地耀目 / Telluric Glare", IUMWWeaponAttackPattern.Gunline),
                    A("BlissfulBombardier", "至福轰炸器 / Blissful Bombardier", IUMWWeaponAttackPattern.BombRain),
                    A("PurgeGuzzler", "净化吞食者 / Purge Guzzler", IUMWWeaponAttackPattern.MagicCore),
                    A("DazzlingStabberStaff", "炫目刺击杖 / Dazzling Stabber Staff", IUMWWeaponAttackPattern.SummonCore),
                    A("MoltenAmputator", "熔火截肢者 / Molten Amputator", IUMWWeaponAttackPattern.ReturningBlade),
                    A("PristineFury", "圣洁怒火 / Pristine Fury", IUMWWeaponAttackPattern.Gunline),
                    A("AetherfluxCannon", "以太通量炮 / Aetherflux Cannon", IUMWWeaponAttackPattern.StarField),
                    A("AngelicShotgun", "天使霰弹枪 / Angelic Shotgun", IUMWWeaponAttackPattern.Gunline),
                    A("DarkSpark", "暗黑火花 / Dark Spark", IUMWWeaponAttackPattern.BombRain),
                    A("GalactusBlade", "星河吞噬之刃 / Galactus Blade", IUMWWeaponAttackPattern.Slash),
                    A("HandheldTank", "手持坦克 / Handheld Tank", IUMWWeaponAttackPattern.Gunline),
                    A("MirrorofKalandra", "卡兰德拉之镜 / Mirror of Kalandra", IUMWWeaponAttackPattern.SpaceRift),
                    A("Mourningstar", "哀悼之星 / Mourningstar", IUMWWeaponAttackPattern.StarField),
                    A("ShatteredDawn", "破晓碎光 / Shattered Dawn", IUMWWeaponAttackPattern.ReturningBlade),
                    A("SeekingScorcher", "追踪灼炎 / Seeking Scorcher", IUMWWeaponAttackPattern.BombRain),
                    A("TheMaelstrom", "大漩涡 / The Maelstrom", IUMWWeaponAttackPattern.Gunline),
                    A("ThePrince", "王子 / The Prince", IUMWWeaponAttackPattern.MagicCore)
                }),

            new(
                "风暴编织者",
                new Color(96, 232, 255),
                IUMWWeaponBossMovementStyle.Worm,
                new[] { "StormWeaverHead" },
                new[]
                {
                    A("SkytideDragoon", "天潮龙骑枪 / Skytide Dragoon", IUMWWeaponAttackPattern.Slash),
                    A("TheStorm", "风暴 / The Storm", IUMWWeaponAttackPattern.LightningChain),
                    A("Volterion", "伏特隆 / Volterion", IUMWWeaponAttackPattern.LightningChain),
                    A("AquasScepter", "碧水权杖 / Aqua's Scepter", IUMWWeaponAttackPattern.MagicCore),
                    A("CorinthPrime", "科林斯至尊 / Corinth Prime", IUMWWeaponAttackPattern.Gunline),
                    A("StellarTorusStaff", "星环法杖 / Stellar Torus Staff", IUMWWeaponAttackPattern.SummonCore),
                    A("Teslastaff", "特斯拉法杖 / Tesla Staff", IUMWWeaponAttackPattern.LightningChain),
                    A("TwistingThunder", "扭曲雷霆 / Twisting Thunder", IUMWWeaponAttackPattern.LightningChain),
                    A("ThePack", "群狼 / The Pack", IUMWWeaponAttackPattern.Gunline),
                    A("ShadowboltStaff", "暗影箭杖 / Shadowbolt Staff", IUMWWeaponAttackPattern.MagicCore),
                    A("Seadragon", "海龙 / Seadragon", IUMWWeaponAttackPattern.CreatureRush),
                    A("FourSeasonsGalaxia", "四季银河 / Four Seasons Galaxia", IUMWWeaponAttackPattern.StarField),
                    A("RealityRupture", "现实撕裂 / Reality Rupture", IUMWWeaponAttackPattern.SpaceRift)
                }),

            new(
                "无尽虚空",
                new Color(146, 86, 255),
                IUMWWeaponBossMovementStyle.VoidCore,
                new[] { "CeaselessVoid" },
                new[]
                {
                    A("MirrorBlade", "镜刃 / Mirror Blade", IUMWWeaponAttackPattern.SpaceRift),
                    A("VoidConcentrationStaff", "虚空凝聚杖 / Void Concentration Staff", IUMWWeaponAttackPattern.MagicCore),
                    A("DarkSpark", "暗黑火花 / Dark Spark", IUMWWeaponAttackPattern.BombRain),
                    A("EventHorizon", "事件视界 / Event Horizon", IUMWWeaponAttackPattern.SpaceRift),
                    A("Mistlestorm", "槲寄生风暴 / Mistlestorm", IUMWWeaponAttackPattern.StarField),
                    A("OntologicalDespoiler", "本体论亵渎者 / Ontological Despoiler", IUMWWeaponAttackPattern.Gunline),
                    A("SealedSingularity", "密封奇点 / Sealed Singularity", IUMWWeaponAttackPattern.SpaceRift),
                    A("TacticiansTrumpCard", "战术家的王牌 / Tactician's Trump Card", IUMWWeaponAttackPattern.SummonCore),
                    A("Eternity", "永恒 / Eternity", IUMWWeaponAttackPattern.MagicCore),
                    A("PhantasmalFury", "幻魂怒火 / Phantasmal Fury", IUMWWeaponAttackPattern.Gunline),
                    A("FourSeasonsGalaxia", "四季银河 / Four Seasons Galaxia", IUMWWeaponAttackPattern.StarField),
                    A("RealityRupture", "现实撕裂 / Reality Rupture", IUMWWeaponAttackPattern.SpaceRift)
                }),

            new(
                "西格纳斯",
                new Color(118, 86, 255),
                IUMWWeaponBossMovementStyle.Hover,
                new[] { "Signus" },
                new[]
                {
                    A("CosmicKunai", "宇宙苦无 / Cosmic Kunai", IUMWWeaponAttackPattern.ReturningBlade),
                    A("Cosmilamp", "宇宙灯 / Cosmilamp", IUMWWeaponAttackPattern.SummonCore),
                    A("AethersWhisper", "以太低语 / Aether's Whisper", IUMWWeaponAttackPattern.StarField),
                    A("DeathsAscension", "死亡升华 / Death's Ascension", IUMWWeaponAttackPattern.Slash),
                    A("EmpyreanKnives", "至天飞刀 / Empyrean Knives", IUMWWeaponAttackPattern.ReturningBlade),
                    A("KingofConstellationsTenryu", "星座之王天龙 / King of Constellations, Tenryu", IUMWWeaponAttackPattern.SummonCore),
                    A("MagneticMeltdown", "磁能熔毁 / Magnetic Meltdown", IUMWWeaponAttackPattern.LightningChain),
                    A("Nadir", "天底 / Nadir", IUMWWeaponAttackPattern.SpaceRift),
                    A("TheSevensStriker", "七发打击者 / The Sevens Striker", IUMWWeaponAttackPattern.Gunline),
                    A("VenusianTrident", "金星三叉戟 / Venusian Trident", IUMWWeaponAttackPattern.Slash),
                    A("FourSeasonsGalaxia", "四季银河 / Four Seasons Galaxia", IUMWWeaponAttackPattern.StarField),
                    A("RealityRupture", "现实撕裂 / Reality Rupture", IUMWWeaponAttackPattern.SpaceRift)
                }),

            new(
                "噬魂幽花",
                new Color(206, 86, 255),
                IUMWWeaponBossMovementStyle.Hover,
                new[] { "Polterghast" },
                new[]
                {
                    A("TerrorBlade", "惊惧之刃 / Terror Blade", IUMWWeaponAttackPattern.Slash),
                    A("BansheeHook", "女妖之钩 / Banshee Hook", IUMWWeaponAttackPattern.ReturningBlade),
                    A("DaemonsFlame", "魔鬼之焰 / Daemon's Flame", IUMWWeaponAttackPattern.BombRain),
                    A("FatesReveal", "命运揭示 / Fate's Reveal", IUMWWeaponAttackPattern.MagicCore),
                    A("GhastlyVisage", "幽魂面容 / Ghastly Visage", IUMWWeaponAttackPattern.SummonCore),
                    A("EtherealSubjugator", "虚灵支配者 / Ethereal Subjugator", IUMWWeaponAttackPattern.SummonCore),
                    A("GhoulishGouger", "食尸鬼钻掘者 / Ghoulish Gouger", IUMWWeaponAttackPattern.CreatureRush),
                    A("GalileoGladius", "伽利略短剑 / Galileo Gladius", IUMWWeaponAttackPattern.Slash),
                    A("CrescentMoon", "新月 / Crescent Moon", IUMWWeaponAttackPattern.ReturningBlade),
                    A("HalleysInferno", "哈雷地狱火 / Halley's Inferno", IUMWWeaponAttackPattern.BombRain),
                    A("AlphaDraconis", "右枢星 / Alpha Draconis", IUMWWeaponAttackPattern.StarField),
                    A("StratusSphere", "层云球 / Stratus Sphere", IUMWWeaponAttackPattern.MagicCore),
                    A("Sirius", "天狼星 / Sirius", IUMWWeaponAttackPattern.StarField),
                    A("WarloksMoonFist", "战月之拳 / Warloks' Moon Fist", IUMWWeaponAttackPattern.SummonCore),
                    A("Vega", "织女星 / Vega", IUMWWeaponAttackPattern.StarField)
                }),

            new(
                "渊海灾虫",
                new Color(68, 214, 180),
                IUMWWeaponBossMovementStyle.Worm,
                new[] { "AquaticScourgeHead" },
                new[]
                {
                    A("SubmarineShocker", "潜艇震击者 / Submarine Shocker", IUMWWeaponAttackPattern.LightningChain),
                    A("Barinautical", "巴利纳提卡 / Barinautical", IUMWWeaponAttackPattern.Gunline),
                    A("Downpour", "倾盆大雨 / Downpour", IUMWWeaponAttackPattern.AcidRain),
                    A("DeepseaStaff", "深海法杖 / Deepsea Staff", IUMWWeaponAttackPattern.MagicCore),
                    A("ScourgeoftheSeas", "海洋灾厄 / Scourge of the Seas", IUMWWeaponAttackPattern.CreatureRush),
                    A("FlakToxicannon", "毒性高射炮 / Flak Toxicannon", IUMWWeaponAttackPattern.Gunline),
                    A("SlitheringEels", "滑行电鳗 / Slithering Eels", IUMWWeaponAttackPattern.CreatureRush),
                    A("CausticCroakerStaff", "腐蚀蛙杖 / Caustic Croaker Staff", IUMWWeaponAttackPattern.SummonCore),
                    A("SkyfinBombers", "天鳍轰炸机 / Skyfin Bombers", IUMWWeaponAttackPattern.BombRain),
                    A("SpentFuelContainer", "废燃料容器 / Spent Fuel Container", IUMWWeaponAttackPattern.AcidRain),
                    A("SulphurousGrabber", "硫磺抓取器 / Sulphurous Grabber", IUMWWeaponAttackPattern.ReturningBlade)
                }),

            new(
                "硫海遗爵",
                new Color(176, 238, 80),
                IUMWWeaponBossMovementStyle.Hover,
                new[] { "OldDuke" },
                new[]
                {
                    A("InsidiousImpaler", "阴险穿刺者 / Insidious Impaler", IUMWWeaponAttackPattern.Slash),
                    A("FetidEmesis", "恶臭呕吐 / Fetid Emesis", IUMWWeaponAttackPattern.AcidRain),
                    A("SepticSkewer", "败血穿叉 / Septic Skewer", IUMWWeaponAttackPattern.Slash),
                    A("VitriolicViper", "硫酸毒蛇 / Vitriolic Viper", IUMWWeaponAttackPattern.CreatureRush),
                    A("MutatedTruffle", "变异松露 / Mutated Truffle", IUMWWeaponAttackPattern.CreatureRush),
                    A("CadaverousCarrion", "腐尸秃鹫 / Cadaverous Carrion", IUMWWeaponAttackPattern.CreatureRush),
                    A("ToxicantTwister", "毒素旋风 / Toxicant Twister", IUMWWeaponAttackPattern.AcidRain),
                    A("TheOldReaper", "老收割者 / The Old Reaper", IUMWWeaponAttackPattern.ReturningBlade),
                    A("SulphuricAcidCannon", "硫酸炮 / Sulphuric Acid Cannon", IUMWWeaponAttackPattern.Gunline),
                    A("GammaHeart", "伽马之心 / Gamma Heart", IUMWWeaponAttackPattern.AcidRain),
                    A("PhosphorescentGauntlet", "磷光拳套 / Phosphorescent Gauntlet", IUMWWeaponAttackPattern.Slash),
                    A("FlakToxicannon", "毒性高射炮 / Flak Toxicannon", IUMWWeaponAttackPattern.Gunline),
                    A("SlitheringEels", "滑行电鳗 / Slithering Eels", IUMWWeaponAttackPattern.CreatureRush),
                    A("SkyfinBombers", "天鳍轰炸机 / Skyfin Bombers", IUMWWeaponAttackPattern.BombRain),
                    A("SpentFuelContainer", "废燃料容器 / Spent Fuel Container", IUMWWeaponAttackPattern.AcidRain),
                    A("SulphurousGrabber", "硫磺抓取器 / Sulphurous Grabber", IUMWWeaponAttackPattern.ReturningBlade)
                })
        };

        private static IUMWWeaponBossAttack A(string itemName, string displayName, IUMWWeaponAttackPattern pattern = IUMWWeaponAttackPattern.Auto)
        {
            return new IUMWWeaponBossAttack(itemName, displayName, pattern);
        }
    }
}
