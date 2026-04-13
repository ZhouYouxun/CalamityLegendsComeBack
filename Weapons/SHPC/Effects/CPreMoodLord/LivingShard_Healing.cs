using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Particles;
using System;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class LivingShard_Healing : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ===== 自定义计时器（禁止用localAI）=====
        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;

            Projectile.friendly = false; // 不参与伤害
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.penetrate = -1;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 420;
        }

        // ===== 永远不能造成伤害 =====
        public override bool? CanDamage() => false;
        private int targetPlayerIndex = -1; // -1表示未锁定
        public override void AI()
        {
            timer++;

            Player player = Main.player[Projectile.owner];

            // ===== 追踪玩家 =====
            if (timer > 10)
            {
                // ===== 只在第一次锁定目标 =====
                if (targetPlayerIndex == -1)
                {
                    float lowestRatio = 1f;

                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];

                        if (!p.active || p.dead)
                            continue;

                        float ratio = (float)p.statLife / p.statLifeMax2;

                        if (ratio < lowestRatio)
                        {
                            lowestRatio = ratio;
                            targetPlayerIndex = i;
                        }
                    }

                    // 兜底（防止全死）
                    if (targetPlayerIndex == -1)
                        targetPlayerIndex = Projectile.owner;
                }

                Player target = Main.player[targetPlayerIndex];

                Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);

                // 平滑追踪
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 20f, 0.08f);

                // ===== 碰到目标 → 自毁 =====
                if (Projectile.Hitbox.Intersects(target.Hitbox))
                {
                    Projectile.Kill();
                }
            }

            // ===== 视觉：持续绿色粒子 =====
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 1; i++)
            {
                float angle = Main.rand.NextFloat(-0.6f, 0.6f);

                Vector2 velocity = forward.RotatedBy(angle) * Main.rand.NextFloat(1f, 4f);

                float scale = Main.rand.NextFloat(0.6f, 1.2f);

                Color particleColor = Color.Lerp(
                    new Color(120, 255, 120),
                    new Color(60, 200, 120),
                    Main.rand.NextFloat()
                );

                int lifetime = Main.rand.Next(10, 20);

                SquishyLightParticle particle = new(
                    Projectile.Center,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        public override void OnKill(int timeLeft)
        {
            int healAmount = Main.rand.Next(2, 6);
            Player owner = Main.player[Projectile.owner];
            owner.statLife += healAmount;
            owner.HealEffect(healAmount);

            Vector2 center = Projectile.Center;


            // ================= 1.主圆环（放射爆散） =================
            int count = 12;
            float baseSpeedMin = 2f;
            float baseSpeedMax = 5f;

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;

                Vector2 dir = angle.ToRotationVector2();
                Vector2 vel = dir * Main.rand.NextFloat(baseSpeedMin, baseSpeedMax);

                SquishyLightParticle particle = new(
                    center,
                    vel,
                    1.2f,
                    Color.Lerp(Color.LimeGreen, Color.White, 0.3f),
                    18
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // ================= 2.椭圆 Dust 环（X结构） =================
            int dustCount = 24;

            float longAxis = 70f;   // ⭐ 长轴（你可以改）
            float shortAxis = 30f;  // ⭐ 短轴（你可以改）

            float speed = 3f;       // ⭐ 外扩速度

            for (int i = 0; i < dustCount; i++)
            {
                float t = MathHelper.TwoPi * i / dustCount;

                // ===== 椭圆1 =====
                Vector2 ellipse1 = new Vector2(
                    (float)Math.Cos(t) * longAxis,
                    (float)Math.Sin(t) * shortAxis
                );

                // ===== 椭圆2（旋转90°形成X）=====
                Vector2 ellipse2 = ellipse1.RotatedBy(MathHelper.PiOver2);

                // ===== 方向归一化（用于爆散）=====
                Vector2 dir1 = ellipse1.SafeNormalize(Vector2.UnitY);
                Vector2 dir2 = ellipse2.SafeNormalize(Vector2.UnitY);

                Vector2 vel1 = dir1 * speed;
                Vector2 vel2 = dir2 * speed;

                // ===== Dust 1 =====
                Dust d1 = Dust.NewDustPerfect(
                    center + ellipse1,
                    Main.rand.NextBool() ? 107 : 110,
                    vel1,
                    120,
                    Main.rand.NextBool() ? Color.LightGreen : Color.LimeGreen,
                    Main.rand.NextFloat(1.0f, 2.2f)
                );
                d1.noGravity = true;

                // ===== Dust 2 =====
                Dust d2 = Dust.NewDustPerfect(
                    center + ellipse2,
                    Main.rand.NextBool() ? 107 : 110,
                    vel2,
                    120,
                    Main.rand.NextBool() ? Color.LightGreen : Color.LimeGreen,
                    Main.rand.NextFloat(1.0f, 2.2f)
                );
                d2.noGravity = true;
            }
        }



    }
}