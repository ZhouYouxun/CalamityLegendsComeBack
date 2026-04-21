using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class AuricEffect : LeonidMetalEffect
    {
        public override int EffectID => 31;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 spawnPosition = target.Center + new Vector2(Main.rand.NextFloat(-90f, 90f), -440f - Main.rand.NextFloat(0f, 80f));
                Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 21f;
                int shard = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), spawnPosition, velocity, ModContent.ProjectileType<LeonidSimpleShard>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner, 2f);
                if (shard >= 0 && shard < Main.maxProjectiles)
                    Main.projectile[shard].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
