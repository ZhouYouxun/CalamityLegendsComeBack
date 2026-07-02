using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General
{
    // “轨迹缠绕电流”粒子：沿弹幕的历史飞行轨迹（oldPos 快照）铺设电流。
    // 数学结构：
    // 1. 双螺旋股——两条相位相差 π 的正弦股绕轨迹切线缠绕，
    //    亮度/宽度随 cos(θ) 起伏，模拟电流转到轨迹“背面”时变暗变细的伪 3D 包裹感；
    // 2. 相位随时间高速推进（racePhaseSpeed），电流看起来沿着轨迹奔流；
    // 3. 正弦骨架上叠加持续再抖动的分形游走（与 AzureThunderArcParticle 同源），狂野而不散架；
    // 4. 稀疏的“横档”把两股连起来，强化缠绕结构；
    // 5. 快照是留在世界里的——弹幕飞远后电流仍包裹旧路径，直到淡出。
    internal sealed class AzureThunderTrailLightningParticle : Particle
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool SetLifetime => true;
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;

        private readonly Vector2[] path;          // 世界坐标折线，index 0 = 头部（生成时的弹幕中心）
        private readonly Vector2[] normals;       // 每个点的路径法线
        private readonly float[] jitterCurrentA;
        private readonly float[] jitterTargetA;
        private readonly float[] jitterCurrentB;
        private readonly float[] jitterTargetB;
        private readonly float helixAmplitude;
        private readonly float waveNumber;        // 每个采样点推进的螺旋角
        private readonly float racePhaseSpeed;    // 每帧相位推进量（电流奔流速度）
        private readonly float globalPhase;
        private readonly float baseWidth;
        private readonly float jitterAmplitude;

        public AzureThunderTrailLightningParticle(
            Vector2[] pathSnapshot,
            Color color,
            int lifetime,
            float width,
            float helixAmp,
            float jaggedness)
        {
            path = pathSnapshot;
            Color = color;
            Lifetime = lifetime;
            baseWidth = width;
            helixAmplitude = helixAmp;
            jitterAmplitude = helixAmp * 0.4f * jaggedness;

            // 每 5.5~8.5 个采样点缠绕一圈；正负号决定缠绕方向。
            waveNumber = MathHelper.TwoPi / Main.rand.NextFloat(5.5f, 8.5f);
            racePhaseSpeed = Main.rand.NextFloat(0.9f, 1.5f) * (Main.rand.NextBool() ? 1f : -1f);
            globalPhase = Main.rand.NextFloat(MathHelper.TwoPi);

            Position = path[path.Length / 2];
            Velocity = Vector2.Zero;

            // 预计算路径法线（相邻段切线的垂线，端点取邻段）。
            normals = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                Vector2 tangent;
                if (i == 0)
                    tangent = path[1] - path[0];
                else if (i == path.Length - 1)
                    tangent = path[i] - path[i - 1];
                else
                    tangent = path[i + 1] - path[i - 1];

                normals[i] = tangent.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            }

            jitterCurrentA = new float[path.Length];
            jitterTargetA = new float[path.Length];
            jitterCurrentB = new float[path.Length];
            jitterTargetB = new float[path.Length];
            AzureThunderArcParticle.RollOffsets(jitterTargetA, jitterAmplitude);
            AzureThunderArcParticle.RollOffsets(jitterTargetB, jitterAmplitude);
            Array.Copy(jitterTargetA, jitterCurrentA, path.Length);
            Array.Copy(jitterTargetB, jitterCurrentB, path.Length);
        }

        public override void Update()
        {
            // 与电弧粒子同款“活体”机制：每两帧掷新形状并插值。
            if (Time % 2 == 0)
            {
                AzureThunderArcParticle.RollOffsets(jitterTargetA, jitterAmplitude);
                AzureThunderArcParticle.RollOffsets(jitterTargetB, jitterAmplitude);
            }

            for (int i = 0; i < path.Length; i++)
            {
                jitterCurrentA[i] = MathHelper.Lerp(jitterCurrentA[i], jitterTargetA[i], 0.45f);
                jitterCurrentB[i] = MathHelper.Lerp(jitterCurrentB[i], jitterTargetB[i], 0.45f);
            }

            // 沿轨迹每隔几个点发光，让整条历史路径亮起来。
            float lightStrength = 0.3f * (1f - LifetimeCompletion);
            for (int i = 0; i < path.Length; i += 6)
                Lighting.AddLight(path[i], Color.ToVector3() * lightStrength);
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            float flicker = 0.84f + 0.16f * MathF.Sin(Time * 2.3f + globalPhase);
            float fade = MathF.Pow(1f - LifetimeCompletion, 1.5f) * flicker;
            if (Time < 2)
                fade *= 1.2f;

            int count = path.Length;
            Span<Vector2> strandA = stackalloc Vector2[count];
            Span<Vector2> strandB = stackalloc Vector2[count];
            Span<float> depthA = stackalloc float[count];
            Span<float> depthB = stackalloc float[count];

            float phase = globalPhase - Time * racePhaseSpeed;
            for (int i = 0; i < count; i++)
            {
                // 头部收紧、尾部略收，中段摆幅最大。
                float headTaper = MathHelper.Lerp(0.45f, 1f, Math.Min(i / 5f, 1f));
                float tailTaper = MathHelper.Lerp(1f, 0.6f, i / (count - 1f));
                float envelope = headTaper * tailTaper;

                float theta = i * waveNumber + phase;
                float sin = MathF.Sin(theta);
                float cos = MathF.Cos(theta);

                strandA[i] = path[i] + normals[i] * (sin * helixAmplitude * envelope + jitterCurrentA[i]);
                strandB[i] = path[i] + normals[i] * (-sin * helixAmplitude * envelope + jitterCurrentB[i]);

                // cos → [0,1]：股转到“正面”时亮、转到“背面”时暗。
                depthA[i] = cos * 0.5f + 0.5f;
                depthB[i] = 1f - depthA[i];
            }

            DrawStrand(spriteBatch, strandA, depthA, Color, fade);
            DrawStrand(spriteBatch, strandB, depthB, Color.Lerp(Color, Color.White, 0.22f), fade);

            // 稀疏横档：把两股连起来，强化“缠绕在一根轴上”的结构感。
            Color rungColor = Color.Lerp(Color, Color.White, 0.4f) * 0.32f * fade;
            for (int i = 3; i < count - 1; i += 6)
                DrawSegment(spriteBatch, strandA[i], strandB[i], rungColor, baseWidth * 0.5f);

            // 头部亮斑：电流源头压在弹幕当前/最近位置上。
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BloomCirclePinpoint").Value;
            spriteBatch.Draw(bloom, path[0] - Main.screenPosition, null, Color * 0.6f * fade,
                0f, bloom.Size() * 0.5f, baseWidth * 0.075f, SpriteEffects.None, 0f);
        }

        private void DrawStrand(SpriteBatch spriteBatch, Span<Vector2> points, Span<float> depth, Color strandColor, float fade)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                // 亮度与宽度都吃缠绕深度：背面暗且细，正面亮且粗。
                float depthMix = MathHelper.Lerp(0.3f, 1f, (depth[i] + depth[i + 1]) * 0.5f);
                float widthTaper = MathHelper.Lerp(1f, 0.45f, i / (points.Length - 1f));
                float width = baseWidth * widthTaper * MathHelper.Lerp(0.55f, 1f, depthMix);
                float brightness = fade * depthMix;

                DrawSegment(spriteBatch, points[i], points[i + 1], strandColor * 0.36f * brightness, width * 3.1f);
                DrawSegment(spriteBatch, points[i], points[i + 1],
                    Color.Lerp(strandColor, Color.White, 0.32f) * 0.6f * brightness, width * 1.55f);
                DrawSegment(spriteBatch, points[i], points[i + 1],
                    Color.Lerp(strandColor, Color.White, 0.8f) * brightness, width * 0.7f);
            }
        }

        private static void DrawSegment(SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color color, float width)
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
