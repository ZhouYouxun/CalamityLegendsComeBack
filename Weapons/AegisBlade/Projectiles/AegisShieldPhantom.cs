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
    // 右键松开时释放的盾牌幻影。
    // ai[0] = 0:普通幻影 / 1:强化幻影（完美格挡或蓄力完成）
    // 强化版：更大、更有力量感，消失前释放椭圆粒子环（蘑菇云效果）
    public class AegisShieldPhantom : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/庇护盾牌";

        private static readonly Color GoldColor  = new(255, 200, 60);
        private static readonly Color BrightGold = new(255, 235, 140);
        private static readonly Color PowerGold  = new(255, 255, 180);

        private bool IsEnhanced => Projectile.ai[0] > 0.5f;
        private float DisplayScale    => IsEnhanced ? 2.4f : 1.5f;
        private float KnockbackMult   => IsEnhanced ? 3.2f : 1.5f;
        private int   Lifetime        => IsEnhanced ? 34 : 22;
        private float HitRadius       => IsEnhanced ? 175f : 110f;

        private ref float Timer    => ref Projectile.ai[1];
        private float Alpha        = 1f;
        private bool burstFired    = false;   // 蘑菇云爆炸只触发一次

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 70;
            Projectile.friendly    = true;
            Projectile.DamageType  = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate   = -1;
            Projectile.timeLeft    = 45;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity   = true;
            Projectile.localNPCHitCooldown    = -1;
        }

        public override void AI()
        {
            Timer++;
            float progress = Timer / Lifetime;

            // 前半段向前飞冲
            if (progress < 0.55f)
            {
                float speedMult = MathHelper.SmoothStep(1f, 0f, progress / 0.55f);
                Projectile.position += Projectile.velocity * speedMult;
            }

            // 淡出
            Alpha = MathHelper.SmoothStep(1f, 0f, MathHelper.Clamp((progress - 0.35f) / 0.65f, 0f, 1f));
            Projectile.Opacity = Alpha;

            // 强化版：消失前几帧释放蘑菇云爆炸
            if (IsEnhanced && !burstFired && Timer >= Lifetime - 8)
            {
                burstFired = true;
                EmitMushroomCloudBurst();
            }

            if (Timer >= Lifetime) Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float unused = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - Projectile.velocity * 4f,
                Projectile.Center + Projectile.velocity * 4f,
                HitRadius, ref unused);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.Knockback *= KnockbackMult;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ) return;

            int dustCount = IsEnhanced ? 18 : 10;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 10f);
                Dust dust = Dust.NewDustPerfect(target.Center, DustID.GoldFlame, vel, 0,
                    Main.rand.NextBool(2) ? BrightGold : GoldColor, 1.3f);
                dust.noGravity = true;
            }
        }

        private void EmitMushroomCloudBurst()
        {
            if (Main.dedServ) return;

            SoundEngine.PlaySound(
                SoundID.DD2_WitherBeastCrystalImpact with { Volume = 1f, Pitch = 0.2f },
                Projectile.Center);

            // 椭圆粒子环（蘑菇云底部）
            for (int i = 0; i < 26; i++)
            {
                float angle = MathHelper.TwoPi * i / 26f;
                // 椭圆：水平宽，垂直窄
                Vector2 dir = new Vector2(MathF.Cos(angle) * 2.2f, MathF.Sin(angle) * 0.55f);
                Vector2 vel = dir * Main.rand.NextFloat(6f, 14f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, vel,
                    "CalamityMod/Particles/Sparkle", false,
                    Main.rand.Next(20, 32),
                    Main.rand.NextFloat(1.0f, 1.8f) * (DisplayScale / 2.4f),
                    Main.rand.NextBool(2) ? BrightGold : PowerGold,
                    new Vector2(0.25f, 1.3f), true, true, shrinkSpeed: 0.10f));
            }

            // 向上升腾的蘑菇柱粒子
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(8f, 22f));
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), vel, false,
                    Main.rand.Next(16, 28), 0.06f * (DisplayScale / 2.4f),
                    Main.rand.NextBool(2) ? GoldColor : BrightGold,
                    new Vector2(0.5f, 1.5f), true, false, 0.9f));
            }

            // 中心冲击波
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, BrightGold,
                new Vector2(2.4f, 1.2f), 0f, 0.06f, 2.2f, 22));

            // 白光核心
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, PowerGold,
                Vector2.One, 0f, 0.08f, 1.0f, 14));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Alpha <= 0.02f) return false;

            Texture2D shieldTex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPos     = Projectile.Center - Main.screenPosition;
            float rotation      = Projectile.velocity.ToRotation();
            float currentScale  = DisplayScale;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 外描边光晕（金色轮廓）
            int outlineCount = IsEnhanced ? 10 : 7;
            for (int i = 0; i < outlineCount; i++)
            {
                float angle  = MathHelper.TwoPi * i / outlineCount;
                float offset = IsEnhanced ? 6f : 3.5f;
                Vector2 off  = angle.ToRotationVector2() * offset;
                Main.EntitySpriteDraw(shieldTex, drawPos + off, null,
                    GoldColor with { A = 0 } * Alpha * (IsEnhanced ? 0.4f : 0.28f),
                    rotation, shieldTex.Size() * 0.5f, currentScale, SpriteEffects.None, 0);
            }

            // 强化版额外白色外层
            if (IsEnhanced)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angle = MathHelper.TwoPi * i / 6f + Main.GlobalTimeWrappedHourly * 3f;
                    Vector2 off = angle.ToRotationVector2() * 10f;
                    Main.EntitySpriteDraw(shieldTex, drawPos + off, null,
                        PowerGold with { A = 0 } * Alpha * 0.18f,
                        rotation, shieldTex.Size() * 0.5f, currentScale * 1.1f, SpriteEffects.None, 0);
                }
            }

            // 盾牌本体（金色滤镜，高透明度）
            Main.EntitySpriteDraw(shieldTex, drawPos, null,
                BrightGold with { A = 0 } * Alpha * (IsEnhanced ? 0.7f : 0.55f),
                rotation, shieldTex.Size() * 0.5f, currentScale, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
