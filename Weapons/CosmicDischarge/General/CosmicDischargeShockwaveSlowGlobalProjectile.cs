using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public sealed class CosmicDischargeShockwaveSlowGlobalProjectile : GlobalProjectile
    {
        private int slowTime;
        private float speedLimit;

        public override bool InstancePerEntity => true;

        public void ApplyShockwave(Projectile projectile, int duration, float speedMultiplier)
        {
            float currentSpeed = projectile.velocity.Length();
            speedLimit = slowTime > 0
                ? Math.Min(speedLimit, Math.Max(2.5f, currentSpeed * speedMultiplier))
                : Math.Max(2.5f, currentSpeed * speedMultiplier);
            slowTime = Math.Max(slowTime, duration);
            projectile.velocity *= speedMultiplier;
            projectile.netUpdate = true;
        }

        public override void PostAI(Projectile projectile)
        {
            if (slowTime <= 0)
                return;

            slowTime--;
            if (projectile.velocity.LengthSquared() > speedLimit * speedLimit)
                projectile.velocity = projectile.velocity.SafeNormalize(Microsoft.Xna.Framework.Vector2.Zero) * speedLimit;
        }
    }
}
