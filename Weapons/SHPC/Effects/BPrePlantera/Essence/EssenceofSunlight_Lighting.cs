using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSunlight_Lighting : ModProjectile, ILocalizedModType
    {
        private static readonly Color[] SunlightTrailPalette =
        {
            new(255, 255, 185),
            new(255, 225, 95),
            new(255, 168, 54),
            new(255, 238, 132)
        };

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;
        private Vector2 oldLightPosition = Vector2.Zero;
        private Color sparkColor = new(255, 230, 120);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.TrailCacheLength[Type] = 36;
        }

        public override void SetDefaults()
        {
            Projectile.width = 25; // Reduced by 30% (originally 36)
            Projectile.height = 25; // Reduced by 30% (originally 36)
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 7;
            Projectile.timeLeft = 30;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 127;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            timer++;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), 40f, 0.085f);

            Projectile.velocity = forward * speed;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = MathHelper.Lerp(0.001f, 1f, Utils.GetLerpValue(0f, 10f, timer, true));
            Lighting.AddLight(Projectile.Center, new Color(255, 220, 100).ToVector3() * 0.42f);

            UpdateSparkColor();
            SpawnExoLightStyleFlightEffects(forward);
        }

        private void UpdateSparkColor()
        {
            float rate = Main.GlobalTimeWrappedHourly * 8f + Projectile.identity * 0.11f;
            int colorIndex = (int)(rate / 2f % SunlightTrailPalette.Length);
            Color currentColor = SunlightTrailPalette[colorIndex];
            Color nextColor = SunlightTrailPalette[(colorIndex + 1) % SunlightTrailPalette.Length];
            sparkColor = Color.Lerp(currentColor, nextColor, rate % 2f > 1f ? 1f : rate % 1f);
        }

        private void SpawnExoLightStyleFlightEffects(Vector2 forward)
        {
            float side = Projectile.ai[1] == 0f ? 1f : MathHelper.Clamp(Projectile.ai[1], -1f, 1f);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 waveOffset = normal * side * (float)System.Math.Sin(timer * 0.28f + Projectile.identity * 0.17f) * 5f;

            if (oldLightPosition != Vector2.Zero)
            {
                Vector2 trailDirection = (oldLightPosition - Projectile.Center).SafeNormalize(-forward);
                Vector2 bloomPosition = Projectile.Center + waveOffset;

                Particle beam = new CustomSpark(
                    bloomPosition,
                    trailDirection,
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    13,
                    0.2f,
                    sparkColor,
                    new Vector2(1f, 1f),
                    true,
                    true,
                    0,
                    false,
                    false,
                    0.3f);
                GeneralParticleHandler.SpawnParticle(beam);
            }

            oldLightPosition = Projectile.Center;

            if (timer % 3 == 0)
            {
                Particle tailSpark = new GlowSparkParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(3f, 9f) - waveOffset * 0.35f,
                    -forward * Main.rand.NextFloat(0.9f, 2.3f) + normal * Main.rand.NextFloat(-0.25f, 0.25f),
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.055f, 0.085f),
                    Color.Lerp(sparkColor, Color.White, Main.rand.NextFloat(0.08f, 0.22f)),
                    new Vector2(0.9f, 0.22f),
                    true,
                    false,
                    1f);
                GeneralParticleHandler.SpawnParticle(tailSpark);
            }

            if (timer % 5 == 0)
            {
                Particle pulse = new DirectionalPulseRing(
                    Projectile.Center - forward * 5f,
                    Projectile.velocity * 0.12f,
                    sparkColor * 0.38f,
                    new Vector2(0.75f, 1.85f),
                    Projectile.rotation - MathHelper.PiOver2,
                    0.035f,
                    0.004f,
                    11);
                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        //public override bool? CanDamage() => timer > 1;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = target.Center;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            //Particle hitRing = new DirectionalPulseRing(
            //    pos,
            //    Vector2.Zero,
            //    new Color(255, 230, 100),
            //    new Vector2(1f, 1f),
            //    0f,
            //    0.1f,
            //    3.0f,
            //    16
            //);
            //GeneralParticleHandler.SpawnParticle(hitRing);

            for (int i = 0; i < 6; i++)
            {
                Vector2 sparkVel = Main.rand.NextVector2Circular(3f, 3f) + forward * Main.rand.NextFloat(2f, 5f);
                CritSpark hitSpark = new CritSpark(
                    pos,
                    sparkVel,
                    Color.White,
                    new Color(255, 200, 80),
                    Main.rand.NextFloat(0.8f, 1.2f),
                    18
                );
                GeneralParticleHandler.SpawnParticle(hitSpark);
            }

            for (int i = 0; i < 12; i++)
            {
                Particle core = new GlowSparkParticle(
                    pos,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    false,
                    6,
                    0.2f,
                    new Color(255, 240, 150),
                    new Vector2(0.95f, 0.32f),
                    true,
                    false,
                    1);
                GeneralParticleHandler.SpawnParticle(core);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = forward.RotatedByRandom(MathHelper.ToRadians(4.8f)) * Main.rand.NextFloat(4.8f, 9.6f);
                Particle jet = new GlowSparkParticle(
                    pos,
                    vel,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.12f, 0.2f),
                    new Color(255, 220, 100),
                    new Vector2(1.55f, 0.26f),
                    true,
                    false,
                    1);

                GeneralParticleHandler.SpawnParticle(jet);
            }

            SoundEngine.PlaySound(SoundID.Item94, pos);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
