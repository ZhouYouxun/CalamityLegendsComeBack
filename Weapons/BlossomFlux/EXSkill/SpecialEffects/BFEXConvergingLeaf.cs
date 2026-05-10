using System;
using System.Linq;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill.SpecialEffects
{
    internal sealed class BFEXConvergingLeaf : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int FadeFrames = 24;
        private static readonly Color LeafGreen = new(74, 255, 126);
        private static readonly Color DeepGreen = new(18, 112, 58);
        private static readonly Color PetalWhite = new(226, 255, 214);

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.ai[0];
        private ref float Seed => ref Projectile.ai[1];
        private ref float FadeSignal => ref Projectile.ai[2];

        private float Opacity =>
            Utils.GetLerpValue(0f, 10f, Timer, true) *
            Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 28;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Seed == 0f)
                Seed = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
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

            Timer++;

            if (FadeSignal != 0f)
            {
                if (Projectile.timeLeft > FadeFrames)
                    Projectile.timeLeft = FadeFrames;

                Projectile.velocity *= 0.94f;
            }
            else
            {
                HomeTowardOwner(owner);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(0.72f, 1.08f, Utils.GetLerpValue(0f, 18f, Timer, true));
            Lighting.AddLight(Projectile.Center, LeafGreen.ToVector3() * (0.32f * Opacity));

            if (Projectile.Hitbox.Intersects(owner.Hitbox) || Projectile.Distance(owner.Center) < 30f)
                Projectile.Kill();

            SpawnTravelParticles();
        }

        private void HomeTowardOwner(Player owner)
        {
            Vector2 targetOffset = new(
                (float)Math.Sin(Timer * 0.055f + Seed) * 18f,
                (float)Math.Cos(Timer * 0.071f + Seed * 0.7f) * 26f);

            Vector2 target = owner.Center + targetOffset;
            Vector2 currentDirection = Projectile.velocity.SafeNormalize((target - Projectile.Center).SafeNormalize(Vector2.UnitY));
            Vector2 desiredDirection = (target - Projectile.Center).SafeNormalize(currentDirection);
            float speed = MathHelper.Lerp(12f, 24f, Utils.GetLerpValue(0f, 36f, Timer, true));
            Vector2 desiredVelocity = desiredDirection * speed;
            float inertia = MathHelper.Lerp(18f, 5f, Utils.GetLerpValue(0f, 52f, Timer, true));

            Projectile.velocity = (Projectile.velocity * inertia + desiredVelocity) / (inertia + 1f);
        }

        private void SpawnTravelParticles()
        {
            if (Main.dedServ || Main.rand.NextBool(3))
                return;

            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                back * Main.rand.NextFloat(0.25f, 0.75f),
                false,
                Main.rand.Next(9, 14),
                Main.rand.NextFloat(0.16f, 0.26f),
                Color.Lerp(LeafGreen, PetalWhite, Main.rand.NextFloat(0.12f, 0.38f)) * Opacity,
                true,
                false,
                true));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || Timer < 4f)
                return;

            Color burstColor = Color.Lerp(LeafGreen, PetalWhite, 0.28f);
            for (int i = 0; i < 5; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 4.2f),
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.18f, 0.3f),
                    burstColor,
                    true,
                    false,
                    true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Opacity <= 0f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D streak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + Seed);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                LeafGreen with { A = 0 } * (0.42f * Opacity),
                0f,
                bloom.Size() * 0.5f,
                0.24f * pulse * Projectile.scale,
                SpriteEffects.None,
                0);

            for (int i = 0; i < 4; i++)
            {
                float rotation = forward.ToRotation() + MathHelper.PiOver2 * i + Main.GlobalTimeWrappedHourly * 1.1f;
                Main.EntitySpriteDraw(
                    streak,
                    drawPosition,
                    null,
                    Color.Lerp(LeafGreen, PetalWhite, 0.32f) with { A = 0 } * (0.25f * Opacity),
                    rotation,
                    new Vector2(streak.Width * 0.5f, streak.Height * 0.5f),
                    new Vector2(0.34f, 0.78f) * Projectile.scale * pulse,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                star,
                drawPosition,
                null,
                PetalWhite with { A = 0 } * (0.34f * Opacity),
                Projectile.rotation,
                star.Size() * 0.5f,
                new Vector2(0.18f, 0.28f) * Projectile.scale,
                SpriteEffects.None,
                0);

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
            float maxWidth = Projectile.scale * 24f * Opacity;
            const float curveRatio = 0.2f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxWidth + 0.02f;

            return Utils.Remap(completion, curveRatio, 1f, maxWidth, 0f);
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            float pulse = (float)Math.Cos(completion * 3.6f - Main.GlobalTimeWrappedHourly * 4.5f + Seed) * 0.5f + 0.5f;
            Color head = Color.Lerp(PetalWhite, LeafGreen, 0.35f + pulse * 0.28f);
            Color tail = Color.Lerp(LeafGreen, DeepGreen, completion);
            Color color = Color.Lerp(head, tail, completion);
            color.A = 0;
            return color * Opacity * Utils.GetLerpValue(1f, 0.12f, completion, true);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (Opacity <= 0f)
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
    }
}
