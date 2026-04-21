using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class AdamantiteEffect : LeonidMetalEffect
    {
        public override int EffectID => 18;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int field = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<LeonidLingeringField>(), meteor.Projectile.damage / 3, 0f, meteor.Projectile.owner, 0f);
            if (field >= 0 && field < Main.maxProjectiles)
                Main.projectile[field].DamageType = meteor.Projectile.DamageType;
        }
    }
}
