namespace CalamityRangerExpansion.Content.DeveloperItems.Weapon.HD2.LAS17
{
    internal class LAS17Proj : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/LaserProj";
        public new string LocalizationCategory => "DeveloperItems.LAS17";

        // 从 Holdout 传入的热量阶段
        public int WeaponStage = 0;

        private bool penetratedSet;
        private int fxCounter;

        // SHPL 同款：偏黄激光
        public override Color? GetAlpha(Color lightColor)
            => new Color(255, 235, 120, 0);

        // 纯 Beam 绘制
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawBeam(200f, 3f, lightColor);
            return false;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 500;
            Projectile.penetrate = 2;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;

            // 🔑 SHPL 核心：出生即不可见
            Projectile.alpha = 255;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // SHPL 同款初始化标记
            Projectile.localAI[0] = 0f;
        }


        public override void AI()
        {          
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 25;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            Lighting.AddLight((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16, 0.5f, 0.2f, 0.5f);
            float timerIncr = 3f;
            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += timerIncr;
                if (Projectile.localAI[0] > 100f)
                {
                    Projectile.localAI[0] = 100f;
                }
            }
            else
            {
                Projectile.localAI[0] -= timerIncr;
                if (Projectile.localAI[0] <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }

            // =========================
            // 穿透只设置一次
            // =========================
            if (!penetratedSet)
            {
                Projectile.penetrate = WeaponStage switch
                {
                    >= 5 => -1,
                    >= 4 => 7,
                    >= 2 => 3,
                    _ => 1
                };
                penetratedSet = true;
            }


            // 只在主更新帧推进特效节奏
            if (Projectile.numUpdates == 0)
                fxCounter++;

            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -dir;

 
            Lighting.AddLight(Projectile.Center, new Color(255, 220, 120).ToVector3() * 0.6f);






            {


                //// =========================
                //// 特效：直线逻辑线（每帧在直线上释放一个辉光点）
                //// =========================
                //{
                //    // 在弹幕后方沿直线均匀铺点
                //    float backDist = 14f; // 点与弹幕中心的固定距离
                //    Vector2 pos = Projectile.Center + back * backDist;

                //    GlowOrbParticle orb = new GlowOrbParticle(
                //        pos,                 // 固定在直线上的位置
                //        Vector2.Zero,        // 不移动
                //        false,               // 不受重力
                //        5,                   // 生命周期短，形成连续线感
                //        0.9f,                // 尺寸
                //        Color.Gold,          // 偏黄
                //        true,                // 加法混合，亮
                //        false,
                //        true
                //    );
                //    GeneralParticleHandler.SpawnParticle(orb);
                //}


            }




        }



        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 高阶段：触发 FuckYou 弹幕（75% 伤害）
            if (WeaponStage >= 3)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<FuckYou>(),
                    (int)(Projectile.damage * 0.75f),
                    Projectile.knockBack,
                    Projectile.owner
                );
            }


            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -dir;

            // =========================
            // ① 有序溅射：扇形反向喷射（主视觉）
            // =========================
            int orderedCount = 12;
            float fanHalfAngle = MathHelper.ToRadians(30f); // 扇形角度
            float baseAngle = back.ToRotation();

            for (int i = 0; i < orderedCount; i++)
            {
                float t = orderedCount == 1 ? 0.5f : i / (float)(orderedCount - 1);
                float angle = baseAngle + MathHelper.Lerp(-fanHalfAngle, fanHalfAngle, t);

                // 中央更强，边缘更弱（有数学秩序）
                float centerWeight = 1f - Math.Abs(t - 0.5f) * 2f;
                float speed = MathHelper.Lerp(3f, 8f, centerWeight);

                Vector2 velocity = angle.ToRotationVector2() * speed;

                Dust d = Dust.NewDustPerfect(
                    target.Center,
                    267, // 火焰 / 能量系 Dust
                    velocity,
                    120,
                    Color.OrangeRed,
                    MathHelper.Lerp(1.0f, 1.6f, centerWeight)
                );
                d.noGravity = true;
            }

            // =========================
            // ② 无序溅射：随机爆散（能量破裂感）
            // =========================
            int chaoticCount = 8;
            for (int i = 0; i < chaoticCount; i++)
            {
                Vector2 velocity =
                    back.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2)) *
                    Main.rand.NextFloat(1.5f, 6f) +
                    Main.rand.NextVector2Circular(1.2f, 1.2f);

                Dust d = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    267,
                    velocity,
                    150,
                    Color.Gold,
                    Main.rand.NextFloat(0.8f, 1.3f)
                );
                d.noGravity = true;
            }
        }



        public override void OnKill(int timeLeft)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -dir;

            // =========================
            // ① 无序扩散：点刺型粒子（向外炸开）
            // =========================
            int chaosCount = 10;
            for (int i = 0; i < chaosCount; i++)
            {
                Vector2 v =
                    back.RotatedBy(Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi)) *
                    Main.rand.NextFloat(2.5f, 6.5f);

                PointParticle spark = new PointParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    v,
                    false,
                    15,
                    Main.rand.NextFloat(0.9f, 1.2f),
                    Color.Orange
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // =========================
            // ② 有序释放：辉光球（稳定、干净的能量残留）
            // =========================
            int orbCount = 6;
            for (int i = 0; i < orbCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(4f, 4f);

                GlowOrbParticle orb = new GlowOrbParticle(
                    Projectile.Center + offset,
                    Vector2.Zero,
                    false,
                    5,
                    0.9f,
                    Color.Red,
                    true,
                    false,
                    true
                );
                GeneralParticleHandler.SpawnParticle(orb);
            }

            // =========================
            // ③ 收尾：细节爆炸（定点、短促、有质感）
            // =========================
            Particle explosion = new DetailedExplosion(
                Projectile.Center,
                Vector2.Zero,
                Color.OrangeRed * 0.9f,
                Vector2.One,
                Main.rand.NextFloat(-5f, 5f),
                0f,
                0.28f,
                10
            );
            GeneralParticleHandler.SpawnParticle(explosion);
        }



    }
}
