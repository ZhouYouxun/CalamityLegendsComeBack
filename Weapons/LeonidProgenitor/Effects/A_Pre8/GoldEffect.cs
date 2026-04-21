using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class GoldEffect : LeonidMetalEffect
    {
        public override int EffectID => 7;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Coins with { Volume = 0.55f }, target.Center);
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, DustID.GoldCoin, Main.rand.NextVector2Circular(3.6f, 3.6f), 100, Color.White, Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
            }
        }
    }
}
