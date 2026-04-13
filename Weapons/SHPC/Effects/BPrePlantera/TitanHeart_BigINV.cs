using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class TitanHeart_BigINV : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 250;
            Projectile.height = 250;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            int boundMainProjectileID = (int)Projectile.ai[0];
            if (!Main.projectile.IndexInRange(boundMainProjectileID))
            {
                Projectile.Kill();
                return;
            }

            Projectile mainProjectile = Main.projectile[boundMainProjectileID];
            if (!mainProjectile.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = mainProjectile.Center;
            Projectile.velocity = mainProjectile.velocity;
            Projectile.rotation = mainProjectile.rotation;
            Projectile.direction = mainProjectile.direction;
            Projectile.spriteDirection = mainProjectile.spriteDirection;
            Projectile.damage = mainProjectile.damage;
            Projectile.knockBack = mainProjectile.knockBack;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
