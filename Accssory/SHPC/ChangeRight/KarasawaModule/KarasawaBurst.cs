#if KARASAWA_MODULE_ENABLED
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.KarasawaModule
{
    internal sealed class KarasawaBurst : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int AfterHitBlastSize = 800;

        private bool detonated;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float ReleaseRatio => MathHelper.Clamp(Projectile.ai[0], 0f, 1f);
        private bool FullyCharged => Projectile.ai[1] != -1f;

        private ref float VisualSize => ref Projectile.localAI[0];
        private ref float FrameCounter => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 34;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = false;
            Projectile.MaxUpdates = 49;
            Projectile.penetrate = 1;
            Projectile.alpha = 100;
            Projectile.timeLeft = 3000;
            Projectile.tileCollide = false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            VisualSize = MathHelper.Lerp(1.5f, 2.85f, ReleaseRatio);
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, BeamColor().ToVector3() * MathHelper.Lerp(0.55f, 1.15f, ReleaseRatio));

            if (FrameCounter >= 15f)
            {
                if (FrameCounter >= 25f)
                    VisualSize = Math.Max(1.5f, VisualSize * 0.992f);

                if (Projectile.numUpdates % 7 == 0)
                    SpawnFlightEffects();

                if (FullyCharged)
                    Projectile.ai[1] += 0.1f;
            }

            if (FrameCounter <= 30f)
                FrameCounter++;
        }

        public override bool PreDraw(ref Color lightColor) => false;

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
                    OuterTrailWidth,
                    OuterTrailColor,
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                trailPoints.Length * 2);

            Vector2[] coreTrail = trailPoints.Take(Math.Min(12, trailPoints.Length)).ToArray();
            if (coreTrail.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                coreTrail,
                new PrimitiveSettings(
                    CoreTrailWidth,
                    CoreTrailColor,
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                coreTrail.Length * 2);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DefenseEffectiveness *= 0f;

            float calamityDamageReduction = MathHelper.Clamp(target.Calamity().DR, 0f, 0.95f);
            if (calamityDamageReduction < 0.95f)
                modifiers.FinalDamage /= 1f - calamityDamageReduction;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 impactCenter = Projectile.Center;
            SpawnImpactEffects(impactCenter);

            if (FullyCharged)
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);

            if (detonated)
                return;

            detonated = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 5;
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            Projectile.width = AfterHitBlastSize;
            Projectile.height = AfterHitBlastSize;
            Projectile.Center = impactCenter;
            Projectile.netUpdate = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!detonated)
                SpawnFadeoutSparks();
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

        private float OuterTrailWidth(float completion, Vector2 _)
        {
            float maxWidth = MathHelper.Lerp(46f, 92f, ReleaseRatio) * VisualSize;
            return completion < 0.18f
                ? MathF.Sin(completion / 0.18f * MathHelper.PiOver2) * maxWidth + 0.18f
                : Utils.Remap(completion, 0.18f, 1f, maxWidth, 0f);
        }

        private float CoreTrailWidth(float completion, Vector2 _)
        {
            float maxWidth = MathHelper.Lerp(19f, 42f, ReleaseRatio) * VisualSize;
            return completion < 0.15f
                ? MathF.Sin(completion / 0.15f * MathHelper.PiOver2) * maxWidth + 0.15f
                : Utils.Remap(completion, 0.15f, 1f, maxWidth, 0f);
        }

        private Color OuterTrailColor(float completion, Vector2 _)
        {
            Color start = BeamColor();
            Color end = FullyCharged ? new Color(255, 74, 54) : new Color(86, 225, 255);
            Color body = Color.Lerp(start, end, completion * 0.55f) * Projectile.Opacity;
            body.A = 0;
            return Color.Lerp(body, Color.Transparent, Utils.GetLerpValue(0.72f, 1f, completion, true));
        }

        private Color CoreTrailColor(float completion, Vector2 _)
        {
            Color body = Color.Lerp(Color.White, Color.Lerp(new Color(170, 250, 255), new Color(255, 210, 170), ReleaseRatio), completion * 0.55f) * Projectile.Opacity;
            body.A = 0;
            return Color.Lerp(body, Color.Transparent, Utils.GetLerpValue(0.76f, 1f, completion, true));
        }

        private Color BeamColor()
        {
            return Color.Lerp(new Color(70, 210, 255), new Color(255, 68, 54), ReleaseRatio);
        }

        private void SpawnFlightEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Color color = BeamColor();

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = right * Main.rand.NextFloat(-12f, 12f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset + Main.rand.NextVector2Circular(5f, 5f), DustID.RainbowMk2);
                dust.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.5f, 1.4f) + Main.rand.NextVector2Circular(1.4f, 1.4f);
                dust.scale = VisualSize * Main.rand.NextFloat(0.85f, 1.45f);
                dust.color = Color.Lerp(color, Color.White, Main.rand.NextFloat(0.15f, 0.65f));
                dust.color.A = 0;
                dust.noGravity = true;
            }

            float squish = FrameCounter > 30f ? FrameCounter / 6.67f : 1.5f;
            Particle glow = new SquishyLightParticle(
                Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.8f, 1.6f),
                Main.rand.NextFloat(0.9f, VisualSize * 1.45f),
                color,
                Main.rand.Next(24, 34),
                0.2f,
                squish);
            GeneralParticleHandler.SpawnParticle(glow);

            if (!FullyCharged)
                return;

            float phase = Projectile.ai[1];
            Vector2 helixPos = Projectile.Center + right * MathF.Sin(phase) * 18f;
            Particle helix = new SquishyLightParticle(
                helixPos,
                Main.rand.NextVector2Circular(0.5f, 0.5f),
                0.65f,
                Color.Lerp(color, Color.White, 0.3f),
                Main.rand.Next(24, 34),
                0.2f,
                squish,
                10);
            GeneralParticleHandler.SpawnParticle(helix);
        }

        private void SpawnImpactEffects(Vector2 impactCenter)
        {
            if (Main.dedServ)
                return;

            Color color = BeamColor();
            Color white = Color.Lerp(color, Color.White, 0.5f);

            for (int i = 0; i < 70; i++)
            {
                Dust dust = Dust.NewDustPerfect(impactCenter, DustID.RainbowMk2, Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 34f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.5f, 2.2f);
                dust.color = Color.Lerp(color, white, Main.rand.NextFloat(0.15f, 0.85f));
                dust.color.A = 0;
            }

            for (int i = 0; i < 45; i++)
            {
                SquishyLightParticle fire = new(
                    impactCenter,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(7f, 13f),
                    Main.rand.NextFloat(0.8f, 1.3f),
                    color,
                    64,
                    1.4f,
                    2.7f,
                    3f);
                GeneralParticleHandler.SpawnParticle(fire);
            }

            for (int i = 0; i < 22; i++)
            {
                SquishyLightParticle fire = new(
                    impactCenter,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 9f),
                    Main.rand.NextFloat(1.6f, 2.4f),
                    white,
                    64,
                    1.4f,
                    2.7f,
                    3f);
                GeneralParticleHandler.SpawnParticle(fire);
            }
        }

        private void SpawnFadeoutSparks()
        {
            if (Main.dedServ)
                return;

            Color color = BeamColor();
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.RainbowMk2);
                dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
                dust.color = Color.Lerp(color, Color.White, Main.rand.NextFloat(0.2f, 0.8f));
                dust.color.A = 0;
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.9f, 1.4f);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(detonated);
            writer.Write(VisualSize);
            writer.Write(FrameCounter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            detonated = reader.ReadBoolean();
            VisualSize = reader.ReadSingle();
            FrameCounter = reader.ReadSingle();
        }
    }
}
#endif
