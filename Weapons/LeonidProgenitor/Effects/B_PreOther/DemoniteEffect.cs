using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.B_PreOther
{
    public class DemoniteEffect : LeonidMetalEffect
    {
        public override int EffectID => 9;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnWisps(meteor, target, false);
        }

        protected static void SpawnWisps(LeonidCometSmall meteor, NPC target, bool crimsonVariant)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            // ProjectileID.VampireKnife = 304, ProjectileID.EatersBite = 306.
            int projectileType = crimsonVariant ? ProjectileID.VampireKnife : ProjectileID.EatersBite;
            int projectileCount = crimsonVariant ? 3 : 2;
            for (int i = 0; i < projectileCount; i++)
            {
                Microsoft.Xna.Framework.Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(70f, 70f) + Main.rand.NextVector2Circular(12f, 12f);
                Microsoft.Xna.Framework.Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Microsoft.Xna.Framework.Vector2.UnitY) * (crimsonVariant ? 13f : 10f);

                int spawned = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    projectileType,
                    crimsonVariant ? meteor.Projectile.damage / 3 : meteor.Projectile.damage / 2,
                    meteor.Projectile.knockBack,
                    meteor.Projectile.owner);

                if (spawned >= 0 && spawned < Main.maxProjectiles)
                {
                    Main.projectile[spawned].friendly = true;
                    Main.projectile[spawned].hostile = false;
                    Main.projectile[spawned].DamageType = meteor.Projectile.DamageType;
                }
            }
        }
    }
}
