using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFDog_ChargeOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float Charge => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float FullChargePulseCreated => ref Projectile.localAI[1];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.penetrate = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            int index = (int)HoldoutIndex;
            if (!Main.projectile.IndexInRange(index) || !Main.projectile[index].active || Main.projectile[index].ModProjectile is not NewLegendPristineFuryHoldOut holdout || holdout.CurrentMark != PristineFuryMark.Dog)
            {
                Projectile.Kill();
                return;
            }

            float charge = MathHelper.Clamp(Charge, 0f, 1f);
            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Projectile.Center = holdout.GunTipPosition + direction * (8f + charge * 8f);
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();
            Projectile.timeLeft = 2;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (0.3f + charge * 1.35f));

            if (charge < 0.98f)
                FullChargePulseCreated = 0f;

            if (Main.dedServ)
                return;

            SpawnChargeParticles(direction, charge);

            if (charge >= 1f && FullChargePulseCreated == 0f)
            {
                FullChargePulseCreated = 1f;
                SpawnFullChargePulse(direction);
            }
        }

        private void SpawnChargeParticles(Vector2 direction, float charge)
        {
            Color theme = Color.Lerp(ThemeColor, Color.White, charge * 0.32f);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);

            if (Main.rand.NextFloat() < 0.34f + charge * 0.54f)
            {
                Vector2 offset = -direction * Main.rand.NextFloat(22f, 82f + charge * 46f) + side * Main.rand.NextFloat(-18f - charge * 24f, 18f + charge * 24f);
                Vector2 spawnPosition = Projectile.Center + offset;
                Vector2 pullVelocity = -offset.SafeNormalize(direction) * Main.rand.NextFloat(2.1f, 5.8f + charge * 2.5f);
                Color particleColor = Main.rand.NextBool(4)
                    ? Color.White
                    : Color.Lerp(theme, Color.Gold, Main.rand.NextFloat(0.12f, 0.48f));

                Particle spark = Main.rand.NextBool(3)
                    ? new SparkParticle(
                        spawnPosition,
                        pullVelocity,
                        false,
                        Main.rand.Next(13, 24),
                        Main.rand.NextFloat(0.55f, 1.05f) * (0.75f + charge * 0.55f),
                        particleColor)
                    : new GlowOrbParticle(
                        spawnPosition,
                        pullVelocity,
                        false,
                        Main.rand.Next(10, 18),
                        Main.rand.NextFloat(0.22f, 0.44f) * (0.9f + charge),
                        particleColor,
                        true,
                        false,
                        true);

                GeneralParticleHandler.SpawnParticle(spark);
            }

            if ((int)Timer % 5 == 0)
            {
                Particle core = new SquishyLightParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f + charge * 8f, 5f + charge * 8f),
                    Main.rand.NextVector2Circular(0.28f, 0.28f) - direction * Main.rand.NextFloat(0.05f, 0.18f),
                    Main.rand.NextFloat(0.32f, 0.62f) * (0.85f + charge * 0.7f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.18f, 0.55f)),
                    Main.rand.Next(14, 23));

                GeneralParticleHandler.SpawnParticle(core);
            }

            if (charge > 0.35f && Main.rand.NextBool(5))
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(8f, 36f) + side * Main.rand.NextFloat(-12f, 12f),
                    -direction * Main.rand.NextFloat(0.35f, 1.25f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    Color.Lerp(theme, Color.DarkGoldenrod, 0.34f),
                    Main.rand.Next(18, 31),
                    Main.rand.NextFloat(0.42f, 0.78f) * (0.85f + charge * 0.45f),
                    0.58f,
                    Main.rand.NextFloat(-0.055f, 0.055f),
                    glowing: true);

                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (charge > 0.62f && Main.rand.NextBool(3))
            {
                Particle crack = new GlowOrbParticle(
                    Projectile.Center + side * Main.rand.NextFloat(-24f, 24f) - direction * Main.rand.NextFloat(4f, 28f),
                    -direction.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.6f, 1.8f),
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.22f, 0.4f) * (0.85f + charge * 0.55f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.08f, 0.28f)),
                    true,
                    false,
                    true);

                GeneralParticleHandler.SpawnParticle(crack);
            }
        }

        private void SpawnFullChargePulse(Vector2 direction)
        {
            Color theme = Color.Lerp(ThemeColor, Color.White, 0.24f);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                theme * 0.9f,
                Vector2.One,
                direction.ToRotation(),
                0.08f,
                1.35f,
                24));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.White * 0.55f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.08f,
                0.82f,
                18,
                false));

            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 28f).ToRotationVector2() * Main.rand.NextFloat(5.4f, 8.2f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.GoldFlame : ModContent.DustType<SquashDust>(),
                    velocity,
                    0,
                    Main.rand.NextBool(4) ? Color.White : theme,
                    Main.rand.NextFloat(1.0f, 1.45f));

                dust.noGravity = true;
                dust.fadeIn = 1.25f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float charge = MathHelper.Clamp(Charge, 0f, 1f);
            if (charge <= 0.02f || Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D circularSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmear").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = Color.Lerp(ThemeColor, Color.White, charge * 0.42f);
            Color color = (theme with { A = 0 }) * charge;
            Color white = (Color.White with { A = 0 }) * charge;
            float pulse = 0.86f + 0.14f * (float)System.Math.Sin(Timer * 0.18f);
            float chargeScale = 0.35f + charge * 1.35f;

            PFLeftEffectRules.BeginAdditive();

            for (int i = 0; i < 3; i++)
            {
                Color bloomColor = Color.Lerp(color, white, i * 0.2f);
                float scale = (0.17f + chargeScale * (0.19f - i * 0.035f)) * pulse;
                Main.EntitySpriteDraw(bloom, drawPosition, null, bloomColor * (0.72f - i * 0.12f), Projectile.rotation + Main.rand.NextFloat(-0.3f, 0.3f), bloom.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(ring, drawPosition, null, color * (0.28f + charge * 0.42f), Projectile.rotation + Timer * 0.035f, ring.Size() * 0.5f, (0.16f + charge * 0.44f) * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(circularSmear, drawPosition, null, color * 0.3f, -Projectile.rotation + Timer * 0.04f, circularSmear.Size() * 0.5f, 0.16f + charge * 0.42f, SpriteEffects.None, 0);

            if (charge > 0.42f)
            {
                float orbitRadius = (12f + charge * 22f) * (0.55f + charge * 0.45f);
                for (int i = 0; i < 3; i++)
                {
                    float angle = Timer * (0.035f + i * 0.006f) + MathHelper.TwoPi * i / 3f;
                    Vector2 orbitPosition = drawPosition + angle.ToRotationVector2() * orbitRadius;
                    float orbitPulse = 0.8f + 0.2f * (float)System.Math.Sin(Timer * 0.21f + i);
                    Color orbitColor = Color.Lerp(color, white, 0.22f + i * 0.12f);
                    Main.EntitySpriteDraw(bloom, orbitPosition, null, orbitColor * 0.45f, angle, bloom.Size() * 0.5f, (0.09f + charge * 0.12f) * orbitPulse, SpriteEffects.None, 0);
                }
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
