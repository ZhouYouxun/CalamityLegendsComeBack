using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal sealed class SHPCRight_Explosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int IgnoredTarget => (int)Projectile.ai[0];
        private int ExplosionSize => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            if (ExplosionSize > 0 && Projectile.width != ExplosionSize)
            {
                Vector2 center = Projectile.Center;
                Projectile.width = ExplosionSize;
                Projectile.height = ExplosionSize;
                Projectile.Center = center;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.whoAmI == IgnoredTarget)
                return false;

            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
