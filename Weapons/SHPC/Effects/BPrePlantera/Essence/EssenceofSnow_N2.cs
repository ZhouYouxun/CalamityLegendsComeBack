using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using CalamityMod.Particles;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    internal class EssenceofSnow_N2 : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 自定义计数器（禁止用localAI）
        private int timer;
        private float sizeFactor = 1f; // 从1 → 2（200 → 400）
        private int hitCount;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 200;

            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;

            Projectile.penetrate = 15;
            Projectile.timeLeft = 50;

            Projectile.DamageType = DamageClass.Magic;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            timer++;

            {
                // ===== 尺寸线性增长（50帧内 400 → 800）=====
                float progress = Utils.GetLerpValue(0f, 50f, timer, true);
                sizeFactor = MathHelper.Lerp(1f, 2f, progress);

                // 保持中心不变，动态调整大小
                Vector2 center = Projectile.Center;

                Projectile.width = (int)(400 * sizeFactor);
                Projectile.height = (int)(400 * sizeFactor);

                Projectile.Center = center;
            }

            // ===== 轻微前进衰减（液氮停留感）=====
            Projectile.velocity *= 0.8f;



            // ===== 白雾填充（核心）=====
            int spawnCount = 10;

            for (int i = 0; i < spawnCount; i++)
            {
                // 区域内随机点（200×200）

                // 当前半径（跟随动态尺寸）
                float radius = Projectile.width * 0.5f;

                // 均匀圆形采样（不是正方形！）
                Vector2 randomPos = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);

                // 向上+随机扩散
                Vector2 velocity = new Vector2(
                    Main.rand.NextFloat(-1.2f, 1.2f),
                    Main.rand.NextFloat(-6f, -2f)
                );

                // 白雾粒子（来自TauCannon）
                Particle mist = new MediumMistParticle(
                    randomPos,
                    velocity,
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.6f, 1.1f),
                    Main.rand.NextFloat(200f, 300f)
                );

                GeneralParticleHandler.SpawnParticle(mist);
            }

            // ===== 数学扩散环（增强空间感）=====
            if (timer % 6 == 0)
            {
                int amount = 18;

                for (int i = 0; i < amount; i++)
                {
                    float angle = MathHelper.TwoPi / amount * i + timer * 0.03f;

                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * 60f;

                    Vector2 vel = (pos - Projectile.Center).SafeNormalize(Vector2.Zero) * 2f;

                    Particle mist = new MediumMistParticle(
                        pos,
                        vel,
                        Color.White,
                        Color.Transparent,
                        0.7f,
                        180f
                    );

                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }



            // ===== 高亮液氮烟雾（强化版 HeavySmoke）=====
            int smokeCount = 8;

            for (int i = 0; i < smokeCount; i++)
            {
                // 椭圆区域随机（和你原本雾一致）
                float radius = Projectile.width * 0.5f * 0.6f;
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);

                // 上升 + 扩散（比原版更柔一点）
                Vector2 vel = new Vector2(
                    Main.rand.NextFloat(-1.0f, 1.0f),
                    Main.rand.NextFloat(-3.5f, -1.2f)
                );

                // 👉 关键：纯白 + 更亮（接近雾核）
                Color smokeColor = Color.Lerp(Color.White, Color.WhiteSmoke, 0.3f);

                Particle smoke = new HeavySmokeParticle(
                    spawnPos,
                    vel,
                    smokeColor,
                    22,                                 // 生命周期稍微长一点
                    Main.rand.NextFloat(1.2f, 1.8f),     // 更大 → 更“糊”
                    0.55f,                               // 更不透明 → 更浓
                    Main.rand.NextFloat(-0.0f, 0.0f),     // 不要旋转的
                    true                                 // 👉 改成 true：更厚的烟
                );

                GeneralParticleHandler.SpawnParticle(smoke);
            }






            // ===== 生命周期 / 命中次数限制 =====
            if (timer > 90 || hitCount >= 12)
            {
                Projectile.Kill();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitCount++;

            // 冰冻效果（强化控制）
            target.AddBuff(BuffID.Frostburn, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}