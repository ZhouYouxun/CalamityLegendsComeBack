using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.B_PreOther
{
    public class CrimtaneEffect : DemoniteEffect
    {
        public override int EffectID => 10;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnWisps(meteor, target, true);
        }
    }
}
