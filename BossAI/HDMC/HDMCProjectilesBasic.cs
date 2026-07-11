using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Systems;
using CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    /// <summary>
    /// HDMC Boss 共享工具：配色、伤害换算、粒子速写。
    /// 全部特效沿用高维矩阵核心武器的视觉语言（HyperdimensionalMatrixVisuals）。
    /// </summary>
    internal static class HDMCUtil
    {
        /// <summary>数据流配色（同武器）。</summary>
        public static Color DataColor(float offset, float opacity = 1f)
            => HyperdimensionalMatrixVisuals.GetDataColor(offset, opacity);

        /// <summary>
        /// 敌对弹幕伤害：原版对 hostile 弹幕在专家/大师有 ×2/×4 缩放，
        /// 因此传入值取 意图伤害/4 的常用约定。
        /// </summary>
        public static int HostileDamage(NPC npc, float mult)
            => Math.Max(1, (int)(npc.damage * mult * 0.25f));

        /// <summary>渐入渐出透明度。</summary>
        public static float FadeInOut(int age, int lifetime, int edge)
        {
            if (age < edge)
                return age / (float)edge;
            if (age > lifetime - edge)
                return (lifetime - age) / (float)edge;
            return 1f;
        }

        /// <summary>标准数据爆散粒子（GlowOrb + Square 混合）。</summary>
        public static void DataBurstParticles(Vector2 pos, int orbCount, int squareCount, float speedMax)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < orbCount; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(speedMax * 0.3f, speedMax);
                Color c = DataColor(i / (float)Math.Max(1, orbCount));
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    pos, vel, false, 10 + Main.rand.Next(12), 0.55f + Main.rand.NextFloat(0.4f), c, true, false, i < orbCount / 4));
            }
            for (int i = 0; i < squareCount; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(speedMax * 0.2f, speedMax * 0.7f);
                Color c = DataColor(i * 0.11f + 0.05f);
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    pos, vel, false, 24, 1.3f + Main.rand.NextFloat(0.8f), c * 1.5f));
            }
        }

        /// <summary>距离衰减屏幕震动。</summary>
        public static void ScreenShake(Vector2 source, float power, float range)
        {
            if (Main.dedServ || !Main.LocalPlayer.active)
                return;
            float dist = Vector2.Distance(Main.LocalPlayer.Center, source);
            if (dist < range)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * (1f - dist / range));
        }

        /// <summary>确定性哈希 → [0,1)。</summary>
        public static float Hash01(int v)
        {
            unchecked
            {
                v ^= v << 13;
                v ^= v >> 17;
                v ^= v << 5;
            }
            return (v & 0xFFFFFF) / (float)0x1000000;
        }
    }

    // ──────────────────────────────────────────────────────
    // 数据矛（敌对）：直线飞行 + 加速，无追踪
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对数据矛：短暂凝滞展开后沿固定方向加速飞行，带轻微正弦侧摆。
    /// ai[0] = 最大速度（0 = 默认21），ai[1] = 起飞延迟帧数。
    /// </summary>
    public sealed class HDMCLanceHostile : ModProjectile
    {
        private const int Lifetime = 240;
        private int Age => Lifetime - Projectile.timeLeft;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                Projectile.oldPos[i] = Projectile.position;
        }

        public override bool CanHitPlayer(Player target) => Age > 16;

        public override void AI()
        {
            int age = Age;
            int delay = (int)Projectile.ai[1];
            float maxSpeed = Projectile.ai[0] > 0f ? Projectile.ai[0] : 21f;

            if (age < delay)
            {
                Projectile.velocity *= 0.9f; // 凝滞展开
            }
            else
            {
                float speed = Projectile.velocity.Length();
                if (speed < 0.5f)
                    Projectile.velocity = Projectile.rotation.ToRotationVector2() * 2f;
                if (speed < maxSpeed)
                    Projectile.velocity *= 1.055f;

                // 轻微侧摆——有机的"数据流"轨迹，不改变大方向
                float sway = (float)Math.Sin((age + Projectile.identity * 7f) * 0.09f) * 0.008f;
                Projectile.velocity = Projectile.velocity.RotatedBy(sway);
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(Projectile.identity * 0.07f).ToVector3() * 0.25f);

            if (!Main.dedServ && age % 4 == 0)
            {
                Color tc = HDMCUtil.DataColor(Projectile.identity * 0.073f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, -Projectile.velocity * 0.15f, false, 5, 0.35f, tc, true, false, false));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color baseColor = HDMCUtil.DataColor(Projectile.identity * 0.073f);
            float fadeIn = MathHelper.Clamp(Age / 14f, 0f, 1f);
            baseColor *= fadeIn;
            Vector2 center = Projectile.Center;
            Vector2 dir = Projectile.velocity.SafeNormalize(Projectile.rotation.ToRotationVector2());
            Vector2 perp = dir.RotatedBy(MathHelper.PiOver2);

            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;
                float pct = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 a = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 b = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f;
                Main.spriteBatch.DrawLineBetter(a, b, baseColor * (pct * 0.7f), 3f + pct * 2.4f);
            }

            // V 形箭头（同武器数据矛）
            float tipLen = 16f;
            float tipWidth = 7.5f;
            Vector2 tip       = center + dir * tipLen * 0.6f;
            Vector2 leftWing  = center - dir * tipLen * 0.3f + perp * tipWidth;
            Vector2 rightWing = center - dir * tipLen * 0.3f - perp * tipWidth;
            Main.spriteBatch.DrawLineBetter(leftWing,  tip, baseColor, 2.4f);
            Main.spriteBatch.DrawLineBetter(rightWing, tip, baseColor, 2.4f);
            Main.spriteBatch.DrawLineBetter(leftWing, rightWing, baseColor * 0.3f, 1.3f);

            HyperdimensionalMatrixVisuals.DrawNode(tip, baseColor, 7f);
            HyperdimensionalMatrixVisuals.DrawNode(tip, baseColor * 0.2f, 17f);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            HDMCUtil.DataBurstParticles(Projectile.Center, 3, 1, 4f);
        }
    }

    // ──────────────────────────────────────────────────────
    // 几何碎片（敌对）：高速直线短刃
    // ──────────────────────────────────────────────────────

    /// <summary>敌对几何边线碎片：恒速直线，视觉为发光短线段。</summary>
    public sealed class HDMCShardHostile : ModProjectile
    {
        private const int Lifetime = 180;
        private int Age => Lifetime - Projectile.timeLeft;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                Projectile.oldPos[i] = Projectile.position;
        }

        public override bool CanHitPlayer(Player target) => Age > 10;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(Projectile.identity * 0.05f).ToVector3() * 0.22f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = HDMCUtil.DataColor(Projectile.identity * 0.053f);
            color *= MathHelper.Clamp(Age / 10f, 0f, 1f);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;
                float pct = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 a = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 b = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f;
                Main.spriteBatch.DrawLineBetter(a, b, color * (pct * 0.5f), pct * 3.8f);
            }

            Main.spriteBatch.DrawLineBetter(Projectile.Center - forward * 20f, Projectile.Center + forward * 9f, color * 0.28f, 9f);
            Main.spriteBatch.DrawLineBetter(Projectile.Center - forward * 20f, Projectile.Center + forward * 9f, color, 2f);
            Main.spriteBatch.DrawLineBetter(Projectile.Center - forward * 20f, Projectile.Center + forward * 9f, Color.White with { A = 0 } * 0.8f, 0.9f);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            HDMCUtil.DataBurstParticles(Projectile.Center, 3, 1, 4f);
        }
    }

    // ──────────────────────────────────────────────────────
    // 数据激光（敌对）：长预警 → 贯穿光束
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对数据激光：velocity 存单位方向，自身不位移。
    /// ai[0] = 光束长度（0 = 默认1100），ai[1] = 蓄力帧数（0 = 默认45）。
    /// 蓄力期显示细预警线，激活期造成伤害。
    /// </summary>
    public sealed class HDMCLaserHostile : ModProjectile
    {
        private const int ActiveDuration = 26;
        private const int FadeDuration   = 8;
        private const float BeamWidth    = 17f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int ChargeTime => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : 45;
        private float BeamLength => Projectile.ai[0] > 0f ? Projectile.ai[0] : 1100f;
        private int Lifetime => ChargeTime + ActiveDuration + FadeDuration;
        private int Age => (int)Projectile.localAI[0];
        private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120; // 实际由 localAI[0] 计龄
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            int age = Age;
            Projectile.timeLeft = 2;
            if (age >= Lifetime)
            {
                Projectile.Kill();
                return;
            }

            Projectile.rotation = Direction.ToRotation();

            if (age == ChargeTime)
            {
                if (!Main.dedServ)
                {
                    CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, HDMCUtil.DataColor(0.35f), 0.6f);
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.45f, Pitch = 0.1f, MaxInstances = 5 }, Projectile.Center);
                }
                HDMCUtil.ScreenShake(Projectile.Center, 2f, 900f);
            }

            float glow = age >= ChargeTime && age < ChargeTime + ActiveDuration ? 0.7f : 0.25f;
            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(age * 0.02f).ToVector3() * glow);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int age = Age;
            if (age < ChargeTime || age >= ChargeTime + ActiveDuration)
                return false;

            Vector2 start = Projectile.Center;
            Vector2 end = start + Direction * BeamLength;
            float cp = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(), start, end, BeamWidth, ref cp);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            Vector2 dir = Direction;
            Vector2 start = Projectile.Center;
            Vector2 end = start + dir * BeamLength;
            Color baseColor = HDMCUtil.DataColor(Projectile.identity * 0.09f);
            float t = Main.GlobalTimeWrappedHourly;

            if (age < ChargeTime)
            {
                float chargePct = age / (float)ChargeTime;
                Color warnColor = baseColor * (0.14f + chargePct * 0.5f);
                Main.spriteBatch.DrawLineBetter(start, end, warnColor, 0.7f + chargePct * 1.5f);

                float pulse = 3.5f + 2.5f * chargePct * (float)Math.Sin(t * 14f);
                HyperdimensionalMatrixVisuals.DrawNode(start, baseColor * (0.4f + chargePct * 0.5f), pulse);
                HyperdimensionalMatrixVisuals.DrawScanRing(start, 15f + chargePct * 12f, t * 3f,
                    baseColor * (0.3f + chargePct * 0.4f), 16, 1.3f);
            }
            else if (age < ChargeTime + ActiveDuration)
            {
                float activePct = (age - ChargeTime) / (float)ActiveDuration;
                float fade = activePct < 0.15f ? activePct / 0.15f : 1f;

                Main.spriteBatch.DrawLineBetter(start, end, baseColor * (0.25f * fade), BeamWidth * 1.9f);
                Main.spriteBatch.DrawLineBetter(start, end, baseColor * fade, BeamWidth * 0.6f);
                Main.spriteBatch.DrawLineBetter(start, end, Color.White with { A = 0 } * (fade * 0.85f), BeamWidth * 0.18f);

                for (int i = 0; i < 7; i++)
                {
                    float flow = (t * 2.6f + i / 7f) % 1f;
                    HyperdimensionalMatrixVisuals.DrawNode(
                        Vector2.Lerp(start, end, flow), HDMCUtil.DataColor(i * 0.11f, fade), 4.5f);
                }

                HyperdimensionalMatrixVisuals.DrawNode(start, Color.White with { A = 0 } * fade, 8f);
                HyperdimensionalMatrixVisuals.DrawScanRing(start, 22f, t * 3f, baseColor * (fade * 0.6f), 16, 1.6f);
            }
            else
            {
                float fadePct = (age - ChargeTime - ActiveDuration) / (float)FadeDuration;
                Main.spriteBatch.DrawLineBetter(start, end, baseColor * (0.4f * (1f - fadePct)), BeamWidth * 0.35f * (1f - fadePct));
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            HDMCUtil.DataBurstParticles(Projectile.Center, 5, 0, 3f);
        }
    }

    // ──────────────────────────────────────────────────────
    // 聚合爆炸（敌对）：膨胀圆形伤害场
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对聚合爆炸：正弦膨胀-收缩的圆形伤害区。
    /// ai[0] = 最大半径（0 = 默认175）。
    /// </summary>
    public sealed class HDMCFusionBlastHostile : ModProjectile
    {
        private const int Lifetime = 30;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 175f;
        private float Completion => 1f - Projectile.timeLeft / (float)Lifetime;
        private float Radius => MaxRadius * (float)Math.Sin(MathHelper.Pi * MathHelper.Clamp(Completion, 0f, 1f));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.dedServ)
                return;

            HDMCUtil.DataBurstParticles(Projectile.Center, 22, 14, 10f);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center, Vector2.Zero, false, 12, 3.2f, Color.White, true, false, true));

            Color fuseColor = HDMCUtil.DataColor(Main.GlobalTimeWrappedHourly * 0.4f);
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, fuseColor, 1.2f);
            CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(Projectile.Center, fuseColor);
            HDMCUtil.ScreenShake(Projectile.Center, 3.5f, 850f);
            SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndFusionBoom) { Volume = 0.7f }, Projectile.Center);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float r = Radius;
            Vector2 closest = new(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= r * r;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float c = MathHelper.Clamp(Completion, 0f, 1f);
            float r = Radius;
            Color color = HDMCUtil.DataColor(c * 0.5f, 1f - c);

            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, r, c * 4f, color, 40, 4.5f);
            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, r * 0.65f, -c * 6f, color * 0.55f, 30, 2.5f);

            for (int i = 0; i < 16; i++)
            {
                Vector2 dir = (MathHelper.TwoPi * i / 16f).ToRotationVector2();
                Main.spriteBatch.DrawLineBetter(
                    Projectile.Center + dir * r * 0.2f,
                    Projectile.Center + dir * r,
                    color * 0.5f, 2f);
            }

            return false;
        }
    }

    // ──────────────────────────────────────────────────────
    // 环形冲击波（敌对）：带安全缺口的膨胀数据环
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对环形冲击波：从中心匀速膨胀的数据环，环上有两个安全缺口。
    /// 只有环带本身造成伤害——玩家可穿过缺口或跳过环带。
    /// ai[0] = 膨胀速度（0 = 默认7），ai[1] = 最大半径（0 = 默认1250）。
    /// 缺口位置由 identity 确定性生成，所有客户端一致。
    /// </summary>
    public sealed class HDMCRingWaveHostile : ModProjectile
    {
        private const float BandHalfWidth = 15f;
        private const float GapHalfArc = MathHelper.Pi * 0.17f; // 缺口半张角 ~30°
        private const int GraceFrames = 18;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float ExpandSpeed => Projectile.ai[0] > 0f ? Projectile.ai[0] : 7f;
        private float MaxRadius => Projectile.ai[1] > 0f ? Projectile.ai[1] : 1250f;
        private int Age => (int)Projectile.localAI[0];
        private float Radius => Math.Max(0f, (Age - GraceFrames) * ExpandSpeed);

        private float GapAngleA => HDMCUtil.Hash01(Projectile.identity * 61 + 7) * MathHelper.TwoPi;
        private float GapAngleB => GapAngleA + MathHelper.Pi + (HDMCUtil.Hash01(Projectile.identity * 97 + 3) - 0.5f) * 1.4f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Radius >= MaxRadius)
            {
                Projectile.Kill();
                return;
            }

            if (Age == GraceFrames && !Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.5f, Pitch = -0.2f, MaxInstances = 4 }, Projectile.Center);

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(Age * 0.01f).ToVector3() * 0.4f);
        }

        private bool AngleInGap(float angle)
        {
            static float Delta(float a, float b)
            {
                float d = MathHelper.WrapAngle(a - b);
                return Math.Abs(d);
            }
            return Delta(angle, GapAngleA) < GapHalfArc || Delta(angle, GapAngleB) < GapHalfArc;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Age <= GraceFrames)
                return false;

            float r = Radius;
            Vector2 targetCenter = targetHitbox.Center.ToVector2();
            float dist = Vector2.Distance(Projectile.Center, targetCenter);
            float reach = MathF.Max(targetHitbox.Width, targetHitbox.Height) * 0.5f;
            if (Math.Abs(dist - r) > BandHalfWidth + reach)
                return false;

            float angle = (targetCenter - Projectile.Center).ToRotation();
            return !AngleInGap(angle);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float r = Radius;
            if (r < 4f && Age < GraceFrames)
            {
                // 预备闪烁：中心节点警示
                float warmPct = Age / (float)GraceFrames;
                HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center,
                    HDMCUtil.DataColor(0.1f, warmPct), 6f + warmPct * 8f);
                return false;
            }

            float lifePct = MathHelper.Clamp(r / MaxRadius, 0f, 1f);
            float opacity = (1f - lifePct * 0.65f) * MathHelper.Clamp((Age - GraceFrames) / 10f, 0f, 1f);
            const int segments = 72;

            for (int i = 0; i < segments; i++)
            {
                float angleA = MathHelper.TwoPi * i / segments;
                float angleB = MathHelper.TwoPi * (i + 0.85f) / segments;
                float midAngle = (angleA + angleB) * 0.5f;
                if (AngleInGap(midAngle))
                    continue;

                Vector2 a = Projectile.Center + angleA.ToRotationVector2() * r;
                Vector2 b = Projectile.Center + angleB.ToRotationVector2() * r;
                Color c = HDMCUtil.DataColor(i / (float)segments + Main.GlobalTimeWrappedHourly * 0.2f, opacity);
                Main.spriteBatch.DrawLineBetter(a, b, c * 0.3f, 11f);
                Main.spriteBatch.DrawLineBetter(a, b, c, 3.2f);
            }

            // 缺口边缘节点标示——告诉玩家"这里能过"
            foreach (float gap in new[] { GapAngleA, GapAngleB })
            {
                foreach (float edge in new[] { -GapHalfArc, GapHalfArc })
                {
                    Vector2 p = Projectile.Center + (gap + edge).ToRotationVector2() * r;
                    HyperdimensionalMatrixVisuals.DrawNode(p, Color.White with { A = 0 } * opacity, 6f);
                }
            }

            return false;
        }
    }
}
