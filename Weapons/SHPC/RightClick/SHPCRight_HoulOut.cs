using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.Visuals;
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
        public override bool UseBaseDraw => true;

        public override Vector2 GunTipPosition =>
            Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * 56f;

        public override float MaxOffsetLengthFromArm => 35f;

        #region ===== Energy Core Position =====

        // Angle controls the side rotation; distance controls how far it extends from Projectile.Center.
        private float energyCorePolarAngle = -1.04f;
        private float energyCorePolarDistance = 4f;

        private Vector2 EnergyCorePosition
        {
            get
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX.RotatedBy(Projectile.rotation));
                int facing = Projectile.spriteDirection == 0 ? 1 : Projectile.spriteDirection;

                return Projectile.Center + direction.RotatedBy(energyCorePolarAngle * facing) * energyCorePolarDistance;
            }
        }

        #endregion

        #region ===== 基础状态 =====

        private int frameCounter;
        private int spawnDelay = 15;

        #endregion

        #region ===== 阶段系统 =====

        private int stage;
        private int stageTimer;
        private int overheatTimer;

        private float visualProgress; // 用于UI的真实进度（可升可降）
        private int manualCoolingVisualTimer;

        // ===== 世界进度状态（来自武器）=====
        public int ProgressState; // 0~4

        private int MaxHeatStage;
        private int LaserChainCount;
        private readonly BalanceSHPC balance = new();

        // ===== 每阶段升级所需时间 =====
        private int GetStageUpTime()
        {
            return balance.GetHeatFillTime(stage);
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

            Player owner = Main.player[Projectile.owner];
            stage = Utils.Clamp(owner.GetModPlayer<SHPCRight_Player>().HeatStage, 0, MaxHeatStage);
        }
        // ===== 根据进度初始化规则 =====
        private void InitializeProgressRules()
        {
            switch (ProgressState)
            {
                default: // 初始
                case 0:
                    MaxHeatStage = 1;
                    LaserChainCount = 1;
                    break;

                case 1: // Hardmode
                    MaxHeatStage = 2;
                    LaserChainCount = 2;
                    break;

                case 2: // Plantera
                    MaxHeatStage = 3;
                    LaserChainCount = 2;
                    break;

                case 3: // Moon Lord
                    MaxHeatStage = 4;
                    LaserChainCount = 3;
                    break;

                case 4: // Devourer of Gods
                    MaxHeatStage = 5;
                    LaserChainCount = 3;
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

            if (fireStopTimer <= 0)
            {
                int stageUpTime = GetStageUpTime();

                if (stage < MaxHeatStage && stageTimer >= stageUpTime)
                {
                    stage++;
                    stageTimer = 0;
                    overheatTimer = 0;
                    TriggerStageOutlinePulse();
                    SpawnStageUpEnergyBurst();
                }
                else if (stage >= MaxHeatStage)
                {
                    overheatTimer++;

                    if (overheatTimer >= BalanceSHPC.OverheatGraceTime)
                    {
                        ForceShutdown(player);
                    }
                }
            }

            #endregion

            #region ===== 后坐力 & 充能 =====

            OffsetLengthFromArm -= 2f;

            ApplyRecoilRotation();

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
                targetProgress = manualCoolingVisualTimer > 0
                    ? manualCoolingVisualTimer / (float)CoolingRecoilTotalTime
                    : fireStopTimer / (float)BalanceSHPC.ForcedShutdownTime;
            }

            if (manualCoolingVisualTimer > 0)
                manualCoolingVisualTimer--;

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
        // ===== 后坐力 =====TryReduceHeat
        private int recoilFrame;
        private bool recoilActive;
        private const int RecoilRaiseTime = 6;   // 抬枪：快
        private const int RecoilHoldTime = 2;    // 顶点短停
        private const int RecoilReturnTime = 5;  // 收回：快
        private const float RecoilAngle = 0.72f; // 抬高角度，自行调

        private const float RecoilAngleMultiplier = 3.152778f; // 0.72 * 3.152778 ≈ 2.27 rad ≈ 130°

        private const int CoolingRecoilRaiseTime = RecoilRaiseTime + 2;
        private const int CoolingRecoilHoldTime = RecoilHoldTime + 40;
        private const int CoolingRecoilReturnTime = RecoilReturnTime + 5;
        private const int CoolingRecoilTotalTime = CoolingRecoilRaiseTime + CoolingRecoilHoldTime + CoolingRecoilReturnTime;

        private void ForceShutdown(Player player)
        {
            stage = Math.Max(0, stage - 1);
            stageTimer = 0;
            overheatTimer = 0;
            fireStopTimer = BalanceSHPC.ForcedShutdownTime;
            manualCoolingVisualTimer = 0;
            frameCounter = 0;
            player.GetModPlayer<SHPCRight_Player>().HeatStage = stage;
            player.GetModPlayer<SHPCRight_Player>().SetAttackLockout(BalanceSHPC.ForcedShutdownTime);
            TriggerStageOutlinePulse();
            SpawnCoolingVentMist();
        }

        private void ApplyRecoilRotation()
        {
            if (!recoilActive)
                return;

            // ===== 每帧都重新读取“基础瞄准角” =====
            // 这样回程时不会绕圈，而是始终朝当前鼠标方向收回
            Vector2 aimDir = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX);
            float baseAimRotation = aimDir.ToRotation();

            float recoilSide = aimDir.X >= 0f ? -1f : 1f;
            float recoilOffset;

            if (recoilFrame < CoolingRecoilRaiseTime)
            {
                // 先快后慢：EaseOut
                float t = recoilFrame / (float)CoolingRecoilRaiseTime;
                float eased = 1f - (1f - t) * (1f - t);
                recoilOffset = recoilSide * RecoilAngle * RecoilAngleMultiplier * eased;
            }
            else if (recoilFrame < CoolingRecoilRaiseTime + CoolingRecoilHoldTime)
            {
                // 顶点短暂停顿
                recoilOffset = recoilSide * RecoilAngle * RecoilAngleMultiplier;
            }
            else if (recoilFrame < CoolingRecoilTotalTime)
            {
                // 快速收回：时间短，视觉上就是“嗖”一下压回去
                float t = (recoilFrame - CoolingRecoilRaiseTime - CoolingRecoilHoldTime + 1f) / CoolingRecoilReturnTime;
                float eased = t * t; // 开头快，后面贴近终点时更稳
                recoilOffset = MathHelper.Lerp(recoilSide * RecoilAngle * RecoilAngleMultiplier, 0f, eased);
            }
            else
            {
                Projectile.rotation = baseAimRotation;
                Projectile.velocity = baseAimRotation.ToRotationVector2();
                recoilActive = false;
                recoilFrame = 0;
                return;
            }

            float finalRotation = baseAimRotation + recoilOffset;

            Projectile.rotation = finalRotation;
            Projectile.velocity = finalRotation.ToRotationVector2();

            int direction = Math.Sign(aimDir.X);
            if (direction == 0)
                direction = Owner.direction;

            Projectile.spriteDirection = direction;
            Owner.ChangeDir(direction);

            // ===== 手臂也同步到这个新角度，否则会有一帧不跟 =====
            Owner.itemRotation = (Projectile.velocity * direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir +
                                (Owner.gravDir == -1 ? MathHelper.Pi : 0f);

            Owner.SetCompositeArmFront(true, FrontArmStretch, armRotation + ExtraFrontArmRotation * direction);
            Owner.SetCompositeArmBack(true, BackArmStretch, armRotation + ExtraBackArmRotation * direction);

            recoilFrame++;
        }

        public void TryReduceHeat()
        {
            if (reduceCooldown > 0 || stage <= 0)
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
            overheatTimer = 0;
            Owner.GetModPlayer<SHPCRight_Player>().HeatStage = stage;
            
            reduceCooldown = CoolingRecoilTotalTime + BalanceSHPC.ManualCoolingExtraLockout;
            fireStopTimer = CoolingRecoilTotalTime; // 停火时间
            manualCoolingVisualTimer = CoolingRecoilTotalTime;

            OffsetLengthFromArm -= 18f;

            // ===== 抬枪后坐力：启动三段式动画 =====
            recoilActive = true;
            recoilFrame = 0;

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
            Vector2 sideDirection = fireDirection.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < count; i++)
            {
                float fixedAngle = i switch
                {
                    1 => MathHelper.ToRadians(1.15f),
                    2 => MathHelper.ToRadians(-2.35f),
                    _ => 0f
                };
                float waveAngle = i == 0
                    ? 0f
                    : (float)Math.Sin(spiralCounter * 0.26f + i * 1.7f) * MathHelper.ToRadians(i == 1 ? 0.25f : 0.4f);
                float sideOffset = i switch
                {
                    1 => 2.5f,
                    2 => -4f,
                    _ => 0f
                };
                Vector2 velocity = baseVelocity.RotatedBy(fixedAngle + waveAngle);

                int projIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GetSafeFirePosition(player) - fireDirection * 22f + sideDirection * sideOffset,
                    velocity,
                    ModContent.ProjectileType<SHPCRight_Proj>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    player.whoAmI,
                    i
                );

                if (Main.projectile.IndexInRange(projIndex) &&
                    Main.projectile[projIndex].ModProjectile is SHPCRight_Proj beam)
                {
                    beam.WeaponStage = stage;
                    beam.HeatLevel = stage;
                    beam.BeamIndex = i;
                    beam.IsMainBeam = i == 0;
                }
            }

            // ===== Heat4+：施加过热Debuff =====
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
            Texture2D outlineTexture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 outlinePosition = Projectile.Center - Main.screenPosition;
            Vector2 outlineOrigin = outlineTexture.Size() * 0.5f;
            float outlineRotation = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
            SpriteEffects outlineEffects =
                (float)Projectile.spriteDirection * Owner.gravDir == -1f
                    ? SpriteEffects.FlipHorizontally
                    : SpriteEffects.None;
            Vector2 outlineScale = Vector2.One * Projectile.scale * Math.Abs(Owner.gravDir);

            float stageProgress = stage >= MaxHeatStage
                ? 1f
                : MathHelper.Clamp(stageTimer / (float)GetStageUpTime(), 0f, 1f);
            float heatGlow = MathHelper.SmoothStep(0f, 1f, stageProgress);
            float stageSnap = stageOutlineTimer / (float)StageOutlineDuration;
            float stageColorProgress = MathHelper.Clamp((stage + heatGlow) / Math.Max(1f, MaxHeatStage), 0f, 1f);
            Color heatColor = Color.Lerp(new Color(70, 210, 255), new Color(255, 92, 214), stageColorProgress);
            heatColor = Color.Lerp(heatColor, Color.White, 0.08f + heatGlow * 0.18f);
            float heatRadius = 1.25f + heatGlow * 4.85f + stageSnap * 0.55f;
            float heatOpacity = 0.1f + heatGlow * 0.42f + stageSnap * 0.12f;
            int heatDraws = 12 + (int)(heatGlow * 10f) + (stageSnap > 0f ? 4 : 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            HoldoutOutlineHelper.DrawSolidOutline(
                outlineTexture,
                outlinePosition,
                outlineRotation,
                outlineOrigin,
                outlineScale,
                outlineEffects,
                heatColor,
                heatRadius,
                heatOpacity,
                Main.GlobalTimeWrappedHourly + Projectile.identity * 0.1f,
                heatDraws,
                manageBlendState: false);

            if (stageOutlineTimer > 0)
            {
                HoldoutOutlineHelper.DrawStarmadaRainbowOutline(
                    outlineTexture,
                    outlinePosition,
                    outlineRotation,
                    outlineOrigin,
                    outlineScale,
                    outlineEffects,
                    1.15f + stageSnap * 1.65f,
                    0.12f + stageSnap * 0.3f,
                    Main.GlobalTimeWrappedHourly + Projectile.identity * 0.17f,
                    18,
                    manageBlendState: false);

                stageOutlineTimer--;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
            // ===== 阶段升级描边脉冲 =====
            if (stageOutlineTimer > StageOutlineDuration + 1)
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
                float heatColorProgress = MathHelper.Clamp((stage + visualProgress) / Math.Max(1f, MaxHeatStage), 0f, 1f);
                Color color = GetHeatBarColor(heatColorProgress);
                if (fireStopTimer > 0)
                    color = Color.Lerp(color, Color.White, 0.25f);

                Main.spriteBatch.Draw(barBG, drawPos, null, color * opacity, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(barFG, drawPos, frameCrop, color * opacity * 0.8f, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
            }

            return base.PreDraw(ref lightColor);
        }

        private Color GetHeatBarColor(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);

            if (progress < 0.25f)
                return Color.Lerp(new Color(70, 210, 255), new Color(120, 245, 255), progress / 0.25f);

            if (progress < 0.5f)
                return Color.Lerp(new Color(120, 245, 255), new Color(255, 230, 90), (progress - 0.25f) / 0.25f);

            if (progress < 0.75f)
                return Color.Lerp(new Color(255, 230, 90), new Color(255, 112, 67), (progress - 0.5f) / 0.25f);

            return Color.Lerp(new Color(255, 112, 67), Color.White, (progress - 0.75f) / 0.25f);
        }

        #endregion
        public override void OnKill(int timeLeft)
        {
            stage = 0;
            stageTimer = 0;
        }
    }
}

