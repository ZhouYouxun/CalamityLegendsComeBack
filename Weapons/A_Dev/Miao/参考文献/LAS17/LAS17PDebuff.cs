namespace CalamityRangerExpansion.Content.DeveloperItems.Weapon.HD2.LAS17
{
    internal class LAS17PDebuff : ModBuff, ILocalizedModType
    {
        public new string LocalizationCategory => "DeveloperItems.LAS17";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        // 通过这个字段来控制当前使用的是哪种模式（1：默认，2：高烈度）
        public static int FireMode = 1;

        public override void Update(Player player, ref int buffIndex)
        {
            // 不要刷新 buffTime，由外部控制持续时间
            // player.buffTime[buffIndex] = 2;

            // 只在特定帧执行伤害：普通模式每5帧执行一次
            bool shouldLoseLife = FireMode >= 2 || Main.GameUpdateCount % 5 == 0;

            if (shouldLoseLife)
            {
                player.statLife -= 1;
                player.lifeRegen = 0;
                player.lifeRegenTime = 0;

                if (player.statLife <= 0)
                {
                    player.KillMe(PlayerDeathReason.ByCustomReason($"{player.name} 忘了更换散热器"), 10.0, 0);
                }
            }


            // 粒子表现
            Vector2 center = player.Center + Main.rand.NextVector2Circular(20f, 20f);
            Vector2 velocity = (player.Center - center).SafeNormalize(Vector2.Zero) * 1.2f;

            // 1️⃣ Spark核心粒子
            Particle spark = new SparkParticle(
                center,
                velocity,
                false,
                40,
                FireMode >= 2 ? 1.5f : 1.0f,
                Color.OrangeRed
            );
            GeneralParticleHandler.SpawnParticle(spark);

            // 2️⃣ CritSpark 闪光环绕
            if (Main.GameUpdateCount % 3 == 0)
            {
                int count = FireMode >= 2 ? 16 : 8;
                float speed = FireMode >= 2 ? 4f : 2.5f;

                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi * i / count;
                    Vector2 critVel = angle.ToRotationVector2() * speed;

                    CritSpark crit = new CritSpark(
                        center,
                        critVel,
                        Color.Orange,
                        Color.Yellow,
                        0.8f,
                        25
                    );
                    GeneralParticleHandler.SpawnParticle(crit);
                }
            }

            // 3️⃣ 轻型橙烟
            Particle smoke = new HeavySmokeParticle(
                center,
                velocity * 0.4f,
                new Color(255, 120, 0, 100),
                30,
                Main.rand.NextFloat(0.6f, 1.2f),
                FireMode >= 2 ? 0.6f : 0.3f,
                Main.rand.NextFloat(-0.1f, 0.1f),
                false
            );
            GeneralParticleHandler.SpawnParticle(smoke);

            // 4️⃣ 随机Dust火花
            if (Main.rand.NextBool(FireMode >= 2 ? 1 : 3))
            {
                Dust d = Dust.NewDustPerfect(
                    center,
                    DustID.Torch,
                    velocity * 0.3f,
                    100,
                    Color.Orange,
                    Main.rand.NextFloat(0.8f, 1.4f)
                );
                d.noGravity = true;
            }

            // 5️⃣ 可选 Bloom 光晕（仅高烈度启用）
            if (FireMode >= 2 && Main.rand.NextBool(3))
            {
                Particle bloom = new GenericBloom(
                    center,
                    Vector2.Zero,
                    Color.OrangeRed * 0.4f,
                    1.1f,
                    40
                );
                GeneralParticleHandler.SpawnParticle(bloom);
            }
        }
    }
}
