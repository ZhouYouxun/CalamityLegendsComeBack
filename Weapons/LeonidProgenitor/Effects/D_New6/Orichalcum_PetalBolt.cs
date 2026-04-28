using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class Orichalcum_PetalBolt : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_221";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity = Projectile.velocity.RotatedBy(0.012f * System.Math.Sign(Projectile.ai[0] == 0f ? 1f : Projectile.ai[0]));
            Lighting.AddLight(Projectile.Center, new Vector3(0.34f, 0.18f, 0.26f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(255, 155, 195, 0), Projectile.rotation, texture.Size() * 0.5f, 0.62f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
