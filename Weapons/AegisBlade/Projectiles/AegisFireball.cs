using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 左键挥舞时向两侧射出的火球。初速较高，逐渐减速至零后悬停。
    // 后期升级为4个（由 AegisSwingHoldout.SpawnFireballs 决定）。
    public class AegisFireball : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color CoreColor  = new(255, 230, 120);
        private static readonly Color OuterColor = new(255, 160, 60);
        private static readonly Color FadeColor  = new(255, 200, 80);

        private const float DecelerationRate = 0.042f;
        private const float StopThreshold   = 0.25f;
        private const int   MaxLifetime     = 240;  // 4秒
        private bool stopped = false;

        private ref float Timer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 34;
            Projectile.friendly   = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate  = 3;
            Projectile.timeLeft   = MaxLifetime;
            Projectile.usesLocalNPCImmunity   = true;
            Projectile.localNPCHitCooldown    = 20;
        }

        public override void AI()
        {
            Timer++;

            if (!stopped)
            {
                Projectile.velocity *= (1f - DecelerationRate);

                if (Projectile.velocity.Length() < StopThreshold)
                {
                    Projectile.velocity = Vector2.Zero;
                    stopped = true;
                    Projectile.tileCollide = false;
                    Projectile.penetrate   = -1;
                }
            }

            // 悬停时缓慢脉动（轻微上下浮动）
            if (stopped)
            {
                Projectile.position.Y += MathF.Sin(Timer * 0.06f) * 0.3f;
            }

            // 发光粒子
            if (!Main.dedServ)
            {
                int particleChance = stopped ? 2 : 3;
                if (Main.rand.NextBool(particleChance))
                {
                    float angle  = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = Main.rand.NextFloat(8f, 22f);
                    Vector2 offset = angle.ToRotationVector2() * radius;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GoldFlame,
                        Vector2.Zero, 0,
                        Main.rand.NextBool(2) ? CoreColor : OuterColor,
                        Main.rand.NextFloat(0.8f, 1.5f));
                    dust.noGravity = true;
                    dust.velocity *= 0.4f;
                }
            }

            Projectile.rotation = stopped
                ? Main.GlobalTimeWrappedHourly * 0.8f
                : Projectile.velocity.ToRotation();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            stopped = true;
            Projectile.velocity    = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.penetrate   = -1;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos  = Projectile.Center - Main.screenPosition;
            float lifeRatio  = Projectile.timeLeft / (float)MaxLifetime;
            float fadeFactor = MathHelper.Clamp(lifeRatio * 3f, 0f, 1f);  // 最后1/3秒淡出
            float pulse      = 0.45f + 0.10f * MathF.Sin(Timer * 0.18f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 外层柔光
            Main.EntitySpriteDraw(bloom, drawPos, null,
                OuterColor with { A = 0 } * 0.55f * fadeFactor,
                0f, bloom.Size() * 0.5f, pulse * 1.7f, SpriteEffects.None, 0);

            // 内核
            Main.EntitySpriteDraw(bloom, drawPos, null,
                CoreColor with { A = 0 } * 0.9f * fadeFactor,
                0f, bloom.Size() * 0.5f, pulse * 0.9f, SpriteEffects.None, 0);

            // 悬停中的追加白核
            if (stopped)
            {
                Main.EntitySpriteDraw(bloom, drawPos, null,
                    Color.White with { A = 0 } * 0.35f * fadeFactor,
                    0f, bloom.Size() * 0.5f, pulse * 0.35f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
