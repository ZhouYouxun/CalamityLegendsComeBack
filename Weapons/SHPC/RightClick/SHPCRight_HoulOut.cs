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
        private int currentEffectID;
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

        public override void HoldoutAI()
        {
            Player player = Main.player[Projectile.owner];

            #region ===== 基础生存判定 =====

            if (!player.active || player.dead || player.HeldItem.type != AssociatedItemID)
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
                    PlayStartupEffects();

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
                SpawnCoolingSmoke();
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
                TriggerStageEffect();
                PlayStageUpEffects();
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

            if (player.statMana <= 0)
            {
                // ===== 强制停火 =====
                fireStopTimer = Math.Max(fireStopTimer, 2); // 持续停火
                frameCounter = 0; // 防止继续触发Fire

                //// ===== 枪口向上喷烟 =====
                //for (int i = 0; i < 2; i++)
                //{
                //    Vector2 smokeVel = -Vector2.UnitY.RotatedByRandom(0.25f) * Main.rand.NextFloat(2f, 5f);
                //    float smokeScale = Main.rand.NextFloat(0.8f, 1.4f);

                //    SmallSmokeParticle smoke = new SmallSmokeParticle(
                //        GunTipPosition + Main.rand.NextVector2Circular(6f, 6f),
                //        smokeVel,
                //        Color.DimGray,
                //        Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                //        smokeScale,
                //        100
                //    );

                //    GeneralParticleHandler.SpawnParticle(smoke);
                //}

                return; // ❗直接终止本帧AI（关键）
            }

            #endregion
        }

        #region ===== 核心技能：降温 =====

        public void TryReduceHeat()
        {
            if (!AllowManualCool || reduceCooldown > 0 || stage <= 0)
                return;

            // ===== 降温音效 =====
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Weapons/SHPC/AWM开火")
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

            PlayReduceEffects();
            SpawnRockets();
        }


        #endregion

        #region ===== 功能逻辑 =====

        private void Fire(Player player)
        {
            player.statMana -= 2;

            //// ===== 调试：显示当前进度 =====
            //if (Main.myPlayer == player.whoAmI)
            //{
            //    string text = ProgressState switch
            //    {
            //        0 => "当前武器阶段：未击败任何关键Boss",
            //        1 => "当前武器阶段：已击败毁灭者",
            //        2 => "当前武器阶段：已击败星神游龙",
            //        3 => "当前武器阶段：已击败风暴编织者",
            //        4 => "当前武器阶段：已击败星流巨械",
            //        _ => "未知阶段"
            //    };

            //    Main.NewText(text, Color.LimeGreen);
            //}




            int count = LaserChainCount;

            Vector2 baseVelocity = Vector2.UnitX.RotatedBy(Projectile.rotation) * 25f;

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
                    float speedX = baseVelocity.X + Main.rand.Next(-20, 21) * 0.05f;
                    float speedY = baseVelocity.Y + Main.rand.Next(-20, 21) * 0.05f;

                    velocity = new Vector2(speedX, speedY);
                }

                int projIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GetSafeFirePosition(player) - Vector2.UnitX.RotatedBy(Projectile.rotation) * 22f,
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

                // 传递当前热值（给Buff用）
                //player.GetModPlayer<SHPCRight_Player>().HeatStage = stage;
            }

            PlayFireEffects(Vector2.UnitX.RotatedBy(Projectile.rotation));
            PlayMuzzleEffect(Vector2.UnitX.RotatedBy(Projectile.rotation));
        }

        // ===== 安全开火位置判定 =====
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

        #region ===== 特效封装 =====

        private void PlayStartupEffects()
        {
            bool zenith = Main.zenithWorld;

            string path = zenith
                ? "CalamityLegendsComeBack/Weapons/SHPC/M14拉枪"
                : "CalamityLegendsComeBack/Weapons/SHPC/双刃镰启动音效";

            SoundStyle style = new SoundStyle(path)
            {
                Volume = zenith ? 1.2f : 1f,
                Pitch = zenith ? 0.1f : 0f
            };

            SoundEngine.PlaySound(style, Projectile.Center);
        }

        private void PlayFireEffects(Vector2 direction)
        {
            bool zenith = Main.zenithWorld;

            string path = zenith
                ? "CalamityLegendsComeBack/Weapons/SHPC/M14开枪"
                : "CalamityLegendsComeBack/Weapons/SHPC/双刃镰开火音效";

            SoundStyle style = new SoundStyle(path)
            {
                Volume = zenith ? 1.2f : 1f,
                Pitch = zenith ? 0.1f : 0f
            };

            SoundEngine.PlaySound(style, Projectile.Center);
        }

        // 枪口特效
        private void PlayMuzzleEffect(Vector2 direction)
        {
            // 控制极低密度（核心）
            if (!Main.rand.NextBool(2))
                return;

            // 轻微前方偏移（贴近枪口）
            Vector2 spawnPos = GetSafeFirePosition(Main.player[Projectile.owner]) + direction * Main.rand.NextFloat(2f, 6f);

            // 主体方向：沿射击方向
            Vector2 velocity =
                direction.RotatedByRandom(0.08f) *
                Main.rand.NextFloat(1.5f, 3.2f);

            Dust dust = Dust.NewDustPerfect(spawnPos, 267);

            dust.velocity = velocity;

            // 科幻感颜色（白→金微过渡）
            dust.color = Color.Lerp(Color.White, Color.Gold, Main.rand.NextFloat(0.2f, 0.6f));

            // 小而精致
            dust.scale = Main.rand.NextFloat(0.6f, 0.9f);

            dust.noGravity = true;
        }

        // ===== 阶段升级特效（枪口电能上升）=====
        private void PlayStageUpEffects()
        {
            // ===== 音效 =====
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Weapons/SHPC/迫击哨戒炮单次攻击")
                {
                    Volume = 5.2f,
                    Pitch = 0.2f
                },
                GunTipPosition
            );

            // ===== 电能Dust上升 =====
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

            // ===== 光芒粒子（上升核心）=====
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

            // ===== 辉光球（能量核心上升）=====
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

        private void PlayReduceEffects()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Weapons/SHPC/解放者机甲左手火箭弹")
                {
                    Volume = 2.7f,
                    Pitch = 0.2f
                },
                Projectile.Center
            );
        }

        // 散热时散发的火箭弹
        private void SpawnRockets()
        {
            SoundEngine.PlaySound(
                new SoundStyle("CalamityMod/Sounds/Item/AnomalysNanogunMPFBShot")
                {
                    Volume = 1f, // 👉 音量（默认1，可改）
                    Pitch = 0f   // 👉 音调（默认0，可改）
                },
                GunTipPosition
            );

            Player player = Main.player[Projectile.owner];
            NewLegendSHPC weapon = player.HeldItem.ModItem as NewLegendSHPC;

            player.statMana -= 150;


            if (weapon == null)
                return;

            // ===== 扣除X发弹夹（只在右键触发时）=====
            if (weapon.storedEffectPower > 0)
            {
                weapon.storedEffectPower -= 5;

                if (weapon.storedEffectPower < 0)
                    weapon.storedEffectPower = 0;
            }

            //int effectID = weapon.storedEffectID > 0 ? weapon.storedEffectID : -1;
            int effectID = currentEffectID;

            Vector2 dir = Vector2.UnitX.RotatedBy(Projectile.rotation);

            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f; // 0~1

                // ===== 对称角度（不变）=====
                float angle = MathHelper.Lerp(-0.3f, 0.3f, t);

                // ===== 距离中心的“偏移程度”（0在中间，1在两边）=====
                float distFromCenter = Math.Abs(t - 0.5f) * 2f; // 0~1

                // ===== 尖峰速度曲线（核心）=====
                float speedFactor = 1f - distFromCenter; // 中间=1，两边=0

                // 👉 再压一下，让尖峰更明显
                speedFactor = (float)Math.Pow(speedFactor, 1.5f);

                // ===== 最终速度 =====
                float speed = MathHelper.Lerp(10f, 18f, speedFactor);
                // 左右最慢≈6，中间最快≈14

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition,
                    dir.RotatedBy(angle) * speed,
                    ModContent.ProjectileType<NewLegendSHPB>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    effectID // ⭐ 现在是正确的
                );
                // 原先是漩涡火箭：ProjectileID.VortexBeaterRocket

            }
        }

        // ===== 散热喷气（斜后方，高压版）=====
        private void SpawnCoolingSmoke()
        {
            // 控制频率（更密一点）
            if (!Main.rand.NextBool(1))
                return;

            Vector2 forward = Vector2.UnitX.RotatedBy(Projectile.rotation);
            Vector2 back = -forward;

            // 👉 核心：斜后方喷（不是纯后）
            Vector2 direction =
                back.RotatedBy(Main.rand.NextFloat(-0.4f, 0.4f)); // 扇形

            // 👉 更强的喷射力度（比Tau猛）
            Vector2 velocity =
                direction * Main.rand.NextFloat(5f, 11f);

            Particle smoke = new MediumMistParticle(
                GunBackPosition + Main.rand.NextVector2Circular(6f, 6f),
                velocity,
                Color.White,          // 白烟
                Color.Transparent,
                Main.rand.NextFloat(0.7f, 1.2f),
                Main.rand.NextFloat(180f, 220f) // 持续时间
            );

            GeneralParticleHandler.SpawnParticle(smoke);
        }
        private void TriggerStageEffect()
        {
            stageOutlineTimer = StageOutlineDuration;
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
                var barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
                var barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

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