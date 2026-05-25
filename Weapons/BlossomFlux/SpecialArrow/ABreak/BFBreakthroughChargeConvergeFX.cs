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
    internal sealed class BFBreakthroughChargeConvergeFX : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private const int Lifetime = 64;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float Phase => ref Projectile.ai[1];
        private ref float ChargeAtSpawn => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];
        private Vector2 startPosition;
        private bool initialized;

        private float VisualOpacity =>
            Utils.GetLerpValue(0f, 8f, Timer, true) *
            Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 28;
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
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            startPosition = Projectile.Center;
            initialized = true;
        }

        public override void AI()
        {
            if (!initialized)
            {
                startPosition = Projectile.Center;
                initialized = true;
            }

            Timer++;
            if (Timer >= Lifetime)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Opacity = VisualOpacity;

            Vector2 anchor = GetAnchor(out Vector2 aimDirection);
            Vector2 side = aimDirection.RotatedBy(MathHelper.PiOver2);
            float progress = MathHelper.Clamp(Timer / Lifetime, 0f, 1f);
            float easedProgress = MathHelper.SmoothStep(0f, 1f, progress);
            Vector2 endPosition = anchor + aimDirection * MathHelper.Lerp(26f, 56f, ChargeAtSpawn);
            Vector2 railPosition = Vector2.Lerp(startPosition, endPosition, easedProgress);
            float spiralRadius = MathHelper.Lerp(44f, 1.5f, easedProgress);
            float spiralPhase = progress * MathHelper.TwoPi * 4.35f + Phase;
            Vector2 spiralOffset =
                side * MathF.Sin(spiralPhase) * spiralRadius +
                aimDirection * MathF.Sin(progress * MathHelper.Pi) * 10f;
            Vector2 nextPosition = railPosition + spiralOffset;

            Projectile.velocity = nextPosition - Projectile.Center;
            Projectile.Center = nextPosition;
            Projectile.rotation = Projectile.velocity.SafeNormalize(aimDirection).ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(0.58f + ChargeAtSpawn * 0.12f, 0.22f, progress);

            Lighting.AddLight(Projectile.Center, new Vector3(0.22f, 0.56f, 0.15f) * Projectile.Opacity);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GreenTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.14f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    100,
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.18f, 0.42f)),
                    Main.rand.NextFloat(0.6f, 1.05f) * Projectile.Opacity);
                dust.noGravity = true;

                if (Main.rand.NextBool(4))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        Projectile.Center,
                        -Projectile.velocity.SafeNormalize(aimDirection) * Main.rand.NextFloat(0.5f, 1.2f),
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.018f, 0.032f),
                        Color.Lerp(mainColor, Color.White, 0.28f) * Projectile.Opacity,
                        new Vector2(1.35f, 0.42f),
                        true));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), Color.White, 0.25f) * Projectile.Opacity;
            float rotation = Projectile.rotation;
            float scale = 0.34f + ChargeAtSpawn * 0.16f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.72f, rotation, bloom.Size() * 0.5f, new Vector2(0.22f, scale), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(spark, drawPosition, null, Color.White * Projectile.Opacity * 0.45f, rotation, spark.Size() * 0.5f, new Vector2(0.035f, 0.22f + scale * 0.18f), SpriteEffects.None, 0);
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

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float maxWidth = MathHelper.Lerp(13f, 5f, completion) * Projectile.Opacity;
            return maxWidth * Utils.GetLerpValue(1f, 0.08f, completion, true);
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color leaf = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Color violet = new(132, 68, 255);
            Color color = Color.Lerp(Color.White, Color.Lerp(leaf, violet, 0.28f), completion);
            color.A = 0;
            return color * Projectile.Opacity * Utils.GetLerpValue(1f, 0.16f, completion, true);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (Projectile.Opacity <= 0f)
                return;

            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

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
        }

        private Vector2 GetAnchor(out Vector2 aimDirection)
        {
            aimDirection = Vector2.UnitX;
            if (BFArrowCommon.InBounds(HoldoutIndex, Main.maxProjectiles))
            {
                Projectile holdout = Main.projectile[(int)HoldoutIndex];
                if (holdout.active && holdout.type == ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>())
                {
                    aimDirection = holdout.velocity.SafeNormalize(Vector2.UnitX * Main.player[Projectile.owner].direction);
                    return holdout.Center + aimDirection * 42f;
                }
            }

            Player owner = Main.player[Projectile.owner];
            aimDirection = owner.direction == -1 ? -Vector2.UnitX : Vector2.UnitX;
            return owner.MountedCenter + aimDirection * 28f;
        }
    }
}
