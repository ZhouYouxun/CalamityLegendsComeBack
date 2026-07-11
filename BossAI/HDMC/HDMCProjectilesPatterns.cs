using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Systems;
using CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    // ──────────────────────────────────────────────────────
    // 数据矩阵面板（敌对）：格子点亮 → 齐射数据矛
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对数据面板：在固定位置展开 5×6 数据网格，逐格点亮后所有亮格
    /// 朝"开火瞬间最近玩家的位置"齐射数据矛（射出后不再追踪）。
    /// </summary>
    public sealed class HDMCGridPanelHostile : ModProjectile
    {
        private const int Cols = 5;
        private const int Rows = 6;
        private const int CellCount = Cols * Rows;
        private const float CellSize = 20f;
        private const int FadeInEnd = 28;
        private const int FireFrame = 96;
        private const int Lifetime  = 126;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
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
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;
            if (age == FireFrame && Main.netMode != NetmodeID.MultiplayerClient)
                FireCells();
            if (age == FireFrame && !Main.dedServ)
            {
                HDMCUtil.DataBurstParticles(Projectile.Center, 10, 5, 5f);
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, HDMCUtil.DataColor(0.5f), 0.6f);
                SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.3f, Pitch = 0.4f, MaxInstances = 5 }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(age * 0.012f).ToVector3() * 0.3f);
        }

        private void FireCells()
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 aimPos = target.Center;
            Vector2 origin = GetGridOrigin();
            int damage = Projectile.damage;

            for (int i = 0; i < CellCount; i++)
            {
                if (!CellIsLit(i))
                    continue;

                Vector2 cellWorld = origin + GetCellLocalPos(i) + new Vector2(CellSize * 0.5f);
                Vector2 dir = (aimPos - cellWorld).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), cellWorld, dir * 2.5f,
                    ModContent.ProjectileType<HDMCLanceHostile>(),
                    damage, 0f, Main.myPlayer, 17f, 10f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float opacity = HDMCUtil.FadeInOut(age, Lifetime, FadeInEnd);
            Vector2 origin = GetGridOrigin();

            for (int i = 0; i < CellCount; i++)
            {
                Vector2 cellWorld = origin + GetCellLocalPos(i);
                bool lit = CellIsLit(i);
                Color c = lit
                    ? HDMCUtil.DataColor(i / (float)CellCount, opacity)
                    : HDMCUtil.DataColor(i / (float)CellCount, opacity * 0.2f);

                DrawCell(cellWorld, CellSize, c, lit ? 1.9f : 1f);
                if (lit)
                    HyperdimensionalMatrixVisuals.DrawNode(cellWorld + Vector2.One * (CellSize * 0.5f), c, 4f);
            }

            Color borderColor = HDMCUtil.DataColor(0.25f, opacity * 0.55f);
            float w = Cols * CellSize;
            float h = Rows * CellSize;
            Main.spriteBatch.DrawLineBetter(origin, origin + new Vector2(w, 0f), borderColor, 1.6f);
            Main.spriteBatch.DrawLineBetter(origin + new Vector2(w, 0f), origin + new Vector2(w, h), borderColor, 1.6f);
            Main.spriteBatch.DrawLineBetter(origin + new Vector2(w, h), origin + new Vector2(0f, h), borderColor, 1.6f);
            Main.spriteBatch.DrawLineBetter(origin + new Vector2(0f, h), origin, borderColor, 1.6f);

            return false;
        }

        private static void DrawCell(Vector2 worldOrigin, float size, Color color, float width)
        {
            Vector2 tl = worldOrigin;
            Vector2 tr = worldOrigin + new Vector2(size, 0f);
            Vector2 br = worldOrigin + new Vector2(size, size);
            Vector2 bl = worldOrigin + new Vector2(0f, size);
            Main.spriteBatch.DrawLineBetter(tl, tr, color, width);
            Main.spriteBatch.DrawLineBetter(tr, br, color, width);
            Main.spriteBatch.DrawLineBetter(br, bl, color, width);
            Main.spriteBatch.DrawLineBetter(bl, tl, color, width);
        }

        private Vector2 GetGridOrigin()
            => Projectile.Center + new Vector2(-(Cols * CellSize) * 0.5f, -(Rows * CellSize) * 0.5f);

        private static Vector2 GetCellLocalPos(int index)
            => new((index % Cols) * CellSize, (index / Cols) * CellSize);

        private bool CellIsLit(int index)
        {
            int age = Age;
            if (age < FadeInEnd)
                return false;

            int lightOrder = (index * 17 + Projectile.identity * 7) % CellCount;
            float litByAge = (age - FadeInEnd) / 2.2f;
            return lightOrder < litByAge;
        }
    }

    // ──────────────────────────────────────────────────────
    // 几何爆裂体（敌对）：多面体构筑 → 顶点方向爆射碎片
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对几何爆裂体：在固定位置构筑旋转多面体（形状由 identity 决定），
    /// 展开后向各顶点方向爆射碎片，另有 3 枚瞄准最近玩家开火瞬间的位置。
    /// ai[1] = 建造时长偏移（错峰引爆用）。
    /// </summary>
    public sealed class HDMCGeoBurstHostile : ModProjectile
    {
        private const int BaseBuildEnd = 70;
        private const int Lifetime = 100;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int BuildEnd => BaseBuildEnd + (int)Projectile.ai[1];
        private int Age => Lifetime - Projectile.timeLeft;
        private MatrixGeometryShape Shape => (Projectile.identity % 3) switch
        {
            0 => MatrixGeometryShape.Tetrahedron,
            1 => MatrixGeometryShape.Icosahedron,
            _ => MatrixGeometryShape.Cube
        };

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
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
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;
            if (age == BuildEnd && Main.netMode != NetmodeID.MultiplayerClient)
                FireShards();
            if (age == BuildEnd && !Main.dedServ)
            {
                HDMCUtil.DataBurstParticles(Projectile.Center, 16, 8, 8f);
                Color burstColor = HDMCUtil.DataColor(Main.GlobalTimeWrappedHourly * 0.55f);
                CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(Projectile.Center, burstColor);
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, burstColor, 0.7f);
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.36f, Pitch = 0.15f, MaxInstances = 4 }, Projectile.Center);
                HDMCUtil.ScreenShake(Projectile.Center, 1.5f, 600f);
            }

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(age * 0.01f).ToVector3() * 0.35f);
        }

        private void FireShards()
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2[] vertices = HyperdimensionalMatrixVisuals.GetProjectedVertices(
                Shape, Projectile.Center, 66f, time, Projectile.identity);
            int damage = Projectile.damage;

            foreach (Vector2 v in vertices)
            {
                Vector2 dir = (v - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + dir * 10f, dir * 13.5f,
                    ModContent.ProjectileType<HDMCShardHostile>(),
                    damage, 0f, Main.myPlayer);
            }

            // 3 枚瞄准玩家当前位置（射出即固定方向）
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 aim = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            for (int i = -1; i <= 1; i++)
            {
                Vector2 dir = aim.RotatedBy(i * MathHelper.ToRadians(9f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + dir * 10f, dir * 15f,
                    ModContent.ProjectileType<HDMCShardHostile>(),
                    damage, 0f, Main.myPlayer);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);
            float explodePct = age >= BuildEnd ? (age - BuildEnd) / (float)(Lifetime - BuildEnd) : 0f;
            float opacity = buildPct * (1f - explodePct);

            float radius = age < BuildEnd
                ? 66f * buildPct
                : 66f + explodePct * 90f;

            HyperdimensionalMatrixVisuals.DrawGeometry(
                Projectile.Center, Shape, radius, time * 1.4f, opacity, Projectile.identity);

            if (age > BuildEnd * 0.7f && age < BuildEnd)
            {
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 12f);
                HyperdimensionalMatrixVisuals.DrawScanRing(
                    Projectile.Center, radius * 1.25f, time * 2f,
                    HDMCUtil.DataColor(0.85f, opacity * pulse), 16, 2.5f);
            }

            return false;
        }
    }

    // ──────────────────────────────────────────────────────
    // 七芒星封印（敌对）：定点描绘 → 七道汇聚激光
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对七芒星封印：在生成时的固定位置逐笔描绘 {7/2} 七芒星阵，
    /// 描绘完成后 7 道贯穿激光沿"顶点→中心"方向汇聚穿过阵心。
    /// 玩家在描绘期间离开阵区即可安全——纹章即警告。
    /// </summary>
    public sealed class HDMCHeptagramHostile : ModProjectile
    {
        private const int DrawEnd   = 66;
        private const int FireFrame = 80;
        private const int Lifetime  = 112;

        private const float StampRadius = 150f;
        private const int   VertexCount = 7;

        private static readonly int[] StarTrace = { 0, 2, 4, 6, 1, 3, 5 };

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 900;
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
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.netMode != NetmodeID.MultiplayerClient)
                FireLasers();
            if (age == FireFrame && !Main.dedServ)
            {
                HDMCUtil.DataBurstParticles(Projectile.Center, 20, 8, 9f);
                Color burstColor = HDMCUtil.DataColor(Main.GlobalTimeWrappedHourly * 0.5f);
                CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(Projectile.Center, burstColor);
                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndInscFire) { Volume = 0.65f }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(age * 0.016f).ToVector3() * 0.4f);
        }

        private static Vector2[] GetStarVerts(Vector2 center, float rotation)
        {
            var verts = new Vector2[VertexCount];
            for (int i = 0; i < VertexCount; i++)
                verts[i] = center + (MathHelper.TwoPi * i / VertexCount + rotation).ToRotationVector2() * StampRadius;
            return verts;
        }

        private float StarRotation => Projectile.identity * 0.41f;

        private void FireLasers()
        {
            Vector2[] verts = GetStarVerts(Projectile.Center, StarRotation);
            int damage = Projectile.damage;

            for (int i = 0; i < VertexCount; i++)
            {
                Vector2 dir = (Projectile.Center - verts[i]).SafeNormalize(Vector2.UnitX);
                Vector2 origin = Projectile.Center - dir * 550f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), origin, dir,
                    ModContent.ProjectileType<HDMCLaserHostile>(),
                    damage, 0f, Main.myPlayer, 1100f, 14f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float t = Main.GlobalTimeWrappedHourly;
            Vector2[] verts = GetStarVerts(Projectile.Center, StarRotation);

            float fadeOpacity = HDMCUtil.FadeInOut(age, Lifetime, 12);
            float flashPulse  = age >= DrawEnd ? 1f + 0.45f * MathF.Sin(t * 15f) : 1f;

            float edgeProgress = age < DrawEnd
                ? age / (float)DrawEnd * VertexCount
                : VertexCount;

            for (int i = 0; i < VertexCount; i++)
            {
                float edgePct = MathHelper.Clamp(edgeProgress - i, 0f, 1f);
                if (edgePct <= 0f)
                    break;

                int fromIdx = StarTrace[i];
                int toIdx   = StarTrace[(i + 1) % VertexCount];
                Vector2 edgeStart = verts[fromIdx];
                Vector2 edgeEnd   = Vector2.Lerp(verts[fromIdx], verts[toIdx], edgePct);

                Color edgeColor = HDMCUtil.DataColor(i / (float)VertexCount) * fadeOpacity * flashPulse;

                Main.spriteBatch.DrawLineBetter(edgeStart, edgeEnd, edgeColor, 2.6f);
                Main.spriteBatch.DrawLineBetter(edgeStart, edgeEnd, edgeColor * 0.22f, 8f);

                if (edgePct < 0.98f)
                {
                    float tipPulse = 1f + 0.6f * MathF.Sin(t * 18f);
                    HyperdimensionalMatrixVisuals.DrawNode(edgeEnd, edgeColor, 5.5f * tipPulse);
                    HyperdimensionalMatrixVisuals.DrawNode(edgeEnd, edgeColor * 0.28f, 14f * tipPulse);
                }
            }

            int litVerts = (int)Math.Min(edgeProgress + 1f, VertexCount);
            for (int i = 0; i < litVerts; i++)
            {
                int vIdx = StarTrace[i];
                Color nodeColor = HDMCUtil.DataColor(i / (float)VertexCount) * fadeOpacity * flashPulse;
                HyperdimensionalMatrixVisuals.DrawNode(verts[vIdx], nodeColor, 5f + flashPulse * 1.4f);
            }

            // 阵区边界警示环——纹章之内即危险区
            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, StampRadius * 1.15f, t * 1.5f,
                HDMCUtil.DataColor(0.65f, fadeOpacity * 0.4f), 24, 1.8f);

            if (age >= DrawEnd)
            {
                float innerFade = MathHelper.SmoothStep(0f, 1f, (age - DrawEnd) / (float)(FireFrame - DrawEnd));
                HyperdimensionalMatrixVisuals.DrawGeometry(Projectile.Center, MatrixGeometryShape.Tetrahedron,
                    22f, t * 2.8f, fadeOpacity * innerFade * 0.75f, Projectile.identity);
                HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center,
                    HDMCUtil.DataColor(t * 0.5f, fadeOpacity * innerFade * 0.9f), 7f * flashPulse);
            }

            return false;
        }
    }

    // ──────────────────────────────────────────────────────
    // 分形地刺树（敌对）：地下预警 → 破土生长 → 叶端爆射
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对分形递归树：先沿生长轴显示预警线，随后一棵笔直的
    /// L-System 分形树破土生长，所有叶节点沿生长方向锥形爆射几何碎片。
    /// ai[0] = 生长方向（弧度；0 = 默认向上。需要正右方向时传入极小非零值）。
    /// </summary>
    public sealed class HDMCFractalTreeHostile : ModProjectile
    {
        private const int WarnEnd   = 42;
        private const int BuildEnd  = 92;
        private const int FireFrame = 100;
        private const int Lifetime  = 132;

        private const int   MaxDepth      = 4;
        private const float InitialLength = 85f;
        private const float BranchAngle   = MathHelper.Pi / 180f * 33f;
        private const float LengthRatio   = 0.62f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private float BaseAngle => Projectile.ai[0] == 0f ? -MathHelper.PiOver2 : Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1000;
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
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == FireFrame && Main.netMode != NetmodeID.MultiplayerClient)
                FireShards();
            if (age == FireFrame && !Main.dedServ)
            {
                List<(Vector2 pos, float angle)> leaves = new();
                CollectLeaves(Projectile.Center, BaseAngle, InitialLength, 0, leaves);
                foreach (var (pos, _) in leaves)
                {
                    Color c = HDMCUtil.DataColor(Main.rand.NextFloat());
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        pos, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f),
                        false, 8 + Main.rand.Next(8), 0.5f, c, true, false, false));
                }
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, HDMCUtil.DataColor(0.4f), 0.55f);
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.36f, Pitch = 0.15f, MaxInstances = 4 }, Projectile.Center);
            }
            if (age == WarnEnd && !Main.dedServ)
            {
                HDMCUtil.DataBurstParticles(Projectile.Center, 8, 4, 5f);
                SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.3f, Pitch = -0.2f, MaxInstances = 4 }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(age * 0.01f).ToVector3() * 0.3f);
        }

        private void FireShards()
        {
            List<(Vector2 pos, float angle)> leaves = new();
            CollectLeaves(Projectile.Center, BaseAngle, InitialLength, 0, leaves);
            int damage = Projectile.damage;

            foreach (var (pos, angle) in leaves)
            {
                Vector2 dir = angle.ToRotationVector2();
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), pos, dir * 14f,
                    ModContent.ProjectileType<HDMCShardHostile>(),
                    damage, 0f, Main.myPlayer);
            }
        }

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
            float t = Main.GlobalTimeWrappedHourly;
            float fadeOut = age > Lifetime - 15 ? (Lifetime - age) / 15f : 1f;

            if (age < WarnEnd)
            {
                // 阶段1：沿生长轴的预警线 + 汇聚粒子提示
                float warnPct = age / (float)WarnEnd;
                float pulse = 0.4f + 0.6f * (float)Math.Sin(t * 11f);
                Color warnColor = HDMCUtil.DataColor(0.08f, warnPct * pulse * 0.7f);
                Main.spriteBatch.DrawLineBetter(
                    Projectile.Center, Projectile.Center + BaseAngle.ToRotationVector2() * 420f, warnColor, 1.4f);
                HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, 30f + warnPct * 16f, t * 2f,
                    HDMCUtil.DataColor(0.15f, warnPct * 0.7f), 16, 1.6f);
                HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center, warnColor * 1.5f, 5f + warnPct * 5f);
                return false;
            }

            // 阶段2/3：破土生长 + 爆射
            float visibleDepth = MathHelper.Clamp((age - WarnEnd) / (float)(BuildEnd - WarnEnd) * MaxDepth, 0f, MaxDepth);
            bool flashing = age >= BuildEnd;
            DrawBranch(Projectile.Center, BaseAngle, InitialLength, 0, visibleDepth, fadeOut, flashing);

            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, InitialLength * 0.9f, t * 1.5f,
                HDMCUtil.DataColor(0.7f, fadeOut * 0.35f), 18, 1.2f);

            return false;
        }

        private void DrawBranch(Vector2 origin, float angle, float length, int depth,
            float visibleDepth, float opacity, bool flashing)
        {
            if (depth > visibleDepth)
                return;

            float layerPct = depth < (int)visibleDepth
                ? 1f
                : MathHelper.Clamp(visibleDepth - depth, 0f, 1f);

            float drawLength = length * layerPct;
            Vector2 end = origin + angle.ToRotationVector2() * drawLength;

            Color c = HDMCUtil.DataColor(depth / (float)MaxDepth, opacity);
            float lineWidth = Math.Max(1f, 3f - depth * 0.4f);
            Main.spriteBatch.DrawLineBetter(origin, end, c, lineWidth);

            if (depth >= MaxDepth)
            {
                float t = Main.GlobalTimeWrappedHourly;
                float pulse = 3.5f + 1.8f * MathF.Sin(t * 8f + depth + angle);
                Color leafColor = flashing
                    ? HDMCUtil.DataColor(angle * 0.3f, opacity) * 1.5f
                    : HDMCUtil.DataColor(angle * 0.3f, opacity * 0.8f);
                HyperdimensionalMatrixVisuals.DrawNode(end, leafColor, pulse);
                return;
            }

            HyperdimensionalMatrixVisuals.DrawNode(end, c * 0.6f, 3.2f);

            if (layerPct >= 1f)
            {
                DrawBranch(end, angle - BranchAngle, length * LengthRatio, depth + 1, visibleDepth, opacity, flashing);
                DrawBranch(end, angle + BranchAngle, length * LengthRatio, depth + 1, visibleDepth, opacity, flashing);
            }
        }
    }

    // ──────────────────────────────────────────────────────
    // 彭罗斯陨晶（敌对）：高空镶嵌坍缩 → 俯冲 → 落地爆炸
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 敌对彭罗斯陨晶：在落点上空构筑彭罗斯菱形镶嵌图案，坍缩为晶核后
    /// 加速俯冲，落点全程有地面预警环。落地引爆聚合爆炸 + 放射碎片。
    /// 生成位置 = 落点（弹幕自身固定在落点，天空锚点由内部计算）。
    /// ai[0] = 落地爆炸半径（0 = 默认190）。
    /// </summary>
    public sealed class HDMCPenroseHostile : ModProjectile
    {
        private const int BuildEnd    = 58;
        private const int CollapseEnd = 92;
        private const int PlungeEnd   = 108;
        private const int Lifetime    = 118;

        private const float SkyHeight = 400f;
        private static readonly float Phi = (1f + MathF.Sqrt(5f)) * 0.5f;
        private const float ThinAngle  = MathHelper.Pi / 5f;
        private const float ThickAngle = MathHelper.Pi * 2f / 5f;
        private const float RhombusSize = 12f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private Vector2 ImpactPoint => Projectile.Center;
        private Vector2 SkyAnchor => Projectile.Center + new Vector2(0f, -SkyHeight);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
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
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;

            if (age == PlungeEnd && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float blastRadius = Projectile.ai[0] > 0f ? Projectile.ai[0] : 190f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), ImpactPoint, Vector2.Zero,
                    ModContent.ProjectileType<HDMCFusionBlastHostile>(),
                    Projectile.damage, 0f, Main.myPlayer, blastRadius);

                for (int i = 0; i < 8; i++)
                {
                    Vector2 dir = (MathHelper.TwoPi * i / 8f + 0.3f).ToRotationVector2();
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(), ImpactPoint, dir * 12f,
                        ModContent.ProjectileType<HDMCShardHostile>(),
                        Math.Max(1, (int)(Projectile.damage * 0.7f)), 0f, Main.myPlayer);
                }
            }
            if (age == PlungeEnd && !Main.dedServ)
                HDMCUtil.ScreenShake(ImpactPoint, 3f, 800f);

            Lighting.AddLight(GetAnchor(), HDMCUtil.DataColor(age * 0.008f).ToVector3() * 0.3f);
        }

        private Vector2 GetAnchor()
        {
            int age = Age;
            if (age <= CollapseEnd)
                return SkyAnchor;

            float plungePct = MathHelper.Clamp((age - CollapseEnd) / (float)(PlungeEnd - CollapseEnd), 0f, 1f);
            plungePct *= plungePct;
            return Vector2.Lerp(SkyAnchor, ImpactPoint, plungePct);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float opacity = HDMCUtil.FadeInOut(age, Lifetime, 15);

            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);
            float collapsePct = age > BuildEnd
                ? MathHelper.Clamp((age - BuildEnd) / (float)(CollapseEnd - BuildEnd), 0f, 1f)
                : 0f;
            float flashPct = age >= CollapseEnd ? MathHelper.Clamp((age - CollapseEnd) / 8f, 0f, 1f) : 0f;

            Vector2 center = GetAnchor();
            Vector2[] rhombusCenters = GetRhombusCenters(center);

            float maxDist = 1f;
            float[] distances = new float[rhombusCenters.Length];
            for (int i = 0; i < rhombusCenters.Length; i++)
            {
                distances[i] = Vector2.Distance(rhombusCenters[i], center);
                if (distances[i] > maxDist) maxDist = distances[i];
            }

            for (int i = 0; i < rhombusCenters.Length; i++)
            {
                float normalizedDist = distances[i] / maxDist;
                if (buildPct < normalizedDist)
                    continue;

                float highlightPct = MathHelper.Clamp(collapsePct * 1.5f - (1f - normalizedDist) * 0.7f, 0f, 1f);
                Vector2 rhombCenter = Vector2.Lerp(rhombusCenters[i], center, collapsePct * highlightPct * 0.75f);

                bool isThin = (i % 2 == 0);
                float halfAngle = isThin ? ThinAngle : ThickAngle;
                float rot = MathF.Atan2(rhombusCenters[i].Y - center.Y, rhombusCenters[i].X - center.X) + time * 0.15f;

                Vector2 v0 = rhombCenter + rot.ToRotationVector2() * RhombusSize;
                Vector2 v1 = rhombCenter + (rot + halfAngle).ToRotationVector2() * RhombusSize;
                Vector2 v2 = rhombCenter + (rot + MathHelper.Pi).ToRotationVector2() * RhombusSize;
                Vector2 v3 = rhombCenter + (rot + MathHelper.Pi + halfAngle).ToRotationVector2() * RhombusSize;

                Color c = HDMCUtil.DataColor(i / (float)rhombusCenters.Length, opacity);
                float lineW = (highlightPct > 0.3f ? 2.4f : 1.5f) + flashPct * 2.5f;

                Main.spriteBatch.DrawLineBetter(v0, v1, c, lineW);
                Main.spriteBatch.DrawLineBetter(v1, v2, c, lineW);
                Main.spriteBatch.DrawLineBetter(v2, v3, c, lineW);
                Main.spriteBatch.DrawLineBetter(v3, v0, c, lineW);
            }

            // 落点预警：全程可见的地面警示环 + 天地连接线
            if (age < PlungeEnd)
            {
                float telegraphPulse = 0.5f + 0.5f * MathF.Sin(time * 6f);
                Color telegraphColor = HDMCUtil.DataColor(0.05f, opacity * 0.45f * telegraphPulse);
                HyperdimensionalMatrixVisuals.DrawScanRing(ImpactPoint, 52f, time * 1.1f, telegraphColor, 20, 1.7f);
                HyperdimensionalMatrixVisuals.DrawScanRing(ImpactPoint, 34f, -time * 1.6f, telegraphColor * 0.7f, 14, 1.3f);
                Main.spriteBatch.DrawLineBetter(center, ImpactPoint, telegraphColor * 0.5f, 1.1f);
            }

            return false;
        }

        private Vector2[] GetRhombusCenters(Vector2 center)
        {
            var centers = new List<Vector2>();
            const float baseSpacing = 24f;

            for (int i = 0; i < 5; i++)
                centers.Add(center + (MathHelper.TwoPi * i / 5f).ToRotationVector2() * baseSpacing);
            for (int i = 0; i < 5; i++)
                centers.Add(center + (MathHelper.TwoPi * i / 5f + MathHelper.Pi / 5f).ToRotationVector2() * baseSpacing * Phi);
            for (int i = 0; i < 10; i++)
                centers.Add(center + (MathHelper.TwoPi * i / 10f).ToRotationVector2() * baseSpacing * Phi * Phi);
            for (int i = 0; i < 10; i++)
                centers.Add(center + (MathHelper.TwoPi * i / 10f + MathHelper.Pi / 10f).ToRotationVector2() * baseSpacing * Phi * Phi * Phi * 0.65f);

            return centers.ToArray();
        }
    }
}
