using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5
{
    public class Hallowed_Laser : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "Terraria/Images/Projectile_466";

        private const int Lifetime = 14;
        private const float MaxBeamLength = 360f;
        private Vector2 beamVector = Vector2.UnitY;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }

        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero)
            {
                beamVector = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                Projectile.rotation = beamVector.ToRotation();
                Projectile.velocity = Vector2.Zero;
                Projectile.ai[0] = MaxBeamLength;
            }

            float power = Projectile.timeLeft / (float)Lifetime;
            Projectile.scale = MathHelper.Lerp(0.35f, 1.05f, power);
            Lighting.AddLight(Projectile.Center, new Vector3(0.35f, 0.32f, 0.12f) * power);

            for (int i = 0; i < 6; i++)
            {
                Vector2 sample = Projectile.Center + beamVector * Main.rand.NextFloat(0f, Projectile.ai[0]);
                Dust dust = Dust.NewDustPerfect(sample + Main.rand.NextVector2Circular(4f, 4f), DustID.GoldFlame, beamVector.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.2f, 1.2f), 100, Color.White, Main.rand.NextFloat(0.7f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + beamVector * Projectile.ai[0],
                14f * Projectile.scale,
                ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = (Projectile.Center.X < target.Center.X).ToDirectionInt();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 beamStart = Projectile.Center.Floor() + beamVector * Projectile.scale * 10f - Main.screenPosition;
            Vector2 beamEnd = beamStart + beamVector * Projectile.ai[0];
            Vector2 scale = new(Projectile.scale * 0.7f);
            Utils.LaserLineFraming framing = new(DelegateMethods.RainbowLaserDraw);

            Color beamColor = new Color(255, 237, 145, 0) * (Projectile.timeLeft / (float)Lifetime);
            DelegateMethods.c_1 = beamColor;
            DelegateMethods.f_1 = 1f;
            Utils.DrawLaser(Main.spriteBatch, texture, beamStart, beamEnd, scale, framing);

            DelegateMethods.c_1 = Color.White * 0.55f * (Projectile.timeLeft / (float)Lifetime);
            Utils.DrawLaser(Main.spriteBatch, texture, beamStart, beamEnd, scale * 0.55f, framing);
            return false;
        }
    }
}
