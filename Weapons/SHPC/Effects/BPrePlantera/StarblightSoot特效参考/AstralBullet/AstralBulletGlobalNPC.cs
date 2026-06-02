namespace CalamityRangerExpansion.Content.Ammunition.CPreMoodLord.AstralBullet
{
    public class AstralBulletGlobalNPC : GlobalNPC
    {
        public bool hasAstralBulletBuff;
        public int cometTimer;
        private int cachedAstralBulletDamage;
        private float currentRadius = 50f * 16f; // 初始半径，最大值
        private const float minRadius = 5f * 16f; // 最小半径
        private const float shrinkRate = (50f * 16f - minRadius) / 180f; // 每帧缩小的速率，基于 Buff 持续时间 180 帧

        public override bool InstancePerEntity => true;

        public override void ResetEffects(NPC npc)
        {
            hasAstralBulletBuff = false;
        }

        public override void AI(NPC npc)
        {
            if (hasAstralBulletBuff)
            {
                // 动态调整半径（缩小到最小半径后停止缩小）
                if (currentRadius > minRadius)
                {
                    currentRadius -= shrinkRate;
                    if (currentRadius < minRadius)
                    {
                        currentRadius = minRadius; // 确保不会低于最小值
                    }
                }

                // 调整彗星生成频率
                float timePercentage = Math.Max((currentRadius - minRadius) / (50f * 16f - minRadius), 0f); // 动态时间比例
                float dynamicFrequencyMultiplier = 2f - timePercentage; // 频率范围 100% ~ 200%
                int cometSpawnRate = (int)(5 / dynamicFrequencyMultiplier); // 动态间隔

                // 更新粒子生成逻辑
                GenerateParticles(npc, currentRadius);

                // 彗星生成逻辑
                cometTimer++;
                if (cometTimer >= cometSpawnRate)
                {
                    cometTimer = 0;

                    // 调用player文件获取最后一次存在的子弹的伤害
                    Player player = Main.player[npc.target];
                    var astralBulletPlayer = player.GetModPlayer<AstralBulletPlayer>();
                    cachedAstralBulletDamage = (int)((astralBulletPlayer.LastAstralBulletDamage) * 0.1); // 仅造成2.5%的伤害

                    SummonComet(npc, currentRadius); // 使用动态半径
                }
            }
            else
            {
                // 如果 Buff 消失，则重置半径和计时器
                currentRadius = 50f * 16f;
                cometTimer = 0;
            }
        }

        private void GenerateParticles(NPC npc, float dynamicRadius)
        {
            Vector2 npcCenter = npc.Center;

            for (int i = 0; i < 18; i++) // 每帧生成18个粒子
            {
                // 随机生成一个角度
                float randomAngle = Main.rand.NextFloat(MathHelper.TwoPi);

                // 计算粒子的生成位置
                Vector2 spawnPosition = npcCenter + dynamicRadius * new Vector2((float)Math.Cos(randomAngle), (float)Math.Sin(randomAngle));

                // 随机选择颜色：AstralOrange 或 AstralBlue
                int randomDust = Utils.SelectRandom(Main.rand, new int[]
                {
                    ModContent.DustType<AstralOrange>(),
                    ModContent.DustType<AstralBlue>()
                });

                // 创建粒子，设置随机速度和旋转
                Dust particle = Dust.NewDustPerfect(spawnPosition, randomDust, Vector2.Zero, 0, default, 1.2f);
                particle.velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(0.5f, 1.5f); // 随机旋转速度
                particle.noGravity = true;
                particle.fadeIn = 1.5f; // 粒子逐渐消失
            }
        }

        private void SummonComet(NPC npc, float dynamicRadius)
        {
            Player player = Main.player[npc.target];
            Vector2 targetPosition = npc.Center;

            float randomAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 spawnPosition = targetPosition + dynamicRadius * new Vector2((float)Math.Cos(randomAngle), (float)Math.Sin(randomAngle));

            Vector2 direction = targetPosition - spawnPosition;
            direction.Normalize();
            float arrowSpeed = 10f;
            float speedMultiplier = 0.5f; // 设置一个加速倍数，专门加速初始速度的倍率
            float speedX = direction.X * arrowSpeed * 3f * speedMultiplier + Main.rand.Next(-120, 121) * 0.01f;
            float speedY = direction.Y * arrowSpeed * 3f * speedMultiplier + Main.rand.Next(-120, 121) * 0.01f;

            int newDamage2 = cachedAstralBulletDamage; // 使用缓存的 AstralBullet 伤害值
            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPosition, new Vector2(speedX, speedY), ModContent.ProjectileType<AstralBulletSTAR>(), newDamage2, 0, player.whoAmI);
        }
    }
}