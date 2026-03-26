using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    internal class EnergyCore_Spark : ModProjectile, ILocalizedModType, IModType
    {
        public new string LocalizationCategory => "Projectiles";

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            base.Projectile.width = 6;
            base.Projectile.height = 12;
            base.Projectile.friendly = true;
            base.Projectile.penetrate = 5;
            base.Projectile.timeLeft = 60;
            base.Projectile.usesLocalNPCImmunity = true;
            base.Projectile.localNPCHitCooldown = 15;
            base.Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            if (base.Projectile.velocity.X != base.Projectile.velocity.X)
            {
                base.Projectile.velocity.X = base.Projectile.velocity.X * -0.1f;
            }

            if (base.Projectile.velocity.X != base.Projectile.velocity.X)
            {
                base.Projectile.velocity.X = base.Projectile.velocity.X * -0.5f;
            }

            if (base.Projectile.velocity.Y != base.Projectile.velocity.Y && base.Projectile.velocity.Y > 1f)
            {
                base.Projectile.velocity.Y = base.Projectile.velocity.Y * -0.5f;
            }

            base.Projectile.ai[0] += 1f;
            if (base.Projectile.ai[0] > 5f)
            {
                base.Projectile.ai[0] = 5f;
                if (base.Projectile.velocity.Y == 0f && base.Projectile.velocity.X != 0f)
                {
                    base.Projectile.velocity.X = base.Projectile.velocity.X * 0.97f;
                    if ((double)base.Projectile.velocity.X > -0.01 && (double)base.Projectile.velocity.X < 0.01)
                    {
                        base.Projectile.velocity.X = 0f;
                        base.Projectile.netUpdate = true;
                    }
                }

                base.Projectile.velocity.Y = base.Projectile.velocity.Y + 0.2f;
            }

            base.Projectile.rotation += base.Projectile.velocity.X * 0.1f;
            int num = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, 206, 0f, 0f, 100);
            Main.dust[num].position.X -= 2f;
            Main.dust[num].position.Y += 2f;
            Main.dust[num].scale += (float)Main.rand.Next(50) * 0.01f;
            Main.dust[num].noGravity = true;
            Main.dust[num].velocity.Y -= 2f;
            if (Main.rand.NextBool())
            {
                int num2 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, 206, 0f, 0f, 100);
                Main.dust[num2].position.X -= 2f;
                Main.dust[num2].position.Y += 2f;
                Main.dust[num2].scale += 0.3f + (float)Main.rand.Next(50) * 0.01f;
                Main.dust[num2].noGravity = true;
                Main.dust[num2].velocity *= 0.1f;
            }

            if ((double)base.Projectile.velocity.Y < 0.25 && (double)base.Projectile.velocity.Y > 0.15)
            {
                base.Projectile.velocity.X = base.Projectile.velocity.X * 0.8f;
            }

            base.Projectile.rotation = (0f - base.Projectile.velocity.X) * 0.05f;
            if (base.Projectile.velocity.Y > 16f)
            {
                base.Projectile.velocity.Y = 16f;
            }

            Time++;
        }

        public ref float Time => ref Projectile.ai[1];

        public override bool? CanDamage() => Time >= 10f; // 初始的时候不会造成伤害，直到x为止
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }
    }
}
