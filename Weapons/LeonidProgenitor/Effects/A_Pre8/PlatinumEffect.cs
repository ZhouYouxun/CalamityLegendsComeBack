using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class PlatinumEffect : LeonidMetalEffect
    {
        public override int EffectID => 8;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.rand.NextBool(4))
                return;

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int buffType = owner.buffType[i];
                if (buffType <= 0 || !Main.debuff[buffType])
                    continue;

                owner.DelBuff(i);
                break;
            }
        }
    }
}
