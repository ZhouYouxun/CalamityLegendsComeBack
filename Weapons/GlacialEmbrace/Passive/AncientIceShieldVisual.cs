using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.Passive
{
    public class AncientIceShieldVisual : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GlacialEmbrace/远古冰盾";

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 116;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            GlacialEmbracePlayer modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();
            if (!modPlayer.AncientIceShieldActive)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = player.Center + new Vector2(0f, -4f);
            Projectile.rotation = MathHelper.Lerp(Projectile.rotation, 0f, 0.12f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.22f, 0.58f, 0.9f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float pulse = 0.94f + (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.035f;

            Color outer = new Color(80, 210, 255, 0) * 0.45f;
            Color inner = new Color(220, 250, 255, 110) * 0.78f;

            Main.EntitySpriteDraw(texture, drawPosition, null, outer, Projectile.rotation, origin, pulse * 1.08f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, inner, Projectile.rotation, origin, pulse, SpriteEffects.None);
            return false;
        }
    }
}
