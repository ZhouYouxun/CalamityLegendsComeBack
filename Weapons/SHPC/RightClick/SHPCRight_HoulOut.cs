using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal partial class SHPCRight_HoulOut : RightClickHoldoutBase, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
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


            // 普通开火特效的额外模块，必须得放在这【可能有点难找，但是你懂的，有些】
            if (normalShotFXLastCenter == Vector2.Zero)
                normalShotFXLastCenter = GunTipPosition;

            Vector2 normalShotDelta = GunTipPosition - normalShotFXLastCenter;
            normalShotFXLastCenter = GunTipPosition;

            for (int i = normalShotFXParticles.Count - 1; i >= 0; i--)
            {
                Particle p = normalShotFXParticles[i];

                if (p.Time >= p.Lifetime)
                {
                    normalShotFXParticles.RemoveAt(i);
                    continue;
                }

                p.Position += normalShotDelta * 0.45f;
            }
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
                    float speedX = baseVelocity.X + Main.rand.Next(-20, 21) * 0.05f;
                    float speedY = baseVelocity.Y + Main.rand.Next(-20, 21) * 0.05f;
                    velocity = new Vector2(speedX, speedY);



                    //float t = spiralCounter * 0.15f; // 时间参数（调速度）

                    //float maxAngle = MathHelper.Pi / 180f * 4f;   // 主扇形 ±X°
                    //float waveAngle = MathHelper.Pi / 180f * 1f;  // 螺旋扰动 ±Y°

                    //float spread = MathHelper.Lerp(-maxAngle, maxAngle, i / (float)(count - 1));
                    //Vector2 baseDir = baseVelocity.RotatedBy(spread);

                    //float phase = i * 1.2f;
                    //float offset = (float)Math.Sin(t + phase) * waveAngle;

                    //velocity = baseDir.RotatedBy(offset);
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
        public override void OnKill(int timeLeft)
        {
            stage = 0;
            stageTimer = 0;
        }
    }
}

