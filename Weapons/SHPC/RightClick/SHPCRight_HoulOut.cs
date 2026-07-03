using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatModule;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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

        public override void KillHoldoutLogic()
        {
            if (!Owner.active || Owner.dead)
            {
                KillAndPreserveHeat();
                return;
            }

            int heldType = Owner.HeldItem.type;
            if (heldType != AssociatedItemID)
            {
                KillAndPreserveHeat();
                return;
            }

            bool wantsToRelease = Projectile.owner == Main.myPlayer &&
                (!Main.mouseRight || !NewLegendSHPC.CanUseWorldRightClick(Owner));

            if (!wantsToRelease)
                return;

            if (ShouldBlockReleaseDuringForcedShutdown())
                return;

            KillAndPreserveHeat();
        }

        private bool ShouldBlockReleaseDuringForcedShutdown()
        {
            SHPCRight_Player heatPlayer = Owner.GetModPlayer<SHPCRight_Player>();
            return heatPlayer.IsForcedShutdownCooling();
        }

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

        #region ===== 基础状�?=====

        private int frameCounter;
        private int spawnDelay = 15;
        private bool startupSoundPlayed;

        #endregion

        #region ===== 阶段系统 =====

        private int stage;
        private int stageTimer;
        private int overheatTimer;
        private float bonusHeatProgress;
        private int manualCoolingVisualTimer;

        private float visualProgress; // 用于UI的真实进度（可升可降�?

        // ===== 世界进度状态（来自武器�?===
        public int ProgressState; // 0~4

        private int MaxHeatStage;
        private int LaserChainCount;
        private readonly BalanceSHPC balance = new();

        // ===== 每阶段升级所需时间 =====
        private int GetStageUpTime()
        {
            return GetStageUpTime(stage, MaxHeatStage);
        }

        private int GetStageUpTime(int completedHeatLevel)
        {
            return GetStageUpTime(completedHeatLevel, MaxHeatStage);
        }

        private int GetStageUpTime(int completedHeatLevel, int maxHeatStage)
        {
            int defaultFillTime = balance.GetHeatFillTime(completedHeatLevel, maxHeatStage);
            if (!Main.player.IndexInRange(Projectile.owner))
                return defaultFillTime;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
                return defaultFillTime;

            return owner.GetModPlayer<HeatModulePlayer>().GetHeatFillTime(defaultFillTime);
        }

        #endregion

        #region ===== 冷却 / 技能控�?=====

        private int reduceCooldown;
        private int fireStopTimer;
        private int manaStarvedStopTimer;
        private int forcedShutdownVisualDuration = BalanceSHPC.ForcedShutdownTime;
        private bool manaStarvedSoundPlayed;
        private bool maxHeatEntrySoundPlayed;

        #endregion

        #region ===== 枪托位置 =====

        // ===== 枪托位置（可手动调节�?====
        private const float GunBackOffset = 26f;     // 往后距离（核心调这里）
        private const float GunBackUpOffset = 4f;    // 往上微调（类似Tau�?

        private Vector2 GunBackPosition =>
            Projectile.Center
            - Vector2.UnitX.RotatedBy(Projectile.rotation) * GunBackOffset
            - Vector2.UnitY.RotatedBy(Projectile.rotation) * GunBackUpOffset;

        #endregion

        #region ===== 额外数据 =====

        private int currentEffectID;

        #endregion

        #region ===== 初始�?=====
        public override void OnSpawn(IEntitySource source)
        {
            ProgressState = (int)Projectile.ai[0];
            currentEffectID = (int)Projectile.ai[1];
            InitializeProgressRules();

            Player owner = Main.player[Projectile.owner];
            spawnDelay = owner.GetModPlayer<SHPCEnergyCorePlayer>().GetRightClickStartupFrames(spawnDelay);
            SHPCRight_Player heatPlayer = owner.GetModPlayer<SHPCRight_Player>();
            stage = Utils.Clamp(heatPlayer.HeatStage, 0, MaxHeatStage);
            stageTimer = Utils.Clamp(heatPlayer.HeatProgressTimer, 0, GetStageUpTime(stage));
            heatPlayer.SyncHeatFromHoldout(stage, stageTimer, MaxHeatStage);
        }
        // ===== 根据进度初始化规�?=====
        private void InitializeProgressRules()
        {
            MaxHeatStage = balance.GetRightClickMaxHeatLevelForProgressState(ProgressState);
            LaserChainCount = balance.GetRightClickLaserCountForProgressState(ProgressState);
        }

        #endregion

        #region ===== 主AI =====

        public override void HoldoutAI()
        {
            Player player = Main.player[Projectile.owner];

            #region ===== 基础生存判定 =====

            if (!player.active || player.dead ||
                player.HeldItem.type != AssociatedItemID)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.owner != Main.myPlayer)
            {
                ApplyRemoteSyncedState(player);
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
                if (!startupSoundPlayed)
                {
                    PlayStartupSound();
                    startupSoundPlayed = true;
                }

                spawnDelay--;
                if (spawnDelay > 0)
                    return;

                frameCounter = Math.Max(frameCounter, 4);
            }

            #endregion

            #region ===== 常规运行 =====

            frameCounter++;

            // Sync after stage changes so detached heat UI and cooling use the final value.
            SHPCRight_Player heatPlayer = player.GetModPlayer<SHPCRight_Player>();
            bool forcedShutdownCooling = heatPlayer.IsForcedShutdownCooling();
            if (forcedShutdownCooling)
            {
                stage = Utils.Clamp(heatPlayer.HeatStage, 0, MaxHeatStage);
                stageTimer = Utils.Clamp(heatPlayer.HeatProgressTimer, 0, GetStageUpTime(stage));
                frameCounter = 0;
            }

            int manaCost = player.GetModPlayer<SHPCEnergyCorePlayer>().GetRightClickManaCost(2);
            bool hasEnoughManaToFire = manaCost <= 0 || player.CheckMana(player.HeldItem, manaCost, false, false);
            if (hasEnoughManaToFire)
                manaStarvedSoundPlayed = false;

            if (!forcedShutdownCooling && fireStopTimer <= 0 && hasEnoughManaToFire)
            {
                stageTimer++;
                HeatModulePlayer heatRedirect = player.GetModPlayer<HeatModulePlayer>();
                bonusHeatProgress += heatRedirect.HeatGenerationMultiplier - 1f;
                if (bonusHeatProgress >= 1f)
                {
                    int bonusHeat = (int)bonusHeatProgress;
                    stageTimer += bonusHeat;
                    bonusHeatProgress -= bonusHeat;
                }

                if (stage >= MaxHeatStage && CanSustainCurrentMaximumHeat())
                {
                    stageTimer = Math.Min(stageTimer, GetStageUpTime());
                    bonusHeatProgress = 0f;
                }
            }

            #endregion

            #region ===== 开火控�?=====

            if (forcedShutdownCooling)
            {
                if (fireStopTimer > 0)
                    fireStopTimer--;
                if (manaStarvedStopTimer > 0)
                    manaStarvedStopTimer--;
                else
                    SpawnCoolingVentMist();
            }
            else if (fireStopTimer > 0)
            {
                fireStopTimer--;
                if (manaStarvedStopTimer > 0)
                    manaStarvedStopTimer--;
                else
                    SpawnCoolingVentMist();
            }
            else if (frameCounter >= 5)
            {
                Fire(player);
                frameCounter = 0;
            }

            if (heatPlayer.IsForcedShutdownCooling())
            {
                stage = Utils.Clamp(heatPlayer.HeatStage, 0, MaxHeatStage);
                stageTimer = Utils.Clamp(heatPlayer.HeatProgressTimer, 0, GetStageUpTime(stage));
            }

            #endregion

            #region ===== 升级系统 =====

            bool stageFilledThisFrame = false;
            if (!forcedShutdownCooling && fireStopTimer <= 0)
            {
                int stageUpTime = GetStageUpTime();

                if (stage < MaxHeatStage && stageTimer >= stageUpTime)
                {
                    stageFilledThisFrame = true;
                    visualProgress = 1f;
                    heatPlayer.SyncHeatFromHoldout(stage, stageUpTime, MaxHeatStage);
                    stage++;
                    stageTimer = 0;
                    overheatTimer = 0;
                    TriggerStageOutlinePulse();
                    SpawnStageUpEnergyBurst();

                    if (stage >= MaxHeatStage && !maxHeatEntrySoundPlayed)
                    {
                        maxHeatEntrySoundPlayed = true;
                        PlayMaxHeatEntrySound();
                    }
                }
                else if (stage >= MaxHeatStage)
                {
                    if (CanSustainCurrentMaximumHeat())
                    {
                        overheatTimer = 0;
                    }
                    else
                    {
                        overheatTimer++;

                        if (overheatTimer >= balance.GetOverheatGraceTime(MaxHeatStage))
                            ForceShutdown(player);
                    }
                }
            }

            #endregion

            #region ===== 后坐�?& 充能 =====

            if (heatPlayer.IsForcedShutdownCooling())
            {
                stage = Utils.Clamp(heatPlayer.HeatStage, 0, MaxHeatStage);
                stageTimer = Utils.Clamp(heatPlayer.HeatProgressTimer, 0, GetStageUpTime(stage));
            }
            else if (!stageFilledThisFrame)
                heatPlayer.SyncHeatFromHoldout(stage, stageTimer, MaxHeatStage);

            SpawnTopHeatPlayerGlow(player, heatPlayer);

            if (stage < MaxHeatStage)
                maxHeatEntrySoundPlayed = false;

            OffsetLengthFromArm -= 2f;

            ApplyRecoilRotation();

            float targetProgress;

            // ===== 正常充能 =====
            if (heatPlayer.IsForcedShutdownCooling())
            {
                targetProgress = heatPlayer.GetDetachedHeatProgress();
            }
            else if (fireStopTimer <= 0)
            {
                if (stageFilledThisFrame)
                {
                    targetProgress = 1f;
                }
                else if (stage >= MaxHeatStage)
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
                if (manaStarvedStopTimer > 0)
                    targetProgress = visualProgress;
                else
                    targetProgress = manualCoolingVisualTimer > 0
                    ? manualCoolingVisualTimer / (float)CoolingRecoilTotalTime
                    : fireStopTimer / (float)Math.Max(1, forcedShutdownVisualDuration);
            }

            if (manualCoolingVisualTimer > 0)
                manualCoolingVisualTimer--;

            // ===== 平滑过渡（关键）=====
            visualProgress = targetProgress;



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


            // 普通开火特效的额外模块，必须得放在这【可能有点难找，但是你懂的，有些�?
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
        // ===== 后坐�?=====TryReduceHeat
        private void ApplyRemoteSyncedState(Player player)
        {
            if (MaxHeatStage <= 0 || LaserChainCount <= 0)
                InitializeProgressRules();

            stage = Utils.Clamp(stage, 0, MaxHeatStage);
            int fillTime = GetStageUpTime(Utils.Clamp(stage, 0, 4));
            stageTimer = stage >= MaxHeatStage
                ? fillTime
                : Utils.Clamp(stageTimer, 0, fillTime);

            player.GetModPlayer<SHPCRight_Player>().SyncHeatFromHoldout(stage, stageTimer, MaxHeatStage);

            visualProgress = stage >= MaxHeatStage
                ? 1f
                : Utils.Clamp(stageTimer / (float)Math.Max(1, fillTime), 0f, 1f);
        }

        private int recoilFrame;
        private bool recoilActive;
        private int recoilDirection = 1;
        private float recoilBaseRotation;
        private float recoilTargetRotation;
        private float recoilVisualRotation;

        private const int CoolingRecoilRaiseTime = 22;
        private const int CoolingRecoilReturnTime = 30;
        private const int CoolingRecoilTotalTime = 59;
        private const float CoolingRecoilAngle = MathHelper.PiOver2 * 1.3f;
        private const float CoolingRecoilCurrentWeight = 0.795f;

        private void ForceShutdown(Player player)
        {
            PlayForcedShutdownSound();
            stage = MaxHeatStage;
            stageTimer = GetStageUpTime(Utils.Clamp(stage, 0, 4));
            overheatTimer = 0;
            bool heatModuleEquipped = player.GetModPlayer<HeatModulePlayer>().HeatModuleEquipped;
            int shutdownTime = balance.GetForcedShutdownTime(player);
            forcedShutdownVisualDuration = shutdownTime;
            fireStopTimer = shutdownTime;
            manualCoolingVisualTimer = 0;
            frameCounter = 0;
            SHPCRight_Player heatPlayer = player.GetModPlayer<SHPCRight_Player>();
            heatPlayer.StartForcedShutdownCooling(shutdownTime, MaxHeatStage);
            TriggerStageOutlinePulse();
            SpawnForcedShutdownGasBurst();
            SpawnHeatRedirectField(player, heatModuleEquipped ? 420 : 280, shutdownTime);
            player.AddBuff(ModContent.BuffType<SHPCRight_DeBuff>(), shutdownTime);
        }

        private bool CanSustainCurrentMaximumHeat()
        {
            return MaxHeatStage >= 5 && stage >= 5;
        }

        private void ApplyRecoilRotation()
        {
            if (!recoilActive)
                return;

            if (recoilFrame >= CoolingRecoilTotalTime)
            {
                recoilActive = false;
                recoilFrame = 0;
                return;
            }

            if (recoilFrame < CoolingRecoilRaiseTime)
            {
                recoilVisualRotation = UpdateCoolingRecoil(recoilTargetRotation.ToRotationVector2());
            }
            else if (CoolingRecoilTotalTime - recoilFrame < CoolingRecoilReturnTime)
            {
                recoilVisualRotation = UpdateCoolingRecoil(recoilBaseRotation.ToRotationVector2());
            }

            ApplyCoolingRecoilRotation(recoilVisualRotation);
            recoilFrame++;
        }

        private float UpdateCoolingRecoil(Vector2 target)
        {
            Vector2 current = recoilVisualRotation.ToRotationVector2();
            return Vector2.Lerp(target, current, CoolingRecoilCurrentWeight).ToRotation();
        }

        private void ApplyCoolingRecoilRotation(float rotation)
        {
            Projectile.rotation = rotation;
            Projectile.velocity = rotation.ToRotationVector2();

            Projectile.spriteDirection = recoilDirection;
            Owner.ChangeDir(recoilDirection);

            // ===== 手臂也同步到这个新角度，否则会有一帧不�?=====
            Owner.itemRotation = (Projectile.velocity * recoilDirection).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir +
                                (Owner.gravDir == -1 ? MathHelper.Pi : 0f);

            Owner.SetCompositeArmFront(true, FrontArmStretch, armRotation + ExtraFrontArmRotation * recoilDirection);
            Owner.SetCompositeArmBack(true, BackArmStretch, armRotation + ExtraBackArmRotation * recoilDirection);
        }

        public void TryReduceHeat()
        {
            if (Owner.GetModPlayer<SHPCRight_Player>().IsForcedShutdownCooling())
                return;

            if (reduceCooldown > 0 || stage <= 0)
                return;

            // ===== 降温音效 =====
            SoundEngine.PlaySound(
                new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/AWM\u5F00\u706B")
                {
                    Volume = 5.2f,
                    Pitch = 0.2f
                },
                GunTipPosition
            );

            stage--;
            stageTimer = 0;
            overheatTimer = 0;
            Owner.GetModPlayer<SHPCRight_Player>().SyncHeatFromHoldout(stage, stageTimer, MaxHeatStage);
            
            reduceCooldown = CoolingRecoilTotalTime + BalanceSHPC.ManualCoolingExtraLockout;
            fireStopTimer = CoolingRecoilTotalTime; // 停火时间
            manualCoolingVisualTimer = CoolingRecoilTotalTime;

            OffsetLengthFromArm -= 18f;

            // ===== 抬枪后坐力：启动三段式动�?=====
            Vector2 aimWorld = CtrlChipPlayer.GetAimWorld(Owner, Main.MouseWorld);
            Vector2 aimDir = (aimWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            recoilDirection = Math.Sign(aimDir.X);
            if (recoilDirection == 0)
                recoilDirection = Owner.direction;

            recoilBaseRotation = aimDir.ToRotation();
            recoilTargetRotation = aimDir.RotatedBy(-CoolingRecoilAngle * recoilDirection).ToRotation();
            recoilVisualRotation = Projectile.rotation;
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

            int manaCost = player.GetModPlayer<SHPCEnergyCorePlayer>().GetRightClickManaCost(2);
            if (manaCost > 0 && !player.CheckMana(player.HeldItem, manaCost, true, false))
            {
                // 蓝不�?�?停火
                if (!manaStarvedSoundPlayed)
                {
                    manaStarvedSoundPlayed = true;
                    PlayManaStarvedSound();
                }

                fireStopTimer = Math.Max(fireStopTimer, 2);
                manaStarvedStopTimer = Math.Max(manaStarvedStopTimer, 2);
                frameCounter = 0;
                return;
            }

            int count = LaserChainCount;

            Vector2 fireDirection = Vector2.UnitX.RotatedBy(Projectile.rotation);
            Vector2 baseVelocity = fireDirection * 25f;
            Vector2 sideDirection = fireDirection.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < count; i++)
            {
                float fixedAngle = i == 0 ? 0f : Main.rand.NextFloat(MathHelper.ToRadians(-2.2f), MathHelper.ToRadians(2.2f));
                if (i > 0 && Math.Abs(fixedAngle) < MathHelper.ToRadians(0.45f))
                    fixedAngle = Math.Sign(fixedAngle == 0f ? Main.rand.NextFloat(-1f, 1f) : fixedAngle) * MathHelper.ToRadians(0.45f);

                float sideOffset = i switch
                {
                    1 => 2.5f,
                    2 => -4f,
                    _ => 0f
                };
                Vector2 velocity = baseVelocity.RotatedBy(fixedAngle);

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
                    Main.projectile[projIndex].CritChance = Projectile.CritChance;
                    beam.WeaponStage = stage;
                    beam.HeatLevel = stage;
                    beam.BeamIndex = i;
                    beam.IsMainBeam = i == 0;
                }
            }

            // ===== Heat4+：施加过热Debuff =====
            if (ShouldApplyHeatBurnDebuff())
            {
                player.AddBuff(ModContent.BuffType<SHPCRight_DeBuff>(), 180); // 3�?
            }

            spiralCounter++;
            PlayNormalFireSound();
            SpawnNormalShotMuzzleEffect(player, Vector2.UnitX.RotatedBy(Projectile.rotation));

            if (stage >= 5 && (int)Main.GameUpdateCount % 15 == 0)
                SpawnHeatRedirectField(player, 240, 45);
        }

        private bool ShouldApplyHeatBurnDebuff()
        {
            int secondHighestStage = Math.Max(1, MaxHeatStage - 1);
            return stage >= secondHighestStage || stage >= 4;
        }

        #endregion

        #region ===== 开火辅�?=====

        private void SpawnHeatRedirectField(Player player, int size, int duration)
        {
            if (!player.GetModPlayer<HeatModulePlayer>().HeatModuleEquipped)
                return;

            int damage = Math.Max(1, (int)(Projectile.damage * (0.35f + stage * 0.12f)));
            SHPCRight_HeatRedirectField.SpawnOrRefresh(player, player.Center, damage, stage, size, duration);
        }

        private Vector2 GetSafeFirePosition(Player player)
        {
            Vector2 gunTip = GunTipPosition;

            // ===== 1. 枪口在方块内 =====
            if (Collision.SolidCollision(gunTip, 1, 1))
                return Projectile.Center;

            // ===== 2. 获取最近敌�?=====
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
            float dangerRange = 56f; // 和你的枪口长度一�?

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

                // 0 �?1 �?0 的线性曲�?
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

                // �?4 个方向的描边（十字）
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

            return base.PreDraw(ref lightColor);
        }

        #endregion
        public override void OnKill(int timeLeft)
        {
            PreserveHeatState();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(stage);
            writer.Write(stageTimer);
            writer.Write(overheatTimer);
            writer.Write(ProgressState);
            writer.Write(MaxHeatStage);
            writer.Write(LaserChainCount);
            writer.Write(currentEffectID);
            writer.Write(visualProgress);
            writer.Write(stageOutlineTimer);
            writer.Write(fireStopTimer);
            writer.Write(manualCoolingVisualTimer);
            writer.Write(forcedShutdownVisualDuration);
            writer.Write(manaStarvedStopTimer);
            writer.Write(recoilActive);
            writer.Write(recoilDirection);
            writer.Write(recoilVisualRotation);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            stage = reader.ReadInt32();
            stageTimer = reader.ReadInt32();
            overheatTimer = reader.ReadInt32();
            ProgressState = reader.ReadInt32();
            MaxHeatStage = reader.ReadInt32();
            LaserChainCount = reader.ReadInt32();
            currentEffectID = reader.ReadInt32();
            visualProgress = reader.ReadSingle();
            stageOutlineTimer = reader.ReadInt32();
            fireStopTimer = reader.ReadInt32();
            manualCoolingVisualTimer = reader.ReadInt32();
            forcedShutdownVisualDuration = reader.ReadInt32();
            manaStarvedStopTimer = reader.ReadInt32();
            recoilActive = reader.ReadBoolean();
            recoilDirection = reader.ReadInt32();
            recoilVisualRotation = reader.ReadSingle();
        }

        private void KillAndPreserveHeat()
        {
            PreserveHeatState();
            Projectile.Kill();
        }

        private void PreserveHeatState()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            if (!Main.player.IndexInRange(Projectile.owner))
                return;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
                return;

            SHPCRight_Player heatPlayer = owner.GetModPlayer<SHPCRight_Player>();
            if (heatPlayer.IsForcedShutdownCooling())
                return;

            int maxHeatStage = MaxHeatStage > 0 ? MaxHeatStage : balance.GetRightClickMaxHeatLevel();
            maxHeatStage = Utils.Clamp(maxHeatStage, 1, balance.GetRightClickMaxHeatLevel());
            int safeStage = Utils.Clamp(stage, 0, maxHeatStage);
            int fillTime = GetStageUpTime(Utils.Clamp(safeStage, 0, 4), maxHeatStage);
            int safeTimer = Utils.Clamp(stageTimer, 0, fillTime);

            heatPlayer.SyncHeatFromHoldout(safeStage, safeTimer, maxHeatStage);
        }
    }
}

