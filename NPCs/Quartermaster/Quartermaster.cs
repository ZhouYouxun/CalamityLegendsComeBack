using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.NPCs.Quartermaster;

[AutoloadHead]
public sealed class Quartermaster : ModNPC
{
    public override void SetStaticDefaults()
    {
        // Supplied image: 40 x 1400, 25 frames of 40 x 56.
        Main.npcFrameCount[Type] = 25;
        NPCID.Sets.ExtraFramesCount[Type] = 9;
        NPCID.Sets.AttackFrameCount[Type] = 4;
        NPCID.Sets.DangerDetectRange[Type] = 600;
        NPCID.Sets.AttackType[Type] = 0;
        NPCID.Sets.AttackTime[Type] = 90;
        NPCID.Sets.AttackAverageChance[Type] = 30;
        NPCID.Sets.HatOffsetY[Type] = 4;
        NPC.Happiness
            .SetBiomeAffection<ForestBiome>(AffectionLevel.Like)
            .SetBiomeAffection<DesertBiome>(AffectionLevel.Dislike)
            .SetNPCAffection(NPCID.Mechanic, AffectionLevel.Like)
            .SetNPCAffection(NPCID.Merchant, AffectionLevel.Like)
            .SetNPCAffection(NPCID.Demolitionist, AffectionLevel.Dislike)
            .SetNPCAffection(NPCID.Angler, AffectionLevel.Dislike);
    }

    public override void SetDefaults()
    {
        NPC.townNPC = true;
        NPC.friendly = true;
        NPC.width = 18;
        NPC.height = 40;
        NPC.aiStyle = NPCAIStyleID.Passive;
        NPC.damage = 10;
        NPC.defense = 25;
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.5f;
        AnimationType = NPCID.Guide;
    }

    public override bool CanTownNPCSpawn(int numTownNPCs) => true;

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // The supplied sheet is mirrored relative to a vanilla town NPC sheet.
        // Swap the left and right draw choices immediately before vanilla draws it.
        NPC.spriteDirection = -NPC.direction;
        return true;
    }

    public override List<string> SetNPCNameList() => new()
    {
        this.GetLocalizedValue("Name.Erin"),
        this.GetLocalizedValue("Name.Vera"),
        this.GetLocalizedValue("Name.Mira"),
        this.GetLocalizedValue("Name.Nora"),
        this.GetLocalizedValue("Name.Lena"),
        this.GetLocalizedValue("Name.Celine")
    };

    public override void AI()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient &&
            (NPC.GivenName == "物资官" || NPC.GivenName == "Quartermaster"))
        {
            var names = SetNPCNameList();
            NPC.GivenName = names[Main.rand.Next(names.Count)];
            NPC.netUpdate = true;
        }
    }
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface);
        bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.CalamityLegendsComeBack.Quartermaster.Bestiary"));
    }

    private static QuartermasterShopPlayer ShopPlayer => Main.LocalPlayer.GetModPlayer<QuartermasterShopPlayer>();
    private static QuartermasterStock.Page CurrentPage => QuartermasterStock.Pages[ShopPlayer.Page % QuartermasterStock.Pages.Count];
    internal static string CurrentShopName => CurrentPage.Name;
    internal static string PageTitle
    {
        get
        {
            var page = CurrentPage;
            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.Quartermaster.Categories." + page.Category);
            return page.Count == 1 ? title : $"{title} ({page.Number}/{page.Count})";
        }
    }

    public override string GetChat()
    {
        return Language.GetTextValue("Mods.CalamityLegendsComeBack.Quartermaster.Chat", PageTitle);
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = PageTitle;
        button2 = Language.GetTextValue("Mods.CalamityLegendsComeBack.Quartermaster.NextPage");
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        if (firstButton)
        {
            shopName = CurrentPage.Name;
            return;
        }

        SelectNextPage();
    }

    internal static void SelectNextPage()
    {
        ShopPlayer.Page = (ShopPlayer.Page + 1) % QuartermasterStock.Pages.Count;
        Main.npcChatText = Language.GetTextValue("Mods.CalamityLegendsComeBack.Quartermaster.Chat", PageTitle);
    }

    public override void AddShops() => QuartermasterStock.Register(Type);
    public override void Unload() => QuartermasterStock.Pages.Clear();
    public override void TownNPCAttackStrength(ref int damage, ref float knockback) { damage = 20; knockback = 4f; }
    public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) { cooldown = 30; randExtraCooldown = 30; }
    public override void TownNPCAttackProj(ref int projType, ref int attackDelay) { projType = ProjectileID.ThrowingKnife; attackDelay = 1; }
    public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
    { multiplier = 10f; gravityCorrection = 0.1f; randomOffset = 1f; }
}
