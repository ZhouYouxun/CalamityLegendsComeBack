using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.Pa5
{
    // The player is invulnerable while this follows the dash; local NPC immunity makes each target take one impact per dash.
    internal sealed class BFPa5BreakthroughDashHitbox : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 16;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            // The dash begins on the controlling client. Keep this networked hitbox alive for its
            // fixed lifetime instead of relying on an unsynchronised ModPlayer timer on the server.
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.velocity = Microsoft.Xna.Framework.Vector2.Zero;
            owner.immuneNoBlink = true;
            owner.GiveUniversalIFrames(2, false);
        }
    }
}
