using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers
{
    public class LeonidLingeringField : ModProjectile
    {
        public override string Texture => "CalamityMod/Particles/BloomCircle";

        private bool CryonicField => Projectile.ai[0] > 0.5f;

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 92;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.alpha = 220;
        }

        public override void AI()
        {
            Projectile.scale = MathHelper.Lerp(0.3f, 1.15f, 1f - Projectile.timeLeft / 90f);
            Lighting.AddLight(Projectile.Center, (CryonicField ? new Vector3(0.1f, 0.22f, 0.34f) : new Vector3(0.34f, 0.14f, 0.08f)) * 1.2f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.45f, Projectile.height * 0.45f),
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(1f, 1f),
                    100,
                    CryonicField ? new Color(148, 232, 255) : new Color(255, 142, 84),
                    Main.rand.NextFloat(0.85f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(CryonicField ? BuffID.Frostburn2 : BuffID.OnFire3, CryonicField ? 180 : 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color drawColor = (CryonicField ? new Color(126, 220, 255, 0) : new Color(255, 129, 82, 0)) * 0.45f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor, 0f, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
