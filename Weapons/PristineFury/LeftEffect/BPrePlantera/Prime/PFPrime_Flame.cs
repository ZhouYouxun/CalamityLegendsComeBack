using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPrime_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 86;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation += 0.38f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
            Projectile.velocity = Projectile.velocity.RotatedBy((float)Math.Sin(Timer * 0.2f) * 0.018f) * 0.99f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.95f, 0.68f, 0.22f) * 0.55f);

            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Main.rand.NextBool(2))
            {
                Particle spark = new SparkParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 16f),
                    -forward.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.2f, 3.8f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.NextBool() ? Color.Gold : Color.Orange);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Timer % 9f == 0f)
            {
                Particle gearPulse = new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Projectile.velocity * 0.12f,
                    "CalamityMod/Particles/PulseStar",
                    false,
                    15,
                    0.06f,
                    new Color(255, 206, 92),
                    new Vector2(0.6f, 1.7f),
                    true,
                    false,
                    extraRotation: Projectile.rotation);
                GeneralParticleHandler.SpawnParticle(gearPulse);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 240);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/PulseStar").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 10f, Timer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, drawPosition, null, new Color(255, 206, 92, 0) * opacity * 0.3f, Projectile.rotation, bloom.Size() * 0.5f, 0.24f, SpriteEffects.None, 0);
            for (int i = 0; i < 3; i++)
            {
                float rot = Projectile.rotation + MathHelper.TwoPi * i / 3f + Timer * 0.03f;
                Main.EntitySpriteDraw(star, drawPosition, null, new Color(255, 226, 132, 0) * opacity * 0.45f, rot, star.Size() * 0.5f, 0.2f + i * 0.03f, SpriteEffects.None, 0);
            }
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
