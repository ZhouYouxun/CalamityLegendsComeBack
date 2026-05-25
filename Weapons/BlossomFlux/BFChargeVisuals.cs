using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal static class BFChargeVisualHelper
    {
        internal static bool TryGetHoldout(float holdoutIndex, int ownerIndex, out Projectile holdout, out Vector2 aimDirection)
        {
            holdout = null;
            aimDirection = Vector2.UnitX;

            if (ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
                aimDirection = Vector2.UnitX * Main.player[ownerIndex].direction;

            int index = (int)holdoutIndex;
            if (index < 0 || index >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[index];
            if (!candidate.active || candidate.type != ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>())
                return false;

            holdout = candidate;
            aimDirection = candidate.velocity.SafeNormalize(aimDirection);
            return true;
        }

        internal static Vector2 GetMuzzle(Projectile holdout, Vector2 aimDirection) => holdout.Center + aimDirection * 42f;

        internal static Vector2 GetRecoveryCore(Projectile holdout, Vector2 aimDirection) =>
            Vector2.Lerp(holdout.Center, GetMuzzle(holdout, aimDirection), 0.36f);
    }

    internal sealed class BFRecoveryHeartConvergeFX : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private const int Lifetime = 70;
        private static readonly Color LeafGreen = new(74, 255, 126);
        private static readonly Color DeepGreen = new(16, 118, 54);
        private static readonly Color PaleGreen = new(226, 255, 220);

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float SideSign => ref Projectile.ai[1];
        private ref float Seed => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];
        private Vector2 startPosition;
        private bool initialized;

        private float VisualOpacity =>
            Utils.GetLerpValue(0f, 8f, Timer, true) *
            Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 34;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            startPosition = Projectile.Center;
            initialized = true;
            if (Seed == 0f)
                Seed = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            if (!initialized)
            {
                startPosition = Projectile.Center;
                initialized = true;
            }

            if (!BFChargeVisualHelper.TryGetHoldout(HoldoutIndex, Projectile.owner, out Projectile holdout, out Vector2 aimDirection))
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            if (Timer >= Lifetime)
            {
                Projectile.Kill();
                return;
            }

            float progress = MathHelper.Clamp(Timer / Lifetime, 0f, 1f);
            float easedProgress = MathHelper.SmoothStep(0f, 1f, progress);
            Vector2 target = BFChargeVisualHelper.GetRecoveryCore(holdout, aimDirection) + aimDirection * 5f;
            Vector2 side = aimDirection.RotatedBy(MathHelper.PiOver2) * Math.Sign(SideSign == 0f ? 1f : SideSign);
            Player owner = Main.player[Projectile.owner];
            Vector2 down = Vector2.UnitY * owner.gravDir;
            Vector2 controlA = startPosition + side * (64f + 20f * MathF.Sin(Seed)) - down * (92f + 18f * MathF.Cos(Seed * 1.4f));
            Vector2 controlB = target + side * (78f + 16f * MathF.Cos(Seed * 0.8f)) - down * (54f + 24f * MathF.Sin(Seed * 1.7f));
            Vector2 nextPosition = CubicBezier(startPosition, controlA, controlB, target, easedProgress);
            nextPosition += side * MathF.Sin(progress * MathHelper.TwoPi * 1.8f + Seed) * MathHelper.Lerp(10f, 0f, easedProgress);

            Projectile.velocity = nextPosition - Projectile.Center;
            Projectile.Center = nextPosition;
            Projectile.rotation = Projectile.velocity.SafeNormalize(aimDirection).ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(0.55f, 1.08f, Utils.GetLerpValue(0f, 26f, Timer, true));
            Projectile.Opacity = VisualOpacity;
            Lighting.AddLight(Projectile.Center, LeafGreen.ToVector3() * (0.36f * Projectile.Opacity));

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    back * Main.rand.NextFloat(0.2f, 0.7f),
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.14f, 0.24f),
                    Color.Lerp(LeafGreen, PaleGreen, Main.rand.NextFloat(0.1f, 0.45f)) * Projectile.Opacity,
                    true,
                    false,
                    true));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || Timer < 8f)
                return;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(LeafGreen, PaleGreen, 0.28f) * 0.62f,
                0.18f,
                8));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9f + Seed);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                LeafGreen with { A = 0 } * (0.35f * Projectile.Opacity),
                0f,
                bloom.Size() * 0.5f,
                0.11f * Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                spark,
                drawPosition,
                null,
                PaleGreen with { A = 0 } * (0.55f * Projectile.Opacity),
                Projectile.rotation,
                spark.Size() * 0.5f,
                new Vector2(0.045f, 0.23f) * Projectile.scale,
                SpriteEffects.None,
                0f);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private static Vector2 CubicBezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float inverse = 1f - t;
            return a * inverse * inverse * inverse +
                b * 3f * inverse * inverse * t +
                c * 3f * inverse * t * t +
                d * t * t * t;
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            return trailPoints;
        }

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float maxWidth = MathHelper.Lerp(22f, 5f, completion) * Projectile.Opacity;
            return maxWidth * Utils.GetLerpValue(1f, 0.12f, completion, true);
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            float pulse = MathF.Cos(completion * 4.4f - Main.GlobalTimeWrappedHourly * 5.6f + Seed) * 0.5f + 0.5f;
            Color color = Color.Lerp(Color.Lerp(PaleGreen, LeafGreen, 0.42f + pulse * 0.22f), Color.Lerp(LeafGreen, DeepGreen, completion), completion);
            color.A = 0;
            return color * Projectile.Opacity * Utils.GetLerpValue(1f, 0.12f, completion, true);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (Projectile.Opacity <= 0f)
                return;

            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ExobladePierce"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/BlobbyNoise"));
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseImage2("Images/Extra_189");
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseColor(LeafGreen);
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseSecondaryColor(DeepGreen);

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    false,
                    GameShaders.Misc["CalamityMod:ExobladePierce"]),
                trailPoints.Length * 2);
        }
    }

    internal sealed class BFReconContractingWaveFX : ModProjectile
    {
        private const int Lifetime = 34;
        private static readonly Color ReconBlue = new(96, 232, 255);
        private static readonly Color ReconViolet = new(114, 112, 255);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float StartRadius => ref Projectile.ai[1];
        private ref float Phase => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];
        private Vector2 aimDirection = Vector2.UnitX;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            if (!BFChargeVisualHelper.TryGetHoldout(HoldoutIndex, Projectile.owner, out Projectile holdout, out aimDirection))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = holdout.Center + aimDirection * 30f;
            Projectile.rotation = aimDirection.ToRotation();
            Projectile.Opacity =
                Utils.GetLerpValue(0f, 5f, Timer, true) *
                Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, ReconBlue.ToVector3() * (0.24f * Projectile.Opacity));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Timer / Lifetime, 0f, 1f);
            float eased = MathHelper.SmoothStep(0f, 1f, progress);
            float radius = MathHelper.Lerp(StartRadius <= 0f ? 86f : StartRadius, 8f, eased);
            float opacity = Projectile.Opacity * (1f - progress * 0.35f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Color ringColor = Color.Lerp(ReconBlue, Color.White, 0.2f) with { A = 0 };
            Color violet = ReconViolet with { A = 0 };
            float ringScale = radius / (ring.Width * 0.5f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                ringColor * (0.52f * opacity),
                Projectile.rotation + Phase,
                ring.Size() * 0.5f,
                new Vector2(ringScale, ringScale * 0.72f),
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                violet * (0.24f * opacity),
                -Projectile.rotation + Phase * 0.7f,
                ring.Size() * 0.5f,
                new Vector2(ringScale * 0.62f, ringScale * 0.44f),
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 6; i++)
            {
                float angle = Projectile.rotation + Phase + MathHelper.TwoPi * i / 6f + progress * 1.8f;
                Vector2 offset = angle.ToRotationVector2() * radius * 0.48f;
                Main.EntitySpriteDraw(
                    spark,
                    drawPosition + offset,
                    null,
                    Color.Lerp(ReconBlue, Color.White, 0.36f) with { A = 0 } * (0.48f * opacity),
                    angle + MathHelper.PiOver2,
                    spark.Size() * 0.5f,
                    new Vector2(0.028f, 0.18f + 0.06f * (1f - progress)),
                    SpriteEffects.None,
                    0f);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    internal sealed class BFBombardStrafeFX : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private const int Lifetime = 42;
        private static readonly Color BloodRed = new(255, 54, 42);
        private static readonly Color HotGold = new(255, 194, 72);

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float LaneOffset => ref Projectile.ai[1];
        private ref float BackOffset => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];
        private int launchDelay;
        private float phase;
        private bool launched;

        private float VisualOpacity =>
            Utils.GetLerpValue(0f, 4f, Timer, true) *
            Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            launchDelay = Main.rand.Next(5, 10);
            phase = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            Timer++;
            Projectile.Opacity = VisualOpacity;

            bool hasHoldout = BFChargeVisualHelper.TryGetHoldout(HoldoutIndex, Projectile.owner, out Projectile holdout, out Vector2 aimDirection);
            Vector2 side = aimDirection.RotatedBy(MathHelper.PiOver2);

            if (!launched)
            {
                if (!hasHoldout)
                {
                    Projectile.Kill();
                    return;
                }

                Vector2 lanePosition =
                    holdout.Center +
                    aimDirection * BackOffset +
                    side * LaneOffset +
                    side * MathF.Sin(Timer * 0.42f + phase) * 3f;

                Projectile.velocity = lanePosition - Projectile.Center;
                Projectile.Center = lanePosition;

                if (Timer >= launchDelay)
                {
                    launched = true;
                    Projectile.velocity = aimDirection.RotatedBy(Main.rand.NextFloat(-0.085f, 0.085f)) * Main.rand.NextFloat(10.5f, 15.5f) + side * Main.rand.NextFloat(-0.8f, 0.8f);
                }
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(MathF.Sin((Timer - launchDelay) * 0.2f + phase) * 0.007f) * 1.018f;
            }

            Projectile.rotation = Projectile.velocity.SafeNormalize(aimDirection).ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(0.7f, 0.35f, Utils.GetLerpValue(18f, Lifetime, Timer, true));
            Lighting.AddLight(Projectile.Center, BloodRed.ToVector3() * (0.34f * Projectile.Opacity));

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.1f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    100,
                    Color.Lerp(BloodRed, HotGold, Main.rand.NextFloat(0.1f, 0.45f)),
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D streak = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                BloodRed with { A = 0 } * (0.24f * Projectile.Opacity),
                0f,
                bloom.Size() * 0.5f,
                0.08f * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                streak,
                drawPosition,
                null,
                Color.Lerp(BloodRed, HotGold, 0.24f) with { A = 0 } * (0.62f * Projectile.Opacity),
                Projectile.rotation,
                streak.Size() * 0.5f,
                new Vector2(0.12f, 0.36f) * Projectile.scale,
                SpriteEffects.None,
                0f);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            return trailPoints;
        }

        private float TrailWidthFunction(float completion, Vector2 _) =>
            MathHelper.Lerp(12f, 2f, completion) * Projectile.Opacity * Utils.GetLerpValue(1f, 0.1f, completion, true);

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color color = Color.Lerp(HotGold, BloodRed, completion);
            color.A = 0;
            return color * Projectile.Opacity * Utils.GetLerpValue(1f, 0.08f, completion, true);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (Projectile.Opacity <= 0f)
                return;

            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                trailPoints.Length * 2);
        }
    }

    internal sealed class BFPlagueNanomachineCloudFX : ModProjectile
    {
        private const int Lifetime = 104;
        private static readonly Color AcidGreen = new(188, 255, 62);
        private static readonly Color PlagueGreen = new(74, 205, 54);
        private static readonly Color DarkPlague = new(18, 62, 28);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Seed => ref Projectile.ai[0];
        private ref float RadiusScale => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];

        private float VisualOpacity =>
            Utils.GetLerpValue(0f, 18f, Timer, true) *
            Utils.GetLerpValue(0f, 28f, Projectile.timeLeft, true);

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Seed == 0f)
                Seed = Main.rand.NextFloat(MathHelper.TwoPi);
            if (RadiusScale <= 0f)
                RadiusScale = Main.rand.NextFloat(0.82f, 1.22f);
            Projectile.rotation = Seed;
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= 0.92f;
            Projectile.rotation += 0.008f + 0.004f * MathF.Sin(Timer * 0.05f + Seed);
            Projectile.scale = RadiusScale * MathHelper.Lerp(0.5f, 1.25f, Utils.GetLerpValue(0f, 54f, Timer, true));
            Projectile.Opacity = VisualOpacity;
            Lighting.AddLight(Projectile.Center, PlagueGreen.ToVector3() * (0.2f * Projectile.Opacity));

            if (Main.dedServ)
                return;

            if (Timer % 2f == 0f)
            {
                Vector2 offset = Main.rand.NextVector2Circular(48f, 34f) * Projectile.scale;
                Vector2 velocity = Main.rand.NextVector2Circular(0.75f, 0.75f) - offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.05f, 0.22f);
                Color color = Main.rand.NextBool(4) ? AcidGreen : Color.Lerp(PlagueGreen, Color.Cyan, Main.rand.NextFloat(0.05f, 0.22f));
                GeneralParticleHandler.SpawnParticle(new NanoParticle(
                    Projectile.Center + offset,
                    velocity,
                    color * (0.78f * Projectile.Opacity),
                    Main.rand.NextFloat(0.38f, 0.75f),
                    Main.rand.Next(24, 42),
                    Main.rand.NextBool(5),
                    true,
                    true));
            }

            if (Timer % 5f == 0f)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(42f, 30f) * Projectile.scale,
                    Main.rand.NextVector2Circular(0.34f, 0.34f),
                    Color.Lerp(DarkPlague, AcidGreen, Main.rand.NextFloat(0.18f, 0.45f)) * Projectile.Opacity,
                    Main.rand.Next(22, 36),
                    Main.rand.NextFloat(0.42f, 0.76f) * Projectile.scale,
                    0.52f,
                    Main.rand.NextFloat(-0.03f, 0.03f),
                    false));
            }

            if (Timer % 14f == 1f)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + Main.rand.NextVector2Circular(22f, 16f) * Projectile.scale,
                    Vector2.Zero,
                    Color.Lerp(PlagueGreen, AcidGreen, 0.36f) * (0.46f * Projectile.Opacity),
                    new Vector2(1.2f, 0.82f),
                    Main.rand.NextFloat(-0.4f, 0.4f),
                    0.05f,
                    0.18f * Projectile.scale,
                    20));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D fog = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/BlightFlames").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D noise = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/BlobbyNoise").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 3; i++)
            {
                float rotation = Projectile.rotation * (i % 2 == 0 ? 1f : -0.7f) + i * MathHelper.TwoPi / 3f;
                Vector2 offset = (Seed + i * MathHelper.TwoPi / 3f).ToRotationVector2() * (5f + i * 2f) * Projectile.scale;
                Color color = Color.Lerp(DarkPlague, AcidGreen, 0.32f + i * 0.16f) with { A = 0 };
                Main.EntitySpriteDraw(
                    fog,
                    drawPosition + offset,
                    null,
                    color * (0.28f * opacity),
                    rotation,
                    fog.Size() * 0.5f,
                    Projectile.scale * (0.42f + i * 0.08f),
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                PlagueGreen with { A = 0 } * (0.14f * opacity),
                0f,
                bloom.Size() * 0.5f,
                0.4f * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                noise,
                drawPosition,
                null,
                AcidGreen with { A = 0 } * (0.18f * opacity),
                -Projectile.rotation * 1.4f,
                noise.Size() * 0.5f,
                new Vector2(0.7f, 0.42f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
