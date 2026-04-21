using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class ShadowspecEffect : LeonidMetalEffect
    {
        public override int EffectID => 32;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * 8f;
            int shard = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, velocity, ModContent.ProjectileType<LeonidSimpleShard>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner, 3f);
            if (shard >= 0 && shard < Main.maxProjectiles)
                Main.projectile[shard].DamageType = meteor.Projectile.DamageType;
        }
    }
}
