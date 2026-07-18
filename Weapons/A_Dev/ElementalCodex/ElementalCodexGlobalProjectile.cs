using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    internal sealed class ElementalCodexGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => false;

        public override void AI(Projectile projectile)
        {
            if (!projectile.friendly || projectile.hostile || projectile.damage <= 0)
                return;

            if (!Main.player.IndexInRange(projectile.owner))
                return;

            Player owner = Main.player[projectile.owner];
            if (!owner.active || !owner.GetModPlayer<ElementalCodexPlayer>().ElementalCodexEquipped)
                return;

            NPC target = ElementalCodexGlobalNPC.FindFlourishTarget(owner, projectile.Center);
            if (target == null)
                return;

            Vector2 toTarget = target.Center - projectile.Center;
            float speed = projectile.velocity.Length();
            if (speed < 2f || toTarget.LengthSquared() < 24f * 24f)
                return;

            Vector2 desiredVelocity = toTarget.SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitY)) * speed;
            projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, 0.075f);
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.player.IndexInRange(projectile.owner))
                return;

            Player owner = Main.player[projectile.owner];
            ElementalCodexGlobalNPC.TryApplyWeaponElement(target, owner, owner.HeldItem);
        }
    }
}
