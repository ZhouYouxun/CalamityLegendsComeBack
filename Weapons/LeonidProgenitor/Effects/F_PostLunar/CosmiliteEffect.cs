using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class CosmiliteEffect : LeonidMetalEffect
    {
        public override int EffectID => 30;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.DisableGravity();
            meteor.EnableSimpleHoming(0.06f, 980f);
            meteor.Projectile.velocity *= 1.18f;
            meteor.SetState("cosmilite_rift_timer", Main.rand.Next(18, 28));
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            float timer = meteor.GetState("cosmilite_rift_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 24f : 36f;
                if (Main.myPlayer == projectile.owner)
                    SpawnRift(projectile, projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitY) * 40f, -1, false);
            }

            meteor.SetState("cosmilite_rift_timer", timer);

            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.GemSapphire,
                    -projectile.velocity * Main.rand.NextFloat(0.01f, 0.05f),
                    100,
                    new Color(80, 230, 255),
                    Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            SpawnRift(meteor.Projectile, target.Center, target.whoAmI, true);

            int shardCount = meteor.FromStealthRain ? 6 : 4;
            for (int i = 0; i < shardCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * 13f;
                int shard = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    target.Center,
                    velocity,
                    ModContent.ProjectileType<Cosmilite_Fragment>(),
                    System.Math.Max(1, meteor.Projectile.damage / 3),
                    0f,
                    meteor.Projectile.owner,
                    target.whoAmI,
                    i);
                if (shard >= 0 && shard < Main.maxProjectiles)
                {
                    Main.projectile[shard].DamageType = meteor.Projectile.DamageType;
                    Main.projectile[shard].penetrate = 2;
                }
            }
        }

        private static void SpawnRift(Projectile source, Vector2 center, int targetIndex, bool strong)
        {
            int rift = Projectile.NewProjectile(
                source.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<Cosmilite_Rift>(),
                System.Math.Max(1, strong ? source.damage / 2 : source.damage / 4),
                source.knockBack * 0.25f,
                source.owner,
                targetIndex,
                strong ? 1f : 0f);

            if (rift >= 0 && rift < Main.maxProjectiles)
                Main.projectile[rift].DamageType = source.DamageType;
        }
    }
}
