using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class OrichalcumEffect : LeonidMetalEffect
    {
        public override int EffectID => 17;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            float side = Main.rand.NextBool() ? -1f : 1f;
            Vector2 spawnPosition = target.Center + new Vector2(150f * side, Main.rand.NextFloat(-70f, 40f));
            Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitX * -side) * 15f;

            int petal = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), spawnPosition, velocity, ModContent.ProjectileType<Orichalcum_PetalBolt>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner, side);
            if (petal >= 0 && petal < Main.maxProjectiles)
                Main.projectile[petal].DamageType = meteor.Projectile.DamageType;
        }
    }
}
