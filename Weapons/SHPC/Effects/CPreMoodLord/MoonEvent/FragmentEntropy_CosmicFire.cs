using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
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
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
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

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.velocity *= 1.002f;

            float phase = Time * 0.19f + Projectile.identity * 0.41f;
            float wave = (float)System.Math.Sin(phase);
            if (Time < 26f)
                Projectile.position += side * wave * 0.22f;

            if (Time > 12f && Time % 17f == 0f)
                Projectile.velocity = Projectile.velocity.RotatedBy(Main.rand.NextFloat(-0.035f, 0.035f));

            Lighting.AddLight(Projectile.Center, InnerColor.ToVector3() * 0.2f);

            Player owner = Main.player[Projectile.owner];
            float targetDistance = Vector2.Distance(owner.Center, Projectile.Center);

            SpawnCosmicFireFlightVisuals(targetDistance);
            SpawnStrangeEntropyFlicker(targetDistance, side);
        }

        private void SpawnCosmicFireFlightVisuals(float targetDistance)
        {
            if (Main.rand.NextBool(5) && Time > 12f && targetDistance < 1400f)
            {
                Particle orb = new GenericBloom(
                    Projectile.Center + Main.rand.NextVector2CircularEdge(5f, 5f),
                    Projectile.velocity * Main.rand.NextFloat(0.05f, 0.5f),
                    Color.Black,
                    Main.rand.NextFloat(0.2f, 0.4f),
                    Main.rand.Next(9, 12),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(orb);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    ModContent.DustType<VoidDustInverted>());
                dust.scale = Main.rand.NextFloat(0.6f, 1.2f);
                dust.velocity = new Vector2(0f, Main.rand.NextFloat(0.1f, 5f));
                dust.noGravity = false;
                dust.color = InnerColor;
            }

            if (Projectile.timeLeft % 2 == 0 && Time > 12f && targetDistance < 1400f)
            {
                Particle spark = new CustomSpark(
                    Projectile.Center,
                    -Projectile.velocity * 0.05f,
                    "CalamityMod/Particles/GlowSpark2",
                    false,
                    17,
                    0.052f,
                    Color.Black,
                    new Vector2(0.6f, 1.3f),
                    false);
                GeneralParticleHandler.SpawnParticle(spark);

                Particle spark2 = new CustomSpark(
                    Projectile.Center,
                    -Projectile.velocity * 0.05f,
                    "CalamityMod/Particles/GlowSpark",
                    false,
                    17,
                    0.027f,
                    InnerColor,
                    new Vector2(0.6f, 1.3f),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(spark2);
                spark2.DrawLayer = GeneralDrawLayer.AfterEverything;
            }

            if (Time == 9f)
            {
                for (int i = 0; i <= 10; i++)
                {
                    float variance = Main.rand.NextFloat(-0.7f, 0.7f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                    dust.scale = Main.rand.NextFloat(1.7f, 1.9f) - System.Math.Abs(variance);
                    dust.velocity = (Projectile.velocity * 2f).RotatedBy(variance) * Main.rand.NextFloat(0.35f, 1f) * (1f - System.Math.Abs(variance));
                    dust.noGravity = true;
                    dust.color = InnerColor;
                }
            }
        }

        private void SpawnStrangeEntropyFlicker(float targetDistance, Vector2 side)
        {
            if (Time <= 12f || targetDistance >= 1400f || !Main.rand.NextBool(9))
                return;

            float offset = (float)System.Math.Sin(Time * 0.31f + Projectile.identity) * Main.rand.NextFloat(4f, 13f);
            Color sicklyColor = Color.Lerp(InnerColor, new Color(170, 70, 255), 0.22f);

            Particle warpedSpark = new CustomSpark(
                Projectile.Center + side * offset,
                -Projectile.velocity.RotatedByRandom(0.18f) * Main.rand.NextFloat(0.025f, 0.09f),
                Main.rand.NextBool() ? "CalamityMod/Particles/GlowSpark" : "CalamityMod/Particles/GlowSpark2",
                false,
                Main.rand.Next(12, 19),
                Main.rand.NextFloat(0.018f, 0.035f),
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
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            if (Main.projectile.IndexInRange(explosionIndex))
            {
                Projectile explosion = Main.projectile[explosionIndex];
                explosion.width = 170;
                explosion.height = 170;
                explosion.Center = Projectile.Center;
                explosion.netUpdate = true;
            }
        }

        private void SpawnCosmicFireImpactVisuals(Vector2 center)
        {
            for (int i = 0; i < 2; i++)
            {
                Particle blastRing = new CustomPulse(
                    center,
                    Vector2.Zero,
                    Color.Black,
                    "CalamityMod/Particles/LargeBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    0.35f,
                    0.4f,
                    38,
                    false);
                GeneralParticleHandler.SpawnParticle(blastRing);
            }

            for (int i = 0; i < 3; i++)
            {
                Particle blastRing = new CustomPulse(
                    center,
                    Vector2.Zero,
                    InnerColor,
                    "CalamityMod/Particles/BloomCircle",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    0.48f,
                    0.52f,
                    38);
                GeneralParticleHandler.SpawnParticle(blastRing);
                blastRing.DrawLayer = GeneralDrawLayer.AfterEverything;
            }

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, ModContent.DustType<VoidDustInverted>());
                dust.noGravity = true;
                dust.velocity = new Vector2(5f, 5f).RotatedByRandom(100f) * Main.rand.NextFloat(0.3f, 1f);
                dust.scale = Main.rand.NextFloat(1.6f, 2.2f);
                dust.color = InnerColor;
            }

            for (int i = 0; i < 4; i++)
            {
                Particle warpedRing = new CustomPulse(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    Vector2.Zero,
                    Main.rand.NextBool(3) ? Color.Black : Color.Lerp(InnerColor, new Color(170, 70, 255), 0.25f),
                    "CalamityMod/Particles/BloomCircle",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    Main.rand.NextFloat(0.16f, 0.3f),
                    Main.rand.NextFloat(0.24f, 0.45f),
                    Main.rand.Next(18, 28),
                    false);
                GeneralParticleHandler.SpawnParticle(warpedRing);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
