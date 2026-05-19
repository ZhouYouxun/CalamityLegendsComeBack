using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class BloodstoneCore_BloodOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            timer++;

            if (timer > 6)
            {
                NPC target = Projectile.Center.ClosestNPCAt(1500f);
                if (target != null)
                {
                    float trackingPower = Utils.GetLerpValue(6f, 50f, timer, true);
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * MathHelper.Lerp(16f, 25f, trackingPower);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, MathHelper.Lerp(0.12f, 0.34f, trackingPower));
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(220, 20, 20).ToVector3() * 0.32f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextBool() ? DustID.Blood : DustID.RedTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.22f),
                    0,
                    Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.85f + (float)System.Math.Sin(timer * 0.22f) * 0.12f;

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(220, 24, 24) with { A = 0 } * 0.5f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                0.38f * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.16f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                0.16f * pulse,
                SpriteEffects.None);

            return false;
        }
    }
}
