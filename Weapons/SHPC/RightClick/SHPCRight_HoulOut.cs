using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityLegendsComeBack.Weapons.SHPC;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal class SHPCRight_HoulOut : RightClickHoldoutBase, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";
        public override int AssociatedItemID => ModContent.ItemType<NewLegendSHPC>();

        public override Vector2 GunTipPosition =>
            Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * 56f;

        public override float MaxOffsetLengthFromArm => 35f;

        #region ===== 基础状态 =====

        private int frameCounter;
        private int spawnDelay = 15;

        #endregion

        #region ===== 阶段系统 =====

        private int stage;
        private int stageTimer;
        private float chargeProgress;

        private float visualProgress; // 用于UI的真实进度（可升可降）

        // ===== 世界进度状态（来自武器）=====
        public int ProgressState; // 0~4

        private int MaxHeatStage;
        private int LaserChainCount;
        private bool AllowManualCool;

        // ===== 每阶段升级所需时间 =====
        private int GetStageUpTime()
        {
            return stage switch
            {
                0 => 120,  // 1级（初始→1）
                1 => 150,
                2 => 180,
                3 => 210,
                4 => 240,
                5 => 270,
                6 => 300,
                _ => 180
            };
        }

        #endregion

        #region ===== 冷却 / 技能控制 =====

        private int reduceCooldown;
        private int fireStopTimer;

        #endregion

        #region ===== 特效状态 =====

        private int stageOutlineTimer;
        private const int StageOutlineDuration = 24;

        #endregion

        #region ===== 枪托位置 =====

        // ===== 枪托位置（可手动调节）=====
        private const float GunBackOffset = 26f;     // 往后距离（核心调这里）
        private const float GunBackUpOffset = 4f;    // 往上微调（类似Tau）

        private Vector2 GunBackPosition =>
            Projectile.Center
            - Vector2.UnitX.RotatedBy(Projectile.rotation) * GunBackOffset
            - Vector2.UnitY.RotatedBy(Projectile.rotation) * GunBackUpOffset;

        #endregion

        #region ===== 额外数据 =====

        private int currentEffectID;

        #endregion

        #region ===== 初始化 =====
        public override void OnSpawn(IEntitySource source)
        {
            ProgressState = (int)Projectile.ai[0];
            currentEffectID = (int)Projectile.ai[1];
            InitializeProgressRules();
        }
        // ===== 根据进度初始化规则 =====
        private void InitializeProgressRules()
        {
            switch (ProgressState)
            {
                default: // 初始
                case 0:
                    MaxHeatStage = 3;
                    LaserChainCount = 1;
                    AllowManualCool = false;
                    break;

                case 1: // 毁灭者
                    MaxHeatStage = 4;
                    LaserChainCount = 2;
                    AllowManualCool = false;
                    break;

                case 2: // 星神游龙
                    MaxHeatStage = 5;
                    LaserChainCount = 3;
                    AllowManualCool = true;
                    break;

                case 3: // 风暴编织者
                    MaxHeatStage = 6;
                    LaserChainCount = 3;
                    AllowManualCool = true;
                    break;

                case 4: // 星流巨械
                    MaxHeatStage = 7;
                    LaserChainCount = 4;
                    AllowManualCool = true;
                    break;
            }
        }

        #endregion

        #region ===== 主AI =====

        public override void HoldoutAI()
        {
            Player player = Main.player[Projectile.owner];

            #region ===== 基础生存判定 =====

            if (!player.active || player.dead || (player.HeldItem.type != AssociatedItemID && player.HeldItem.type != ModContent.ItemType<NewLegendSHPC>()))
            {
                Projectile.Kill();
                return;
            }

            #endregion

            #region ===== 冷却更新 =====

            if (reduceCooldown > 0)
                reduceCooldown--;

            #endregion

            #region ===== 左键监听（核心新增）=====

            if (Main.myPlayer == Projectile.owner &&
                Main.mouseLeft &&
                Main.mouseLeftRelease)
            {
                TryReduceHeat();
            }

            #endregion

            #region ===== 启动阶段 =====

            if (spawnDelay > 0)
            {
                if (spawnDelay == 15)
                    PlayStartupSound();

                spawnDelay--;
                return;
            }

            #endregion

            #region ===== 常规运行 =====

            frameCounter++;
            stageTimer++;

            // ===== 同步热值到玩家（必须每帧写）=====
            player.GetModPlayer<SHPCRight_Player>().HeatStage = stage;

            #endregion

            #region ===== 开火控制 =====

            if (fireStopTimer > 0)
            {
                fireStopTimer--;
                SpawnCoolingVentMist();
            }
            else if (frameCounter >= 5)
            {
                Fire(player);
                frameCounter = 0;
            }

            #endregion

            #region ===== 升级系统 =====

            int stageUpTime = GetStageUpTime();

            if (stage < MaxHeatStage && stageTimer >= stageUpTime)
            {
                stage++;
                stageTimer = 0;
                TriggerStageOutlinePulse();
                SpawnStageUpEnergyBurst();
            }

            #endregion

            #region ===== 后坐力 & 充能 =====

            OffsetLengthFromArm -= 2f;


            float targetProgress;

            // ===== 正常充能 =====
            if (fireStopTimer <= 0)
            {
                if (stage >= MaxHeatStage)
                {
                    targetProgress = 1f; // 满级锁满
                }
                else
                {
                    int stageUpTime1 = GetStageUpTime();
                    targetProgress = stageTimer / (float)stageUpTime1;
                }
            }
            // ===== 手动降温（反向）=====
            else
            {
                float coolTime = 30f; // 用你的 fireStopTimer 最大值
                targetProgress = fireStopTimer / coolTime;
            }

            // ===== 平滑过渡（关键）=====
            visualProgress = MathHelper.Lerp(visualProgress, targetProgress, 0.25f);



            #endregion

            #region ===== 魔力耗尽强制停火 =====
            // 每帧都尝试触发魔力花（关键）
            player.CheckMana(player.HeldItem, 0, true, false);

            //if (player.statMana <= 0)
            //{
            //    // ===== 强制停火 =====
            //    fireStopTimer = Math.Max(fireStopTimer, 2); // 持续停火
            //    frameCounter = 0; // 防止继续触发Fire

            //    //// ===== 枪口向上喷烟 =====
            //    //for (int i = 0; i < 2; i++)
            //    //{
            //    //    Vector2 smokeVel = -Vector2.UnitY.RotatedByRandom(0.25f) * Main.rand.NextFloat(2f, 5f);
            //    //    float smokeScale = Main.rand.NextFloat(0.8f, 1.4f);

            //    //    SmallSmokeParticle smoke = new SmallSmokeParticle(
            //    //        GunTipPosition + Main.rand.NextVector2Circular(6f, 6f),
            //    //        smokeVel,
            //    //        Color.DimGray,
            //    //        Main.rand.NextBool() ? Color.SlateGray : Color.Black,
            //    //        smokeScale,
            //    //        100
            //    //    );

            //    //    GeneralParticleHandler.SpawnParticle(smoke);
            //    //}

            //    return; // ❗直接终止本帧AI（关键）
            //}

            #endregion
        }

        #endregion

        #region ===== 核心技能：降温 =====

        public void TryReduceHeat()
        {
            if (!AllowManualCool || reduceCooldown > 0 || stage <= 0)
                return;

            // ===== 降温音效 =====
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/AWM开火")
                {
                    Volume = 5.2f,
                    Pitch = 0.2f
                },
                GunTipPosition
            );

            stage--;
            stageTimer = 0;
            
            reduceCooldown = 300; // 降温的冷却时长
            fireStopTimer = 60; // 停火时间

            OffsetLengthFromArm -= 18f;

            PlayManualCooldownSound();
            FireCooldownRocketSalvo();
        }


        #endregion

        #region ===== 开火逻辑 =====
        private int spiralCounter;
        private void Fire(Player player)
        {

            if (!player.CheckMana(player.HeldItem, 2, true, false))
            {
                // 蓝不够 → 停火
                fireStopTimer = Math.Max(fireStopTimer, 2);
                frameCounter = 0;
                return;
            }

            int count = LaserChainCount;

            Vector2 fireDirection = Vector2.UnitX.RotatedBy(Projectile.rotation);
            Vector2 baseVelocity = fireDirection * 25f;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity;

                // ===== 单链：基本直线 =====
                if (count == 1)
                {
                    velocity = baseVelocity.RotatedBy(Main.rand.NextFloat(-0.02f, 0.02f));
                }
                // ===== 多链：霰射感 =====
                else
                {
                    // 原本：完全随机【前面的逻辑】
                    //float speedX = baseVelocity.X + Main.rand.Next(-20, 21) * 0.05f;
                    //float speedY = baseVelocity.Y + Main.rand.Next(-20, 21) * 0.05f;
                    //velocity = new Vector2(speedX, speedY);

                    float t = spiralCounter * 0.15f; // 时间参数（调速度）

                    float maxAngle = MathHelper.Pi / 180f * 4f;   // 主扇形 ±X°
                    float waveAngle = MathHelper.Pi / 180f * 1f;  // 螺旋扰动 ±Y°

                    float spread = MathHelper.Lerp(-maxAngle, maxAngle, i / (float)(count - 1));
                    Vector2 baseDir = baseVelocity.RotatedBy(spread);

                    float phase = i * 1.2f;
                    float offset = (float)Math.Sin(t + phase) * waveAngle;

                    velocity = baseDir.RotatedBy(offset);
                }

                int projIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GetSafeFirePosition(player) - fireDirection * 22f,
                    velocity,
                    ModContent.ProjectileType<SHPCRight_Proj>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    player.whoAmI
                );

                if (Main.projectile.IndexInRange(projIndex) &&
                    Main.projectile[projIndex].ModProjectile is SHPCRight_Proj beam)
                {
                    beam.WeaponStage = stage;
                }
            }

            // ===== Stage5+：施加烧伤Debuff =====
            if (stage >= 4)
            {
                player.AddBuff(ModContent.BuffType<SHPCRight_DeBuff>(), 180); // 3秒
            }

            spiralCounter++;
            PlayNormalFireSound();
            SpawnNormalShotMuzzleEffect(player, Vector2.UnitX.RotatedBy(Projectile.rotation));
        }

        #endregion

        #region ===== 开火辅助 =====

        private Vector2 GetSafeFirePosition(Player player)
        {
            Vector2 gunTip = GunTipPosition;

            // ===== 1. 枪口在方块内 =====
            if (Collision.SolidCollision(gunTip, 1, 1))
                return Projectile.Center;

            // ===== 2. 获取最近敌人 =====
            NPC target = null;
            float maxDetect = 300f;
            float closestDist = maxDetect;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float dist = Vector2.Distance(player.Center, npc.Center);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = npc;
                }
            }

            // ===== 3. 距离判定（贴脸）=====
            float dangerRange = 56f; // 和你的枪口长度一致

            if (target != null && closestDist < dangerRange)
                return Projectile.Center;

            // ===== 4. 敌人在枪口前方（防穿脸）=====
            if (target != null)
            {
                float distToPlayer = Vector2.Distance(player.Center, target.Center);
                float distToGunTip = Vector2.Distance(gunTip, target.Center);

                if (distToPlayer < distToGunTip)
                    return Projectile.Center;
            }

            return gunTip;
        }

        #endregion

        #region ===== 绘制 =====

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
            if (Main.myPlayer == Projectile.owner)
            {          
                var barBG = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCBarBack").Value;
                var barFG = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCBarFront").Value;

                Vector2 drawPos = Owner.Center - Main.screenPosition + new Vector2(0, -56f) - barBG.Size() / 1.5f;
                Rectangle frameCrop = new Rectangle(0, 0, (int)(barFG.Width * visualProgress), barFG.Height);

                float opacity = 1f;
                Color color = Color.Orange;

                Main.spriteBatch.Draw(barBG, drawPos, null, color * opacity, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(barFG, drawPos, frameCrop, color * opacity * 0.8f, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
            }

            return base.PreDraw(ref lightColor);
        }

        #endregion

        #region ===== 音效 =====

        private void PlayStartupSound()
        {
            bool zenith = Main.zenithWorld;

            string path = zenith
                ? "CalamityLegendsComeBack/Sound/SHPC/M14拉枪"
                : "CalamityLegendsComeBack/Sound/SHPC/双刃镰启动音效";

            SoundStyle style = new SoundStyle(path)
            {
                Volume = zenith ? 1.2f : 1f,
                Pitch = zenith ? 0.1f : 0f
            };

            SoundEngine.PlaySound(style, Projectile.Center);
        }

        private void PlayNormalFireSound()
        {
            bool zenith = Main.zenithWorld;

            string path = zenith
                ? "CalamityLegendsComeBack/Sound/SHPC/M14开枪"
                : "CalamityLegendsComeBack/Sound/SHPC/双刃镰开火音效";

            SoundStyle style = new SoundStyle(path)
            {
                Volume = zenith ? 1.2f : 1f,
                Pitch = zenith ? 0.1f : 0f
            };

            SoundEngine.PlaySound(style, Projectile.Center);
        }

        private void PlayStageUpSound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/迫击哨戒炮单次攻击")
                {
                    Volume = 5.2f,
                    Pitch = 0.2f
                },
                GunTipPosition
            );
        }

        private void PlayManualCooldownSound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/解放者机甲左手火箭弹")
                {
                    Volume = 2.7f,
                    Pitch = 0.2f
                },
                Projectile.Center
            );
        }

        private void PlayRocketSalvoSound()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityMod/Sounds/Item/AnomalysNanogunMPFBShot")
                {
                    Volume = 1f,
                    Pitch = 0f
                },
                GunTipPosition
            );
        }

        #endregion

        #region ===== 特效：阶段与状态 =====

        private void TriggerStageOutlinePulse()
        {
            stageOutlineTimer = StageOutlineDuration;
        }

        private void SpawnStageUpEnergyBurst()
        {
            PlayStageUpSound();

            for (int i = 0; i < 12; i++)
            {
                Vector2 upward = -Vector2.UnitY.RotatedByRandom(0.4f);

                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(6f, 6f),
                    267
                );

                dust.velocity = upward * Main.rand.NextFloat(3f, 7f);
                dust.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.4f, 1f));
                dust.scale = Main.rand.NextFloat(1.0f, 1.4f);
                dust.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity =
                    -Vector2.UnitY.RotatedByRandom(0.5f) *
                    Main.rand.NextFloat(1.5f, 3.5f);

                float scale = Main.rand.NextFloat(0.4f, 0.7f);
                Color color = Color.Lerp(Color.Orange, Color.White, Main.rand.NextFloat(0.3f, 0.8f));

                SquishyLightParticle particle = new(
                    GunTipPosition,
                    velocity,
                    scale,
                    color,
                    Main.rand.Next(16, 24)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity =
                    -Vector2.UnitY.RotatedByRandom(0.6f) *
                    Main.rand.NextFloat(1f, 2.5f);

                GlowOrbParticle glow = new GlowOrbParticle(
                    GunTipPosition + Main.rand.NextVector2Circular(4f, 4f),
                    velocity,
                    false,
                    18,
                    Main.rand.NextFloat(0.7f, 1.0f),
                    Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.3f, 0.8f)),
                    true,
                    true
                );

                GeneralParticleHandler.SpawnParticle(glow);
            }
        }

        private void SpawnCoolingVentMist()
        {
            if (frameCounter % 3 != 0)
                return;

            Vector2 forward = Vector2.UnitX.RotatedBy(Projectile.rotation);
            Vector2 back = -forward;
            float baseAngle = back.ToRotation();
            float angleOffset = MathHelper.Pi / 9f;
            float finalAngle = forward.X > 0f ? baseAngle - angleOffset : baseAngle + angleOffset;

            Vector2 direction = finalAngle.ToRotationVector2();
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            Vector2 spawnPos =
                GunBackPosition
                + forward * Main.rand.NextFloat(-10f, 4f)
                + right * Main.rand.NextFloat(-6f, 6f);

            direction = direction.RotatedBy(Main.rand.NextFloat(-0.08f, 0.08f));

            Particle smoke = new MediumMistParticle(
                spawnPos,
                direction * Main.rand.NextFloat(5f, 11f),
                Color.White,
                Color.Transparent,
                Main.rand.NextFloat(0.7f, 1.2f),
                Main.rand.NextFloat(180f, 220f)
            );

            GeneralParticleHandler.SpawnParticle(smoke);
        }

        #endregion

        #region ===== 特效：普通开火 =====

        private void SpawnNormalShotMuzzleEffect(Player player, Vector2 direction)
        {
            //Vector2 muzzlePos = GetSafeFirePosition(player) + direction * 4f;
            Vector2 muzzlePos = GunTipPosition + direction * 4f;
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);

            // 当前激光数量固定只会是 1~4，这里顺手钳一下
            int laserCount = Math.Max(1, Math.Min(LaserChainCount, 4));
            float heatInterpolant = MathHelper.Clamp(stage / 7f, 0f, 1f);

            Color techBlue = new Color(90, 190, 255);
            Color paleBlue = new Color(180, 235, 255);
            Color hotWhite = Color.Lerp(paleBlue, Color.White, 0.35f + heatInterpolant * 0.45f);

            // ================= BloomLineSoftEdge：数量 = 激光数量 × 2 =================
            // 设计思路：
            // 1条激光 -> 2条平行前冲
            // 2~4条激光 -> 每条“激光通道”各自带 2 条平行线，并整体按 fire 的风格展开成扇形

            // 👉 基础扇形（贴近原始fire）
            float baseFanAngle = laserCount == 1
                ? 0f
                : MathHelper.Lerp(0.04f, 0.13f, (laserCount - 2f) / 2f);

            // 👉 特效扇形：比激光更大（+20%）并且额外增强一点可见性
            float fanAngle = baseFanAngle * 1.2f + MathHelper.Lerp(0f, 0.035f, heatInterpolant);

            float pairSpacing = MathHelper.Lerp(1.1f, 3.2f, heatInterpolant);
            float forwardSpeed = MathHelper.Lerp(7.2f, 14.6f, heatInterpolant);
            int lineLifetime = (int)MathHelper.Lerp(7f, 11f, heatInterpolant);
            float lineScale = MathHelper.Lerp(0.024f, 0.042f, heatInterpolant);
            Vector2 lineStretch = new Vector2(
                MathHelper.Lerp(0.88f, 1.32f, heatInterpolant),
                MathHelper.Lerp(0.56f, 0.86f, heatInterpolant)
            );

            for (int i = 0; i < laserCount; i++)
            {
                float laneT = laserCount == 1 ? 0.5f : i / (float)(laserCount - 1);
                float laneAngle = laserCount == 1 ? 0f : MathHelper.Lerp(-fanAngle, fanAngle, laneT);

                Vector2 laneDirection = direction.RotatedBy(laneAngle);
                Vector2 laneRight = laneDirection.RotatedBy(MathHelper.PiOver2);

                // 中心通道略强，边缘稍弱，形成更稳定的秩序感
                float centerWeight = laserCount == 1 ? 1f : 1f - Math.Abs(laneT - 0.5f) * 0.35f;

                for (int side = -1; side <= 1; side += 2)
                {


                    // 起点就在枪口稍微前一点，保证一定能看到
                    Vector2 spawnPos =
                        muzzlePos
                        + laneDirection * Main.rand.NextFloat(0.8f, 2.2f)
                        + laneRight * (pairSpacing * 0.5f * side);

                    // 前进速度：明显更快，但不离谱
                    Vector2 velocity =
                        laneDirection * Main.rand.NextFloat(forwardSpeed * 1.1f, forwardSpeed * 1.4f)
                        + laneRight * (0.12f * side);

                    // 颜色：直接亮一点，不再压太狠
                    Color lineColor = Color.Lerp(
                        techBlue,
                        hotWhite,
                        0.55f + heatInterpolant * 0.35f
                    );

                    // 生命周期稍微拉长一点，但别太长
                    int lifetime = (int)(lineLifetime * 1.2f);

                    // 拉伸：强调“线”的感觉
                    Vector2 stretch = new Vector2(
                        lineStretch.X * 1.2f,
                        lineStretch.Y * 0.9f
                    );

                    // 👉 不用fadeIn，直接给一点透明度控制
                    Particle line = new CustomSpark(
                        spawnPos,
                        velocity,
                        "CalamityMod/Particles/BloomLineSoftEdge",
                        false,
                        lifetime,
                        Main.rand.NextFloat(lineScale * 0.95f, lineScale * 1.1f),
                        lineColor * 0.85f,   // 稍微透明，但不是看不见
                        stretch,
                        shrinkSpeed: 0.65f   // 稍慢一点，让尾巴更明显
                    );

                    GeneralParticleHandler.SpawnParticle(line);


                }
            }




            // ================= Dust：强化喷射（角度更大+速度更高+数量更密） =================
            int dustCount = 30 + (int)Math.Round(heatInterpolant * 10f);

            float dustForwardBase = MathHelper.Lerp(4.5f, 10.5f, heatInterpolant);
            float dustSideBase = MathHelper.Lerp(1.2f, 4.2f, heatInterpolant);

            // 👉 比激光扇形再大X0%
            float dustFanAngle = fanAngle * 1.7f + MathHelper.Lerp(0f, 0.06f, heatInterpolant);

            for (int i = 0; i < dustCount; i++)
            {
                float t = dustCount == 1 ? 0.5f : i / (float)(dustCount - 1);

                // 扇形角度分布（核心喷射感来源）
                float angle = MathHelper.Lerp(-dustFanAngle, dustFanAngle, t);
                Vector2 dir = direction.RotatedBy(angle);
                Vector2 dirRight = dir.RotatedBy(MathHelper.PiOver2);

                // 中间更强，两侧稍弱
                float centerWeight = 3f - Math.Abs(t - 0.5f) * 1.2f;

                Vector2 dustVelocity =
                    dir * dustForwardBase * Main.rand.NextFloat(0.9f, 1.3f) * centerWeight +
                    dirRight * Main.rand.NextFloat(-dustSideBase, dustSideBase) * 0.75f;

                Vector2 dustSpawnPos =
                    muzzlePos
                    + dir * Main.rand.NextFloat(0.4f, 2.8f)
                    + dirRight * Main.rand.NextFloat(-1.6f, 1.6f);

                Dust dust = Dust.NewDustPerfect(dustSpawnPos, 267);
                dust.velocity = dustVelocity;
                dust.color = Color.Lerp(techBlue, hotWhite, Main.rand.NextFloat(0.25f, 0.85f));
                dust.scale = Main.rand.NextFloat(
                    MathHelper.Lerp(0.7f, 0.9f, heatInterpolant),
                    MathHelper.Lerp(1.2f, 1.6f, heatInterpolant)
                );
                dust.noGravity = true;
            }



            // ================= GlowOrb：强化主喷流（明显前冲+更集中） =================
            int glowCount = 15 + (int)Math.Round(heatInterpolant * 4f);

            // 👉 同样扩角（但比dust略收一点，形成层次）
            float glowFanAngle = fanAngle * 1.2f;

            for (int i = 0; i < glowCount; i++)
            {
                float t = glowCount == 1 ? 0.5f : i / (float)(glowCount - 1);
                float angleOffset = MathHelper.Lerp(-glowFanAngle, glowFanAngle, t);

                Vector2 glowDirection = direction.RotatedBy(angleOffset);

                float centerWeight = 3f - Math.Abs(t - 0.5f) * 0.5f;

                Vector2 glowVelocity = glowDirection * Main.rand.NextFloat(
                    MathHelper.Lerp(2.5f, 4.5f, heatInterpolant),
                    MathHelper.Lerp(5.5f, 8.5f, heatInterpolant)
                ) * centerWeight;

                GlowOrbParticle glow = new GlowOrbParticle(
                    muzzlePos + glowDirection * Main.rand.NextFloat(0.5f, 3.2f),
                    glowVelocity,
                    false,
                    20 + (int)(heatInterpolant * 10f),
                    Main.rand.NextFloat(
                        MathHelper.Lerp(0.7f, 0.95f, heatInterpolant),
                        MathHelper.Lerp(1.1f, 1.6f, heatInterpolant)
                    ),
                    Color.Lerp(techBlue, hotWhite, Main.rand.NextFloat(0.4f, 0.9f)),
                    true,
                    true
                );

                GeneralParticleHandler.SpawnParticle(glow);
            }


            // 记住了，这个叫：“中心偏置喷流模型（Center-biased jet distribution）”






        }

        #endregion

        #region ===== 特效：火箭齐射 =====

        private void FireCooldownRocketSalvo()
        {
            PlayRocketSalvoSound();

            Player player = Main.player[Projectile.owner];
            NewLegendSHPC weapon = player.HeldItem.ModItem as NewLegendSHPC;
            NewLegendSHPC testWeapon = player.HeldItem.ModItem as NewLegendSHPC;

            player.CheckMana(player.HeldItem, 150, true, false);

            if (weapon == null && testWeapon == null)
                return;

            if (weapon != null && weapon.storedEffectPower > 0)
            {
                weapon.storedEffectPower -= 5;

                if (weapon.storedEffectPower < 0)
                    weapon.storedEffectPower = 0;
            }

            if (testWeapon != null && testWeapon.storedEffectPower > 0)
            {
                testWeapon.storedEffectPower -= 5;

                if (testWeapon.storedEffectPower < 0)
                    testWeapon.storedEffectPower = 0;
            }

            int effectID = currentEffectID;
            Vector2 dir = Vector2.UnitX.RotatedBy(Projectile.rotation);

            float shakePower = 20f;
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                shakePower * distanceFactor);

            player.velocity -= dir * 3.2f;

            SpawnNormalShotMuzzleEffect(player, dir);
            SpawnRocketSalvoMuzzleEffect(player, dir);

            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                float angle = MathHelper.Lerp(-0.3f, 0.3f, t);
                float distFromCenter = Math.Abs(t - 0.5f) * 2f;
                float speedFactor = (float)Math.Pow(1f - distFromCenter, 1.5f);
                float speed = MathHelper.Lerp(10f, 18f, speedFactor);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition,
                    dir.RotatedBy(angle) * speed,
                    ModContent.ProjectileType<NewLegendSHPB>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    effectID
                );
            }
        }

        private void SpawnRocketSalvoMuzzleEffect(Player player, Vector2 baseDirection)
        {
            Vector2 muzzlePos = GetSafeFirePosition(player) + baseDirection * 6f;

            Color techBlue = new Color(90, 190, 255);
            Color paleBlue = new Color(180, 235, 255);

            for (int i = 0; i < 4; i++)
            {
                Color lineColor = Color.Lerp(techBlue, Color.White, Main.rand.NextFloat(0.3f, 0.65f));
                Vector2 lineVelocity = baseDirection.RotatedByRandom(0.16f) * Main.rand.NextFloat(13f, 19f);

                Particle line = new CustomSpark(
                    muzzlePos,
                    lineVelocity,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    12,
                    Main.rand.NextFloat(0.04f, 0.055f),
                    lineColor,
                    new Vector2(1.25f, 0.8f),
                    shrinkSpeed: 0.72f
                );

                GeneralParticleHandler.SpawnParticle(line);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 dustVelocity = baseDirection.RotatedByRandom(0.24f) * Main.rand.NextFloat(3.5f, 8f);

                Dust dust = Dust.NewDustPerfect(
                    muzzlePos + baseDirection * Main.rand.NextFloat(0f, 3f),
                    267
                );

                dust.velocity = dustVelocity;
                dust.color = Color.Lerp(techBlue, paleBlue, Main.rand.NextFloat(0.2f, 0.8f));
                dust.scale = Main.rand.NextFloat(0.8f, 1.1f);
                dust.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 smokeVelocity = baseDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(2.4f, 5.4f);

                Particle smoke = new HeavySmokeParticle(
                    muzzlePos,
                    smokeVelocity,
                    Color.Lerp(Color.White, paleBlue, 0.35f),
                    18,
                    Main.rand.NextFloat(0.38f, 0.58f),
                    0.5f,
                    Main.rand.NextFloat(-0.12f, 0.12f),
                    Main.rand.NextBool()
                );

                GeneralParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                float angle = MathHelper.Lerp(-0.3f, 0.3f, t);

                Vector2 laneDirection = baseDirection.RotatedBy(angle);
                Vector2 lanePos = muzzlePos + laneDirection * Main.rand.NextFloat(2f, 5f);

                for (int j = 0; j < 2; j++)
                {
                    Color lineColor = Color.Lerp(techBlue, Color.White, Main.rand.NextFloat(0.25f, 0.55f));
                    Vector2 lineVelocity = laneDirection.RotatedByRandom(0.08f) * Main.rand.NextFloat(11f, 17f);

                    Particle laneLine = new CustomSpark(
                        lanePos,
                        lineVelocity,
                        "CalamityMod/Particles/BloomLineSoftEdge",
                        false,
                        10,
                        Main.rand.NextFloat(0.03f, 0.045f),
                        lineColor,
                        new Vector2(1.05f, 0.72f),
                        shrinkSpeed: 0.75f
                    );

                    GeneralParticleHandler.SpawnParticle(laneLine);
                }

                for (int j = 0; j < 2; j++)
                {
                    Vector2 dustVelocity = laneDirection.RotatedByRandom(0.14f) * Main.rand.NextFloat(2.8f, 6.8f);

                    Dust dust = Dust.NewDustPerfect(
                        lanePos,
                        267
                    );

                    dust.velocity = dustVelocity;
                    dust.color = Color.Lerp(techBlue, paleBlue, Main.rand.NextFloat(0.2f, 0.75f));
                    dust.scale = Main.rand.NextFloat(0.7f, 0.95f);
                    dust.noGravity = true;
                }

                if (Main.rand.NextBool(2))
                {
                    Vector2 smokeVelocity = laneDirection.RotatedByRandom(0.18f) * Main.rand.NextFloat(1.8f, 4.4f);

                    Particle smoke = new HeavySmokeParticle(
                        lanePos,
                        smokeVelocity,
                        Color.Lerp(Color.White, paleBlue, 0.35f),
                        16,
                        Main.rand.NextFloat(0.26f, 0.42f),
                        0.42f,
                        Main.rand.NextFloat(-0.08f, 0.08f),
                        Main.rand.NextBool()
                    );

                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }
        }

        #endregion
        public override void OnKill(int timeLeft)
        {
            stage = 0;
            stageTimer = 0;
        }
    }
}

