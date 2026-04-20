using System;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSD_VirtualPROJ : ModProjectile
    {
        internal const float SpawnRingRadius = 20f * 16f;

        private const int MaxLifetimeFrames = 54;
        private const float BaseDrawScale = 0.1f;
        private const float EndDrawScale = 0.062f;
        private const float ArrivalKillDistance = 18f;

        private float motionTimer;
        private int visualTimer;
        private float phaseOffset;
        private float swirlDirection;
        private Color mainColor;
        private Color accentColor;
        private bool burstSpawned;
        private bool pathInitialized;
        private float scaleVariance;
        private string starTexturePath;
        private Vector2 spawnOrigin;
        private Vector2 controlPoint;

        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 26;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = MaxLifetimeFrames;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            phaseOffset = Projectile.ai[1];
            swirlDirection = Main.rand.NextBool() ? 1f : -1f;
            mainColor = new Color(92, 208, 255);
            accentColor = new Color(232, 248, 255);

            string[] starTextures =
            {
                "CalamityLegendsComeBack/Texture/KsTexture/star_01",
                "CalamityLegendsComeBack/Texture/KsTexture/star_02",
                "CalamityLegendsComeBack/Texture/KsTexture/star_04",
                "CalamityLegendsComeBack/Texture/KsTexture/star_05",
                "CalamityLegendsComeBack/Texture/KsTexture/star_06",
                "CalamityLegendsComeBack/Texture/KsTexture/star_07",
                "CalamityLegendsComeBack/Texture/KsTexture/star_08",
                "CalamityLegendsComeBack/Texture/KsTexture/star_09"
            };
            starTexturePath = starTextures[Main.rand.Next(starTextures.Length)];
            scaleVariance = Main.rand.NextFloat(0.94f, 1.08f);
            Projectile.scale = BaseDrawScale * scaleVariance;
            Projectile.timeLeft = MaxLifetimeFrames;

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && Main.player[Projectile.owner].active)
                InitializeCurvePath(Main.player[Projectile.owner]);
        }

        public override void AI()
        {
            if (!Main.projectile.IndexInRange((int)Projectile.ai[0]))
            {
                Projectile.Kill();
                return;
            }

            Projectile boundProjectile = Main.projectile[(int)Projectile.ai[0]];
            if (!boundProjectile.active || boundProjectile.type != ModContent.ProjectileType<Z_BrinyBaron_SkillSuperCharge_SuperDash>())
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!pathInitialized)
                InitializeCurvePath(owner);

            if (Projectile.numUpdates == 0)
                visualTimer++;

            motionTimer += 1f / (Projectile.extraUpdates + 1f);

            Vector2 target = owner.MountedCenter;
            float progress = EvaluateApproachProgress((MaxLifetimeFrames - Projectile.timeLeft + 1f) / MaxLifetimeFrames);
            Vector2 nextCenter = EvaluateQuadraticBezier(spawnOrigin, controlPoint, target, progress);
            Projectile.velocity = nextCenter - Projectile.Center;
            Projectile.Center = nextCenter;

            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            float lifeProgress = Utils.GetLerpValue(MaxLifetimeFrames, 0f, Projectile.timeLeft, true);
            float opacity = ResolveOpacity();
            float dist = Vector2.Distance(Projectile.Center, target);
            Vector2 forward = Projectile.velocity.SafeNormalize((target - Projectile.Center).SafeNormalize(Vector2.UnitY));
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            Projectile.scale = MathHelper.Lerp(BaseDrawScale, EndDrawScale, progress) * scaleVariance *
                MathHelper.Lerp(1.02f, 0.9f, lifeProgress);
            SpawnFlightEffects(forward, right, progress, opacity);

            Lighting.AddLight(Projectile.Center, 0.16f * opacity, 0.46f * opacity, 0.72f * opacity);

            if (dist <= ArrivalKillDistance)
                Projectile.Kill();
        }

        private void InitializeCurvePath(Player owner)
        {
            Vector2 ringDirection = phaseOffset.ToRotationVector2();
            Vector2 tangent = ringDirection.RotatedBy(swirlDirection * MathHelper.PiOver2);
            Vector2 target = owner.MountedCenter;

            spawnOrigin = target + ringDirection * SpawnRingRadius;
            controlPoint =
                target +
                ringDirection * (SpawnRingRadius * Main.rand.NextFloat(0.34f, 0.52f)) +
                tangent * (SpawnRingRadius * Main.rand.NextFloat(0.24f, 0.4f));

            Projectile.Center = spawnOrigin;
            Projectile.velocity = (target - spawnOrigin).SafeNormalize(Vector2.UnitY);
            pathInitialized = true;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
                Projectile.oldPos[i] = Projectile.position;
        }

        private static float EvaluateApproachProgress(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float fastCurve = 1f - (float)Math.Pow(1f - progress, 2.6f);
            float cleanCurve = progress * progress * (3f - 2f * progress);
            return MathHelper.Lerp(fastCurve, cleanCurve, 0.2f);
        }

        private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * start + 2f * inverse * progress * control + progress * progress * end;
        }

        private float ResolveOpacity()
        {
            float age = MaxLifetimeFrames - Projectile.timeLeft;
            float appear = Utils.GetLerpValue(0f, 5f, age, true);
            float disappear = Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);
            return appear * disappear;
        }

        private void SpawnFlightEffects(Vector2 forward, Vector2 right, float progress, float opacity)
        {
            if (Main.dedServ || Projectile.numUpdates != 0)
                return;

            float orbitPhase = phaseOffset + motionTimer * 0.44f * swirlDirection;
            float orbitStrength = MathHelper.Lerp(18f, 4f, progress);
            Vector2 orbOffset =
                right * ((float)Math.Cos(orbitPhase) * orbitStrength) +
                forward * ((float)Math.Sin(orbitPhase * 1.25f) * 10f * (1f - progress));
            Vector2 orbVelocity =
                right * ((float)Math.Sin(orbitPhase * 1.4f) * 0.24f) -
                forward * MathHelper.Lerp(0.16f, 1.6f, progress);

            GeneralParticleHandler.SpawnParticle(
                new GlowOrbParticle(
                    Projectile.Center + orbOffset,
                    orbVelocity,
                    false,
                    7,
                    MathHelper.Lerp(0.3f, 0.18f, progress),
                    Color.Lerp(mainColor, accentColor, 0.24f),
                    true,
                    false,
                    true));

            if (visualTimer % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(
                    new LineParticle(
                        Projectile.Center,
                        -forward * MathHelper.Lerp(0.45f, 1.6f, progress),
                        false,
                        8,
                        MathHelper.Lerp(0.28f, 0.16f, progress),
                        Color.Lerp(mainColor, accentColor, 0.35f) * opacity));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (burstSpawned || Main.dedServ)
                return;

            burstSpawned = true;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            GeneralParticleHandler.SpawnParticle(
                new DirectionalPulseRing(
                    Projectile.Center,
                    Vector2.Zero,
                    Color.Lerp(mainColor, Color.White, 0.22f),
                    new Vector2(0.28f, 0.82f),
                    forward.ToRotation() - MathHelper.PiOver2,
                    0.14f,
                    0.02f,
                    10));

            for (int i = 0; i < 6; i++)
            {
                float spread = MathHelper.Lerp(-0.85f, 0.85f, i / 5f);
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        Projectile.Center,
                        forward.RotatedBy(spread) * Main.rand.NextFloat(2.8f, 6.2f),
                        false,
                        7,
                        0.26f,
                        Color.Lerp(mainColor, Color.White, 0.35f),
                        true,
                        false,
                        true));
            }
        }

        private float TrailWidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            float width = 56f * Projectile.scale;
            float envelope = (float)Math.Sin(completionRatio * MathHelper.Pi);
            envelope = (float)Math.Pow(envelope, 0.58f);
            return MathHelper.Lerp(0.55f, width, envelope);
        }

        private Color TrailColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            float fadeToEnd = Utils.GetLerpValue(0f, 0.86f, completionRatio, true);
            float opacity = ResolveOpacity();
            Color baseColor = Color.Lerp(mainColor, accentColor, 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + completionRatio * 5f));
            Color color = Color.Lerp(baseColor, Color.White, 0.52f) * (fadeToEnd * 2.15f * opacity);
            color.A = 0;
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D starTexture = ModContent.Request<Texture2D>(starTexturePath).Value;
            Texture2D glowTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 starOrigin = starTexture.Size() * 0.5f;
            Vector2 glowOrigin = glowTexture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 1f + 0.16f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f + phaseOffset);
            float opacity = ResolveOpacity();

            Vector2[] trailPositions = new Vector2[Projectile.oldPos.Length];
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                trailPositions[i] = Projectile.oldPos[i] == Vector2.Zero ? Projectile.Center : Projectile.oldPos[i] + Projectile.Size * 0.5f;

            GameShaders.Misc["CalamityMod:TrailStreak"].UseImage1("Images/Misc/Perlin");
            PrimitiveRenderer.RenderTrail(
                trailPositions,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (completionRatio, vertexPos) => Vector2.Zero,
                    shader: GameShaders.Misc["CalamityMod:TrailStreak"]),
                40);

            Main.EntitySpriteDraw(
                glowTexture,
                drawPosition,
                null,
                mainColor * 0.55f * opacity,
                0f,
                glowOrigin,
                Projectile.scale * 0.92f * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                starTexture,
                drawPosition,
                null,
                mainColor * 1.05f * opacity,
                Projectile.rotation,
                starOrigin,
                Projectile.scale * pulse,
                SpriteEffects.None,
                0f);
            Main.EntitySpriteDraw(
                starTexture,
                drawPosition,
                null,
                accentColor * 0.95f * opacity,
                -Projectile.rotation * 0.7f,
                starOrigin,
                Projectile.scale * 0.82f * pulse,
                SpriteEffects.FlipHorizontally,
                0f);
            return false;
        }
    }
}
