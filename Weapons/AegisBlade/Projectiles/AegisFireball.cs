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
    public class AegisFireball : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color CoreColor = new(255, 230, 120);
        private static readonly Color OuterColor = new(255, 160, 60);

        private const float DecelerationRate = 0.038f;
        private const float StopThreshold = 0.3f;
        private const int MaxLifetime = 240;   // 4 秒
        private bool stopped = false;

        private ref float Timer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
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

            if (!stopped)
            {
                // 逐帧减速
                Projectile.velocity *= (1f - DecelerationRate);

                if (Projectile.velocity.Length() < StopThreshold)
                {
                    Projectile.velocity = Vector2.Zero;
                    stopped = true;
                    Projectile.tileCollide = false;
                    Projectile.penetrate = -1;
                }
            }

            // 发光粒子
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(8f, 20f);
                Vector2 offset = angle.ToRotationVector2() * radius;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GoldFlame,
                    Vector2.Zero, 0, Main.rand.NextBool(2) ? CoreColor : OuterColor,
                    Main.rand.NextFloat(0.8f, 1.4f));
                dust.noGravity = true;
                dust.velocity *= 0.5f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            stopped = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float scale = 0.45f + 0.08f * MathF.Sin(Timer * 0.18f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 外层光晕
            Main.EntitySpriteDraw(bloom, drawPos, null,
                OuterColor with { A = 0 } * 0.55f, 0f, bloom.Size() * 0.5f, scale * 1.6f, SpriteEffects.None, 0);

            // 核心
            Main.EntitySpriteDraw(bloom, drawPos, null,
                CoreColor with { A = 0 } * 0.85f, 0f, bloom.Size() * 0.5f, scale * 0.8f, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
