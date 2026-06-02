namespace CalamityRangerExpansion.Content.Ammunition.CPreMoodLord.AstralBullet
{
    public class AstralBulletPROJ : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectile.CPreMoodLord";
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
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.MaxUpdates = 5;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            // 由于我们是水平贴图，因此什么也不需要转动
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.005f;
            // 添加光效
            Lighting.AddLight(Projectile.Center, Color.Lerp(Color.Blue, Color.AliceBlue, 0.5f).ToVector3() * 0.49f);

            // 子弹在出现之后很短一段时间会变得可见
            if (Projectile.timeLeft == 296)
                Projectile.alpha = 0;

            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                // 每帧生成两个粒子，释放频率翻倍
                for (int k = 0; k < 2; k++)
                {
                    int randomDust = Utils.SelectRandom(Main.rand, new int[]
                    {
            ModContent.DustType<AstralOrange>(),
            ModContent.DustType<AstralBlue>()
                    });

                    // 创建粒子特效
                    int astral = Dust.NewDust(Projectile.position, 1, 1, randomDust, 0f, 0f, 0, default, Main.rand.NextFloat(0.5f, 0.75f)); // 调整大小范围
                    Main.dust[astral].alpha = Projectile.alpha;

                    // 设置粒子的初始速度（前后随机偏移）
                    Vector2 initialVelocity = Projectile.velocity * Main.rand.NextFloat(-0.04f, 0.04f); // 随机前后偏移速度
                    Main.dust[astral].velocity += initialVelocity;

                    // 禁用重力
                    Main.dust[astral].noGravity = true;

                    // 设置旋转效果和逐渐消失
                    Main.dust[astral].fadeIn = 0.1f; // 逐渐淡入
                    Main.dust[astral].rotation += Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4); // 随机初始旋转
                }
            }
        }

        public override void OnSpawn(IEntitySource source)
        {

        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {

        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralBulletEBuff>(), 150); // 射击星星的效果仅持续1.5秒
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);
        }
        public override void OnKill(int timeLeft)
        {
            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                int particleCount = Main.rand.Next(10, 16); // 生成 x 到 y 个粒子
                for (int i = 0; i < particleCount; i++)
                {
                    float rotationOffset = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4); // 随机旋转 -45 到 45 度
                    Vector2 direction = Projectile.velocity.RotatedBy(rotationOffset).SafeNormalize(Vector2.Zero);
                    int randomDust = Utils.SelectRandom(Main.rand, new int[]
                    {
        ModContent.DustType<AstralOrange>(),
        ModContent.DustType<AstralBlue>()
                    });
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, randomDust, direction * Main.rand.NextFloat(3f, 6f), 0, default, 1f);
                    dust.noGravity = true;
                    dust.scale = 1.2f + Main.rand.NextFloat(0.3f); // 粒子大小有一定随机性
                }
            }
        }
    }
}
