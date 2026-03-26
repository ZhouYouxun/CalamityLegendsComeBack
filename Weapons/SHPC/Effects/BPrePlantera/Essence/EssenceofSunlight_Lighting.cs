using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using CalamityMod.Particles;
using Terraria.Audio;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSunlight_Lighting : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";

        // 透明贴图
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 自定义计数器
        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;

            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.penetrate = 1;
            Projectile.timeLeft = 60;

            Projectile.extraUpdates = 10; // ⚠️ 超高更新频率

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            // ===== 前端高速电弧推进 =====
            Vector2 futurePos = Projectile.Center + Projectile.velocity * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 vel =
                    forward.RotatedByRandom(MathHelper.ToRadians(10f))
                    * Main.rand.NextFloat(6f, 12f);

                Particle spark = new GlowSparkParticle(
                    futurePos,
                    vel,
                    false,
                    8,
                    0.12f,
                    new Color(255, 230, 120),
                    new Vector2(2.4f, 0.35f),
                    true,
                    false,
                    1
                );

                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ===== 后方拖尾 =====
            if (Main.rand.NextBool(2))
            {
                Vector2 backPos = Projectile.Center - forward * 10f;

                Particle trail = new GlowSparkParticle(
                    backPos,
                    -forward * Main.rand.NextFloat(2f, 5f),
                    false,
                    10,
                    0.1f,
                    new Color(255, 200, 80),
                    new Vector2(1.8f, 0.3f),
                    true,
                    false,
                    1
                );

                GeneralParticleHandler.SpawnParticle(trail);
            }

            // ===== 直线锁定（不旋转模型）=====
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool? CanDamage() => timer > 2;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = target.Center;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            // ===== 核心爆闪 =====
            for (int i = 0; i < 12; i++)
            {
                Particle core = new GlowSparkParticle(
                    pos,
                    Main.rand.NextVector2Circular(1f, 1f),
                    false,
                    6,
                    0.2f,
                    new Color(255, 240, 150),
                    new Vector2(1.5f, 0.5f),
                    true,
                    false,
                    1
                );
                GeneralParticleHandler.SpawnParticle(core);
            }

            // ===== 向下冲击流 =====
            for (int i = 0; i < 18; i++)
            {
                Vector2 vel =
                    forward.RotatedByRandom(MathHelper.ToRadians(8f))
                    * Main.rand.NextFloat(8f, 16f);

                Particle jet = new GlowSparkParticle(
                    pos,
                    vel,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.12f, 0.2f),
                    new Color(255, 220, 100),
                    new Vector2(2.8f, 0.4f),
                    true,
                    false,
                    1
                );

                GeneralParticleHandler.SpawnParticle(jet);
            }

            // ===== 闪电音效 =====
            SoundEngine.PlaySound(SoundID.Item94, pos);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}