using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash
{
    // The Xyk death animation's language translated into Briny Baron's sea-blue palette:
    // a short afterimage collapse, radial energy sparks, then a compact blue phase burst.
    internal sealed class BrinyBaron_VortexEyePhaseResidue : ModProjectile
    {
        private const int Lifetime = 32;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Main.dedServ)
                return;

            int age = Lifetime - Projectile.timeLeft;
            Color core = Color.Lerp(new Color(45, 180, 255), Color.Cyan, 0.42f);
            Color edge = Color.Lerp(Color.DodgerBlue, Color.RoyalBlue, 0.35f);

            if (age == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(9f, 9f);
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        Projectile.Center,
                        velocity,
                        true,
                        18,
                        Main.rand.NextFloat(0.55f, 0.9f),
                        core,
                        true));
                }
            }

            if (age < 18 && age % 3 == 0)
            {
                Vector2 inward = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(22f, 58f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + inward, DustID.BlueTorch, -inward * 0.13f, 40, core, Main.rand.NextFloat(1.05f, 1.45f));
                dust.noGravity = true;
            }

            if (age == 18)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    Projectile.Center, Vector2.Zero, edge * 0.8f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One,
                    Main.rand.NextFloat(MathHelper.TwoPi), 0.035f, 0.21f, 13, true));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    Projectile.Center, Vector2.Zero, core * 0.78f,
                    "CalamityMod/Particles/BloomCircle", Vector2.One,
                    Main.rand.NextFloat(MathHelper.TwoPi), 0.05f, 0.38f, 15, true));

                for (int i = 0; i < 14; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        Projectile.Center, velocity, false, Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.25f, 0.55f), i % 2 == 0 ? core : edge,
                        Vector2.One * Main.rand.NextFloat(0.45f, 0.78f), true, false));
                }

                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = 0.16f }, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    internal static class BrinyBaronVortexEyeTeleportEffects
    {
        public static void SpawnDeparture(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Projectile.NewProjectile(
                new EntitySource_Misc("BrinyBaronVortexEyeDeparture"),
                position, Vector2.Zero,
                ModContent.ProjectileType<BrinyBaron_VortexEyePhaseResidue>(),
                0, 0f, Main.myPlayer);

            Color blue = Color.Lerp(Color.DodgerBlue, Color.Cyan, 0.45f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(1.1f) * Main.rand.NextFloat(4f, 13f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    position + Main.rand.NextVector2Circular(12f, 12f), velocity * 0.1f,
                    false, Main.rand.Next(12, 19), Main.rand.NextFloat(0.22f, 0.46f), blue, true, false, true));
            }

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.65f, Pitch = -0.15f }, position);
        }

        public static void SpawnArrival(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Color core = new Color(90, 220, 255);
            Color edge = Color.DodgerBlue;
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                position, Vector2.Zero, edge * 0.85f,
                "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One,
                direction.ToRotation(), 0.025f, 0.16f, 12, true));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                position, Vector2.Zero, core * 0.75f,
                "CalamityMod/Particles/BloomCircle", Vector2.One,
                direction.ToRotation(), 0.04f, 0.34f, 13, true));

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.85f) * Main.rand.NextFloat(4f, 16f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    position, velocity, true, Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.42f, 0.78f), i % 2 == 0 ? core : edge, true));
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = 0.2f }, position);
        }
    }
}
