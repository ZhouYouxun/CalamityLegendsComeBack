using System;
using System.Linq;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFEXVernalShot : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private static readonly Color BodyColor = new(164, 255, 106);
        private static readonly Color AccentColor = new(227, 255, 195);
        private static readonly Color TrailStartColor = new(72, 255, 120);
        private static readonly Color TrailMidColor = new(128, 255, 168);
        private static readonly Color TrailEndColor = new(232, 255, 220);

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/EXSkill/BFEXVernalShot";

        private ref float StoredSpeed => ref Projectile.localAI[0];
        private ref float SpiralSeed => ref Projectile.ai[0];
        private ref float BloomSeed => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            StoredSpeed = MathHelper.Max(Projectile.velocity.Length(), 18f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;

        public override void AI()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.velocity = forward * StoredSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;

            Lighting.AddLight(Projectile.Center, TrailStartColor.ToVector3() * 0.62f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center - Projectile.velocity * 0.12f,
                    -Projectile.velocity * 0.04f + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    Main.rand.NextFloat(0.16f, 0.24f),
                    Color.Lerp(TrailStartColor, Color.White, 0.2f),
                    Main.rand.Next(9, 13)));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.26f, Pitch = 0.3f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.34f, Pitch = -0.08f }, Projectile.Center);

            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center,
                Vector2.Zero,
                TrailStartColor,
                0.75f,
                18));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center + forward * 8f,
                forward * 1.6f,
                TrailStartColor * 0.9f,
                new Vector2(0.62f, 5.8f),
                forward.ToRotation(),
                0.22f,
                0.038f,
                24));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                TrailMidColor * 0.8f,
                new Vector2(0.48f, 3.6f),
                forward.ToRotation() + MathHelper.PiOver2,
                0.18f,
                0.032f,
                22));

            for (int i = 0; i < 8; i++)
            {
                Particle mist = new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    forward * Main.rand.NextFloat(1.2f, 4.2f) + Main.rand.NextVector2Circular(1.25f, 1.25f),
                    Main.rand.NextBool(3) ? TrailStartColor : TrailMidColor,
                    Color.Black,
                    Main.rand.NextFloat(0.7f, 1.15f),
                    Main.rand.Next(140, 210));
                GeneralParticleHandler.SpawnParticle(mist);
            }

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center,
                    forward * Main.rand.NextFloat(0.8f, 2.8f) + Main.rand.NextVector2Circular(1.4f, 1.4f),
                    Main.rand.NextFloat(0.24f, 0.34f),
                    Color.Lerp(TrailMidColor, Color.White, 0.35f),
                    Main.rand.Next(10, 14)));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D projectileTexture = TextureAssets.Projectile[Type].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.2f + BloomSeed);

            DrawHelix(drawPosition, forward);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Vector2 offset = angle.ToRotationVector2() * (1.1f + 0.55f * pulse);
                Main.EntitySpriteDraw(
                    projectileTexture,
                    drawPosition + offset,
                    null,
                    TrailStartColor * 0.28f,
                    Projectile.rotation,
                    projectileTexture.Size() * 0.5f,
                    Projectile.scale * (1.02f + 0.02f * pulse),
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                TrailStartColor * 0.68f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.42f * pulse,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition + forward * 2f,
                null,
                Color.White * 0.42f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.18f * pulse,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                projectileTexture,
                drawPosition,
                null,
                Color.Lerp(BodyColor, AccentColor, 0.35f) * 0.82f,
                Projectile.rotation,
                projectileTexture.Size() * 0.5f,
                Projectile.scale * (1.04f + 0.02f * pulse),
                SpriteEffects.None,
                0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Main.EntitySpriteDraw(
                projectileTexture,
                drawPosition,
                null,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                projectileTexture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        private void DrawHelix(Vector2 drawPosition, Vector2 forward)
        {
            Texture2D helixTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float time = Main.GlobalTimeWrappedHourly * 6.8f + SpiralSeed;
            const int helixCount = 8;

            for (int i = 0; i < helixCount; i++)
            {
                float progress = i / (float)(helixCount - 1);
                float helixAngle = time - progress * 2.35f + MathHelper.TwoPi * i / helixCount;
                Vector2 spiralOffset = right.RotatedBy(helixAngle) * MathHelper.Lerp(11f, 5f, progress);
                Vector2 backwardOffset = -forward * MathHelper.Lerp(4f, 30f, progress);
                Vector2 totalOffset = spiralOffset + backwardOffset;
                float opacity = MathHelper.Lerp(0.75f, 0.12f, progress);

                Main.EntitySpriteDraw(
                    helixTexture,
                    drawPosition + totalOffset,
                    null,
                    Color.Lerp(Color.Black, TrailMidColor, 0.45f) * opacity,
                    forward.ToRotation() - MathHelper.PiOver2,
                    helixTexture.Size() * 0.5f,
                    new Vector2(0.24f, MathHelper.Lerp(0.95f, 0.4f, progress)) * Projectile.scale,
                    SpriteEffects.None,
                    0);
            }
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(pos => pos != Vector2.Zero)
                .Select(pos => pos + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            return trailPoints;
        }

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float maxBodyWidth = Projectile.scale * 22f;
            float curveRatio = 0.16f;
            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
        }

        private float CoreTrailWidthFunction(float completion, Vector2 _) =>
            TrailWidthFunction(completion, Vector2.Zero) * 0.48f;

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color bodyColor = Color.Lerp(TrailStartColor, TrailMidColor, Utils.GetLerpValue(0f, 0.38f, completion, true));
            bodyColor = Color.Lerp(bodyColor, TrailEndColor, Utils.GetLerpValue(0.38f, 1f, completion, true));
            return Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.82f, 1f, completion, true));
        }

        private Color CoreTrailColorFunction(float completion, Vector2 _)
        {
            Color coreColor = Color.Lerp(Color.White, AccentColor, Utils.GetLerpValue(0f, 0.55f, completion, true));
            return Color.Lerp(coreColor, Color.Transparent, Utils.GetLerpValue(0.65f, 1f, completion, true));
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
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

            Vector2[] coreTrail = trailPoints.Take(Math.Min(10, trailPoints.Length)).ToArray();
            if (coreTrail.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                coreTrail,
                new PrimitiveSettings(
                    CoreTrailWidthFunction,
                    CoreTrailColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                coreTrail.Length * 2);
        }
    }
}
