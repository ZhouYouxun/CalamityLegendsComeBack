using Terraria;
using Terraria.ModLoader;

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

            for (int i = 0; i < 2; i++)
            {
                int wisp = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, Main.rand.NextVector2CircularEdge(1f, 1f) * 10f, ModContent.ProjectileType<Demonite_Wisp>(), meteor.Projectile.damage / 2, meteor.Projectile.knockBack, meteor.Projectile.owner, crimsonVariant ? 1f : 0f);
                if (wisp >= 0 && wisp < Main.maxProjectiles)
                    Main.projectile[wisp].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
