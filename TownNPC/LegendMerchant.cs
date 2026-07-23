using System.Collections.Generic;
using CalamityMod;
// 传奇武器（第1商店）
using CalamityLegendsComeBack.Weapons.AegisBlade;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.CosmicDischarge;
using CalamityLegendsComeBack.Weapons.GaelsGreatsword;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor;
using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityLegendsComeBack.Weapons.PristineFury;
using CalamityLegendsComeBack.Weapons.SeasSearing;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.Vesuvius;
using CalamityLegendsComeBack.Weapons.YharimsCrystal;
// BOSS召唤物（第2商店）
using CalamityMod.Items.SummonItems;
// 密码破译机相关（第3商店）
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items.Placeables.DraedonStructures;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.TownNPC
{
    // 临时性城镇NPC：直接借用灾厄“盗贼(Bandit)”的贴图与小地图头像。
    // 三个商店：传奇武器 / BOSS召唤物 / 密码破译机套件。
    public class LegendMerchant : ModNPC
    {
        // 借用灾厄盗贼的主贴图；[AutoloadHead] 会自动请求同路径的 _Head 小地图头像。
        public override string Texture => "CalamityMod/NPCs/TownNPCs/Bandit";

        // 商店内部名
        private const string WeaponShopName = "LegendWeapons";
        private const string SummonShopName = "BossSummons";
        private const string DraedonShopName = "Draedon";

        // 当前正在浏览的商店页（0=武器 1=召唤物 2=破译机）。单个商人足够用静态字段。
        private static int shopPage;

        public override void SetStaticDefaults()
        {
            // 盗贼的贴图为 23 帧，沿用派对女孩的帧切换逻辑。
            Main.npcFrameCount[Type] = 23;
            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 500;
            NPCID.Sets.AttackType[Type] = 0;
            NPCID.Sets.AttackTime[Type] = 60;
            NPCID.Sets.AttackAverageChance[Type] = 10;
            NPCID.Sets.ShimmerTownTransform[Type] = false;

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = 1f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 44;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.PartyGirl;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("Mods.CalamityLegendsComeBack.NPCs.LegendMerchant.Bestiary")
            });
        }

        // 有可用房屋即可入住（开局就能买到传奇武器）。
        public override bool CanTownNPCSpawn(int numTownNPCs) => true;

        public override List<string> SetNPCNameList() => new List<string>()
        {
            this.GetLocalizedValue("Name.Name1"),
            this.GetLocalizedValue("Name.Name2"),
            this.GetLocalizedValue("Name.Name3"),
            this.GetLocalizedValue("Name.Name4"),
        };

        public override string GetChat()
        {
            switch (Main.rand.Next(4))
            {
                case 0:
                    return this.GetLocalizedValue("Chat.Normal1");
                case 1:
                    return this.GetLocalizedValue("Chat.Normal2");
                case 2:
                    return this.GetLocalizedValue("Chat.Normal3");
                default:
                    return this.GetLocalizedValue("Chat.Normal4");
            }
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            // 第一个按钮：打开当前页商店，标签随页码变化。
            button = shopPage switch
            {
                1 => this.GetLocalizedValue("ShopButton.BossSummons"),
                2 => this.GetLocalizedValue("ShopButton.Draedon"),
                _ => this.GetLocalizedValue("ShopButton.LegendWeapons"),
            };
            // 第二个按钮：切换到下一页。
            button2 = this.GetLocalizedValue("SwitchShopButton");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = shopPage switch
                {
                    1 => SummonShopName,
                    2 => DraedonShopName,
                    _ => WeaponShopName,
                };
            }
            else
            {
                shopPage = (shopPage + 1) % 3;
                Main.npcChatText = shopPage switch
                {
                    1 => this.GetLocalizedValue("SwitchChat.BossSummons"),
                    2 => this.GetLocalizedValue("SwitchChat.Draedon"),
                    _ => this.GetLocalizedValue("SwitchChat.LegendWeapons"),
                };
            }
        }

        public override void AddShops()
        {
            // —— 第1商店：全部传奇武器，开局即可购买 ——
            new NPCShop(Type, WeaponShopName)
                .Add<AegisBlade>()
                .Add<NewLegendBlossomFlux>()
                .Add<NewLegendBrinyBaron>()
                .Add<NewLegendCosmicDischarge>()
                .Add<NewLegendGaelsGreatsword>()
                .Add<GlacialEmbrace>()
                .Add<LeonidProgenitor>()
                .Add<Malachite>()
                .Add<NewLegendPristineFury>()
                .Add<SeasSearing>()
                .Add<NewLegendSHPC>()
                .Add<NewVesuvius>()
                .Add<NewLegendYharimsCrystal>()
                .Register();

            // —— 第2商店：BOSS召唤物，随进度逐步解锁 ——
            new NPCShop(Type, SummonShopName)
                // 前期
                .Add<DesertMedallion>()                                              // 沙漠灾虫（开局）
                .Add<DecapoditaSprout>(CalamityConditions.DownedDesertScourge)       // 蟹钳菇
                .Add<Teratoma>(CalamityConditions.DownedDesertScourge)               // 畸变体（腐化：脑残兽）
                .Add<BloodyWormFood>(CalamityConditions.DownedDesertScourge)         // 血肉虫食（猩红：钻探者）
                .Add<OverloadedSludge>(CalamityConditions.DownedHiveMindOrPerforator)// 超载软泥（史莱姆之神）
                // 硬模式
                .Add<CryoKey>(Condition.Hardmode)                                    // 寒霜之钥（低温之源）
                .Add<Seafood>(Condition.Hardmode)                                    // 海鲜（水生灾虫）
                .Add<CharredIdol>(CalamityConditions.DownedAquaticScourge)           // 焦炭雕像（硫磺火元素）
                .Add<NaiadsWarhorn>(CalamityConditions.DownedBrimstoneElemental)     // 涅伊阿德斯战号（利维坦与阿娜希塔）
                .Add<SandstormsCore>(Condition.Hardmode)                             // 沙暴之核（巨型沙鲨）
                // 机械BOSS后
                .Add<AstralChunk>(Condition.DownedMechBossAny)                       // 星辉碎块（星流土黄）
                .Add<Starcore>(CalamityConditions.DownedAstrumAureus)               // 星核（星神之龙）
                .Add<EyeofDesolation>(Condition.DownedMechBossAll)                   // 荒芜之眼（灾厄之影）
                // 世纪之花后
                .Add<Abombination>(Condition.DownedPlantera)                         // 憎恶体（瘟疫使者歌利亚）
                .Add<DeathWhistle>(Condition.DownedPlantera)                         // 死亡之笛（毁灭者）
                .Add<ExoticPheromones>(Condition.DownedPlantera)                     // 异域信息素（叛龙）
                // 神明后
                .Add<ProfanedShard>(Condition.DownedGolem)                           // 亵渎碎片（亵渎守卫）
                .Add<ProfanedCore>(CalamityConditions.DownedGuardians)               // 亵渎之核（神明）
                .Add<MarkofProvidence>(CalamityConditions.DownedProvidence)          // 神迹印记（哨兵三体）
                .Add<NecroplasmicBeacon>(Condition.DownedMoonLord)                   // 死灵质信标（幽灵）
                .Add<CosmicWorm>(CalamityConditions.DownedPolterghast)               // 宇宙之虫（神明吞噬者）
                .Add<YharonEgg>(CalamityConditions.DownedDevourerOfGods)             // 尧龙之卵（尧龙）
                // 终局
                .Add<Terminus>(CalamityConditions.DownedYharon)                      // 终结（BOSS快速战）
                .Register();

            // —— 第3商店：密码破译机基站 + 消耗物 + 关键家具 ——
            new NPCShop(Type, DraedonShopName)
                .Add<CodebreakerBase>()          // 密码破译机基站
                .Add<DraedonPowerCell>()         // 供能电池（消耗物）
                .Add<DecryptionComputer>()       // 解密电脑（关键家具1）
                .Add<PowerCellFactoryItem>()     // 电池工厂（关键家具2）
                .Register();
        }
    }
}
