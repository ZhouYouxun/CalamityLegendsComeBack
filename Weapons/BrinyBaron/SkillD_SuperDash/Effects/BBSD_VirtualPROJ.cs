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
        private float motionTimer;
        private int visualTimer;
        private float phaseOffset;
        private float swirlDirection;
        private Color mainColor;
        private Color accentColor;
        private bool burstSpawned;
        private string starTexturePath;

        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.extraUpdates = 4;
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
            mainColor = new Color(82, 198, 255);
            accentColor = new Color(220, 245, 255);

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
            Projectile.scale = Main.rand.NextFloat(0.82f, 1.05f);
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

            if (Projectile.numUpdates == 0)
                visualTimer++;

            motionTimer += 1f / (Projectile.extraUpdates + 1f);

            Vector2 forward = (boundProjectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float lifeProgress = Utils.GetLerpValue(60f, 0f, Projectile.timeLeft, true);
            float speedFactor = MathHelper.SmoothStep(0.18f, 1f, lifeProgress);
            float theta = phaseOffset + motionTimer * MathHelper.Lerp(0.45f, 3.4f, speedFactor) * swirlDirection;
            float axialTravel = MathHelper.Lerp(0f, 960f, lifeProgress);
            float radius = MathHelper.Lerp(160f, 24f, lifeProgress);
            float localY = (float)Math.Sin(theta) * radius;
            float localZ = (float)Math.Cos(theta * 0.9f + phaseOffset) * radius * 0.72f;
            float perspective = 1f / (1f + Math.Max(localZ + 140f, 0f) * 0.0045f);
            Vector2 targetCenter =
                boundProjectile.Center +
                forward * axialTravel +
                right * localY * perspective +
                new Vector2(0f, -localZ * 0.48f);

            if (Projectile.Center == Vector2.Zero)
                Projectile.Center = targetCenter;

            Projectile.velocity = targetCenter - Projectile.Center;
            Projectile.Center = targetCenter;
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            Projectile.scale = (0.055f + (1f - lifeProgress) * 0.05f) * MathHelper.Lerp(0.72f, 1.08f, perspective);
            SpawnFlightEffects(forward, right, theta, speedFactor);
            TrySpawnBurst(forward);
            Lighting.AddLight(Projectile.Center, 0.16f, 0.46f, 0.72f);
        }

        private void SpawnFlightEffects(Vector2 forward, Vector2 right, float theta, float speedFactor)
        {
            if (Main.dedServ || Projectile.numUpdates != 0)
                return;

            float orbitStrength = MathHelper.Lerp(5f, 18f, speedFactor);
            Vector2 orbOffset =
                right * ((float)Math.Cos(theta * 1.35f) * orbitStrength) +
                new Vector2(0f, (float)Math.Sin(theta * 1.8f) * 7f);
            Vector2 orbVelocity =
                right * ((float)Math.Sin(theta * 1.65f) * 0.15f * speedFactor) -
                forward * (0.08f + 0.42f * speedFactor);

            GeneralParticleHandler.SpawnParticle(
                new GlowOrbParticle(
                    Projectile.Center + orbOffset,
                    orbVelocity,
                    false,
                    5,
                    0.9f,
                    new Color(92, 204, 255),
                    true,
                    false,
                    true));
        }

        private void TrySpawnBurst(Vector2 forward)
        {
            if (burstSpawned || Projectile.timeLeft > 10 || Main.dedServ)
                return;

            burstSpawned = true;
            for (int i = 0; i < 8; i++)
            {
                float spread = MathHelper.Lerp(-0.85f, 0.85f, i / 7f);
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        Projectile.Center,
                        forward.RotatedBy(spread) * Main.rand.NextFloat(2.2f, 5.2f),
                        false,
                        7,
                        0.78f,
                        Color.Lerp(mainColor, Color.White, 0.35f),
                        true,
                        false,
                        true));
            }
        }

        private float TrailWidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            float width = 11f * Projectile.scale;
            float envelope = (float)Math.Sin(completionRatio * MathHelper.Pi);
            envelope = (float)Math.Pow(envelope, 0.65f);
            return MathHelper.Lerp(1.25f, width, envelope);
        }

        private Color TrailColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            float fadeToEnd = Utils.GetLerpValue(0f, 0.86f, completionRatio, true);
            Color baseColor = Color.Lerp(mainColor, accentColor, 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + completionRatio * 5f));
            Color color = Color.Lerp(baseColor, Color.White, 0.52f) * (fadeToEnd * 1.9f);
            color.A = 0;
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D starTexture = ModContent.Request<Texture2D>(starTexturePath).Value;
            Vector2 starOrigin = starTexture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 1f + 0.16f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f + phaseOffset);

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
                starTexture,
                drawPosition,
                null,
                mainColor * 1.05f,
                Projectile.rotation,
                starOrigin,
                Projectile.scale * pulse,
                SpriteEffects.None,
                0f);
            Main.EntitySpriteDraw(
                starTexture,
                drawPosition,
                null,
                accentColor * 0.95f,
                -Projectile.rotation * 0.7f,
                starOrigin,
                Projectile.scale * 0.72f * pulse,
                SpriteEffects.FlipHorizontally,
                0f);
            return false;
        }
    }
}
