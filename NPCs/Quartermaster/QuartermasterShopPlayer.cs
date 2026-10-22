using CalamityMod;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.NPCs.Quartermaster;

// Shop page and exploration history belong to the character, not the shared NPC/world.
public sealed class QuartermasterShopPlayer : ModPlayer
{
    internal int Page;
    internal bool VisitedUnderworld;
    internal bool VisitedJungle;
    internal bool VisitedAnySea;
    internal bool VisitedCaverns;
    internal bool VisitedSunkenSea;
    internal bool VisitedAstralInHardmode;

    public override void Initialize() => Page = 0;
    public override void OnEnterWorld() => Page = 0;

    public override void PostUpdate()
    {
        if (Player.dead)
            return;

        var calamity = Player.Calamity();
        VisitedUnderworld |= Player.ZoneUnderworldHeight;
        VisitedJungle |= Player.ZoneJungle;
        VisitedAnySea |= Player.ZoneBeach || calamity.ZoneSulphur || calamity.ZoneAbyss;
        VisitedCaverns |= Player.ZoneRockLayerHeight;
        VisitedSunkenSea |= calamity.ZoneSunkenSea;
        VisitedAstralInHardmode |= Main.hardMode && calamity.ZoneAstral;
    }

    public override void SaveData(TagCompound tag)
    {
        if (VisitedUnderworld) tag[nameof(VisitedUnderworld)] = true;
        if (VisitedJungle) tag[nameof(VisitedJungle)] = true;
        if (VisitedAnySea) tag[nameof(VisitedAnySea)] = true;
        if (VisitedCaverns) tag[nameof(VisitedCaverns)] = true;
        if (VisitedSunkenSea) tag[nameof(VisitedSunkenSea)] = true;
        if (VisitedAstralInHardmode) tag[nameof(VisitedAstralInHardmode)] = true;
    }

    public override void LoadData(TagCompound tag)
    {
        VisitedUnderworld = tag.GetBool(nameof(VisitedUnderworld));
        VisitedJungle = tag.GetBool(nameof(VisitedJungle));
        VisitedAnySea = tag.GetBool(nameof(VisitedAnySea));
        VisitedCaverns = tag.GetBool(nameof(VisitedCaverns));
        VisitedSunkenSea = tag.GetBool(nameof(VisitedSunkenSea));
        VisitedAstralInHardmode = tag.GetBool(nameof(VisitedAstralInHardmode));
    }
}
