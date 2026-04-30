using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class ScoriaEffect : LeonidMetalEffect
    {
        public override int EffectID => 26;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);

            for (int i = 0; i < 32; i++)
            {
                Vector2 velocity = new(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-10.5f, -4.8f));
                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(18f, 10f),
                    Main.rand.NextBool(3) ? DustID.LavaMoss : DustID.Torch,
                    velocity,
                    100,
                    Color.Lerp(new Color(255, 80, 36), new Color(255, 205, 88), Main.rand.NextFloat(0.45f)),
                    Main.rand.NextFloat(1.1f, 1.8f));

                dust.noGravity = false;
                dust.fadeIn = Main.rand.NextFloat(0.4f, 0.9f);
            }
        }
    }
}
