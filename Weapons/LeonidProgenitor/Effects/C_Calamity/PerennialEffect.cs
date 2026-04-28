using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class PerennialEffect : LeonidMetalEffect
    {
        public override int EffectID => 23;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int orb = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<Perennial_HealingOrb>(), 0, 0f, meteor.Projectile.owner);
            if (orb >= 0 && orb < Main.maxProjectiles)
                Main.projectile[orb].DamageType = meteor.Projectile.DamageType;
        }
    }
}
