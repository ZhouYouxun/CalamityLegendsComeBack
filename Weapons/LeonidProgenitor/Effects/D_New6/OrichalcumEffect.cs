using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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

            for (int i = 0; i < 2; i++)
            {
                float spawnX = owner.direction > 0 ? Main.screenPosition.X : Main.screenPosition.X + Main.screenWidth;
                float spawnY = Main.screenPosition.Y + Main.rand.Next(Main.screenHeight);
                Vector2 spawnPosition = new(spawnX, spawnY);
                Vector2 trajectory = target.Center - spawnPosition;
                trajectory.X += Main.rand.NextFloat(-5f, 5f);
                trajectory.Y += Main.rand.NextFloat(-5f, 5f);
                trajectory = trajectory.SafeNormalize(Vector2.UnitY) * 24f;

                int petal = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    spawnPosition,
                    trajectory,
                    ProjectileID.FlowerPetal,
                    (int)(meteor.Projectile.damage * 0.75f),
                    0f,
                    meteor.Projectile.owner);

                if (petal >= 0 && petal < Main.maxProjectiles)
                {
                    Main.projectile[petal].DamageType = DamageClass.Ranged;
                    Main.projectile[petal].penetrate = 1;
                }
            }
        }
    }
}
