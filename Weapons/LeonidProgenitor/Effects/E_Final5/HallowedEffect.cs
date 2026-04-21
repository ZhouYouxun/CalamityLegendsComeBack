using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5
{
    public class HallowedEffect : LeonidMetalEffect
    {
        public override int EffectID => 21;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            Vector2 spawnPosition = target.Center + Main.rand.NextVector2Circular(140f, 90f);
            Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 18f;
            int laser = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), spawnPosition, velocity, ModContent.ProjectileType<LeonidHolyLaser>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner);
            if (laser >= 0 && laser < Main.maxProjectiles)
                Main.projectile[laser].DamageType = meteor.Projectile.DamageType;
        }
    }
}
