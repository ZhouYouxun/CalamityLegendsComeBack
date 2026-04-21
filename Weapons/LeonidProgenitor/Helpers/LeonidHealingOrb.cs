using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers
{
    public class LeonidHealingOrb : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_642";

        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Vector2 desiredVelocity = (Owner.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 10.5f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.08f);
            Projectile.rotation += 0.08f;

            Lighting.AddLight(Projectile.Center, new Vector3(0.14f, 0.34f, 0.16f));

            if (Projectile.Hitbox.Intersects(Owner.Hitbox))
            {
                Owner.statLife = System.Math.Min(Owner.statLife + 2, Owner.statLifeMax2);
                Owner.HealEffect(2, true);
                Projectile.Kill();
                return;
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, Main.rand.NextVector2Circular(0.8f, 0.8f), 100, Color.White, Main.rand.NextFloat(0.75f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(142, 255, 166, 0), Projectile.rotation, texture.Size() * 0.5f, 0.42f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
