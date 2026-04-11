using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Passive
{
    internal sealed class SHPCPassiveOrb : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        private const float PullStrength = 0.65f;
        private const float MaxSpeed = 14f;

        public ref float OwnerIndex => ref Projectile.ai[0];
        public ref float OrbScaleSeed => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 24;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Main.player.IndexInRange((int)OwnerIndex))
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[(int)OwnerIndex];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 toOwner = owner.Center - Projectile.Center;
            float distance = toOwner.Length();

            Vector2 desiredVelocity = toOwner.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(4f, MaxSpeed, Utils.GetLerpValue(100f, 12f, distance, true));
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, PullStrength);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            float pulse = 0.9f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + Projectile.identity * 0.35f) * 0.08f;
            Projectile.scale = OrbScaleSeed * pulse;

            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.58f) * 1.3f);

            if (distance < 16f)
            {
                SpawnAbsorbBurst(owner.Center);
                Projectile.Kill();
                return;
            }

            if (Main.rand.NextBool(2))
            {
                Particle trail = new CustomSpark(
                    Projectile.Center - Projectile.velocity * 0.35f,
                    -Projectile.velocity * 0.03f,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    10,
                    0.09f * Projectile.scale,
                    Color.Lerp(new Color(40, 170, 255), new Color(150, 235, 255), Main.rand.NextFloat()) * 0.55f,
                    new Vector2(0.65f, 1.35f),
                    true,
                    false,
                    shrinkSpeed: 0.18f
                );
                GeneralParticleHandler.SpawnParticle(trail);
            }
        }

        private void SpawnAbsorbBurst(Vector2 center)
        {
            for (int i = 0; i < 2; i++)
            {
                Particle sparkle = new CustomSpark(
                    center + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextVector2Circular(0.4f, 0.4f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    12,
                    0.1f * Projectile.scale,
                    Color.Lerp(new Color(70, 200, 255), Color.White, Main.rand.NextFloat(0.25f, 0.6f)) * 0.75f,
                    new Vector2(0.6f, 1.2f),
                    true,
                    false,
                    shrinkSpeed: 0.22f
                );
                GeneralParticleHandler.SpawnParticle(sparkle);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            float fade = Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(new Color(22, 110, 210), new Color(120, 225, 255), completion) * 0.32f * fade;
                Vector2 oldDrawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    orb.Value,
                    oldDrawPos,
                    null,
                    trailColor,
                    Projectile.rotation,
                    orb.Size() * 0.5f,
                    Projectile.scale * (0.32f + completion * 0.22f),
                    SpriteEffects.None);
            }

            for (int i = 0; i < 4; i++)
            {
                Color orbColor = Color.Lerp(new Color(28, 145, 255), Color.White, i * 0.18f) with { A = 0 };
                orbColor *= (0.45f - i * 0.07f) * fade;

                Main.EntitySpriteDraw(
                    orb.Value,
                    drawPos,
                    null,
                    orbColor,
                    Projectile.rotation,
                    orb.Size() * 0.5f,
                    Projectile.scale * (0.34f + i * 0.08f),
                    SpriteEffects.None);
            }

            return false;
        }
    }
}
