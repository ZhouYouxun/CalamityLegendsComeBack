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
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBeam";

        private const float DecelerationRate = 0.042f;
        private const float StopThreshold    = 0.25f;
        private const int   MaxLifetime      = 240;

        private ref float Timer => ref Projectile.ai[0];
        private ref float State => ref Projectile.ai[1];
        private bool Stopped => State > 0.5f;

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
            Projectile.usesLocalNPCImmunity  = true;
            Projectile.localNPCHitCooldown   = 20;
        }

        public override void AI()
        {
            Timer++;
            if (!Stopped)
            {
                Projectile.velocity *= MathHelper.Max(0.99f, 1f - DecelerationRate);
                if (Projectile.velocity.Length() < StopThreshold)
                {
                    Projectile.velocity    = Vector2.Zero;
                    Projectile.tileCollide = false;
                    Projectile.penetrate   = -1;
                    State = 1f;
                    EmitFlameBurst(Projectile.Center, Vector2.UnitY, 0.5f);
                }
            }
            else
            {
                Projectile.position.Y += MathF.Sin(Timer * 0.06f) * 0.3f;
            }

            Projectile.rotation = Stopped
                ? Main.GlobalTimeWrappedHourly * 1.2f
                : Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, FlameColor.ToVector3() * (Stopped ? 0.52f : 0.9f));

            if (!Main.dedServ && !Stopped)
                EmitFlameTrail();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity    = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.penetrate   = -1;
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
            Color   theme     = Color.Lerp(FlameColor, CoreColor, 0.2f);

            // GlowOrbParticle 拖尾，类似 PFSlimeGod_Flame 风格
            if ((int)Timer % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    -direction * Main.rand.NextFloat(0.3f, 0.9f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.18f, 0.32f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.35f)),
                    true, false, true));
            }

            // 细小尘埃拖尾
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f);
                Dust dust   = Dust.NewDustPerfect(pos, Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch);
                dust.scale    = Main.rand.NextFloat(0.3f, 0.65f);
                dust.velocity = -direction * 0.6f;
                dust.noGravity = true;
                dust.color    = theme;
            }

            if ((int)Timer % 5 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    -direction * 0.3f,
                    Color.Lerp(FlameColor, CoreColor, 0.45f), 0.18f, 7));
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
            float   lifeRatio    = Projectile.timeLeft / (float)MaxLifetime;
            float   fade         = MathHelper.Clamp(lifeRatio * 3f, 0f, 1f);
            float   pulse        = 0.46f + 0.11f * MathF.Sin(Timer * 0.18f);
            Color   theme        = Color.Lerp(FlameColor, CoreColor, 0.2f) * fade;
            Vector2 texOrigin    = texture.Size() * 0.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 历史帧拖尾 (类似 PFSlimeGod_Flame)
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float   completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPos   = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color   trailColor = Color.Lerp(theme, Color.Transparent, completion) * (1f - completion) * 0.65f;
                float   trailScale = Projectile.scale * MathHelper.Lerp(0.55f, 1.1f, 1f - completion);

                if (trailColor.A == 0 && trailColor.R == 0 && trailColor.G == 0 && trailColor.B == 0)
                    continue;

                Main.EntitySpriteDraw(texture, trailPos, null, trailColor with { A = 0 }, Projectile.oldRot[i],
                    texOrigin, trailScale, SpriteEffects.None, 0);
            }

            // 主体贴图
            Main.EntitySpriteDraw(texture, drawPosition, null, theme with { A = 0 }, Projectile.rotation,
                texOrigin, Projectile.scale * 1.05f, SpriteEffects.None, 0);

            // bloom光晕
            Main.EntitySpriteDraw(bloom, drawPosition, null, FlameColor with { A = 0 } * fade * 0.62f,
                0f, bloom.Size() * 0.5f, pulse * 1.2f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, CoreColor with { A = 0 } * fade * 0.82f,
                0f, bloom.Size() * 0.5f, pulse * 0.55f, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
