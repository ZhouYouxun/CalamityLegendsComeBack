using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Terraria.Audio;
using System;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class PurifiedGel_Ball : ModProjectile
    {
        // 自定义计时器
        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

            // ===== Aerialite风格拖尾 =====
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float interp = (float)Math.Cos(Projectile.timeLeft / 32f + Main.GlobalTimeWrappedHourly / 20f + i / (float)Projectile.oldPos.Length * MathHelper.Pi) * 0.5f + 0.5f;

                Color color = Color.Lerp(new Color(255, 140, 200), new Color(120, 200, 255), interp) * 0.45f;
                color.A = 0;

                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                float intensity = MathHelper.Lerp(0.2f, 1f, 1f - i / (float)Projectile.oldPos.Length);

                Vector2 scaleOuter = new Vector2(1.8f) * intensity;
                Vector2 scaleInner = new Vector2(1.8f) * intensity * 0.7f;

                Main.EntitySpriteDraw(tex, pos, null, color, Projectile.rotation, tex.Size() * 0.5f, scaleOuter * 0.6f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex, pos, null, color * 0.5f, Projectile.rotation, tex.Size() * 0.5f, scaleInner * 0.6f, SpriteEffects.None, 0);
            }

            // 本体
            Main.EntitySpriteDraw(tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                tex.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        public override void AI()
        {
            timer++;

            Projectile.rotation += 0.22f;

            Color pink = new Color(255, 140, 200);
            Color blue = new Color(120, 200, 255);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.Pi / 2f);

            // ===== 原有双螺旋（加强数学感）=====
            float sine = (float)Math.Sin(timer * 0.45f);
            float squeeze = (float)Math.Cos(timer * 0.7f) * 0.6f;

            Vector2 offset = normal * sine * (10f + squeeze * 4f);

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = Projectile.Center + (i == 0 ? offset : -offset);

                Dust dust = Dust.NewDustPerfect(
                    pos,
                    ModContent.DustType<SquashDust>(),
                    -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f)
                );

                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.7f, 2.2f);
                dust.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
                dust.fadeIn = 1.6f;
            }

            // ===== Auric示波器结构（改色版）=====
            int points = 2;
            float amplitude = 5.5f;
            float freq = 0.35f;

            for (int i = 0; i < points; i++)
            {
                float t = timer + i * 0.4f;

                float wave1 = MathF.Sin(t * freq) * amplitude;
                float wave2 = MathF.Sin(t * freq + MathHelper.Pi) * amplitude;

                Vector2 pos1 = Projectile.Center + normal * wave1 - forward * (i * 4f);
                Vector2 pos2 = Projectile.Center + normal * wave2 - forward * (i * 4f);

                Dust d1 = Dust.NewDustPerfect(pos1, DustID.GemDiamond, Vector2.Zero, 100, pink, 0.9f);
                d1.noGravity = true;

                Dust d2 = Dust.NewDustPerfect(pos2, DustID.GemDiamond, Vector2.Zero, 100, blue, 0.9f);
                d2.noGravity = true;
            }

            // ===== 中轴能量火花 =====
            if (Main.rand.NextBool(2))
            {
                PointParticle spark = new PointParticle(
                    Projectile.Center,
                    -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.8f, 2.5f),
                    false,
                    12,
                    Main.rand.NextFloat(0.7f, 1.1f),
                    Main.rand.NextBool() ? pink : blue
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ===== 轻量光点 =====
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    DustID.GemDiamond,
                    -Projectile.velocity * 0.2f
                );
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.5f, 0.8f);
                d.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
            }

            // 光照
            Lighting.AddLight(Projectile.Center, Color.Lerp(pink, blue, 0.5f).ToVector3() * 0.45f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = Projectile.Center;

            // ===== 爆炸环 =====
            for (int i = 0; i < 16; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 7f);

                Dust dust = Dust.NewDustPerfect(
                    pos,
                    DustID.GemDiamond,
                    vel,
                    80,
                    Color.Lerp(new Color(255, 140, 200), new Color(120, 200, 255), Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.2f, 1.6f)
                );
                dust.noGravity = true;
            }

            // ===== 螺旋粒子 =====
            int count = 14;
            float radius = 30f;
            float baseRot = Main.rand.NextFloat(MathHelper.TwoPi);

            for (int i = 0; i < count; i++)
            {
                float angle = baseRot + MathHelper.TwoPi * i / count;

                Vector2 posOffset = angle.ToRotationVector2() * radius;
                Vector2 vel = posOffset.RotatedBy(MathHelper.Pi / 2).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 4f);

                Particle spark = new SparkParticle(
                    pos + posOffset,
                    vel,
                    false,
                    30,
                    1.1f,
                    Color.Lerp(new Color(255, 140, 200), new Color(120, 200, 255), Main.rand.NextFloat())
                );

                GeneralParticleHandler.SpawnParticle(spark);
            }

            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.6f, Pitch = 0.4f }, pos);
        }
    }
}