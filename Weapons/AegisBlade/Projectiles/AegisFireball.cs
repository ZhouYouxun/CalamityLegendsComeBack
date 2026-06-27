using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    public class AegisFireball : ModProjectile
    {
        public override string Texture => "CalamityMod/ExtraTextures/TinyGreyscaleCircle";

        private const float VelocityRetention = 0.99f;
        private const int   MaxLifetime      = 240;
        private const float VisualScale      = 0.55f;

        private ref float Timer => ref Projectile.ai[0];
        private float BloomPower => Utils.Remap(Timer, 0f, 130f, 0.8f, 1.18f, true) *
            Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true);

        private static readonly Color CoreColor  = new(255, 245, 190);
        private static readonly Color FlameColor = new(255, 176, 58);
        private static readonly Color EmberColor = new(255, 92, 32);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 22;
            Projectile.friendly    = true;
            Projectile.DamageType  = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate   = 3;
            Projectile.timeLeft    = MaxLifetime;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity  = true;
            Projectile.localNPCHitCooldown   = 20;
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= VelocityRetention;
            Projectile.rotation += Projectile.velocity.X * 0.02f;

            Lighting.AddLight(Projectile.Center, FlameColor.ToVector3() * 0.82f);
            EmitFlameTrail();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.48f, Pitch = 0.18f }, Projectile.Center);
            EmitFlameBurst(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitY), 0.75f);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.08f }, target.Center);
            EmitFlameBurst(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY), 0.9f);
        }

        private void EmitFlameTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color   theme     = Color.Lerp(FlameColor, CoreColor, 0.2f);

            if ((int)Timer % 8 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    -direction * Main.rand.NextFloat(0.25f, 0.9f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.24f, 0.42f) * (0.9f + BloomPower * 0.22f) * VisualScale,
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.35f)),
                    true, false, true));
            }

            if (Main.rand.NextBool(7))
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                Dust dust   = Dust.NewDustPerfect(pos, Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch);
                dust.scale    = Main.rand.NextFloat(0.35f, 0.75f) * VisualScale;
                dust.velocity = -Projectile.velocity * 0.7f;
                dust.noGravity = true;
                dust.color = Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.04f, 0.22f));
            }
        }

        private void EmitFlameBurst(Vector2 position, Vector2 direction, float strength)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(position, Vector2.Zero,
                FlameColor, Vector2.One, direction.ToRotation(), 0f, 0.48f * strength, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, direction * 0.8f,
                CoreColor, new Vector2(0.8f, 1.55f) * strength, direction.ToRotation(), 0.08f, 0.07f, 15));
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = direction.RotatedByRandom(0.8f) * Main.rand.NextFloat(1.4f, 5.2f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    position + Main.rand.NextVector2Circular(7f, 7f), vel,
                    Color.Lerp(EmberColor, FlameColor, Main.rand.NextFloat()), Color.Transparent,
                    Main.rand.NextFloat(0.38f, 0.65f) * strength, Main.rand.Next(16, 25), Main.rand.NextFloat(-0.08f, 0.08f)));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom   = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color   theme        = Color.Lerp(FlameColor, CoreColor, 0.08f) * Projectile.Opacity;
            Vector2 texOrigin    = texture.Size() * 0.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float   completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPos   = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color   trailColor = Color.Lerp(theme, Color.Transparent, completion) * (1f - completion);
                float   trailScale = Projectile.scale * MathHelper.Lerp(0.45f, 1.2f, 1f - completion) * VisualScale;

                Main.EntitySpriteDraw(texture, trailPos, null, trailColor, Projectile.rotation,
                    texOrigin, trailScale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, theme, Projectile.rotation,
                texOrigin, Projectile.scale * 1.15f * VisualScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, theme * 0.48f,
                Projectile.rotation, bloom.Size() * 0.5f, BloomPower * Projectile.scale * 0.65f * VisualScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.Lerp(theme, Color.White, 0.4f) * 0.55f,
                -Projectile.rotation * 0.5f, bloom.Size() * 0.5f, BloomPower * Projectile.scale * 0.32f * VisualScale, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
