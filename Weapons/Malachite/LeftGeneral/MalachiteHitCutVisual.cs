using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite.LeftGeneral
{
    public sealed class MalachiteHitCutVisual : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private bool Enhanced => Projectile.ai[1] >= 1f;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == 1f)
                SpawnParallelCutParticles();

            Lighting.AddLight(Projectile.Center, 0.04f, 0.18f, 0.04f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        private void SpawnParallelCutParticles()
        {
            Vector2 forward = Projectile.ai[0].ToRotationVector2();
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            int count = Enhanced ? 24 : 15;
            float spread = Enhanced ? 72f : 46f;
            Color baseColor = Enhanced ? new Color(198, 255, 112) : new Color(96, 255, 135);

            Particle sparkle = new GenericSparkle(
                Projectile.Center,
                Vector2.Zero,
                Color.White,
                baseColor,
                Enhanced ? 1.15f : 0.82f,
                12,
                0.025f,
                1.05f,
                false);
            GeneralParticleHandler.SpawnParticle(sparkle);

            for (int i = 0; i < count; i++)
            {
                float centered = i - (count - 1) * 0.5f;
                Vector2 position =
                    Projectile.Center -
                    forward * Main.rand.NextFloat(56f, 104f) +
                    normal * (centered / Math.Max(1f, count - 1f) * spread + Main.rand.NextFloat(-2.5f, 2.5f));
                Vector2 velocity = forward * Main.rand.NextFloat(7.5f, Enhanced ? 13.5f : 10.5f);
                Color color = Color.Lerp(baseColor, Color.White, Main.rand.NextFloat(0.08f, 0.22f)) * Main.rand.NextFloat(0.68f, 0.9f);

                Particle line = Main.rand.NextBool()
                    ? new AltSparkParticle(position, velocity, false, Main.rand.Next(10, 15), Main.rand.NextFloat(0.46f, 0.72f), color)
                    : new LineParticle(position, velocity * 0.38f, false, Main.rand.Next(11, 16), Main.rand.NextFloat(0.55f, 0.86f), color);
                GeneralParticleHandler.SpawnParticle(line);

                if (i % 4 != 0)
                    continue;

                Particle softLine = new CustomSpark(
                    position,
                    velocity * 0.14f,
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    2,
                    Main.rand.NextFloat(0.36f, 0.52f),
                    color * 0.62f,
                    new Vector2(1.75f, 0.34f),
                    true,
                    true,
                    0f,
                    false,
                    false,
                    0.54f,
                    0.82f,
                    0.82f,
                    false,
                    false,
                    0f);
                GeneralParticleHandler.SpawnParticle(softLine);
            }
        }
    }
}
