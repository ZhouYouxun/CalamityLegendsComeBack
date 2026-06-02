namespace CalamityRangerExpansion.Content.Ammunition.BPrePlantera.StarblightSootBullet
{
    internal class StarblightSootBulletPROJ : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectile.BPrePlantera";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
                return false;
            }
            return true;
        }
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 300;
            Projectile.MaxUpdates = 6;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            // 由于我们是水平贴图，因此什么也不需要转动
            Projectile.rotation = Projectile.velocity.ToRotation();

            // 添加光效
            Lighting.AddLight(Projectile.Center, Color.Lerp(Color.Blue, Color.AliceBlue, 0.5f).ToVector3() * 0.49f);

            // 子弹在出现之后很短一段时间会变得可见
            if (Projectile.timeLeft == 296)
                Projectile.alpha = 0;

            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                // 飞行粒子特效
                if (Main.rand.NextBool(5))
                {
                    // 星辉瘟疫的同款小圆圈
                    Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero, Color.DarkTurquoise, new Vector2(1f, 1f), 0f, 0.1f, 0f, 20));
                }
            }

        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);

            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                // 击中敌人释放更多粒子特效
                for (int i = 0; i < 10; i++) // 增加粒子数量
                {
                    // 粒子的起始位置：围绕目标中心随机生成
                    Vector2 position = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);

                    // 计算随机偏移方向
                    float randomAngleOffset = Main.rand.NextFloat(-0.5f, 0.5f); // 随机角度偏移（-0.5到0.5弧度）
                    Vector2 randomVelocity = Projectile.velocity.RotatedBy(randomAngleOffset).SafeNormalize(Vector2.Zero)
                                             * Main.rand.NextFloat(2f, 4f); // 随机速度大小（2到4）

                    // 添加粒子效果
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        position,
                        randomVelocity, // 粒子移动方向和速度
                        Color.Blue,
                        new Vector2(1.5f, 1.5f),
                        0f,
                        0.2f,
                        0f,
                        30));
                }
                LightingBoltsSystem.Spawn_AstralSoulLightsA(target.Center);
            }


            // 给 GlobalNPC 添加标记
            if (!target.boss && target.TryGetGlobalNPC<StarblightSootBulletGlobalNPC>(out var modNPC))
            {
                modNPC.MarkedByBullet = true;
            }
        }


        public override void OnSpawn(IEntitySource source)
        {

        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {

        }

        public override void OnKill(int timeLeft)
        {

        }
    }
}
