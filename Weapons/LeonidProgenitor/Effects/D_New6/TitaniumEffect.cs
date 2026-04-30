using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class TitaniumEffect : LeonidMetalEffect
    {
        public override int EffectID => 19;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            owner.GetModPlayer<LeonidProgenitorPlayer>().ActivateTitaniumStompers(240);
        }
    }
}
