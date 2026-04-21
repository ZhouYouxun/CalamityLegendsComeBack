using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive
{
    internal sealed class BFPassiveRecoveryField : ModProjectile, ILocalizedModType
    {
        public const int FieldRadius = 16 * 16;
        public const int FieldDiameter = FieldRadius * 2;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static Color MainColor => BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
        private static Color AccentColor => BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);

        public override void SetDefaults()
        {
            Projectile.width = FieldDiameter;
            Projectile.height = FieldDiameter;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = BFPassivePlayer.FinalStandDurationFrames;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[1]++;

            float ambientPulse = 0.86f + 0.14f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.35f + Projectile.whoAmI * 0.2f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.24f, 0.12f) * ambientPulse);

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                PlayFieldActivationSounds();
                SpawnActivationCross();

                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<BFPassiveRecoveryFieldWave>(),
                        0,
                        0f,
                        Projectile.owner);
                }
            }

            if (Projectile.timeLeft % 3 == 0)
                SpawnFieldMist();

            if (owner.GetModPlayer<BFPassivePlayer>().FinalStandActive && Vector2.Distance(Projectile.Center, owner.Center) <= FieldRadius)
                SpawnOwnerHealGlow(owner);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.3f, Pitch = -0.25f }, Projectile.Center);

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
                    DustID.GemEmerald,
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    100,
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            Texture2D flare = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/flare_01").Value;
            Texture2D flower = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/flower_014").Value;
            Texture2D energy = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/fx_EnergyBolt7").Value;

            float fadeIn = Utils.GetLerpValue(0f, 18f, Projectile.localAI[1], true);
            float fadeOut = Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.95f + 0.08f * (float)Math.Sin(time * 2.1f + Projectile.whoAmI * 0.31f);
            float ringScale = Projectile.width / (float)ring.Width * 1.08f;
            float bloomScale = Projectile.width / (float)bloom.Width * 1.04f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            BeginAdditiveBatch(spriteBatch);

            DrawAdditive(ring, drawPosition, MainColor * opacity * 0.18f, time * 0.38f, ringScale * pulse);
            DrawAdditive(ring, drawPosition, AccentColor * opacity * 0.14f, -time * 0.47f, ringScale * 0.84f);
            DrawAdditive(flower, drawPosition, AccentColor * opacity * 0.2f, time * 0.62f, ringScale * 0.72f);
            DrawAdditive(flower, drawPosition, MainColor * opacity * 0.13f, -time * 0.41f, ringScale * 0.53f);
            DrawAdditive(flare, drawPosition, AccentColor * opacity * 0.12f, 0f, ringScale * 0.6f * pulse);
            DrawAdditive(flare, drawPosition, MainColor * opacity * 0.08f, MathHelper.PiOver2, ringScale * 0.52f * pulse);
            DrawAdditive(energy, drawPosition, AccentColor * opacity * 0.16f, -time * 0.94f, ringScale * 0.18f);
            DrawAdditive(bloom, drawPosition, AccentColor * opacity * 0.14f, 0f, bloomScale * 0.72f);
            DrawAdditive(bloom, drawPosition, MainColor * opacity * 0.11f, 0f, bloomScale * 0.42f * pulse);

            BeginAlphaBatch(spriteBatch);
            return false;
        }

        private void SpawnActivationCross()
        {
            Color coreColor = Color.Lerp(MainColor, Color.White, 0.2f);
            Color flashColor = Color.Lerp(AccentColor, Color.White, 0.34f);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, flashColor, 0.95f, 22));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero, flashColor * 0.75f, Vector2.One, Main.rand.NextFloat(-0.2f, 0.2f), 0f, 0.26f, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, coreColor * 0.8f, new Vector2(0.55f, 5.6f), 0f, 0.22f, 0.032f, 24));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, coreColor * 0.8f, new Vector2(0.55f, 5.6f), MathHelper.PiOver2, 0.22f, 0.032f, 24));

            Vector2[] cardinalDirections =
            {
                Vector2.UnitX,
                -Vector2.UnitX,
                Vector2.UnitY,
                -Vector2.UnitY
            };

            foreach (Vector2 direction in cardinalDirections)
            {
                for (int i = 0; i < 10; i++)
                {
                    float chainProgress = i / 9f;
                    float speed = 2.6f + i * 1.15f;
                    float scale = 0.36f + i * 0.08f;
                    int life = 18 + i * 2;
                    Vector2 position = Projectile.Center + direction * i * 6.5f;
                    Vector2 velocity = direction * speed + direction.RotatedByRandom(0.08f) * Main.rand.NextFloat(0.12f, 0.35f);
                    Color chainColor = Color.Lerp(coreColor, flashColor, chainProgress);

                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                        position,
                        velocity,
                        scale,
                        chainColor,
                        life,
                        1f,
                        1.7f + i * 0.12f));

                    if (i % 2 == 0)
                    {
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            position + Main.rand.NextVector2Circular(4f, 4f),
                            direction * Main.rand.NextFloat(0.5f, 1.35f),
                            false,
                            10 + i,
                            0.2f + chainProgress * 0.18f,
                            Color.Lerp(MainColor, Color.White, 0.28f),
                            true,
                            false,
                            true));
                    }

                    if (i % 3 == 0)
                    {
                        Dust crossDust = Dust.NewDustPerfect(
                            position,
                            DustID.TerraBlade,
                            velocity * 0.3f,
                            90,
                            chainColor,
                            1f + chainProgress * 0.3f);
                        crossDust.noGravity = true;
                    }
                }
            }
        }

        private void SpawnFieldMist()
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float radius = Main.rand.NextFloat(24f, FieldRadius * 0.86f);
            Vector2 offset = angle.ToRotationVector2() * radius;
            Vector2 tangentVelocity = offset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-0.9f, 0.9f);

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center + offset,
                tangentVelocity * 0.2f + new Vector2(0f, -0.08f),
                false,
                Main.rand.Next(10, 16),
                Main.rand.NextFloat(0.2f, 0.34f),
                Color.Lerp(MainColor, Color.White, Main.rand.NextFloat(0.15f, 0.3f)),
                true,
                false,
                true));

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    Projectile.Center + offset * 0.78f,
                    Main.rand.NextVector2Circular(0.22f, 0.22f),
                    AccentColor,
                    Color.White,
                    Main.rand.NextFloat(0.55f, 0.9f),
                    Main.rand.Next(9, 13),
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    1.3f));
            }
        }

        private void SpawnOwnerHealGlow(Player owner)
        {
            if (!Main.rand.NextBool(2))
                return;

            Vector2 bodyPoint = owner.Center + new Vector2(
                Main.rand.NextFloat(-owner.width * 0.36f, owner.width * 0.36f),
                Main.rand.NextFloat(-owner.height * 0.48f, owner.height * 0.16f));
            Vector2 riseVelocity = new Vector2(
                Main.rand.NextFloat(-0.35f, 0.35f),
                Main.rand.NextFloat(-2.1f, -0.9f));

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                bodyPoint,
                riseVelocity.RotatedByRandom(0.3f),
                Main.rand.NextFloat(0.38f, 0.58f),
                Color.Lerp(MainColor, Color.White, Main.rand.NextFloat(0.16f, 0.34f)),
                Main.rand.Next(11, 18),
                1f,
                Main.rand.NextFloat(1.25f, 1.8f)));

            if (Main.rand.NextBool(3))
            {
                Dust healDust = Dust.NewDustPerfect(
                    bodyPoint,
                    DustID.GemEmerald,
                    riseVelocity * Main.rand.NextFloat(0.35f, 0.7f),
                    100,
                    Color.Lerp(AccentColor, Color.White, 0.2f),
                    Main.rand.NextFloat(0.9f, 1.25f));
                healDust.noGravity = true;
            }
        }

        private void PlayFieldActivationSounds()
        {
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.62f, Pitch = -0.12f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.54f, Pitch = -0.24f }, Projectile.Center);
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

        private static void BeginAdditiveBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlphaBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal sealed class BFPassiveRecoveryFieldWave : ModProjectile, ILocalizedModType
    {
        private const int WaveLifetime = 60;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static Color MainColor => BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
        private static Color AccentColor => BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = WaveLifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D waveTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResFoggyCircleHardEdge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float progress = Utils.GetLerpValue(0f, WaveLifetime, Projectile.localAI[0], true);
            float eased = MathHelper.SmoothStep(0f, 1f, progress);
            float baseScale = MathHelper.Lerp(1.65f, 3.35f, eased);
            float opacity = (1f - eased) * 0.42f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            BeginAdditiveBatch(spriteBatch);

            for (int i = 0; i < 3; i++)
            {
                float layerScale = baseScale * (1f + i * 0.22f);
                float rotation = Projectile.identity * 0.13f + i * 0.35f + eased * (0.15f + i * 0.08f);
                Color layerColor = Color.Lerp(MainColor, AccentColor, i / 2f) * opacity * (0.95f - i * 0.22f);

                Main.EntitySpriteDraw(
                    waveTexture,
                    drawPosition,
                    null,
                    layerColor,
                    rotation,
                    waveTexture.Size() * 0.5f,
                    layerScale,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                AccentColor * opacity * 0.35f,
                0f,
                bloom.Size() * 0.5f,
                baseScale * 0.72f,
                SpriteEffects.None,
                0);

            BeginAlphaBatch(spriteBatch);
            return false;
        }

        private static void BeginAdditiveBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlphaBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(
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
