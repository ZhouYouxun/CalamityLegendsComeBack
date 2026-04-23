using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
{
    internal static class ChloroplastCommon
    {
        private const string HelixTexturePath = "CalamityMod/Particles/WaterFlavored";
        private const string SparkTexturePath = "CalamityMod/Particles/BloomLineSoftEdge";
        private const string BloomTexturePath = "CalamityMod/Particles/BloomCircle";
        private const string MagicTexturePath = "CalamityLegendsComeBack/Texture/KsTexture/magic_03";
        private const float ProjectileDrawScale = 1.8f;
        private const float ExtraGlowScaleMultiplier = 0.05f;

        public static Color PresetColor(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(255, 228, 92),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(140, 255, 162),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(110, 240, 255),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 94, 68),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(182, 220, 82),
            _ => Color.White
        };

        public static Color PresetAccentColor(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(255, 250, 214),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(220, 255, 232),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(220, 250, 255),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 210, 164),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(238, 255, 166),
            _ => Color.White
        };

        public static int PresetDustType(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => DustID.YellowTorch,
            BlossomFluxChloroplastPresetType.Chlo_BRecov => DustID.GemEmerald,
            BlossomFluxChloroplastPresetType.Chlo_CDetec => DustID.IceTorch,
            BlossomFluxChloroplastPresetType.Chlo_DBomb => DustID.RedTorch,
            BlossomFluxChloroplastPresetType.Chlo_EPlague => DustID.GreenTorch,
            _ => DustID.GemEmerald
        };

        public static void FaceForward(Projectile projectile, float rotationOffset = MathHelper.PiOver2)
        {
            if (projectile.velocity != Vector2.Zero)
                projectile.rotation = projectile.velocity.ToRotation() + rotationOffset;
        }

        public static void EmitTrail(Projectile projectile, BlossomFluxChloroplastPresetType preset, float intensity = 1f)
        {
            Color mainColor = PresetColor(preset);
            Color accentColor = PresetAccentColor(preset);
            Vector2 forward = GetForwardDirection(projectile);

            if ((int)projectile.ai[1] % 5 == 0 && !Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    projectile.Center - projectile.velocity * 0.15f,
                    projectile.velocity * 0.03f,
                    Color.Lerp(mainColor, accentColor, 0.35f) * 0.72f,
                    new Vector2(0.72f, 1.95f),
                    forward.ToRotation(),
                    0.14f,
                    0.03f,
                    12));
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Particle mist = new MediumMistParticle(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    -projectile.velocity * 0.02f + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    mainColor,
                    Color.Black,
                    Main.rand.NextFloat(0.34f, 0.52f),
                    Main.rand.Next(90, 130));
                GeneralParticleHandler.SpawnParticle(mist);
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                GlowSparkParticle glowSpark = new(
                    projectile.Center,
                    projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.35f) * Main.rand.NextFloat(2f, 5f),
                    false,
                    14,
                    0.02f,
                    Color.Gold,
                    new Vector2(1.8f, 0.45f),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(glowSpark);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    PresetDustType(preset),
                    -projectile.velocity * Main.rand.NextFloat(0.015f, 0.04f) + Main.rand.NextVector2Circular(0.7f, 0.7f),
                    100,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.15f, 0.55f)),
                    Main.rand.NextFloat(0.85f, 1.2f));
                dust.noGravity = true;
            }
        }

        public static void EmitBurst(Projectile projectile, BlossomFluxChloroplastPresetType preset, int amount, float speedMin, float speedMax, float scaleMin = 0.9f, float scaleMax = 1.3f)
        {
            Color mainColor = PresetColor(preset);
            Color accentColor = PresetAccentColor(preset);
            int dustType = PresetDustType(preset);
            float intensity = 0.9f + Utils.GetLerpValue(8f, 16f, amount, true) * 0.55f;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    projectile.Center,
                    Vector2.Zero,
                    Color.Lerp(mainColor, accentColor, 0.3f),
                    0.65f * intensity,
                    18));

                GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                    projectile.Center,
                    Vector2.Zero,
                    Color.Lerp(mainColor, accentColor, 0.35f),
                    Vector2.One,
                    Main.rand.NextFloat(-0.2f, 0.2f),
                    0f,
                    0.18f * intensity,
                    16));

                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    projectile.Center,
                    Vector2.Zero,
                    Color.Lerp(mainColor, accentColor, 0.3f) * 0.8f,
                    new Vector2(1.2f, 1.9f),
                    Main.rand.NextFloat(-0.2f, 0.2f),
                    0.18f * intensity,
                    0.034f,
                    16));

                for (int i = 0; i < Math.Max(8, amount); i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(speedMin * 0.9f, speedMax * 1.25f);
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                        velocity,
                        SparkTexturePath,
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.026f, 0.042f) * intensity,
                        Main.rand.NextBool() ? mainColor : accentColor,
                        new Vector2(Main.rand.NextFloat(1.15f, 1.85f), Main.rand.NextFloat(0.55f, 0.85f)),
                        shrinkSpeed: 0.74f));
                }

                for (int i = 0; i < 4; i++)
                {
                    Particle mist = new MediumMistParticle(
                        projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                        Main.rand.NextVector2Circular(2.2f, 2.2f),
                        Main.rand.NextBool() ? mainColor : accentColor,
                        Color.Black,
                        Main.rand.NextFloat(0.45f, 0.75f) * intensity,
                        Main.rand.Next(110, 170));
                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }

            for (int i = 0; i < amount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(speedMin, speedMax);
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    dustType,
                    velocity,
                    100,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.15f, 0.55f)),
                    Main.rand.NextFloat(scaleMin, scaleMax));
                dust.noGravity = true;
            }
        }

        public static void DrawPresetProjectile(Projectile projectile, BlossomFluxChloroplastPresetType preset, Color lightColor, float scale = 1f)
        {
            Texture2D bodyTexture = TextureAssets.Projectile[projectile.type].Value;
            Texture2D helixTexture = ModContent.Request<Texture2D>(HelixTexturePath).Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>(BloomTexturePath).Value;
            Texture2D sparkTexture = ModContent.Request<Texture2D>(SparkTexturePath).Value;
            Texture2D magicTexture = ModContent.Request<Texture2D>(MagicTexturePath).Value;
            Rectangle frame = bodyTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 forward = GetForwardDirection(projectile);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            Color mainColor = projectile.GetAlpha(PresetColor(preset));
            Color accentColor = projectile.GetAlpha(PresetAccentColor(preset));
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6.4f + projectile.identity * 0.31f);
            float bodyScale = projectile.scale * ProjectileDrawScale;
            float tipDistance = 42f;

            for (int i = 0; i < projectile.oldPos.Length; i++)
            {
                if (projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)projectile.oldPos.Length;
                Main.EntitySpriteDraw(
                    bodyTexture,
                    projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition,
                    frame,
                    Color.Lerp(mainColor, accentColor, 0.25f) * (0.1f * completion),
                    projectile.oldRot[i],
                    origin,
                    bodyScale * MathHelper.Lerp(0.88f, 1f, completion),
                    SpriteEffects.None,
                    0);
            }

            BeginAdditiveBatch();

            DrawHelixOrbit(projectile, helixTexture, drawPosition, forward, normal, mainColor, accentColor);

            Vector2 tipPosition = drawPosition + forward * tipDistance;
            Vector2 tailPosition = drawPosition - forward * (tipDistance - 14f);

            DrawOrientedAdditive(
                sparkTexture,
                tipPosition,
                accentColor * 0.78f,
                forward.RotatedBy(-MathHelper.PiOver4).ToRotation(),
                new Vector2(0.8f, 0.24f) * pulse * ExtraGlowScaleMultiplier);
            DrawOrientedAdditive(
                sparkTexture,
                tipPosition,
                accentColor * 0.78f,
                forward.RotatedBy(MathHelper.PiOver4).ToRotation(),
                new Vector2(0.8f, 0.24f) * pulse * ExtraGlowScaleMultiplier);
            DrawOrientedAdditive(
                sparkTexture,
                tailPosition,
                mainColor * 0.72f,
                (-forward).ToRotation(),
                new Vector2(0.92f, 0.22f) * pulse * ExtraGlowScaleMultiplier);

            Main.EntitySpriteDraw(
                magicTexture,
                tipPosition,
                null,
                Color.Lerp(mainColor, accentColor, 0.45f) * 0.42f,
                Main.GlobalTimeWrappedHourly * 1.7f + projectile.identity * 0.03f,
                magicTexture.Size() * 0.5f,
                (0.38f + 0.03f * pulse) * ExtraGlowScaleMultiplier,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                mainColor * 0.48f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.34f * pulse * ExtraGlowScaleMultiplier,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                tipPosition,
                null,
                accentColor * 0.54f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.2f * pulse * ExtraGlowScaleMultiplier,
                SpriteEffects.None,
                0);

            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Vector2 offset = angle.ToRotationVector2() * (1.2f + 0.45f * pulse);
                Main.EntitySpriteDraw(
                    bodyTexture,
                    drawPosition + offset,
                    frame,
                    mainColor * 0.18f,
                    projectile.rotation,
                    origin,
                    bodyScale * 1.02f,
                    SpriteEffects.None,
                    0);
            }

            BeginAlphaBatch();

            Main.EntitySpriteDraw(
                bodyTexture,
                drawPosition,
                frame,
                projectile.GetAlpha(lightColor),
                projectile.rotation,
                origin,
                bodyScale,
                SpriteEffects.None,
                0);
        }

        private static Vector2 GetForwardDirection(Projectile projectile)
        {
            if (projectile.velocity.LengthSquared() > 0.001f)
                return projectile.velocity.SafeNormalize(Vector2.UnitX);

            return projectile.rotation.ToRotationVector2();
        }

        private static void DrawHelixOrbit(Projectile projectile, Texture2D helixTexture, Vector2 drawPosition, Vector2 forward, Vector2 normal, Color mainColor, Color accentColor)
        {
            float time = Main.GlobalTimeWrappedHourly * 6.6f + projectile.identity * 0.17f;
            const int helixCount = 8;

            for (int i = 0; i < helixCount; i++)
            {
                float progress = i / (float)(helixCount - 1);
                float helixAngle = time - progress * 2.3f + MathHelper.TwoPi * i / helixCount;
                Vector2 spiralOffset = normal.RotatedBy(helixAngle) * MathHelper.Lerp(24f, 10f, progress);
                Vector2 backwardOffset = -forward * MathHelper.Lerp(10f, 46f, progress);
                Vector2 totalOffset = spiralOffset + backwardOffset;
                Color orbitColor = Color.Lerp(mainColor, accentColor, progress * 0.45f) * MathHelper.Lerp(0.82f, 0.18f, progress);

                Main.EntitySpriteDraw(
                    helixTexture,
                    drawPosition + totalOffset,
                    null,
                    orbitColor,
                    forward.ToRotation() - MathHelper.PiOver2,
                    helixTexture.Size() * 0.5f,
                    new Vector2(0.28f, MathHelper.Lerp(1.05f, 0.48f, progress)) * ExtraGlowScaleMultiplier,
                    SpriteEffects.None,
                    0);
            }
        }

        private static void DrawOrientedAdditive(Texture2D texture, Vector2 position, Color color, float rotation, Vector2 scale)
        {
            Main.EntitySpriteDraw(
                texture,
                position,
                null,
                color,
                rotation,
                texture.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0);
        }

        private static void BeginAdditiveBatch()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlphaBatch()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
