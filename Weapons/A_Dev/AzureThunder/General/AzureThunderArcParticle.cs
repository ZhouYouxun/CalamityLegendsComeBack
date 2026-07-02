using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General
{
    // 自研“活电弧”粒子：
    // 1. 用平滑随机游走生成锯齿折线（中点位移思路），而不是贴图或 shader；
    // 2. 存活期间每两帧向新形状插值，电弧会持续扭动而非一闪而过；
    // 3. 首尾端点可吸附到弹幕/NPC 上，形成跟随移动的“静电触须”；
    // 4. 随机在中段分叉出一条短支线；
    // 5. 三层加法辉光（宽晕、中层、白亮核心）+ 端点 bloom。
    internal sealed class AzureThunderArcParticle : Particle
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool SetLifetime => true;
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;

        private const int MaxAnchorPoints = 26;

        private Vector2 startPoint;
        private Vector2 endPoint;

        // 端点吸附：token 用于识别实体槽位被复用的情况，失效时立即脱附并加速淡出。
        private Entity startAnchor;
        private int startAnchorToken;
        private Vector2 startAnchorOffset;
        private Entity endAnchor;
        private int endAnchorToken;
        private Vector2 endAnchorOffset;

        private readonly int anchorCount;
        private readonly float amplitude;
        private readonly float coreWidth;
        private readonly float flickerPhase;
        private readonly float[] currentOffsets;
        private readonly float[] targetOffsets;

        // 分叉支线：锚定在主线的某个中段点上，跟随主线一起扭动。
        private readonly int branchAnchorIndex = -1;
        private readonly float branchLength;
        private readonly float branchAngle;
        private readonly float[] branchCurrentOffsets;
        private readonly float[] branchTargetOffsets;

        public AzureThunderArcParticle(
            Vector2 start,
            Vector2 end,
            Color color,
            int lifetime,
            float width,
            float jaggedness = 1f,
            bool allowBranch = true)
        {
            startPoint = start;
            endPoint = end;
            Color = color;
            Lifetime = lifetime;
            coreWidth = width;
            flickerPhase = Main.rand.NextFloat(MathHelper.TwoPi);
            Position = (start + end) * 0.5f;
            Velocity = Vector2.Zero;

            float length = start.Distance(end);
            anchorCount = (int)MathHelper.Clamp(length / 12f + 4f, 6f, MaxAnchorPoints);
            amplitude = MathHelper.Clamp(length * 0.085f, 3f, 30f) * jaggedness;

            currentOffsets = new float[anchorCount];
            targetOffsets = new float[anchorCount];
            RollOffsets(targetOffsets, amplitude);
            Array.Copy(targetOffsets, currentOffsets, anchorCount);

            if (allowBranch && length > 70f && Main.rand.NextBool(2))
            {
                branchAnchorIndex = (int)(anchorCount * Main.rand.NextFloat(0.35f, 0.65f));
                branchLength = length * Main.rand.NextFloat(0.24f, 0.42f);
                branchAngle = Main.rand.NextFloat(0.5f, 1.05f) * (Main.rand.NextBool() ? 1f : -1f);

                int branchPoints = Math.Max(5, anchorCount / 2);
                branchCurrentOffsets = new float[branchPoints];
                branchTargetOffsets = new float[branchPoints];
                RollOffsets(branchTargetOffsets, amplitude * 0.7f);
                Array.Copy(branchTargetOffsets, branchCurrentOffsets, branchPoints);
            }
        }

        // ── 端点吸附 ─────────────────────────────────────────────
        private static int TokenFor(Entity entity) => entity switch
        {
            Projectile projectile => projectile.identity,
            NPC npc => npc.whoAmI,
            _ => 0
        };

        public void AttachStart(Entity entity)
        {
            startAnchor = entity;
            startAnchorToken = TokenFor(entity);
            startAnchorOffset = startPoint - entity.Center;
        }

        public void AttachEnd(Entity entity)
        {
            endAnchor = entity;
            endAnchorToken = TokenFor(entity);
            endAnchorOffset = endPoint - entity.Center;
        }

        private static bool AnchorValid(Entity anchor, int token)
        {
            if (anchor == null || !anchor.active)
                return false;

            // 槽位被新实体复用时 token 不再匹配，视为失效。
            return anchor switch
            {
                Projectile projectile => projectile.identity == token,
                NPC npc => npc.whoAmI == token,
                _ => false
            };
        }

        // ── 每帧更新：跟随锚点 + 形状再抖动 ───────────────────────
        public override void Update()
        {
            if (startAnchor != null)
            {
                if (AnchorValid(startAnchor, startAnchorToken))
                {
                    startPoint = startAnchor.Center + startAnchorOffset;
                }
                else
                {
                    // 光球消失后电弧不再跟随，快速收尾。
                    startAnchor = null;
                    Lifetime = Math.Min(Lifetime, Time + 4);
                }
            }

            if (endAnchor != null)
            {
                if (AnchorValid(endAnchor, endAnchorToken))
                {
                    endPoint = endAnchor.Center + endAnchorOffset;
                }
                else
                {
                    endAnchor = null;
                    Lifetime = Math.Min(Lifetime, Time + 4);
                }
            }

            // 每两帧掷出一个新形状，然后向它插值——电弧持续“活着”扭动。
            if (Time % 2 == 0)
            {
                RollOffsets(targetOffsets, amplitude);
                if (branchAnchorIndex >= 0)
                    RollOffsets(branchTargetOffsets, amplitude * 0.7f);
            }

            for (int i = 0; i < anchorCount; i++)
                currentOffsets[i] = MathHelper.Lerp(currentOffsets[i], targetOffsets[i], 0.45f);

            if (branchAnchorIndex >= 0)
            {
                for (int i = 0; i < branchCurrentOffsets.Length; i++)
                    branchCurrentOffsets[i] = MathHelper.Lerp(branchCurrentOffsets[i], branchTargetOffsets[i], 0.45f);
            }

            Position = (startPoint + endPoint) * 0.5f;

            // 电弧自身发出微弱青光，随生命衰减。
            Lighting.AddLight(Position, Color.ToVector3() * 0.35f * (1f - LifetimeCompletion));
        }

        internal static void RollOffsets(float[] into, float rollAmplitude)
        {
            // 平滑随机游走：0.46 的记忆系数让折线锯齿又不至于碎成噪点；
            // 正弦包络把首尾钉住，中段摆幅最大。
            float value = 0f;
            into[0] = 0f;
            into[^1] = 0f;
            for (int i = 1; i < into.Length - 1; i++)
            {
                value = value * 0.46f + Main.rand.NextFloat(-1f, 1f) * rollAmplitude;
                float envelope = MathF.Sin(MathHelper.Pi * i / (into.Length - 1f)) * 0.6f + 0.4f;
                into[i] = value * envelope;
            }
        }

        // ── 绘制 ─────────────────────────────────────────────────
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            // 亮度：生命曲线衰减 × 高频闪烁 × 出生瞬间的爆闪。
            float flicker = 0.8f + 0.2f * MathF.Sin(Time * 2.1f + flickerPhase);
            float fade = MathF.Pow(1f - LifetimeCompletion, 1.4f) * flicker;
            if (Time < 3)
                fade *= 1.25f;

            Vector2 axis = endPoint - startPoint;
            Vector2 direction = axis.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            Span<Vector2> points = stackalloc Vector2[anchorCount];
            for (int i = 0; i < anchorCount; i++)
                points[i] = startPoint + axis * (i / (anchorCount - 1f)) + normal * currentOffsets[i];

            DrawPolyline(spriteBatch, points, Color, coreWidth, fade);

            if (branchAnchorIndex >= 0 && branchAnchorIndex < anchorCount)
            {
                Vector2 branchStart = points[branchAnchorIndex];
                Vector2 branchDirection = direction.RotatedBy(branchAngle);
                Vector2 branchNormal = branchDirection.RotatedBy(MathHelper.PiOver2);

                Span<Vector2> branchPoints = stackalloc Vector2[branchCurrentOffsets.Length];
                for (int i = 0; i < branchPoints.Length; i++)
                {
                    branchPoints[i] = branchStart
                        + branchDirection * branchLength * (i / (branchPoints.Length - 1f))
                        + branchNormal * branchCurrentOffsets[i];
                }

                DrawPolyline(spriteBatch, branchPoints, Color * 0.8f, coreWidth * 0.62f, fade);
            }

            // 端点 bloom：放电起点和落点各一个亮斑，接触感更实。
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BloomCirclePinpoint").Value;
            float bloomScale = coreWidth * 0.055f;
            spriteBatch.Draw(bloom, startPoint - Main.screenPosition, null, Color * 0.5f * fade,
                0f, bloom.Size() * 0.5f, bloomScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(bloom, endPoint - Main.screenPosition, null, Color * 0.62f * fade,
                0f, bloom.Size() * 0.5f, bloomScale * 1.25f, SpriteEffects.None, 0f);
        }

        private static void DrawPolyline(SpriteBatch spriteBatch, Span<Vector2> points, Color color, float width, float fade)
        {
            // 三层辉光：宽晕 → 中层 → 白亮核心。
            Color outerColor = color * 0.36f * fade;
            Color midColor = Color.Lerp(color, Color.White, 0.3f) * 0.62f * fade;
            Color coreColor = Color.Lerp(color, Color.White, 0.78f) * fade;

            for (int i = 0; i < points.Length - 1; i++)
                DrawArcLine(spriteBatch, points[i], points[i + 1], outerColor, width * 3.3f);
            for (int i = 0; i < points.Length - 1; i++)
                DrawArcLine(spriteBatch, points[i], points[i + 1], midColor, width * 1.65f);
            for (int i = 0; i < points.Length - 1; i++)
                DrawArcLine(spriteBatch, points[i], points[i + 1], coreColor, width * 0.72f);
        }

        private static void DrawArcLine(SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 delta = b - a;
            if (delta.LengthSquared() < 0.01f)
                return;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1),
                color, delta.ToRotation(), new Vector2(0f, 0.5f), new Vector2(delta.Length(), width),
                SpriteEffects.None, 0f);
        }
    }
}
