using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

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
            int[] meteorTypes = { ProjectileID.Meteor1, ProjectileID.Meteor2, ProjectileID.Meteor3 }; // IDs 424, 425, 426.

            int meteorID = Projectile.NewProjectile(
                meteor.Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                meteorTypes[Main.rand.Next(meteorTypes.Length)],
                meteor.Projectile.damage / 2,
                meteor.Projectile.knockBack,
                meteor.Projectile.owner);

            if (meteorID >= 0 && meteorID < Main.maxProjectiles)
            {
                Main.projectile[meteorID].friendly = true;
                Main.projectile[meteorID].hostile = false;
                Main.projectile[meteorID].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
