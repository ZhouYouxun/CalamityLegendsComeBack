using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class LifeAlloyEffect : LeonidMetalEffect
    {
        public override int EffectID => 33;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(70f, 70f);
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 3.6f);
                int gleam = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<LifeAlloy_Gleam>(),
                    meteor.Projectile.damage / 2,
                    0f,
                    meteor.Projectile.owner,
                    target.whoAmI,
                    Main.rand.Next(3));

                if (gleam >= 0 && gleam < Main.maxProjectiles)
                    Main.projectile[gleam].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
