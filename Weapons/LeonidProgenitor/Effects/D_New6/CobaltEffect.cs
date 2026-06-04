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
            float speed = System.Math.Max(meteor.Projectile.velocity.Length(), 9f);
            float reflectedAngle = meteor.Projectile.velocity.ToRotation() + MathHelper.Pi;
            meteor.Projectile.velocity = (reflectedAngle + Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4)).ToRotationVector2() * speed;
            meteor.Projectile.netUpdate = true;
        }
    }
}
