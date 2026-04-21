using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.B_PreOther;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects
{
    public class LeonidMetalEffectLoader : ModSystem
    {
        public override void PostSetupContent()
        {
            LeonidMetalEffectRegistry.Register(new CopperEffect());
            LeonidMetalEffectRegistry.Register(new TinEffect());
            LeonidMetalEffectRegistry.Register(new IronEffect());
            LeonidMetalEffectRegistry.Register(new LeadEffect());
            LeonidMetalEffectRegistry.Register(new SilverEffect());
            LeonidMetalEffectRegistry.Register(new TungstenEffect());
            LeonidMetalEffectRegistry.Register(new GoldEffect());
            LeonidMetalEffectRegistry.Register(new PlatinumEffect());

            LeonidMetalEffectRegistry.Register(new DemoniteEffect());
            LeonidMetalEffectRegistry.Register(new CrimtaneEffect());
            LeonidMetalEffectRegistry.Register(new AerialiteEffect());
            LeonidMetalEffectRegistry.Register(new MeteoriteEffect());
            LeonidMetalEffectRegistry.Register(new HellstoneEffect());

            LeonidMetalEffectRegistry.Register(new CobaltEffect());
            LeonidMetalEffectRegistry.Register(new PalladiumEffect());
            LeonidMetalEffectRegistry.Register(new MythrilEffect());
            LeonidMetalEffectRegistry.Register(new OrichalcumEffect());
            LeonidMetalEffectRegistry.Register(new AdamantiteEffect());
            LeonidMetalEffectRegistry.Register(new TitaniumEffect());

            LeonidMetalEffectRegistry.Register(new CryonicEffect());
            LeonidMetalEffectRegistry.Register(new HallowedEffect());
            LeonidMetalEffectRegistry.Register(new ChlorophyteEffect());
            LeonidMetalEffectRegistry.Register(new PerennialEffect());
            LeonidMetalEffectRegistry.Register(new ShroomiteEffect());
            LeonidMetalEffectRegistry.Register(new SpectreEffect());
            LeonidMetalEffectRegistry.Register(new ScoriaEffect());
            LeonidMetalEffectRegistry.Register(new LunarEffect());

            LeonidMetalEffectRegistry.Register(new AstralEffect());
            LeonidMetalEffectRegistry.Register(new UelibloomEffect());
            LeonidMetalEffectRegistry.Register(new CosmiliteEffect());
            LeonidMetalEffectRegistry.Register(new AuricEffect());
            LeonidMetalEffectRegistry.Register(new ShadowspecEffect());
        }
    }
}
