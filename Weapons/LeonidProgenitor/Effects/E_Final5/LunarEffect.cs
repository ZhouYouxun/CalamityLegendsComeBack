using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5
{
    public class LunarEffect : LeonidMetalEffect
    {
        public override int EffectID => 27;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 spawnPosition = target.Center + new Vector2(Main.rand.NextFloat(-120f, 120f), -480f - Main.rand.NextFloat(0f, 160f));
                Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 20f;
                int projectileType = Main.rand.NextBool() ? ProjectileID.MoonlordArrowTrail : ProjectileID.LunarFlare; // IDs 640 and 645.
                int strike = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), spawnPosition, velocity, projectileType, meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner);
                if (strike >= 0 && strike < Main.maxProjectiles)
                {
                    Main.projectile[strike].friendly = true;
                    Main.projectile[strike].hostile = false;
                    Main.projectile[strike].DamageType = meteor.Projectile.DamageType;
                }
            }
        }
    }
}
