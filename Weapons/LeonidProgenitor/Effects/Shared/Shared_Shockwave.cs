using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.Shared
{
    public class Shared_Shockwave : ModProjectile
    {
        public override string Texture => "CalamityMod/Particles/BloomRing";

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (!Main.dedServ && Main.LocalPlayer.Distance(Projectile.Center) < 900f)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = System.Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, 1.8f);
            }

            Projectile.scale += 0.08f;
            Projectile.alpha = System.Math.Max(0, Projectile.alpha - 24);
            Lighting.AddLight(Projectile.Center, new Vector3(0.22f, 0.2f, 0.26f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color drawColor = new Color(205, 205, 255, 0) * (1f - Projectile.alpha / 255f) * 0.75f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor, 0f, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
