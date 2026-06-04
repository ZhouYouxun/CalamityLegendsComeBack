using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5
{
    public class Lunar_Strike : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_645";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.28f, 0.38f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 8f), DustID.Vortex, Main.rand.NextVector2Circular(1f, 1f), 100, Color.White, Main.rand.NextFloat(0.75f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(176, 226, 255, 0), Projectile.rotation, texture.Size() * 0.5f, 0.78f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
