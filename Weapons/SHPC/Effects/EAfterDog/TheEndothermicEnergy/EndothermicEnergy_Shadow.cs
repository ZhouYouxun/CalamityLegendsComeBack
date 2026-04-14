using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheEndothermicEnergy
{
    internal class EndothermicEnergy_Shadow : ModProjectile, ILocalizedModType
    {
        private static readonly Color FrostWhite = new(240, 250, 255);
        private static readonly Color FrostBlue = new(150, 205, 255);
        private static readonly Color FrostDeep = new(72, 120, 210);

        public new string LocalizationCategory => "Projectiles.SHPC";
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

            Vector2 forward = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            float goldenAngle = MathHelper.TwoPi * 0.38196601125f;

            // ===== 主中心轴：冰霜脉冲核心 =====
            for (int i = 0; i < 3; i++)
            {
                Particle corePulse = new CustomPulse(
                    Projectile.Center,
                    Vector2.Zero,
                    i == 0 ? FrostWhite * 0.8f : Color.Lerp(FrostBlue, FrostDeep, i * 0.38f) * 0.72f,
                    "CalamityLegendsComeBack/Texture/Myown/christmas512",
                    new Vector2(0.85f, 0.85f),
                    Main.rand.NextFloat(-0.35f, 0.35f),
                    0.024f + i * 0.004f,
                    0.095f + i * 0.012f,
                    16 + i * 2
                );
                GeneralParticleHandler.SpawnParticle(corePulse);
            }

            // ===== 冰晶爆感：小十字星沿前向和外环炸开 =====
            for (int i = 0; i < 18; i++)
            {
                Vector2 direction = i < 8
                    ? forward.RotatedBy(MathHelper.Lerp(-0.9f, 0.9f, i / 7f))
                    : Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 sparkVelocity = direction.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4)) * Main.rand.NextFloat(3.6f, 6.4f);
                CritSpark spark = new CritSpark(
                    Projectile.Center,
                    sparkVelocity,
                    FrostWhite,
                    Color.Lerp(FrostBlue, FrostDeep, Main.rand.NextFloat(0.2f, 0.7f)),
                    Main.rand.NextFloat(0.72f, 0.95f),
                    Main.rand.Next(12, 18)
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ===== 爆炸感：高能边缘拖尾 =====
            for (int i = 0; i < 20; i++)
            {
                Vector2 glowVelocity = forward.RotatedByRandom(1.1f) * Main.rand.NextFloat(2.2f, 5.8f);
                GlowSparkParticle glowSpark = new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    glowVelocity,
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.16f, 0.25f),
                    Color.Lerp(FrostWhite, FrostBlue, Main.rand.NextFloat(0.15f, 0.75f)),
                    new Vector2(1.8f, 0.45f),
                    true,
                    false
                );
                GeneralParticleHandler.SpawnParticle(glowSpark);
            }

            // ===== 迷雾感：轻柔寒气云层 =====
            for (int i = 0; i < 16; i++)
            {
                Vector2 mistVelocity = new Vector2(
                    Main.rand.NextFloat(-1.1f, 1.1f),
                    Main.rand.NextFloat(-2.8f, -0.8f)).RotatedByRandom(0.75f);
                Particle mediumMist = new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    mistVelocity,
                    Color.Lerp(FrostWhite, FrostBlue, Main.rand.NextFloat(0.2f, 0.7f)) * 0.75f,
                    Color.Transparent,
                    Main.rand.NextFloat(0.45f, 0.72f),
                    Main.rand.NextFloat(140f, 190f)
                );
                GeneralParticleHandler.SpawnParticle(mediumMist);
            }

            // ===== 复杂层次：黄金角冰环 =====
            for (int i = 0; i < 24; i++)
            {
                float t = i + 1f;
                float angle = goldenAngle * t;
                float radius = 5.6f * (float)Math.Sqrt(t) * 3.2f;
                Vector2 offset = angle.ToRotationVector2() * radius;
                Vector2 tangential = offset.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * (1.4f + t * 0.05f);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    Main.rand.NextBool() ? DustID.IceTorch : DustID.GemDiamond,
                    tangential + forward * Main.rand.NextFloat(0.8f, 2.2f),
                    0,
                    Color.Lerp(FrostWhite, FrostBlue, Main.rand.NextFloat(0.15f, 0.85f)),
                    Main.rand.NextFloat(0.8f, 1.1f)
                );
                dust.noGravity = true;
                dust.fadeIn = 0.35f;
            }

            // ===== 末端收束：小型白核终闪 =====
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemDiamond,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 4.8f),
                    0,
                    FrostWhite,
                    Main.rand.NextFloat(0.9f, 1.2f)
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
