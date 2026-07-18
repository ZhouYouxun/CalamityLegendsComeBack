using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    // 独立放在元素法典内，避免该武器同步到 V1.0 时依赖另一把开发武器。
    internal sealed class AzureThunderArcParticle : Particle
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool SetLifetime => true;
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;

        private const int MaxPoints = 26;
        private readonly Vector2 start;
        private readonly Vector2 end;
        private readonly float width;
        private readonly float amplitude;
        private readonly float flickerPhase;
        private readonly float[] currentOffsets;
        private readonly float[] targetOffsets;

        private readonly int branchAnchor = -1;
        private readonly float branchLength;
        private readonly float branchAngle;
        private readonly float[] branchCurrentOffsets;
        private readonly float[] branchTargetOffsets;

        public AzureThunderArcParticle(Vector2 start, Vector2 end, Color color, int lifetime, float width, float jaggedness = 1f, bool allowBranch = true)
        {
            this.start = start;
            this.end = end;
            Color = color;
            Lifetime = lifetime;
            this.width = width;
            flickerPhase = Main.rand.NextFloat(MathHelper.TwoPi);
            Position = (start + end) * 0.5f;

            float length = start.Distance(end);
            int pointCount = (int)MathHelper.Clamp(length / 12f + 4f, 6f, MaxPoints);
            amplitude = MathHelper.Clamp(length * 0.085f, 3f, 30f) * jaggedness;
            currentOffsets = new float[pointCount];
            targetOffsets = new float[pointCount];
            RollOffsets(targetOffsets, amplitude);
            Array.Copy(targetOffsets, currentOffsets, pointCount);

            if (allowBranch && length > 70f && Main.rand.NextBool(2))
            {
                branchAnchor = (int)(pointCount * Main.rand.NextFloat(0.35f, 0.65f));
                branchLength = length * Main.rand.NextFloat(0.24f, 0.42f);
                branchAngle = Main.rand.NextFloat(0.5f, 1.05f) * (Main.rand.NextBool() ? 1f : -1f);
                branchCurrentOffsets = new float[Math.Max(5, pointCount / 2)];
                branchTargetOffsets = new float[branchCurrentOffsets.Length];
                RollOffsets(branchTargetOffsets, amplitude * 0.7f);
                Array.Copy(branchTargetOffsets, branchCurrentOffsets, branchCurrentOffsets.Length);
            }
        }

        public override void Update()
        {
            if (Time % 2 == 0)
            {
                RollOffsets(targetOffsets, amplitude);
                if (branchAnchor >= 0)
                    RollOffsets(branchTargetOffsets, amplitude * 0.7f);
            }

            for (int i = 0; i < currentOffsets.Length; i++)
                currentOffsets[i] = MathHelper.Lerp(currentOffsets[i], targetOffsets[i], 0.45f);

            if (branchAnchor >= 0)
            {
                for (int i = 0; i < branchCurrentOffsets.Length; i++)
                    branchCurrentOffsets[i] = MathHelper.Lerp(branchCurrentOffsets[i], branchTargetOffsets[i], 0.45f);
            }

            Lighting.AddLight(Position, Color.ToVector3() * 0.35f * (1f - LifetimeCompletion));
        }

        private static void RollOffsets(float[] offsets, float offsetAmplitude)
        {
            offsets[0] = 0f;
            offsets[^1] = 0f;
            float value = 0f;
            for (int i = 1; i < offsets.Length - 1; i++)
            {
                value = value * 0.46f + Main.rand.NextFloat(-1f, 1f) * offsetAmplitude;
                float envelope = MathF.Sin(MathHelper.Pi * i / (offsets.Length - 1f)) * 0.6f + 0.4f;
                offsets[i] = value * envelope;
            }
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            float fade = MathF.Pow(1f - LifetimeCompletion, 1.4f) * (0.8f + 0.2f * MathF.Sin(Time * 2.1f + flickerPhase));
            Vector2 axis = end - start;
            Vector2 direction = axis.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2[] points = BuildPoints(start, axis, normal, currentOffsets);
            DrawPolyline(spriteBatch, points, Color, width, fade);

            if (branchAnchor >= 0)
            {
                Vector2 branchDirection = direction.RotatedBy(branchAngle);
                Vector2[] branch = BuildPoints(points[branchAnchor], branchDirection * branchLength,
                    branchDirection.RotatedBy(MathHelper.PiOver2), branchCurrentOffsets);
                DrawPolyline(spriteBatch, branch, Color * 0.8f, width * 0.62f, fade);
            }

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BloomCirclePinpoint").Value;
            float bloomScale = width * 0.055f;
            spriteBatch.Draw(bloom, start - Main.screenPosition, null, Color * 0.5f * fade, 0f, bloom.Size() * 0.5f, bloomScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(bloom, end - Main.screenPosition, null, Color * 0.62f * fade, 0f, bloom.Size() * 0.5f, bloomScale * 1.25f, SpriteEffects.None, 0f);
        }

        private static Vector2[] BuildPoints(Vector2 origin, Vector2 axis, Vector2 normal, float[] offsets)
        {
            Vector2[] points = new Vector2[offsets.Length];
            for (int i = 0; i < points.Length; i++)
                points[i] = origin + axis * (i / (points.Length - 1f)) + normal * offsets[i];
            return points;
        }

        private static void DrawPolyline(SpriteBatch spriteBatch, Vector2[] points, Color color, float lineWidth, float fade)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                float multiplier = pass == 0 ? 3.3f : pass == 1 ? 1.65f : 0.72f;
                Color drawColor = pass == 0 ? color * 0.36f * fade : pass == 1 ? Color.Lerp(color, Color.White, 0.3f) * 0.62f * fade : Color.Lerp(color, Color.White, 0.78f) * fade;
                for (int i = 0; i < points.Length - 1; i++)
                    DrawLine(spriteBatch, points[i], points[i + 1], drawColor, lineWidth * multiplier);
            }
        }

        private static void DrawLine(SpriteBatch spriteBatch, Vector2 from, Vector2 to, Color color, float lineWidth)
        {
            Vector2 delta = to - from;
            if (delta.LengthSquared() < 0.01f)
                return;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, new Rectangle(0, 0, 1, 1), color,
                delta.ToRotation(), new Vector2(0f, 0.5f), new Vector2(delta.Length(), lineWidth), SpriteEffects.None, 0f);
        }
    }
}
