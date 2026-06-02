namespace CalamityRangerExpansion.Content.Arrows.BPrePlantera.StarblightSootArrow
{
    public class StarblightSootArrowPROJ : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityRangerExpansion/Content/Arrows/BPrePlantera/StarblightSootArrow/StarblightSootArrow";

        public new string LocalizationCategory => "Projectile.BPrePlantera";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                // 画残影效果
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
                return false;
            }
            return true;
        }
        public override void SetDefaults()
        {
            // 设置弹幕的基础属性
            Projectile.width = 11; // 弹幕宽度
            Projectile.height = 24; // 弹幕高度
            Projectile.friendly = true; // 对敌人有效
            Projectile.DamageType = DamageClass.Ranged; // 远程伤害类型
            Projectile.penetrate = 1; // 穿透力为1，击中一个敌人就消失
            Projectile.timeLeft = 600; // 弹幕存在时间为600帧
            Projectile.usesLocalNPCImmunity = true; // 弹幕使用本地无敌帧
            Projectile.localNPCHitCooldown = 14; // 无敌帧冷却时间为14帧
            Projectile.ignoreWater = true; // 弹幕不受水影响
            Projectile.arrow = true;
            Projectile.extraUpdates = 1;
        }
        public override void OnSpawn(IEntitySource source)
        {
            // 定义随机生成角度的范围
            float minAngle = -20f; // 左偏最大角度
            float maxAngle = 20f;  // 右偏最大角度

            // 随机生成三个角度
            float[] randomAngles = new float[3];
            for (int i = 0; i < 3; i++)
            {
                randomAngles[i] = Main.rand.NextFloat(minAngle, maxAngle); // 生成 [-20, 20] 的随机角度
            }

            // 遍历随机角度，生成特效弹幕
            foreach (float angle in randomAngles)
            {
                // 计算旋转后的速度向量，飞行速度为主弹幕速度的 0.5x 到 1.5x
                Vector2 sparkVelocity = Projectile.velocity.RotatedBy(MathHelper.ToRadians(angle)) * Main.rand.NextFloat(0.5f, 1.5f);

                // 释放 StarblightSootArrowSpark
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    sparkVelocity,
                    ModContent.ProjectileType<StarblightSootArrowSpark>(),
                    (int)(Projectile.damage * 0.075f),// 伤害
                    Projectile.knockBack,
                    Projectile.owner
                );
            }
        }


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 添加Buff
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300); // 幻星感染
                                                                               // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                LightingBoltsSystem.Spawn_AstralSoulLightsB(target.Center + Main.rand.NextVector2Circular(16f, 16f));
            }

            #region 旧代码

            //// 360度中随机选择三个角度释放Spark弹幕
            //for (int i = 0; i < 3; i++)
            //{
            //    float randomAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            //    Vector2 sparkVelocity = new Vector2((float)Math.Cos(randomAngle), (float)Math.Sin(randomAngle)) * 10f; // 自定义速度
            //    Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, sparkVelocity, ModContent.ProjectileType<StarblightSootArrowSpark>(), (int)(damageDone * 0.5f), hit.Knockback, Projectile.owner);
            //}

            #endregion
        }

        public override void AI()
        {
            // 调整弹幕的旋转，使其在飞行时保持水平
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

            // Lighting - 添加天蓝色光源，光照强度为 0.49
            Lighting.AddLight(Projectile.Center, Color.LightSkyBlue.ToVector3() * 0.49f);

            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                // 添加橙色、蓝色、白色和黑色的Dust粒子效果
                int[] dustColors = { DustID.OrangeTorch, DustID.BlueTorch, DustID.WhiteTorch, DustID.CrimsonTorch };
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustPosition = Projectile.position + new Vector2(Main.rand.NextFloat(Projectile.width), Main.rand.NextFloat(Projectile.height));
                    Dust dust = Dust.NewDustPerfect(dustPosition, dustColors[Main.rand.Next(dustColors.Length)]);
                    dust.noGravity = true; // 滞留粒子效果
                    dust.velocity *= 0.1f; // 让粒子减缓运动
                }
            }
              
        }

        public override void OnKill(int timeLeft)
        {
            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                // 定义Dust颜色数组
                int[] dustColors = { DustID.OrangeTorch, DustID.BlueTorch, DustID.WhiteTorch, DustID.CrimsonTorch };

                // 获取弹幕的方向向量并计算偏转角度
                Vector2 direction = -Projectile.velocity.SafeNormalize(Vector2.Zero); // 取反向，即正后方
                float angleOffset = MathHelper.ToRadians(15); // 15度偏移角

                // 向左偏转15度
                Vector2 leftDirection = direction.RotatedBy(-angleOffset);
                SpawnDustParticles(leftDirection, dustColors);

                // 向右偏转15度
                Vector2 rightDirection = direction.RotatedBy(angleOffset);
                SpawnDustParticles(rightDirection, dustColors);
            }
        }

        // 辅助方法用于生成粒子特效
        private void SpawnDustParticles(Vector2 direction, int[] dustColors)
        {
            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                int numberOfParticles = 10; // 自定义粒子数量
                for (int i = 0; i < numberOfParticles; i++)
                {
                    Vector2 dustVelocity = direction * Main.rand.NextFloat(8f, 12f); // 随机化速度，确保粒子快速移动
                    Vector2 dustPosition = Projectile.position + new Vector2(Main.rand.NextFloat(Projectile.width), Main.rand.NextFloat(Projectile.height));
                    Dust dust = Dust.NewDustPerfect(dustPosition, dustColors[Main.rand.Next(dustColors.Length)]);
                    dust.velocity = dustVelocity;
                    dust.noGravity = true; // 让粒子悬浮效果更明显
                }
            }
        }
    }
}