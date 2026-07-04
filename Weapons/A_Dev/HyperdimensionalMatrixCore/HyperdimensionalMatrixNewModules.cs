using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    // ──────────────────────────────────────────────────────
    // MODULE · MÖBIUS RING
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 莫比乌斯数据环模块：在目标周围构建一条旋转的莫比乌斯带，
    /// 数据节点沿环面流动，构建完成后从环面均匀取点向目标射出 MatrixGridCell。
    /// </summary>
    public sealed class MatrixMobiusRing : ModProjectile, ILocalizedModType
    {
        private const int BuildEnd  = 55;
        private const int FlashEnd  = 65;
        private const int FireFrame = 66;
        private const int Lifetime  = 95;

        private const float StripR     = 28f;
        private const int   StripPts   = 28;
        private const int   FlowNodes  = 7;
        private const int   FireCount  = 8;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;

        // ai[0] = coreWhoAmI, ai[1] = targetIndex
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                SpawnCells();

            if (age == FireFrame && !Main.dedServ)
                SpawnFireParticles();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.012f).ToVector3() * 0.3f);
        }

        private void SpawnFireParticles()
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 center = GetAnchor();
            Vector2[] pts = GetStripPoints(center, time);

            for (int i = 0; i < FireCount; i++)
            {
                int idx = i * StripPts / FireCount;
                if (idx >= pts.Length) idx = pts.Length - 1;
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)FireCount);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    pts[idx], Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 3f),
                    false, 8 + Main.rand.Next(6), 0.55f, c, true, false, i < 3));
                if (Main.rand.NextBool(3))
                    GeneralParticleHandler.SpawnParticle(new SquareParticle(
                        pts[idx], Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 2f),
                        false, 16, 0.9f, c * 1.4f));
            }

            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(center,
                HyperdimensionalMatrixVisuals.GetDataColor(0.5f), 0.6f);
        }

        private void SpawnCells()
        {
            NPC target = GetTarget();
            Vector2 aimPos = target?.Center ?? Projectile.Center;
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 center = GetAnchor();
            Vector2[] pts = GetStripPoints(center, time);

            for (int i = 0; i < FireCount; i++)
            {
                int idx = i * StripPts / FireCount;
                if (idx >= pts.Length) idx = pts.Length - 1;
                Vector2 dir = (aimPos - pts[idx]).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    pts[idx],
                    dir * 22f,
                    ModContent.ProjectileType<MatrixGridCell>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    TargetIndex);
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.36f, Pitch = 0.15f, MaxInstances = 4 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);
            float fadeOut = age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;
            float opacity = buildPct * fadeOut;

            // Flash expansion after FlashEnd
            float flashExpand = 0f;
            if (age >= FlashEnd && age < Lifetime)
                flashExpand = (age - FlashEnd) / (float)(Lifetime - FlashEnd);

            Vector2 center = GetAnchor();
            Vector2[] pts = GetStripPoints(center, time);

            // Draw Möbius strip as connected line segments
            int visibleCount = (int)(pts.Length * buildPct);
            for (int i = 0; i < visibleCount - 1; i++)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)pts.Length, opacity);
                float width = 1.8f + flashExpand * 2f;
                Main.spriteBatch.DrawLineBetter(pts[i], pts[i + 1], c, width);
            }
            // Close the loop
            if (visibleCount >= pts.Length && pts.Length > 1)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(0.95f, opacity);
                Main.spriteBatch.DrawLineBetter(pts[pts.Length - 1], pts[0], c, 1.8f + flashExpand * 2f);
            }

            // Flowing data nodes along the strip
            for (int n = 0; n < FlowNodes; n++)
            {
                float flow = (time * 0.8f + n / (float)FlowNodes) % 1f;
                int segIdx = (int)(flow * (pts.Length - 1));
                float segFrac = flow * (pts.Length - 1) - segIdx;
                if (segIdx >= pts.Length - 1) { segIdx = pts.Length - 2; segFrac = 1f; }
                Vector2 nodePos = Vector2.Lerp(pts[segIdx], pts[segIdx + 1], segFrac);
                Color nc = HyperdimensionalMatrixVisuals.GetDataColor(flow, opacity);
                HyperdimensionalMatrixVisuals.DrawNode(nodePos, nc, 4.5f);
            }

            // Scan ring around center
            if (buildPct > 0.5f)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin(time * 10f);
                HyperdimensionalMatrixVisuals.DrawScanRing(center, StripR * 1.5f + flashExpand * 20f, time * 2f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.65f, opacity * pulse * 0.5f), 20, 1.5f);
            }

            return false;
        }

        /// <summary>莫比乌斯带参数曲面采样，投影到 2D。</summary>
        private Vector2[] GetStripPoints(Vector2 center, float time)
        {
            Vector2[] pts = new Vector2[StripPts];
            Matrix rot = Matrix.CreateFromYawPitchRoll(time * 0.8f, time * 0.55f, time * 0.3f);

            for (int i = 0; i < StripPts; i++)
            {
                float u = MathHelper.TwoPi * i / StripPts;
                // Möbius strip center-line (v=0)
                float cx = (StripR + 0f) * MathF.Cos(u);
                float cy = (StripR + 0f) * MathF.Sin(u);
                float cz = 0f * MathF.Sin(u * 0.5f);

                Vector3 pt3 = Vector3.Transform(new Vector3(cx, cy, cz), rot);
                float perspective = 620f / MathF.Max(180f, 620f + pt3.Z);
                pts[i] = center + new Vector2(pt3.X * perspective, pt3.Y * perspective);
            }

            return pts;
        }

        private Vector2 GetAnchor()
        {
            NPC target = GetTarget();
            return target?.Center ?? Projectile.Center;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · FRACTAL TREE
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 分形递归树模块：从目标方向生长一棵 L-System 分形树，
    /// 逐层展开到最大深度后，所有叶节点同时向目标发射 MatrixGeoShard。
    /// 共 2^4 = 16 条碎片。
    /// </summary>
    public sealed class MatrixFractalTree : ModProjectile, ILocalizedModType
    {
        private const int BuildEnd  = 65;
        private const int FlashEnd  = 75;
        private const int FireFrame = 76;
        private const int Lifetime  = 105;

        private const int   MaxDepth      = 4;
        private const float InitialLength = 45f;
        private const float BranchAngle   = MathHelper.Pi / 180f * 35f; // 35 degrees
        private const float LengthRatio   = 0.65f;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                SpawnShards();

            if (age == FireFrame && !Main.dedServ)
                SpawnLeafParticles();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.01f).ToVector3() * 0.3f);
        }

        private void SpawnLeafParticles()
        {
            NPC target = GetTarget();
            Vector2 anchor = target?.Center ?? Projectile.Center;
            float baseAngle = target != null
                ? (anchor - Projectile.Center).ToRotation()
                : -MathHelper.PiOver2;

            List<(Vector2 pos, float angle)> leaves = new();
            CollectLeaves(Projectile.Center, baseAngle, InitialLength, 0, leaves);

            foreach (var (pos, _) in leaves)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(Main.rand.NextFloat());
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    pos, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f),
                    false, 8 + Main.rand.Next(8), 0.5f, c, true, false, false));
            }

            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center,
                HyperdimensionalMatrixVisuals.GetDataColor(0.4f), 0.5f);
        }

        private void SpawnShards()
        {
            NPC target = GetTarget();
            Vector2 anchor = target?.Center ?? Projectile.Center;
            float baseAngle = target != null
                ? (anchor - Projectile.Center).ToRotation()
                : -MathHelper.PiOver2;

            List<(Vector2 pos, float angle)> leaves = new();
            CollectLeaves(Projectile.Center, baseAngle, InitialLength, 0, leaves);

            foreach (var (pos, angle) in leaves)
            {
                Vector2 dir = angle.ToRotationVector2();
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    pos,
                    dir * 26f,
                    ModContent.ProjectileType<MatrixGeoShard>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    TargetIndex);
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.36f, Pitch = 0.15f, MaxInstances = 4 }, Projectile.Center);
        }

        /// <summary>收集所有叶节点（depth == MaxDepth）的位置和角度。</summary>
        private void CollectLeaves(Vector2 origin, float angle, float length, int depth,
            List<(Vector2 pos, float angle)> leaves)
        {
            Vector2 end = origin + angle.ToRotationVector2() * length;
            if (depth >= MaxDepth)
            {
                leaves.Add((end, angle));
                return;
            }
            CollectLeaves(end, angle - BranchAngle, length * LengthRatio, depth + 1, leaves);
            CollectLeaves(end, angle + BranchAngle, length * LengthRatio, depth + 1, leaves);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float fadeOut = age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;
            float opacity = fadeOut;

            // Visible depth animates during build
            float visibleDepth = MathHelper.Clamp((float)age / BuildEnd * MaxDepth, 0f, MaxDepth);
            bool flashing = age >= FlashEnd;

            NPC target = GetTarget();
            Vector2 anchor = target?.Center ?? Projectile.Center;
            float baseAngle = target != null
                ? (anchor - Projectile.Center).ToRotation()
                : -MathHelper.PiOver2;

            DrawBranch(Projectile.Center, baseAngle, InitialLength, 0, visibleDepth, opacity, flashing);

            // Scan ring
            if (age > BuildEnd * 0.5f)
            {
                float t = Main.GlobalTimeWrappedHourly;
                HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, InitialLength * 1.3f, t * 1.5f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.7f, opacity * 0.35f), 20, 1.2f);
            }

            return false;
        }

        /// <summary>递归绘制分形树的分支。</summary>
        private void DrawBranch(Vector2 origin, float angle, float length, int depth,
            float visibleDepth, float opacity, bool flashing)
        {
            if (depth > visibleDepth)
                return;

            // Partial length for the currently-growing layer
            float layerPct = depth < (int)visibleDepth
                ? 1f
                : MathHelper.Clamp(visibleDepth - depth, 0f, 1f);

            float drawLength = length * layerPct;
            Vector2 end = origin + angle.ToRotationVector2() * drawLength;

            // Each depth gets a different color offset
            Color c = HyperdimensionalMatrixVisuals.GetDataColor(depth / (float)MaxDepth, opacity);
            float lineWidth = 2.5f - depth * 0.35f;
            if (lineWidth < 1f) lineWidth = 1f;

            Main.spriteBatch.DrawLineBetter(origin, end, c, lineWidth);

            // Leaf nodes (depth == MaxDepth) drawn as pulsing nodes
            if (depth >= MaxDepth)
            {
                float t = Main.GlobalTimeWrappedHourly;
                float pulse = 3f + 1.5f * MathF.Sin(t * 8f + depth + angle);
                Color leafColor = flashing
                    ? HyperdimensionalMatrixVisuals.GetDataColor(angle * 0.3f, opacity) * 1.5f
                    : HyperdimensionalMatrixVisuals.GetDataColor(angle * 0.3f, opacity * 0.8f);
                HyperdimensionalMatrixVisuals.DrawNode(end, leafColor, pulse);
                return;
            }

            // Branch node
            HyperdimensionalMatrixVisuals.DrawNode(end, c * 0.6f, 3f);

            if (layerPct >= 1f)
            {
                DrawBranch(end, angle - BranchAngle, length * LengthRatio, depth + 1, visibleDepth, opacity, flashing);
                DrawBranch(end, angle + BranchAngle, length * LengthRatio, depth + 1, visibleDepth, opacity, flashing);
            }
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · VORONOI SHATTER
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 沃罗诺伊破碎模块：生成 12 个确定性种子点并构建三角化网格，
    /// 网格从外向内高亮收缩后，所有种子点同时向目标射出 MatrixGridCell。
    /// </summary>
    public sealed class MatrixVoronoiShatter : ModProjectile, ILocalizedModType
    {
        private const int BuildEnd    = 45;
        private const int ShatterEnd  = 90;
        private const int FireFrame   = 92;
        private const int Lifetime    = 120;

        private const int SeedCount = 12;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                SpawnCells();

            if (age == FireFrame && !Main.dedServ)
                SpawnShatterParticles();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.01f).ToVector3() * 0.25f);
        }

        private void SpawnShatterParticles()
        {
            Vector2 center = GetAnchor();
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.07f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center, vel, false, 10 + Main.rand.Next(12), 0.55f, c, true, false, i < 4));
            }
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 5f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.12f + 0.05f);
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    center, vel, false, 22, 1.2f + Main.rand.NextFloat(0.6f), c * 1.4f));
            }
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(center,
                HyperdimensionalMatrixVisuals.GetDataColor(0.5f), 0.8f);
        }

        private void SpawnCells()
        {
            NPC target = GetTarget();
            Vector2 aimPos = target?.Center ?? Projectile.Center;
            Vector2 center = GetAnchor();
            Vector2[] seeds = GetSeeds(center);

            for (int i = 0; i < SeedCount; i++)
            {
                Vector2 dir = (aimPos - seeds[i]).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    seeds[i],
                    dir * 22f,
                    ModContent.ProjectileType<MatrixGridCell>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    TargetIndex);
            }

            SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.36f, Pitch = 0.24f, MaxInstances = 3 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float fadeIn = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);
            float fadeOut = age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;
            float opacity = fadeIn * fadeOut;

            Vector2 center = GetAnchor();
            Vector2[] seeds = GetSeeds(center);
            var edges = GetTriangulationEdges(seeds);

            // Phase 2: shatter collapse factor
            float collapsePct = 0f;
            if (age > BuildEnd && age <= ShatterEnd)
                collapsePct = (age - BuildEnd) / (float)(ShatterEnd - BuildEnd);
            else if (age > ShatterEnd)
                collapsePct = 1f;

            // Phase 3: flash
            float flashPct = age >= ShatterEnd ? MathHelper.Clamp((age - ShatterEnd) / 8f, 0f, 1f) : 0f;

            // Draw triangulation edges
            for (int e = 0; e < edges.Count; e++)
            {
                var (a, b) = edges[e];
                float edgeDist = (Vector2.Distance(seeds[a], center) + Vector2.Distance(seeds[b], center)) * 0.5f;
                float maxDist = 65f;
                float normalizedDist = MathHelper.Clamp(edgeDist / maxDist, 0f, 1f);

                // Highlight from outermost to innermost during phase 2
                float highlightPct = MathHelper.Clamp(collapsePct * 1.5f - (1f - normalizedDist) * 0.8f, 0f, 1f);

                // Collapse toward center
                Vector2 pa = Vector2.Lerp(seeds[a], center, collapsePct * highlightPct * 0.6f);
                Vector2 pb = Vector2.Lerp(seeds[b], center, collapsePct * highlightPct * 0.6f);

                Color c = HyperdimensionalMatrixVisuals.GetDataColor(e / (float)edges.Count, opacity);
                float w = highlightPct > 0.3f ? 2.5f : 1.5f;
                if (flashPct > 0f) w += flashPct * 3f;
                Main.spriteBatch.DrawLineBetter(pa, pb, c, w);
            }

            // Draw seed nodes
            for (int i = 0; i < SeedCount; i++)
            {
                Vector2 seedPos = Vector2.Lerp(seeds[i], center, collapsePct * 0.6f);
                Color nc = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)SeedCount, opacity);
                float nodeSize = 4f + flashPct * 3f;
                HyperdimensionalMatrixVisuals.DrawNode(seedPos, nc, nodeSize);
            }

            // Scan ring
            if (fadeIn > 0.4f)
            {
                float t = Main.GlobalTimeWrappedHourly;
                HyperdimensionalMatrixVisuals.DrawScanRing(center, 70f - collapsePct * 30f, t * 1.5f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.3f, opacity * 0.4f), 18, 1.5f);
            }

            return false;
        }

        private Vector2[] GetSeeds(Vector2 center)
        {
            Vector2[] seeds = new Vector2[SeedCount];
            for (int i = 0; i < SeedCount; i++)
            {
                float angle = HashFloat(i * 73 + Projectile.identity * 37) * MathHelper.TwoPi;
                float dist = 18f + HashFloat(i * 131 + Projectile.identity * 59) * 47f;
                seeds[i] = center + angle.ToRotationVector2() * dist;
            }
            return seeds;
        }

        /// <summary>近似德劳内三角化：每个种子连接最近的 3 个邻居。</summary>
        private List<(int a, int b)> GetTriangulationEdges(Vector2[] seeds)
        {
            var edges = new HashSet<(int, int)>();
            for (int i = 0; i < seeds.Length; i++)
            {
                // Find 3 nearest neighbors
                var neighbors = new List<(int idx, float dist)>();
                for (int j = 0; j < seeds.Length; j++)
                {
                    if (j == i) continue;
                    neighbors.Add((j, Vector2.DistanceSquared(seeds[i], seeds[j])));
                }
                neighbors.Sort((a, b) => a.dist.CompareTo(b.dist));
                int count = Math.Min(3, neighbors.Count);
                for (int k = 0; k < count; k++)
                {
                    int a = Math.Min(i, neighbors[k].idx);
                    int b = Math.Max(i, neighbors[k].idx);
                    edges.Add((a, b));
                }
            }
            return new List<(int a, int b)>(edges);
        }

        private static float HashFloat(int v)
        {
            unchecked
            {
                v ^= v << 13;
                v ^= v >> 17;
                v ^= v << 5;
            }
            return (v & 0xFFFF) / (float)0xFFFF;
        }

        private Vector2 GetAnchor()
        {
            NPC target = GetTarget();
            return target?.Center ?? Projectile.Center;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · TORUS KNOT (CONTINUOUS DAMAGE)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 环面纽结束缚模块：在目标周围生成一个 (2,3) 环面纽结，
    /// 纽结逐渐收缩绞紧目标，造成持续伤害。
    /// 这是一个连续伤害弹幕，使用 localNPCImmunity 系统。
    /// </summary>
    public sealed class MatrixTorusKnot : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 95;

        private const int KnotP   = 2;
        private const int KnotQ   = 3;
        private const int KnotPts = 60;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            NPC target = GetTarget();
            if (target != null)
                Projectile.Center = target.Center;

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(Age * 0.012f).ToVector3() * 0.35f);
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetIndex ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Hit if target is within the knot's current radius
            float progress = MathHelper.Clamp(Age / (float)Lifetime, 0f, 1f);
            float currentR = MathHelper.Lerp(50f, 15f, progress) + 12f;
            Vector2 closest = new(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= currentR * currentR;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color burstColor = HyperdimensionalMatrixVisuals.GetDataColor(Main.GlobalTimeWrappedHourly * 0.5f);
            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.08f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 8 + Main.rand.Next(10),
                    0.5f + Main.rand.NextFloat(0.3f), c, true, false, i < 3));
            }
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, burstColor, 0.7f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float fade = age < 12 ? age / 12f : age > Lifetime - 12 ? (Lifetime - age) / 12f : 1f;
            float progress = MathHelper.Clamp(age / (float)Lifetime, 0f, 1f);

            // Radii shrink over time: knot tightens around target
            float bigR = MathHelper.Lerp(50f, 15f, progress);
            float smallR = MathHelper.Lerp(18f, 5f, progress);

            Vector2 center = Projectile.Center;
            Vector2[] pts = GetKnotPoints(center, bigR, smallR, time);

            // Draw torus knot as connected line segments
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)pts.Length, fade);
                Main.spriteBatch.DrawLineBetter(pts[i], pts[i + 1], c, 2f);
            }
            // Close the knot loop
            if (pts.Length > 1)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(0.95f, fade);
                Main.spriteBatch.DrawLineBetter(pts[pts.Length - 1], pts[0], c, 2f);
            }

            // Flowing data nodes
            for (int n = 0; n < 5; n++)
            {
                float flow = (time * 1.2f + n * 0.2f) % 1f;
                int segIdx = (int)(flow * (pts.Length - 1));
                float segFrac = flow * (pts.Length - 1) - segIdx;
                if (segIdx >= pts.Length - 1) { segIdx = pts.Length - 2; segFrac = 1f; }
                Vector2 nodePos = Vector2.Lerp(pts[segIdx], pts[segIdx + 1], segFrac);
                Color nc = HyperdimensionalMatrixVisuals.GetDataColor(flow, fade);
                HyperdimensionalMatrixVisuals.DrawNode(nodePos, nc, 4f);
            }

            // Vertex nodes at regular intervals
            for (int i = 0; i < pts.Length; i += 10)
            {
                Color vc = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)pts.Length, fade * 0.7f);
                HyperdimensionalMatrixVisuals.DrawNode(pts[i], vc, 3f);
            }

            // Scan rings
            HyperdimensionalMatrixVisuals.DrawScanRing(center, bigR * 1.2f, time * 1.5f,
                HyperdimensionalMatrixVisuals.GetDataColor(0.4f, fade * 0.35f), 22, 1.2f);
            HyperdimensionalMatrixVisuals.DrawScanRing(center, bigR * 0.6f, -time * 2f,
                HyperdimensionalMatrixVisuals.GetDataColor(0.8f, fade * 0.25f), 16, 1f);

            return false;
        }

        /// <summary>(2,3) 环面纽结参数方程，3D 旋转后透视投影到 2D。</summary>
        private Vector2[] GetKnotPoints(Vector2 center, float R, float r, float time)
        {
            Vector2[] pts = new Vector2[KnotPts];
            Matrix rot = Matrix.CreateFromYawPitchRoll(time * 0.8f, time * 0.55f, time * 0.3f);

            for (int i = 0; i < KnotPts; i++)
            {
                float t = MathHelper.TwoPi * i / KnotPts;
                float x = (R + r * MathF.Cos(KnotQ * t)) * MathF.Cos(KnotP * t);
                float y = (R + r * MathF.Cos(KnotQ * t)) * MathF.Sin(KnotP * t);
                float z = r * MathF.Sin(KnotQ * t);

                Vector3 pt3 = Vector3.Transform(new Vector3(x, y, z), rot);
                float perspective = 620f / MathF.Max(180f, 620f + pt3.Z);
                pts[i] = center + new Vector2(pt3.X * perspective, pt3.Y * perspective);
            }

            return pts;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · LORENZ SWARM (CONTINUOUS DAMAGE)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 洛伦兹蝶群模块：在目标周围生成三条洛伦兹吸引子轨迹（蝴蝶形），
    /// 利用初始条件的微小差异展现混沌分叉，造成持续伤害。
    /// </summary>
    public sealed class MatrixLorenzSwarm : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 115;

        // Lorenz parameters
        private const float Sigma = 10f;
        private const float Rho   = 28f;
        private const float Beta  = 8f / 3f;
        private const float Dt    = 0.008f;
        private const int   Steps = 250;
        private const float FitScale = 55f / 25f; // scale to ~55px radius

        private const int TrailCount = 3;
        private const int FlowNodesPerTrail = 3;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        // Cached trajectory data
        private Vector3[][] _trails;
        private bool _trailsBuilt;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            NPC target = GetTarget();
            if (target != null)
                Projectile.Center = target.Center;

            if (!_trailsBuilt)
            {
                _trails = BuildTrails();
                _trailsBuilt = true;
            }

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(Age * 0.01f).ToVector3() * 0.3f);
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetIndex ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Wider hit area — the whole butterfly region
            float r = 60f;
            Vector2 closest = new(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= r * r;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color burstColor = HyperdimensionalMatrixVisuals.GetDataColor(Main.GlobalTimeWrappedHourly * 0.45f);
            for (int i = 0; i < 15; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.065f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 8 + Main.rand.Next(12),
                    0.5f + Main.rand.NextFloat(0.4f), c, true, false, i < 4));
            }
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, burstColor, 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (_trails == null)
                return false;

            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float fade = age < 12 ? age / 12f : age > Lifetime - 12 ? (Lifetime - age) / 12f : 1f;

            Vector2 center = Projectile.Center;
            Matrix rot = Matrix.CreateFromYawPitchRoll(time * 0.3f, time * 0.2f, time * 0.15f);

            for (int trail = 0; trail < TrailCount; trail++)
            {
                Vector3[] trailData = _trails[trail];
                float colorOffset = trail * 0.33f;

                // Draw trajectory as connected line chain
                for (int i = 0; i < trailData.Length - 1; i++)
                {
                    Vector2 a = Project3D(trailData[i], center, rot);
                    Vector2 b = Project3D(trailData[i + 1], center, rot);
                    float pct = i / (float)trailData.Length;
                    Color c = HyperdimensionalMatrixVisuals.GetDataColor(pct + colorOffset, fade * 0.7f);
                    Main.spriteBatch.DrawLineBetter(a, b, c, 1.5f);
                }

                // Flowing data nodes along trajectory
                for (int n = 0; n < FlowNodesPerTrail; n++)
                {
                    float flow = (time * 0.6f + n / (float)FlowNodesPerTrail + trail * 0.11f) % 1f;
                    int idx = (int)(flow * (trailData.Length - 1));
                    if (idx >= trailData.Length - 1) idx = trailData.Length - 2;
                    float frac = flow * (trailData.Length - 1) - idx;
                    Vector3 interpPt = Vector3.Lerp(trailData[idx], trailData[idx + 1], frac);
                    Vector2 nodePos = Project3D(interpPt, center, rot);
                    Color nc = HyperdimensionalMatrixVisuals.GetDataColor(flow + colorOffset, fade);
                    HyperdimensionalMatrixVisuals.DrawNode(nodePos, nc, 4.5f);
                }
            }

            // Scan ring around the whole attractor
            HyperdimensionalMatrixVisuals.DrawScanRing(center, FitScale * 25f * 0.8f, time * 1f,
                HyperdimensionalMatrixVisuals.GetDataColor(0.55f, fade * 0.3f), 24, 1.2f);

            return false;
        }

        private Vector2 Project3D(Vector3 pt, Vector2 center, Matrix rotation)
        {
            Vector3 rotated = Vector3.Transform(pt, rotation);
            float perspective = 620f / MathF.Max(180f, 620f + rotated.Z);
            return center + new Vector2(rotated.X * perspective, rotated.Y * perspective);
        }

        /// <summary>构建 3 条洛伦兹吸引子轨迹，使用不同初始条件。</summary>
        private Vector3[][] BuildTrails()
        {
            Vector3[][] trails = new Vector3[TrailCount][];
            Vector3[] inits =
            {
                new(1f, 1f, 1f),
                new(1.01f, 1f, 1f),   // 微小差异 → 混沌分叉
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

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · FIBONACCI SPIRAL
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 斐波那契螺旋矛阵模块：以黄金角分布 18 个节点沿对数螺旋排列，
    /// 螺旋从中心向外展开后，所有节点同时向目标射出 MatrixGridCell。
    /// </summary>
    public sealed class MatrixFibonacciSpiral : ModProjectile, ILocalizedModType
    {
        private const int BuildEnd  = 55;
        private const int FlashEnd  = 62;
        private const int FireFrame = 63;
        private const int Lifetime  = 90;

        private const int   NodeCount   = 18;
        private const float GoldenAngle = 2.399963f; // 137.508° in radians
        private const float SpiralA     = 8f;
        private const float SpiralB     = 0.12f;
        private const float MaxRadius   = 65f;
        private const int   SpiralSegs  = 60;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                SpawnCells();

            if (age == FireFrame && !Main.dedServ)
                SpawnSpiralParticles();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.012f).ToVector3() * 0.3f);
        }

        private void SpawnSpiralParticles()
        {
            Vector2 center = GetAnchor();
            float rotation = Main.GlobalTimeWrappedHourly * 0.4f;

            for (int i = 0; i < NodeCount; i++)
            {
                Vector2 nodePos = GetNodePosition(center, i, rotation, 1f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)NodeCount);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    nodePos, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f),
                    false, 8 + Main.rand.Next(8), 0.5f, c, true, false, i < 5));
            }
            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.16f + 0.1f);
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    center, vel, false, 20, 1f + Main.rand.NextFloat(0.5f), c * 1.3f));
            }
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(center,
                HyperdimensionalMatrixVisuals.GetDataColor(0.5f), 0.7f);
        }

        private void SpawnCells()
        {
            NPC target = GetTarget();
            Vector2 aimPos = target?.Center ?? Projectile.Center;
            Vector2 center = GetAnchor();
            float rotation = Main.GlobalTimeWrappedHourly * 0.4f;

            for (int i = 0; i < NodeCount; i++)
            {
                Vector2 nodePos = GetNodePosition(center, i, rotation, 1f);
                Vector2 dir = (aimPos - nodePos).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    nodePos,
                    dir * 22f,
                    ModContent.ProjectileType<MatrixGridCell>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    TargetIndex);
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.36f, Pitch = 0.15f, MaxInstances = 4 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);
            float fadeOut = age > Lifetime - 12 ? (Lifetime - age) / 12f : 1f;
            float opacity = buildPct * fadeOut;
            bool flashing = age >= BuildEnd && age < FlashEnd + 5;

            float rotation = time * 0.4f;
            Vector2 center = GetAnchor();

            // Draw continuous spiral curve (unwinds from center outward)
            float maxTheta = GetMaxSpiralTheta();
            float visibleTheta = maxTheta * buildPct;
            Vector2 prevPt = center;
            for (int s = 1; s <= SpiralSegs; s++)
            {
                float theta = visibleTheta * s / SpiralSegs;
                float r = SpiralA * MathF.Exp(SpiralB * theta);
                if (r > MaxRadius) r = MaxRadius;
                Vector2 pt = center + (theta + rotation).ToRotationVector2() * r;

                Color c = HyperdimensionalMatrixVisuals.GetDataColor(s / (float)SpiralSegs, opacity * 0.6f);
                Main.spriteBatch.DrawLineBetter(prevPt, pt, c, 1.2f);
                prevPt = pt;
            }

            // Draw nodes at golden-angle positions (light up sequentially)
            int visibleNodes = (int)(NodeCount * buildPct);
            for (int i = 0; i < visibleNodes; i++)
            {
                Vector2 nodePos = GetNodePosition(center, i, rotation, 1f);

                // Faint line to center
                Color lineColor = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)NodeCount, opacity * 0.25f);
                Main.spriteBatch.DrawLineBetter(center, nodePos, lineColor, 0.8f);

                // Pulsing node
                float pulse = 3.5f + 1.5f * MathF.Sin(time * 6f + i * 0.7f);
                Color nodeColor = flashing
                    ? HyperdimensionalMatrixVisuals.GetDataColor(i / (float)NodeCount, opacity) * 1.5f
                    : HyperdimensionalMatrixVisuals.GetDataColor(i / (float)NodeCount, opacity * 0.85f);
                HyperdimensionalMatrixVisuals.DrawNode(nodePos, nodeColor, pulse);
            }

            // Center node
            HyperdimensionalMatrixVisuals.DrawNode(center, HyperdimensionalMatrixVisuals.GetDataColor(0.5f, opacity), 5f);

            // Scan ring
            if (buildPct > 0.3f)
            {
                HyperdimensionalMatrixVisuals.DrawScanRing(center, MaxRadius * 1.1f, time * 1.5f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.6f, opacity * 0.35f), 20, 1.2f);
            }

            return false;
        }

        private Vector2 GetNodePosition(Vector2 center, int index, float rotation, float scale)
        {
            float angle = index * GoldenAngle;
            float r = SpiralA * MathF.Exp(SpiralB * angle);
            if (r > MaxRadius) r = MaxRadius;
            return center + (angle + rotation).ToRotationVector2() * (r * scale);
        }

        private float GetMaxSpiralTheta()
        {
            // Find the theta where r reaches MaxRadius
            // r = a * exp(b * theta) => theta = ln(r/a) / b
            return MathF.Log(MaxRadius / SpiralA) / SpiralB;
        }

        private Vector2 GetAnchor()
        {
            NPC target = GetTarget();
            return target?.Center ?? Projectile.Center;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · PENROSE TILING COLLAPSE
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 彭罗斯镶嵌坍缩模块：构建一个准晶体彭罗斯菱形镶嵌图案，
    /// 菱形从中心向外展开再从外向内坍缩，最终在中心引爆一次强力
    /// MatrixFusionExplosion。这是稀有的高伤害攻击模块。
    /// </summary>
    public sealed class MatrixPenroseTiling : ModProjectile, ILocalizedModType
    {
        private const int BuildEnd    = 75;
        private const int CollapseEnd = 115;
        private const int FireFrame   = 117;
        private const int Lifetime    = 140;

        private static readonly float Phi = (1f + MathF.Sqrt(5f)) * 0.5f;

        // Thin rhombus: 36° / 144°, Thick rhombus: 72° / 108°
        private const float ThinAngle  = MathHelper.Pi / 5f;       // 36°
        private const float ThickAngle = MathHelper.Pi * 2f / 5f;  // 72°
        private const float RhombusSize = 11f;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                SpawnExplosion();

            if (age == FireFrame && !Main.dedServ)
                SpawnCollapseParticles();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.008f).ToVector3() * 0.3f);
        }

        private void SpawnCollapseParticles()
        {
            Vector2 center = GetAnchor();

            for (int i = 0; i < 24; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.04f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center, vel, false, 14 + Main.rand.Next(16),
                    0.6f + Main.rand.NextFloat(0.5f), c, true, false, i < 6));
            }
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.07f + 0.05f);
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    center, vel, false, 28, 1.4f + Main.rand.NextFloat(1f), c * 1.5f));
            }

            Color burstColor = HyperdimensionalMatrixVisuals.GetDataColor(Main.GlobalTimeWrappedHourly * 0.5f);
            CLCBLightingBoltsSystem.Spawn_MatrixSingularityCollapse(center);
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(center, burstColor, 1.2f);
            CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(center, burstColor);

            // Screen shake
            if (Main.LocalPlayer.active)
            {
                float _sd = Vector2.Distance(Main.LocalPlayer.Center, center);
                if (_sd < 700f)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower, 2.25f * (1f - _sd / 700f));
            }
        }

        private void SpawnExplosion()
        {
            Vector2 center = GetAnchor();
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<MatrixFusionExplosion>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.42f, Pitch = -0.1f, MaxInstances = 3 }, center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float fadeIn = MathHelper.Clamp(age / 20f, 0f, 1f);
            float fadeOut = age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;
            float opacity = fadeIn * fadeOut;

            // Build progress: rhombuses appear from center outward
            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);

            // Collapse progress: rhombuses highlight from outside-in and move toward center
            float collapsePct = 0f;
            if (age > BuildEnd && age <= CollapseEnd)
                collapsePct = (age - BuildEnd) / (float)(CollapseEnd - BuildEnd);
            else if (age > CollapseEnd)
                collapsePct = 1f;

            // Flash at collapse end
            float flashPct = age >= CollapseEnd ? MathHelper.Clamp((age - CollapseEnd) / 8f, 0f, 1f) : 0f;

            Vector2 center = GetAnchor();
            Vector2[] rhombusCenters = GetRhombusCenters(center);

            // Sort by distance from center for animation ordering
            float maxDist = 0f;
            float[] distances = new float[rhombusCenters.Length];
            for (int i = 0; i < rhombusCenters.Length; i++)
            {
                distances[i] = Vector2.Distance(rhombusCenters[i], center);
                if (distances[i] > maxDist) maxDist = distances[i];
            }
            if (maxDist < 1f) maxDist = 1f;

            for (int i = 0; i < rhombusCenters.Length; i++)
            {
                float normalizedDist = distances[i] / maxDist;

                // Phase 1: appear from center outward
                float appearThreshold = normalizedDist;
                if (buildPct < appearThreshold)
                    continue;

                // Phase 2: collapse toward center (highlight from outside-in)
                float highlightPct = MathHelper.Clamp(collapsePct * 1.5f - (1f - normalizedDist) * 0.7f, 0f, 1f);
                Vector2 rhombCenter = Vector2.Lerp(rhombusCenters[i], center, collapsePct * highlightPct * 0.7f);

                // Alternate thin/thick rhombus
                bool isThin = (i % 2 == 0);
                float halfAngle = isThin ? ThinAngle : ThickAngle;

                // Determine rotation based on position and time
                float baseRot = MathF.Atan2(rhombusCenters[i].Y - center.Y, rhombusCenters[i].X - center.X);
                float rot = baseRot + time * 0.15f;

                // Draw rhombus: 4 vertices
                Vector2 dirA = rot.ToRotationVector2();
                Vector2 dirB = (rot + halfAngle).ToRotationVector2();
                Vector2 dirC = (rot + MathHelper.Pi).ToRotationVector2();
                Vector2 dirD = (rot + MathHelper.Pi + halfAngle).ToRotationVector2();

                Vector2 v0 = rhombCenter + dirA * RhombusSize;
                Vector2 v1 = rhombCenter + dirB * RhombusSize;
                Vector2 v2 = rhombCenter + dirC * RhombusSize;
                Vector2 v3 = rhombCenter + dirD * RhombusSize;

                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)rhombusCenters.Length, opacity);
                float lineW = highlightPct > 0.3f ? 2.2f : 1.4f;
                if (flashPct > 0f) lineW += flashPct * 2.5f;

                Main.spriteBatch.DrawLineBetter(v0, v1, c, lineW);
                Main.spriteBatch.DrawLineBetter(v1, v2, c, lineW);
                Main.spriteBatch.DrawLineBetter(v2, v3, c, lineW);
                Main.spriteBatch.DrawLineBetter(v3, v0, c, lineW);

                // Node at center of each rhombus
                if (highlightPct > 0.5f)
                    HyperdimensionalMatrixVisuals.DrawNode(rhombCenter, c, 3f + flashPct * 2f);
            }

            // Outer scan ring
            if (buildPct > 0.4f)
            {
                float scanR = maxDist + RhombusSize - collapsePct * maxDist * 0.5f;
                HyperdimensionalMatrixVisuals.DrawScanRing(center, scanR, time * 1.2f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.45f, opacity * 0.35f), 24, 1.5f);
            }

            return false;
        }

        /// <summary>生成彭罗斯式菱形中心点，使用五重旋转对称和黄金比例间距。</summary>
        private Vector2[] GetRhombusCenters(Vector2 center)
        {
            var centers = new List<Vector2>();
            float baseSpacing = 22f;

            // Central ring: 5 rhombuses
            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f;
                centers.Add(center + angle.ToRotationVector2() * baseSpacing);
            }
            // Second ring (rotated by π/5): 5 rhombuses
            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f + MathHelper.Pi / 5f;
                centers.Add(center + angle.ToRotationVector2() * baseSpacing * Phi);
            }
            // Third ring: 10 rhombuses
            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 10f;
                centers.Add(center + angle.ToRotationVector2() * baseSpacing * Phi * Phi);
            }
            // Outermost ring: 10 rhombuses
            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 10f + MathHelper.Pi / 10f;
                centers.Add(center + angle.ToRotationVector2() * baseSpacing * Phi * Phi * Phi * 0.65f);
            }

            return centers.ToArray();
        }

        private Vector2 GetAnchor()
        {
            NPC target = GetTarget();
            return target?.Center ?? Projectile.Center;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · HYPERBOLIC PARABOLOID WARP
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 双曲抛物马鞍面模块：在目标位置投影一个由发光数据网格线交织而成的马鞍面，
    /// 网格自转并在蓄力完成时，从马鞍边缘的 8 个顶点朝目标发射 MatrixGridCell。
    /// </summary>
    public sealed class MatrixParaboloidWarp : ModProjectile, ILocalizedModType
    {
        private const int BuildEnd  = 55;
        private const int FlashEnd  = 65;
        private const int FireFrame = 66;
        private const int Lifetime  = 95;

        private const int GridSize = 5; // 5x5 grid

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                SpawnCells();

            if (age == FireFrame && !Main.dedServ)
                SpawnParticles();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.012f).ToVector3() * 0.28f);
        }

        private void SpawnParticles()
        {
            Vector2 center = GetAnchor();
            for (int i = 0; i < 8; i++)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / 8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3f),
                    false, 8 + Main.rand.Next(6), 0.5f, c, true, false, false));
            }
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(center, HyperdimensionalMatrixVisuals.GetDataColor(0.2f), 0.6f);
        }

        private void SpawnCells()
        {
            NPC target = GetTarget();
            Vector2 aimPos = target?.Center ?? Projectile.Center;
            Vector2 center = GetAnchor();
            float time = Main.GlobalTimeWrappedHourly;
            Vector2[] borderPts = GetBorderPoints(center, time);

            for (int i = 0; i < borderPts.Length; i++)
            {
                Vector2 dir = (aimPos - borderPts[i]).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    borderPts[i],
                    dir * 22f,
                    ModContent.ProjectileType<MatrixGridCell>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    TargetIndex);
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.36f, Pitch = 0.15f, MaxInstances = 4 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);
            float fadeOut = age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;
            float opacity = buildPct * fadeOut;

            Vector2 center = GetAnchor();
            Vector3[,] grid = GetParaboloidGrid(time);

            Matrix rot = Matrix.CreateFromYawPitchRoll(time * 0.6f, time * 0.45f, time * 0.2f);

            // Project grid to 2D
            Vector2[,] projGrid = new Vector2[GridSize, GridSize];
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    Vector3 rotated = Vector3.Transform(grid[x, y], rot);
                    float perspective = 620f / MathF.Max(180f, 620f + rotated.Z);
                    projGrid[x, y] = center + new Vector2(rotated.X * perspective, rotated.Y * perspective);
                }
            }

            // Draw grid lines
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize - 1; y++)
                {
                    float factor = (x * GridSize + y) / (float)(GridSize * GridSize);
                    Color c = HyperdimensionalMatrixVisuals.GetDataColor(factor, opacity);
                    Main.spriteBatch.DrawLineBetter(projGrid[x, y], projGrid[x, y + 1], c, 1.3f);
                }
            }
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize - 1; x++)
                {
                    float factor = (x * GridSize + y) / (float)(GridSize * GridSize);
                    Color c = HyperdimensionalMatrixVisuals.GetDataColor(factor + 0.5f, opacity);
                    Main.spriteBatch.DrawLineBetter(projGrid[x, y], projGrid[x + 1, y], c, 1.3f);
                }
            }

            // Draw border nodes
            Vector2[] borders = GetBorderPoints(center, time);
            for (int i = 0; i < borders.Length; i++)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)borders.Length, opacity);
                float pulse = 3.5f + 1.5f * MathF.Sin(time * 8f + i);
                HyperdimensionalMatrixVisuals.DrawNode(borders[i], c, pulse);
            }

            return false;
        }

        private Vector3[,] GetParaboloidGrid(float time)
        {
            Vector3[,] grid = new Vector3[GridSize, GridSize];
            float scale = 14f;
            float zScale = 4.2f;

            for (int x = 0; x < GridSize; x++)
            {
                float u = x - (GridSize - 1) * 0.5f; // -2 to 2
                for (int y = 0; y < GridSize; y++)
                {
                    float v = y - (GridSize - 1) * 0.5f; // -2 to 2
                    // Saddle equation: z = u^2 - v^2
                    float z = (u * u - v * v) * zScale;
                    grid[x, y] = new Vector3(u * scale, v * scale, z);
                }
            }
            return grid;
        }

        private Vector2[] GetBorderPoints(Vector2 center, float time)
        {
            Vector3[,] grid = GetParaboloidGrid(time);
            Matrix rot = Matrix.CreateFromYawPitchRoll(time * 0.6f, time * 0.45f, time * 0.2f);
            
            // Get 8 boundary grid points
            Vector3[] borders3D = new Vector3[8];
            borders3D[0] = grid[0, 0];
            borders3D[1] = grid[0, GridSize / 2];
            borders3D[2] = grid[0, GridSize - 1];
            borders3D[3] = grid[GridSize / 2, 0];
            borders3D[4] = grid[GridSize / 2, GridSize - 1];
            borders3D[5] = grid[GridSize - 1, 0];
            borders3D[6] = grid[GridSize - 1, GridSize / 2];
            borders3D[7] = grid[GridSize - 1, GridSize - 1];

            Vector2[] result = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 rotated = Vector3.Transform(borders3D[i], rot);
                float perspective = 620f / MathF.Max(180f, 620f + rotated.Z);
                result[i] = center + new Vector2(rotated.X * perspective, rotated.Y * perspective);
            }
            return result;
        }

        private Vector2 GetAnchor()
        {
            NPC target = GetTarget();
            return target?.Center ?? Projectile.Center;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · CLIFFORD TORUS HOPF FIBRATION
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 克利福德环面霍普夫纤维化模块：在目标周围投影克利福德环面的 4D 投影圆环，
    /// 三个圆环交织并倾斜，周期性脱离发光数据结点作为 MatrixGridCell 发射。
    /// </summary>
    public sealed class MatrixCliffordTorus : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 110;
        private const int KnotPts = 48;
        private const int RingCount = 3;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (Main.myPlayer == Projectile.owner && (age == 50 || age == 100))
                FireRings();

            NPC target = GetTarget();
            if (target != null)
                Projectile.Center = target.Center;

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.01f).ToVector3() * 0.32f);
        }

        private void FireRings()
        {
            NPC target = GetTarget();
            Vector2 aimPos = target?.Center ?? Projectile.Center;
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 center = Projectile.Center;

            for (int r = 0; r < RingCount; r++)
            {
                float flow = (time * 1.5f + r * 0.33f) % 1f;
                Vector2 nodePos = GetRingPoint(center, r, flow, time);
                Vector2 dir = (aimPos - nodePos).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    nodePos,
                    dir * 22f,
                    ModContent.ProjectileType<MatrixGridCell>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    TargetIndex);
            }

            SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.3f, Pitch = 0.4f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float fade = age < 15 ? age / 15f : age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;

            Vector2 center = Projectile.Center;

            for (int r = 0; r < RingCount; r++)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(r / (float)RingCount, fade * 0.7f);

                // Draw circles
                Vector2 prevPt = GetRingPoint(center, r, 0f, time);
                for (int i = 1; i <= KnotPts; i++)
                {
                    float phi = MathHelper.TwoPi * i / KnotPts;
                    Vector2 pt = GetRingPoint(center, r, phi, time);
                    Main.spriteBatch.DrawLineBetter(prevPt, pt, c, 1.6f);
                    prevPt = pt;
                }

                // Draw flowing node
                float flow = (time * 1.5f + r * 0.33f) % MathHelper.TwoPi;
                Vector2 flowPos = GetRingPoint(center, r, flow, time);
                Color nc = HyperdimensionalMatrixVisuals.GetDataColor(r / (float)RingCount + 0.5f, fade);
                HyperdimensionalMatrixVisuals.DrawNode(flowPos, nc, 5f);
            }

            return false;
        }

        private Vector2 GetRingPoint(Vector2 center, int ringIdx, float phi, float time)
        {
            // Theta defines the Clifford torus coordinate
            float theta = ringIdx * (MathHelper.TwoPi / RingCount) + time * 0.25f;
            
            // Hopf circular coordinates on S^3
            float X = MathF.Cos(phi) * MathF.Cos(theta);
            float Y = MathF.Sin(phi) * MathF.Cos(theta);
            float Z = MathF.Cos(phi) * MathF.Sin(theta);
            float W = MathF.Sin(phi) * MathF.Sin(theta);

            // Stereographic projection from 4D to 3D
            float scale3D = 45f;
            float wOffset = 1.4f; // avoids division by zero and stretches Hopf circles
            float denom = wOffset - W;
            Vector3 pt3D = new Vector3(X, Y, Z) * (scale3D / denom);

            // Apply 3D rotation
            Matrix rot = Matrix.CreateFromYawPitchRoll(time * 0.4f, time * 0.3f, time * 0.2f);
            Vector3 rotated = Vector3.Transform(pt3D, rot);
            float perspective = 620f / MathF.Max(180f, 620f + rotated.Z);

            return center + new Vector2(rotated.X * perspective, rotated.Y * perspective);
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · GIELIS SUPERFORMULA MORPH
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 超配方形态渐变模块：以 Gielis 超配方公式进行极坐标实时形态过渡，
    /// 角对称数在 3, 5, 8 之间平滑渐变，蓄力时向核心边缘发射 MatrixGeoShard。
    /// </summary>
    public sealed class MatrixSuperformulaMorph : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 120;
        private const int LinePts = 72;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (Main.myPlayer == Projectile.owner && age % 22 == 0)
                FireShards();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.012f).ToVector3() * 0.28f);
        }

        private void FireShards()
        {
            NPC target = GetTarget();
            if (target == null) return;
            Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                dir * 18f,
                ModContent.ProjectileType<MatrixGeoShard>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                TargetIndex);
            
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.28f, Pitch = 0.35f, MaxInstances = 3 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float fade = age < 15 ? age / 15f : age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;

            Vector2 center = Projectile.Center;

            // Oscillate symmetry m between 3 and 8
            float m = 5f + 3f * MathF.Sin(time * 3f);
            float a = 1f, b = 1f;
            float n1 = 1f, n2 = 1.2f, n3 = 1.2f;

            // Draw Gielis Superformula loop
            Vector2 prevPt = GetSuperformulaPoint(center, 0f, m, a, b, n1, n2, n3, time);
            for (int i = 1; i <= LinePts; i++)
            {
                float theta = MathHelper.TwoPi * i / LinePts;
                Vector2 pt = GetSuperformulaPoint(center, theta, m, a, b, n1, n2, n3, time);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)LinePts + time * 0.2f, fade);
                Main.spriteBatch.DrawLineBetter(prevPt, pt, c, 1.6f);
                prevPt = pt;
            }

            // Draw some peak nodes
            for (int k = 0; k < 5; k++)
            {
                float peakTheta = MathHelper.TwoPi * k / 5f + time * 0.4f;
                Vector2 peakPos = GetSuperformulaPoint(center, peakTheta, m, a, b, n1, n2, n3, time);
                Color nc = HyperdimensionalMatrixVisuals.GetDataColor(k / 5f, fade);
                HyperdimensionalMatrixVisuals.DrawNode(peakPos, nc, 4.5f);
            }

            return false;
        }

        private Vector2 GetSuperformulaPoint(Vector2 center, float theta, float m, float a, float b, float n1, float n2, float n3, float time)
        {
            // Gielis Superformula math
            float term1 = MathF.Pow(MathF.Abs(MathF.Cos(m * theta / 4f) / a), n2);
            float term2 = MathF.Pow(MathF.Abs(MathF.Sin(m * theta / 4f) / b), n3);
            float r = MathF.Pow(term1 + term2, -1f / n1);

            // Scale and rotate
            float baseR = 34f + 8f * MathF.Sin(time * 4f);
            r *= baseR;

            float rotAngle = theta + time * 0.5f;
            return center + rotAngle.ToRotationVector2() * r;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE · SIERPINSKI GASKET COLLAPSE
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 谢尔宾斯基三角网格塌缩模块：投影一个递归 3 层的分形等边三角形，
    /// 在蓄力过程中细分小三角形开始向中心收缩，最终在中心引爆 MatrixFusionExplosion。
    /// </summary>
    public sealed class MatrixSierpinskiCollapse : ModProjectile, ILocalizedModType
    {
        private const int BuildEnd    = 60;
        private const int CollapseEnd = 100;
        private const int FireFrame   = 102;
        private const int Lifetime    = 125;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                SpawnExplosion();

            if (age == FireFrame && !Main.dedServ)
                SpawnParticles();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.01f).ToVector3() * 0.35f);
        }

        private void SpawnParticles()
        {
            Vector2 center = GetAnchor();
            for (int i = 0; i < 18; i++)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.05f);
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center, vel, false, 12 + Main.rand.Next(12), 0.6f, c, true, false, i < 4));
            }
            CLCBLightingBoltsSystem.Spawn_MatrixSingularityCollapse(center);
        }

        private void SpawnExplosion()
        {
            Vector2 center = GetAnchor();
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<MatrixFusionExplosion>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.4f, Pitch = -0.1f }, center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float fadeIn = MathHelper.Clamp(age / 15f, 0f, 1f);
            float fadeOut = age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;
            float opacity = fadeIn * fadeOut;

            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);

            float collapsePct = 0f;
            if (age > BuildEnd && age <= CollapseEnd)
                collapsePct = (age - BuildEnd) / (float)(CollapseEnd - BuildEnd);
            else if (age > CollapseEnd)
                collapsePct = 1f;

            float flashPct = age >= CollapseEnd ? MathHelper.Clamp((age - CollapseEnd) / 8f, 0f, 1f) : 0f;

            Vector2 center = GetAnchor();

            // Equalateral main triangle points rotated
            float rot = time * 0.5f;
            float radius = 52f;
            Vector2 a = center + rot.ToRotationVector2() * radius;
            Vector2 b = center + (rot + MathHelper.TwoPi / 3f).ToRotationVector2() * radius;
            Vector2 c = center + (rot + MathHelper.TwoPi * 2f / 3f).ToRotationVector2() * radius;

            // Draw recursive Sierpinski triangle
            DrawSierpinski(a, b, c, 3, opacity, collapsePct, flashPct, center);

            return false;
        }

        private void DrawSierpinski(Vector2 a, Vector2 b, Vector2 c, int depth, float opacity, float collapsePct, float flashPct, Vector2 center)
        {
            // Collapse moves coordinates closer to center
            Vector2 finalA = Vector2.Lerp(a, center, collapsePct * 0.7f);
            Vector2 finalB = Vector2.Lerp(b, center, collapsePct * 0.7f);
            Vector2 finalC = Vector2.Lerp(c, center, collapsePct * 0.7f);

            if (depth <= 0)
            {
                Color tint = HyperdimensionalMatrixVisuals.GetDataColor(depth * 0.25f, opacity);
                float lineW = 1.3f + flashPct * 2f;
                Main.spriteBatch.DrawLineBetter(finalA, finalB, tint, lineW);
                Main.spriteBatch.DrawLineBetter(finalB, finalC, tint, lineW);
                Main.spriteBatch.DrawLineBetter(finalC, finalA, tint, lineW);

                // Peak node
                HyperdimensionalMatrixVisuals.DrawNode(finalA, tint, 3f);
                return;
            }

            Vector2 ab = (a + b) * 0.5f;
            Vector2 bc = (b + c) * 0.5f;
            Vector2 ca = (c + a) * 0.5f;

            DrawSierpinski(a, ab, ca, depth - 1, opacity, collapsePct, flashPct, center);
            DrawSierpinski(ab, b, bc, depth - 1, opacity, collapsePct, flashPct, center);
            DrawSierpinski(ca, bc, c, depth - 1, opacity, collapsePct, flashPct, center);
        }

        private Vector2 GetAnchor()
        {
            NPC target = GetTarget();
            return target?.Center ?? Projectile.Center;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }
}
