using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class ScoriaEffect : LeonidMetalEffect
    {
        public override int EffectID => 26;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-8.5f, -5.5f));
                int glob = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, velocity, ModContent.ProjectileType<Scoria_LavaGlob>(), meteor.Projectile.damage / 3, 0f, meteor.Projectile.owner);
                if (glob >= 0 && glob < Main.maxProjectiles)
                    Main.projectile[glob].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
