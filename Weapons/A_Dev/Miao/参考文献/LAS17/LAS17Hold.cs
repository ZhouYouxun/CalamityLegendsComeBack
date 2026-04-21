namespace CalamityRangerExpansion.Content.DeveloperItems.Weapon.HD2.LAS17
{
    internal class LAS17Hold : BaseGunHoldoutProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "DeveloperItems.LAS17";
        public override string Texture => "CalamityRangerExpansion/Content/DeveloperItems/Weapon/HD2/LAS17/LAS17";
        public override int AssociatedItemID => ModContent.ItemType<LAS17>();
        public override Vector2 GunTipPosition => Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * (Projectile.width * 0.5f + 5f);
        public override float MaxOffsetLengthFromArm => 55f;

        private int frameCounter = 0;
        private int stage = 0;
        private int stageTimer = 0;
        private const int MaxStage = 5;
        private const int StageUpTime = 180; // 每180帧（3秒）升级一次
        private float chargeProgress = 0f;
        private int spawnDelay = 15;

        private int stageOutlineTimer = 0;
        private const int StageOutlineDuration = 24; // 总时长（线性上升 + 下降）
        //public override void OnSpawn(IEntitySource source)
        //{
        //    base.OnSpawn(source);

        //    // 仅缩放显示尺寸，不影响任何逻辑与碰撞
        //    Projectile.scale = 1.00f;
        //}


        public override void HoldoutAI()
        {
            Player player = Main.player[Projectile.owner];

            // =========================
            // 启动缓冲期（15 帧不攻击）
            // =========================
            if (spawnDelay > 0)
            {
                // 第 1 帧：启动音效 + 收缩光环 + 内收辉光点
                if (spawnDelay == 15)
                {
                    // 启动音效
                    SoundEngine.PlaySound(
                        new SoundStyle("CalamityRangerExpansion/Content/DeveloperItems/Weapon/HD2/LAS17/LAS17启动音效") with { Volume = 7.0f, Pitch = 0.0f },
                        Projectile.Center
                    );

                    //// =========================
                    //// ① 往内收缩的圆形冲击波（核心仪式感）
                    //// =========================
                    //Particle shrinkingPulse = new DirectionalPulseRing(
                    //    GunTipPosition,
                    //    Vector2.Zero,              // 静止，仅做缩放
                    //    Color.Purple,              // 紫色能量环
                    //    new Vector2(1f, 1f),       // 圆形
                    //    Main.rand.NextFloat(6f, 10f), // 初始半径（大）
                    //    0.15f,                     // 最终收缩到很小
                    //    3f,                        // 扩散范围
                    //    10                          // 生命周期
                    //);
                    //GeneralParticleHandler.SpawnParticle(shrinkingPulse);

                    //// =========================
                    //// ② 辉光球：向内坠落的能量点（少量但有秩序）
                    //// =========================
                    //int orbCount = 6;
                    //for (int i = 0; i < orbCount; i++)
                    //{
                    //    // 在一个小圆环上生成，然后“看起来像往中心塌缩”
                    //    Vector2 offset = Main.rand.NextVector2CircularEdge(18f, 18f);

                    //    GlowOrbParticle orb = new GlowOrbParticle(
                    //        GunTipPosition + offset, // 起始在外圈
                    //        -offset * 0.15f,            // 速度指向中心（内收）
                    //        false,
                    //        5,
                    //        Main.rand.NextFloat(0.85f, 1.05f),
                    //        Color.Red,
                    //        true,
                    //        false,
                    //        true
                    //    );
                    //    GeneralParticleHandler.SpawnParticle(orb);
                    //}
                }


                spawnDelay--;
                return;
            }

            // 若未持有该武器，则重置
            if (player.HeldItem.type != AssociatedItemID)
            {
                ResetStage();
                return;
            }

            frameCounter++;
            stageTimer++;

            // 射击逻辑
            if (frameCounter >= 5)
            {
                Fire(player);
                frameCounter = 0;
            }

            // 强化逻辑：3秒一级，最多5级
            if (stage < MaxStage && stageTimer >= StageUpTime)
            {
                stage++;
                stageTimer = 0;
                TriggerStageEffect(player);
            }

            // 后坐力模拟（简单推后）
            OffsetLengthFromArm -= 2f;

            // 更新阶段充能进度（满级后直接保持满值）
            if (stage < MaxStage)
                chargeProgress = stageTimer / (float)StageUpTime;
            else
                chargeProgress = 1f;

        }
        public override bool PreDraw(ref Color lightColor)
        {
            // ===== 阶段升级描边脉冲 =====
            if (stageOutlineTimer > 0)
            {
                Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;

                // 0 → 1 → 0 的线性曲线
                float half = StageOutlineDuration / 2f;
                float t = stageOutlineTimer > half
                    ? (StageOutlineDuration - stageOutlineTimer) / half
                    : stageOutlineTimer / half;

                float outlineStrength = MathHelper.Lerp(0f, 1.4f, t);

                Color outlineColor = Color.OrangeRed * 0.6f;

                Vector2 origin = tex.Size() * 0.5f;
                Vector2 basePos = Projectile.Center - Main.screenPosition;
                float rot = Projectile.rotation + ((Projectile.spriteDirection == -1) ? MathF.PI : 0f);
                SpriteEffects fx =
                    ((float)Projectile.spriteDirection * Owner.gravDir == -1f)
                        ? SpriteEffects.FlipHorizontally
                        : SpriteEffects.None;

                // 画 4 个方向的描边（十字）
                Vector2[] offsets =
                {
            new Vector2( outlineStrength, 0),
            new Vector2(-outlineStrength, 0),
            new Vector2(0,  outlineStrength),
            new Vector2(0, -outlineStrength),
        };

                foreach (var off in offsets)
                {
                    Main.EntitySpriteDraw(
                        tex,
                        basePos + off,
                        null,
                        outlineColor,
                        rot,
                        origin,
                        Projectile.scale * Owner.gravDir,
                        fx
                    );
                }

                stageOutlineTimer--;
            }


            // 👉我们只要绘制阶段充能条，不影响主视觉
            if (Main.myPlayer == Projectile.owner && stage < MaxStage)
            {
                var barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
                var barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

                Vector2 drawPos = Owner.Center - Main.screenPosition + new Vector2(0, -56f) - barBG.Size() / 1.5f;
                Rectangle frameCrop = new Rectangle(0, 0, (int)(barFG.Width * chargeProgress), barFG.Height);

                float opacity = 1f;
                Color color = Color.Orange;

                Main.spriteBatch.Draw(barBG, drawPos, null, color * opacity, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(barFG, drawPos, frameCrop, color * opacity * 0.8f, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
            }

            return base.PreDraw(ref lightColor);
        }


        public override void OnKill(int timeLeft)
        {
            ResetStage();
        }

        private void ResetStage()
        {
            stage = 0;
            stageTimer = 0;
        }

        private void Fire(Player player)
        {
            //Vector2 direction = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);// 瞄准鼠标，这不对
            Vector2 direction =
                Vector2.UnitX.RotatedBy(Projectile.rotation)
                    .RotatedBy(Main.rand.NextFloat(
                        -MathHelper.ToRadians(2f),
                         MathHelper.ToRadians(2f)
                    ));

            int proj = ModContent.ProjectileType<LAS17Proj>();
            int damage = Projectile.damage;
            float knockback = Projectile.knockBack;

            // 创建子弹并传入当前阶段
            int index = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                direction * 25f,
                proj,
                damage,
                knockback,
                player.whoAmI
            );

            if (Main.projectile.IndexInRange(index))
            {
                Projectile p = Main.projectile[index];
                if (p.ModProjectile is LAS17Proj spike)
                {
                    spike.WeaponStage = stage;
                }
            }

            // 播放基础音效
            SoundEngine.PlaySound(
                new SoundStyle("CalamityRangerExpansion/Content/DeveloperItems/Weapon/HD2/LAS17/LAS17开火音效") with { Volume = 3.0f, Pitch = 0.0f },
                Projectile.Center
            );


            // 3级及以上 → 在玩家身上制造橙色旋转重烟，模拟灼烧
            if (stage >= 3)
            {
                for (int i = 0; i < 2; i++)
                {
                    Particle burnSmoke = new HeavySmokeParticle(
                        player.Center + new Vector2(0, -6),
                        new Vector2(0, -1).RotatedByRandom(MathHelper.ToRadians(45f)) * Main.rand.NextFloat(3f, 6f),
                        Color.Orange,
                        30,
                        Main.rand.NextFloat(0.9f, 1.4f),
                        1f,
                        MathHelper.ToRadians(Main.rand.NextFloat(-3f, 3f)),
                        true
                    );
                    GeneralParticleHandler.SpawnParticle(burnSmoke);
                }
            }

            if (stage >= 5)
            {
                LAS17PDebuff.FireMode = 2;
                player.AddBuff(ModContent.BuffType<LAS17PDebuff>(), 300); // 5秒刷新
            }
            else if (stage >= 3)
            {
                LAS17PDebuff.FireMode = 1;
                player.AddBuff(ModContent.BuffType<LAS17PDebuff>(), 300); // 5秒刷新
            }


            // 5级 → 加强开火视觉（10倍爆量的橙红粒子）
            if (stage >= 5)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        GunTipPosition,
                        DustID.Torch,
                        direction.RotatedByRandom(0.6f) * Main.rand.NextFloat(2f, 6f),
                        150,
                        Color.OrangeRed,
                        Main.rand.NextFloat(1.2f, 2f)
                    );
                    dust.noGravity = true;
                }
            }

            // 后坐力模拟（更强烈）
            OffsetLengthFromArm -= stage >= 5 ? 4f : 2f;
        }

        private void TriggerStageEffect(Player player)
        {
            stageOutlineTimer = StageOutlineDuration;

            Vector2 fireDirection = Vector2.UnitX.RotatedBy(Projectile.rotation);

            // 🔥1. 火把 Dust 粒子：喷射向前
            for (int i = 0; i < 25; i++)
            {
                Vector2 velocity = fireDirection.RotatedByRandom(MathHelper.ToRadians(15)) * Main.rand.NextFloat(6f, 12f);
                Dust dust = Dust.NewDustPerfect(GunTipPosition, DustID.Torch, velocity, 150, Color.Orange, Main.rand.NextFloat(1.2f, 2f));
                dust.noGravity = true;
            }

            // ⚡2. Spark 橙色粒子：速度更快更亮
            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkVel = fireDirection.RotatedByRandom(MathHelper.ToRadians(10)) * Main.rand.NextFloat(8f, 16f);
                Particle spark = new SparkParticle(
                    GunTipPosition,
                    sparkVel,
                    false,
                    30,
                    1.2f,
                    Color.Orange
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // 💥3. 音效更清脆强烈（替代旧烟雾音）
            SoundEngine.PlaySound(SoundID.Item14.WithPitchOffset(0.15f), GunTipPosition);
        }












    }
}
