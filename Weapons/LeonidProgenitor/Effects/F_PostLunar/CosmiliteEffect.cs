using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class CosmiliteEffect : LeonidMetalEffect
    {
        public override int EffectID => 30;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * 13f;
                int shard = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, velocity, ModContent.ProjectileType<PostLunar_Shard>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner, 1f);
                if (shard >= 0 && shard < Main.maxProjectiles)
                {
                    Main.projectile[shard].DamageType = meteor.Projectile.DamageType;
                    Main.projectile[shard].penetrate = 2;
                }
            }
        }
    }
}
