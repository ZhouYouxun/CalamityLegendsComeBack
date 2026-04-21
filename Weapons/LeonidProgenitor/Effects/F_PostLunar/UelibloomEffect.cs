using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class UelibloomEffect : LeonidMetalEffect
    {
        public override int EffectID => 29;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity = Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 4f + Main.rand.NextFloat(-0.25f, 0.25f)) * 12f;
                int shard = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, velocity, ModContent.ProjectileType<LeonidSimpleShard>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner, 0f);
                if (shard >= 0 && shard < Main.maxProjectiles)
                    Main.projectile[shard].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
