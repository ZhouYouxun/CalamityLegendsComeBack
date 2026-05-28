using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    public class M61Bullet : ModProjectile, ILocalizedModType
    {
        private static readonly Color CoreColor = new(215, 248, 255);
        private static readonly Color TrailBlue = new(76, 190, 255);
        private static readonly Color ImpactGold = new(255, 208, 88);

        public new string LocalizationCategory => "Projectiles.MK14EBR";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/MK14EBR/M61Bullet";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = Projectile.direction;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float speedRatio = MathHelper.Clamp(Projectile.velocity.Length() / 22f, 0.45f, 1.25f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.52f) * speedRatio);

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] <= 2f)
                return;

            SpawnFlightVisuals(direction, speedRatio);
        }

        private void SpawnFlightVisuals(Vector2 direction, float speedRatio)
        {
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);

            if (Main.rand.NextBool(2))
            {
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(2f, 10f) + Main.rand.NextVector2Circular(1.6f, 1.6f),
                    DustID.Electric,
                    -direction.RotatedByRandom(0.16f) * Main.rand.NextFloat(0.8f, 2.4f),
                    120,
                    Main.rand.NextBool(3) ? ImpactGold : TrailBlue,
                    Main.rand.NextFloat(0.45f, 0.74f));
                spark.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Dust tracer = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(8f, 18f) + perpendicular * Main.rand.NextFloat(-2.2f, 2.2f),
                    DustID.GemDiamond,
                    -direction * Main.rand.NextFloat(0.45f, 1.2f),
                    100,
                    Color.Lerp(CoreColor, TrailBlue, Main.rand.NextFloat(0.2f, 0.8f)),
                    Main.rand.NextFloat(0.36f, 0.58f));
                tracer.noGravity = true;
                tracer.noLight = true;
            }

            if (Projectile.localAI[0] % 3f == 0f)
            {
                Particle line = new LineParticle(
                    Projectile.Center - direction * 8f,
                    -direction * Main.rand.NextFloat(0.05f, 0.18f),
                    false,
                    Main.rand.Next(5, 8),
                    Main.rand.NextFloat(0.46f, 0.72f) * speedRatio,
                    Color.Lerp(TrailBlue, CoreColor, Main.rand.NextFloat(0.25f, 0.9f)));
                GeneralParticleHandler.SpawnParticle(line);
            }

            if (Main.rand.NextBool(4))
            {
                Particle glow = new CustomSpark(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 14f),
                    -direction.RotatedByRandom(0.08f) * Main.rand.NextFloat(0.4f, 1.4f),
                    "CalamityMod/Particles/GlowSpark",
                    false,
                    Main.rand.Next(5, 8),
                    Main.rand.NextFloat(0.045f, 0.07f) * speedRatio,
                    Color.Lerp(CoreColor, TrailBlue, Main.rand.NextFloat(0.2f, 0.75f)),
                    new Vector2(Main.rand.NextFloat(1.4f, 2.0f), Main.rand.NextFloat(0.28f, 0.42f)),
                    true,
                    true,
                    0f,
                    false,
                    false,
                    0.68f);
                GeneralParticleHandler.SpawnParticle(glow);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 textureOrigin = new(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 bloomOrigin = new(bloom.Width * 0.5f, bloom.Height * 0.5f);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                float fade = completion * completion;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color afterimageColor = Color.Lerp(TrailBlue, CoreColor, completion) * (0.08f + fade * 0.38f);
                afterimageColor.A = 0;

                Main.EntitySpriteDraw(
                    bloom,
                    drawPosition,
                    null,
                    afterimageColor * 0.72f,
                    Projectile.rotation,
                    bloomOrigin,
                    new Vector2(0.12f + fade * 0.05f, 0.035f + fade * 0.012f) * Projectile.scale,
                    SpriteEffects.None,
                    0f);

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    afterimageColor,
                    Projectile.rotation,
                    textureOrigin,
                    Projectile.scale * (0.72f + completion * 0.22f),
                    SpriteEffects.None,
                    0f);
            }

            Color glowColor = CoreColor * 0.88f;
            glowColor.A = 0;
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                glowColor,
                Projectile.rotation,
                bloomOrigin,
                new Vector2(0.15f, 0.044f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                textureOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
            SpawnImpactVisuals(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), true);
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.numHits <= 0)
                SpawnImpactVisuals(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), false);
        }

        private void SpawnImpactVisuals(Vector2 center, Vector2 direction, bool hitTarget)
        {
            SoundEngine.PlaySound(SoundID.Dig with { Volume = hitTarget ? 0.18f : 0.25f, Pitch = 0.25f }, center);

            Particle pulse = new DirectionalPulseRing(
                center,
                Vector2.Zero,
                Color.Lerp(TrailBlue, CoreColor, 0.35f) * (hitTarget ? 0.72f : 0.52f),
                Vector2.One,
                direction.ToRotation(),
                0.05f,
                hitTarget ? 0.38f : 0.28f,
                12);
            GeneralParticleHandler.SpawnParticle(pulse);

            Particle coreFlash = new CustomPulse(
                center,
                Vector2.Zero,
                Color.Lerp(CoreColor, ImpactGold, 0.22f) * 0.68f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.08f,
                hitTarget ? 0.34f : 0.24f,
                10,
                true);
            GeneralParticleHandler.SpawnParticle(coreFlash);

            int dustCount = hitTarget ? 10 : 7;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.9f) * Main.rand.NextFloat(1.6f, 5.8f);
                if (Main.rand.NextBool())
                    velocity *= -0.65f;

                Dust spark = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextBool(3) ? DustID.GemDiamond : DustID.Electric,
                    velocity,
                    80,
                    Main.rand.NextBool(3) ? ImpactGold : Color.Lerp(TrailBlue, CoreColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.58f, 1.05f));
                spark.noGravity = true;
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.55f) * Main.rand.NextFloat(2.8f, 6.2f);
                Particle shard = new SparkParticle(
                    center + Main.rand.NextVector2Circular(2f, 2f),
                    velocity,
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.38f, 0.62f),
                    Main.rand.NextBool() ? CoreColor : ImpactGold);
                GeneralParticleHandler.SpawnParticle(shard);
            }
        }
    }
}
