using System;
using System.Collections.Generic;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General
{
    // 青霆专用「裂变电弧」。
    // 和常见的单线扭动电弧（一根不停乱扭的电线）刻意区分开，辨识度靠四件事建立：
    // 1. 树状裂变：主干 → 分支 → 末梢共三级，读起来像玻璃裂纹而不是导线；
    // 2. 生长前沿：整棵树在出生后约 3 帧内从起点「撕」到终点，分支必须等前沿扫过锚点才出现；
    // 3. 逆向凋零：末梢先灭、分支次之、主干最后，闪电是「烧回主干」而不是整体一起淡出；
    // 4. 形状冻结：成型后骨架不再重掷，只留 1px 级的呼吸抖动——快、脆、不粘手。
    // 整棵树只占一个 Particle 槽位，绘制全部自己接管。
    internal sealed class AzureThunderFissionBoltParticle : Particle
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool SetLifetime => true;
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;

        // 主角级特效，粒子上限满了也要顶掉一个普通粒子挤进去。
        public override bool Important => true;

        // 单条分支最多的折点数，同时也是绘制时 stackalloc 的上限。
        private const int MaxBranchPoints = 22;

        // 整棵树的折点预算，防止高裂变率下无限分叉。
        // 裂变率调高后这里必须跟着放宽，否则多出来的分支会被预算直接掐掉。
        private const int PointBudget = 260;

        // 生长前沿扫完全树所需帧数。4.5 帧≈75ms：还是一瞬间，但前沿推进能看清。
        private const float GrowthFrames = 4.5f;

        // 一条分支：世界坐标折点 + 各点抖动种子 + 出场/凋零时刻。
        private sealed class Branch
        {
            public Vector2[] Points;
            public float[] Seeds;
            public float[] WidthFactors;
            public float Width;
            public int Depth;
            public float RevealStart;
            public float RevealEnd;
            public float FadeStart;
            public float FadeEnd;
        }

        private readonly List<Branch> branches = new();

        // 裂变节点：前沿扫到时爆一下星芒，是这套电弧最明显的签名。
        private readonly List<(Vector2 Position, float Reveal, float Scale)> forkNodes = new();

        private readonly Color accentColor;
        private readonly Vector2 impactPoint;
        private readonly float jitterStrength;
        private int pointsUsed;

        /// <param name="start">起点（天上/爆心）。</param>
        /// <param name="end">终点（落点）。</param>
        /// <param name="color">主色，通常是青霆青。</param>
        /// <param name="accent">裂变分支和节点的高热色，通常是淡金。</param>
        /// <param name="lifetime">存活帧数，越短越「迅速」。</param>
        /// <param name="width">主干核心宽度。</param>
        /// <param name="chaos">折线摆幅倍率。</param>
        /// <param name="maxDepth">裂变层数：0=只有主干，2=主干/分支/末梢。</param>
        /// <param name="forkChance">主干每个折点的分叉概率。</param>
        public AzureThunderFissionBoltParticle(
            Vector2 start,
            Vector2 end,
            Color color,
            Color accent,
            int lifetime,
            float width,
            float chaos = 1f,
            int maxDepth = 2,
            float forkChance = 0.3f)
        {
            Color = color;
            accentColor = accent;
            Lifetime = Math.Max(4, lifetime);
            Position = (start + end) * 0.5f;
            Velocity = Vector2.Zero;
            impactPoint = end;
            jitterStrength = MathHelper.Clamp(width * 0.42f, 0.6f, 1.8f);

            BuildBranch(start, end, 0, 0f, 1f, Math.Max(0.4f, width), chaos, maxDepth, forkChance, -1f);
        }

        // ── 生成 ────────────────────────────────────────────────
        private void BuildBranch(
            Vector2 start,
            Vector2 end,
            int depth,
            float revealStart,
            float revealEnd,
            float width,
            float chaos,
            int maxDepth,
            float forkChance,
            float rootSeed)
        {
            float length = Vector2.Distance(start, end);
            if (length < 6f || pointsUsed >= PointBudget)
                return;

            // 主干折点多、分支少：末梢只需要几笔就能读成「裂开」。
            int pointCount = depth switch
            {
                0 => (int)MathHelper.Clamp(length / 26f + 5f, 6f, MaxBranchPoints),
                1 => (int)MathHelper.Clamp(length / 34f + 3f, 4f, 9f),
                _ => 4
            };
            pointCount = Math.Min(pointCount, MaxBranchPoints);
            pointsUsed += pointCount;

            Vector2 axis = end - start;
            Vector2 direction = axis.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float amplitude = MathHelper.Clamp(length * 0.11f, 3f, 34f) * chaos * (1f - depth * 0.18f);

            Branch branch = new()
            {
                Points = new Vector2[pointCount],
                Seeds = new float[pointCount],
                WidthFactors = new float[pointCount],
                Width = width,
                Depth = depth,
                RevealStart = revealStart,
                RevealEnd = revealEnd
            };

            // 末梢先死、主干最后死，整棵树是「烧回主干」而不是一起淡出。
            switch (depth)
            {
                case 0:
                    branch.FadeStart = 0.4f;
                    branch.FadeEnd = 1f;
                    break;
                case 1:
                    branch.FadeStart = 0.14f + Main.rand.NextFloat(0.1f);
                    branch.FadeEnd = 0.52f + Main.rand.NextFloat(0.16f);
                    break;
                default:
                    branch.FadeStart = 0.04f + Main.rand.NextFloat(0.06f);
                    branch.FadeEnd = 0.26f + Main.rand.NextFloat(0.12f);
                    break;
            }

            // 折线：不等分推进 + 有记忆的随机游走，让折角成簇而不是均匀弹簧。
            float walk = 0f;
            float previousT = 0f;
            for (int i = 0; i < pointCount; i++)
            {
                float t;
                if (i == 0)
                    t = 0f;
                else if (i == pointCount - 1)
                    t = 1f;
                else
                {
                    // 分段长度随机化，越靠后越不平均。
                    t = i / (pointCount - 1f) + Main.rand.NextFloat(-0.4f, 0.4f) / (pointCount - 1f);
                    t = MathHelper.Clamp(t, previousT + 0.02f, 0.97f);
                }
                previousT = t;

                float offset;
                if (i == 0 || i == pointCount - 1)
                {
                    offset = 0f;
                    walk *= 0.5f;
                }
                else
                {
                    walk = walk * 0.42f + Main.rand.NextFloat(-1f, 1f) * amplitude;
                    // 正弦包络钉住首尾，中段摆幅最大。
                    offset = walk * (MathF.Sin(MathHelper.Pi * t) * 0.65f + 0.35f);
                }

                branch.Points[i] = start + axis * t + normal * offset;
                branch.Seeds[i] = i == 0 && rootSeed >= 0f ? rootSeed : Main.rand.NextFloat(MathHelper.TwoPi);

                // 主干朝落点微微变粗（冲击感），分支朝末端收细（裂纹感）。
                branch.WidthFactors[i] = depth == 0 ? 0.88f + 0.34f * t : 1f - 0.6f * t;
            }

            branches.Add(branch);

            if (depth >= maxDepth || pointsUsed >= PointBudget)
                return;

            // 裂变：只在中段折点上抛硬币，末端分叉会糊掉落点。
            for (int i = 1; i < pointCount - 1; i++)
            {
                if (pointsUsed >= PointBudget)
                    break;
                if (!(Main.rand.NextFloat() < forkChance))
                    continue;

                Vector2 forkStart = branch.Points[i];
                Vector2 localDirection = (branch.Points[i + 1] - branch.Points[i]).SafeNormalize(direction);

                // 30°~72° 的大偏角，避免看起来像主干抖出来的毛刺。
                float angle = Main.rand.NextFloat(0.52f, 1.26f) * (Main.rand.NextBool() ? 1f : -1f);

                // 分叉长度刻意压短：太长的分支会和主干织成一张网，主干就读不出来了。
                float forkLength = length * Main.rand.NextFloat(0.14f, 0.3f) * (depth == 0 ? 1f : 0.62f);
                Vector2 forkEnd = forkStart + localDirection.RotatedBy(angle) * forkLength;

                // 前沿扫到锚点才开始长，分叉自己的传播时间按长度折算。
                float anchorReveal = MathHelper.Lerp(revealStart, revealEnd, i / (pointCount - 1f));
                float span = (revealEnd - revealStart) * (forkLength / Math.Max(1f, length)) * 0.85f;

                forkNodes.Add((forkStart, anchorReveal, width * (depth == 0 ? 1f : 0.7f)));

                BuildBranch(
                    forkStart,
                    forkEnd,
                    depth + 1,
                    anchorReveal,
                    MathHelper.Clamp(anchorReveal + span, anchorReveal + 0.03f, 1.2f),
                    width * (depth == 0 ? 0.42f : 0.5f),
                    chaos * 1.1f,
                    maxDepth,
                    forkChance * 0.6f,
                    branch.Seeds[i]);
            }
        }

        // ── 更新 ────────────────────────────────────────────────
        public override void Update()
        {
            float glow = MathF.Pow(1f - LifetimeCompletion, 1.3f);
            Lighting.AddLight(impactPoint, Color.ToVector3() * 0.75f * glow);
            Lighting.AddLight(Position, Color.ToVector3() * 0.4f * glow);
        }

        // 骨架冻结，只做统一相位的呼吸抖动；父子分支共用种子，接缝不会裂开。
        private Vector2 JitterOf(float seed)
        {
            float amount = MathF.Sin(Time * 2.6f + seed) * jitterStrength * (1f - LifetimeCompletion);
            return (seed + Time * 0.85f).ToRotationVector2() * amount;
        }

        // ── 绘制 ────────────────────────────────────────────────
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            float completion = LifetimeCompletion;

            // 前沿推进只做很轻的 ease-out：曲线太陡的话第一帧就撕到底，生长过程等于没有。
            float growth = Utils.GetLerpValue(0f, GrowthFrames, Time, true);
            growth = 1f - MathF.Pow(1f - growth, 1.35f);

            // 出生两帧过曝，之后按生命曲线塌下去。
            float flash = Time < 2 ? 1.45f : MathF.Pow(1f - completion, 1.25f);
            if (flash <= 0.002f)
                return;

            foreach (Branch branch in branches)
                DrawBranch(spriteBatch, branch, growth, completion, flash);

            DrawForkNodes(spriteBatch, growth, completion, flash);
            DrawEndpointBloom(spriteBatch, growth, flash);
        }

        private void DrawBranch(SpriteBatch spriteBatch, Branch branch, float growth, float completion, float flash)
        {
            float alive = Utils.GetLerpValue(branch.FadeEnd, branch.FadeStart, completion, true);
            if (alive <= 0.004f)
                return;

            float reveal = Utils.GetLerpValue(branch.RevealStart, branch.RevealEnd, growth, true);
            if (reveal <= 0.004f)
                return;

            int count = branch.Points.Length;
            Span<Vector2> points = stackalloc Vector2[MaxBranchPoints];
            for (int i = 0; i < count; i++)
                points[i] = branch.Points[i] + JitterOf(branch.Seeds[i]);

            // 生长前沿：只画到 reveal 对应的位置，最后一段按小数部分插值截断。
            float exact = reveal * (count - 1);
            int full = Math.Min((int)exact, count - 1);
            float frac = exact - full;
            if (full < count - 1 && frac > 0.03f)
            {
                points[full + 1] = Vector2.Lerp(points[full], points[full + 1], frac);
                full++;
            }
            if (full < 1)
                return;

            // 分支必须明确暗于主干，否则整棵树会糊成一张亮度均一的网。
            float depthDim = branch.Depth switch { 0 => 1f, 1 => 0.72f, _ => 0.5f };
            float opacity = alive * flash * depthDim;

            // 分支越深越偏高热色，主干保持青霆青——一眼能看出「主干/裂纹」两种角色。
            Color baseColor = branch.Depth == 0 ? Color : Color.Lerp(Color, accentColor, branch.Depth * 0.3f);

            // 加法混合下一层的实际贡献约等于「颜色系数的平方」，所以整体提亮 75%
            // 对应各层系数乘 sqrt(1.75)≈1.32。核心系数早就到顶了，它那份增益改成加宽实现，
            // 不然只会把核心推成纯白、整条弧反而褪色。
            Color outerColor = baseColor * 0.45f * opacity;
            Color midColor = Color.Lerp(baseColor, accentColor, 0.35f) * 0.93f * opacity;
            Color coreColor = Color.Lerp(baseColor, Color.White, 0.68f) * opacity;

            // 主干独享一层宽晕，把视线钉在主干上，分支只做裂纹。
            if (branch.Depth == 0)
                Stroke(spriteBatch, points, branch, full, baseColor * 0.17f * opacity, 7.6f);

            Stroke(spriteBatch, points, branch, full, outerColor, 4.1f);
            Stroke(spriteBatch, points, branch, full, midColor, 1.95f);
            Stroke(spriteBatch, points, branch, full, coreColor, 1f);
        }

        private static void Stroke(SpriteBatch spriteBatch, Span<Vector2> points, Branch branch, int segmentCount, Color color, float widthMultiplier)
        {
            int lastFactor = branch.WidthFactors.Length - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                float factor = (branch.WidthFactors[Math.Min(i, lastFactor)] + branch.WidthFactors[Math.Min(i + 1, lastFactor)]) * 0.5f;
                DrawLine(spriteBatch, points[i], points[i + 1], color, branch.Width * factor * widthMultiplier);
            }
        }

        private void DrawForkNodes(SpriteBatch spriteBatch, float growth, float completion, float flash)
        {
            if (forkNodes.Count == 0)
                return;

            // 裂变节点星芒沿用青霆自己的 HalfStar 语言，和剑体的镜头光是一套。
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 origin = star.Size() * 0.5f;

            foreach ((Vector2 position, float reveal, float scale) in forkNodes)
            {
                // 前沿刚扫过时爆一下，然后迅速收掉，不会一直挂着。
                float pop = Utils.GetLerpValue(reveal, reveal + 0.05f, growth, true) *
                            Utils.GetLerpValue(reveal + 0.34f, reveal + 0.04f, growth, true);
                float opacity = pop * flash * (1f - completion);
                if (opacity <= 0.01f)
                    continue;

                Color color = Color.Lerp(accentColor, Color.White, 0.35f) * opacity * 0.85f;
                Vector2 drawScale = new Vector2(0.14f, 0.5f) * scale * (0.6f + pop * 0.7f);
                Vector2 drawPosition = position - Main.screenPosition;

                spriteBatch.Draw(star, drawPosition, null, color, 0f, origin, drawScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(star, drawPosition, null, color, MathHelper.PiOver2, origin, drawScale * 0.72f, SpriteEffects.None, 0f);
            }
        }

        private void DrawEndpointBloom(SpriteBatch spriteBatch, float growth, float flash)
        {
            if (branches.Count == 0)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BloomCirclePinpoint").Value;
            Branch trunk = branches[0];
            float trunkWidth = trunk.Width;

            // 起点只是个小放电点，落点在前沿抵达后才炸开。
            spriteBatch.Draw(bloom, trunk.Points[0] - Main.screenPosition, null, Color * 0.45f * flash,
                0f, bloom.Size() * 0.5f, trunkWidth * 0.06f, SpriteEffects.None, 0f);

            float landed = Utils.GetLerpValue(0.82f, 1f, growth, true);
            if (landed <= 0.01f)
                return;

            Color impactColor = Color.Lerp(Color, Color.White, 0.55f) * landed * flash * 0.9f;
            spriteBatch.Draw(bloom, impactPoint - Main.screenPosition, null, impactColor,
                0f, bloom.Size() * 0.5f, trunkWidth * 0.15f * (0.7f + landed * 0.6f), SpriteEffects.None, 0f);
        }

        private static void DrawLine(SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 delta = b - a;
            if (delta.LengthSquared() < 0.01f)
                return;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1),
                color, delta.ToRotation(), new Vector2(0f, 0.5f), new Vector2(delta.Length(), width),
                SpriteEffects.None, 0f);
        }
    }

    // 裂变电弧的对外入口：负责组装多层电弧、落点闪光和粉尘，调用方只给一个坐标。
    internal static class AzureThunderFissionBolt
    {
        /// <summary>
        /// 从上方撕下一道裂变雷劈中指定点。用于地剑锻造这种「天雷落下」的演出。
        /// </summary>
        /// <param name="impact">落点。</param>
        /// <param name="height">起点距落点的高度。</param>
        /// <param name="tiltRadians">下落方向相对竖直的倾角。</param>
        /// <param name="scale">整体粗细/强度倍率。</param>
        public static void Strike(Vector2 impact, float height, float tiltRadians, float scale = 1f, Color? color = null, Color? accent = null)
        {
            if (Main.dedServ)
                return;

            Color main = color ?? AzureThunderColors.Azure;
            Color hot = accent ?? AzureThunderColors.PaleYellow;
            Vector2 fallDirection = Vector2.UnitY.RotatedBy(tiltRadians);
            Vector2 start = impact - fallDirection * height;

            // 主干：三级裂变，寿命只有 17 帧，读起来是「一闪」而不是「一条挂着的电线」。
            // 这道上劈雷是招式的门面，主干加粗、裂变率拉满，怎么张扬怎么来。
            GeneralParticleHandler.SpawnParticle(new AzureThunderFissionBoltParticle(
                start, impact, main, hot, 17, 5.2f * scale, 1f, 2, 0.44f));

            // 三道更细、更短命的重影，只做厚度，裂变很少，避免和主干织成一张网。
            for (int i = 0; i < 3; i++)
            {
                Vector2 ghostStart = start + Main.rand.NextVector2Circular(34f, 16f);
                Vector2 ghostEnd = impact + Main.rand.NextVector2Circular(8f, 8f);
                GeneralParticleHandler.SpawnParticle(new AzureThunderFissionBoltParticle(
                    ghostStart, ghostEnd, Color.Lerp(main, hot, 0.3f), hot, 11, 1.9f * scale, 1.4f, 1, 0.24f));
            }

            SpawnImpactFlash(impact, main, hot, scale);
        }

        /// <summary>
        /// 以命中点为中心朝四周炸开一圈裂变电弧。用于飞剑命中这种「爆裂」演出。
        /// </summary>
        /// <param name="center">爆心。</param>
        /// <param name="radius">电弧基准长度。</param>
        /// <param name="count">电弧条数。</param>
        public static void Burst(Vector2 center, float radius, int count = 9, float scale = 1f, Color? color = null, Color? accent = null)
        {
            if (Main.dedServ)
                return;

            Color main = color ?? AzureThunderColors.Azure;
            Color hot = accent ?? AzureThunderColors.PaleYellow;

            // 均分角度再加抖动：既保证四周都有覆盖，又不至于像标准的放射状图案。
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.34f, 0.34f);
                Vector2 direction = angle.ToRotationVector2();
                float length = radius * Main.rand.NextFloat(0.55f, 1.3f);

                // 起点从爆心稍微推开，读起来像「壳裂开」而不是所有线从同一像素射出。
                Vector2 boltStart = center + direction * Main.rand.NextFloat(4f, 12f);
                GeneralParticleHandler.SpawnParticle(new AzureThunderFissionBoltParticle(
                    boltStart,
                    boltStart + direction * length,
                    Main.rand.NextBool(4) ? Color.Lerp(main, hot, 0.45f) : main,
                    hot,
                    Main.rand.Next(12, 16),
                    3.3f * scale,
                    1.15f,
                    2,
                    0.52f));
            }

            SpawnImpactFlash(center, main, hot, scale * 0.85f);
        }

        private static void SpawnImpactFlash(Vector2 position, Color main, Color hot, float scale)
        {
            // 落点闪光沿用平雷同款脉冲和 LightDust，保证和武器现有命中反馈是一套语言。
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                position, Vector2.Zero, main, "CalamityMod/Particles/HighResFoggyCircleHardEdge",
                Vector2.One, 0f, 0f, 0.13f * scale, 9));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                position, Vector2.Zero, Color.Lerp(hot, Color.White, 0.4f), "CalamityMod/Particles/BloomCircle",
                Vector2.One, Main.rand.NextFloat(-10f, 10f), 1.15f * scale, 0.18f * scale, 12));

            for (int i = 0; i < (int)(9 * scale); i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    position,
                    ModContent.DustType<LightDust>(),
                    Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(1.6f, 7.5f) * scale,
                    0,
                    Main.rand.NextBool(3) ? hot : main,
                    Main.rand.NextFloat(0.42f, 0.72f) * scale);
                dust.noGravity = !Main.rand.NextBool(4);
            }
        }
    }
}
