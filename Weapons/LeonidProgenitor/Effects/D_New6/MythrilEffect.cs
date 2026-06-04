using CalamityMod.Projectiles.Typeless;
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

            Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(120f, 120f) + Main.rand.NextVector2Circular(32f, 32f);
            Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.7f) * 5f;
            int flame = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), spawnPosition, velocity, ModContent.ProjectileType<MythrilFlare>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner);
            if (flame >= 0 && flame < Main.maxProjectiles)
                Main.projectile[flame].DamageType = meteor.Projectile.DamageType;
        }
    }
}
