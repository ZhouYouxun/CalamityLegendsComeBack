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
            if (!Main.rand.NextBool(2))
                return;

            Color mainColor = PresetColor(preset);
            Color accentColor = PresetAccentColor(preset);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 spawnPosition = projectile.Center - projectile.velocity * 0.08f + normal * Main.rand.NextFloat(-6f, 6f);

            Dust dust = Dust.NewDustPerfect(
                spawnPosition,
                PresetDustType(preset),
                -projectile.velocity * 0.05f + Main.rand.NextVector2Circular(0.8f, 0.8f),
                100,
                Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.18f, 0.55f)),
                Main.rand.NextFloat(0.85f, 1.25f) * intensity);
            dust.noGravity = true;

            if (!Main.rand.NextBool(4))
                return;

            Dust sparkle = Dust.NewDustPerfect(
                projectile.Center + normal * Main.rand.NextFloat(-4f, 4f),
                DustID.TerraBlade,
                direction.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.3f, 1.15f),
                100,
                accentColor,
                Main.rand.NextFloat(0.75f, 1.05f) * intensity);
            sparkle.noGravity = true;

            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                {
                    if (!Main.rand.NextBool(3))
                    {
                        Dust crystalDust = Dust.NewDustPerfect(
                            projectile.Center + direction * 5f + normal * Main.rand.NextFloat(-4f, 4f),
                            DustID.ChlorophyteWeapon,
                            direction * Main.rand.NextFloat(0.5f, 1.55f),
                            100,
                            accentColor,
                            Main.rand.NextFloat(0.8f, 1.1f) * intensity);
                        crystalDust.noGravity = true;
                    }

                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                {
                    if (!Main.rand.NextBool(3))
                    {
                        Dust mistDust = Dust.NewDustPerfect(
                            projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                            DustID.Smoke,
                            -projectile.velocity * Main.rand.NextFloat(0.02f, 0.05f) + Main.rand.NextVector2Circular(0.3f, 0.3f),
                            140,
                            Color.Lerp(mainColor, Color.White, 0.4f),
                            Main.rand.NextFloat(0.75f, 1f) * intensity);
                        mistDust.noGravity = true;
                    }

                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                {
                    if (!Main.rand.NextBool(3))
                    {
                        Dust reconDust = Dust.NewDustPerfect(
                            projectile.Center + normal * Main.rand.NextFloat(-3f, 3f),
                            DustID.Electric,
                            direction.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.6f, 1.35f),
                            100,
                            accentColor,
                            Main.rand.NextFloat(0.7f, 0.95f) * intensity);
                        reconDust.noGravity = true;
                    }

                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                {
                    if (!Main.rand.NextBool(3))
                    {
                        Dust emberDust = Dust.NewDustPerfect(
                            projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                            DustID.RedTorch,
                            -direction * Main.rand.NextFloat(0.15f, 0.55f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                            100,
                            Color.Lerp(mainColor, accentColor, 0.45f),
                            Main.rand.NextFloat(0.85f, 1.15f) * intensity);
                        emberDust.noGravity = true;
                    }

                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                {
                    if (!Main.rand.NextBool(3))
                    {
                        Dust plagueDust = Dust.NewDustPerfect(
                            projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                            DustID.GreenTorch,
                            -projectile.velocity * Main.rand.NextFloat(0.01f, 0.04f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                            100,
                            Color.Lerp(mainColor, accentColor, 0.35f),
                            Main.rand.NextFloat(0.85f, 1.15f) * intensity);
                        plagueDust.noGravity = true;
                    }

                    break;
                }
            }
        }

        public static void EmitBurst(Projectile projectile, BlossomFluxChloroplastPresetType preset, int amount, float speedMin, float speedMax, float scaleMin = 0.9f, float scaleMax = 1.3f)
        {
            Color mainColor = PresetColor(preset);
            Color accentColor = PresetAccentColor(preset);
            int dustType = PresetDustType(preset);

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

            for (int i = 0; i < amount / 3; i++)
            {
                Dust sparkle = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.TerraBlade,
                    Main.rand.NextVector2CircularEdge(2.2f, 2.2f) * Main.rand.NextFloat(speedMin * 0.45f, speedMax * 0.75f),
                    100,
                    accentColor,
                    Main.rand.NextFloat(0.8f, 1.1f));
                sparkle.noGravity = true;
            }
        }

        public static void DrawPresetProjectile(Projectile projectile, BlossomFluxChloroplastPresetType preset, Color lightColor, float scale = 1f)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ringTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            Texture2D flareTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/flare_01").Value;
            Texture2D slashTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/slash_01").Value;
            Texture2D flowerTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/flower_011").Value;
            Texture2D shieldFlowerTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/flower_014").Value;
            Texture2D streakTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/fx_EnergyBolt6").Value;
            Texture2D haloTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/fx_EnergyBolt7").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Color mainColor = projectile.GetAlpha(PresetColor(preset));
            Color accentColor = projectile.GetAlpha(PresetAccentColor(preset));
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float pulse = 1f + 0.07f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 6f + projectile.identity * 0.33f);

            for (int i = 0; i < projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)projectile.oldPos.Length;
                if (completion <= 0f)
                    continue;

                Main.EntitySpriteDraw(
                    texture,
                    projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition,
                    frame,
                    mainColor * (0.12f * completion),
                    projectile.oldRot[i],
                    origin,
                    projectile.scale * scale * MathHelper.Lerp(0.84f, 1f, completion),
                    SpriteEffects.None,
                    0);
            }

            BeginAdditiveBatch();

            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                {
                    DrawAdditive(bloomTexture, drawPosition, mainColor * 0.24f, 0f, projectile.scale * scale * 0.22f * pulse);
                    DrawAdditive(haloTexture, drawPosition, accentColor * 0.18f, -Main.GlobalTimeWrappedHourly * 1.6f, projectile.scale * scale * 0.1f);
                    DrawAdditive(streakTexture, drawPosition + forward * 4f, Color.Lerp(mainColor, accentColor, 0.38f) * 0.18f, projectile.rotation + MathHelper.PiOver2, projectile.scale * scale * 0.09f);
                    DrawAdditive(slashTexture, drawPosition + normal * 4f, accentColor * 0.24f, projectile.rotation + MathHelper.PiOver2 * 0.2f, projectile.scale * scale * 0.12f);
                    DrawAdditive(slashTexture, drawPosition - normal * 4f, mainColor * 0.18f, projectile.rotation - MathHelper.PiOver2 * 0.2f, projectile.scale * scale * 0.1f);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                {
                    DrawAdditive(ringTexture, drawPosition, mainColor * 0.17f, -Main.GlobalTimeWrappedHourly * 0.8f, projectile.scale * scale * 0.15f * pulse);
                    DrawAdditive(shieldFlowerTexture, drawPosition, accentColor * 0.18f, Main.GlobalTimeWrappedHourly * 1.1f, projectile.scale * scale * 0.115f);
                    DrawAdditive(streakTexture, drawPosition + forward * 2f, Color.Lerp(mainColor, accentColor, 0.45f) * 0.12f, projectile.rotation + MathHelper.PiOver2, projectile.scale * scale * 0.07f);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                {
                    DrawAdditive(ringTexture, drawPosition, mainColor * 0.15f, Main.GlobalTimeWrappedHourly * 1.8f, projectile.scale * scale * 0.14f * pulse);
                    DrawAdditive(flareTexture, drawPosition, accentColor * 0.16f, 0f, projectile.scale * scale * 0.09f);
                    DrawAdditive(flareTexture, drawPosition, accentColor * 0.12f, MathHelper.PiOver2, projectile.scale * scale * 0.08f);
                    DrawAdditive(haloTexture, drawPosition + forward * 2f, mainColor * 0.16f, -Main.GlobalTimeWrappedHourly * 1.3f, projectile.scale * scale * 0.082f);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                {
                    DrawAdditive(bloomTexture, drawPosition, mainColor * 0.28f, 0f, projectile.scale * scale * 0.24f * pulse);
                    DrawAdditive(flareTexture, drawPosition, accentColor * 0.22f, 0f, projectile.scale * scale * 0.11f);
                    DrawAdditive(streakTexture, drawPosition, Color.Lerp(mainColor, accentColor, 0.3f) * 0.18f, projectile.rotation + MathHelper.PiOver2, projectile.scale * scale * 0.11f);
                    DrawAdditive(haloTexture, drawPosition, mainColor * 0.18f, Main.GlobalTimeWrappedHourly * 1.7f, projectile.scale * scale * 0.09f);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                {
                    DrawAdditive(bloomTexture, drawPosition, mainColor * 0.18f, 0f, projectile.scale * scale * 0.2f * pulse);
                    DrawAdditive(shieldFlowerTexture, drawPosition + normal * 1.5f, accentColor * 0.12f, -Main.GlobalTimeWrappedHourly * 1.25f, projectile.scale * scale * 0.105f);
                    DrawAdditive(haloTexture, drawPosition, mainColor * 0.16f, Main.GlobalTimeWrappedHourly * 1.5f, projectile.scale * scale * 0.085f);
                    DrawAdditive(ringTexture, drawPosition, accentColor * 0.1f, Main.GlobalTimeWrappedHourly * 0.95f, projectile.scale * scale * 0.13f);
                    break;
                }
            }

            BeginAlphaBatch();

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                frame,
                projectile.GetAlpha(lightColor),
                projectile.rotation,
                origin,
                projectile.scale * scale,
                SpriteEffects.None,
                0);
        }

        private static void DrawAdditive(Texture2D texture, Vector2 position, Color color, float rotation, float scale)
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
