using CalamityMod;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class AstralEffect : LeonidMetalEffect
    {
        public override int EffectID => 28;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 position = target.Center + Main.rand.NextVector2Circular(150f, 110f);
                int blast = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<SummonAstralExplosion>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner);
                if (blast >= 0 && blast < Main.maxProjectiles)
                    Main.projectile[blast].DamageType = ModContent.GetInstance<RogueDamageClass>();
            }
        }
    }
}
