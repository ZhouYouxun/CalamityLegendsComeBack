using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.DarksunFragment
{
    internal class DarksunFragmentEclipseBolt : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 72;
        private const float CoreVanishDistance = 10f;
        private Vector2 startPosition;
        private Vector2 targetPosition;
        private int ActualLifetime => Projectile.localAI[0] > 0f ? (int)Projectile.localAI[0] : Lifetime;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            startPosition = Projectile.Center;
            targetPosition = GetParentCenter();
            Projectile.rotation = (targetPosition - Projectile.Center).ToRotation();
        }

        public override void AI()
        {
            int actualLifetime = ActualLifetime;
            if (Projectile.localAI[0] > 0f && Projectile.timeLeft > actualLifetime)
                Projectile.timeLeft = actualLifetime;

            if (startPosition == Vector2.Zero)
            {
                startPosition = Projectile.Center;
                targetPosition = GetParentCenter();
            }

            targetPosition = GetParentCenter();
            float completion = 1f - Projectile.timeLeft / (float)actualLifetime;
            completion = MathHelper.Clamp(completion, 0f, 1f);

            Vector2 previous = Projectile.Center;
            Vector2 point = Vector2.Lerp(startPosition, targetPosition, completion);
            Projectile.Center = point;
            Projectile.velocity = point - previous;
            if (Projectile.velocity.LengthSquared() > 0.001f)
                Projectile.rotation = Projectile.velocity.ToRotation();

            if (Vector2.DistanceSquared(Projectile.Center, targetPosition) <= CoreVanishDistance * CoreVanishDistance || !ParentIsActive())
                Projectile.Kill();

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.25f, 0, Main.rand.NextBool() ? new Color(255, 205, 66) : Color.Black, Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.22f, Pitch = 0.22f, PitchVariance = 0.08f, MaxInstances = 5 }, target.Center);
            SpawnHitBurst(GetParentCenter());
        }

        private void SpawnHitBurst(Vector2 center)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                new Color(255, 202, 56),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.04f,
                0.34f,
                18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                new Color(22, 16, 4),
                "CalamityMod/Particles/PlasmaExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.03f,
                0.26f,
                15));

            for (int i = 0; i < 28; i++)
            {
                float angle = MathHelper.TwoPi * i / 28f;
                float rose = 0.75f + 0.35f * (float)Math.Sin(angle * 4f + Projectile.identity * 0.17f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(4.4f, 11.8f) * rose;
                Color color = i % 4 == 0 ? Color.Black : Color.Lerp(new Color(255, 212, 68), Color.White, Main.rand.NextFloat(0.25f));

                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    center + velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(3f, 18f),
                    velocity,
                    false,
                    Main.rand.Next(13, 24),
                    Main.rand.NextFloat(0.04f, 0.11f),
                    color,
                    new Vector2(2.6f, 0.46f),
                    true));
            }
        }

        private bool ParentIsActive()
        {
            int parent = (int)Projectile.ai[0];
            return parent >= 0 && parent < Main.maxProjectiles && Main.projectile[parent].active && Main.projectile[parent].type == ModContent.ProjectileType<DarksunFragmentBlackSun>();
        }

        private Vector2 GetParentCenter()
        {
            int parent = (int)Projectile.ai[0];
            if (parent >= 0 && parent < Main.maxProjectiles && Main.projectile[parent].active)
                return Main.projectile[parent].Center;

            return Projectile.Center;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, center, null, new Color(255, 205, 64, 0) * 0.85f, Projectile.rotation, bloom.Size() * 0.5f, 0.12f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        public float WidthFunc(float completionRatio, Vector2 trailPoint)
        {
            completionRatio = MathHelper.Clamp(completionRatio, 0f, 1f);
            float headFade = Utils.GetLerpValue(0f, 0.08f, completionRatio, true);
            float tailFade = Utils.GetLerpValue(1f, 0.68f, completionRatio, true);
            return 14f * headFade * tailFade;
        }

        public Color ColorFunc(float completionRatio, Vector2 trailPoint)
        {
            completionRatio = MathHelper.Clamp(completionRatio, 0f, 1f);
            Color dark = new(34, 21, 4, 220);
            Color gold = new(255, 198, 48, 0);
            Color color = Color.Lerp(gold, dark, Utils.GetLerpValue(0.1f, 0.55f, completionRatio, true));
            return color * Utils.GetLerpValue(1f, 0.72f, completionRatio, true);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    WidthFunc,
                    ColorFunc,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                30);
        }
    }
}
