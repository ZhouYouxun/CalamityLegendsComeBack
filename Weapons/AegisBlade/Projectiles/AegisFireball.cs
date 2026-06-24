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
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float DecelerationRate = 0.042f;
        private const float StopThreshold = 0.25f;
        private const int MaxLifetime = 240;

        private ref float Timer => ref Projectile.ai[0];
        private ref float State => ref Projectile.ai[1];
        private bool Stopped => State > 0.5f;

        private static readonly Color CoreColor = new(255, 245, 190);
        private static readonly Color FlameColor = new(255, 176, 58);
        private static readonly Color EmberColor = new(255, 92, 32);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = MaxLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Timer++;
            if (!Stopped)
            {
                Projectile.velocity *= 1f - DecelerationRate;
                if (Projectile.velocity.Length() < StopThreshold)
                {
                    Projectile.velocity = Vector2.Zero;
                    Projectile.tileCollide = false;
                    Projectile.penetrate = -1;
                    State = 1f;
                    EmitFlameBurst(Projectile.Center, Vector2.UnitY, 0.5f);
                }
            }
            else
            {
                Projectile.position.Y += MathF.Sin(Timer * 0.06f) * 0.3f;
            }

            Lighting.AddLight(Projectile.Center, FlameColor.ToVector3() * (Stopped ? 0.52f : 0.9f));
            Projectile.rotation = Stopped ? Main.GlobalTimeWrappedHourly * 0.8f : Projectile.velocity.ToRotation();

            if (!Main.dedServ && !Stopped && Main.rand.NextBool(2))
                EmitFlameTrail();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            State = 1f;
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
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 position = Projectile.Center - direction * Main.rand.NextFloat(8f, 26f) + Main.rand.NextVector2Circular(6f, 6f);
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                position, -direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.3f, 3.8f),
                Color.Lerp(EmberColor, FlameColor, Main.rand.NextFloat(0.25f, 0.85f)), Color.Transparent,
                Main.rand.NextFloat(0.5f, 0.82f), Main.rand.Next(18, 30), Main.rand.NextFloat(-0.08f, 0.08f)));

            if ((int)Timer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(position, -direction * 0.4f,
                    Color.Lerp(FlameColor, CoreColor, 0.45f), 0.26f, 9));
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
                Vector2 velocity = direction.RotatedByRandom(0.8f) * Main.rand.NextFloat(1.4f, 5.2f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(position + Main.rand.NextVector2Circular(7f, 7f),
                    velocity, Color.Lerp(EmberColor, FlameColor, Main.rand.NextFloat()), Color.Transparent,
                    Main.rand.NextFloat(0.38f, 0.65f) * strength, Main.rand.Next(16, 25), Main.rand.NextFloat(-0.08f, 0.08f)));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float lifeRatio = Projectile.timeLeft / (float)MaxLifetime;
            float fade = MathHelper.Clamp(lifeRatio * 3f, 0f, 1f);
            float pulse = 0.46f + 0.11f * MathF.Sin(Timer * 0.18f);
            Vector2 direction = Stopped ? Vector2.UnitY : Projectile.velocity.SafeNormalize(Vector2.UnitY);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            if (!Stopped)
            {
                Main.EntitySpriteDraw(bloom, drawPosition - direction * 13f, null,
                    EmberColor with { A = 0 } * fade * 0.46f, direction.ToRotation() + MathHelper.PiOver2,
                    bloom.Size() * 0.5f, new Vector2(pulse * 0.48f, pulse * 1.72f), SpriteEffects.None);
            }
            Main.EntitySpriteDraw(bloom, drawPosition, null, FlameColor with { A = 0 } * fade * 0.68f,
                0f, bloom.Size() * 0.5f, pulse * 1.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, CoreColor with { A = 0 } * fade * 0.92f,
                0f, bloom.Size() * 0.5f, pulse * 0.72f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
