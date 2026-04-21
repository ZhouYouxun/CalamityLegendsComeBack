using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class CopperEffect : LeonidMetalEffect
    {
        public override int EffectID => 1;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int bolt = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, Main.rand.NextVector2CircularEdge(1f, 1f) * 14f, ModContent.ProjectileType<LeonidChainLightning>(), meteor.Projectile.damage / 2, meteor.Projectile.knockBack, meteor.Projectile.owner, target.whoAmI, 2f);
            if (bolt >= 0 && bolt < Main.maxProjectiles)
                Main.projectile[bolt].DamageType = meteor.Projectile.DamageType;
        }
    }
}
