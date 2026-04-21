using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class LeadEffect : LeonidMetalEffect
    {
        public override int EffectID => 4;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int shockwave = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<LeonidShockwave>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner);
            if (shockwave >= 0 && shockwave < Main.maxProjectiles)
                Main.projectile[shockwave].DamageType = meteor.Projectile.DamageType;
        }
    }
}
