using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Systems;
using CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    // ──────────────────────────────────────────────────────
    // 沃罗诺伊囚笼（敌对）：雷达扫描式点亮的三角网格牢笼
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对沃罗诺伊囚笼：以生成点为中心构建德劳内三角网格罩，
    /// 网格边以"雷达扫描"方式按角度顺序逐渐点亮——点亮的边造成伤害。
    /// 玩家必须赶在扫描封闭前从尚未点亮的扇区离开。
    /// 同时种子节点周期性朝笼心射出交叉数据矛。
    /// </summary>
    public sealed class HDMCVoronoiHostile : ModProjectile
    {
        private const int BuildEnd = 50;
        private const int ArmEnd   = 145;
        private const int Lifetime = 190;

        private const int SeedCount = 16;
        private const float CageRadiusMin = 190f;
        private const float CageRadiusMax = 330f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;

        private Vector2[] _seeds;
        private List<(int a, int b)> _edges;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
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
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        private void EnsureGeometry()
        {
            if (_seeds != null)
                return;

            _seeds = new Vector2[SeedCount];
            for (int i = 0; i < SeedCount; i++)
            {
                float angle = HDMCUtil.Hash01(i * 73 + Projectile.identity * 37) * MathHelper.TwoPi;
                float dist = CageRadiusMin + HDMCUtil.Hash01(i * 131 + Projectile.identity * 59) * (CageRadiusMax - CageRadiusMin);
                _seeds[i] = Projectile.Center + angle.ToRotationVector2() * dist;
            }

            var edgeSet = new HashSet<(int, int)>();
            for (int i = 0; i < SeedCount; i++)
            {
                var neighbors = new List<(int idx, float dist)>();
                for (int j = 0; j < SeedCount; j++)
                {
                    if (j == i) continue;
                    neighbors.Add((j, Vector2.DistanceSquared(_seeds[i], _seeds[j])));
                }
                neighbors.Sort((a, b) => a.dist.CompareTo(b.dist));
                for (int k = 0; k < Math.Min(3, neighbors.Count); k++)
                    edgeSet.Add((Math.Min(i, neighbors[k].idx), Math.Max(i, neighbors[k].idx)));
            }
            _edges = new List<(int a, int b)>(edgeSet);
        }

        /// <summary>雷达扫描角：在武装阶段从 0 → 2π 顺时针扫过。边中点角度小于扫描角即点亮。</summary>
        private float SweepAngle
        {
            get
            {
                int age = Age;
                if (age <= BuildEnd)
                    return 0f;
                return MathHelper.Clamp((age - BuildEnd) / (float)(ArmEnd - BuildEnd), 0f, 1f) * MathHelper.TwoPi;
            }
        }

        private float SweepStartAngle => HDMCUtil.Hash01(Projectile.identity * 17 + 5) * MathHelper.TwoPi;

        private bool EdgeIsLit((int a, int b) edge)
        {
            Vector2 mid = (_seeds[edge.a] + _seeds[edge.b]) * 0.5f;
            float angle = MathHelper.WrapAngle((mid - Projectile.Center).ToRotation() - SweepStartAngle);
            if (angle < 0f)
                angle += MathHelper.TwoPi;
            return angle < SweepAngle;
        }

        public override void AI()
        {
            EnsureGeometry();
            int age = Age;

            // 种子节点周期性朝笼心射矛（交叉封锁，非瞄准玩家）
            if (Main.netMode != NetmodeID.MultiplayerClient && age > BuildEnd && age < ArmEnd && age % 20 == 0)
            {
                int seedIdx = (age / 20 * 5 + Projectile.identity) % SeedCount;
                Vector2 from = _seeds[seedIdx];
                Vector2 dir = (Projectile.Center - from).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), from, dir * 2f,
                    ModContent.ProjectileType<HDMCLanceHostile>(),
                    Math.Max(1, (int)(Projectile.damage * 0.8f)), 0f, Main.myPlayer, 15f, 6f);
            }

            if (age == BuildEnd && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.4f, Pitch = 0.1f, MaxInstances = 3 }, Projectile.Center);
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, HDMCUtil.DataColor(0.5f), 0.7f);
            }

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(age * 0.01f).ToVector3() * 0.3f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (_seeds == null || Age <= BuildEnd)
                return false;

            float cp = 0f;
            foreach (var edge in _edges)
            {
                if (!EdgeIsLit(edge))
                    continue;
                if (Collision.CheckAABBvLineCollision(
                    targetHitbox.TopLeft(), targetHitbox.Size(),
                    _seeds[edge.a], _seeds[edge.b], 6f, ref cp))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnsureGeometry();
            int age = Age;
            float t = Main.GlobalTimeWrappedHourly;
            float fadeIn = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);
            float fadeOut = age > Lifetime - 20 ? (Lifetime - age) / 20f : 1f;
            float opacity = fadeIn * fadeOut;

            for (int e = 0; e < _edges.Count; e++)
            {
                var edge = _edges[e];
                bool lit = age > BuildEnd && EdgeIsLit(edge);
                Color c = HDMCUtil.DataColor(e / (float)_edges.Count, lit ? opacity : opacity * 0.22f);
                float w = lit ? 2.6f : 1.2f;
                Main.spriteBatch.DrawLineBetter(_seeds[edge.a], _seeds[edge.b], c, w);
                if (lit)
                    Main.spriteBatch.DrawLineBetter(_seeds[edge.a], _seeds[edge.b], c * 0.25f, 7f);
            }

            for (int i = 0; i < SeedCount; i++)
            {
                Color nc = HDMCUtil.DataColor(i / (float)SeedCount, opacity);
                HyperdimensionalMatrixVisuals.DrawNode(_seeds[i], nc, 4.5f);
            }

            // 扫描指针——直观显示封锁进度
            if (age > BuildEnd && age < ArmEnd)
            {
                float pointerAngle = SweepStartAngle + SweepAngle;
                Vector2 pointerEnd = Projectile.Center + pointerAngle.ToRotationVector2() * (CageRadiusMax + 25f);
                Color pc = Color.White with { A = 0 } * (opacity * 0.5f);
                Main.spriteBatch.DrawLineBetter(Projectile.Center, pointerEnd, pc, 1.4f);
                HyperdimensionalMatrixVisuals.DrawNode(pointerEnd, pc * 1.4f, 5f);
            }

            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, CageRadiusMax + 20f, t * 0.8f,
                HDMCUtil.DataColor(0.3f, opacity * 0.35f), 28, 1.4f);

            return false;
        }
    }

    // ──────────────────────────────────────────────────────
    // 谢尔宾斯基坍缩（敌对）：分形三角波次点亮牢笼
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对谢尔宾斯基坍缩：在生成点展开大型递归分形三角，27 个叶三角
    /// 分 3 波先预警（暗淡填充）后激活（明亮伤害），波次错开保证任意时刻
    /// 都有安全格。终结时在质心引爆聚合爆炸。
    /// </summary>
    public sealed class HDMCSierpinskiHostile : ModProjectile
    {
        private const int BuildEnd  = 55;
        private const int WavePeriod = 62;   // 每波：预警 26 + 激活 36
        private const int WaveWarn   = 26;
        private const int WaveCount  = 3;
        private const int CollapseFrame = BuildEnd + WavePeriod * WaveCount;
        private const int Lifetime  = CollapseFrame + 34;

        private const float TriRadius = 430f;
        private const int SubDepth = 3; // 3^3 = 27 叶三角

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;

        private Vector2[][] _leafTris; // 每个叶三角的3个顶点（生成时冻结旋转）

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
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        private void EnsureGeometry()
        {
            if (_leafTris != null)
                return;

            float rot = HDMCUtil.Hash01(Projectile.identity * 29 + 11) * MathHelper.TwoPi;
            Vector2 a = Projectile.Center + rot.ToRotationVector2() * TriRadius;
            Vector2 b = Projectile.Center + (rot + MathHelper.TwoPi / 3f).ToRotationVector2() * TriRadius;
            Vector2 c = Projectile.Center + (rot + MathHelper.TwoPi * 2f / 3f).ToRotationVector2() * TriRadius;

            var list = new List<Vector2[]>();
            Subdivide(a, b, c, SubDepth, list);
            _leafTris = list.ToArray();
        }

        private static void Subdivide(Vector2 a, Vector2 b, Vector2 c, int depth, List<Vector2[]> outList)
        {
            if (depth <= 0)
            {
                outList.Add(new[] { a, b, c });
                return;
            }
            Vector2 ab = (a + b) * 0.5f;
            Vector2 bc = (b + c) * 0.5f;
            Vector2 ca = (c + a) * 0.5f;
            Subdivide(a, ab, ca, depth - 1, outList);
            Subdivide(ab, b, bc, depth - 1, outList);
            Subdivide(ca, bc, c, depth - 1, outList);
        }

        private int TriWave(int idx) => (int)(HDMCUtil.Hash01(idx * 53 + Projectile.identity * 13) * WaveCount);

        /// <summary>返回：-1 未开始，0 预警中，1 激活伤害中，2 已结束。</summary>
        private int TriState(int idx)
        {
            int age = Age;
            int wave = TriWave(idx);
            int waveStart = BuildEnd + wave * WavePeriod;
            if (age < waveStart)
                return -1;
            if (age < waveStart + WaveWarn)
                return 0;
            if (age < waveStart + WavePeriod)
                return 1;
            return 2;
        }

        public override void AI()
        {
            EnsureGeometry();
            int age = Age;

            if (age == CollapseFrame && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<HDMCFusionBlastHostile>(),
                    Projectile.damage, 0f, Main.myPlayer, 200f);
            }
            if (age == CollapseFrame && !Main.dedServ)
            {
                CLCBLightingBoltsSystem.Spawn_MatrixSingularityCollapse(Projectile.Center);
                HDMCUtil.ScreenShake(Projectile.Center, 3f, 800f);
            }

            // 每波激活音效
            for (int w = 0; w < WaveCount; w++)
            {
                if (age == BuildEnd + w * WavePeriod + WaveWarn && !Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.35f, Pitch = 0.2f + w * 0.12f, MaxInstances = 3 }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(age * 0.01f).ToVector3() * 0.35f);
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);

            static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
                => (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (_leafTris == null || Age <= BuildEnd)
                return false;

            // 距离粗筛
            Vector2 targetCenter = targetHitbox.Center.ToVector2();
            if (Vector2.DistanceSquared(targetCenter, Projectile.Center) > (TriRadius + 80f) * (TriRadius + 80f))
                return false;

            Vector2[] checkPoints =
            {
                targetCenter,
                targetHitbox.TopLeft(),
                new(targetHitbox.Right, targetHitbox.Top),
                new(targetHitbox.Left, targetHitbox.Bottom),
                new(targetHitbox.Right, targetHitbox.Bottom)
            };

            for (int i = 0; i < _leafTris.Length; i++)
            {
                if (TriState(i) != 1)
                    continue;
                Vector2[] tri = _leafTris[i];
                foreach (Vector2 p in checkPoints)
                {
                    if (PointInTriangle(p, tri[0], tri[1], tri[2]))
                        return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnsureGeometry();
            int age = Age;
            float t = Main.GlobalTimeWrappedHourly;
            float fadeIn = MathHelper.Clamp(age / 18f, 0f, 1f);
            float fadeOut = age > Lifetime - 18 ? (Lifetime - age) / 18f : 1f;
            float opacity = fadeIn * fadeOut;

            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);

            for (int i = 0; i < _leafTris.Length; i++)
            {
                // 建造期：按索引顺序淡入
                float appearThreshold = i / (float)_leafTris.Length;
                if (buildPct < appearThreshold)
                    continue;

                Vector2[] tri = _leafTris[i];
                int state = TriState(i);

                Color c;
                float lineW;
                switch (state)
                {
                    case 0: // 预警：暗淡脉冲
                        float warnPulse = 0.35f + 0.3f * MathF.Sin(t * 12f + i);
                        c = HDMCUtil.DataColor(i / (float)_leafTris.Length, opacity * warnPulse);
                        lineW = 1.6f;
                        break;
                    case 1: // 激活：明亮 + 中心节点
                        c = HDMCUtil.DataColor(i / (float)_leafTris.Length, opacity) * 1.4f;
                        lineW = 2.8f;
                        Vector2 centroid = (tri[0] + tri[1] + tri[2]) / 3f;
                        HyperdimensionalMatrixVisuals.DrawNode(centroid, c, 4.5f);
                        break;
                    case 2: // 结束：残影
                        c = HDMCUtil.DataColor(i / (float)_leafTris.Length, opacity * 0.15f);
                        lineW = 1f;
                        break;
                    default: // 未开始：结构线
                        c = HDMCUtil.DataColor(i / (float)_leafTris.Length, opacity * 0.3f);
                        lineW = 1.2f;
                        break;
                }

                Main.spriteBatch.DrawLineBetter(tri[0], tri[1], c, lineW);
                Main.spriteBatch.DrawLineBetter(tri[1], tri[2], c, lineW);
                Main.spriteBatch.DrawLineBetter(tri[2], tri[0], c, lineW);
            }

            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, TriRadius * 1.08f, t * 0.7f,
                HDMCUtil.DataColor(0.45f, opacity * 0.3f), 30, 1.4f);

            // 终结前质心警示
            if (age > CollapseFrame - 40 && age < CollapseFrame)
            {
                float warn = (age - (CollapseFrame - 40)) / 40f;
                float pulse = 0.5f + 0.5f * MathF.Sin(t * 16f);
                HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center,
                    Color.White with { A = 0 } * (warn * pulse), 8f + warn * 10f);
                HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, 200f * (1f - warn * 0.4f), t * 3f,
                    HDMCUtil.DataColor(0.05f, warn * 0.6f), 20, 2f);
            }

            return false;
        }
    }

    // ──────────────────────────────────────────────────────
    // 洛伦兹蝶群区（敌对）：漂移的混沌吸引子伤害区
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对洛伦兹蝶群区：三条洛伦兹吸引子轨迹构成的持续伤害区域，
    /// 淡入预警后激活，整体缓慢漂移。轨迹段实时随 3D 旋转投影，
    /// 视觉与碰撞使用同一投影保证所见即所得。
    /// </summary>
    public sealed class HDMCLorenzHostile : ModProjectile
    {
        private const int WarmupEnd = 45;
        private const int Lifetime  = 400;

        private const float Sigma = 10f;
        private const float Rho   = 28f;
        private const float Beta  = 8f / 3f;
        private const float Dt    = 0.008f;
        private const int   Steps = 220;
        private const float FitScale = 150f / 25f;

        private const int TrailCount = 3;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;

        private Vector3[][] _trails;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
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
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (_trails == null)
                _trails = BuildTrails();

            // 缓慢漂移（velocity 由生成者设置，这里持续衰减到低速巡航）
            if (Projectile.velocity.Length() > 1.1f)
                Projectile.velocity *= 0.985f;

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(Age * 0.008f).ToVector3() * 0.3f);

            if (!Main.dedServ && Age % 6 == 0 && Age > WarmupEnd)
            {
                Color c = HDMCUtil.DataColor(Main.rand.NextFloat());
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(120f, 120f),
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 1.5f),
                    false, 10 + Main.rand.Next(8), 0.4f, c, true, false, false));
            }
        }

        private Matrix CurrentRotation
        {
            get
            {
                float time = Main.GlobalTimeWrappedHourly;
                return Matrix.CreateFromYawPitchRoll(time * 0.3f, time * 0.2f, time * 0.15f);
            }
        }

        private Vector2 Project3D(Vector3 pt, Vector2 center, Matrix rotation)
        {
            Vector3 rotated = Vector3.Transform(pt, rotation);
            float perspective = 620f / MathF.Max(180f, 620f + rotated.Z);
            return center + new Vector2(rotated.X * perspective, rotated.Y * perspective);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (_trails == null || Age <= WarmupEnd)
                return false;

            Vector2 targetCenter = targetHitbox.Center.ToVector2();
            if (Vector2.DistanceSquared(targetCenter, Projectile.Center) > 340f * 340f)
                return false;

            Matrix rot = CurrentRotation;
            float cp = 0f;
            for (int trail = 0; trail < TrailCount; trail++)
            {
                Vector3[] data = _trails[trail];
                // 每4段采样一次碰撞，覆盖足够密
                for (int i = 0; i < data.Length - 4; i += 4)
                {
                    Vector2 a = Project3D(data[i], Projectile.Center, rot);
                    Vector2 b = Project3D(data[i + 4], Projectile.Center, rot);
                    if (Collision.CheckAABBvLineCollision(
                        targetHitbox.TopLeft(), targetHitbox.Size(), a, b, 7f, ref cp))
                        return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (_trails == null)
                return false;

            int age = Age;
            float warmupPct = MathHelper.Clamp(age / (float)WarmupEnd, 0f, 1f);
            float fadeOut = age > Lifetime - 25 ? (Lifetime - age) / 25f : 1f;
            // 预警期半透明，激活后全亮
            float fade = (age <= WarmupEnd ? 0.3f + warmupPct * 0.25f : 1f) * fadeOut;
            float time = Main.GlobalTimeWrappedHourly;

            Vector2 center = Projectile.Center;
            Matrix rot = CurrentRotation;

            for (int trail = 0; trail < TrailCount; trail++)
            {
                Vector3[] trailData = _trails[trail];
                float colorOffset = trail * 0.33f;

                for (int i = 0; i < trailData.Length - 1; i++)
                {
                    Vector2 a = Project3D(trailData[i], center, rot);
                    Vector2 b = Project3D(trailData[i + 1], center, rot);
                    float pct = i / (float)trailData.Length;
                    Color c = HDMCUtil.DataColor(pct + colorOffset, fade * 0.7f);
                    Main.spriteBatch.DrawLineBetter(a, b, c, age > WarmupEnd ? 2f : 1.2f);
                }

                for (int n = 0; n < 3; n++)
                {
                    float flow = (time * 0.6f + n / 3f + trail * 0.11f) % 1f;
                    int idx = Math.Min((int)(flow * (trailData.Length - 1)), trailData.Length - 2);
                    float frac = flow * (trailData.Length - 1) - idx;
                    Vector3 interpPt = Vector3.Lerp(trailData[idx], trailData[idx + 1], frac);
                    Vector2 nodePos = Project3D(interpPt, center, rot);
                    HyperdimensionalMatrixVisuals.DrawNode(nodePos, HDMCUtil.DataColor(flow + colorOffset, fade), 5f);
                }
            }

            HyperdimensionalMatrixVisuals.DrawScanRing(center, 260f, time * 1f,
                HDMCUtil.DataColor(0.55f, fade * 0.3f), 26, 1.3f);

            return false;
        }

        private Vector3[][] BuildTrails()
        {
            Vector3[][] trails = new Vector3[TrailCount][];
            Vector3[] inits =
            {
                new(1f, 1f, 1f),
                new(1.01f, 1f, 1f),
                new(-1f, -1f, 1f)
            };

            for (int t = 0; t < TrailCount; t++)
            {
                trails[t] = new Vector3[Steps];
                Vector3 pos = inits[t];
                for (int s = 0; s < Steps; s++)
                {
                    trails[t][s] = pos * FitScale;
                    float dx = Sigma * (pos.Y - pos.X);
                    float dy = pos.X * (Rho - pos.Z) - pos.Y;
                    float dz = pos.X * pos.Y - Beta * pos.Z;
                    pos += new Vector3(dx, dy, dz) * Dt;
                }
            }

            return trails;
        }
    }

    // ──────────────────────────────────────────────────────
    // 数据奇点（敌对终章）：聚焦 → 牵引 → 坍缩 → 全屏爆发
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对数据奇点：Boss 终章。白色奇点聚焦成形 → 温和牵引全场玩家 →
    /// 坍缩 → 引爆三重环形冲击波 + 十二向放射激光 + 中心聚合爆炸。
    /// 牵引力有上限且可通过反向移动抵抗——压迫但不锁死。
    /// </summary>
    public sealed class HDMCSingularityHostile : ModProjectile
    {
        private const int FocusEnd    = 90;
        private const int PullEnd     = 210;
        private const int CollapseEnd = 240;
        private const int Lifetime    = 300;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == 1 && !Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSingularity) { Volume = 0.85f }, Projectile.Center);

            // 牵引阶段：温和吸引，可被反向移动抵抗
            if (age > FocusEnd && age <= PullEnd)
            {
                foreach (Player player in Main.ActivePlayers)
                {
                    if (player.dead)
                        continue;
                    float dist = Vector2.Distance(player.Center, Projectile.Center);
                    if (dist > 1700f || dist < 60f)
                        continue;

                    float pullStrength = MathHelper.Lerp(0.24f, 0.06f, dist / 1700f);
                    Vector2 pull = (Projectile.Center - player.Center).SafeNormalize(Vector2.Zero) * pullStrength;
                    player.velocity += pull;
                }
            }

            if (age == CollapseEnd)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Detonate();
                if (!Main.dedServ)
                {
                    CLCBLightingBoltsSystem.Spawn_MatrixSingularityCollapse(Projectile.Center);
                    HDMCUtil.DataBurstParticles(Projectile.Center, 36, 20, 13f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center, Vector2.Zero, false, 16, 5.5f, Color.White, true, false, true));
                    HDMCUtil.ScreenShake(Projectile.Center, 8f, 1600f);
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndCompileStorm) { Volume = 0.9f }, Projectile.Center);
                }
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.9f, 1f) * (age > FocusEnd ? 0.9f : 0.4f));
        }

        private void Detonate()
        {
            int damage = Projectile.damage;

            // 三重环波错峰扩散
            for (int i = 0; i < 3; i++)
            {
                int ring = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<HDMCRingWaveHostile>(),
                    damage, 0f, Main.myPlayer, 6.2f + i * 1.6f, 1500f);
                if (ring >= 0 && ring < Main.maxProjectiles)
                    Main.projectile[ring].localAI[0] = -i * 22f; // 错峰
            }

            // 十二向放射激光（固定角度轮盘）
            float baseAngle = HDMCUtil.Hash01(Projectile.identity * 41) * MathHelper.TwoPi;
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = (baseAngle + MathHelper.TwoPi * i / 12f).ToRotationVector2();
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), Projectile.Center, dir,
                    ModContent.ProjectileType<HDMCLaserHostile>(),
                    damage, 0f, Main.myPlayer, 1500f, 34f);
            }

            // 中心聚合爆炸
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<HDMCFusionBlastHostile>(),
                damage, 0f, Main.myPlayer, 260f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float t = Main.GlobalTimeWrappedHourly;
            Vector2 center = Projectile.Center;

            if (age < FocusEnd)
            {
                // 聚焦：白点成形 + 内收扫描环
                float focusPct = age / (float)FocusEnd;
                HyperdimensionalMatrixVisuals.DrawNode(center, Color.White with { A = 0 } * focusPct, 6f + focusPct * 14f);

                for (int i = 0; i < 3; i++)
                {
                    float ringR = (1f - focusPct) * (320f - i * 70f);
                    Color rColor = HDMCUtil.DataColor(i * 0.28f, focusPct * 0.5f);
                    HyperdimensionalMatrixVisuals.DrawScanRing(center, ringR, t * (1.2f + i * 0.3f), rColor, 22, 2f);
                }
            }
            else if (age < PullEnd)
            {
                // 牵引：向心数据流线 + 稳定奇点
                float pullPct = (age - FocusEnd) / (float)(PullEnd - FocusEnd);
                HyperdimensionalMatrixVisuals.DrawNode(center, Color.White with { A = 0 }, 18f + 4f * MathF.Sin(t * 8f));
                HyperdimensionalMatrixVisuals.DrawNode(center, Color.White with { A = 0 } * 0.3f, 44f);

                for (int i = 0; i < 20; i++)
                {
                    float angle = MathHelper.TwoPi * i / 20f + t * 0.6f;
                    float streamLen = 500f - 260f * ((t * 1.4f + i * 0.17f) % 1f);
                    Vector2 streamStart = center + angle.ToRotationVector2() * streamLen;
                    Color streamColor = HDMCUtil.DataColor(i * 0.05f, 0.5f);
                    Main.spriteBatch.DrawLineBetter(streamStart, Vector2.Lerp(streamStart, center, 0.15f), streamColor, 1.5f);
                    if (i % 4 == 0)
                        HyperdimensionalMatrixVisuals.DrawNode(streamStart, streamColor, 4f);
                }

                HyperdimensionalMatrixVisuals.DrawGeometry(center, MatrixGeometryShape.Icosahedron,
                    60f + pullPct * 20f, t * 2.5f, 0.6f, Projectile.identity);
            }
            else if (age < CollapseEnd)
            {
                // 坍缩：急剧收缩 + 增亮
                float collapsePct = (age - PullEnd) / (float)(CollapseEnd - PullEnd);
                float size = MathHelper.Lerp(20f, 5f, collapsePct);
                HyperdimensionalMatrixVisuals.DrawNode(center, Color.White with { A = 0 } * (1f + collapsePct * 2f), size);
                HyperdimensionalMatrixVisuals.DrawScanRing(center, 80f * (1f - collapsePct), t * 5f,
                    Color.White with { A = 0 } * collapsePct, 16, 2.5f);
            }
            else
            {
                // 爆发余韵：外扩残光（实际伤害由子弹幕承担）
                float postPct = (age - CollapseEnd) / (float)(Lifetime - CollapseEnd);
                Color afterColor = HDMCUtil.DataColor(postPct * 0.5f, 1f - postPct);
                HyperdimensionalMatrixVisuals.DrawScanRing(center, postPct * 420f, t * 3f, afterColor, 36, 4f);
            }

            return false;
        }
    }
}
