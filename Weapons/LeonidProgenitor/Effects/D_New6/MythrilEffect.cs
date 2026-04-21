using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class MythrilEffect : LeonidMetalEffect
    {
        public override int EffectID => 16;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int flame = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), owner.Center, Vector2.Zero, ModContent.ProjectileType<LeonidOrbitFlame>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner, Main.rand.NextFloat(0f, MathHelper.TwoPi));
            if (flame >= 0 && flame < Main.maxProjectiles)
                Main.projectile[flame].DamageType = meteor.Projectile.DamageType;
        }
    }
}
