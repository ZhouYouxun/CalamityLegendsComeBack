using System;
using System.Linq;
using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    // ══════════════════════════════════════════════════════════════════════════════
    //  SeedOfSilvaDelphiniumArc
    //
    //  侦查花（Delphinium / Chlo_CDetec 槽）每次激活时生成 3 枚弧形弹幕。
    //  每枚弹幕通过 ai[1] 获取自身编号（0 = 正中，1 = 偏左，2 = 偏右）。
    //
    //  ── 飞行逻辑 ──────────────────────────────────────────────────────────────
    //  Phase 1 (0 → PHASE1_END)      : 扇形展开。无追踪，各弧有独立的正弦漂移。
    //  Phase 2 (PHASE1_END+1 → PHASE2_END) : 自由漂移 → 逐步加速追踪，
    //                                         与 BFLeafProj.UpdateReconHoming 同逻辑。
    //  Phase 3 (PHASE2_END+1 → 消亡) : 全力锁定，预测引导，近身增压。
    //
    //  ── 视觉层（全部 Additive，无任何不透明贴图）────────────────────────────
    //  1. IPixelatedPrimitiveRenderer —— SylvestaffStreak + ImpFlameTrail 着色器轨迹
    //  2. 双股螺旋 (WaterFlavored) 沿 oldPos 缓存延伸
    //  3. 7 瓣前向螺旋花序（与 BFLeafProj.DrawHelix 完全对应）
    //  4. 7 个旋转轨道光晕（与 BFLeafProj.PreDraw 7-spark 环完全对应）
    //  5. 中心旋转星 (BFArrowCommon.DrawCentredRotatingStar，isLeftClick=true)
    //  6. 侦查光学翼 (muzzle_02，每翼 3 层重影，与 BFArrow_CDetec 同技术)
    //  7. 扫描环：外环 + 内反向环 + 雷达牙 + 锁定脉冲扩散
    //  8. 旋转扫描波束（内外环之间的旋转光带）
    //  9. 虚线目标连接线（Phase 3 近身时出现）
    // 10. 轨迹幽灵光点（沿 oldPos 的残像 bloom）
    // ══════════════════════════════════════════════════════════════════════════════
    internal sealed class SeedOfSilvaDelphiniumArc : ModProjectile, IPixelatedPrimitiveRenderer
    {
        // ─── 飞行阶段边界 ──────────────────────────────────────────────────────────
        private const int Phase1_End = 14;
        private const int Phase2_End = 46;

        // ─── 速度上限（像素/帧） ───────────────────────────────────────────────────
        private const float Phase1_MaxSpeed     = 9.5f;
        private const float Phase2_MinSpeed     = 9.5f;
        private const float Phase2_MaxSpeed     = 19.5f;
        private const float Phase3_SpeedFar     = 24f;
        private const float Phase3_SpeedClose   = 29f;

        // ─── 转向惯量（越大 = 越迟钝） ────────────────────────────────────────────
        private const float Phase2_InertiaStart = 42f;
        private const float Phase2_InertiaEnd   = 20f;
        private const float Phase3_InertiaFar   = 13f;
        private const float Phase3_InertiaClose = 4.5f;

        // ─── 近身距离阈值 ──────────────────────────────────────────────────────────
        private const float CloseRange          = 150f;
        private const float ConvergeRange       = 68f;

        // ─── 自由漂移（Phase 2 无目标时） ─────────────────────────────────────────
        private const float Drift_Damping       = 0.9965f;
        private const float Drift_WanderAmp     = 0.022f;
        private const float Drift_WanderFreq    = 0.19f;

        // ─── 视觉常量 ──────────────────────────────────────────────────────────────
        private const int   TrailCacheSize      = 30;
        private const float HelixRadius         = 8.0f;
        private const float ScanRingMaxRadius   = 62f;
        private const int   OuterRingSegments   = 20;
        private const int   InnerRingSegments   = 10;
        private const int   OrbitSparkCount     = 7;
        private const int   RadarTeethCount     = 4;

        // ─── 各弧编号专属数据 ─────────────────────────────────────────────────────
        //     0 = 正中，1 = 左翼，2 = 右翼
        //     Divergence : 初始额外偏角（OnSpawn 里不再旋转，改为此处驱动）
        //     PhaseOff   : 螺旋/扫描环动画的相位偏移，三弧互不重叠
        //     HelixMul   : 螺旋速度系数，使三弧螺旋节奏各异
        //     ScanMul    : 扫描环半径系数
        private static readonly float[] ArcDivergence  = { 0f,    -0.38f,  0.38f };
        private static readonly float[] ArcPhaseOffset = { 0f,     2.094f, 4.189f };
        private static readonly float[] ArcHelixMul    = { 1.00f,  1.15f,  0.87f };
        private static readonly float[] ArcScanMul     = { 1.00f,  0.82f,  0.82f };

        // ─── AI 槽别名 ────────────────────────────────────────────────────────────
        private ref float TargetIndex => ref Projectile.ai[0];
        private ref float ArcIndex    => ref Projectile.ai[1];
        private ref float Timer       => ref Projectile.localAI[0];

        // ─── 仅本地视觉状态（不参与网络同步） ────────────────────────────────────
        private float helixAnimPhase;
        private float scanRingRadius;
        private float scanRingAlpha;
        private float scanBeamAngle;
        private float lockFlashTimer;
        private bool  hadTargetLastFrame;
        private int   tick;
        private float lastEmittedPhase = -1f;

        // ─── 每弧索引计算属性 ─────────────────────────────────────────────────────
        private int   Slot       => (int)MathHelper.Clamp(ArcIndex, 0f, 2f);
        private float PhaseOff   => ArcPhaseOffset[Slot];
        private float HelixMul   => ArcHelixMul[Slot];
        private float ScanMul    => ArcScanMul[Slot];

        // ─── 阶段判断属性 ─────────────────────────────────────────────────────────
        private bool  InPhase1   => Timer <= Phase1_End;
        private bool  InPhase2   => Timer > Phase1_End && Timer <= Phase2_End;
        private bool  InPhase3   => Timer > Phase2_End;

        private float Phase2Progress => InPhase2
            ? (Timer - Phase1_End) / (float)(Phase2_End - Phase1_End)
            : (InPhase3 ? 1f : 0f);

        private float PhaseBrightness => InPhase1 ? 0.42f : (InPhase2 ? 0.78f : 1.0f);

        // ─── 侦查调色板（真正的青绿色，来自 BFArrowCommon Chlo_CDetec） ──────────
        private static readonly BlossomFluxChloroplastPresetType ReconPreset =
            BlossomFluxChloroplastPresetType.Chlo_CDetec;

        private Color MainColor   => BFArrowCommon.GetPresetColor(ReconPreset);
        private Color AccentColor => BFArrowCommon.GetPresetAccentColor(ReconPreset);

        // IPixelatedPrimitiveRenderer
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;

        // ══════════════════════════════════════════════════════════════════════════
        //  ModProjectile 基本设置
        // ══════════════════════════════════════════════════════════════════════════
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailCacheSize;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width                = 16;
            Projectile.height               = 16;
            Projectile.friendly             = true;
            Projectile.DamageType           = DamageClass.Ranged;
            Projectile.penetrate            = 1;
            Projectile.timeLeft             = 180;
            Projectile.tileCollide          = false;
            Projectile.ignoreWater          = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // 在 OnSpawn 里完成弧展开偏角，之后 AI 不再修改初始方向
            float diverge = ArcDivergence[Slot];
            if (Math.Abs(diverge) > 0.001f)
                Projectile.velocity = Projectile.velocity.RotatedBy(diverge);

            helixAnimPhase    = PhaseOff;
            scanRingRadius    = 0f;
            scanRingAlpha     = 0f;
            scanBeamAngle     = PhaseOff;
            lockFlashTimer    = 0f;
            hadTargetLastFrame = false;
            lastEmittedPhase  = -1f;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  主 AI
        // ══════════════════════════════════════════════════════════════════════════
        public override void AI()
        {
            Timer++;
            tick++;
            helixAnimPhase += 0.158f + (Timer / 180f) * 0.048f;

            NPC target = FindTarget();

            CheckPhaseTransition(target);
            UpdateFlight(target);

            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            float lightIntensity = InPhase1 ? 0.14f : (InPhase2 ? 0.28f : 0.44f);
            Lighting.AddLight(Projectile.Center, MainColor.ToVector3() * lightIntensity);

            UpdateScanRingState(target);
            UpdateLockFlash(target);

            // 扫描波束持续旋转
            float beamSpeed = InPhase3 ? 0.072f : (InPhase2 ? 0.055f : 0.038f);
            scanBeamAngle += beamSpeed * HelixMul;

            if (!Main.dedServ)
                EmitTrailParticles(target);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  阶段过渡爆发
        // ══════════════════════════════════════════════════════════════════════════
        private void CheckPhaseTransition(NPC target)
        {
            float currentPhase = InPhase1 ? 1f : (InPhase2 ? 2f : 3f);
            if (Math.Abs(currentPhase - lastEmittedPhase) < 0.5f)
                return;

            bool isFirst = lastEmittedPhase < 0f;
            lastEmittedPhase = currentPhase;

            if (isFirst || Main.dedServ)
                return;

            SpawnPhaseTransitionBurst(currentPhase);
        }

        private void SpawnPhaseTransitionBurst(float phase)
        {
            if (Main.dedServ) return;

            Vector2 fwd   = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float   scale = phase >= 3f ? 1.50f : 0.90f;

            // 核心强光
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center, Vector2.Zero,
                Color.Lerp(MainColor, Color.White, 0.24f) * 0.62f,
                0.38f * scale, 11));

            // 前向方向脉冲环（与 BFArrow_CDetec.SpawnPenetrationImpactFX 同结构）
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, fwd * 0.55f,
                Color.Lerp(MainColor, AccentColor, 0.28f),
                new Vector2(0.68f, 2.00f), fwd.ToRotation(),
                0.14f * scale, 0.028f, 10));

            // 等向次级脉冲环
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero,
                AccentColor with { A = 0 },
                Vector2.One, PhaseOff,
                0.09f * scale, 0.024f, 8));

            // CritSpark 扇形迸射
            int sparkCount = (int)(4 + phase * 3f);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 sVel = fwd.RotatedByRandom(MathHelper.PiOver2 * 0.85f)
                               * Main.rand.NextFloat(1.4f, 4.8f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    sVel, Color.White, AccentColor,
                    Main.rand.NextFloat(0.38f, 0.76f) * scale,
                    Main.rand.Next(9, 15)));
            }

            // 预设尘埃爆发
            BFArrowCommon.EmitPresetBurst(Projectile, ReconPreset,
                (int)(4 * scale), 0.5f, 2.2f, 0.72f, 1.08f);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  飞行逻辑 — 三阶段
        // ══════════════════════════════════════════════════════════════════════════
        private void UpdateFlight(NPC target)
        {
            if (InPhase1)      UpdatePhase1();
            else if (InPhase2) UpdatePhase2(target);
            else               UpdatePhase3(target);
        }

        // ─── Phase 1：扇形展开，正弦漂移，无追踪 ─────────────────────────────────
        private void UpdatePhase1()
        {
            float t = Timer / (float)Phase1_End;

            // 轻微阻尼让速度稳定
            float damping = MathHelper.Lerp(0.9975f, 0.9994f, t);
            Projectile.velocity *= damping;

            // 每弧的正弦漂移角度不同，视觉上三弧不重叠
            float wanderAmp  = MathHelper.Lerp(0.018f, 0.004f, t);
            float wanderFreq = 0.22f + Slot * 0.065f;
            float wander     = (float)Math.Sin((Timer + Slot * 14.3f + Projectile.identity * 3.1f)
                                               * wanderFreq) * wanderAmp;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander);

            ClampSpeed(Phase1_MaxSpeed);
        }

        // ─── Phase 2：漂移 → 追踪，与 BFLeafProj.ApplyReconFreeDrift+AcceleratingHoming ─
        private void UpdatePhase2(NPC target)
        {
            float p       = Phase2Progress;
            float inertia = MathHelper.Lerp(Phase2_InertiaStart, Phase2_InertiaEnd, p);
            float speed   = MathHelper.Lerp(Phase2_MinSpeed, Phase2_MaxSpeed, p);

            if (target == null)
            {
                // 无目标：自由漂移（与 BFLeafProj.ApplyReconFreeDrift 同逻辑）
                float curSpeed  = Math.Max(Projectile.velocity.Length(), speed * 0.68f);
                float wanderOff = (float)Math.Sin((Timer + Slot * 7.3f + Projectile.identity * 5f)
                                                  * Drift_WanderFreq) * Drift_WanderAmp;
                Projectile.velocity = Projectile.velocity
                    .SafeNormalize(Vector2.UnitX)
                    .RotatedBy(wanderOff)
                    * (curSpeed * Drift_Damping);
                return;
            }

            // 预测引导目标
            Vector2 aim     = PredictAimPoint(target, speed, MathHelper.Lerp(14f, 6f, p));
            Vector2 desired = (aim - Projectile.Center).SafeNormalize(
                                  Projectile.velocity.SafeNormalize(Vector2.UnitY)) * speed;
            Projectile.velocity = (Projectile.velocity * (inertia - 1f) + desired) / inertia;
            ClampSpeed(Phase2_MaxSpeed);
        }

        // ─── Phase 3：全力锁定 + 近身增压 ─────────────────────────────────────────
        private void UpdatePhase3(NPC target)
        {
            if (target == null)
            {
                // 无目标时以大弧度扫荡，保持高速
                float sweep = (float)Math.Cos((Timer + Slot * 5.9f) * 0.122f) * 0.026f;
                Projectile.velocity = Projectile.velocity.RotatedBy(sweep);
                LerpSpeedTo(Phase3_SpeedFar, 0.09f);
                return;
            }

            float dist     = Vector2.Distance(Projectile.Center, target.Center);
            float closeFrac = Utils.GetLerpValue(CloseRange, ConvergeRange * 0.5f, dist, true);
            float inertia  = MathHelper.Lerp(Phase3_InertiaFar, Phase3_InertiaClose, closeFrac);
            float speed    = MathHelper.Lerp(Phase3_SpeedFar, Phase3_SpeedClose, closeFrac);

            // 极近时缩短预测帧数，防止过冲轨道
            float predFrames = MathHelper.Lerp(8f, 1.5f, closeFrac);
            Vector2 aim      = PredictAimPoint(target, speed, predFrames);
            Vector2 desired  = (aim - Projectile.Center).SafeNormalize(
                                    Projectile.velocity.SafeNormalize(Vector2.UnitY)) * speed;
            Projectile.velocity = (Projectile.velocity * (inertia - 1f) + desired) / inertia;
            ClampSpeed(speed);
        }

        // ─── 辅助：预测命中点 ────────────────────────────────────────────────────
        private Vector2 PredictAimPoint(NPC target, float speed, float overridePred = -1f)
        {
            float dist  = Vector2.Distance(Projectile.Center, target.Center);
            float pred  = overridePred > 0f
                ? overridePred
                : MathHelper.Clamp(dist / Math.Max(speed, 1f) * 0.52f, 2f, 18f);
            return target.Center + target.velocity * pred;
        }

        private void ClampSpeed(float max)
        {
            if (Projectile.velocity.LengthSquared() > max * max)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * max;
        }

        private void LerpSpeedTo(float targetSpeed, float rate)
        {
            float cur = Projectile.velocity.Length();
            if (cur < 0.01f) return;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY)
                                  * MathHelper.Lerp(cur, targetSpeed, rate);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  目标检索
        // ══════════════════════════════════════════════════════════════════════════
        private NPC FindTarget()
        {
            if (!BFArrowCommon.InBounds(TargetIndex, Main.maxNPCs))
                return null;
            NPC npc = Main.npc[(int)TargetIndex];
            return (npc.active && npc.CanBeChasedBy()) ? npc : null;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  视觉状态更新
        // ══════════════════════════════════════════════════════════════════════════
        private void UpdateScanRingState(NPC target)
        {
            float maxR = ScanRingMaxRadius * ScanMul;
            float targetR = InPhase1 ? 0f
                          : (target != null ? maxR : maxR * 0.42f);
            float targetA = InPhase1 ? 0f
                          : (target != null ? 0.62f : 0.24f);

            scanRingRadius = MathHelper.Lerp(scanRingRadius, targetR, 0.11f);
            scanRingAlpha  = MathHelper.Lerp(scanRingAlpha,  targetA, 0.09f);
        }

        private void UpdateLockFlash(NPC target)
        {
            bool hasTarget = target != null;
            if (hasTarget && !hadTargetLastFrame)
                lockFlashTimer = 38f;

            hadTargetLastFrame = hasTarget;
            if (lockFlashTimer > 0f)
                lockFlashTimer = Math.Max(0f, lockFlashTimer - 1f);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  粒子发射
        // ══════════════════════════════════════════════════════════════════════════
        private void EmitTrailParticles(NPC target)
        {
            Vector2 fwd  = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = fwd.RotatedBy(MathHelper.PiOver2);

            // ── SquishyLightParticle 核心轨迹（与 BFLeafProj Chlo_CDetec 同调用） ─
            int sqRate = InPhase1 ? 6 : (InPhase2 ? 4 : 2);
            if (tick % sqRate == 0)
            {
                float scBase = InPhase3 ? 0.16f : 0.10f;
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center - fwd * Main.rand.NextFloat(3f, 10f)
                               + side * Main.rand.NextFloat(-4.5f, 4.5f),
                    -Projectile.velocity * 0.022f + Main.rand.NextVector2Circular(0.22f, 0.22f),
                    Main.rand.NextFloat(scBase, scBase + 0.06f),
                    Color.Lerp(MainColor, Color.White, 0.32f),
                    Main.rand.Next(7, 13)));
            }

            // ── CritSpark 侧向迸射（Chlo_CDetec 特有，与 BFLeafProj 同条件） ───────
            if (InPhase2 && tick % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + side * Main.rand.NextFloat(-5f, 5f),
                    fwd * Main.rand.NextFloat(1.4f, 3.0f),
                    Color.White, AccentColor,
                    Main.rand.NextFloat(0.34f, 0.58f),
                    Main.rand.Next(8, 13)));
            }

            if (InPhase3 && tick % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + side * Main.rand.NextFloat(-6f, 6f),
                    fwd * Main.rand.NextFloat(2.4f, 5.2f)
                        + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    Color.White, AccentColor,
                    Main.rand.NextFloat(0.42f, 0.78f),
                    Main.rand.Next(9, 14)));
            }

            // ── 预设尘埃轨迹（BFArrowCommon.EmitPresetTrail，与 BFLeafProj 同调用） ─
            if (tick % 2 == 0)
                BFArrowCommon.EmitPresetTrail(Projectile, ReconPreset,
                    InPhase3 ? 1.15f : (InPhase2 ? 0.90f : 0.60f));

            // ── 锁定获得一次性爆发 ─────────────────────────────────────────────────
            if ((int)lockFlashTimer == 35)
                SpawnLockAcquireBurst();
        }

        private void SpawnLockAcquireBurst()
        {
            if (Main.dedServ) return;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center, Vector2.Zero,
                Color.Lerp(MainColor, Color.White, 0.28f) * 0.66f, 0.46f, 12));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero,
                AccentColor with { A = 0 }, Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.16f, 0.030f, 10));

            // 周向 CritSpark 扩散
            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + dir * Main.rand.NextFloat(4f, 9f),
                    dir * Main.rand.NextFloat(2.5f, 6.5f),
                    Color.White, AccentColor,
                    Main.rand.NextFloat(0.46f, 0.84f),
                    Main.rand.Next(10, 17)));
            }

            // GlowOrb 环绕爆散
            for (int i = 0; i < 5; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.0f),
                    false, Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.14f, 0.26f),
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat()),
                    true, false, true));
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  命中 / 消亡事件
        // ══════════════════════════════════════════════════════════════════════════
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyDelphiniumStun(Projectile.owner, 58);

            if (BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
            {
                Player owner = Main.player[Projectile.owner];
                owner.wingTime = Math.Min(owner.wingTimeMax, owner.wingTime + 28f);
            }

            SpawnImpactFX(target.Center);
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.22f, Pitch = 0.50f }, target.Center);
        }

        private void SpawnImpactFX(Vector2 pos)
        {
            if (Main.dedServ) return;

            Vector2 fwd  = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = fwd.RotatedBy(MathHelper.PiOver2);

            // 核心闪光
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                pos, Vector2.Zero,
                Color.Lerp(MainColor, Color.White, 0.26f) * 0.82f, 0.64f, 13));

            // 前向方向脉冲环（与 BFArrow_CDetec.SpawnPenetrationImpactFX 完全对齐）
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                pos, fwd * 0.88f,
                Color.Lerp(MainColor, AccentColor, 0.35f),
                new Vector2(0.74f, 2.08f), fwd.ToRotation(),
                0.19f, 0.042f, 12));

            // 等向次级环
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                pos, Vector2.Zero,
                Color.Lerp(AccentColor, Color.White, 0.42f) with { A = 0 },
                new Vector2(1.24f, 1.24f), 0f,
                0.11f, 0.026f, 9));

            // 上下交替 CritSpark 爆射（与 BFArrow_CDetec.SpawnPenetrationImpactFX 同结构）
            for (int i = 0; i < 14; i++)
            {
                float ySign   = (i % 2 == 0) ? -1f : 1f;
                float bSpeed  = ySign < 0f ? Main.rand.NextFloat(5f, 9.5f) : Main.rand.NextFloat(3f, 7f);
                Vector2 vel   = new Vector2(0f, ySign * bSpeed).RotatedByRandom(MathHelper.Pi / 7f)
                                * Main.rand.NextFloat(0.15f, 1.9f);
                Color primary = Main.rand.NextBool()
                    ? new Color(82, 255, 228) : Color.White;
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    pos + Main.rand.NextVector2Circular(9f, 9f), vel,
                    primary, AccentColor,
                    Main.rand.NextFloat(0.52f, 0.90f),
                    Main.rand.Next(10, 17)));
            }

            // 侧向 GlowOrb 点缀
            for (int i = 0; i < 4; i++)
            {
                Vector2 oVel = side * (i % 2 == 0 ? 1f : -1f)
                               * Main.rand.NextFloat(0.8f, 2.4f)
                               + fwd * Main.rand.NextFloat(-0.5f, 1.5f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    pos + Main.rand.NextVector2Circular(7f, 7f),
                    oVel, false, Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.16f, 0.28f),
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat()),
                    true, false, true));
            }

            BFArrowCommon.EmitPresetBurst(Projectile, ReconPreset,
                9, 0.9f, 3.6f, 0.76f, 1.14f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            SpawnKillFX();
        }

        private void SpawnKillFX()
        {
            if (Main.dedServ) return;

            // 小型消散环
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero,
                Color.Lerp(MainColor, Color.White, 0.32f) with { A = 0 },
                Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi),
                0.10f, 0.024f, 8));

            // GlowOrb 分裂
            for (int i = 0; i < 7; i++)
            {
                Vector2 dir = (MathHelper.TwoPi * i / 7f + PhaseOff).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + dir * Main.rand.NextFloat(4f, 8f),
                    dir * Main.rand.NextFloat(0.9f, 2.8f),
                    false, Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.14f, 0.26f),
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat()),
                    true, false, true));
            }

            // 尘埃
            int dustType = BFArrowCommon.GetPresetDustType(ReconPreset);
            for (int i = 0; i < 7; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, dustType,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 4.0f),
                    100,
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.72f, 1.05f));
                d.noGravity = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PreDraw — 按层次编排所有视觉绘制
        // ══════════════════════════════════════════════════════════════════════════
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            NPC     target  = FindTarget();
            Vector2 center  = Projectile.Center - Main.screenPosition;
            Vector2 fwd     = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float   opacity = Projectile.Opacity;

            // 由远到近渲染，防止遮挡关系错乱
            DrawScanRing(center, fwd, target, opacity);
            DrawScanBeam(center, fwd, opacity);
            DrawTargetLockRay(center, fwd, target, opacity);
            DrawHelixTrail(center, fwd, opacity);
            DrawGhostAfterimages(opacity);
            DrawScoutWings(center, fwd, opacity);
            DrawCoreGlow(center, fwd, opacity);

            return false;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ── 绘制层 1：扫描环 ────────────────────────────────────────────────────
        //     外环 + 内反向环 + 雷达牙 + 锁定脉冲
        // ══════════════════════════════════════════════════════════════════════════
        private void DrawScanRing(Vector2 center, Vector2 fwd, NPC target, float opacity)
        {
            if (scanRingRadius < 1.5f || scanRingAlpha < 0.012f) return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float time  = Main.GlobalTimeWrappedHourly;
            float pulse = 0.87f + 0.13f * (float)Math.Sin(time * 3.8f + PhaseOff);

            BeginAdditive();

            // ── 外环（顺时针旋转，OuterRingSegments 个 bloom 节点）────────────────
            float outerSegScale = scanRingRadius * MathHelper.TwoPi
                                  / (OuterRingSegments * bloom.Width) * 2.30f;
            Color outerColor = MainColor with { A = 0 };

            for (int i = 0; i < OuterRingSegments; i++)
            {
                float ang = MathHelper.TwoPi * i / OuterRingSegments
                            + time * 1.10f * HelixMul;
                Vector2 p = center + ang.ToRotationVector2() * scanRingRadius;
                Main.EntitySpriteDraw(bloom, p, null,
                    outerColor * (scanRingAlpha * pulse * opacity),
                    0f, bloom.Size() * 0.5f, outerSegScale, SpriteEffects.None, 0);
            }

            // ── 内环（逆时针，半径 48%，InnerRingSegments 个节点）────────────────
            float innerRadius   = scanRingRadius * 0.48f;
            float innerSegScale = innerRadius * MathHelper.TwoPi
                                  / (InnerRingSegments * bloom.Width) * 2.60f;
            Color innerColor = AccentColor with { A = 0 };

            for (int i = 0; i < InnerRingSegments; i++)
            {
                float ang = MathHelper.TwoPi * i / InnerRingSegments
                            - time * 1.62f * HelixMul + PhaseOff;
                Vector2 p = center + ang.ToRotationVector2() * innerRadius;
                Main.EntitySpriteDraw(bloom, p, null,
                    innerColor * (scanRingAlpha * 0.55f * pulse * opacity),
                    0f, bloom.Size() * 0.5f, innerSegScale, SpriteEffects.None, 0);
            }

            // ── 雷达牙：内环缘 → 外环缘的虚线辐条 ───────────────────────────────
            if (!InPhase1)
            {
                float teethTime  = time * 1.45f * HelixMul + PhaseOff;
                float innerEdge  = innerRadius * 1.10f;
                float outerEdge  = scanRingRadius * 0.92f;

                for (int t = 0; t < RadarTeethCount; t++)
                {
                    float toothAngle = teethTime + MathHelper.TwoPi * t / RadarTeethCount;
                    Vector2 toothDir = toothAngle.ToRotationVector2();
                    Vector2 p0 = center + toothDir * innerEdge;
                    Vector2 p1 = center + toothDir * outerEdge;
                    float   segLen = Vector2.Distance(p0, p1);
                    int     dots   = Math.Max(2, (int)(segLen / 5.8f));

                    for (int s = 0; s < dots; s++)
                    {
                        // 每隔 3 个点跳过 1 个，产生虚线效果
                        if (s % 3 == 0) continue;
                        float   tp    = s / (float)(dots - 1);
                        Vector2 dpos  = Vector2.Lerp(p0, p1, tp);
                        float   dA    = (1f - tp) * scanRingAlpha * 0.70f * opacity;
                        Main.EntitySpriteDraw(bloom, dpos, null,
                            innerColor * dA,
                            0f, bloom.Size() * 0.5f,
                            outerSegScale * 0.55f, SpriteEffects.None, 0);
                    }
                }
            }

            // ── 锁定获得时向外扩散的脉冲环 ───────────────────────────────────────
            if (lockFlashTimer > 0f)
            {
                float   lockProg      = 1f - lockFlashTimer / 38f;
                float   pulseRadius   = scanRingRadius * (1f + lockProg * 0.55f);
                float   pulseAlpha    = (lockFlashTimer / 38f) * opacity;
                float   pulseSegScale = pulseRadius * MathHelper.TwoPi
                                        / (OuterRingSegments * bloom.Width) * 2.30f;
                Color   lockColor     = AccentColor with { A = 0 };

                for (int i = 0; i < OuterRingSegments; i++)
                {
                    float   ang = MathHelper.TwoPi * i / OuterRingSegments;
                    Vector2 p   = center + ang.ToRotationVector2() * pulseRadius;
                    Main.EntitySpriteDraw(bloom, p, null,
                        lockColor * (pulseAlpha * 0.94f),
                        0f, bloom.Size() * 0.5f, pulseSegScale, SpriteEffects.None, 0);
                }
            }

            EndAdditive();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ── 绘制层 2：旋转扫描波束 ──────────────────────────────────────────────
        //     内外环之间旋转的光带，是侦查模式"扫场"感的核心视觉
        // ══════════════════════════════════════════════════════════════════════════
        private void DrawScanBeam(Vector2 center, Vector2 fwd, float opacity)
        {
            if (scanRingRadius < 4f || scanRingAlpha < 0.04f) return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float innerR = scanRingRadius * 0.48f * 1.05f;
            float outerR = scanRingRadius * 0.96f;
            float beamA  = scanRingAlpha * 0.38f * opacity;

            // 波束不可见时不绘制（Phase 1 或环太小）
            if (beamA < 0.005f) return;

            BeginAdditive();

            // 主波束：沿 scanBeamAngle 方向从内环到外环打出一条光线
            Vector2 beamDir = scanBeamAngle.ToRotationVector2();
            float   beamLen = outerR - innerR;
            int     steps   = Math.Max(4, (int)(beamLen / 3.5f));

            for (int s = 0; s < steps; s++)
            {
                float   sp    = s / (float)(steps - 1);
                float   r     = innerR + beamLen * sp;
                Vector2 bpos  = center + beamDir * r;
                float   bA    = beamA * (1f - sp * 0.65f);
                float   bSc   = MathHelper.Lerp(0.042f, 0.018f, sp);
                Color   bCol  = Color.Lerp(AccentColor, MainColor, sp) with { A = 0 };
                Main.EntitySpriteDraw(bloom, bpos, null,
                    bCol * bA, 0f, bloom.Size() * 0.5f, bSc, SpriteEffects.None, 0);
            }

            // 次级对称波束（与主波束反向，减少"探照灯"感，强调旋转扫场）
            Vector2 beamDir2 = (scanBeamAngle + MathHelper.Pi + 0.42f).ToRotationVector2();
            for (int s = 0; s < steps; s++)
            {
                float   sp    = s / (float)(steps - 1);
                float   r     = innerR + beamLen * sp;
                Vector2 bpos  = center + beamDir2 * r;
                float   bA    = beamA * 0.45f * (1f - sp * 0.75f);
                float   bSc   = MathHelper.Lerp(0.030f, 0.013f, sp);
                Color   bCol  = Color.Lerp(MainColor, AccentColor, sp * 0.5f) with { A = 0 };
                Main.EntitySpriteDraw(bloom, bpos, null,
                    bCol * bA, 0f, bloom.Size() * 0.5f, bSc, SpriteEffects.None, 0);
            }

            EndAdditive();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ── 绘制层 3：虚线目标连接线 ────────────────────────────────────────────
        //     Phase 3 且距离 < 400 时显示，提示锁定追踪中
        // ══════════════════════════════════════════════════════════════════════════
        private void DrawTargetLockRay(Vector2 center, Vector2 fwd, NPC target, float opacity)
        {
            if (!InPhase3 || target == null) return;

            float dist = Vector2.Distance(Projectile.Center, target.Center);
            if (dist > 400f) return;

            Texture2D bloom    = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float     time     = Main.GlobalTimeWrappedHourly;
            float     rayAlpha = Utils.GetLerpValue(400f, 160f, dist, true) * opacity * 0.38f;
            float     pulse    = 0.68f + 0.32f * (float)Math.Sin(time * 6.8f + PhaseOff);

            Vector2 targetScreen = target.Center - Main.screenPosition;
            Vector2 rayDir       = (targetScreen - center).SafeNormalize(Vector2.UnitY);
            float   rayLen       = Vector2.Distance(center, targetScreen);
            int     dots         = Math.Max(3, (int)(rayLen / 13f));

            BeginAdditive();

            for (int i = 1; i < dots; i++)
            {
                // 跳过每隔 3 个，产生虚线视觉
                if (i % 3 == 0) continue;

                float   dp    = i / (float)dots;
                Vector2 dpos  = center + rayDir * (rayLen * dp);
                float   dA    = rayAlpha * (1f - dp * 0.72f) * pulse;
                Color   dCol  = Color.Lerp(MainColor, AccentColor, dp) with { A = 0 };
                float   dSc   = MathHelper.Lerp(0.030f, 0.011f, dp);
                Main.EntitySpriteDraw(bloom, dpos, null,
                    dCol * dA, 0f, bloom.Size() * 0.5f, dSc, SpriteEffects.None, 0);
            }

            // 目标端小圆弧标记
            Vector2 tipPos  = center + rayDir * (rayLen * 0.96f);
            float   tipAlpha = rayAlpha * pulse;
            for (int i = 0; i < 6; i++)
            {
                float tAng = (MathHelper.TwoPi * i / 6f) + time * 1.8f + PhaseOff;
                Vector2 tOff = tAng.ToRotationVector2() * 5.5f;
                Main.EntitySpriteDraw(bloom, tipPos + tOff, null,
                    (AccentColor with { A = 0 }) * (tipAlpha * 0.60f),
                    0f, bloom.Size() * 0.5f, 0.022f, SpriteEffects.None, 0);
            }

            EndAdditive();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ── 绘制层 4：螺旋轨迹 ─────────────────────────────────────────────────
        //     沿 oldPos 缓存的双股螺旋 + 头部 7 瓣前向螺旋花序
        //     与 BFLeafProj.DrawHelix 完全对齐（7 瓣，WaterFlavored 贴图）
        // ══════════════════════════════════════════════════════════════════════════
        private void DrawHelixTrail(Vector2 center, Vector2 fwd, float opacity)
        {
            Texture2D helixTex = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Texture2D sparkTex = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 side  = fwd.RotatedBy(MathHelper.PiOver2);
            float   time  = Main.GlobalTimeWrappedHourly * (2.65f * HelixMul) + PhaseOff;
            float   phBrt = PhaseBrightness;

            BeginAdditive();

            // ── 沿 oldPos 的双股螺旋 ──────────────────────────────────────────────
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;

                float   comp = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 tp   = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                // 每个位置上的核心 bloom 光点（大小随远近变化）
                Main.EntitySpriteDraw(bloomTex, tp, null,
                    (MainColor with { A = 0 }) * (0.13f * comp * phBrt * opacity),
                    0f, bloomTex.Size() * 0.5f,
                    0.016f + comp * 0.014f, SpriteEffects.None, 0);

                // 股 A：主色
                float hAngA  = time - i * 0.20f;
                Vector2 offA = side.RotatedBy(hAngA) * (HelixRadius * comp);
                float sA     = 0.44f * comp * phBrt * opacity;
                Main.EntitySpriteDraw(sparkTex, tp + offA, null,
                    (MainColor with { A = 0 }) * sA,
                    fwd.ToRotation() + hAngA + MathHelper.PiOver2,
                    sparkTex.Size() * 0.5f,
                    new Vector2(0.022f, 0.140f * comp), SpriteEffects.None, 0);

                // 股 B：强调色，与 A 相位相差 π
                Vector2 offB = side.RotatedBy(hAngA + MathHelper.Pi) * (HelixRadius * comp);
                Main.EntitySpriteDraw(sparkTex, tp + offB, null,
                    (AccentColor with { A = 0 }) * (sA * 0.56f),
                    fwd.ToRotation() + hAngA + MathHelper.Pi + MathHelper.PiOver2,
                    sparkTex.Size() * 0.5f,
                    new Vector2(0.016f, 0.100f * comp), SpriteEffects.None, 0);

                // Phase 3 额外：第三股（斜 90°，减弱），三股螺旋
                if (InPhase3 && i % 2 == 0)
                {
                    Vector2 offC = side.RotatedBy(hAngA + MathHelper.PiOver2)
                                   * (HelixRadius * 0.58f * comp);
                    Main.EntitySpriteDraw(sparkTex, tp + offC, null,
                        (Color.Lerp(MainColor, AccentColor, 0.55f) with { A = 0 })
                            * (sA * 0.32f),
                        fwd.ToRotation() + hAngA + MathHelper.PiOver2 + MathHelper.PiOver2,
                        sparkTex.Size() * 0.5f,
                        new Vector2(0.014f, 0.078f * comp), SpriteEffects.None, 0);
                }
            }

            // ── 头部 7 瓣前向螺旋（与 BFLeafProj.DrawHelix 精确对应，helixCount=7）─
            const int helixCount = 7;
            for (int i = 0; i < helixCount; i++)
            {
                float progress  = i / (float)(helixCount - 1);
                float rotDir    = (i % 2 == 0) ? 1f : -1f;
                float helixAngle = time * rotDir
                                   - progress * 2.1f
                                   + MathHelper.TwoPi * i / helixCount;
                Vector2 spiralOff = side.RotatedBy(helixAngle)
                                    * MathHelper.Lerp(HelixRadius * 1.40f, 3.5f, progress);
                Vector2 backOff  = -fwd * MathHelper.Lerp(2f, 28f, progress);
                Color   pCol     = Color.Lerp(MainColor, AccentColor, progress * 0.48f) with { A = 0 };
                float   pA       = MathHelper.Lerp(0.58f, 0.10f, progress) * phBrt * opacity;
                Vector2 pScale   = new Vector2(0.20f, MathHelper.Lerp(0.78f, 0.34f, progress))
                                   * Projectile.scale;

                Main.EntitySpriteDraw(helixTex,
                    center + spiralOff + backOff, null,
                    pCol * pA,
                    fwd.ToRotation() - MathHelper.PiOver2,
                    helixTex.Size() * 0.5f, pScale, SpriteEffects.None, 0);
            }

            EndAdditive();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ── 绘制层 5：轨迹幽灵光点 ──────────────────────────────────────────────
        //     在旧位置上绘制渐隐的 bloom 残像
        // ══════════════════════════════════════════════════════════════════════════
        private void DrawGhostAfterimages(float opacity)
        {
            if (InPhase1) return;

            Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float phMult = InPhase2 ? 0.52f : 0.88f;

            BeginAdditive();

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float comp = 1f - i / (float)Projectile.oldPos.Length;
                if (comp < 0.05f) continue;

                Vector2 pos  = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float   a    = 0.28f * comp * phMult * opacity;
                float   sc   = MathHelper.Lerp(0.052f, 0.012f, 1f - comp);

                // 外圈（主色）
                Main.EntitySpriteDraw(bloomTex, pos, null,
                    (MainColor with { A = 0 }) * a,
                    0f, bloomTex.Size() * 0.5f, sc, SpriteEffects.None, 0);

                // 内核（强调色，亮度更高）
                if (i % 2 == 0)
                {
                    Main.EntitySpriteDraw(bloomTex, pos, null,
                        (AccentColor with { A = 0 }) * (a * 0.42f),
                        0f, bloomTex.Size() * 0.5f, sc * 0.45f, SpriteEffects.None, 0);
                }
            }

            EndAdditive();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ── 绘制层 6：侦查光学翼 ────────────────────────────────────────────────
        //     muzzle_02 贴图，每翼 3 层重影，与 BFArrow_CDetec.DrawReconOpticWing
        //     完全相同的算法，只缩小至弧弹幕的体量。
        // ══════════════════════════════════════════════════════════════════════════
        private void DrawScoutWings(Vector2 center, Vector2 fwd, float opacity)
        {
            float fadeIn  = Utils.GetLerpValue(0f, Phase1_End, Timer, true);
            float fadeOut = Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);
            float fade    = fadeIn * fadeOut * opacity;
            if (fade < 0.008f) return;

            float phMult = InPhase3 ? 1.30f : (InPhase2 ? 0.90f : 0.40f);

            Texture2D opticTex = ModContent.Request<Texture2D>(
                "CalamityLegendsComeBack/Texture/KsTexture/muzzle_02").Value;
            Vector2 side = fwd.RotatedBy(MathHelper.PiOver2);

            BeginAdditive();
            DrawOneScoutWing(opticTex, center, fwd,  side, +1f, fade * phMult);
            DrawOneScoutWing(opticTex, center, fwd, -side, -1f, fade * phMult);
            EndAdditive();
        }

        private void DrawOneScoutWing(Texture2D tex, Vector2 center, Vector2 fwd,
                                       Vector2 wingDir, float sideSign, float fade)
        {
            // 与 BFArrow_CDetec.DrawReconOpticWing 完全相同的 origin（0.84H）和结构
            Vector2 origin  = new Vector2(tex.Width * 0.5f, tex.Height * 0.84f);
            float   baseRot = wingDir.ToRotation() + MathHelper.PiOver2;

            float scanPulse  = 0.5f + 0.5f * (float)Math.Sin(Timer * 0.36f + sideSign * 0.82f
                                                               + Projectile.identity * 0.13f);
            float wingOpen   = InPhase3 ? 0.40f : (InPhase2 ? 0.23f : 0.10f);
            float flutter    = scanPulse * wingOpen
                               * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.60f
                                                 + sideSign * 0.84f + Slot * 1.08f);

            Vector2 basePos  = center + wingDir * 3.2f - fwd * 1.2f;

            // 3 层重影，与 BFArrow_CDetec 完全对应
            for (int g = 0; g < 3; g++)
            {
                float   gOff  = g - 1f;
                float   gA    = (g == 1 ? 0.44f : 0.15f) * fade;
                Color   gCol  = Color.Lerp(
                                    MainColor,
                                    Color.Lerp(AccentColor, Color.White, 0.26f),
                                    0.30f + 0.18f * g)
                                with { A = 0 } * gA;
                Vector2 gSc   = new Vector2(0.058f, 0.088f + g * 0.006f);

                Main.EntitySpriteDraw(tex,
                    basePos + wingDir * gOff * 1.50f, null,
                    gCol,
                    baseRot + flutter + gOff * 0.18f,
                    origin, gSc, SpriteEffects.None, 0);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ── 绘制层 7：核心发光 + 轨道光晕 + 旋转星 ─────────────────────────────
        //     与 BFLeafProj.PreDraw Chlo_CDetec 分支完全对应：
        //     7 个轨道光晕 → 中心 bloom → DrawCentredRotatingStar
        // ══════════════════════════════════════════════════════════════════════════
        private void DrawCoreGlow(Vector2 center, Vector2 fwd, float opacity)
        {
            Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkTex = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            float time   = Main.GlobalTimeWrappedHourly;
            float pulse  = 0.90f + 0.10f * (float)Math.Sin(
                time * 7.4f + Projectile.identity * 0.30f + PhaseOff);
            float phBrt  = PhaseBrightness * 1.18f;

            BeginAdditive();

            // 外晕（大 bloom）
            Main.EntitySpriteDraw(bloomTex, center, null,
                (MainColor with { A = 0 }) * (0.50f * pulse * phBrt * opacity),
                0f, bloomTex.Size() * 0.5f, 0.30f * pulse, SpriteEffects.None, 0);

            // 内核（强调色偏白）
            Main.EntitySpriteDraw(bloomTex, center, null,
                (Color.Lerp(AccentColor, Color.White, 0.46f) with { A = 0 })
                    * (0.68f * phBrt * opacity),
                0f, bloomTex.Size() * 0.5f, 0.13f * pulse, SpriteEffects.None, 0);

            // 7 个旋转轨道光晕（与 BFLeafProj.PreDraw 7-spark 环精确对应）
            Vector2 side    = fwd.RotatedBy(MathHelper.PiOver2);
            float   orbitT  = time * (3.5f * HelixMul) + PhaseOff;

            for (int o = 0; o < OrbitSparkCount; o++)
            {
                float   ang      = orbitT + MathHelper.TwoPi * o / OrbitSparkCount;
                float   sSide    = (float)Math.Sin(ang + time * 2.7f);
                float   sForward = (float)Math.Cos(ang);
                Vector2 orbitOff = side * sSide * (3.4f + 1.3f * pulse)
                                   + fwd * sForward * (2.7f + 1.0f * pulse);
                Color   oCol     = Color.Lerp(MainColor, AccentColor, 0.36f) with { A = 0 };

                Main.EntitySpriteDraw(sparkTex, center + orbitOff, null,
                    oCol * (0.26f * phBrt * opacity),
                    Projectile.rotation + MathHelper.PiOver2,
                    sparkTex.Size() * 0.5f,
                    new Vector2(0.030f, (0.098f + 0.032f * pulse) * 0.5f) * Projectile.scale,
                    SpriteEffects.None, 0);
            }

            // 旋转星（BFArrowCommon.DrawCentredRotatingStar，isLeftClick=true 使用小尺寸高速变体）
            BFArrowCommon.DrawCentredRotatingStar(
                Projectile, ReconPreset, isLeftClick: true, manageBlendState: false);

            EndAdditive();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  IPixelatedPrimitiveRenderer — 着色器驱动的本体轨迹
        //  与 BFLeafProj.RenderPixelatedPrimitives 完全相同的着色器组合和参数
        // ══════════════════════════════════════════════════════════════════════════
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] points = BuildTrailPoints();
            if (points.Length < 2) return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>(
                    "CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                points,
                new PrimitiveSettings(
                    TrailWidthFunc, TrailColorFunc,
                    (_, _) => Vector2.Zero,
                    true, true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                points.Length * 2);
        }

        private Vector2[] BuildTrailPoints()
        {
            var list = Projectile.oldPos
                .Where(p => p != Vector2.Zero)
                .Select(p => p + Projectile.Size * 0.5f)
                .ToList();

            if (list.Count == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (list.Count == 0 || list[0] != Projectile.Center)
                list.Insert(0, Projectile.Center);

            return list.ToArray();
        }

        private float TrailWidthFunc(float completion, Vector2 _)
        {
            float maxW = Projectile.scale * (InPhase3 ? 13f : (InPhase2 ? 10f : 7f));
            const float curveRatio = 0.14f;
            if (completion < curveRatio)
                return (float)Math.Sin(completion / curveRatio * MathHelper.PiOver2) * maxW + curveRatio;
            return Utils.Remap(completion, curveRatio, 1f, maxW, 0f);
        }

        private Color TrailColorFunc(float completion, Vector2 _)
        {
            Color body = Color.Lerp(MainColor, AccentColor,
                Utils.GetLerpValue(0f, 0.52f, completion, true));
            body = Color.Lerp(body, Color.White,
                Utils.GetLerpValue(0.65f, 0f, completion, true) * 0.16f);
            return Color.Lerp(
                body * (Projectile.Opacity * PhaseBrightness),
                Color.Transparent,
                Utils.GetLerpValue(0.80f, 1f, completion, true));
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  SpriteBatch 状态切换辅助
        //  与 BFArrowCommon.BeginAdditiveBatch / BeginAlphaBatch 完全相同逻辑
        // ══════════════════════════════════════════════════════════════════════════
        private static void BeginAdditive()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private static void EndAdditive()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
