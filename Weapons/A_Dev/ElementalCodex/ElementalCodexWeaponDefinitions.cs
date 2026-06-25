using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    internal static class ElementalCodexWeaponDatabase
    {
        private static readonly Dictionary<int, ElementalCodexWeaponDefinition> definitionsByType = new();
        private static bool initialized;

        public static bool TryGetDefinition(int itemType, out ElementalCodexWeaponDefinition definition)
        {
            EnsureInitialized();
            return definitionsByType.TryGetValue(itemType, out definition);
        }

        public static void Unload()
        {
            definitionsByType.Clear();
            initialized = false;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            definitionsByType.Clear();
            RegisterDefinitions();
        }

        private static void Register(int type, string chineseName, string internalName, bool vanilla, params ElementalCodexElement[] elements)
        {
            if (definitionsByType.TryGetValue(type, out ElementalCodexWeaponDefinition existing))
            {
                string mergedChineseName = existing.ChineseName == chineseName ? existing.ChineseName : $"{existing.ChineseName}/{chineseName}";
                ElementalCodexElement[] mergedElements = existing.Elements.Concat(elements).Distinct().ToArray();
                definitionsByType[type] = new ElementalCodexWeaponDefinition(mergedChineseName, existing.InternalName, existing.Vanilla, mergedElements);
                return;
            }

            definitionsByType[type] = new ElementalCodexWeaponDefinition(chineseName, internalName, vanilla, elements);
        }

        private static void Vanilla(string chineseName, string internalName, params ElementalCodexElement[] elements)
        {
            if (ItemID.Search.TryGetId(internalName, out int type))
                Register(type, chineseName, internalName, true, elements);
        }

        private static void Calamity(string chineseName, string internalName, params ElementalCodexElement[] elements)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind(internalName, out ModItem item))
                Register(item.Type, chineseName, internalName, false, elements);
        }

        private static void Local(string chineseName, string internalName, params ElementalCodexElement[] elements)
        {
            if (ModLoader.TryGetMod("CalamityLegendsComeBack", out Mod local) &&
                local.TryFind(internalName, out ModItem item))
                Register(item.Type, chineseName, internalName, false, elements);
        }

        private static void MixedCalamity(string chineseName, string internalName)
        {
            Calamity(chineseName, internalName,
                ElementalCodexElement.Fire,
                ElementalCodexElement.Water,
                ElementalCodexElement.Ice,
                ElementalCodexElement.Lightning,
                ElementalCodexElement.Nature,
                ElementalCodexElement.Disease);
        }

        private static void RegisterDefinitions()
        {
            // 任意 Boss 前
            Vanilla("火花魔棒", "WandofSparking", ElementalCodexElement.Fire);
            Vanilla("红玉法杖", "RubyStaff", ElementalCodexElement.Fire);
            Vanilla("蓝玉法杖", "SapphireStaff", ElementalCodexElement.Water);
            Vanilla("逆转之风", "WeatherPain", ElementalCodexElement.Water, ElementalCodexElement.Ice);
            Vanilla("结霜魔杖", "WandofFrosting", ElementalCodexElement.Ice);
            Vanilla("钻石法杖", "DiamondStaff", ElementalCodexElement.Ice);
            Vanilla("冰坠法杖", "IceRod", ElementalCodexElement.Ice);
            Calamity("冰矢", "FrostBolt", ElementalCodexElement.Ice);
            Vanilla("霹雳法杖", "ThunderStaff", ElementalCodexElement.Lightning);
            Vanilla("紫晶法杖", "AmethystStaff", ElementalCodexElement.Lightning);
            Calamity("等离子射线", "PlasmaRod", ElementalCodexElement.Lightning);
            Calamity("钨钢义肢", "WulfrumProsthesis", ElementalCodexElement.Lightning);
            Vanilla("黄玉法杖", "TopazStaff", ElementalCodexElement.Nature);
            Vanilla("琥珀法杖", "AmberStaff", ElementalCodexElement.Nature);
            Vanilla("猩红魔杖", "CrimsonRod", ElementalCodexElement.Nature);
            Calamity("魔法玫瑰", "ManaRose", ElementalCodexElement.Nature);
            Vanilla("翡翠法杖", "EmeraldStaff", ElementalCodexElement.Disease);
            Vanilla("魔刺", "Vilethorn", ElementalCodexElement.Disease);
            Vanilla("恶魔镰刀", "DemonScythe", ElementalCodexElement.Disease);

            // 血肉宿主 / 腐巢意志前
            Calamity("狱火蝙蝠杖", "HellwingStaff", ElementalCodexElement.Fire);
            Calamity("火矢", "FlareBolt", ElementalCodexElement.Fire);
            Calamity("火山锅", "TheCauldron", ElementalCodexElement.Fire);
            Calamity("海水杖", "Waywasher", ElementalCodexElement.Water);
            Calamity("清道夫", "Keelhaul", ElementalCodexElement.Water);
            Calamity("珊瑚泥", "CoralSpout", ElementalCodexElement.Water);
            Calamity("闪光皇后鱼", "GleamingMagnolia", ElementalCodexElement.Ice);
            Vanilla("灰冲击枪", "ZapinatorGray", ElementalCodexElement.Lightning);
            Vanilla("太空枪", "SpaceGun", ElementalCodexElement.Lightning);
            Calamity("流沙法杖", "SandstreamScepter", ElementalCodexElement.Nature);
            Calamity("菌杖", "HyphaeRod", ElementalCodexElement.Nature);
            Calamity("天空之羽", "SkyGlaze", ElementalCodexElement.Nature);
            Calamity("寄生权杖", "ParasiticSceptor", ElementalCodexElement.Disease);
            Calamity("酸液枪", "AcidGun", ElementalCodexElement.Disease);

            // 骷髅王前
            Calamity("脉冲手枪", "PulsePistol", ElementalCodexElement.Lightning);
            Vanilla("蜜蜂枪", "BeeGun", ElementalCodexElement.Nature);
            Calamity("信风", "Tradewinds", ElementalCodexElement.Nature);
            Calamity("腐蚀之雨", "ShaderainStaff", ElementalCodexElement.Disease);
            Calamity("血浴", "BloodBath", ElementalCodexElement.Disease);

            // 肉山前
            Calamity("诡触之书", "EldritchTome", ElementalCodexElement.Fire);
            Vanilla("火之花", "FlowerofFire", ElementalCodexElement.Fire);
            Vanilla("烈焰火鞭", "Flamelash", ElementalCodexElement.Fire);
            Calamity("深渊之书", "AbyssalTome", ElementalCodexElement.Water);
            Calamity("海蓝法杖", "AquamarineStaff", ElementalCodexElement.Water);
            Vanilla("水矢", "WaterBolt", ElementalCodexElement.Water);
            Vanilla("魔法导弹", "MagicMissile", ElementalCodexElement.Ice);
            Calamity("深渊电击者", "AbyssShocker", ElementalCodexElement.Lightning);
            Calamity("天候棒", "TheWand", ElementalCodexElement.Nature);
            Calamity("黑尿鱼", "BlackAnurian", ElementalCodexElement.Nature);
            Calamity("永夜射线", "NightsRay", ElementalCodexElement.Disease);
            Vanilla("骷髅头法书", "BookofSkulls", ElementalCodexElement.Disease);

            // 肉山后未击杀 Boss
            Vanilla("诅咒焰", "CursedFlames", ElementalCodexElement.Fire);
            Vanilla("爬藤怪法杖", "ClingerStaff", ElementalCodexElement.Fire);
            Calamity("冰火矢", "FrigidflashBolt", ElementalCodexElement.Fire, ElementalCodexElement.Ice);
            Calamity("裁缝之怒", "ClothiersWrath", ElementalCodexElement.Fire);
            Vanilla("雨云魔杖", "NimbusRod", ElementalCodexElement.Water);
            Calamity("波塞冬", "Poseidon", ElementalCodexElement.Water);
            Calamity("海蟒咒书", "Serpentine", ElementalCodexElement.Water);
            Vanilla("裂天剑", "SkyFracture", ElementalCodexElement.Ice);
            Vanilla("寒霜之花", "FlowerofFrost", ElementalCodexElement.Ice);
            Vanilla("寒霜法杖", "FrostStaff", ElementalCodexElement.Ice);
            Vanilla("水晶风暴", "CrystalStorm", ElementalCodexElement.Ice);
            Calamity("冰雪魔杖", "SnowstormStaff", ElementalCodexElement.Ice);
            Vanilla("流星法杖", "MeteorStaff", ElementalCodexElement.Lightning);
            Vanilla("激光步枪", "LaserRifle", ElementalCodexElement.Lightning);
            Vanilla("橙冲击枪", "ZapinatorOrange", ElementalCodexElement.Lightning);
            Calamity("SHPC", "SHPC", ElementalCodexElement.Lightning);
            Local("SHPC（传奇）", "NewLegendSHPC", ElementalCodexElement.Lightning);
            Vanilla("水晶蛇", "CrystalSerpent", ElementalCodexElement.Nature);
            Vanilla("魔晶碎块", "CrystalVileShard", ElementalCodexElement.Nature);
            Vanilla("血荆棘", "BloodThorn", ElementalCodexElement.Nature);
            Calamity("荣耀尽头", "GloriousEnd", ElementalCodexElement.Nature);
            Calamity("飞龙之歌", "WyvernsCall", ElementalCodexElement.Nature);
            Vanilla("夺命杖", "LifeDrain", ElementalCodexElement.Disease);
            Vanilla("剧毒法杖", "PoisonStaff", ElementalCodexElement.Disease);
            Vanilla("黄金雨", "GoldenShower", ElementalCodexElement.Disease);
            Vanilla("蛇发女妖头", "MedusaHead", ElementalCodexElement.Disease);
            Vanilla("暗影焰妖娃", "ShadowFlameHexDoll", ElementalCodexElement.Disease);

            // 世纪之花 / 灾厄之影前
            Vanilla("邪恶三叉戟", "UnholyTrident", ElementalCodexElement.Fire);
            Vanilla("神灯烈焰", "SpiritFlame", ElementalCodexElement.Fire);
            Calamity("硫火玫瑰", "BrimroseStaff", ElementalCodexElement.Fire);
            Calamity("离子冲击波", "IonBlaster", ElementalCodexElement.Fire);
            Calamity("蒸海硫火", "BurningSea", ElementalCodexElement.Fire);
            Calamity("沸腾之火", "SeethingDischarge", ElementalCodexElement.Fire);
            Calamity("水灌", "Downpour", ElementalCodexElement.Water);
            Calamity("冰晶风暴", "ShadecrystalBarrage", ElementalCodexElement.Ice);
            Calamity("冰锥三叉戟", "IcicleTrident", ElementalCodexElement.Ice);
            Calamity("恐寒症", "Cryophobia", ElementalCodexElement.Ice);
            Calamity("暗晶风暴", "ShadecrystalBarrage", ElementalCodexElement.Ice);
            Calamity("北极熊爪", "ArcticBearPaw", ElementalCodexElement.Ice);
            Calamity("女武神射线", "ValkyrieRay", ElementalCodexElement.Lightning);
            Calamity("高斯手枪", "GaussPistol", ElementalCodexElement.Lightning);
            Vanilla("彩虹魔杖", "RainbowRod", ElementalCodexElement.Nature);
            Vanilla("无限智慧巨著", "BookStaff", ElementalCodexElement.Nature);
            Vanilla("魔法竖琴", "MagicalHarp", ElementalCodexElement.Nature);
            Calamity("烁兰", "ArchAmaryllis", ElementalCodexElement.Nature);
            Calamity("遗迹圣物", "RelicofRuin", ElementalCodexElement.Nature);
            Vanilla("毒液法杖", "VenomStaff", ElementalCodexElement.Disease);
            Calamity("瘴气", "Miasma", ElementalCodexElement.Disease);
            Calamity("死亡之尘", "DeathValleyDuster", ElementalCodexElement.Disease);
            Calamity("蜿蜒酸鳗", "SlitheringEels", ElementalCodexElement.Disease);
            Calamity("喷嗝萨克斯", "BelchingSaxophone", ElementalCodexElement.Disease);

            // 石巨人前
            Vanilla("狱火叉", "InfernoFork", ElementalCodexElement.Fire);
            Calamity("混乱火鞭", "LashesofChaos", ElementalCodexElement.Fire);
            Calamity("亚特兰蒂斯", "Atlantis", ElementalCodexElement.Water);
            Calamity("深渊女神之复仇", "UndinesRetribution", ElementalCodexElement.Water);
            Calamity("斥责法杖", "Effervescence", ElementalCodexElement.Water);
            Calamity("天堂之泪", "TearsofHeaven", ElementalCodexElement.Water);
            Calamity("阿娜希塔琶音", "AnahitasArpeggio", ElementalCodexElement.Water);
            Vanilla("暴雪法杖", "BlizzardStaff", ElementalCodexElement.Ice);
            Calamity("凛冬之怒", "WintersFury", ElementalCodexElement.Ice);
            Vanilla("暗影束法杖", "ShadowbeamStaff", ElementalCodexElement.Lightning);
            Vanilla("彩虹枪", "RainbowGun", ElementalCodexElement.Lightning);
            Vanilla("磁球", "MagnetSphere", ElementalCodexElement.Lightning);
            Calamity("南极光羽", "AlulaAustralis", ElementalCodexElement.Lightning);
            Calamity("远古之怒", "PrimordialAncient", ElementalCodexElement.Lightning);
            Vanilla("爆裂藤蔓", "NettleBurst", ElementalCodexElement.Nature);
            Vanilla("剃刀松", "Razorpine", ElementalCodexElement.Nature);
            Vanilla("蝙蝠权杖", "BatScepter", ElementalCodexElement.Nature);
            Vanilla("共鸣权杖", "OcularResonance", ElementalCodexElement.Nature);
            Vanilla("胡蜂枪", "WaspGun", ElementalCodexElement.Nature);
            Vanilla("吹叶机", "LeafBlower", ElementalCodexElement.Nature);
            Calamity("光合射线", "Photosynthesis", ElementalCodexElement.Nature);
            Calamity("移沙接土", "ShiftingSands", ElementalCodexElement.Nature);
            Vanilla("毒气瓶", "ToxicFlask", ElementalCodexElement.Disease);
            Calamity("星幻蛛法杖", "AstralachneaStaff", ElementalCodexElement.Disease);
            Calamity("常青之气", "EvergladeSpray", ElementalCodexElement.Disease);
            Calamity("始源之尘", "PrimordialEarth", ElementalCodexElement.Disease);
            Calamity("奈落瓮", "HadalUrn", ElementalCodexElement.Disease);

            // 教徒前
            Calamity("双足翼龙怒气", "AlphaDraconis", ElementalCodexElement.Fire);
            Vanilla("高温射线枪", "HeatRay", ElementalCodexElement.Fire);
            Calamity("禁忌之阳", "ForbiddenSun", ElementalCodexElement.Fire);
            Calamity("狱炎裂空", "InfernalRift", ElementalCodexElement.Fire);
            Calamity("维苏威阿斯", "Vesuvius", ElementalCodexElement.Fire);
            Local("维苏威阿斯（传奇）", "NewVesuvius", ElementalCodexElement.Fire);
            Vanilla("泡泡枪", "BubbleGun", ElementalCodexElement.Water);
            Vanilla("利刃台风", "RazorbladeTyphoon", ElementalCodexElement.Water);
            Vanilla("激光机枪", "LaserMachinegun", ElementalCodexElement.Lightning);
            Vanilla("充能爆破炮", "ChargedBlasterCannon", ElementalCodexElement.Lightning);
            Vanilla("星星吉他", "SparkleGuitar", ElementalCodexElement.Lightning);
            Calamity("侧翼", "Wingman", ElementalCodexElement.Lightning);
            Vanilla("激光加特林", "LaserMachinegun", ElementalCodexElement.Lightning);
            Vanilla("大地法杖", "StaffofEarth", ElementalCodexElement.Nature);
            Vanilla("夜光", "FairyQueenMagicItem", ElementalCodexElement.Nature);
            Calamity("血涌", "Hematemesis", ElementalCodexElement.Nature);
            Calamity("瘟疫法杖", "PlagueStaff", ElementalCodexElement.Disease);

            // 月总前
            Vanilla("星云烈焰", "NebulaBlaze", ElementalCodexElement.Fire);
            Local("拉扎尔射线", "Lazhar", ElementalCodexElement.Fire);
            Calamity("命运之手", "FatesReveal", ElementalCodexElement.Ice);
            Calamity("幻星法杖", "AstralStaff", ElementalCodexElement.Ice);
            Vanilla("星云奥秘", "NebulaArcanum", ElementalCodexElement.Lightning);
            Calamity("流星雨", "StarShower", ElementalCodexElement.Lightning);
            Calamity("宙虹", "CosmicRainbow", ElementalCodexElement.Nature);
            Calamity("蜂群", "TheSwarmer", ElementalCodexElement.Nature);
            Calamity("玄法百枝莲", "NuclearFury", ElementalCodexElement.Nature);
            Calamity("纳米净化", "NanoPurge", ElementalCodexElement.Disease);

            // 亵渎天神前
            Calamity("源", "Genesis", ElementalCodexElement.Fire);
            Calamity("凋亡射线", "ApoctosisArray", ElementalCodexElement.Fire);
            MixedCalamity("元素射线", "ElementalRay");
            MixedCalamity("炼金狂人的手套", "MadAlchemistsCocktailGlove");
            Calamity("泡沫冲锋枪", "Effervescence", ElementalCodexElement.Water);
            Calamity("原核之怒", "NuclearFury", ElementalCodexElement.Water);
            Vanilla("月耀", "LunarFlareBook", ElementalCodexElement.Ice);
            Calamity("时空术士镰刀", "ChronomancersScythe", ElementalCodexElement.Ice);
            Calamity("行星法杖", "AstralStaff", ElementalCodexElement.Ice);
            Vanilla("终极棱镜", "LastPrism", ElementalCodexElement.Lightning);
            Calamity("殷红鞭笞", "RougeSlash", ElementalCodexElement.Lightning);
            Calamity("先兆元素", "PrimordialAncient", ElementalCodexElement.Nature);
            Calamity("终结裂空戟", "Eternity", ElementalCodexElement.Disease);

            // 噬魂幽花前
            Calamity("神圣天罚", "Vehemence", ElementalCodexElement.Fire);
            Calamity("王子", "ThePrince", ElementalCodexElement.Fire);
            Calamity("净化激光炮", "UltraLiquidator", ElementalCodexElement.Fire);
            Calamity("猩红烈焰", "SanguineFlare", ElementalCodexElement.Water);
            Calamity("以太之低语", "AethersWhisper", ElementalCodexElement.Ice);
            Calamity("磁场之融", "MagneticMeltdown", ElementalCodexElement.Lightning);
            Calamity("等离子液铸器", "PlasmaCaster", ElementalCodexElement.Lightning);
            Calamity("雷暴雨", "Mistlestorm", ElementalCodexElement.Lightning);
            Calamity("特斯拉杖", "Teslastaff", ElementalCodexElement.Lightning);
            Calamity("战术家的王牌", "TacticiansTrumpCard", ElementalCodexElement.Lightning);
            Calamity("荆棘烁兰", "ThornBlossom", ElementalCodexElement.Nature);
            Calamity("心泵血杖", "Hematemesis", ElementalCodexElement.Nature);
            Calamity("槲叶暴风", "Mistlestorm", ElementalCodexElement.Nature);
            Calamity("生命光流", "Photosynthesis", ElementalCodexElement.Nature);
            Calamity("等离子步枪", "PlasmaRifle", ElementalCodexElement.Disease);

            // 神明吞噬者前
            Calamity("命运神启", "FatesReveal", ElementalCodexElement.Fire);
            Calamity("金星三叉戟", "VenusianTrident", ElementalCodexElement.Fire);
            Calamity("鬼之形", "GhastlyVisage", ElementalCodexElement.Fire);
            Calamity("幻妖龙吟", "EidolicWail", ElementalCodexElement.Water);
            Calamity("冰幻法杖", "EidolonStaff", ElementalCodexElement.Ice);
            Calamity("引流法杖", "AetherfluxCannon", ElementalCodexElement.Lightning);
            Calamity("寂虚之光", "VividClarity", ElementalCodexElement.Lightning);
            Calamity("幻象之怒", "PhantasmalFury", ElementalCodexElement.Nature);
            Calamity("星空破龙杖", "AlphaDraconis", ElementalCodexElement.Nature);
            Calamity("酸蚀毒蝰", "VitriolicViper", ElementalCodexElement.Disease);

            // 犽戎前
            Calamity("星云灾变", "NebulousCataclysm", ElementalCodexElement.Fire);
            Calamity("狂野复诵", "RecitationoftheBeast", ElementalCodexElement.Fire);
            Calamity("黑洞边缘", "EventHorizon", ElementalCodexElement.Fire);
            Calamity("变脸熔炉", "FaceMelter", ElementalCodexElement.Fire);
            Calamity("圣神光辉", "LightGodsBrilliance", ElementalCodexElement.Water);
            Calamity("寒冰弹幕", "IceBarrage", ElementalCodexElement.Ice);
            Calamity("始源之遗", "PrimordialAncient", ElementalCodexElement.Ice);
            Calamity("死亡冰雹", "DeathHailStaff", ElementalCodexElement.Lightning);
            Calamity("极点光伏", "VoltaicClimax", ElementalCodexElement.Lightning);
            Calamity("奥密克戎", "Omicron", ElementalCodexElement.Lightning);
            Calamity("特斯拉巨炮", "TeslaCannon", ElementalCodexElement.Lightning);
            Calamity("灵魂穿透者", "SoulPiercer", ElementalCodexElement.Disease);

            // 犽戎后
            Calamity("星火凤凰雨", "PhoenixFlameBarrage", ElementalCodexElement.Fire);
            Calamity("神杖·暴风灼炎", "StaffofBlushie", ElementalCodexElement.Fire);
            Calamity("氦闪", "HeliumFlash", ElementalCodexElement.Fire);
            Calamity("虚空漩涡", "VoidVortex", ElementalCodexElement.Lightning);
            Calamity("以太流光炮", "AetherfluxCannon", ElementalCodexElement.Lightning);
            Calamity("亚利姆水晶", "YharimsCrystal", ElementalCodexElement.Lightning);
            Local("亚利姆水晶（传奇）", "NewLegendYharimsCrystal", ElementalCodexElement.Lightning);

            // 星流巨械 / 至尊灾厄其中一个后
            Calamity("狞桀", "Rancor", ElementalCodexElement.Fire);
            Calamity("归元漩涡", "SubsumingVortex", ElementalCodexElement.Fire);
            Calamity("怨戾", "GruesomeEminence", ElementalCodexElement.Fire);
            Calamity("耀界之光", "TheDanceofLight", ElementalCodexElement.Lightning);
            Calamity("异端", "Heresy", ElementalCodexElement.Disease);
            Calamity("异端僭越", "Apathanull", ElementalCodexElement.Disease);
        }
    }

    internal sealed class ElementalCodexDatabaseSystem : ModSystem
    {
        public override void Unload()
        {
            ElementalCodexWeaponDatabase.Unload();
        }
    }
}
