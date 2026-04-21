using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class CobaltEffect : LeonidMetalEffect
    {
        public override int EffectID => 14;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate, 2);
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (meteor.HasFlag("cobalt_ricochet"))
                return;

            meteor.SetFlag("cobalt_ricochet");
            meteor.Projectile.velocity = meteor.Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(100f));
            meteor.Projectile.netUpdate = true;
        }
    }
}
