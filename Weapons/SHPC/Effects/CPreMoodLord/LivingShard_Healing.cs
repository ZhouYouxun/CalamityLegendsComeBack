using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Particles;
using System;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class LivingShard_Healing : ModProjectile
    {
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
            Projectile.timeLeft = 420;
        }

        // ===== 永远不能造成伤害 =====
        public override bool? CanDamage() => false;

        public override void AI()
        {
            timer++;

            Player player = Main.player[Projectile.owner];

            // ===== 追踪玩家 =====
            if (timer > 20) // 略微延迟一下再追踪（保留原逻辑节奏）
            {
                Vector2 dir = (player.Center - Projectile.Center).SafeNormalize(Vector2.Zero);

                // 平滑追踪（完全继承原逻辑感觉） :contentReference[oaicite:1]{index=1}
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 20f, 0.08f);

                // ===== 碰到玩家 → 自毁 =====
                if (Projectile.Hitbox.Intersects(player.Hitbox))
                {
                    Projectile.Kill();
                }
            }

            // ===== 视觉：持续绿色粒子 =====
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 2; i++)
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
            Player player = Main.player[Projectile.owner];

            // ===== 回血（完全继承逻辑）=====
            int healAmount = Main.rand.Next(2, 6);
            player.statLife += healAmount;
            player.HealEffect(healAmount);

            // ===== 小型爆散特效 =====
            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;

                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 5f);

                SquishyLightParticle particle = new(
                    Projectile.Center,
                    vel,
                    1.2f,
                    Color.Lerp(Color.LimeGreen, Color.White, 0.3f),
                    18
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }
    }
}