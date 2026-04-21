using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers
{
    public class LeonidTrailMushroom : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_131";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 42;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.rotation += 0.18f;
            Projectile.scale *= 0.985f;
            Projectile.alpha += 6;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color drawColor = new Color(128, 176, 255, 0) * (1f - Projectile.alpha / 255f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor, Projectile.rotation, texture.Size() * 0.5f, 0.4f * Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
