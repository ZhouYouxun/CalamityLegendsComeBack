using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    public class AegisBigFireball : ModProjectile
    {
        public override string Texture => "CalamityMod/ExtraTextures/TinyGreyscaleCircle";

        private const int MaxHoverTimer = 65;

        private ref float Timer => ref Projectile.ai[0];
        private ref float HasErupted => ref Projectile.ai[1];

        private static readonly Color CoreColor  = new(255, 245, 190);
        private static readonly Color FlameColor = new(255, 180, 50);
        private static readonly Color EmberColor = new(255, 110, 30);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 36;
            Projectile.friendly    = true;
            Projectile.DamageType  = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate   = -1;
            Projectile.timeLeft    = MaxHoverTimer;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 15;
        }

        public override void AI()
        {
            Timer++;

            // 逐渐减速至空中悬停
            if (Projectile.velocity.Length() > 0.15f)
            {
                Projectile.velocity *= 0.90f;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
            }

            Projectile.rotation += 0.08f;
            Lighting.AddLight(Projectile.Center, FlameColor.ToVector3() * 1.1f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 particleVel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(16f, 16f), particleVel,
                    Color.Lerp(FlameColor, CoreColor, Main.rand.NextFloat()), Color.Transparent,
                    Main.rand.NextFloat(0.35f, 0.65f), Main.rand.Next(12, 20), Main.rand.NextFloat(-0.04f, 0.04f)));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (HasErupted > 0f) return;
            HasErupted = 1f;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.82f, Pitch = 0.1f }, Projectile.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero,
                    CoreColor, new Vector2(1.2f, 1.2f), 0f, 0f, 0.6f, 16));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                    FlameColor, Vector2.One, 0f, 0.08f, 1.4f, 18));
            }

            if (Projectile.owner == Main.myPlayer)
            {
                int fireballType = ModContent.ProjectileType<AegisFireball>();
                int fireballDamage = (int)(Projectile.damage * 0.75f);
                int count = 4;

                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.25f, 0.25f);
                    Vector2 shootVelocity = angle.ToRotationVector2() * Main.rand.NextFloat(10f, 14f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, shootVelocity,
                        fireballType, fireballDamage, Projectile.knockBack * 0.5f, Projectile.owner);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float pulse = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + Projectile.whoAmI);
            float scaleProgress = MathHelper.Clamp(Timer / 15f, 0.2f, 1f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // 绘制旧残影拖尾
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                Vector2 oldDrawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float trailFactor = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(EmberColor, FlameColor, trailFactor) * (trailFactor * 0.45f);
                Main.EntitySpriteDraw(bloom, oldDrawPos, null, trailColor with { A = 0 },
                    0f, bloom.Size() * 0.5f, (0.45f + trailFactor * 0.25f) * scaleProgress, SpriteEffects.None);
            }

            // 核心外环与发光
            float ringRotation = Main.GlobalTimeWrappedHourly * 3f + Projectile.whoAmI;
            Main.EntitySpriteDraw(ring, drawPos, null, FlameColor with { A = 0 } * 0.6f * pulse,
                ringRotation, ring.Size() * 0.5f, 0.38f * scaleProgress, SpriteEffects.None);

            Main.EntitySpriteDraw(ring, drawPos, null, CoreColor with { A = 0 } * 0.45f * pulse,
                -ringRotation * 1.5f, ring.Size() * 0.5f, 0.26f * scaleProgress, SpriteEffects.None);

            Main.EntitySpriteDraw(bloom, drawPos, null, FlameColor with { A = 0 } * 0.8f,
                0f, bloom.Size() * 0.5f, 0.65f * scaleProgress * pulse, SpriteEffects.None);

            Main.EntitySpriteDraw(bloom, drawPos, null, CoreColor with { A = 0 } * 0.95f,
                0f, bloom.Size() * 0.5f, 0.32f * scaleProgress, SpriteEffects.None);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
