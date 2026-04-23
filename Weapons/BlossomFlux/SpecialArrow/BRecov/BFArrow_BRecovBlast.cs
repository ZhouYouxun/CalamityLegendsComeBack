using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFArrow_BRecovBlast : ModProjectile
    {
        private bool initialized;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            if (initialized)
                return;

            initialized = true;
            Vector2 center = Projectile.Center;
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.Center = center;

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.34f, Pitch = 0.2f }, center);
            SpawnBlastFX(center);
        }

        private void SpawnBlastFX(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);

            StrongBloom bloom = new(center, Vector2.Zero, Color.Lerp(mainColor, Color.White, 0.2f), 0.95f, 12);
            GeneralParticleHandler.SpawnParticle(bloom);

            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.24f),
                new Vector2(1.15f, 1.15f),
                0f,
                0.18f,
                0.034f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GemEmerald,
                    Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(1.4f, 3.8f),
                    100,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.18f, 0.55f)),
                    Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                GlowOrbParticle orb = new(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 2.8f),
                    false,
                    12,
                    Main.rand.NextFloat(0.24f, 0.38f),
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.2f, 0.45f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }
    }
}
