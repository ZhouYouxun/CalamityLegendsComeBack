using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.B_PreOther
{
    public class MeteoriteEffect : LeonidMetalEffect
    {
        public override int EffectID => 12;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dayTime || !Main.rand.NextBool(3) || Main.myPlayer != meteor.Projectile.owner)
                return;

            Vector2 spawnPosition = new(target.Center.X + Main.rand.Next(-100, 101), target.Center.Y - 500f - Main.rand.Next(20, 80));
            Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 18f;

            Projectile.NewProjectile(
                meteor.Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<LeonidCometSmall>(),
                meteor.Projectile.damage / 2,
                meteor.Projectile.knockBack,
                meteor.Projectile.owner,
                meteor.PrimaryEffectID,
                meteor.SecondaryEffectID,
                0f);
        }
    }
}
