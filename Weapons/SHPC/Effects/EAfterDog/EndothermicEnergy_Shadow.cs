using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog
{
    internal class EndothermicEnergy_Shadow : ModProjectile, ILocalizedModType
    {
        private static readonly Color FrostWhite = new(240, 250, 255);
        private static readonly Color FrostBlue = new(150, 205, 255);
        private static readonly Color FrostDeep = new(72, 120, 210);

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float Timer => ref Projectile.ai[0];
        public ref float TargetIndex => ref Projectile.ai[1];
        public ref float OrbitAngle => ref Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 40;
            Projectile.extraUpdates = 0;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            NPC target = Main.npc.IndexInRange((int)TargetIndex) ? Main.npc[(int)TargetIndex] : null;
            if (target == null || !target.active || !target.CanBeChasedBy())
            {
                Projectile.Kill();
                return;
            }

            float radius = MathHelper.Lerp(48f, 88f, Utils.GetLerpValue(0f, 15f, Timer, true));
            Vector2 offset = OrbitAngle.ToRotationVector2() * radius;
            Projectile.Center = target.Center + offset;

            float appearProgress = Utils.GetLerpValue(0f, 15f, Timer, true);
            Lighting.AddLight(Projectile.Center, Color.Lerp(FrostBlue, FrostWhite, 0.35f).ToVector3() * (0.18f + appearProgress * 0.5f));

            if (Timer < 15f)
            {
                float pulseScale = 0.55f + appearProgress * 1.45f;

                if (Main.rand.NextBool(2))
                {
                    Vector2 swirl = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(10f, 24f) * (1f - appearProgress * 0.35f);
                    Particle bloom = new CustomPulse(
                        Projectile.Center + swirl * 0.15f,
                        Vector2.Zero,
                        Color.Lerp(FrostBlue, FrostWhite, Main.rand.NextFloat(0.35f, 0.8f)) * 0.35f,
                        "CalamityMod/Particles/LargeBloom",
                        new Vector2(0.8f, 1.35f),
                        Main.rand.NextFloat(-0.2f, 0.2f),
                        0.12f * pulseScale,
                        0f,
                        8
                    );
                    GeneralParticleHandler.SpawnParticle(bloom);
                }

                if (Main.rand.NextBool(2))
                {
                    Vector2 sparkVel = Main.rand.NextVector2Circular(0.7f, 0.7f) - offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.15f, 0.45f);
                    GlowSparkParticle spark = new GlowSparkParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        sparkVel,
                        false,
                        Main.rand.Next(10, 15),
                        Main.rand.NextFloat(0.018f, 0.03f),
                        Color.Lerp(FrostWhite, FrostBlue, Main.rand.NextFloat(0.2f, 0.6f)),
                        new Vector2(1.6f, 1f),
                        true,
                        false,
                        1.05f
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                        Main.rand.NextBool() ? DustID.IceTorch : DustID.GemDiamond,
                        Main.rand.NextVector2Circular(0.8f, 0.8f),
                        0,
                        Color.Lerp(FrostWhite, FrostBlue, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.95f, 1.25f)
                    );
                    dust.noGravity = true;
                }
            }
            else
            {
                BurstSplits(target);
                Projectile.Kill();
                return;
            }

            Timer++;
        }

        private void BurstSplits(NPC target)
        {
            int splitCount = Main.rand.Next(6, 11);
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);

            for (int i = 0; i < splitCount; i++)
            {
                float angle = baseAngle + MathHelper.TwoPi * i / splitCount + Main.rand.NextFloat(-0.2f, 0.2f);
                float speed = Main.rand.NextFloat(7.5f, 12.5f);
                Vector2 velocity = angle.ToRotationVector2() * speed;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    velocity,
                    ModContent.ProjectileType<EndothermicEnergy_Split>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner
                );
            }

            for (int i = 0; i < 3; i++)
            {
                Particle bloom = new CustomPulse(
                    Projectile.Center,
                    Vector2.Zero,
                    i == 0 ? FrostWhite * 0.8f : Color.Lerp(FrostBlue, FrostDeep, i * 0.35f) * 0.65f,
                    "CalamityMod/Particles/LargeBloom",
                    new Vector2(1f, 1.25f),
                    Main.rand.NextFloat(-0.35f, 0.35f),
                    0.4f - i * 0.08f,
                    0f,
                    16
                );
                GeneralParticleHandler.SpawnParticle(bloom);
            }

            for (int i = 0; i < 12; i++)
            {
                GlowSparkParticle spark = new GlowSparkParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 7f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.02f, 0.035f),
                    Color.Lerp(FrostWhite, FrostBlue, Main.rand.NextFloat(0.2f, 0.75f)),
                    new Vector2(1.9f, 1f),
                    true,
                    false,
                    1.08f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.IceTorch : DustID.GemDiamond,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6.5f),
                    0,
                    Color.Lerp(FrostWhite, FrostBlue, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, 1.35f)
                );
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Asset<Texture2D> smearTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearRagged");
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float appearProgress = Utils.GetLerpValue(0f, 15f, Timer, true);
            float pulse = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + OrbitAngle) * 0.08f;

            Color outerColor = Color.Lerp(FrostBlue, FrostWhite, 0.45f) * (0.2f + appearProgress * 0.45f);
            Color coreColor = FrostWhite * (0.25f + appearProgress * 0.7f);

            //Main.EntitySpriteDraw(
            //    smearTexture.Value,
            //    drawPosition,
            //    null,
            //    outerColor,
            //    OrbitAngle,
            //    smearTexture.Size() * 0.5f,
            //    new Vector2(0.26f + appearProgress * 0.18f, 0.7f + appearProgress * 0.45f) * pulse,
            //    SpriteEffects.None);

            //Main.EntitySpriteDraw(
            //    bloomTexture.Value,
            //    drawPosition,
            //    null,
            //    outerColor,
            //    0f,
            //    bloomTexture.Size() * 0.5f,
            //    0.3f + appearProgress * 0.45f,
            //    SpriteEffects.None);

            //Main.EntitySpriteDraw(
            //    bloomTexture.Value,
            //    drawPosition,
            //    null,
            //    coreColor,
            //    0f,
            //    bloomTexture.Size() * 0.5f,
            //    0.18f + appearProgress * 0.2f,
            //    SpriteEffects.None);

            return false;
        }
    }
}
