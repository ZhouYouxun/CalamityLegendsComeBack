using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFDog_Flame : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Ascendant/AscendantSpirit_PROJ";

        private const float CollisionRadius = 12f;
        private const int Lifetime = 76;
        private ref float Timer => ref Projectile.localAI[0];
        private float squash = 0.42f;

        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));
        private Color VisualColor
        {
            get
            {
                Color theme = ThemeColor;
                float pulse = (float)Math.Sin(Timer * 0.14f + Projectile.identity * 0.31f) * 0.5f + 0.5f;
                return Color.Lerp(theme, Color.White, 0.18f + pulse * 0.18f);
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 30;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && Main.player[Projectile.owner].GetModPlayer<PristineFuryPlayer>().CurrentMark != PristineFuryMark.Dog)
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 0.98f, 0.08f);
            squash = MathHelper.Lerp(squash, 0.2f, 0.12f);

            Lighting.AddLight(Projectile.Center, VisualColor.ToVector3() * 0.72f);
            SpawnFlightParticles(direction);

            if (Timer > 20f)
                Projectile.velocity *= 1.006f;
        }

        private void SpawnFlightParticles(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color visualColor = VisualColor;

            if ((int)Timer % 2 == 0)
            {
                Particle trail = new GlowOrbParticle(
                    Projectile.Center - direction * 22f + normal * Main.rand.NextFloat(-3f, 3f),
                    -direction * Main.rand.NextFloat(0.8f, 2.2f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.2f, 0.38f),
                    visualColor * 0.84f,
                    true,
                    false,
                    true);

                GeneralParticleHandler.SpawnParticle(trail);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), ModContent.DustType<SquashDust>());
                dust.scale = Main.rand.NextFloat(0.92f, 1.42f);
                dust.velocity = -direction * Main.rand.NextFloat(2.4f, 5.6f) + normal * Main.rand.NextFloat(-1.2f, 1.2f);
                dust.noGravity = true;
                dust.color = Color.Lerp(visualColor, Color.White, Main.rand.NextFloat(0.1f, 0.32f));
                dust.fadeIn = 3.4f;
            }

            if (Main.rand.NextBool(4))
            {
                Particle star = new GlowOrbParticle(
                    Projectile.Center + normal * Main.rand.NextFloat(-6f, 6f),
                    -direction * Main.rand.NextFloat(0.6f, 1.8f) + normal * Main.rand.NextFloat(-0.8f, 0.8f),
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.18f, 0.32f),
                    Color.Lerp(visualColor, Color.White, 0.2f),
                    true,
                    false,
                    true);

                GeneralParticleHandler.SpawnParticle(star);
            }

            if (Main.rand.NextBool(3))
            {
                Particle node = new SquishyLightParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + normal * Main.rand.NextFloat(-7f, 7f),
                    -direction * Main.rand.NextFloat(0.35f, 1.2f) + normal * Main.rand.NextFloat(-0.5f, 0.5f),
                    Main.rand.NextFloat(0.32f, 0.58f),
                    Color.Lerp(visualColor, Color.White, Main.rand.NextFloat(0.14f, 0.42f)),
                    Main.rand.Next(13, 22));

                GeneralParticleHandler.SpawnParticle(node);
            }

            if (Main.rand.NextBool(5))
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(12f, 28f) + normal * Main.rand.NextFloat(-6f, 6f),
                    -direction * Main.rand.NextFloat(0.35f, 1.1f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    Color.Lerp(visualColor, Color.DarkGoldenrod, 0.35f),
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.38f, 0.7f),
                    0.58f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    glowing: true);

                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool? CanDamage() => Timer > 2f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, CollisionRadius, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 300);
            SpawnImpactEffects(target.Center);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                modifiers.SourceDamage *= 0.82f;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color visualColor = VisualColor;

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.8f, 2.8f) + direction * Main.rand.NextFloat(0.6f, 2.2f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(11, 18),
                    Main.rand.NextFloat(0.24f, 0.42f),
                    Color.Lerp(visualColor, Color.White, Main.rand.NextFloat(0.12f, 0.34f)),
                    true,
                    true));
            }

            Particle pulse = new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                visualColor,
                Vector2.One,
                direction.ToRotation(),
                0.18f,
                1.75f,
                19);

            GeneralParticleHandler.SpawnParticle(pulse);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(visualColor, Color.White, 0.18f) * 0.55f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-0.25f, 0.25f),
                0.06f,
                0.68f,
                14,
                false));

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.1f, 3.2f),
                    Color.Lerp(visualColor, Color.DarkGoldenrod, Main.rand.NextFloat(0.22f, 0.55f)),
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.48f, 0.86f),
                    0.56f,
                    Main.rand.NextFloat(-0.06f, 0.06f),
                    glowing: true));
            }
        }

        private void SpawnImpactEffects(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color visualColor = VisualColor;

            for (int i = 0; i < 18; i++)
            {
                float ratio = i / 17f;
                float angle = MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, ratio);
                Vector2 velocity = direction.RotatedBy(angle + Main.rand.NextFloat(-0.18f, 0.18f)) * Main.rand.NextFloat(2.2f, 7.6f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center + velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0f, 8f),
                    velocity,
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.55f, 1.12f),
                    Main.rand.NextBool(4) ? Color.White : Color.Lerp(visualColor, Color.Gold, Main.rand.NextFloat(0.08f, 0.36f))));
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.1f, 5.4f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    Main.rand.NextFloat(0.48f, 0.92f),
                    Color.Lerp(visualColor, Color.White, Main.rand.NextFloat(0.12f, 0.45f)),
                    Main.rand.Next(16, 28)));
            }

            for (int i = 0; i < 5; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4.2f),
                    Color.Lerp(visualColor, Color.DarkGoldenrod, Main.rand.NextFloat(0.22f, 0.55f)),
                    Main.rand.Next(20, 34),
                    Main.rand.NextFloat(0.62f, 1.08f),
                    0.58f,
                    Main.rand.NextFloat(-0.075f, 0.075f),
                    glowing: true));
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                visualColor * 0.82f,
                Vector2.One,
                direction.ToRotation(),
                0.16f,
                1.25f,
                16));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D circularSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmear").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color visualColor = VisualColor;
            Color outlineColor = Color.Lerp(visualColor, Color.White, 0.2f);
            Color bodyColor = Color.Lerp(visualColor, Color.White, 0.16f);
            float fadeIn = Utils.GetLerpValue(0f, 8f, Timer, true);
            float fadeOut = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            Vector2 needleScale = new Vector2(0.78f + squash * 0.12f, 2.4f + (1f - squash) * 0.85f) * Projectile.scale;

            PFLeftEffectRules.BeginAdditive();

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                visualColor with { A = 0 } * 0.28f * opacity,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.22f, 0.64f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                circularSmear,
                drawPosition,
                null,
                visualColor with { A = 0 } * 0.34f * opacity,
                Projectile.rotation * 1.3f,
                circularSmear.Size() * 0.5f,
                Projectile.scale * 0.32f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                bloomRing,
                drawPosition,
                null,
                visualColor with { A = 0 } * 0.22f * opacity,
                -Projectile.rotation + Timer * 0.045f,
                bloomRing.Size() * 0.5f,
                Projectile.scale * 0.2f,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 10; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 2.75f;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + drawOffset,
                    null,
                    outlineColor with { A = 0 } * 0.48f * opacity,
                    Projectile.rotation,
                    origin,
                    needleScale,
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                bodyColor * opacity,
                Projectile.rotation,
                origin,
                needleScale,
                SpriteEffects.None,
                0f);

            PFLeftEffectRules.EndAdditive();
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

        private float TrailWidthFunction(float completion, Vector2 _) =>
            Utils.Remap(completion, 0f, 0.85f, Projectile.scale * 35f, 0f) * Projectile.Opacity;

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color bodyColor = Color.Lerp(VisualColor, Color.White, 0.22f);
            Color tailColor = Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.68f, 1f, completion, true));
            bodyColor.A = 0;
            tailColor.A = 0;
            return Color.Lerp(bodyColor, tailColor, completion) * (1f - completion * 0.32f);
        }

        private float TrailCoreWidthFunction(float completion, Vector2 _) =>
            Utils.Remap(completion, 0f, 0.82f, Projectile.scale * 13f, 0f) * Projectile.Opacity;

        private Color TrailCoreColorFunction(float completion, Vector2 _)
        {
            Color bodyColor = Color.Lerp(Color.White, VisualColor, 0.22f);
            Color tailColor = Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.74f, 1f, completion, true));
            bodyColor.A = 0;
            tailColor.A = 0;
            return Color.Lerp(bodyColor, tailColor, completion);
        }
    }
}
