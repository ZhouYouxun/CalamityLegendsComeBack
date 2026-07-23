using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    public class AegisSwingHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private const int SwingDuration = 52;
        private const float FireballSpawnProgress = 0.3f;
        private const float SwordVisualScale = 1.55f;
        private const float BladeReach = 104f;
        private const float SpinDiscRadiusScale = 0.134f;
        private const float SpinDiscSpriteScale = 0.201f;
        private const float SpinSprayRadiusScale = 0.8f;    // 线性粒子喷射覆盖半径（独立于刀盘）
        private const float SpinSpraySpriteScale = 0.3f;    // 线性粒子本体大小（独立于刀盘）
        private const int BladeTrailHistoryFrames = 16;
        private const int TrackingSoulInterval = 4;
        private const int TrackingSoulBurstCount = 2;
        private const float TrackingSoulSpeed = 30f * 0.67f;
        private const float SpinAcceleration = 0.075f;
        private const int ReleaseFadeFrames = 10;
        private const float DiscBrightness = 0.67f;
        private const float LoopSweepDegrees = 1080f;   // 每次挥动 3 圈，线性匀速

        // ── 视觉重做新增常量 ─────────────────────────────────────────────
        // 旧版把「刀盘」画在半径约 47px 处，而真实伤害判定是 146px 的核心框推到 124px 外，
        // 剑贴图本身也画到 161px 远 —— 于是玩家看到的是一把大剑外加一个对不上的小光盘。
        // 现在圣火轮盘直接跟着剑刃的真实长度走，视觉半径 = 判定半径。
        private const float WheelRadiusScale = 0.94f;
        private const int BladeAfterimageCount = 11;      // 沿弧线拖在身后的剑残像
        private const float BladeAfterimageStep = 0.145f; // 每一片残像回退的弧度
        private const int WheelSmearCount = 3;            // 新月拖抹片数量

        private Player Owner => Main.player[Projectile.owner];
        private readonly Vector2[] bladeTipHistory = new Vector2[BladeTrailHistoryFrames];
        private int bladeTipHistoryLength;
        private int swingCount;
        private int stateTimer;
        private int swingDirection = 1;
        private bool fireballSpawned;
        private float currentAngle;
        private float startAngle;
        private float endAngle;
        private Vector2 lockedMouseDirection = Vector2.UnitX;
        private float scale = 1f;
        private float swingProgress;
        private float spinSpeedFactor;
        private float visualOpacity = 1f;
        private float slashOpacity;
        private float outlineBaseOpacity;
        private float outlinePulseOpacity;
        private float discRingOpacity;   // 刀盘环形光圈强度
        private float discFadeIn;        // 刀盘整体淡入（20帧内透明度逐渐降低至完全显示）
        private bool releaseEnding;
        private int releaseTimer;
        private const int DiscFadeInFrames = 20;

        private float BladeRadius => BladeReach * scale * WheelRadiusScale;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = SwingDuration;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        private bool IsLeftHeld()
        {
            return Owner.channel && (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        private Vector2 GetMouseDirection()
        {
            return Owner.MountedCenter.DirectionTo(AegisBlade.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (Owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                if (Owner.heldProj == Projectile.whoAmI)
                    Owner.heldProj = -1;

                Owner.itemTime = 0;
                Owner.itemAnimation = 0;
                Projectile.Kill();
                return;
            }

            discFadeIn = Math.Min(discFadeIn + 1f / DiscFadeInFrames, 1f);
            scale = Owner.GetMeleeScale() * SwordVisualScale;
            Owner.GetModPlayer<AegisBladePlayer>().IsSwinging = true;
            if (!IsLeftHeld())
                releaseEnding = true;

            if (!releaseEnding)
            {
                Owner.heldProj = Projectile.whoAmI;
                Owner.itemTime = Math.Max(Owner.itemTime, 2);
                Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            }
            Projectile.timeLeft = 4;
            Projectile.Center = Owner.MountedCenter;

            DoSwing();

            AegisVisuals.Light(Owner.MountedCenter, 0.85f * visualOpacity);

            float armAngle = currentAngle - MathHelper.ToRadians(130f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = currentAngle;
        }

        private void StartSwing()
        {
            stateTimer = 0;
            swingProgress = 0f;
            fireballSpawned = false;
            releaseTimer = 0;
            lockedMouseDirection = GetMouseDirection();

            if (swingCount == 0)
            {
                swingDirection = -Math.Sign(Owner.Center.X - AegisBlade.GetMouseWorld(Owner).X);
                if (swingDirection == 0)
                    swingDirection = Owner.direction;
                // 第一圈从瞄准方向直接起步，无初始偏移
                currentAngle = lockedMouseDirection.ToRotation();
                SpawnIgnitionBurst();
            }
            Owner.direction = swingDirection;

            startAngle = currentAngle;
            endAngle   = startAngle + MathHelper.ToRadians(LoopSweepDegrees * swingDirection);
        }

        /// <summary>起手点火：圣印从地上升起，火星向内汇聚，武器"被点燃"。</summary>
        private void SpawnIgnitionBurst()
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = 0.25f }, Owner.Center);
            AegisVisuals.CoronaRing(Owner.MountedCenter, 14, 1.1f, lockedMouseDirection.ToRotation());
            AegisVisuals.WarbannerConverge(Owner.MountedCenter, lockedMouseDirection, 1.6f, 10, 1.1f);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Owner.MountedCenter, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Gold, 0.8f), AegisVisuals.TexBloom, Vector2.One, 0f,
                0.05f, 0.85f, 14));
        }

        private bool isThrown;
        private Vector2 throwTargetPos;
        private float throwDistProgress;

        private void DoSwing()
        {
            if (stateTimer == 0)
                StartSwing();

            if (!IsLeftHeld())
                releaseEnding = true;

            if (releaseEnding)
            {
                if (!isThrown)
                {
                    isThrown = true;
                    throwTargetPos = AegisBlade.GetMouseWorld(Owner);
                    throwDistProgress = 0f;
                    SoundEngine.PlaySound(SoundID.Item120 with { Volume = 0.75f, Pitch = -0.1f }, Projectile.Center);
                    SpawnThrowLaunchBurst();
                }

                releaseTimer++;
                throwDistProgress = MathHelper.Clamp(throwDistProgress + 0.05f, 0f, 1f);
                float easeDist = MathF.Sin(throwDistProgress * MathHelper.PiOver2);

                Vector2 targetCenter = Owner.MountedCenter + Owner.MountedCenter.DirectionTo(throwTargetPos).SafeNormalize(Vector2.UnitX * Owner.direction) * (520f * easeDist);
                Projectile.Center = Vector2.Lerp(Projectile.Center, targetCenter, 0.25f);
                currentAngle += 0.42f * swingDirection;

                visualOpacity = 1f - MathHelper.Clamp((releaseTimer - 20) / (float)ReleaseFadeFrames, 0f, 1f);
                TrackBladeTrail(false);
                EmitThrownFlames();
                stateTimer++;

                if (releaseTimer >= ReleaseFadeFrames + 20)
                    Projectile.Kill();

                return;
            }

            spinSpeedFactor = MathHelper.Lerp(spinSpeedFactor, 1f, SpinAcceleration);
            if (spinSpeedFactor > 0.985f)
                spinSpeedFactor = 1f;
            visualOpacity = MathHelper.Lerp(visualOpacity, 1f, 0.18f);

            swingProgress = MathHelper.Clamp(swingProgress + spinSpeedFactor / SwingDuration, 0f, 1f);
            float progress = GetCurrentSwingProgress();
            currentAngle = EvaluateSwingAngle(progress);

            // 每帧跟刀尖方向实时更新玩家朝向
            Owner.direction = MathF.Cos(currentAngle) >= 0f ? 1 : -1;

            if (!releaseEnding && Main.myPlayer == Projectile.owner && stateTimer % TrackingSoulInterval == 0)
                SpawnTrackingSouls();

            if (!releaseEnding && !fireballSpawned && progress >= FireballSpawnProgress && Main.myPlayer == Projectile.owner)
            {
                SpawnBigFireballs();
                SpawnOrbitalStrikes();
                fireballSpawned = true;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.82f, Pitch = Main.rand.NextFloat(-0.12f, 0.16f) }, Owner.Center);
                SpawnDetonationPulse();
            }

            bool hitWindow = !releaseEnding && progress >= 0.23f && progress <= 0.87f;
            TrackBladeTrail(hitWindow);
            EmitSwingTrail(hitWindow);

            if (progress < 1f)
            {
                stateTimer++;
                return;
            }

            if (IsLeftHeld())
            {
                swingCount++;
                Projectile.ResetLocalNPCHitImmunity();
                stateTimer = 0;
                swingProgress = 0f;
                fireballSpawned = false;
                return;
            }

            releaseEnding = true;
        }

        /// <summary>火球齐射的那一帧：整个轮盘对外炸出一圈日冕，配合震屏。</summary>
        private void SpawnDetonationPulse()
        {
            if (Main.dedServ)
                return;

            AegisVisuals.CoronaRing(Owner.MountedCenter, 18, 1.35f, currentAngle);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Owner.MountedCenter, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Flame, 0.75f), AegisVisuals.TexSoftExplosion, Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi), 0.008f, 0.05f, 22));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Owner.MountedCenter, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Ember, 0.8f), AegisVisuals.TexShatteredExplosion, Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi), 0.006f, 0.036f, 18));
            AegisVisuals.Screenshake(Owner.Center, 2.4f, 900f);
        }

        /// <summary>脱手甩出的瞬间：向瞄准方向劈出一道定向冲击。</summary>
        private void SpawnThrowLaunchBurst()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Owner.MountedCenter.DirectionTo(throwTargetPos).SafeNormalize(Vector2.UnitX * Owner.direction);
            AegisVisuals.DirectionalImpact(Owner.MountedCenter + direction * 30f, direction, 1.15f);
            AegisVisuals.EmberJet(Owner.MountedCenter + direction * 24f, direction, 12, 1.2f, 0.28f);
            AegisVisuals.Screenshake(Owner.Center, 1.8f, 700f);
        }

        private float GetCurrentSwingProgress()
        {
            return MathHelper.Clamp(swingProgress, 0f, 1f);
        }

        private float EvaluateSwingAngle(float progress)
        {
            // 线性匀速旋转，消除循环之间的加减速停顿感
            return startAngle + MathHelper.ToRadians(LoopSweepDegrees * swingDirection) * progress;
        }

        private void SpawnBigFireballs()
        {
            int fireballType = ModContent.ProjectileType<AegisBigFireball>();
            int damage = Math.Max(1, (int)(Projectile.damage * 0.85f));
            Vector2 aimDir = lockedMouseDirection.SafeNormalize(Vector2.UnitX * Owner.direction);

            // 向左右两侧75度角发射2个大火球
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 shootVel = aimDir.RotatedBy(MathHelper.ToRadians(75f * i)) * 14f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, shootVel,
                    fireballType, damage, Projectile.knockBack * 0.5f, Projectile.owner);
            }
        }

        private void SpawnTrackingSouls()
        {
            int laserType = ModContent.ProjectileType<AegisBorrowedLazharLaser>();
            int laserDamage = Math.Max(1, (int)(Projectile.damage * 0.42f));
            Vector2 mousePosition = AegisBlade.GetMouseWorld(Owner);
            float discRadius = BladeReach * scale * SpinDiscRadiusScale * 2.2f;

            for (int i = 0; i < TrackingSoulBurstCount; i++)
            {
                // 从圆盘边缘发向鼠标/目标位置
                Vector2 edgeOffset = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * discRadius;
                Vector2 spawnPosition = Projectile.Center + edgeOffset;
                Vector2 shootDirection = spawnPosition.DirectionTo(mousePosition)
                    .SafeNormalize(Vector2.UnitX * Owner.direction);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    shootDirection * TrackingSoulSpeed,
                    laserType,
                    laserDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }

        private void SpawnOrbitalStrikes()
        {
            int orbitalType = ModContent.ProjectileType<AegisBorrowedOrbitalStrike>();
            int orbitalDamage = Math.Max(1, (int)(Projectile.damage * 0.52f));

            for (int i = 0; i < 4; i++)
            {
                int targetIndex = FindOrbitalTargetIndex();
                Vector2 destination = targetIndex >= 0
                    ? Main.npc[targetIndex].Center + Main.rand.NextVector2Circular(42f, 34f)
                    : AegisBlade.GetMouseWorld(Owner);

                Vector2 spawnPosition = new(
                    destination.X,
                    destination.Y - Main.rand.NextFloat(940f, 1220f));

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Vector2.UnitY * Main.rand.NextFloat(34f, 42f),
                    orbitalType,
                    orbitalDamage,
                    Projectile.knockBack * 0.55f,
                    Projectile.owner,
                    targetIndex,
                    destination.X,
                    destination.Y);
            }
        }

        private int FindOrbitalTargetIndex()
        {
            const float range = 100f * 16f;
            int bestIndex = -1;
            float bestDistance = range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.life <= 0 || !npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Owner.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestIndex = i;
            }

            return bestIndex;
        }

        public override bool? CanDamage()
        {
            float progress = GetCurrentSwingProgress();
            return !releaseEnding && progress >= 0.23f && progress <= 0.87f ? null : false;
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            hitbox = GetSwingDamageHitbox();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle swingHitbox = GetSwingDamageHitbox();
            bool boxHit = Collision.CheckAABBvAABBCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                swingHitbox.TopLeft(),
                swingHitbox.Size());

            if (boxHit)
                return null;

            Vector2 direction = currentAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 start = Owner.MountedCenter + direction * (26f * scale);
            Vector2 end = Owner.MountedCenter + direction * ((BalanceAegisBlade.LeftClickCoreHitboxOutset + BalanceAegisBlade.LeftClickCoreHitboxSize * 0.55f) * scale);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, BalanceAegisBlade.LeftClickCoreHitboxSize * 0.36f * scale, ref collisionPoint);
        }

        private Rectangle GetSwingDamageHitbox()
        {
            Vector2 direction = currentAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            float hitboxSize = BalanceAegisBlade.LeftClickCoreHitboxSize * scale;
            Vector2 center = Owner.MountedCenter + direction * BalanceAegisBlade.LeftClickCoreHitboxOutset * scale;
            return new Rectangle(
                (int)(center.X - hitboxSize * 0.5f),
                (int)(center.Y - hitboxSize * 0.5f),
                (int)hitboxSize,
                (int)hitboxSize);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnBodyHitEffects(target);

            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<AegisSparkExplosion>(), Math.Max(1, (int)(Projectile.damage * 0.42f)),
                Projectile.knockBack * 0.35f, Projectile.owner);
        }

        private void SpawnBodyHitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = currentAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);

            // 沿刀锋方向的定向冲击（外扁内亮），而不是一个圆环了事
            AegisVisuals.DirectionalImpact(target.Center, direction, 1f);
            AegisVisuals.EmberJet(target.Center, direction, 7, 0.95f, 0.62f);

            // 正义旗式火光被"吸"进敌人身体：玩家越近，火越猛
            float intensity = Utils.Remap(Vector2.Distance(Owner.Center, target.Center), 620f, 90f, 1.1f, 2.4f, true);
            AegisVisuals.WarbannerConverge(target.Center,
                Owner.Center.DirectionTo(target.Center).SafeNormalize(direction),
                intensity, 4, 1f + target.Hitbox.Width / 380f);

            for (int i = 0; i < 6; i++)
            {
                Dust ember = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(14f, 14f),
                    AegisVisuals.ProfanedFireDust, direction.RotatedByRandom(0.75f) * Main.rand.NextFloat(3f, 10f),
                    0, Color.White, Main.rand.NextFloat(1f, 1.7f));
                ember.noGravity = true;
            }
        }

        private void TrackBladeTrail(bool hitWindow)
        {
            if (!hitWindow)
            {
                slashOpacity    = MathHelper.Lerp(slashOpacity,    0f, 0.25f);
                discRingOpacity = MathHelper.Lerp(discRingOpacity, 0f, 0.18f);
                outlineBaseOpacity = MathHelper.Lerp(outlineBaseOpacity, 0f, 0.12f);
                outlinePulseOpacity = MathHelper.Lerp(outlinePulseOpacity, 0f, 0.32f);
                if (slashOpacity < 0.01f)
                    bladeTipHistoryLength = 0;
                return;
            }

            slashOpacity    = MathHelper.Lerp(slashOpacity,    1f, 0.42f);
            discRingOpacity = MathHelper.Lerp(discRingOpacity, 1f, 0.22f);
            outlineBaseOpacity = MathHelper.Clamp(outlineBaseOpacity + 0.018f, 0f, 0.58f);
            float pulseDrop = MathHelper.Lerp(0.09f, 0.24f, outlinePulseOpacity * outlinePulseOpacity);
            outlinePulseOpacity = MathHelper.Max(0f, outlinePulseOpacity - pulseDrop);
            // 刀光轨迹取剑身 78% 处，让 primitive 光带正好压在刃口上
            Vector2 tipOffset = currentAngle.ToRotationVector2() * BladeRadius * 0.78f;
            Array.Copy(bladeTipHistory, 0, bladeTipHistory, 1, bladeTipHistory.Length - 1);
            bladeTipHistory[0] = tipOffset;
            if (bladeTipHistoryLength < bladeTipHistory.Length)
                bladeTipHistoryLength++;
        }

        /// <summary>
        /// 轮盘边缘的切向火星喷射。旧版是 7 条随机 LineParticle，现在改成
        /// 「切向火星 + 圣火尘 + 逆向深灰圣灰」三层，火是被离心力甩出去的，不是凭空撒的。
        /// </summary>
        private void EmitSwingTrail(bool hitWindow)
        {
            if (Main.dedServ || !hitWindow)
                return;

            float sprayRadius = BladeReach * scale * SpinSprayRadiusScale;
            float ringPhase = stateTimer * 0.38f * swingDirection;

            for (int i = 0; i < 5; i++)
            {
                float angle = ringPhase + MathHelper.TwoPi * i / 5f + Main.rand.NextFloat(-0.22f, 0.22f);
                Vector2 orbitDir = angle.ToRotationVector2();
                Vector2 pos = Owner.MountedCenter + orbitDir * Main.rand.NextFloat(sprayRadius * 0.5f, sprayRadius * 1.2f);
                Vector2 tangent = orbitDir.RotatedBy(MathHelper.PiOver2 * swingDirection);

                // 切向甩出的火星（离心）
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    pos,
                    tangent * Main.rand.NextFloat(1.4f, 4.6f) + orbitDir * Main.rand.NextFloat(0.1f, 1.2f),
                    false, Main.rand.Next(9, 19), Main.rand.NextFloat(0.28f, 0.72f) * SpinSpraySpriteScale,
                    AegisVisuals.Gradient(Main.rand.NextFloat(0.05f, 0.7f))));

                if (Main.rand.NextBool(3))
                {
                    Dust ember = Dust.NewDustPerfect(pos, AegisVisuals.ProfanedFireDust,
                        tangent * Main.rand.NextFloat(1.5f, 4.5f), 0, Color.White,
                        Main.rand.NextFloat(0.9f, 1.6f));
                    ember.noGravity = true;
                }
            }

            // 刀尖处最亮的一点：跟着刃口跑的白金火花
            if (stateTimer % 2 == 0)
            {
                Vector2 tipPosition = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeRadius;
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(tipPosition,
                    currentAngle.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * swingDirection) *
                    Main.rand.NextFloat(2.5f, 6f),
                    false, Main.rand.Next(7, 12), Main.rand.NextFloat(0.08f, 0.14f),
                    AegisVisuals.Add(AegisVisuals.Core, 0.9f), new Vector2(2.4f, 0.5f), true, false, 1f));
            }

            // 逆着轮盘飘出的圣灰：Providence 全系"圣火必配深灰烟"的签名
            if (Main.rand.NextBool(3))
            {
                Vector2 smokeDir = (ringPhase + Main.rand.NextFloat(MathHelper.TwoPi)).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Owner.MountedCenter + smokeDir * sprayRadius * Main.rand.NextFloat(0.7f, 1.15f),
                    smokeDir * Main.rand.NextFloat(0.6f, 2.1f),
                    Color.Lerp(AegisVisuals.Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.3f, 0.85f)),
                    Color.Transparent, Main.rand.NextFloat(0.35f, 0.7f), Main.rand.Next(26, 44),
                    Main.rand.NextFloat(-0.05f, 0.05f)));
            }
        }

        /// <summary>甩出去之后剑本体还在自转，沿途持续掉火。</summary>
        private void EmitThrownFlames()
        {
            if (Main.dedServ)
                return;

            AegisVisuals.FlightTrail(Projectile.Center, Projectile.Center - Owner.MountedCenter,
                1.15f, releaseTimer, 3);

            if (releaseTimer % 2 == 0)
            {
                Vector2 tangent = currentAngle.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * swingDirection);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + currentAngle.ToRotationVector2() * 30f * scale * 0.5f,
                    tangent * Main.rand.NextFloat(2f, 5f), false, Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.5f, 0.95f), AegisVisuals.RandomFlameColor()));
            }
        }

        // ────────────────────────────────────────────────────────────────
        // 绘制
        // ────────────────────────────────────────────────────────────────

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin       = new(0f, swordTexture.Height);
            float drawRotation   = currentAngle + MathHelper.PiOver4;
            Vector2 drawPosition = Owner.MountedCenter - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float swipeDir       = Math.Sign(endAngle - startAngle);
            if (swipeDir == 0f) swipeDir = 1f;
            float drawOpacity = MathHelper.Clamp(visualOpacity, 0f, 1f);
            Vector2 bladePosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // ① 脚下的亵渎符文圣印（整套视觉的地基，反向自转）
            DrawRuneFloor(drawPosition, swipeDir, drawOpacity);

            // ② 圣火轮盘：新月拖抹 + 火环 + 内外双环
            DrawFlameWheel(drawPosition, swipeDir, drawOpacity);

            // ③ 沿弧线拖在身后的剑残像（真正的运动模糊，取代 52 片糊成一坨的副本）
            DrawBladeAfterimages(swordTexture, drawPosition, origin, drawOpacity);

            // ④ 剑身火脊 + 刀尖星芒（一张竖向火焰贴图搞定，取代 45 次 bloom 循环）
            DrawBladeSpine(drawPosition, drawOpacity);

            // ⑤ 亵渎背光：本体下方的暗红/焦黑底光
            AegisVisuals.ProfanedBackglow(swordTexture, isThrown ? bladePosition : drawPosition, null,
                drawRotation, origin, new Vector2(scale), drawOpacity, 5f * scale, 6);

            Main.spriteBatch.ExitShaderRegion();

            // ⑥ 刀光 primitive（原版写好了却从没被调用过，这次真正接上）
            DrawBladeTrail();

            // ⑦ 圣火锁链（脱手飞行时把剑和玩家连起来）
            DrawTetherChain(drawOpacity);

            // ⑧ 本体
            Main.EntitySpriteDraw(swordTexture, isThrown ? bladePosition : drawPosition, null,
                lightColor * drawOpacity, drawRotation, origin, scale, SpriteEffects.None);
            return false;
        }

        /// <summary>脚下的符文圣印。挥舞越久越亮，脱手后迅速熄灭。</summary>
        private void DrawRuneFloor(Vector2 drawPosition, float swipeDir, float drawOpacity)
        {
            float sigilOpacity = discFadeIn * drawOpacity * MathHelper.Lerp(0.32f, 0.72f, discRingOpacity);
            if (sigilOpacity <= 0.01f)
                return;

            float spin = Main.GlobalTimeWrappedHourly * -swipeDir * 0.85f;
            // 略微压扁，读起来像"平铺在地面上"而不是竖在身前
            AegisVisuals.DrawRuneSigil(drawPosition, BladeRadius * 1.12f, spin, sigilOpacity,
                new Vector2(1f, 0.78f), 0.9f + discRingOpacity * 0.35f);
        }

        /// <summary>
        /// 圣火轮盘。三片新月拖抹沿刃口铺开构成"盘面"，外圈是反向旋转的火环，
        /// 内圈是收紧的余烬环 —— 亮金在外、余烬在内，盘心留空给玩家。
        /// </summary>
        private void DrawFlameWheel(Vector2 drawPosition, float swipeDir, float drawOpacity)
        {
            float wheelOpacity = MathHelper.Clamp(0.55f + discRingOpacity * 0.4f, 0.55f, 0.95f)
                                 * discFadeIn * DiscBrightness * drawOpacity;
            if (wheelOpacity <= 0.01f)
                return;

            Texture2D twirl = AegisVisuals.Tex(AegisVisuals.TexTwirl);
            Texture2D crescent = AegisVisuals.Tex(AegisVisuals.TexCrescent);
            Texture2D smearFire = AegisVisuals.Tex(AegisVisuals.TexSmearFire2);
            Texture2D ringThick = AegisVisuals.Tex(AegisVisuals.TexRingThick);
            Texture2D corona = AegisVisuals.Tex(AegisVisuals.TexCorona);

            float radius = BladeRadius;
            float spin = Main.GlobalTimeWrappedHourly * swipeDir * 4.4f;

            // 底层：余烬色厚环，负责给整个轮盘一个暗底
            Main.EntitySpriteDraw(ringThick, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.44f * wheelOpacity),
                -spin * 0.3f, ringThick.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(ringThick, radius * 1.04f)), SpriteEffects.None, 0);

            // 日冕分段环：给轮盘边缘"齿"，避免边缘是一条死板的圆
            Main.EntitySpriteDraw(corona, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Flame, 0.28f * wheelOpacity),
                spin * 0.22f, corona.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(corona, radius * 0.98f)), SpriteEffects.None, 0);

            // 新月拖抹片：跟着刃口转，三片均分一圈，构成真正的"盘面"
            for (int i = 0; i < WheelSmearCount; i++)
            {
                float smearAngle = currentAngle + MathHelper.TwoPi * i / WheelSmearCount;
                float layerFade = 1f - i * 0.18f;

                Main.EntitySpriteDraw(twirl, drawPosition, null,
                    AegisVisuals.Add(AegisVisuals.Flame, 0.5f * wheelOpacity * layerFade),
                    smearAngle + MathHelper.PiOver2 * swipeDir, twirl.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(twirl, radius)), SpriteEffects.None, 0);

                Main.EntitySpriteDraw(crescent, drawPosition, null,
                    AegisVisuals.Add(AegisVisuals.Gold, 0.42f * wheelOpacity * layerFade),
                    smearAngle + MathHelper.PiOver2 * swipeDir + 0.18f * swipeDir, crescent.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(crescent, radius * 0.88f)), SpriteEffects.None, 0);
            }

            // Calamity 火焰圆抹：正反两片高速对转，让盘面"在烧"
            Main.EntitySpriteDraw(smearFire, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Gold, 0.4f * wheelOpacity),
                spin, smearFire.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(smearFire, radius * 0.82f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(smearFire, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.22f * wheelOpacity),
                -spin * 1.5f, smearFire.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(smearFire, radius * 0.52f)), SpriteEffects.None, 0);
        }

        /// <summary>
        /// 剑残像：沿旋转方向往回排列，透明度指数衰减。
        /// 这是"一把剑转得很快"，而不是"一圈剑并排站着"。
        /// </summary>
        private void DrawBladeAfterimages(Texture2D swordTexture, Vector2 drawPosition, Vector2 origin, float drawOpacity)
        {
            float ghostStrength = MathHelper.Clamp(outlineBaseOpacity * 1.4f + discRingOpacity * 0.5f, 0f, 1f)
                                  * discFadeIn * drawOpacity;
            if (ghostStrength <= 0.01f || isThrown)
                return;

            for (int i = 0; i < BladeAfterimageCount; i++)
            {
                float back = (i + 1) * BladeAfterimageStep;
                float ghostAngle = currentAngle - back * swingDirection;
                float fade = MathF.Pow(1f - (i + 1f) / (BladeAfterimageCount + 1f), 1.7f);

                Color ghostColor = AegisVisuals.Add(AegisVisuals.Gradient(0.15f + 0.7f * (i / (float)BladeAfterimageCount)),
                    0.30f * fade * ghostStrength);

                Main.EntitySpriteDraw(swordTexture, drawPosition, null, ghostColor,
                    ghostAngle + MathHelper.PiOver4, origin, scale * (1f - i * 0.012f),
                    SpriteEffects.None, 0);
            }

            // 挥舞高峰的一圈爆发描边：只在 outlinePulse 抬头时出现，平时完全不画
            float outlineOpacity = MathHelper.Clamp(MathF.Pow(outlinePulseOpacity, 1.6f), 0f, 1f) * drawOpacity;
            if (outlineOpacity > 0.01f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 outlineOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() *
                                            MathHelper.Lerp(4.5f, 9f, outlinePulseOpacity) * scale;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + outlineOffset, null,
                        AegisVisuals.Add(AegisVisuals.Core, 0.24f * outlineOpacity),
                        currentAngle + MathHelper.PiOver4, origin, scale, SpriteEffects.None, 0);
                }
            }
        }

        /// <summary>
        /// 剑身火脊：一张竖向火焰喷流贴图沿刃口拉伸，外加刃口白芯与刀尖星芒。
        /// 取代旧版沿刀身循环 45 次 BloomCircle 的做法（同样的观感，1/15 的开销，而且真的像火）。
        /// </summary>
        private void DrawBladeSpine(Vector2 drawPosition, float drawOpacity)
        {
            float spineOpacity = MathHelper.Clamp(outlineBaseOpacity + slashOpacity * 0.7f + discRingOpacity * 0.25f, 0f, 1f)
                                 * drawOpacity;
            if (spineOpacity <= 0.01f)
                return;

            Texture2D jet = AegisVisuals.Tex(AegisVisuals.TexJet);
            Texture2D star = AegisVisuals.Tex(AegisVisuals.TexStarPinch);
            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);

            Vector2 forward = currentAngle.ToRotationVector2();
            Vector2 anchor = isThrown
                ? Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY)
                : drawPosition;
            Vector2 spineCenter = anchor + forward * BladeRadius * 0.52f;
            float spineRotation = currentAngle + MathHelper.PiOver2; // muzzle_04 是竖向的

            // 外焰
            Main.EntitySpriteDraw(jet, spineCenter, null,
                AegisVisuals.Add(AegisVisuals.Flame, 0.42f * spineOpacity),
                spineRotation, jet.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(jet, BladeRadius * 0.30f),
                            AegisVisuals.RadiusScale(jet, BladeRadius * 0.62f)),
                SpriteEffects.None, 0);

            // 主焰
            Main.EntitySpriteDraw(jet, spineCenter, null,
                AegisVisuals.Add(AegisVisuals.Gold, 0.5f * spineOpacity),
                spineRotation, jet.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(jet, BladeRadius * 0.17f),
                            AegisVisuals.RadiusScale(jet, BladeRadius * 0.58f)),
                SpriteEffects.None, 0);

            // 刃口白芯
            Main.EntitySpriteDraw(jet, spineCenter, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.36f * spineOpacity),
                spineRotation, jet.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(jet, BladeRadius * 0.075f),
                            AegisVisuals.RadiusScale(jet, BladeRadius * 0.5f)),
                SpriteEffects.None, 0);

            // 刀尖星芒 + 柄部炉心
            Vector2 tipPosition = anchor + forward * BladeRadius;
            Main.EntitySpriteDraw(star, tipPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.45f * spineOpacity),
                currentAngle + Main.GlobalTimeWrappedHourly * 3f, star.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(star, BladeRadius * 0.26f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, anchor, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.5f * spineOpacity),
                0f, bloom.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(bloom, BladeRadius * 0.24f)), SpriteEffects.None, 0);
        }

        private void DrawBladeTrail()
        {
            if (bladeTipHistoryLength < 2 || slashOpacity <= 0.01f || visualOpacity <= 0.01f || Main.dedServ)
                return;

            List<Vector2> trailPoints = new(bladeTipHistoryLength);
            for (int i = 0; i < bladeTipHistoryLength; i++)
                trailPoints.Add(bladeTipHistory[i]);

            Vector2 Anchor(float _, Vector2 __) => Owner.MountedCenter;

            // ── 外焰：余烬色宽带，走 Exoblade 刀光着色器 ──
            Main.spriteBatch.EnterShaderRegion();
            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            slashShader.UseColor(AegisVisuals.Gold);
            slashShader.UseSecondaryColor(AegisVisuals.Ember);
            slashShader.Shader.Parameters["fireColor"].SetValue(AegisVisuals.Flame.ToVector3());
            slashShader.Shader.Parameters["flipped"].SetValue(Owner.direction == 1);
            slashShader.Apply();

            float OuterWidth(float completionRatio, Vector2 _) =>
                scale * 30f * Utils.GetLerpValue(1f, 0f, completionRatio, true) * slashOpacity * visualOpacity;
            Color OuterColor(float completionRatio, Vector2 _) =>
                Color.White * Utils.GetLerpValue(1f, 0.2f, completionRatio, true) * slashOpacity * visualOpacity;

            PrimitiveRenderer.RenderTrail(trailPoints,
                new PrimitiveSettings(OuterWidth, OuterColor, Anchor, shader: slashShader),
                BladeTrailHistoryFrames);
            Main.spriteBatch.ExitShaderRegion();

            // ── 内芯：不走着色器的白金细带，压在外焰中间，刀光才有"刃" ──
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            float CoreWidth(float completionRatio, Vector2 _) =>
                scale * 11f * Utils.GetLerpValue(0.92f, 0f, completionRatio, true) * slashOpacity * visualOpacity;
            Color CoreColor(float completionRatio, Vector2 _) =>
                AegisVisuals.TrailColor(completionRatio, 2, slashOpacity * visualOpacity * 0.9f);

            PrimitiveRenderer.RenderTrail(trailPoints,
                new PrimitiveSettings(CoreWidth, CoreColor, Anchor),
                BladeTrailHistoryFrames);

            Main.spriteBatch.ExitShaderRegion();
        }

        /// <summary>
        /// 圣火锁链：剑脱手飞出后，与玩家之间连着一条烧红的锁链。
        /// 结构 = 深红底链（ThickEndedLine 段） + 圣金亮链 + 每三节一枚符文扣（magic_04，按口径 ×0.5） + 掉落火屑。
        /// </summary>
        private void DrawTetherChain(float drawOpacity)
        {
            if (!isThrown && !releaseEnding)
                return;

            Vector2 start = Owner.MountedCenter;
            Vector2 end = Projectile.Center;
            float distance = Vector2.Distance(start, end);
            if (distance < 12f)
                return;

            Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
            float chainRotation = direction.ToRotation() + MathHelper.PiOver2;
            int segments = Math.Max(2, (int)(distance / 18f));

            Texture2D link = AegisVisuals.Tex(AegisVisuals.TexThickLine);
            Texture2D runeLink = AegisVisuals.Tex(AegisVisuals.TexRuneSpike);
            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            for (int i = 0; i <= segments; i++)
            {
                float factor = i / (float)segments;
                // 锁链略微下垂，不是一根死板的直线
                float sag = MathF.Sin(factor * MathHelper.Pi) * distance * 0.045f;
                Vector2 position = Vector2.Lerp(start, end, factor) + Vector2.UnitY * sag - Main.screenPosition;
                float pulse = 0.72f + 0.28f * MathF.Sin(Main.GlobalTimeWrappedHourly * 11f + i * 0.55f);

                Main.EntitySpriteDraw(link, position, null,
                    AegisVisuals.Add(AegisVisuals.Ember, 0.75f * drawOpacity * pulse),
                    chainRotation, link.Size() * 0.5f, new Vector2(0.5f, 0.42f), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(link, position, null,
                    AegisVisuals.Add(Color.Lerp(AegisVisuals.Gold, AegisVisuals.Core, factor), 0.65f * drawOpacity * pulse),
                    chainRotation, link.Size() * 0.5f, new Vector2(0.26f, 0.34f), SpriteEffects.None, 0);

                if (i % 3 == 0)
                {
                    // magic_04 原图 512²，按项目口径预期缩放后再 ×0.5
                    float runeIntended = AegisVisuals.RadiusScale(runeLink, 11f);
                    Main.EntitySpriteDraw(runeLink, position, null,
                        AegisVisuals.Add(AegisVisuals.Gold, 0.5f * drawOpacity),
                        Main.GlobalTimeWrappedHourly * 3.5f + i, runeLink.Size() * 0.5f,
                        runeIntended * AegisVisuals.RuneCrossShrink, SpriteEffects.None, 0);
                }
            }

            // 两端的炉口
            Main.EntitySpriteDraw(bloom, start - Main.screenPosition, null,
                AegisVisuals.Add(AegisVisuals.Flame, 0.55f * drawOpacity),
                0f, bloom.Size() * 0.5f, new Vector2(AegisVisuals.RadiusScale(bloom, 22f)), SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
