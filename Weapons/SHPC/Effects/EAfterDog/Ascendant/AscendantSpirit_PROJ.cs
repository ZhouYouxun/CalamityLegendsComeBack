using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ascendant
{
    internal class AscendantSpirit_PROJ : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Ascendant/AscendantSpirit_PROJ";

        private static readonly Color[] ThemePalette =
        {
            new Color(255, 176, 68),
            new Color(90, 214, 255),
            new Color(255, 126, 218)
        };

        private Vector2 startPoint;
        private Vector2 controlPoint;
        private Vector2 endPoint;
        private Color currentColor = ThemePalette[0];
        private float curveTimer;
        private float curveDuration;
        private bool initializedCurve;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.alpha = 0;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            currentColor = RandomThemeColor();
            curveDuration = 36f * (Projectile.extraUpdates + 1);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = 1f;
        }

        public static Color RandomThemeColor() => ThemePalette[Main.rand.Next(ThemePalette.Length)];

        public void InitializeCurve(Vector2 start, Vector2 control, Vector2 end, Color color, float durationFrames)
        {
            startPoint = start;
            controlPoint = control;
            endPoint = end;
            currentColor = color;
            curveTimer = 0f;
            curveDuration = Math.Max(12f, durationFrames) * (Projectile.extraUpdates + 1);
            initializedCurve = true;
            Projectile.Center = start;
            Projectile.velocity = (control - start).SafeNormalize(Vector2.UnitY) * 12f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void AI()
        {
            if (!initializedCurve)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                InitializeCurve(
                    Projectile.Center,
                    Projectile.Center + forward * 72f + forward.RotatedBy(MathHelper.PiOver2) * 56f,
                    Projectile.Center + forward * 240f,
                    currentColor,
                    36f);
            }

            curveTimer++;
            float completion = Utils.GetLerpValue(0f, curveDuration, curveTimer, true);
            Vector2 previousCenter = Projectile.Center;
            Vector2 currentPosition = EvaluateQuadratic(startPoint, controlPoint, endPoint, completion);
            Vector2 tangent = EvaluateQuadraticDerivative(startPoint, controlPoint, endPoint, completion).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));

            Projectile.Center = currentPosition;
            Projectile.velocity = currentPosition - previousCenter;
            if (Projectile.velocity.LengthSquared() < 0.0001f)
                Projectile.velocity = tangent * 0.01f;

            Projectile.rotation = tangent.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, currentColor.ToVector3() * 0.65f);

            SpawnFlightParticles(tangent, completion);

            if (completion >= 1f)
                Projectile.Kill();
        }

        private void SpawnFlightParticles(Vector2 tangent, float completion)
        {
            Vector2 normal = tangent.RotatedBy(MathHelper.PiOver2);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 14f + Projectile.identity * 0.37f + completion * 8f);

            if (Main.rand.NextBool(2))
            {
                Vector2 velocity = -tangent * Main.rand.NextFloat(0.8f, 2f) + normal * Main.rand.NextFloat(-0.9f, 0.9f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center + normal * Main.rand.NextFloat(-5f, 5f),
                    velocity,
                    Main.rand.NextFloat(0.18f, 0.3f),
                    Color.Lerp(currentColor, Color.White, 0.18f + pulse * 0.24f),
                    Main.rand.Next(9, 14)));
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + normal * Main.rand.NextFloat(-3f, 3f),
                    tangent * Main.rand.NextFloat(1.8f, 3.4f) + normal * Main.rand.NextFloat(-0.55f, 0.55f),
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.018f, 0.032f),
                    Color.Lerp(currentColor, Color.White, 0.25f),
                    new Vector2(Main.rand.NextFloat(1f, 1.3f), Main.rand.NextFloat(0.45f, 0.72f)),
                    shrinkSpeed: 0.85f));
            }
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 fallbackTangent = (endPoint - controlPoint).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));

            for (int i = 0; i < 7; i++)
            {
                float completion = i / 6f;
                Vector2 point = EvaluateQuadratic(startPoint, controlPoint, endPoint, completion);
                Vector2 tangent = EvaluateQuadraticDerivative(startPoint, controlPoint, endPoint, completion).SafeNormalize(fallbackTangent);
                Vector2 normal = tangent.RotatedBy(MathHelper.PiOver2);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    point,
                    tangent * Main.rand.NextFloat(1.4f, 3.1f) + normal * Main.rand.NextFloat(-0.75f, 0.75f),
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.018f, 0.032f),
                    Color.Lerp(currentColor, Color.White, 0.3f),
                    new Vector2(Main.rand.NextFloat(1f, 1.35f), Main.rand.NextFloat(0.48f, 0.78f)),
                    shrinkSpeed: 0.82f));
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2Circular(2.2f, 2.2f) + fallbackTangent * Main.rand.NextFloat(0.4f, 1.8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    burstVelocity,
                    false,
                    Main.rand.Next(11, 18),
                    Main.rand.NextFloat(0.28f, 0.45f),
                    Color.Lerp(currentColor, Color.White, Main.rand.NextFloat(0.12f, 0.32f)),
                    true,
                    true));
            }

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.RainbowTorch,
                    Main.rand.NextVector2Circular(2.8f, 2.8f));

                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.7f, 1.15f);
                dust.color = currentColor;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 12, targetHitbox);

        public override bool? CanDamage() => initializedCurve && curveTimer > 4f;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Color outlineColor = Color.Lerp(currentColor, Color.White, 0.2f);
            Color bodyColor = Color.Lerp(currentColor, Color.White, 0.16f);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                currentColor * 0.2f,
                0f,
                bloomOrigin,
                Projectile.scale * 0.45f,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 10; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 2.5f;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + drawOffset,
                    null,
                    outlineColor * 0.55f,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                bodyColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                trailPoints = new[] { Projectile.Center - Projectile.velocity, Projectile.Center };
            else if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                trailPoints.Length * 2);

            Vector2[] corePoints = trailPoints.Take(Math.Min(10, trailPoints.Length)).ToArray();
            if (corePoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(
                    TrailCoreWidthFunction,
                    TrailCoreColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                corePoints.Length * 2);
        }

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float maxWidth = Projectile.scale * 46f;
            float curveRatio = 0.18f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxWidth, 0f);
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color tailColor = Color.Lerp(currentColor, Color.Transparent, Utils.GetLerpValue(0.68f, 1f, completion, true));
            tailColor.A = 0;
            Color bodyColor = Color.Lerp(currentColor, Color.White, 0.18f);
            bodyColor.A = 0;
            return Color.Lerp(bodyColor, tailColor, completion);
        }

        private float TrailCoreWidthFunction(float completion, Vector2 _)
        {
            float maxWidth = Projectile.scale * 24f;
            float curveRatio = 0.18f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxWidth, 0f);
        }

        private Color TrailCoreColorFunction(float completion, Vector2 _)
        {
            Color bodyColor = Color.Lerp(Color.White, currentColor, 0.25f);
            bodyColor.A = 0;
            Color tailColor = Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.76f, 1f, completion, true));
            tailColor.A = 0;
            return Color.Lerp(bodyColor, tailColor, completion);
        }

        private static Vector2 EvaluateQuadratic(Vector2 start, Vector2 control, Vector2 end, float completion)
        {
            Vector2 firstLerp = Vector2.Lerp(start, control, completion);
            Vector2 secondLerp = Vector2.Lerp(control, end, completion);
            return Vector2.Lerp(firstLerp, secondLerp, completion);
        }

        private static Vector2 EvaluateQuadraticDerivative(Vector2 start, Vector2 control, Vector2 end, float completion)
        {
            return 2f * (1f - completion) * (control - start) + 2f * completion * (end - control);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(startPoint.X);
            writer.Write(startPoint.Y);
            writer.Write(controlPoint.X);
            writer.Write(controlPoint.Y);
            writer.Write(endPoint.X);
            writer.Write(endPoint.Y);
            writer.Write(curveTimer);
            writer.Write(curveDuration);
            writer.Write(initializedCurve);
            writer.Write(currentColor.R);
            writer.Write(currentColor.G);
            writer.Write(currentColor.B);
            writer.Write(currentColor.A);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            startPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            controlPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            endPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            curveTimer = reader.ReadSingle();
            curveDuration = reader.ReadSingle();
            initializedCurve = reader.ReadBoolean();
            currentColor = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }
    }
}
