using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.Shared
{
    public class Shared_SplitMeteor : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";

        private bool GhostVariant => Projectile.ai[0] > 0.5f;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (!GhostVariant)
            {
                Projectile.localAI[0]++;
                if (Projectile.localAI[0] >= 10f)
                    Projectile.velocity.Y += 0.2f;
            }
            else
            {
                Projectile.velocity *= 1.01f;
                Projectile.alpha += 1;
            }

            Lighting.AddLight(Projectile.Center, GhostVariant ? new Vector3(0.18f, 0.34f, 0.38f) : new Vector3(0.22f, 0.22f, 0.32f));
            Projectile.rotation += 0.22f * System.Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
        }

        public override void OnKill(int timeLeft)
        {
            Color color = GhostVariant ? new Color(168, 255, 255) : new Color(218, 222, 255);
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, Main.rand.NextVector2Circular(3f, 3f), 100, color, Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color drawColor = GhostVariant ? new Color(180, 255, 255, 0) : new Color(236, 236, 255, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor * 0.7f, Projectile.rotation, texture.Size() * 0.5f, GhostVariant ? 0.72f : 0.6f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
