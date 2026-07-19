using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    internal class FragmentEntropy_CosmicFire : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float Time => ref Projectile.ai[0];

        public Color InnerColor = Color.LightGreen;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 118;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Time++;

            Projectile.velocity *= 1.002f;

            float phase = Time * 0.19f + Projectile.identity * 0.41f;
            float wave = (float)System.Math.Sin(phase);

            if (Time > 12f && Time % 17f == 0f)
                Projectile.velocity = Projectile.velocity.RotatedBy(Main.rand.NextFloat(-0.035f, 0.035f));

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (Time < 26f)
                Projectile.position += side * wave * 0.22f;

            Lighting.AddLight(Projectile.Center, InnerColor.ToVector3() * 0.2f);

            Player owner = Main.player[Projectile.owner];
            float targetDistance = Vector2.Distance(owner.Center, Projectile.Center);

            SpawnCosmicFireFlightVisuals(targetDistance);
            SpawnCosmicFireVoidDustHelix(targetDistance, forward, side);
            SpawnStrangeEntropyFlicker(targetDistance, side);
        }

        private void SpawnCosmicFireFlightVisuals(float targetDistance)
        {
            if (Main.rand.NextBool(2) && Time >= 1f && targetDistance < 1400f)
            {
                Particle orb = new GenericBloom(
                    Projectile.Center + Main.rand.NextVector2CircularEdge(4f, 4f),
                    Projectile.velocity * Main.rand.NextFloat(0.05f, 0.5f),
                    Color.Black,
                    Main.rand.NextFloat(0.12f, 0.24f),
                    Main.rand.Next(7, 10),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(orb);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    ModContent.DustType<VoidDustInverted>());
                dust.scale = Main.rand.NextFloat(0.36f, 0.72f);
                dust.velocity = new Vector2(0f, Main.rand.NextFloat(0.1f, 5f));
                dust.noGravity = false;
                dust.color = InnerColor;
            }

            if (Time >= 1f && targetDistance < 1400f)
            {
                Particle spark = new CustomSpark(
                    Projectile.Center,
                    -Projectile.velocity * 0.05f,
                    "CalamityMod/Particles/GlowSpark2",
                    false,
                    13,
                    0.062f,
                    Color.Black,
                    new Vector2(0.6f, 1.3f),
                    false);
                GeneralParticleHandler.SpawnParticle(spark);

                Particle spark2 = new CustomSpark(
                    Projectile.Center,
                    -Projectile.velocity * 0.05f,
                    "CalamityMod/Particles/GlowSpark",
                    false,
                    13,
                    0.032f,
                    InnerColor,
                    new Vector2(0.6f, 1.3f),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(spark2);
                spark2.DrawLayer = GeneralDrawLayer.AfterEverything;
            }

            if (Time == 1f)
            {
                for (int i = 0; i <= 10; i++)
                {
                    float variance = Main.rand.NextFloat(-0.7f, 0.7f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                    dust.scale = (Main.rand.NextFloat(1.7f, 1.9f) - System.Math.Abs(variance)) * 0.6f;
                    dust.velocity = (Projectile.velocity * 2f).RotatedBy(variance) * Main.rand.NextFloat(0.35f, 1f) * (1f - System.Math.Abs(variance));
                    dust.noGravity = true;
                    dust.color = InnerColor;
                }
            }
        }

        private void SpawnCosmicFireVoidDustHelix(float targetDistance, Vector2 forward, Vector2 side)
        {
            if (targetDistance >= 1400f)
                return;

            float age = Utils.GetLerpValue(0f, 34f, Time, true);
            float radius = MathHelper.Lerp(5f, 19f, age);
            float basePhase = Time * 0.92f + Projectile.velocity.ToRotation() * 0.45f + Projectile.identity * 0.37f;

            for (int sample = 0; sample < 3; sample++)
            {
                float sampleOffset = sample * 0.32f;
                float phase = basePhase - sampleOffset;
                float sine = (float)System.Math.Sin(phase);
                float cosine = (float)System.Math.Cos(phase);
                Vector2 sampleCenter = Projectile.Center - forward * Projectile.velocity.Length() * 0.16f * sample;

                for (int i = 0; i < 2; i++)
                {
                    float sign = i == 0 ? 1f : -1f;
                    Vector2 helixPos =
                        sampleCenter +
                        side * sine * radius * sign +
                        forward * cosine * 5f * sign;

                    Dust dust = Dust.NewDustPerfect(
                        helixPos + Main.rand.NextVector2Circular(7f, 7f),
                        ModContent.DustType<VoidDustInverted>());
                    dust.scale = Main.rand.NextFloat(0.36f, 0.72f) * 2f;
                    dust.velocity = new Vector2(0f, Main.rand.NextFloat(0.1f, 5f));
                    dust.noGravity = false;
                    dust.color = InnerColor;
                }
            }
        }

        private void SpawnStrangeEntropyFlicker(float targetDistance, Vector2 side)
        {
            if (targetDistance >= 1400f || !Main.rand.NextBool(4))
                return;

            float offset = (float)System.Math.Sin(Time * 0.31f + Projectile.identity) * Main.rand.NextFloat(4f, 13f);
            Color sicklyColor = Color.Lerp(InnerColor, new Color(170, 70, 255), 0.22f);

            Particle warpedSpark = new CustomSpark(
                Projectile.Center + side * offset,
                -Projectile.velocity.RotatedByRandom(0.18f) * Main.rand.NextFloat(0.025f, 0.09f),
                Main.rand.NextBool(2) ? "CalamityMod/Particles/GlowSpark2" : "CalamityMod/Particles/GlowSpark",
                false,
                Main.rand.Next(10, 16),
                Main.rand.NextFloat(0.011f, 0.021f),
                Main.rand.NextBool(3) ? Color.Black : sicklyColor,
                new Vector2(Main.rand.NextFloat(0.45f, 0.85f), Main.rand.NextFloat(1.0f, 1.65f)),
                true,
                false);
            GeneralParticleHandler.SpawnParticle(warpedSpark);
            warpedSpark.DrawLayer = GeneralDrawLayer.AfterEverything;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SpawnCosmicFireImpactVisuals(Projectile.Center);

            if (Projectile.owner != Main.myPlayer)
                return;

            int explosionIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(Projectile.damage * 3.25f),
                Projectile.knockBack * 1.5f,
                Projectile.owner,
                0f,
                0f);

            if (Main.projectile.IndexInRange(explosionIndex))
            {
                Projectile explosion = Main.projectile[explosionIndex];
                explosion.width = 500;
                explosion.height = 500;
                explosion.Center = Projectile.Center;
                explosion.friendly = true;
                explosion.hostile = false;
                explosion.DamageType = DamageClass.Magic;
                explosion.netUpdate = true;
            }
        }

        private void SpawnCosmicFireImpactVisuals(Vector2 center)
        {
            float power = 1.18f;
            Color entropyGreen = InnerColor;
            entropyGreen.A = 0;
            Color sicklyWhite = Color.Lerp(Color.White, InnerColor, 0.28f);
            sicklyWhite.A = 0;

            Particle greenBloom = new CustomPulse(
                center,
                Vector2.Zero,
                entropyGreen,
                "CalamityMod/Particles/LargeBloom",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                0.82f * power,
                11);
            GeneralParticleHandler.SpawnParticle(greenBloom);
            greenBloom.DrawLayer = GeneralDrawLayer.AfterEverything;

            Particle whiteBloom = new CustomPulse(
                center,
                Vector2.Zero,
                sicklyWhite,
                "CalamityMod/Particles/LargeBloom",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                0.74f * power,
                11);
            GeneralParticleHandler.SpawnParticle(whiteBloom);
            whiteBloom.DrawLayer = GeneralDrawLayer.AfterEverything;

            Particle bloomRing = new CustomPulse(
                center,
                Vector2.Zero,
                InnerColor,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                3.32f * power,
                18);
            GeneralParticleHandler.SpawnParticle(bloomRing);
            bloomRing.DrawLayer = GeneralDrawLayer.AfterEverything;

            Particle softExplosion = new CustomPulse(
                center,
                Vector2.Zero,
                InnerColor,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                0.3f * power,
                30);
            GeneralParticleHandler.SpawnParticle(softExplosion);
            softExplosion.DrawLayer = GeneralDrawLayer.AfterEverything;

            Particle largerSoftExplosion = new CustomPulse(
                center,
                Vector2.Zero,
                InnerColor * 0.55f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                0.405f * power,
                30);
            GeneralParticleHandler.SpawnParticle(largerSoftExplosion);
            largerSoftExplosion.DrawLayer = GeneralDrawLayer.AfterEverything;

            for (int i = 0; i < 2; i++)
            {
                Particle blackCore = new CustomPulse(
                    center,
                    Vector2.Zero,
                    Color.Black,
                    "CalamityMod/Particles/SmallBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    (2.2f + i * 0.5f) * power,
                    0f,
                    80,
                    false);
                GeneralParticleHandler.SpawnParticle(blackCore);
            }

            for (int i = 0; i < 26; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, ModContent.DustType<VoidDustInverted>());
                dust.noGravity = true;
                dust.velocity = new Vector2(21f, 21f).RotatedByRandom(100f) * Main.rand.NextFloat(0.1f, 1f) * power;
                dust.scale = Main.rand.NextFloat(1.4f, 2.2f) * power;
                dust.color = InnerColor;
            }

            for (int i = 0; i < 34; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.FireworksRGB);
                dust.noGravity = false;
                dust.velocity = new Vector2(18f, 18f).RotatedByRandom(100f) * Main.rand.NextFloat(0.3f, 1f) * power;
                dust.scale = Main.rand.NextFloat(0.8f, 1.2f);
                dust.color = InnerColor;
            }

            Main.player[Projectile.owner].SetScreenshake(9f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") { Volume = 0.95f, Pitch = -0.12f }, center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = -0.25f }, center);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
