using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 青霆剑左键自定义挥舞弹幕：负责连段状态机、近战判定、飞剑/光球/落雷/巨剑生成和绘制。
    internal sealed class AzureThunderSwingHoldout : BaseCustomUseStyleProjectile, ILocalizedModType
    {
        // 旧资源尺寸和当前贴图尺寸不同，用比例补偿绘制缩放。
        private const float OldTextureSize = 158f;
        private const float CurrentTextureSize = 80f;
        private const float HoldoutDrawScale = OldTextureSize / CurrentTextureSize;
        private const float GripOriginX = 0f;
        private const float GripOriginY = 172f / HoldoutDrawScale;
        private const float LeftSweepLightningScale = 2.5f;
        private const int HarmonyStageLightningCount = 3;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunder";
        public override int AssignedItemID => ModContent.ItemType<AzureThunder>();

        public override Vector2 SpriteOrigin => new(0, 78); // 左右和上下偏移
        public override float HitboxOutset => 118f;
        public override Vector2 HitboxSize => new(170f, 170f);
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        // 连段状态：comboIndex 决定当前招式，stageTimer/stageDuration 控制单段生命周期。
        private int comboIndex;
        private int currentStage;
        private int stageTimer;
        private int stageDuration;
        private int stageImpactFrame;
        private int gapTimer;
        private int swingDirection = 1;

        // 单段事件锁，确保每段技能只触发一次或按计数触发。
        private bool stageActive;
        private bool releaseRequested;
        private bool releaseFinalStarted;
        private bool stageEventOne;
        private bool swingSoundPlayed;
        private bool postSwing;
        private int harmonyBarrageShotsFired;
        private int finalLightningFired;
        private float fadeIn;

        // 每段开始时锁定鼠标点和方向，保证后续弹幕围绕同一攻击意图展开。
        private Vector2 lockedMouseWorld;
        private Vector2 lockedAimDirection;

        // 常用玩家态快捷入口。
        private AzureThunderPlayer ThunderPlayer => Owner.GetModPlayer<AzureThunderPlayer>();
        private bool HarmonyActive => ThunderPlayer.HarmonyActive;

        // 普通状态四段连招，天理真和压缩为三段。
        private int ComboLength => HarmonyActive ? 3 : 4;

        public override void SetDefaults()
        {
            base.SetDefaults();
            // 近战 holdout 自身是魔法伤害，局部 NPC 免疫避免同段挥砍多次刷命中。
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.extraUpdates = 0;
            Projectile.scale = 1f;
        }

        public override void WhenSpawned()
        {
            // 自定义状态机接管物品动画，弹幕至少存活到玩家松开左键。
            IgnoreActiveAnimation = true;
            DrawUnconditionally = true;
            Projectile.timeLeft = 2;
            Projectile.knockBack = 0f;

            // 生成首帧锁定当前鼠标方向并决定玩家朝向。
            lockedMouseWorld = AzureThunderPlayer.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
            Projectile.ai[1] = -1f;

            if (ThunderPlayer.ConsumeDashHeavyStrike())
                comboIndex = 3;
            else if (ThunderPlayer.TryConsumeRetainedLeftCombo(out int retainedComboIndex))
                comboIndex = retainedComboIndex;
        }

        public override void AI()
        {
            if (whenSpawned)
            {
                // BaseCustomUseStyleProjectile 的首帧初始化延迟到 AI 中执行。
                WhenSpawned();
                whenSpawned = false;
                Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
                Projectile.netUpdate = true;
            }

            bool itemAnimationActive = Owner.ItemAnimationActive;
            if (Owner.HeldItem.type != AssignedItemID || Owner.dead)
            {
                // 切武器或死亡时立刻清理 holdout。
                Projectile.Kill();
                return;
            }

            // 左键挥舞和派生弹幕都需要灾厄鼠标监听。
            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;

            if (itemAnimationActive || IgnoreActiveAnimation)
            {
                // 物品动画仍在时推进自定义 UseStyle，并强制玩家手臂跟随剑。
                Animation++;
                UseStyle();
                Owner.heldProj = Projectile.whoAmI;
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
            }
            else
            {
                // 动画停下时重置基类状态，但 DrawUnconditionally 仍允许收尾绘制。
                Animation = 0f;
                if (DrawUnconditionally)
                {
                    Owner.heldProj = Projectile.whoAmI;
                    Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
                }

                NumberOfAnimations = 0;
                ResetStyle();
            }

            int itemAnimationMax = Math.Max(1, Owner.itemAnimationMax);
            AnimationProgress = Animation % itemAnimationMax;

            // 同步弹幕位置到玩家中心；AbsolutePosition 非零时支持基类位移逻辑。
            if (AbsolutePosition == Vector2.Zero)
                Projectile.position = Owner.position + Owner.Size / 2f - Projectile.Size / 2f + Offset;
            else
            {
                AbsolutePosition += Projectile.velocity;
                Projectile.position = AbsolutePosition - Projectile.Size / 2f + Offset;
            }

            if (AnimationProgress == itemAnimationMax - 1)
            {
                // 基类动画循环结束钩子。
                OnEndUse();
                NumberOfAnimations++;
            }

            if (Owner.itemAnimation == itemAnimationMax - 1)
            {
                // 新一轮物品动画开始钩子。
                Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
                OnBeginUse();
            }

            if (DrawUnconditionally)
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
        }

        public override bool? CanDamage() => CanHit;

        public override void UseStyle()
        {
            // 玩家失效时终止全部连段。
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != AssignedItemID)
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.timeLeft = 2;
            Projectile.Center = Owner.MountedCenter;

            // 按住左键继续连段；松开、开地图或被 UI 拦截会请求收尾。
            bool holdingLeft = Owner.channel &&
                (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                !Main.mapFullscreen &&
                !Main.blockMouse;

            if (!holdingLeft)
                releaseRequested = true;

            if (!stageActive)
            {
                // 段间隙阶段不造成伤害，只做回正和等待。
                CanHit = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.25f);

                if (gapTimer > 0)
                {
                    gapTimer--;
                    ApplyIdleRotation();
                    ApplyArmRotation();
                    return;
                }

                // 没有间隙时立即进入下一段。
                StartStage();
                ApplyArmRotation();
                return;
            }

            RunStage();
            ApplyArmRotation();
        }

        private void StartStage()
        {
            // 每段开始扣一次左键魔力；本地玩家负责扣费。
            if (Main.myPlayer == Projectile.owner && !ThunderPlayer.TrySpendMana())
            {
                Projectile.Kill();
                return;
            }

            stageActive = true;
            releaseFinalStarted = releaseRequested;
            currentStage = comboIndex % ComboLength;
            stageDuration = HarmonyActive ? 30 : 45;
            stageImpactFrame = HarmonyActive ? stageDuration / 3 : (int)(stageDuration / 1.5f);

            if (!HarmonyActive && currentStage == 3)
            {
                stageImpactFrame = 30;
                stageDuration = Math.Max(stageDuration, stageImpactFrame + (GetFinalLightningCount() - 1) * 8 + 10);
            }
            else if (HarmonyActive && currentStage <= 1)
            {
                stageImpactFrame = 10;
                stageDuration = Math.Max(stageDuration, stageImpactFrame + HarmonyStageLightningCount * 8 + 18);
            }
            else if (HarmonyActive && currentStage == 2)
            {
                stageImpactFrame = 10;
                stageDuration = Math.Max(stageDuration, stageImpactFrame + 34);
            }

            // 重置本段事件和命中状态。
            stageTimer = 0;
            stageEventOne = false;
            harmonyBarrageShotsFired = 0;
            finalLightningFired = 0;
            swingSoundPlayed = false;
            postSwing = false;
            CanHit = false;

            // 清空本弹幕对所有 NPC 的局部免疫，保证新一段能重新命中。
            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;

            // 每段起手重新锁定鼠标，后续派生弹幕都基于这个方向。
            lockedMouseWorld = AzureThunderPlayer.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
            swingDirection = comboIndex % 2 == 0 ? -1 : 1;
            Projectile.ai[1] = swingDirection;

            if (HarmonyActive && Main.myPlayer == Projectile.owner)
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, currentStage == 2 ? 7.5f : 5f);

            // 普通四段最后一段触发手持地剑数量返魔成长。
            if (!HarmonyActive && currentStage == 3)
                ThunderPlayer.RestoreManaForOwnedSwords(includeLeftClickGrowth: true);
        }

        private void RunStage()
        {
            stageTimer++;

            if (HarmonyActive)
            {
                RunHarmonyCastingStage();
                return;
            }

            if (stageTimer < SwingImpactFrame)
            {
                // 前摇期持续跟随鼠标修正方向，直到进入挥砍命中帧。
                UpdateLockedAimFromMouse();
                postSwing = false;
                CanHit = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.35f);
                Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.1f);
                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction * GetWindupAngleScale()),
                    0.2f);

                if (stageTimer >= stageDuration)
                    EndStage();

                return;
            }

            // 进入挥砍期后锁定 sprite 翻转，避免挥砍中途跳边。
            if (!postSwing)
                FlipAsSword = Owner.direction < 0;

            postSwing = true;
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.1f);

            float swingTime = stageTimer - stageDuration / 3f;
            float swingTimeMax = stageDuration - stageDuration / 3f;
            float swingProgress = MathHelper.Clamp(swingTime / swingTimeMax, 0f, 1f);

            // 只有挥砍中段打开 melee hitbox，前后摇都不伤害。
            bool hitWindow = swingTime > (int)(swingTimeMax * 0.4f) && swingTime < (int)(swingTimeMax * 0.7f);
            CanHit = hitWindow;
            fadeIn = MathHelper.Lerp(fadeIn, hitWindow ? 1f : 0f, hitWindow ? 0.3f : 0.35f);

            if (swingTime >= (int)(swingTimeMax * 0.4f) && !swingSoundPlayed)
            {
                // 命中窗口开始时播放挥剑音。
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.75f, Pitch = Main.rand.NextFloat(0.08f, 0.18f) }, Owner.Center);
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.35f, Pitch = 0.3f }, Owner.Center);
                swingSoundPlayed = true;
            }

            RotationOffset = MathHelper.Lerp(
                RotationOffset,
                MathHelper.ToRadians(MathHelper.Lerp(
                    150f * Projectile.ai[1] * Owner.direction,
                    120f * -Projectile.ai[1] * Owner.direction,
                    CalamityUtils.ExpInOutEasing(swingProgress, 1))),
                0.2f);

            // 终极状态和普通状态使用完全不同的派生弹幕表。
            RunNormalStage();

            SpawnSwingParticles(swingProgress);

            if (stageTimer >= stageDuration)
                EndStage();
        }

        private void RunHarmonyCastingStage()
        {
            CanHit = false;
            fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.4f);
            UpdateLockedAimFromMouse();
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.16f);
            RotationOffset = MathHelper.Lerp(RotationOffset, 0f, 0.28f);
            RunHarmonyStage();

            if (stageTimer >= stageDuration)
                EndStage();
        }

        private void UpdateLockedAimFromMouse()
        {
            // 前摇期允许玩家微调方向，更新锁定点和朝向。
            lockedMouseWorld = AzureThunderPlayer.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
        }

        private float GetWindupAngleScale()
        {
            // 第一段接近释放时加大蓄力角度，后续连段保持更直接的挥砍。
            float chainedSwingBoost = comboIndex > 0 ? 1f : Utils.GetLerpValue(stageDuration * 0.8f, stageDuration, stageTimer, true);
            return 1f + chainedSwingBoost * 0.35f;
        }

        // 终极状态更快进入命中帧，普通状态保留较长前摇。
        private int SwingImpactFrame => stageImpactFrame;
        private bool ReachedSwingImpact(int delay = 0) => stageTimer >= SwingImpactFrame + delay;

        private void RunNormalStage()
        {
            // 普通四段：光球、飞剑+雷、更多飞剑+雷、连续终结落雷。
            switch (currentStage)
            {
                case 0:
                    if (!stageEventOne && ReachedSwingImpact())
                    {
                        SpawnLightOrbs();
                        stageEventOne = true;
                    }
                    break;

                case 1:
                    if (!stageEventOne && ReachedSwingImpact())
                    {
                        SpawnFlyingSwords(2, 16, false);
                        SpawnMouseLightning(0.4f, false);
                        stageEventOne = true;
                    }
                    break;

                case 2:
                    if (!stageEventOne && ReachedSwingImpact())
                    {
                        SpawnFlyingSwords(4, 17, true);
                        SpawnMouseLightning(0.5f, false);
                        stageEventOne = true;
                    }
                    break;

                case 3:
                    int strikeCount = GetFinalLightningCount();
                    if (finalLightningFired < strikeCount && ReachedSwingImpact(finalLightningFired * 8))
                    {
                        SpawnForwardLightning(finalLightningFired);
                        finalLightningFired++;
                    }
                    break;
            }
        }

        private void RunHarmonyStage()
        {
            // 终极前两段变成平行雷幕。
            if (currentStage <= 1)
            {
                if (harmonyBarrageShotsFired < HarmonyStageLightningCount && ReachedSwingImpact(harmonyBarrageShotsFired * 8))
                {
                    SpawnParallelBarrageLightning(harmonyBarrageShotsFired);
                    harmonyBarrageShotsFired++;
                }

                if (!stageEventOne && harmonyBarrageShotsFired >= HarmonyStageLightningCount && ReachedSwingImpact(HarmonyStageLightningCount * 8))
                {
                    SpawnGrandSword(0.78f, suppressPetStrongAttack: true);
                    stageEventOne = true;
                }

                return;
            }

            // 终极第三段召唤巨剑。
            if (!stageEventOne && ReachedSwingImpact())
            {
                SpawnHarmonyFinalJudgement();
                stageEventOne = true;
            }
        }

        private int GetFinalLightningCount()
        {
            // 犽戎后且地剑满上限时，普通最终段升级为 9 道雷。
            if (AzureThunderProgression.DownedYharon && AzureThunderPlayer.CountOwnedGroundSwords(Owner) >= AzureThunderGroundSword.MaxGroundSwords)
                return 9;

            return 3 + (AzureThunderProgression.DownedDragonfolly ? 1 : 0);
        }

        private void EndStage()
        {
            // 结束当前段并进入 5 帧段间隙；如果松手已请求收尾则直接销毁。
            stageActive = false;
            CanHit = false;
            stageTimer = 0;
            comboIndex++;
            swingDirection = comboIndex % 2 == 0 ? -1 : 1;
            Projectile.ai[1] = swingDirection;

            if (releaseRequested || releaseFinalStarted)
            {
                if (!HarmonyActive)
                    ThunderPlayer.RetainLeftCombo(comboIndex % 4);

                Projectile.Kill();
                return;
            }

            gapTimer = 5;
        }

        private void ApplyIdleRotation()
        {
            // 段间隙持续看向鼠标，保持手感跟随。
            UpdateLockedAimFromMouse();
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.3f);
            RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(40f * Projectile.ai[1] * Owner.direction), 0.18f);
        }

        private void ApplyArmRotation()
        {
            // 手臂固定为全伸展握剑角度。
            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        private void SpawnLightOrbs()
        {
            // 第一段在锁定方向两侧生成四颗追踪光球。
            if (Main.myPlayer != Projectile.owner)
                return;

            AzureThunderSounds.PlayOrbRelease(Owner.Center);
            for (int i = 0; i < 4; i++)
            {
                // 两列两排排布，避免光球从完全同一点发射。
                int side = i % 2 == 0 ? -1 : 1;
                int row = i / 2;
                Vector2 spawnPosition = GetDanceOfLightSpawnPosition(lockedAimDirection, side) - lockedAimDirection * row * 92f;
                Vector2 velocity = lockedAimDirection.RotatedBy(side * (0.14f + row * 0.05f)) * 28f;

                int orb = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<AzureThunderLightOrb>(),
                    Math.Max(1, (int)(Projectile.damage * 0.35f)),
                    Projectile.knockBack,
                    Projectile.owner);

                if (Main.projectile.IndexInRange(orb))
                    AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[orb]);
            }
        }

        private void SpawnMouseLightning(float damageFactor, bool gainCharge)
        {
            // 第二、三段落雷优先打鼠标附近目标，没有目标就劈鼠标点。
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = AzureThunderPlayer.FindMouseNearestTarget(Owner);
            Vector2 impactPosition = target?.Center ?? lockedMouseWorld;

            // 第二/三段左键雷现在是真正的头顶落雷，不再从侧边发射。
            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                impactPosition,
                target,
                Math.Max(1, (int)(Projectile.damage * damageFactor)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: gainCharge,
                applyStaticDischarge: false,
                big: false,
                spawnHeightMultiplier: 0.9f,
                weak: true,
                lightningScale: 0.72f);
        }

        private void SpawnForwardLightning(int index)
        {
            // 普通最终段沿玩家瞄准方向铺开多道落雷。
            if (Main.myPlayer != Projectile.owner)
                return;

            int totalStrikes = GetFinalLightningCount();
            Vector2 forward = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float centeredIndex = index - (totalStrikes - 1) * 0.5f;
            float forwardDistance = 250f + index * 170f + Main.rand.NextFloat(-42f, 56f);
            float sideOffset = centeredIndex * 128f + Main.rand.NextFloat(-48f, 48f);
            Vector2 strikeTarget = Owner.Center + forward * forwardDistance + right * sideOffset - Vector2.UnitY * Main.rand.NextFloat(20f, 90f);

            // 最终连雷拉大间距并放大到覆盖约三分之一屏幕。
            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                strikeTarget,
                null,
                Math.Max(1, (int)(Projectile.damage * 1.75f)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: true,//index == totalStrikes - 1,
                applyStaticDischarge: false,//施加静电逸散
                big: true,
                spawnHeightMultiplier: 0.95f,
                fixedTiltRadians: GetFixedLightningTilt(),
                normalVisualIntensity: true,
                lightningScale: LeftSweepLightningScale);
        }

        private void SpawnParallelBarrageLightning(int index)
        {
            // 终极前两段围绕锁定鼠标点横向扫出雷幕。
            if (Main.myPlayer != Projectile.owner)
                return;

            int strikeCount = HarmonyStageLightningCount;
            Vector2 sweepAxis = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction).RotatedBy(MathHelper.PiOver2);
            float centeredIndex = index - (strikeCount - 1) * 0.5f;
            Vector2 focusPoint = lockedMouseWorld + Main.rand.NextVector2Circular(44f, 34f);
            Vector2 lineCenter = focusPoint + sweepAxis * (centeredIndex * 138f + Main.rand.NextFloat(-34f, 34f));
            lineCenter += lockedAimDirection * Main.rand.NextFloat(-54f, 54f);

            // 天理真和雷幕围绕捕获的鼠标区域扫过，而不是强锁一个目标。
            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                lineCenter,
                null,
                Math.Max(1, (int)(Projectile.damage * 0.8f)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: false,
                applyStaticDischarge: true,
                big: true,
                spawnHeightMultiplier: 0.82f,
                fixedTiltRadians: GetFixedLightningTilt(),
                oneThirdVisualIntensity: true,
                lightningScale: LeftSweepLightningScale);
        }

        private void SpawnFlyingSwords(int count, int delay, bool behindOwner)
        {
            // 飞剑由本地玩家生成，并把锁定鼠标点塞入 ai[1]/ai[2]。
            if (Main.myPlayer != Projectile.owner)
                return;

            AzureThunderSounds.PlaySwordMaterialize(Owner.Center);
            Vector2 right = lockedAimDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < count; i++)
            {
                // behindOwner=true 时按两侧队列从玩家身后出现。
                float centeredIndex = i - (count - 1) * 0.5f;
                Vector2 spawnPosition;
                if (behindOwner)
                {
                    int row = i / 2;
                    int side = i % 2 == 0 ? -1 : 1;
                    spawnPosition = GetDanceOfLightSpawnPosition(lockedAimDirection, side) - lockedAimDirection * row * 36f;
                }
                else
                    spawnPosition = GetDanceOfLightSpawnPosition(lockedAimDirection, centeredIndex < 0f ? -1 : 1);

                int sword = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Vector2.Zero,
                    ModContent.ProjectileType<AzureThunderFlyingSword>(),
                    Math.Max(1, (int)(Projectile.damage * 0.25f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    delay + i * 2,
                    lockedMouseWorld.X,
                    lockedMouseWorld.Y);

                if (Main.projectile.IndexInRange(sword))
                    AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[sword]);
            }
        }

        private void SpawnHarmonyFinalJudgement()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = AzureThunderPlayer.FindMouseNearestTarget(Owner);
            Vector2 impactPosition = target?.Center ?? lockedMouseWorld;
            float damageFactor = AzureThunderProgression.UltimateRightClickFinalDamageFactor * 1.12f;

            int judgement = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                impactPosition - Vector2.UnitY * 920f,
                Vector2.UnitY,
                ModContent.ProjectileType<AzureThunderFinalJudgementBolt>(),
                Math.Max(1, (int)(Projectile.damage * damageFactor)),
                Projectile.knockBack,
                Projectile.owner,
                target?.whoAmI ?? -1,
                impactPosition.X,
                impactPosition.Y);

            if (Main.projectile.IndexInRange(judgement))
                AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[judgement]);

            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 10f);
        }

        private void SpawnGrandSword(float damageMultiplier = 1f, bool suppressPetStrongAttack = false)
        {
            // 终极第三段召唤巨剑，目标优先取鼠标附近 NPC。
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = AzureThunderPlayer.FindMouseNearestTarget(Owner);
            Vector2 impactPosition = target?.Center ?? lockedMouseWorld;
            Vector2 spawnPosition = Owner.MountedCenter + lockedAimDirection * 48f;
            spawnPosition = impactPosition - Vector2.UnitY * 780f;

            // ai[0] 传目标索引，ai[1]/ai[2] 传兜底落点。
            int grandSword = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                Vector2.Zero,
                ModContent.ProjectileType<AzureThunderGrandSword>(),
                Math.Max(1, (int)(Projectile.damage * AzureThunderProgression.UltimateGrandSwordDamageFactor * damageMultiplier)),
                Projectile.knockBack,
                Projectile.owner,
                target?.whoAmI ?? -1,
                impactPosition.X,
                impactPosition.Y);

            if (Main.projectile.IndexInRange(grandSword))
            {
                Terraria.Projectile sword = Main.projectile[grandSword];
                if (suppressPetStrongAttack)
                    sword.localAI[1] = 1f;
                AzureThunderPlayer.ApplyProjectileGrowth(sword);
            }
        }

        private Vector2 GetDanceOfLightSpawnPosition(Vector2 shootDirection, float sideBias)
        {
            // 根据射击方向和左右偏置，计算光球/飞剑的散布出生点。
            shootDirection = shootDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            float shootAngle = shootDirection.ToRotation();
            float side = sideBias == 0f ? (Main.rand.NextBool() ? -1f : 1f) : Math.Sign(sideBias);
            float offsetAngle = MathHelper.Pi + side * Main.rand.NextFloat(0.18f * MathHelper.Pi, 0.4f * MathHelper.Pi);
            Vector2 offset = offsetAngle.ToRotationVector2().RotatedBy(shootAngle) * Main.rand.NextFloat(40f, 140f);
            return Owner.MountedCenter + offset;
        }

        private float GetFixedLightningTilt()
        {
            // 让落雷略微向瞄准方向倾斜，视觉上更像从前方斩落。
            int sweepDirection = Math.Abs(lockedAimDirection.X) > 0.05f ? Math.Sign(lockedAimDirection.X) : Owner.direction;
            return MathHelper.ToRadians(sweepDirection < 0 ? 10f : -10f);
        }

        private void SpawnSwingParticles(float progress)
        {
            // 只有命中窗口生成挥砍线粒子，避免前摇粒子误导 hitbox。
            if (!CanHit)
                return;

            Vector2 slashDirection = (FinalRotation + MathHelper.ToRadians(-45f)).ToRotationVector2();
            Vector2 right = slashDirection.RotatedBy(MathHelper.PiOver2);
            float distance = HarmonyActive ? 210f : currentStage == 3 ? 205f : 160f;

            for (int i = 0; i < 3; i++)
            {
                // 沿斩击方向随机撒线粒子，表现剑气轨迹。
                Vector2 position = Owner.Center + slashDirection * Main.rand.NextFloat(40f, distance) + right * Main.rand.NextFloat(-20f, 20f);
                Vector2 velocity = -slashDirection.RotatedByRandom(0.25f) * Main.rand.NextFloat(2f, 5f);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.NextBool(3) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure));
            }

            if (Main.rand.NextBool(2))
            {
                // 斩击末端补一颗 dust，增强刀尖扫过感。
                Dust dust = Dust.NewDustPerfect(
                    Owner.Center + slashDirection * distance + Main.rand.NextVector2Circular(24f, 24f),
                    DustID.FireworksRGB,
                    -slashDirection.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.2f, 4f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.Yellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // holdout 本体命中始终附加基础电击，终极期间额外升级为终极 DoT。
            if (HarmonyActive)
            {
                AzureThunderPlayer.ApplyUltimateDot(target, 180);
                AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (HarmonyActive)
                return false;

            // 非强制绘制且物品动画结束时不绘制 holdout。
            if (!DrawUnconditionally && Owner.itemAnimation <= 0)
                return false;

            // 主体、幽灵和 smear 三层贴图共同组成挥砍表现。
            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
            Asset<Texture2D> ghost = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunderGhost");
            Asset<Texture2D> swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");

            float r = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
            SpriteEffects effects = spriteEffects != SpriteEffects.None ? spriteEffects : (FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            Vector2 origin = FlipAsSword ? new Vector2(texture.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float drawScale = Projectile.scale * HoldoutDrawScale;

            // smear 贴图是主挥砍弧光，fadeIn 只在命中窗口附近升高。
            Main.EntitySpriteDraw(
                swoosh.Value,
                drawPosition,
                null,
                AzureThunderColors.Azure with { A = 0 } * fadeIn * 0.38f,
                (FinalRotation + MathHelper.ToRadians(45f)) + MathHelper.ToRadians(Projectile.ai[1] == 1f ? -90f : 90f) * -Owner.direction,
                swoosh.Size() * 0.5f,
                Projectile.scale * (HarmonyActive ? 0.86f : 0.68f),
                SpriteEffects.None);

            for (int i = 0; i < 20; i++)
            {
                // 环形幽灵影作为剑身辉光，终极状态颜色更偏金。
                Vector2 offset = (MathHelper.TwoPi * i / 20f).ToRotationVector2() * 4.5f * fadeIn;
                Color auraColor = Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.Yellow, HarmonyActive ? 0.65f : 0.22f) with { A = 0 };

                Main.EntitySpriteDraw(
                    ghost.Value,
                    drawPosition + offset,
                    ghost.Value.Frame(1, FrameCount, 0, Frame),
                    auraColor * 0.13f * fadeIn,
                    Projectile.rotation + RotationOffset + r,
                    origin,
                    drawScale,
                    effects);
            }

            Main.EntitySpriteDraw(
                texture.Value,
                drawPosition,
                texture.Value.Frame(1, FrameCount, 0, Frame),
                lightColor,
                Projectile.rotation + RotationOffset + r,
                origin,
                drawScale,
                effects);

            return false;
        }
    }
}
