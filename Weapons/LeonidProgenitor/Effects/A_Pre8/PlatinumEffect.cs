using CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class PlatinumEffect : LeonidMetalEffect
    {
        public override int EffectID => 8;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int slashCount = target.life <= target.lifeMax * 0.18f ? 3 : 2;
            for (int i = 0; i < slashCount; i++)
            {
                Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(90f, 90f) + Main.rand.NextVector2Circular(24f, 24f);
                Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(9f, 12f);
                int slash = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<TwistingNether_BlackSLASH>(),
                    (int)(meteor.Projectile.damage * (target.life <= target.lifeMax * 0.18f ? 0.85f : 0.6f)),
                    meteor.Projectile.knockBack,
                    meteor.Projectile.owner);

                if (slash >= 0 && slash < Main.maxProjectiles)
                    Main.projectile[slash].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
