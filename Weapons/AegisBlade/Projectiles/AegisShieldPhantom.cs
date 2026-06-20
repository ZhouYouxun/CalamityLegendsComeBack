using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 右键松开后释放的盾牌幻影，向鼠标方向推进并造成高击退伤害。
    // ai[0] = 0:普通幻影 / 1:完美格挡强化版
    public class AegisShieldPhantom : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/庇护盾牌";

        private static readonly Color GoldColor = new(255, 200, 60);
        private static readonly Color BrightGold = new(255, 235, 140);

        private bool IsPerfectParry => Projectile.ai[0] > 0.5f;
        private float Scale => IsPerfectParry ? 2.2f : 1.4f;
        private float KnockbackMult => IsPerfectParry ? 3f : 1.5f;

        private ref float Timer => ref Projectile.ai[1];

        // 幻影飞行时间：移动20帧后消失（强化版32帧）
        private int LifeTime => IsPerfectParry ? 32 : 20;
        private int HitRadius => IsPerfectParry ? 160 : 100;

        private float Alpha = 1f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 40;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            float progress = Timer / LifeTime;

            // 前半段：向前飞行
            if (progress < 0.5f)
                Projectile.position += Projectile.velocity * MathHelper.SmoothStep(1f, 0f, progress * 2f);

            // 后半段：快速消散
            Alpha = MathHelper.SmoothStep(1f, 0f, Math.Max(0f, (progress - 0.4f) / 0.6f));
            Projectile.Opacity = Alpha;

            // 完美格挡：消失前爆发粒子（蘑菇云效果）
            if (IsPerfectParry && Timer == LifeTime - 6)
                EmitMushroomCloudBurst();

            if (Timer >= LifeTime)
                Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float unused = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - Projectile.velocity * 5f,
                Projectile.Center + Projectile.velocity * 5f,
                HitRadius, ref unused);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.Knockback *= KnockbackMult;

            // 如有BOSS锁定目标，已在spawning时处理速度方向
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 8f);
                    Dust dust = Dust.NewDustPerfect(target.Center, DustID.GoldFlame, vel, 0, GoldColor, 1.2f);
                    dust.noGravity = true;
                }
            }
        }

        private void EmitMushroomCloudBurst()
        {
            if (Main.dedServ) return;

            SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 0.9f, Pitch = 0.15f }, Projectile.Center);

            // 椭圆形粒子环（蘑菇云底部）
            for (int i = 0; i < 20; i++)
            {
                float angle = MathHelper.TwoPi * i / 20f;
                Vector2 dir = new Vector2(MathF.Cos(angle) * 2f, MathF.Sin(angle) * 0.5f);
                Vector2 vel = dir * Main.rand.NextFloat(5f, 10f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, vel,
                    "CalamityMod/Particles/Sparkle", false, Main.rand.Next(18, 28),
                    Main.rand.NextFloat(0.8f, 1.4f) * Scale,
                    Main.rand.NextBool(2) ? BrightGold : GoldColor,
                    new Vector2(0.3f, 1.2f), true, true, shrinkSpeed: 0.12f));
            }

            // 中心冲击波
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                BrightGold, new Vector2(2f, 1f), 0f, 0.05f, 1.8f, 20));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Alpha <= 0.02f) return false;

            Texture2D shieldTex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.velocity.ToRotation();

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 金色发光轮廓（外描边）
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Vector2 offset = angle.ToRotationVector2() * (IsPerfectParry ? 5f : 3f);
                Main.EntitySpriteDraw(shieldTex, drawPos + offset, null,
                    GoldColor with { A = 0 } * Alpha * 0.35f,
                    rotation, shieldTex.Size() * 0.5f, Scale, SpriteEffects.None, 0);
            }

            // 盾牌本体（高透明度金色滤镜）
            Main.EntitySpriteDraw(shieldTex, drawPos, null,
                BrightGold with { A = 0 } * Alpha * 0.6f,
                rotation, shieldTex.Size() * 0.5f, Scale, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
